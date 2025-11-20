using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using Xunit;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeFactoryTests
{
    [Fact]
    public void GetFirstAndLastLineTextHonorLineBreaks()
    {
        var builder = new PieceTreeBuilder();
        builder.AcceptChunk("first line\r\nsecond line\nthird");

        var factory = builder.Finish();
        Assert.Equal("first line", factory.GetFirstLineText(100));
        Assert.Equal("third", factory.GetLastLineText(100));
    }

    [Fact]
    public void CreateUsesDefaultEolWhenTextHasNoTerminators()
    {
        var builder = new PieceTreeBuilder();
        builder.AcceptChunk("hello world");
        var options = PieceTreeBuilderOptions.Default with { NormalizeEol = false };
        var factory = builder.Finish(options);

        var result = factory.Create(DefaultEndOfLine.CRLF);
        Assert.Equal("\r\n", result.Model.Eol);
    }

    [Fact]
    public void CreateNormalizesMixedLineEndingsWhenRequested()
    {
        var builder = new PieceTreeBuilder();
        builder.AcceptChunk("foo\rbar\nbaz\r\n");
        var options = PieceTreeBuilderOptions.Default with { NormalizeEol = true };
        var factory = builder.Finish(options);

        var result = factory.Create(DefaultEndOfLine.LF);
        var text = PieceTreeTestHelpers.ReconstructText(result);
        Assert.Equal("foo\r\nbar\r\nbaz\r\n", text);
    }
}
