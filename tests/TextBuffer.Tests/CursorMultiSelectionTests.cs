// Source: ts/src/vs/editor/contrib/multicursor/test/browser/cursorMultiSelection.test.ts
// - Tests: multi-cursor rendering, paste ordering, cancellation semantics
// Ported/updated: 2025-11-27
// Updated: 2025-11-28 (CL7-Stage1 Phase 3: Updated to use CursorContext-based API)

using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Cursor;
using PieceTree.TextBuffer.Rendering;
using PieceTree.TextBuffer.Tests.Helpers;

namespace PieceTree.TextBuffer.Tests;

public class CursorMultiSelectionTests
{
    [Fact]
    public void MarkdownRenderer_RendersMultipleCursorsSnapshot()
    {
        TestEditorContext context = TestEditorBuilder.Create()
            .WithMarkedContent("|alpha beta|\ngamma |delta|")
            .BuildContext();

        using CursorCollection collection = CreateCollection(context);
        MarkdownRenderer renderer = new();

        string output = renderer.Render(context.Model);
        SnapshotTestUtils.AssertMatchesSnapshot("Cursor", "multicursor-render-basic", output);
    }

    [Fact]
    public void MultiCursorPaste_PreservesInsertionOrder()
    {
        TestEditorContext context = TestEditorBuilder.Create()
            .WithLines("abc", "def")
            .WithCursor(1, 1)
            .WithCursor(2, 1)
            .BuildContext();

        using CursorCollection collection = CreateCollection(context);
        CursorTestHelper.AssertMultiCursors(collection.GetCursorPositions(), (1, 1), (2, 1));

        Cursor.Cursor top = collection.Cursors[0];
        Cursor.Cursor bottom = collection.Cursors[1];

        TextEdit[] edits =
        [
            new TextEdit(top.Selection.Active, top.Selection.Active, "1"),
            new TextEdit(bottom.Selection.Active, bottom.Selection.Active, "2"),
        ];

        IReadOnlyList<TextPosition> before = collection.GetCursorPositions();
        context.Model.PushEditOperations(edits, beforeCursorState: before, cursorStateComputer: _ => before);

        Assert.Equal("1abc", context.GetLineContent(1));
        Assert.Equal("2def", context.GetLineContent(2));
    }

    [Fact]
    public void CancellingSecondaryCursorsPreservesPrimarySelection()
    {
        TestEditorContext context = TestEditorBuilder.Create()
            .WithLines("var x = (3 * 5)", "var y = (3 * 5)", "var z = (3 * 5)")
            .WithCursor(2, 9)
            .BuildContext();

        using CursorCollection collection = CreateCollection(context);
        Cursor.Cursor primary = collection.Cursors[0];
        primary.SelectTo(new TextPosition(2, 16));
        CursorTestHelper.AssertSelection(primary.Selection, 2, 9, 2, 16);

        Cursor.Cursor secondaryAbove = collection.CreateCursor(new TextPosition(1, 9));
        secondaryAbove.SelectTo(new TextPosition(1, 16));
        Cursor.Cursor secondaryBelow = collection.CreateCursor(new TextPosition(3, 9));
        secondaryBelow.SelectTo(new TextPosition(3, 16));

        CursorTestHelper.AssertMultiCursors(collection.GetCursorPositions(), (2, 16), (1, 16), (3, 16));
        List<Selection> selections = collection.Cursors.Select(cursor => cursor.Selection).ToList();
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
        // Use CursorContext-based constructor which creates a primary cursor automatically
        CursorContext cursorContext = CursorContext.FromModel(context.Model);
        CursorCollection collection = new(cursorContext);

        // Move primary cursor to first position if specified
        if (context.InitialCursors.Count > 0)
        {
            collection.Cursors[0].MoveTo(context.InitialCursors[0]);

            // Add remaining positions as secondary cursors
            for (int i = 1; i < context.InitialCursors.Count; i++)
            {
                collection.CreateCursor(context.InitialCursors[i]);
            }
        }

        return collection;
    }
}
