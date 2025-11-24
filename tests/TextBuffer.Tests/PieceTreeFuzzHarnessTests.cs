using System;
using System.Collections.Generic;
using System.Text;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;
using PieceTree.TextBuffer.Tests.Helpers;
using static PieceTree.TextBuffer.Tests.Helpers.PieceTreeScript;
using Xunit;

namespace PieceTree.TextBuffer.Tests;

public sealed class PieceTreeFuzzHarnessTests
{
    private const string TsAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ\r\n";

    private static string CreateTsRandomString(Random random, int len)
    {
        if (len <= 1)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(len - 1);
        for (var i = 1; i < len; i++)
        {
            builder.Append(TsAlphabet[random.Next(TsAlphabet.Length)]);
        }

        return builder.ToString();
    }

    private static List<string> CreateRandomChunks(Random random, int count, int len)
    {
        var chunks = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            chunks.Add(CreateTsRandomString(random, len));
        }

        return chunks;
    }

    private static bool ShouldInsert(Random random)
    {
        return random.NextDouble() < 0.6;
    }

    [Fact]
    public void FuzzHarnessRunsShortDeterministicSequence()
    {
        using var harness = new PieceTreeFuzzHarness(nameof(FuzzHarnessRunsShortDeterministicSequence), initialText: "seed", seedOverride: 314159);
        harness.RunRandomEdits(iterations: 75, maxInsertLength: 8);

        var actual = harness.Buffer.GetText();
        Assert.Equal(harness.ExpectedText, actual);
        Assert.True(harness.GetLineCount() >= 1);

        var fullRange = new Range(TextPosition.Origin, harness.GetPositionAt(harness.Buffer.Length));
        Assert.Equal(harness.ExpectedText, harness.GetValueInRange(fullRange));
    }

    [Fact]
    public void HarnessDetectsExternalCorruption()
    {
        using var harness = new PieceTreeFuzzHarness(nameof(HarnessDetectsExternalCorruption), initialText: "abc", seedOverride: 1337);
        harness.Insert(0, "XYZ");
        harness.Replace(1, 1, "\r\n");
        harness.Delete(0, 1);

        harness.Buffer.ApplyEdit(0, 0, "!");
        var diff = harness.DescribeFirstDifference();
        Assert.True(diff.HasDifference);

        var ex = Assert.Throws<InvalidOperationException>(() => harness.AssertState("manual-corruption"));
        Assert.Contains("PieceTreeFuzzHarness", ex.Message, StringComparison.Ordinal);
        Assert.Contains("seed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RandomTestOneMatchesTsScript()
    {
        // Mirrors ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts random test 1 (lines 271-312).
        using var harness = new PieceTreeFuzzHarness(nameof(RandomTestOneMatchesTsScript), initialText: string.Empty, seedOverride: 1024);
        RunScript(
            harness,
            InsertStep(0, "ceLPHmFzvCtFeHkCBej ", "random-test-1-step-1"),
            InsertStep(8, "gDCEfNYiBUNkSwtvB K ", "random-test-1-step-2"),
            InsertStep(38, "cyNcHxjNPPoehBJldLS ", "random-test-1-step-3"),
            InsertStep(59, "ejMx\nOTgWlbpeDExjOk ", "random-test-1-step-4"));

        harness.AssertState("random-test-1-final");
    }

    [Fact]
    public void RandomTestTwoMatchesTsScript()
    {
        // Mirrors TS random test 2 (pieceTreeTextBuffer.test.ts lines 271-312).
        using var harness = new PieceTreeFuzzHarness(nameof(RandomTestTwoMatchesTsScript), initialText: string.Empty, seedOverride: 2048);
        RunScript(
            harness,
            InsertStep(0, "VgPG ", "random-test-2-step-1"),
            InsertStep(2, "DdWF ", "random-test-2-step-2"),
            InsertStep(0, "hUJc ", "random-test-2-step-3"),
            InsertStep(8, "lQEq ", "random-test-2-step-4"),
            InsertStep(10, "Gbtp ", "random-test-2-step-5"));

        harness.AssertState("random-test-2-final");
    }

    [Fact]
    public void RandomTestThreeMatchesTsScript()
    {
        // Mirrors TS random test 3 sequence (pieceTreeTextBuffer.test.ts lines 300-312).
        using var harness = new PieceTreeFuzzHarness(nameof(RandomTestThreeMatchesTsScript), initialText: string.Empty, seedOverride: 4096);
        RunScript(
            harness,
            InsertStep(0, "gYSz", "random-test-3-step-1"),
            InsertStep(1, "mDQe", "random-test-3-step-2"),
            InsertStep(1, "DTMQ", "random-test-3-step-3"),
            InsertStep(2, "GGZB", "random-test-3-step-4"),
            InsertStep(12, "wXpq", "random-test-3-step-5"));

        harness.AssertState("random-test-3-final");
    }

    [Fact]
    public void RandomDeleteOneMatchesTsScript()
    {
        // Mirrors TS random delete 1 (pieceTreeTextBuffer.test.ts lines 331-360).
        using var harness = new PieceTreeFuzzHarness(nameof(RandomDeleteOneMatchesTsScript), initialText: string.Empty, seedOverride: 8192);
        RunScript(
            harness,
            InsertStep(0, "vfb", "random-delete-1-step-1"),
            InsertStep(0, "zRq", "random-delete-1-step-2"),
            DeleteStep(5, 1, "random-delete-1-step-3"),
            InsertStep(1, "UNw", "random-delete-1-step-4"),
            DeleteStep(4, 3, "random-delete-1-step-5"),
            DeleteStep(1, 4, "random-delete-1-step-6"),
            DeleteStep(0, 1, "random-delete-1-step-7"));

        harness.AssertState("random-delete-1-final");
    }

    [Fact]
    public void RandomDeleteTwoMatchesTsScript()
    {
        // Mirrors TS random delete 2 (pieceTreeTextBuffer.test.ts lines 360-385).
        using var harness = new PieceTreeFuzzHarness(nameof(RandomDeleteTwoMatchesTsScript), initialText: string.Empty, seedOverride: 16384);
        RunScript(
            harness,
            InsertStep(0, "IDT", "random-delete-2-step-1"),
            InsertStep(3, "wwA", "random-delete-2-step-2"),
            InsertStep(3, "Gnr", "random-delete-2-step-3"),
            DeleteStep(6, 3, "random-delete-2-step-4"),
            InsertStep(4, "eHp", "random-delete-2-step-5"),
            InsertStep(1, "UAi", "random-delete-2-step-6"),
            InsertStep(2, "FrR", "random-delete-2-step-7"),
            DeleteStep(6, 7, "random-delete-2-step-8"),
            DeleteStep(3, 5, "random-delete-2-step-9"));

        harness.AssertState("random-delete-2-final");
    }

    [Fact]
    public void RandomDeleteThreeMatchesTsScript()
    {
        // Mirrors TS random delete 3 (pieceTreeTextBuffer.test.ts lines 385-404).
        using var harness = new PieceTreeFuzzHarness(nameof(RandomDeleteThreeMatchesTsScript), initialText: string.Empty, seedOverride: 32768);
        RunScript(
            harness,
            InsertStep(0, "PqM", "random-delete-3-step-1"),
            DeleteStep(1, 2, "random-delete-3-step-2"),
            InsertStep(1, "zLc", "random-delete-3-step-3"),
            InsertStep(0, "MEX", "random-delete-3-step-4"),
            InsertStep(0, "jZh", "random-delete-3-step-5"),
            InsertStep(8, "GwQ", "random-delete-3-step-6"),
            DeleteStep(5, 6, "random-delete-3-step-7"),
            InsertStep(4, "ktw", "random-delete-3-step-8"),
            InsertStep(5, "GVu", "random-delete-3-step-9"),
            InsertStep(9, "jdm", "random-delete-3-step-10"),
            InsertStep(15, "na\n", "random-delete-3-step-11"),
            DeleteStep(5, 8, "random-delete-3-step-12"),
            DeleteStep(3, 4, "random-delete-3-step-13"));

        harness.AssertState("random-delete-3-final");
    }

    [Fact]
    public void RandomInsertDeleteCrBugOneMatchesTsScript()
    {
        // Mirrors TS "random insert/delete \r bug 1" (pieceTreeTextBuffer.test.ts lines 404-432).
        using var harness = new PieceTreeFuzzHarness(nameof(RandomInsertDeleteCrBugOneMatchesTsScript), initialText: "a", seedOverride: 65536);
        RunScript(
            harness,
            DeleteStep(0, 1, "cr-bug-1-delete-1"),
            InsertStep(0, "\r\r\n\n", "cr-bug-1-insert-1"),
            DeleteStep(3, 1, "cr-bug-1-delete-2"),
            InsertStep(2, "\n\n\ra", "cr-bug-1-insert-2"),
            DeleteStep(4, 3, "cr-bug-1-delete-3"),
            InsertStep(2, "\na\r\r", "cr-bug-1-insert-3"),
            InsertStep(6, "\ra\n\n", "cr-bug-1-insert-4"),
            InsertStep(0, "aa\n\n", "cr-bug-1-insert-5"),
            InsertStep(5, "\n\na\r", "cr-bug-1-insert-6"));

        harness.AssertState("random-insert-delete-cr-bug-1-final");
    }

    [Fact]
    public void RandomInsertDeleteCrBugTwoMatchesTsScript()
    {
        // Mirrors TS "random insert/delete \r bug 2" (pieceTreeTextBuffer.test.ts lines 432-460).
        using var harness = new PieceTreeFuzzHarness(nameof(RandomInsertDeleteCrBugTwoMatchesTsScript), initialText: "a", seedOverride: 65537);
        RunScript(
            harness,
            InsertStep(1, "\naa\r", "cr-bug-2-insert-1"),
            DeleteStep(0, 4, "cr-bug-2-delete-1"),
            InsertStep(1, "\r\r\na", "cr-bug-2-insert-2"),
            InsertStep(2, "\n\r\ra", "cr-bug-2-insert-3"),
            DeleteStep(4, 1, "cr-bug-2-delete-2"),
            InsertStep(8, "\r\n\r\r", "cr-bug-2-insert-4"),
            InsertStep(7, "\n\n\na", "cr-bug-2-insert-5"),
            InsertStep(13, "a\n\na", "cr-bug-2-insert-6"),
            DeleteStep(17, 3, "cr-bug-2-delete-3"),
            InsertStep(2, "a\ra\n", "cr-bug-2-insert-7"));

        harness.AssertState("random-insert-delete-cr-bug-2-final");
    }

    [Fact]
    public void RandomInsertDeleteCrBugThreeMatchesTsScript()
    {
        // Mirrors TS "random insert/delete \r bug 3" (pieceTreeTextBuffer.test.ts lines 460-490).
        using var harness = new PieceTreeFuzzHarness(nameof(RandomInsertDeleteCrBugThreeMatchesTsScript), initialText: "a", seedOverride: 65538);
        RunScript(
            harness,
            InsertStep(0, "\r\na\r", "cr-bug-3-insert-1"),
            DeleteStep(2, 3, "cr-bug-3-delete-1"),
            InsertStep(2, "a\r\n\r", "cr-bug-3-insert-2"),
            DeleteStep(4, 2, "cr-bug-3-delete-2"),
            InsertStep(4, "a\n\r\n", "cr-bug-3-insert-3"),
            InsertStep(1, "aa\n\r", "cr-bug-3-insert-4"),
            InsertStep(7, "\na\r\n", "cr-bug-3-insert-5"),
            InsertStep(5, "\n\na\r", "cr-bug-3-insert-6"),
            InsertStep(10, "\r\r\n\r", "cr-bug-3-insert-7"),
            DeleteStep(21, 3, "cr-bug-3-delete-3"));

        harness.AssertState("random-insert-delete-cr-bug-3-final");
    }

    [Fact]
    public void RandomInsertDeleteCrBugFourMatchesTsScript()
    {
        // Mirrors TS "random insert/delete \r bug 4" (pieceTreeTextBuffer.test.ts lines 490-520).
        using var harness = new PieceTreeFuzzHarness(nameof(RandomInsertDeleteCrBugFourMatchesTsScript), initialText: "a", seedOverride: 65539);
        RunScript(
            harness,
            DeleteStep(0, 1, "cr-bug-4-delete-1"),
            InsertStep(0, "\naaa", "cr-bug-4-insert-1"),
            InsertStep(2, "\n\naa", "cr-bug-4-insert-2"),
            DeleteStep(1, 4, "cr-bug-4-delete-2"),
            DeleteStep(3, 1, "cr-bug-4-delete-3"),
            DeleteStep(1, 2, "cr-bug-4-delete-4"),
            DeleteStep(0, 1, "cr-bug-4-delete-5"),
            InsertStep(0, "a\n\n\r", "cr-bug-4-insert-3"),
            InsertStep(2, "aa\r\n", "cr-bug-4-insert-4"),
            InsertStep(3, "a\naa", "cr-bug-4-insert-5"));

        harness.AssertState("random-insert-delete-cr-bug-4-final");
    }

    [Fact]
    public void RandomInsertDeleteCrBugFiveMatchesTsScript()
    {
        // Mirrors TS "random insert/delete \r bug 5" (pieceTreeTextBuffer.test.ts lines 520-550).
        using var harness = new PieceTreeFuzzHarness(nameof(RandomInsertDeleteCrBugFiveMatchesTsScript), initialText: string.Empty, seedOverride: 65540);
        RunScript(
            harness,
            InsertStep(0, "\n\n\n\r", "cr-bug-5-insert-1"),
            InsertStep(1, "\n\n\n\r", "cr-bug-5-insert-2"),
            InsertStep(2, "\n\r\r\r", "cr-bug-5-insert-3"),
            InsertStep(8, "\n\r\n\r", "cr-bug-5-insert-4"),
            DeleteStep(5, 2, "cr-bug-5-delete-1"),
            InsertStep(4, "\n\r\r\r", "cr-bug-5-insert-5"),
            InsertStep(8, "\n\n\n\r", "cr-bug-5-insert-6"),
            DeleteStep(0, 7, "cr-bug-5-delete-2"),
            InsertStep(1, "\r\n\r\r", "cr-bug-5-insert-7"),
            InsertStep(15, "\n\r\r\r", "cr-bug-5-insert-8"));

        harness.AssertState("random-insert-delete-cr-bug-5-final");
    }

    [Fact]
    public void RandomChunksMatchesTsSuite()
    {
        // Mirrors TS "random chunks" suite (pieceTreeTextBuffer.test.ts lines 1668-1708).
        const int seed = 0xB3C001;
        var rng = new Random(seed);
        var chunks = CreateRandomChunks(rng, count: 5, len: 1000);
        using var harness = new PieceTreeFuzzHarness(nameof(RandomChunksMatchesTsSuite), chunks, normalizeChunks: false, seedOverride: seed);
        var expected = new StringBuilder(string.Concat(chunks));

        for (var i = 0; i < 1000; i++)
        {
            if (ShouldInsert(rng))
            {
                var text = CreateTsRandomString(rng, 100);
                var position = rng.Next(expected.Length + 1);
                harness.Insert(position, text, $"random-chunks-insert-{i}");
                expected.Insert(position, text);
            }
            else if (expected.Length > 0)
            {
                var position = rng.Next(expected.Length);
                var maxLength = expected.Length - position;
                var deleteLength = maxLength == 0 ? 0 : Math.Min(maxLength, rng.Next(10));
                harness.Delete(position, deleteLength, $"random-chunks-delete-{i}");
                if (deleteLength > 0)
                {
                    expected.Remove(position, deleteLength);
                }
            }
        }

        harness.AssertState("random-chunks-final");
        Assert.Equal(expected.ToString(), harness.ExpectedText);
    }

    [Fact]
    public void RandomChunksTwoMatchesTsSuite()
    {
        // Mirrors TS "random chunks 2" suite (pieceTreeTextBuffer.test.ts lines 1708-1725).
        const int seed = 0xB3C002;
        var rng = new Random(seed);
        var chunks = CreateRandomChunks(rng, count: 1, len: 1000);
        using var harness = new PieceTreeFuzzHarness(nameof(RandomChunksTwoMatchesTsSuite), chunks, normalizeChunks: false, seedOverride: seed);
        var expected = new StringBuilder(string.Concat(chunks));

        for (var i = 0; i < 50; i++)
        {
            if (ShouldInsert(rng))
            {
                var text = CreateTsRandomString(rng, 30);
                var position = rng.Next(expected.Length + 1);
                harness.Insert(position, text, $"random-chunks2-insert-{i}");
                expected.Insert(position, text);
            }
            else if (expected.Length > 0)
            {
                var position = rng.Next(expected.Length);
                var maxLength = expected.Length - position;
                var deleteLength = maxLength == 0 ? 0 : Math.Min(maxLength, rng.Next(10));
                harness.Delete(position, deleteLength, $"random-chunks2-delete-{i}");
                if (deleteLength > 0)
                {
                    expected.Remove(position, deleteLength);
                }
            }

            harness.AssertState($"random-chunks2-iteration-{i}");
        }

        harness.AssertState("random-chunks2-final");
        Assert.Equal(expected.ToString(), harness.ExpectedText);
    }
}
