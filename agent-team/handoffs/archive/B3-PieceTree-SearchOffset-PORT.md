# B3 – PieceTree Search Offset Cache Porter Memo (Run R31)

**Date:** 2025-11-25  \
**Owner:** Porter-CS  \
**Changefeed anchor:** `#delta-2025-11-25-b3-search-offset`

## Scope
- `tests/TextBuffer.Tests/PieceTreeSearchOffsetCacheTests.cs` now mirrors the TS search-offset cache suite (lines 1810-1884) with five deterministic Facts: render-whitespace plus the four normalized EOL insert variants.
- `PieceTreeDeterministicScripts` gained the `SearchOffset*` seeds + step logs so `PieceTreeFuzzHarness` can replay the TS scripts verbatim (builder normalization left enabled to match TS inputs).
- `PieceTreeBufferAssertions` exposes `AssertSearchCachePrimed`, ensuring every script finishes with the cache warmed against the final snapshot, alongside the existing `AssertState`/`AssertLineStarts` parity checks.
- No runtime code changes were required beyond the helper/script additions already merged for this drop; the porter task here captures documentation + verification so QA/Info-Indexer can unblock their queues.

## Commands Executed
- `cd /repos/PieceTreeSharp && export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo`
  - Result: **5/5 passed**, duration **1.6s** (PIECETREE_DEBUG disabled). Run output archived in terminal history for QA reference.

## Next Steps / Dependencies
- **QA-Automation:** Rerun the same targeted filter plus the full-suite regression once changefeed prep is ready; attach logs to `tests/TextBuffer.Tests/TestMatrix.md` and the migration log entry for `#delta-2025-11-25-b3-search-offset`.
- **Info-Indexer / DocMaintainer:** Publish the changefeed entry, update Sprint/Task Board references, and backfill `TestMatrix.md` + changefeed indices that previously pointed to the missing porter doc.

## References
- Investigator brief: `agent-team/handoffs/B3-PieceTree-SearchOffset-INV.md`
- Tests: `tests/TextBuffer.Tests/PieceTreeSearchOffsetCacheTests.cs`
- Deterministic assets: `tests/TextBuffer.Tests/Helpers/PieceTreeDeterministicScripts.cs`
- Assertions: `tests/TextBuffer.Tests/Helpers/PieceTreeBufferAssertions.cs`
- Verification command log: terminal run on 2025-11-25 (PIECETREE_DEBUG=0, 5/5, 1.6s).
