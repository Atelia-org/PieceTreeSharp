# B3 – PieceTree Deterministic Backlog (2025-11-25)

CRLF + centralized line-start deterministics are now green (per `B3-PieceTree-Deterministic-CRLF-QA.md`). Remaining parity gaps live in the same TS file (`ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`). This note inventories the unported suites, proposed C# targets, required helpers/dependencies, and the order we should land them so Porter/QA can plan the next batches.

## Priority order
1. **Snapshot deterministics** – Blocks snapshot consumers (diff/search) and regresses easily.
2. **Search offset cache scripts** – Exercises `PieceTreeSearchCache` invalidation and normalized-EOL insert paths.
3. **Chunk-based search deterministics** – Verifies `FindMatchesLineByLine` never escapes node boundaries.
4. **Random (unsupervised) scripts** – Validates builder splitting + long-running random edits.
5. **Buffer API deterministics** – Needs new API surface (`Equal`, `GetNearestChunk`) before tests can land.

## Outstanding suites

### 1. Snapshot deterministics (TS lines 1888–1998)
- **TS tests**: `bug #45564, piece tree pieces should be immutable`, `immutable snapshot 1`, `immutable snapshot 2`, `immutable snapshot 3`.
- **Current coverage**: `PieceTreeSnapshotTests.cs` only exercises read + simple immutability; it does not mirror the multi-snapshot/edit interleaving in TS.
- **Proposed C#**: new `PieceTreeSnapshotParityTests` with methods:
  - `Bug45564_PieceTreePiecesRemainImmutable()`
  - `ImmutableSnapshot_RangeDeletionRoundTrip()`
  - `ImmutableSnapshot_InsertAfterDeletion()`
  - `ImmutableSnapshot_DetectsSubsequentMutations()`
  Each test should build a `TextModel`, apply the exact TS `Range` edits, call `CreateSnapshot`, and use a `SnapshotReader.ReadToEnd(ITextSnapshot)` helper to assert parity.
- **Helpers/Deps**: `TextModel`, `Range`, the existing `TextPosition` utilities, and a new test helper to drain an `ITextSnapshot` (TS `getValueInSnapshot`). No new production dependencies required.
- **Priority rationale**: P1. These scenarios previously regressed (`bug #45564`). Missing parity leaves `FindModel`/diff snapshots exposed when Porter resumes snapshot plumbing.
- **QA hooks**: `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotParityTests`; update `tests/TextBuffer.Tests/TestMatrix.md` (Snapshot row) and log `#delta-2025-11-25-b3-piecetree-snapshot` in `docs/reports/migration-log.md` + `agent-team/task-board.md`.

### 2. Search offset cache (TS lines 1809–1884)
- **TS tests**: `render white space exception` plus `Line breaks replacement is not necessary when EOL is normalized` cases 1–4 (only case 1 is partially covered today).
- **Current coverage**: `PieceTreeNormalizationTests.Line_Breaks_Replacement_Is_Not_Necessary_When_EOL_Is_Normalized` maps to the first TS case; the render-whitespace script and the remaining three normalized-EOL variants are missing entirely.
- **Proposed C#**: add `PieceTreeSearchOffsetCacheTests`:
  - `RenderWhitespaceScriptPreservesSearchCache()` – replay the full insert/delete sequence from TS lines ~1810–1842 using `PieceTreeFuzzHarness` or direct `PieceTreeBuffer`, then assert `GetLinesRawContent()` and `PieceTreeBufferAssertions.AssertState`.
  - `NormalizedInsert_AppendsWithoutExtraBreaks()` (TS case 2)
  - `NormalizedInsert_AfterTrailingLf()` (case 3)
  - `NormalizedInsert_WithinLine()` (case 4)
  - `NormalizedInsert_BeforeTrailingLf()` (case 5)
  These can live beside the existing normalization tests and should assert both `testLineStarts`/`testLinesContent` equivalence via `PieceTreeBufferAssertions`.
- **Helpers/Deps**: `PieceTreeFuzzHarness`, `PieceTreeBufferAssertions`, `PieceTreeScript` helpers; no new product work.
- **Priority rationale**: P1. Search offset cache is hot-path infra for editor search; bugs surface as incorrect highlights/insert positions.
- **QA hooks**: `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests`; add entries in `TestMatrix` under the PieceTree deterministic section; log changefeed `#delta-2025-11-25-b3-search-offset-cache` and call out in `docs/plans/ts-test-alignment.md`.

