using Xunit;
using PieceTree.TextBuffer;
using CursorClass = PieceTree.TextBuffer.Cursor.Cursor;

namespace PieceTree.TextBuffer.Tests;

public class CursorTests
{
    [Fact]
    public void TestCursor_InitialState()
    {
        var model = new TextModel("Hello");
        var cursor = new CursorClass(model);
        
        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Active);
        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Anchor);
    }

    [Fact]
    public void TestCursor_MoveRight()
    {
        var model = new TextModel("Hello\nWorld");
        var cursor = new CursorClass(model);
        
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
        var model = new TextModel("Hello\nWorld");
        var cursor = new CursorClass(model);
        
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
        var model = new TextModel("Hello\nWorld");
        var cursor = new CursorClass(model);
        
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
        var model = new TextModel("Hello\nWorld");
        var cursor = new CursorClass(model);
        
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
        var model = new TextModel("Hello");
        var cursor = new CursorClass(model);
        
        cursor.SelectTo(new TextPosition(1, 3));
        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Anchor);
        Assert.Equal(new TextPosition(1, 3), cursor.Selection.Active);
        
        cursor.SelectTo(new TextPosition(1, 5));
        Assert.Equal(new TextPosition(1, 1), cursor.Selection.Anchor);
        Assert.Equal(new TextPosition(1, 5), cursor.Selection.Active);
    }
}
