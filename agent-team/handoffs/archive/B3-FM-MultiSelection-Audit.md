# B3-FM Multi-Selection Audit (2025-11-24)

**Owner:** Investigator-TS  
**Plan Link:** `agent-team/handoffs/B3-FM-MultiSelection-Plan.md`  
**Target changefeed tag:** `#delta-2025-11-24-b3-fm-multisel`

## Context
Batch #3 still lacks the two TS parity tests that cover multi-selection search scopes (`findModel.test.ts` Test07 & Test08). These scenarios validate that `FindModel` keeps navigation constrained to the union of user selections, including overlapping ranges. This audit captures the canonical TS behavior, highlights the missing harness features on the C# side, and enumerates the code/test/doc work Porter + QA must finish before we can claim 43/43 parity for `DocUIFindModelTests`.

## TS Reference Snapshots
All references originate from `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts`.

### Test07 – `multi-selection find model next stays in scope (overlap)`
- **Search string:** `"hello"`
- **Flags:** `wholeWord = true`, `matchCase = false`
- **Input search scopes:** `[[7,1,8,2], [8,1,9,1]]`
- **Normalized scopes (TS `_normalizeFindScopes`)**:
  - Scope A → `[7,1,8,maxColumn(8)]`
  - Scope B → `[8,1,8,maxColumn(8)]` (second selection ends at column 1 of line 9, so it collapses to full line 8)
- **Decorations (constant through the test):** `[[7,14,7,19], [8,14,8,19]]`

| Step | Action | Cursor (start→end) | Highlighted | Notes |
| --- | --- | --- | --- | --- |
| S0 | After `findState.change(...)` | `[1,1→1,1]` | _none_ | `findMatches` already filtered to the two scopes above |
| S1 | `findModel.moveToNextMatch()` | `[7,14→7,19]` | `[7,14→7,19]` | First scope match becomes current |
| S2 | `moveToNextMatch()` | `[8,14→8,19]` | `[8,14→8,19]` | Advances to overlapping segment on line 8 |
| S3 | `moveToNextMatch()` | `[7,14→7,19]` | `[7,14→7,19]` | Wraps inside the same union of scopes |

### Test08 – `multi-selection find model next stays in scope`
- **Search string:** `"hello"`
- **Flags:** `matchCase = true`, `wholeWord = false`
- **Input search scopes:** `[[6,1,7,38], [9,3,9,38]]`
- **Normalized scopes:**
  - Scope A → `[6,1,7,maxColumn(7)]`
  - Scope B → `[9,3,9,38]` (single-line selection retains exact columns)
- **Decorations:** `[[6,14,6,19], [7,14,7,19], [9,14,9,19]]`

| Step | Action | Cursor (start→end) | Highlighted | Notes |
| --- | --- | --- | --- | --- |
| S0 | After `findState.change(...)` | `[1,1→1,1]` | _none_ | Matches limited to both scopes |
| S1 | `findModel.moveToNextMatch()` | `[6,14→6,19]` | `[6,14→6,19]` | Enters first scope |
| S2 | `moveToNextMatch()` | `[7,14→7,19]` | `[7,14→7,19]` | Continues inside first scope |
| S3 | `moveToNextMatch()` | `[9,14→9,19]` | `[9,14→9,19]` | Jumps to second scope (non-contiguous) |
| S4 | `moveToNextMatch()` | `[6,14→6,19]` | `[6,14→6,19]` | Wraps to earliest match within the scoped union |

These tables should be copied into the eventual C# tests so QA can diff expected vs actual cursor/highlight states exactly as TS does.

## Findings

