// Source: ts/src/vs/editor/common/diff/defaultLinesDiffComputer/heuristicSequenceOptimizations.ts
// - Functions: optimizeSequenceDiffs, removeShortMatches, extendDiffsToEntireWordIfAppropriate,
//   removeVeryShortMatchingLinesBetweenDiffs, removeVeryShortMatchingTextBetweenLongDiffs,
//   joinSequenceDiffsByShifting, shiftSequenceDiffs (Lines: 12-473)
// Ported: 2025-11-19

using System;
using System.Collections.Generic;
using System.Linq;
using PieceTree.TextBuffer.Diff.Algorithms;

namespace PieceTree.TextBuffer.Diff;

internal static class HeuristicSequenceOptimizations
{
    public static List<SequenceDiff> OptimizeSequenceDiffs(ISequence sequence1, ISequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs)
    {
        var result = JoinSequenceDiffsByShifting(sequence1, sequence2, sequenceDiffs);
        result = JoinSequenceDiffsByShifting(sequence1, sequence2, result);
        result = ShiftSequenceDiffs(sequence1, sequence2, result);
        return result;
    }

    public static List<SequenceDiff> RemoveShortMatches(ISequence sequence1, ISequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs)
    {
        var result = new List<SequenceDiff>();
        foreach (var diff in sequenceDiffs)
        {
            if (result.Count == 0)
            {
                result.Add(diff);
                continue;
            }

            var last = result[^1];
            if (diff.Seq1Range.Start - last.Seq1Range.EndExclusive <= 2 || diff.Seq2Range.Start - last.Seq2Range.EndExclusive <= 2)
            {
                result[^1] = last.Join(diff);
            }
            else
            {
                result.Add(diff);
            }
        }

        return result;
    }

    public static List<SequenceDiff> ExtendDiffsToEntireWordIfAppropriate(
        LinesSliceCharSequence sequence1,
        LinesSliceCharSequence sequence2,
        List<SequenceDiff> sequenceDiffs,
        Func<LinesSliceCharSequence, int, OffsetRange?> findParent,
        bool force = false)
    {
        var equalMappings = SequenceDiff.Invert(sequenceDiffs, sequence1.Length).ToList();
        var additional = new List<SequenceDiff>();
        var lastPoint = OffsetPair.Zero;

        while (equalMappings.Count > 0)
        {
            var next = equalMappings[0];
            equalMappings.RemoveAt(0);
            if (next.Seq1Range.IsEmpty)
            {
                continue;
            }

            ScanWord(sequence1, sequence2, findParent, force, additional, equalMappings, ref lastPoint, next.GetStarts(), next);
            ScanWord(sequence1, sequence2, findParent, force, additional, equalMappings, ref lastPoint, next.GetEndExclusives().Delta(-1), next);
        }

        return MergeSequenceDiffs(sequenceDiffs, additional);
    }

    public static List<SequenceDiff> RemoveVeryShortMatchingLinesBetweenDiffs(LineSequence sequence1, List<SequenceDiff> sequenceDiffs)
    {
        var diffs = sequenceDiffs;
        if (diffs.Count == 0)
        {
            return diffs;
        }

        var counter = 0;
        bool shouldRepeat;
        do
        {
            shouldRepeat = false;
            var result = new List<SequenceDiff> { diffs[0] };
            for (var i = 1; i < diffs.Count; i++)
            {
                var current = diffs[i];
                var last = result[^1];
                var unchangedRange = new OffsetRange(last.Seq1Range.EndExclusive, current.Seq1Range.Start);
                var text = sequence1.GetText(unchangedRange);
                var stripped = text.Replace("\r", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty).Replace(" ", string.Empty);

                if (stripped.Length <= 4 && (last.Seq1Range.Length + last.Seq2Range.Length > 5 || current.Seq1Range.Length + current.Seq2Range.Length > 5))
                {
                    result[^1] = last.Join(current);
                    shouldRepeat = true;
                }
                else
                {
                    result.Add(current);
                }
            }

            diffs = result;
        }
        while (shouldRepeat && counter++ < 10);

        return diffs;
    }

