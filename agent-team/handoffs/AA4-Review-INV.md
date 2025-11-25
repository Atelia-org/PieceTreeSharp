# AA4-Review-INV – Investigator Notes (2025-11-24)

## Scope
- Staged doc/UI changes under `src/TextBuffer/DocUI/*` (`FindDecorations.cs`, `FindModel.cs`, `DocUIFindController.cs`).
- Decoration infrastructure in `src/TextBuffer/TextModel.cs` plus new tests (`DecorationStickinessTests`, `DecorationTests`, `DocUIFindDecorationsTests`, `TestEditorContext`).
- Documentation touchpoints: `agent-team/task-board.md`, `tests/TextBuffer.Tests/TestMatrix.md`.

## Change Summaries & Findings

### 1. FindDecorations & TextModel helpers
- **Summary:** `FindDecorations` now allocates a per-instance owner ID, removes `_cachedFindScopes`, adds range-highlighting + viewport-aware overview throttling, and feeds the new TextModel helpers (`GetAllDecorations`, `GetLineDecorations`, `GetDecorationIdsByOwner`). `DocUIFindController`/`FindModel` accept a viewport-height provider so large result sets can throttle overview ruler decorations. Tests cover stickiness matrix + overview throttling and assert the new APIs.
- **Status:** Implementation matches the TS surface for decorations; no blocking issues spotted in this layer. Keep the new helpers documented so Porter/QA can reuse them when migrating DocUI diff/minimap features.

### 2. FindModel search-scope plumbing still static (Sev: Critical)
- **Files:** `src/TextBuffer/DocUI/FindModel.cs` (lines 168-210) vs `ts/src/vs/editor/contrib/find/browser/findModel.ts` (lines 205-242).
- **Problem:** Even after removing `_cachedFindScopes`, `FindModel.ResolveFindScopes()` simply returns `_state.SearchScope` (plain `Range` structs). When the user enables find-in-selection and then edits text, these structs do **not** move with the document, so every subsequent `Research()`/`FindMatches()` call searches stale offsets. TS fixes this by falling back to `this._decorations.getFindScopes()` whenever `newFindScope` is `undefined`, leveraging tracked decorations to follow edits.
- **Impact:** The newly-added tests (`DocUIFindDecorationsTests.FindScopesTrackEdits`) validate the decoration object directly, but `FindModel` never queries those ranges. Users will still see find-in-selection jump to the old location after the first edit, and DocUI cannot rely on scope highlights staying in sync.
- **TODO – Porter:**
  1. Mirror TS logic: in `FindModel.Research`, if `searchScope` wasn’t explicitly provided for this call, ask `_decorations.GetFindScopes()` and use that list for both searching **and** scope reapplication.
  2. When a scope is active, stop reading `_state.SearchScope` after the first application (or refresh it from the tracked decoration) so edits no longer desync the state bag.
- **TODO – QA:** Add a `DocUIFindModelTests` / `DocUIFindControllerTests` case that: (a) sets a selection scope, (b) types inside the scope, (c) asserts the next `FindNext` uses the updated coordinates. This corresponds to TS regression tests around `findController.test.ts` issue #9043 / `FindModel` scope persistence.

### 3. Multi-line scope normalization removed (Sev: High)
- **Files:** `src/TextBuffer/DocUI/FindModel.cs` (lines 188-208) vs `ts/src/vs/editor/contrib/find/browser/findModel.ts` (same block cited above).
- **Problem:** The C# port dropped `_normalizeFindScopes`. TS always expands multi-line scopes to start at column 1 and trims the trailing blank line when the end column is 1. By returning the raw selection, the DocUI port now searches only the literal selection span, which diverges from VS Code’s behavior for partial multi-line selections and the `Issue27083` family of tests.
- **Impact:**
  - Multi-line selections beginning mid-line will miss matches that live before the selection’s starting column, whereas VS Code still finds them because it normalizes to the full line.
  - Trailing newline trimming no longer happens, so regexes like `^$` or `\n` behave differently at the scope boundary.
  - The new test `DocUIFindDecorationsTests.FindScopesPreserveTrailingNewline` now codifies behavior that does **not** match TS, making future parity fixes harder.
