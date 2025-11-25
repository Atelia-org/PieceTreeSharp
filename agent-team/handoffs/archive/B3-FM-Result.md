````markdown
# B3-FM Result – Sprint 03 R12 (SelectAllMatches Parity)

## Summary
- Completed Batch #3 focus item **B3-FM (#delta-2025-11-23-b3-fm)** by fully implementing `FindModel.SelectAllMatches()` so C# mirrors the TS multi-cursor semantics (search-scope filtering, sorted matches, primary cursor stability).
- Added FM-01/FM-02 regression tests that assert scope-aware ordering (`selectAllMatches` TS parity) plus the issue #14143 scenario where the existing primary cursor must remain active even when duplicate ranges are pruned.
- Updated supporting docs (Porter memory, Sprint 03 R12 log, TestMatrix, migration log) so QA/DocMaintainer can trace the new behavior and test coverage.

## Tests
- `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` ⇒ **Passed (186/186)**
- Targeted assertions: `DocUIFindModelTests.Test28_SelectAllMatchesHonorsSearchScopeOrdering()` and `DocUIFindModelTests.Test29_SelectAllMatchesMaintainsPrimaryCursorInDuplicates()`

## File Highlights
- `src/TextBuffer/DocUI/FindModel.cs` – new `SelectAllMatches()` implementation plus `SelectionInfo` bookkeeping so matches remain sorted and primary selection metadata survives reordering.
- `tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs` – FM-01/FM-02 parity tests + helpers (`ToRanges`, `CreateRange`, `Range` alias) to validate multi-selection ordering/primary retention.
- `tests/TextBuffer.Tests/TestMatrix.md` – records the additional DocUI coverage row and the Batch #3 baseline (186/186) for QA reference.

## Known Limitations / Follow-ups
- Remaining Batch #3 scope: DocUI FindController + selection-derived search heuristics; WordBoundary-focused tests still deferred pending Investigator-TS guidance.
- Need DocUI visual capture of multi-cursor overlays once FindController parity lands (tracked in Sprint 03 backlog, no action in this drop).

## Changefeed & Migration Log
- Logged under **2025-11-23 · B3-FM** in `docs/reports/migration-log.md` with command + artifacts; Sprint 03 R12 entry references this result file for the handoff chain.
````