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
        if (node is null || ReferenceEquals(node, PieceTreeNode.Sentinel) || node.Piece.Length == 0)
        {
            return string.Empty;
        }

        var buffer = model.Buffers[node.Piece.BufferIndex];
        return buffer.Slice(node.Piece.Start, node.Piece.End);
    }
    [Fact]
    public void LastChangeBufferPos_AppendOptimization()
    {
        var buffer = new PieceTreeBuffer("");
        var initialChangeBufLength = buffer.InternalChunkBuffers[0].Length;
        var initialChunkCount = buffer.InternalChunkBuffers.Count;

        for (int i = 0; i < 10; i++)
        {
            buffer.ApplyEdit(buffer.Length, 0, "a");
        }

        var finalChangeBufLength = buffer.InternalChunkBuffers[0].Length;
        var finalChunkCount = buffer.InternalChunkBuffers.Count;

        Assert.True(finalChangeBufLength >= initialChangeBufLength + 10, "Change buffer did not grow as expected.");
        Assert.Equal(initialChunkCount, finalChunkCount); // no new chunks should have been created for small typing
        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void AverageBufferSize_InsertLargePayload()
    {
        var payload = new string('x', ChunkUtilities.DefaultChunkSize + 10);
        var buffer = new PieceTreeBuffer("");
        var initialPieceCount = buffer.InternalModel.PieceCount;
        var initialChunkCount = buffer.InternalChunkBuffers.Count;

        buffer.ApplyEdit(0, 0, payload);
        var newPieceCount = buffer.InternalModel.PieceCount;
        var newChunkCount = buffer.InternalChunkBuffers.Count;

        // We expect at least 2 pieces to represent the large payload (chunk split)
        Assert.True(newPieceCount - initialPieceCount >= 2);
        Assert.True(newChunkCount > initialChunkCount, "Large payload should allocate a new chunk buffer.");

        // Validate textual correctness
        var text = buffer.GetText();
        Assert.True(text.StartsWith(payload.Substring(0, 10)) || text.Contains(payload), "Payload not found in buffer text");
        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void CRLF_RepairAcrossChunks()
    {
        var buffer = PieceTreeBuffer.FromChunks(new[] { "Hello\r", "\nWorld" });
        buffer.InternalModel.AssertPieceIntegrity();
        Assert.Equal(1, buffer.InternalModel.TotalLineFeeds);
        Assert.Equal("Hello\r\nWorld", buffer.GetText());

        var crIndex = buffer.GetText().IndexOf('\r');
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
        var rng = new System.Random(42);
        var buffer = new PieceTreeBuffer("");
        var expected = new StringBuilder();

        for (int i = 0; i < 200; i++)
        {
            var offset = rng.Next(0, Math.Max(0, buffer.Length + 1));
            var op = rng.Next(0, 20);
            if (op == 0 && buffer.Length > 0)
            {
                // Delete a small span
                var delLen = Math.Min(buffer.Length - offset, rng.Next(1, Math.Min(8, buffer.Length - offset + 1)));
                buffer.ApplyEdit(offset, delLen, null);
                if (offset < expected.Length)
                {
                    var len = Math.Min(delLen, expected.Length - offset);
                    expected.Remove(offset, len);
                }
            }
            else
            {
                var toInsert = new string((char)('a' + (rng.Next(0, 26))), rng.Next(1, 6));
                buffer.ApplyEdit(offset, 0, toInsert);
                expected.Insert(offset, toInsert);
            }

            var actual = buffer.GetText();
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
        var rng = new System.Random(123);
        var buffer = new PieceTreeBuffer("");
        var expected = new StringBuilder();
        using var log = new FuzzLogCollector(nameof(CRLF_FuzzAcrossChunks));

        for (int i = 0; i < 200; i++)
        {
            var offset = rng.Next(0, Math.Max(0, buffer.Length + 1));
            var op = rng.Next(0, 20);
            if (op == 0 && buffer.Length > 0)
            {
                var delLen = Math.Min(buffer.Length - offset, rng.Next(1, Math.Min(8, buffer.Length - offset + 1)));
                log.Add($"del offset={offset} len={delLen}");
                buffer.ApplyEdit(offset, delLen, null);
                if (offset < expected.Length)
                {
                    var len = Math.Min(delLen, expected.Length - offset);
                    expected.Remove(offset, len);
                }
            }
            else
            {
                var pick = rng.Next(0, 10);
                string toInsert;
                if (pick < 3) toInsert = "\r";
                else if (pick < 6) toInsert = "\n";
                else toInsert = new string((char)('a' + rng.Next(0, 26)), rng.Next(1, 3));
                log.Add($"ins offset={offset} text='{toInsert.Replace("\r", "\\r").Replace("\n", "\\n")}'");
                buffer.ApplyEdit(offset, 0, toInsert);
                expected.Insert(offset, toInsert);
            }

            var actual = buffer.GetText();
            Assert.Equal(expected.ToString(), actual);

            int expectedLFs = 0;
            var snapshot = expected.ToString();
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
                var logPath = log.FlushToFile();
                var message = $"CRLF fuzz mismatch at iteration {i}: expectedLFs={expectedLFs}, actual={buffer.InternalModel.TotalLineFeeds}. Log: {logPath}";
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
        var buffer = new PieceTreeBuffer("");
        buffer.ApplyEdit(0, 0, "\r");
        buffer.ApplyEdit(1, 0, "\n");

        var pieces = buffer.InternalModel.EnumeratePiecesInOrder().ToList();
        Assert.DoesNotContain(pieces, piece => piece.Length == 0);
        Assert.Equal("\r\n", buffer.GetText());
        Assert.Equal(1, buffer.InternalModel.TotalLineFeeds);
        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void MetadataRebuild_AfterBulkDeleteAndInsert()
    {
        var buffer = new PieceTreeBuffer("abc\r\ndef");
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
        var buffer = new PieceTreeBuffer("");
        buffer.ApplyEdit(0, 0, "\r");
        var model = buffer.InternalModel;
        Assert.Equal(1, model.TotalLineFeeds);
        var pieces = model.EnumeratePiecesInOrder().ToList();
        Assert.Single(pieces);
        var p = pieces[0];
        Assert.Equal(1, p.LineFeedCount);
        model.AssertPieceIntegrity();
    }

    [Fact]
    public void SearchCacheInvalidation_Precise()
    {
        var model = PieceTreeBuilder.BuildFromChunks(new[] { "abc", "def", "ghi" }).Model;
        // Prime cache with first and second node
        var first = model.NodeAt(0);
        Assert.Equal("abc", ReadPieceText(model, first.Node));
        var second = model.NodeAt(4); // Offset 4 lies inside second node 'def'
        Assert.Equal("def", ReadPieceText(model, second.Node));

        // Delete the middle chunk at offset 3
        model.Delete(3, 3);

        // First node should still be valid cached & correct
        var hit = model.NodeAt(0);
        Assert.Equal("abc", ReadPieceText(model, hit.Node));
        // The new node at offset 3 is now the old 3rd element 'ghi'
        var newHit = model.NodeAt(4);
        Assert.Equal("ghi", ReadPieceText(model, newHit.Node));
        model.AssertPieceIntegrity();
    }
}