    public static List<SequenceDiff> RemoveVeryShortMatchingTextBetweenLongDiffs(LinesSliceCharSequence sequence1, LinesSliceCharSequence sequence2, List<SequenceDiff> sequenceDiffs)
    {
        var diffs = sequenceDiffs;
        if (diffs.Count == 0)
        {
            return diffs;
        }

        var counter = 0;
        bool shouldRepeat;
        do
        {
            shouldRepeat = false;
            var result = new List<SequenceDiff> { diffs[0] };
            for (var i = 1; i < diffs.Count; i++)
            {
                var current = diffs[i];
                var last = result[^1];

                bool ShouldJoinDiffs(SequenceDiff before, SequenceDiff after)
                {
                    var unchangedRange = new OffsetRange(last.Seq1Range.EndExclusive, current.Seq1Range.Start);
                    var unchangedLineCount = sequence1.CountLinesIn(unchangedRange);
                    if (unchangedLineCount > 5 || unchangedRange.Length > 500)
                    {
                        return false;
                    }

                    var unchangedText = sequence1.GetText(unchangedRange).Trim();
                    if (unchangedText.Length > 20 || unchangedText.Contains('\r') || unchangedText.Contains('\n'))
                    {
                        return false;
                    }

                    var beforeLineCount1 = sequence1.CountLinesIn(before.Seq1Range);
                    var beforeSeq1Length = before.Seq1Range.Length;
                    var beforeLineCount2 = sequence2.CountLinesIn(before.Seq2Range);
                    var beforeSeq2Length = before.Seq2Range.Length;

                    var afterLineCount1 = sequence1.CountLinesIn(after.Seq1Range);
                    var afterSeq1Length = after.Seq1Range.Length;
                    var afterLineCount2 = sequence2.CountLinesIn(after.Seq2Range);
                    var afterSeq2Length = after.Seq2Range.Length;

                    const double max = 2 * 40 + 50;
                    static double Cap(double value, double cap) => Math.Min(value, cap);

                    double Score(double lineCount, double seqLength)
                    {
                        return Math.Pow(Cap(lineCount * 40 + seqLength, max), 1.5);
                    }

                    var beforeScore = Math.Pow(Score(beforeLineCount1, beforeSeq1Length) + Score(beforeLineCount2, beforeSeq2Length), 1.5);
                    var afterScore = Math.Pow(Score(afterLineCount1, afterSeq1Length) + Score(afterLineCount2, afterSeq2Length), 1.5);
                    var threshold = Math.Pow(Math.Pow(max, 1.5), 1.5) * 1.3;
                    return beforeScore + afterScore > threshold;
                }

                if (ShouldJoinDiffs(last, current))
                {
                    result[^1] = last.Join(current);
                    shouldRepeat = true;
                }
                else
                {
                    result.Add(current);
                }
            }

            diffs = result;
        }
        while (counter++ < 10 && shouldRepeat);

        var merged = new List<SequenceDiff>();
        for (var i = 0; i < diffs.Count; i++)
        {
            var prev = i > 0 ? diffs[i - 1] : null;
            var cur = diffs[i];
            var next = i + 1 < diffs.Count ? diffs[i + 1] : null;

            var newDiff = cur;

            bool ShouldMarkAsChanged(string text)
            {
                return text.Length > 0 && text.Trim().Length <= 3 && cur.Seq1Range.Length + cur.Seq2Range.Length > 100;
            }

            var fullRange1 = sequence1.ExtendToFullLines(cur.Seq1Range);
            var prefix = sequence1.GetText(new OffsetRange(fullRange1.Start, cur.Seq1Range.Start));
            if (ShouldMarkAsChanged(prefix))
            {
                newDiff = newDiff.DeltaStart(-prefix.Length);
            }

            var suffix = sequence1.GetText(new OffsetRange(cur.Seq1Range.EndExclusive, fullRange1.EndExclusive));
            if (ShouldMarkAsChanged(suffix))
            {
                newDiff = newDiff.DeltaEnd(suffix.Length);
            }

            var availableSpace = SequenceDiff.FromOffsetPairs(
                prev?.GetEndExclusives() ?? OffsetPair.Zero,
                next?.GetStarts() ?? OffsetPair.Max);

            var clipped = newDiff.Intersect(availableSpace);
            if (clipped == null)
            {
                continue;
            }

            if (merged.Count > 0)
            {
                var last = merged[^1];
                var lastEnd = last.GetEndExclusives();
                var start = clipped.GetStarts();
                if (lastEnd.Offset1 == start.Offset1 && lastEnd.Offset2 == start.Offset2)
                {
                    merged[^1] = last.Join(clipped);
                    continue;
                }
            }

            merged.Add(clipped);
        }

        return merged;
    }

