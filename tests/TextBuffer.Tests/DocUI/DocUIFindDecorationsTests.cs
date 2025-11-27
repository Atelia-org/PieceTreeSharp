/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.DocUI;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests.DocUI;

public sealed class DocUIFindDecorationsTests
{
    [Fact]
    public void RangeHighlightTrimsTrailingBlankLines()
    {
        TextModel model = new("one\ntwo\nthree\n\n");
        using FindDecorations decorations = new(model);

        Range match = CreateRange(1, 1, 4, 1);
        decorations.Set(CreateMatches(match), null);
        decorations.SetCurrentMatch(match);

        ModelDecoration highlight = GetDecorationsByDescription(model, "find-range-highlight").Single();
        Range highlightRange = ToRange(model, highlight);

        Assert.Equal(1, highlightRange.StartLineNumber);
        Assert.Equal(3, highlightRange.EndLineNumber);
        Assert.Equal(model.GetLineMaxColumn(3), highlightRange.EndColumn);

        decorations.SetCurrentMatch(null);
        Assert.Empty(GetDecorationsByDescription(model, "find-range-highlight"));
    }

    [Fact]
    public void LargeResultSetsCreateOverviewApproximationDecorations()
    {
        TextModel model = new(string.Join("\n", Enumerable.Repeat("abc", 1205)));
        using FindDecorations decorations = new(model);
        Range[] matches = Enumerable.Range(1, 1205)
            .Select(line => new Range(new TextPosition(line, 1), new TextPosition(line, 4)))
            .ToArray();

        decorations.Set(CreateMatches(matches), findScopes: null);

        IReadOnlyList<ModelDecoration> all = model.GetAllDecorations();
        ModelDecoration[] matchDecorations = all.Where(d => d.Options.Description == "find-match-no-overview").ToArray();
        ModelDecoration[] overviewDecorations = all.Where(d => d.Options.Description == "find-match-only-overview").ToArray();

        Assert.Equal(matches.Length, matchDecorations.Length);
        Assert.NotEmpty(overviewDecorations);
        Assert.Equal(matches.Length, decorations.GetCount());
    }

    [Fact]
    public void CurrentMatchPositionUsesSelectionIntersection()
    {
        TextModel model = new("one\ntwo\nthree\n");
        using FindDecorations decorations = new(model);
        FindMatch[] matches = CreateMatches(
            CreateRange(1, 1, 1, 4),
            CreateRange(2, 1, 2, 4),
            CreateRange(3, 1, 3, 4));

        decorations.Set(matches, null);
        decorations.SetCurrentMatch(matches[1].Range);

        int index = decorations.GetCurrentMatchesPosition(matches[2].Range);
        Assert.Equal(3, index);
    }

    [Fact]
    public void CollapsedCaretAtMatchStartReturnsIndex()
    {
        TextModel model = new("match-one\nmatch-two\n");
        using FindDecorations decorations = new(model);
        FindMatch[] matches = CreateMatches(
            CreateRange(1, 1, 1, 6),
            CreateRange(2, 1, 2, 6));

        decorations.Set(matches, null);

        Range caret = new(new TextPosition(1, 1), new TextPosition(1, 1));
        int index = decorations.GetCurrentMatchesPosition(caret);

        Assert.Equal(1, index);
    }

    [Fact]
    public void FindScopesPreserveTrailingNewline()
    {
        TextModel model = new("one\ntwo\nthree\n\n");
        using FindDecorations decorations = new(model);
        Range scope = CreateRange(1, 1, 4, 1);
        decorations.Set(Array.Empty<FindMatch>(), [scope]);

        Range[]? scopes = decorations.GetFindScopes();
        Assert.NotNull(scopes);
        Range resolved = Assert.Single(scopes!);
        Assert.Equal(scope, resolved);
    }

    [Fact]
    public void FindScopesTrackEdits()
    {
        TextModel model = new("first\nscope-line\nscope-end\n");
        using FindDecorations decorations = new(model);
        Range scope = CreateRange(2, 1, 3, 1);
        decorations.Set(Array.Empty<FindMatch>(), [scope]);

        model.PushEditOperations(
        [
            new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "intro\n")
        ]);

        model.PushEditOperations(
        [
            new TextEdit(new TextPosition(3, 6), new TextPosition(3, 6), "++")
        ]);

