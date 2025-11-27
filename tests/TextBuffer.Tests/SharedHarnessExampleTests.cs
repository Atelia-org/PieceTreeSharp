// WS5-PORT: Demonstration tests using the new shared test harness
// Purpose: Show how to use TestEditorBuilder, CursorTestHelper, WordTestUtils, and SnapshotTestUtils
// Created: 2025-11-26

using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Cursor;
using PieceTree.TextBuffer.Tests.Helpers;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests;

/// <summary>
/// Demonstration tests showing usage of the shared test harness.
/// These tests serve as examples for how to use the new helpers.
/// </summary>
public class SharedHarnessExampleTests
{
    #region TestEditorBuilder Examples

    [Fact]
    public void TestEditorBuilder_SimpleContent()
    {
        // Using the fluent builder to create a TextModel
        TextModel model = TestEditorBuilder.Create()
            .WithContent("hello world")
            .WithTabSize(4)
            .WithInsertSpaces(true)
            .Build();

        Assert.Equal("hello world", model.GetValue());
        Assert.Equal(4, model.GetOptions().TabSize);
        Assert.True(model.GetOptions().InsertSpaces);
    }

    [Fact]
    public void TestEditorBuilder_WithLines()
    {
        TextModel model = TestEditorBuilder.Create()
            .WithLines("line 1", "line 2", "line 3")
            .WithLF()
            .Build();

        Assert.Equal("line 1\nline 2\nline 3", model.GetValue());
        Assert.Equal(3, model.GetLineCount());
    }

    [Fact]
    public void TestEditorBuilder_WithMarkedContent()
    {
        // Use pipe markers to indicate cursor positions
        TestEditorContext context = TestEditorBuilder.Create()
            .WithMarkedContent("hello| world")
            .BuildContext();

        Assert.Equal("hello world", context.GetValue());
        Assert.Single(context.InitialCursors);
        CursorTestHelper.AssertPosition(context.InitialCursors[0], 1, 6);
    }

    [Fact]
    public void TestEditorBuilder_WithMultipleCursors()
    {
        TestEditorContext context = TestEditorBuilder.Create()
            .WithContent("hello world")
            .WithCursor(1, 1)
            .WithCursor(1, 7)
            .BuildContext();

        Assert.Equal(2, context.InitialCursors.Count);
        CursorTestHelper.AssertMultiCursors(context.InitialCursors, (1, 1), (1, 7));
    }

    [Fact]
    public void TestEditorBuilder_WithSelection()
    {
        TestEditorContext context = TestEditorBuilder.Create()
            .WithContent("hello world")
            .WithSelection(1, 1, 1, 6) // Select "hello"
            .BuildContext();

        Assert.Single(context.InitialSelections);
        CursorTestHelper.AssertSelection(context.PrimarySelection, 1, 1, 1, 6);
    }

    [Fact]
    public void TestEditorBuilder_BuildContext_ProvidesCursorConfig()
    {
        TestEditorContext context = TestEditorBuilder.Create()
            .WithContent("test")
            .WithTabSize(2)
            .WithStickyTabStops(true)
            .WithWordSeparators(" -_")
            .BuildContext();

        Assert.Equal(2, context.CursorConfig.TabSize);
        Assert.True(context.CursorConfig.StickyTabStops);
        Assert.Equal(" -_", context.CursorConfig.WordSeparators);
    }

    #endregion

    #region CursorTestHelper Examples

    [Fact]
    public void CursorTestHelper_ParsePipePositions()
    {
        (string? content, List<TextPosition>? positions) = CursorTestHelper.ParsePipePositions("hello| world|");

        Assert.Equal("hello world", content);
        Assert.Equal(2, positions.Count);
        CursorTestHelper.AssertPosition(positions[0], 1, 6);
        CursorTestHelper.AssertPosition(positions[1], 1, 12);
    }

    [Fact]
    public void CursorTestHelper_ParsePipePositions_MultiLine()
    {
        (string? content, List<TextPosition>? positions) = CursorTestHelper.ParsePipePositions("line1|\nline2|");

        Assert.Equal("line1\nline2", content);
        Assert.Equal(2, positions.Count);
        CursorTestHelper.AssertPosition(positions[0], 1, 6);
        CursorTestHelper.AssertPosition(positions[1], 2, 6);
    }

