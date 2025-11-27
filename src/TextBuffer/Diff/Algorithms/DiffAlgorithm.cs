// Source: ts/src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/diffAlgorithm.ts
// - Interface: IDiffAlgorithm
// - Class: DiffAlgorithmResult, SequenceDiff, OffsetPair
// - Interfaces: ISequence, ITimeout
// - Classes: InfiniteTimeout, DateTimeout
// Ported: 2025-11-21

using System;
using System.Collections.Generic;
using PieceTree.TextBuffer.Diff;

namespace PieceTree.TextBuffer.Diff.Algorithms;

internal interface IDiffAlgorithm
{
    DiffAlgorithmResult Compute(ISequence sequence1, ISequence sequence2, ITimeout? timeout = null, Func<int, int, double>? equalityScore = null);
}

internal sealed class DiffAlgorithmResult
{
    public static DiffAlgorithmResult Trivial(ISequence seq1, ISequence seq2)
    {
        return new DiffAlgorithmResult(new[] { new SequenceDiff(OffsetRange.OfLength(seq1.Length), OffsetRange.OfLength(seq2.Length)) }, false);
    }

    public static DiffAlgorithmResult TrivialTimedOut(ISequence seq1, ISequence seq2)
    {
        return new DiffAlgorithmResult(new[] { new SequenceDiff(OffsetRange.OfLength(seq1.Length), OffsetRange.OfLength(seq2.Length)) }, true);
    }

    public DiffAlgorithmResult(IReadOnlyList<SequenceDiff> diffs, bool hitTimeout)
    {
        Diffs = diffs;
        HitTimeout = hitTimeout;
    }

    public IReadOnlyList<SequenceDiff> Diffs { get; }
    public bool HitTimeout { get; }
}

internal sealed class SequenceDiff
{
    public static IReadOnlyList<SequenceDiff> Invert(IReadOnlyList<SequenceDiff> sequenceDiffs, int doc1Length)
    {
        List<SequenceDiff> result = [];
        for (int i = 0; i <= sequenceDiffs.Count; i++)
        {
            SequenceDiff? previous = i == 0 ? null : sequenceDiffs[i - 1];
            SequenceDiff? next = i == sequenceDiffs.Count ? null : sequenceDiffs[i];
            OffsetPair startPair = previous?.GetEndExclusives() ?? OffsetPair.Zero;
            OffsetPair endPair;
            if (next != null)
            {
                endPair = next.GetStarts();
            }
            else
            {
                int offset2 = (previous != null ? previous.Seq2Range.EndExclusive - previous.Seq1Range.EndExclusive : 0) + doc1Length;
                endPair = new OffsetPair(doc1Length, offset2);
            }

            result.Add(FromOffsetPairs(startPair, endPair));
        }

        return result;
    }

    public static SequenceDiff FromOffsetPairs(OffsetPair start, OffsetPair endExclusive)
    {
        return new SequenceDiff(new OffsetRange(start.Offset1, endExclusive.Offset1), new OffsetRange(start.Offset2, endExclusive.Offset2));
    }

    public SequenceDiff(OffsetRange seq1Range, OffsetRange seq2Range)
    {
        Seq1Range = seq1Range;
        Seq2Range = seq2Range;
    }

    public OffsetRange Seq1Range { get; }
    public OffsetRange Seq2Range { get; }

    public SequenceDiff Swap() => new(Seq2Range, Seq1Range);

    public SequenceDiff Join(SequenceDiff other)
    {
        return new SequenceDiff(Seq1Range.Join(other.Seq1Range), Seq2Range.Join(other.Seq2Range));
    }

    public SequenceDiff Delta(int offset)
    {
        if (offset == 0)
        {
            return this;
        }

        return new SequenceDiff(Seq1Range.Delta(offset), Seq2Range.Delta(offset));
    }

    public SequenceDiff DeltaStart(int offset)
    {
        if (offset == 0)
        {
            return this;
        }

        return new SequenceDiff(Seq1Range.DeltaStart(offset), Seq2Range.DeltaStart(offset));
    }

    public SequenceDiff DeltaEnd(int offset)
    {
        if (offset == 0)
        {
            return this;
        }

        return new SequenceDiff(Seq1Range.DeltaEnd(offset), Seq2Range.DeltaEnd(offset));
    }

    public bool IntersectsOrTouches(SequenceDiff other)
    {
        return Seq1Range.IntersectsOrTouches(other.Seq1Range) || Seq2Range.IntersectsOrTouches(other.Seq2Range);
    }

    public SequenceDiff? Intersect(SequenceDiff other)
    {
        OffsetRange? i1 = Seq1Range.Intersect(other.Seq1Range);
        OffsetRange? i2 = Seq2Range.Intersect(other.Seq2Range);
        if (i1.HasValue && i2.HasValue)
        {
            return new SequenceDiff(i1.Value, i2.Value);
        }

        return null;
    }

    public OffsetPair GetStarts() => new(Seq1Range.Start, Seq2Range.Start);

    public OffsetPair GetEndExclusives() => new(Seq1Range.EndExclusive, Seq2Range.EndExclusive);
}

internal readonly struct OffsetPair
{
    public static readonly OffsetPair Zero = new(0, 0);
    public static readonly OffsetPair Max = new(int.MaxValue, int.MaxValue);

    public OffsetPair(int offset1, int offset2)
    {
        Offset1 = offset1;
        Offset2 = offset2;
    }

    public int Offset1 { get; }
    public int Offset2 { get; }

    public OffsetPair Delta(int offset)
    {
        if (offset == 0)
        {
            return this;
        }

        return new OffsetPair(Offset1 + offset, Offset2 + offset);
    }

    public bool Equals(OffsetPair other)
    {
        return Offset1 == other.Offset1 && Offset2 == other.Offset2;
    }

    public override bool Equals(object? obj)
    {
        return obj is OffsetPair other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Offset1, Offset2);
    }
}

internal interface ISequence
{
    int GetElement(int offset);
    int Length { get; }
    int GetBoundaryScore(int length);
    bool IsStronglyEqual(int offset1, int offset2);
}

internal interface ITimeout
{
    bool IsValid { get; }
}

internal sealed class InfiniteTimeout : ITimeout
{
    public static readonly InfiniteTimeout Instance = new();

    private InfiniteTimeout()
    {
    }

    public bool IsValid => true;
}

internal sealed class DateTimeout : ITimeout
{
    private readonly int _timeoutMs;
    private readonly long _startTicks;
    private bool _stillValid = true;

    public DateTimeout(int timeoutMs)
    {
        if (timeoutMs <= 0)
        {
            throw new ArgumentException("timeout must be positive", nameof(timeoutMs));
        }

        _timeoutMs = timeoutMs;
        _startTicks = Environment.TickCount64;
    }

    public bool IsValid
    {
        get
        {
            if (!_stillValid)
            {
                return false;
            }

            if (Environment.TickCount64 - _startTicks < _timeoutMs)
            {
                return true;
            }

            _stillValid = false;
            return false;
        }
    }
}
