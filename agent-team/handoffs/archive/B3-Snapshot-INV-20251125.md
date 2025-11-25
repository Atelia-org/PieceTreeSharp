# B3-Snapshot-INV-20251125

**Date:** 2025-11-25  
**Investigator:** GitHub Copilot (GPT-5.1-Codex)  
**Delta tag:** `#delta-2025-11-25-b3-piecetree-snapshot`

## Scope
- Compare `src/TextBuffer/Core/PieceTreeSnapshot.cs` + `src/TextBuffer/TextModel.cs` against their TS sources:
  - `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts#L160-L210` (`PieceTreeSnapshot` + `getPieceContent`)
  - `ts/src/vs/editor/common/model/textModel.ts#L72-L180` (`TextModelSnapshot`, `TextModel.createSnapshot`)
- Verify the newly-landed helpers/tests:
  - `tests/TextBuffer.Tests/Helpers/SnapshotReader.cs`
  - `tests/TextBuffer.Tests/PieceTreeSnapshotParityTests.cs`
  - `tests/TextBuffer.Tests/Helpers/PieceTreeDeterministicScripts.cs`
  - `tests/TextBuffer.Tests/PieceTreeSearchOffsetCacheTests.cs`
  - `tests/TextBuffer.Tests/Helpers/PieceTreeBufferAssertions.cs`
  - TS baselines: `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts#L1810-L2044`
- Call out any residual drift for chunk slicing, BOM injection, and search cache semantics.

## Findings

### 1. `PieceTreeSnapshot` streaming matches TS, including chunk slicing & BOM
- C# snapshot (`src/TextBuffer/Core/PieceTreeSnapshot.cs#L12-L48`) snapshots the in-order piece list plus immutable `ChunkBuffer` references, just like TS `PieceTreeSnapshot` stores `_pieces` and reuses `_tree` (`pieceTreeBase.ts#L160-L207`).
- Every `Read()` call slices the stored chunk via `ChunkBuffer.Slice(piece.Start, piece.End)` (`ChunkBuffer.cs#L55-L83`). That is the same math as TS `getPieceContent()` (`pieceTreeBase.ts#L1794-L1802`, which offsets into the backing buffer using line-start tables). Slice guards for empty spans and reuses the precomputed `LineStartTable`, so CR/LF splits are handled identically.
- BOM handling is correct: if the tree is empty, we still emit `_bom` once; otherwise the first non-null chunk is `_bom + slice`, mirroring TS `read()` logic at `pieceTreeBase.ts#L182-L199`.
- Snapshot immutability holds because `PieceSegment` is an immutable `record` and we copy the buffer list up front (`new List<ChunkBuffer>(model.Buffers)`), so later edits cannot mutate captured data. TS relies on the same invariant (“TreeNode.piece immutable after capture”).

### 2. `TextModel.CreateSnapshot` skips the TS `TextModelSnapshot` wrapper
- TS funnels every call through `new TextModelSnapshot(this._buffer.createSnapshot(preserveBOM))` (`textModel.ts#L72-L115`), which batches multiple `read()` calls until ≈64KB before returning a chunk. This keeps snapshot consumers from dealing with thousands of tiny piece-sized strings and matches VS Code’s throttling expectations.
- C# currently returns the raw `PieceTreeSnapshot` (`src/TextBuffer/TextModel.cs#L195-L200`). Functionally it works, but callers will now see one chunk per piece and no upper bound on chunk count. For large models (many small pieces) this is a measurable perf+allocation regression versus TS and diverges from the documented contract of `ITextSnapshot` in VS Code.
- Because `_buffer.InternalModel.CreateSnapshot(bom)` bypasses the `TextModelSnapshot` stage entirely, features like `createTextBufferFactoryFromSnapshot` (TS `textModel.ts#createTextBufferFactoryFromSnapshot`) will observe different chunking behavior in C#. This is the primary parity gap uncovered in this review.

### 3. Snapshot helper/tests align with TS suites (with one optional guard)
- `SnapshotReader.ReadAll` (`tests/.../Helpers/SnapshotReader.cs`) is a direct port of TS `getValueInSnapshot()` (`pieceTreeTextBuffer.test.ts#L1898-L1909`) plus a defensive `MaxChunks` safeguard (1M iterations) to prevent runaway loops. The guard never trips for real snapshots; it simply surfaces infinite-loop bugs earlier.
- `PieceTreeSnapshotParityTests` (`tests/.../PieceTreeSnapshotParityTests.cs`) reproduce the TS cases “bug #45564” and “immutable snapshot 1/2/3” (`pieceTreeTextBuffer.test.ts#L1915-L2044`). They now route through `TextModel.CreateSnapshot` and `SnapshotReader`, so parity coverage matches the TS intent (immutability across edits, snapshot divergence after later mutations).