    private static List<SequenceDiff> JoinSequenceDiffsByShifting(ISequence sequence1, ISequence sequence2, IReadOnlyList<SequenceDiff> sequenceDiffs)
    {
        if (sequenceDiffs.Count == 0)
        {
            return new List<SequenceDiff>();
        }

        var result = new List<SequenceDiff> { sequenceDiffs[0] };
        for (var i = 1; i < sequenceDiffs.Count; i++)
        {
            var prevResult = result[^1];
            var current = sequenceDiffs[i];

            if (current.Seq1Range.IsEmpty || current.Seq2Range.IsEmpty)
            {
                var length = current.Seq1Range.Start - prevResult.Seq1Range.EndExclusive;
                var d = 1;
                while (d <= length)
                {
                    if (sequence1.GetElement(current.Seq1Range.Start - d) != sequence1.GetElement(current.Seq1Range.EndExclusive - d)
                        || sequence2.GetElement(current.Seq2Range.Start - d) != sequence2.GetElement(current.Seq2Range.EndExclusive - d))
                    {
                        break;
                    }

                    d++;
                }

                d--;
                if (d == length)
                {
                    result[^1] = new SequenceDiff(
                        new OffsetRange(prevResult.Seq1Range.Start, current.Seq1Range.EndExclusive - length),
                        new OffsetRange(prevResult.Seq2Range.Start, current.Seq2Range.EndExclusive - length));
                    continue;
                }

                current = current.Delta(-d);
            }

            result.Add(current);
        }

        var secondPass = new List<SequenceDiff>();
        for (var i = 0; i < result.Count - 1; i++)
        {
            var next = result[i + 1];
            var current = result[i];

            if (current.Seq1Range.IsEmpty || current.Seq2Range.IsEmpty)
            {
                var length = next.Seq1Range.Start - current.Seq1Range.EndExclusive;
                var d = 0;
                while (d < length)
                {
                    if (!sequence1.IsStronglyEqual(current.Seq1Range.Start + d, current.Seq1Range.EndExclusive + d)
                        || !sequence2.IsStronglyEqual(current.Seq2Range.Start + d, current.Seq2Range.EndExclusive + d))
                    {
                        break;
                    }

                    d++;
                }

                if (d == length)
                {
                    result[i + 1] = new SequenceDiff(
                        new OffsetRange(current.Seq1Range.Start + length, next.Seq1Range.EndExclusive),
                        new OffsetRange(current.Seq2Range.Start + length, next.Seq2Range.EndExclusive));
                    continue;
                }

                if (d > 0)
                {
                    current = current.Delta(d);
                }
            }

            secondPass.Add(current);
        }

        if (result.Count > 0)
        {
            secondPass.Add(result[^1]);
        }

        return secondPass;
    }

    private static List<SequenceDiff> ShiftSequenceDiffs(ISequence sequence1, ISequence sequence2, List<SequenceDiff> diffs)
    {
        if (sequence1 is not { } seq1 || sequence2 is not { } seq2)
        {
            return diffs;
        }

        for (var i = 0; i < diffs.Count; i++)
        {
            var prev = i > 0 ? diffs[i - 1] : null;
            var current = diffs[i];
            var next = i + 1 < diffs.Count ? diffs[i + 1] : null;

            var seq1ValidRange = new OffsetRange(prev != null ? prev.Seq1Range.EndExclusive + 1 : 0, next != null ? next.Seq1Range.Start - 1 : seq1.Length);
            var seq2ValidRange = new OffsetRange(prev != null ? prev.Seq2Range.EndExclusive + 1 : 0, next != null ? next.Seq2Range.Start - 1 : seq2.Length);

            if (current.Seq1Range.IsEmpty)
            {
                diffs[i] = ShiftDiffToBetterPosition(current, sequence1, sequence2, seq1ValidRange, seq2ValidRange);
            }
            else if (current.Seq2Range.IsEmpty)
            {
                diffs[i] = ShiftDiffToBetterPosition(current.Swap(), sequence2, sequence1, seq2ValidRange, seq1ValidRange).Swap();
            }
        }

        return diffs;
    }

