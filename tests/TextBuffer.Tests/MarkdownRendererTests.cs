// Original C# implementation
// Purpose: Tests for MarkdownRenderer - visual debugging output for editor state
// - Validates cursor, selection, and decoration rendering in text format
// Created: 2025-11-22

using System;
using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.Rendering;

namespace PieceTree.TextBuffer.Tests;

public class MarkdownRendererTests
{
    [Fact]
    public void TestRender_Cursor()
    {
        TextModel model = new("Hello World");
        MarkdownRenderer renderer = new();

        // Cursor at 1, 7 ('W')
        TextPosition pos = new(1, 7);
        int offset = model.GetOffsetAt(pos);
        model.AddDecoration(new TextRange(offset, offset), ModelDecorationOptions.CreateCursorOptions());

        string output = renderer.Render(model);

        string expected =
@"```text
Hello |World
```";
        Assert.Equal(expected, output.Trim());
    }

    [Fact]
    public void TestRender_Selection()
    {
        TextModel model = new("Hello World");
        MarkdownRenderer renderer = new();

        // Selection "World" (1, 7) to (1, 12)
        TextPosition startPos = new(1, 7);
        TextPosition endPos = new(1, 12);
        int startOffset = model.GetOffsetAt(startPos);
        int endOffset = model.GetOffsetAt(endPos);

        model.AddDecoration(new TextRange(startOffset, endOffset), ModelDecorationOptions.CreateSelectionOptions());

        string output = renderer.Render(model);

        string expected =
@"```text
Hello [World]
```";
        Assert.Equal(expected, output.Trim());
    }

    [Fact]
    public void TestRender_MultiLine_Cursor()
    {
        TextModel model = new("Line1\nLine2");
        MarkdownRenderer renderer = new();

        // Cursor at start of Line 2 (2, 1)
        TextPosition pos = new(2, 1);
        int offset = model.GetOffsetAt(pos);
        model.AddDecoration(new TextRange(offset, offset), ModelDecorationOptions.CreateCursorOptions());

        string output = renderer.Render(model);

        string expected =
@"```text
Line1
|Line2
```";
        Assert.Equal(expected, output.Trim());
    }

    [Fact]
    public void TestRender_MultiLine_Selection()
    {
        TextModel model = new("Line1\nLine2");
        MarkdownRenderer renderer = new();

        // Selection from start of Line 1 to end of Line 2
        TextPosition startPos = new(1, 1);
        TextPosition endPos = new(2, 6); // "Line2" is 5 chars. 2,6 is end.
        int startOffset = model.GetOffsetAt(startPos);
        int endOffset = model.GetOffsetAt(endPos);

        model.AddDecoration(new TextRange(startOffset, endOffset), ModelDecorationOptions.CreateSelectionOptions());

        string output = renderer.Render(model);

        string expected =
@"```text
[Line1
Line2]
```";
        Assert.Equal(expected, output.Trim());
    }

    [Fact]
    public void TestRender_SearchHighlights()
    {
        TextModel model = new("Hello World");
        MarkdownRenderer renderer = new();
        model.HighlightSearchMatches(new SearchHighlightOptions { Query = "World" });

        string output = renderer.Render(model);
        string expected =
@"```text
Hello <World>
```";
        Assert.Equal(expected, output.Trim());
    }

    [Fact]
    public void TestRender_SearchHighlightsRespectOwnerFilter()
    {
        TextModel model = new("Hello World");
        MarkdownRenderer renderer = new();
        model.HighlightSearchMatches(new SearchHighlightOptions { Query = "World" });

        string searchOnly = renderer.Render(model, new MarkdownRenderOptions
        {
            OwnerIdFilter = DecorationOwnerIds.SearchHighlights,
        });
        Assert.Contains("<World>", searchOnly);

        int unrelatedOwner = model.AllocateDecorationOwnerId();
        string filtered = renderer.Render(model, new MarkdownRenderOptions
        {
            OwnerIdFilter = unrelatedOwner,
        });
        Assert.DoesNotContain("<World>", filtered);
    }

    [Fact]
    public void TestRender_OwnerFilter()
    {
        TextModel model = new("Hello World");
        MarkdownRenderer renderer = new();

        int cursorOffset = model.GetOffsetAt(new TextPosition(1, 6));
        model.AddDecoration(new TextRange(cursorOffset, cursorOffset), ModelDecorationOptions.CreateCursorOptions());

        int selectionOwner = model.AllocateDecorationOwnerId();
        model.DeltaDecorations(selectionOwner, null, new[]
        {
            new ModelDeltaDecoration(new TextRange(0, 5), ModelDecorationOptions.CreateSelectionOptions()),
        });

        int otherOwner = model.AllocateDecorationOwnerId();
        model.DeltaDecorations(otherOwner, null, new[]
        {
            new ModelDeltaDecoration(new TextRange(6, 11), new ModelDecorationOptions
            {
                Description = "other",
                RenderKind = DecorationRenderKind.Generic,
                ShowIfCollapsed = true,
            }),
        });

        string filtered = renderer.Render(model, new MarkdownRenderOptions { OwnerIdFilter = selectionOwner });

        Assert.Contains("|", filtered); // Global cursor decorations remain visible
        Assert.Contains("[Hello", filtered);
        Assert.DoesNotContain("[[other]]", filtered);
    }

    [Fact]
    public void TestRender_OwnerFilterList()
    {
        TextModel model = new("Hello World");
        MarkdownRenderer renderer = new();

        int cursorOffset = model.GetOffsetAt(new TextPosition(1, 1));
        model.AddDecoration(new TextRange(cursorOffset, cursorOffset), ModelDecorationOptions.CreateCursorOptions());

        int selectionOwner = model.AllocateDecorationOwnerId();
        model.DeltaDecorations(selectionOwner, null, new[]
        {
            new ModelDeltaDecoration(new TextRange(0, model.GetLength()), ModelDecorationOptions.CreateSelectionOptions()),
        });

        int otherOwner = model.AllocateDecorationOwnerId();
        model.DeltaDecorations(otherOwner, null, new[]
        {
            new ModelDeltaDecoration(new TextRange(0, 1), new ModelDecorationOptions
            {
                Description = "other",
                RenderKind = DecorationRenderKind.Generic,
                ShowIfCollapsed = true,
            }),
        });

        string filtered = renderer.Render(model, new MarkdownRenderOptions
        {
            OwnerIdFilters = new[] { selectionOwner },
        });

        string filteredWithoutCursors = filtered.Replace("|", string.Empty, StringComparison.Ordinal);
        Assert.Contains("[Hello World]", filteredWithoutCursors);
        Assert.Contains("|", filtered);
        Assert.DoesNotContain("[[other]]", filtered);
    }

    [Fact]
    public void TestRender_OwnerFilterPredicateRestrictsOwners()
    {
        TextModel model = new("Hello World");
        MarkdownRenderer renderer = new();

        int selectionOwner = model.AllocateDecorationOwnerId();
        int cursorOwner = model.AllocateDecorationOwnerId();

        model.AddDecoration(new TextRange(0, model.GetLength()), ModelDecorationOptions.CreateSelectionOptions(), selectionOwner);
        int cursorOffset = model.GetOffsetAt(new TextPosition(1, 6));
        model.AddDecoration(new TextRange(cursorOffset, cursorOffset), ModelDecorationOptions.CreateCursorOptions(), cursorOwner);

        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            OwnerFilterPredicate = ownerId => ownerId == cursorOwner,
        });

        Assert.DoesNotContain("[Hello World]", output);
        Assert.Contains("|", output);
    }

    [Fact]
    public void TestRender_OwnerFilterPredicateCanExcludeGlobalDecorations()
    {
        TextModel model = new("Hello World");
        MarkdownRenderer renderer = new();

        int selectionOwner = model.AllocateDecorationOwnerId();

        model.AddDecoration(new TextRange(0, model.GetLength()), ModelDecorationOptions.CreateSelectionOptions(), selectionOwner);
        int cursorOffset = model.GetOffsetAt(new TextPosition(1, 6));
        model.AddDecoration(new TextRange(cursorOffset, cursorOffset), ModelDecorationOptions.CreateCursorOptions());

        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            OwnerFilterPredicate = ownerId => ownerId == selectionOwner,
        });

        Assert.Contains("[Hello World]", output);
        Assert.DoesNotContain("|", output);
    }

    [Fact]
    public void TestRender_OwnerFilterPredicateConflictsWithListThrows()
    {
        TextModel model = new("Hello World");
        MarkdownRenderer renderer = new();
        int ownerId = model.AllocateDecorationOwnerId();

        Assert.Throws<ArgumentException>(() => renderer.Render(model, new MarkdownRenderOptions
        {
            OwnerIdFilters = new[] { ownerId },
            OwnerFilterPredicate = _ => true,
        }));
    }

    [Fact]
    public void TestRender_IncludesInjectedText()
    {
        TextModel model = new("Hello Earth");
        MarkdownRenderer renderer = new();

        int wordStart = model.GetOffsetAt(new TextPosition(1, 7));
        int wordEnd = model.GetOffsetAt(new TextPosition(1, 12));
        ModelDecorationOptions options = new()
        {
            RenderKind = DecorationRenderKind.Generic,
            Before = new ModelDecorationInjectedTextOptions { Content = "BEF" },
            After = new ModelDecorationInjectedTextOptions { Content = "AFT" },
            ShowIfCollapsed = true,
        };

        model.AddDecoration(new TextRange(wordStart, wordEnd), options);

        string output = renderer.Render(model);

        Assert.Contains("<<before:BEF>>", output);
        Assert.Contains("<<after:AFT>>", output);
    }

    [Fact]
    public void TestRender_SuppressesInjectedTextWhenDisabled()
    {
        TextModel model = new("Hello Earth");
        MarkdownRenderer renderer = new();

        int wordStart = model.GetOffsetAt(new TextPosition(1, 7));
        int wordEnd = model.GetOffsetAt(new TextPosition(1, 12));
        ModelDecorationOptions options = new()
        {
            RenderKind = DecorationRenderKind.Generic,
            Before = new ModelDecorationInjectedTextOptions { Content = "BEF" },
            After = new ModelDecorationInjectedTextOptions { Content = "AFT" },
            ShowIfCollapsed = true,
        };

        model.AddDecoration(new TextRange(wordStart, wordEnd), options);

        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            IncludeInjectedText = false,
        });

        Assert.DoesNotContain("<<before:BEF>>", output);
        Assert.DoesNotContain("<<after:AFT>>", output);
    }

    [Fact]
    public void TestRender_RendersGlyphAndMinimapAnnotations()
    {
        TextModel model = new("abc");
        MarkdownRenderer renderer = new();

        model.AddDecoration(new TextRange(0, model.GetLength()), CreateAnnotationDecorationOptions());

        string output = renderer.Render(model);

        Assert.Contains("{glyph:git-add@left!}", output);
        Assert.Contains("{margin:margin-add}", output);
        Assert.Contains("{lines:line-add}", output);
        Assert.Contains("{line-number:line-number-add}", output);
        Assert.Contains("{inline:inline-add}", output);
        Assert.Contains("{decor:diff-add}", output);
        Assert.Contains("{minimap:gutter:#00ff00#Add!underlined}", output);
        Assert.Contains("{overview:right:#00ff00}", output);
        Assert.Contains("{font:family=Fira Code,style=italic}", output);
        Assert.Contains("{line-height:24}", output);
    }

    [Fact]
    public void TestRender_DisableGlyphAnnotations()
    {
        TextModel model = new("abc");
        MarkdownRenderer renderer = new();
        model.AddDecoration(new TextRange(0, model.GetLength()), CreateAnnotationDecorationOptions());

        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            IncludeGlyphAnnotations = false,
        });

        Assert.DoesNotContain("{glyph:git-add@left!}", output);
        Assert.Contains("{margin:margin-add}", output);
    }

    [Fact]
    public void TestRender_DisableMarginAnnotations()
    {
        TextModel model = new("abc");
        MarkdownRenderer renderer = new();
        model.AddDecoration(new TextRange(0, model.GetLength()), CreateAnnotationDecorationOptions());

        string output = renderer.Render(model, new MarkdownRenderOptions
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
        TextModel model = new("abc");
        MarkdownRenderer renderer = new();
        model.AddDecoration(new TextRange(0, model.GetLength()), CreateAnnotationDecorationOptions());

        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            IncludeOverviewAnnotations = false,
        });

        Assert.DoesNotContain("{overview:right:#00ff00}", output);
        Assert.Contains("{minimap:gutter:#00ff00#Add!underlined}", output);
    }

    [Fact]
    public void TestRender_DisableMinimapAnnotations()
    {
        TextModel model = new("abc");
        MarkdownRenderer renderer = new();
        model.AddDecoration(new TextRange(0, model.GetLength()), CreateAnnotationDecorationOptions());

        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            IncludeMinimapAnnotations = false,
        });

        Assert.DoesNotContain("{minimap:gutter:#00ff00#Add!underlined}", output);
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
                SectionHeaderStyle = MinimapSectionHeaderStyle.Underlined,
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
        TextModel model = new("foo\nbar");
        MarkdownRenderer renderer = new();

        model.AddDecoration(new TextRange(0, 3), new ModelDecorationOptions
        {
            Description = "diff-add",
            RenderKind = DecorationRenderKind.Generic,
            ShowIfCollapsed = true,
        });

        int insertOffset = model.GetOffsetAt(new TextPosition(2, 2));
        model.AddDecoration(new TextRange(insertOffset, insertOffset), new ModelDecorationOptions
        {
            Description = "diff-insert",
            RenderKind = DecorationRenderKind.Generic,
            ShowIfCollapsed = true,
            ZIndex = 5,
        });

        string output = renderer.Render(model);

        Assert.Contains("[[diff-add]]foo[[/diff-add]]", output);
        Assert.Contains("[[diff-insert]]", output);
        Assert.Contains("[[/diff-insert]]", output);
    }

    [Fact]
    public void TestRender_ViewportRestrictsLines()
    {
        TextModel model = new("one\ntwo\nthree\nfour");
        MarkdownRenderer renderer = new();

        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            StartLineNumber = 2,
            LineCount = 2,
        });

        string expected =
@"```text
two
three
```";
        Assert.Equal(expected, output.Trim());
    }

    [Fact]
    public void TestRender_ViewportOutsideDocumentProducesEmptyBlock()
    {
        TextModel model = new("one\ntwo");
        MarkdownRenderer renderer = new();

        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            StartLineNumber = 10,
            EndLineNumber = 12,
        });

        string expected =
@"```text
```";
        Assert.Equal(expected, output.Trim());
    }
    
    #region Phase 3: FindDecorations Integration Tests
    
    [Fact]
    public void TestRender_WithFindDecorations_UsesCachedMatches()
    {
        // Arrange
        TextModel model = new("hello world, hello there");
        MarkdownRenderer renderer = new();
        
        // Create FindDecorations and set matches
        using var findDecorations = new PieceTree.TextBuffer.DocUI.FindDecorations(model);
        PieceTree.TextBuffer.Core.FindMatch[] matches =
        [
            new(new PieceTree.TextBuffer.Core.Range(1, 1, 1, 6), null),  // "hello" at start
            new(new PieceTree.TextBuffer.Core.Range(1, 14, 1, 19), null), // "hello" second occurrence
        ];
        findDecorations.Set(matches, null);
        
        // Act
        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            FindDecorations = findDecorations,
            UseDirectFindDecorations = true,
        });
        
        // Assert - should have search markers from FindDecorations
        Assert.Contains("<", output);
        Assert.Contains(">", output);
    }
    
    [Fact]
    public void TestRender_WithFindDecorations_CurrentMatchHighlight()
    {
        // Arrange
        TextModel model = new("hello world, hello there");
        MarkdownRenderer renderer = new();
        
        using var findDecorations = new PieceTree.TextBuffer.DocUI.FindDecorations(model);
        PieceTree.TextBuffer.Core.FindMatch[] matches =
        [
            new(new PieceTree.TextBuffer.Core.Range(1, 1, 1, 6), null),
            new(new PieceTree.TextBuffer.Core.Range(1, 14, 1, 19), null),
        ];
        findDecorations.Set(matches, null);
        
        // Set current match to first occurrence
        var currentMatch = new PieceTree.TextBuffer.Core.Range(1, 1, 1, 6);
        findDecorations.SetCurrentMatch(currentMatch);
        
        // Act
        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            FindDecorations = findDecorations,
            UseDirectFindDecorations = true,
        });
        
        // Assert - when FindDecorations provides cached data, search markers are added
        // The model also renders its decorations, so we see both
        Assert.Contains("<", output);  // Search marker from FindDecorations cache
        Assert.Contains(">", output);  // Search marker from FindDecorations cache
        Assert.Contains("hello", output);
    }
    
    [Fact]
    public void TestRender_WithoutFindDecorations_QueriesModelDirectly()
    {
        // Arrange
        TextModel model = new("test content");
        MarkdownRenderer renderer = new();
        
        // Add a search decoration directly to model
        model.AddDecoration(new TextRange(0, 4), ModelDecorationOptions.CreateSearchMatchOptions());
        
        // Act - render without FindDecorations
        string output = renderer.Render(model);
        
        // Assert - should still show search markers from model
        Assert.Contains("<test>", output);
    }
    
    [Fact]
    public void TestRender_WithFindDecorations_Disabled()
    {
        // Arrange
        TextModel model = new("hello world");
        MarkdownRenderer renderer = new();
        
        using var findDecorations = new PieceTree.TextBuffer.DocUI.FindDecorations(model);
        PieceTree.TextBuffer.Core.FindMatch[] matches =
        [
            new(new PieceTree.TextBuffer.Core.Range(1, 1, 1, 6), null),
        ];
        findDecorations.Set(matches, null);
        
        // Act - render with FindDecorations but disabled
        // When disabled, we rely on model's decorations only
        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            FindDecorations = findDecorations,
            UseDirectFindDecorations = false,
        });
        
        // Assert - model decorations still render as search markers
        // even when the cached path is disabled
        Assert.Contains("<hello>", output);
        Assert.DoesNotContain("[hello]", output);
    }
    
    [Fact]
    public void TestRender_WithFindDecorations_BackwardCompatibility()
    {
        // Test that rendering works the same way when FindDecorations is null
        TextModel model = new("hello world");
        MarkdownRenderer renderer = new();
        
        // Add search decoration directly
        model.AddDecoration(new TextRange(0, 5), ModelDecorationOptions.CreateSearchMatchOptions());
        
        // Render with null FindDecorations (backward compatible path)
        string outputWithNull = renderer.Render(model, new MarkdownRenderOptions
        {
            FindDecorations = null,
        });
        
        // Render without options at all
        string outputWithoutOptions = renderer.Render(model);
        
        // Both should produce same output
        Assert.Equal(outputWithoutOptions, outputWithNull);
    }
    
    [Fact]
    public void TestRender_WithFindDecorations_SuppressesSelectionMarkers()
    {
        TextModel model = new("hello world, hello again");
        MarkdownRenderer renderer = new();
        using var findDecorations = new PieceTree.TextBuffer.DocUI.FindDecorations(model);
        PieceTree.TextBuffer.Core.FindMatch[] matches =
        [
            new(new PieceTree.TextBuffer.Core.Range(1, 1, 1, 6), null),
            new(new PieceTree.TextBuffer.Core.Range(1, 14, 1, 19), null),
        ];
        findDecorations.Set(matches, null);
        findDecorations.SetCurrentMatch(matches[0].Range);
        
        string output = renderer.Render(model, new MarkdownRenderOptions
        {
            FindDecorations = findDecorations,
            UseDirectFindDecorations = true,
        });
        
        Assert.Contains("<hello", output);
        Assert.DoesNotContain("[hello", output);
    }
    
    [Fact]
    public void TestRender_WithFindDecorations_RespectsOwnerFilterPredicate()
    {
        TextModel model = new("hello world");
        MarkdownRenderer renderer = new();
        using var findDecorations = new PieceTree.TextBuffer.DocUI.FindDecorations(model);
        PieceTree.TextBuffer.Core.FindMatch[] matches =
        [
            new(new PieceTree.TextBuffer.Core.Range(1, 1, 1, 6), null),
        ];
        findDecorations.Set(matches, null);
        
        string filtered = renderer.Render(model, new MarkdownRenderOptions
        {
            FindDecorations = findDecorations,
            UseDirectFindDecorations = true,
            OwnerFilterPredicate = ownerId => ownerId != findDecorations.OwnerId,
        });
        
        Assert.DoesNotContain("<hello", filtered);
    }
    
    #endregion
}
