// Source: ts/src/vs/editor/common/diff/defaultLinesDiffComputer/defaultLinesDiffComputer.ts
// - Class: DefaultLinesDiffComputer
// - Lines: 30-600
// Ported: 2025-11-21

using System;
using System.Collections.Generic;
using System.Linq;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Diff.Algorithms;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Diff;

public static class DiffComputer
{
    private static readonly DynamicProgrammingDiffing DynamicProgramming = new();
    private static readonly MyersDiffAlgorithm Myers = new();

    public static DiffResult Compute(string original, string modified, DiffComputerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(modified);
        return Compute(SplitIntoLines(original), SplitIntoLines(modified), options);
    }

    public static DiffResult Compute(IReadOnlyList<string> originalLines, IReadOnlyList<string> modifiedLines, DiffComputerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(originalLines);
        ArgumentNullException.ThrowIfNull(modifiedLines);

        string[] original = NormalizeLines(originalLines);
        string[] modified = NormalizeLines(modifiedLines);
        return ComputeInternal(original, modified, options ?? new DiffComputerOptions());
    }

    private static DiffResult ComputeInternal(string[] originalLines, string[] modifiedLines, DiffComputerOptions options)
    {
        if (originalLines.Length <= 1 && LinesEqual(originalLines, modifiedLines))
        {
            return new DiffResult(Array.Empty<DetailedLineRangeMapping>(), Array.Empty<DiffMove>(), false);
        }

        if (IsSingleEmptyLine(originalLines) || IsSingleEmptyLine(modifiedLines))
        {
            return BuildWholeDocumentChange(originalLines, modifiedLines);
        }

        ITimeout timeout = options.MaxComputationTimeMs == 0
            ? InfiniteTimeout.Instance
            : new DateTimeout(options.MaxComputationTimeMs);

        bool considerWhitespaceChanges = !options.IgnoreTrimWhitespace;
        (int[]? hashedOriginalLines, int[]? hashedModifiedLines) = BuildLineHashes(originalLines, modifiedLines);
        LineSequence sequence1 = new(hashedOriginalLines, originalLines);
        LineSequence sequence2 = new(hashedModifiedLines, modifiedLines);

        DiffAlgorithmResult lineAlignmentResult = sequence1.Length + sequence2.Length < 1700
            ? DynamicProgramming.Compute(sequence1, sequence2, timeout, (offset1, offset2) => ComputeEqualityScore(originalLines[offset1], modifiedLines[offset2]))
            : Myers.Compute(sequence1, sequence2, timeout);

        bool hitTimeout = lineAlignmentResult.HitTimeout;
        List<SequenceDiff> lineAlignments = HeuristicSequenceOptimizations.OptimizeSequenceDiffs(sequence1, sequence2, lineAlignmentResult.Diffs);
        lineAlignments = HeuristicSequenceOptimizations.RemoveVeryShortMatchingLinesBetweenDiffs(sequence1, lineAlignments);

        List<RangeMapping> alignments = [];
        int seq1LastStart = 0;
        int seq2LastStart = 0;

        void ScanWhitespace(int equalLinesCount)
        {
            if (!considerWhitespaceChanges)
            {
                return;
            }

            for (int i = 0; i < equalLinesCount; i++)
            {
                int seq1Offset = seq1LastStart + i;
                int seq2Offset = seq2LastStart + i;
                if (seq1Offset >= originalLines.Length || seq2Offset >= modifiedLines.Length)
                {
                    continue;
                }

                if (!string.Equals(originalLines[seq1Offset], modifiedLines[seq2Offset], StringComparison.Ordinal))
                {
                    DiffRefinementResult refinement = RefineDiff(
                        originalLines,
                        modifiedLines,
                        new SequenceDiff(
                            OffsetRange.OfStartAndLength(seq1Offset, 1),
                            OffsetRange.OfStartAndLength(seq2Offset, 1)),
                        timeout,
                        considerWhitespaceChanges,
                        options);

                    alignments.AddRange(refinement.Mappings);
                    hitTimeout |= refinement.HitTimeout;
                }
            }
        }

        foreach (SequenceDiff diff in lineAlignments)
        {
            int equalLinesCount = diff.Seq1Range.Start - seq1LastStart;
            ScanWhitespace(equalLinesCount);

            seq1LastStart = diff.Seq1Range.EndExclusive;
            seq2LastStart = diff.Seq2Range.EndExclusive;

            DiffRefinementResult refinement = RefineDiff(originalLines, modifiedLines, diff, timeout, considerWhitespaceChanges, options);
            alignments.AddRange(refinement.Mappings);
            hitTimeout |= refinement.HitTimeout;
        }

        ScanWhitespace(originalLines.Length - seq1LastStart);

        IReadOnlyList<DetailedLineRangeMapping> changes = LineRangeMappingBuilder.FromRangeMappings(alignments, originalLines, modifiedLines);
        IReadOnlyList<DiffMove> moves = options.ComputeMoves
            ? ComputeMoves(changes, originalLines, modifiedLines, hashedOriginalLines, hashedModifiedLines, timeout, considerWhitespaceChanges, options)
            : Array.Empty<DiffMove>();

        return new DiffResult(changes, moves, hitTimeout);
    }

