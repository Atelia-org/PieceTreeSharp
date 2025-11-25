# B3-FC Review – DocUI Find Controller Parity (2025-11-23)

## Context
- **Sprint Anchor:** docs/sprints/sprint-03.md (Batch #3 – FindController scope R14)
- **TS References:** ts/src/vs/editor/contrib/find/browser/findController.ts, findModel.ts, find.test.ts
- **Audits Consulted:** agent-team/handoffs/AA4-004-Audit.md, docs/reports/migration-log.md (Batch #3 entries)
- **Staged C# Files Reviewed:**
  - src/TextBuffer/DocUI/DocUIFindController.cs
  - src/TextBuffer/DocUI/FindModel.cs
  - src/TextBuffer/DocUI/FindUtilities.cs
  - tests/TextBuffer.Tests/DocUI/*

## Findings

### Warning W1 – Search scope clears when auto-start runs with empty selection
- **Location:** DocUIFindController.cs (Start at L275-L308)
- **TS Parity:** CommonFindController._start (ts/src/vs/editor/contrib/find/browser/findController.ts#L205-L242) only overwrites `state.searchScope` when `currentSelections.some(!selection.isEmpty())`.
- **Issue:** `_state.Change` is always invoked with `searchScopeProvided: options.UpdateSearchScope`, even when `BuildSearchScope()` returns `null`. Re-running `Start` with `UpdateSearchScope=true` but no non-empty selection drops the previous scoped range, so `find in selection` silently disables itself (contrary to VS Code, which keeps the last scope until the user clears it).
- **Impact:** Scoped searches created via auto find-in-selection (Auto/Multiline modes) or manual harness calls lose their range the moment the caret collapses, breaking AA4-004 CL8 “scope lifecycle” intent.
- **Fix Recommendation:** In `DocUIFindController.Start` only pass `searchScope` and set `searchScopeProvided=true` when `BuildSearchScope()` returns a non-null array. Otherwise leave the existing `_state.SearchScope` untouched (mirrors TS behavior where `stateChanges.searchScope` stays undefined).
- **Tests Needed:** Extend `DocUIFindControllerTests` with a scenario that (1) seeds a scope via `UpdateSearchScope = true`, (2) collapses the selection, (3) calls `Start` again with `UpdateSearchScope = true`, and asserts `SearchScope` still matches the original range.
- **Resolution (2025-11-23 – `#delta-2025-11-23-b3-fc-scope`):** `DocUIFindController.Start` now only passes `searchScope` to `_state.Change` when `BuildSearchScope()` returns a non-null range, and `DocUIFindControllerTests.SearchScopePersistsWhenSelectionCollapses` safeguards the behavior.

### Warning W2 – `NextSelectionMatchFindAction` no-ops on whitespace instead of revealing Find widget
- **Location:** DocUIFindController.cs (NextSelectionMatchFindAction at L243-L269)
- **TS Parity:** `SelectionMatchFindAction.run` (ts/src/vs/editor/contrib/find/browser/findController.ts#L865-L900) always calls `controller.start(...)` when `moveToNextMatch()` fails, even if `getSelectionSearchString` returned `null`, so the widget opens and focuses input.
- **Issue:** The C# port only calls `Start(...)` if `SeedSearchStringFromSelection(...)` succeeded. When the caret sits on whitespace/punctuation (no seed) and the user presses Ctrl/Cmd+F3, the command simply returns `false` and the DocUI find session never becomes visible, diverging from VS Code’s behavior and blocking clipboard seeding/loop fallback.
- **Impact:** Batch #3 objective “selection utilities parity” is not met; keyboard users lose the ability to enter a find session from whitespace contexts, and QA can’t reproduce TS issue #38232 flows faithfully.
- **Fix Recommendation:** After `MoveToNextMatch()` fails, always invoke `Start(...)` (with `SeedSearchStringFromSelection = SelectionSeedMode.None`) before retrying `MoveToNextMatch()`, regardless of the earlier seeding result. This keeps Ctrl/Cmd+F3 behavior aligned with TS and ensures `_state.IsRevealed` flips on.
- **Tests Needed:** Add a DocUIFindController test that positions the caret on whitespace, calls `NextSelectionMatchFindAction()`, and asserts the widget becomes visible (and that `_state.SearchString` stays untouched when nothing can be seeded).
- **Resolution (2025-11-23 – `#delta-2025-11-23-b3-fc-scope`):** `NextSelectionMatchFindAction` now always calls `Start(...)` after a failed `MoveToNextMatch()` and immediately retries; `DocUIFindControllerTests.NextSelectionMatchOnWhitespaceRevealsWidget` confirms widget visibility flips on even without a selection seed.

## Next Steps
1. ✅ (2025-11-23) Porter-CS 修复完成，`DocUIFindController.Start` scope 生命周期与 `NextSelectionMatchFindAction()` whitespace 行为现已与 TS 对齐，详情见 **#delta-2025-11-23-b3-fc-scope**。
2. QA-Automation 复查 DocUI FindController 回归（15/15 targeted run）并决定是否追加更高阶 scope/clipboard 用例，结果将写入 `agent-team/handoffs/B3-FC-QA.md`（TBD）。
3. ✅ Info-Indexer 已在 `agent-team/indexes/README.md#delta-2025-11-23-b3-fc-scope` 登记 scope/whitespace deferral 关闭情况；如后续再有 scope 相关缺口需复用同一 delta。

## Changefeed Hook
Coverage for this review now anchored at **agent-team/indexes/README.md#delta-2025-11-23-b3-fc-scope**; reference该 delta when updating downstream docs.