### 4. Search-offset cache scripts/tests cover TS scenarios but cache assertion is superficial
- The deterministic scripts in `PieceTreeDeterministicScripts` (`SearchOffsetRenderWhitespace*`, `SearchOffsetNormalizedEolCase{1-4}`) mirror the sequences from `pieceTreeTextBuffer.test.ts#L1810-L1884` exactly (same offsets and payloads). Each `PieceTreeSearchOffsetCacheTests` case replays the TS flow inside `PieceTreeFuzzHarness`, then runs the same invariants TS checks via `PieceTreeBufferAssertions.AssertState` (text parity + `PieceTreeModel.AssertPieceIntegrity()`) and `AssertLineStarts` (equivalent to TS `testLineStarts`).
- `AssertSearchCachePrimed` confirms that the `PieceTreeModel.NodeAt()` walk stores cache entries and that `TryGetCachedNodeByOffset` round-trips to the same node. However, because the helper primes the cache via `NodeAt` in the same loop, it cannot detect scenarios where edits failed to invalidate stale entries—the real protection still comes from the line-start/content assertions, just as in TS. If we need explicit cache-state coverage later, we should expose counters (e.g., cache entry count) before calling `NodeAt`.

- The underlying cache implementation (`PieceTreeSearchCache.cs`) matches TS (`pieceTreeBase.ts#L205-L263`): limit=1, MRU eviction, offset and line-based lookups, plus targeted invalidation calls inside `PieceTreeModel.Edit` (`PieceTreeModel.Edit.cs#L23`, `#L272`, `#L289`, etc.).

## Gaps & Recommendations

| ID | Gap | Evidence | Recommendation |
| --- | --- | --- | --- |
| **F1** | Missing `TextModelSnapshot` wrapper | TS: `textModel.ts#L72-L115`; C#: `TextModel.cs#L195-L200` | Port `TextModelSnapshot` verbatim (private class in `TextModel.cs` or standalone file). Have it aggregate up to 64KB per `Read()`, exactly like TS, and wrap `_buffer.InternalModel.CreateSnapshot(bom)` before exposing the snapshot to callers. |
| **F2** | No regression coverage for the new wrapper | N/A (new code) | After adding `TextModelSnapshot`, add a couple of targeted tests (e.g., `TextModelSnapshotTests`) that feed a model with many tiny pieces and assert that `Read()` returns ≤N chunks and that concatenated output equals the model value. |
| **F3** | `AssertSearchCachePrimed` cannot catch stale-cache bugs | Helper opportunistically primes the cache right before checking it | (Optional) Extend the helper to take an `expectPrime` flag: first check `TryGetCachedNodeByOffset` **before** calling `NodeAt` (should be false), then call `NodeAt` to ensure the cache gets populated (should be true). Alternatively, expose `PieceTreeModel.SearchCacheEntryCount` so we can assert that editing scripts populate at least one entry via normal operations. |

## QA / Next Steps
1. Porter-CS: Implement **F1** (`TextModelSnapshot`) + optional helper improvements, then update `TextModel.CreateSnapshot` to return the wrapper.
2. QA-Automation: Re-run `PieceTreeSnapshotTests`, `PieceTreeSnapshotParityTests`, and `PieceTreeSearchOffsetCacheTests` plus a targeted `TextModelSnapshotTests` suite once added:
   ```bash
   export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotTests --nologo
   export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotParityTests --nologo
   export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo
   ```
3. DocMaintainer: When Porter lands the wrapper, update `tests/TextBuffer.Tests/TestMatrix.md` and `docs/reports/migration-log.md` under the same delta tag.

## Investigator Follow-up – 2025-11-25

