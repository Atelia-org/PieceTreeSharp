# B3 PieceTree Fuzz Review (2025-11-24)

## Scope
Staged files: `src/TextBuffer/Core/PieceTreeModel.Search.cs`, `src/TextBuffer/Core/PieceTreeModel.cs`, `src/TextBuffer/PieceTreeBuffer.cs`, `tests/TextBuffer.Tests/Helpers/FuzzLogCollector.cs`, `tests/TextBuffer.Tests/Helpers/PieceTreeFuzzHarness.cs`, `tests/TextBuffer.Tests/PieceTreeFuzzHarnessTests.cs`.

## Findings

### F1. TS fuzz suites still untranslated (Critical)
- Evidence: `tests/TextBuffer.Tests/PieceTreeFuzzHarnessTests.cs` lines 12-41 only exercise the harness itself, while TS `pieceTreeTextBuffer.test.ts` lines 271-1727 contain dozens of deterministic `random test` / `random delete` cases plus the `random is unsupervised` suite. None of those scenarios were ported into the new harness.
- Impact: The new infrastructure does not currently run any parity scenarios, so regressions in insert/delete balancing, prefix sums, CRLF normalization, or search cache invalidation remain undetected. We still lack coverage for the cases that repeatedly caught regressions in VS Code (issues linked in TS comments around lines 696+, 879+, 1516+, 1629+).
- Recommendation: Use `PieceTreeFuzzHarness` to script the same deterministic sequences found in TS (start with `random test 1/2/3`, `random delete` series, and the CRLF-focused bug repros). Then add at least one harness-driven fuzz loop mirroring `suite('random is unsupervised', ...)` with a deterministic seed so failures can be reproduced from the new `FuzzLogCollector` output. Wire these cases into `PieceTreeFuzzHarnessTests` (or a sibling suite) so we can tick off CI-1 in the fuzz audit checklist.

### F2. Harness cannot seed multi-chunk inputs (Major)
- Evidence: `PieceTreeFuzzHarness` constructor (tests/TextBuffer.Tests/Helpers/PieceTreeFuzzHarness.cs lines 25-33) always creates `new PieceTreeBuffer(initialText ?? string.Empty)`. The TS `random chunks` suites (see `pieceTreeTextBuffer.test.ts` lines 1668-1727) rely on `createTextBuffer(chunks)` to pre-populate multiple large immutable buffers before running edits.
- Impact: We cannot port the TS chunk-specific fuzz tests because there is no way to feed a pre-split chunk array (with mixed CR/LF combinations) into the harness. That leaves CRLF chunk-boundary bugs (#27050, #29649) uncovered.
- Recommendation: Teach the harness to accept either (a) an `IEnumerable<string>` of initial chunks (hooked up through `PieceTreeBuffer.FromChunks`) or (b) a callback that can configure the underlying `PieceTreeModel` before edits start. Once in place, port `random chunks`/`random chunks 2` plus the large-chunk regressions that lean on builder metadata.

## Next Steps
1. Porter-CS: extend the harness API (chunk seeding + deterministic suite plumbing) and port the TS tests called out above.
2. QA: once suites land, add targeted commands to `tests/TextBuffer.Tests/TestMatrix.md` and capture seeds/log locations for reproducibility (`PIECETREE_FUZZ_SEED`, `PIECETREE_FUZZ_LOG_DIR`).
3. Investigator-TS: revisit after Porter drop to confirm CI-1/CI-2 from the fuzz audit are cleared.