### F1 – Test harness cannot express multi-selection editors yet
`tests/TextBuffer.Tests/DocUI/TestEditorContext.cs` only tracks a single `_selection` (`Range _selection` at lines 20-34) and exposes `SetPosition` for a single caret. There is no analogue to TS `withTestCodeEditor(..., editor.setSelections([...]))`, so we cannot simulate nor assert multiple selections or automatically derive `searchScope` arrays from them. As a result, Test07/08 were skipped (see `DocUIFindModelTests.cs` lines 439-456) because reproducing the TS setup would require manual plumbing that the harness does not support. **Action:** extend `TestEditorContextOptions` + the context itself with `SetSelections(params Range[])`, `GetSelections()`, and helpers to apply scopes exactly as TS harness does.

### F2 – `FindModel` only records a single active selection
`src/TextBuffer/DocUI/FindModel.cs` keeps a single `_currentSelection` (`SetSelection(Range selection)` around lines 60-95) and does not offer the `_setSelections` / `_startPositionBeforeSelectionScope` pairing that TS uses to remember the pre-selection start point when multiple scopes are applied. This means Porter currently cannot feed a `Selection[]` array (or remember which primary selection set `_globalStartPosition`) even if the harness produced one. Without augmenting `FindModel` with a `SetSelections(IEnumerable<Range> selections)` helper (or equivalent API used by `DocUIFindController`), we cannot reproduce VS Code’s behavior when different selections become active (e.g., verifying that the start position reverts correctly after clearing `searchScope`).

### F3 – No parity tests guard overlapping/disjoint scope navigation
`DocUIFindModelTests` still stop at Test06, leaving the overlapping (Test07) and disjoint (Test08) cases completely untested on the C# side. That means regressions inside `ResolveFindScopes()` (lines 200-233), `SearchRangeSet.MergeRanges` (disjoint ordering), or `_decorations.GetFindScopes()` would go unnoticed. We risk reintroducing the very TS bugs these tests prevent (stuck navigation, duplicated matches, wrap skipping). Porting the TS cases verbatim—including the scope arrays/tables above—is mandatory before Porter hands the suite to QA/Porter for completion.

## Required updates

### Porter-CS
1. **Harness upgrades (F1)**
   - Extend `TestEditorContextOptions` with `initialSelections?: Range[]` and expose `SetSelections(params Range[])` so tests can mutate multi-selection state mid-scenario.
   - Surface a `State.ChangeSearchScope(Range[] scopes)` helper to reduce boilerplate when asserting expected scopes.
2. **FindModel API parity (F2)**
   - Add an overload such as `SetSelections(IReadOnlyList<Range> selections, int primaryIndex)` that updates `_currentSelection`, caches `_startPositionBeforeSelectionScope`, and (optionally) stores the entire array for future assertions.
   - Ensure `_globalStartPosition` is restored correctly once the last scope is cleared (matching TS `_startPositionBeforeSelectionScope` semantics).
3. **Test port (F3)**
   - Implement `DocUIFindModelTests.Test07` and `Test08` using the tables above.
   - Keep numbering aligned with TS and update `tests/TextBuffer.Tests/TestMatrix.md` `FindModel` block to show 43/43 coverage.

### QA Hooks
- Targeted rerun once tests land:  
  `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests.Test0(7|8) --nologo`
- Full suite validation:  
  `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo`
- Capture before/after state diffs in `agent-team/handoffs/AA4-009-QA.md` referencing `#delta-2025-11-24-b3-fm-multisel`.

### Documentation & Coordination
- Link this audit from `docs/plans/ts-test-alignment.md` Live Checkpoints and from the task board rows `B3-FM-MSel-INV` / `B3-FM-MSel-PORT`.
- Once Porter lands the fixes, DocMaintainer should update `AGENTS.md`, Sprint 03, Task Board, and TestMatrix with the new delta tag.
- Info-Indexer: publish `#delta-2025-11-24-b3-fm-multisel` after tests pass.

---
**Next Steps:** Porter picks up the harness + API work (F1/F2), then ports Test07/08 and hands the suite to QA for the commands above. QA records targeted reruns, DocMaintainer propagates the changefeed references, and Planner can finally close the `B3-FM-MSel-*` tasks.
