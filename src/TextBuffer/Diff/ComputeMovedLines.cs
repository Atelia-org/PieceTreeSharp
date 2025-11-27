// Source: ts/src/vs/editor/common/diff/defaultLinesDiffComputer/computeMovedLines.ts
// - Function: computeMovedLines
// - Helper types: LineRangeFragment, PossibleMapping, WindowKey
// - Lines: 20-800
// Ported: 2025-11-21

using PieceTree.TextBuffer.Diff.Algorithms;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Diff;

internal static class MoveDetection
{
    private static readonly DetailedLineRangeMapping FallbackChange = new(new LineRange(1, 1), new LineRange(1, 1), Array.Empty<RangeMapping>());

    public static IReadOnlyList<LineRangeMapping> ComputeMovedLines(
        IReadOnlyList<DetailedLineRangeMapping> changes,
        string[] originalLines,
        string[] modifiedLines,
        int[] hashedOriginalLines,
        int[] hashedModifiedLines,
        ITimeout timeout)
    {
        (List<LineRangeMapping>? moves, HashSet<DetailedLineRangeMapping>? excludedChanges) = ComputeMovesFromSimpleDeletionsToSimpleInsertions(changes, originalLines, modifiedLines, timeout);
        if (!timeout.IsValid)
        {
            return Array.Empty<LineRangeMapping>();
        }

        List<DetailedLineRangeMapping> filteredChanges = changes.Where(c => !excludedChanges.Contains(c)).ToList();
        List<DetailedLineRangeMapping> mergedChangesForMoves = MergeAdjacentChangesForMoves(filteredChanges, 1);
        List<LineRangeMapping> unchangedMoves = ComputeUnchangedMoves(filteredChanges, mergedChangesForMoves, hashedOriginalLines, hashedModifiedLines, originalLines, modifiedLines, timeout);
        moves.AddRange(unchangedMoves);

        moves = JoinCloseConsecutiveMoves(moves);
        moves = FilterMovesByContent(moves, originalLines);

        moves = RemoveMovesInSameDiff(changes, moves);

        List<LineRangeMapping> shiftedBlocks = DetectShiftedBlocks(filteredChanges, originalLines.Length, modifiedLines.Length);
        shiftedBlocks = FilterMovesByContent(shiftedBlocks, originalLines);
        moves.AddRange(shiftedBlocks);
        return moves;
    }

    private static int CountWhere<T>(IEnumerable<T> source, Func<T, bool> predicate)
    {
        int count = 0;
        foreach (T? item in source)
        {
            if (predicate(item))
            {
                count++;
            }
        }

        return count;
    }

    private static (List<LineRangeMapping> moves, HashSet<DetailedLineRangeMapping> excludedChanges) ComputeMovesFromSimpleDeletionsToSimpleInsertions(
        IReadOnlyList<DetailedLineRangeMapping> changes,
        string[] originalLines,
        string[] modifiedLines,
        ITimeout timeout)
    {
        List<LineRangeMapping> moves = [];
        List<LineRangeFragment> deletions = changes
            .Where(c => c.Modified.IsEmpty && c.Original.Length >= 3)
            .Select(c => new LineRangeFragment(c.Original, originalLines, c))
            .ToList();
        HashSet<LineRangeFragment> insertions = new(changes
            .Where(c => c.Original.IsEmpty && c.Modified.Length >= 3)
            .Select(c => new LineRangeFragment(c.Modified, modifiedLines, c)));

        HashSet<DetailedLineRangeMapping> excluded = [];
        foreach (LineRangeFragment deletion in deletions)
        {
            double highestSimilarity = -1;
            LineRangeFragment? best = null;
            foreach (LineRangeFragment insertion in insertions)
            {
                double similarity = deletion.ComputeSimilarity(insertion);
                if (similarity > highestSimilarity)
                {
                    highestSimilarity = similarity;
                    best = insertion;
                }
            }

            if (highestSimilarity > 0.90 && best != null)
            {
                insertions.Remove(best);
                moves.Add(new LineRangeMapping(deletion.Range, best.Range));
                excluded.Add(deletion.Source);
                excluded.Add(best.Source);
            }

            if (!timeout.IsValid)
            {
                return (moves, excluded);
            }
        }

        return (moves, excluded);
    }

