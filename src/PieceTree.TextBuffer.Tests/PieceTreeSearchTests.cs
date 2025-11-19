using Xunit;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests;

public class TextModelSearchTests
{
    private static TextModel CreateModel(string text) => new(text);

    [Fact]
    public void FindMatches_ReturnsLiteralHits()
    {
        var model = CreateModel("foo bar foo");
        var matches = model.FindMatches("foo", searchRange: null, isRegex: false, matchCase: false, wordSeparators: null, captureMatches: false);

        Assert.Equal(2, matches.Count);
        Assert.Equal(1, matches[0].Range.Start.Column);
        Assert.Equal(9, matches[1].Range.Start.Column);
    }

    [Fact]
    public void FindMatches_ProvidesCaptureGroups()
    {
        var model = CreateModel("line1\nline2");
        var matches = model.FindMatches("(l(in)e)(\\d)", null, isRegex: true, matchCase: false, wordSeparators: null, captureMatches: true);

        Assert.Equal(2, matches.Count);
        Assert.Equal("line1", matches[0].Matches?[0]);
        Assert.Equal("line", matches[0].Matches?[1]);
        Assert.Equal("in", matches[0].Matches?[2]);
    }

    [Fact]
    public void FindMatches_MultilineLiteralAcrossCrLf()
    {
        var model = CreateModel("alpha\r\nbeta\r\ngamma");
        var matches = model.FindMatches("alpha\nbeta", null, isRegex: false, matchCase: false, wordSeparators: null, captureMatches: false);

        Assert.Single(matches);
        Assert.Equal(1, matches[0].Range.Start.LineNumber);
        Assert.Equal(1, matches[0].Range.Start.Column);
        Assert.Equal(2, matches[0].Range.End.LineNumber);
        Assert.Equal(5, matches[0].Range.End.Column);
    }

    [Fact]
    public void FindMatches_WholeWordHonorsSeparators()
    {
        var model = CreateModel("foobar foo");
        var separators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";

        var matches = model.FindMatches("foo", null, isRegex: false, matchCase: false, wordSeparators: separators, captureMatches: false);

        Assert.Single(matches);
        Assert.Equal(8, matches[0].Range.Start.Column);
    }

    [Fact]
    public void FindMatches_CustomSeparatorsSupportUnicode()
    {
        var model = CreateModel("foo·bar foo");
        var defaultSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";
        var separatorsWithDot = defaultSeparators + "·";

        var withoutCustom = model.FindMatches("foo", null, isRegex: false, matchCase: true, wordSeparators: defaultSeparators, captureMatches: false);
        Assert.Single(withoutCustom);

        var withCustom = model.FindMatches("foo", null, isRegex: false, matchCase: true, wordSeparators: separatorsWithDot, captureMatches: false);
        Assert.Equal(2, withCustom.Count);
    }

    [Fact]
    public void FindMatches_ZeroLengthRegexOnAstralAdvances()
    {
        var emoji = char.ConvertFromUtf32(0x1F600);
        var model = CreateModel(emoji);
        var range = new Range(1, 1, 1, model.GetLineMaxColumn(1));

        var matches = model.FindMatches("(?=)", range, isRegex: true, matchCase: false, wordSeparators: null, captureMatches: false, limitResultCount: 2);

        Assert.Equal(2, matches.Count);
        Assert.Equal(1, matches[0].Range.Start.Column);
        Assert.Equal(model.GetLineMaxColumn(1), matches[1].Range.Start.Column);
    }

    [Fact]
    public void FindNextAndPrevious_WrapAroundDocument()
    {
        var model = CreateModel("foo\nbar\nfoo");
        var next = model.FindNextMatch("foo", new TextPosition(2, 1), isRegex: false, matchCase: false, wordSeparators: null);
        Assert.NotNull(next);
        Assert.Equal(3, next!.Range.Start.LineNumber);

        var previous = model.FindPreviousMatch("foo", new TextPosition(1, 1), isRegex: false, matchCase: false, wordSeparators: null);
        Assert.NotNull(previous);
        Assert.Equal(3, previous!.Range.Start.LineNumber);
    }
}
