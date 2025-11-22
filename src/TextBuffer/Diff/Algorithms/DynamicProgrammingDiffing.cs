// Source: ts/src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/dynamicProgrammingDiffing.ts
// - Class: DynamicProgrammingDiffing
// - Lines: 10-150
// Ported: 2025-11-21

using System;
using System.Collections.Generic;
using PieceTree.TextBuffer.Diff;

namespace PieceTree.TextBuffer.Diff.Algorithms;

internal sealed class DynamicProgrammingDiffing : IDiffAlgorithm
{
    public DiffAlgorithmResult Compute(ISequence sequence1, ISequence sequence2, ITimeout? timeout = null, Func<int, int, double>? equalityScore = null)
    {
        timeout ??= InfiniteTimeout.Instance;
        if (sequence1.Length == 0 || sequence2.Length == 0)
        {
            return DiffAlgorithmResult.Trivial(sequence1, sequence2);
        }

        var lcsLengths = new Array2D<double>(sequence1.Length, sequence2.Length);
        var directions = new Array2D<int>(sequence1.Length, sequence2.Length);
        var lengths = new Array2D<int>(sequence1.Length, sequence2.Length);

        for (int s1 = 0; s1 < sequence1.Length; s1++)
        {
            for (int s2 = 0; s2 < sequence2.Length; s2++)
            {
                if (!timeout.IsValid)
                {
                    return DiffAlgorithmResult.TrivialTimedOut(sequence1, sequence2);
                }

                var horizontalLen = s1 == 0 ? 0 : lcsLengths.Get(s1 - 1, s2);
                var verticalLen = s2 == 0 ? 0 : lcsLengths.Get(s1, s2 - 1);

                double extendedSeqScore;
                if (sequence1.GetElement(s1) == sequence2.GetElement(s2))
                {
                    extendedSeqScore = (s1 == 0 || s2 == 0) ? 0 : lcsLengths.Get(s1 - 1, s2 - 1);
                    if (s1 > 0 && s2 > 0 && directions.Get(s1 - 1, s2 - 1) == 3)
                    {
                        extendedSeqScore += lengths.Get(s1 - 1, s2 - 1);
                    }

                    extendedSeqScore += equalityScore?.Invoke(s1, s2) ?? 1;
                }
                else
                {
                    extendedSeqScore = -1;
                }

                var newValue = horizontalLen;
                var direction = 1; // horizontal
                if (verticalLen > newValue)
                {
                    newValue = verticalLen;
                    direction = 2; // vertical
                }
                if (extendedSeqScore > newValue)
                {
                    newValue = extendedSeqScore;
                    direction = 3; // diagonal
                }

                if (direction == 3)
                {
                    var prevLen = (s1 > 0 && s2 > 0) ? lengths.Get(s1 - 1, s2 - 1) : 0;
                    lengths.Set(s1, s2, prevLen + 1);
                }
                else
                {
                    lengths.Set(s1, s2, 0);
                }

                directions.Set(s1, s2, direction);
                lcsLengths.Set(s1, s2, newValue);
            }
        }

        var result = new List<SequenceDiff>();
        var lastAligningPosS1 = sequence1.Length;
        var lastAligningPosS2 = sequence2.Length;

        void Report(int s1, int s2)
        {
            if (s1 + 1 != lastAligningPosS1 || s2 + 1 != lastAligningPosS2)
            {
                result.Add(new SequenceDiff(new OffsetRange(s1 + 1, lastAligningPosS1), new OffsetRange(s2 + 1, lastAligningPosS2)));
            }

            lastAligningPosS1 = s1;
            lastAligningPosS2 = s2;
        }

        var i1 = sequence1.Length - 1;
        var i2 = sequence2.Length - 1;
        while (i1 >= 0 && i2 >= 0)
        {
            var dir = directions.Get(i1, i2);
            if (dir == 3)
            {
                Report(i1, i2);
                i1--;
                i2--;
            }
            else if (dir == 1)
            {
                i1--;
            }
            else
            {
                i2--;
            }
        }

        Report(-1, -1);
        result.Reverse();
        return new DiffAlgorithmResult(result, false);
    }
}
