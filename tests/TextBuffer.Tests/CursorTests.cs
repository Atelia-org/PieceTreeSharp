// Source: ts/src/vs/editor/test/common/controller/cursorAtomicMoveOperations.test.ts
// - Tests: Basic cursor movement operations (left, right, up, down, select, sticky column)
// Ported: 2025-11-22

using PieceTree.TextBuffer.Decorations;
using CursorClass = PieceTree.TextBuffer.Cursor.Cursor;

namespace PieceTree.TextBuffer.Tests;

public class CursorTests
{
    [Fact]
    public void TestCursor_InitialState()
    {
        TextModel model = new("Hello");
        CursorClass cursor = new(model);

        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Active);
        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Anchor);
    }

    [Fact]
    public void TestCursor_MoveRight()
    {
        TextModel model = new("Hello\nWorld");
        CursorClass cursor = new(model);

        // 1,1 -> 1,2
        cursor.MoveRight();
        Assert.Equal(new TextPosition(1, 2), cursor.Selection.Active);

        // Move to end of line "Hello" (length 5) -> 1,6
        cursor.MoveTo(new TextPosition(1, 5));
        cursor.MoveRight();
        Assert.Equal(new TextPosition(1, 6), cursor.Selection.Active);

        // Move right from end of line -> 2,1
        cursor.MoveRight();
        Assert.Equal(new TextPosition(2, 1), cursor.Selection.Active);

        // Move right at end of document -> Stay
        cursor.MoveTo(new TextPosition(2, 6)); // "World" length 5 -> 2,6
        cursor.MoveRight();
        Assert.Equal(new TextPosition(2, 6), cursor.Selection.Active);
    }

    [Fact]
    public void TestCursor_MoveLeft()
    {
        TextModel model = new("Hello\nWorld");
        CursorClass cursor = new(model);

        // 1,1 -> Stay
        cursor.MoveLeft();
        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Active);

        // 1,2 -> 1,1
        cursor.MoveTo(new TextPosition(1, 2));
        cursor.MoveLeft();
        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Active);

        // 2,1 -> 1,6 (End of "Hello")
        cursor.MoveTo(new TextPosition(2, 1));
        cursor.MoveLeft();
        Assert.Equal(new TextPosition(1, 6), cursor.Selection.Active);
    }

    [Fact]
    public void TestCursor_MoveDown()
    {
        TextModel model = new("Hello\nWorld");
        CursorClass cursor = new(model);

        // 1,1 -> 2,1
        cursor.MoveDown();
        Assert.Equal(new TextPosition(2, 1), cursor.Selection.Active);

        // 2,1 -> Stay
        cursor.MoveDown();
        Assert.Equal(new TextPosition(2, 1), cursor.Selection.Active);

        // Column clamping
        // "LongLine" (8)
        // "Short" (5)
        model = new TextModel("LongLine\nShort");
        cursor = new CursorClass(model);
        cursor.MoveTo(new TextPosition(1, 8)); // 'e'
        cursor.MoveDown();
        Assert.Equal(new TextPosition(2, 6), cursor.Selection.Active); // End of "Short"
    }

    [Fact]
    public void TestCursor_MoveUp()
    {
        TextModel model = new("Hello\nWorld");
        CursorClass cursor = new(model);

        cursor.MoveTo(new TextPosition(2, 1));
        cursor.MoveUp();
        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Active);

        // 1,1 -> Stay
        cursor.MoveUp();
        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Active);

        // Column clamping
        // "Short" (5)
        // "LongLine" (8)
        model = new TextModel("Short\nLongLine");
        cursor = new CursorClass(model);
        cursor.MoveTo(new TextPosition(2, 8));
        cursor.MoveUp();
        Assert.Equal(new TextPosition(1, 6), cursor.Selection.Active);
    }

    [Fact]
    public void TestCursor_SelectTo()
    {
        TextModel model = new("Hello");
        CursorClass cursor = new(model);

        cursor.SelectTo(new TextPosition(1, 3));
        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Anchor);
        Assert.Equal(new TextPosition(1, 3), cursor.Selection.Active);

        cursor.SelectTo(new TextPosition(1, 5));
        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Anchor);
        Assert.Equal(new TextPosition(1, 5), cursor.Selection.Active);
    }

    [Fact]
    public void TestCursor_StickyColumn()
    {
        // Line 1: "LongLine" (8 chars)
        // Line 2: "Short" (5 chars)
        // Line 3: "LongLineAgain" (12 chars)
        TextModel model = new("LongLine\nShort\nLongLineAgain");
        CursorClass cursor = new(model);

        // Move to end of first line (1, 9)
        cursor.MoveTo(new TextPosition(1, 9));

        // Move down to short line. Should be clamped to (2, 6).
        cursor.MoveDown();
        Assert.Equal(new TextPosition(2, 6), cursor.Selection.Active);

        // Move down to long line. Should recover column 9.
        cursor.MoveDown();
        Assert.Equal(new TextPosition(3, 9), cursor.Selection.Active);

        // Move up to short line. Should be clamped to (2, 6).
        cursor.MoveUp();
        Assert.Equal(new TextPosition(2, 6), cursor.Selection.Active);

        // Move up to first line. Should recover column 9.
        cursor.MoveUp();
        Assert.Equal(new TextPosition(1, 9), cursor.Selection.Active);

        // Move Left resets sticky column
        cursor.MoveLeft(); // (1, 8)
        cursor.MoveDown(); // (2, 6)
        cursor.MoveDown(); // (3, 8) - NOT 9
        Assert.Equal(new TextPosition(3, 8), cursor.Selection.Active);
        cursor.Dispose();
    }

    [Fact]
    public void CursorProducesDecorations()
    {
        TextModel model = new("Hello");
        using CursorClass cursor = new(model);

        IReadOnlyList<ModelDecoration> decorations = model.GetDecorationsInRange(new TextRange(0, model.GetLength()));
        Assert.Single(decorations);
        Assert.Equal(DecorationRenderKind.Cursor, decorations[0].Options.RenderKind);
    }
}
