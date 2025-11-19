using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeBaseTests
{
    [Fact]
    public void BasicInsertDelete()
    {
        var buffer = new PieceTreeBuffer("This is a document with some text.");
        
        buffer.ApplyEdit(34, 0, "This is some more text to insert at offset 34.");
        Assert.Equal("This is a document with some text.This is some more text to insert at offset 34.", buffer.GetText());
        
        buffer.ApplyEdit(42, 5, null); // Delete 5 chars at 42
        Assert.Equal("This is a document with some text.This is more text to insert at offset 34.", buffer.GetText());
    }

    [Fact]
    public void MoreInserts()
    {
        var buffer = new PieceTreeBuffer("");
        
        buffer.ApplyEdit(0, 0, "AAA");
        Assert.Equal("AAA", buffer.GetText());
        
        buffer.ApplyEdit(0, 0, "BBB");
        Assert.Equal("BBBAAA", buffer.GetText());
        
        buffer.ApplyEdit(6, 0, "CCC");
        Assert.Equal("BBBAAACCC", buffer.GetText());
        
        buffer.ApplyEdit(5, 0, "DDD");
        Assert.Equal("BBBAADDDACCC", buffer.GetText());
    }

    [Fact]
    public void MoreDeletes()
    {
        var buffer = new PieceTreeBuffer("012345678");
        
        buffer.ApplyEdit(8, 1, null);
        Assert.Equal("01234567", buffer.GetText());
        
        buffer.ApplyEdit(0, 1, null);
        Assert.Equal("1234567", buffer.GetText());
        
        buffer.ApplyEdit(5, 1, null);
        Assert.Equal("123457", buffer.GetText());
        
        buffer.ApplyEdit(5, 1, null);
        Assert.Equal("12345", buffer.GetText());
        
        buffer.ApplyEdit(0, 5, null);
        Assert.Equal("", buffer.GetText());
    }
}
