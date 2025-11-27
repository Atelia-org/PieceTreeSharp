// Source: ts/src/vs/editor/test/common/controller/cursorAtomicMoveOperations.test.ts
// Ported: 2025-11-28 (WS5-CursorAtomicMove)

using PieceTree.TextBuffer.Cursor;
using Xunit;

namespace PieceTree.TextBuffer.Tests.CursorOperations;

/// <summary>
/// Test suite for AtomicTabMoveOperations.
/// Ports TS 'Cursor move command test' suite from cursorAtomicMoveOperations.test.ts.
/// </summary>
public class CursorAtomicMoveTests
{
    #region Test Data

    /// <summary>
    /// Test case data for whitespaceVisibleColumn tests.
    /// </summary>
    public class WhitespaceVisibleColumnTestCase
    {
        public string LineContent { get; }
        public int TabSize { get; }
        public int[] ExpectedPrevTabStopPosition { get; }
        public int[] ExpectedPrevTabStopVisibleColumn { get; }
        public int[] ExpectedVisibleColumn { get; }

        public WhitespaceVisibleColumnTestCase(
            string lineContent,
            int tabSize,
            int[] expectedPrevTabStopPosition,
            int[] expectedPrevTabStopVisibleColumn,
            int[] expectedVisibleColumn)
        {
            LineContent = lineContent;
            TabSize = tabSize;
            ExpectedPrevTabStopPosition = expectedPrevTabStopPosition;
            ExpectedPrevTabStopVisibleColumn = expectedPrevTabStopVisibleColumn;
            ExpectedVisibleColumn = expectedVisibleColumn;
        }

        public override string ToString() =>
            $"LineContent=\"{LineContent.Replace("\t", "\\t")}\", TabSize={TabSize}";
    }

    public static IEnumerable<object[]> WhitespaceVisibleColumnTestCases()
    {
        // Case 1: 8 spaces, tabSize=4
        yield return new object[]
        {
            new WhitespaceVisibleColumnTestCase(
                lineContent: "        ",
                tabSize: 4,
                expectedPrevTabStopPosition: new[] { -1, 0, 0, 0, 0, 4, 4, 4, 4, -1 },
                expectedPrevTabStopVisibleColumn: new[] { -1, 0, 0, 0, 0, 4, 4, 4, 4, -1 },
                expectedVisibleColumn: new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, -1 })
        };

        // Case 2: 2 spaces, tabSize=4
        yield return new object[]
        {
            new WhitespaceVisibleColumnTestCase(
                lineContent: "  ",
                tabSize: 4,
                expectedPrevTabStopPosition: new[] { -1, 0, 0, -1 },
                expectedPrevTabStopVisibleColumn: new[] { -1, 0, 0, -1 },
                expectedVisibleColumn: new[] { 0, 1, 2, -1 })
        };

        // Case 3: single tab, tabSize=4
        yield return new object[]
        {
            new WhitespaceVisibleColumnTestCase(
                lineContent: "\t",
                tabSize: 4,
                expectedPrevTabStopPosition: new[] { -1, 0, -1 },
                expectedPrevTabStopVisibleColumn: new[] { -1, 0, -1 },
                expectedVisibleColumn: new[] { 0, 4, -1 })
        };

        // Case 4: tab + space, tabSize=4
        yield return new object[]
        {
            new WhitespaceVisibleColumnTestCase(
                lineContent: "\t ",
                tabSize: 4,
                expectedPrevTabStopPosition: new[] { -1, 0, 1, -1 },
                expectedPrevTabStopVisibleColumn: new[] { -1, 0, 4, -1 },
                expectedVisibleColumn: new[] { 0, 4, 5, -1 })
        };

        // Case 5: space + tab + tab + space, tabSize=4
        yield return new object[]
        {
            new WhitespaceVisibleColumnTestCase(
                lineContent: " \t\t ",
                tabSize: 4,
                expectedPrevTabStopPosition: new[] { -1, 0, 0, 2, 3, -1 },
                expectedPrevTabStopVisibleColumn: new[] { -1, 0, 0, 4, 8, -1 },
                expectedVisibleColumn: new[] { 0, 1, 4, 8, 9, -1 })
        };

