# Doc Review – 2025-11-25

## Findings & Actions
1. **`#delta-2025-11-25-b3-search-offset` changefeed does not exist**  
   - Evidence: `agent-team/indexes/README.md` has no `### delta-2025-11-25-b3-search-offset` block (`rg "delta-2025-11-25-b3-search-offset"` returns no matches), yet the anchor is referenced in `agent-team/task-board.md` (B3-SearchOffset rows), `docs/plans/ts-test-alignment.md` (PieceTree row), `docs/sprints/sprint-03.md` (Runs R30–R32), `docs/reports/migration-log.md` ("B3-SearchOffset-PORT" entry), and multiple member memories.  
   - Action: draft the missing changefeed section in `agent-team/indexes/README.md` summarizing the SearchOffset scope (tests, commands, evidence, doc links). Until it exists, those documents should either reference a concrete placeholder (e.g., `TBD`) or drop the dead link.

2. **SearchOffset handoff files are referenced but absent**  
   - Evidence: `ls agent-team/handoffs | grep SearchOffset` only returns `B3-PieceTree-SearchOffset-{INV,PLAN}.md`; however `agent-team/task-board.md` (B3-SearchOffset-PORT/QA rows), `docs/sprints/sprint-03.md` (Run R32), and `docs/reports/migration-log.md` ("B3-SearchOffset-PORT") all link to `agent-team/handoffs/B3-PieceTree-SearchOffset-PORT.md`, and the Task Board also points QA to `agent-team/handoffs/B3-PieceTree-SearchOffset-QA.md`.  
   - Action: either create the PORT/QA handoff files with the expected content (code summary, commands, evidence) or adjust every referencing doc to remove those links until the artifacts exist.

3. **TestMatrix never recorded the SearchOffset delta and still reports 321/321**  
   - Evidence: `tests/TextBuffer.Tests/TestMatrix.md` has no mention of `#delta-2025-11-25-b3-search-offset` (no targeted rerun rows, no task board references) and its "Total Tests Passing" section still states `321/321`. Running `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` now reports `324` tests (see 2025-11-25 run in terminal log), reflecting the addition of `PieceTreeSearchOffsetCacheTests` (5 Facts) and `PieceTreeBufferBomTests` (3 Facts).  
   - Action: update `tests/TextBuffer.Tests/TestMatrix.md` to add the SearchOffset targeted rerun entry (with the 5/5 command) and refresh the baseline row + headline totals from 321 → 324. Sync the same total everywhere it is quoted (e.g., `agent-team/members/qa-automation.md`, `docs/reports/migration-log.md`, Sprint log) once the matrix is corrected.

4. **Docs claim R32 SearchOffset deliverables landed, but supporting assets are missing**  
   - Evidence: `docs/sprints/sprint-03.md` Run R32 and `agent-team/task-board.md` mark B3-SearchOffset-PORT as ✅ with `PieceTreeSearchOffsetCacheTests` + `agent-team/handoffs/B3-PieceTree-SearchOffset-PORT.md`, yet there is no corresponding handoff, TestMatrix row, or changefeed entry (see Findings 1–3). `docs/plans/ts-test-alignment.md` also references the absent changefeed while stating "Priority #2 ... In Flight – scripts captured".  
   - Action: either publish the missing artifacts (changefeed + handoffs + matrix rows) to match the status claims, or roll the plan/task-board/sprint entries back to "Planned" until QA + Info-Indexer work is actually complete.
