using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Tests
{
    public class DecorationTests
    {
        [Fact]
        public void DeltaDecorationsTrackOwnerScopes()
        {
            var model = new TextModel("alpha beta gamma");
            var owner = model.AllocateDecorationOwnerId();

            var added = model.DeltaDecorations(owner, null, new[]
            {
                new ModelDeltaDecoration(new TextRange(0, 5), ModelDecorationOptions.CreateSelectionOptions()),
                new ModelDeltaDecoration(new TextRange(6, 10), ModelDecorationOptions.CreateSelectionOptions()),
            });

            Assert.Equal(2, added.Count);
            Assert.Equal(2, model.GetDecorationsInRange(new TextRange(0, model.GetLength()), owner).Count);

            model.RemoveAllDecorations(owner);
            Assert.Empty(model.GetDecorationsInRange(new TextRange(0, model.GetLength()), owner));
        }

        [Fact]
        public void CollapseOnReplaceEditShrinksRange()
        {
            var model = new TextModel("function test() { call(); }");
            var options = new ModelDecorationOptions { CollapseOnReplaceEdit = true };
            var decoration = model.AddDecoration(new TextRange(13, 19), options);

            var startPosition = model.GetPositionAt(decoration.Range.StartOffset);
            var endPosition = model.GetPositionAt(decoration.Range.EndOffset);
            var expectedOffset = decoration.Range.StartOffset;

            model.ApplyEdits(new[]
            {
                new TextEdit(startPosition, endPosition, "noop();")
            });

            Assert.True(decoration.Range.IsEmpty);
            Assert.Equal(expectedOffset, decoration.Range.StartOffset);
        }

        [Fact]
        public void StickinessHonorsInsertions()
        {
            var model = new TextModel("abcdefghij");
            var always = model.AddDecoration(new TextRange(2, 4), new ModelDecorationOptions { Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges });
            var never = model.AddDecoration(new TextRange(5, 7), new ModelDecorationOptions { Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges });

            var originalAlwaysEnd = always.Range.EndOffset;
            var originalNeverStart = never.Range.StartOffset;

            // Insert at the leading edge of both decorations
            var insertAtAlways = model.GetPositionAt(always.Range.StartOffset);
            model.ApplyEdits(new[] { new TextEdit(insertAtAlways, insertAtAlways, "XX") });

            var insertAtNever = model.GetPositionAt(never.Range.StartOffset);
            model.ApplyEdits(new[] { new TextEdit(insertAtNever, insertAtNever, "YY") });

            Assert.Equal(2, always.Range.StartOffset);
            Assert.True(always.Range.EndOffset > originalAlwaysEnd);

            Assert.True(never.Range.StartOffset > originalNeverStart);
        }

        [Fact]
        public void DecorationOptionsParityRoundTripsMetadata()
        {
            var model = new TextModel("hello world");
            var options = new ModelDecorationOptions
            {
                Description = "diff-add",
                GlyphMarginClassName = "glyph-add",
                MarginClassName = "margin-add",
                LinesDecorationsClassName = "lines-add",
                LineNumberClassName = "line-number-add",
                InlineClassName = "inline-add",
                OverviewRuler = new ModelDecorationOverviewRulerOptions
                {
                    Color = "#00ff00",
                    Position = OverviewRulerLane.Full,
                },
                Minimap = new ModelDecorationMinimapOptions
                {
                    Color = "#00ff00",
                    Position = MinimapPosition.Gutter,
                    SectionHeaderText = "Add",
                    SectionHeaderStyle = "bold",
                },
                GlyphMargin = new ModelDecorationGlyphMarginOptions
                {
                    Position = GlyphMarginLane.Right,
                    PersistLane = true,
                },
                Before = new ModelDecorationInjectedTextOptions { Content = "BEF" },
                After = new ModelDecorationInjectedTextOptions { Content = "AFT" },
                LineHeight = 21,
                FontFamily = "Fira Code",
                FontSize = "13px",
                FontStyle = "italic",
                FontWeight = "600",
                TextDirection = TextDirection.Rtl,
                RenderKind = DecorationRenderKind.Generic,
                ShowIfCollapsed = true,
            };

            model.AddDecoration(new TextRange(0, 5), options);
            var stored = Assert.Single(model.GetDecorationsInRange(new TextRange(0, model.GetLength())));

            Assert.Equal("glyph-add", stored.Options.GlyphMarginClassName);
            Assert.Equal("margin-add", stored.Options.MarginClassName);
            Assert.Equal("lines-add", stored.Options.LinesDecorationsClassName);
            Assert.Equal("line-number-add", stored.Options.LineNumberClassName);
            Assert.Equal("inline-add", stored.Options.InlineClassName);
            Assert.Equal("#00ff00", stored.Options.OverviewRuler?.Color);
            Assert.Equal(MinimapPosition.Gutter, stored.Options.Minimap?.Position);
            Assert.Equal("BEF", stored.Options.Before?.Content);
            Assert.Equal("AFT", stored.Options.After?.Content);
            Assert.Equal(21, stored.Options.LineHeight);
            Assert.Equal("Fira Code", stored.Options.FontFamily);
            Assert.Equal("13px", stored.Options.FontSize);
            Assert.Equal("italic", stored.Options.FontStyle);
            Assert.Equal("600", stored.Options.FontWeight);
            Assert.Equal(TextDirection.Rtl, stored.Options.TextDirection);
            Assert.True(stored.Options.HasInjectedText);
        }

        [Fact]
        public void DecorationsChangedEventIncludesMetadata()
        {
            var model = new TextModel("line1\nline2");
            TextModelDecorationsChangedEventArgs? captured = null;
            model.OnDidChangeDecorations += (_, args) => captured = args;

            var options = new ModelDecorationOptions
            {
                Minimap = new ModelDecorationMinimapOptions { Color = "#111111" },
                OverviewRuler = new ModelDecorationOverviewRulerOptions { Color = "#222222" },
                GlyphMarginClassName = "glyph-info",
                LineNumberClassName = "line-info",
                LineHeight = 26,
                FontFamily = "Consolas",
                RenderKind = DecorationRenderKind.Generic,
                ShowIfCollapsed = true,
            };

            model.AddDecoration(new TextRange(0, 4), options);

            Assert.NotNull(captured);
            Assert.True(captured!.AffectsMinimap);
            Assert.True(captured.AffectsOverviewRuler);
            Assert.True(captured.AffectsGlyphMargin);
            Assert.True(captured.AffectsLineNumber);
            var heightChange = Assert.Single(captured.AffectedLineHeights);
            Assert.Equal(DecorationOwnerIds.Default, heightChange.OwnerId);
            Assert.Equal(1, heightChange.LineNumber);
            Assert.Equal(26, heightChange.LineHeight);
            var fontChange = Assert.Single(captured.AffectedFontLines);
            Assert.Equal(DecorationOwnerIds.Default, fontChange.OwnerId);
            Assert.Equal(1, fontChange.LineNumber);
        }
    }
}