    private static SequenceDiff ShiftDiffToBetterPosition(SequenceDiff diff, ISequence sequence1, ISequence sequence2, OffsetRange seq1ValidRange, OffsetRange seq2ValidRange)
    {
        const int maxShiftLimit = 100;
        var deltaBefore = 1;
        while (diff.Seq1Range.Start - deltaBefore >= seq1ValidRange.Start
            && diff.Seq2Range.Start - deltaBefore >= seq2ValidRange.Start
            && sequence2.IsStronglyEqual(diff.Seq2Range.Start - deltaBefore, diff.Seq2Range.EndExclusive - deltaBefore)
            && deltaBefore < maxShiftLimit)
        {
            deltaBefore++;
        }

        deltaBefore--;
        var deltaAfter = 0;
        while (diff.Seq1Range.Start + deltaAfter < seq1ValidRange.EndExclusive
            && diff.Seq2Range.EndExclusive + deltaAfter < seq2ValidRange.EndExclusive
            && sequence2.IsStronglyEqual(diff.Seq2Range.Start + deltaAfter, diff.Seq2Range.EndExclusive + deltaAfter)
            && deltaAfter < maxShiftLimit)
        {
            deltaAfter++;
        }

        if (deltaBefore == 0 && deltaAfter == 0)
        {
            return diff;
        }

        var bestDelta = 0;
        var bestScore = double.NegativeInfinity;
        for (var delta = -deltaBefore; delta <= deltaAfter; delta++)
        {
            var seq2OffsetStart = diff.Seq2Range.Start + delta;
            var seq2OffsetEnd = diff.Seq2Range.EndExclusive + delta;
            var seq1Offset = diff.Seq1Range.Start + delta;
            var score = sequence1.GetBoundaryScore(seq1Offset) + sequence2.GetBoundaryScore(seq2OffsetStart) + sequence2.GetBoundaryScore(seq2OffsetEnd);
            if (score > bestScore)
            {
                bestScore = score;
                bestDelta = delta;
            }
        }

        return diff.Delta(bestDelta);
    }

    private static List<SequenceDiff> MergeSequenceDiffs(IReadOnlyList<SequenceDiff> diffs1, IReadOnlyList<SequenceDiff> diffs2)
    {
        var result = new List<SequenceDiff>();
        var i = 0;
        var j = 0;
        while (i < diffs1.Count || j < diffs2.Count)
        {
            SequenceDiff next;
            if (i < diffs1.Count && (j >= diffs2.Count || diffs1[i].Seq1Range.Start < diffs2[j].Seq1Range.Start))
            {
                next = diffs1[i++];
            }
            else
            {
                next = diffs2[j++];
            }

            if (result.Count > 0 && result[^1].Seq1Range.EndExclusive >= next.Seq1Range.Start)
            {
                result[^1] = result[^1].Join(next);
            }
            else
            {
                result.Add(next);
            }
        }

        return result;
    }

    private static void ScanWord(
        LinesSliceCharSequence sequence1,
        LinesSliceCharSequence sequence2,
        Func<LinesSliceCharSequence, int, OffsetRange?> findParent,
        bool force,
        List<SequenceDiff> additional,
        List<SequenceDiff> equalMappings,
        ref OffsetPair lastPoint,
        OffsetPair pair,
        SequenceDiff equalMapping)
    {
        if (pair.Offset1 < lastPoint.Offset1 || pair.Offset2 < lastPoint.Offset2)
        {
            return;
        }

        var w1 = findParent(sequence1, pair.Offset1);
        var w2 = findParent(sequence2, pair.Offset2);
        if (w1 == null || w2 == null)
        {
            return;
        }

        var wordDiff = new SequenceDiff(w1.Value, w2.Value);
        var equalPart = wordDiff.Intersect(equalMapping);
        if (equalPart == null)
        {
            return;
        }

        var equalChars1 = equalPart.Seq1Range.Length;
        var equalChars2 = equalPart.Seq2Range.Length;

        while (equalMappings.Count > 0)
        {
            var next = equalMappings[0];
            var intersects = next.Seq1Range.IntersectsOrTouches(wordDiff.Seq1Range) || next.Seq2Range.IntersectsOrTouches(wordDiff.Seq2Range);
            if (!intersects)
            {
                break;
            }

            var parent1 = findParent(sequence1, next.Seq1Range.Start);
            var parent2 = findParent(sequence2, next.Seq2Range.Start);
            if (parent1 == null || parent2 == null)
            {
                break;
            }

            var nextWord = new SequenceDiff(parent1.Value, parent2.Value);
            var eq = nextWord.Intersect(next);
            if (eq != null)
            {
                equalChars1 += eq.Seq1Range.Length;
                equalChars2 += eq.Seq2Range.Length;
            }

            wordDiff = wordDiff.Join(nextWord);
            if (wordDiff.Seq1Range.EndExclusive >= next.Seq1Range.EndExclusive)
            {
                equalMappings.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        var total = wordDiff.Seq1Range.Length + wordDiff.Seq2Range.Length;
        if ((force && equalChars1 + equalChars2 < total) || equalChars1 + equalChars2 < total * 2 / 3)
        {
            additional.Add(wordDiff);
        }

        lastPoint = wordDiff.GetEndExclusives();
    }
}
