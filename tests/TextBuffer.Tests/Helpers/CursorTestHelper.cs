// WS5-PORT: Shared Test Harness - CursorTestHelper
// Purpose: Cursor position/selection assertion methods and multi-cursor comparison utilities
// Created: 2025-11-26

using System;
using System.Collections.Generic;
using System.Text;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Cursor;
using Range = PieceTree.TextBuffer.Core.Range;
using Xunit;

namespace PieceTree.TextBuffer.Tests.Helpers;

/// <summary>
/// Helper utilities for testing cursor positions, selections, and multi-cursor scenarios.
/// Provides assertion methods and cursor state comparison utilities.
/// </summary>
public static class CursorTestHelper
{
    #region Position Parsing and Serialization

    /// <summary>
    /// Parse content with pipe markers (|) to extract cursor positions.
    /// Returns the content with pipes removed and the list of cursor positions.
    /// </summary>
    /// <param name="markedContent">Content with | markers for cursor positions</param>
    /// <returns>Tuple of (content without markers, list of cursor positions)</returns>
    /// <example>
    /// "hello| world" -> ("hello world", [(1, 6)])
    /// "line1|\nline2|" -> ("line1\nline2", [(1, 6), (2, 6)])
    /// </example>
    public static (string content, List<TextPosition> positions) ParsePipePositions(string markedContent)
    {
        if (string.IsNullOrEmpty(markedContent))
        {
            return (string.Empty, new List<TextPosition>());
        }

        List<TextPosition> positions = [];
        StringBuilder content = new();
        int line = 1;
        int column = 1;

        for (int i = 0; i < markedContent.Length; i++)
        {
            char c = markedContent[i];

            if (c == '|')
            {
                positions.Add(new TextPosition(line, column));
            }
            else
            {
                content.Append(c);
                if (c == '\n')
                {
                    line++;
                    column = 1;
                }
                else if (c == '\r')
                {
                    // Handle \r\n as single line break
                    if (i + 1 < markedContent.Length && markedContent[i + 1] == '\n')
                    {
                        content.Append('\n');
                        i++;
                    }
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }
        }

        return (content.ToString(), positions);
    }

    /// <summary>
    /// Serialize positions back to marked content format.
    /// </summary>
    public static string SerializePipePositions(string content, IReadOnlyList<TextPosition> positions)
    {
        if (string.IsNullOrEmpty(content) || positions == null || positions.Count == 0)
        {
            return content ?? string.Empty;
        }

        // Convert positions to offsets
        List<int> offsets = [];
        foreach (TextPosition pos in positions)
        {
            int offset = GetOffset(content, pos);
            offsets.Add(offset);
        }
        offsets.Sort((a, b) => b.CompareTo(a)); // Sort descending to insert from end

        StringBuilder result = new(content);
        foreach (int offset in offsets)
        {
            if (offset >= 0 && offset <= result.Length)
            {
                result.Insert(offset, '|');
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Parse content with selection markers: [anchor] for selection anchor, |active| for active position.
    /// For simple cases, use | for cursor and [...] for selected text.
    /// </summary>
    public static (string content, List<Selection> selections) ParseSelectionMarkers(string markedContent)
    {
        if (string.IsNullOrEmpty(markedContent))
        {
            return (string.Empty, new List<Selection>());
        }

        List<Selection> selections = [];
        StringBuilder content = new();
        int line = 1;
        int column = 1;

        TextPosition? bracketStart = null;
        bool inSelection = false;

        for (int i = 0; i < markedContent.Length; i++)
        {
            char c = markedContent[i];

            switch (c)
            {
                case '[':
                    // Start of selection
                    bracketStart = new TextPosition(line, column);
                    inSelection = true;
                    break;

                case ']':
                    // End of selection - if we have a start, create selection
                    if (bracketStart.HasValue)
                    {
                        TextPosition endPos = new(line, column);
                        selections.Add(Selection.FromPositions(bracketStart.Value, endPos));
                        bracketStart = null;
                        inSelection = false;
                    }
                    break;

                case '|':
                    // Cursor position (collapsed selection)
                    if (!inSelection)
                    {
                        TextPosition pos = new(line, column);
                        selections.Add(Selection.FromPositions(pos, pos));
                    }
                    break;

                default:
                    content.Append(c);
                    if (c == '\n')
                    {
                        line++;
                        column = 1;
                    }
                    else if (c == '\r')
                    {
                        if (i + 1 < markedContent.Length && markedContent[i + 1] == '\n')
                        {
                            content.Append('\n');
                            i++;
                        }
                        line++;
                        column = 1;
                    }
                    else
                    {
                        column++;
                    }
                    break;
            }
        }

        return (content.ToString(), selections);
    }

    /// <summary>
    /// Get offset from position in content.
    /// </summary>
    private static int GetOffset(string content, TextPosition pos)
    {
        int offset = 0;
        int line = 1;
        int column = 1;

        while (offset < content.Length && (line < pos.LineNumber || (line == pos.LineNumber && column < pos.Column)))
        {
            if (content[offset] == '\n')
            {
                line++;
                column = 1;
            }
            else if (content[offset] == '\r')
            {
                if (offset + 1 < content.Length && content[offset + 1] == '\n')
                {
                    offset++;
                }
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
            offset++;
        }

        return offset;
    }

    #endregion

    #region Cursor State Assertions

    /// <summary>
    /// Assert that a cursor position matches expected values.
    /// </summary>
    public static void AssertPosition(TextPosition actual, int expectedLine, int expectedColumn, string? message = null)
    {
        string msg = message ?? $"Position mismatch";
        Assert.True(
            actual.LineNumber == expectedLine && actual.Column == expectedColumn,
            $"{msg}: Expected ({expectedLine}, {expectedColumn}), got ({actual.LineNumber}, {actual.Column})");
    }

    /// <summary>
    /// Assert that two positions are equal.
    /// </summary>
    public static void AssertPositionEquals(TextPosition expected, TextPosition actual, string? message = null)
    {
        AssertPosition(actual, expected.LineNumber, expected.Column, message);
    }

    /// <summary>
    /// Assert that a selection matches expected values.
    /// </summary>
    public static void AssertSelection(Selection actual, int startLine, int startColumn, int endLine, int endColumn, string? message = null)
    {
        string msg = message ?? "Selection mismatch";
        Assert.True(
            actual.SelectionStart.LineNumber == startLine &&
            actual.SelectionStart.Column == startColumn &&
            actual.SelectionEnd.LineNumber == endLine &&
            actual.SelectionEnd.Column == endColumn,
            $"{msg}: Expected ({startLine},{startColumn})-({endLine},{endColumn}), got ({actual.SelectionStart.LineNumber},{actual.SelectionStart.Column})-({actual.SelectionEnd.LineNumber},{actual.SelectionEnd.Column})");
    }

    /// <summary>
    /// Assert that two selections are equal.
    /// </summary>
    public static void AssertSelectionEquals(Selection expected, Selection actual, string? message = null)
    {
        AssertSelection(actual,
            expected.SelectionStart.LineNumber, expected.SelectionStart.Column,
            expected.SelectionEnd.LineNumber, expected.SelectionEnd.Column,
            message);
    }

    /// <summary>
    /// Assert that a selection is empty (collapsed cursor).
    /// </summary>
    public static void AssertSelectionIsEmpty(Selection actual, string? message = null)
    {
        string msg = message ?? "Selection should be empty";
        Assert.True(actual.IsEmpty, $"{msg}: Selection is not empty: ({actual.SelectionStart.LineNumber},{actual.SelectionStart.Column})-({actual.SelectionEnd.LineNumber},{actual.SelectionEnd.Column})");
    }

    /// <summary>
    /// Assert that a selection is not empty.
    /// </summary>
    public static void AssertSelectionIsNotEmpty(Selection actual, string? message = null)
    {
        string msg = message ?? "Selection should not be empty";
        Assert.False(actual.IsEmpty, $"{msg}: Selection is empty at ({actual.SelectionStart.LineNumber},{actual.SelectionStart.Column})");
    }

    #endregion

    #region SingleCursorState Assertions

    /// <summary>
    /// Assert SingleCursorState position.
    /// </summary>
    public static void AssertCursorStatePosition(SingleCursorState state, int expectedLine, int expectedColumn, string? message = null)
    {
        AssertPosition(state.Position, expectedLine, expectedColumn, message ?? "CursorState position mismatch");
    }

    /// <summary>
    /// Assert SingleCursorState selection.
    /// </summary>
    public static void AssertCursorStateSelection(SingleCursorState state, int startLine, int startColumn, int endLine, int endColumn, string? message = null)
    {
        AssertSelection(state.Selection, startLine, startColumn, endLine, endColumn, message ?? "CursorState selection mismatch");
    }

    /// <summary>
    /// Assert that SingleCursorState has no selection.
    /// </summary>
    public static void AssertCursorStateHasNoSelection(SingleCursorState state, string? message = null)
    {
        Assert.False(state.HasSelection(), message ?? "CursorState should have no selection");
    }

    /// <summary>
    /// Assert that SingleCursorState has a selection.
    /// </summary>
    public static void AssertCursorStateHasSelection(SingleCursorState state, string? message = null)
    {
        Assert.True(state.HasSelection(), message ?? "CursorState should have a selection");
    }

    /// <summary>
    /// Assert that two SingleCursorStates are equal.
    /// </summary>
    public static void AssertCursorStatesEqual(SingleCursorState expected, SingleCursorState actual, string? message = null)
    {
        string msg = message ?? "CursorState mismatch";
        Assert.True(expected.Equals(actual), $"{msg}: States are not equal");
    }

    #endregion

    #region Multi-Cursor Assertions

    /// <summary>
    /// Assert that multiple cursors match expected positions.
    /// </summary>
    public static void AssertMultiCursors(IReadOnlyList<TextPosition> actual, params (int line, int column)[] expected)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            AssertPosition(actual[i], expected[i].line, expected[i].column, $"Cursor {i}");
        }
    }

    /// <summary>
    /// Assert that multiple selections match expected values.
    /// </summary>
    public static void AssertMultiSelections(IReadOnlyList<Selection> actual, params (int startLine, int startCol, int endLine, int endCol)[] expected)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            (int startLine, int startCol, int endLine, int endCol) = expected[i];
            AssertSelection(actual[i], startLine, startCol, endLine, endCol, $"Selection {i}");
        }
    }

    /// <summary>
    /// Assert that multiple SingleCursorStates match expected cursor positions.
    /// </summary>
    public static void AssertMultiCursorStates(IReadOnlyList<SingleCursorState> actual, params (int line, int column)[] expectedPositions)
    {
        Assert.Equal(expectedPositions.Length, actual.Count);
        for (int i = 0; i < expectedPositions.Length; i++)
        {
            AssertCursorStatePosition(actual[i], expectedPositions[i].line, expectedPositions[i].column, $"CursorState {i}");
        }
    }

    /// <summary>
    /// Compare multi-cursor selections and return differences.
    /// </summary>
    public static List<string> CompareMultiSelections(IReadOnlyList<Selection> expected, IReadOnlyList<Selection> actual)
    {
        List<string> differences = [];

        if (expected.Count != actual.Count)
        {
            differences.Add($"Selection count mismatch: expected {expected.Count}, got {actual.Count}");
        }

        int count = Math.Min(expected.Count, actual.Count);
        for (int i = 0; i < count; i++)
        {
            Selection e = expected[i];
            Selection a = actual[i];
            if (!e.EqualsSelection(a))
            {
                differences.Add($"Selection {i}: expected ({e.SelectionStart.LineNumber},{e.SelectionStart.Column})-({e.SelectionEnd.LineNumber},{e.SelectionEnd.Column}), got ({a.SelectionStart.LineNumber},{a.SelectionStart.Column})-({a.SelectionEnd.LineNumber},{a.SelectionEnd.Column})");
            }
        }

        return differences;
    }

    #endregion

    #region Cursor Movement Testing

    /// <summary>
    /// Execute a cursor action repeatedly and collect positions.
    /// Useful for testing word movement operations.
    /// </summary>
    public static List<TextPosition> ExecuteAndCollectPositions(
        TextPosition startPosition,
        Func<TextPosition, TextPosition> moveAction,
        int maxIterations = 100)
    {
        List<TextPosition> positions = [startPosition];
        TextPosition current = startPosition;

        for (int i = 0; i < maxIterations; i++)
        {
            TextPosition next = moveAction(current);
            if (next.Equals(current))
            {
                break; // Reached boundary
            }
            positions.Add(next);
            current = next;
        }

        return positions;
    }

    /// <summary>
    /// Test that a cursor action produces expected positions sequence.
    /// </summary>
    public static void AssertCursorMoveSequence(
        TextPosition startPosition,
        Func<TextPosition, TextPosition> moveAction,
        params (int line, int column)[] expectedSequence)
    {
        List<TextPosition> positions = ExecuteAndCollectPositions(startPosition, moveAction, expectedSequence.Length + 1);

        Assert.Equal(expectedSequence.Length + 1, positions.Count); // +1 for start position

        for (int i = 0; i < expectedSequence.Length; i++)
        {
            AssertPosition(positions[i + 1], expectedSequence[i].line, expectedSequence[i].column, $"Step {i + 1}");
        }
    }

    #endregion

    #region Visible Column Helpers

    /// <summary>
    /// Assert visible column calculation.
    /// </summary>
    public static void AssertVisibleColumn(string lineContent, int column, int tabSize, int expectedVisibleColumn)
    {
        int actual = CursorColumnsHelper.VisibleColumnFromColumn(lineContent, column, tabSize);
        Assert.Equal(expectedVisibleColumn, actual);
    }

    /// <summary>
    /// Assert column from visible column calculation.
    /// </summary>
    public static void AssertColumnFromVisible(string lineContent, int visibleColumn, int tabSize, int expectedColumn)
    {
        int actual = CursorColumnsHelper.ColumnFromVisibleColumn(lineContent, visibleColumn, tabSize);
        Assert.Equal(expectedColumn, actual);
    }

    /// <summary>
    /// Assert round-trip conversion between column and visible column.
    /// </summary>
    public static void AssertColumnRoundTrip(string lineContent, int column, int tabSize)
    {
        int visibleColumn = CursorColumnsHelper.VisibleColumnFromColumn(lineContent, column, tabSize);
        int resultColumn = CursorColumnsHelper.ColumnFromVisibleColumn(lineContent, visibleColumn, tabSize);
        Assert.Equal(column, resultColumn);
    }

    #endregion

    #region CursorState Factory Methods

    /// <summary>
    /// Create a SingleCursorState at the given position with no selection.
    /// </summary>
    public static SingleCursorState CreateCursorAt(int line, int column)
    {
        TextPosition pos = new(line, column);
        return new SingleCursorState(
            new Range(line, column, line, column),
            SelectionStartKind.Simple,
            0,
            pos,
            0);
    }

    /// <summary>
    /// Create a SingleCursorState with a selection.
    /// </summary>
    public static SingleCursorState CreateCursorWithSelection(
        int anchorLine, int anchorColumn,
        int activeLine, int activeColumn,
        SelectionStartKind kind = SelectionStartKind.Simple)
    {
        Range selectionStart = new(anchorLine, anchorColumn, anchorLine, anchorColumn);
        TextPosition position = new(activeLine, activeColumn);
        return new SingleCursorState(selectionStart, kind, 0, position, 0);
    }

    /// <summary>
    /// Create a CursorState where model and view states are the same.
    /// </summary>
    public static CursorState CreateFullCursorState(int line, int column)
    {
        SingleCursorState singleState = CreateCursorAt(line, column);
        return new CursorState(singleState, singleState);
    }

    /// <summary>
    /// Create a CursorState with selection where model and view states are the same.
    /// </summary>
    public static CursorState CreateFullCursorStateWithSelection(
        int anchorLine, int anchorColumn,
        int activeLine, int activeColumn)
    {
        SingleCursorState singleState = CreateCursorWithSelection(anchorLine, anchorColumn, activeLine, activeColumn);
        return new CursorState(singleState, singleState);
    }

    #endregion
}