    private static List<LineRangeMapping> ComputeUnchangedMoves(
        IReadOnlyList<DetailedLineRangeMapping> referenceChanges,
        IReadOnlyList<DetailedLineRangeMapping> analysisChanges,
        int[] hashedOriginalLines,
        int[] hashedModifiedLines,
        string[] originalLines,
        string[] modifiedLines,
        ITimeout timeout)
    {
        List<DetailedLineRangeMapping> changes = referenceChanges.ToList();
        List<LineRangeMapping> moves = [];
        Dictionary<WindowKey, List<LineRange>> originalWindowMap = [];
        const int contextLines = 2;
        List<(LineRange Original, LineRange Modified)> clusters = BuildClusters(analysisChanges, contextLines);

        foreach ((LineRange Original, LineRange Modified) in clusters)
        {
            LineRange originalRangeWithContext = ExpandRange(Original, originalLines.Length + 1, contextLines);
            for (int lineNumber = originalRangeWithContext.StartLineNumber; lineNumber <= originalRangeWithContext.EndLineNumberExclusive - 3; lineNumber++)
            {
                int idx = lineNumber - 1;
                if (!TryCreateWindowKey(hashedOriginalLines, idx, out WindowKey key))
                {
                    continue;
                }

                if (!originalWindowMap.TryGetValue(key, out List<LineRange>? ranges))
                {
                    ranges = [];
                    originalWindowMap[key] = ranges;
                }

                ranges.Add(new LineRange(lineNumber, lineNumber + 3));
            }
        }

        List<PossibleMapping> possibleMappings = [];
        changes.Sort((a, b) => a.Modified.StartLineNumber.CompareTo(b.Modified.StartLineNumber));

        foreach ((LineRange Original, LineRange Modified) in clusters)
        {
            List<PossibleMapping> lastMappings = [];
            LineRange modifiedRangeWithContext = ExpandRange(Modified, modifiedLines.Length + 1, contextLines);
            for (int lineNumber = modifiedRangeWithContext.StartLineNumber; lineNumber <= modifiedRangeWithContext.EndLineNumberExclusive - 3; lineNumber++)
            {
                int idx = lineNumber - 1;
                if (!TryCreateWindowKey(hashedModifiedLines, idx, out WindowKey key))
                {
                    continue;
                }

                if (!originalWindowMap.TryGetValue(key, out List<LineRange>? candidates))
                {
                    continue;
                }

                LineRange currentModifiedRange = new(lineNumber, lineNumber + 3);
                List<PossibleMapping> nextMappings = [];
                foreach (LineRange candidate in candidates)
                {
                    bool extended = false;
                    foreach (PossibleMapping previous in lastMappings)
                    {
                        if (previous.OriginalRange.EndLineNumberExclusive + 1 == candidate.EndLineNumberExclusive
                            && previous.ModifiedRange.EndLineNumberExclusive + 1 == currentModifiedRange.EndLineNumberExclusive)
                        {
                            previous.OriginalRange = new LineRange(previous.OriginalRange.StartLineNumber, candidate.EndLineNumberExclusive);
                            previous.ModifiedRange = new LineRange(previous.ModifiedRange.StartLineNumber, currentModifiedRange.EndLineNumberExclusive);
                            nextMappings.Add(previous);
                            extended = true;
                            break;
                        }
                    }

                    if (!extended)
                    {
                        PossibleMapping mapping = new(currentModifiedRange, candidate);
                        possibleMappings.Add(mapping);
                        nextMappings.Add(mapping);
                    }
                }

                lastMappings = nextMappings;
            }

            if (!timeout.IsValid)
            {
                return moves;
            }
        }

        possibleMappings.Sort((a, b) => b.ModifiedRange.Length.CompareTo(a.ModifiedRange.Length));

        LineRangeSet modifiedSet = new();
        LineRangeSet originalSet = new();

        foreach (PossibleMapping mapping in possibleMappings)
        {
            int diff = mapping.ModifiedRange.StartLineNumber - mapping.OriginalRange.StartLineNumber;
            LineRangeSet modifiedSections = modifiedSet.SubtractFrom(mapping.ModifiedRange);
            LineRangeSet originalSections = originalSet.SubtractFrom(mapping.OriginalRange).GetWithDelta(diff);
            LineRangeSet intersections = modifiedSections.GetIntersection(originalSections);

            foreach (LineRange section in intersections.Ranges)
            {
                if (section.Length < 3)
                {
                    continue;
                }

                LineRange modifiedRange = section;
                LineRange originalRange = section.Delta(-diff);
                moves.Add(new LineRangeMapping(originalRange, modifiedRange));
                modifiedSet.AddRange(modifiedRange);
                originalSet.AddRange(originalRange);
            }
        }

        moves.Sort((a, b) => a.Original.StartLineNumber.CompareTo(b.Original.StartLineNumber));

        for (int i = 0; i < moves.Count; i++)
        {
            LineRangeMapping move = moves[i];
            DetailedLineRangeMapping? firstTouchingOriginal = FindLastMonotonous(changes, c => c.Original.StartLineNumber <= move.Original.StartLineNumber);
            DetailedLineRangeMapping? firstTouchingModified = FindLastMonotonous(changes, c => c.Modified.StartLineNumber <= move.Modified.StartLineNumber);
            if (firstTouchingOriginal == null || firstTouchingModified == null)
            {
                continue;
            }

            int linesAbove = Math.Max(
                move.Original.StartLineNumber - firstTouchingOriginal.Original.StartLineNumber,
                move.Modified.StartLineNumber - firstTouchingModified.Modified.StartLineNumber);

            DetailedLineRangeMapping? lastTouchingOriginal = FindLastMonotonous(changes, c => c.Original.StartLineNumber < move.Original.EndLineNumberExclusive);
            DetailedLineRangeMapping? lastTouchingModified = FindLastMonotonous(changes, c => c.Modified.StartLineNumber < move.Modified.EndLineNumberExclusive);
            if (lastTouchingOriginal == null || lastTouchingModified == null)
            {
                continue;
            }

            int linesBelow = Math.Max(
                lastTouchingOriginal.Original.EndLineNumberExclusive - move.Original.EndLineNumberExclusive,
                lastTouchingModified.Modified.EndLineNumberExclusive - move.Modified.EndLineNumberExclusive);

            int extendTop = 0;
            for (; extendTop < linesAbove; extendTop++)
            {
                int origLine = move.Original.StartLineNumber - extendTop - 1;
                int modLine = move.Modified.StartLineNumber - extendTop - 1;
                if (origLine <= 0 || modLine <= 0)
                {
                    break;
                }

                if (origLine > originalLines.Length || modLine > modifiedLines.Length)
                {
                    break;
                }

                if (modifiedSet.Contains(modLine) || originalSet.Contains(origLine))
                {
                    break;
                }

                if (!AreLinesSimilar(originalLines[origLine - 1], modifiedLines[modLine - 1], timeout))
                {
                    break;
                }
            }

            if (extendTop > 0)
            {
                originalSet.AddRange(new LineRange(move.Original.StartLineNumber - extendTop, move.Original.StartLineNumber));
                modifiedSet.AddRange(new LineRange(move.Modified.StartLineNumber - extendTop, move.Modified.StartLineNumber));
            }

            int extendBottom = 0;
            for (; extendBottom < linesBelow; extendBottom++)
            {
                int origLine = move.Original.EndLineNumberExclusive + extendBottom;
                int modLine = move.Modified.EndLineNumberExclusive + extendBottom;
                if (origLine > originalLines.Length || modLine > modifiedLines.Length)
                {
                    break;
                }

                if (modifiedSet.Contains(modLine) || originalSet.Contains(origLine))
                {
                    break;
                }

                if (!AreLinesSimilar(originalLines[origLine - 1], modifiedLines[modLine - 1], timeout))
                {
                    break;
                }
            }

            if (extendBottom > 0)
            {
                originalSet.AddRange(new LineRange(move.Original.EndLineNumberExclusive, move.Original.EndLineNumberExclusive + extendBottom));
                modifiedSet.AddRange(new LineRange(move.Modified.EndLineNumberExclusive, move.Modified.EndLineNumberExclusive + extendBottom));
            }

            if (extendTop > 0 || extendBottom > 0)
            {
                moves[i] = new LineRangeMapping(
                    new LineRange(move.Original.StartLineNumber - extendTop, move.Original.EndLineNumberExclusive + extendBottom),
                    new LineRange(move.Modified.StartLineNumber - extendTop, move.Modified.EndLineNumberExclusive + extendBottom));
            }
        }

        return moves;
    }

