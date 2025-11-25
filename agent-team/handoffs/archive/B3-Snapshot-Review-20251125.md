# B3-Snapshot-Review-20251125

**Date:** 2025-11-25
**Planner:** GitHub Copilot (GPT-5.1-Codex)
**Objective:** Audit the staged snapshot/search-offset edits, ensure they mirror the VS Code TS originals, and outline the follow-up workflow (Investigator → Porter → QA).

## Current Findings
- `src/TextBuffer/Core/PieceTreeSnapshot.cs` now streams chunks one piece at a time, appending the BOM only when `_index == 0`. This mirrors `PieceTreeSnapshot` in `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` (lines ~120-190). The implementation clones `PieceSegment`s plus `ChunkBuffer` references and advances `_index` per `Read()` call. We must still confirm `ChunkBuffer.Slice()` matches TS `getPieceContent()` semantics for CR/LF spanning pieces.
- `src/TextBuffer/TextModel.cs` exposes `CreateSnapshot(bool preserveBom = false)` and forwards to `_buffer.InternalModel.CreateSnapshot(bom)`. TS `TextModel.createSnapshot()` wraps the buffer snapshot inside a `TextModelSnapshot` aggregator (see `ts/src/vs/editor/common/model/textModel.ts`, lines ~120-210) so callers read <=64KB chunks. The C# layer currently returns the raw `PieceTreeSnapshot`, so we should port `TextModelSnapshot` to stay faithful and avoid huge chunk allocations.
- Snapshot tests were reworked to use `TextModel` directly:
  - `tests/TextBuffer.Tests/Helpers/SnapshotReader.cs` ports TS `getValueInSnapshot()` with a `MaxChunks` guard.
  - `tests/TextBuffer.Tests/PieceTreeSnapshotTests.cs` now create snapshots via `TextModel.CreateSnapshot()` and drain them with `SnapshotReader`.
  - `tests/TextBuffer.Tests/PieceTreeSnapshotParityTests.cs` ports TS `bug #45564` and `immutable snapshot 1/2/3` (TS lines 1950-2020). These rely on `TextEdit` plumbing plus the new helper.
- Deterministic coverage for the TS “search offset cache” suite (lines 1810-1884) landed in `tests/TextBuffer.Tests/PieceTreeSearchOffsetCacheTests.cs`. Supporting helpers include:
  - `tests/TextBuffer.Tests/Helpers/PieceTreeDeterministicScripts.cs` (adds the four normalized EOL cases + render whitespace script).
  - `tests/TextBuffer.Tests/Helpers/PieceTreeBufferAssertions.cs` (`AssertState`, `AssertLineStarts`, `AssertSearchCachePrimed`). The last one interrogates `PieceTreeModel.TryGetCachedNodeByOffset()` to ensure `_searchCache` gets populated during scripts. Need to confirm cache size/invalidations still match TS expectations under heavy edits.
- `tests/TextBuffer.Tests/TestMatrix.md` records the new suites under delta `#delta-2025-11-25-b3-piecetree-snapshot`. The matrix already cites the targeted commands QA should run.

## Open Questions / Risks
1. **TextModel snapshot wrapper parity:** Without a C# `TextModelSnapshot` equivalent, high-volume snapshot consumers may observe arbitrarily large return buffers, diverging from TS chunking/stream behavior.
2. **Chunk slicing parity:** `PieceTreeSnapshot` now relies on `ChunkBuffer.Slice(piece.Start, piece.End)`. We need confirmation that this matches `PieceTreeBase.getPieceContent()` across CR/LF splits and multi-buffer pieces.
3. **Search cache visibility:** `PieceTreeBufferAssertions.AssertSearchCachePrimed()` assumes `_searchCache` remembers offsets after `NodeAt` traversals. If Porter ever alters cache limits (`PieceTreeSearchCache(limit = 1)`), these tests might start failing silently. Investigator should double-check the cache interface vs. TS `PieceTreeSearchCache`.
4. **Snapshot helper guard:** `SnapshotReader.MaxChunks` is a C#-only safety net. Ensure it does not mask legitimate long snapshots (TS helper has no guard). Document the intended failure mode so QA knows how to triage chunk overflow.

