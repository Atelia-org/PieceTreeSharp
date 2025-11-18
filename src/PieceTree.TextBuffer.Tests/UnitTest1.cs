using PieceTree.TextBuffer;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeBufferTests
{
    [Fact]
    public void InitializesWithProvidedText()
    {
        var buffer = new PieceTreeBuffer("hello");
        Assert.Equal(5, buffer.Length);
        Assert.Equal("hello", buffer.GetText());
    }

    [Fact]
    public void AppliesSimpleEdit()
    {
        var buffer = new PieceTreeBuffer("hello world");
        buffer.ApplyEdit(6, 5, "piece tree");

        Assert.Equal("hello piece tree", buffer.GetText());
    }

    [Fact]
    public void FromChunksConcatenatesInput()
    {
        var buffer = PieceTreeBuffer.FromChunks(new[] { "abc", "def" });
        Assert.Equal("abcdef", buffer.GetText());
    }
}
