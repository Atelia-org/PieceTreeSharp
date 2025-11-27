// Source: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// - Tests: Builder chunk splitting, BOM/metadata retention, CRLF handling (Lines: 1500+)
// Ported: 2025-11-19

using System;
using System.Collections.Generic;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using Xunit;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeBuilderTests
{
    [Fact]
    public void AcceptChunk_SplitsLargeInputIntoDefaultSizedPieces()
    {
        PieceTreeBuilder builder = new();
        int chunkSize = ChunkUtilities.DefaultChunkSize;
        string text = new string('a', chunkSize * 2);
        builder.AcceptChunk(text);

        PieceTreeBuilderOptions options = PieceTreeBuilderOptions.Default with { NormalizeEol = false };
        PieceTreeBuildResult result = builder.Finish(options).Create(DefaultEndOfLine.LF);
        Assert.Equal(text.Length, result.Model.TotalLength);
        Assert.Equal(2, result.Buffers.Count - 1); // subtract sentinel buffer
        Assert.Equal(2, result.Model.PieceCount);

        string reconstructed = PieceTreeTestHelpers.ReconstructText(result);
        Assert.Equal(text, reconstructed);
    }

    [Fact]
    public void AcceptChunk_RetainsBomAndMetadataFlags()
    {
        PieceTreeBuilder builder = new();
        string chunk = "\uFEFFאבג\u2028";
        builder.AcceptChunk(chunk);

        PieceTreeBuilderOptions options = PieceTreeBuilderOptions.Default with { NormalizeEol = false };
        PieceTreeBuildResult result = builder.Finish(options).Create(DefaultEndOfLine.LF);
        Assert.Equal("\uFEFF", result.Bom);
        Assert.True(result.MightContainRtl);
        Assert.True(result.MightContainUnusualLineTerminators);
        Assert.True(result.MightContainNonBasicAscii);
    }

    [Fact]
    public void AcceptChunk_CarriesTrailingCarriageReturn()
    {
        PieceTreeBuilder builder = new();
        builder.AcceptChunk("hello\r");
        builder.AcceptChunk("\nworld");

        PieceTreeBuilderOptions options = PieceTreeBuilderOptions.Default with { NormalizeEol = false };
        PieceTreeBuildResult result = builder.Finish(options).Create(DefaultEndOfLine.LF);
        string text = PieceTreeTestHelpers.ReconstructText(result);
        Assert.Equal("hello\r\nworld", text);
    }

    [Fact]
    public void CreateNewPieces_SplitsLargeInsert()
    {
        List<ChunkBuffer> buffers = [ChunkBuffer.Empty];
        PieceTreeModel model = new(buffers);
        string insert = new string('x', (ChunkUtilities.DefaultChunkSize * 2) + 10);

        model.Insert(0, insert);

        // +1 to skip sentinel buffer
        Assert.Equal(3, model.Buffers.Count - 1);
        Assert.Equal(insert.Length, model.TotalLength);
    }
}
