// Source: ts/src/vs/editor/common/cursorCommon.ts
// - Class: CursorState, SingleCursorState, PartialModelCursorState, PartialViewCursorState (Lines: 271-380)
// - Enum: SelectionStartKind (Lines: 260-264)
// Ported: 2025-11-22
// Updated: 2025-11-26 (WS4-PORT-Core Stage 0: TS parity architecture)

using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Indicates how the selection was started.
/// </summary>
public enum SelectionStartKind
{
    /// <summary>
    /// Simple cursor placement.
    /// </summary>
    Simple = 0,

    /// <summary>
    /// Selection started by double-click (word selection).
    /// </summary>
    Word = 1,

    /// <summary>
    /// Selection started by triple-click (line selection).
    /// </summary>
    Line = 2,
}

/// <summary>
/// Represents the cursor state on either the model or on the view model.
/// </summary>
public sealed class SingleCursorState
{
    private readonly object _singleCursorStateBrand = new();

    /// <summary>
    /// The range where the selection started.
    /// </summary>
    public Range SelectionStart { get; }

    /// <summary>
    /// How the selection was started.
    /// </summary>
    public SelectionStartKind SelectionStartKind { get; }

    /// <summary>
    /// Leftover visible columns from selection start (for column select).
    /// </summary>
    public int SelectionStartLeftoverVisibleColumns { get; }

    /// <summary>
    /// The current cursor position.
    /// </summary>
    public TextPosition Position { get; }

    /// <summary>
    /// Leftover visible columns from current position (for column select).
    /// </summary>
    public int LeftoverVisibleColumns { get; }

    /// <summary>
    /// The computed selection based on selectionStart and position.
    /// </summary>
    public Selection Selection { get; }

    public SingleCursorState(
        Range selectionStart,
        SelectionStartKind selectionStartKind,
        int selectionStartLeftoverVisibleColumns,
        TextPosition position,
        int leftoverVisibleColumns)
    {
        SelectionStart = selectionStart;
        SelectionStartKind = selectionStartKind;
        SelectionStartLeftoverVisibleColumns = selectionStartLeftoverVisibleColumns;
        Position = position;
        LeftoverVisibleColumns = leftoverVisibleColumns;
        Selection = ComputeSelection(selectionStart, position);
    }

    /// <summary>
    /// Check if this state equals another state.
    /// </summary>
    public bool Equals(SingleCursorState other)
    {
        if (other is null)
        {
            return false;
        }

        return SelectionStartLeftoverVisibleColumns == other.SelectionStartLeftoverVisibleColumns
            && LeftoverVisibleColumns == other.LeftoverVisibleColumns
            && SelectionStartKind == other.SelectionStartKind
            && Position.Equals(other.Position)
            && Range.EqualsRange(SelectionStart, other.SelectionStart);
    }

    /// <summary>
    /// Check if there is a selection (non-empty range or position outside selection start).
    /// </summary>
    public bool HasSelection()
    {
        return !Selection.IsEmpty || !SelectionStart.IsEmpty;
    }

    /// <summary>
    /// Create a new state after moving.
    /// </summary>
    /// <param name="inSelectionMode">Whether to extend the selection.</param>
    /// <param name="lineNumber">New line number.</param>
    /// <param name="column">New column.</param>
    /// <param name="leftoverVisibleColumns">New leftover visible columns.</param>
    public SingleCursorState Move(bool inSelectionMode, int lineNumber, int column, int leftoverVisibleColumns)
    {
        if (inSelectionMode)
        {
            // Move just position, keep selection start
            return new SingleCursorState(
                SelectionStart,
                SelectionStartKind,
                SelectionStartLeftoverVisibleColumns,
                new TextPosition(lineNumber, column),
                leftoverVisibleColumns);
        }
        else
        {
            // Move everything - collapse to new position
            return new SingleCursorState(
                new Range(lineNumber, column, lineNumber, column),
                SelectionStartKind.Simple,
                leftoverVisibleColumns,
                new TextPosition(lineNumber, column),
                leftoverVisibleColumns);
        }
    }

    /// <summary>
    /// Compute the selection from selectionStart and position.
    /// </summary>
    private static Selection ComputeSelection(Range selectionStart, TextPosition position)
    {
        TextPosition startPos = selectionStart.GetStartPosition();
        TextPosition endPos = selectionStart.GetEndPosition();

        if (selectionStart.IsEmpty || !position.IsBeforeOrEqual(startPos))
        {
            // Normal case: selection from start to position
            return Selection.FromPositions(startPos, position);
        }
        else
        {
            // Position is before selection start, use end of selectionStart
            return Selection.FromPositions(endPos, position);
        }
    }

    public override bool Equals(object? obj) => obj is SingleCursorState other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(SelectionStart, SelectionStartKind, Position, LeftoverVisibleColumns);
}

/// <summary>
/// Represents a full cursor state with both model and view states.
/// </summary>
public sealed class CursorState
{
    private readonly object _cursorStateBrand = new();

    /// <summary>
    /// The model-side cursor state.
    /// </summary>
    public SingleCursorState ModelState { get; }

    /// <summary>
    /// The view-side cursor state.
    /// </summary>
    public SingleCursorState ViewState { get; }

    public CursorState(SingleCursorState modelState, SingleCursorState viewState)
    {
        ModelState = modelState ?? throw new ArgumentNullException(nameof(modelState));
        ViewState = viewState ?? throw new ArgumentNullException(nameof(viewState));
    }

