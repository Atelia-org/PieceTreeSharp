// Source: ts/src/vs/editor/test/common/model/textModelSearch.test.ts
// - Suites: multi-range selection, word boundary matrix, multiline regex, capture navigation,
//           zero-width + unicode anchors, CRLF handling, issue coverage
// Ported: 2025-11-19

using System.Text.RegularExpressions;
using PieceTree.TextBuffer.Core;
using Xunit.Sdk;
using static PieceTree.TextBuffer.Tests.TextModelSearchTestHelper;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests;

internal static class TextModelSearchTestHelper
{
    internal const string UsualWordSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";
    internal static readonly RegexOptions DefaultRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ECMAScript;

    internal static Range R(int startLine, int startColumn, int endLine, int endColumn)
        => new(startLine, startColumn, endLine, endColumn);

    internal static void AssertParseSearchResult(
        string searchString,
        bool isRegex,
        bool matchCase,
        string? wordSeparators,
        ExpectedSearchData? expected)
    {
        SearchParams searchParams = new(searchString, isRegex, matchCase, wordSeparators);
        SearchData? actual = searchParams.ParseSearchRequest();

        if (expected == null)
        {
            Assert.Null(actual);
            return;
        }

        Assert.NotNull(actual);
        Assert.Equal(expected.Pattern, actual!.Regex.ToString());
        Assert.Equal(expected.Options, actual.Regex.Options);
        Assert.Equal(expected.SimpleSearch, actual.SimpleSearch);
        Assert.Equal(expected.ExpectWordSeparators, actual.WordSeparators is not null);
        Assert.Equal(expected.IsMultiline, actual.IsMultiline);
        Assert.Equal(expected.IsCaseSensitive, actual.IsCaseSensitive);
    }

    internal static void AssertFindMatches(string text, string pattern, bool isRegex, bool matchCase, string? wordSeparators, params Range[] expected)
    {
        SearchParams searchParams = new(pattern, isRegex, matchCase, wordSeparators);
        AssertFindMatches(new TextModel(text), searchParams, expected);

        TextModel crlfModel = new(text);
        crlfModel.SetEol(EndOfLineSequence.CRLF);
        AssertFindMatches(crlfModel, searchParams, expected);
    }

    internal static void AssertFindMatches(TextModel model, SearchParams searchParams, params Range[] expected)
    {
        IReadOnlyList<FindMatch> matches = model.FindMatches(searchParams, searchRange: null, captureMatches: false);
        Assert.Equal(expected.Length, matches.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            AssertRangeEqual(expected[i], matches[i].Range);
        }

        if (expected.Length == 0)
        {
            Assert.Null(model.FindNextMatch(searchParams, new TextPosition(1, 1)));
            TextPosition documentEnd = new(model.GetLineCount(), model.GetLineMaxColumn(model.GetLineCount()));
            Assert.Null(model.FindPreviousMatch(searchParams, documentEnd));
            return;
        }

        FindMatch? next = model.FindNextMatch(searchParams, new TextPosition(1, 1));
        Assert.NotNull(next);
        AssertRangeEqual(expected[0], next!.Range);

        foreach (Range expectedMatch in expected)
        {
            next = model.FindNextMatch(searchParams, new TextPosition(expectedMatch.StartLineNumber, expectedMatch.StartColumn), captureMatches: false);
            Assert.NotNull(next);
            AssertRangeEqual(expectedMatch, next!.Range);
        }

        TextPosition docEnd = new(model.GetLineCount(), model.GetLineMaxColumn(model.GetLineCount()));
        FindMatch? previous = model.FindPreviousMatch(searchParams, docEnd, captureMatches: false);
        Assert.NotNull(previous);
        AssertRangeEqual(expected[^1], previous!.Range);

        foreach (Range expectedMatch in expected)
        {
            previous = model.FindPreviousMatch(searchParams, new TextPosition(expectedMatch.EndLineNumber, expectedMatch.EndColumn), captureMatches: false);
            Assert.NotNull(previous);
            AssertRangeEqual(expectedMatch, previous!.Range);
        }
    }

    internal static IReadOnlyList<FindMatch> GetMatches(TextModel model, SearchParams searchParams, bool captureMatches = true)
        => model.FindMatches(searchParams, searchRange: null, captureMatches: captureMatches);

    internal static void AssertMatch(FindMatch? actual, Range expected, params string[] expectedCaptures)
    {
        Assert.NotNull(actual);
        AssertRangeEqual(expected, actual!.Range);
        if (expectedCaptures.Length == 0)
        {
            Assert.True(actual.Matches == null || actual.Matches.Length == 0);
            return;
        }

        Assert.NotNull(actual.Matches);
        Assert.Equal(expectedCaptures, actual.Matches!);
    }

