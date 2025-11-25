# B3-PieceTree-Fuzz Execution Plan (R25–R29)

## Sprint 03 Decision (2025-11-24)
- **Fit assessment:** The proposed R25→R29 chain requires ~3.5 runSubAgent-days (0.5 + 1 + 1 + 0.5 + 0.5). With five working days left before Sprint 03 closes on **2025-11-29**, the sequence fits **if** we reserve consecutive Porter-CS slots on Nov 24–26 and keep QA/DocMaintainer ready on Nov 27–28.
- **Scope guardrails:** We will finish the deterministic suites, CRLF/search coverage, and single-seed fuzz smoke within Sprint 03. The heavier multi-seed “random unsupervised” soak plus optional buffer API perf instrumentation slide to Sprint 04 (documented below) to preserve QA/Doc bandwidth.
- **Escalation trigger:** If R25 is not merged by **Nov 25 10:00 UTC**, Planner will automatically slip R27–R29 to Sprint 04 and raise a change note in `docs/sprints/sprint-03.md`.

## Run Breakdown & Ordering
| Run | Target Date | Owner | Scope | Success Criteria | Dependencies |
| --- | --- | --- | --- | --- | --- |
| **R25 – Harness & Invariants** | Nov 24 | Porter-CS | Implement `PieceTreeFuzzHarness` + `FuzzLogCollector` integration, add env-driven seed (`PIECETREE_FUZZ_SEED`), extend `PieceTreeModel.AssertPieceIntegrity()` with RB-color/metadata checks, and add reusable range diff helpers under `tests/TextBuffer.Tests/Helpers/`. | New helpers compiled, unit tests covering harness utilities, and Investigator memo deltas referenced in `docs/reports/migration-log.md`. | Investigator spec (`B3-PieceTree-Fuzz-INV`), Planner approval for partial `InternalsVisibleTo` exposure. |
| **R26 – Deterministic Suites Batch 1** | Nov 25 | Porter-CS | Port TS deterministic insert/delete, prefix-sum, `getValueInRange`, and offset mapping suites into `PieceTreeBaseTests`, `PieceTreeRangeTests`, etc., using the R25 harness assertions after each edit. | Tests replay TS sequences verbatim, covering each bucket listed in Section 1 rows 1–3 of the INV memo; `dotnet test --filter PieceTreeDeterministic` passes and logs command in TestMatrix. | R25 merged; TS fixtures referenced in repo. |
| **R27 – CRLF + Search + Snapshot** | Nov 26 | Porter-CS | Port TS CRLF suites, buffer API/search offset cache/chunk-based search, and the remaining snapshot immutability cases. Provide a friend adapter for `PieceTreeBuffer.FindMatchesLineByLine`. Include a single-seed unsupervised fuzz (1000 ops) smoke test to validate harness throughput. | New tests green under `dotnet test --filter PieceTreeCRLF|PieceTreeSearch|PieceTreeSnapshot`; friend accessor limited to `InternalsVisibleTo(Tests)`; logs recorded for the single-seed fuzz run. | R25 + R26 merged; Planner sign-off on friend accessor exposure. |
| **R28 – QA Validation & Matrix Update** | Nov 27 | QA-Automation | Extend `tests/TextBuffer.Tests/TestMatrix.md` with deterministic/CRLF/fuzz rows, run full `dotnet test` plus focused fuzz commands under at least two seeds (`PIECETREE_FUZZ_SEED=271828` and default). Archive logs + failures (if any) into `agent-team/handoffs/B3-PieceTree-Fuzz-QA.md`. | Matrix updated, commands captured, TRX/log artifacts attached, and changefeed placeholder referenced. | R25–R27 merged in main; QA gets harness instructions from Porter. |
| **R29 – Changefeed & Documentation** | Nov 28 | DocMaintainer + Info-Indexer | Publish `#delta-2025-11-23-b3-piecetree-fuzz` with all file/test additions, update `AGENTS.md`, `docs/sprints/sprint-03.md`, `agent-team/task-board.md`, `docs/plans/ts-test-alignment.md`, and `docs/reports/migration-log.md`. | Delta published + cross-doc references consistent; Sprint 03 log reflects completion; Task Board rows updated to “Done”. | QA sign-off (R28) and Porter change hashes. |

## Scope Deferrals / Sprint 04 Carryovers
1. **Multi-seed unsupervised fuzz soak** (5+ seeds, nightly schedule) – move to Sprint 04 “Batch #3 follow-through” so QA can budget time; document as `B3-PieceTree-Fuzz-Soak` in Task Board backlog.
2. **`PieceTreeLineSnapshot` caching/perf instrumentation** – optional optimization flagged in `B3-PieceTree-Fuzz-INV`; defer until we observe performance regressions.
3. **Buffer API perf microbenchmarks** – leave under Sprint 04 along with any ICU/Intl segmenter parity once WordOps re-prioritize.

## Dependencies & Coordination
- **Friend access approval:** Planner authorizes a test-only accessor (internals visible to `PieceTree.TextBuffer.Tests`) so R26 can call `FindMatchesLineByLine`.
- **Changefeed anchor:** Every run will reference the placeholder `#delta-2025-11-23-b3-piecetree-fuzz` until Info-Indexer publishes it during R28.
- **Reporting:** Each owner updates `docs/sprints/sprint-03.md` progress log (R25–R29) immediately after their run and drops artifacts under `agent-team/handoffs/` with matching IDs (`B3-PieceTree-Fuzz-Harness.md`, `...-QA.md`, etc.).

## References
- Investigator brief: `agent-team/handoffs/B3-PieceTree-Fuzz-INV.md`
- Sprint tracker: `docs/sprints/sprint-03.md`
- Plan linkage: `docs/plans/ts-test-alignment.md`
- Task board: `agent-team/task-board.md`
- Changefeed placeholder: `agent-team/indexes/README.md#delta-2025-11-23-b3-piecetree-fuzz`
