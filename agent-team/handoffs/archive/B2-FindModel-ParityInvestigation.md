# B2-FindModel Parity Investigation

## Scope
- Compare VS Code find-model behaviour (TS) against current DocUI port.
- Inputs: `ts/src/vs/editor/contrib/find/browser/{findModel.ts, findDecorations.ts, findState.ts}` + `findModel.test.ts` vs `src/PieceTree.TextBuffer/DocUI/*` and `DocUIFindModelTests`.
- Surface the gaps that currently break 12 DocUI tests: cursor tracking, search scope clearing, decoration cleanup, replace/replaceAll, and match position bookkeeping.

## Observations
- **Search scope cannot be cleared**: TS `FindReplaceState.change` (findState.ts ≈L120-210) distinguishes between `undefined` (no change) and `null` (clear scope). C# signature `FindReplaceState.Change(..., Range[]? searchScope = null, ...)` (FindReplaceState.cs ≈L150-210) treats `null` as "no arg", so calling `Change(searchScope: null)` leaves the previous scope intact. This is why `Test01_IncrementalFindFromBeginningOfFile` never exits selection-find after toggling off, whereas TS immediately returns to global matches.
- **Replace blindly edits even when caret is not on a match**: TS `FindModel.replace` (findModel.ts ≈L214-267) only issues a command if the current selection already equals the next match; otherwise it just updates the selection. C# `FindModel.Replace` (FindModel.cs ≈L198-241) always edits whatever `_getNextMatch` returned, so typing `Replace` once skips the preview step and advances the caret, breaking Tests 20/21/31/32/34 and any scenario with look-ahead captures or multi-replace flows.
- **ReplaceAll diverges in two ways**:
  - After executing, VS Code keeps the caret anchored near the last replacement via `ReplaceAllCommand`/`ReplaceCommandThatPreservesSelection` (findModel.ts ≈L268-339). C# unconditionally resets the selection to `(1,1)` (`SetSelection(new Range(new TextPosition(1,1)...))`, FindModel.cs ≈L339-352), so tests expecting the caret to stay on the edited block (Test22) fail.
  - `_regularReplaceAll` in TS applies edits in reverse order to avoid offset drift. Our port builds edits from the tail but then calls `edits.Reverse()` before `PushEditOperations`, causing replacements to run from top-to-bottom with stale offsets. The symptom is the missing caret replacement on the final (empty) line in Test39 (``^`` on >1000 lines) and general misalignment risk in large documents.
- **MatchesPosition initialised incorrectly**: TS `research` (findModel.ts ≈L120-190) leaves `matchesPosition` at `0` until `_setCurrentFindMatch` selects something. C# `ComputeMatchesPosition` (FindModel.cs ≈L260-287) scans the match list and immediately returns the index of the first range after the caret, so `_state.MatchesPosition` becomes `1` even though nothing is selected. Loop-control tests 42/43 expect the widget to stay at `0` when `loop=false` until the user advances.
- **Model content change scheduling + decoration reset**: VS Code defers re-search via `_updateDecorationsScheduler` and, on flushes, clears editor decorations before recomputing (findModel.ts constructor + onDidChangeModelContent handler). Our `OnModelContentChanged` (FindModel.cs ≈L66-96) runs `Research` synchronously and `FindDecorations.Reset()` (FindDecorations.cs ≈L134-151) only clears local caches, leaving the actual `TextModel` decorations intact. Result: Test27 sees matches immediately after `setValue('hello\nhi')` (TS expected empty state until the async refresh), and Test30 shows duplicate `findMatch` entries because the old decorations were never removed when `_decorationIds` got reset.
- **Search scope start position drift**: Because `_decorations.SetStartPosition` is only called from `SetSelection` and `Replace`, clearing the scope (`searchScope: null`) never rewinds the start position to the global selection, so even after patching the state issue, the next automatic navigation needs to reset `_decorations` to the editor caret to mirror TS behaviour where cursor telemetry (via `onDidChangeCursorPosition`) keeps `_startPosition` in sync.
- **Large match sets lack overview handling**: TS flips to `_FIND_MATCH_NO_OVERVIEW_DECORATION` and approximate overview markers when matches > 1000 (findDecorations.ts ≈L120-210). The C# port currently ignores that path, so huge searches still try to materialise every decoration and minimap entry. Not a current test failure but a parity gap noted while tracing Test39.

## Required Changes (TS reference → C# target)
1. **Search scope API parity**
   - *TS*: `FindReplaceState.change` accepts explicit `null` scope (findState.ts ≈L150-190).
   - *C#*: `FindReplaceState.Change` (FindReplaceState.cs ≈L150-210) needs a tri-state input (e.g., `bool searchScopeProvided, Range[]? searchScope`) so dedup/clear works. Update listeners to handle `null` by calling `_decorations.Set(..., null)` and resetting `_state.SearchScope`.
   - *Tests blocked*: `Test01_IncrementalFindFromBeginningOfFile` when toggling selection find off.

2. **Replace flow guard + start-position updates**
   - *TS*: `replace()` (findModel.ts ≈L214-267) compares `selection.equalsRange(nextMatch.range)` before editing and updates `_decorations.setStartPosition` when only moving the caret.
   - *C#*: `FindModel.Replace` (FindModel.cs ≈L198-241) must replicate the guard: if `_currentSelection` != `nextMatch`, just set `_decorations` current match and return; only build a `TextEdit` when the selection already sits on the match. Also ensure `_decorations.SetStartPosition` follows the new caret like TS to keep incremental typing in sync.
   - *Tests blocked*: 20, 21, 31, 32, 34 and any replace-with-lookahead flows.

