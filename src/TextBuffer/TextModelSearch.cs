// Source: ts/src/vs/editor/common/model/textModelSearch.ts
// - Class: TextModelSearch
// - Class: SearchParams
// Ported: 2025-11-19

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer;

internal sealed class SearchRangeSet
{
    private readonly List<Range> _ranges;

    private SearchRangeSet(List<Range> ranges, bool isWholeDocument)
    {
        _ranges = ranges;
        IsWholeDocument = isWholeDocument;
    }

    public bool IsWholeDocument { get; }
    public int Count => _ranges.Count;
    public IReadOnlyList<Range> Ranges => _ranges;
    public bool IsEmpty => _ranges.Count == 0;

    public Range this[int index] => _ranges[index];

    public static SearchRangeSet EntireDocument(ITextSearchAccess model)
    {
        int lineCount = Math.Max(1, model.LineCount);
        TextPosition end = new(lineCount, model.GetLineMaxColumn(lineCount));
        return new SearchRangeSet([new(new TextPosition(1, 1), end)], true);
    }

    public static SearchRangeSet FromRange(ITextSearchAccess model, Range range)
    {
        return new SearchRangeSet([NormalizeRange(model, range)], false);
    }

    public static SearchRangeSet FromRanges(ITextSearchAccess model, IReadOnlyList<Range>? ranges, bool findInSelection)
    {
        if (!findInSelection || ranges == null || ranges.Count == 0)
        {
            return EntireDocument(model);
        }

        List<Range> merged = MergeRanges(model, ranges);
        if (merged.Count == 0)
        {
            return EntireDocument(model);
        }

        return new SearchRangeSet(merged, false);
    }

    public int FindContainingRange(TextPosition position)
    {
        for (int i = 0; i < _ranges.Count; i++)
        {
            if (Contains(_ranges[i], position, includeEnd: false))
            {
                return i;
            }
        }

        return -1;
    }

    public int FindFirstRangeStartingAfter(TextPosition position)
    {
        for (int i = 0; i < _ranges.Count; i++)
        {
            if (Compare(_ranges[i].Start, position) >= 0)
            {
                return i;
            }
        }

        return 0;
    }

    public int FindLastRangeEndingBefore(TextPosition position)
    {
        for (int i = _ranges.Count - 1; i >= 0; i--)
        {
            if (Compare(_ranges[i].End, position) <= 0)
            {
                return i;
            }
        }

        return _ranges.Count - 1;
    }

    private static List<Range> MergeRanges(ITextSearchAccess model, IReadOnlyList<Range> ranges)
    {
        List<Range> normalized = new(ranges.Count);
        foreach (Range range in ranges)
        {
            normalized.Add(NormalizeRange(model, range));
        }

        normalized.Sort((a, b) => Compare(a.Start, b.Start));
        List<Range> merged = [];
        foreach (Range range in normalized)
        {
            if (merged.Count == 0)
            {
                merged.Add(range);
                continue;
            }

            Range last = merged[^1];
            if (Compare(range.Start, last.End) <= 0)
            {
                TextPosition newEnd = Compare(range.End, last.End) > 0 ? range.End : last.End;
                merged[^1] = new Range(last.Start, newEnd);
            }
            else
            {
                merged.Add(range);
            }
        }

        return merged;
    }

    private static Range NormalizeRange(ITextSearchAccess model, Range range)
    {
        TextPosition start = Clamp(model, range.Start);
        TextPosition end = Clamp(model, range.End);
        if (Compare(end, start) < 0)
        {
            (start, end) = (end, start);
        }

        return new Range(start, end);
    }

    private static TextPosition Clamp(ITextSearchAccess model, TextPosition position)
    {
        int lineCount = Math.Max(1, model.LineCount);
        int line = Math.Clamp(position.LineNumber, 1, lineCount);
        int column = Math.Clamp(position.Column, 1, Math.Max(1, model.GetLineMaxColumn(line)));
        return new TextPosition(line, column);
    }

    internal static int Compare(TextPosition left, TextPosition right)
    {
        if (left.LineNumber != right.LineNumber)
        {
            return left.LineNumber.CompareTo(right.LineNumber);
        }

        return left.Column.CompareTo(right.Column);
    }

    internal static bool Contains(Range range, TextPosition position, bool includeEnd)
    {
        int startComparison = Compare(range.Start, position);
        int endComparison = Compare(position, range.End);
        if (includeEnd)
        {
            return startComparison <= 0 && endComparison <= 0;
        }

        return startComparison <= 0 && endComparison < 0;
    }
}