## Proposed Workflow
### Investigator-TS
1. **Diff validation:** Line-by-line compare `PieceTreeSnapshot` (C#) against `ts/.../pieceTreeBase.ts` to confirm `_pieces` capture, `_index` increments, BOM injection, and empty-tree handling match exactly. Spot-check `ChunkBuffer.Slice` vs. TS `getPieceContent` for CRLF-splitting buffers.
2. **TextModel wrapper gap:** Review `ts/src/vs/editor/common/model/textModel.ts` (`TextModelSnapshot` + `createSnapshot`) and spell out what is missing in C#. Deliver a design note covering chunk aggregation thresholds, EOS behavior, and whether BOM preservation should travel via the wrapper.
3. **Helper/test parity:** Map each new C# snapshot test back to the TS test case (bug #45564, immutable snapshot 1-3) and ensure `SnapshotReader` semantics (single-pass drain, null termination) match `getValueInSnapshot`.
4. **Search cache scripts:** Validate that `PieceTreeSearchOffsetCacheTests` scripts match the TS sequence order (lines 1810-1884) and that `AssertSearchCachePrimed` probes offsets equivalent to TS `nodePos` checks.

### Porter-CS
1. **Implement TextModelSnapshot:** Port the TS `TextModelSnapshot` aggregator into `src/TextBuffer/TextModelSnapshot.cs` (or nested type) and have `TextModel.CreateSnapshot()` return it. Ensure the wrapper batches underlying `PieceTreeSnapshot.Read()` calls up to ~64KB, just like TS.
2. **Finalize PieceTreeSnapshot parity:** If Investigator finds gaps (e.g., BOM handling for empty trees or multi-buffer slice issues), finalize fixes inside `PieceTreeSnapshot.cs` and update any dependent helpers (`ChunkBuffer`, `PieceTreeModel.EnumeratePiecesInOrder`).
3. **Solidify search cache assertions:** If cache behavior differs, expose any missing instrumentation (e.g., `PieceTreeModel.RememberNodePosition`) or align invalidation logic so `AssertSearchCachePrimed` becomes reliable.
4. **Docs/TestMatrix:** Once code is updated, refresh `tests/TextBuffer.Tests/TestMatrix.md` and `docs/reports/migration-log.md` with the final delta ID and note the new `TextModelSnapshot` coverage.

### QA-Automation
1. Targeted suites to rerun after Porter’s drop:
   - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotTests --nologo`
   - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotParityTests --nologo`
   - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo`
   - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeDeterministicTests --nologo` (ensures the broader deterministic harness remains stable).
2. Full regression: `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` once targeted suites pass.
3. File updated evidence under `tests/TextBuffer.Tests/TestMatrix.md` plus a QA handoff referencing the new delta ID.

## Cross-File Dependencies / Watch Items
- `PieceTreeSnapshot` depends on `PieceTreeModel.EnumeratePiecesInOrder()` and `ChunkBuffer.Slice`; any refactor in those layers can silently corrupt snapshots.
- `TextModel.CreateSnapshot()` pulls the BOM via `PieceTreeBuffer.GetBom()`. Porter must ensure `PieceTreeBuffer` continues to expose BOM and internal model handles; otherwise the new method breaks.
- `PieceTreeSearchOffsetCacheTests` exercise `PieceTreeFuzzHarness`, `PieceTreeBufferAssertions`, `PieceTreeDeterministicScripts`, and the internal `_searchCache`. Changes in cache invalidation or harness logging will ripple through all three helpers.
- The new `SnapshotReader` helper is reused wherever we need to drain snapshots. If its chunk guard triggers, we expect an `InvalidOperationException`; QA should surface that as a regression, not silently swallow it.

Deliverable owners are now queued per the workflow above. Let me know if we should fast-track any of the Investigator tasks via runSubAgent.

## Run Plan – 2025-11-25
*Reminder: Investigator-TS, Porter-CS, QA-Automation, and DocMaintainer must each update their personal memory files before reporting back on this run.*

### Investigator-TS
- **Diff sweep:** `git diff --staged src/TextBuffer/Core/PieceTreeSnapshot.cs src/TextBuffer/TextModel.cs src/TextBuffer/TextModelSnapshot.cs tests/TextBuffer.Tests/Helpers/PieceTreeBufferAssertions.cs tests/TextBuffer.Tests/Helpers/PieceTreeDeterministicScripts.cs tests/TextBuffer.Tests/PieceTreeSearchOffsetCacheTests.cs tests/TextBuffer.Tests/PieceTreeSnapshotParityTests.cs tests/TextBuffer.Tests/TextModelSnapshotTests.cs` and annotate any deviations from `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` + `textModel.ts`. Capture notes in `agent-team/handoffs/B3-Snapshot-INV-20251125.md` referencing `#delta-2025-11-25-b3-piecetree-snapshot` / `#delta-2025-11-25-b3-search-offset`.
- **TS parity verification:** Use `rg -n "TextModelSnapshot" ts/src/vs/editor/common/model/textModel.ts` and `rg -n "search offset" ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` to confirm chunk batching (≤64KB/window) and deterministic scripts match the staged C# helpers. Flag any differences in chunk aggregation or cache invalidation paths.
- **Helper audit:** Re-run the staged `tests/TextBuffer.Tests/Helpers/SnapshotReader.cs` logic against the TS `getValueInSnapshot()` to verify `MaxChunks` guard won’t fire under legitimate workloads; document the rationale so QA knows the expected behavior if it ever trips.
- **Output:** Update the investigator memo with the verified checklist, including explicit go/no-go criteria for Porter (e.g., chunk batching threshold, cache probe expectations) and cross-link to `agent-team/task-board.md` items.

### Porter-CS
- **Finalize `TextModelSnapshot`:** Ensure `src/TextBuffer/TextModelSnapshot.cs` batches underlying `PieceTreeSnapshot.Read()` calls using the TS loop (`while (aggregate.Length < 65536 && (piece = snapshot.Read()) != null)`). Wire it through `TextModel.CreateSnapshot()` and `PieceTreeBuffer.CreateSnapshot()` so callers never receive raw per-piece chunks.
- **Snapshot/Search cache polish:** Reconcile any Investigator findings inside `PieceTreeSnapshot.cs`, `PieceTreeBufferAssertions.cs`, and `PieceTreeDeterministicScripts.cs`. Confirm `_searchCache.InvalidateRange` mirrors TS semantics by instrumenting `PieceTreeModel.Edit.cs` if needed.
- **Self-check commands:**
  - `dotnet format src/TextBuffer/TextModelSnapshot.cs src/TextBuffer/TextModel.cs --folder` (if stylistic fixes emerge).
  - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~TextModelSnapshotTests --nologo` before handing off to QA.
- **Artifacts:** Update `agent-team/task-board.md` status and prepare a Porter handoff referencing both changefeeds (`#delta-2025-11-25-b3-piecetree-snapshot`, `#delta-2025-11-25-b3-search-offset`).

### QA-Automation
- **Targeted reruns (post-Porter):**
  - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeSnapshotTests --nologo`
  - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeSnapshotParityTests --nologo`
  - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeSearchOffsetCacheTests --nologo`
  - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~TextModelSnapshotTests --nologo`
  - Follow with full regression: `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`
- **Evidence logging:** Record pass/fail data in `tests/TextBuffer.Tests/TestMatrix.md` under the two changefeed anchors. If any suite fails, capture console output plus offending seed in `docs/reports/migration-log.md` before filing bugs.
- **Handoff:** Produce `agent-team/handoffs/B3-Snapshot-QA-20251125.md` (or append to existing QA memo) summarizing results and pointing DocMaintainer to the relevant command lines.

### DocMaintainer
- **Documentation sweep:** Once QA signs off, update `docs/reports/migration-log.md`, `tests/TextBuffer.Tests/TestMatrix.md`, and `docs/plans/ts-test-alignment.md` to reflect the snapshot/search-offset completion with pointers to `agent-team/indexes/README.md#delta-2025-11-25-b3-piecetree-snapshot` and `#delta-2025-11-25-b3-search-offset`.
- **Changefeed publication:** Coordinate with Info-Indexer (if needed) to append the final delta entries. Mention in the log which targeted commands were executed.
- **Cross-file hygiene:** Ensure `agent-team/task-board.md`, `docs/sprints/sprint-03.md`, and any open meeting notes reference the finalized plan. Spell out the requirement that every subagent updated their memory docs.
- **Close-out memo:** Produce a short summary in `agent-team/members/doc-maintainer.md` worklog citing this plan and the twin changefeeds so future runs know where to look.
