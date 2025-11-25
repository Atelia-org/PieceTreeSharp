# B3 PieceTree Search Offset Cache QA — 2025-11-25

## Scope
- Certify `#delta-2025-11-25-b3-search-offset` so Info-Indexer can publish `#delta-2025-11-25-b3-search-offset`.
- Targeted validation: ensure `PieceTreeSearchOffsetCacheTests` remain deterministic after the cache-drop wiring.
- Baseline validation: rerun the entire `tests/TextBuffer.Tests` suite with `PIECETREE_DEBUG=0` to confirm no collateral regressions.

## Commands & Results
| Step | Command | Total | Passed | Failed | Duration |
| --- | --- | ---: | ---: | ---: | ---: |
| Targeted rerun | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo` | 5 | 5 | 0 | 4.3s |
| Full TextBuffer baseline | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | 324 | 324 | 0 | 58.2s |

## Observations
- Targeted cache suite stayed green (5/5) with no retries required; VSTest adapter log shows consistent discovery/teardown times.
- Full suite emitted the expected one-time `PieceTreeModel Dump` from deterministic diagnostics when CR-only fixtures execute; no warnings or flaky retries observed.
- Both runs executed with `PIECETREE_DEBUG=0`, matching the handoff requirements; build + test phases completed cleanly in ~60s end-to-end.

## Artifacts
- Evidence captured in this handoff plus `tests/TextBuffer.Tests/TestMatrix.md` baseline + targeted rerun sections.
- Upstream consumers (Info-Indexer) can cite this document when broadcasting `#delta-2025-11-25-b3-search-offset`.

## Follow-ups
- None blocking release; Info-Indexer can proceed with changefeed publication once this QA note is indexed.
