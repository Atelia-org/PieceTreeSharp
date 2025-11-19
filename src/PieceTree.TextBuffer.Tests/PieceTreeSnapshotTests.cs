using Xunit;
using PieceTree.TextBuffer.Core;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeSnapshotTests
{
    [Fact]
    public void SnapshotReadsContent()
    {
        // Arrange
        var text = "Hello World";
        var buffers = new List<ChunkBuffer> { ChunkBuffer.FromText(text) };
        var model = new PieceTreeModel(buffers);
        model.InsertPieceAtEnd(new PieceSegment(0, new BufferCursor(0, 0), new BufferCursor(0, 11), 0, 11));

        // Act
        var snapshot = model.CreateSnapshot("");
        var content = snapshot.Read();

        // Assert
        Assert.Equal("Hello World", content);
    }

    [Fact]
    public void SnapshotIsImmutable()
    {
        // Arrange
        var text = "Hello";
        var buffers = new List<ChunkBuffer> { ChunkBuffer.FromText(text) };
        var model = new PieceTreeModel(buffers);
        model.InsertPieceAtEnd(new PieceSegment(0, new BufferCursor(0, 0), new BufferCursor(0, 5), 0, 5));

        var snapshot = model.CreateSnapshot("");
        Assert.Equal("Hello", snapshot.Read());

        // Act - Modify the tree
        // Add a new buffer and a new piece
        var text2 = " World";
        buffers.Add(ChunkBuffer.FromText(text2));
        model.InsertPieceAtEnd(new PieceSegment(1, new BufferCursor(0, 0), new BufferCursor(0, 6), 0, 6));

        // Assert
        // Snapshot should still be "Hello"
        Assert.Equal("Hello", snapshot.Read());
        
        // Model should be "Hello World"
        // We can verify model length or content if we had a helper, but TotalLength is enough
        Assert.Equal(11, model.TotalLength);
    }
}
