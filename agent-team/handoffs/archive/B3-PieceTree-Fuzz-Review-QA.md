# B3-PieceTree-Fuzz-Review-QA
_Date: 2025-11-24_

## Scope
- Review CI-2 / W-1 / W-2 fixes from Porter-CS for the PieceTree fuzz harness drop.
- Confirm staged code covers `PieceTreeFuzzHarness.cs`, `PieceTreeModel.cs`, `PieceTreeModel.Search.cs`, `PieceTreeBuffer.cs`, `PieceTreeFuzzHarnessTests.cs`, and `FuzzLogCollector.cs`.
- Execute the requested targeted test suites under `PIECETREE_DEBUG=0` and capture reproducible logs.

## Diff Verification (git diff --cached)
- `tests/TextBuffer.Tests/Helpers/PieceTreeFuzzHarness.cs` now mirrors TS `testLinesContent/testLineStarts`, adds deterministic RNG plumbing, per-line parity checks, and exposes helpers for harness-driven insert/delete/replace.
- `src/TextBuffer/Core/PieceTreeModel.cs` splits `AssertPieceIntegrity()` into `ValidatePieceMetadata()` + `ValidateTreeInvariants()` with sentinel metadata guards and RB-tree black-height validation (covers W-1).
- `src/TextBuffer/Core/PieceTreeModel.Search.cs` introduces `TrimTrailingLineFeed` and caches normalized line content when `_eolNormalized` is active (feeds CI-2 parity assertions).
- `src/TextBuffer/PieceTreeBuffer.cs` clamps columns using measured line breaks so `GetOffsetAt` matches TS semantics on CR/LF endings.
- `tests/TextBuffer.Tests/Helpers/FuzzLogCollector.cs` records structured fuzz operations via `FuzzOperationLogEntry`, providing sanitized repro logs.
- `tests/TextBuffer.Tests/PieceTreeFuzzHarnessTests.cs` adds deterministic smoke + corruption detection coverage for the harness itself.

## Tests Executed
| Command | Result | Duration | Log |
| --- | --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeFuzzHarnessTests --nologo --results-directory tests/TextBuffer.Tests/TestResults --logger "trx;LogFileName=b3-piecetree-fuzz-harness.trx"` | Passed (2/2) | 2.3s | `tests/TextBuffer.Tests/TestResults/b3-piecetree-fuzz-harness.trx` |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~PieceTreeModelTests" --nologo --results-directory tests/TextBuffer.Tests/TestResults --logger "trx;LogFileName=b3-piecetree-model.trx"` | Passed (9/9) | 1.8s | `tests/TextBuffer.Tests/TestResults/b3-piecetree-model.trx` |

## Findings
- ✅ CI-2 parity assertions now trigger `GetLineContent`, `GetValueInRange`, `GetPositionAt`, and `GetOffsetAt` equivalence per line; no regressions observed.
- ✅ W-1 sentinel metadata checks (`SizeLeft`, `LineFeedsLeft`, `AggregatedLength`, `AggregatedLineFeeds`) enforced during `AssertPieceIntegrity()`; RB-tree suite remains green.
- ✅ W-2 random text alphabet matches TS distribution; harness RNG replayable via `PIECETREE_FUZZ_SEED` (verified indirectly through deterministic smoke test).
- No new issues detected during targeted runs. CI-1 (TS random suite migration) and DOC-1 changefeed/TestMatrix updates remain outstanding per Porter-CS notes and should be tracked separately.

## Artifacts / Notes
- Fuzz harness operation logs will emit under the default temp path if a failure occurs; current run produced no logs (all tests passed).
- Re-run instructions recorded in this document for future CI reproductions.