internal static class TextModelSearch
{
    public const int DefaultLimit = 999;

    public static IReadOnlyList<FindMatch> FindMatches(ITextSearchAccess model, SearchData searchData, Range searchRange, bool captureMatches, int limitResultCount)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(searchData);

        if (limitResultCount <= 0)
        {
            return Array.Empty<FindMatch>();
        }

        Range normalizedRange = NormalizeSearchRange(model, searchRange);
        if (searchData.IsMultiline)
        {
            PieceTreeSearcher searcher = new(searchData.WordSeparators, searchData.Regex);
            return DoFindMatchesMultiline(model, normalizedRange, searcher, captureMatches, limitResultCount);
        }

        return DoFindMatchesLineByLine(model, normalizedRange, searchData, captureMatches, limitResultCount);
    }

    public static IReadOnlyList<FindMatch> FindMatches(ITextSearchAccess model, SearchData searchData, SearchRangeSet rangeSet, bool captureMatches, int limitResultCount)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(searchData);
        ArgumentNullException.ThrowIfNull(rangeSet);

        if (rangeSet.Count == 0)
        {
            return Array.Empty<FindMatch>();
        }

        if (rangeSet.IsWholeDocument && rangeSet.Count == 1)
        {
            return FindMatches(model, searchData, rangeSet[0], captureMatches, limitResultCount);
        }

        List<FindMatch> results = [];
        for (int i = 0; i < rangeSet.Count && results.Count < limitResultCount; i++)
        {
            Range range = rangeSet[i];
            IReadOnlyList<FindMatch> matches = FindMatches(model, searchData, range, captureMatches, limitResultCount - results.Count);
            if (matches.Count > 0)
            {
                results.AddRange(matches);
            }
        }

        return results;
    }

    public static FindMatch? FindNextMatch(ITextSearchAccess model, SearchData searchData, TextPosition searchStart, bool captureMatches)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(searchData);

        TextPosition clampedStart = ClampPosition(model, searchStart);
        PieceTreeSearcher searcher = new(searchData.WordSeparators, searchData.Regex);
        if (searchData.IsMultiline)
        {
            return DoFindNextMatchMultiline(model, clampedStart, searcher, captureMatches);
        }

        return DoFindNextMatchLineByLine(model, clampedStart, searcher, captureMatches);
    }

    public static FindMatch? FindNextMatch(ITextSearchAccess model, SearchData searchData, TextPosition searchStart, bool captureMatches, SearchRangeSet rangeSet)
    {
        ArgumentNullException.ThrowIfNull(rangeSet);
        if (rangeSet.IsWholeDocument)
        {
            return FindNextMatch(model, searchData, searchStart, captureMatches);
        }

        return FindNextMatchWithinRangeSet(model, searchData, searchStart, captureMatches, rangeSet);
    }

    public static FindMatch? FindPreviousMatch(ITextSearchAccess model, SearchData searchData, TextPosition searchStart, bool captureMatches)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(searchData);

        TextPosition clampedStart = ClampPosition(model, searchStart);
        PieceTreeSearcher searcher = new(searchData.WordSeparators, searchData.Regex);
        if (searchData.IsMultiline)
        {
            return DoFindPreviousMatchMultiline(model, clampedStart, searcher, captureMatches);
        }

        return DoFindPreviousMatchLineByLine(model, clampedStart, searcher, captureMatches);
    }

    public static FindMatch? FindPreviousMatch(ITextSearchAccess model, SearchData searchData, TextPosition searchStart, bool captureMatches, SearchRangeSet rangeSet)
    {
        ArgumentNullException.ThrowIfNull(rangeSet);
        if (rangeSet.IsWholeDocument)
        {
            return FindPreviousMatch(model, searchData, searchStart, captureMatches);
        }

        return FindPreviousMatchWithinRangeSet(model, searchData, searchStart, captureMatches, rangeSet);
    }

    private static IReadOnlyList<FindMatch> DoFindMatchesMultiline(ITextSearchAccess model, Range searchRange, PieceTreeSearcher searcher, bool captureMatches, int limitResultCount)
    {
        List<FindMatch> result = [];
        int deltaOffset = model.GetOffsetAt(searchRange.Start);
        string text = model.GetValueInRange(searchRange, normalizeLineEndings: true, out LineFeedCounter? lineFeedCounter);

        searcher.Reset(0);
        Match? match;
        while ((match = searcher.Next(text)) != null)
        {
            Range range = GetMultilineMatchRange(model, deltaOffset, text, lineFeedCounter, match.Index, match.Length);
            result.Add(CreateFindMatch(range, match, captureMatches));
            if (result.Count >= limitResultCount)
            {
                break;
            }
        }

        return result;
    }

    private static IReadOnlyList<FindMatch> DoFindMatchesLineByLine(ITextSearchAccess model, Range searchRange, SearchData searchData, bool captureMatches, int limitResultCount)
    {
        List<FindMatch> result = [];
        if (limitResultCount <= 0)
        {
            return result;
        }

        int startLine = searchRange.StartLineNumber;
        int endLine = searchRange.EndLineNumber;

        if (startLine == endLine)
        {
            string text = model.GetLineContent(startLine);
            string slice = Slice(text, searchRange.StartColumn - 1, searchRange.EndColumn - 1);
            FindMatchesInLine(searchData, slice, startLine, searchRange.StartColumn - 1, captureMatches, limitResultCount, result);
            return result;
        }

        string firstLineText = model.GetLineContent(startLine);
        string firstSlice = Slice(firstLineText, searchRange.StartColumn - 1, firstLineText.Length);
        FindMatchesInLine(searchData, firstSlice, startLine, searchRange.StartColumn - 1, captureMatches, limitResultCount, result);
        if (result.Count >= limitResultCount)
        {
            return result;
        }

        for (int lineNumber = startLine + 1; lineNumber < endLine && result.Count < limitResultCount; lineNumber++)
        {
            string lineContent = model.GetLineContent(lineNumber);
            FindMatchesInLine(searchData, lineContent, lineNumber, 0, captureMatches, limitResultCount, result);
        }

        if (result.Count >= limitResultCount)
        {
            return result;
        }

        string lastLineText = model.GetLineContent(endLine);
        string lastSlice = Slice(lastLineText, 0, searchRange.EndColumn - 1);
        FindMatchesInLine(searchData, lastSlice, endLine, 0, captureMatches, limitResultCount, result);
        return result;
    }

    private static FindMatch? DoFindNextMatchMultiline(ITextSearchAccess model, TextPosition searchStart, PieceTreeSearcher searcher, bool captureMatches)
    {
        TextPosition searchTextStart = new(searchStart.LineNumber, 1);
        int lineCount = Math.Max(1, model.LineCount);
        TextPosition endPosition = new(lineCount, model.GetLineMaxColumn(lineCount));
        Range searchRange = new(searchTextStart, endPosition);
        int deltaOffset = model.GetOffsetAt(searchTextStart);
        string text = model.GetValueInRange(searchRange, normalizeLineEndings: true, out LineFeedCounter? lineFeedCounter);

        searcher.Reset(searchStart.Column - 1);
        Match? match = searcher.Next(text);
        if (match != null)
        {
            Range range = GetMultilineMatchRange(model, deltaOffset, text, lineFeedCounter, match.Index, match.Length);
            return CreateFindMatch(range, match, captureMatches);
        }

        if (searchStart.LineNumber != 1 || searchStart.Column != 1)
        {
            return DoFindNextMatchMultiline(model, new TextPosition(1, 1), searcher, captureMatches);
        }

        return null;
    }

    private static FindMatch? DoFindNextMatchLineByLine(ITextSearchAccess model, TextPosition searchStart, PieceTreeSearcher searcher, bool captureMatches)
    {
        int lineCount = Math.Max(1, model.LineCount);
        int startLineNumber = searchStart.LineNumber;

        string firstLineContent = model.GetLineContent(startLineNumber);
        FindMatch? first = FindFirstMatchInLine(searcher, firstLineContent, startLineNumber, searchStart.Column, captureMatches);
        if (first != null)
        {
            return first;
        }

        for (int i = 1; i <= lineCount; i++)
        {
            int lineIndex = (startLineNumber + i - 1) % lineCount;
            string text = model.GetLineContent(lineIndex + 1);
            FindMatch? match = FindFirstMatchInLine(searcher, text, lineIndex + 1, 1, captureMatches);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static FindMatch? DoFindPreviousMatchMultiline(ITextSearchAccess model, TextPosition searchStart, PieceTreeSearcher searcher, bool captureMatches)
    {
        Range searchRange = NormalizeSearchRange(model, new Range(new TextPosition(1, 1), searchStart));
        IReadOnlyList<FindMatch> matches = DoFindMatchesMultiline(model, searchRange, searcher, captureMatches, DefaultLimit * 10);
        if (matches.Count > 0)
        {
            return matches[^1];
        }

        int lineCount = Math.Max(1, model.LineCount);
        int lastLineMaxColumn = model.GetLineMaxColumn(lineCount);
        if (searchStart.LineNumber != lineCount || searchStart.Column != lastLineMaxColumn)
        {
            TextPosition end = new(lineCount, lastLineMaxColumn);
            return DoFindPreviousMatchMultiline(model, end, searcher, captureMatches);
        }

        return null;
    }

    private static FindMatch? DoFindPreviousMatchLineByLine(ITextSearchAccess model, TextPosition searchStart, PieceTreeSearcher searcher, bool captureMatches)
    {
        int lineCount = Math.Max(1, model.LineCount);
        int startLineNumber = searchStart.LineNumber;

        string firstLineContent = model.GetLineContent(startLineNumber);
        string headSlice = Slice(firstLineContent, 0, Math.Max(0, searchStart.Column - 1));
        FindMatch? first = FindLastMatchInLine(searcher, headSlice, startLineNumber, captureMatches);
        if (first != null)
        {
            return first;
        }

        for (int i = 1; i <= lineCount; i++)
        {
            int lineIndex = (lineCount + startLineNumber - i - 1) % lineCount;
            string text = model.GetLineContent(lineIndex + 1);
            FindMatch? match = FindLastMatchInLine(searcher, text, lineIndex + 1, captureMatches);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void FindMatchesInLine(SearchData searchData, string text, int lineNumber, int deltaOffset, bool captureMatches, int limitResultCount, List<FindMatch> result)
    {
        if (!captureMatches && searchData.SimpleSearch != null)
        {
            string searchString = searchData.SimpleSearch;
            int searchLength = searchString.Length;
            int lastMatchIndex = -searchLength;
            while (result.Count < limitResultCount && (lastMatchIndex = text.IndexOf(searchString, lastMatchIndex + searchLength, StringComparison.Ordinal)) != -1)
            {
                if (searchData.WordSeparators == null || searchData.WordSeparators.IsValidMatch(text, lastMatchIndex, searchLength))
                {
                    Range range = new(lineNumber, lastMatchIndex + 1 + deltaOffset, lineNumber, lastMatchIndex + 1 + searchLength + deltaOffset);
                    result.Add(new FindMatch(range, null));
                }
            }
            return;
        }

        PieceTreeSearcher searcher = new(searchData.WordSeparators, searchData.Regex);
        searcher.Reset(0);
        Match? match;
        while (result.Count < limitResultCount && (match = searcher.Next(text)) != null)
        {
            Range range = new(lineNumber, match.Index + 1 + deltaOffset, lineNumber, match.Index + match.Length + 1 + deltaOffset);
            result.Add(CreateFindMatch(range, match, captureMatches));
        }
    }

    private static FindMatch? FindFirstMatchInLine(PieceTreeSearcher searcher, string text, int lineNumber, int fromColumn, bool captureMatches)
    {
        searcher.Reset(Math.Max(0, fromColumn - 1));
        Match? match = searcher.Next(text);
        if (match == null)
        {
            return null;
        }

        Range range = new(lineNumber, match.Index + 1, lineNumber, match.Index + match.Length + 1);
        return CreateFindMatch(range, match, captureMatches);
    }

    private static FindMatch? FindLastMatchInLine(PieceTreeSearcher searcher, string text, int lineNumber, bool captureMatches)
    {
        FindMatch? best = null;
        searcher.Reset(0);
        Match? match;
        while ((match = searcher.Next(text)) != null)
        {
            Range range = new(lineNumber, match.Index + 1, lineNumber, match.Index + match.Length + 1);
            best = CreateFindMatch(range, match, captureMatches);
        }

        return best;
    }

    private static FindMatch? FindFirstMatchInRange(ITextSearchAccess model, SearchData searchData, Range searchRange, bool captureMatches)
    {
        IReadOnlyList<FindMatch> matches = FindMatches(model, searchData, searchRange, captureMatches, 1);
        return matches.Count > 0 ? matches[0] : null;
    }

    private static FindMatch? FindLastMatchInRange(ITextSearchAccess model, SearchData searchData, Range searchRange, bool captureMatches)
    {
        IReadOnlyList<FindMatch> matches = FindMatches(model, searchData, searchRange, captureMatches, int.MaxValue);
        return matches.Count > 0 ? matches[matches.Count - 1] : null;
    }

    private static FindMatch? FindNextMatchWithinRangeSet(ITextSearchAccess model, SearchData searchData, TextPosition searchStart, bool captureMatches, SearchRangeSet rangeSet)
    {
        if (rangeSet.IsEmpty)
        {
            return null;
        }

        TextPosition clampedStart = ClampPosition(model, searchStart);
        int index = rangeSet.FindContainingRange(clampedStart);
        int visited = 0;

        if (index >= 0)
        {
            Range range = rangeSet[index];
            if (SearchRangeSet.Compare(clampedStart, range.End) < 0)
            {
                Range partial = new(clampedStart, range.End);
                FindMatch? match = FindFirstMatchInRange(model, searchData, partial, captureMatches);
                if (match != null)
                {
                    return match;
                }
            }

            index = (index + 1) % rangeSet.Count;
            visited++;
        }
        else
        {
            index = rangeSet.FindFirstRangeStartingAfter(clampedStart);
        }

        for (; visited < rangeSet.Count; visited++, index = (index + 1) % rangeSet.Count)
        {
            Range range = rangeSet[index];
            FindMatch? match = FindFirstMatchInRange(model, searchData, range, captureMatches);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static FindMatch? FindPreviousMatchWithinRangeSet(ITextSearchAccess model, SearchData searchData, TextPosition searchStart, bool captureMatches, SearchRangeSet rangeSet)
    {
        if (rangeSet.IsEmpty)
        {
            return null;
        }

        TextPosition clampedStart = ClampPosition(model, searchStart);
        int index = rangeSet.FindContainingRange(clampedStart);
        int visited = 0;

        if (index >= 0)
        {
            Range range = rangeSet[index];
            if (SearchRangeSet.Compare(clampedStart, range.Start) > 0)
            {
                Range partial = new(range.Start, clampedStart);
                FindMatch? match = FindLastMatchInRange(model, searchData, partial, captureMatches);
                if (match != null)
                {
                    return match;
                }
            }

            index = (index - 1 + rangeSet.Count) % rangeSet.Count;
            visited++;
        }
        else
        {
            index = rangeSet.FindLastRangeEndingBefore(clampedStart);
        }

        for (; visited < rangeSet.Count; visited++, index = (index - 1 + rangeSet.Count) % rangeSet.Count)
        {
            Range range = rangeSet[index];
            FindMatch? match = FindLastMatchInRange(model, searchData, range, captureMatches);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static Range GetMultilineMatchRange(ITextSearchAccess model, int deltaOffset, string text, LineFeedCounter? lineFeedCounter, int matchIndex, int matchLength)
    {
        int lineFeedsBeforeMatch = lineFeedCounter?.CountBefore(matchIndex) ?? 0;
        int startOffset = deltaOffset + matchIndex + lineFeedsBeforeMatch;

        int endOffset;
        if (lineFeedCounter != null)
        {
            int lineFeedsBeforeEnd = lineFeedCounter.CountBefore(matchIndex + matchLength);
            int lineFeedsInsideMatch = lineFeedsBeforeEnd - lineFeedsBeforeMatch;
            endOffset = startOffset + matchLength + lineFeedsInsideMatch;
        }
        else
        {
            endOffset = startOffset + matchLength;
        }

        TextPosition startPosition = model.GetPositionAt(startOffset);
        TextPosition endPosition = model.GetPositionAt(endOffset);
        return new Range(startPosition, endPosition);
    }

    private static FindMatch CreateFindMatch(Range range, Match match, bool captureMatches)
    {
        if (!captureMatches)
        {
            return new FindMatch(range, null);
        }

        GroupCollection groups = match.Groups;
        string[] values = new string[groups.Count];
        for (int i = 0; i < groups.Count; i++)
        {
            values[i] = groups[i].Value;
        }

        return new FindMatch(range, values);
    }

    private static Range NormalizeSearchRange(ITextSearchAccess model, Range range)
    {
        TextPosition start = ClampPosition(model, range.Start);
        TextPosition end = ClampPosition(model, range.End);
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return new Range(start, end);
    }

    private static TextPosition ClampPosition(ITextSearchAccess model, TextPosition position)
    {
        int lineCount = Math.Max(1, model.LineCount);
        int lineNumber = Math.Clamp(position.LineNumber, 1, lineCount);
        int maxColumn = model.GetLineMaxColumn(lineNumber);
        int column = Math.Clamp(position.Column, 1, Math.Max(1, maxColumn));
        return new TextPosition(lineNumber, column);
    }

    private static string Slice(string text, int startIndex, int endIndex)
    {
        if (startIndex < 0)
        {
            startIndex = 0;
        }

        if (endIndex < startIndex)
        {
            endIndex = startIndex;
        }

        int length = Math.Clamp(endIndex - startIndex, 0, Math.Max(0, text.Length - startIndex));
        if (length <= 0)
        {
            return string.Empty;
        }

        return text.Substring(startIndex, length);
    }
}
