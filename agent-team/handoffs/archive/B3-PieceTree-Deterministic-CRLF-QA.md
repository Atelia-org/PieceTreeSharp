# B3 PieceTree Deterministic + CRLF QA — 2025-11-24

- **QA window:** 2025-11-24T17:23:45Z (UTC)
- **Env:** `PIECETREE_DEBUG=0`, repo head as-of current workspace, tests run from `/repos/PieceTreeSharp`.

## Commands & Outcomes
1. `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeDeterministicTests --nologo`
   - Total 50 · Passed 50 · Failed 0 · Skipped 0 · Duration 3.5s.
2. `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`
   - Total 308 · Passed 308 · Failed 0 · Skipped 0 · Duration 67.2s.

## Notes
- Both suites completed green on first attempt; binaries restored/build took 21.6s for the filtered run and 70.5s for the full sweep.
- The full run emitted a single `PieceTreeModel Dump` block while exercising CRLF repair coverage; log is informational only and matched prior instrumentation expectations.
- No anomalies or retries required; artifacts limited to console output.