    private static void AssertRangeEqual(Range expected, Range actual)
    {
        if (expected.StartLineNumber == actual.StartLineNumber
            && expected.StartColumn == actual.StartColumn
            && expected.EndLineNumber == actual.EndLineNumber
            && expected.EndColumn == actual.EndColumn)
        {
            return;
        }

        static string FormatRange(Range range)
            => $"[{range.StartLineNumber},{range.StartColumn}]->[{range.EndLineNumber},{range.EndColumn}]";

        throw new XunitException(
            $"Expected range {FormatRange(expected)} but found {FormatRange(actual)}");
    }

    internal sealed record ExpectedSearchData(
        string Pattern,
        RegexOptions Options,
        string? SimpleSearch,
        bool ExpectWordSeparators,
        bool IsMultiline,
        bool IsCaseSensitive);
}

public class TextModelSearchTests_RangeScopes
{
    [Fact]
    // Source: textModelSearch.test.ts – multi-range literal search coverage
    public void MultiRangeFindMatchesHonorsSelection()
    {
        TextModel model = new("alpha bravo\ncharlie delta\nalpha echo\n");
        Range[] ranges =
        [
            new Range(new TextPosition(1, 1), new TextPosition(1, 6)),
            new Range(new TextPosition(3, 1), new TextPosition(3, 6)),
        ];

        IReadOnlyList<FindMatch> matches = model.FindMatches(
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
    // Source: textModelSearch.test.ts – findNextMatch within selection ranges
    public void FindNextMatchWrapsWithinSelection()
    {
        TextModel model = new("one two three\none two three\n");
        Range[] ranges =
        [
            new Range(new TextPosition(1, 1), new TextPosition(1, 4)),
            new Range(new TextPosition(2, 1), new TextPosition(2, 4)),
        ];

        FindMatch? match1 = model.FindNextMatch(
            "one",
            new TextPosition(1, 1),
            ranges,
            findInSelection: true,
            isRegex: false,
            matchCase: true,
            wordSeparators: null);

        Assert.NotNull(match1);
        Assert.Equal(1, match1!.Range.StartLineNumber);

        FindMatch? match2 = model.FindNextMatch(
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
    // Source: textModelSearch.test.ts – findPreviousMatch within selection ranges
    public void FindPreviousMatchWrapsWithinSelection()
    {
        TextModel model = new("one two three\none two three\n");
        Range[] ranges =
        [
            new Range(new TextPosition(1, 1), new TextPosition(1, 4)),
            new Range(new TextPosition(2, 1), new TextPosition(2, 4)),
        ];

        FindMatch? match = model.FindPreviousMatch(
            "one",
            new TextPosition(2, 14),
            ranges,
            findInSelection: true,
            isRegex: false,
            matchCase: true,
            wordSeparators: null);

        Assert.NotNull(match);
        Assert.Equal(2, match!.Range.StartLineNumber);

        FindMatch? wrapped = model.FindPreviousMatch(
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

    [Fact]
    // Source: textModelSearch.test.ts – selection-scoped regex find
    public void MultiRangeRegexSearchFindsCafeWithinSelection()
    {
        TextModel model = new("caf\u00E9\ncaf\ncaf\u00E9\n");
        Range[] ranges =
        [
            new Range(new TextPosition(1, 1), new TextPosition(1, model.GetLineMaxColumn(1))),
            new Range(new TextPosition(3, 1), new TextPosition(3, model.GetLineMaxColumn(3))),
        ];

        IReadOnlyList<FindMatch> matches = model.FindMatches(
            "\\bcaf\\b",
            ranges,
            findInSelection: true,
            isRegex: true,
            matchCase: true,
            wordSeparators: null,
            captureMatches: false);

        Assert.Equal(2, matches.Count);
        Assert.Equal(1, matches[0].Range.StartLineNumber);
        Assert.Equal(3, matches[1].Range.StartLineNumber);
    }
}

public class TextModelSearchTests_WordBoundaries
{
    private static readonly string[] RegularText =
    [
        "This is some foo - bar text which contains foo and bar - as in Barcelona.",
        "Now it begins a word fooBar and now it is caps Foo-isn't this great?",
        "And here's a dull line with nothing interesting in it",
        "It is also interesting if it's part of a word like amazingFooBar",
        "Again nothing interesting here"
    ];

    private static readonly string RegularParagraph = string.Join('\n', RegularText);

    [Fact]
    // Source: textModelSearch.test.ts – "Simple find"
    public void SimpleFindMatchesTsMatrix()
    {
        AssertFindMatches(
            RegularParagraph,
            "foo",
            isRegex: false,
            matchCase: false,
            wordSeparators: null,
            R(1, 14, 1, 17),
            R(1, 44, 1, 47),
            R(2, 22, 2, 25),
            R(2, 48, 2, 51),
            R(4, 59, 4, 62));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "Case sensitive find"
    public void CaseSensitiveFindHonorsCasing()
    {
        AssertFindMatches(
            RegularParagraph,
            "foo",
            isRegex: false,
            matchCase: true,
            wordSeparators: null,
            R(1, 14, 1, 17),
            R(1, 44, 1, 47),
            R(2, 22, 2, 25));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "Whole words find"
    public void WholeWordFindUsesSeparators()
    {
        AssertFindMatches(
            RegularParagraph,
            "foo",
            isRegex: false,
            matchCase: false,
            wordSeparators: TextModelSearchTestHelper.UsualWordSeparators,
            R(1, 14, 1, 17),
            R(1, 44, 1, 47),
            R(2, 48, 2, 51));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "issue #3623"
    public void Issue3623_WholeWordMatchesNonLatin()
    {
        string text = string.Join('\n', "я", "компилятор", "обфускация", ":я-я");
        AssertFindMatches(
            text,
            "я",
            isRegex: false,
            matchCase: false,
            wordSeparators: TextModelSearchTestHelper.UsualWordSeparators,
            R(1, 1, 1, 2),
            R(4, 2, 4, 3),
            R(4, 4, 4, 5));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "issue #27459"
    public void Issue27459_WholeWordRegression()
    {
        string text = string.Join('\n',
            "this._register(this._textAreaInput.onKeyDown((e: IKeyboardEvent) => {",
            "       this._viewController.emitKeyDown(e);",
            "}));");

        AssertFindMatches(
            text,
            "((e: ",
            isRegex: false,
            matchCase: false,
            wordSeparators: TextModelSearchTestHelper.UsualWordSeparators,
            R(1, 45, 1, 50));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "issue #27594"
    public void Issue27594_SearchResultsPersist()
    {
        string text = "this.server.listen(0);";
        AssertFindMatches(
            text,
            "listen(",
            isRegex: false,
            matchCase: false,
            wordSeparators: TextModelSearchTestHelper.UsualWordSeparators,
            R(1, 13, 1, 20));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "issue #53415"
    public void Issue53415_WMatchesLineBreaks()
    {
        AssertFindMatches(
            string.Join('\n', "text", "180702-", "180703-180704"),
            "\\d{6}-\\W",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(2, 1, 3, 1));

        AssertFindMatches(
            string.Join('\n', "Just some text", string.Empty, "Just"),
            "\\W",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 5, 1, 6),
            R(1, 10, 1, 11),
            R(1, 15, 2, 1),
            R(2, 1, 3, 1));

        AssertFindMatches(
            string.Join("\r\n", "Just some text", string.Empty, "Just"),
            "\\W",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 5, 1, 6),
            R(1, 10, 1, 11),
            R(1, 15, 2, 1),
            R(2, 1, 3, 1));

        AssertFindMatches(
            string.Join('\n', "Just some text", "\tJust", "Just"),
            "\\W",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 5, 1, 6),
            R(1, 10, 1, 11),
            R(1, 15, 2, 1),
            R(2, 1, 2, 2),
            R(2, 6, 3, 1));

        AssertFindMatches(
            string.Join('\n', "Just  some text", string.Empty, "Just"),
            "\\W{2}",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 5, 1, 7),
            R(1, 16, 3, 1));

        AssertFindMatches(
            string.Join("\r\n", "Just  some text", string.Empty, "Just"),
            "\\W{2}",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 5, 1, 7),
            R(1, 16, 3, 1));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "Simple find using unicode escape sequences"
    public void SimpleFindUsingUnicodeEscapes()
    {
        AssertFindMatches(
            RegularParagraph,
            "\\u{0066}\\u006f\\u006F",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 14, 1, 17),
            R(1, 44, 1, 47),
            R(2, 22, 2, 25),
            R(2, 48, 2, 51),
            R(4, 59, 4, 62));
    }
}

public class TextModelSearchTests_MultilineRegex
{
    [Fact]
    // Source: textModelSearch.test.ts – "multiline find 1"
    public void MultilineFind_TextFollowedByNewline()
    {
        string text = string.Join('\n',
            "Just some text text",
            "Just some text text",
            "some text again",
            "again some text");

        AssertFindMatches(
            text,
            "text\\n",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 16, 2, 1),
            R(2, 16, 3, 1));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "multiline find 2"
    public void MultilineFind_TextFollowedByLiteral()
    {
        string text = string.Join('\n',
            "Just some text text",
            "Just some text text",
            "some text again",
            "again some text");

        AssertFindMatches(
            text,
            "text\\nJust",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 16, 2, 5));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "multiline find 3"
    public void MultilineFind_NewlineAgain()
    {
        string text = string.Join('\n',
            "Just some text text",
            "Just some text text",
            "some text again",
            "again some text");

        AssertFindMatches(
            text,
            "\\nagain",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(3, 16, 4, 6));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "multiline find 4"
    public void MultilineFind_MatchesAcrossThreeLines()
    {
        string text = string.Join('\n',
            "Just some text text",
            "Just some text text",
            "some text again",
            "again some text");

        AssertFindMatches(
            text,
            ".*\\nJust.*\\n",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 1, 3, 1));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "multiline find with line beginning regex"
    public void MultilineFind_WithLineBeginningRegex()
    {
        string text = string.Join('\n', "if", "else", string.Empty, "if", "else");
        AssertFindMatches(
            text,
            "^if\\nelse",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 1, 2, 5),
            R(4, 1, 5, 5));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "matching empty lines using boundary expression"
    public void BoundaryExpressionMatchesEmptyLines()
    {
        string text = string.Join('\n', "if", string.Empty, "else", "  ", "if", " ", "else");
        AssertFindMatches(
            text,
            "^\\s*$\\n",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(2, 1, 3, 1),
            R(4, 1, 5, 1),
            R(6, 1, 7, 1));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "matching lines starting with A and ending with B"
    public void RegexMatchesLinesStartingAndEnding()
    {
        string text = string.Join('\n', "a if b", "a", "ab", "eb");
        AssertFindMatches(
            text,
            "^a.*b$",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 1, 1, 7),
            R(3, 1, 3, 3));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "multiline find with line ending regex"
    public void MultilineFind_WithLineEndingRegex()
    {
        string text = string.Join('\n', "if", "else", string.Empty, "if", "elseif", "else");
        AssertFindMatches(
            text,
            "if\\nelse$",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 1, 2, 5),
            R(5, 5, 6, 5));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "issue #4836 - ^.*$"
    public void Issue4836_CaretDotStarMatchesEmptyLines()
    {
        string text = string.Join('\n',
            "Just some text text",
            string.Empty,
            "some text again",
            string.Empty,
            "again some text");

        AssertFindMatches(
            text,
            "^.*$",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 1, 1, 20),
            R(2, 1, 2, 1),
            R(3, 1, 3, 16),
            R(4, 1, 4, 1),
            R(5, 1, 5, 16));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "multiline find for non-regex string"
    public void MultilineFindForLiteralString()
    {
        string text = string.Join('\n',
            "Just some text text",
            "some text text",
            "some text again",
            "again some text",
            "but not some");

        AssertFindMatches(
            text,
            "text\nsome",
            isRegex: false,
            matchCase: false,
            wordSeparators: null,
            R(1, 16, 2, 5),
            R(2, 11, 3, 5));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "\\n matches \\r\\n" / "\\r can never be found"
    public void NewlineEscapeMatchesWhileCarriageReturnDoesNot()
    {
        string text = string.Join("\r\n", new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i" });
        TextModel model = new(text);
        Assert.Equal("\r\n", model.Eol);

        SearchParams searchParams = new("h\\n", isRegex: true, matchCase: false, wordSeparators: null);
        IReadOnlyList<FindMatch> matches = TextModelSearchTestHelper.GetMatches(model, searchParams, captureMatches: true);
        Assert.Single(matches);
        TextModelSearchTestHelper.AssertMatch(matches[0], R(8, 1, 9, 1), "h\n");

        searchParams = new SearchParams("g\\nh\\n", isRegex: true, matchCase: false, wordSeparators: null);
        matches = TextModelSearchTestHelper.GetMatches(model, searchParams, captureMatches: true);
        Assert.Single(matches);
        TextModelSearchTestHelper.AssertMatch(matches[0], R(7, 1, 9, 1), "g\nh\n");

        searchParams = new SearchParams("\\ni", isRegex: true, matchCase: false, wordSeparators: null);
        matches = TextModelSearchTestHelper.GetMatches(model, searchParams, captureMatches: true);
        Assert.Single(matches);
        TextModelSearchTestHelper.AssertMatch(matches[0], R(8, 2, 9, 2), "\ni");

        SearchParams noMatchParams = new("\\r\\n", isRegex: true, matchCase: false, wordSeparators: null);
        Assert.Empty(TextModelSearchTestHelper.GetMatches(model, noMatchParams, captureMatches: true));
        Assert.Null(model.FindNextMatch(noMatchParams, new TextPosition(1, 1), captureMatches: true));
    }
}

public class TextModelSearchTests_CaptureNavigation
{
    private const string Sample = "one line line\ntwo line\nthree";

    [Fact]
    // Source: textModelSearch.test.ts – "findMatches with capturing matches"
    public void FindMatchesCapturingGroups()
    {
        TextModel model = new(Sample);
        SearchParams searchParams = new("(l(in)e)", isRegex: true, matchCase: false, wordSeparators: null);
        IReadOnlyList<FindMatch> matches = TextModelSearchTestHelper.GetMatches(model, searchParams, captureMatches: true);

        Assert.Equal(3, matches.Count);
        TextModelSearchTestHelper.AssertMatch(matches[0], R(1, 5, 1, 9), "line", "line", "in");
        TextModelSearchTestHelper.AssertMatch(matches[1], R(1, 10, 1, 14), "line", "line", "in");
        TextModelSearchTestHelper.AssertMatch(matches[2], R(2, 5, 2, 9), "line", "line", "in");
    }

    [Fact]
    // Source: textModelSearch.test.ts – "findMatches multiline with capturing matches"
    public void FindMatchesMultilineCapturing()
    {
        TextModel model = new(Sample);
        SearchParams searchParams = new("(l(in)e)\\n", isRegex: true, matchCase: false, wordSeparators: null);
        IReadOnlyList<FindMatch> matches = TextModelSearchTestHelper.GetMatches(model, searchParams, captureMatches: true);

        Assert.Equal(2, matches.Count);
        TextModelSearchTestHelper.AssertMatch(matches[0], R(1, 10, 2, 1), "line\n", "line", "in");
        TextModelSearchTestHelper.AssertMatch(matches[1], R(2, 5, 3, 1), "line\n", "line", "in");
    }

    [Fact]
    // Source: textModelSearch.test.ts – "findNextMatch with capturing matches"
    public void FindNextMatchReturnsCaptures()
    {
        TextModel model = new(Sample);
        SearchParams searchParams = new("(l(in)e)", isRegex: true, matchCase: false, wordSeparators: null);
        FindMatch? match = model.FindNextMatch(searchParams, new TextPosition(1, 1), captureMatches: true);
        TextModelSearchTestHelper.AssertMatch(match, R(1, 5, 1, 9), "line", "line", "in");
    }

    [Fact]
    // Source: textModelSearch.test.ts – "findNextMatch multiline with capturing matches"
    public void FindNextMatchMultilineReturnsCaptures()
    {
        TextModel model = new(Sample);
        SearchParams searchParams = new("(l(in)e)\\n", isRegex: true, matchCase: false, wordSeparators: null);
        FindMatch? match = model.FindNextMatch(searchParams, new TextPosition(1, 1), captureMatches: true);
        TextModelSearchTestHelper.AssertMatch(match, R(1, 10, 2, 1), "line\n", "line", "in");
    }

    [Fact]
    // Source: textModelSearch.test.ts – "findPreviousMatch with capturing matches"
    public void FindPreviousMatchReturnsCaptures()
    {
        TextModel model = new(Sample);
        SearchParams searchParams = new("(l(in)e)", isRegex: true, matchCase: false, wordSeparators: null);
        FindMatch? match = model.FindPreviousMatch(searchParams, new TextPosition(1, 1), captureMatches: true);
        TextModelSearchTestHelper.AssertMatch(match, R(2, 5, 2, 9), "line", "line", "in");
    }

    [Fact]
    // Source: textModelSearch.test.ts – "findPreviousMatch multiline with capturing matches"
    public void FindPreviousMatchMultilineReturnsCaptures()
    {
        TextModel model = new(Sample);
        SearchParams searchParams = new("(l(in)e)\\n", isRegex: true, matchCase: false, wordSeparators: null);
        FindMatch? match = model.FindPreviousMatch(searchParams, new TextPosition(1, 1), captureMatches: true);
        TextModelSearchTestHelper.AssertMatch(match, R(2, 5, 3, 1), "line\n", "line", "in");
    }
}

public class TextModelSearchTests_ZeroWidthAndUnicode
{
    private static readonly string[] RegularText =
    [
        "This is some foo - bar text which contains foo and bar - as in Barcelona.",
        "Now it begins a word fooBar and now it is caps Foo-isn't this great?",
        "And here's a dull line with nothing interesting in it",
        "It is also interesting if it's part of a word like amazingFooBar",
        "Again nothing interesting here"
    ];

    [Fact]
    // Source: textModelSearch.test.ts – "/^/ find"
    public void CaretAnchorMatchesEveryLine()
    {
        AssertFindMatches(
            string.Join('\n', RegularText),
            "^",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 1, 1, 1),
            R(2, 1, 2, 1),
            R(3, 1, 3, 1),
            R(4, 1, 4, 1),
            R(5, 1, 5, 1));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "/$/ find"
    public void DollarAnchorMatchesLineEnds()
    {
        AssertFindMatches(
            string.Join('\n', RegularText),
            "$",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 74, 1, 74),
            R(2, 69, 2, 69),
            R(3, 54, 3, 54),
            R(4, 65, 4, 65),
            R(5, 31, 5, 31));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "/.*/ find"
    public void DotStarMatchesWholeLine()
    {
        AssertFindMatches(
            string.Join('\n', RegularText),
            ".*",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 1, 1, 74),
            R(2, 1, 2, 69),
            R(3, 1, 3, 54),
            R(4, 1, 4, 65),
            R(5, 1, 5, 31));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "/^$/ find"
    public void CaretDollarMatchesEmptyLines()
    {
        string text = string.Join('\n',
            "This is some foo - bar text which contains foo and bar - as in Barcelona.",
            string.Empty,
            "And here's a dull line with nothing interesting in it",
            string.Empty,
            "Again nothing interesting here");

        AssertFindMatches(
            text,
            "^$",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(2, 1, 2, 1),
            R(4, 1, 4, 1));
    }

    [Fact]
    // Source: textModelSearch.test.ts – "issue #74715"
    public void Issue74715_DigitStarAdvances()
    {
        TextModel model = new("10.243.30.10");
        SearchParams searchParams = new("\\d*", isRegex: true, matchCase: false, wordSeparators: null);
        IReadOnlyList<FindMatch> matches = TextModelSearchTestHelper.GetMatches(model, searchParams, captureMatches: true);

        List<(Range Range, string Capture)> expected =
        [
            (R(1, 1, 1, 3), "10"),
            (R(1, 3, 1, 3), string.Empty),
            (R(1, 4, 1, 7), "243"),
            (R(1, 7, 1, 7), string.Empty),
            (R(1, 8, 1, 10), "30"),
            (R(1, 10, 1, 10), string.Empty),
            (R(1, 11, 1, 13), "10")
        ];

        Assert.Equal(expected.Count, matches.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            TextModelSearchTestHelper.AssertMatch(matches[i], expected[i].Range, expected[i].Capture);
        }
    }

    [Fact]
    // Source: textModelSearch.test.ts – "issue #100134"
    public void Issue100134_ZeroLengthMatchesSkipSurrogates()
    {
        AssertFindMatches(
            "1\uD83D\uDCBB1",
            "()",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 1, 1, 1),
            R(1, 2, 1, 2),
            R(1, 4, 1, 4),
            R(1, 5, 1, 5));

        AssertFindMatches(
            "1\uD83D\uDC31\u200D\uD83D\uDCBB1",
            "()",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            R(1, 1, 1, 1),
            R(1, 2, 1, 2),
            R(1, 4, 1, 4),
            R(1, 5, 1, 5),
            R(1, 7, 1, 7),
            R(1, 8, 1, 8));
    }
}

public class TextModelSearchTests_ParseSearchRequest
{
    private const string LiteralSlashN = @"foo\n";
    private const string LiteralDoubleSlashN = @"foo\\n";
    private const string LiteralSlashR = @"foo\r";
    private const string LiteralDoubleSlashR = @"foo\\r";

    [Fact]
    public void ParseSearchRequest_Invalid()
    {
        AssertParseSearchResult(string.Empty, isRegex: true, matchCase: true, wordSeparators: UsualWordSeparators, expected: null);
        AssertParseSearchResult("(", isRegex: true, matchCase: false, wordSeparators: null, expected: null);
    }

    [Fact]
    public void ParseSearchRequest_NonRegex()
    {
        RegexOptions ignoreCase = TextModelSearchTestHelper.DefaultRegexOptions | RegexOptions.IgnoreCase;
        RegexOptions caseSensitive = TextModelSearchTestHelper.DefaultRegexOptions;

        AssertParseSearchResult(
            "foo",
            isRegex: false,
            matchCase: false,
            wordSeparators: null,
            expected: Expect("foo", ignoreCase, simpleSearch: null, expectWordSeparators: false, isMultiline: false, isCaseSensitive: false));

        AssertParseSearchResult(
            "foo",
            isRegex: false,
            matchCase: false,
            wordSeparators: UsualWordSeparators,
            expected: Expect("foo", ignoreCase, simpleSearch: null, expectWordSeparators: true, isMultiline: false, isCaseSensitive: false));

        AssertParseSearchResult(
            "foo",
            isRegex: false,
            matchCase: true,
            wordSeparators: null,
            expected: Expect("foo", caseSensitive, simpleSearch: "foo", expectWordSeparators: false, isMultiline: false, isCaseSensitive: true));

        AssertParseSearchResult(
            "foo",
            isRegex: false,
            matchCase: true,
            wordSeparators: UsualWordSeparators,
            expected: Expect("foo", caseSensitive, simpleSearch: "foo", expectWordSeparators: true, isMultiline: false, isCaseSensitive: true));

        AssertParseSearchResult(
            LiteralSlashN,
            isRegex: false,
            matchCase: false,
            wordSeparators: null,
            expected: Expect(Regex.Escape(LiteralSlashN), ignoreCase, simpleSearch: null, expectWordSeparators: false, isMultiline: false, isCaseSensitive: false));

        AssertParseSearchResult(
            LiteralDoubleSlashN,
            isRegex: false,
            matchCase: false,
            wordSeparators: null,
            expected: Expect(Regex.Escape(LiteralDoubleSlashN), ignoreCase, simpleSearch: null, expectWordSeparators: false, isMultiline: false, isCaseSensitive: false));

        AssertParseSearchResult(
            LiteralSlashR,
            isRegex: false,
            matchCase: false,
            wordSeparators: null,
            expected: Expect(Regex.Escape(LiteralSlashR), ignoreCase, simpleSearch: null, expectWordSeparators: false, isMultiline: false, isCaseSensitive: false));

        AssertParseSearchResult(
            LiteralDoubleSlashR,
            isRegex: false,
            matchCase: false,
            wordSeparators: null,
            expected: Expect(Regex.Escape(LiteralDoubleSlashR), ignoreCase, simpleSearch: null, expectWordSeparators: false, isMultiline: false, isCaseSensitive: false));
    }

    [Fact]
    public void ParseSearchRequest_Regex()
    {
        RegexOptions ignoreCase = TextModelSearchTestHelper.DefaultRegexOptions | RegexOptions.IgnoreCase;
        RegexOptions caseSensitive = TextModelSearchTestHelper.DefaultRegexOptions;
        RegexOptions ignoreCaseMultiline = ignoreCase | RegexOptions.Multiline;
        RegexOptions caseSensitiveMultiline = caseSensitive | RegexOptions.Multiline;

        AssertParseSearchResult(
            "foo",
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            expected: Expect("foo", ignoreCase, simpleSearch: null, expectWordSeparators: false, isMultiline: false, isCaseSensitive: false));

        AssertParseSearchResult(
            "foo",
            isRegex: true,
            matchCase: false,
            wordSeparators: UsualWordSeparators,
            expected: Expect("foo", ignoreCase, simpleSearch: null, expectWordSeparators: true, isMultiline: false, isCaseSensitive: false));

        AssertParseSearchResult(
            "foo",
            isRegex: true,
            matchCase: true,
            wordSeparators: null,
            expected: Expect("foo", caseSensitive, simpleSearch: null, expectWordSeparators: false, isMultiline: false, isCaseSensitive: true));

        AssertParseSearchResult(
            "foo",
            isRegex: true,
            matchCase: true,
            wordSeparators: UsualWordSeparators,
            expected: Expect("foo", caseSensitive, simpleSearch: null, expectWordSeparators: true, isMultiline: false, isCaseSensitive: true));

        AssertParseSearchResult(
            LiteralSlashN,
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            expected: Expect(LiteralSlashN, ignoreCaseMultiline, simpleSearch: null, expectWordSeparators: false, isMultiline: true, isCaseSensitive: false));

        AssertParseSearchResult(
            LiteralSlashN,
            isRegex: true,
            matchCase: true,
            wordSeparators: null,
            expected: Expect(LiteralSlashN, caseSensitiveMultiline, simpleSearch: null, expectWordSeparators: false, isMultiline: true, isCaseSensitive: true));

        AssertParseSearchResult(
            LiteralDoubleSlashN,
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            expected: Expect(LiteralDoubleSlashN, ignoreCase, simpleSearch: null, expectWordSeparators: false, isMultiline: false, isCaseSensitive: false));

        AssertParseSearchResult(
            LiteralSlashR,
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            expected: Expect(LiteralSlashR, ignoreCaseMultiline, simpleSearch: null, expectWordSeparators: false, isMultiline: true, isCaseSensitive: false));

        AssertParseSearchResult(
            LiteralSlashR,
            isRegex: true,
            matchCase: true,
            wordSeparators: null,
            expected: Expect(LiteralSlashR, caseSensitiveMultiline, simpleSearch: null, expectWordSeparators: false, isMultiline: true, isCaseSensitive: true));

        AssertParseSearchResult(
            LiteralDoubleSlashR,
            isRegex: true,
            matchCase: false,
            wordSeparators: null,
            expected: Expect(LiteralDoubleSlashR, ignoreCase, simpleSearch: null, expectWordSeparators: false, isMultiline: false, isCaseSensitive: false));
    }

    private static TextModelSearchTestHelper.ExpectedSearchData Expect(
        string pattern,
        RegexOptions options,
        string? simpleSearch,
        bool expectWordSeparators,
        bool isMultiline,
        bool isCaseSensitive)
        => new(pattern, options, simpleSearch, expectWordSeparators, isMultiline, isCaseSensitive);
}

public class TextModelSearchTests_IsMultilineRegexSource
{
    [Fact]
    public void IsMultilineRegexSource_BasicMatrix()
    {
        Assert.False(SearchPatternUtilities.IsMultilineRegexSource("foo"));
        Assert.False(SearchPatternUtilities.IsMultilineRegexSource(string.Empty));
        Assert.False(SearchPatternUtilities.IsMultilineRegexSource(@"foo\sbar"));
        Assert.False(SearchPatternUtilities.IsMultilineRegexSource(@"\\notnewline"));

        Assert.True(SearchPatternUtilities.IsMultilineRegexSource(@"foo\nbar"));
        Assert.True(SearchPatternUtilities.IsMultilineRegexSource(@"foo\nbar\s"));
        Assert.True(SearchPatternUtilities.IsMultilineRegexSource(@"foo\r\n"));
        Assert.True(SearchPatternUtilities.IsMultilineRegexSource(@"\n"));
        Assert.True(SearchPatternUtilities.IsMultilineRegexSource(@"foo\W"));
        Assert.True(SearchPatternUtilities.IsMultilineRegexSource("foo\n"));
        Assert.True(SearchPatternUtilities.IsMultilineRegexSource("foo\r\n"));
    }

    [Fact]
    public void IsMultilineRegexSource_DifferentiatesPatternSets()
    {
        string[] singleLinePatterns =
        [
            @"MARK:\s*(?<label>.*)$",
            @"^// Header$",
            @"\s*[-=]+\s*",
        ];

        string[] multiLinePatterns =
        [
            @"^// =+\n^// (?<label>[^\n]+?)\n^// =+$",
            @"header\r\nfooter",
            @"start\r|\nend",
            "top\nmiddle\r\nbottom"
        ];

        foreach (string pattern in singleLinePatterns)
        {
            Assert.False(SearchPatternUtilities.IsMultilineRegexSource(pattern));
        }

        foreach (string pattern in multiLinePatterns)
        {
            Assert.True(SearchPatternUtilities.IsMultilineRegexSource(pattern));
        }
    }
}

public class TextModelSearchTests_FindNextMatchNavigation
{
    [Fact]
    public void FindNextMatchWithoutRegex()
    {
        TextModel model = new("line line one\nline two\nthree");
        SearchParams searchParams = new("line", isRegex: false, matchCase: false, wordSeparators: null);

        FindMatch? match = model.FindNextMatch(searchParams, new TextPosition(1, 1), captureMatches: false);
        AssertMatch(match, R(1, 1, 1, 5));

        match = model.FindNextMatch(searchParams, new TextPosition(match!.Range.EndLineNumber, match.Range.EndColumn), captureMatches: false);
        AssertMatch(match, R(1, 6, 1, 10));

        match = model.FindNextMatch(searchParams, new TextPosition(1, 3), captureMatches: false);
        AssertMatch(match, R(1, 6, 1, 10));

        match = model.FindNextMatch(searchParams, new TextPosition(match!.Range.EndLineNumber, match.Range.EndColumn), captureMatches: false);
        AssertMatch(match, R(2, 1, 2, 5));
    }

    [Fact]
    public void FindNextMatchWithBeginningBoundaryRegex()
    {
        TextModel model = new("line one\nline two\nthree");
        SearchParams searchParams = new("^line", isRegex: true, matchCase: false, wordSeparators: null);

        FindMatch? match = model.FindNextMatch(searchParams, new TextPosition(1, 1), captureMatches: false);
        AssertMatch(match, R(1, 1, 1, 5));

        match = model.FindNextMatch(searchParams, new TextPosition(match!.Range.EndLineNumber, match.Range.EndColumn), captureMatches: false);
        AssertMatch(match, R(2, 1, 2, 5));

        match = model.FindNextMatch(searchParams, new TextPosition(1, 3), captureMatches: false);
        AssertMatch(match, R(2, 1, 2, 5));

        match = model.FindNextMatch(searchParams, new TextPosition(match!.Range.EndLineNumber, match.Range.EndColumn), captureMatches: false);
        AssertMatch(match, R(1, 1, 1, 5));
    }

    [Fact]
    public void FindNextMatchWithRepeatedPrefixes()
    {
        TextModel model = new("line line one\nline two\nthree");
        SearchParams searchParams = new("^line", isRegex: true, matchCase: false, wordSeparators: null);

        FindMatch? match = model.FindNextMatch(searchParams, new TextPosition(1, 1), captureMatches: false);
        AssertMatch(match, R(1, 1, 1, 5));

        match = model.FindNextMatch(searchParams, new TextPosition(match!.Range.EndLineNumber, match.Range.EndColumn), captureMatches: false);
        AssertMatch(match, R(2, 1, 2, 5));

        match = model.FindNextMatch(searchParams, new TextPosition(1, 3), captureMatches: false);
        AssertMatch(match, R(2, 1, 2, 5));

        match = model.FindNextMatch(searchParams, new TextPosition(match!.Range.EndLineNumber, match.Range.EndColumn), captureMatches: false);
        AssertMatch(match, R(1, 1, 1, 5));
    }

    [Fact]
    public void FindNextMatchWithMultilineBeginningRegex()
    {
        TextModel model = new("line line one\nline two\nline three\nline four");
        SearchParams searchParams = new("^line.*\\nline", isRegex: true, matchCase: false, wordSeparators: null);

        FindMatch? match = model.FindNextMatch(searchParams, new TextPosition(1, 1), captureMatches: false);
        AssertMatch(match, R(1, 1, 2, 5));

        match = model.FindNextMatch(searchParams, new TextPosition(match!.Range.EndLineNumber, match.Range.EndColumn), captureMatches: false);
        AssertMatch(match, R(3, 1, 4, 5));

        match = model.FindNextMatch(searchParams, new TextPosition(2, 1), captureMatches: false);
        AssertMatch(match, R(2, 1, 3, 5));
    }

    [Fact]
    public void FindNextMatchWithLineEndingRegex()
    {
        const string sample = "one line line\ntwo line\nthree";
        TextModel model = new(sample);
        SearchParams searchParams = new("line$", isRegex: true, matchCase: false, wordSeparators: null);

        FindMatch? match = model.FindNextMatch(searchParams, new TextPosition(1, 1), captureMatches: false);
        AssertMatch(match, R(1, 10, 1, 14));

        match = model.FindNextMatch(searchParams, new TextPosition(1, 4), captureMatches: false);
        AssertMatch(match, R(1, 10, 1, 14));

        match = model.FindNextMatch(searchParams, new TextPosition(match!.Range.EndLineNumber, match.Range.EndColumn), captureMatches: false);
        AssertMatch(match, R(2, 5, 2, 9));

        match = model.FindNextMatch(searchParams, new TextPosition(match!.Range.EndLineNumber, match.Range.EndColumn), captureMatches: false);
        AssertMatch(match, R(1, 10, 1, 14));
    }
}
