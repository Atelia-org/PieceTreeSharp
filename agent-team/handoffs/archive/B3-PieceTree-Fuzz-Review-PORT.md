# B3-PieceTree-Fuzz-Review-PORT
_Date: 2025-11-24_

## Completed in this drop
- **CI-2 – Line invariants restored:** `PieceTreeFuzzHarness.AssertState()` now mirrors TS `testLinesContent`/`testLineStarts`. The harness recomputes expected line splits, line starts, and performs detailed parity checks covering `GetLineContent`, `GetValueInRange` (with `TrimLineFeed`), and `GetPositionAt`⇄`GetOffsetAt` round trips. Supporting fixes: `PieceTreeModel.GetLineContent()` trims trailing EOLs like TS and `PieceTreeBuffer.GetOffsetAt()` now accounts for CR/LF columns, so the parity checks exercise the same surface as the original tests. Failures emit the offending line/offset plus a flushed fuzz log so QA can replay the sequence.
- **W-1 – Sentinel metadata guard:** `PieceTreeModel.ValidateTreeInvariants()` now asserts `_sentinel.SizeLeft`, `_sentinel.LineFeedsLeft`, `_sentinel.AggregatedLength`, and `_sentinel.AggregatedLineFeeds` all remain zero before walking the tree, aligning with TS `assertTreeInvariants`.
- **W-2 – TS alphabet parity:** `PieceTreeFuzzHarness.CreateRandomText()` now samples characters from `abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ\r\n`, removing the previous "lowercase-only + CRLF token" bias.

## Outstanding work
- _None – CI-1/CI-2 gaps from Investigator’s audit are closed via `#delta-2025-11-24-b3-piecetree-fuzz`. Additional fuzz scenarios can build atop the new script helper as needed._

## Validation
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeFuzzHarnessTests --nologo` ✅

## 2025-11-24 – CI-1/CI-2 harness parity (#delta-2025-11-24-b3-piecetree-fuzz)
- **Multi-chunk seeding + logging:** `PieceTreeFuzzHarness` can now materialize `PieceTreeBuffer` via `PieceTreeBuffer.FromChunks(chunks, normalize)` so TS `createTextBuffer(chunks, /*normalize*/)` cases replay deterministically. The harness concatenates the seed chunks to bootstrap `_expected`, logs `chunks=N normalize=<bool>` via `FuzzLogCollector`, and keeps the single-string constructor path untouched for existing callers.
- **TS random suites translated:** Added a small script-runner helper plus TS-style `randomStr` generator in `PieceTreeFuzzHarnessTests` to port `random test 1/2/3`, `random delete 1/2/3`, and both `random chunks` suites (ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts lines 271-404 & 1668-1725). Chunk suites seed 5×1000 or 1×1000 buffers, drive 1000/50 iterations with deterministic RNG seeds, and assert harness state each loop (matching TS `testLinesContent`).
- **Docs + matrix:** `tests/TextBuffer.Tests/TestMatrix.md` now tracks the new CI-1/CI-2 coverage rows with delta tag `#delta-2025-11-24-b3-piecetree-fuzz`, and this handoff supersedes the earlier "Outstanding work" bullets (CI-1/CI-2 closed).

### Validation
```
cd /repos/PieceTreeSharp
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeFuzzHarnessTests --nologo
	TextBuffer.Tests test succeeded (52.5s)
Test summary: total: 10, failed: 0, succeeded: 10, skipped: 0, duration: 53.7s
```
