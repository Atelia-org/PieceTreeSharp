// WS5-QA: TextModel indentation detection parity with VS Code
// Source: ts/src/vs/editor/test/common/model/textModel.test.ts (guessIndentation suite)

namespace PieceTree.TextBuffer.Tests;

public class TextModelIndentationTests
{
    private const int DefaultSpacesTabSize = 13370;
    private const int DefaultTabsTabSize = 13371;

    private static void TestGuessIndentation(
        bool defaultInsertSpaces,
        int defaultTabSize,
        bool expectedInsertSpaces,
        int expectedTabSize,
        string[] lines,
        string? message = null)
    {
        TextModelCreationOptions options = TextModelCreationOptions.Default with
        {
            DetectIndentation = true,
            InsertSpaces = defaultInsertSpaces,
            TabSize = defaultTabSize,
            IndentSize = defaultTabSize,
        };

        TextModel model = new(string.Join("\n", lines), options);
        TextModelResolvedOptions resolved = model.GetOptions();

        Assert.True(
            expectedInsertSpaces == resolved.InsertSpaces,
            BuildGuessMessage("InsertSpaces", expectedInsertSpaces, resolved.InsertSpaces, message));
        Assert.True(
            expectedTabSize == resolved.TabSize,
            BuildGuessMessage("TabSize", expectedTabSize, resolved.TabSize, message));
    }

    private static string BuildGuessMessage(string field, object expected, object actual, string? message)
    {
        string core = $"{field} mismatch (expected {expected}, actual {actual})";
        return message is null ? core : $"{core} :: {message}";
    }

    private static void AssertGuess(
        bool? expectedInsertSpaces,
        int? expectedTabSize,
        bool tabSizeRequiresInsertSpaces,
        string[] lines,
        string? message = null)
    {
        if (!expectedInsertSpaces.HasValue)
        {
            if (!expectedTabSize.HasValue)
            {
                TestGuessIndentation(true, DefaultSpacesTabSize, true, DefaultSpacesTabSize, lines, message);
                TestGuessIndentation(false, DefaultTabsTabSize, false, DefaultTabsTabSize, lines, message);
            }
            else if (!tabSizeRequiresInsertSpaces)
            {
                TestGuessIndentation(true, DefaultSpacesTabSize, true, expectedTabSize.Value, lines, message);
                TestGuessIndentation(false, DefaultTabsTabSize, false, expectedTabSize.Value, lines, message);
            }
            else
            {
                TestGuessIndentation(true, DefaultSpacesTabSize, true, expectedTabSize.Value, lines, message);
                TestGuessIndentation(false, DefaultTabsTabSize, false, DefaultTabsTabSize, lines, message);
            }
            return;
        }

        bool insertSpaces = expectedInsertSpaces.Value;
        if (!expectedTabSize.HasValue)
        {
            TestGuessIndentation(true, DefaultSpacesTabSize, insertSpaces, DefaultSpacesTabSize, lines, message);
            TestGuessIndentation(false, DefaultTabsTabSize, insertSpaces, DefaultTabsTabSize, lines, message);
        }
        else if (!tabSizeRequiresInsertSpaces)
        {
            TestGuessIndentation(true, DefaultSpacesTabSize, insertSpaces, expectedTabSize.Value, lines, message);
            TestGuessIndentation(false, DefaultTabsTabSize, insertSpaces, expectedTabSize.Value, lines, message);
        }
        else if (insertSpaces)
        {
            TestGuessIndentation(true, DefaultSpacesTabSize, insertSpaces, expectedTabSize.Value, lines, message);
            TestGuessIndentation(false, DefaultTabsTabSize, insertSpaces, expectedTabSize.Value, lines, message);
        }
        else
        {
            TestGuessIndentation(true, DefaultSpacesTabSize, insertSpaces, DefaultSpacesTabSize, lines, message);
            TestGuessIndentation(false, DefaultTabsTabSize, insertSpaces, DefaultTabsTabSize, lines, message);
        }
    }

