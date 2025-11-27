// Source: ts/src/vs/editor/test/common/model/modelDecorations.test.ts
// - Tests: decoration ranges, owner filters, stickiness, delta snapshots
// Ported/updated: 2025-11-27

using System.Linq;
using System.Text;
using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.Tests.Helpers;

namespace PieceTree.TextBuffer.Tests;

public class DecorationTests
{
    private const string DefaultText = "My First Line\r\n\t\tMy Second Line\n    Third Line\n\r\n1";

    [Fact]
    public void SingleCharacterDecorationMatchesTsExpectations()
    {
        TextModel model = CreateDefaultModel();
        AddDecoration(model, 1, 1, 1, 2, "myType");

        AssertLineDecorations(model, 1, ("myType", 1, 2));
        AssertLineDecorations(model, 2);
        AssertLineDecorations(model, 3);
        AssertLineDecorations(model, 4);
        AssertLineDecorations(model, 5);
    }

    [Fact]
    public void MultipleLineDecorationAppearsAcrossLines()
    {
        TextModel model = CreateDefaultModel();
        AddDecoration(model, 1, 2, 3, 2, "myType");

        AssertLineDecorations(model, 1, ("myType", 2, model.GetLineMaxColumn(1)));
        AssertLineDecorations(model, 2, ("myType", 1, model.GetLineMaxColumn(2)));
        AssertLineDecorations(model, 3, ("myType", 1, 2));
        AssertLineDecorations(model, 4);
    }

    [Fact]
    public void DeltaDecorationsTrackOwnerScopes()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("alpha beta gamma").Build();
        int owner = model.AllocateDecorationOwnerId();

        IReadOnlyList<ModelDecoration> added = model.DeltaDecorations(owner, null, new[]
        {
            SelectionDecoration(model, 1, 1, 1, 6),
            SelectionDecoration(model, 1, 7, 1, 11),
        });

        Assert.Equal(2, added.Count);
        Assert.Equal(2, model.GetDecorationsInRange(FullModelRange(model), owner).Count);

