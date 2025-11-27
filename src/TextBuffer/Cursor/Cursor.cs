// Source: ts/src/vs/editor/common/cursor/oneCursor.ts
// - Class: Cursor (Lines: 15-200)
// Ported: 2025-11-22
// Updated: 2025-11-28 (CL7-Stage1: State fields, _setState, tracked ranges)

using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Cursor;

public class Cursor : IDisposable
{
    private readonly TextModel _model;
    private CursorContext _context;  // Stage 1: CursorContext

    // Stage 1: Dual-mode state storage
    private SingleCursorState? _modelState;
    private SingleCursorState? _viewState;

    // Legacy fields (used when EnableVsCursorParity is false)
    private Selection _selection;
    private int _stickyColumn = -1;
    private bool _isColumnSelecting = false;
    private int _columnSelectAnchorVisible = -1;
    private TextPosition _columnSelectAnchorPosition = new(1, 1);
    private readonly int _ownerId;
    private string[] _decorationIds = Array.Empty<string>();
    private bool _disposed;

    // Stage 1: Tracked range support (matches TS)
    private string? _selTrackedRange;
    private bool _trackSelection = true;

    /// <summary>
    /// Create a Cursor from a CursorContext (Stage 1 path).
    /// </summary>
    public Cursor(CursorContext context)
    {
        _model = context.Model ?? throw new ArgumentNullException(nameof(context));
        _context = context;
        _selTrackedRange = null;
        _trackSelection = true;
        _ownerId = _model.AllocateDecorationOwnerId();

        // Initialize with default state at position (1,1)
        SingleCursorState initialState = new(
            new Range(1, 1, 1, 1),
            SelectionStartKind.Simple,
            0,
            new TextPosition(1, 1),
            0);

        _setState(context, initialState, initialState);

        _model.OnDidChangeOptions += HandleOptionsChanged;
        UpdateDecorations();
    }

    /// <summary>
    /// Legacy constructor for backward compatibility.
    /// </summary>
    public Cursor(TextModel model) : this(CursorContext.FromModel(model))
    {
    }

    public Selection Selection => _selection;

    /// <summary>
    /// Update the CursorContext backing this cursor (used when editor/view config changes).
    /// </summary>
    internal void UpdateContext(CursorContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        if (_model.GetOptions().EnableVsCursorParity)
        {
            _setState(_context, _modelState, _viewState);
        }
    }

    /// <summary>
    /// Get the full CursorState (model + view).
    /// </summary>
    public CursorState AsCursorState()
    {
        if (_modelState == null || _viewState == null)
        {
            // Fallback for legacy mode or uninitialized state
            SingleCursorState legacyState = new(
                Range.FromPositions(_selection.Anchor, _selection.Anchor),
                SelectionStartKind.Simple,
                _stickyColumn >= 0 ? _stickyColumn : 0,
                _selection.Active,
                _stickyColumn >= 0 ? _stickyColumn : 0);
            return new CursorState(legacyState, legacyState);
        }
        return new CursorState(_modelState, _viewState);
    }

    /// <summary>
    /// Set the cursor state (Stage 1 API).
    /// </summary>
    public void SetState(CursorContext context, SingleCursorState? modelState, SingleCursorState? viewState)
    {
        ArgumentNullException.ThrowIfNull(context);
        _setState(context, modelState, viewState);
        UpdateDecorations();
    }

    /// <summary>
    /// Re-validate the current state against the model.
    /// </summary>
    public void EnsureValidState(CursorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _setState(context, _modelState, _viewState);
    }

    /// <summary>
    /// Start tracking the selection for edit recovery.
    /// </summary>
    public void StartTrackingSelection(CursorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _trackSelection = true;
        UpdateTrackedRange(context);
    }

    /// <summary>
    /// Stop tracking the selection.
    /// </summary>
    public void StopTrackingSelection(CursorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _trackSelection = false;
        RemoveTrackedRange(context);
    }

    /// <summary>
    /// Read the selection from tracked markers after an edit.
    /// </summary>
    public Selection ReadSelectionFromMarkers(CursorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        TextModel model = context.Model;

        if (_selTrackedRange == null || !model.GetOptions().EnableVsCursorParity)
        {
            return _selection;
        }

        Range? range = model._getTrackedRange(_selTrackedRange);
        if (range == null)
        {
            return _selection;
        }

        if (_modelState != null && _modelState.Selection.IsEmpty && !range.Value.IsEmpty)
        {
            // Avoid selecting text when recovering from markers
            return Selection.FromRange(range.Value.CollapseToEnd(), _modelState.Selection.Direction);
        }

        return Selection.FromRange(range.Value, _modelState?.Selection.Direction ?? SelectionDirection.LTR);
    }

