// Source: ts/src/vs/editor/common/cursor/cursorCollection.ts
// - Class: CursorCollection (Lines: 15-250)
// Ported: 2025-11-22
// Updated: 2025-11-28 (CL7-Stage1 Phase 3: CursorContext, setStates, normalize, tracked selection lifecycle)

using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Cursor;

public sealed class CursorCollection : IDisposable
{
    private CursorContext _context;
    private readonly List<Cursor> _cursors = [];
    private int _lastAddedCursorIndex = 0;
    private bool _disposed;

    /// <summary>
    /// Create a CursorCollection from a CursorContext (Stage 1 path).
    /// </summary>
    public CursorCollection(CursorContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _cursors.Add(new Cursor(context));
    }

    /// <summary>
    /// Legacy constructor for backward compatibility.
    /// </summary>
    public CursorCollection(TextModel model) : this(CursorContext.FromModel(model))
    {
    }

    public IReadOnlyList<Cursor> Cursors => _cursors.AsReadOnly();

    /// <summary>
    /// Update the context (for example when editor options change).
    /// </summary>
    public void UpdateContext(CursorContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        foreach (Cursor cursor in _cursors)
        {
            cursor.UpdateContext(_context);
        }
    }

    #region State Access Methods

    /// <summary>
    /// Get all cursor states.
    /// </summary>
    public IReadOnlyList<CursorState> GetAll()
    {
        return _cursors.Select(c => c.AsCursorState()).ToList();
    }

    /// <summary>
    /// Get the primary cursor state (first cursor).
    /// </summary>
    public CursorState GetPrimaryCursor()
    {
        return _cursors[0].AsCursorState();
    }

    /// <summary>
    /// Get all view positions.
    /// </summary>
    public IReadOnlyList<TextPosition> GetViewPositions()
    {
        return _cursors.Select(c => c.AsCursorState().ViewState.Position).ToList();
    }

    /// <summary>
    /// Get all model selections.
    /// </summary>
    public IReadOnlyList<Selection> GetSelections()
    {
        return _cursors.Select(c => c.AsCursorState().ModelState.Selection).ToList();
    }

    /// <summary>
    /// Get all view selections.
    /// </summary>
    public IReadOnlyList<Selection> GetViewSelections()
    {
        return _cursors.Select(c => c.AsCursorState().ViewState.Selection).ToList();
    }

