// WS5-QA: PieceTree buffer API tests (#6 from WS5-INV-TestBacklog)
// Source Reference: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (lines 1750-1888)
// Tests: getLineCharCode, buffer equal semantics
// Created: 2025-11-26

using System;
using System.Text;
using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Tests.Helpers;

namespace PieceTree.TextBuffer.Tests;

/// <summary>
/// Tests for PieceTree buffer API methods: getLineCharCode, buffer equality.
/// These tests mirror TS pieceTreeTextBuffer.test.ts lines 1750-1888.
/// </summary>
public class PieceTreeBufferApiTests
{
    #region Helper Methods

    private static PieceTreeBuffer CreateBuffer(string content) => new PieceTreeBuffer(content);

    private static TextModel CreateModel(string content) => new TextModel(content);

    private static PieceTreeBuffer CreateBufferFromChunks(params string[] chunks)
    {
        return PieceTreeBuffer.FromChunks(chunks);
    }

    private static string ReadSnapshot(ITextSnapshot snapshot)
    {
        var builder = new StringBuilder();
        string? chunk;
        while ((chunk = snapshot.Read()) is not null)
        {
            builder.Append(chunk);
        }

        return builder.ToString();
    }

    #endregion

    #region getLineCharCode Tests - Issue #45735

    /// <summary>
    /// Test getLineCharCode - issue #45735
    /// TS: test('getLineCharCode - issue #45735', () => {...})
    /// Source: pieceTreeTextBuffer.test.ts lines 1786-1800
    /// </summary>
    [Fact]
    public void GetLineCharCode_Issue45735_SingleChunk()
    {
        var buffer = CreateBuffer("LINE1\nline2");

        // Line 1: "LINE1\n"
        Assert.Equal('L', (char)buffer.GetLineCharCode(1, 0));
        Assert.Equal('I', (char)buffer.GetLineCharCode(1, 1));
        Assert.Equal('N', (char)buffer.GetLineCharCode(1, 2));
        Assert.Equal('E', (char)buffer.GetLineCharCode(1, 3));
        Assert.Equal('1', (char)buffer.GetLineCharCode(1, 4));
        Assert.Equal('\n', (char)buffer.GetLineCharCode(1, 5));

        // Line 2: "line2"
        Assert.Equal('l', (char)buffer.GetLineCharCode(2, 0));
        Assert.Equal('i', (char)buffer.GetLineCharCode(2, 1));
        Assert.Equal('n', (char)buffer.GetLineCharCode(2, 2));
        Assert.Equal('e', (char)buffer.GetLineCharCode(2, 3));
        Assert.Equal('2', (char)buffer.GetLineCharCode(2, 4));
    }

    /// <summary>
    /// Test getLineCharCode - issue #47733
    /// TS: test('getLineCharCode - issue #47733', () => {...})
    /// Source: pieceTreeTextBuffer.test.ts lines 1804-1818
    /// Tests multi-chunk buffer where first chunk is empty.
    /// </summary>
    [Fact]
    public void GetLineCharCode_Issue47733_MultiChunkWithEmptyFirst()
    {
        // In TS: createTextBuffer(['', 'LINE1\n', 'line2'])
        // This creates buffer from multiple chunks
        var buffer = CreateBufferFromChunks("", "LINE1\n", "line2");

        // Line 1: "LINE1\n"
        Assert.Equal('L', (char)buffer.GetLineCharCode(1, 0));
        Assert.Equal('I', (char)buffer.GetLineCharCode(1, 1));
        Assert.Equal('N', (char)buffer.GetLineCharCode(1, 2));
        Assert.Equal('E', (char)buffer.GetLineCharCode(1, 3));
        Assert.Equal('1', (char)buffer.GetLineCharCode(1, 4));
        Assert.Equal('\n', (char)buffer.GetLineCharCode(1, 5));

        // Line 2: "line2"
        Assert.Equal('l', (char)buffer.GetLineCharCode(2, 0));
        Assert.Equal('i', (char)buffer.GetLineCharCode(2, 1));
        Assert.Equal('n', (char)buffer.GetLineCharCode(2, 2));
        Assert.Equal('e', (char)buffer.GetLineCharCode(2, 3));
        Assert.Equal('2', (char)buffer.GetLineCharCode(2, 4));
    }