    public void MoveTo(TextPosition position)
    {
        TextPosition validated = ValidatePosition(position);
        _selection = new Selection(validated, validated);
        _stickyColumn = -1;

        if (_model.GetOptions().EnableVsCursorParity)
        {
            SingleCursorState newState = new(
                new Range(validated.LineNumber, validated.Column, validated.LineNumber, validated.Column),
                SelectionStartKind.Simple,
                0,
                validated,
                0);
            _setState(_context, newState, null);
        }

        UpdateDecorations();
    }

    public void StartColumnSelection()
    {
        int tabSize = _model.GetOptions().TabSize;
        _isColumnSelecting = true;
        _columnSelectAnchorVisible = CursorColumns.GetVisibleColumnFromPosition(_model, _selection.Active, tabSize);
        _columnSelectAnchorPosition = _selection.Active;
    }

    public void ColumnSelectTo(TextPosition active)
    {
        if (!_isColumnSelecting)
        {
            // fall back to normal selection
            SelectTo(active);
            return;
        }

        int tabSize = _model.GetOptions().TabSize;
        TextPosition anchorReal = CursorColumns.GetPositionFromVisibleColumn(_model, _columnSelectAnchorPosition.LineNumber, _columnSelectAnchorVisible, tabSize);
        int activeVisible = CursorColumns.GetVisibleColumnFromPosition(_model, active, tabSize);
        TextPosition activeReal = CursorColumns.GetPositionFromVisibleColumn(_model, active.LineNumber, activeVisible, tabSize);
        _selection = new Selection(anchorReal, activeReal);
        UpdateDecorations();
    }

    public void EndColumnSelection()
    {
        _isColumnSelecting = false;
        _columnSelectAnchorVisible = -1;
    }

    public void SelectTo(TextPosition position)
    {
        TextPosition validated = ValidatePosition(position);
        _selection = new Selection(_selection.Anchor, validated);
        _stickyColumn = -1;
        UpdateDecorations();
    }

    public void MoveLeft()
    {
        TextPosition current = _selection.Active;
        if (current.Column > 1)
        {
            MoveTo(new TextPosition(current.LineNumber, current.Column - 1));
        }
        else if (current.LineNumber > 1)
        {
            int prevLine = current.LineNumber - 1;
            int len = _model.GetLineContent(prevLine).Length;
            MoveTo(new TextPosition(prevLine, len + 1));
        }
    }

    public void MoveRight()
    {
        TextPosition current = _selection.Active;
        int lineLen = _model.GetLineContent(current.LineNumber).Length;

        if (current.Column <= lineLen)
        {
            MoveTo(new TextPosition(current.LineNumber, current.Column + 1));
        }
        else if (current.LineNumber < _model.GetLineCount())
        {
            MoveTo(new TextPosition(current.LineNumber + 1, 1));
        }
    }

    public void MoveUp()
    {
        TextPosition current = _selection.Active;
        if (current.LineNumber > 1)
        {
            int sticky = _stickyColumn;
            if (sticky == -1)
            {
                sticky = current.Column;
            }

            int newLine = current.LineNumber - 1;
            int len = _model.GetLineContent(newLine).Length;
            int newCol = Math.Min(sticky, len + 1);

            MoveTo(new TextPosition(newLine, newCol));
            _stickyColumn = sticky;
        }
    }

    public void MoveDown()
    {
        TextPosition current = _selection.Active;
        if (current.LineNumber < _model.GetLineCount())
        {
            int sticky = _stickyColumn;
            if (sticky == -1)
            {
                sticky = current.Column;
            }

            int newLine = current.LineNumber + 1;
            int len = _model.GetLineContent(newLine).Length;
            int newCol = Math.Min(sticky, len + 1);

            MoveTo(new TextPosition(newLine, newCol));
            _stickyColumn = sticky;
        }
    }

    public void MoveWordLeft(string? wordSeparators = null)
    {
        TextPosition current = _selection.Active;
        TextPosition target = WordOperations.MoveWordLeft(_model, current, wordSeparators);
        MoveTo(target);
    }

    public void MoveWordRight(string? wordSeparators = null)
    {
        TextPosition current = _selection.Active;
        TextPosition target = WordOperations.MoveWordRight(_model, current, wordSeparators);
        MoveTo(target);
    }

    public void SelectWordLeft(string? wordSeparators = null)
    {
        _selection = WordOperations.SelectWordLeft(_model, _selection, wordSeparators);
        UpdateDecorations();
    }

    public void SelectWordRight(string? wordSeparators = null)
    {
        _selection = WordOperations.SelectWordRight(_model, _selection, wordSeparators);
        UpdateDecorations();
    }

