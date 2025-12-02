# WS3-PORT-TextModel Implementation Result

## Task Summary
**WS3-PORT-TextModel**: 将 IntervalTree 的 `AcceptReplace` 集成到 TextModel

## Implementation Details

### Changes Made

#### 1. `DecorationsTrees.cs` - New `AcceptReplace` Method
Added `AcceptReplace(int offset, int removedLength, int insertedLength, bool forceMoveMarkers)` method that:
- Calls `AcceptReplace` on all 3 scope trees (`_regular`, `_overview`, `_injected`)
- Aggregates `DecorationChange` results from all trees
- Fast-path returns for empty/single results

```csharp
public IReadOnlyList<DecorationChange> AcceptReplace(int offset, int removedLength, int insertedLength, bool forceMoveMarkers)
{
    IReadOnlyList<DecorationChange> regularChanges = _regular.AcceptReplace(offset, removedLength, insertedLength, forceMoveMarkers);
    IReadOnlyList<DecorationChange> overviewChanges = _overview.AcceptReplace(offset, removedLength, insertedLength, forceMoveMarkers);
    IReadOnlyList<DecorationChange> injectedChanges = _injected.AcceptReplace(offset, removedLength, insertedLength, forceMoveMarkers);
    // ... merge and return
}
```

#### 2. `IntervalTree.cs` - Enhanced `AcceptReplace` and `NormalizeDelta`

**AcceptReplace Changes**:
- Signature changed from `void` to `IReadOnlyList<DecorationChange>`
- Phase 4 now captures `oldRange` before `NodeAcceptEdit` and emits `DecorationChange` if range changed
- Added forced normalization after `NoOverlapReplace` to ensure all `Decoration.Range` values are updated

```csharp
// After NoOverlapReplace, force normalize to update all Decoration.Range values
if (_root != Sentinel)
{
    _normalizePending = true;
    NormalizeDeltaIfNeeded();
}
```

**NormalizeDelta Changes**:
- Now also updates `Decoration.Range` to match the normalized `Start/End` values:
```csharp
// Update Decoration.Range to match
if (node.Decoration != null)
{
    node.Decoration.Range = new TextRange(newStart, newEnd);
}
```

#### 3. `TextModel.cs` - Integration
- Modified `ApplyPendingEdits` to call `_decorationTrees.AcceptReplace()` instead of old `AdjustDecorationsForEdit`
- Removed old `AdjustDecorationsForEdit` method (~40 lines)

```csharp
// Call AcceptReplace on decoration trees (replaces old AdjustDecorationsForEdit)
IReadOnlyList<DecorationChange> changes = _decorationTrees.AcceptReplace(
    edit.OldStartOffset, removedLength, edit.NewText.Length, forceMoveMarkers);

// Emit decoration change events
foreach (var change in changes)
{
    _decorationChanges.Add(change);
}
```

### Key Design Decisions

1. **Forced Normalization After NoOverlapReplace**: 
   - Problem: `NoOverlapReplace` uses lazy delta propagation which doesn't update `Decoration.Range` for nodes in the right subtree of a shifted node
   - Solution: Force `NormalizeDelta()` after `NoOverlapReplace` to materialize all delta values and update all `Decoration.Range` values

2. **NormalizeDelta Updates Decoration.Range**:
   - Problem: Original `NormalizeDelta` only updated node's `Start/End` but not `Decoration.Range`
   - Solution: Modified `NormalizeDelta` to also update `Decoration.Range = new TextRange(newStart, newEnd)`

3. **DecorationChange Collection**:
   - Phase 4 (nodes of interest) collects changes for nodes that were edited
   - `NoOverlapReplace` collects changes for nodes that were shifted (nodeStart > end)
   - `NormalizeDelta` doesn't collect changes (they're already collected by NoOverlapReplace)

## Test Results

### Targeted Test
```bash
export PIECETREE_DEBUG=0 && dotnet test --filter "TrackedSelections" --nologo
# Result: 3/3 pass
```

### Full Suite
```bash
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
# Result: 818 passed, 5 skipped (≈100s)
```

## Files Modified
| File | Lines Changed | Description |
|------|---------------|-------------|
| `src/TextBuffer/Decorations/DecorationsTrees.cs` | +45 | New `AcceptReplace` method |
| `src/TextBuffer/Decorations/IntervalTree.cs` | +71 | Enhanced `AcceptReplace` with forced normalize, `NormalizeDelta` updates `Decoration.Range` |
| `src/TextBuffer/TextModel.cs` | -38 | Removed old `AdjustDecorationsForEdit`, integrated new flow |

## TS Alignment Notes
| TS Element | C# Implementation | Notes |
|------------|-------------------|-------|
| `IntervalTree.acceptReplace()` | `IntervalTree.AcceptReplace()` | Now returns `IReadOnlyList<DecorationChange>` |
| `_decorationsTree.acceptReplace()` | `_decorationTrees.AcceptReplace()` | Aggregates 3 scope trees |
| Lazy delta semantics | Forced normalize after NoOverlapReplace | Ensures `Decoration.Range` is always current |
| `_onDidChangeDecorations.fire()` | `DecorationChange` list | More detailed change tracking |

## Known Differences from TS
1. **Forced Normalization**: C# implementation forces normalization after `NoOverlapReplace` to ensure `Decoration.Range` values are always current. TS relies on lazy resolution at search time. This has O(n) cost but is necessary because C# code expects `Decoration.Range` to be immediately accurate.

2. **DecorationChange Details**: C# emits detailed `DecorationChange` events with `OldRange` for each changed decoration. TS fires a generic `_onDidChangeDecorations` without individual change details.

## Remaining Issues
None. All 818 tests pass.

## Changefeed Anchor
`#delta-2025-12-02-ws3-port-textmodel`

---
*Created by Porter-CS, 2025-12-02*
