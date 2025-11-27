// Source: ts/src/vs/editor/common/diff/rangeMapping.ts
// - Class: OffsetRange (Lines: 76-107)
// Ported: 2025-11-19
//
// Note: In TS, OffsetRange is defined within rangeMapping.ts alongside LineRange and RangeMapping.

namespace PieceTree.TextBuffer.Diff;

public readonly struct OffsetRange : IEquatable<OffsetRange>
{
    public int Start { get; }
    public int EndExclusive { get; }

    public static OffsetRange Empty(int offset) => new(offset, offset);
    public static OffsetRange OfLength(int length) => new(0, length);
    public static OffsetRange OfStartAndLength(int start, int length) => new(start, start + length);

    public OffsetRange(int start, int endExclusive)
    {
        if (start > endExclusive)
        {
            throw new ArgumentException($"Invalid offset range: [{start},{endExclusive})");
        }

        Start = start;
        EndExclusive = endExclusive;
    }

    public int Length => EndExclusive - Start;
    public bool IsEmpty => Start == EndExclusive;

    public OffsetRange Delta(int offset) => new(Start + offset, EndExclusive + offset);
    public OffsetRange DeltaStart(int offset) => new(Start + offset, EndExclusive);
    public OffsetRange DeltaEnd(int offset) => new(Start, EndExclusive + offset);

    public bool Contains(int offset) => Start <= offset && offset < EndExclusive;
    public bool Contains(OffsetRange other) => Start <= other.Start && other.EndExclusive <= EndExclusive;

    public OffsetRange Join(OffsetRange other) => new(Math.Min(Start, other.Start), Math.Max(EndExclusive, other.EndExclusive));

    public OffsetRange? Intersect(OffsetRange other)
    {
        int start = Math.Max(Start, other.Start);
        int end = Math.Min(EndExclusive, other.EndExclusive);
        if (start <= end)
        {
            return new OffsetRange(start, end);
        }

        return null;
    }

    public bool Intersects(OffsetRange other)
    {
        int start = Math.Max(Start, other.Start);
        int end = Math.Min(EndExclusive, other.EndExclusive);
        return start < end;
    }

    public bool IntersectsOrTouches(OffsetRange other)
    {
        int start = Math.Max(Start, other.Start);
        int end = Math.Min(EndExclusive, other.EndExclusive);
        return start <= end;
    }

    public OffsetRange WithMargin(int marginStart, int marginEnd)
    {
        return new OffsetRange(Start - marginStart, EndExclusive + marginEnd);
    }

    public IEnumerable<int> Enumerate()
    {
        for (int i = Start; i < EndExclusive; i++)
        {
            yield return i;
        }
    }

    public OffsetRange JoinRightTouching(OffsetRange other)
    {
        if (EndExclusive != other.Start)
        {
            throw new InvalidOperationException($"Ranges {this} and {other} are not touching");
        }

        return new OffsetRange(Start, other.EndExclusive);
    }

    public override string ToString() => $"[{Start},{EndExclusive})";

    public bool Equals(OffsetRange other)
    {
        return Start == other.Start && EndExclusive == other.EndExclusive;
    }

    public override bool Equals(object? obj)
    {
        return obj is OffsetRange other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, EndExclusive);
    }
}