        // Case 6: space + tab + 'A' (non-whitespace), tabSize=4
        yield return new object[]
        {
            new WhitespaceVisibleColumnTestCase(
                lineContent: " \tA",
                tabSize: 4,
                expectedPrevTabStopPosition: new[] { -1, 0, 0, -1, -1 },
                expectedPrevTabStopVisibleColumn: new[] { -1, 0, 0, -1, -1 },
                expectedVisibleColumn: new[] { 0, 1, 4, -1, -1 })
        };

        // Case 7: single 'A' (non-whitespace from start), tabSize=4
        yield return new object[]
        {
            new WhitespaceVisibleColumnTestCase(
                lineContent: "A",
                tabSize: 4,
                expectedPrevTabStopPosition: new[] { -1, -1, -1 },
                expectedPrevTabStopVisibleColumn: new[] { -1, -1, -1 },
                expectedVisibleColumn: new[] { 0, -1, -1 })
        };

        // Case 8: empty string, tabSize=4
        yield return new object[]
        {
            new WhitespaceVisibleColumnTestCase(
                lineContent: "",
                tabSize: 4,
                expectedPrevTabStopPosition: new[] { -1, -1 },
                expectedPrevTabStopVisibleColumn: new[] { -1, -1 },
                expectedVisibleColumn: new[] { 0, -1 })
        };
    }

    /// <summary>
    /// Test case data for atomicPosition tests.
    /// </summary>
    public class AtomicPositionTestCase
    {
        public string LineContent { get; }
        public int TabSize { get; }
        public int[] ExpectedLeft { get; }
        public int[] ExpectedRight { get; }
        public int[] ExpectedNearest { get; }

        public AtomicPositionTestCase(
            string lineContent,
            int tabSize,
            int[] expectedLeft,
            int[] expectedRight,
            int[] expectedNearest)
        {
            LineContent = lineContent;
            TabSize = tabSize;
            ExpectedLeft = expectedLeft;
            ExpectedRight = expectedRight;
            ExpectedNearest = expectedNearest;
        }

        public override string ToString() =>
            $"LineContent=\"{LineContent.Replace("\t", "\\t")}\", TabSize={TabSize}";
    }

    public static IEnumerable<object[]> AtomicPositionTestCases()
    {
        // Case 1: 8 spaces, tabSize=4
        yield return new object[]
        {
            new AtomicPositionTestCase(
                lineContent: "        ",
                tabSize: 4,
                expectedLeft: new[] { -1, 0, 0, 0, 0, 4, 4, 4, 4, -1 },
                expectedRight: new[] { 4, 4, 4, 4, 8, 8, 8, 8, -1, -1 },
                expectedNearest: new[] { 0, 0, 0, 4, 4, 4, 4, 8, 8, -1 })
        };

        // Case 2: space + tab, tabSize=4
        yield return new object[]
        {
            new AtomicPositionTestCase(
                lineContent: " \t",
                tabSize: 4,
                expectedLeft: new[] { -1, 0, 0, -1 },
                expectedRight: new[] { 2, 2, -1, -1 },
                expectedNearest: new[] { 0, 0, 2, -1 })
        };

        // Case 3: tab + space, tabSize=4
        yield return new object[]
        {
            new AtomicPositionTestCase(
                lineContent: "\t ",
                tabSize: 4,
                expectedLeft: new[] { -1, 0, -1, -1 },
                expectedRight: new[] { 1, -1, -1, -1 },
                expectedNearest: new[] { 0, 1, -1, -1 })
        };

        // Case 4: space + tab + space, tabSize=4
        yield return new object[]
        {
            new AtomicPositionTestCase(
                lineContent: " \t ",
                tabSize: 4,
                expectedLeft: new[] { -1, 0, 0, -1, -1 },
                expectedRight: new[] { 2, 2, -1, -1, -1 },
                expectedNearest: new[] { 0, 0, 2, -1, -1 })
        };

        // Case 5: 8 spaces + 'A', tabSize=4
        yield return new object[]
        {
            new AtomicPositionTestCase(
                lineContent: "        A",
                tabSize: 4,
                expectedLeft: new[] { -1, 0, 0, 0, 0, 4, 4, 4, 4, -1, -1 },
                expectedRight: new[] { 4, 4, 4, 4, 8, 8, 8, 8, -1, -1, -1 },
                expectedNearest: new[] { 0, 0, 0, 4, 4, 4, 4, 8, 8, -1, -1 })
        };

        // Case 6: 6 spaces + 'foo' (partial indentation), tabSize=4
        yield return new object[]
        {
            new AtomicPositionTestCase(
                lineContent: "      foo",
                tabSize: 4,
                expectedLeft: new[] { -1, 0, 0, 0, 0, -1, -1, -1, -1, -1, -1 },
                expectedRight: new[] { 4, 4, 4, 4, -1, -1, -1, -1, -1, -1, -1 },
                expectedNearest: new[] { 0, 0, 0, 4, 4, -1, -1, -1, -1, -1, -1 })
        };
    }

    #endregion

    #region WhitespaceVisibleColumn Tests

    [Theory]
    [MemberData(nameof(WhitespaceVisibleColumnTestCases), DisableDiscoveryEnumeration = true)]
    public void WhitespaceVisibleColumn_AllPositions_MatchesExpected(WhitespaceVisibleColumnTestCase testCase)
    {
        int maxPosition = testCase.ExpectedVisibleColumn.Length;

        for (int position = 0; position < maxPosition; position++)
        {
            (int prevTabStopPosition, int prevTabStopVisibleColumn, int visibleColumn) =
                AtomicTabMoveOperations.WhitespaceVisibleColumn(testCase.LineContent, position, testCase.TabSize);

            Assert.Equal(
                testCase.ExpectedPrevTabStopPosition[position],
                prevTabStopPosition);

            Assert.Equal(
                testCase.ExpectedPrevTabStopVisibleColumn[position],
                prevTabStopVisibleColumn);

            Assert.Equal(
                testCase.ExpectedVisibleColumn[position],
                visibleColumn);
        }
    }

    [Fact]
    public void WhitespaceVisibleColumn_EightSpaces_PositionZero()
    {
        // At position 0, there's no previous tab stop yet
        (int, int, int) result = AtomicTabMoveOperations.WhitespaceVisibleColumn("        ", 0, 4);
        Assert.Equal((-1, -1, 0), result);
    }

    [Fact]
    public void WhitespaceVisibleColumn_EightSpaces_PositionFour()
    {
        (int, int, int) result = AtomicTabMoveOperations.WhitespaceVisibleColumn("        ", 4, 4);
        Assert.Equal((0, 0, 4), result);
    }

    [Fact]
    public void WhitespaceVisibleColumn_SingleTab_PositionOne()
    {
        (int, int, int) result = AtomicTabMoveOperations.WhitespaceVisibleColumn("\t", 1, 4);
        Assert.Equal((0, 0, 4), result);
    }

    [Fact]
    public void WhitespaceVisibleColumn_NonWhitespace_ReturnsMinusOne()
    {
        (int, int, int) result = AtomicTabMoveOperations.WhitespaceVisibleColumn("A", 1, 4);
        Assert.Equal((-1, -1, -1), result);
    }

    [Fact]
    public void WhitespaceVisibleColumn_PastEndOfLine_ReturnsMinusOne()
    {
        (int, int, int) result = AtomicTabMoveOperations.WhitespaceVisibleColumn("  ", 5, 4);
        Assert.Equal((-1, -1, -1), result);
    }

    #endregion

    #region AtomicPosition Left Tests

    [Theory]
    [MemberData(nameof(AtomicPositionTestCases), DisableDiscoveryEnumeration = true)]
    public void AtomicPosition_Left_AllPositions_MatchesExpected(AtomicPositionTestCase testCase)
    {
        for (int position = 0; position < testCase.ExpectedLeft.Length; position++)
        {
            int actual = AtomicTabMoveOperations.AtomicPosition(
                testCase.LineContent, position, testCase.TabSize, Direction.Left);

            Assert.Equal(
                testCase.ExpectedLeft[position],
                actual);
        }
    }

    [Fact]
    public void AtomicPosition_Left_EightSpaces_FromPositionFive()
    {
        int result = AtomicTabMoveOperations.AtomicPosition("        ", 5, 4, Direction.Left);
        Assert.Equal(4, result);
    }

    [Fact]
    public void AtomicPosition_Left_PartialIndentation_ReturnsMinusOne()
    {
        // "      foo" with tabSize 4, from position 5
        // This is partial indentation (6 spaces, not a full 8 spaces to next tab stop)
        int result = AtomicTabMoveOperations.AtomicPosition("      foo", 5, 4, Direction.Left);
        Assert.Equal(-1, result);
    }

    #endregion

    #region AtomicPosition Right Tests

    [Theory]
    [MemberData(nameof(AtomicPositionTestCases), DisableDiscoveryEnumeration = true)]
    public void AtomicPosition_Right_AllPositions_MatchesExpected(AtomicPositionTestCase testCase)
    {
        for (int position = 0; position < testCase.ExpectedRight.Length; position++)
        {
            int actual = AtomicTabMoveOperations.AtomicPosition(
                testCase.LineContent, position, testCase.TabSize, Direction.Right);

            Assert.Equal(
                testCase.ExpectedRight[position],
                actual);
        }
    }

    [Fact]
    public void AtomicPosition_Right_EightSpaces_FromPositionZero()
    {
        int result = AtomicTabMoveOperations.AtomicPosition("        ", 0, 4, Direction.Right);
        Assert.Equal(4, result);
    }

    [Fact]
    public void AtomicPosition_Right_AtEndOfWhitespace_ReturnsMinusOne()
    {
        int result = AtomicTabMoveOperations.AtomicPosition("        ", 8, 4, Direction.Right);
        Assert.Equal(-1, result);
    }

    #endregion

    #region AtomicPosition Nearest Tests

    [Theory]
    [MemberData(nameof(AtomicPositionTestCases), DisableDiscoveryEnumeration = true)]
    public void AtomicPosition_Nearest_AllPositions_MatchesExpected(AtomicPositionTestCase testCase)
    {
        for (int position = 0; position < testCase.ExpectedNearest.Length; position++)
        {
            int actual = AtomicTabMoveOperations.AtomicPosition(
                testCase.LineContent, position, testCase.TabSize, Direction.Nearest);

            Assert.Equal(
                testCase.ExpectedNearest[position],
                actual);
        }
    }

    [Fact]
    public void AtomicPosition_Nearest_AtTabStop_ReturnsSamePosition()
    {
        // Position 0 is at visible column 0, which is a tab stop
        int result = AtomicTabMoveOperations.AtomicPosition("        ", 0, 4, Direction.Nearest);
        Assert.Equal(0, result);
    }

    [Fact]
    public void AtomicPosition_Nearest_MidwayBetweenTabStops_GoesLeft()
    {
        // Position 2 is at visible column 2, which is closer to 0 than 4
        int result = AtomicTabMoveOperations.AtomicPosition("        ", 2, 4, Direction.Nearest);
        Assert.Equal(0, result);
    }

    [Fact]
    public void AtomicPosition_Nearest_CloserToNextTabStop_GoesRight()
    {
        // Position 3 is at visible column 3, which is closer to 4 than 0
        int result = AtomicTabMoveOperations.AtomicPosition("        ", 3, 4, Direction.Nearest);
        Assert.Equal(4, result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AtomicPosition_EmptyLine_Left_ReturnsMinusOne()
    {
        int result = AtomicTabMoveOperations.AtomicPosition("", 0, 4, Direction.Left);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void AtomicPosition_EmptyLine_Right_ReturnsMinusOne()
    {
        int result = AtomicTabMoveOperations.AtomicPosition("", 0, 4, Direction.Right);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void AtomicPosition_NonWhitespace_ReturnsMinusOne()
    {
        int result = AtomicTabMoveOperations.AtomicPosition("foo", 1, 4, Direction.Left);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void AtomicPosition_MixedSpaceTab_Left()
    {
        // " \t" has visible columns: pos0=0, pos1=1, pos2=4
        // At position 2 (visibleCol 4), left should go to previous tab stop at position 0
        int result = AtomicTabMoveOperations.AtomicPosition(" \t", 2, 4, Direction.Left);
        Assert.Equal(0, result);
    }

    [Fact]
    public void AtomicPosition_TabSpace_Right_FromTab()
    {
        // "\t " has visible columns: pos0=0, pos1=4, pos2=5
        // At position 1 (visibleCol 4), there's no full tab stop width to the right (only 1 space)
        int result = AtomicTabMoveOperations.AtomicPosition("\t ", 1, 4, Direction.Right);
        Assert.Equal(-1, result);
    }

    #endregion
}