    /// <summary>
    /// Test getLineCharCode returns 0 for out-of-bounds access.
    /// Note: C# implementation clamps to valid range, so we test boundary behavior.
    /// </summary>
    [Fact]
    public void GetLineCharCode_OutOfBounds_ReturnsZero()
    {
        var buffer = CreateBuffer("ab\ncd");

        // At the exact end of content, should return 0
        // Line 1 has "ab\n" (length 3 with terminator)
        // Line 2 has "cd" (length 2)
        // Testing beyond the buffer length
        var emptyBuffer = CreateBuffer("");
        Assert.Equal(0, emptyBuffer.GetLineCharCode(1, 0));
        Assert.Equal(0, emptyBuffer.GetLineCharCode(1, 10));
    }

    /// <summary>
    /// Test getLineCharCode with CRLF line endings.
    /// </summary>
    [Fact]
    public void GetLineCharCode_CrlfLineEndings()
    {
        var buffer = CreateBuffer("AB\r\nCD");

        // Line 1: "AB\r\n"
        Assert.Equal('A', (char)buffer.GetLineCharCode(1, 0));
        Assert.Equal('B', (char)buffer.GetLineCharCode(1, 1));
        Assert.Equal('\r', (char)buffer.GetLineCharCode(1, 2));
        Assert.Equal('\n', (char)buffer.GetLineCharCode(1, 3));
        
        // Line 2: "CD"
        Assert.Equal('C', (char)buffer.GetLineCharCode(2, 0));
        Assert.Equal('D', (char)buffer.GetLineCharCode(2, 1));
    }

    /// <summary>
    /// Ensure CR and LF split across chunk boundaries still return correct char codes.
    /// Mirrors TS regression tests for CRLF straddling pieces.
    /// </summary>
    [Fact]
    public void GetLineCharCode_CrlfAcrossChunks()
    {
        var buffer = CreateBufferFromChunks("A\r", "\nB");

        Assert.Equal('A', (char)buffer.GetLineCharCode(1, 0));
        Assert.Equal('\r', (char)buffer.GetLineCharCode(1, 1));
        Assert.Equal('\n', (char)buffer.GetLineCharCode(1, 2));
        Assert.Equal('B', (char)buffer.GetLineCharCode(2, 0));
    }

    #endregion

    #region Buffer Equal Tests

    /// <summary>
    /// Test buffer equality for identical content.
    /// </summary>
    [Fact]
    public void Buffer_Equal_IdenticalContent()
    {
        var content = "hello\nworld";
        var buffer1 = CreateBuffer(content);
        var buffer2 = CreateBuffer(content);

        // Both buffers should have same content
        Assert.True(buffer1.Equal(buffer2));
        Assert.True(buffer2.Equal(buffer1));
    }

    /// <summary>
    /// Test buffer equality for different content.
    /// </summary>
    [Fact]
    public void Buffer_Equal_DifferentContent()
    {
        var buffer1 = CreateBuffer("hello\nworld");
        var buffer2 = CreateBuffer("hello\nworld!");

        Assert.False(buffer1.Equal(buffer2));
        Assert.False(buffer2.Equal(buffer1));
    }

    /// <summary>
    /// Test buffer equality after edits resulting in same content.
    /// </summary>
    [Fact]
    public void Buffer_Equal_AfterEditsToSameContent()
    {
        var buffer1 = CreateBuffer("abc");
        var buffer2 = CreateBuffer("xyz");

        // Edit buffer2 to have same content as buffer1
        buffer2.ApplyEdit(0, 3, "abc");

        Assert.True(buffer1.Equal(buffer2));
        Assert.True(buffer2.Equal(buffer1));
    }

    /// <summary>
    /// Test buffer equality for empty buffers.
    /// </summary>
    [Fact]
    public void Buffer_Equal_EmptyBuffers()
    {
        var buffer1 = CreateBuffer("");
        var buffer2 = CreateBuffer("");

        Assert.True(buffer1.Equal(buffer2));
        Assert.True(buffer2.Equal(buffer1));
        Assert.Equal(0, buffer1.GetText().Length);
    }

