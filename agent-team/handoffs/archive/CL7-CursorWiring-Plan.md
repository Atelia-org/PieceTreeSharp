# CL7 Cursor Wiring Implementation Plan
_By: Investigator-TS (Callie Stone) • Date: 2025-11-28 • Scope: `#delta-2025-11-26-aa4-cl7-cursor-core`_

## 1. Executive Summary

This plan details the **Stage 1** wiring to connect the existing Stage 0 `CursorConfiguration`/`CursorState`/`CursorContext` artifacts (delivered by `WS4-PORT-Core`) into the running `Cursor.cs` and `CursorCollection.cs`. The goal is to close the gap tracked at [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core).

### Key Points:
1. **Switch `Cursor` to use `SingleCursorState`** instead of maintaining `_selection`/`_stickyColumn` fields directly
2. **Wire `CursorCollection` to hold `CursorContext`** and implement `setStates`/`normalize`/tracked range plumbing
3. **Enable feature flag `TextModelOptions.EnableVsCursorParity`** with dual-path fallback
4. **Add 15-20 new tests** to `CursorCoreTests` and create `CursorCollectionTests`
5. **Minimal TextModel changes** - tracked range APIs already in place

### Estimated Complexity: **Medium**
- Core architecture is in place (Stage 0)
- Main work is rewiring existing code to consume new abstractions
- Risk is contained by feature flag

---

## 2. Current State Analysis

### 2.1 Stage 0 Deliverables (Done)
| Component | File | Status |
|-----------|------|--------|
| `CursorConfiguration` | `src/TextBuffer/Cursor/CursorConfiguration.cs` | ✅ Complete |
| `SingleCursorState`, `CursorState` | `src/TextBuffer/Cursor/CursorState.cs` | ✅ Complete |
| `CursorContext`, `ICoordinatesConverter` | `src/TextBuffer/Cursor/CursorContext.cs` | ✅ Complete |
| `ICursorSimpleModel`, `TextModelCursorAdapter` | `src/TextBuffer/Cursor/CursorConfiguration.cs` | ✅ Complete |
| TrackedRange APIs | `src/TextBuffer/TextModel.cs` (`_setTrackedRange`, `_getTrackedRange`) | ✅ Complete |
| `CursorCoreTests` (25 tests) | `tests/TextBuffer.Tests/CursorCoreTests.cs` | ✅ Complete |

### 2.2 Stage 1 Gaps (This Plan)
| Component | Gap Description | TS Reference |
|-----------|-----------------|--------------|
| `Cursor.cs` | Still uses `_selection`/`_stickyColumn` instead of `SingleCursorState`; no `_setState`; no tracked range | `oneCursor.ts` lines 15-200 |
| `CursorCollection.cs` | Missing `setStates`, `normalize`, `lastAddedCursorIndex`, tracked range lifecycle | `cursorCollection.ts` full file |
| Feature flag | `TextModelOptions.EnableVsCursorParity` not defined | N/A |
| Tests | No `CursorCollectionTests`, limited state wiring tests | N/A |

---

## 3. Implementation Plan

### Phase 1: Feature Flag Infrastructure (Est: 30 min)

#### Step 1.1: Add Feature Flag to TextModelResolvedOptions

```csharp
// In src/TextBuffer/TextModelOptions.cs
public sealed class TextModelResolvedOptions
{
    // ... existing properties ...
    
    /// <summary>
    /// When true, Cursor/CursorCollection use VS Code-compatible state management.
    /// Default: false for backward compatibility during transition.
    /// </summary>
    public bool EnableVsCursorParity { get; }
}
```

Add to `TextModelCreationOptions`:
```csharp
public bool EnableVsCursorParity { get; init; } = false;
```

#### Step 1.2: Update TextModelResolvedOptions.Resolve

Propagate the flag through the options resolution chain.

---

### Phase 2: Cursor.cs Refactoring (Est: 2-3 hours)

#### Step 2.1: Add State Fields (Dual-Mode)

Replace the existing fields with state-based approach while maintaining backward compatibility:

