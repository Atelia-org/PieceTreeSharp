# B3-TextModel-Snapshot-QA

**Date:** 2025-11-25  
**QA:** QA-Automation (GitHub Copilot · GPT-5.1-Codex)  
**Delta Tag:** `#delta-2025-11-25-b3-textmodel-snapshot`

## Objective
- Re-run the snapshot-focused suites requested by Porter to confirm the new `TextModelSnapshot` wrapper (64KB batching, empty-chunk skip, EOS caching) behaves exactly like the TS original.
- Double-check the legacy PieceTree snapshot/deterministic/search-cache suites to make sure wrapping `TextModel.CreateSnapshot()` does not leak regressions.
- Capture runtimes + pass counts for each command and refresh `tests/TextBuffer.Tests/TestMatrix.md` (targeted reruns + baseline row).

## Test Evidence
| Step | Command | Total | Passed | Duration | Notes |
| --- | --- | ---: | ---: | ---: | --- |
| a | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSnapshotTests --nologo` | 4 | 4 | 1.6s | Verifies the wrapper batches into 64KB windows, skips empty chunks, and caches EOS before returning `null`. |
| b | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotTests --nologo` | 2 | 2 | 1.7s | Legacy PieceTree snapshot invariants remain stable while `TextModel.CreateSnapshot()` now returns the wrapper. |
| c | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotParityTests --nologo` | 4 | 4 | 1.7s | TS parity scripts (`bug #45564`, immutable snapshots 1/2/3) drain identical chunk sequences. |
| d | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo` | 5 | 5 | 1.7s | Snapshot wrapper leaves the search offset cache invalidation logic untouched. |
| e | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeDeterministicTests --nologo` | 50 | 50 | 1.6s | Deterministic prefix-sum/CRLF suites remain green, ensuring PieceTree invariants are unchanged. |
| f | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | 321 | 321 | 59.7s | Full-suite baseline for Sprint 03 Run R30; expected PieceTree dump ("A\r") surfaced once while the CRLF sentinel asserts ran. |

## Notes & Follow-ups
- No regressions or timing anomalies observed; the new wrapper stays transparent to snapshot/deterministic/search cache flows.
- `tests/TextBuffer.Tests/TestMatrix.md` now lists the new targeted rerun block plus a refreshed baseline row showing 321/321 passing for this delta.
- Residual risk: none flagged; continue to monitor CRLF dumps during full runs (existing known diagnostic when CR-only buffers are enumerated).