    public void DeleteWordLeft(string? wordSeparators = null)
    {
        Selection sel = WordOperations.DeleteWordLeft(_model, _selection, wordSeparators);
        TextPosition start = sel.Start;
        TextPosition end = sel.End;
        TextEdit edit = new(start, end, string.Empty);
        _model.PushEditOperations([edit], null);
        // Move cursor to start
        MoveTo(start);
    }

    /// <summary>
    /// Dispose with context (Stage 1 API for CursorCollection).
    /// </summary>
    public void Dispose(CursorContext context)
    {
        StopTrackingSelection(context);
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _model.OnDidChangeOptions -= HandleOptionsChanged;
        _model.RemoveAllDecorations(_ownerId);

        // Clean up tracked range if any
        if (_selTrackedRange != null)
        {
            _model._setTrackedRange(_selTrackedRange, null, TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);
            _selTrackedRange = null;
        }
    }

    /// <summary>
    /// Core state setter - ported from TS oneCursor.ts _setState
    /// </summary>
    private void _setState(CursorContext context, SingleCursorState? modelState, SingleCursorState? viewState)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (modelState == null && viewState == null)
        {
            return;
        }

        if (!_model.GetOptions().EnableVsCursorParity)
        {
            // Legacy path: update _selection and _stickyColumn directly
            UpdateLegacyState(modelState, viewState);
            return;
        }

        // Validate view state if provided
        if (viewState != null)
        {
            viewState = ValidateViewState(context.ViewModel, viewState);
        }

        // Compute missing side
        if (modelState == null && viewState != null)
        {
            // View → Model conversion
            Range selectionStart = _model.ValidateRange(
                context.CoordinatesConverter.ConvertViewRangeToModelRange(viewState.SelectionStart));
            TextPosition position = _model.ValidatePosition(
                context.CoordinatesConverter.ConvertViewPositionToModelPosition(viewState.Position));
            modelState = new SingleCursorState(
                selectionStart,
                viewState.SelectionStartKind,
                viewState.SelectionStartLeftoverVisibleColumns,
                position,
                viewState.LeftoverVisibleColumns);
        }
        else if (modelState != null)
        {
            // Validate model state
            Range selectionStart = _model.ValidateRange(modelState.SelectionStart);
            int ssLeftover = modelState.SelectionStart.EqualsRange(selectionStart)
                ? modelState.SelectionStartLeftoverVisibleColumns : 0;
            TextPosition position = _model.ValidatePosition(modelState.Position);
            int leftover = modelState.Position.Equals(position)
                ? modelState.LeftoverVisibleColumns : 0;
            modelState = new SingleCursorState(
                selectionStart, modelState.SelectionStartKind, ssLeftover, position, leftover);
        }

        // Compute view state from model if missing
        if (viewState == null && modelState != null)
        {
            // Model → View conversion
            TextPosition vs1 = context.CoordinatesConverter.ConvertModelPositionToViewPosition(
                new TextPosition(modelState.SelectionStart.StartLineNumber, modelState.SelectionStart.StartColumn));
            TextPosition vs2 = context.CoordinatesConverter.ConvertModelPositionToViewPosition(
                new TextPosition(modelState.SelectionStart.EndLineNumber, modelState.SelectionStart.EndColumn));
            Range viewSelectionStart = new(vs1.LineNumber, vs1.Column, vs2.LineNumber, vs2.Column);
            TextPosition viewPosition = context.CoordinatesConverter.ConvertModelPositionToViewPosition(modelState.Position);
            viewState = new SingleCursorState(
                viewSelectionStart,
                modelState.SelectionStartKind,
                modelState.SelectionStartLeftoverVisibleColumns,
                viewPosition,
                modelState.LeftoverVisibleColumns);
        }
        else if (viewState != null && modelState != null)
        {
            // Validate view state against model
            Range viewSS = context.CoordinatesConverter.ValidateViewRange(
                viewState.SelectionStart, modelState.SelectionStart);
            TextPosition viewPos = context.CoordinatesConverter.ValidateViewPosition(
                viewState.Position, modelState.Position);
            viewState = new SingleCursorState(
                viewSS,
                modelState.SelectionStartKind,
                modelState.SelectionStartLeftoverVisibleColumns,
                viewPos,
                modelState.LeftoverVisibleColumns);
        }

        _modelState = modelState;
        _viewState = viewState;

        // Sync legacy fields for consumers not yet migrated
        if (modelState != null)
        {
            _selection = modelState.Selection;
            _stickyColumn = modelState.LeftoverVisibleColumns;
        }

