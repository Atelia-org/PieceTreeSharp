// WS5-PORT: Shared Test Harness - WordTestUtils
// Purpose: Word boundary test data generation and word operation verification
// Source Reference: ts/src/vs/editor/contrib/wordOperations/test/browser/wordTestUtils.ts
// Created: 2025-11-26

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Cursor;

namespace PieceTree.TextBuffer.Tests.Helpers;

/// <summary>
/// Utilities for testing word boundary detection and word operations.
/// Provides test data constants and helper methods for word movement testing.
/// </summary>
public static class WordTestUtils
{
    #region Test Data Constants

    /// <summary>
    /// ASCII word test cases with expected boundaries.
    /// </summary>
    public static class AsciiTestCases
    {
        public const string SimpleWords = "hello world foo bar";
        public const string WithNumbers = "hello123 world456";
        public const string WithPunctuation = "hello, world! foo-bar";
        public const string WithUnderscores = "hello_world foo_bar_baz";
        public const string MixedSeparators = "hello-world_foo.bar";
        public const string MultipleSpaces = "hello   world";
        public const string LeadingSpaces = "   hello world";
        public const string TrailingSpaces = "hello world   ";
        public const string EmptyString = "";
        public const string SingleWord = "hello";
        public const string SingleChar = "x";
        public const string OnlySpaces = "     ";
        public const string OnlyPunctuation = "...!!!";
        public const string Tabs = "hello\tworld";
        public const string TabsAndSpaces = "hello \t world";
    }

    /// <summary>
    /// CamelCase test cases with expected word boundaries.
    /// </summary>
    public static class CamelCaseTestCases
    {
        public const string SimpleCamelCase = "helloWorld";
        public const string MultipleCamelCase = "helloWorldFooBar";
        public const string WithAcronym = "XMLParser";
        public const string AcronymAtEnd = "parseXML";
        public const string MixedCaseNumbers = "hello123World";
        public const string AllCaps = "HELLO";
        public const string AllLower = "hello";
        public const string SingleUpperStart = "Hello";
        public const string ConsecutiveCaps = "HTTPServer";
        public const string CapsInMiddle = "getHTTPServer";
        public const string UnderscoreAndCamel = "hello_worldFooBar";
    }

    /// <summary>
    /// Emoji test cases for Unicode handling.
    /// </summary>
    public static class EmojiTestCases
    {
        public const string SimpleEmoji = "hello 👋 world";
        public const string MultipleEmoji = "👋 hello 🌍 world 🎉";
        public const string EmojiSequence = "👨‍👩‍👧‍👦 family";
        public const string FlagEmoji = "🇺🇸 flag";
        public const string SkinToneEmoji = "👋🏽 wave";
        public const string EmojiAtStart = "🎉hello";
        public const string EmojiAtEnd = "hello🎉";
        public const string OnlyEmoji = "🎉🎊🎈";
    }

    /// <summary>
    /// CJK (Chinese, Japanese, Korean) test cases.
    /// </summary>
    public static class CJKTestCases
    {
        public const string ChineseSimple = "你好 世界";
        public const string ChineseWithEnglish = "hello 你好 world";
        public const string Japanese = "こんにちは 世界";
        public const string JapaneseKatakana = "コンニチハ ワールド";
        public const string JapaneseMixed = "Hello こんにちは World";
        public const string Korean = "안녕하세요 세계";
        public const string KoreanWithEnglish = "hello 안녕 world";
        public const string MixedCJK = "你好こんにちは안녕";
    }

    /// <summary>
    /// Special character and edge case test strings.
    /// </summary>
    public static class SpecialCases
    {
        public const string Brackets = "hello(world)foo[bar]";
        public const string Quotes = "hello\"world\"foo'bar'";
        public const string Operators = "x + y * z";
        public const string CodeLike = "function test() { return x; }";
        public const string Path = "/path/to/file.txt";
        public const string Url = "https://example.com/path";
        public const string Email = "user@example.com";
        public const string SnakeCase = "hello_world_foo_bar";
        public const string KebabCase = "hello-world-foo-bar";
        public const string DotCase = "hello.world.foo.bar";
    }

    #endregion

    #region Word Boundary Detection

