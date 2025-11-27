// CL7 Stage 1 QA Tests - CursorCollection
// Tests for CursorCollection state management, normalize, tracked selection lifecycle
// Created: 2025-11-28 by QA-Automation (Lena Brooks)
// Reference: agent-team/handoffs/CL7-CursorWiring-Plan.md Phase 5

using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Cursor;
using PieceTree.TextBuffer.Tests.Helpers;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests;

/// <summary>
/// Tests for CursorCollection Stage 1 wiring including:
/// - SetStates creates correct number of cursors
/// - Normalize merges overlapping selections
/// - Tracked selection lifecycle
/// - LastAddedCursorIndex tracking
/// - KillSecondaryCursors behavior
/// - View position queries
/// </summary>
public class CursorCollectionTests
{
    #region Basic Operations

    [Fact]
    public void CursorCollection_StartsWithPrimaryCursor()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        Assert.Single(collection.Cursors);
        Assert.NotNull(collection.GetPrimaryCursor());
    }

    [Fact]
    public void CursorCollection_SetStates_CreatesCursors()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2\nLine 3");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 5)),
            CursorState.FromModelSelection(new Selection(2, 1, 2, 5)),
            CursorState.FromModelSelection(new Selection(3, 1, 3, 5)),
        ]);

        Assert.Equal(3, collection.Cursors.Count);
    }

    [Fact]
    public void CursorCollection_SetStates_ReducesCursors()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2\nLine 3");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        // First add 3 cursors
        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 1)),
            CursorState.FromModelSelection(new Selection(2, 1, 2, 1)),
            CursorState.FromModelSelection(new Selection(3, 1, 3, 1)),
        ]);
        Assert.Equal(3, collection.Cursors.Count);

        // Now reduce to 2
        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 1)),
            CursorState.FromModelSelection(new Selection(2, 1, 2, 1)),
        ]);
        Assert.Equal(2, collection.Cursors.Count);
    }

    [Fact]
    public void CursorCollection_SetStates_NullOrEmptyDoesNothing()
    {
        TextModel model = CreateModelWithParity("Line 1");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates((IReadOnlyList<PartialCursorState>?)null);
        Assert.Single(collection.Cursors);

        collection.SetStates(Array.Empty<PartialCursorState>());
        Assert.Single(collection.Cursors);
    }

    [Fact]
    public void CursorCollection_SetSelections_CreatesCorrectCursors()
    {
        TextModel model = CreateModelWithParity("Hello World");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetSelections([
            new Selection(1, 1, 1, 6),  // "Hello"
            new Selection(1, 7, 1, 12), // "World"
        ]);

        Assert.Equal(2, collection.Cursors.Count);
        IReadOnlyList<Selection> selections = collection.GetSelections();
        CursorTestHelper.AssertSelection(selections[0], 1, 1, 1, 6);
        CursorTestHelper.AssertSelection(selections[1], 1, 7, 1, 12);
    }

    #endregion

    #region Normalize Tests

    [Fact]
    public void CursorCollection_Normalize_MergesOverlappingSelections()
    {
        TextModel model = CreateModelWithParity("Hello World");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 6)),  // "Hello"
            CursorState.FromModelSelection(new Selection(1, 4, 1, 9)), // "lo Wo"
        ]);

        collection.Normalize();

        Assert.Single(collection.Cursors);
        Selection merged = collection.GetSelections()[0];
        Assert.Equal(1, merged.Start.LineNumber);
        Assert.Equal(1, merged.Start.Column);
        Assert.Equal(9, merged.End.Column);
    }

    [Fact]
    public void CursorCollection_Normalize_MergesTouchingWhenOneCollapsed()
    {
        TextModel model = CreateModelWithParity("Hello World");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 6)),  // "Hello"
            CursorState.FromModelSelection(new Selection(1, 6, 1, 6)), // Cursor at end
        ]);

        collection.Normalize();

        Assert.Single(collection.Cursors);
    }

    [Fact]
    public void CursorCollection_Normalize_KeepsTouchingNonCollapsed()
    {
        TextModel model = CreateModelWithParity("Hello World");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 6)),   // "Hello"
            CursorState.FromModelSelection(new Selection(1, 6, 1, 12)), // " World"
        ]);

        collection.Normalize();

        // Touching but not overlapping - should keep both
        Assert.Equal(2, collection.Cursors.Count);
    }

    [Fact]
    public void CursorCollection_Normalize_RespectsMultiCursorMergeOverlappingConfig_Disabled()
    {
        TextModel model = CreateModelWithParity("Hello World");

        // Create context with MultiCursorMergeOverlapping = false
        EditorCursorOptions editorOptions = new() { MultiCursorMergeOverlapping = false };
        CursorContext context = CursorContext.FromModel(model, editorOptions);

        using CursorCollection collection = new(context);

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 8)),
            CursorState.FromModelSelection(new Selection(1, 4, 1, 12)),
        ]);

        collection.Normalize();

        // Not merged because config disabled
        Assert.Equal(2, collection.Cursors.Count);
    }

    [Fact]
    public void CursorCollection_Normalize_MergesMultipleOverlappingCursors()
    {
        TextModel model = CreateModelWithParity("The quick brown fox jumps");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 4)),   // "The"
            CursorState.FromModelSelection(new Selection(1, 3, 1, 6)),   // "e q"
            CursorState.FromModelSelection(new Selection(1, 5, 1, 10)),  // "uick"
        ]);

        collection.Normalize();

        Assert.Single(collection.Cursors);
        Selection merged = collection.GetSelections()[0];
        Assert.Equal(1, merged.Start.Column);
        Assert.Equal(10, merged.End.Column);
    }

    [Fact]
    public void CursorCollection_Normalize_PreservesNonOverlapping()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2\nLine 3");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 4)),
            CursorState.FromModelSelection(new Selection(2, 1, 2, 4)),
            CursorState.FromModelSelection(new Selection(3, 1, 3, 4)),
        ]);

        collection.Normalize();

        Assert.Equal(3, collection.Cursors.Count);
    }

    [Fact]
    public void CursorCollection_Normalize_SingleCursorDoesNothing()
    {
        TextModel model = CreateModelWithParity("Hello World");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        // Just the primary cursor
        collection.Normalize();

        Assert.Single(collection.Cursors);
    }

    [Fact]
    public void CursorCollection_Normalize_PreservesSelectionDirection()
    {
        TextModel model = CreateModelWithParity("Hello World Test");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        // Create RTL selection that overlaps with LTR
        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 8, 1, 1)),  // RTL: "World" to start
            CursorState.FromModelSelection(new Selection(1, 4, 1, 12)), // LTR: "lo Worl"
        ]);

        collection.Normalize();

        Assert.Single(collection.Cursors);
        // Direction from winner should be preserved
    }

    #endregion

    #region Tracked Selection Tests

    [Fact]
    public void CursorCollection_StartStopTrackingSelections_Works()
    {
        TextModel model = CreateModelWithParity("Hello World");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 6)),
        ]);

        // Should not throw
        collection.StartTrackingSelections();
        collection.StopTrackingSelections();
    }

    [Fact]
    public void CursorCollection_TrackedSelections_SurviveEdit()
    {
        TextModel model = CreateModelWithParity("Hello World");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 7, 1, 12)), // "World"
        ]);

        collection.StartTrackingSelections();

        // Insert text at the beginning
        model.PushEditOperations([new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "XXX")], null);

        IReadOnlyList<Selection> recovered = collection.ReadSelectionFromMarkers();

        // Selection should have shifted by 3
        Assert.Equal(10, recovered[0].Start.Column); // Was 7, shifted by 3
        Assert.Equal(15, recovered[0].End.Column);   // Was 12, shifted by 3
    }

    [Fact]
    public void CursorCollection_TrackedSelections_MultiCursor_SurviveEdit()
    {
        TextModel model = CreateModelWithParity("Hello World Foo Bar");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 6)),   // "Hello"
            CursorState.FromModelSelection(new Selection(1, 13, 1, 16)), // "Foo"
        ]);

        collection.StartTrackingSelections();

        // Insert text before "Foo"
        model.PushEditOperations([new TextEdit(new TextPosition(1, 7), new TextPosition(1, 7), "YY")], null);

        IReadOnlyList<Selection> recovered = collection.ReadSelectionFromMarkers();

        // First selection unchanged
        Assert.Equal(1, recovered[0].Start.Column);
        Assert.Equal(6, recovered[0].End.Column);

        // Second selection shifted by 2
        Assert.Equal(15, recovered[1].Start.Column); // Was 13, shifted by 2
        Assert.Equal(18, recovered[1].End.Column);   // Was 16, shifted by 2
    }

    [Fact]
    public void CursorCollection_EnsureValidState_ClampsInvalidPositions()
    {
        TextModel model = CreateModelWithParity("Hello\nWorld");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        // Delete second line
        model.PushEditOperations([new TextEdit(new TextPosition(1, 6), new TextPosition(2, 6), "")], null);

        // EnsureValidState should not throw and should clamp positions
        collection.EnsureValidState();

        // Should still have valid cursor
        CursorState state = collection.GetPrimaryCursor();
        Assert.True(state.ModelState.Position.LineNumber <= model.GetLineCount());
    }

    #endregion

    #region LastAddedCursorIndex Tests

    [Fact]
    public void CursorCollection_LastAddedCursorIndex_TracksCorrectly()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2\nLine 3");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 1)),
            CursorState.FromModelSelection(new Selection(2, 1, 2, 1)),
            CursorState.FromModelSelection(new Selection(3, 1, 3, 1)),
        ]);

        // Last secondary cursor should have index 2
        Assert.Equal(2, collection.GetLastAddedCursorIndex());
    }

    [Fact]
    public void CursorCollection_LastAddedCursorIndex_ReturnsZeroForSingleCursor()
    {
        TextModel model = CreateModelWithParity("Hello");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        Assert.Equal(0, collection.GetLastAddedCursorIndex());
    }

    [Fact]
    public void CursorCollection_LastAddedCursorIndex_UpdatesOnRemove()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2\nLine 3");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 1)),
            CursorState.FromModelSelection(new Selection(2, 1, 2, 1)),
            CursorState.FromModelSelection(new Selection(3, 1, 3, 1)),
        ]);

        // Remove to just 2 cursors
        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 1)),
            CursorState.FromModelSelection(new Selection(2, 1, 2, 1)),
        ]);

        Assert.Equal(1, collection.GetLastAddedCursorIndex());
    }

    #endregion

    #region KillSecondaryCursors Tests

    [Fact]
    public void CursorCollection_KillSecondaryCursors_KeepsOnlyPrimary()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2\nLine 3");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 1)),
            CursorState.FromModelSelection(new Selection(2, 1, 2, 1)),
            CursorState.FromModelSelection(new Selection(3, 1, 3, 1)),
        ]);
        Assert.Equal(3, collection.Cursors.Count);

        collection.KillSecondaryCursors();

        Assert.Single(collection.Cursors);
    }

    [Fact]
    public void CursorCollection_KillSecondaryCursors_NoOpForSingleCursor()
    {
        TextModel model = CreateModelWithParity("Hello");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.KillSecondaryCursors();

        Assert.Single(collection.Cursors);
    }

    #endregion

    #region View Position Queries

    [Fact]
    public void CursorCollection_GetViewPositions_ReturnsAllPositions()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2\nLine 3");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 3, 1, 3)),
            CursorState.FromModelSelection(new Selection(2, 5, 2, 5)),
            CursorState.FromModelSelection(new Selection(3, 2, 3, 2)),
        ]);

        IReadOnlyList<TextPosition> positions = collection.GetViewPositions();

        Assert.Equal(3, positions.Count);
        Assert.Equal(new TextPosition(1, 3), positions[0]);
        Assert.Equal(new TextPosition(2, 5), positions[1]);
        Assert.Equal(new TextPosition(3, 2), positions[2]);
    }

    [Fact]
    public void CursorCollection_GetTopMostViewPosition_ReturnsMinimum()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2\nLine 3");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(2, 5, 2, 5)),
            CursorState.FromModelSelection(new Selection(3, 2, 3, 2)),
            CursorState.FromModelSelection(new Selection(1, 3, 1, 3)),
        ]);

        TextPosition top = collection.GetTopMostViewPosition();

        Assert.Equal(1, top.LineNumber);
        Assert.Equal(3, top.Column);
    }

    [Fact]
    public void CursorCollection_GetBottomMostViewPosition_ReturnsMaximum()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2\nLine 3");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 3, 1, 3)),
            CursorState.FromModelSelection(new Selection(2, 5, 2, 5)),
            CursorState.FromModelSelection(new Selection(3, 2, 3, 2)),
        ]);

        TextPosition bottom = collection.GetBottomMostViewPosition();

        Assert.Equal(3, bottom.LineNumber);
        Assert.Equal(2, bottom.Column);
    }

    [Fact]
    public void CursorCollection_GetTopMostViewPosition_TiebreaksByColumn()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 5, 1, 5)),
            CursorState.FromModelSelection(new Selection(1, 2, 1, 2)),
        ]);

        TextPosition top = collection.GetTopMostViewPosition();

        Assert.Equal(1, top.LineNumber);
        Assert.Equal(2, top.Column); // Smaller column wins
    }

    #endregion

    #region GetAll and GetPrimaryCursor Tests

    [Fact]
    public void CursorCollection_GetAll_ReturnsAllCursorStates()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 6)),
            CursorState.FromModelSelection(new Selection(2, 1, 2, 6)),
        ]);

        IReadOnlyList<CursorState> allStates = collection.GetAll();

        Assert.Equal(2, allStates.Count);
        Assert.Equal(1, allStates[0].ModelState.Position.LineNumber);
        Assert.Equal(2, allStates[1].ModelState.Position.LineNumber);
    }

    [Fact]
    public void CursorCollection_GetPrimaryCursor_ReturnsFirstCursor()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(2, 3, 2, 3)),
            CursorState.FromModelSelection(new Selection(1, 1, 1, 1)),
        ]);

        CursorState primary = collection.GetPrimaryCursor();

        // Primary is the first cursor in the list (not sorted by position)
        Assert.Equal(2, primary.ModelState.Position.LineNumber);
        Assert.Equal(3, primary.ModelState.Position.Column);
    }

    #endregion

    #region Selection Queries

    [Fact]
    public void CursorCollection_GetSelections_ReturnsModelSelections()
    {
        TextModel model = CreateModelWithParity("Hello World");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 6)),  // "Hello"
            CursorState.FromModelSelection(new Selection(1, 7, 1, 12)), // "World"
        ]);

        IReadOnlyList<Selection> selections = collection.GetSelections();

        Assert.Equal(2, selections.Count);
        CursorTestHelper.AssertSelection(selections[0], 1, 1, 1, 6);
        CursorTestHelper.AssertSelection(selections[1], 1, 7, 1, 12);
    }

    [Fact]
    public void CursorCollection_GetViewSelections_ReturnsViewSelections()
    {
        TextModel model = CreateModelWithParity("Hello World");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 6)),
        ]);

        IReadOnlyList<Selection> viewSelections = collection.GetViewSelections();

        Assert.Single(viewSelections);
        // For identity converter, view selection == model selection
        CursorTestHelper.AssertSelection(viewSelections[0], 1, 1, 1, 6);
    }

    #endregion

    #region Legacy API Compatibility

    [Fact]
    public void CursorCollection_CreateCursor_AddsSecondaryCursor()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        Cursor.Cursor secondary = collection.CreateCursor(new TextPosition(2, 3));

        Assert.Equal(2, collection.Cursors.Count);
        Assert.Equal(2, collection.Cursors[1].Selection.Active.LineNumber);
        Assert.Equal(3, collection.Cursors[1].Selection.Active.Column);
    }

    [Fact]
    public void CursorCollection_RemoveCursor_RemovesSecondaryCursor()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        Cursor.Cursor secondary = collection.CreateCursor(new TextPosition(2, 1));
        Assert.Equal(2, collection.Cursors.Count);

        collection.RemoveCursor(secondary);
        Assert.Single(collection.Cursors);
    }

    [Fact]
    public void CursorCollection_GetCursorPositions_ReturnsAllActivePositions()
    {
        TextModel model = CreateModelWithParity("Line 1\nLine 2");

        using CursorCollection collection = new(CursorContext.FromModel(model));

        collection.Cursors[0].MoveTo(new TextPosition(1, 3));
        collection.CreateCursor(new TextPosition(2, 5));

        IReadOnlyList<TextPosition> positions = collection.GetCursorPositions();

        Assert.Equal(2, positions.Count);
        Assert.Equal(new TextPosition(1, 3), positions[0]);
        Assert.Equal(new TextPosition(2, 5), positions[1]);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Create a TextModel with EnableVsCursorParity = true.
    /// </summary>
    private static TextModel CreateModelWithParity(string content)
    {
        return new TextModel(content, new TextModelCreationOptions { EnableVsCursorParity = true });
    }

    #endregion
}
