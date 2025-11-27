using System;
using System.Collections.Generic;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Tests.Helpers;
using Xunit;
using Range = PieceTree.TextBuffer.Core.Range;
using TextPosition = PieceTree.TextBuffer.TextPosition;

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
        var lastLine = Math.Max(1, harness.GetLineCount());
        var lastColumn = harness.GetLineContent(lastLine).Length + 1;
        return new Range(1, 1, lastLine, lastColumn);
    }

    #region pieceTreeTextBufferSearch.test.ts parity

    [Fact]
    public void MultiLineRegex_MatchesAcrossPieceBoundaries()
    {
        var model = TestEditorBuilder.Create()
            .WithLines(
                "alpha block one",
                "mid block one",
                "beta closing one",
                "separator",
                "alpha block two",
                "mid block two",
                "beta closing two")
            .Build();

        var matches = model.FindMatches(
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
        var model = TestEditorBuilder.Create()
            .WithCRLF()
            .WithLines("alpha", "beta", "alpha", "beta")
            .Build();

        static List<Range> SnapshotAlphaBeta(TextModel textModel)
        {
            var matches = textModel.FindMatches(
                "alpha\nbeta",
                searchRange: null,
                isRegex: false,
                matchCase: true,
                wordSeparators: null,
                captureMatches: false);

            var ranges = new List<Range>(matches.Count);
            foreach (var match in matches)
            {
                ranges.Add(match.Range);
            }
            return ranges;
        }

        var before = SnapshotAlphaBeta(model);
        Assert.Equal(2, before.Count);

        model.SetEol(EndOfLineSequence.LF);
        var after = SnapshotAlphaBeta(model);

        Assert.Equal(before, after);
    }

    [Fact]
    public void SearchOffsetCache_RebuildsAfterEdits()
    {
        using var harness = new PieceTreeFuzzHarness(
            nameof(SearchOffsetCache_RebuildsAfterEdits),
            initialText: string.Join("\n", new[] { "* [ ] task1", "* [x] task2", "* [ ] task3" }));

        var searchParams = new SearchParams("\\[[ x]\\]", isRegex: true, matchCase: true, wordSeparators: null);
        var searchData = searchParams.ParseSearchRequest();
        Assert.NotNull(searchData);

        var initialMatches = RunPieceTreeSearch(harness, searchData!);
        Assert.Equal(3, initialMatches.Count);
        PieceTreeBufferAssertions.AssertSearchCachePrimed(harness, "search-cache-initial");

        DeleteRange(harness, initialMatches[1].Range, "delete second checkbox");
        var afterDelete = RunPieceTreeSearch(harness, searchData!);
        Assert.Equal(2, afterDelete.Count);
        PieceTreeBufferAssertions.AssertSearchCachePrimed(harness, "search-cache-after-delete");

        harness.Insert(harness.Buffer.Length, "\n* [x] task4", "append new checkbox");
        var afterInsert = RunPieceTreeSearch(harness, searchData!);
        Assert.Equal(3, afterInsert.Count);
        PieceTreeBufferAssertions.AssertSearchCachePrimed(harness, "search-cache-after-insert");
    }

    [Fact]
    public void SurrogateAwareReplacement_DoesNotSplitPairs()
    {
        var grin = char.ConvertFromUtf32(0x1F600);
        var catLaptop = char.ConvertFromUtf32(0x1F408) + "\u200D" + char.ConvertFromUtf32(0x1F4BB);

        var model = TestEditorBuilder.Create()
            .WithContent($"Icons: {grin} and {catLaptop}")
            .Build();

        var replacements = new (string Emoji, string Replacement)[]
        {
            (grin, "[grin]"),
            (catLaptop, "[cat]")
        };

        foreach (var (emoji, replacement) in replacements)
        {
            var matches = model.FindMatches(
                emoji,
                searchRange: null,
                isRegex: false,
                matchCase: true,
                wordSeparators: null,
                captureMatches: false);

            Assert.Single(matches);
            var range = matches[0].Range;
            Assert.Equal(emoji.Length, range.End.Column - range.Start.Column);

            model.ApplyEdits(new[] { new TextEdit(range.Start, range.End, replacement) });
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
        var model = TestEditorBuilder.Create().WithContent(string.Empty).Build();

        var matches = model.FindMatches(
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
        var model = TestEditorBuilder.Create().WithContent(string.Empty).Build();

        var matches = model.FindMatches(
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
        var model = TestEditorBuilder.Create().WithContent("some content").Build();

        model.ApplyEdits(new[]
        {
            new TextEdit(
                new TextPosition(1, 1),
                new TextPosition(1, model.GetLineMaxColumn(1)),
                string.Empty)
        });

        Assert.Equal(string.Empty, model.GetValue());

        var matches = model.FindMatches(
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
        var model = TestEditorBuilder.Create()
            .WithLines(
                "balabalababalabalababalabalaba",
                "balabalababalabalababalabalaba",
                string.Empty,
                "* [ ] task1",
                "* [x] task2 balabalaba",
                "* [ ] task 3")
            .Build();

        model.ApplyEdits(new[]
        {
            new TextEdit(new TextPosition(1, 1), model.GetPositionAt(62), string.Empty)
        });

        var pos16 = model.GetPositionAt(16);
        var pos17 = model.GetPositionAt(17);
        model.ApplyEdits(new[] { new TextEdit(pos16, pos17, string.Empty) });

        var newPos16 = model.GetPositionAt(16);
        model.ApplyEdits(new[] { new TextEdit(newPos16, newPos16, " ") });

        var matches = model.FindMatches(
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
        var model = BuildModel("* [ ] task1", "* [x] task2", "* [ ] task3");

        var matches = model.FindMatches(
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
        var model = BuildModel("def", "dbcabc");

        var pos4 = model.GetPositionAt(4);
        var pos5 = model.GetPositionAt(5);
        model.ApplyEdits(new[] { new TextEdit(pos4, pos5, string.Empty) });

        Assert.Equal("def\nbcabc", model.GetValue());

        var matches = model.FindMatches(
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
        var model = TestEditorBuilder.Create().WithContent("abcdefghij").Build();

        model.ApplyEdits(new[] { new TextEdit(new TextPosition(1, 3), new TextPosition(1, 3), "X") });
        model.ApplyEdits(new[] { new TextEdit(new TextPosition(1, 7), new TextPosition(1, 7), "Y") });

        var matches = model.FindMatches(
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
        var model = BuildModel("line1", "line2", "line3");

        var matches = model.FindMatches(
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
        var model = TestEditorBuilder.Create().WithContent("a a a a a a a a a a").Build();

        var matches = model.FindMatches(
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
        var range = WholeDocument(harness);
        return harness.Buffer.InternalModel.FindMatchesLineByLine(range, searchData, captureMatches: false, limitResultCount: int.MaxValue);
    }

    private static void DeleteRange(PieceTreeFuzzHarness harness, Range range, string operation)
    {
        var startOffset = harness.GetOffsetAt(range.Start);
        var endOffset = harness.GetOffsetAt(range.End);
        var length = Math.Max(0, endOffset - startOffset);
        harness.Delete(startOffset, length, operation);
    }
}