    private static bool AreLinesSimilar(string line1, string line2, ITimeout timeout)
    {
        if (line1.Trim() == line2.Trim())
        {
            return true;
        }

        if (line1.Length > 300 && line2.Length > 300)
        {
            return false;
        }

        MyersDiffAlgorithm myers = new();
        LinesSliceCharSequence slice1 = new([line1], new Range(1, 1, 1, Math.Max(1, line1.Length)), false);
        LinesSliceCharSequence slice2 = new([line2], new Range(1, 1, 1, Math.Max(1, line2.Length)), false);
        DiffAlgorithmResult result = myers.Compute(slice1, slice2, timeout);
        IReadOnlyList<SequenceDiff> inverted = SequenceDiff.Invert(result.Diffs, line1.Length);
        int commonNonSpaceChars = 0;
        foreach (SequenceDiff seq in inverted)
        {
            for (int idx = seq.Seq1Range.Start; idx < seq.Seq1Range.EndExclusive; idx++)
            {
                if (!IsSpace(line1[idx]))
                {
                    commonNonSpaceChars++;
                }
            }
        }

        string longer = line1.Length > line2.Length ? line1 : line2;
        int longerLength = CountNonWhitespace(longer);
        return longerLength > 10 && longerLength > 0 && (double)commonNonSpaceChars / longerLength > 0.6;

        static int CountNonWhitespace(string value)
        {
            int count = 0;
            foreach (char ch in value)
            {
                if (!IsSpace(ch))
                {
                    count++;
                }
            }

            return count;
        }
    }