        Range[]? scopes = decorations.GetFindScopes();
        Assert.NotNull(scopes);
        Range updated = Assert.Single(scopes!);
        Assert.Equal(3, updated.StartLineNumber);
        Assert.Equal(4, updated.EndLineNumber);
        string scopedValue = model.GetValueInRange(updated, EndOfLinePreference.LF);
        Assert.StartsWith("scope", scopedValue, StringComparison.Ordinal);
        Assert.Contains("++", scopedValue, StringComparison.Ordinal);
        Assert.EndsWith("\n", scopedValue, StringComparison.Ordinal);
    }

    [Fact]
    public void OverviewThrottlingRespectsViewportHeight()
    {
        const int lineCount = 2400;
        string text = string.Join("\n", Enumerable.Repeat("match", lineCount));
        Range[] ranges = Enumerable.Range(1, lineCount)
            .Select(line => CreateRange(line, 1, line, 6))
            .ToArray();

        TextModel model = new(text);
        using FindDecorations decorations = new(model, () => 300d);
        decorations.Set(CreateMatches(ranges), null);

        ModelDecoration[] overviewDecorations = model.GetAllDecorations()
            .Where(d => string.Equals(d.Options.Description, "find-match-only-overview", StringComparison.Ordinal))
            .ToArray();

        int mergeDelta = ComputeMergeLinesDelta(300d, lineCount);
        int expected = ComputeOverviewDecorationCount(ranges, mergeDelta);

        Assert.Equal(expected, overviewDecorations.Length);
        Assert.True(expected < lineCount / 10, "overview decorations should remain bounded");
    }

    [Fact]
    public void MatchNavigationWrapsAroundDocumentEdges()
    {
        TextModel model = new("one\ntwo\nthree\n");
        using FindDecorations decorations = new(model);
        FindMatch[] matches = CreateMatches(
            CreateRange(1, 1, 1, 4),
            CreateRange(2, 1, 2, 4));

        decorations.Set(matches, null);

        Range? before = decorations.MatchBeforePosition(new TextPosition(1, 1));
        Assert.Equal(matches[^1].Range, before);

        Range? after = decorations.MatchAfterPosition(new TextPosition(10, 1));
        Assert.Equal(matches[0].Range, after);
    }

    [Fact]
    public void CurrentMatchSwapKeepsSingleDecorationPerRange()
    {
        TextModel model = new("alpha\nbeta\n");
        using FindDecorations decorations = new(model);
        Range match = CreateRange(2, 1, 2, 3);

        decorations.Set(CreateMatches(match), null);
        decorations.SetCurrentMatch(match);

        ModelDecoration[] relevant = model.GetAllDecorations()
            .Where(IsFindMatchDecoration)
            .ToArray();

        Assert.Single(relevant);
    }

    private static FindMatch[] CreateMatches(params Range[] ranges)
        => CreateMatches((IEnumerable<Range>)ranges);

    private static FindMatch[] CreateMatches(IEnumerable<Range> ranges)
        => ranges.Select(r => new FindMatch(r, matches: null)).ToArray();

    private static Range ToRange(TextModel model, ModelDecoration decoration)
    {
        TextPosition start = model.GetPositionAt(decoration.Range.StartOffset);
        TextPosition end = model.GetPositionAt(decoration.Range.EndOffset);
        return new Range(start, end);
    }

    private static IReadOnlyList<ModelDecoration> GetDecorationsByDescription(TextModel model, string description)
        => model.GetAllDecorations().Where(d => string.Equals(d.Options.Description, description, StringComparison.Ordinal)).ToArray();

    private static Range CreateRange(int startLine, int startColumn, int endLine, int endColumn)
        => new(new TextPosition(startLine, startColumn), new TextPosition(endLine, endColumn));

    private static bool IsFindMatchDecoration(ModelDecoration decoration)
    {
        string? className = decoration.Options.ClassName;
        return string.Equals(className, "findMatch", StringComparison.Ordinal)
            || string.Equals(className, "currentFindMatch", StringComparison.Ordinal);
    }

    private static int ComputeMergeLinesDelta(double viewportHeight, int lineCount)
    {
        double height = viewportHeight > 0 ? viewportHeight : 600d;
        int lines = Math.Max(1, lineCount);
        double approxPixelsPerLine = height / lines;
        if (approxPixelsPerLine <= 0)
        {
            return 2;
        }

        int delta = (int)Math.Ceiling(3d / approxPixelsPerLine);
        return Math.Max(2, delta);
    }

    private static int ComputeOverviewDecorationCount(IReadOnlyList<Range> ranges, int mergeLinesDelta)
    {
        if (ranges.Count == 0)
        {
            return 0;
        }

        int count = 1;
        int prevEnd = ranges[0].EndLineNumber;

        for (int i = 1; i < ranges.Count; i++)
        {
            Range current = ranges[i];
            if (prevEnd + mergeLinesDelta >= current.StartLineNumber)
            {
                if (current.EndLineNumber > prevEnd)
                {
                    prevEnd = current.EndLineNumber;
                }

                continue;
            }

            count++;
            prevEnd = current.EndLineNumber;
        }

        return count;
    }
}