```csharp
public class Cursor : IDisposable
{
    private readonly TextModel _model;
    private CursorContext _context;  // NEW: Stage 1
    
    // Dual-mode state storage
    private SingleCursorState? _modelState;  // NEW: Stage 1
    private SingleCursorState? _viewState;   // NEW: Stage 1
    
    // Legacy fields (to be deprecated when flag is on)
    private Selection _selection;
    private int _stickyColumn = -1;
    
    // Tracked range support (matches TS)
    private string? _selTrackedRange;
    private bool _trackSelection = true;
```

#### Step 2.2: Implement _setState (Core Logic)

Port from `ts/src/vs/editor/common/cursor/oneCursor.ts` lines 75-140:

```csharp
private void _setState(CursorContext context, SingleCursorState? modelState, SingleCursorState? viewState)
{
    if (!_model.GetOptions().EnableVsCursorParity)
    {
        // Legacy path: update _selection and _stickyColumn directly
        UpdateLegacyState(modelState, viewState);
        return;
    }
    
    // Validate view state
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
```

#### Step 2.3: Implement Tracked Range Methods

```csharp
public void StartTrackingSelection(CursorContext context)
{
    _trackSelection = true;
    UpdateTrackedRange(context);
}

public void StopTrackingSelection(CursorContext context)
{
    _trackSelection = false;
    RemoveTrackedRange(context);
}

private void UpdateTrackedRange(CursorContext context)
{
    if (!_trackSelection || !_model.GetOptions().EnableVsCursorParity)
        return;
    
    _selTrackedRange = _model._setTrackedRange(
        _selTrackedRange,
        _modelState?.Selection.ToRange(),
        TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);
}

private void RemoveTrackedRange(CursorContext context)
{
    _selTrackedRange = _model._setTrackedRange(
        _selTrackedRange, null, 
        TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges);
}

public Selection ReadSelectionFromMarkers(CursorContext context)
{
    if (_selTrackedRange == null || !_model.GetOptions().EnableVsCursorParity)
        return _selection;
    
    Range? range = _model._getTrackedRange(_selTrackedRange);
    if (range == null)
        return _selection;
    
    if (_modelState!.Selection.IsEmpty && !range.Value.IsEmpty)
    {
        // Avoid selecting text when recovering from markers
        return Selection.FromRange(range.Value.CollapseToEnd(), _modelState.Selection.Direction);
    }
    
    return Selection.FromRange(range.Value, _modelState.Selection.Direction);
}
```

#### Step 2.4: Add State Access Methods

```csharp
public CursorState AsCursorState()
{
    if (_modelState == null || _viewState == null)
    {
        // Fallback for legacy mode
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

public void SetState(CursorContext context, SingleCursorState? modelState, SingleCursorState? viewState)
{
    _setState(context, modelState, viewState);
}

public void EnsureValidState(CursorContext context)
{
    _setState(context, _modelState, _viewState);
}
```

#### Step 2.5: Update Constructor

```csharp
public Cursor(CursorContext context)
{
    _model = context.Model;
    _context = context;
    _selTrackedRange = null;
    _trackSelection = true;
    _ownerId = _model.AllocateDecorationOwnerId();
    
    _setState(
        context,
        new SingleCursorState(new Range(1, 1, 1, 1), SelectionStartKind.Simple, 0, new TextPosition(1, 1), 0),
        new SingleCursorState(new Range(1, 1, 1, 1), SelectionStartKind.Simple, 0, new TextPosition(1, 1), 0));
    
    _model.OnDidChangeOptions += HandleOptionsChanged;
}

// Legacy constructor for backward compatibility
public Cursor(TextModel model) : this(CursorContext.FromModel(model))
{
}
```

---

### Phase 3: CursorCollection.cs Refactoring (Est: 3-4 hours)

#### Step 3.1: Add CursorContext and State Management