    private static bool IsSpace(char ch) => ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';

    private static List<LineRangeMapping> JoinCloseConsecutiveMoves(List<LineRangeMapping> moves)
    {
        if (moves.Count == 0)
        {
            return moves;
        }

        moves.Sort((a, b) => a.Original.StartLineNumber.CompareTo(b.Original.StartLineNumber));
        List<LineRangeMapping> result = [moves[0]];
        for (int i = 1; i < moves.Count; i++)
        {
            LineRangeMapping last = result[^1];
            LineRangeMapping current = moves[i];
            int originalDist = current.Original.StartLineNumber - last.Original.EndLineNumberExclusive;
            int modifiedDist = current.Modified.StartLineNumber - last.Modified.EndLineNumberExclusive;
            bool currentAfterLast = originalDist >= 0 && modifiedDist >= 0;
            if (currentAfterLast && originalDist + modifiedDist <= 2)
            {
                result[^1] = last.Join(current);
                continue;
            }

            result.Add(current);
        }

        return result;
    }

    private static List<LineRangeMapping> RemoveMovesInSameDiff(IReadOnlyList<DetailedLineRangeMapping> changes, List<LineRangeMapping> moves)
    {
        List<DetailedLineRangeMapping> sorted = changes.OrderBy(c => c.Modified.StartLineNumber).ToList();
        return moves.Where(move =>
        {
            DetailedLineRangeMapping diffBeforeEndOriginal = FindLastMonotonous(sorted, c => c.Original.StartLineNumber < move.Original.EndLineNumberExclusive) ?? FallbackChange;
            DetailedLineRangeMapping? diffBeforeEndModified = FindLastMonotonous(sorted, c => c.Modified.StartLineNumber < move.Modified.EndLineNumberExclusive);
            bool keep = diffBeforeEndOriginal != diffBeforeEndModified;
            return keep;
        }).ToList();
    }