- **TODO – Porter:** Reintroduce a normalization helper (same logic as TS: shift start column to 1, trim trailing newline via `GetLineMaxColumn`) before calling `_model.FindMatches` and before passing scopes into `FindDecorations.Set`. If divergence is intentional, document it in `docs/plans/ts-test-alignment.md` and add experiments proving VS Code’s scope actually keeps the raw range.
- **TODO – QA:** Once normalization is restored, extend `DocUIFindModelTests` with the TS cases around `Issue27083` and multi-selection scopes to prevent regressions.

### 4. Coverage claims vs. actual parity (Sev: Medium)
- **Files:** `tests/TextBuffer.Tests/TestMatrix.md` (CL4.F5 row) and `agent-team/task-board.md` (B3-Decor marked Done).
- **Problem:** The matrix now marks CL4.F5 “Covered” based on the new decoration-specific tests, yet the functional gaps above (scope tracking + normalization) remain. Without integration tests, the coverage entry risks hiding the regression from Porter/QA planning.
- **TODO – DocMaintainer/QA:** Keep CL4.F5 flagged as “At risk” (or add a note) until the FindModel fixes ship. Update the Task Board row with a follow-up subtask for the Fix/QA work so downstream agents know another Porter pass is required.

## Suggested Next Steps
1. Porter-CS to implement the FindModel scope plumbing + normalization fixes, referencing the TS logic noted above.
2. QA-Automation to add end-to-end DocUI tests that cover editing after enabling find-in-selection and multi-line scope normalization scenarios.
3. DocMaintainer to annotate TestMatrix/Task Board once the fixes are staged, including the rerun commands for targeted suites.
4. Investigator to re-review the follow-up patch (AA4-Review-INV#2) focusing on integration behavior rather than decoration-only surfaces.

## Re-review – 2025-11-24
- **Status overview:** Staged delta `#delta-2025-11-24-find-scope` refreshes DocUI scope plumbing/normalization. Targeted `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo` now reports 44/44 green, and `DocUIFindDecorationsTests` covers scope tracking plus overview throttling.
- **F2 – Scope tracking (Resolved):** `FindModel.ResolveFindScopes()` (src/TextBuffer/DocUI/FindModel.cs lines 194-244) now consumes a pending state override once, then hydrates from `_decorations.GetFindScopes()` so active ranges follow edits. `FindDecorations.GetFindScopes()` (src/TextBuffer/DocUI/FindDecorations.cs lines 126-149) rebuilt to read live decoration IDs (no stale cache). Regression tests `DocUIFindModelTests.Test45_SearchScopeTracksEditsAfterTyping` and `DocUIFindDecorationsTests.FindScopesTrackEdits` confirm the decorations move with insertions before/inside the scope.
- **F3 – Scope normalization (Resolved):** `NormalizeScopes()` (FindModel.cs lines 233-258) reintroduces TS `_normalizeFindScopes` behavior (start column forced to 1, trailing newline trimmed when end column is 1). `DocUIFindModelTests.Test46_MultilineScopeIsNormalizedToFullLines` + the TestMatrix row for CL4.F5 (tagged `#delta-2025-11-24-find-scope`) match the TS issue #27083 expectations.
- **F4 – Scoped regex replace (Resolved):** `FindModel.GetMatchesForReplace()` now invokes `GetActiveFindScopesForReplace()`/`NormalizeScopes()` before calling `TextModel.FindNextMatch`, matching TS `_getReplaceString` hydration. Regression **Test47_RegexReplaceWithinScopeUsesLiveRangesAfterEdit** locks the behavior, and `tests/TextBuffer.Tests/TestMatrix.md` references targeted rerun `#delta-2025-11-24-find-replace-scope` (45/45). Documentation trail updated via `docs/reports/migration-log.md`（B3-FM-ReplaceScope）与 Info-Indexer changefeed。Remaining AA4 scope work limited to B3-FM multi-selection (separate task).
