// Source: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// - Tests: basic insert/delete, more inserts, more deletes (Lines: 214-265)
// Ported: 2025-11-19

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

    [Fact]
    public void GetLineContent_Cache_Invalidation_Insert()
    {
        var buffer = new PieceTreeBuffer("Line 1\nLine 2\nLine 3");
        
        // Cache miss -> load
        var line2 = buffer.GetLineContent(2);
        Assert.Equal("Line 2\n", line2);
        
        // Insert in Line 2 (at start of line 2, offset 7)
        buffer.ApplyEdit(7, 0, "Modified "); // "Line 1\nModified Line 2\nLine 3"
        
        // Should get updated content (cache invalidated)
        var line2Modified = buffer.GetLineContent(2);
        Assert.Equal("Modified Line 2\n", line2Modified);
    }

    [Fact]
    public void GetLineContent_Cache_Invalidation_Delete()
    {
        var buffer = new PieceTreeBuffer("Line 1\nLine 2\nLine 3");
        
        // Cache miss -> load
        var line2 = buffer.GetLineContent(2);
        Assert.Equal("Line 2\n", line2);
        
        // Delete from Line 2 (at start of line 2, offset 7, length 5 "Line ")
        buffer.ApplyEdit(7, 5, null); // "Line 1\n2\nLine 3"
        
        // Should get updated content (cache invalidated)
        var line2Modified = buffer.GetLineContent(2);
        Assert.Equal("2\n", line2Modified);
    }
}