    [Fact]
    public void GuessIndentation_MatrixMatchesTypeScript()
    {
        AssertGuess(null, null, false,
        [
            "x",
            "x",
            "x",
            "x",
            "x",
            "x",
            "x",
        ], "no clues");

        AssertGuess(false, null, false,
        [
            "\tx",
            "x",
            "x",
            "x",
            "x",
            "x",
            "x",
        ], "no spaces, 1xTAB");

        AssertGuess(true, 2, false,
        [
            "  x",
            "x",
            "x",
            "x",
            "x",
            "x",
            "x",
        ], "1x2");

        AssertGuess(false, null, false,
        [
            "\tx",
            "\tx",
            "\tx",
            "\tx",
            "\tx",
            "\tx",
            "\tx",
        ], "7xTAB");

        AssertGuess(null, 2, true,
        [
            "\tx",
            "  x",
            "\tx",
            "  x",
            "\tx",
            "  x",
            "\tx",
            "  x",
        ], "4x2, 4xTAB");

        AssertGuess(false, null, false,
        [
            "\tx",
            " x",
            "\tx",
            " x",
            "\tx",
            " x",
            "\tx",
            " x",
        ], "4x1, 4xTAB");

        AssertGuess(false, null, false,
        [
            "\tx",
            "\tx",
            "  x",
            "\tx",
            "  x",
            "\tx",
            "  x",
            "\tx",
            "  x",
        ], "4x2, 5xTAB");

        AssertGuess(false, null, false,
        [
            "\tx",
            "\tx",
            "x",
            "\tx",
            "x",
            "\tx",
            "x",
            "\tx",
            "  x",
        ], "1x2, 5xTAB");

        AssertGuess(false, null, false,
        [
            "\tx",
            "\tx",
            "x",
            "\tx",
            "x",
            "\tx",
            "x",
            "\tx",
            "    x",
        ], "1x4, 5xTAB");

        AssertGuess(false, null, false,
        [
            "\tx",
            "\tx",
            "x",
            "\tx",
            "x",
            "\tx",
            "  x",
            "\tx",
            "    x",
        ], "1x2, 1x4, 5xTAB");

        AssertGuess(null, null, false,
        [
            "x",
            " x",
            " x",
            " x",
            " x",
            " x",
            " x",
            " x",
        ], "7x1 - 1 space is never guessed as an indentation");

        AssertGuess(true, null, false,
        [
            "x",
            "          x",
            " x",
            " x",
            " x",
            " x",
            " x",
            " x",
        ], "1x10, 6x1");

        AssertGuess(null, null, false,
        [
            string.Empty,
            "  ",
            "    ",
            "      ",
            "        ",
            "          ",
            "            ",
            "              ",
        ], "whitespace lines don't count");

        AssertGuess(true, 3, false,
        [
            "x",
            "   x",
            "   x",
            "    x",
            "x",
            "   x",
            "   x",
            "    x",
            "x",
            "   x",
            "   x",
            "    x",
        ], "6x3, 3x4");

        AssertGuess(true, 5, false,
        [
            "x",
            "     x",
            "     x",
            "    x",
            "x",
            "     x",
            "     x",
            "    x",
            "x",
            "     x",
            "     x",
            "    x",
        ], "6x5, 3x4");

        AssertGuess(true, 7, false,
        [
            "x",
            "       x",
            "       x",
            "     x",
            "x",
            "       x",
            "       x",
            "    x",
            "x",
            "       x",
            "       x",
            "    x",
        ], "6x7, 1x5, 2x4");

        AssertGuess(true, 2, false,
        [
            "x",
            "  x",
            "  x",
            "  x",
            "  x",
            "x",
            "  x",
            "  x",
            "  x",
            "  x",
        ], "8x2");

        AssertGuess(true, 2, false,
        [
            "x",
            "  x",
            "  x",
            "x",
            "  x",
            "  x",
            "x",
            "  x",
            "  x",
            "x",
            "  x",
            "  x",
        ], "8x2 (alternating)");

        AssertGuess(true, 2, false,
        [
            "x",
            "  x",
            "    x",
            "x",
            "  x",
            "    x",
            "x",
            "  x",
            "    x",
            "x",
            "  x",
            "    x",
        ], "4x2, 4x4");

        AssertGuess(true, 2, false,
        [
            "x",
            "  x",
            "  x",
            "    x",
            "x",
            "  x",
            "  x",
            "    x",
            "x",
            "  x",
            "  x",
            "    x",
        ], "6x2, 3x4");

        AssertGuess(true, 2, false,
        [
            "x",
            "  x",
            "  x",
            "    x",
            "    x",
            "x",
            "  x",
            "  x",
            "    x",
            "    x",
        ], "4x2, 4x4 (doubles)");

        AssertGuess(true, 2, false,
        [
            "x",
            "  x",
            "    x",
            "    x",
            "x",
            "  x",
            "    x",
            "    x",
        ], "2x2, 4x4");

        AssertGuess(true, 4, false,
        [
            "x",
            "    x",
            "    x",
            "x",
            "    x",
            "    x",
            "x",
            "    x",
            "    x",
            "x",
            "    x",
            "    x",
        ], "8x4");

        AssertGuess(true, 2, false,
        [
            "x",
            "  x",
            "    x",
            "    x",
            "      x",
            "x",
            "  x",
            "    x",
            "    x",
            "      x",
        ], "2x2, 4x4, 2x6");

        AssertGuess(true, 2, false,
        [
            "x",
            "  x",
            "    x",
            "    x",
            "      x",
            "      x",
            "        x",
        ], "1x2, 2x4, 2x6, 1x8");

        AssertGuess(true, 4, false,
        [
            "x",
            "    x",
            "    x",
            "    x",
            "     x",
            "        x",
            "x",
            "    x",
            "    x",
            "    x",
            "     x",
            "        x",
        ], "6x4, 2x5, 2x8");

        AssertGuess(true, 4, false,
        [
            "x",
            "    x",
            "    x",
            "    x",
            "     x",
            "        x",
            "        x",
        ], "3x4, 1x5, 2x8");

        AssertGuess(true, 4, false,
        [
            "x",
            "x",
            "    x",
            "    x",
            "     x",
            "        x",
            "        x",
            "x",
            "x",
            "    x",
            "    x",
            "     x",
            "        x",
            "        x",
        ], "6x4, 2x5, 4x8");

        AssertGuess(true, 3, false,
        [
            "x",
            " x",
            " x",
            " x",
            " x",
            " x",
            "x",
            "   x",
            "    x",
            "    x",
        ], "5x1, 2x0, 1x3, 2x4");

        AssertGuess(false, null, false,
        [
            "\t x",
            " \t x",
            "\tx",
        ], "mixed whitespace 1");

        AssertGuess(false, null, false,
        [
            "\tx",
            "\t    x",
        ], "mixed whitespace 2");
    }

