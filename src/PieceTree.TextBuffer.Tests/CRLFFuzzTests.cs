using System;
using System.Text;
using Xunit;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests;

public class CRLFFuzzTests
{
    [Fact]
    public void LargeInsert_HugePayload()
    {
        var payload = new string('x', ChunkUtilities.DefaultChunkSize + 100);
        var buffer = new PieceTreeBuffer("");

        var initialPieces = buffer.InternalModel.PieceCount;
        buffer.ApplyEdit(0, 0, payload);
        Assert.True(buffer.InternalModel.PieceCount >= initialPieces + 2);
        Assert.Equal(payload.Length, buffer.Length);
        Assert.Equal(payload, buffer.GetText());
    }

    [Fact]
    public void CRLF_SplitAcrossNodes()
    {
        var buffer = PieceTreeBuffer.FromChunks(new[] { "Hello\r", "\nWorld" });
        // Should normalize to a single CRLF
        Assert.Equal(1, buffer.InternalModel.TotalLineFeeds);
        Assert.Equal("Hello\r\nWorld", buffer.GetText());
    }

    [Fact]
    public void CRLF_RandomFuzz_1000()
    {
        var rng = new Random(123);
        var buffer = new PieceTreeBuffer("");
        var expected = new StringBuilder();

        for (var i = 0; i < 1000; i++)
        {
            var offset = rng.Next(0, Math.Max(0, buffer.Length + 1));
            var op = rng.Next(0, 10);
            if (op == 0 && buffer.Length > 0)
            {
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
                // Focus insert candidates on CR/LF mixed with letters
                var choice = rng.Next(0, 5);
                string toInsert;
                if (choice == 0) toInsert = "\r";
                else if (choice == 1) toInsert = "\n";
                else if (choice == 2) toInsert = "\r\n";
                else toInsert = new string((char)('a' + rng.Next(0, 26)), rng.Next(1, 6));
                buffer.ApplyEdit(offset, 0, toInsert);
                expected.Insert(offset, toInsert);
            }

            var actual = buffer.GetText();
            try
            {
                Assert.Equal(expected.ToString(), actual);
            }
            catch (Exception ex)
            {
                // Log the failing input and rethrow for clearer test failure
                var sb = new StringBuilder();
                sb.AppendLine("Fuzz failure at iteration: " + i);
                sb.AppendLine("Expected (len=" + expected.Length + "): '" + expected.ToString().Replace("\n", "\\n").Replace("\r", "\\r") + "'");
                sb.AppendLine("Actual (len=" + actual.Length + "): '" + actual.Replace("\n", "\\n").Replace("\r", "\\r") + "'");
                sb.AppendLine("Last op offset: " + offset);
                throw new InvalidOperationException(sb.ToString(), ex);
            }
        }
    }
}
