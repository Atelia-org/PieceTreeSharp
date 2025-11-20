using System;
using System.Collections.Generic;
using PieceTree.TextBuffer.Diff;

namespace PieceTree.TextBuffer.Diff.Algorithms;

internal sealed class MyersDiffAlgorithm : IDiffAlgorithm
{
    public DiffAlgorithmResult Compute(ISequence seq1, ISequence seq2, ITimeout? timeout = null, Func<int, int, double>? equalityScore = null)
    {
        timeout ??= InfiniteTimeout.Instance;
        if (seq1.Length == 0 || seq2.Length == 0)
        {
            return DiffAlgorithmResult.Trivial(seq1, seq2);
        }

        int GetXAfterSnake(int x, int y)
        {
            while (x < seq1.Length && y < seq2.Length && seq1.GetElement(x) == seq2.GetElement(y))
            {
                x++;
                y++;
            }

            return x;
        }

        var v = new FastInt32Array();
        var paths = new FastArrayNegativeIndices<SnakePath?>();
        v.Set(0, GetXAfterSnake(0, 0));
        paths.Set(0, v.Get(0) == 0 ? null : new SnakePath(null, 0, 0, v.Get(0)));

        var d = 0;
        var k = 0;

        while (true)
        {
            d++;
            if (!timeout.IsValid)
            {
                return DiffAlgorithmResult.TrivialTimedOut(seq1, seq2);
            }

            var lowerBound = -Math.Min(d, seq2.Length + (d % 2));
            var upperBound = Math.Min(d, seq1.Length + (d % 2));
            for (k = lowerBound; k <= upperBound; k += 2)
            {
                var maxXofDLineTop = k == upperBound ? -1 : v.Get(k + 1);
                var maxXofDLineLeft = k == lowerBound ? -1 : v.Get(k - 1) + 1;
                var x = Math.Min(Math.Max(maxXofDLineTop, maxXofDLineLeft), seq1.Length);
                var y = x - k;
                if (x > seq1.Length || y > seq2.Length)
                {
                    continue;
                }

                var newMaxX = GetXAfterSnake(x, y);
                v.Set(k, newMaxX);
                var lastPath = x == maxXofDLineTop ? paths.Get(k + 1) : paths.Get(k - 1);
                paths.Set(k, newMaxX != x ? new SnakePath(lastPath, x, y, newMaxX - x) : lastPath);

                if (v.Get(k) == seq1.Length && v.Get(k) - k == seq2.Length)
                {
                    goto BuildResult;
                }
            }
        }

    BuildResult:
        var path = paths.Get(k);
        var diffs = new List<SequenceDiff>();
        var lastAligningPosS1 = seq1.Length;
        var lastAligningPosS2 = seq2.Length;

        while (true)
        {
            var endX = path != null ? path.X + path.Length : 0;
            var endY = path != null ? path.Y + path.Length : 0;
            if (endX != lastAligningPosS1 || endY != lastAligningPosS2)
            {
                diffs.Add(new SequenceDiff(new OffsetRange(endX, lastAligningPosS1), new OffsetRange(endY, lastAligningPosS2)));
            }

            if (path == null)
            {
                break;
            }

            lastAligningPosS1 = path.X;
            lastAligningPosS2 = path.Y;
            path = path.Previous;
        }

        diffs.Reverse();
        return new DiffAlgorithmResult(diffs, false);
    }

    private sealed class SnakePath
    {
        public SnakePath(SnakePath? previous, int x, int y, int length)
        {
            Previous = previous;
            X = x;
            Y = y;
            Length = length;
        }

        public SnakePath? Previous { get; }
        public int X { get; }
        public int Y { get; }
        public int Length { get; }
    }

    private sealed class FastInt32Array
    {
        private int[] _positive = new int[16];
        private int[] _negative = new int[16];

        public int Get(int index)
        {
            if (index < 0)
            {
                index = -index - 1;
                return _negative[index];
            }

            return _positive[index];
        }

        public void Set(int index, int value)
        {
            if (index < 0)
            {
                index = -index - 1;
                if (index >= _negative.Length)
                {
                    Array.Resize(ref _negative, _negative.Length * 2);
                }

                _negative[index] = value;
                return;
            }

            if (index >= _positive.Length)
            {
                Array.Resize(ref _positive, _positive.Length * 2);
            }

            _positive[index] = value;
        }
    }

    private sealed class FastArrayNegativeIndices<T>
    {
        private readonly List<T> _positive = new();
        private readonly List<T> _negative = new();

        public T? Get(int index)
        {
            if (index < 0)
            {
                index = -index - 1;
                return index < _negative.Count ? _negative[index] : default;
            }

            return index < _positive.Count ? _positive[index] : default;
        }

        public void Set(int index, T? value)
        {
            if (index < 0)
            {
                index = -index - 1;
                EnsureSize(_negative, index + 1);
                _negative[index] = value!;
                return;
            }

            EnsureSize(_positive, index + 1);
            _positive[index] = value!;
        }

        private static void EnsureSize(List<T> list, int size)
        {
            while (list.Count < size)
            {
                list.Add(default!);
            }
        }
    }
}