```csharp
public sealed class CursorCollection : IDisposable
{
    private CursorContext _context;
    private readonly List<Cursor> _cursors = [];
    private int _lastAddedCursorIndex = 0;
    private bool _disposed;
    
    public CursorCollection(CursorContext context)
    {
        _context = context;
        _cursors.Add(new Cursor(context));
    }
    
    // Legacy constructor
    public CursorCollection(TextModel model) : this(CursorContext.FromModel(model))
    {
    }
    
    public void UpdateContext(CursorContext context)
    {
        _context = context;
    }
```

#### Step 3.2: Implement Core State Methods

```csharp
public IReadOnlyList<CursorState> GetAll()
{
    return _cursors.Select(c => c.AsCursorState()).ToList();
}

public CursorState GetPrimaryCursor()
{
    return _cursors[0].AsCursorState();
}

public IReadOnlyList<TextPosition> GetViewPositions()
{
    return _cursors.Select(c => c.AsCursorState().ViewState.Position).ToList();
}

public IReadOnlyList<Selection> GetSelections()
{
    return _cursors.Select(c => c.AsCursorState().ModelState.Selection).ToList();
}

public IReadOnlyList<Selection> GetViewSelections()
{
    return _cursors.Select(c => c.AsCursorState().ViewState.Selection).ToList();
}

public TextPosition GetTopMostViewPosition()
{
    return _cursors
        .Select(c => c.AsCursorState().ViewState.Position)
        .OrderBy(p => p.LineNumber)
        .ThenBy(p => p.Column)
        .First();
}

public TextPosition GetBottomMostViewPosition()
{
    return _cursors
        .Select(c => c.AsCursorState().ViewState.Position)
        .OrderByDescending(p => p.LineNumber)
        .ThenByDescending(p => p.Column)
        .First();
}
```

#### Step 3.3: Implement setStates (Critical Path)

```csharp
public void SetStates(IReadOnlyList<PartialCursorState>? states)
{
    if (states == null || states.Count == 0)
        return;
    
    // Set primary cursor state
    _cursors[0].SetState(_context, states[0].ModelState, states[0].ViewState);
    
    // Set secondary states
    SetSecondaryStates(states.Skip(1).ToList());
}

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

public void SetSelections(IReadOnlyList<Selection> selections)
{
    SetStates(CursorState.FromModelSelections(selections));
}
```

#### Step 3.4: Implement normalize (Multi-Cursor Merge)

This is critical for multi-cursor behavior:

```csharp
public void Normalize()
{
    if (_cursors.Count == 1)
        return;
    
    // Create sorted list with original indices
    List<(int Index, Selection Selection)> sortedCursors = _cursors
        .Select((c, i) => (Index: i, Selection: c.AsCursorState().ModelState.Selection))
        .OrderBy(x => x.Selection.StartLineNumber)
        .ThenBy(x => x.Selection.StartColumn)
        .ToList();
    
    for (int i = 0; i < sortedCursors.Count - 1; i++)
    {
        var current = sortedCursors[i];
        var next = sortedCursors[i + 1];
        
        if (!_context.CursorConfig.MultiCursorMergeOverlapping)
            continue;
        
        bool shouldMerge;
        if (next.Selection.IsEmpty || current.Selection.IsEmpty)
        {
            // Merge touching cursors if one is collapsed
            shouldMerge = next.Selection.Start.IsBeforeOrEqual(current.Selection.End);
        }
        else
        {
            // Merge only overlapping (not touching) if both have selection
            shouldMerge = next.Selection.Start.IsBefore(current.Selection.End);
        }
        
        if (shouldMerge)
        {
            int winnerIdx = current.Index < next.Index ? i : i + 1;
            int loserIdx = current.Index < next.Index ? i + 1 : i;
            
            int loserOriginalIdx = sortedCursors[loserIdx].Index;
            int winnerOriginalIdx = sortedCursors[winnerIdx].Index;
            
            Selection loserSel = sortedCursors[loserIdx].Selection;
            Selection winnerSel = sortedCursors[winnerIdx].Selection;
            
            if (!loserSel.EqualsSelection(winnerSel))
            {
                Range resultingRange = loserSel.PlusRange(winnerSel);
                bool loserLTR = loserSel.IsLTR;
                bool winnerLTR = winnerSel.IsLTR;
                
                bool resultingLTR;
                if (loserOriginalIdx == _lastAddedCursorIndex)
                {
                    resultingLTR = loserLTR;
                    _lastAddedCursorIndex = winnerOriginalIdx;
                }
                else
                {
                    resultingLTR = winnerLTR;
                }
                
                Selection resultingSel = resultingLTR
                    ? new Selection(resultingRange.StartLineNumber, resultingRange.StartColumn,
                                    resultingRange.EndLineNumber, resultingRange.EndColumn)
                    : new Selection(resultingRange.EndLineNumber, resultingRange.EndColumn,
                                    resultingRange.StartLineNumber, resultingRange.StartColumn);
                
                sortedCursors[winnerIdx] = (winnerOriginalIdx, resultingSel);
                PartialModelCursorState state = CursorState.FromModelSelection(resultingSel);
                _cursors[winnerOriginalIdx].SetState(_context, state.ModelState, null);
            }
            
            // Update indices for removed cursor
            for (int j = 0; j < sortedCursors.Count; j++)
            {
                if (sortedCursors[j].Index > loserOriginalIdx)
                {
                    sortedCursors[j] = (sortedCursors[j].Index - 1, sortedCursors[j].Selection);
                }
            }
            
            _cursors[loserOriginalIdx].Dispose(_context);
            _cursors.RemoveAt(loserOriginalIdx);
            sortedCursors.RemoveAt(loserIdx);
            
            if (_lastAddedCursorIndex >= loserOriginalIdx + 1)
            {
                _lastAddedCursorIndex--;
            }
            
            i--; // Re-check this index
        }
    }
}
```

