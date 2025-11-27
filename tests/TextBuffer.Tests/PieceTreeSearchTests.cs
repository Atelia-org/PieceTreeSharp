// Source: ts/src/vs/editor/test/common/model/textModelSearch.test.ts
// - Tests: FindMatches literal/regex, capture groups, multiline search, word boundaries
// Ported: 2025-11-19

using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeSearchTests
{
    private static TextModel CreateModel(string text) => new(text);

    [Fact]
    public void FindMatches_ReturnsLiteralHits()
    {
        TextModel model = CreateModel("foo bar foo");
        IReadOnlyList<Core.FindMatch> matches = model.FindMatches("foo", searchRange: null, isRegex: false, matchCase: false, wordSeparators: null, captureMatches: false);

        Assert.Equal(2, matches.Count);
        Assert.Equal(1, matches[0].Range.Start.Column);
        Assert.Equal(9, matches[1].Range.Start.Column);
    }

    [Fact]
    public void FindMatches_ProvidesCaptureGroups()
    {
        TextModel model = CreateModel("line1\nline2");
        IReadOnlyList<Core.FindMatch> matches = model.FindMatches("(l(in)e)(\\d)", null, isRegex: true, matchCase: false, wordSeparators: null, captureMatches: true);

        Assert.Equal(2, matches.Count);
        Assert.Equal("line1", matches[0].Matches?[0]);
        Assert.Equal("line", matches[0].Matches?[1]);
        Assert.Equal("in", matches[0].Matches?[2]);
    }

    [Fact]
    public void FindMatches_MultilineLiteralAcrossCrLf()
    {
        TextModel model = CreateModel("alpha\r\nbeta\r\ngamma");
        IReadOnlyList<Core.FindMatch> matches = model.FindMatches("alpha\nbeta", null, isRegex: false, matchCase: false, wordSeparators: null, captureMatches: false);

        Assert.Single(matches);
        Assert.Equal(1, matches[0].Range.Start.LineNumber);
        Assert.Equal(1, matches[0].Range.Start.Column);
        Assert.Equal(2, matches[0].Range.End.LineNumber);
        Assert.Equal(5, matches[0].Range.End.Column);
    }

    [Fact]
    public void FindMatches_WholeWordHonorsSeparators()
    {
        TextModel model = CreateModel("foobar foo");
        string separators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";

        IReadOnlyList<Core.FindMatch> matches = model.FindMatches("foo", null, isRegex: false, matchCase: false, wordSeparators: separators, captureMatches: false);

        Assert.Single(matches);
        Assert.Equal(8, matches[0].Range.Start.Column);
    }

    [Fact]
    public void FindMatches_CustomSeparatorsSupportUnicode()
    {
        TextModel model = CreateModel("foo·bar foo");
        string defaultSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";
        string separatorsWithDot = defaultSeparators + "·";

        IReadOnlyList<Core.FindMatch> withoutCustom = model.FindMatches("foo", null, isRegex: false, matchCase: true, wordSeparators: defaultSeparators, captureMatches: false);
        Assert.Single(withoutCustom);

        IReadOnlyList<Core.FindMatch> withCustom = model.FindMatches("foo", null, isRegex: false, matchCase: true, wordSeparators: separatorsWithDot, captureMatches: false);
        Assert.Equal(2, withCustom.Count);
    }

    [Fact]
    public void FindMatches_ZeroLengthRegexOnAstralAdvances()
    {
        string emoji = char.ConvertFromUtf32(0x1F600);
        TextModel model = CreateModel(emoji);
        Range range = new(1, 1, 1, model.GetLineMaxColumn(1));

        IReadOnlyList<Core.FindMatch> matches = model.FindMatches("(?=)", range, isRegex: true, matchCase: false, wordSeparators: null, captureMatches: false, limitResultCount: 2);

        Assert.Equal(2, matches.Count);
        Assert.Equal(1, matches[0].Range.Start.Column);
        Assert.Equal(model.GetLineMaxColumn(1), matches[1].Range.Start.Column);
    }

    [Fact]
    public void FindNextAndPrevious_WrapAroundDocument()
    {
        TextModel model = CreateModel("foo\nbar\nfoo");
        Core.FindMatch? next = model.FindNextMatch("foo", new TextPosition(2, 1), isRegex: false, matchCase: false, wordSeparators: null);
        Assert.NotNull(next);
        Assert.Equal(3, next!.Range.Start.LineNumber);

        Core.FindMatch? previous = model.FindPreviousMatch("foo", new TextPosition(1, 1), isRegex: false, matchCase: false, wordSeparators: null);
        Assert.NotNull(previous);
        Assert.Equal(3, previous!.Range.Start.LineNumber);
    }

    [Fact]
    public void Regex_WordBoundaryHonorsEcmaDefinition()
    {
        TextModel model = CreateModel("café caf");

        IReadOnlyList<Core.FindMatch> matches = model.FindMatches(@"\bcaf\b", null, isRegex: true, matchCase: true, wordSeparators: null, captureMatches: false);

        Assert.Equal(2, matches.Count);
        Assert.Equal(1, matches[0].Range.Start.Column);
        Assert.Equal(4, matches[0].Range.End.Column);
        Assert.Equal(6, matches[1].Range.Start.Column);
        Assert.Equal(9, matches[1].Range.End.Column);
    }

    [Fact]
    public void Regex_DigitsRestrictToAscii()
    {
        string text = "aaa123٤٥٦bbb"; // Arabic-Indic digits should not match
        TextModel model = CreateModel(text);

        IReadOnlyList<Core.FindMatch> matches = model.FindMatches(@"\d+", null, isRegex: true, matchCase: true, wordSeparators: null, captureMatches: false);

        Assert.Single(matches);
        Assert.Equal(4, matches[0].Range.Start.Column);
        Assert.Equal(7, matches[0].Range.End.Column);
    }

    [Fact]
    public void WholeWord_IgnoresUnicodeSpacesUnlessExplicit()
    {
        string text = $"foo\u00A0bar foo\u2002baz foo";
        TextModel model = CreateModel(text);
        string defaultSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";

        Assert.Empty(model.FindMatches("bar", null, isRegex: false, matchCase: true, wordSeparators: defaultSeparators, captureMatches: false));
        Assert.Empty(model.FindMatches("baz", null, isRegex: false, matchCase: true, wordSeparators: defaultSeparators, captureMatches: false));

        string withNbsp = defaultSeparators + "\u00A0";
        IReadOnlyList<Core.FindMatch> barMatches = model.FindMatches("bar", null, isRegex: false, matchCase: true, wordSeparators: withNbsp, captureMatches: false);
        Assert.Single(barMatches);

        Assert.Empty(model.FindMatches("baz", null, isRegex: false, matchCase: true, wordSeparators: withNbsp, captureMatches: false));

        string withEnSpace = withNbsp + "\u2002";
        IReadOnlyList<Core.FindMatch> bazMatches = model.FindMatches("baz", null, isRegex: false, matchCase: true, wordSeparators: withEnSpace, captureMatches: false);
        Assert.Single(bazMatches);
    }

    [Fact]
    public void Regex_EmojiQuantifiersConsumeCodePoints()
    {
        string emoji = char.ConvertFromUtf32(0x1F600);
        string text = string.Concat(emoji, emoji);
        TextModel model = CreateModel(text);

        IReadOnlyList<Core.FindMatch> singleMatches = model.FindMatches(".", null, isRegex: true, matchCase: true, wordSeparators: null, captureMatches: false);
        Assert.Equal(2, singleMatches.Count);
        Assert.Equal(1, singleMatches[0].Range.Start.Column);
        Assert.Equal(emoji.Length + 1, singleMatches[0].Range.End.Column);
        Assert.Equal(emoji.Length + 1, singleMatches[1].Range.Start.Column);
        Assert.Equal(text.Length + 1, singleMatches[1].Range.End.Column);

        IReadOnlyList<Core.FindMatch> doubleMatch = model.FindMatches(@"^.{2}$", null, isRegex: true, matchCase: true, wordSeparators: null, captureMatches: false);
        Assert.Single(doubleMatch);
        Assert.Equal(1, doubleMatch[0].Range.Start.Column);
        Assert.Equal(text.Length + 1, doubleMatch[0].Range.End.Column);
    }
}
