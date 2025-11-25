# B3 – PieceTree Search Offset Cache Plan (2025-11-25)

**Changefeed target:** `#delta-2025-11-25-b3-search-offset`

## Context
- Source backlog: `agent-team/handoffs/B3-PieceTree-Deterministic-Backlog.md` Priority #2 (TS lines 1809–1884).
- Recent baseline: `#delta-2025-11-23-b3-piecetree-fuzz` (harness) and `#delta-2025-11-24-b3-piecetree-fuzz` (deterministic suites) already unlocked the helpers we need (`PieceTreeFuzzHarness`, `PieceTreeScript`, `PieceTreeBufferAssertions`).
- Snapshot deterministics closed in `#delta-2025-11-25-b3-piecetree-snapshot`; search offset cache is the next blocker before chunk-search and random scripts.

## Scope
Bring the remaining TS search offset cache suites across:
1. **Render whitespace exception script** (TS `render white space exception`).
2. **Normalized EOL inserts** (TS "Line breaks replacement is not necessary when EOL is normalized" cases 2–5). Case 1 already maps to `PieceTreeNormalizationTests.Line_Breaks_Replacement_Is_Not_Necessary_When_EOL_Is_Normalized`.

C# deliverable: new test fixture `PieceTreeSearchOffsetCacheTests` (located under `tests/TextBuffer.Tests`) with five Facts mirroring the TS scripts. Tests must assert:
- `testLineStarts` vs `PieceTreeBuffer.LineStarts` parity via `PieceTreeBufferAssertions.AssertLineStarts`.
- `testLinesContent` vs `PieceTreeBuffer.GetLinesRawContent()` parity via `PieceTreeBufferAssertions.AssertState`.
- `PieceTreeSearchCache` keeps offsets across normalized insert/delete paths (no stale cache hits, no duplicated line breaks).

## Execution Overview
- Leverage `PieceTreeDeterministicScripts` to host the TS edit logs so both Porter and QA can replay identical operations.
- Use `PieceTreeFuzzHarness` helpers (already introduced in `#delta-2025-11-23-b3-piecetree-fuzz`) to stage scripts when direct buffer API calls would be verbose.
- Tests should run under the same `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo` command that QA will re-use.
- Documentation touch points: `tests/TextBuffer.Tests/TestMatrix.md`, `docs/plans/ts-test-alignment.md#piecetree`, `docs/reports/migration-log.md`, `agent-team/task-board.md`, and `docs/sprints/sprint-03.md` (Run R30+ entries).

## Run Order & Budgets
| Step | Owner | runSubAgent Budget | Deliverable(s) | Notes & Dependencies |
| --- | --- | --- | --- | --- |
| 1. `B3-SearchOffset-INV` | Investigator-TS | 1 | Script extraction memo (TS ranges 1809–1884), normalized edit logs, mapping table (TS name → planned C# Fact), helper checklist. | Depends on updated TS references already captured in the backlog. Investigator should drop payloads into `agent-team/handoffs/B3-PieceTree-SearchOffset-INV.md` and flag any deltas vs Appendix in `docs/plans/ts-test-alignment.md`. |
| 2. `B3-SearchOffset-PORT` | Porter-CS | 2 | Implementation of `PieceTreeSearchOffsetCacheTests` + new deterministic script assets (if not already in `PieceTreeDeterministicScripts`). Wire tests into `TextBuffer.Tests.csproj`. | Reuse `PieceTreeScriptBuilder`, `PieceTreeBufferAssertions`, `PieceTreeFuzzHarness`. Ensure `PieceTreeNormalizationTests` references remain intact. |
| 3. `B3-SearchOffset-QA` | QA-Automation | 1 | QA handoff summarizing `dotnet test --filter PieceTreeSearchOffsetCacheTests` reruns (expected 5/5) + full suite baseline, `TestMatrix` rows, and captured console output. | Blocks on Porter artifacts and must cite changefeed `#delta-2025-11-25-b3-search-offset`. |
| 4. `B3-SearchOffset-INFO` | Info-Indexer | 1 | Changefeed entry + `docs/reports/migration-log.md` row referencing all touched files/tests and rerun commands. | Publish same day as QA handoff to keep Sprint 03 audit trail tight. |
| 5. `B3-SearchOffset-DOC` | DocMaintainer | 1 | Task Board, Sprint 03 (Run log), `docs/plans/ts-test-alignment.md`, and `tests/TextBuffer.Tests/TestMatrix.md` cross-links updated to the new changefeed. | Should confirm `agent-team/task-board.md` statuses (Batch #3 entries + Search Offset rows) and propagate to AGENTS if needed. |

## Dependencies & Risks
- **Prereq:** Deterministic scripts rely on the R25 harness; ensure no pending merges are altering `PieceTreeScript` formats.
- **Data collection:** Investigator must capture the exact TS script operations (including EOL normalization flags) so Porter can avoid guesswork.
- **Cache assertions:** Porter should expose helper assertions to inspect `PieceTreeSearchCache` internals, or leverage existing hooks in `PieceTreeBufferAssertions`. If hooks prove insufficient, a lightweight internal friend accessor may be required.
- **Test duration:** Five deterministic cases are lightweight (<2s). QA should still re-run the entire `PieceTree` filter bucket to catch regressions.

## QA & Documentation Checklist
1. Add a `PieceTreeSearchOffsetCache` row to `tests/TextBuffer.Tests/TestMatrix.md` with TS references, portability tier, and changefeed tag.
2. Log commands:
   - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo`
   - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`
3. Append migration log row referencing `PieceTreeSearchOffsetCacheTests.cs` (or equivalent) once merged.
4. Update `docs/plans/ts-test-alignment.md` Live Checkpoints to mark Priority #2 as "in-flight".
5. Ensure Task Board and Sprint 03 R-log quote the same changefeed anchor and runSubAgent IDs when closing the batch.

## Timeline Guidance
- Fits within Sprint 03 (runs R31–R34 if we start immediately after this plan).
- Investigator + Porter steps should land within two working days (given scripts already exist); QA/Info/Doc steps can share the closing day.
- If Porter uncovers cache instrumentation gaps, flag Planner immediately so we can insert a fast-track Porter fix before QA engages.
