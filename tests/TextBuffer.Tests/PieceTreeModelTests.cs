// Source: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// - Tests: Change buffer optimization, chunk splitting, search cache invalidation
// Ported: 2025-11-21

using Xunit;
using System.Linq;
using System.Text;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Tests.Helpers;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeModelTests
{
    private static string ReadPieceText(PieceTreeModel model, PieceTreeNode node)
    {
        if (node is null || ReferenceEquals(node, model.Sentinel) || node.Piece.Length == 0)
        {
            return string.Empty;
        }

        ChunkBuffer buffer = model.Buffers[node.Piece.BufferIndex];
        return buffer.Slice(node.Piece.Start, node.Piece.End);
    }
    [Fact]
    public void LastChangeBufferPos_AppendOptimization()
    {
        PieceTreeBuffer buffer = new("");
        int initialChangeBufLength = buffer.InternalChunkBuffers[0].Length;
        int initialChunkCount = buffer.InternalChunkBuffers.Count;

        for (int i = 0; i < 10; i++)
        {
            buffer.ApplyEdit(buffer.Length, 0, "a");
        }

        int finalChangeBufLength = buffer.InternalChunkBuffers[0].Length;
        int finalChunkCount = buffer.InternalChunkBuffers.Count;

        Assert.True(finalChangeBufLength >= initialChangeBufLength + 10, "Change buffer did not grow as expected.");
        Assert.Equal(initialChunkCount, finalChunkCount); // no new chunks should have been created for small typing
        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void AverageBufferSize_InsertLargePayload()
    {
        string payload = new string('x', ChunkUtilities.DefaultChunkSize + 10);
        PieceTreeBuffer buffer = new("");
        int initialPieceCount = buffer.InternalModel.PieceCount;
        int initialChunkCount = buffer.InternalChunkBuffers.Count;

        buffer.ApplyEdit(0, 0, payload);
        int newPieceCount = buffer.InternalModel.PieceCount;
        int newChunkCount = buffer.InternalChunkBuffers.Count;

        // We expect at least 2 pieces to represent the large payload (chunk split)
        Assert.True(newPieceCount - initialPieceCount >= 2);
        Assert.True(newChunkCount > initialChunkCount, "Large payload should allocate a new chunk buffer.");

        // Validate textual correctness
        string text = buffer.GetText();
        Assert.True(text.StartsWith(payload.Substring(0, 10)) || text.Contains(payload), "Payload not found in buffer text");
        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void CRLF_RepairAcrossChunks()
    {
        PieceTreeBuffer buffer = PieceTreeBuffer.FromChunks(new[] { "Hello\r", "\nWorld" });
        buffer.InternalModel.AssertPieceIntegrity();
        Assert.Equal(1, buffer.InternalModel.TotalLineFeeds);
        Assert.Equal("Hello\r\nWorld", buffer.GetText());

        int crIndex = buffer.GetText().IndexOf('\r');
        Assert.NotEqual(-1, crIndex);

        buffer.ApplyEdit(crIndex, 1, null);
        Assert.Equal("Hello\nWorld", buffer.GetText());
        Assert.Equal(1, buffer.InternalModel.TotalLineFeeds);
        buffer.InternalModel.AssertPieceIntegrity();

        buffer.ApplyEdit(crIndex, 0, "\r");
        Assert.Equal("Hello\r\nWorld", buffer.GetText());
        Assert.Equal(1, buffer.InternalModel.TotalLineFeeds);
        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void ChangeBufferFuzzTests()
    {
        Random rng = new(42);
        PieceTreeBuffer buffer = new("");
        StringBuilder expected = new();

        for (int i = 0; i < 200; i++)
        {
            int offset = rng.Next(0, Math.Max(0, buffer.Length + 1));
            int op = rng.Next(0, 20);
            if (op == 0 && buffer.Length > 0)
            {
                // Delete a small span
                int delLen = Math.Min(buffer.Length - offset, rng.Next(1, Math.Min(8, buffer.Length - offset + 1)));
                buffer.ApplyEdit(offset, delLen, null);
                if (offset < expected.Length)
                {
                    int len = Math.Min(delLen, expected.Length - offset);
                    expected.Remove(offset, len);
                }
            }
            else
            {
                string toInsert = new string((char)('a' + rng.Next(0, 26)), rng.Next(1, 6));
                buffer.ApplyEdit(offset, 0, toInsert);
                expected.Insert(offset, toInsert);
            }

            string actual = buffer.GetText();
            Assert.Equal(expected.ToString(), actual);

            if (i % 20 == 0)
            {
                buffer.InternalModel.AssertPieceIntegrity();
            }
        }

        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void CRLF_FuzzAcrossChunks()
    {
        Random rng = new(123);
        PieceTreeBuffer buffer = new("");
        StringBuilder expected = new();
        using FuzzLogCollector log = new(nameof(CRLF_FuzzAcrossChunks));

        for (int i = 0; i < 200; i++)
        {
            int offset = rng.Next(0, Math.Max(0, buffer.Length + 1));
            int op = rng.Next(0, 20);
            if (op == 0 && buffer.Length > 0)
            {
                int delLen = Math.Min(buffer.Length - offset, rng.Next(1, Math.Min(8, buffer.Length - offset + 1)));
                log.Add($"del offset={offset} len={delLen}");
                buffer.ApplyEdit(offset, delLen, null);
                if (offset < expected.Length)
                {
                    int len = Math.Min(delLen, expected.Length - offset);
                    expected.Remove(offset, len);
                }
            }
            else
            {
                int pick = rng.Next(0, 10);
                string toInsert;
                if (pick < 3)
                {
                    toInsert = "\r";
                }
                else if (pick < 6)
                {
                    toInsert = "\n";
                }
                else
                {
                    toInsert = new string((char)('a' + rng.Next(0, 26)), rng.Next(1, 3));
                }

                log.Add($"ins offset={offset} text='{toInsert.Replace("\r", "\\r").Replace("\n", "\\n")}'");
                buffer.ApplyEdit(offset, 0, toInsert);
                expected.Insert(offset, toInsert);
            }

            string actual = buffer.GetText();
            Assert.Equal(expected.ToString(), actual);

            int expectedLFs = 0;
            string snapshot = expected.ToString();
            for (int k = 0; k < snapshot.Length; k++)
            {
                if (snapshot[k] == '\r')
                {
                    if (k + 1 < snapshot.Length && snapshot[k + 1] == '\n')
                    {
                        expectedLFs++;
                        k++;
                    }
                    else
                    {
                        expectedLFs++;
                    }
                }
                else if (snapshot[k] == '\n')
                {
                    expectedLFs++;
                }
            }

            if (expectedLFs != buffer.InternalModel.TotalLineFeeds)
            {
                string logPath = log.FlushToFile();
                string message = $"CRLF fuzz mismatch at iteration {i}: expectedLFs={expectedLFs}, actual={buffer.InternalModel.TotalLineFeeds}. Log: {logPath}";
                PieceTreeModelTestHelpers.DebugDumpModel(buffer.InternalModel);
                throw new InvalidOperationException(message);
            }

            if ((i & 31) == 0)
            {
                buffer.InternalModel.AssertPieceIntegrity();
            }
        }

        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void CRLFRepair_DoesNotLeaveZeroLengthNodes()
    {
        PieceTreeBuffer buffer = new("");
        buffer.ApplyEdit(0, 0, "\r");
        buffer.ApplyEdit(1, 0, "\n");

        List<PieceSegment> pieces = buffer.InternalModel.EnumeratePiecesInOrder().ToList();
        Assert.DoesNotContain(pieces, piece => piece.Length == 0);
        Assert.Equal("\r\n", buffer.GetText());
        Assert.Equal(1, buffer.InternalModel.TotalLineFeeds);
        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void MetadataRebuild_AfterBulkDeleteAndInsert()
    {
        PieceTreeBuffer buffer = new("abc\r\ndef");
        buffer.ApplyEdit(0, buffer.Length, null);
        buffer.InternalModel.AssertPieceIntegrity();
        Assert.Equal(0, buffer.Length);

        buffer.ApplyEdit(0, 0, "xyz\r\n123\n");
        Assert.Equal("xyz\r\n123\n", buffer.GetText());
        Assert.Equal(2, buffer.InternalModel.TotalLineFeeds);
        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void StandaloneCRPieceCountsAsOneLineFeed()
    {
        PieceTreeBuffer buffer = new("");
        buffer.ApplyEdit(0, 0, "\r");
        PieceTreeModel model = buffer.InternalModel;
        Assert.Equal(1, model.TotalLineFeeds);
        List<PieceSegment> pieces = model.EnumeratePiecesInOrder().ToList();
        Assert.Single(pieces);
        PieceSegment p = pieces[0];
        Assert.Equal(1, p.LineFeedCount);
        model.AssertPieceIntegrity();
    }

    [Fact]
    public void SearchCacheInvalidation_Precise()
    {
        PieceTreeModel model = PieceTreeBuilder.BuildFromChunks(new[] { "abc", "def", "ghi" }).Model;
        // Prime cache with first and second node
        NodeHit first = model.NodeAt(0);
        Assert.Equal("abc", ReadPieceText(model, first.Node));
        NodeHit second = model.NodeAt(4); // Offset 4 lies inside second node 'def'
        Assert.Equal("def", ReadPieceText(model, second.Node));

        // Delete the middle chunk at offset 3
        model.Delete(3, 3);

        // First node should still be valid cached & correct
        NodeHit hit = model.NodeAt(0);
        Assert.Equal("abc", ReadPieceText(model, hit.Node));
        // The new node at offset 3 is now the old 3rd element 'ghi'
        NodeHit newHit = model.NodeAt(4);
        Assert.Equal("ghi", ReadPieceText(model, newHit.Node));
        model.AssertPieceIntegrity();
    }
}
