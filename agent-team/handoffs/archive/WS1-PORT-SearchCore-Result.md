# WS1-PORT — Search Core Result

## Intro
- **Changefeed Anchors:** [`#delta-2025-11-26-sprint04-r1-r11`](../../docs/reports/migration-log.md#delta-2025-11-26-sprint04-r1-r11) · [`#delta-2025-11-26-ws1-port-searchcore`](../../docs/reports/migration-log.md#delta-2025-11-26-ws1-port-searchcore)
- **Traceability:** Logged in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-ws1-port-searchcore) with coverage tracked in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws1-port).
- **Scope Summary:** Implement Step 1 & 4 from `PORT-PT-Search-Plan.md`—hybridize `GetAccumulatedValue`, keep `NodeAt2` in parity with TS, and instrument the search cache.
- **TS References:** `pieceTreeTextBuffer.test.ts` (search cache cases) and `src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts` (`getAccumulatedValue`, `SearchCache`).

## Implementation
| File | Description | TS Reference |
| --- | --- | --- |
| `src/TextBuffer/Core/PieceTreeModel.Search.cs` | Added LF-fast-path logic guarded by `_eolNormalized && _eol == "\n"` and fallback traversal that preserves CRLF correctness. | [`pieceTreeTextBuffer.ts`](../../ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts) |
| `src/TextBuffer/Core/PieceTreeSearchCache.cs` | Added DEBUG-only counters (`CacheHit`, `CacheMiss`, `ClearedAfterEdit`) and `SearchCacheDiagnostics` snapshot helpers. | [`pieceTreeTextBuffer.ts`](../../ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts) |
| `src/TextBuffer/Core/PieceTreeModel.cs` | Surfaced `GetSearchCacheDiagnostics()` for tooling. | [`pieceTreeTextBuffer.ts`](../../ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts) |

### Key Decisions
- Hybrid `GetAccumulatedValue`: keep the LineStarts O(1) path for normalized `\n` buffers and fall back to a character scan whenever CRLF bridging is active to avoid offset drift.
- Cache tuple reuse is deferred until the CRLF bridge is fully landed; `NodeAt2` continues to compose `GetOffsetAt + NodeAt` so Range/Selection consumers see stable offsets.
- DEBUG counters are intentionally behind `#if DEBUG` so perf investigations can pull `CacheHit/Miss/ClearedAfterEdit` snapshots without affecting release builds.


## Testing
| Command | Result |
| --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | `Passed! - Failed: 0, Passed: 365, Skipped: 0, Total: 365, Duration: 53s` |

- Coverage mapping updated in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws1-port).

## Open Items
- **Risk:** `NodeAt2` still relies on repeated `GetAccumulatedValue` invocations when CRLF bridging is active; we could regress perf on large mixed-EOL docs.
- **Next-Owner Instructions:**
    1. Fold the CRLF bridge work from WS1-PORT-CRLF before attempting any wider cache reuse.
    2. Once bridging is stable, re-evaluate tuple reuse (AA4 backlog `PT-NodeAt2-Perf`).
    3. Add telemetry hooks to the DEBUG counters when we spin up perf labs; they currently only live in developer builds.
