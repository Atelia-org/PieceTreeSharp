# PORT-PT-Search Step12 — QA Evidence (NodeAt2 Tuple Reuse + Search Cache Diagnostics)

## Scope & Anchors
- Validated the Step1/2 drop in `src/TextBuffer/Core/PieceTreeModel.Search.cs` (NodeAt2 tuple reuse, CRLF-aware accumulation, cache telemetry) plus the new release diagnostics in `PieceTreeSearchCache.cs`.
- Anchors: [`docs/reports/migration-log.md#sprint04-r1-r11`](../../docs/reports/migration-log.md#sprint04-r1-r11) · [`agent-team/indexes/README.md#delta-2025-11-27-ws1-port-search-step12`](../indexes/README.md#delta-2025-11-27-ws1-port-search-step12) *(pending Info-Indexer publish; QA evidence ready for that changefeed).* 
- Environment: `PIECETREE_DEBUG=0`, repo head after PORT-PT-Search Step12 commit, Ubuntu 22.04, .NET 9.0.11.

## Commands & Results
| Local Timestamp (UTC+8) | Command | Result |
| --- | --- | --- |
| 2025-11-27T23:02:30 | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeDeterministicTests --nologo` | 50/50 passed, 0 skips, duration 1.9s |
| 2025-11-27T23:02:43 | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeFuzzHarnessTests --nologo` | 15/15 passed, 0 skips, duration 63.6s |
| 2025-11-27T23:03:57 | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~CRLFFuzzTests --nologo` | 13/13 passed, 0 skips, duration 13.0s |
| 2025-11-27T23:04:19 | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeSearchRegressionTests --nologo` | 13/13 passed, 0 skips, duration 2.1s |
| 2025-11-27T23:04:34 | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeSearchOffsetCacheTests --nologo` | 5/5 passed, 0 skips, duration 1.8s |
| 2025-11-27T23:04:45 | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | 639/639 passed, 2 skips (`CursorCoreTests.SelectHighlightsAction_ParityPending`, `CursorCoreTests.MultiCursorSnippetIntegration_ParityPending`), duration 107.4s |

Notes:
- All commands executed with `export PIECETREE_DEBUG=0` to mimic production parity runs.
- The two `CursorCoreTests` skips match the AA4 CL7 placeholders referenced in the log output; no unexpected skips or flakes.

## Search Cache Diagnostics
- `PieceTreeSearchRegressionTests` and `PieceTreeSearchOffsetCacheTests` exercised the new tuple reuse path and invoked `PieceTreeBufferAssertions.AssertSearchCachePrimed`, which queries `PieceTreeModel.TryGetCachedNodeByOffset` and the `PieceTreeModel.Diagnostics.SearchCache` snapshot. Each probe verified cached tuples covered offsets `[0, mid, end]` without triggering assertion failures, indicating hits are recorded immediately after NodeAt2.
- No `CacheInvalidated` event warnings surfaced in stdout, confirming invalidation-from-offset clears stayed bounded. The snapshot remains available via `PieceTreeModel.Diagnostics.SearchCache` (hit/miss counters, entry count, last invalidation offset) for downstream telemetry; current test harness does not emit the raw numbers but passed all cache-health assertions.

## Conclusion
- Step1/2 tuple reuse + search cache diagnostics remain green under the targeted fuzz, regression, and deterministic suites as well as the full 641-case sweep. Evidence is ready to reference from [`docs/reports/migration-log.md#sprint04-r1-r11`](../../docs/reports/migration-log.md#sprint04-r1-r11) and to seed the new changefeed row at [`agent-team/indexes/README.md#delta-2025-11-27-ws1-port-search-step12`](../indexes/README.md#delta-2025-11-27-ws1-port-search-step12).
- No anomalies beyond the known CursorCore skips. Perf logs under `/tmp/piecetree-fuzz/DocUI.IntervalTreePerf.*` were regenerated during the full sweep; they match previous baselines and require no action.
