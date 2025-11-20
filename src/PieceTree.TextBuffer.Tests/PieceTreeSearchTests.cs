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

    [Fact]
    public void Regex_WordBoundaryHonorsEcmaDefinition()
    {
        var model = CreateModel("café caf");

        var matches = model.FindMatches(@"\bcaf\b", null, isRegex: true, matchCase: true, wordSeparators: null, captureMatches: false);

        Assert.Equal(2, matches.Count);
        Assert.Equal(1, matches[0].Range.Start.Column);
        Assert.Equal(4, matches[0].Range.End.Column);
        Assert.Equal(6, matches[1].Range.Start.Column);
        Assert.Equal(9, matches[1].Range.End.Column);
    }

    [Fact]
    public void Regex_DigitsRestrictToAscii()
    {
        var text = "aaa123٤٥٦bbb"; // Arabic-Indic digits should not match
        var model = CreateModel(text);

        var matches = model.FindMatches(@"\d+", null, isRegex: true, matchCase: true, wordSeparators: null, captureMatches: false);

        Assert.Single(matches);
        Assert.Equal(4, matches[0].Range.Start.Column);
        Assert.Equal(7, matches[0].Range.End.Column);
    }

    [Fact]
    public void WholeWord_IgnoresUnicodeSpacesUnlessExplicit()
    {
        var text = $"foo\u00A0bar foo\u2002baz foo";
        var model = CreateModel(text);
        var defaultSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";

        Assert.Empty(model.FindMatches("bar", null, isRegex: false, matchCase: true, wordSeparators: defaultSeparators, captureMatches: false));
        Assert.Empty(model.FindMatches("baz", null, isRegex: false, matchCase: true, wordSeparators: defaultSeparators, captureMatches: false));

        var withNbsp = defaultSeparators + "\u00A0";
        var barMatches = model.FindMatches("bar", null, isRegex: false, matchCase: true, wordSeparators: withNbsp, captureMatches: false);
        Assert.Single(barMatches);

        Assert.Empty(model.FindMatches("baz", null, isRegex: false, matchCase: true, wordSeparators: withNbsp, captureMatches: false));

        var withEnSpace = withNbsp + "\u2002";
        var bazMatches = model.FindMatches("baz", null, isRegex: false, matchCase: true, wordSeparators: withEnSpace, captureMatches: false);
        Assert.Single(bazMatches);
    }

    [Fact]
    public void Regex_EmojiQuantifiersConsumeCodePoints()
    {
        var emoji = char.ConvertFromUtf32(0x1F600);
        var text = string.Concat(emoji, emoji);
        var model = CreateModel(text);

        var singleMatches = model.FindMatches(".", null, isRegex: true, matchCase: true, wordSeparators: null, captureMatches: false);
        Assert.Equal(2, singleMatches.Count);
        Assert.Equal(1, singleMatches[0].Range.Start.Column);
        Assert.Equal(emoji.Length + 1, singleMatches[0].Range.End.Column);
        Assert.Equal(emoji.Length + 1, singleMatches[1].Range.Start.Column);
        Assert.Equal(text.Length + 1, singleMatches[1].Range.End.Column);

        var doubleMatch = model.FindMatches(@"^.{2}$", null, isRegex: true, matchCase: true, wordSeparators: null, captureMatches: false);
        Assert.Single(doubleMatch);
        Assert.Equal(1, doubleMatch[0].Range.Start.Column);
        Assert.Equal(text.Length + 1, doubleMatch[0].Range.End.Column);
    }
}
