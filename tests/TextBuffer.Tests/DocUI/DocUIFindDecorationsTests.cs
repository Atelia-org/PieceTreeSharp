/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.DocUI;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests.DocUI
{
    public sealed class DocUIFindDecorationsTests
    {
        [Fact]
        public void RangeHighlightTrimsTrailingBlankLines()
        {
            var model = new TextModel("one\ntwo\nthree\n\n");
            using var decorations = new FindDecorations(model);

            var match = CreateRange(1, 1, 4, 1);
            decorations.Set(CreateMatches(match), null);
            decorations.SetCurrentMatch(match);

            var highlight = GetDecorationsByDescription(model, "find-range-highlight").Single();
            var highlightRange = ToRange(model, highlight);

            Assert.Equal(1, highlightRange.StartLineNumber);
            Assert.Equal(3, highlightRange.EndLineNumber);
            Assert.Equal(model.GetLineMaxColumn(3), highlightRange.EndColumn);

            decorations.SetCurrentMatch(null);
            Assert.Empty(GetDecorationsByDescription(model, "find-range-highlight"));
        }

        [Fact]
        public void LargeResultSetsCreateOverviewApproximationDecorations()
        {
            var model = new TextModel(string.Join("\n", Enumerable.Repeat("abc", 1205)));
            using var decorations = new FindDecorations(model);
            var matches = Enumerable.Range(1, 1205)
                .Select(line => new Range(new TextPosition(line, 1), new TextPosition(line, 4)))
                .ToArray();

            decorations.Set(CreateMatches(matches), findScopes: null);

            var all = model.GetAllDecorations();
            var matchDecorations = all.Where(d => d.Options.Description == "find-match-no-overview").ToArray();
            var overviewDecorations = all.Where(d => d.Options.Description == "find-match-only-overview").ToArray();

            Assert.Equal(matches.Length, matchDecorations.Length);
            Assert.NotEmpty(overviewDecorations);
            Assert.Equal(matches.Length, decorations.GetCount());
        }

        [Fact]
        public void CurrentMatchPositionUsesSelectionIntersection()
        {
            var model = new TextModel("one\ntwo\nthree\n");
            using var decorations = new FindDecorations(model);
            var matches = CreateMatches(
                CreateRange(1, 1, 1, 4),
                CreateRange(2, 1, 2, 4),
                CreateRange(3, 1, 3, 4));

            decorations.Set(matches, null);
            decorations.SetCurrentMatch(matches[1].Range);

            var index = decorations.GetCurrentMatchesPosition(matches[2].Range);
            Assert.Equal(3, index);
        }

        [Fact]
        public void CollapsedCaretAtMatchStartReturnsIndex()
        {
            var model = new TextModel("match-one\nmatch-two\n");
            using var decorations = new FindDecorations(model);
            var matches = CreateMatches(
                CreateRange(1, 1, 1, 6),
                CreateRange(2, 1, 2, 6));

            decorations.Set(matches, null);

            var caret = new Range(new TextPosition(1, 1), new TextPosition(1, 1));
            var index = decorations.GetCurrentMatchesPosition(caret);

            Assert.Equal(1, index);
        }

        [Fact]
        public void FindScopesPreserveTrailingNewline()
        {
            var model = new TextModel("one\ntwo\nthree\n\n");
            using var decorations = new FindDecorations(model);
            var scope = CreateRange(1, 1, 4, 1);
            decorations.Set(Array.Empty<FindMatch>(), new[] { scope });

            var scopes = decorations.GetFindScopes();
            Assert.NotNull(scopes);
            var resolved = Assert.Single(scopes!);
            Assert.Equal(scope, resolved);
        }

        [Fact]
        public void FindScopesTrackEdits()
        {
            var model = new TextModel("first\nscope-line\nscope-end\n");
            using var decorations = new FindDecorations(model);
            var scope = CreateRange(2, 1, 3, 1);
            decorations.Set(Array.Empty<FindMatch>(), new[] { scope });

            model.PushEditOperations(new[]
            {
                new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "intro\n")
            });

            model.PushEditOperations(new[]
            {
                new TextEdit(new TextPosition(3, 6), new TextPosition(3, 6), "++")
            });

            var scopes = decorations.GetFindScopes();
            Assert.NotNull(scopes);
            var updated = Assert.Single(scopes!);
            Assert.Equal(3, updated.StartLineNumber);
            Assert.Equal(4, updated.EndLineNumber);
            var scopedValue = model.GetValueInRange(updated, EndOfLinePreference.LF);
            Assert.StartsWith("scope", scopedValue, StringComparison.Ordinal);
            Assert.Contains("++", scopedValue, StringComparison.Ordinal);
            Assert.EndsWith("\n", scopedValue, StringComparison.Ordinal);
        }

        [Fact]
        public void OverviewThrottlingRespectsViewportHeight()
        {
            const int lineCount = 2400;
            var text = string.Join("\n", Enumerable.Repeat("match", lineCount));
            var ranges = Enumerable.Range(1, lineCount)
                .Select(line => CreateRange(line, 1, line, 6))
                .ToArray();

            var model = new TextModel(text);
            using var decorations = new FindDecorations(model, () => 300d);
            decorations.Set(CreateMatches(ranges), null);

            var overviewDecorations = model.GetAllDecorations()
                .Where(d => string.Equals(d.Options.Description, "find-match-only-overview", StringComparison.Ordinal))
                .ToArray();

            var mergeDelta = ComputeMergeLinesDelta(300d, lineCount);
            var expected = ComputeOverviewDecorationCount(ranges, mergeDelta);

            Assert.Equal(expected, overviewDecorations.Length);
            Assert.True(expected < lineCount / 10, "overview decorations should remain bounded");
        }

        [Fact]
        public void MatchNavigationWrapsAroundDocumentEdges()
        {
            var model = new TextModel("one\ntwo\nthree\n");
            using var decorations = new FindDecorations(model);
            var matches = CreateMatches(
                CreateRange(1, 1, 1, 4),
                CreateRange(2, 1, 2, 4));

            decorations.Set(matches, null);

            var before = decorations.MatchBeforePosition(new TextPosition(1, 1));
            Assert.Equal(matches[^1].Range, before);

            var after = decorations.MatchAfterPosition(new TextPosition(10, 1));
            Assert.Equal(matches[0].Range, after);
        }

        [Fact]
        public void CurrentMatchSwapKeepsSingleDecorationPerRange()
        {
            var model = new TextModel("alpha\nbeta\n");
            using var decorations = new FindDecorations(model);
            var match = CreateRange(2, 1, 2, 3);

            decorations.Set(CreateMatches(match), null);
            decorations.SetCurrentMatch(match);

            var relevant = model.GetAllDecorations()
                .Where(IsFindMatchDecoration)
                .ToArray();

            Assert.Single(relevant);
        }

        private static FindMatch[] CreateMatches(params Range[] ranges)
            => ranges.Select(r => new FindMatch(r, matches: null)).ToArray();

        private static FindMatch[] CreateMatches(IEnumerable<Range> ranges)
            => ranges.Select(r => new FindMatch(r, matches: null)).ToArray();

        private static Range ToRange(TextModel model, ModelDecoration decoration)
        {
            var start = model.GetPositionAt(decoration.Range.StartOffset);
            var end = model.GetPositionAt(decoration.Range.EndOffset);
            return new Range(start, end);
        }

        private static IReadOnlyList<ModelDecoration> GetDecorationsByDescription(TextModel model, string description)
            => model.GetAllDecorations().Where(d => string.Equals(d.Options.Description, description, StringComparison.Ordinal)).ToArray();

        private static Range CreateRange(int startLine, int startColumn, int endLine, int endColumn)
            => new(new TextPosition(startLine, startColumn), new TextPosition(endLine, endColumn));

        private static bool IsFindMatchDecoration(ModelDecoration decoration)
        {
            var className = decoration.Options.ClassName;
            return string.Equals(className, "findMatch", StringComparison.Ordinal)
                || string.Equals(className, "currentFindMatch", StringComparison.Ordinal);
        }

        private static int ComputeMergeLinesDelta(double viewportHeight, int lineCount)
        {
            var height = viewportHeight > 0 ? viewportHeight : 600d;
            var lines = Math.Max(1, lineCount);
            var approxPixelsPerLine = height / lines;
            if (approxPixelsPerLine <= 0)
            {
                return 2;
            }

            var delta = (int)Math.Ceiling(3d / approxPixelsPerLine);
            return Math.Max(2, delta);
        }

        private static int ComputeOverviewDecorationCount(IReadOnlyList<Range> ranges, int mergeLinesDelta)
        {
            if (ranges.Count == 0)
            {
                return 0;
            }

            var count = 1;
            var prevEnd = ranges[0].EndLineNumber;

            for (int i = 1; i < ranges.Count; i++)
            {
                var current = ranges[i];
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
}