    /// <summary>
    /// Equal buffers should compare true even when chunk layouts differ.
    /// Mirrors TS buffer api 'equal' test.
    /// </summary>
    [Fact]
    public void Buffer_Equal_SameContentAcrossChunkBoundaries()
    {
        var bufferA = CreateBufferFromChunks("abc");
        var bufferB = CreateBufferFromChunks("ab", "c");
        var bufferC = CreateBufferFromChunks("abd");
        var bufferD = CreateBufferFromChunks("abcd");

        Assert.True(bufferA.Equal(bufferB));
        Assert.True(bufferB.Equal(bufferA));
        Assert.False(bufferA.Equal(bufferC));
        Assert.False(bufferA.Equal(bufferD));
    }

    /// <summary>
    /// Equal buffers remain equal with more chunk permutations.
    /// Mirrors TS buffer api 'equal with more chunks'.
    /// </summary>
    [Fact]
    public void Buffer_Equal_ChunkPermutations()
    {
        var bufferA = CreateBufferFromChunks("ab", "cd", "e");
        var bufferB = CreateBufferFromChunks("ab", "c", "de");

        Assert.True(bufferA.Equal(bufferB));
        Assert.True(bufferB.Equal(bufferA));
    }

    /// <summary>
    /// Equality should handle empty buffers in both operands.
    /// </summary>
    [Fact]
    public void Buffer_Equal_EmptyVsNonEmpty()
    {
        var emptyA = CreateBufferFromChunks("");
        var emptyB = CreateBufferFromChunks("");
        var nonEmpty = CreateBufferFromChunks("a");

        Assert.True(emptyA.Equal(emptyB));
        Assert.False(emptyA.Equal(nonEmpty));
        Assert.False(nonEmpty.Equal(emptyB));
    }

    /// <summary>
    /// Buffers with different BOM values should not compare equal even if text matches.
    /// Mirrors TS PieceTreeTextBuffer.equals implementation.
    /// </summary>
    [Fact]
    public void Buffer_Equal_DifferentBom()
    {
        var bomBuffer = CreateBuffer("\uFEFFhello");
        var plainBuffer = CreateBuffer("hello");

        Assert.False(bomBuffer.Equal(plainBuffer));
        Assert.False(plainBuffer.Equal(bomBuffer));
    }

    /// <summary>
    /// Buffers with different preferred EOL sequences should not compare equal.
    /// </summary>
    [Fact]
    public void Buffer_Equal_DifferentEolSequences()
    {
        var bufferLf = CreateBuffer("hello\nworld");
        var bufferCrlf = CreateBuffer("hello\nworld");

        bufferLf.SetEol("\n");
        bufferCrlf.SetEol("\r\n");

        Assert.False(bufferLf.Equal(bufferCrlf));
        Assert.False(bufferCrlf.Equal(bufferLf));
    }

    #endregion

    #region GetCharCode Tests

    /// <summary>
    /// Test GetCharCode for basic ASCII content.
    /// </summary>
    [Fact]
    public void GetCharCode_BasicAscii()
    {
        var buffer = CreateBuffer("Hello");

        Assert.Equal('H', (char)buffer.GetCharCode(0));
        Assert.Equal('e', (char)buffer.GetCharCode(1));
        Assert.Equal('l', (char)buffer.GetCharCode(2));
        Assert.Equal('l', (char)buffer.GetCharCode(3));
        Assert.Equal('o', (char)buffer.GetCharCode(4));
    }

    /// <summary>
    /// Test GetCharCode with Unicode content.
    /// </summary>
    [Fact]
    public void GetCharCode_Unicode()
    {
        var buffer = CreateBuffer("你好");

        Assert.Equal('你', (char)buffer.GetCharCode(0));
        Assert.Equal('好', (char)buffer.GetCharCode(1));
    }

    /// <summary>
    /// Test GetCharCode at line break positions.
    /// </summary>
    [Fact]
    public void GetCharCode_LineBreaks()
    {
        var buffer = CreateBuffer("a\nb\r\nc");

        Assert.Equal('a', (char)buffer.GetCharCode(0));
        Assert.Equal('\n', (char)buffer.GetCharCode(1));
        Assert.Equal('b', (char)buffer.GetCharCode(2));
        Assert.Equal('\r', (char)buffer.GetCharCode(3));
        Assert.Equal('\n', (char)buffer.GetCharCode(4));
        Assert.Equal('c', (char)buffer.GetCharCode(5));
    }

    #endregion

    #region API Integration Tests

