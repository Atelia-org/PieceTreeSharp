# B3-PieceTree-Fuzz-Review-INV
_Date: 2025-11-24_

## Scope
- **Staged code**: `src/TextBuffer/Core/PieceTreeModel.cs`, `tests/TextBuffer.Tests/Helpers/FuzzLogCollector.cs`, `tests/TextBuffer.Tests/Helpers/PieceTreeFuzzHarness.cs`, `tests/TextBuffer.Tests/PieceTreeFuzzHarnessTests.cs`
- **TS references**: `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts`, `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` (especially `testLinesContent`, `testLineStarts`, `suite('random is unsupervised')`)
- **Tests executed**: _Not run (analysis-only review)_

## Findings

### CI-1 – Random suites were not ported
The staged change adds a reusable `PieceTreeFuzzHarness` plus two harness smoke tests, but none of the TS random suites (`random bug 1..10`, `random chunk bug 1..4`, `suite('random is unsupervised')` with `random insert delete`, `random chunks`, `random chunks 2`, etc. from `pieceTreeTextBuffer.test.ts` lines ≈ 180–640 & 1380–1760) were actually migrated. Those suites are the ones exercising long insert/delete loops, chunk splitting under stress, and metadata rebuild assertions. As-is, the C# test count only proves the harness itself works; we still have zero coverage for the TS scenarios the harness was supposed to port.
- **Action**: Translate each TS test into an xUnit fact that drives the new harness (or direct `PieceTreeModel`) so we cover the same edit sequences, chunk layouts, and assertions. Record the imports in `tests/TextBuffer.Tests/TestMatrix.md` when done.

### CI-2 – TS invariants (`testLinesContent`/`testLineStarts`) were dropped
Every TS random test ends with both `testLinesContent(str, pieceTable)` and `testLineStarts(str, pieceTable)` (see `pieceTreeTextBuffer.test.ts` lines 70–180). These helpers validate that `getLineContent`, `getValueInRange`, `getPositionAt`, and `getOffsetAt` stay in sync for every line and line start. The new `PieceTreeFuzzHarness.AssertState()` only compares the flattened buffer text and then calls `InternalModel.AssertPieceIntegrity()`. That means any regression inside the line map (`GetPositionAt`/`GetOffsetAt`), per-line content, or LF-normalized range slicing would slip past the C# harness even though TS would catch it. We need parity-level assertions inside the harness (or per-test) to keep those API contracts under test.
- **Action**: Extend `AssertState()` (or add explicit helpers) to recreate TS behaviour: recompute expected line starts, walk each line verifying `GetLineContent`/`GetValueInRange`, and round-trip `GetPositionAt`↔`GetOffsetAt` for both the start and `lineStart-1` positions. Failures should emit to the fuzz log just like TS.

### W-1 – Sentinel metadata checks missing
TS `assertTreeInvariants` (same file, lines ≈ 110–150) explicitly asserts `SENTINEL.size_left === 0` and `SENTINEL.lf_left === 0` before descending. The new `PieceTreeModel.ValidateTreeInvariants()` checks the sentinel color/links but never validates `SizeLeft`/`LineFeedsLeft`. If those counters drift from zero the TS test would fail while our C# `AssertPieceIntegrity()` would still pass, masking pointer bugs when rotating the root. Please match the TS assertions so the sentinel cannot accumulate stale metadata.

### W-2 – Random text distribution diverges from TS
`randomStr()` in TS pulls from `alphabet = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ\r\n'`, i.e., mix of upper/lowercase letters plus single CR/LF characters (never an atomic "\r\n" token). `PieceTreeFuzzHarness.CreateRandomText()` only emits lowercase letters and adds `"\r\n"` as a single choice. That materially changes split/merge patterns: we no longer hit uppercase branches (which TS uses to flip between ASCII classifications) and we generate synthesized CRLF pairs that TS never produces, so parity bugs may be invisible or we might chase false positives. Consider mirroring the TS alphabet and per-character generation so the fuzz pressure matches upstream expectations.

### DOC-1 – Changefeed/TestMatrix updates absent from the staged diff
Per `AGENTS.md` and Sprint-03 docs, every B3 deliverable must be reflected in `docs/reports/migration-log.md`, `docs/plans/ts-test-alignment.md`, `agent-team/task-board.md`, and the TestMatrix. The current staged set only touches code; none of the documentation files were updated (and `git status -sb` still shows unstaged edits to those paths). We need the canonical changefeed row + task board state change + TestMatrix row before landing the harness.

## Recommendations for Porter / QA
1. Port the TS random suites immediately on top of the harness (CI-1) and list them in `tests/TextBuffer.Tests/TestMatrix.md` under a new PT-005 bucket.
2. Augment `PieceTreeFuzzHarness.AssertState()` with line-content and line-start parity checks so it genuinely mirrors TS coverage (CI-2). Keep the extra failures flowing into `FuzzLogCollector` for reproduction.
3. Extend `ValidateTreeInvariants()` to check sentinel `SizeLeft`/`LineFeedsLeft` just like TS (W-1).
4. Align `CreateRandomText()` with TS `randomStr()` (alphabet + per-char generation) to keep the same fuzz surface (W-2).
5. Before merging, update the doc chain (AGENTS, task board, sprint log, migration log, ts-test-alignment plan, TestMatrix) and publish the placeholder changefeed ID promised in Sprint 03 (DOC-1).

## QA Hooks
- Once the TS suites are ported, run `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeFuzzHarnessTests --nologo` plus the new specific suite names (e.g., `--filter PieceTreeRandomTests` if you split them out) to capture parity evidence.
- Keep `PIECETREE_FUZZ_SEED`/`PIECETREE_FUZZ_LOG_DIR` exposed so failing seeds can be reproduced exactly the way TS logs raw operations.

## Open Questions / Follow-ups
- Do we still need direct `PieceTreeModel` entry points for future tests, or will everything route through `PieceTreeBuffer`? Clarify before we wire more helpers.
- Confirm where the canonical changefeed anchor for B3-Fuzz will live so Info-Indexer can cross-link it (placeholder currently points to `#delta-2025-11-23-b3-piecetree-fuzz`).