#### Step 3.5: Implement Tracking Selection Lifecycle

```csharp
public void StartTrackingSelections()
{
    foreach (Cursor cursor in _cursors)
    {
        cursor.StartTrackingSelection(_context);
    }
}

public void StopTrackingSelections()
{
    foreach (Cursor cursor in _cursors)
    {
        cursor.StopTrackingSelection(_context);
    }
}

public IReadOnlyList<Selection> ReadSelectionFromMarkers()
{
    return _cursors.Select(c => c.ReadSelectionFromMarkers(_context)).ToList();
}

public void EnsureValidState()
{
    foreach (Cursor cursor in _cursors)
    {
        cursor.EnsureValidState(_context);
    }
}
```

#### Step 3.6: Helper Methods

```csharp
private void AddSecondaryCursor()
{
    _cursors.Add(new Cursor(_context));
    _lastAddedCursorIndex = _cursors.Count - 1;
}

private void RemoveSecondaryCursor(int removeIndex)
{
    if (_lastAddedCursorIndex >= removeIndex + 1)
    {
        _lastAddedCursorIndex--;
    }
    _cursors[removeIndex + 1].Dispose(_context);
    _cursors.RemoveAt(removeIndex + 1);
}

public void KillSecondaryCursors()
{
    SetSecondaryStates([]);
}

public int GetLastAddedCursorIndex()
{
    if (_cursors.Count == 1 || _lastAddedCursorIndex == 0)
        return 0;
    return _lastAddedCursorIndex;
}
```

---

### Phase 4: TextModel Changes (Est: 30 min)

Minimal changes needed since tracked range APIs already exist.

#### Step 4.1: Add CursorCollection Factory with Context

```csharp
// In TextModel.cs
public CursorCollection CreateCursorCollection(EditorCursorOptions? editorOptions = null)
{
    CursorContext context = CursorContext.FromModel(this, editorOptions);
    return new CursorCollection(context);
}
```

#### Step 4.2: Update CursorContext.FromModel

```csharp
// In CursorContext.cs
public static CursorContext FromModel(TextModel model, EditorCursorOptions? editorOptions = null)
{
    TextModelCursorAdapter viewModel = new(model);
    IdentityCoordinatesConverter converter = new(model);
    CursorConfiguration config = new(model.GetOptions(), editorOptions);
    return new CursorContext(model, viewModel, converter, config);
}
```