    private static DiffRefinementResult RefineDiff(
        string[] originalLines,
        string[] modifiedLines,
        SequenceDiff diff,
        ITimeout timeout,
        bool considerWhitespaceChanges,
        DiffComputerOptions options)
    {
        LineRangeMapping lineRangeMapping = ToLineRangeMapping(diff);
        RangeMapping rangeMapping = lineRangeMapping.ToRangeMapping2(originalLines, modifiedLines);

        LinesSliceCharSequence slice1 = new(originalLines, rangeMapping.OriginalRange, considerWhitespaceChanges);
        LinesSliceCharSequence slice2 = new(modifiedLines, rangeMapping.ModifiedRange, considerWhitespaceChanges);

        DiffAlgorithmResult diffResult = slice1.Length + slice2.Length < 500
            ? DynamicProgramming.Compute(slice1, slice2, timeout)
            : Myers.Compute(slice1, slice2, timeout);

        List<SequenceDiff> diffs = HeuristicSequenceOptimizations.OptimizeSequenceDiffs(slice1, slice2, diffResult.Diffs);

        if (options.ExtendToWordBoundaries)
        {
            diffs = HeuristicSequenceOptimizations.ExtendDiffsToEntireWordIfAppropriate(slice1, slice2, diffs, (seq, idx) => seq.FindWordContaining(idx));
        }

        if (options.ExtendToSubwords)
        {
            diffs = HeuristicSequenceOptimizations.ExtendDiffsToEntireWordIfAppropriate(slice1, slice2, diffs, (seq, idx) => seq.FindSubWordContaining(idx), true);
        }

        diffs = HeuristicSequenceOptimizations.RemoveShortMatches(slice1, slice2, diffs);
        diffs = HeuristicSequenceOptimizations.RemoveVeryShortMatchingTextBetweenLongDiffs(slice1, slice2, diffs);

        List<RangeMapping> mappings = diffs
            .Select(d => new RangeMapping(
                slice1.TranslateRange(d.Seq1Range),
                slice2.TranslateRange(d.Seq2Range)))
            .ToList();

        return new DiffRefinementResult(mappings, diffResult.HitTimeout);
    }

    private static IReadOnlyList<DiffMove> ComputeMoves(
        IReadOnlyList<DetailedLineRangeMapping> changes,
        string[] originalLines,
        string[] modifiedLines,
        int[] hashedOriginalLines,
        int[] hashedModifiedLines,
        ITimeout timeout,
        bool considerWhitespaceChanges,
        DiffComputerOptions options)
    {
        IReadOnlyList<LineRangeMapping> ranges = MoveDetection.ComputeMovedLines(changes, originalLines, modifiedLines, hashedOriginalLines, hashedModifiedLines, timeout);
        if (ranges.Count == 0)
        {
            return Array.Empty<DiffMove>();
        }

        List<DiffMove> moves = new(ranges.Count);
        foreach (LineRangeMapping move in ranges)
        {
            DiffRefinementResult refinement = RefineDiff(
                originalLines,
                modifiedLines,
                new SequenceDiff(move.Original.ToOffsetRange(), move.Modified.ToOffsetRange()),
                timeout,
                considerWhitespaceChanges,
                options);

            IReadOnlyList<DetailedLineRangeMapping> mapped = LineRangeMappingBuilder.FromRangeMappings(refinement.Mappings, originalLines, modifiedLines, dontAssertStartLine: true);
            moves.Add(new DiffMove(move, mapped));
        }

        return moves;
    }

