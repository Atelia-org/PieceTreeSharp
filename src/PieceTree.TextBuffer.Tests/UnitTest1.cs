using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeBufferTests
{
    [Fact]
    public void InitializesWithProvidedText()
    {
        var buffer = new PieceTreeBuffer("hello");
        Assert.Equal(5, buffer.Length);
        Assert.Equal("hello", buffer.GetText());
    }

    [Fact]
    public void LargeBufferRoundTripsContent()
    {
        var largeText = new string('x', 16_384);
        var buffer = new PieceTreeBuffer(largeText);

        Assert.Equal(largeText.Length, buffer.Length);
        Assert.Equal(largeText, buffer.GetText());
    }

    [Fact]
    public void AppliesSimpleEdit()
    {
        var buffer = new PieceTreeBuffer("hello world");
        buffer.ApplyEdit(6, 5, "piece tree");

        Assert.Equal("hello piece tree", buffer.GetText());
        Assert.Equal("hello piece tree".Length, buffer.Length);
    }

    [Fact]
    public void FromChunksBuildsPieceTreeAcrossMultipleBuffers()
    {
        var buffer = PieceTreeBuffer.FromChunks(new[] { "abc", string.Empty, "123\r\n", "xyz" });
        var expected = "abc123\r\nxyz";

        Assert.Equal(expected, buffer.GetText());
        Assert.Equal(expected.Length, buffer.Length);
    }

    [Fact]
    public void PieceTreeModelTracksLineFeedsAcrossChunks()
    {
        var build = PieceTreeBuilder.BuildFromChunks(new[]
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
        var buffer = PieceTreeBuffer.FromChunks(new[] { "foo\r\nbar" });
        buffer.ApplyEdit(5, 3, "piece\r\ntree");

        var expected = "foo\r\npiece\r\ntree";
        Assert.Equal(expected, buffer.GetText());
        Assert.Equal(expected.Length, buffer.Length);
    }

    [Fact]
    public void ApplyEditAcrossChunkBoundarySpansMultiplePieces()
    {
        var buffer = PieceTreeBuffer.FromChunks(new[] { "abcd", "ef", "ghij" });
        buffer.ApplyEdit(3, 4, "XYZ");

        Assert.Equal("abcXYZhij", buffer.GetText());
        Assert.Equal("abcXYZhij".Length, buffer.Length);
    }

    [Fact]
    public void PositionLookupMatchesTsPrefixSumExpectations()
    {
        const string text = "line1\nline2\r\nline3";
        var buffer = new PieceTreeBuffer(text);

        var pos0 = buffer.GetPositionAt(0);
        Assert.Equal(new TextPosition(1, 1), pos0);

        var posAfterLine1 = buffer.GetPositionAt("line1\n".Length);
        Assert.Equal(new TextPosition(2, 1), posAfterLine1);

        var posWithinLine3 = buffer.GetPositionAt(text.Length - 1);
        Assert.Equal(new TextPosition(3, 5), posWithinLine3);

        Assert.Equal(0, buffer.GetOffsetAt(1, 1));
        Assert.Equal("line1\n".Length, buffer.GetOffsetAt(2, 1));
        Assert.Equal(text.Length, buffer.GetOffsetAt(3, buffer.GetLineLength(3) + 1));
    }

    [Fact]
    public void LineCharCodeFollowsCrlfBoundaries()
    {
        var buffer = PieceTreeBuffer.FromChunks(new[] { "foo\r\nbar", "\nend" });

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
        var buffer = new PieceTreeBuffer("abc");
        Assert.Equal('a', buffer.GetCharCode(0));
        Assert.Equal('c', buffer.GetCharCode(2));
        Assert.Equal('c', buffer.GetCharCode(100));
    }

    // TODO(PT-005.S8): Blocked on Porter-CS (PT-004.G2) exposing EnumeratePieces to assert piece-level layout & chunk reuse.
    // TODO(PT-005.S9): Blocked on Investigator-TS finalizing BufferRange/SearchContext mapping before property-based edit fuzzing can begin.
    // TODO(PT-005.S10): Planned sequential delete+insert coverage to assert metadata after back-to-back ApplyEdit calls.
}
