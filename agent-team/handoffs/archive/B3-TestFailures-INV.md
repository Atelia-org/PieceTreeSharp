# B3-TestFailures-INV
**Date:** 2025-11-24
**Investigator:** Investigator-TS

## Context
- Full run (`export PIECETREE_DEBUG=0 && dotnet test -v m`) now fails 8 tests (previous report listed 6) immediately after staging the `GetLineContent` parity change plus the new sentinel assertions in `PieceTreeModel.AssertPieceIntegrity`.
- Targeted repro commands:
  - `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj -v m --filter PieceTreeBaseTests.GetLineContent_Cache_Invalidation_Insert` → deterministic failure (missing trailing `\n`).
  - `dotnet test ... --filter PieceTreeFuzzHarnessTests.RandomTestThreeMatchesTsScript` passes when executed in isolation, but fails inside the full suite with `Sentinel invariants violated`.

## Findings
1. **`GetLineContent` cache tests expect legacy behavior**
   - Tests: `PieceTreeBaseTests.GetLineContent_Cache_Invalidation_Insert/Delete` (`tests/TextBuffer.Tests/PieceTreeBaseTests.cs` lines 65-95) and `PieceTreeNormalizationTests.Delete_CR_In_CRLF_{1,2}`, `Line_Breaks_Replacement_Is_Not_Necessary_When_EOL_Is_Normalized` (`tests/TextBuffer.Tests/PieceTreeNormalizationTests.cs` lines 28-90) all assert that `GetLineContent` returns the newline terminator for non-final lines.
   - C# change: `PieceTreeModel.GetLineContent` now trims trailing EOLs unless `lineNumber == TotalLineFeeds + 1` or the buffer was normalized (`src/TextBuffer/Core/PieceTreeModel.Search.cs` lines 258-279, helper at lines 424-446).
   - TS reference: `PieceTreeBase.getLineContent` applies the same trimming (`ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` lines 610-632). The upstream tests (`testLinesContent` in `pieceTreeTextBuffer.test.ts`) compare against `splitLines`, which strips terminators.
   - **Conclusion:** runtime behavior is finally TS-aligned; the failing C# tests still encode the pre-trim assumption introduced before parity work. They should be updated to assert trimmed values (and rerun cache invalidation coverage) instead of reverting the code change.

2. **Normalization suite failure is the same delta, not an EOL-normalizer regression**
   - `Delete_CR_In_CRLF_{1,2}` expect `"\n"`/`"a\r"` because the assertions were written against raw `GetLineRawContent` semantics. After trimming, the first line now returns `""` and `"a"`, matching TS.
   - `Line_Breaks_Replacement_Is_Not_Necessary_When_EOL_Is_Normalized` similarly expects `"abcdef\n"` for line 1 even though TS uses `_EOLLength` to drop the terminator whenever `_EOLNormalized` is `true`.
   - **Recommendation:** adjust the test oracle to match TS (line content without the trailing breaker) and add explicit coverage for `GetLineRawContent` if we need to assert the presence of `\r`/`\n` characters.

3. **Sentinel invariant failures are cross-test interference, not tree corruption**
   - Failing cases: `PieceTreeFuzzHarnessTests.RandomTestThreeMatchesTsScript`, `RandomChunksMatchesTsSuite`, `RandomChunksTwoMatchesTsSuite` (see stack traces at `src/TextBuffer/Core/PieceTreeModel.cs:564`).
   - Each test passes on its own but fails when the suite runs in parallel, which points at a global/shared dependency. `PieceTreeNode.Sentinel` is a static singleton (`src/TextBuffer/Core/PieceTreeNode.cs` lines 11-68) reused by every `PieceTreeModel`. During `rbDelete`, we temporarily assign `_sentinel.Parent` to whatever subtree we are splicing (`PieceTreeModel.Edit.cs` lines 887-905) and reset it only after the fix-up (`lines 860-966`).
   - When xUnit runs multiple `PieceTreeBuffer` tests concurrently (default behavior), one tree can observe another tree's temporary sentinel parent/links while `ValidateTreeInvariants` runs (`PieceTreeModel.cs` lines 552-610), yielding the reported "sentinel must remain black and self-referential" errors even though the individual tree is healthy.
   - **Implication:** the new invariant check is correctly flagging a race caused by sharing the sentinel across models. We either need an instance-local sentinel (mirrors JS single-thread semantics) or we must serialize the PieceTree test collection/disable the new invariant check under parallel execution.

## Recommendations for Porter (priority order)
1. **Update the `GetLineContent`-related tests and docs.**
   - Switch the expectations in `PieceTreeBaseTests` + `PieceTreeNormalizationTests` to trimmed strings, add a companion assertion for `GetLineRawContent` if we still want to observe raw terminators, and mention the TS contract in `tests/TextBuffer.Tests/TestMatrix.md` so QA stops flagging these as regressions.
2. **Decide on sentinel scoping vs. test parallelization.**
   - Option A (preferred): give each `PieceTreeModel` its own sentinel node (convert `PieceTreeNode.Sentinel` into a factory, wire through constructors). That matches TS behavior conceptually (single-threaded) while remaining deterministic under xUnit parallelism and future multi-buffer scenarios.
   - Option B: keep the static sentinel but mark all PieceTree test classes with `[Collection("PieceTreeSerial")]` or disable xUnit parallelization globally until we finish the refactor. This unblocks CI quickly but does not solve future multi-thread consumers.
   - Regardless of path, keep `ValidateTreeInvariants` in place—it's catching genuine races.
3. **Add a regression harness once sentinel scoping is fixed.**
   - Re-run `PieceTreeFuzzHarnessTests` under full parallel load (`dotnet test -v m`) and keep a targeted command (`--filter PieceTreeFuzzHarnessTests.RandomChunksMatchesTsSuite`) in the TestMatrix as the smoke for sentinel metadata issues.

## Notes / TS References
- `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` lines 610-632 (`getLineContent`).
- `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` `testLinesContent()` helper uses `splitLines()` (strips terminators).
- `src/TextBuffer/Core/PieceTreeModel.Search.cs` lines 258-279, 424-446 (`GetLineContent` parity change).
- `src/TextBuffer/Core/PieceTreeNode.cs` lines 8-70 (static sentinel shared across models).
- `src/TextBuffer/Core/PieceTreeModel.cs` lines 552-610 (new `ValidateTreeInvariants`).

## Next Steps
- Await Porter decision on sentinel strategy, then re-run `export PIECETREE_DEBUG=0 && dotnet test -v m` to confirm only the intentionally failing TS-parity tests remain.
- Once the test expectations are updated, bump `tests/TextBuffer.Tests/TestMatrix.md` to reflect the `GetLineContent` parity milestone.
