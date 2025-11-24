using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using Xunit;

namespace PieceTree.TextBuffer.Tests.Helpers;

internal static class PieceTreeBufferAssertions
{
    public static void AssertPositions(PieceTreeFuzzHarness harness, params (int Offset, TextPosition Position)[] expectations)
    {
        foreach (var (offset, position) in expectations)
        {
            Assert.Equal(position, harness.GetPositionAt(offset));
        }
    }

    public static void AssertOffsets(PieceTreeFuzzHarness harness, params (TextPosition Position, int Offset)[] expectations)
    {
        foreach (var (position, expectedOffset) in expectations)
        {
            var actualOffset = harness.GetOffsetAt(position);
            Assert.Equal(expectedOffset, actualOffset);
        }
    }

    public static void AssertLineCount(PieceTreeFuzzHarness harness, int expectedLineCount)
    {
        Assert.Equal(expectedLineCount, harness.GetLineCount());
    }

    public static void AssertValueInRange(PieceTreeFuzzHarness harness, PieceTree.TextBuffer.Core.Range range, string expected)
    {
        var actual = harness.GetValueInRange(range);
        Assert.Equal(expected, actual);
    }

    public static void AssertLineContent(PieceTreeFuzzHarness harness, int lineNumber, string expectedContent)
    {
        var actual = harness.GetLineContent(lineNumber);
        Assert.Equal(expectedContent, actual);
    }
}
