// Source: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// - Tests: Core PieceTree buffer operations, chunk handling, position mapping
// Ported: 2025-11-22

using System.Linq;
using System.Text;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeBufferTests
{
    [Fact]
    public void InitializesWithProvidedText()
    {
        PieceTreeBuffer buffer = new("hello");
        Assert.Equal(5, buffer.Length);
        Assert.Equal("hello", buffer.GetText());
    }

    [Fact]
    public void LargeBufferRoundTripsContent()
    {
        string largeText = new string('x', 16_384);
        PieceTreeBuffer buffer = new(largeText);

        Assert.Equal(largeText.Length, buffer.Length);
        Assert.Equal(largeText, buffer.GetText());
    }

    [Fact]
    public void AppliesSimpleEdit()
    {
        PieceTreeBuffer buffer = new("hello world");
        buffer.ApplyEdit(6, 5, "piece tree");

        Assert.Equal("hello piece tree", buffer.GetText());
        Assert.Equal("hello piece tree".Length, buffer.Length);
    }

    [Fact]
    public void FromChunksBuildsPieceTreeAcrossMultipleBuffers()
    {
        PieceTreeBuffer buffer = PieceTreeBuffer.FromChunks(new[] { "abc", string.Empty, "123\r\n", "xyz" });
        string expected = "abc123\r\nxyz";

        Assert.Equal(expected, buffer.GetText());
        Assert.Equal(expected.Length, buffer.Length);
    }

    [Fact]
    public void PieceTreeModelTracksLineFeedsAcrossChunks()
    {
        PieceTreeBuildResult build = PieceTreeBuilder.BuildFromChunks(new[]
        {
            "line1\nline2\r\n",
            "tail"
        });

        Assert.Equal(2, build.Model.TotalLineFeeds);
        Assert.Equal("line1\nline2\r\ntail".Length, build.Model.TotalLength);
    }

    [Fact]
    public void ApplyEditHandlesCrLfSequences()
    {
        PieceTreeBuffer buffer = PieceTreeBuffer.FromChunks(new[] { "foo\r\nbar" });
        buffer.ApplyEdit(5, 3, "piece\r\ntree");

        string expected = "foo\r\npiece\r\ntree";
        Assert.Equal(expected, buffer.GetText());
        Assert.Equal(expected.Length, buffer.Length);
    }

    [Fact]
    public void ApplyEditAcrossChunkBoundarySpansMultiplePieces()
    {
        PieceTreeBuffer buffer = PieceTreeBuffer.FromChunks(new[] { "abcd", "ef", "ghij" });
        buffer.ApplyEdit(3, 4, "XYZ");

        Assert.Equal("abcXYZhij", buffer.GetText());
        Assert.Equal("abcXYZhij".Length, buffer.Length);
    }

    [Fact]
    public void PositionLookupMatchesTsPrefixSumExpectations()
    {
        const string text = "line1\nline2\r\nline3";
        PieceTreeBuffer buffer = new(text);

        TextPosition pos0 = buffer.GetPositionAt(0);
        Assert.Equal(new TextPosition(1, 1), pos0);

        TextPosition posAfterLine1 = buffer.GetPositionAt("line1\n".Length);
        Assert.Equal(new TextPosition(2, 1), posAfterLine1);

        TextPosition posWithinLine3 = buffer.GetPositionAt(text.Length - 1);
        Assert.Equal(new TextPosition(3, 5), posWithinLine3);

        Assert.Equal(0, buffer.GetOffsetAt(1, 1));
        Assert.Equal("line1\n".Length, buffer.GetOffsetAt(2, 1));
        Assert.Equal(text.Length, buffer.GetOffsetAt(3, buffer.GetLineLength(3) + 1));
    }

    [Fact]
    public void LineCharCodeFollowsCrlfBoundaries()
    {
        PieceTreeBuffer buffer = PieceTreeBuffer.FromChunks(new[] { "foo\r\nbar", "\nend" });

        Assert.Equal('f', buffer.GetLineCharCode(1, 0));
        Assert.Equal('b', buffer.GetLineCharCode(2, 0));
        Assert.Equal('e', buffer.GetLineCharCode(3, 0));

        Assert.Equal(3, buffer.GetLineLength(1));
        Assert.Equal(3, buffer.GetLineLength(2));
        Assert.Equal(3, buffer.GetLineLength(3));
    }

    [Fact]
    public void CharCodeClampedWithinDocument()
    {
        PieceTreeBuffer buffer = new("abc");
        Assert.Equal('a', buffer.GetCharCode(0));
        Assert.Equal('c', buffer.GetCharCode(2));
        Assert.Equal('c', buffer.GetCharCode(100));
    }

    [Fact]
    public void DeleteAcrossCrlfRepairsBoundary()
    {
        PieceTreeBuildResult build = PieceTreeBuilder.BuildFromChunks(new[] { "foo\r", "\nbar" });
        PieceTreeModel model = build.Model;

        model.Delete(0, 3);

        string snapshot = ReadModelText(model);
        Assert.Equal("\r\nbar", snapshot);
        Assert.Equal(snapshot.Length, model.TotalLength);
        Assert.Equal(snapshot.Count(c => c == '\n'), model.TotalLineFeeds);
    }

    [Fact]
    public void MetadataRecomputesAfterMultiLineDelete()
    {
        const string text = "line1\r\nline2\r\nline3";
        PieceTreeBuildResult build = PieceTreeBuilder.BuildFromChunks(new[] { text });
        PieceTreeModel model = build.Model;

        model.Delete("line1\r\n".Length, "line2\r\n".Length);

        string snapshot = ReadModelText(model);
        Assert.Equal("line1\r\nline3", snapshot);
        Assert.Equal(snapshot.Length, model.TotalLength);
        Assert.Equal(snapshot.Count(c => c == '\n'), model.TotalLineFeeds);
    }

    [Fact]
    public void PieceCountTracksTreeMutations()
    {
        PieceTreeBuildResult build = PieceTreeBuilder.BuildFromChunks(new[] { "abc", "def" });
        PieceTreeModel model = build.Model;

        Assert.Equal(model.EnumeratePiecesInOrder().Count(), model.PieceCount);

        model.Insert(3, "XYZ");
        model.Delete(0, 2);

        Assert.Equal(model.EnumeratePiecesInOrder().Count(), model.PieceCount);
    }

    [Fact]
    public void SearchCacheDropsDetachedNodes()
    {
        PieceTreeBuildResult build = PieceTreeBuilder.BuildFromChunks(new[] { "abc", "def" });
        PieceTreeModel model = build.Model;

        NodeHit primed = model.NodeAt(0);
        Assert.Equal("abc", ReadPieceText(model, primed.Node));

        model.Delete(0, 3);

        NodeHit hit = model.NodeAt(0);
        Assert.Equal("def", ReadPieceText(model, hit.Node));
    }

    // TODO(PT-005.S8): Blocked on Porter-CS (PT-004.G2) exposing EnumeratePieces to assert piece-level layout & chunk reuse.
    // TODO(PT-005.S9): Blocked on Investigator-TS finalizing BufferRange/SearchContext mapping before property-based edit fuzzing can begin.
    // TODO(PT-005.S10): Planned sequential delete+insert coverage to assert metadata after back-to-back ApplyEdit calls.

    private static string ReadModelText(PieceTreeModel model)
    {
        IReadOnlyList<ChunkBuffer> buffers = model.Buffers;
        StringBuilder builder = new();

        foreach (PieceSegment piece in model.EnumeratePiecesInOrder())
        {
            if (piece.Length == 0)
            {
                continue;
            }

            ChunkBuffer buffer = buffers[piece.BufferIndex];
            builder.Append(buffer.Slice(piece.Start, piece.End));
        }

        return builder.ToString();
    }

    private static string ReadPieceText(PieceTreeModel model, PieceTreeNode node)
    {
        if (node is null || ReferenceEquals(node, model.Sentinel) || node.Piece.Length == 0)
        {
            return string.Empty;
        }

        ChunkBuffer buffer = model.Buffers[node.Piece.BufferIndex];
        return buffer.Slice(node.Piece.Start, node.Piece.End);
    }
}