    private static bool LinesEqual(IReadOnlyList<string> original, IReadOnlyList<string> modified)
    {
        if (original.Count != modified.Count)
        {
            return false;
        }

        for (int i = 0; i < original.Count; i++)
        {
            if (!string.Equals(original[i], modified[i], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSingleEmptyLine(IReadOnlyList<string> lines)
    {
        return lines.Count == 1 && lines[0].Length == 0;
    }

    private static DiffResult BuildWholeDocumentChange(string[] originalLines, string[] modifiedLines)
    {
        Range originalRange = new(1, 1, Math.Max(1, originalLines.Length), originalLines[^1].Length + 1);
        Range modifiedRange = new(1, 1, Math.Max(1, modifiedLines.Length), modifiedLines[^1].Length + 1);
        RangeMapping mapping = new(originalRange, modifiedRange);
        DetailedLineRangeMapping change = new(
            new LineRange(1, originalLines.Length + 1),
            new LineRange(1, modifiedLines.Length + 1),
            new[] { mapping });

        return new DiffResult(new[] { change }, Array.Empty<DiffMove>(), false);
    }

    private static (int[] Original, int[] Modified) BuildLineHashes(string[] originalLines, string[] modifiedLines)
    {
        Dictionary<string, int> hashes = new(StringComparer.Ordinal);

        int GetOrCreateHash(string text)
        {
            if (!hashes.TryGetValue(text, out int hash))
            {
                hash = hashes.Count;
                hashes[text] = hash;
            }

            return hash;
        }

        int[] original = new int[originalLines.Length];
        int[] modified = new int[modifiedLines.Length];

        for (int i = 0; i < originalLines.Length; i++)
        {
            original[i] = GetOrCreateHash(originalLines[i].Trim());
        }

        for (int i = 0; i < modifiedLines.Length; i++)
        {
            modified[i] = GetOrCreateHash(modifiedLines[i].Trim());
        }

        return (original, modified);
    }

    private static double ComputeEqualityScore(string originalLine, string modifiedLine)
    {
        if (string.Equals(originalLine, modifiedLine, StringComparison.Ordinal))
        {
            return modifiedLine.Length == 0
                ? 0.1
                : 1 + Math.Log(1 + modifiedLine.Length);
        }

        return 0.99;
    }

    private static LineRangeMapping ToLineRangeMapping(SequenceDiff diff)
    {
        return new LineRangeMapping(
            LineRange.OfLength(diff.Seq1Range.Start + 1, diff.Seq1Range.Length),
            LineRange.OfLength(diff.Seq2Range.Start + 1, diff.Seq2Range.Length));
    }

    private static string[] SplitIntoLines(string value)
    {
        if (value.Length == 0)
        {
            return [string.Empty];
        }

        List<string> lines = [];
        int start = 0;

        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];
            if (ch == '\r' || ch == '\n')
            {
                lines.Add(value.Substring(start, i - start));
                if (ch == '\r' && i + 1 < value.Length && value[i + 1] == '\n')
                {
                    i++;
                }

                start = i + 1;
            }
        }

        if (start <= value.Length)
        {
            lines.Add(value.Substring(start));
        }

        if (lines.Count == 0)
        {
            lines.Add(string.Empty);
        }

        return lines.ToArray();
    }

    private static string[] NormalizeLines(IReadOnlyList<string> source)
    {
        if (source.Count == 0)
        {
            return [string.Empty];
        }

        return source as string[] ?? source.ToArray();
    }

    private readonly record struct DiffRefinementResult(IReadOnlyList<RangeMapping> Mappings, bool HitTimeout);
}
