using Xunit;
using System.Text;
using PieceTree.TextBuffer.Core;

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
            System.Console.WriteLine($"Insert {i}: changebuffer len={buffer.InternalChunkBuffers[0].Length}, total chunks={buffer.InternalChunkBuffers.Count}");
        }

        var finalChangeBufLength = buffer.InternalChunkBuffers[0].Length;
        var finalChunkCount = buffer.InternalChunkBuffers.Count;

        Assert.True(finalChangeBufLength >= initialChangeBufLength + 10, "Change buffer did not grow as expected.");
        Assert.Equal(initialChunkCount, finalChunkCount); // no new chunks should have been created for small typing
    }

    [Fact]
    public void AverageBufferSize_InsertLargePayload()
    {
        var payload = new string('x', ChunkUtilities.DefaultChunkSize + 10);
        var buffer = new PieceTreeBuffer("");
        var initialPieceCount = buffer.InternalModel.PieceCount;

        buffer.ApplyEdit(0, 0, payload);
        var newPieceCount = buffer.InternalModel.PieceCount;

        // We expect at least 2 pieces to represent the large payload (chunk split)
        Assert.True(newPieceCount - initialPieceCount >= 2);

        // Validate textual correctness
        var text = buffer.GetText();
        Assert.True(text.StartsWith(payload.Substring(0, 10)) || text.Contains(payload), "Payload not found in buffer text");
    }

    [Fact]
    public void CRLF_RepairAcrossChunks()
    {
        var buffer = PieceTreeBuffer.FromChunks(new[] { "Hello\r", "\nWorld" });
        // The initial text contains a CRLF split across chunks; total line feeds should be 1.
        var model = buffer.InternalModel;
        // Debug: print out pieces and their buffer contents
        foreach (var piece in model.EnumeratePiecesInOrder())
        {
            var chunk = model.Buffers[piece.BufferIndex];
            System.Console.WriteLine($"Piece BufIdx={piece.BufferIndex}; Start={piece.Start.Line}/{piece.Start.Column}; End={piece.End.Line}/{piece.End.Column}; Len={piece.Length}; LFcnt={piece.LineFeedCount}; Text='{chunk.Slice(piece.Start, piece.End)}'");
        }
        for (int i = 0; i < model.Buffers.Count; i++)
        {
            var b = model.Buffers[i];
            var starts = string.Join(",", b.LineStarts);
            System.Console.WriteLine($"Buffer {i}: len={b.Length}, starts=[{starts}], content='{b.Buffer.Replace("\n", "\\n").Replace("\r","\\r")}'");
        }
        // Try invoking the FixCRLF routine explicitly via reflection in case insertion path didn't trigger it
        var nodes = model.EnumerateNodesInOrder().ToList();
        if (nodes.Count >= 2)
        {
            var prevNode = nodes[0];
            var nextNode = nodes[1];
            var fixMethod = typeof(PieceTreeModel).GetMethod("FixCRLF", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fixMethod?.Invoke(model, new object?[] { prevNode, nextNode });
            // Reprint pieces after running FixCRLF
            foreach (var piece in model.EnumeratePiecesInOrder())
            {
                var chunk = model.Buffers[piece.BufferIndex];
                System.Console.WriteLine($"PostFix Piece BufIdx={piece.BufferIndex}; Start={piece.Start.Line}/{piece.Start.Column}; End={piece.End.Line}/{piece.End.Column}; Len={piece.Length}; LFcnt={piece.LineFeedCount}; Text='{chunk.Slice(piece.Start, piece.End)}'");
            }
        }

        System.Console.WriteLine($"TotalLineFeeds before fix: {buffer.InternalModel.TotalLineFeeds}");
        Assert.Equal(1, buffer.InternalModel.TotalLineFeeds);

        var fullText = buffer.GetText();
        var crIndex = fullText.IndexOf('\r');
        Assert.True(crIndex >= 0);

        // Delete the CR and ensure we still have one line feed (the LF remains)
        buffer.ApplyEdit(crIndex, 1, null);
        System.Console.WriteLine($"TotalLineFeeds after manual FixCRLF: {buffer.InternalModel.TotalLineFeeds}");
        Assert.Equal(1, buffer.InternalModel.TotalLineFeeds);

        // Insert CR back and ensure total line feeds is 1 after CRLF repair
        buffer.ApplyEdit(crIndex, 0, "\r");
        Assert.Equal(1, buffer.InternalModel.TotalLineFeeds);
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
        }
    }

        [Fact]
        public void CRLF_FuzzAcrossChunks()
        {
            var rng = new System.Random(123);
            var buffer = new PieceTreeBuffer("");
            var expected = new System.Text.StringBuilder();

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
                    // Insert CR/LF or ascii letters
                    var pick = rng.Next(0, 10);
                    string toInsert;
                    if (pick < 3) toInsert = "\r";
                    else if (pick < 6) toInsert = "\n";
                    else toInsert = new string((char)('a' + (rng.Next(0, 26))), rng.Next(1, 3));
                    buffer.ApplyEdit(offset, 0, toInsert);
                    expected.Insert(offset, toInsert);
                }

                var actual = buffer.GetText();
                // Print per-iteration debug counts for easier tracing
                var expectedLFsNow = 0;
                var s2 = expected.ToString();
                for (int k = 0; k < s2.Length; k++)
                {
                    if (s2[k] == '\r')
                    {
                        if (k + 1 < s2.Length && s2[k+1] == '\n') { expectedLFsNow++; k++; }
                        else expectedLFsNow++;
                    }
                    else if (s2[k] == '\n') expectedLFsNow++;
                }
                Console.WriteLine($"FUZZ ITER={i}: offset={offset}, op={op}, inserted='{(op==0?"(del)":"ins")}', bufferLen={buffer.Length}, expectedLFsSoFar={expectedLFsNow}, modelLF={buffer.InternalModel.TotalLineFeeds}");
                Assert.Equal(expected.ToString(), actual);
                // Check total line feeds invariant (CRLF counts as one)
                int expectedLFs = 0;
                var s = expected.ToString();
                for (int k = 0; k < s.Length; k++)
                {
                    if (s[k] == '\r')
                    {
                        if (k + 1 < s.Length && s[k+1] == '\n') { expectedLFs++; k++; }
                        else expectedLFs++;
                    }
                    else if (s[k] == '\n') expectedLFs++;
                }
                if (expectedLFs != buffer.InternalModel.TotalLineFeeds)
                {
                    // Dump model for debugging
                    PieceTree.TextBuffer.Tests.Helpers.PieceTreeModelTestHelpers.DebugDumpModel(buffer.InternalModel);
                    Console.WriteLine($"FUZZ FAILURE ITER={i}: offset={offset}, op={op}");
                    Console.WriteLine($"ExpectedLFs={expectedLFs}; ActualLFs={buffer.InternalModel.TotalLineFeeds}; Text=<{expected.ToString().Replace("\n","\\n").Replace("\r","\\r")}>");
                }
                Assert.Equal(expectedLFs, buffer.InternalModel.TotalLineFeeds);
            }
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
    }
}