    /// <summary>
    /// Get all word boundaries in the given content using specified separators.
    /// Returns list of column positions (1-based) where word boundaries occur.
    /// </summary>
    public static List<int> GetWordBoundaries(string lineContent, string wordSeparators)
    {
        var boundaries = new List<int> { 1 }; // Start of line is always a boundary

        if (string.IsNullOrEmpty(lineContent))
        {
            return boundaries;
        }

        var separatorSet = new HashSet<char>(wordSeparators ?? string.Empty);
        var prevCharType = GetCharType(lineContent[0], separatorSet);

        for (int i = 1; i < lineContent.Length; i++)
        {
            var currentCharType = GetCharType(lineContent[i], separatorSet);
            if (currentCharType != prevCharType)
            {
                boundaries.Add(i + 1); // 1-based column
            }
            prevCharType = currentCharType;
        }

        boundaries.Add(lineContent.Length + 1); // End of line is always a boundary
        return boundaries;
    }

    /// <summary>
    /// Get word start positions in the given content.
    /// </summary>
    public static List<int> GetWordStarts(string lineContent, string wordSeparators)
    {
        var starts = new List<int>();

        if (string.IsNullOrEmpty(lineContent))
        {
            return starts;
        }

        var separatorSet = new HashSet<char>(wordSeparators ?? string.Empty);
        
        // First non-separator char at start is a word start
        if (!IsSeparatorOrWhitespace(lineContent[0], separatorSet))
        {
            starts.Add(1);
        }

        for (int i = 1; i < lineContent.Length; i++)
        {
            var prevIsSeparator = IsSeparatorOrWhitespace(lineContent[i - 1], separatorSet);
            var currentIsSeparator = IsSeparatorOrWhitespace(lineContent[i], separatorSet);
            
            if (prevIsSeparator && !currentIsSeparator)
            {
                starts.Add(i + 1); // 1-based column
            }
        }

        return starts;
    }

    /// <summary>
    /// Get word end positions in the given content.
    /// </summary>
    public static List<int> GetWordEnds(string lineContent, string wordSeparators)
    {
        var ends = new List<int>();

        if (string.IsNullOrEmpty(lineContent))
        {
            return ends;
        }

        var separatorSet = new HashSet<char>(wordSeparators ?? string.Empty);

        for (int i = 0; i < lineContent.Length - 1; i++)
        {
            var currentIsSeparator = IsSeparatorOrWhitespace(lineContent[i], separatorSet);
            var nextIsSeparator = IsSeparatorOrWhitespace(lineContent[i + 1], separatorSet);
            
            if (!currentIsSeparator && nextIsSeparator)
            {
                ends.Add(i + 2); // 1-based column, position after the word
            }
        }

        // Last char ends a word if it's not a separator
        if (!IsSeparatorOrWhitespace(lineContent[lineContent.Length - 1], separatorSet))
        {
            ends.Add(lineContent.Length + 1);
        }

        return ends;
    }

    private enum CharType
    {
        Whitespace,
        Separator,
        Regular
    }

    private static CharType GetCharType(char c, HashSet<char> separators)
    {
        if (char.IsWhiteSpace(c))
        {
            return CharType.Whitespace;
        }
        if (separators.Contains(c))
        {
            return CharType.Separator;
        }
        return CharType.Regular;
    }

    private static bool IsSeparatorOrWhitespace(char c, HashSet<char> separators)
    {
        return char.IsWhiteSpace(c) || separators.Contains(c);
    }

    #endregion

    #region Word Operation Verification

    /// <summary>
    /// Verify word-left movement produces expected sequence of positions.
    /// </summary>
    public static void VerifyWordLeftSequence(
        TextModel model,
        TextPosition startPosition,
        string wordSeparators,
        params (int line, int column)[] expectedPositions)
    {
        var current = startPosition;
        var positions = new List<TextPosition> { current };

        foreach (var expected in expectedPositions)
        {
            current = WordOperations.MoveWordLeft(model, current, wordSeparators);
            positions.Add(current);
            CursorTestHelper.AssertPosition(current, expected.line, expected.column, 
                $"Word-left from ({positions[^2].LineNumber},{positions[^2].Column})");
        }
    }

    /// <summary>
    /// Verify word-right movement produces expected sequence of positions.
    /// </summary>
    public static void VerifyWordRightSequence(
        TextModel model,
        TextPosition startPosition,
        string wordSeparators,
        params (int line, int column)[] expectedPositions)
    {
        var current = startPosition;
        var positions = new List<TextPosition> { current };

        foreach (var expected in expectedPositions)
        {
            current = WordOperations.MoveWordRight(model, current, wordSeparators);
            positions.Add(current);
            CursorTestHelper.AssertPosition(current, expected.line, expected.column,
                $"Word-right from ({positions[^2].LineNumber},{positions[^2].Column})");
        }
    }