    [Fact]
    public void CursorTestHelper_SerializePipePositions()
    {
        TextPosition[] positions = [new TextPosition(1, 6), new TextPosition(1, 12)];
        string result = CursorTestHelper.SerializePipePositions("hello world", positions);

        Assert.Equal("hello| world|", result);
    }

    [Fact]
    public void CursorTestHelper_AssertionMethods()
    {
        TextPosition pos = new(3, 10);
        Selection selection = new(1, 5, 3, 10);

        // These assertions should pass
        CursorTestHelper.AssertPosition(pos, 3, 10);
        CursorTestHelper.AssertPositionEquals(pos, new TextPosition(3, 10));
        CursorTestHelper.AssertSelection(selection, 1, 5, 3, 10);
        CursorTestHelper.AssertSelectionIsNotEmpty(selection);
    }

    [Fact]
    public void CursorTestHelper_CreateCursorAt()
    {
        SingleCursorState state = CursorTestHelper.CreateCursorAt(5, 10);

        CursorTestHelper.AssertCursorStatePosition(state, 5, 10);
        CursorTestHelper.AssertCursorStateHasNoSelection(state);
    }

    [Fact]
    public void CursorTestHelper_CreateCursorWithSelection()
    {
        SingleCursorState state = CursorTestHelper.CreateCursorWithSelection(1, 1, 2, 5);

        CursorTestHelper.AssertCursorStateHasSelection(state);
        CursorTestHelper.AssertCursorStatePosition(state, 2, 5);
    }

    [Fact]
    public void CursorTestHelper_VisibleColumnAssertions()
    {
        // Tab expands to 4 spaces
        CursorTestHelper.AssertVisibleColumn("\thello", 2, 4, 4);
        CursorTestHelper.AssertColumnFromVisible("\thello", 4, 4, 2);

        // Round-trip should preserve column
        CursorTestHelper.AssertColumnRoundTrip("hello\tworld", 8, 4);
    }

    #endregion

    #region WordTestUtils Examples

    [Fact]
    public void WordTestUtils_DeserializePipePositions()
    {
        (string? text, List<TextPosition>? positions) = WordTestUtils.DeserializePipePositions("|hello| world|");

        Assert.Equal("hello world", text);
        Assert.Equal(3, positions.Count);
        CursorTestHelper.AssertPosition(positions[0], 1, 1);
        CursorTestHelper.AssertPosition(positions[1], 1, 6);
        CursorTestHelper.AssertPosition(positions[2], 1, 12);
    }

    [Fact]
    public void WordTestUtils_SerializePipePositions()
    {
        string result = WordTestUtils.SerializePipePositions(
            "hello world",
            new[]
            {
                new TextPosition(1, 1),
                new TextPosition(1, 6),
                new TextPosition(1, 12)
            });

        Assert.Equal("|hello| world|", result);
    }

    [Fact]
    public void WordTestUtils_GetWordBoundaries()
    {
        List<int> boundaries = WordTestUtils.GetWordBoundaries("hello world", " ");

        // Boundaries: 1 (start), 6 (end of hello), 7 (start of world), 12 (end)
        Assert.Contains(1, boundaries);
        Assert.Contains(6, boundaries);
        Assert.Contains(7, boundaries);
        Assert.Contains(12, boundaries);
    }

    [Fact]
    public void WordTestUtils_GetWordStarts()
    {
        List<int> starts = WordTestUtils.GetWordStarts("hello world foo", " ");

        Assert.Equal(3, starts.Count);
        Assert.Contains(1, starts);  // hello
        Assert.Contains(7, starts);  // world
        Assert.Contains(13, starts); // foo
    }

    [Fact]
    public void WordTestUtils_GetWordEnds()
    {
        List<int> ends = WordTestUtils.GetWordEnds("hello world foo", " ");

        Assert.Equal(3, ends.Count);
        Assert.Contains(6, ends);   // after hello
        Assert.Contains(12, ends);  // after world
        Assert.Contains(16, ends);  // after foo
    }

