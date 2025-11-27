// Original C# implementation
// Purpose: Tests for MarkdownRenderer - visual debugging output for editor state
// - Validates cursor, selection, and decoration rendering in text format
// Created: 2025-11-22

using System;
using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Rendering;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Tests
{
    public class MarkdownRendererTests
    {
        [Fact]
        public void TestRender_Cursor()
        {
            var model = new TextModel("Hello World");
            var renderer = new MarkdownRenderer();

            // Cursor at 1, 7 ('W')
            var pos = new TextPosition(1, 7);
            var offset = model.GetOffsetAt(pos);
            model.AddDecoration(new TextRange(offset, offset), ModelDecorationOptions.CreateCursorOptions());

            var output = renderer.Render(model);
            
            var expected = 
@"```text
Hello |World
```";
            Assert.Equal(expected, output.Trim());
        }

        [Fact]
        public void TestRender_Selection()
        {
            var model = new TextModel("Hello World");
            var renderer = new MarkdownRenderer();

            // Selection "World" (1, 7) to (1, 12)
            var startPos = new TextPosition(1, 7);
            var endPos = new TextPosition(1, 12);
            var startOffset = model.GetOffsetAt(startPos);
            var endOffset = model.GetOffsetAt(endPos);
            
            model.AddDecoration(new TextRange(startOffset, endOffset), ModelDecorationOptions.CreateSelectionOptions());

            var output = renderer.Render(model);
            
            var expected = 
@"```text
Hello [World]
```";
            Assert.Equal(expected, output.Trim());
        }

        [Fact]
        public void TestRender_MultiLine_Cursor()
        {
            var model = new TextModel("Line1\nLine2");
            var renderer = new MarkdownRenderer();

            // Cursor at start of Line 2 (2, 1)
            var pos = new TextPosition(2, 1);
            var offset = model.GetOffsetAt(pos);
            model.AddDecoration(new TextRange(offset, offset), ModelDecorationOptions.CreateCursorOptions());

            var output = renderer.Render(model);
            
            var expected = 
@"```text
Line1
|Line2
```";
            Assert.Equal(expected, output.Trim());
        }

        [Fact]
        public void TestRender_MultiLine_Selection()
        {
            var model = new TextModel("Line1\nLine2");
            var renderer = new MarkdownRenderer();

            // Selection from start of Line 1 to end of Line 2
            var startPos = new TextPosition(1, 1);
            var endPos = new TextPosition(2, 6); // "Line2" is 5 chars. 2,6 is end.
            var startOffset = model.GetOffsetAt(startPos);
            var endOffset = model.GetOffsetAt(endPos);
            
            model.AddDecoration(new TextRange(startOffset, endOffset), ModelDecorationOptions.CreateSelectionOptions());

            var output = renderer.Render(model);
            
            var expected = 
@"```text
[Line1
Line2]
```";
            Assert.Equal(expected, output.Trim());
        }

        [Fact]
        public void TestRender_SearchHighlights()
        {
            var model = new TextModel("Hello World");
            var renderer = new MarkdownRenderer();
            model.HighlightSearchMatches(new SearchHighlightOptions { Query = "World" });

            var output = renderer.Render(model);
            var expected = 
@"```text
Hello <World>
```";
            Assert.Equal(expected, output.Trim());
        }

        [Fact]
        public void TestRender_SearchHighlightsRespectOwnerFilter()
        {
            var model = new TextModel("Hello World");
            var renderer = new MarkdownRenderer();
            model.HighlightSearchMatches(new SearchHighlightOptions { Query = "World" });

            var searchOnly = renderer.Render(model, new MarkdownRenderOptions
            {
                OwnerIdFilter = DecorationOwnerIds.SearchHighlights,
            });
            Assert.Contains("<World>", searchOnly);

            var unrelatedOwner = model.AllocateDecorationOwnerId();
            var filtered = renderer.Render(model, new MarkdownRenderOptions
            {
                OwnerIdFilter = unrelatedOwner,
            });
            Assert.DoesNotContain("<World>", filtered);
        }

        [Fact]
        public void TestRender_OwnerFilter()
        {
            var model = new TextModel("Hello World");
            var renderer = new MarkdownRenderer();

            var cursorOffset = model.GetOffsetAt(new TextPosition(1, 6));
            model.AddDecoration(new TextRange(cursorOffset, cursorOffset), ModelDecorationOptions.CreateCursorOptions());

            var ownerId = model.AllocateDecorationOwnerId();
            model.DeltaDecorations(ownerId, null, new[]
            {
                new ModelDeltaDecoration(new TextRange(2, 4), ModelDecorationOptions.CreateSelectionOptions()),
            });

            var filtered = renderer.Render(model, new MarkdownRenderOptions { OwnerIdFilter = ownerId });

            Assert.DoesNotContain("|", filtered);
            Assert.Contains("[ll", filtered);
        }

        [Fact]
        public void TestRender_OwnerFilterList()
        {
            var model = new TextModel("Hello World");
            var renderer = new MarkdownRenderer();

            var cursorOffset = model.GetOffsetAt(new TextPosition(1, 1));
            model.AddDecoration(new TextRange(cursorOffset, cursorOffset), ModelDecorationOptions.CreateCursorOptions());

            var selectionOwner = model.AllocateDecorationOwnerId();
            model.DeltaDecorations(selectionOwner, null, new[]
            {
                new ModelDeltaDecoration(new TextRange(0, model.GetLength()), ModelDecorationOptions.CreateSelectionOptions()),
            });

            var filtered = renderer.Render(model, new MarkdownRenderOptions
            {
                OwnerIdFilters = new[] { selectionOwner },
            });

            Assert.Contains("[Hello World]", filtered);
            Assert.DoesNotContain("|", filtered);
        }

        [Fact]
        public void TestRender_OwnerFilterPredicateRestrictsOwners()
        {
            var model = new TextModel("Hello World");
            var renderer = new MarkdownRenderer();

            var selectionOwner = model.AllocateDecorationOwnerId();
            var cursorOwner = model.AllocateDecorationOwnerId();

            model.AddDecoration(new TextRange(0, model.GetLength()), ModelDecorationOptions.CreateSelectionOptions(), selectionOwner);
            var cursorOffset = model.GetOffsetAt(new TextPosition(1, 6));
            model.AddDecoration(new TextRange(cursorOffset, cursorOffset), ModelDecorationOptions.CreateCursorOptions(), cursorOwner);

            var output = renderer.Render(model, new MarkdownRenderOptions
            {
                OwnerFilterPredicate = ownerId => ownerId == cursorOwner || ownerId == DecorationOwnerIds.Default,
            });

            Assert.DoesNotContain("[Hello World]", output);
            Assert.Contains("|", output);
        }

        [Fact]
        public void TestRender_OwnerFilterPredicateConflictsWithListThrows()
        {
            var model = new TextModel("Hello World");
            var renderer = new MarkdownRenderer();
            var ownerId = model.AllocateDecorationOwnerId();

            Assert.Throws<ArgumentException>(() => renderer.Render(model, new MarkdownRenderOptions
            {
                OwnerIdFilters = new[] { ownerId },
                OwnerFilterPredicate = _ => true,
            }));
        }

        [Fact]
        public void TestRender_IncludesInjectedText()
        {
            var model = new TextModel("Hello Earth");
            var renderer = new MarkdownRenderer();

            var wordStart = model.GetOffsetAt(new TextPosition(1, 7));
            var wordEnd = model.GetOffsetAt(new TextPosition(1, 12));
            var options = new ModelDecorationOptions
            {
                RenderKind = DecorationRenderKind.Generic,
                Before = new ModelDecorationInjectedTextOptions { Content = "BEF" },
                After = new ModelDecorationInjectedTextOptions { Content = "AFT" },
                ShowIfCollapsed = true,
            };

            model.AddDecoration(new TextRange(wordStart, wordEnd), options);

            var output = renderer.Render(model);

            Assert.Contains("<<before:BEF>>", output);
            Assert.Contains("<<after:AFT>>", output);
        }

        [Fact]
        public void TestRender_SuppressesInjectedTextWhenDisabled()
        {
            var model = new TextModel("Hello Earth");
            var renderer = new MarkdownRenderer();

            var wordStart = model.GetOffsetAt(new TextPosition(1, 7));
            var wordEnd = model.GetOffsetAt(new TextPosition(1, 12));
            var options = new ModelDecorationOptions
            {
                RenderKind = DecorationRenderKind.Generic,
                Before = new ModelDecorationInjectedTextOptions { Content = "BEF" },
                After = new ModelDecorationInjectedTextOptions { Content = "AFT" },
                ShowIfCollapsed = true,
            };

            model.AddDecoration(new TextRange(wordStart, wordEnd), options);

            var output = renderer.Render(model, new MarkdownRenderOptions
            {
                IncludeInjectedText = false,
            });

            Assert.DoesNotContain("<<before:BEF>>", output);
            Assert.DoesNotContain("<<after:AFT>>", output);
        }

        [Fact]
        public void TestRender_RendersGlyphAndMinimapAnnotations()
        {
            var model = new TextModel("abc");
            var renderer = new MarkdownRenderer();

            model.AddDecoration(new TextRange(0, model.GetLength()), CreateAnnotationDecorationOptions());

            var output = renderer.Render(model);

            Assert.Contains("{glyph:git-add@left!}", output);
            Assert.Contains("{margin:margin-add}", output);
            Assert.Contains("{lines:line-add}", output);
            Assert.Contains("{line-number:line-number-add}", output);
            Assert.Contains("{inline:inline-add}", output);
            Assert.Contains("{decor:diff-add}", output);
            Assert.Contains("{minimap:gutter:#00ff00#Add!solid}", output);
            Assert.Contains("{overview:right:#00ff00}", output);
            Assert.Contains("{font:family=Fira Code,style=italic}", output);
            Assert.Contains("{line-height:24}", output);
        }

        [Fact]
        public void TestRender_DisableGlyphAnnotations()
        {
            var model = new TextModel("abc");
            var renderer = new MarkdownRenderer();
            model.AddDecoration(new TextRange(0, model.GetLength()), CreateAnnotationDecorationOptions());

            var output = renderer.Render(model, new MarkdownRenderOptions
            {
                IncludeGlyphAnnotations = false,
            });

            Assert.DoesNotContain("{glyph:git-add@left!}", output);
            Assert.Contains("{margin:margin-add}", output);
        }

        [Fact]
        public void TestRender_DisableMarginAnnotations()
        {
            var model = new TextModel("abc");
            var renderer = new MarkdownRenderer();
            model.AddDecoration(new TextRange(0, model.GetLength()), CreateAnnotationDecorationOptions());

            var output = renderer.Render(model, new MarkdownRenderOptions
            {
                IncludeMarginAnnotations = false,
            });

            Assert.DoesNotContain("{margin:margin-add}", output);
            Assert.DoesNotContain("{lines:line-add}", output);
            Assert.DoesNotContain("{line-number:line-number-add}", output);
            Assert.Contains("{glyph:git-add@left!}", output);
        }

        [Fact]
        public void TestRender_DisableOverviewAnnotations()
        {
            var model = new TextModel("abc");
            var renderer = new MarkdownRenderer();
            model.AddDecoration(new TextRange(0, model.GetLength()), CreateAnnotationDecorationOptions());

            var output = renderer.Render(model, new MarkdownRenderOptions
            {
                IncludeOverviewAnnotations = false,
            });

            Assert.DoesNotContain("{overview:right:#00ff00}", output);
            Assert.Contains("{minimap:gutter:#00ff00#Add!solid}", output);
        }

        [Fact]
        public void TestRender_DisableMinimapAnnotations()
        {
            var model = new TextModel("abc");
            var renderer = new MarkdownRenderer();
            model.AddDecoration(new TextRange(0, model.GetLength()), CreateAnnotationDecorationOptions());

            var output = renderer.Render(model, new MarkdownRenderOptions
            {
                IncludeMinimapAnnotations = false,
            });

            Assert.DoesNotContain("{minimap:gutter:#00ff00#Add!solid}", output);
            Assert.Contains("{overview:right:#00ff00}", output);
        }

        private static ModelDecorationOptions CreateAnnotationDecorationOptions()
        {
            return new ModelDecorationOptions
            {
                Description = "diff-add",
                RenderKind = DecorationRenderKind.Generic,
                GlyphMarginClassName = "git-add",
                GlyphMargin = new ModelDecorationGlyphMarginOptions
                {
                    Position = GlyphMarginLane.Left,
                    PersistLane = true,
                },
                MarginClassName = "margin-add",
                LinesDecorationsClassName = "line-add",
                LineNumberClassName = "line-number-add",
                OverviewRuler = new ModelDecorationOverviewRulerOptions
                {
                    Color = "#00ff00",
                    Position = OverviewRulerLane.Right,
                },
                Minimap = new ModelDecorationMinimapOptions
                {
                    Color = "#00ff00",
                    Position = MinimapPosition.Gutter,
                    SectionHeaderText = "Add",
                    SectionHeaderStyle = "solid",
                },
                InlineClassName = "inline-add",
                LineHeight = 24,
                FontFamily = "Fira Code",
                FontStyle = "italic",
                ShowIfCollapsed = true,
            };
        }

        [Fact]
        public void TestRender_DiffDecorationsExposeGenericMarkers()
        {
            var model = new TextModel("foo\nbar");
            var renderer = new MarkdownRenderer();

            model.AddDecoration(new TextRange(0, 3), new ModelDecorationOptions
            {
                Description = "diff-add",
                RenderKind = DecorationRenderKind.Generic,
                ShowIfCollapsed = true,
            });

            var insertOffset = model.GetOffsetAt(new TextPosition(2, 2));
            model.AddDecoration(new TextRange(insertOffset, insertOffset), new ModelDecorationOptions
            {
                Description = "diff-insert",
                RenderKind = DecorationRenderKind.Generic,
                ShowIfCollapsed = true,
                ZIndex = 5,
            });

            var output = renderer.Render(model);

            Assert.Contains("[[diff-add]]foo[[/diff-add]]", output);
            Assert.Contains("[[diff-insert]]", output);
            Assert.Contains("[[/diff-insert]]", output);
        }

        [Fact]
        public void TestRender_ViewportRestrictsLines()
        {
            var model = new TextModel("one\ntwo\nthree\nfour");
            var renderer = new MarkdownRenderer();

            var output = renderer.Render(model, new MarkdownRenderOptions
            {
                StartLineNumber = 2,
                LineCount = 2,
            });

            var expected =
@"```text
two
three
```";
            Assert.Equal(expected, output.Trim());
        }

        [Fact]
        public void TestRender_ViewportOutsideDocumentProducesEmptyBlock()
        {
            var model = new TextModel("one\ntwo");
            var renderer = new MarkdownRenderer();

            var output = renderer.Render(model, new MarkdownRenderOptions
            {
                StartLineNumber = 10,
                EndLineNumber = 12,
            });

            var expected =
@"```text
```";
            Assert.Equal(expected, output.Trim());
        }
    }
}
