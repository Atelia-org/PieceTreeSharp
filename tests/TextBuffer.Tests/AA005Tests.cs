// Source: Custom C# test implementation
// Purpose: AA-005 CRLF splitting validation tests
// Created: 2025-11-21

using System;
using System.Text;
using PieceTree.TextBuffer.Core;
using Xunit;

namespace PieceTree.TextBuffer.Tests;

public class AA005Tests
{
    [Fact]
    public void TestSplitCRLF()
    {
        PieceTreeModel model = CreateModel("");

        // Insert "A\r"
        model.Insert(0, "A\r");
        PieceTree.TextBuffer.Tests.Helpers.PieceTreeModelTestHelpers.DebugDumpModel(model);
        Assert.Equal(2, model.TotalLineFeeds + 1); // 1 LF means 2 lines. "A\r" has 1 LF (if \r is break).

        // Insert "\nB" at end
        model.Insert(2, "\nB");

        // "A\r\nB"
        // Should be 2 lines: "A" and "B".
        // TotalLineFeeds should be 1.

        Assert.Equal(1, model.TotalLineFeeds);
        Assert.Equal("A\r\nB", GetText(model));

        // Check line content
        // Line 1: "A" (stripped EOL) or "A\r\n" (raw)
        // GetLineRawContent returns raw content including EOL for the line.
        // Line 1: "A\r\n"
        // Line 2: "B"

        Assert.Equal("A\r\n", model.GetLineRawContent(1));
        Assert.Equal("B", model.GetLineRawContent(2));
    }

    [Fact]
    public void TestSplitCRLF_InsertMiddle()
    {
        PieceTreeModel model = CreateModel("AB");
        // "AB"

        // Insert "\r" between A and B
        model.Insert(1, "\r");
        // "A\rB" -> 2 lines
        Assert.Equal(1, model.TotalLineFeeds);

        // Insert "\n" after "\r"
        model.Insert(2, "\n");
        // "A\r\nB" -> 2 lines (1 LF)

        Assert.Equal(1, model.TotalLineFeeds);
        Assert.Equal("A\r\n", model.GetLineRawContent(1));
    }

    [Fact]
    public void TestCacheInvalidation()
    {
        PieceTreeModel model = CreateModel("Line1\nLine2\nLine3");

        // Prime cache
        string line2 = model.GetLineRawContent(2);
        Assert.Equal("Line2\n", line2);

        // Insert at Line 1. Should invalidate cache.
        model.Insert(0, "Prefix");

        // Check Line 2 again. It should be "Line2\n" but at different offset?
        // No, we inserted at 0. So "PrefixLine1\nLine2\nLine3".
        // Line 2 is still "Line2\n".

        string line2_new = model.GetLineRawContent(2);
        Assert.Equal("Line2\n", line2_new);

        // Insert newline at start.
        model.Insert(0, "\n");
        // "\nPrefixLine1\nLine2\nLine3"
        // Line 1: "\n"
        // Line 2: "PrefixLine1\n"
        // Line 3: "Line2\n"

        Assert.Equal("Line2\n", model.GetLineRawContent(3));
    }

    private PieceTreeModel CreateModel(string text)
    {
        PieceTreeBuilder builder = new();
        builder.AcceptChunk(text);
        PieceTreeBuilderOptions options = PieceTreeBuilderOptions.Default with { NormalizeEol = false };
        PieceTreeBuildResult buildResult = builder.Finish(options).Create(options.DefaultEndOfLine);
        return buildResult.Model;
    }

    private string GetText(PieceTreeModel model)
    {
        StringBuilder sb = new();
        foreach (PieceSegment piece in model.EnumeratePiecesInOrder())
        {
            ChunkBuffer buffer = model.Buffers[piece.BufferIndex];
            sb.Append(buffer.Slice(piece.Start, piece.End));
        }
        return sb.ToString();
    }
}
