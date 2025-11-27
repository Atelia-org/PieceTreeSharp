// Source parity: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// Scenario: PieceTreeBuffer should expose BOM metadata via getBOM().
using PieceTree.TextBuffer;
using Xunit;

namespace PieceTree.TextBuffer.Tests;

public sealed class PieceTreeBufferBomTests
{
    [Fact]
    public void GetBom_ReturnsMarkerWhenInputStartsWithUtf8Bom()
    {
        PieceTreeBuffer buffer = new("\uFEFFHello world");

        Assert.Equal("\uFEFF", buffer.GetBom());
        Assert.Equal("Hello world", buffer.GetText());
    }

    [Fact]
    public void GetBom_TracksBomWhenFirstChunkOnlyContainsBom()
    {
        PieceTreeBuffer buffer = PieceTreeBuffer.FromChunks(new[] { "\uFEFF", "Line 1", "\nLine 2" });

        Assert.Equal("\uFEFF", buffer.GetBom());
        Assert.Equal("Line 1\nLine 2", buffer.GetText());
    }

    [Fact]
    public void GetBom_ReturnsEmptyStringWhenInputHasNoBom()
    {
        PieceTreeBuffer buffer = new("Plain text");

        Assert.Equal(string.Empty, buffer.GetBom());
        Assert.Equal("Plain text", buffer.GetText());
    }
}
