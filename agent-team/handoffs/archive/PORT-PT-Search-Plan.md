# PORT-PT-Search-Plan – 2025-11-26

## Implementation Strategy
### 1. `GetAccumulatedValue` + cached tuple reuse
- Files: `src/TextBuffer/Core/PieceTreeModel.Search.cs`, `src/TextBuffer/Core/PieceTreeSearchCache.cs`.
- Replace the ad-hoc char scanning inside `GetAccumulatedValue` with the TS parity math that relies on the owning buffer's `LineStarts`. Compute offsets via the piece's `start` coordinates and the already materialized line-start table instead of looping over the string payload, and include CR/LF correction by reading the next line-start entry when the piece spans multiple physical lines.
- Extend the cache entry structure (currently just `node`) to stash `(node, nodeStartOffset, nodeStartLine)` so `NodeAt2`, `GetLineRawContent`, and `GetOffsetAt` can reuse the single tree walk; expose helpers to materialize the tuple once and pass it to callers.
- Update `NodeAt2` to request the cached tuple, short-circuit when the cached node already covers the query offset, otherwise perform a single left/right traversal and refresh the cache. `GetLineRawContent` and `GetOffsetAt` should then consume the cached `(node, startLine, startOffset)` rather than repeating the traversal.
- Guard against cross-piece CR/LF splits by making `GetAccumulatedValue` aware of the previous piece's terminal `\r` and the next piece's initial `\n`; we can reuse the TS logic that inspects the neighbor pieces before finalizing the line delta and offset remainder.

### 2. `_lastChangeBufferPos` + `AppendToChangeBufferNode`
- File: `src/TextBuffer/Core/PieceTreeModel.Edit.cs` (`AppendToChangeBufferNode`).
- Port the TS `hitCRLF` detection: if the node we append to currently ends with `\r` and the incoming text starts with `\n`, remove the trailing entry from `ChunkBuffer.LineStarts`, decrement `_lastChangeBufferPos.column`, and persist whether the bridge consumed a logical newline.
- Only after reconciling the CR/LF bridge should we call `ChunkBuffer.Append(value)`. Once append completes, re-run `AdjustCarriageReturnFromNext` against the updated buffer and ensure `_lastChangeBufferPos` advances using the adjusted cursor from `ChunkBuffer.Append`.
- Emit cache invalidations that cover both the bridged character (offset-1) and the new payload so the search cache never observes the transient double-line-break state.

### 3. CRLF bridge while creating new pieces
- File: `src/TextBuffer/Core/PieceTreeModel.Edit.cs` (`CreateNewPieces`).
- Before we append the freshly carved chunk into buffer 0, detect `(previousChunkEndsWithCR && newChunkStartsWithLF)`. If true, temporarily advance `_lastChangeBufferPos.column` and mark a bridge flag so that the eventual append removes the placeholder CR line-start and keeps the logical pair counted once.
- Mirror the TS sentinel approach: prepend a throwaway character (e.g. `_`) just for the append call, shift the incoming `lineStarts` by the sentinel offset, and trim the sentinel from the actual piece length so observers never see it. This keeps `_buffers[0].LineStarts` monotonic even while we stitch the CR/LF pair.
- After the append, restore `_lastChangeBufferPos` so it reflects the true cursor, re-evaluate `TotalLineFeeds`, and invalidate search cache entries at/after the chunk's start offset.

### 4. Search cache invalidation semantics
- Files: `src/TextBuffer/Core/PieceTreeSearchCache.cs`, `src/TextBuffer/Core/PieceTreeModel.Edit.cs` (insert/delete call sites).
- Align with TS by nuking the cache on any edit. Simplest path: replace `InvalidateRange` calls with `Clear()` inside insert/delete flows; retain the method for API parity but have it delegate to `Clear()` whenever the right boundary is `int.MaxValue` or the range intersects an existing entry.
- If we need to keep range-based invalidation, change the implementation so edits call `InvalidateRange(mutationOffset, int.MaxValue)`; internally this should drop every cached entry whose `nodeStartOffset >= mutationOffset` or whose span intersects the edit, ensuring no stale start offsets survive an insert/delete.
- Add lightweight tracing (behind `DEBUG_SEARCH_CACHE`) to log when a cache entry survives an edit so the fuzz harness can assert that no survivors remain.

