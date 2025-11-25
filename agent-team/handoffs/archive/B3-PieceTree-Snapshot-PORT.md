# B3-PieceTree-Snapshot-PORT
_Date: 2025-11-25_

## Completed in this drop
- **Snapshot streaming parity:** `src/TextBuffer/Core/PieceTreeSnapshot.cs` now mirrors the TS snapshot iterator: `_index == 0` emits BOM once, subsequent `Read()` calls stream each piece slice in order, and empty snapshots terminate immediately. This prevents earlier chunk duplication when draining via helper loops and lets QA rely on TS-style deterministic output.
- **TextModel API + helper:** Added `TextModel.CreateSnapshot(bool preserveBom = false)` so higher layers/tests can request snapshots without reaching into `PieceTreeModel`. Introduced `tests/TextBuffer.Tests/Helpers/SnapshotReader.cs` that drains `ITextSnapshot.Read()` exactly once (guarding against infinite loops), ensuring parity tests and smoke tests observe the same behavior as TS `getValueInSnapshot` helper.
- **Parity + smoke coverage:** Reworked `PieceTreeSnapshotTests` to allocate snapshots through `TextModel` then cache the content via `SnapshotReader`. Added `tests/TextBuffer.Tests/PieceTreeSnapshotParityTests.cs` to port TS `bug #45564` + `immutable snapshot 1/2/3` scripts. Updated `tests/TextBuffer.Tests/TestMatrix.md` with the new suite, rerun command, and delta tag `#delta-2025-11-25-b3-piecetree-snapshot`; migration log + Sprint R28 entry reference the same anchor.

## Outstanding work / follow-ups
- **Snapshot tooling (OI-013):** Now that `TextModel.CreateSnapshot()` and `SnapshotReader` exist, QA-Automation can begin wiring snapshot diff tooling. DocMaintainer to coordinate with Info-Indexer before flipping the OI-013 status.
- **Doc sync:** AGENTS/task-board still need to reference the new delta; DocMaintainer will pick this up after Info-Indexer broadcast.

## Validation commands
1. `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotParityTests --nologo`
   - Total 4 · Passed 4 · Failed 0 · Skipped 0 · Duration ≈1.7s.
2. `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`
   - Total 312 · Passed 312 · Failed 0 · Skipped 0 · Duration ≈58.7s.

## Evidence / References
- Code/tests: `src/TextBuffer/Core/PieceTreeSnapshot.cs`, `src/TextBuffer/TextModel.cs`, `tests/TextBuffer.Tests/Helpers/SnapshotReader.cs`, `tests/TextBuffer.Tests/PieceTreeSnapshotTests.cs`, `tests/TextBuffer.Tests/PieceTreeSnapshotParityTests.cs`.
- Docs: `tests/TextBuffer.Tests/TestMatrix.md`, `docs/reports/migration-log.md`, `docs/sprints/sprint-03.md#progress-log` Run R28, `agent-team/indexes/README.md#delta-2025-11-25-b3-piecetree-snapshot`, `agent-team/members/porter-cs.md` (Latest Focus + Worklog update).
