// Original C# implementation
// Purpose: Fuzz testing for CRLF handling edge cases in PieceTree
// - Validates cross-chunk CRLF normalization and split boundary handling
// - Step 2 (WS1-PORT-CRLF): _lastChangeBufferPos + AppendToChangeBufferNode hitCRLF
// - Step 3 (WS1-PORT-CRLF): CRLF bridge while creating new pieces
// Created: 2025-11-22
// Updated: 2025-11-26 (WS1-PORT-CRLF)

using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Tests.Helpers;
using static PieceTree.TextBuffer.Tests.Helpers.PieceTreeScript;

namespace PieceTree.TextBuffer.Tests;

public class CRLFFuzzTests
{
    [Fact]
    public void LargeInsert_HugePayload()
    {
        string payload = new string('x', ChunkUtilities.DefaultChunkSize + 100);
        PieceTreeBuffer buffer = new("");

        int initialPieces = buffer.InternalModel.PieceCount;
        buffer.ApplyEdit(0, 0, payload);
        Assert.True(buffer.InternalModel.PieceCount >= initialPieces + 2);
        Assert.Equal(payload.Length, buffer.Length);
        Assert.Equal(payload, buffer.GetText());
        buffer.InternalModel.AssertPieceIntegrity();
    }

    [Fact]
    public void CRLF_SplitAcrossNodes()
    {
        PieceTreeBuffer buffer = PieceTreeBuffer.FromChunks(new[] { "Hello\r", "\nWorld" });
        // Should normalize to a single CRLF
        Assert.Equal(1, buffer.InternalModel.TotalLineFeeds);
        Assert.Equal("Hello\r\nWorld", buffer.GetText());
    }

    [Fact]
    public void CRLF_RandomFuzz_1000()
    {
        using PieceTreeFuzzHarness harness = new(nameof(CRLF_RandomFuzz_1000), initialText: string.Empty, seedOverride: 123);
        Random rng = harness.Random;

        for (int i = 0; i < 1000; i++)
        {
            harness.SetIteration(i);
            int bufferLength = harness.Buffer.Length;
            int offset = rng.Next(0, Math.Max(0, bufferLength + 1));
            int op = rng.Next(0, 10);

            if (op == 0 && bufferLength > 0)
            {
                int deleteLength = Math.Min(bufferLength - offset, rng.Next(1, Math.Min(8, bufferLength - offset + 1)));
                if (deleteLength > 0)
                {
                    harness.Delete(offset, deleteLength, $"crlf-random-fuzz-delete-{i}");
                }
            }
            else
            {
                string toInsert = NextCrlfPayload(rng);
                harness.Insert(offset, toInsert, $"crlf-random-fuzz-insert-{i}");
            }
        }

        harness.AssertState("crlf-random-fuzz-1000-final");
        harness.ResetIteration();
    }

    [Fact]
    public void CRLF_NewlineInsert_BetweenCrLf()
    {
        using PieceTreeFuzzHarness harness = new(nameof(CRLF_NewlineInsert_BetweenCrLf));
        RunScript(
            harness,
            InsertStep(0, "vvvv", "crlf-newline-between-step-01"),
            DeleteStep(0, 1, "crlf-newline-between-step-02"),
            InsertStep(0, "m", "crlf-newline-between-step-03"),
            DeleteStep(2, 2, "crlf-newline-between-step-04"),
            InsertStep(1, "\r", "crlf-newline-between-step-05"),
            InsertStep(3, "\r", "crlf-newline-between-step-06"),
            DeleteStep(3, 1, "crlf-newline-between-step-07"),
            InsertStep(2, "n", "crlf-newline-between-step-08"),
            InsertStep(2, "bbb", "crlf-newline-between-step-09"),
            InsertStep(0, "\n", "crlf-newline-between-step-10"),
            InsertStep(3, "xxx", "crlf-newline-between-step-11"),
            InsertStep(2, "\r", "crlf-newline-between-step-12"),
            InsertStep(2, "\r", "crlf-newline-between-step-13"),
            InsertStep(6, "\r", "crlf-newline-between-step-14"),
            InsertStep(0, "y", "crlf-newline-between-step-15"),
            DeleteStep(11, 1, "crlf-newline-between-step-16"),
            InsertStep(5, "b", "crlf-newline-between-step-17"),
            InsertStep(0, "\r", "crlf-newline-between-step-18"),
            InsertStep(14, "jjjjj", "crlf-newline-between-step-19"),
            DeleteStep(4, 2, "crlf-newline-between-step-20"),
            InsertStep(10, "\n", "crlf-newline-between-step-21"),
            InsertStep(13, "\n", "crlf-newline-between-step-22"),
            DeleteStep(16, 3, "crlf-newline-between-step-23"),
            InsertStep(8, "\n", "crlf-newline-between-step-24"),
            InsertStep(4, "e", "crlf-newline-between-step-25"),
            InsertStep(6, "ggg", "crlf-newline-between-step-26"),
            InsertStep(3, "\n", "crlf-newline-between-step-27"),
            InsertStep(1, "\r", "crlf-newline-between-step-28"),
            InsertStep(14, "\n", "crlf-newline-between-step-29"));

        harness.AssertState("crlf-newline-between-crlf-final");
    }
    #region Step 2: AppendToChangeBufferNode CRLF Bridges

