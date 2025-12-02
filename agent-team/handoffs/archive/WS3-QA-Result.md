# WS3-QA — IntervalTree Validation Result

## Intro
- **Changefeed Anchors:** [`#delta-2025-11-26-sprint04-r1-r11`](../../docs/reports/migration-log.md#delta-2025-11-26-sprint04-r1-r11) · [`#delta-2025-11-26-ws3-qa`](../../docs/reports/migration-log.md#delta-2025-11-26-ws3-qa)
- **Traceability:** QA suites logged in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-ws3-qa) and mapped in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws3-qa).
- **Scope Summary:** Validate the WS3 IntervalTree port using deterministic AcceptReplace suites, perf guardrails, and DEBUG-counter verification.
- **TS References:** VS Code `intervalTree.test.ts` plus the DocUI perf harness for 10k-decoration workloads.

## Implementation
| Artifact | File | Tests | Focus | TS Reference |
| --- | --- | ---: | --- | --- |
| Functional suite | `tests/TextBuffer.Tests/IntervalTreeTests.cs` | 13 | AcceptReplace insert/delete/replace flows, stickiness permutations, collapse-on-replace, decoration change plumbing. | [`intervalTree.test.ts`](../../ts/src/vs/editor/test/common/model/intervalTree.test.ts) |
| Perf suite | `tests/TextBuffer.Tests/DocUI/IntervalTreePerfTests.cs` | 7 | 10k-decoration batch/mixed edits, range query timing, and DEBUG counter accessibility. | [`intervalTree.perf.test.ts`](../../ts/src/vs/editor/test/common/model/intervalTree.perf.test.ts) |

### DEBUG Counter Validation
```csharp
#if DEBUG
public static int NodesRemovedCount { get; }
public static int RequestNormalizeHits { get; }
public static void ResetDebugCounters();
#endif
```
- Tests assert removal counts increment, normalize hits fire when deltas overflow, and `ResetDebugCounters()` clears state.

## Testing
| Command | Result |
| --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter IntervalTreeTests --nologo` | `13/13 green (≈2.2s)` |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter IntervalTreePerfTests --nologo` | `7/7 green (≈6.6s)` |
| Perf metrics | Batch edits 100×10k ≈ 2.3s · Single edit @ top ≈ 38ms · Mixed ops 50× ≈ 1.3s · 1000 queries ≈ 10ms |

- Verification entries captured inside [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws3-qa).

## Open Items
- **Risk:** xUnit parallelization stays disabled while IntervalTree still shares a static sentinel, so cross-test state bleed remains possible.
- **Next-Owner Instructions:**
  1. Re-run both suites once the per-instance sentinel from `IntervalTree-StackFix-Result.md` merges to ensure DEBUG counters stay accurate.
  2. After sentinel isolation, promote the perf suite to nightly CI to detect regressions automatically.
  3. Coordinate with WS5 harness owners so these fixtures become part of the shared deterministic data pool.
