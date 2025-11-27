// Source: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// - Tests: Factory line text retrieval, EOL handling, normalization (Lines: 100+)
// Ported: 2025-11-19

using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeFactoryTests
{
    [Fact]
    public void GetFirstAndLastLineTextHonorLineBreaks()
    {
        PieceTreeBuilder builder = new();
        builder.AcceptChunk("first line\r\nsecond line\nthird");

        PieceTreeTextBufferFactory factory = builder.Finish();
        Assert.Equal("first line", factory.GetFirstLineText(100));
        Assert.Equal("third", factory.GetLastLineText(100));
    }

    [Fact]
    public void CreateUsesDefaultEolWhenTextHasNoTerminators()
    {
        PieceTreeBuilder builder = new();
        builder.AcceptChunk("hello world");
        PieceTreeBuilderOptions options = PieceTreeBuilderOptions.Default with { NormalizeEol = false };
        PieceTreeTextBufferFactory factory = builder.Finish(options);

        PieceTreeBuildResult result = factory.Create(DefaultEndOfLine.CRLF);
        Assert.Equal("\r\n", result.Model.Eol);
    }

    [Fact]
    public void CreateNormalizesMixedLineEndingsWhenRequested()
    {
        PieceTreeBuilder builder = new();
        builder.AcceptChunk("foo\rbar\nbaz\r\n");
        PieceTreeBuilderOptions options = PieceTreeBuilderOptions.Default with { NormalizeEol = true };
        PieceTreeTextBufferFactory factory = builder.Finish(options);

        PieceTreeBuildResult result = factory.Create(DefaultEndOfLine.LF);
        string text = PieceTreeTestHelpers.ReconstructText(result);
        Assert.Equal("foo\r\nbar\r\nbaz\r\n", text);
    }
}
