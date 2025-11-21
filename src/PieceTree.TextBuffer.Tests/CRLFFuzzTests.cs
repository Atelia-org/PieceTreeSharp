using System;
using System.Text;
using Xunit;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Tests.Helpers;

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
        buffer.InternalModel.AssertPieceIntegrity();
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

        using var log = new FuzzLogCollector(nameof(CRLF_RandomFuzz_1000));
        for (var i = 0; i < 1000; i++)
        {
            var offset = rng.Next(0, Math.Max(0, buffer.Length + 1));
            var op = rng.Next(0, 10);
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
                // Focus insert candidates on CR/LF mixed with letters
                var choice = rng.Next(0, 5);
                string toInsert;
                if (choice == 0) toInsert = "\r";
                else if (choice == 1) toInsert = "\n";
                else if (choice == 2) toInsert = "\r\n";
                else toInsert = new string((char)('a' + rng.Next(0, 26)), rng.Next(1, 6));
                log.Add($"ins offset={offset} text='{toInsert.Replace("\r", "\\r").Replace("\n", "\\n")}'");
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
                var sb = new StringBuilder();
                sb.AppendLine($"Fuzz failure at iteration: {i}");
                sb.AppendLine($"Expected (len={expected.Length}): '{expected.ToString().Replace("\n", "\\n").Replace("\r", "\\r")}'");
                sb.AppendLine($"Actual   (len={actual.Length}): '{actual.Replace("\n", "\\n").Replace("\r", "\\r")}'");
                sb.AppendLine($"Last offset: {offset}");
                var logPath = log.FlushToFile();
                sb.AppendLine($"Operation log saved to: {logPath}");
                throw new InvalidOperationException(sb.ToString(), ex);
            }

            if ((i & 63) == 0)
            {
                buffer.InternalModel.AssertPieceIntegrity();
            }
        }

        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void CRLF_NewlineInsert_BetweenCrLf()
    {
        var buffer = new PieceTreeBuffer("");
        var expected = new StringBuilder();

        (string kind, int offset, int length, string? text)[] operations =
        [
            ("ins", 0, 0, "vvvv"),
            ("del", 0, 1, null),
            ("ins", 0, 0, "m"),
            ("del", 2, 2, null),
            ("ins", 1, 0, "\r"),
            ("ins", 3, 0, "\r"),
            ("del", 3, 1, null),
            ("ins", 2, 0, "n"),
            ("ins", 2, 0, "bbb"),
            ("ins", 0, 0, "\n"),
            ("ins", 3, 0, "xxx"),
            ("ins", 2, 0, "\r"),
            ("ins", 2, 0, "\r"),
            ("ins", 6, 0, "\r"),
            ("ins", 0, 0, "y"),
            ("del", 11, 1, null),
            ("ins", 5, 0, "b"),
            ("ins", 0, 0, "\r"),
            ("ins", 14, 0, "jjjjj"),
            ("del", 4, 2, null),
            ("ins", 10, 0, "\n"),
            ("ins", 13, 0, "\n"),
            ("del", 16, 3, null),
            ("ins", 8, 0, "\n"),
            ("ins", 4, 0, "e"),
            ("ins", 6, 0, "ggg"),
            ("ins", 3, 0, "\n"),
            ("ins", 1, 0, "\r"),
            ("ins", 14, 0, "\n"),
        ];

        for (var i = 0; i < operations.Length; i++)
        {
            var op = operations[i];
            if (op.kind == "ins")
            {
                buffer.ApplyEdit(op.offset, 0, op.text);
                expected.Insert(op.offset, op.text);
            }
            else
            {
                buffer.ApplyEdit(op.offset, op.length, null);
                var len = Math.Min(op.length, expected.Length - op.offset);
                if (len > 0)
                {
                    expected.Remove(op.offset, len);
                }
            }

            var actual = buffer.GetText();
            var expectedText = expected.ToString();

            Assert.Equal(expectedText, actual);
            buffer.InternalModel.AssertPieceIntegrity();
        }
    }

    [Fact]
    public void CRLF_InsertNewlineInsideExistingPair()
    {
        var buffer = PieceTreeBuffer.FromChunks(new[] { "\r\nxx" });
        buffer.ApplyEdit(1, 0, "\n");
        Assert.Equal("\r\n\nxx", buffer.GetText());
        buffer.InternalModel.AssertPieceIntegrity();
    }

}
