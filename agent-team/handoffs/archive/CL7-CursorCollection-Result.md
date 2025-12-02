```markdown
# CL7 CursorCollection Wiring Phase 3 Result
_By: Porter-CS (Viktor Zoric) • Date: 2025-11-28 • Task: CL7-CursorWiring Stage 1 Phase 3_

## 1. Summary

Successfully implemented Phase 3 (CursorCollection.cs Refactoring) from `CL7-CursorWiring-Plan.md`, completing the Stage 1 cursor wiring.

### Key Deliverables
1. **CursorCollection.cs** refactored with `CursorContext`, state management, `SetStates`, `Normalize`, tracked selection lifecycle
2. **Selection.PlusRange** helper method added for normalize merge logic
3. **TextModel.CreateCursorCollection** updated to accept optional `EditorCursorOptions`
4. **Test fixes** for `CursorMultiSelectionTests` to work with the new API

## 2. Files Modified

| File | Changes |
|------|---------|
| `src/TextBuffer/Cursor/CursorCollection.cs` | Major refactoring: added `_context` field, `_lastAddedCursorIndex` tracking; implemented `GetAll`, `GetPrimaryCursor`, `GetViewPositions`, `GetSelections`, `GetViewSelections`, `GetTopMostViewPosition`, `GetBottomMostViewPosition`; added `SetStates`, `SetSecondaryStates`, `SetSelections`; implemented full `Normalize` algorithm; added `StartTrackingSelections`, `StopTrackingSelections`, `ReadSelectionFromMarkers`, `EnsureValidState`; added helper methods `AddSecondaryCursor`, `RemoveSecondaryCursor`, `KillSecondaryCursors`, `GetLastAddedCursorIndex`; preserved legacy API for backward compatibility |
| `src/TextBuffer/Core/Selection.cs` | Added `PlusRange(Selection other)` instance method for range union |
| `src/TextBuffer/TextModel.cs` | Updated `CreateCursorCollection` to accept optional `EditorCursorOptions` parameter |
| `tests/TextBuffer.Tests/CursorMultiSelectionTests.cs` | Updated `CreateCollection` helper to use `CursorContext`-based API |

## 3. Implementation Details

### 3.1 CursorContext and State Management

```csharp
public sealed class CursorCollection : IDisposable
{
    private CursorContext _context;
    private readonly List<Cursor> _cursors = [];
    private int _lastAddedCursorIndex = 0;
    
    public CursorCollection(CursorContext context)
    {
        _context = context;
        _cursors.Add(new Cursor(context)); // Primary cursor always exists
    }
    
    // Legacy constructor for backward compatibility
    public CursorCollection(TextModel model) : this(CursorContext.FromModel(model))
    {
    }
}
```

### 3.2 Core State Methods

Implemented all state access methods per the plan:
- `GetAll()` - returns list of `CursorState` for all cursors
- `GetPrimaryCursor()` - returns first cursor's state
- `GetViewPositions()` / `GetSelections()` / `GetViewSelections()`
- `GetTopMostViewPosition()` / `GetBottomMostViewPosition()`

### 3.3 SetStates (Critical Path)

```csharp
public void SetStates(IReadOnlyList<PartialCursorState>? states)
{
    if (states == null || states.Count == 0) return;
    
    // Set primary cursor state
    _cursors[0].SetState(_context, states[0].ModelState, states[0].ViewState);
    
    // Set secondary states (creates/removes cursors as needed)
    SetSecondaryStates(states.Skip(1).ToList());
}

private void SetSecondaryStates(IReadOnlyList<PartialCursorState> secondaryStates)
{
    // Add or remove cursors to match count
    // Update all secondary cursor states
}
```

### 3.4 Normalize (Multi-Cursor Merge)

Ported the full TS `normalize` algorithm:
- Sorts cursors by selection start position
- Respects `CursorConfiguration.MultiCursorMergeOverlapping` setting
- Merges touching cursors when one is collapsed
- Merges overlapping (not just touching) when both have selection
- Preserves last-added cursor direction during merge
- Updates `_lastAddedCursorIndex` correctly

```csharp
public void Normalize()
{
    if (_cursors.Count == 1) return;
    
    // Create sorted list with original indices
    List<SortedCursor> sortedCursors = ...
    sortedCursors.Sort((a, b) => Range.CompareRangesUsingStarts(...));
    
    for (int i = 0; i < sortedCursors.Count - 1; i++)
    {
        // Check merge conditions
        bool shouldMerge = ...;
        
        if (shouldMerge)
        {
            // Merge selections, update winner, remove loser
            // Update lastAddedCursorIndex
        }
    }
}
```

### 3.5 Tracked Selection Lifecycle

```csharp
public void StartTrackingSelections()
{
    foreach (Cursor cursor in _cursors)
        cursor.StartTrackingSelection(_context);
}