    /// <summary>
    /// Test that GetLineCharCode is consistent with GetLineContent.
    /// </summary>
    [Fact]
    public void GetLineCharCode_ConsistentWithGetLineContent()
    {
        var content = "Line One\nLine Two\nLine Three";
        var buffer = CreateBuffer(content);

        // Use the model's line count
        var model = CreateModel(content);
        for (int lineNum = 1; lineNum <= model.GetLineCount(); lineNum++)
        {
            var lineContent = buffer.GetLineContent(lineNum);
            for (int i = 0; i < lineContent.Length; i++)
            {
                var charCode = buffer.GetLineCharCode(lineNum, i);
                Assert.Equal(lineContent[i], (char)charCode);
            }
        }
    }

    /// <summary>
    /// Test buffer APIs after insert operations.
    /// </summary>
    [Fact]
    public void BufferApis_AfterInsert()
    {
        var buffer = CreateBuffer("hello");

        // Insert " world" at end (offset 5)
        buffer.ApplyEdit(5, 0, " world");

        Assert.Equal("hello world", buffer.GetText());
        Assert.Equal('h', (char)buffer.GetLineCharCode(1, 0));
        Assert.Equal(' ', (char)buffer.GetLineCharCode(1, 5));
        Assert.Equal('w', (char)buffer.GetLineCharCode(1, 6));
    }

    /// <summary>
    /// Test buffer APIs after delete operations.
    /// </summary>
    [Fact]
    public void BufferApis_AfterDelete()
    {
        var buffer = CreateBuffer("hello world");

        // Delete " world" (offset 5, length 6)
        buffer.ApplyEdit(5, 6, null);

        Assert.Equal("hello", buffer.GetText());
        Assert.Equal('h', (char)buffer.GetLineCharCode(1, 0));
        Assert.Equal('o', (char)buffer.GetLineCharCode(1, 4));
    }

    /// <summary>
    /// Test buffer APIs with mixed operations.
    /// </summary>
    [Fact]
    public void BufferApis_MixedOperations()
    {
        var buffer = CreateBuffer("abc");

        // Insert newline to create second line (at offset 1)
        buffer.ApplyEdit(1, 0, "\n");

        Assert.Equal("a\nbc", buffer.GetText());
        // Check line content via GetLineContent
        Assert.Equal("a", buffer.GetLineContent(1));
        Assert.Equal("bc", buffer.GetLineContent(2));
        Assert.Equal('a', (char)buffer.GetLineCharCode(1, 0));
        Assert.Equal('b', (char)buffer.GetLineCharCode(2, 0));
        Assert.Equal('c', (char)buffer.GetLineCharCode(2, 1));
    }

    /// <summary>
    /// Test GetNearestChunk behavior around insert/delete edits.
    /// Mirrors TS buffer api getNearestChunk.
    /// </summary>
    [Fact]
    public void GetNearestChunk_BasicLifecycle()
    {
        var buffer = CreateBuffer("012345678");

        buffer.ApplyEdit(3, 0, "ABC");
        Assert.Equal("012ABC345678", buffer.GetText());
        Assert.Equal("ABC", buffer.GetNearestChunk(3));
        Assert.Equal("345678", buffer.GetNearestChunk(6));

        buffer.ApplyEdit(9, 1, null);
        Assert.Equal("012ABC34578", buffer.GetText());
        Assert.Equal("345", buffer.GetNearestChunk(6));
        Assert.Equal("78", buffer.GetNearestChunk(9));
    }

    #endregion

    #region Raw Content And Snapshot Tests

    [Fact]
    public void GetLineRawContent_ReturnsRawLineIncludingTerminators()
    {
        var buffer = CreateBuffer("foo\r\nbar\n");

        Assert.Equal("foo\r\n", buffer.GetLineRawContent(1));
        Assert.Equal("bar\n", buffer.GetLineRawContent(2));
        Assert.Equal(string.Empty, buffer.GetLineRawContent(3));
    }

    [Fact]
    public void CreateSnapshot_HonorsPreserveBomFlag()
    {
        var buffer = CreateBuffer("\uFEFFabc\r\ndef");

        var withBom = buffer.CreateSnapshot(preserveBom: true);
        var withoutBom = buffer.CreateSnapshot();

        Assert.Equal("\uFEFFabc\r\ndef", ReadSnapshot(withBom));
        Assert.Equal("abc\r\ndef", ReadSnapshot(withoutBom));
    }

    #endregion
}
