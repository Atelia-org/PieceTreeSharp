// Source: ts/src/vs/editor/contrib/multicursor/test/browser/cursorMultiSelection.test.ts
// - Tests: multi-cursor rendering, paste ordering, cancellation semantics
// Ported/updated: 2025-11-27

using System.Linq;
using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Cursor;
using PieceTree.TextBuffer.Rendering;
using PieceTree.TextBuffer.Tests.Helpers;

namespace PieceTree.TextBuffer.Tests;

public class CursorMultiSelectionTests
{
    [Fact]
    public void MarkdownRenderer_RendersMultipleCursorsSnapshot()
    {
        var context = TestEditorBuilder.Create()
            .WithMarkedContent("|alpha beta|\ngamma |delta|")
            .BuildContext();

        using var collection = CreateCollection(context);
        var renderer = new MarkdownRenderer();

        var output = renderer.Render(context.Model);
        SnapshotTestUtils.AssertMatchesSnapshot("Cursor", "multicursor-render-basic", output);
    }

    [Fact]
    public void MultiCursorPaste_PreservesInsertionOrder()
    {
        var context = TestEditorBuilder.Create()
            .WithLines("abc", "def")
            .WithCursor(1, 1)
            .WithCursor(2, 1)
            .BuildContext();

        using var collection = CreateCollection(context);
        CursorTestHelper.AssertMultiCursors(collection.GetCursorPositions(), (1, 1), (2, 1));

        var top = collection.Cursors[0];
        var bottom = collection.Cursors[1];

        var edits = new[]
        {
            new TextEdit(top.Selection.Active, top.Selection.Active, "1"),
            new TextEdit(bottom.Selection.Active, bottom.Selection.Active, "2"),
        };

        var before = collection.GetCursorPositions();
        context.Model.PushEditOperations(edits, beforeCursorState: before, cursorStateComputer: _ => before);

        Assert.Equal("1abc", context.GetLineContent(1));
        Assert.Equal("2def", context.GetLineContent(2));
    }

    [Fact]
    public void CancellingSecondaryCursorsPreservesPrimarySelection()
    {
        var context = TestEditorBuilder.Create()
            .WithLines("var x = (3 * 5)", "var y = (3 * 5)", "var z = (3 * 5)")
            .WithCursor(2, 9)
            .BuildContext();

        using var collection = CreateCollection(context);
        var primary = collection.Cursors[0];
        primary.SelectTo(new TextPosition(2, 16));
        CursorTestHelper.AssertSelection(primary.Selection, 2, 9, 2, 16);

        var secondaryAbove = collection.CreateCursor(new TextPosition(1, 9));
        secondaryAbove.SelectTo(new TextPosition(1, 16));
        var secondaryBelow = collection.CreateCursor(new TextPosition(3, 9));
        secondaryBelow.SelectTo(new TextPosition(3, 16));

        CursorTestHelper.AssertMultiCursors(collection.GetCursorPositions(), (2, 16), (1, 16), (3, 16));
        var selections = collection.Cursors.Select(cursor => cursor.Selection).ToList();
        CursorTestHelper.AssertMultiSelections(selections,
            (2, 9, 2, 16),
            (1, 9, 1, 16),
            (3, 9, 3, 16));

        collection.RemoveCursor(secondaryAbove);
        collection.RemoveCursor(secondaryBelow);

        CursorTestHelper.AssertSelection(primary.Selection, 2, 9, 2, 16);
    }

    // TODO(#delta-2025-11-26-aa4-cl7-commands-tests): Add SelectHighlights + AddSelectionToNextFindMatch parity cases when MultiCursorSelectionController is ported.

    private static CursorCollection CreateCollection(TestEditorContext context)
    {
        var collection = new CursorCollection(context.Model);
        if (context.InitialCursors.Count == 0)
        {
            collection.CreateCursor();
        }
        else
        {
            foreach (var cursor in context.InitialCursors)
            {
                collection.CreateCursor(cursor);
            }
        }

        return collection;
    }
}
