# PORT-IntervalTree-Normalize

**Role:** Porter Felix Novak  
**Objective:** Restore the TS lazy-normalized decoration trees in C# so large documents avoid O(n) range rewrites while preserving the richer `DecorationChange` payload we added earlier.

## References
- Audit gap: `docs/reports/alignment-audit/04-decorations.md` (§5 IntervalTree, §4 DecorationsTrees API)
- ALIGN plan: `agent-team/handoffs/ALIGN-20251126-Plan.md` (Workstream 3 milestones & perf expectations)
- Current code: `src/TextBuffer/Decorations/IntervalTree.cs`, `DecorationsTrees.cs`, `TextModel.cs`
- TS reference: `ts/src/vs/editor/common/model/intervalTree.ts`, `textModel.ts` (decorations sections)

## IntervalTree data-structure changes
- **Node layout.** Replace the current `Node(ModelDecoration decoration)` wrapper with TS-style fields: `int start`, `int end`, `int delta`, `int maxEnd`, `NodeFlags flags`, plus cached absolute offsets/version. Keep a back-reference to `ModelDecoration` so we can still produce `DecorationChange`s. `ModelDecoration.Range` becomes a cache that gets refreshed by `resolveState`.
- **NodeFlags bitmask.** Introduce a packed `uint` similar to TS `metadata` to encode color (RB status), visited bit, validation bit, glyph-margin bit, affects-font bit, stickiness (2 bits) and `collapseOnReplace`. Populate flags from `ModelDecorationOptions` when inserting or when options change so `Search/IntervalSearch` can evaluate `filterOutValidation`, `filterFontDecorations`, `onlyMarginDecorations` without allocating.
- **Lazy deltas.** Mirror TS `delta` semantics: every node stores local offsets, parents store the accumulated delta for the right subtree. `noOverlapReplace` updates `node.delta` and `node.start/end` without touching the decoration; once `delta` drifts beyond ±2^30 we flip a `_normalizePending` flag via `requestNormalize()`.
- **Normalization.** Implement `_NormalizeDeltaIfNeeded()` (same in insert/delete/acceptReplace paths) that performs the in-order traversal, applies accumulated `delta` to `start/end`, resets `delta` to 0, and clears visited bits. The traversal should reuse the TS pseudocode so we avoid stack allocations.
- **resolveState cache.** Add `ResolveState(Node node, int versionId)` that walks up the tree, sums deltas, writes the absolute start/end into the node cache, and copies them into `ModelDecoration.Range` (together with `DecorationChange`’s `OldRange`). `DecorationsTrees.Search/Enumerate` must call `ResolveState` before returning decorations to callers.

## API adjustments (`requestNormalize`, `acceptReplace`, `resolveState`)
- **`IntervalTree.RequestNormalize()`** – public (or internal) method that marks `_normalizePending = true`; called when `node.delta` exceeds bounds or when external callers (e.g., `TextModel.SetEol`) need canonical offsets before operating on ranges. All mutating APIs (`Insert`, `Delete`, `AcceptReplace`) finish by calling `_NormalizeDeltaIfNeeded`.
- **`IntervalTree.AcceptReplace(...)`.** Port the TS four-phase algorithm:
  1. `nodesOfInterest = SearchForEditing(offset, offset+removedLength)` that returns nodes intersecting the edit window and resolves them immediately so we can capture `OldRange`.
  2. Remove those nodes from the RB tree (preserving them in a list for reinsert) and normalize if needed.
  3. Run `NoOverlapReplace` to lazily shift the untouched nodes via delta arithmetic (set `requestNormalize` if delta drifts).
  4. For each node of interest, reset its `start/end` from cached absolute offsets, feed them through `DecorationRangeUpdater.ApplyEdit(...)` (so we reuse the proven stickiness logic), refresh flags/metadata, update `ModelDecoration.Range`, and reinsert. Collect a `DecorationChange` for each mutated decoration.