**Confirmed parity – `#delta-2025-11-25-b3-piecetree-snapshot` / `#delta-2025-11-25-b3-search-offset`**
- `src/TextBuffer/Core/PieceTreeSnapshot.cs` now matches `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts#L160-L210`: streaming `Read()` advances one stored `PieceSegment` per call, prefixes the BOM only when `_index == 0`, and uses `ChunkBuffer.Slice` for CR/LF-aware slicing, so chunk consumers observe the same sequence TS returns.
- `TextModelSnapshot` (`src/TextBuffer/TextModelSnapshot.cs`) plus the new `TextModel.CreateSnapshot()` wrapper (`src/TextBuffer/TextModel.cs`) faithfully port `textModel.ts#L72-L115/#L790-L804`: the wrapper batches ~64 KB per `Read()`, skips zero-length chunks, and honors `preserveBom` by threading `_buffer.GetBom()` into the inner `PieceTreeSnapshot`.
- Snapshot helpers/tests mirror the TS harness: `tests/TextBuffer.Tests/Helpers/SnapshotReader.cs` drains snapshots exactly like TS `getValueInSnapshot`, while `tests/TextBuffer.Tests/PieceTreeSnapshotParityTests.cs` and `tests/TextBuffer.Tests/TextModelSnapshotTests.cs` replay “bug #45564” plus immutable snapshot cases (`ts/.../pieceTreeTextBuffer.test.ts#L1898-L2044`). The only intentional divergence is the 1 000 000-iteration guard in `SnapshotReader`, which simply surfaces infinite-loop bugs sooner.
- Search-offset coverage: `tests/TextBuffer.Tests/Helpers/PieceTreeDeterministicScripts.cs` (`SearchOffsetRenderWhitespace*`, `SearchOffsetNormalizedEolCase{1-4}`) and `tests/TextBuffer.Tests/PieceTreeSearchOffsetCacheTests.cs` port the TS suite from `pieceTreeTextBuffer.test.ts#L1810-L1884`, reuse `PieceTreeBufferAssertions.AssertState/AssertLineStarts`, and ensure `_searchCache` is exercised through `AssertSearchCachePrimed`.

**Outstanding deltas**
- `AssertSearchCachePrimed` currently primes the cache via `model.NodeAt()` inside the same loop (`tests/TextBuffer.Tests/Helpers/PieceTreeBufferAssertions.cs`). Because our `PieceTreeBuffer.GetPositionAt/GetOffsetAt` implementations still rely on the cached snapshot string (`src/TextBuffer/PieceTreeBuffer.cs`), the deterministic scripts never hit `_searchCache` the way TS `testLineStarts/testLinesContent` do. We still need a follow-up that either routes the buffer APIs through `PieceTreeModel` (so edits exercise the cache) or extends the assertion helper to probe cache state **before** calling `NodeAt()` to catch stale entries (e.g., expose a `SearchCacheEntryCount`/`TryPeek` API and assert it stays valid across scripts).
- `TextModelSnapshotTests` validate the 64 KB batching with a `FakeSnapshot`, but we lack an integration test proving that `TextModel.CreateSnapshot(preserveBom: true)` yields a single BOM-prefixed chunk just like TS. Adding a regression that seeds a model with a UTF-8 BOM and asserts the wrapper drains it once would close the remaining coverage gap around chunk aggregation.

**Guidance for Porter-CS**
1. For `#delta-2025-11-25-b3-search-offset`, decide whether to (a) port `PieceTreeBuffer.GetPositionAt/GetOffsetAt` to call into `PieceTreeModel` (preferred, file `src/TextBuffer/PieceTreeBuffer.cs` around the existing snapshot-based helpers) or (b) extend `PieceTreeBufferAssertions.AssertSearchCachePrimed` to inspect cache state before invoking `NodeAt`. Either path needs line-level notes in `tests/TextBuffer.Tests/Helpers/PieceTreeBufferAssertions.cs` so future investigators can see how we exercise `_searchCache` parity.
2. For `#delta-2025-11-25-b3-piecetree-snapshot`, add a targeted test in `tests/TextBuffer.Tests/TextModelSnapshotTests.cs` that builds a `TextModel` containing a BOM and calls `CreateSnapshot(preserveBom: true)`, asserting the first chunk starts with the BOM and that subsequent `Read()` calls no longer return it. This mirrors `textModel.ts#createSnapshot` expectations and guards against regressions in the new wrapper.

**Guidance for QA**
- Keep the existing targeted commands (snapshot/search-offset suites) as the verification gate for both changefeeds:
  ```bash
  export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotTests --nologo
  export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotParityTests --nologo
  export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo
  export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSnapshotTests --nologo
  ```
- If `SnapshotReader` ever trips the `MaxChunks` guard, treat it as a regression (the TS helper has no such guard), capture the offending snapshot/test name, and attach it to the changefeed report so Porter can triage the infinite-loop root cause.