        UpdateTrackedRange(context);
    }

    /// <summary>
    /// Update legacy state fields from state objects (when flag is off).
    /// </summary>
    private void UpdateLegacyState(SingleCursorState? modelState, SingleCursorState? viewState)
    {
        SingleCursorState? stateToUse = modelState ?? viewState;
        if (stateToUse != null)
        {
            _selection = stateToUse.Selection;
            _stickyColumn = stateToUse.LeftoverVisibleColumns;
            _modelState = modelState;
            _viewState = viewState;
        }
    }

    /// <summary>
    /// Validate a view state against the view model.
    /// </summary>
    private static SingleCursorState ValidateViewState(ICursorSimpleModel viewModel, SingleCursorState viewState)
    {
        static TextPosition ValidatePositionWithCache(ICursorSimpleModel vm, TextPosition position, TextPosition cacheInput, TextPosition cacheOutput)
        {
            if (position.Equals(cacheInput))
            {
                return cacheOutput;
            }

            return vm.NormalizePosition(position, PositionAffinity.None);
        }

        TextPosition position = viewState.Position;
        TextPosition selectionStartStart = viewState.SelectionStart.GetStartPosition();
        TextPosition selectionStartEnd = viewState.SelectionStart.GetEndPosition();

        TextPosition validPosition = viewModel.NormalizePosition(position, PositionAffinity.None);
        TextPosition validSelectionStartStart = ValidatePositionWithCache(viewModel, selectionStartStart, position, validPosition);
        TextPosition validSelectionStartEnd = ValidatePositionWithCache(viewModel, selectionStartEnd, selectionStartStart, validSelectionStartStart);

        if (position.Equals(validPosition)
            && selectionStartStart.Equals(validSelectionStartStart)
            && selectionStartEnd.Equals(validSelectionStartEnd))
        {
            return viewState;
        }

        Range validatedSelectionStart = Range.FromPositions(validSelectionStartStart, validSelectionStartEnd);
        int selectionStartDelta = selectionStartStart.Column - validSelectionStartStart.Column;
        int leftoverDelta = position.Column - validPosition.Column;

        return new SingleCursorState(
            validatedSelectionStart,
            viewState.SelectionStartKind,
            viewState.SelectionStartLeftoverVisibleColumns + selectionStartDelta,
            validPosition,
            viewState.LeftoverVisibleColumns + leftoverDelta);
    }

    /// <summary>
    /// Update the tracked range to match current selection.
    /// </summary>
    private void UpdateTrackedRange(CursorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        TextModel model = context.Model;

        if (!_trackSelection || !model.GetOptions().EnableVsCursorParity)
        {
            return;
        }

        if (_modelState == null)
        {
            return;
        }

        _selTrackedRange = model._setTrackedRange(
            _selTrackedRange,
            _modelState.Selection.ToRange(),
            TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);
    }

    /// <summary>
    /// Remove the tracked range.
    /// </summary>
    private void RemoveTrackedRange(CursorContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        TextModel model = context.Model;

        _selTrackedRange = model._setTrackedRange(
            _selTrackedRange,
            null,
            TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);
    }

    private TextPosition ValidatePosition(TextPosition position)
    {
        int lineCount = _model.GetLineCount();
        int line = Math.Clamp(position.LineNumber, 1, lineCount);

        int lineLen = _model.GetLineContent(line).Length;
        int col = Math.Clamp(position.Column, 1, lineLen + 1);

        return new TextPosition(line, col);
    }

    private void UpdateDecorations()
    {
        if (_disposed)
        {
            return;
        }

        IReadOnlyList<ModelDeltaDecoration> specs = BuildDecorations();
        IReadOnlyList<ModelDecoration> created = _model.DeltaDecorations(_ownerId, _decorationIds, specs);
        _decorationIds = created.Select(d => d.Id).ToArray();
    }

    private IReadOnlyList<ModelDeltaDecoration> BuildDecorations()
    {
        List<ModelDeltaDecoration> result = [];

        int activeOffset = _model.GetOffsetAt(_selection.Active);
        result.Add(new ModelDeltaDecoration(new TextRange(activeOffset, activeOffset), ModelDecorationOptions.CreateCursorOptions()));

        if (!_selection.IsEmpty)
        {
            int startOffset = _model.GetOffsetAt(_selection.Start);
            int endOffset = _model.GetOffsetAt(_selection.End);
            result.Add(new ModelDeltaDecoration(new TextRange(startOffset, endOffset), ModelDecorationOptions.CreateSelectionOptions()));
        }

        return result;
    }

    private void HandleOptionsChanged(object? sender, TextModelOptionsChangedEventArgs e)
    {
        _stickyColumn = -1;
        // TODO(SnippetController): integrate snippet tabstop stickiness so column resets fold into snippet navigation once ported.
    }
}