    private static bool TryCreateWindowKey(int[] hashes, int startIndex, out WindowKey key)
    {
        if (startIndex < 0 || startIndex + 2 >= hashes.Length)
        {
            key = default;
            return false;
        }

        key = new WindowKey(hashes[startIndex], hashes[startIndex + 1], hashes[startIndex + 2]);
        return true;
    }

    private static LineRange ExpandRange(LineRange range, int maxLineNumberExclusive, int context)
    {
        if (maxLineNumberExclusive <= 1)
        {
            return new LineRange(1, 1);
        }

        int start = Math.Max(1, range.StartLineNumber - context);
        int end = Math.Min(maxLineNumberExclusive, range.EndLineNumberExclusive + context);

        if (start >= end)
        {
            int anchor = Math.Clamp(range.StartLineNumber, 1, maxLineNumberExclusive - 1);
            start = Math.Max(1, anchor - context);
            end = Math.Min(maxLineNumberExclusive, start + context);
        }

        return new LineRange(start, end);
    }

    private static List<(LineRange Original, LineRange Modified)> BuildClusters(IReadOnlyList<DetailedLineRangeMapping> changes, int maxGap)
    {
        List<(LineRange Original, LineRange Modified)> clusters = [];
        if (changes.Count == 0)
        {
            return clusters;
        }

        LineRange currentOriginal = changes[0].Original;
        LineRange currentModified = changes[0].Modified;

        for (int i = 1; i < changes.Count; i++)
        {
            DetailedLineRangeMapping change = changes[i];
            int originalGap = change.Original.StartLineNumber - currentOriginal.EndLineNumberExclusive;
            int modifiedGap = change.Modified.StartLineNumber - currentModified.EndLineNumberExclusive;

            if (originalGap <= maxGap && modifiedGap <= maxGap)
            {
                currentOriginal = currentOriginal.Join(change.Original);
                currentModified = currentModified.Join(change.Modified);
            }
            else
            {
                clusters.Add((currentOriginal, currentModified));
                currentOriginal = change.Original;
                currentModified = change.Modified;
            }
        }

        clusters.Add((currentOriginal, currentModified));
        return clusters;
    }

    private static List<DetailedLineRangeMapping> MergeAdjacentChangesForMoves(IReadOnlyList<DetailedLineRangeMapping> changes, int maxGap)
    {
        if (changes.Count == 0)
        {
            return [];
        }

        List<DetailedLineRangeMapping> merged = [];
        DetailedLineRangeMapping current = changes[0];

        for (int i = 1; i < changes.Count; i++)
        {
            DetailedLineRangeMapping next = changes[i];
            int originalGap = next.Original.StartLineNumber - current.Original.EndLineNumberExclusive;
            int modifiedGap = next.Modified.StartLineNumber - current.Modified.EndLineNumberExclusive;

            if (originalGap <= maxGap && modifiedGap <= maxGap)
            {
                RangeMapping[] inner = current.InnerChanges.Concat(next.InnerChanges).ToArray();
                current = new DetailedLineRangeMapping(current.Original.Join(next.Original), current.Modified.Join(next.Modified), inner);
            }
            else
            {
                merged.Add(current);
                current = next;
            }
        }

        merged.Add(current);
        return merged;
    }

    private static List<LineRangeMapping> FilterMovesByContent(List<LineRangeMapping> moves, string[] originalLines)
    {
        return moves.Where(move =>
        {
            string[] lines = ReadLines(move.Original, originalLines).Select(l => l.Trim()).ToArray();
            string text = string.Join('\n', lines);
            return text.Length >= 15 && CountWhere(lines, l => l.Length >= 2) >= 2;
        }).ToList();
    }