        model.RemoveAllDecorations(owner);
        Assert.Empty(model.GetDecorationsInRange(FullModelRange(model), owner));
    }

    [Fact]
    public void DeltaDecorationsCanChangeAndRemove()
    {
        TextModel model = CreateDefaultModel();
        int owner = model.AllocateDecorationOwnerId();
        IReadOnlyList<ModelDecoration> added = model.DeltaDecorations(owner, null, new[]
        {
            SelectionDecoration(model, 1, 2, 3, 2),
        });

        IReadOnlyList<ModelDecoration> updated = model.DeltaDecorations(owner, new[] { added[0].Id }, new[]
        {
            SelectionDecoration(model, 1, 1, 1, 2),
        });

        AssertLineDecorations(model, 1, ("selection", 1, 2));

        model.DeltaDecorations(owner, new[] { updated[0].Id }, null);
        Assert.Empty(model.GetAllDecorations(owner));
    }

    [Fact]
    public void DecorationsChangedEventIncludesMetadata()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("line1\nline2").Build();
        TextModelDecorationsChangedEventArgs? captured = null;
        model.OnDidChangeDecorations += (_, args) => captured = args;

        ModelDecorationOptions options = new()
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

        model.AddDecoration(CreateRange(model, 1, 1, 1, 5), options);

        Assert.NotNull(captured);
        Assert.True(captured!.AffectsMinimap);
        Assert.True(captured.AffectsOverviewRuler);
        Assert.True(captured.AffectsGlyphMargin);
        Assert.True(captured.AffectsLineNumber);
        LineHeightChange heightChange = Assert.Single(captured.AffectedLineHeights);
        Assert.Equal(DecorationOwnerIds.Any, heightChange.OwnerId);
        Assert.Equal(1, heightChange.LineNumber);
        Assert.Equal(26, heightChange.LineHeight);
        LineFontChange fontChange = Assert.Single(captured.AffectedFontLines);
        Assert.Equal(DecorationOwnerIds.Any, fontChange.OwnerId);
        Assert.Equal(1, fontChange.LineNumber);
    }

    [Fact]
    public void DecorationsEmitEventsForLifecycle()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("abc").Build();
        int owner = model.AllocateDecorationOwnerId();
        List<TextModelDecorationsChangedEventArgs> notifications = [];
        model.OnDidChangeDecorations += (_, args) => notifications.Add(args);

        IReadOnlyList<ModelDecoration> added = model.DeltaDecorations(owner, null, new[] { SelectionDecoration(model, 1, 1, 1, 2) });
        Assert.Single(notifications);
        notifications.Clear();

        model.DeltaDecorations(owner, new[] { added[0].Id }, new[] { SelectionDecoration(model, 1, 1, 1, 3) });
        Assert.Single(notifications);
        notifications.Clear();

        model.RemoveAllDecorations(owner);
        Assert.Single(notifications);
    }

    [Fact]
    public void DecorationsMoveWhenTextInsertedBefore()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("alpha beta").Build();
        ModelDecoration decoration = AddDecoration(model, 1, 2, 1, 6, "tracked");

        TextPosition editPosition = new(1, 1);
        model.ApplyEdits([new TextEdit(editPosition, editPosition, "zz ")]);

        TextPosition start = model.GetPositionAt(decoration.Range.StartOffset);
        TextPosition end = model.GetPositionAt(decoration.Range.EndOffset);
        CursorTestHelper.AssertPosition(start, 1, 5);
        CursorTestHelper.AssertPosition(end, 1, 9);
    }

    [Fact]
    public void CollapseOnReplaceEditShrinksRange()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("function test() { call(); }").Build();
        ModelDecorationOptions options = new() { CollapseOnReplaceEdit = true };
        ModelDecoration decoration = model.AddDecoration(CreateRange(model, 1, 14, 1, 20), options);

        TextPosition startPosition = model.GetPositionAt(decoration.Range.StartOffset);
        TextPosition endPosition = model.GetPositionAt(decoration.Range.EndOffset);
        int expectedOffset = decoration.Range.StartOffset;

        model.ApplyEdits([new TextEdit(startPosition, endPosition, "noop();")]);

        Assert.True(decoration.Range.IsEmpty);
        Assert.Equal(expectedOffset, decoration.Range.StartOffset);
    }

    [Fact]
    public void StickinessHonorsInsertions()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("abcdefghij").Build();
        ModelDecoration always = model.AddDecoration(CreateRange(model, 1, 3, 1, 5), new ModelDecorationOptions { Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges });
        ModelDecoration never = model.AddDecoration(CreateRange(model, 1, 6, 1, 8), new ModelDecorationOptions { Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges });

        int originalAlwaysEnd = always.Range.EndOffset;
        int originalNeverStart = never.Range.StartOffset;

        TextPosition insertAtAlways = model.GetPositionAt(always.Range.StartOffset);
        model.ApplyEdits([new TextEdit(insertAtAlways, insertAtAlways, "XX")]);

        TextPosition insertAtNever = model.GetPositionAt(never.Range.StartOffset);
        model.ApplyEdits([new TextEdit(insertAtNever, insertAtNever, "YY")]);

        Assert.True(always.Range.EndOffset > originalAlwaysEnd);
        Assert.True(never.Range.StartOffset > originalNeverStart);
    }

    [Fact]
    public void ForceMoveMarkersOverridesStickinessDefaults()
    {
        const string content = "abcdef";
        const int collapsedOffset = 3;
        const string inserted = "++";

        TextModel withoutForce = TestEditorBuilder.Create().WithContent(content).Build();
        TextModel withForce = TestEditorBuilder.Create().WithContent(content).Build();

        static ModelDecorationOptions CreateCollapsedOptions() => new()
        {
            Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges,
            RenderKind = DecorationRenderKind.Generic,
            ShowIfCollapsed = true,
        };

        ModelDecoration decorationWithoutForce = withoutForce.AddDecoration(new TextRange(collapsedOffset, collapsedOffset), CreateCollapsedOptions());
        ModelDecoration decorationWithForce = withForce.AddDecoration(new TextRange(collapsedOffset, collapsedOffset), CreateCollapsedOptions());

        TextPosition editPositionWithoutForce = withoutForce.GetPositionAt(collapsedOffset);
        withoutForce.ApplyEdits([new TextEdit(editPositionWithoutForce, editPositionWithoutForce, inserted)], forceMoveMarkers: false);

        TextPosition editPositionWithForce = withForce.GetPositionAt(collapsedOffset);
        withForce.ApplyEdits([new TextEdit(editPositionWithForce, editPositionWithForce, inserted)], forceMoveMarkers: true);

        Assert.Equal(collapsedOffset, decorationWithoutForce.Range.StartOffset);
        Assert.Equal(collapsedOffset + inserted.Length, decorationWithForce.Range.StartOffset);
    }

    [Fact]
    public void GetAllDecorationsFiltersByOwner()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("alpha beta").Build();
        int ownerA = model.AllocateDecorationOwnerId();
        int ownerB = model.AllocateDecorationOwnerId();

        ModelDecoration decorationA = AddDecoration(model, 1, 1, 1, 3, "ownerA", ownerA);
        ModelDecoration decorationB = AddDecoration(model, 1, 7, 1, 10, "ownerB", ownerB);

        IReadOnlyList<ModelDecoration> ownerAResults = model.GetAllDecorations(ownerA);
        Assert.Single(ownerAResults);
        Assert.Equal(decorationA.Id, ownerAResults[0].Id);

        IReadOnlyList<string> ids = model.GetDecorationIdsByOwner(ownerB);
        Assert.Single(ids);
        Assert.Equal(decorationB.Id, ids[0]);

        TextPosition ownerAPosition = model.GetPositionAt(ownerAResults[0].Range.StartOffset);
        CursorTestHelper.AssertPosition(ownerAPosition, 1, 1);
    }

    [Fact]
    public void DecorationSearchOptionsFilterTypes()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("alpha beta").Build();
        TextRange firstWord = CreateRange(model, 1, 1, 1, 6);
        TextRange secondWord = CreateRange(model, 1, 7, 1, 11);

        ModelDecoration validation = model.AddDecoration(firstWord, new ModelDecorationOptions
        {
            ClassName = "squiggly-error",
            ShowIfCollapsed = true,
            RenderKind = DecorationRenderKind.Generic,
        });
        ModelDecoration font = model.AddDecoration(secondWord, new ModelDecorationOptions
        {
            FontWeight = "bold",
            ShowIfCollapsed = true,
            RenderKind = DecorationRenderKind.Generic,
        });
        ModelDecoration minimap = model.AddDecoration(firstWord, new ModelDecorationOptions
        {
            Minimap = new ModelDecorationMinimapOptions { Color = "#fff" },
            ShowIfCollapsed = true,
            RenderKind = DecorationRenderKind.Generic,
        });
        ModelDecoration margin = model.AddDecoration(secondWord, new ModelDecorationOptions
        {
            GlyphMarginClassName = "glyph-info",
            ShowIfCollapsed = true,
            RenderKind = DecorationRenderKind.Generic,
        });

        TextRange full = FullModelRange(model);

        IReadOnlyList<ModelDecoration> withoutValidation = model.GetDecorationsInRange(full, new DecorationSearchOptions
        {
            FilterOutValidation = true,
        });
        Assert.DoesNotContain(withoutValidation, d => d.Id == validation.Id);

        IReadOnlyList<ModelDecoration> withoutFont = model.GetDecorationsInRange(full, new DecorationSearchOptions
        {
            FilterFontDecorations = true,
        });
        Assert.DoesNotContain(withoutFont, d => d.Id == font.Id);

        IReadOnlyList<ModelDecoration> onlyMinimap = model.GetDecorationsInRange(full, new DecorationSearchOptions
        {
            OnlyMinimapDecorations = true,
        });
        Assert.Equal(new[] { minimap.Id }, onlyMinimap.Select(d => d.Id).ToArray());

        IReadOnlyList<ModelDecoration> onlyMargin = model.GetDecorationsInRange(full, new DecorationSearchOptions
        {
            OnlyMarginDecorations = true,
        });
        Assert.Equal(new[] { margin.Id }, onlyMargin.Select(d => d.Id).ToArray());
    }

    [Fact]
    public void GetAllDecorationsHonorsFilterFlags()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("alpha beta").Build();
        int owner = model.AllocateDecorationOwnerId();

        ModelDecoration validation = model.AddDecoration(CreateRange(model, 1, 1, 1, 5), new ModelDecorationOptions
        {
            ClassName = "squiggly-warning",
            ShowIfCollapsed = true,
        });

        model.DeltaDecorations(owner, null, new[]
        {
            SelectionDecoration(model, 1, 7, 1, 11),
        });

        IReadOnlyList<ModelDecoration> filtered = model.GetAllDecorations(owner, filterOutValidation: true, filterFontDecorations: false);
        Assert.Single(filtered);
        Assert.DoesNotContain(filtered, d => d.Id == validation.Id);
    }

    [Fact]
    public void GetLineDecorationsRespectsValidationFilter()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("alpha beta").Build();
        model.AddDecoration(CreateRange(model, 1, 1, 1, 5), new ModelDecorationOptions
        {
            ClassName = "squiggly-info",
            ShowIfCollapsed = true,
        });

        IReadOnlyList<ModelDecoration> filtered = model.GetLineDecorations(1, DecorationOwnerIds.Any, filterOutValidation: true, filterFontDecorations: false);
        Assert.Empty(filtered);
    }

    [Fact]
    public void GetAllMarginDecorationsReturnsGlyphOnly()
    {
        TextModel model = TestEditorBuilder.Create().WithContent("alpha beta").Build();
        ModelDecoration glyph = model.AddDecoration(CreateRange(model, 1, 1, 1, 5), new ModelDecorationOptions
        {
            GlyphMarginClassName = "glyph-info",
            ShowIfCollapsed = true,
            RenderKind = DecorationRenderKind.Generic,
        });
        model.AddDecoration(CreateRange(model, 1, 7, 1, 11), new ModelDecorationOptions
        {
            MarginClassName = "margin-info",
            ShowIfCollapsed = true,
            RenderKind = DecorationRenderKind.Generic,
        });

        IReadOnlyList<ModelDecoration> results = model.GetAllMarginDecorations();
        Assert.Contains(results, d => d.Id == glyph.Id);
        Assert.DoesNotContain(results, d => d.Options.MarginClassName == "margin-info");
    }

    [Fact]
    public void DecorationSummaryMatchesSnapshot()
    {
        TextModel model = TestEditorBuilder.Create()
            .WithLines("alpha beta", "gamma delta", "epsilon")
            .Build();
        int owner = model.AllocateDecorationOwnerId();

        model.DeltaDecorations(owner, null, new[]
        {
            SelectionDecoration(model, 1, 1, 1, 6, new ModelDecorationOptions
            {
                Description = "primary",
                Stickiness = TrackedRangeStickiness.GrowsOnlyWhenTypingAfter,
            }),
            SelectionDecoration(model, 2, 1, 2, 6, new ModelDecorationOptions
            {
                Description = "secondary",
                Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
                ShowIfCollapsed = true,
            }),
        });

        string dump = DumpDecorations(model, owner);
        SnapshotTestUtils.AssertMatchesSnapshot("Decorations", "summary-basic", dump);
    }

    [Fact]
    public void InjectedTextQueriesSurfaceLineMetadata()
    {
        TextModel model = TestEditorBuilder.Create().WithLines("one", "two", "three").Build();
        IReadOnlyList<int>? injectedLines = null;
        model.OnDidChangeDecorations += (_, args) =>
        {
            if (args.AffectedInjectedTextLines.Count > 0)
            {
                injectedLines = args.AffectedInjectedTextLines;
            }
        };

        ModelDecoration injected = model.AddDecoration(CreateRange(model, 2, 2, 2, 3), new ModelDecorationOptions
        {
            RenderKind = DecorationRenderKind.Generic,
            Before = new ModelDecorationInjectedTextOptions { Content = "PRE" },
            After = new ModelDecorationInjectedTextOptions { Content = "POST" },
            ShowIfCollapsed = true,
        });

        IReadOnlyList<ModelDecoration> lineTwoInjected = model.GetInjectedTextInLine(2);
        Assert.Contains(lineTwoInjected, d => d.Id == injected.Id);
        Assert.Empty(model.GetInjectedTextInLine(1));
        Assert.NotNull(injectedLines);
        Assert.Contains(2, injectedLines!);
    }

    // TODO(#delta-2025-11-26-aa4-cl8-markdown): Add Markdown renderer overlay snapshots once DocUI surfaces search decorations without re-running find.

    private static TextModel CreateDefaultModel() => TestEditorBuilder.Create().WithContent(DefaultText).Build();

    private static TextRange CreateRange(TextModel model, int startLine, int startColumn, int endLine, int endColumn)
    {
        int start = model.GetOffsetAt(new TextPosition(startLine, startColumn));
        int end = model.GetOffsetAt(new TextPosition(endLine, endColumn));
        return new TextRange(start, end);
    }

    private static ModelDecoration AddDecoration(TextModel model, int startLine, int startColumn, int endLine, int endColumn, string? className = null, int ownerId = DecorationOwnerIds.Any)
    {
        TextRange range = CreateRange(model, startLine, startColumn, endLine, endColumn);
        ModelDecorationOptions options = new()
        {
            Description = className ?? "decor",
            ClassName = className ?? "decor",
            InlineClassName = className,
        };
        return model.AddDecoration(range, options, ownerId);
    }

    private static ModelDeltaDecoration SelectionDecoration(TextModel model, int startLine, int startColumn, int endLine, int endColumn, ModelDecorationOptions? options = null)
        => new(CreateRange(model, startLine, startColumn, endLine, endColumn), options ?? ModelDecorationOptions.CreateSelectionOptions());

    private static TextRange FullModelRange(TextModel model) => new(0, model.GetLength());

    private static void AssertLineDecorations(TextModel model, int lineNumber, params (string className, int start, int end)[] expected)
    {
        IReadOnlyList<(string? ClassName, int StartColumn, int EndColumn)> actual = DescribeLineDecorations(model, lineNumber);
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            (string className, int start, int end) = expected[i];
            (string? ClassName, int StartColumn, int EndColumn) = actual[i];
            Assert.Equal(className, ClassName);
            Assert.Equal(start, StartColumn);
            Assert.Equal(end, EndColumn);
        }
    }

    private static IReadOnlyList<(string? ClassName, int StartColumn, int EndColumn)> DescribeLineDecorations(TextModel model, int lineNumber, int ownerId = DecorationOwnerIds.Any)
    {
        int maxColumn = model.GetLineMaxColumn(lineNumber);
        IReadOnlyList<ModelDecoration> decorations = model.GetLineDecorations(lineNumber, ownerId);
        List<(string?, int, int)> list = [];
        foreach (ModelDecoration decoration in decorations)
        {
            TextPosition startPos = model.GetPositionAt(decoration.Range.StartOffset);
            TextPosition endPos = model.GetPositionAt(decoration.Range.EndOffset);
            int start = startPos.LineNumber < lineNumber ? 1 : startPos.Column;
            int end = endPos.LineNumber > lineNumber ? maxColumn : endPos.Column;
            list.Add((decoration.Options.ClassName ?? decoration.Options.Description, start, end));
        }

        return list;
    }

    private static string DumpDecorations(TextModel model, int ownerId = DecorationOwnerIds.Any)
    {
        List<ModelDecoration> decorations = model.GetAllDecorations(ownerId)
            .OrderBy(d => d.OwnerId)
            .ThenBy(d => d.Range.StartOffset)
            .ThenBy(d => d.Id, System.StringComparer.Ordinal)
            .ToList();

        StringBuilder builder = new();
        for (int i = 0; i < decorations.Count; i++)
        {
            ModelDecoration decoration = decorations[i];
            TextPosition start = model.GetPositionAt(decoration.Range.StartOffset);
            TextPosition end = model.GetPositionAt(decoration.Range.EndOffset);
            builder.Append('#').Append(i + 1)
                .Append(" owner=").Append(decoration.OwnerId)
                .Append(" range=(").Append(start.LineNumber).Append(',').Append(start.Column)
                .Append(")->(").Append(end.LineNumber).Append(',').Append(end.Column).Append(')')
                .Append(" desc=").Append(decoration.Options.Description)
                .Append(" stickiness=").Append(decoration.Options.Stickiness)
                .AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}
