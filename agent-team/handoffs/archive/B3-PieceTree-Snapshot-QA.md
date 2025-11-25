# B3 PieceTree Snapshot QA — 2025-11-25

- **QA window:** 2025-11-24T18:38:57Z (UTC)
- **References:**
  - Porter deliverable [`agent-team/handoffs/B3-PieceTree-Snapshot-PORT.md`](B3-PieceTree-Snapshot-PORT.md)
  - Migration log entry [`docs/reports/migration-log.md#delta-2025-11-25-b3-piecetree-snapshot`](../../docs/reports/migration-log.md#delta-2025-11-25-b3-piecetree-snapshot)
- **Env:** `PIECETREE_DEBUG=0`, repo head at run time, commands executed from `/repos/PieceTreeSharp`.

## Scope
Validate Porter-CS snapshot parity drop (`#delta-2025-11-25-b3-piecetree-snapshot`) by rerunning the new `PieceTreeSnapshotParityTests` plus the full `TextBuffer.Tests` suite to ensure the streaming iterator + `SnapshotReader` helper behave identically to the TS originals.

## Commands & Outcomes
1. `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotParityTests --nologo`
   - Total 4 · Passed 4 · Failed 0 · Skipped 0 · Duration 1.7s.
2. `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`
   - Total 312 · Passed 312 · Failed 0 · Skipped 0 · Duration 53.6s.

## Observations
- Snapshot parity suite drained each snapshot exactly once via `SnapshotReader`, matching TS `getValueInSnapshot` behavior—no duplicate chunk output or missed BOM scenarios observed.
- Full-suite log emitted the expected single `PieceTreeModel Dump` block from CRLF coverage; no regressions or retries were required.
- Outstanding handoff note (OI-013 snapshot tooling) remains unchanged; QA evidence above is now mirrored in `tests/TextBuffer.Tests/TestMatrix.md` and Sprint 03 Run R29.
