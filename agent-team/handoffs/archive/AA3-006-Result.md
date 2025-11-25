````markdown
# AA3-006 Result – CL3 Diff/Move Parity

## Summary
- Ported the TS `LinesDiff` pipeline so `DiffComputer` now returns TS-style metadata: `DetailedLineRangeMapping` with inner character spans, `DiffMove` collections, and the `hitTimeout` flag. All options (`ignoreTrimWhitespace`, `extendToWordBoundaries`, `extendToSubwords`, `maxComputationTimeMs`, `computeMoves`) mirror `ILinesDiffComputerOptions`.
- Added the full move-detection heuristic (`ComputeMovedLines`) with hashed line windows, similarity scoring, adjacency extension, and nested refinement so move ranges bubble up with their own inner diff mappings.
- Introduced helper abstractions (`LineRange`, `LineRangeFragment`, `LineSequence`, `LinesSliceCharSequence`, `RangeMapping`, `LineRangeMappingBuilder`, `MoveDetection`, etc.) plus supporting algorithms (dynamic programming, Myers, sequence optimizations) to match the TS implementation.
- Enhanced `DiffMove`/`DiffResult`/`DiffComputerOptions` data models to surface the new metadata while remaining binary-compatible with future doc/decoration consumers that AA3-008 will implement.

## Tests
- `DiffTests.WordDiffProducesInnerChanges` – verifies inner character spans on word insertions.
- `DiffTests.IgnoreTrimWhitespaceTreatsTrailingSpacesAsEqual` – confirms whitespace-ignoring parity.
- `DiffTests.MoveDetectionEmitsNestedMappings` – asserts computed moves include nested `innerChanges`.
- `DiffTests.DiffRespectsTimeoutFlag` – exercises the timeout surface (`hitTimeout = true`).
- Full suite via `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` ⇒ **Passed (80/80)**.

## File Highlights
- `src/PieceTree.TextBuffer/Diff/DiffComputer.cs` – rewired compute path to the new `LinesDiff` model, added heuristic sequencing, move detection wiring, and timeout support.
- `src/PieceTree.TextBuffer/Diff/*` – new helper types (`LineRange`, `LineRangeFragment`, `RangeMapping`, `LineRangeMappingBuilder`, `LineSequence`, `LinesSliceCharSequence`, `ComputeMovedLines`, `HeuristicSequenceOptimizations`, `Algorithms/*`, `Array2D`, `OffsetRange`) implement the TS diff infrastructure.
- `src/PieceTree.TextBuffer/Diff/DiffComputerOptions.cs` / `DiffResult.cs` / `DiffMove.cs` – align option/result surfaces with TS definitions.
- `src/PieceTree.TextBuffer.Tests/DiffTests.cs` – expanded with AA3 parity scenarios listed above.

## Known Limitations
- Diff output currently targets text comparisons only; decoration/DocUI integration remains pending for AA3-008.
- The timeout mechanism mirrors TS granularity but still depends on wall-clock timers; fine-grained cancellation hooks (per-edit progress) remain future work.

## Changefeed & Migration Log
- Recorded in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md) under AA3-006 with links to the diff files and test command.
- Added to [`agent-team/indexes/README.md#delta-2025-11-20`](../indexes/README.md#delta-2025-11-20) so Info-Indexer consumers see the diff/move parity update.

````