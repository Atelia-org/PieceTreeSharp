// Source: ts/src/vs/editor/common/diff/rangeMapping.ts
// - Class: LineRange (Lines: 1-18)
// - Class: LineRangeSet (additional C# implementation for set operations)
// Ported: 2025-11-19
//
// Note: LineRange is a lightweight struct in TS; C# version adds LineRangeSet
// for efficient range set operations not present in the original TS implementation.

using System;
using System.Collections.Generic;
using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Diff;

/// <summary>
/// A 1-based half-open range of lines (<c>[start, end)</c>).
/// </summary>
public readonly struct LineRange : IEquatable<LineRange>
{
    public int StartLineNumber { get; }
    public int EndLineNumberExclusive { get; }

    public static LineRange OfLength(int startLineNumber, int length) => new(startLineNumber, startLineNumber + length);

    public LineRange(int startLineNumber, int endLineNumberExclusive)
    {
        if (startLineNumber > endLineNumberExclusive)
        {
            throw new ArgumentException("startLineNumber cannot be after endLineNumberExclusive", nameof(startLineNumber));
        }

        StartLineNumber = startLineNumber;
        EndLineNumberExclusive = endLineNumberExclusive;
    }

    public int Length => EndLineNumberExclusive - StartLineNumber;
    public bool IsEmpty => Length == 0;

    public bool Contains(int lineNumber) => StartLineNumber <= lineNumber && lineNumber < EndLineNumberExclusive;

    public bool Contains(LineRange other) => StartLineNumber <= other.StartLineNumber && other.EndLineNumberExclusive <= EndLineNumberExclusive;

    public LineRange Delta(int offset) => new(StartLineNumber + offset, EndLineNumberExclusive + offset);

    public LineRange DeltaLength(int offset) => new(StartLineNumber, EndLineNumberExclusive + offset);

    public LineRange Join(LineRange other) => new(Math.Min(StartLineNumber, other.StartLineNumber), Math.Max(EndLineNumberExclusive, other.EndLineNumberExclusive));

    public LineRange? Intersect(LineRange other)
    {
        int start = Math.Max(StartLineNumber, other.StartLineNumber);
        int end = Math.Min(EndLineNumberExclusive, other.EndLineNumberExclusive);
        if (start <= end)
        {
            return new LineRange(start, end);
        }

        return null;
    }

    public bool IntersectsOrTouches(LineRange other)
    {
        return StartLineNumber <= other.EndLineNumberExclusive && other.StartLineNumber <= EndLineNumberExclusive;
    }

    public OffsetRange ToOffsetRange()
    {
        return new OffsetRange(StartLineNumber - 1, EndLineNumberExclusive - 1);
    }

    public Range? ToInclusiveRange()
    {
        if (IsEmpty)
        {
            return null;
        }

        TextPosition start = new(StartLineNumber, 1);
        TextPosition end = new(EndLineNumberExclusive - 1, int.MaxValue);
        return new Range(start, end);
    }

    public Range ToExclusiveRange()
    {
        return new Range(new TextPosition(StartLineNumber, 1), new TextPosition(EndLineNumberExclusive, 1));
    }

    public override string ToString() => $"[{StartLineNumber},{EndLineNumberExclusive})";

    public bool Equals(LineRange other)
    {
        return StartLineNumber == other.StartLineNumber && EndLineNumberExclusive == other.EndLineNumberExclusive;
    }

    public override bool Equals(object? obj)
    {
        return obj is LineRange other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StartLineNumber, EndLineNumberExclusive);
    }
}

internal sealed class LineRangeSet
{
    private readonly List<LineRange> _ranges;

    public LineRangeSet()
    {
        _ranges = [];
    }

    public LineRangeSet(IEnumerable<LineRange> ranges)
    {
        _ranges = [];
        foreach (LineRange range in ranges)
        {
            AddRange(range);
        }
    }

    public IReadOnlyList<LineRange> Ranges => _ranges;

    public void AddRange(LineRange range)
    {
        if (range.IsEmpty)
        {
            return;
        }

        int insertIndex = 0;
        while (insertIndex < _ranges.Count && _ranges[insertIndex].EndLineNumberExclusive < range.StartLineNumber)
        {
            insertIndex++;
        }

        int endIndex = insertIndex;
        while (endIndex < _ranges.Count && _ranges[endIndex].StartLineNumber <= range.EndLineNumberExclusive)
        {
            endIndex++;
        }

        if (insertIndex == endIndex)
        {
            _ranges.Insert(insertIndex, range);
            return;
        }

        LineRange merged = range;
        merged = merged.Join(_ranges[insertIndex]);
        merged = merged.Join(_ranges[endIndex - 1]);
        _ranges.RemoveRange(insertIndex, endIndex - insertIndex);
        _ranges.Insert(insertIndex, merged);
    }

    public bool Contains(int lineNumber)
    {
        (bool found, int index) = BinarySearch(lineNumber);
        if (found)
        {
            return true;
        }

        if (index < _ranges.Count)
        {
            return _ranges[index].Contains(lineNumber);
        }

        if (index > 0)
        {
            return _ranges[index - 1].Contains(lineNumber);
        }

        return false;
    }

    public LineRangeSet SubtractFrom(LineRange range)
    {
        if (range.IsEmpty)
        {
            return new LineRangeSet();
        }

        List<LineRange> result = [];
        int cursor = range.StartLineNumber;
        foreach (LineRange current in _ranges)
        {
            if (current.EndLineNumberExclusive <= cursor)
            {
                continue;
            }

            if (current.StartLineNumber >= range.EndLineNumberExclusive)
            {
                break;
            }

            if (current.StartLineNumber > cursor)
            {
                result.Add(new LineRange(cursor, Math.Min(current.StartLineNumber, range.EndLineNumberExclusive)));
            }

            cursor = Math.Max(cursor, current.EndLineNumberExclusive);
            if (cursor >= range.EndLineNumberExclusive)
            {
                break;
            }
        }

        if (cursor < range.EndLineNumberExclusive)
        {
            result.Add(new LineRange(cursor, range.EndLineNumberExclusive));
        }

        return new LineRangeSet(result);
    }

    public LineRangeSet GetIntersection(LineRangeSet other)
    {
        List<LineRange> result = [];
        int i = 0;
        int j = 0;
        while (i < _ranges.Count && j < other._ranges.Count)
        {
            LineRange a = _ranges[i];
            LineRange b = other._ranges[j];
            LineRange? intersect = a.Intersect(b);

            if (intersect is LineRange intersection && !intersection.IsEmpty)
            {
                result.Add(intersection);
            }

            if (a.EndLineNumberExclusive < b.EndLineNumberExclusive)
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return new LineRangeSet(result);
    }

    public LineRangeSet GetWithDelta(int delta)
    {
        List<LineRange> shifted = new(_ranges.Count);
        foreach (LineRange range in _ranges)
        {
            shifted.Add(range.Delta(delta));
        }

        return new LineRangeSet(shifted);
    }

    private (bool found, int index) BinarySearch(int lineNumber)
    {
        int low = 0;
        int high = _ranges.Count - 1;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            LineRange current = _ranges[mid];
            if (lineNumber < current.StartLineNumber)
            {
                high = mid - 1;
            }
            else if (lineNumber >= current.EndLineNumberExclusive)
            {
                low = mid + 1;
            }
            else
            {
                return (true, mid);
            }
        }

        return (false, low);
    }
}