    [Fact]
    public void Step2_AppendToNode_MultipleCRLFBridges()
    {
        PieceTreeBuffer buffer = new("");

        buffer.ApplyEdit(0, 0, "line1\r");
        buffer.ApplyEdit(buffer.Length, 0, "\nline2\r");
        buffer.ApplyEdit(buffer.Length, 0, "\nline3\r");
        buffer.ApplyEdit(buffer.Length, 0, "\nline4");

        Assert.Equal("line1\r\nline2\r\nline3\r\nline4", buffer.GetText());
        Assert.Equal(4, buffer.InternalModel.TotalLineFeeds + 1);
        Assert.Equal("line1", buffer.GetLineContent(1));
        Assert.Equal("line2", buffer.GetLineContent(2));
        Assert.Equal("line3", buffer.GetLineContent(3));
        Assert.Equal("line4", buffer.GetLineContent(4));
        buffer.InternalModel.AssertPieceIntegrity();
    }

    #endregion

    #region Step 3: CreateNewPieces CRLF Bridge Tests

    /// <summary>
    /// Tests CRLF bridge in CreateNewPieces when buffer[0] ends with \r
    /// and new text starts with \n. Uses the '_' placeholder technique.
    /// </summary>
    [Fact]
    public void Step3_CreateNewPieces_CRLFBridge_PlaceholderTechnique()
    {
        PieceTreeBuffer buffer = new("");

        // First insert something that will go to change buffer and ends with \r
        buffer.ApplyEdit(0, 0, "test\r");

        // Now insert a new piece that starts with \n
        // This should trigger the CRLF bridge in CreateNewPieces
        buffer.ApplyEdit(buffer.Length, 0, "\ncontinue");

        Assert.Equal("test\r\ncontinue", buffer.GetText());
        Assert.Equal(2, buffer.InternalModel.TotalLineFeeds + 1);
        buffer.InternalModel.AssertPieceIntegrity();
    }

    /// <summary>
    /// Tests that GetLineContent returns correct content after CRLF bridge.
    /// </summary>
    [Fact]
    public void Step3_CreateNewPieces_GetLineContent_AfterBridge()
    {
        PieceTreeBuffer buffer = new("");

        buffer.ApplyEdit(0, 0, "first\r");
        buffer.ApplyEdit(buffer.Length, 0, "\nsecond");

        Assert.Equal("first", buffer.GetLineContent(1));
        Assert.Equal("second", buffer.GetLineContent(2));
        buffer.InternalModel.AssertPieceIntegrity();
    }

    /// <summary>
    /// Tests that TotalLineFeeds remains consistent after CRLF bridging.
    /// </summary>
    [Fact]
    public void Step3_CreateNewPieces_TotalLineFeedsConsistent()
    {
        PieceTreeBuffer buffer = new("");

        // Build up content with potential CRLF bridges
        buffer.ApplyEdit(0, 0, "A\r");
        int lfAfterCR = buffer.InternalModel.TotalLineFeeds;

        buffer.ApplyEdit(buffer.Length, 0, "\nB");
        int lfAfterLF = buffer.InternalModel.TotalLineFeeds;

        // TotalLineFeeds should stay the same (CR was already counted, now merged with LF)
        Assert.Equal(lfAfterCR, lfAfterLF);
        Assert.Equal(1, lfAfterLF);

        buffer.ApplyEdit(buffer.Length, 0, "\nC");
        Assert.Equal(2, buffer.InternalModel.TotalLineFeeds);

        Assert.Equal("A\r\nB\nC", buffer.GetText());
        buffer.InternalModel.AssertPieceIntegrity();
    }