    /// <summary>
    /// Check if this state equals another state.
    /// </summary>
    public bool Equals(CursorState other)
    {
        if (other is null)
        {
            return false;
        }

        return ViewState.Equals(other.ViewState) && ModelState.Equals(other.ModelState);
    }

    /// <summary>
    /// Create a PartialModelCursorState from a SingleCursorState.
    /// </summary>
    public static PartialModelCursorState FromModelState(SingleCursorState modelState)
    {
        return new PartialModelCursorState(modelState);
    }

    /// <summary>
    /// Create a PartialViewCursorState from a SingleCursorState.
    /// </summary>
    public static PartialViewCursorState FromViewState(SingleCursorState viewState)
    {
        return new PartialViewCursorState(viewState);
    }

    /// <summary>
    /// Create a PartialModelCursorState from a Selection.
    /// This is a factory method to convert a selection into a cursor state.
    /// </summary>
    public static PartialModelCursorState FromModelSelection(Selection selection)
    {
        Selection liftedSelection = selection;
        SingleCursorState modelState = new(
            Range.FromPositions(liftedSelection.GetSelectionStart(), liftedSelection.GetSelectionStart()),
            SelectionStartKind.Simple,
            0,
            liftedSelection.GetPosition(),
            0);
        return FromModelState(modelState);
    }

    /// <summary>
    /// Create PartialModelCursorStates from multiple selections.
    /// </summary>
    public static IReadOnlyList<PartialModelCursorState> FromModelSelections(IReadOnlyList<Selection> selections)
    {
        PartialModelCursorState[] states = new PartialModelCursorState[selections.Count];
        for (int i = 0; i < selections.Count; i++)
        {
            states[i] = FromModelSelection(selections[i]);
        }
        return states;
    }

    public override bool Equals(object? obj) => obj is CursorState other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(ModelState, ViewState);
}

/// <summary>
/// A partial cursor state containing only the model state.
/// Used when commands specify model coordinates only.
/// </summary>
public sealed class PartialModelCursorState
{
    /// <summary>
    /// The model-side cursor state.
    /// </summary>
    public SingleCursorState ModelState { get; }

    /// <summary>
    /// The view-side cursor state (always null for partial model states).
    /// </summary>
    public SingleCursorState? ViewState => null;

    public PartialModelCursorState(SingleCursorState modelState)
    {
        ModelState = modelState ?? throw new ArgumentNullException(nameof(modelState));
    }
}

/// <summary>
/// A partial cursor state containing only the view state.
/// Used when commands specify view coordinates only.
/// </summary>
public sealed class PartialViewCursorState
{
    /// <summary>
    /// The model-side cursor state (always null for partial view states).
    /// </summary>
    public SingleCursorState? ModelState => null;

    /// <summary>
    /// The view-side cursor state.
    /// </summary>
    public SingleCursorState ViewState { get; }

    public PartialViewCursorState(SingleCursorState viewState)
    {
        ViewState = viewState ?? throw new ArgumentNullException(nameof(viewState));
    }
}

/// <summary>
/// Represents a partial cursor state that can be either model-only, view-only, or full.
/// This is the union type for command interop.
/// </summary>
public abstract class PartialCursorState
{
    public abstract SingleCursorState? ModelState { get; }
    public abstract SingleCursorState? ViewState { get; }

    /// <summary>
    /// Create from a full CursorState.
    /// </summary>
    public static PartialCursorState FromFull(CursorState state) => new FullPartialCursorState(state);

    /// <summary>
    /// Create from a model-only state.
    /// </summary>
    public static PartialCursorState FromModel(SingleCursorState modelState) => new ModelOnlyPartialCursorState(modelState);

    /// <summary>
    /// Create from a view-only state.
    /// </summary>
    public static PartialCursorState FromView(SingleCursorState viewState) => new ViewOnlyPartialCursorState(viewState);

    private sealed class FullPartialCursorState : PartialCursorState
    {
        private readonly CursorState _state;
        public FullPartialCursorState(CursorState state) => _state = state;
        public override SingleCursorState ModelState => _state.ModelState;
        public override SingleCursorState ViewState => _state.ViewState;
    }

    private sealed class ModelOnlyPartialCursorState : PartialCursorState
    {
        private readonly SingleCursorState _modelState;
        public ModelOnlyPartialCursorState(SingleCursorState modelState) => _modelState = modelState;
        public override SingleCursorState ModelState => _modelState;
        public override SingleCursorState? ViewState => null;
    }

    private sealed class ViewOnlyPartialCursorState : PartialCursorState
    {
        private readonly SingleCursorState _viewState;
        public ViewOnlyPartialCursorState(SingleCursorState viewState) => _viewState = viewState;
        public override SingleCursorState? ModelState => null;
        public override SingleCursorState ViewState => _viewState;
    }
}

#region Legacy Compatibility

/// <summary>
/// Legacy CursorState record for backward compatibility with existing code.
/// Will be deprecated once full cursor parity migration is complete.
/// </summary>
[Obsolete("Use SingleCursorState/CursorState instead. This record will be removed after cursor parity migration.")]
public sealed record class LegacyCursorState
{
    public int OwnerId { get; init; }
    public Selection Selection { get; init; }
    public int StickyColumn { get; init; }
    public string[] DecorationIds { get; init; } = Array.Empty<string>();

    public LegacyCursorState(int ownerId, Selection selection, int stickyColumn = -1, string[]? decorationIds = null)
    {
        OwnerId = ownerId;
        Selection = selection;
        StickyColumn = stickyColumn;
        DecorationIds = decorationIds ?? Array.Empty<string>();
    }
}

#endregion
