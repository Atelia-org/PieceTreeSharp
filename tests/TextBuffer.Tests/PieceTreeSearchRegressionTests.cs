using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Tests.Helpers;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests;

/// <summary>
/// Mirrors VS Code's pieceTreeTextBufferSearch.test.ts scenarios plus the historical
/// regression issues that originally motivated this suite.
/// </summary>
public class PieceTreeSearchRegressionTests
{
    private static TextModel BuildModel(params string[] lines)
        => TestEditorBuilder.Create().WithLines(lines).Build();

    private static Range WholeDocument(PieceTreeFuzzHarness harness)
    {
        int lastLine = Math.Max(1, harness.GetLineCount());
        int lastColumn = harness.GetLineContent(lastLine).Length + 1;
        return new Range(1, 1, lastLine, lastColumn);
    }

    #region pieceTreeTextBufferSearch.test.ts parity

    [Fact]
    public void MultiLineRegex_MatchesAcrossPieceBoundaries()
    {
        TextModel model = TestEditorBuilder.Create()
            .WithLines(
                "alpha block one",
                "mid block one",
                "beta closing one",
                "separator",
                "alpha block two",
                "mid block two",
                "beta closing two")
            .Build();

        IReadOnlyList<FindMatch> matches = model.FindMatches(
            "alpha.*\\nmid",
            searchRange: null,
            isRegex: true,
            matchCase: true,
            wordSeparators: null,
            captureMatches: false);

        Assert.Equal(2, matches.Count);
        Assert.Equal(new Range(1, 1, 2, 4), matches[0].Range);
        Assert.Equal(new Range(5, 1, 6, 4), matches[1].Range);
    }

    [Fact]
    public void CrlfNormalization_PreservesLiteralSearchResults()
    {
        TextModel model = TestEditorBuilder.Create()
            .WithCRLF()
            .WithLines("alpha", "beta", "alpha", "beta")
            .Build();

        static List<Range> SnapshotAlphaBeta(TextModel textModel)
        {
            IReadOnlyList<FindMatch> matches = textModel.FindMatches(
                "alpha\nbeta",
                searchRange: null,
                isRegex: false,
                matchCase: true,
                wordSeparators: null,
                captureMatches: false);

            List<Range> ranges = new(matches.Count);
            foreach (FindMatch match in matches)
            {
                ranges.Add(match.Range);
            }
            return ranges;
        }

        List<Range> before = SnapshotAlphaBeta(model);
        Assert.Equal(2, before.Count);

        model.SetEol(EndOfLineSequence.LF);
        List<Range> after = SnapshotAlphaBeta(model);

        Assert.Equal(before, after);
    }

    [Fact]
    public void SearchOffsetCache_RebuildsAfterEdits()
    {
        using PieceTreeFuzzHarness harness = new(
            nameof(SearchOffsetCache_RebuildsAfterEdits),
            initialText: string.Join("\n", new[] { "* [ ] task1", "* [x] task2", "* [ ] task3" }));

        SearchParams searchParams = new("\\[[ x]\\]", isRegex: true, matchCase: true, wordSeparators: null);
        SearchData? searchData = searchParams.ParseSearchRequest();
        Assert.NotNull(searchData);

        List<FindMatch> initialMatches = RunPieceTreeSearch(harness, searchData!);
        Assert.Equal(3, initialMatches.Count);
        PieceTreeBufferAssertions.AssertSearchCachePrimed(harness, "search-cache-initial");

        DeleteRange(harness, initialMatches[1].Range, "delete second checkbox");
        List<FindMatch> afterDelete = RunPieceTreeSearch(harness, searchData!);
        Assert.Equal(2, afterDelete.Count);
        PieceTreeBufferAssertions.AssertSearchCachePrimed(harness, "search-cache-after-delete");

        harness.Insert(harness.Buffer.Length, "\n* [x] task4", "append new checkbox");
        List<FindMatch> afterInsert = RunPieceTreeSearch(harness, searchData!);
        Assert.Equal(3, afterInsert.Count);
        PieceTreeBufferAssertions.AssertSearchCachePrimed(harness, "search-cache-after-insert");
    }

