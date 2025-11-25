# B3-PieceTree-Fuzz-Harness (R25) – Porter-CS

## Scope
- Followed [`agent-team/handoffs/B3-PieceTree-Fuzz-PLAN.md`](../handoffs/B3-PieceTree-Fuzz-PLAN.md) + Investigator memo to deliver the reusable harness + RB-tree invariant checks before kicking off deterministic suites.
- All changefeed references remain pointed at the placeholder `#delta-2025-11-23-b3-piecetree-fuzz` until Info-Indexer publishes the official entry.

## Code & Test Updates
- Added `tests/TextBuffer.Tests/Helpers/PieceTreeFuzzHarness.cs`:
  - Wraps `PieceTreeBuffer` + expected `StringBuilder`, consumes `PIECETREE_FUZZ_SEED` (default `8675309`), and logs every operation via `FuzzLogCollector`.
  - Exposes helpers for inserts/deletes/replacements, `RunRandomEdits`, `GetLineCount`, `GetLineContent`, `GetOffsetAt`, `GetPositionAt`, and `GetValueInRange` (LF/CRLF normalization) so deterministic suites can reuse the plumbing.
  - Provides range diff utilities through `PieceTreeRangeDiff` + `DescribeFirstDifference()` to mirror TS `testLinesContent` debugging aids.
- Extended `tests/TextBuffer.Tests/Helpers/FuzzLogCollector.cs` with structured `FuzzOperationLogEntry` (operation name, offsets, lengths, sanitized text, seed, iteration).
- Strengthened `src/TextBuffer/Core/PieceTreeModel.cs::AssertPieceIntegrity()` with TS `assertTreeInvariants` semantics: root/sentinel color checks, parent/child link validation, red-node constraints, black-height equality, and metadata consistency (`SizeLeft`, `LineFeedsLeft`, aggregates).
- Added smoke tests in `tests/TextBuffer.Tests/PieceTreeFuzzHarnessTests.cs` to prove the harness runs deterministic sequences and surfaces corruption diffs/log paths.
- Updated `tests/TextBuffer.Tests/TestMatrix.md` + `docs/reports/migration-log.md` with the new harness entries and commands.

## Validation
| Command | Purpose | Result |
| --- | --- | --- |
| `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeFuzzHarnessTests --nologo` | Harness-only smoke coverage | 2/2 ✅ |
| `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | Full regression sweep with updated invariants | 245/245 ✅ (≈4.1s) |

## Risks / Follow-ups
1. **Changefeed:** still referencing placeholder `#delta-2025-11-23-b3-piecetree-fuzz`; Info-Indexer needs to publish once QA signs off on subsequent runs (R26+).
2. **Log retention:** `FuzzLogCollector` now captures seeds + operation metadata; ensure QA archive instructions (R28) mention the new environment variables (`PIECETREE_FUZZ_SEED`, optional `PIECETREE_FUZZ_LOG[_DIR]`).
3. **Range diff consumers:** deterministic suites (R26) should call `PieceTreeFuzzHarness.DescribeFirstDifference()` rather than reimplementing offset diagnostics.

## Next Steps
- R26 can reuse `PieceTreeFuzzHarness` for deterministic TS insert/delete suites (prefix-sum + range tests) without re-creating env plumbing.
- QA (R28) should add multi-seed runs by exporting `PIECETREE_FUZZ_SEED` before invoking the new harness filters.
