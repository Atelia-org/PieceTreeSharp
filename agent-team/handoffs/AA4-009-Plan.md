# AA4-009 Plan – QA Automation

## Overview
This plan instructs QA-Automation to validate AA4-005/AA4-006 parity work, expand test coverage into `TestMatrix.md`, run the full `dotnet test` suite, and report any remaining regressions or flaky tests.

Primary objective: validate builder/factory & change buffer fixes, ensure tests pass, and certify the change for Info-Indexer to publish the changefeed.

## Tasks
1) Read your memory file: `agent-team/members/qa-automation.md` and update your worklog before starting.
2) Read the Porter artifacts: `agent-team/handoffs/AA4-005-Result.md`, `agent-team/handoffs/AA4-006-Result.md`, and ensure you understand the edge cases. Consult `docs/reports/audit-checklist-aa4.md#cl5`/`#cl6` and `docs/sprints/sprint-02.md`.
3) Add the Porter-provided tests (append optimization, chunk split, CRLF repair, CRLF fuzz tests, SearchCache tests) to the TestMatrix in `src/PieceTree.TextBuffer.Tests/TestMatrix.md`. Include categories (e.g., CL5/CL6) and expected outcome (pass/skip/flake).
4) Run baseline unit tests and integration tests with `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` and capture the results: total tests, passed, failed, skipped, and duration.
5) If any test failure occurs, create a fine-grained bug entry in the QA log (`agent-team/handoffs/AA4-009-QA.md`) describing failure reproduction steps, evidence, and proposed priority for Porter to fix (e.g., log file, minimal reproducer code). QA should also run targeted repeat runs to detect flakiness.
6) Run the new CRLF fuzz harness (where implemented) and report any additional failures or assertions.
7) Run over TestMatrix to ensure total tests >= 92 and ensure new tests are included with labels & test owner(s).
8) If the suite is green and regressions are resolved, update `docs/reports/migration-log.md` and `agent-team/indexes/README.md` to mark the change as 'QA Verified' and prepare to publish the changefeed in OI-011.
9) Before reporting, append an end-of-task worklog to `agent-team/members/qa-automation.md`.

## Output / Deliverables
- `agent-team/handoffs/AA4-009-QA.md` with test summary, failures (if any), `dotnet test` results, and issue or fix recommendations.
- Update `src/PieceTree.TextBuffer.Tests/TestMatrix.md` with newly added tests and coverage.
- Run counts and `dotnet test` baseline for Info-Indexer.