    /// <summary>
    /// Create a test case for word operations.
    /// </summary>
    public static WordOperationTestCase CreateTestCase(
        string content,
        int startLine,
        int startColumn,
        (int line, int column)[] expectedWordLeftPositions,
        (int line, int column)[] expectedWordRightPositions,
        string? wordSeparators = null)
    {
        return new WordOperationTestCase
        {
            Content = content,
            StartPosition = new TextPosition(startLine, startColumn),
            ExpectedWordLeftPositions = expectedWordLeftPositions,
            ExpectedWordRightPositions = expectedWordRightPositions,
            WordSeparators = wordSeparators ?? CursorConfiguration.DefaultWordSeparators
        };
    }

    #endregion

    #region Theory Data Generation

    /// <summary>
    /// Generate test data for word boundary tests.
    /// </summary>
    public static IEnumerable<object[]> GenerateWordBoundaryTestData()
    {
        yield return new object[] { AsciiTestCases.SimpleWords, CursorConfiguration.DefaultWordSeparators };
        yield return new object[] { AsciiTestCases.WithPunctuation, CursorConfiguration.DefaultWordSeparators };
        yield return new object[] { AsciiTestCases.WithUnderscores, CursorConfiguration.DefaultWordSeparators };
        yield return new object[] { CamelCaseTestCases.SimpleCamelCase, CursorConfiguration.DefaultWordSeparators };
        yield return new object[] { CamelCaseTestCases.WithAcronym, CursorConfiguration.DefaultWordSeparators };
    }

    /// <summary>
    /// Generate test data for visible column calculations with tabs.
    /// This mirrors the TS whitespaceVisibleColumn test data.
    /// </summary>
    public static IEnumerable<object[]> GenerateVisibleColumnTestData()
    {
        // Format: lineContent, tabSize, column, expectedVisibleColumn
        yield return new object[] { "        ", 4, 1, 0 };  // 8 spaces, col 1
        yield return new object[] { "        ", 4, 5, 4 };  // 8 spaces, col 5
        yield return new object[] { "        ", 4, 9, 8 };  // 8 spaces, col 9 (past end)
        yield return new object[] { "\t\t", 4, 1, 0 };      // 2 tabs, col 1
        yield return new object[] { "\t\t", 4, 2, 4 };      // 2 tabs, col 2 (after first tab)
        yield return new object[] { "\t\t", 4, 3, 8 };      // 2 tabs, col 3 (after both tabs)
        yield return new object[] { "  \t", 4, 1, 0 };      // 2 spaces + tab, col 1
        yield return new object[] { "  \t", 4, 2, 1 };      // 2 spaces + tab, col 2
        yield return new object[] { "  \t", 4, 3, 2 };      // 2 spaces + tab, col 3
        yield return new object[] { "  \t", 4, 4, 4 };      // 2 spaces + tab, col 4 (after tab)
        yield return new object[] { "\thello", 4, 1, 0 };   // tab + hello, col 1
        yield return new object[] { "\thello", 4, 2, 4 };   // tab + hello, col 2
        yield return new object[] { "\thello", 4, 3, 5 };   // tab + hello, col 3
    }

    /// <summary>
    /// Generate test data for atomic tab stop positions.
    /// This mirrors the TS atomicPosition test data.
    /// </summary>
    public static IEnumerable<object[]> GenerateAtomicPositionTestData()
    {
        // Format: lineContent, tabSize, direction, positions
        // Direction: -1 = left, 0 = nearest, 1 = right
        yield return new object[] { "        ", 4, -1, new int[] { -1, 0, 0, 0, 0, 4, 4, 4, 4, -1 } };
        yield return new object[] { "        ", 4, 1, new int[] { 4, 4, 4, 4, 8, 8, 8, 8, -1 } };
        yield return new object[] { "\t\t", 4, -1, new int[] { -1, 0, 4, -1 } };
        yield return new object[] { "\t\t", 4, 1, new int[] { 4, 8, -1 } };
    }

    #endregion

    #region Pipe Position Helpers (Migrated from TS wordTestUtils.ts)