---

### Phase 5: Test Coverage (Est: 2-3 hours)

#### Step 5.1: Extend CursorCoreTests

Add tests for `_setState` validation:

```csharp
#region Cursor State Wiring Tests

[Fact]
public void Cursor_SetState_ValidatesModelState()
{
    TestEditorContext ctx = TestEditorBuilder.Create()
        .WithLines("Hello", "World")
        .WithVsCursorParity(true)
        .BuildContext();
    
    using Cursor cursor = new(CursorContext.FromModel(ctx.Model));
    
    // Set state with out-of-bounds position
    SingleCursorState invalidState = new(
        new Range(1, 1, 1, 1),
        SelectionStartKind.Simple,
        0,
        new TextPosition(5, 100), // Invalid
        0);
    
    cursor.SetState(ctx.CursorContext, invalidState, null);
    
    // Should be clamped to valid position
    Assert.Equal(2, cursor.AsCursorState().ModelState.Position.LineNumber);
    Assert.True(cursor.AsCursorState().ModelState.Position.Column <= 6);
}

[Fact]
public void Cursor_TrackedRange_SurvivesEdit()
{
    TestEditorContext ctx = TestEditorBuilder.Create()
        .WithLines("Hello World")
        .WithVsCursorParity(true)
        .BuildContext();
    
    using Cursor cursor = new(ctx.CursorContext);
    cursor.SetState(ctx.CursorContext, 
        new SingleCursorState(new Range(1, 7, 1, 12), SelectionStartKind.Simple, 0, 
                              new TextPosition(1, 12), 0),
        null);
    
    cursor.StartTrackingSelection(ctx.CursorContext);
    
    // Insert text before selection
    ctx.Model.PushEditOperations([new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "XXX")], null);
    
    // Read back from markers
    Selection recovered = cursor.ReadSelectionFromMarkers(ctx.CursorContext);
    
    // Selection should have shifted
    Assert.Equal(1, recovered.Anchor.LineNumber);
    Assert.Equal(10, recovered.Anchor.Column); // Was 7, shifted by 3
}

#endregion
```

#### Step 5.2: Create CursorCollectionTests

New test file:

