// Source: ts/src/vs/editor/test/common/model/modelDecorations.test.ts
// - Tests: decoration ranges, owner filters, stickiness, delta snapshots
// Ported/updated: 2025-11-27

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.Tests.Helpers;

namespace PieceTree.TextBuffer.Tests;

public class DecorationTests
{
    private const string DefaultText = "My First Line\r\n\t\tMy Second Line\n    Third Line\n\r\n1";

    [Fact]
    public void SingleCharacterDecorationMatchesTsExpectations()
    {
        var model = CreateDefaultModel();
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
        var model = CreateDefaultModel();
        AddDecoration(model, 1, 2, 3, 2, "myType");

        AssertLineDecorations(model, 1, ("myType", 2, model.GetLineMaxColumn(1)));
        AssertLineDecorations(model, 2, ("myType", 1, model.GetLineMaxColumn(2)));
        AssertLineDecorations(model, 3, ("myType", 1, 2));
        AssertLineDecorations(model, 4);
    }

    [Fact]
    public void DeltaDecorationsTrackOwnerScopes()
    {
        var model = TestEditorBuilder.Create().WithContent("alpha beta gamma").Build();
        var owner = model.AllocateDecorationOwnerId();

        var added = model.DeltaDecorations(owner, null, new[]
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
        var model = CreateDefaultModel();
        var owner = model.AllocateDecorationOwnerId();
        var added = model.DeltaDecorations(owner, null, new[]
        {
            SelectionDecoration(model, 1, 2, 3, 2),
        });

        var updated = model.DeltaDecorations(owner, new[] { added[0].Id }, new[]
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
        var model = TestEditorBuilder.Create().WithContent("line1\nline2").Build();
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

        model.AddDecoration(CreateRange(model, 1, 1, 1, 5), options);

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

    [Fact]
    public void DecorationsEmitEventsForLifecycle()
    {
        var model = TestEditorBuilder.Create().WithContent("abc").Build();
        var owner = model.AllocateDecorationOwnerId();
        var notifications = new List<TextModelDecorationsChangedEventArgs>();
        model.OnDidChangeDecorations += (_, args) => notifications.Add(args);

        var added = model.DeltaDecorations(owner, null, new[] { SelectionDecoration(model, 1, 1, 1, 2) });
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
        var model = TestEditorBuilder.Create().WithContent("alpha beta").Build();
        var decoration = AddDecoration(model, 1, 2, 1, 6, "tracked");

        var editPosition = new TextPosition(1, 1);
        model.ApplyEdits(new[] { new TextEdit(editPosition, editPosition, "zz ") });

        var start = model.GetPositionAt(decoration.Range.StartOffset);
        var end = model.GetPositionAt(decoration.Range.EndOffset);
        CursorTestHelper.AssertPosition(start, 1, 5);
        CursorTestHelper.AssertPosition(end, 1, 9);
    }

    [Fact]
    public void CollapseOnReplaceEditShrinksRange()
    {
        var model = TestEditorBuilder.Create().WithContent("function test() { call(); }").Build();
        var options = new ModelDecorationOptions { CollapseOnReplaceEdit = true };
        var decoration = model.AddDecoration(CreateRange(model, 1, 14, 1, 20), options);

        var startPosition = model.GetPositionAt(decoration.Range.StartOffset);
        var endPosition = model.GetPositionAt(decoration.Range.EndOffset);
        var expectedOffset = decoration.Range.StartOffset;

        model.ApplyEdits(new[] { new TextEdit(startPosition, endPosition, "noop();") });

        Assert.True(decoration.Range.IsEmpty);
        Assert.Equal(expectedOffset, decoration.Range.StartOffset);
    }

    [Fact]
    public void StickinessHonorsInsertions()
    {
        var model = TestEditorBuilder.Create().WithContent("abcdefghij").Build();
        var always = model.AddDecoration(CreateRange(model, 1, 3, 1, 5), new ModelDecorationOptions { Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges });
        var never = model.AddDecoration(CreateRange(model, 1, 6, 1, 8), new ModelDecorationOptions { Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges });

        var originalAlwaysEnd = always.Range.EndOffset;
        var originalNeverStart = never.Range.StartOffset;

        var insertAtAlways = model.GetPositionAt(always.Range.StartOffset);
        model.ApplyEdits(new[] { new TextEdit(insertAtAlways, insertAtAlways, "XX") });

        var insertAtNever = model.GetPositionAt(never.Range.StartOffset);
        model.ApplyEdits(new[] { new TextEdit(insertAtNever, insertAtNever, "YY") });

        Assert.True(always.Range.EndOffset > originalAlwaysEnd);
        Assert.True(never.Range.StartOffset > originalNeverStart);
    }

    [Fact]
    public void ForceMoveMarkersOverridesStickinessDefaults()
    {
        const string content = "abcdef";
        const int collapsedOffset = 3;
        const string inserted = "++";

        var withoutForce = TestEditorBuilder.Create().WithContent(content).Build();
        var withForce = TestEditorBuilder.Create().WithContent(content).Build();

        static ModelDecorationOptions CreateCollapsedOptions() => new()
        {
            Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges,
            RenderKind = DecorationRenderKind.Generic,
            ShowIfCollapsed = true,
        };

        var decorationWithoutForce = withoutForce.AddDecoration(new TextRange(collapsedOffset, collapsedOffset), CreateCollapsedOptions());
        var decorationWithForce = withForce.AddDecoration(new TextRange(collapsedOffset, collapsedOffset), CreateCollapsedOptions());

        var editPositionWithoutForce = withoutForce.GetPositionAt(collapsedOffset);
        withoutForce.ApplyEdits(new[] { new TextEdit(editPositionWithoutForce, editPositionWithoutForce, inserted) }, forceMoveMarkers: false);

        var editPositionWithForce = withForce.GetPositionAt(collapsedOffset);
        withForce.ApplyEdits(new[] { new TextEdit(editPositionWithForce, editPositionWithForce, inserted) }, forceMoveMarkers: true);

        Assert.Equal(collapsedOffset, decorationWithoutForce.Range.StartOffset);
        Assert.Equal(collapsedOffset + inserted.Length, decorationWithForce.Range.StartOffset);
    }

    [Fact]
    public void GetAllDecorationsFiltersByOwner()
    {
        var model = TestEditorBuilder.Create().WithContent("alpha beta").Build();
        var ownerA = model.AllocateDecorationOwnerId();
        var ownerB = model.AllocateDecorationOwnerId();

        var decorationA = AddDecoration(model, 1, 1, 1, 3, "ownerA", ownerA);
        var decorationB = AddDecoration(model, 1, 7, 1, 10, "ownerB", ownerB);

        var ownerAResults = model.GetAllDecorations(ownerA);
        Assert.Single(ownerAResults);
        Assert.Equal(decorationA.Id, ownerAResults[0].Id);

        var ids = model.GetDecorationIdsByOwner(ownerB);
        Assert.Single(ids);
        Assert.Equal(decorationB.Id, ids[0]);

        var ownerAPosition = model.GetPositionAt(ownerAResults[0].Range.StartOffset);
        CursorTestHelper.AssertPosition(ownerAPosition, 1, 1);
    }

    [Fact]
    public void DecorationSummaryMatchesSnapshot()
    {
        var model = TestEditorBuilder.Create()
            .WithLines("alpha beta", "gamma delta", "epsilon")
            .Build();
        var owner = model.AllocateDecorationOwnerId();

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

        var dump = DumpDecorations(model, owner);
        SnapshotTestUtils.AssertMatchesSnapshot("Decorations", "summary-basic", dump);
    }

    [Fact]
    public void InjectedTextQueriesSurfaceLineMetadata()
    {
        var model = TestEditorBuilder.Create().WithLines("one", "two", "three").Build();
        IReadOnlyList<int>? injectedLines = null;
        model.OnDidChangeDecorations += (_, args) =>
        {
            if (args.AffectedInjectedTextLines.Count > 0)
            {
                injectedLines = args.AffectedInjectedTextLines;
            }
        };

        var injected = model.AddDecoration(CreateRange(model, 2, 2, 2, 3), new ModelDecorationOptions
        {
            RenderKind = DecorationRenderKind.Generic,
            Before = new ModelDecorationInjectedTextOptions { Content = "PRE" },
            After = new ModelDecorationInjectedTextOptions { Content = "POST" },
            ShowIfCollapsed = true,
        });

        var lineTwoInjected = model.GetInjectedTextInLine(2);
        Assert.Contains(lineTwoInjected, d => d.Id == injected.Id);
        Assert.Empty(model.GetInjectedTextInLine(1));
        Assert.NotNull(injectedLines);
        Assert.Contains(2, injectedLines!);
    }

    // TODO(#delta-2025-11-26-aa4-cl8-markdown): Add Markdown renderer overlay snapshots once DocUI surfaces search decorations without re-running find.

    private static TextModel CreateDefaultModel() => TestEditorBuilder.Create().WithContent(DefaultText).Build();

    private static TextRange CreateRange(TextModel model, int startLine, int startColumn, int endLine, int endColumn)
    {
        var start = model.GetOffsetAt(new TextPosition(startLine, startColumn));
        var end = model.GetOffsetAt(new TextPosition(endLine, endColumn));
        return new TextRange(start, end);
    }

    private static ModelDecoration AddDecoration(TextModel model, int startLine, int startColumn, int endLine, int endColumn, string? className = null, int ownerId = DecorationOwnerIds.Default)
    {
        var range = CreateRange(model, startLine, startColumn, endLine, endColumn);
        var options = new ModelDecorationOptions
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
        var actual = DescribeLineDecorations(model, lineNumber);
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            var e = expected[i];
            var a = actual[i];
            Assert.Equal(e.className, a.ClassName);
            Assert.Equal(e.start, a.StartColumn);
            Assert.Equal(e.end, a.EndColumn);
        }
    }

    private static IReadOnlyList<(string? ClassName, int StartColumn, int EndColumn)> DescribeLineDecorations(TextModel model, int lineNumber, int ownerId = DecorationOwnerIds.Any)
    {
        var maxColumn = model.GetLineMaxColumn(lineNumber);
        var decorations = model.GetLineDecorations(lineNumber, ownerId);
        var list = new List<(string?, int, int)>();
        foreach (var decoration in decorations)
        {
            var startPos = model.GetPositionAt(decoration.Range.StartOffset);
            var endPos = model.GetPositionAt(decoration.Range.EndOffset);
            var start = startPos.LineNumber < lineNumber ? 1 : startPos.Column;
            var end = endPos.LineNumber > lineNumber ? maxColumn : endPos.Column;
            list.Add((decoration.Options.ClassName ?? decoration.Options.Description, start, end));
        }

        return list;
    }

    private static string DumpDecorations(TextModel model, int ownerId = DecorationOwnerIds.Any)
    {
        var decorations = model.GetAllDecorations(ownerId)
            .OrderBy(d => d.OwnerId)
            .ThenBy(d => d.Range.StartOffset)
            .ThenBy(d => d.Id, System.StringComparer.Ordinal)
            .ToList();

        var builder = new StringBuilder();
        for (int i = 0; i < decorations.Count; i++)
        {
            var decoration = decorations[i];
            var start = model.GetPositionAt(decoration.Range.StartOffset);
            var end = model.GetPositionAt(decoration.Range.EndOffset);
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
