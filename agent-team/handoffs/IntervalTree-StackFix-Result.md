# IntervalTree Stack Overflow Fix — Result

## Intro
- **Changefeed Anchors:** [`#delta-2025-11-26-sprint04-r1-r11`](../../docs/reports/migration-log.md#delta-2025-11-26-sprint04-r1-r11) · [`#delta-2025-11-26-intervaltree-stackfix`](../../docs/reports/migration-log.md#delta-2025-11-26-intervaltree-stackfix)
- **Traceability:** Evidence captured in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-intervaltree-stackfix) and [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#intervaltree).
- **Scope Summary:** Port the TS iterative `IntervalSearch`, scrub the shared sentinel, and disable xUnit parallelism so IntervalTree suites stop tripping NullReferenceExceptions after large decoration edits.
- **TS References:** `src/vs/editor/common/model/intervalTree.ts` (`intervalSearch`, `resetSentinel`, `intervalSearchNoIterable`).

## Implementation
| Component | Files | TS Reference | Notes |
| --- | --- | --- | --- |
| Iterative `IntervalSearch` | `src/TextBuffer/Decorations/IntervalTree.cs` | [`intervalTree.ts`](../../ts/src/vs/editor/common/model/intervalTree.ts) | Manual stack traversal mirrors TS semantics and removes the 10k-decoration stack overflow seen in Sprint 04 perf runs. |
| Sentinel hygiene | `src/TextBuffer/Decorations/IntervalTree.cs` | [`intervalTree.ts`](../../ts/src/vs/editor/common/model/intervalTree.ts) | Resets `Parent/Left/Right/Delta/Start/End` and clears `IsVisited` so DEBUG counters remain accurate between tests. |
| Runner policy | `tests/TextBuffer.Tests/TextBuffer.Tests.csproj`, `tests/TextBuffer.Tests/xunit.runner.json` | n/a | Adds `parallelizeAssembly=false` / `parallelizeTestCollections=false`, matching production’s single-threaded usage until per-instance sentinels land. |

### Key Fixes
- Iterative traversal plus `ResetSentinel()` honor the guidance in [`docs/reports/migration-log.md#delta-2025-11-26-intervaltree-stackfix`](../../docs/reports/migration-log.md#delta-2025-11-26-intervaltree-stackfix) and eliminate stale child pointers that previously triggered NullReferenceExceptions.
- DEBUG counters (`NodesRemovedCount`, `RequestNormalizeHits`) now reset reliably, giving QA perf suites deterministic baselines.
- xUnit parallelism remains disabled to avoid cross-test sentinel contention until the WS3 per-instance sentinel follow-up (`AA4-IntervalTree-SentinelDetach`) completes.

## Testing
| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | `496/496` green (≈62s) | Full sweep covering IntervalTree, FindModel, and DocUI suites. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter IntervalTreePerfTests --nologo` | `7/7` green with perf guardrails | `[PERF]` Mixed 10k ops 1.2s · Batch edits 2.5s · 1k queries 28ms · Single top edit 42ms. |

- Results recorded in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#intervaltree).

## Open Items
- **Risk:** The static sentinel is still shared, so any premature re-enabling of parallel xUnit execution will reintroduce nondeterministic failures.
- **Next-Owner Instructions:**
  1. Complete the per-instance sentinel work (`AA4-IntervalTree-SentinelDetach`), then re-run both commands above and restore xUnit parallelism.
  2. Keep `IntervalTreePerfTests` in nightly CI to monitor the perf deltas listed above.
  3. Treat `ThreadStatic` as a stop-gap only if the per-instance refactor slips past Sprint 05.
