using PieceTree.TextBuffer;
using Xunit;
using PieceTreeRange = PieceTree.TextBuffer.Core.Range;
using TextPosition = PieceTree.TextBuffer.TextPosition;

namespace PieceTree.TextBuffer.Tests;

public class TextModelRangeSearchTests
{
    [Fact]
    public void MultiRangeFindMatchesHonorsSelection()
    {
        var model = new TextModel("alpha bravo\ncharlie delta\nalpha echo\n");
        PieceTreeRange[] ranges =
        {
            new PieceTreeRange(new TextPosition(1, 1), new TextPosition(1, 6)),
            new PieceTreeRange(new TextPosition(3, 1), new TextPosition(3, 6)),
        };

        var matches = model.FindMatches(
            "alpha",
            ranges,
            findInSelection: true,
            isRegex: false,
            matchCase: true,
            wordSeparators: null,
            captureMatches: false);

        Assert.Equal(2, matches.Count);
        Assert.Equal(1, matches[0].Range.StartLineNumber);
        Assert.Equal(3, matches[1].Range.StartLineNumber);
    }

    [Fact]
    public void FindNextMatchWrapsWithinSelection()
    {
        var model = new TextModel("one two three\none two three\n");
        PieceTreeRange[] ranges =
        {
            new PieceTreeRange(new TextPosition(1, 1), new TextPosition(1, 4)),
            new PieceTreeRange(new TextPosition(2, 1), new TextPosition(2, 4)),
        };

        var match1 = model.FindNextMatch(
            "one",
            new TextPosition(1, 1),
            ranges,
            findInSelection: true,
            isRegex: false,
            matchCase: true,
            wordSeparators: null);

        Assert.NotNull(match1);
        Assert.Equal(1, match1!.Range.StartLineNumber);

        var match2 = model.FindNextMatch(
            "one",
            new TextPosition(1, 4),
            ranges,
            findInSelection: true,
            isRegex: false,
            matchCase: true,
            wordSeparators: null);

        Assert.NotNull(match2);
        Assert.Equal(2, match2!.Range.StartLineNumber);
    }

    [Fact]
    public void FindPreviousMatchWrapsWithinSelection()
    {
        var model = new TextModel("one two three\none two three\n");
        PieceTreeRange[] ranges =
        {
            new PieceTreeRange(new TextPosition(1, 1), new TextPosition(1, 4)),
            new PieceTreeRange(new TextPosition(2, 1), new TextPosition(2, 4)),
        };

        var match = model.FindPreviousMatch(
            "one",
            new TextPosition(2, 14),
            ranges,
            findInSelection: true,
            isRegex: false,
            matchCase: true,
            wordSeparators: null);

        Assert.NotNull(match);
        Assert.Equal(2, match!.Range.StartLineNumber);

        var wrapped = model.FindPreviousMatch(
            "one",
            new TextPosition(2, 1),
            ranges,
            findInSelection: true,
            isRegex: false,
            matchCase: true,
            wordSeparators: null);

        Assert.NotNull(wrapped);
        Assert.Equal(1, wrapped!.Range.StartLineNumber);
    }
}
