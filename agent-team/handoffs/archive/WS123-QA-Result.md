# WS1/WS2/WS3 QA Validation Report

## Intro
- **Changefeed Anchors:** [`#delta-2025-11-26-sprint04-r1-r11`](../../docs/reports/migration-log.md#delta-2025-11-26-sprint04-r1-r11) · [`#delta-2025-11-26-ws123-qa`](../../docs/reports/migration-log.md#delta-2025-11-26-ws123-qa)
- **Traceability:** Aggregated QA findings are logged in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-ws123-qa) and mapped to the suites in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws123-qa).
- **Scope Summary:** Provide a single Sprint 04 QA sign-off that references the per-workstream QA deliverables (`WS1-PORT-SearchCore`, `WS2-PORT`, `WS3-PORT-Tree/QA`) without duplicating their detailed findings.
- **TS References:** Same as the originating workstreams—PieceTree search cache (`pieceTreeTextBuffer.test.ts`), Range/Selection helpers (`range.ts`, `selection.ts`), and IntervalTree (`intervalTree.ts`).

## Implementation
| Workstream | QA Artifact | Anchors | Highlights |
| --- | --- | --- | --- |
| WS1 (Search Core + CRLF) | [`WS1-PORT-SearchCore-Result.md`](WS1-PORT-SearchCore-Result.md) · [`WS1-PORT-CRLF-Result.md`](WS1-PORT-CRLF-Result.md) | `#delta-2025-11-26-ws1-searchcore`, `#delta-2025-11-26-ws1-port-crlf` | Hybrid `GetAccumulatedValue`, CRLF bridge parity, and regression suites linked to TestMatrix section `#ws1-port`. |
| WS2 (Range/Selection APIs) | [`WS2-PORT-Result.md`](WS2-PORT-Result.md) | `#delta-2025-11-26-ws2-port` | 75 Range/Selection/TextPosition unit tests now green; APIs match TS helpers. |
| WS3 (IntervalTree Port + QA) | [`WS3-PORT-Tree-Result.md`](WS3-PORT-Tree-Result.md) · [`WS3-QA-Result.md`](WS3-QA-Result.md) | `#delta-2025-11-26-ws3-port-tree`, `#delta-2025-11-26-ws3-qa` | Lazy delta IntervalTree rewrite plus dedicated perf + DEBUG-counter validation. |

- Scenario-level QA notes (perf metrics, per-suite command logs) stay inside the workstream-specific handoffs above to avoid duplication.

- This roll-up restricts itself to cross-workstream validation (full-suite and smoke runs) and defers to the individual handoffs for tactical detail per instruction (deduped content).

## Testing
| Suite | Command | Result | Anchor |
| --- | --- | --- | --- |
| Full sweep | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | `440/440` green (~62s) | `#delta-2025-11-26-ws123-qa` |
| WS2 helpers | `... --filter RangeSelectionHelperTests --nologo` | `75/75` green (1.6s) | `#delta-2025-11-26-ws2-port` |
| WS1 cache | `... --filter PieceTreeSearchOffsetCacheTests --nologo` | `5/5` green (1.7s) | `#delta-2025-11-26-ws1-searchcore` |
| WS3 decorations | `... --filter DecorationTests --nologo` | `12/12` green (1.7s) | `#delta-2025-11-26-ws3-qa` |
| WS3 stickiness | `... --filter DecorationStickinessTests --nologo` | `4/4` green (1.7s) | `#delta-2025-11-26-ws3-qa` |

- Counts reconcile with [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws123-qa); per-workstream QA docs hold the deeper scenario-by-scenario narratives.

## Open Items
- **Shared Risks:**
    - `NodeAt2` tuple reuse is deferred until CRLF bridge telemetry stabilizes (WS1 backlog).
    - IntervalTree still uses a static sentinel, so parallel test execution remains disabled (WS3 backlog).
    - Intl.Segmenter-backed word segmentation for search is still blocked on TS specs (tracked under AA4).
- **Next-Owner Instructions:**
    1. Keep this roll-up in sync with individual QA docs—no duplicate data; instead, link newly added suites back into TestMatrix so this summary stays lightweight.
    2. Re-run the aggregate suite once WS3 delivers the per-instance sentinel so xUnit parallelism can be re-enabled.
    3. Coordinate with WS5 QA once the GuessIndentation API lands to ensure cross-workstream tests remain aligned.