    /// <summary>
    /// Get the top-most view position (minimum line/column).
    /// </summary>
    public TextPosition GetTopMostViewPosition()
    {
        CursorState firstState = _cursors[0].AsCursorState();
        TextPosition best = firstState.ViewState.Position;

        for (int i = 1; i < _cursors.Count; i++)
        {
            TextPosition candidate = _cursors[i].AsCursorState().ViewState.Position;
            if (TextPosition.Compare(candidate, best) < 0)
            {
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// Get the bottom-most view position (maximum line/column).
    /// </summary>
    public TextPosition GetBottomMostViewPosition()
    {
        CursorState firstState = _cursors[0].AsCursorState();
        TextPosition best = firstState.ViewState.Position;
        int bestIndex = 0;

        for (int i = 1; i < _cursors.Count; i++)
        {
            TextPosition candidate = _cursors[i].AsCursorState().ViewState.Position;
            int comparison = TextPosition.Compare(candidate, best);
            if (comparison > 0 || (comparison == 0 && i > bestIndex))
            {
                best = candidate;
                bestIndex = i;
            }
        }

        return best;
    }

    #endregion

    #region State Setting Methods

    /// <summary>
    /// Set the states for all cursors.
    /// </summary>
    public void SetStates(IReadOnlyList<PartialCursorState>? states)
    {
        if (states == null || states.Count == 0)
        {
            return;
        }

        // Set primary cursor state
        _cursors[0].SetState(_context, states[0].ModelState, states[0].ViewState);

        // Set secondary states
        SetSecondaryStates(states.Skip(1).ToList());
    }

    /// <summary>
    /// Set states from PartialModelCursorState list (convenience overload).
    /// </summary>
    public void SetStates(IReadOnlyList<PartialModelCursorState>? states)
    {
        if (states == null || states.Count == 0)
        {
            return;
        }

        // Set primary cursor state
        _cursors[0].SetState(_context, states[0].ModelState, states[0].ViewState);

        // Set secondary states
        if (states.Count > 1)
        {
            List<PartialCursorState> secondary = states
                .Skip(1)
                .Select(s => PartialCursorState.FromModel(s.ModelState))
                .ToList();
            SetSecondaryStates(secondary);
        }
        else
        {
            SetSecondaryStates([]);
        }
    }

    /// <summary>
    /// Creates or disposes secondary cursors as necessary to match the number of secondary states.
    /// </summary>
    private void SetSecondaryStates(IReadOnlyList<PartialCursorState> secondaryStates)
    {
        int secondaryCursorsLength = _cursors.Count - 1;
        int secondaryStatesLength = secondaryStates.Count;

        // Add cursors if needed
        if (secondaryCursorsLength < secondaryStatesLength)
        {
            int createCnt = secondaryStatesLength - secondaryCursorsLength;
            for (int i = 0; i < createCnt; i++)
            {
                AddSecondaryCursor();
            }
        }
        // Remove cursors if needed
        else if (secondaryCursorsLength > secondaryStatesLength)
        {
            int removeCnt = secondaryCursorsLength - secondaryStatesLength;
            for (int i = 0; i < removeCnt; i++)
            {
                RemoveSecondaryCursor(_cursors.Count - 2);
            }
        }

        // Update all secondary cursor states
        for (int i = 0; i < secondaryStatesLength; i++)
        {
            _cursors[i + 1].SetState(_context, secondaryStates[i].ModelState, secondaryStates[i].ViewState);
        }
    }

    /// <summary>
    /// Set all cursor positions from selections.
    /// </summary>
    public void SetSelections(IReadOnlyList<Selection> selections)
    {
        SetStates(CursorState.FromModelSelections(selections));
    }

    #endregion

    #region Normalize (Multi-Cursor Merge)

    /// <summary>
    /// Normalize cursors by merging overlapping selections.
    /// Respects CursorConfiguration.MultiCursorMergeOverlapping setting.
    /// </summary>
    public void Normalize()
    {
        if (_cursors.Count == 1)
        {
            return;
        }

        // Create a copy of cursors list
        List<Cursor> cursors = _cursors.ToList();

        // Create sorted list with original indices
        List<SortedCursor> sortedCursors = [];
        for (int i = 0; i < cursors.Count; i++)
        {
            sortedCursors.Add(new SortedCursor
            {
                Index = i,
                Selection = cursors[i].AsCursorState().ModelState.Selection
            });
        }

        // Sort by selection start position
        sortedCursors.Sort((a, b) => Range.CompareRangesUsingStarts(a.Selection.ToRange(), b.Selection.ToRange()));

        for (int sortedCursorIndex = 0; sortedCursorIndex < sortedCursors.Count - 1; sortedCursorIndex++)
        {
            SortedCursor current = sortedCursors[sortedCursorIndex];
            SortedCursor next = sortedCursors[sortedCursorIndex + 1];

            Selection currentSelection = current.Selection;
            Selection nextSelection = next.Selection;

            if (!_context.CursorConfig.MultiCursorMergeOverlapping)
            {
                continue;
            }

            bool shouldMergeCursors;
            if (nextSelection.IsEmpty || currentSelection.IsEmpty)
            {
                // Merge touching cursors if one of them is collapsed
                shouldMergeCursors = nextSelection.Start.IsBeforeOrEqual(currentSelection.End);
            }
            else
            {
                // Merge only overlapping cursors (i.e. allow touching ranges)
                shouldMergeCursors = nextSelection.Start.IsBefore(currentSelection.End);
            }

            if (shouldMergeCursors)
            {
                int winnerSortedCursorIndex = current.Index < next.Index ? sortedCursorIndex : sortedCursorIndex + 1;
                int loserSortedCursorIndex = current.Index < next.Index ? sortedCursorIndex + 1 : sortedCursorIndex;

                int loserIndex = sortedCursors[loserSortedCursorIndex].Index;
                int winnerIndex = sortedCursors[winnerSortedCursorIndex].Index;

                Selection loserSelection = sortedCursors[loserSortedCursorIndex].Selection;
                Selection winnerSelection = sortedCursors[winnerSortedCursorIndex].Selection;

                if (!loserSelection.EqualsSelection(winnerSelection))
                {
                    Range resultingRange = loserSelection.PlusRange(winnerSelection);
                    bool loserSelectionIsLTR = loserSelection.IsLTR;
                    bool winnerSelectionIsLTR = winnerSelection.IsLTR;

                    // Give more importance to the last added cursor (think Ctrl-dragging + hitting another cursor)
                    bool resultingSelectionIsLTR;
                    if (loserIndex == _lastAddedCursorIndex)
                    {
                        resultingSelectionIsLTR = loserSelectionIsLTR;
                        _lastAddedCursorIndex = winnerIndex;
                    }
                    else
                    {
                        // Winner takes it all
                        resultingSelectionIsLTR = winnerSelectionIsLTR;
                    }

                    Selection resultingSelection;
                    if (resultingSelectionIsLTR)
                    {
                        resultingSelection = new Selection(
                            resultingRange.StartLineNumber, resultingRange.StartColumn,
                            resultingRange.EndLineNumber, resultingRange.EndColumn);
                    }
                    else
                    {
                        resultingSelection = new Selection(
                            resultingRange.EndLineNumber, resultingRange.EndColumn,
                            resultingRange.StartLineNumber, resultingRange.StartColumn);
                    }

                    sortedCursors[winnerSortedCursorIndex] = new SortedCursor
                    {
                        Index = winnerIndex,
                        Selection = resultingSelection
                    };
                    PartialModelCursorState resultingState = CursorState.FromModelSelection(resultingSelection);
                    cursors[winnerIndex].SetState(_context, resultingState.ModelState, null);
                }

                // Update indices for removed cursor
                for (int j = 0; j < sortedCursors.Count; j++)
                {
                    if (sortedCursors[j].Index > loserIndex)
                    {
                        sortedCursors[j] = new SortedCursor
                        {
                            Index = sortedCursors[j].Index - 1,
                            Selection = sortedCursors[j].Selection
                        };
                    }
                }

                cursors.RemoveAt(loserIndex);
                sortedCursors.RemoveAt(loserSortedCursorIndex);
                RemoveSecondaryCursor(loserIndex - 1);

                sortedCursorIndex--; // Re-check this index
            }
        }
    }

    /// <summary>
    /// Helper struct for normalize algorithm.
    /// </summary>
    private struct SortedCursor
    {
        public int Index;
        public Selection Selection;
    }

    #endregion

    #region Tracked Selection Lifecycle

    /// <summary>
    /// Start tracking selections for all cursors.
    /// </summary>
    public void StartTrackingSelections()
    {
        foreach (Cursor cursor in _cursors)
        {
            cursor.StartTrackingSelection(_context);
        }
    }

    /// <summary>
    /// Stop tracking selections for all cursors.
    /// </summary>
    public void StopTrackingSelections()
    {
        foreach (Cursor cursor in _cursors)
        {
            cursor.StopTrackingSelection(_context);
        }
    }

    /// <summary>
    /// Read all selections from tracked markers.
    /// </summary>
    public IReadOnlyList<Selection> ReadSelectionFromMarkers()
    {
        return _cursors.Select(c => c.ReadSelectionFromMarkers(_context)).ToList();
    }

    /// <summary>
    /// Ensure all cursor states are valid against the model.
    /// </summary>
    public void EnsureValidState()
    {
        foreach (Cursor cursor in _cursors)
        {
            cursor.EnsureValidState(_context);
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Add a secondary cursor.
    /// </summary>
    private void AddSecondaryCursor()
    {
        _cursors.Add(new Cursor(_context));
        _lastAddedCursorIndex = _cursors.Count - 1;
    }

    /// <summary>
    /// Remove a secondary cursor at the given index (0-based index into secondary cursors).
    /// </summary>
    private void RemoveSecondaryCursor(int removeIndex)
    {
        if (removeIndex < 0 || removeIndex >= _cursors.Count - 1)
        {
            return;
        }

        if (_lastAddedCursorIndex >= removeIndex + 1)
        {
            _lastAddedCursorIndex--;
        }
        _cursors[removeIndex + 1].Dispose(_context);
        _cursors.RemoveAt(removeIndex + 1);
    }

    /// <summary>
    /// Remove all secondary cursors, keeping only the primary cursor.
    /// </summary>
    public void KillSecondaryCursors()
    {
        SetSecondaryStates([]);
    }

    /// <summary>
    /// Get the index of the last added cursor.
    /// Returns 0 if there's only the primary cursor or if the primary cursor was last added.
    /// </summary>
    public int GetLastAddedCursorIndex()
    {
        if (_cursors.Count == 1 || _lastAddedCursorIndex == 0)
        {
            return 0;
        }
        return _lastAddedCursorIndex;
    }

    #endregion

    #region Legacy API Compatibility

    /// <summary>
    /// Legacy: Create a cursor at an optional position.
    /// </summary>
    public Cursor CreateCursor(TextPosition? start = null)
    {
        Cursor cursor = new(_context);
        if (start.HasValue)
        {
            cursor.MoveTo(start.Value);
        }
        _cursors.Add(cursor);
        _lastAddedCursorIndex = _cursors.Count - 1;
        return cursor;
    }

    /// <summary>
    /// Legacy: Remove a specific cursor.
    /// </summary>
    public void RemoveCursor(Cursor cursor)
    {
        int index = _cursors.IndexOf(cursor);
        if (index <= 0)
        {
            return;
        }

        RemoveSecondaryCursor(index - 1);
    }

    /// <summary>
    /// Legacy: Get all cursor positions.
    /// </summary>
    public IReadOnlyList<TextPosition> GetCursorPositions()
    {
        List<TextPosition> positions = new(_cursors.Count);
        foreach (Cursor c in _cursors)
        {
            positions.Add(c.Selection.Active);
        }
        return positions;
    }

    #endregion

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (Cursor c in _cursors.ToArray())
        {
            c.Dispose(_context);
        }
        _cursors.Clear();
    }
}