```csharp
// tests/TextBuffer.Tests/CursorCollectionTests.cs

public class CursorCollectionTests
{
    #region Basic Operations
    
    [Fact]
    public void CursorCollection_StartsWithPrimaryCursor()
    {
        TestEditorContext ctx = TestEditorBuilder.Create()
            .WithLines("Line 1", "Line 2")
            .WithVsCursorParity(true)
            .BuildContext();
        
        using CursorCollection collection = ctx.Model.CreateCursorCollection();
        
        Assert.Single(collection.Cursors);
        Assert.NotNull(collection.GetPrimaryCursor());
    }
    
    [Fact]
    public void CursorCollection_SetStates_CreatesCursors()
    {
        TestEditorContext ctx = TestEditorBuilder.Create()
            .WithLines("Line 1", "Line 2", "Line 3")
            .WithVsCursorParity(true)
            .BuildContext();
        
        using CursorCollection collection = ctx.Model.CreateCursorCollection();
        
        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 5)),
            CursorState.FromModelSelection(new Selection(2, 1, 2, 5)),
            CursorState.FromModelSelection(new Selection(3, 1, 3, 5)),
        ]);
        
        Assert.Equal(3, collection.Cursors.Count);
    }
    
    #endregion
    
    #region Normalize Tests
    
    [Fact]
    public void CursorCollection_Normalize_MergesOverlappingSelections()
    {
        TestEditorContext ctx = TestEditorBuilder.Create()
            .WithLines("Hello World")
            .WithVsCursorParity(true)
            .BuildContext();
        
        using CursorCollection collection = ctx.Model.CreateCursorCollection();
        
        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 6)),  // "Hello"
            CursorState.FromModelSelection(new Selection(1, 4, 1, 9)), // "lo Wo"
        ]);
        
        collection.Normalize();
        
        Assert.Single(collection.Cursors);
        Selection merged = collection.GetSelections()[0];
        Assert.Equal(1, merged.Start.Column);
        Assert.Equal(9, merged.End.Column);
    }
    
    [Fact]
    public void CursorCollection_Normalize_MergesTouchingWhenOneCollapsed()
    {
        TestEditorContext ctx = TestEditorBuilder.Create()
            .WithLines("Hello World")
            .WithVsCursorParity(true)
            .BuildContext();
        
        using CursorCollection collection = ctx.Model.CreateCursorCollection();
        
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
        TestEditorContext ctx = TestEditorBuilder.Create()
            .WithLines("Hello World")
            .WithVsCursorParity(true)
            .BuildContext();
        
        using CursorCollection collection = ctx.Model.CreateCursorCollection();
        
        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 6)),   // "Hello"
            CursorState.FromModelSelection(new Selection(1, 6, 1, 12)), // " World"
        ]);
        
        collection.Normalize();
        
        Assert.Equal(2, collection.Cursors.Count); // Touching but not overlapping
    }
    
    [Fact]
    public void CursorCollection_Normalize_RespectsMultiCursorMergeOverlappingConfig()
    {
        TestEditorContext ctx = TestEditorBuilder.Create()
            .WithLines("Hello World")
            .WithVsCursorParity(true)
            .WithEditorOptions(new EditorCursorOptions { MultiCursorMergeOverlapping = false })
            .BuildContext();
        
        using CursorCollection collection = ctx.Model.CreateCursorCollection();
        
        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 8)),
            CursorState.FromModelSelection(new Selection(1, 4, 1, 12)),
        ]);
        
        collection.Normalize();
        
        Assert.Equal(2, collection.Cursors.Count); // Not merged because config disabled
    }
    
    #endregion
    
    #region Tracked Selection Tests
    
    [Fact]
    public void CursorCollection_StartStopTrackingSelections()
    {
        TestEditorContext ctx = TestEditorBuilder.Create()
            .WithLines("Hello World")
            .WithVsCursorParity(true)
            .BuildContext();
        
        using CursorCollection collection = ctx.Model.CreateCursorCollection();
        
        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 6)),
        ]);
        
        collection.StartTrackingSelections();
        
        // Insert at start
        ctx.Model.PushEditOperations([new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "XXX")], null);
        
        IReadOnlyList<Selection> recovered = collection.ReadSelectionFromMarkers();
        
        Assert.Equal(4, recovered[0].Anchor.Column); // Shifted by 3
    }
    
    #endregion
    
    #region LastAddedCursorIndex Tests
    
    [Fact]
    public void CursorCollection_LastAddedCursorIndex_TracksCorrectly()
    {
        TestEditorContext ctx = TestEditorBuilder.Create()
            .WithLines("Line 1", "Line 2", "Line 3")
            .WithVsCursorParity(true)
            .BuildContext();
        
        using CursorCollection collection = ctx.Model.CreateCursorCollection();
        
        collection.SetStates([
            CursorState.FromModelSelection(new Selection(1, 1, 1, 1)),
            CursorState.FromModelSelection(new Selection(2, 1, 2, 1)),
            CursorState.FromModelSelection(new Selection(3, 1, 3, 1)),
        ]);
        
        Assert.Equal(2, collection.GetLastAddedCursorIndex()); // Last secondary = index 2
    }
    
    #endregion
}
```

#### Step 5.3: Update TestEditorBuilder

Add helpers for feature flag:

```csharp
public TestEditorBuilder WithVsCursorParity(bool enabled)
{
    _creationOptions = _creationOptions with { EnableVsCursorParity = enabled };
    return this;
}

public TestEditorBuilder WithEditorOptions(EditorCursorOptions options)
{
    _editorOptions = options;
    return this;
}
```

---

## 4. Feature Flag Strategy

### 4.1 Flag Definition
```csharp
TextModelCreationOptions { EnableVsCursorParity = true }
```

### 4.2 Migration Path
1. **Phase A (This PR):** Flag defaults to `false`, all new paths are opt-in
2. **Phase B (After QA soak):** Flip default to `true`, keep legacy paths
3. **Phase C (Cleanup):** Remove legacy paths and flag