    /// <summary>
    /// Deserialize pipe positions from marked string.
    /// Equivalent to TS deserializePipePositions.
    /// </summary>
    public static (string text, List<TextPosition> positions) DeserializePipePositions(string markedText)
    {
        if (string.IsNullOrEmpty(markedText))
        {
            return (string.Empty, new List<TextPosition>());
        }

        var positions = new List<TextPosition>();
        var sb = new StringBuilder();
        int line = 1;
        int column = 1;
        int index = 0;

        while (index < markedText.Length)
        {
            var c = markedText[index];

            if (c == '|')
            {
                positions.Add(new TextPosition(line, column));
                index++;
                continue;
            }

            if (c == '\r' && index + 1 < markedText.Length && markedText[index + 1] == '\n')
            {
                sb.Append('\r');
                sb.Append('\n');
                index += 2;
                line++;
                column = 1;
                continue;
            }

            sb.Append(c);
            index++;

            if (c == '\n')
            {
                line++;
                column = 1;
            }
            else if (c == '\r')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (sb.ToString(), positions);
    }

    /// <summary>
    /// Serialize positions to pipe-marked string.
    /// Equivalent to TS serializePipePositions.
    /// </summary>
    public static string SerializePipePositions(string text, IEnumerable<TextPosition> positions)
    {
        text ??= string.Empty;
        var orderedPositions = positions?.OrderBy(p => p.LineNumber).ThenBy(p => p.Column)
            ?? Enumerable.Empty<TextPosition>();
        var queue = new Queue<TextPosition>(orderedPositions);
        var sb = new StringBuilder();
        int line = 1;
        int charIndex = 0;
        int index = 0;

        while (index < text.Length)
        {
            AppendPipeMarkers(sb, queue, line, charIndex + 1);

            if (IsCrLf(text, index))
            {
                sb.Append('\r');
                sb.Append('\n');
                index += 2;
                line++;
                charIndex = 0;
                continue;
            }

            var c = text[index];
            sb.Append(c);
            index++;

            if (c == '\n')
            {
                line++;
                charIndex = 0;
            }
            else if (c == '\r')
            {
                line++;
                charIndex = 0;
            }
            else
            {
                charIndex++;
            }
        }

        AppendPipeMarkers(sb, queue, line, charIndex + 1);

        if (queue.Count > 0)
        {
            var leftover = queue.Peek();
            throw new InvalidOperationException($"Unexpected left over positions at ({leftover.LineNumber},{leftover.Column}).");
        }

        return sb.ToString();
    }

    private static void AppendPipeMarkers(StringBuilder sb, Queue<TextPosition> queue, int line, int nextColumn)
    {
        while (queue.Count > 0 && queue.Peek().LineNumber == line && queue.Peek().Column == nextColumn)
        {
            sb.Append('|');
            queue.Dequeue();
        }
    }

    private static bool IsCrLf(string text, int index)
    {
        return index + 1 < text.Length && text[index] == '\r' && text[index + 1] == '\n';
    }

    /// <summary>
    /// Test repeated action and extract positions.
    /// Equivalent to TS testRepeatedActionAndExtractPositions.
    /// </summary>
    public static string TestRepeatedActionAndExtractPositions(
        string lineContent,
        int startColumn,
        Func<int, int> action,
        int maxIterations = 100)
    {
        var positions = new List<TextPosition> { new TextPosition(1, startColumn) };
        int current = startColumn;

        for (int i = 0; i < maxIterations; i++)
        {
            int next = action(current);
            if (next == current || next < 1 || next > lineContent.Length + 1)
            {
                break;
            }
            positions.Add(new TextPosition(1, next));
            current = next;
        }

        return SerializePipePositions(lineContent, positions);
    }

    #endregion
}

/// <summary>
/// Represents a test case for word operations.
/// </summary>
public class WordOperationTestCase
{
    public string Content { get; init; } = string.Empty;
    public TextPosition StartPosition { get; init; }
    public (int line, int column)[] ExpectedWordLeftPositions { get; init; } = Array.Empty<(int, int)>();
    public (int line, int column)[] ExpectedWordRightPositions { get; init; } = Array.Empty<(int, int)>();
    public string WordSeparators { get; init; } = CursorConfiguration.DefaultWordSeparators;

    public override string ToString() => $"'{Content.Replace("\n", "\\n")}' from ({StartPosition.LineNumber},{StartPosition.Column})";
}