    [Fact]
    public void SurrogateAwareReplacement_DoesNotSplitPairs()
    {
        string grin = char.ConvertFromUtf32(0x1F600);
        string catLaptop = char.ConvertFromUtf32(0x1F408) + "\u200D" + char.ConvertFromUtf32(0x1F4BB);

        TextModel model = TestEditorBuilder.Create()
            .WithContent($"Icons: {grin} and {catLaptop}")
            .Build();

        (string Emoji, string Replacement)[] replacements =
        [
            (grin, "[grin]"),
            (catLaptop, "[cat]")
        ];

        foreach ((string? emoji, string? replacement) in replacements)
        {
            IReadOnlyList<FindMatch> matches = model.FindMatches(
                emoji,
                searchRange: null,
                isRegex: false,
                matchCase: true,
                wordSeparators: null,
                captureMatches: false);

            Assert.Single(matches);
            Range range = matches[0].Range;
            Assert.Equal(emoji.Length, range.End.Column - range.Start.Column);

            model.ApplyEdits([new TextEdit(range.Start, range.End, replacement)]);
        }

        Assert.Equal("Icons: [grin] and [cat]", model.GetValue());
    }

    // TODO(AA4-search-backlog): Port the remaining pieceTreeTextBufferSearch.test.ts cases tracked in
    // agent-team/handoffs/AA4-SearchReview-20251125.md (undo/redo + cache diagnostics) once the
    // AA4 search harness exposes those instrumentation hooks to tests.

    #endregion

    #region Issue #45892 - Empty Model Search

    [Fact]
    public void Issue45892_EmptyBufferSearch_ReturnsEmptyArray()
    {
        TextModel model = TestEditorBuilder.Create().WithContent(string.Empty).Build();

        IReadOnlyList<FindMatch> matches = model.FindMatches(
            "abc",
            new Range(1, 1, 1, 1),
            isRegex: false,
            matchCase: false,
            wordSeparators: null,
            captureMatches: false);

        Assert.Empty(matches);
    }

    [Fact]
    public void Issue45892_EmptyBufferRegexSearch_ReturnsEmptyArray()
    {
        TextModel model = TestEditorBuilder.Create().WithContent(string.Empty).Build();

        IReadOnlyList<FindMatch> matches = model.FindMatches(
            "abc",
            new Range(1, 1, 1, 1),
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            captureMatches: false);

        Assert.Empty(matches);
    }

    [Fact]
    public void Issue45892_BufferDeletedToEmpty_SearchReturnsEmpty()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("some content").Build();

        model.ApplyEdits(
        [
            new TextEdit(
                new TextPosition(1, 1),
                new TextPosition(1, model.GetLineMaxColumn(1)),
                string.Empty)
        ]);

        Assert.Equal(string.Empty, model.GetValue());

        IReadOnlyList<FindMatch> matches = model.FindMatches(
            "content",
            searchRange: null,
            isRegex: false,
            matchCase: false,
            wordSeparators: null,
            captureMatches: false);