    [Theory]
    [InlineData(WordTestUtils.AsciiTestCases.SimpleWords)]
    [InlineData(WordTestUtils.AsciiTestCases.WithPunctuation)]
    [InlineData(WordTestUtils.CamelCaseTestCases.SimpleCamelCase)]
    public void WordTestUtils_TestCasesAreValid(string testCase)
    {
        Assert.NotEmpty(testCase);
    }

    #endregion

    #region SnapshotTestUtils Examples

    [Fact]
    public void SnapshotTestUtils_NormalizeLineEndings()
    {
        string mixed = "line1\r\nline2\rline3\nline4";
        string normalized = SnapshotTestUtils.NormalizeLineEndings(mixed);

        Assert.Equal("line1\nline2\nline3\nline4", normalized);
    }

    [Fact]
    public void SnapshotTestUtils_GenerateDiff()
    {
        string expected = "line1\nline2\nline3";
        string actual = "line1\nmodified\nline3";

        string diff = SnapshotTestUtils.GenerateDiff(expected, actual);

        Assert.Contains("line2", diff);
        Assert.Contains("modified", diff);
    }

    [Fact]
    public void SnapshotTestUtils_SnapshotsDirectoryExists()
    {
        Assert.True(System.IO.Directory.Exists(SnapshotTestUtils.SnapshotsDirectory));
    }

    // Uncomment to test snapshot functionality:
    // [Fact]
    // public void SnapshotTestUtils_AssertMatchesSnapshot_Example()
    // {
    //     var output = "hello world\ntest output";
    //     SnapshotTestUtils.AssertMatchesSnapshot("Examples", "hello-world", output);
    // }

    #endregion

    #region Theory + MemberData Pattern Examples

    public static IEnumerable<object[]> VisibleColumnTestData =>
        WordTestUtils.GenerateVisibleColumnTestData();

    [Theory]
    [MemberData(nameof(VisibleColumnTestData))]
    public void VisibleColumn_MatchesTsOracle(string lineContent, int tabSize, int column, int expectedVisibleColumn)
    {
        int result = CursorColumnsHelper.VisibleColumnFromColumn(lineContent, column, tabSize);
        Assert.Equal(expectedVisibleColumn, result);
    }

    public static IEnumerable<object[]> WordBoundaryTestData =>
        WordTestUtils.GenerateWordBoundaryTestData();

    [Theory]
    [MemberData(nameof(WordBoundaryTestData))]
    public void WordBoundaries_DetectedCorrectly(string lineContent, string wordSeparators)
    {
        List<int> boundaries = WordTestUtils.GetWordBoundaries(lineContent, wordSeparators);

        // Basic sanity checks
        Assert.NotEmpty(boundaries);
        Assert.Contains(1, boundaries); // Start is always a boundary
        Assert.Contains(lineContent.Length + 1, boundaries); // End is always a boundary
    }

    #endregion

    #region Integration Examples

    [Fact]
    public void Integration_BuilderWithHelperAssertions()
    {
        // Build a test context
        TestEditorContext context = TestEditorBuilder.Create()
            .WithLines("function test() {", "  return true;", "}")
            .WithCursor(2, 3) // Before "return"
            .WithTabSize(2)
            .BuildContext();

        // Verify initial state
        Assert.Equal(3, context.Model.GetLineCount());
        CursorTestHelper.AssertPosition(context.PrimaryCursor, 2, 3);

        // Get the cursor state
        SingleCursorState cursorState = context.CreateSingleCursorState();
        CursorTestHelper.AssertCursorStateHasNoSelection(cursorState);

        // Verify line content
        Assert.Equal("  return true;", context.GetLineContent(2));
    }

    [Fact]
    public void Integration_WordOperationsWithTestUtils()
    {
        TestEditorContext context = TestEditorBuilder.Create()
            .WithContent(WordTestUtils.AsciiTestCases.SimpleWords)
            .WithCursor(1, 1)
            .BuildContext();

        // Get word starts and ends
        List<int> starts = WordTestUtils.GetWordStarts(context.GetLineContent(1), " ");
        List<int> ends = WordTestUtils.GetWordEnds(context.GetLineContent(1), " ");

        // "hello world foo bar" should have 4 words
        Assert.Equal(4, starts.Count);
        Assert.Equal(4, ends.Count);
    }

    #endregion
}