- **`IntervalTree.ResolveState(...)`.** Provide a method that (a) ensures normalization if pending, (b) resolves absolute offsets for a node, and (c) optionally expands to full `TextRange`. `DecorationsTrees` uses this to expose two helpers: `ResolveAll(TextModel host)` for EOL changes and `ResolveNodeRange(host, node)` for search results.
- **`DecorationsTrees.AcceptReplace`.** Wrap the per-scope trees, aggregate the `DecorationChange` lists from every call, and return a single ordered list back to `TextModel`. This keeps `TextModel` ignorant of tree internals.

## TextModel integration
- **Edit flow.** Replace `AdjustDecorationsForEdit` with `_decorationTrees.AcceptReplace(...)`. `ApplyPendingEdits` (and undo/redo) will:
  1. Ask `_decorationTrees.AcceptReplace(edit.OldStartOffset, removedLength, insertedLength, forceMoveMarkers)` for `DecorationChange`s before mutating the buffer.
  2. Append the returned changes to the per-edit list so the existing `RaiseDecorationsChanged` logic remains untouched.
- **Query APIs.** Update `GetDecorationsInRange`, `GetLineDecorations`, `GetAllDecorations`, etc., to pass the TS filter toggles through `DecorationsTrees.Search` wrappers. Ensure callers who already default to `DecorationOwnerIds.Any` keep getting all owners by mapping `Default (0)` to Any inside `TextModel` (per audit).
- **Injected text / line height.** `DecorationsTrees` already maintains three scopes; after we start deferring ranges, we must call `ResolveState` before building `LineHeightChange` or injected-text payloads. Hook this into `_decorationsTree.GetNodeRange` analogue like TS `IDecorationsTreesHost` that asks `TextModel` for `GetVersionId()` and `GetRangeAt()`.
- **EOL changes & serialization.** `SetEolInternal` currently rebuilds `_decorationTrees`; with lazy ranges we instead call `_decorationsTree.ResolveAll(this)` before rewriting the buffer so nodes hold absolute offsets, mirroring TS `_onBeforeEOLChange`/`_onAfterEOLChange` hooks.

## Testing & perf validation
- **Existing coverage.** Re-run `DecorationTests`, `DecorationStickinessTests`, `DocUI/DocUIFindDecorationsTests`, `DocUIFindModelTests` to ensure owner filters, stickiness overrides, and DocUI throttling stay green. Add targeted tests for `filterOutValidation`, `filterFontDecorations`, `onlyMarginDecorations`, and default-owner semantics.
- **New unit tests.** Add `IntervalTreeTests` validating `AcceptReplace` scenarios (insert, delete, replace, forceMove) and verifying `DecorationChange` payloads (old/new ranges, owner IDs). Include a regression that used to be O(n) by creating 10k decorations and editing at the top – assert the number of touched nodes equals the handful inside the edit window.
- **Perf harness.** Implement the DocUI perf-smoke harness described in ALIGN plan (under `tests/TextBuffer.Tests/DocUI`). Populate 50k decorations spanning 1MB of text, run batched edits, and assert runtime stays under target plus capture allocation counts. Feed telemetry into the audit addendum once numbers match TS traces.
- **Instrumentation.** Temporarily expose counters (nodes removed, `requestNormalize` hits, normalization duration) behind `#if DEBUG` so QA can verify normalization triggers match TS logs during perf runs; strip or guard them before release.

## TODO checklist
- [ ] Refactor `IntervalTree.Node` to TS-style fields, introduce `NodeFlags`, and port normalize/search helpers.
- [ ] Implement `RequestNormalize`, `AcceptReplace`, `ResolveState`, and ensure they emit `DecorationChange`s compatible with current event plumbing.
- [ ] Expand `DecorationsTrees` + `TextModel` query APIs to accept TS filter toggles and ensure owner `0` behaves like `Any`.
- [ ] Wire `TextModel.ApplyPendingEdits`/undo/redo to the new accept/resolve flow and drop `AdjustDecorationsForEdit`.
- [ ] Build the new perf harness + targeted unit tests, document perf baselines, and attach metrics to the audit report.