public void StopTrackingSelections()
{
    foreach (Cursor cursor in _cursors)
        cursor.StopTrackingSelection(_context);
}

public IReadOnlyList<Selection> ReadSelectionFromMarkers()
{
    return _cursors.Select(c => c.ReadSelectionFromMarkers(_context)).ToList();
}

public void EnsureValidState()
{
    foreach (Cursor cursor in _cursors)
        cursor.EnsureValidState(_context);
}
```

### 3.6 Helper Methods

```csharp
private void AddSecondaryCursor()
{
    _cursors.Add(new Cursor(_context));
    _lastAddedCursorIndex = _cursors.Count - 1;
}

private void RemoveSecondaryCursor(int removeIndex)
{
    if (_lastAddedCursorIndex >= removeIndex + 1)
        _lastAddedCursorIndex--;
    _cursors[removeIndex + 1].Dispose(_context);
    _cursors.RemoveAt(removeIndex + 1);
}

public void KillSecondaryCursors() => SetSecondaryStates([]);

public int GetLastAddedCursorIndex()
{
    if (_cursors.Count == 1 || _lastAddedCursorIndex == 0)
        return 0;
    return _lastAddedCursorIndex;
}
```

### 3.7 Selection.PlusRange Helper

Added for the normalize merge logic:
```csharp
public Range PlusRange(Selection other) => Range.PlusRange(ToRange(), other.ToRange());
```

## 4. Test Results

```
Test summary: total: 641, failed: 0, succeeded: 639, skipped: 2, duration: 104.7s
```

All existing tests pass. The 2 skipped tests are pre-existing placeholder tests for multi-cursor/snippet integration pending future work.

## 5. Deviations from Plan

| Deviation | Reason |
|-----------|--------|
| Added `SortedCursor` struct | Clean helper for normalize algorithm instead of anonymous tuple |
| Added `SetStates(IReadOnlyList<PartialModelCursorState>?)` overload | Convenience for `CursorState.FromModelSelections` return type |
| Kept legacy `CreateCursor`/`RemoveCursor`/`GetCursorPositions` | Backward compatibility for existing test code |
| Updated `CursorMultiSelectionTests` helper | Tests needed to use new `CursorContext`-based API |

## 6. Stage 1 Completion Summary

With Phase 3 complete, CL7-Stage1 is now fully implemented:

| Phase | Status | Description |
|-------|--------|-------------|
| Phase 1 | ✅ Complete | Feature flag `EnableVsCursorParity` infrastructure |
| Phase 2 | ✅ Complete | `Cursor.cs` state fields, `_setState`, tracked ranges |
| Phase 3 | ✅ Complete | `CursorCollection.cs` full refactoring |

### Remaining Work (Stage 2+)

- Phase 4: TextModel factory method (minimal, already done as part of Phase 3)
- Phase 5: Extended test coverage (`CursorCollectionTests` suite)
- Integration with movement methods to use `_setState` path
- Flip feature flag default to `true` after soak period

## 7. Verification Commands

```bash
# Run all tests
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo

# Run Cursor tests only
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~Cursor" --nologo

# Run CursorMultiSelectionTests
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "CursorMultiSelectionTests" --nologo
```

## 8. References

- Implementation Plan: `agent-team/handoffs/CL7-CursorWiring-Plan.md`
- Phase 1+2 Result: `agent-team/handoffs/CL7-Cursor-Phase1-Result.md`
- TS Source: `ts/src/vs/editor/common/cursor/cursorCollection.ts`
- Delta Tag: `#delta-2025-11-26-aa4-cl7-cursor-core`

---

_End of CL7-CursorCollection-Result_
```
