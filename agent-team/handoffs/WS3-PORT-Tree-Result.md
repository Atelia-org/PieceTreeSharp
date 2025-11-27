# WS3-PORT — IntervalTree Core Port Result

## Intro
- **Changefeed Anchors:** [`#delta-2025-11-26-sprint04-r1-r11`](../../docs/reports/migration-log.md#delta-2025-11-26-sprint04-r1-r11) · [`#delta-2025-11-26-ws3-port-tree`](../../docs/reports/migration-log.md#delta-2025-11-26-ws3-port-tree)
- **Traceability:** Recorded in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-ws3-port-tree) with coverage noted inside [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws3-port).
- **Scope Summary:** Complete Steps 1-4 of the IntervalTree lazy-normalize rewrite—Node layout, lazy delta semantics, normalization pass, and the TS `AcceptReplace` pipeline.
- **TS References:** `src/vs/editor/common/model/intervalTree.ts` for `IntervalNode`, lazy delta math, and `AcceptReplace` phases.

## Implementation
| Component | Files | TS Reference | Notes |
| --- | --- | --- | --- |
| IntervalTree rewrite | `src/TextBuffer/Decorations/IntervalTree.cs` | [`intervalTree.ts`](../../ts/src/vs/editor/common/model/intervalTree.ts) | Rebuilt to match TS: `NodeFlags`, sentinel layout, lazy delta tracking, iterative normalization, and four-phase `AcceptReplace`. |

### Highlights
- **Node Layout (Step 1):** Introduced `NodeFlags`, new `IntervalNode` storage (`Start/End/Delta/MaxEnd/Metadata`) plus cached absolute offsets for quick resolves.
- **Lazy Delta Semantics (Step 2):** Adopted shared sentinel, parent/child pointers, and safe delta bounds (`±2^30`) that trigger `RequestNormalize()` when exceeded.
- **Normalization (Step 3):** Added iterative in-order traversal that uses `IsVisited` flags to avoid recursion/allocations while applying pending deltas.
- **Resolve + AcceptReplace (Step 4):** Implemented the TS four-phase `AcceptReplace`, including stickiness handling, collapse-on-replace semantics, and `ResolveNode` cache updates.
- **DEBUG Counters:** `NodesRemovedCount`, `RequestNormalizeHits`, and `ResetDebugCounters()` gated by `#if DEBUG` for perf diagnostics.

## Testing
| Command | Result |
| --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | `Passed! - Failed: 0, Passed: 440, Skipped: 0, Total: 440` |
| `... --filter DecorationTests` | `12/12 green` |
| `... --filter DecorationStickinessTests` | `4/4 green` |

- Results documented in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws3-port).

## Open Items
- **Risk:** The static sentinel remains a single point of failure, so xUnit parallelization is intentionally disabled (until WS3 StackFix follow-up completes).
- **Next-Owner Instructions:**
   1. Deliver the per-instance sentinel (linked to `IntervalTree-StackFix-Result.md`) so tests can re-enable parallel execution.
   2. Wire `DecorationsTrees` to use `IntervalTree.AcceptReplace()` directly, reducing duplicate edit logic.
   3. Add perf benchmarks to monitor the lazy delta normalize thresholds once doc-size scale testing resumes.