        Assert.Empty(matches);
    }

    #endregion

    #region Issue #45770 - Node Boundary Search

    [Fact]
    public void Issue45770_FindInNode_ShouldNotCrossNodeBoundary()
    {
        TextModel model = TestEditorBuilder.Create()
            .WithLines(
                "balabalababalabalababalabalaba",
                "balabalababalabalababalabalaba",
                string.Empty,
                "* [ ] task1",
                "* [x] task2 balabalaba",
                "* [ ] task 3")
            .Build();

        model.ApplyEdits(
        [
            new TextEdit(new TextPosition(1, 1), model.GetPositionAt(62), string.Empty)
        ]);

        TextPosition pos16 = model.GetPositionAt(16);
        TextPosition pos17 = model.GetPositionAt(17);
        model.ApplyEdits([new TextEdit(pos16, pos17, string.Empty)]);

        TextPosition newPos16 = model.GetPositionAt(16);
        model.ApplyEdits([new TextEdit(newPos16, newPos16, " ")]);

        IReadOnlyList<FindMatch> matches = model.FindMatches(
            "\\[",
            new Range(1, 1, 4, 13),
            isRegex: true,
            matchCase: true,
            wordSeparators: null,
            captureMatches: false);

        Assert.Equal(3, matches.Count);
    }

    [Fact]
    public void Issue45770_Simplified_SearchAfterDeleteInsert()
    {
        TextModel model = BuildModel("* [ ] task1", "* [x] task2", "* [ ] task3");

        IReadOnlyList<FindMatch> matches = model.FindMatches(
            "\\[",
            searchRange: null,
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            captureMatches: false);

        Assert.Equal(3, matches.Count);

        Assert.All(matches, match => Assert.Equal(3, match.Range.Start.Column));
        Assert.Equal(1, matches[0].Range.Start.LineNumber);
        Assert.Equal(2, matches[1].Range.Start.LineNumber);
        Assert.Equal(3, matches[2].Range.Start.LineNumber);
    }

    [Fact]
    public void Search_SearchingFromTheMiddle()
    {
        TextModel model = BuildModel("def", "dbcabc");

        TextPosition pos4 = model.GetPositionAt(4);
        TextPosition pos5 = model.GetPositionAt(5);
        model.ApplyEdits([new TextEdit(pos4, pos5, string.Empty)]);

        Assert.Equal("def\nbcabc", model.GetValue());

        IReadOnlyList<FindMatch> matches = model.FindMatches(
            "a",
            new Range(2, 3, 2, 6),
            isRegex: true,
            matchCase: true,
            wordSeparators: null,
            captureMatches: false);

        Assert.Single(matches);
        Assert.Equal(new Range(2, 3, 2, 4), matches[0].Range);
    }

    #endregion

    #region Additional Search Edge Cases

    [Fact]
    public void Search_AfterMultipleEdits()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("abcdefghij").Build();

        model.ApplyEdits([new TextEdit(new TextPosition(1, 3), new TextPosition(1, 3), "X")]);
        model.ApplyEdits([new TextEdit(new TextPosition(1, 7), new TextPosition(1, 7), "Y")]);

        IReadOnlyList<FindMatch> matches = model.FindMatches(
            "[XY]",
            searchRange: null,
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            captureMatches: false);

        Assert.Equal(2, matches.Count);
        Assert.Equal(3, matches[0].Range.Start.Column);
        Assert.Equal(7, matches[1].Range.Start.Column);
    }

    [Fact]
    public void Search_SpanningLineBreaks()
    {
        TextModel model = BuildModel("line1", "line2", "line3");

        IReadOnlyList<FindMatch> matches = model.FindMatches(
            "line",
            searchRange: null,
            isRegex: false,
            matchCase: false,
            wordSeparators: null,
            captureMatches: false);

        Assert.Equal(3, matches.Count);
        Assert.Equal(1, matches[0].Range.Start.LineNumber);
        Assert.Equal(2, matches[1].Range.Start.LineNumber);
        Assert.Equal(3, matches[2].Range.Start.LineNumber);
    }

    [Fact]
    public void Search_RespectsLimitCount()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("a a a a a a a a a a").Build();

        IReadOnlyList<FindMatch> matches = model.FindMatches(
            "a",
            searchRange: null,
            isRegex: false,
            matchCase: false,
            wordSeparators: null,
            captureMatches: false,
            limitResultCount: 3);

        Assert.Equal(3, matches.Count);
    }

    #endregion

    private static List<FindMatch> RunPieceTreeSearch(PieceTreeFuzzHarness harness, SearchData searchData)
    {
        Range range = WholeDocument(harness);
        return harness.Buffer.InternalModel.FindMatchesLineByLine(range, searchData, captureMatches: false, limitResultCount: int.MaxValue);
    }

    private static void DeleteRange(PieceTreeFuzzHarness harness, Range range, string operation)
    {
        int startOffset = harness.GetOffsetAt(range.Start);
        int endOffset = harness.GetOffsetAt(range.End);
        int length = Math.Max(0, endOffset - startOffset);
        harness.Delete(startOffset, length, operation);
    }
}