### 4.3 Dual-Path Points
| Location | Legacy Path | New Path |
|----------|-------------|----------|
| `Cursor._setState` | Updates `_selection`/`_stickyColumn` | Updates `_modelState`/`_viewState` |
| `CursorCollection` constructors | Uses `TextModel` directly | Uses `CursorContext` |
| Tracked range updates | No-op | Calls `_setTrackedRange` |

---

## 5. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking existing cursor behavior | Medium | High | Feature flag + dual path + extensive tests |
| Performance regression from tracked ranges | Low | Medium | Tracked ranges are lightweight (hidden decorations) |
| State sync issues model/view | Medium | Medium | Validate via `EnsureValidState` + integration tests |
| PartialCursorState type confusion | Low | Low | Strong typing + compiler checks |

### Rollback Plan
1. Set `EnableVsCursorParity = false` in all creation options
2. No data migration needed - cursor states are transient
3. Rollback is instant with no cleanup required

---

## 6. Test Matrix Additions

| Suite | New Tests | Coverage Goal |
|-------|-----------|---------------|
| `CursorCoreTests` | 8-10 | State validation, tracked ranges, dual-mode |
| `CursorCollectionTests` | 15-20 | `setStates`, `normalize`, tracking lifecycle |
| `CursorIntegrationTests` | 5-8 | Multi-cursor + edit + undo/redo |

### Verification Commands
```bash
# Run Cursor tests only
dotnet test --filter "FullyQualifiedName~Cursor" --nologo

# Run with feature flag enabled
PIECETREE_CURSOR_PARITY=1 dotnet test --filter "FullyQualifiedName~Cursor" --nologo
```

---

## 7. Dependencies

| Dependency | Status | Notes |
|------------|--------|-------|
| `WS4-PORT-Core` Stage 0 | ✅ Done | CursorConfiguration, CursorState, CursorContext |
| `WS2-PORT` Range helpers | ✅ Done | `Range.EqualsRange`, `Selection.PlusRange`, etc. |
| TextModel tracked ranges | ✅ Done | `_setTrackedRange`, `_getTrackedRange` |
| `TestEditorBuilder` | ✅ Done | From WS5-PORT |

---

## 8. Deliverable Checklist

- [ ] Feature flag `EnableVsCursorParity` in `TextModelOptions`
- [ ] `Cursor.cs` refactored with `_setState`, tracked ranges, dual-mode
- [ ] `CursorCollection.cs` with `setStates`, `normalize`, tracking lifecycle
- [ ] `CursorContext.FromModel` overload with `EditorCursorOptions`
- [ ] `CursorCoreTests` extended (8-10 new tests)
- [ ] `CursorCollectionTests` created (15-20 tests)
- [ ] `TestEditorBuilder.WithVsCursorParity` helper
- [ ] Update `docs/reports/alignment-audit/03-cursor.md` with Stage 1 completion
- [ ] Close `#delta-2025-11-26-aa4-cl7-cursor-core` placeholder

---

## 9. References

- **TS Sources:**
  - `ts/src/vs/editor/common/cursor/oneCursor.ts`
  - `ts/src/vs/editor/common/cursor/cursorCollection.ts`
  - `ts/src/vs/editor/common/cursorCommon.ts`
- **Existing C# Files:**
  - `src/TextBuffer/Cursor/Cursor.cs`
  - `src/TextBuffer/Cursor/CursorCollection.cs`
  - `src/TextBuffer/Cursor/CursorState.cs`
  - `src/TextBuffer/Cursor/CursorContext.cs`
  - `src/TextBuffer/Cursor/CursorConfiguration.cs`
- **Related Handoffs:**
  - `agent-team/handoffs/INV-CursorSnippet-Blueprint.md`
  - `docs/reports/alignment-audit/03-cursor.md`
  - `docs/reports/migration-log.md#ws4-port-core`

---

_End of CL7 Cursor Wiring Implementation Plan_