    /// <summary>
    /// Tests interleaved append and CreateNewPieces with CRLF bridges.
    /// </summary>
    [Fact]
    public void Step3_InterleavedAppendAndCreateNewPieces_CRLFBridge()
    {
        PieceTreeBuffer buffer = new("");

        // Small insert (append to change buffer node)
        buffer.ApplyEdit(0, 0, "x\r");

        // Large insert that triggers CreateNewPieces with new buffer
        string largeText = "\n" + new string('y', 100);
        buffer.ApplyEdit(buffer.Length, 0, largeText);

        string expected = "x\r" + largeText;
        Assert.Equal(expected, buffer.GetText());
        Assert.Equal(2, buffer.InternalModel.TotalLineFeeds + 1);
        buffer.InternalModel.AssertPieceIntegrity();
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Tests inserting only \n when buffer ends with \r (empty line creation).
    /// </summary>
    [Fact]
    public void EdgeCase_InsertOnlyLF_AfterCR()
    {
        PieceTreeBuffer buffer = new("");

        buffer.ApplyEdit(0, 0, "text\r");
        buffer.ApplyEdit(buffer.Length, 0, "\n");

        Assert.Equal("text\r\n", buffer.GetText());
        Assert.Equal(2, buffer.InternalModel.TotalLineFeeds + 1);
        Assert.Equal("text", buffer.GetLineContent(1));
        Assert.Equal("", buffer.GetLineContent(2));
        buffer.InternalModel.AssertPieceIntegrity();
    }

    /// <summary>
    /// Tests inserting \r\n when buffer ends with \r (should create two line breaks).
    /// </summary>
    [Fact]
    public void EdgeCase_InsertCRLF_AfterCR()
    {
        PieceTreeBuffer buffer = new("");

        buffer.ApplyEdit(0, 0, "text\r");
        buffer.ApplyEdit(buffer.Length, 0, "\r\n");

        // Should result in: text\r + \r\n = "text\r\r\n"
        // The first \r is a lone CR, then \r\n is a CRLF pair
        Assert.Equal("text\r\r\n", buffer.GetText());
        Assert.Equal(3, buffer.InternalModel.TotalLineFeeds + 1); // text | (empty after CR) | (after CRLF)
        buffer.InternalModel.AssertPieceIntegrity();
    }

    /// <summary>
    /// Tests search cache invalidation after CRLF bridge.
    /// </summary>
    [Fact]
    public void EdgeCase_SearchCacheInvalidation_AfterCRLFBridge()
    {
        PieceTreeBuffer buffer = new("");

        buffer.ApplyEdit(0, 0, "search target\r");

        // Prime the cache by accessing content
        _ = buffer.GetLineContent(1);
        _ = buffer.GetLineContent(2);

        // Now trigger CRLF bridge
        buffer.ApplyEdit(buffer.Length, 0, "\nnew content");

        // Verify content is still correct (cache should be invalidated)
        Assert.Equal("search target", buffer.GetLineContent(1));
        Assert.Equal("new content", buffer.GetLineContent(2));
        buffer.InternalModel.AssertPieceIntegrity();
    }

    /// <summary>
    /// Tests that the placeholder character '_' doesn't leak into content.
    /// </summary>
    [Fact]
    public void EdgeCase_PlaceholderDoesNotLeakIntoContent()
    {
        PieceTreeBuffer buffer = new("");

        // Create multiple CRLF bridges
        for (int i = 0; i < 5; i++)
        {
            buffer.ApplyEdit(buffer.Length, 0, $"line{i}\r");
            buffer.ApplyEdit(buffer.Length, 0, "\n");
        }

        string text = buffer.GetText();

        // The placeholder '_' should never appear in the actual content
        Assert.DoesNotContain("_", text.Replace("_", "FOUND"));
        Assert.Equal("line0\r\nline1\r\nline2\r\nline3\r\nline4\r\n", text);
        buffer.InternalModel.AssertPieceIntegrity();
    }

    #endregion

    private static string NextCrlfPayload(Random rng)
    {
        int variant = rng.Next(0, 6);
        return variant switch
        {
            0 => "\r",
            1 => "\n",
            2 => "\r\n",
            3 => $"{RandomLetters(rng, rng.Next(1, 4))}\r",
            4 => $"\n{RandomLetters(rng, rng.Next(1, 4))}",
            _ => $"{RandomLetters(rng, rng.Next(1, 4))}\r\n{RandomLetters(rng, rng.Next(0, 3))}",
        };
    }

    private static string RandomLetters(Random rng, int length)
    {
        if (length <= 0)
        {
            return string.Empty;
        }

        char[] chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = (char)('a' + rng.Next(0, 26));
        }

        return new string(chars);
    }
}