    private static List<LineRangeMapping> DetectShiftedBlocks(IReadOnlyList<DetailedLineRangeMapping> changes, int originalLineCount, int modifiedLineCount)
    {
        List<LineRangeMapping> result = [];

        int prevOriginalEnd = 1;
        int prevModifiedEnd = 1;
        LineRange? currentOriginal = null;
        LineRange? currentModified = null;
        int? currentOffset = null;

        void Flush()
        {
            if (currentOriginal.HasValue && currentModified.HasValue)
            {
                LineRange originalRange = currentOriginal.Value;
                LineRange modifiedRange = currentModified.Value;
                if (originalRange.Length >= 3 && modifiedRange.Length >= 3)
                {
                    result.Add(new LineRangeMapping(originalRange, modifiedRange));
                }
            }

            currentOriginal = null;
            currentModified = null;
            currentOffset = null;
        }

        for (int i = 0; i <= changes.Count; i++)
        {
            int nextOriginalStart = i < changes.Count ? changes[i].Original.StartLineNumber : originalLineCount + 1;
            int nextModifiedStart = i < changes.Count ? changes[i].Modified.StartLineNumber : modifiedLineCount + 1;

            int originalLength = nextOriginalStart - prevOriginalEnd;
            int modifiedLength = nextModifiedStart - prevModifiedEnd;

            if (originalLength > 0 && modifiedLength > 0)
            {
                int segmentOffset = prevModifiedEnd - prevOriginalEnd;
                if (segmentOffset != 0)
                {
                    LineRange segmentOriginal = new(prevOriginalEnd, nextOriginalStart);
                    LineRange segmentModified = new(prevModifiedEnd, nextModifiedStart);
                    if (currentOffset == segmentOffset && currentOriginal.HasValue && currentModified.HasValue)
                    {
                        currentOriginal = currentOriginal.Value.Join(segmentOriginal);
                        currentModified = currentModified.Value.Join(segmentModified);
                    }
                    else
                    {
                        Flush();
                        currentOffset = segmentOffset;
                        currentOriginal = segmentOriginal;
                        currentModified = segmentModified;
                    }
                }
                else
                {
                    Flush();
                }
            }
            else
            {
                Flush();
            }

            if (i < changes.Count)
            {
                prevOriginalEnd = changes[i].Original.EndLineNumberExclusive;
                prevModifiedEnd = changes[i].Modified.EndLineNumberExclusive;
            }
            else
            {
                prevOriginalEnd = nextOriginalStart;
                prevModifiedEnd = nextModifiedStart;
            }
        }

        Flush();
        return result;
    }

    private static T? FindLastMonotonous<T>(IReadOnlyList<T> list, Func<T, bool> predicate) where T : class
    {
        int low = 0;
        int high = list.Count - 1;
        T? result = null;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            T value = list[mid];
            if (predicate(value))
            {
                result = value;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return result;
    }

    private static IEnumerable<string> ReadLines(LineRange range, string[] lines)
    {
        if (lines.Length == 0 || range.IsEmpty)
        {
            yield break;
        }

        int start = Math.Max(0, range.StartLineNumber - 1);
        int endExclusive = Math.Min(range.EndLineNumberExclusive - 1, lines.Length);
        for (int i = start; i < endExclusive; i++)
        {
            yield return lines[i];
        }
    }

    private sealed class PossibleMapping
    {
        public PossibleMapping(LineRange modifiedRange, LineRange originalRange)
        {
            ModifiedRange = modifiedRange;
            OriginalRange = originalRange;
        }

        public LineRange ModifiedRange { get; set; }
        public LineRange OriginalRange { get; set; }
    }

    private readonly struct WindowKey : IEquatable<WindowKey>
    {
        public WindowKey(int first, int second, int third)
        {
            First = first;
            Second = second;
            Third = third;
        }

        public int First { get; }
        public int Second { get; }
        public int Third { get; }

        public bool Equals(WindowKey other)
        {
            return First == other.First && Second == other.Second && Third == other.Third;
        }

        public override bool Equals(object? obj)
        {
            return obj is WindowKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + First;
                hash = (hash * 31) + Second;
                hash = (hash * 31) + Third;
                return hash;
            }
        }
    }
}