    [Fact]
    public void GuessIndentation_Issue44991()
    {
        AssertGuess(true, 4, false,
        [
            "a = 10             # 0 space indent",
            "b = 5              # 0 space indent",
            "if a > 10:         # 0 space indent",
            "    a += 1         # 4 space indent      delta 4 spaces",
            "    if b > 5:      # 4 space indent",
            "        b += 1     # 8 space indent      delta 4 spaces",
            "        b += 1     # 8 space indent",
            "        b += 1     # 8 space indent",
            "# comment line 1   # 0 space indent      delta 8 spaces",
            "# comment line 2   # 0 space indent",
            "# comment line 3   # 0 space indent",
            "        b += 1     # 8 space indent      delta 8 spaces",
            "        b += 1     # 8 space indent",
            "        b += 1     # 8 space indent",
        ]);
    }

    [Fact]
    public void GuessIndentation_Issue55818()
    {
        AssertGuess(true, 2, false,
        [
            string.Empty,
            "/* REQUIRE */",
            string.Empty,
            "const foo = require ( 'foo' ),",
            "      bar = require ( 'bar' );",
            string.Empty,
            "/* MY FN */",
            string.Empty,
            "function myFn () {",
            string.Empty,
            "  const asd = 1,",
            "        dsa = 2;",
            string.Empty,
            "  return bar ( foo ( asd ) );",
            string.Empty,
            "}",
            string.Empty,
            "/* EXPORT */",
            string.Empty,
            "module.exports = myFn;",
            string.Empty,
        ]);
    }

    [Fact]
    public void GuessIndentation_Issue70832()
    {
        AssertGuess(false, null, false,
        [
            "x",
            "x",
            "x",
            "x",
            "\tx",
            "\t\tx",
            "    x",
            "\t\tx",
            "\tx",
            "\t\tx",
            "\tx",
            "x",
            "x",
            "x",
            "x",
        ]);
    }

    [Fact]
    public void GuessIndentation_Issue62143()
    {
        AssertGuess(true, 2, false, ["x", "x", "  x", "  x"]);
        AssertGuess(true, 2, false, ["x", "  - item2", "  - item3"]);

        TestGuessIndentation(true, 2, true, 2, ["x x", "  x", "  x"]);
        TestGuessIndentation(true, 2, true, 2, ["x x", "  x", "  x", "    x"]);
        TestGuessIndentation(true, 2, true, 2,
        [
            "<!--test1.md -->",
            "- item1",
            "  - item2",
            "    - item3",
        ]);
    }

    [Fact]
    public void GuessIndentation_Issue84217()
    {
        AssertGuess(true, 4, false,
        [
            "def main():",
            "    print('hello')",
        ]);

        AssertGuess(true, 4, false,
        [
            "def main():",
            "    with open('foo') as fp:",
            "        print(fp.read())",
        ]);
    }
}