3. **ReplaceAll parity**
   - *TS*: `_regularReplaceAll`/`_largeReplaceAll` (findModel.ts ≈L268-364) apply edits from end-to-start and use commands that preserve the user selection.
   - *C#*: `FindModel.ReplaceAll` (FindModel.cs ≈L326-354) should drop `edits.Reverse()` (since edits were already collected tail-first) or, alternatively, collect them forward and rely on the undo service to batch them. After execution, restore caret via the same logic as TS (e.g., keep previous selection or emulate `ReplaceCommandThatPreservesSelection`). Consider porting the `_largeReplaceAll` chunking logic for >MATCHES_LIMIT consistency.
   - *Tests blocked*: 22 (caret after replace-all) and 39 (last blank line missed by reverse ordering).

4. **MatchesPosition bookkeeping**
   - *TS*: `research` uses `_decorations.getCurrentMatchesPosition(editorSelection)` and leaves result `0` when the caret is not on a match (findModel.ts ≈L150-190).
   - *C#*: `ComputeMatchesPosition` (FindModel.cs ≈L260-287) should only fall back to "first match after selection" when the existing caret actually overlaps a highlighted range, otherwise keep `0`. This keeps navigation history identical when `loop=false` (Tests 42/43).

5. **Deferred research & decoration cleanup**
   - *TS*: `onDidChangeModelContent` debounce via `_updateDecorationsScheduler` and `TimeoutTimer`, and `FindDecorations.reset` relies on the editor clearing decorations during flush (findModel.ts constructor + findDecorations.ts reset implementation).
   - *C#*: Mirror this by (a) introducing a delayed re-search in `OnModelContentChanged` (or at least skipping immediate recompute when `e.IsFlush`) and (b) adding a method in `FindDecorations` that removes the current owner IDs from `TextModel` when resetting so stale entries are not returned by `GetDecorationsInRange`.
   - *Tests blocked*: 27 (expect empty state immediately after `setValue`) and 30 (duplicate `findMatch` entries).

6. **Cursor/start-position sync when scope changes**
   - *TS*: `_editor.onDidChangeCursorPosition` keeps `_decorations.setStartPosition` aligned with the actual cursor (findModel.ts ≈L44-70).
   - *C#*: Inject equivalent updates when `SetCurrentFindMatch` or search-scope toggles run so that dropping selection find restarts navigation from the correct anchor. This pairs with item #1 for full scope parity.

7. **Overview decoration fallback for huge match sets (forward-looking)**
   - *TS*: switches to `_FIND_MATCH_NO_OVERVIEW_DECORATION` and approximated minimap regions above 1000 matches (findDecorations.ts ≈L140-210).
   - *C#*: Current `FindDecorations.Set` ignores that branch, risking expensive decoration floods. Not a failing DocUI test yet but worth tracking for parity completeness.

## Test Matrix
| Scenario | TS test (`findModel.test.ts`) | C# test | Status | Parity gap |
| --- | --- | --- | --- | --- |
| Incremental find + toggle selection scope | `incremental find from beginning of file` | `Test01_IncrementalFindFromBeginningOfFile` | ❌ | Cannot clear `searchScope` + start position not reset.
| Replace one (cursor not on match) | `replace hello` / `replace bla` / look-ahead variants | `Test20`, `Test21`, `Test31`, `Test32`, `Test34` | ❌ | Replace executes even when selection only moved.
| ReplaceAll basic | `replaceAll hello` | `Test22_ReplaceAllHello` | ❌ | Caret reset to (1,1) instead of preserved position.
| ReplaceAll large (`^` over >1000 matches) | `issue #32522` | `Test39_Issue32522_ReplaceAllWithCaretOnMoreThan1000Matches` | ❌ | Edit order reversed → last blank line not updated.
| Model flush handling | `listens to model content changes` | `Test27_ListensToModelContentChanges` | ❌ | Synchronous re-search + stale decorations.
| Single match bookkeeping | `issue #1914` | `Test30_Issue1914_NPEWhenThereIsOnlyOneFindMatch` | ❌ | Duplicate `findMatch` decorations remain.
| Looping off/on behaviour | `issue #3516` pair | `Test42` / `Test43` | ❌ | `matchesPosition` initialised as 1 instead of 0.
| Regex replace with look-ahead & capture | `replace when search string has look ahead regex (and variants)` | `Tests 31-34` | ❌ | Same replace guard issue as above.
| Baseline scenarios (navigation, simple search, etc.) | Remaining TS suites | Corresponding `FindModelTests` | ✅ | Already passing under C# port.

## Open Questions
- Duplicate decorations (Test30) strongly suggest `FindDecorations.Reset` needs to call into `TextModel.RemoveAllDecorations(ownerId)` during flush, but we should confirm whether `TextModel` itself is expected to drop decorations on `setValue`, as in VS Code, or if the port must do it manually.
- For deferred research (Test27), do we need the exact timer semantics (100ms run-once scheduler) or is a simplified "skip immediate pass on flush, rerun after event loop" sufficient for DocUI consumers?
- How should we expose explicit `null` vs `undefined` semantics in the public `FindReplaceState.Change` signature without breaking existing call sites? (e.g., add a `SearchScopeChange` enum or overload?)
- Overview decoration fallback: do we need the minimap/overview parity immediately, or can we stage it after functional fixes since no tests cover it yet?
