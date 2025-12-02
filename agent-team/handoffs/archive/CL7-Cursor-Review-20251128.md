```markdown
# CL7 Cursor Review – 2025-11-28
_By: Porter-CS (Viktor Zoric) • Scope: `Range/Selection` helpers, `Cursor.cs`, `CursorCollection.cs`, `CursorContext.cs`, cursor atomic move ops + Cursor* test suites • Anchor: #delta-2025-11-26-aa4-cl7-cursor-core_

## 1. Findings & Fixes
1. **CursorContext propagation** – `CursorCollection.UpdateContext` previously only swapped the stored context, so existing `Cursor` instances kept stale `_context` references (breaking `_setState`/`MoveTo` when editor options changed). Added `Cursor.UpdateContext(...)` plus a propagation loop so every cursor revalidates its state when the context refreshes.
2. **Top/Bottom view selection parity** – The `GetTopMostViewPosition`/`GetBottomMostViewPosition` helpers relied on LINQ sorting, which does not mirror TS `findFirstMin/findLastMax` semantics (especially for tie-breaking). Implemented the TS-style single-pass comparison using `TextPosition.Compare`, ensuring top picks the first minimal cursor and bottom picks the last maximal cursor.
3. **Legacy removal semantics** – The legacy `RemoveCursor` API allowed removing the primary cursor (by disposing index 0) and lacked bounds checks for secondary removal. Updated `RemoveCursor` to no-op for primary/unknown cursors and hardened `RemoveSecondaryCursor` with bounds guards so normalization/kill operations cannot underflow.
4. **Tracked selection plumbing** – `_setState`, `ReadSelectionFromMarkers`, `UpdateTrackedRange`, and `RemoveTrackedRange` bypassed `CursorContext`, which meant tracked ranges were not re-bound when the view-model changed and view validation was limited to simple clamps. Ported TS `Cursor._validateViewState` (using `ICursorSimpleModel.NormalizePosition` + cached validations), added a `modelState/viewState == null` guard, and now run all tracked-range mutations through `context.Model`.
5. **Context-aware Cursor API** – Added `Cursor.UpdateContext` (invoked by the collection and constructors) so `_context` stays in sync and `_setState` reruns when `EditorCursorOptions` change. Public APIs (`SetState`, `EnsureValidState`, tracking helpers) now validate `context` arguments and reuse `_setState` to sync tracked ranges.
6. **Atomic tab move parity + warnings cleanup** – Ported `AtomicTabMoveOperations` verbatim from `cursorAtomicMoveOperations.ts` and added `CursorAtomicMoveTests` (with `DisableDiscoveryEnumeration` to avoid xUnit serialization warnings). `CursorMultiSelectionTests` now build collections via `CursorContext` and received whitespace fixes to keep analyzers quiet.
7. **Test coverage expansion** – New suites (`CursorCollectionTests`, CL7 block in `CursorCoreTests`) exercise state wiring, tracked selections, normalize merge semantics, and VS parity toggles. Existing helpers (`Selection.ToRange`, `Selection.PlusRange`, `Range.EqualsRange`) continue to back both implementation and tests.

## 2. Testing
```bash
export PIECETREE_DEBUG=0 \
  && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj \\
       --filter "FullyQualifiedName~CursorAtomicMoveTests|FullyQualifiedName~CursorCollectionTests|FullyQualifiedName~CursorCoreTests|FullyQualifiedName~CursorMultiSelectionTests" \\
       --nologo
```
_Result: 129 tests (127 passed / 2 skipped for existing TODOs), 0 failures, ~2.1s._

## 3. Residual Risks / Follow-ups
- Stage 2 work (driving all movement/selection commands through `_setState`) is still pending; current operations update legacy `_selection` fields when `EnableVsCursorParity=false`.
- Two intentional skips remain in `CursorCoreTests` (SelectHighlights & multi-cursor snippets) awaiting downstream controller ports per `#delta-2025-11-26-aa4-cl7-cursor-core`.
- No regression tests yet cover VS cursor parity flag toggling inside `TextModel.CreateCursorCollection`; plan to extend harness coverage once column-select + command actions are ported.
```