## Instrumentation & Tests
- `tests/TextBuffer.Tests/PieceTreeDeterministicTests.cs`: import the TS parity fixture (mixed CRLF document) and assert that `GetOffsetAt`, `GetLineContent`, and `FindMatchesLineByLine` match the TS baseline after deterministic sequences of inserts/deletes. Add helper assertions that track `_lastChangeBufferPos` via exposed debug hooks.
- `tests/TextBuffer.Tests/PieceTreeFuzzHarnessTests.cs`: extend the fuzz harness to randomly split CR/LF pairs across edits and assert that `PieceTreeModel.GetLineCount()` and `_buffers[0].LineStarts` stay monotonic; include instrumentation that records whenever the search cache is invalidated vs reused.
- `tests/TextBuffer.Tests/PieceTreeSearchOffsetCacheTests.cs`: add a regression test that caches a node, performs an insert before it, and confirms the cache either clears or updates the start offset before the next `NodeAt2` call.
- `tests/TextBuffer.Tests/CRLFFuzzTests.cs`: craft a minimal reproduction where `_buffers[0]` ends with `\r` and we append `\n` both via `AppendToChangeBufferNode` and `CreateNewPieces`. Assert that `GetLineContent` shows a single newline and `_lastChangeBufferPos`/`TotalLineFeeds` remain unchanged.
- Instrumentation hooks: introduce `#if DEBUG` counters inside `PieceTreeSearchCache` to track `CacheHit`, `CacheMiss`, `ClearedAfterEdit`, and `EntriesRemaining`. Surface these via `PieceTreeModel.Diagnostics.SearchCacheSnapshot` so deterministic/fuzz tests can assert invariants without reflection.

## Rollout & Timeline
- No feature flag required; behavior restores parity with the TS reference while remaining internal to `PieceTreeModel`. If safety is desired, guard the new CRLF bridge and cache semantics behind a temporary `PieceTreeOptions.EnableSearchParityFix` toggle set to true in production once parity tests pass.
- Estimated timeline (assuming 1 engineer):
  1. **Day 0.5** – Implement `GetAccumulatedValue` refactor + cache tuple reuse, add debug counters.
  2. **Day 0.5** – Port `AppendToChangeBufferNode` and `_lastChangeBufferPos` hitCRLF logic, wire cache invalidation.
  3. **Day 0.5** – Build the CRLF bridge helpers inside `CreateNewPieces`, add sentinel-handling tests.
  4. **Day 0.5** – Rework search cache invalidation + add diagnostics.
  5. **Day 1** – Add deterministic + fuzz + regression tests, run extended suites, and perform final telemetry review.

## TODO Checklist (post-approval)
- [ ] Refactor `GetAccumulatedValue`, expand cache tuple, update `NodeAt2` callers (`PieceTreeModel.Search.cs`).
- [ ] Implement `hitCRLF` handling + `_lastChangeBufferPos` rewind in `AppendToChangeBufferNode` (`PieceTreeModel.Edit.cs`).
- [ ] Add CRLF bridge + sentinel shift logic to `CreateNewPieces` and ensure cache invalidation covers bridged spans.
- [ ] Simplify `PieceTreeSearchCache` invalidation to full clears (or `mutationOffset` → `int.MaxValue`) and add DEBUG diagnostics.
- [ ] Extend deterministic fixtures in `PieceTreeDeterministicTests` + `PieceTreeSearchOffsetCacheTests` with CRLF/cache coverage.
- [ ] Update fuzz suites (`PieceTreeFuzzHarnessTests`, `CRLFFuzzTests`) to assert cache clears and `_lastChangeBufferPos` stability.
- [ ] Document new instrumentation/diagnostics in `docs/reports/alignment-audit/00-summary.md` once code merges.