### 3. Chunk-based search deterministics (TS lines 1998–2055)
- **TS tests**: `#45892 buffer empty search`, `#45770 FindInNode should not cross node boundary`, `search searching from the middle`.
- **Current coverage**: `TextModelSearchTests` exercise higher-level search APIs but nothing hits `PieceTreeModel.FindMatchesLineByLine` directly; no guardrails on per-node traversal.
- **Proposed C#**: create `PieceTreeChunkSearchTests` mirroring the TS scripts. Use `PieceTreeBuffer` + `SearchData` + `WordCharacterClassifier` to call `FindMatchesLineByLine` and assert returned `Range`s match TS expectations after each edit.
- **Helpers/Deps**: existing `PieceTreeBufferAssertions`, `PieceTreeScript`, `WordCharacterClassifier`. No new product work.
- **Priority rationale**: P2, immediately after cache tests—prevents regressions in Porter’s ongoing `PieceTreeModel.Search` work.
- **QA hooks**: `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeChunkSearchTests`; update `TestMatrix` search section; add changefeed `#delta-2025-11-25-b3-chunk-search`.

### 4. Random (unsupervised) scripts (TS lines 1535–1665)
- **TS tests**: `splitting large change buffer` and `random insert delete`. (`random chunks` + `random chunks 2` already ported via `PieceTreeFuzzHarnessTests`).
- **Current coverage**: `PieceTreeModelTests.ChangeBufferFuzzTests` is similar but not the deterministic TS script; no test replays the exact edit log for the first case or the 1,000-iteration RNG loop for the second.
- **Proposed C#**:
  - Extend `PieceTreeFuzzHarnessTests` with `SplittingLargeChangeBufferMatchesTsScript()` using `PieceTreeScript` steps extracted from TS (requires capturing the TS log once, then pasting into `PieceTreeDeterministicScripts`).
  - Add `RandomInsertDeleteMatchesRecordedScript()` that replays the exact 1,000-iteration sequence (capture from TS run or re-create TS RNG via a `TsRandom` helper seeded with the same values used when the suite last stabilized).
  - Both tests should continue calling `harness.AssertState()` plus `PieceTreeBufferAssertions.AssertLineStarts/LineContent` after the loop (matching TS `testLineStarts/testLinesContent`).
- **Helpers/Deps**: `PieceTreeFuzzHarness`, `PieceTreeScript`, new deterministic log assets generated from TS; no product work.
- **Priority rationale**: P3 – lower than search suites but still required before declaring deterministic coverage complete.
- **QA hooks**: `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeFuzzHarnessTests.SplittingLargeChangeBufferMatchesTsScript`, same for `.RandomInsertDeleteMatchesRecordedScript`; update `TestMatrix` fuzz row (`R25`) and log `#delta-2025-11-25-b3-random-unsupervised`.

### 5. Buffer API deterministics (TS lines 1725–1800)
- **TS tests**: `equal`, `equal with more chunks`, `equal 2/3`, `getLineCharCode - issue #45735`, `getLineCharCode - issue #47733`, `getNearestChunk`.
- **Current coverage**:
  - No `PieceTreeBuffer.Equal` or `PieceTreeModel.getNearestChunk` API exists yet, so the equality + nearest-chunk tests cannot be written.
  - `LineCharCode` is only partially covered via `PieceTreeBufferTests.LineCharCodeFollowsCrlfBoundaries`; the specific TS repros (uppercase `LINE1` sample and leading-empty chunk case) are missing.
- **Proposed C#**:
  - Porter needs to expose `bool PieceTreeBuffer.Equal(PieceTreeBuffer other)` (or equivalent static helper) plus `string PieceTreeModel.GetNearestChunk(int offset)` before tests can be added.
  - Once APIs exist, add `PieceTreeBufferApiParityTests` with methods mirroring each TS case and reuse `PieceTreeBufferAssertions` to validate content.
  - Also port both `getLineCharCode` issues verbatim (use `PieceTreeBuffer.FromChunks` to match TS chunk layout).
- **Helpers/Deps**: new production APIs (`Equal`, `GetNearestChunk`) + `PieceTreeBufferAssertions`.
- **Priority rationale**: P4. Blocked on new APIs, so this batch lands after the higher-priority search/snapshot work. Tracking now prevents the tests from being forgotten once Porter exposes the APIs.
- **QA hooks**: once ready, `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeBufferApiParityTests`; update `TestMatrix` buffer API row; log `#delta-2025-11-25-b3-buffer-api`.

## Shared QA + documentation steps when each batch lands
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTree` to re-run the full deterministic bucket.
- Update `tests/TextBuffer.Tests/TestMatrix.md` with the new test IDs and TS references.
- Append changefeeds to `docs/reports/migration-log.md` (`#delta-2025-11-25-b3-piecetree-det-*` per batch) and mirror them in `agent-team/indexes/README.md` if Info-Indexer needs hooks.
- Reflect status on `agent-team/task-board.md` and `docs/plans/ts-test-alignment.md#piecetree` once each suite moves from "Planned" → "Green".
