# AA4-005 Result – CL5 PieceTree Builder & Factory Parity

## Summary
- Rebuilt the builder pipeline so it preserves BOM/metadata and never splits CRLF or surrogate pairs: `PieceTreeBuilder` now feeds a dedicated `PieceTreeTextBufferFactory`, threads `PieceTreeBuilderOptions` (normalize EOL, default EOL, repair flags), and captures `containsRTL/unusual terminators/basic ASCII` booleans in the `PieceTreeBuildResult` record.
- Added chunk/metadata support utilities (`ChunkUtilities`, `TextMetadataScanner`) plus normalization-aware factory helpers so large inputs honor VS Code’s default chunk size and preview APIs (`GetFirstLineText`, `GetLastLineText`) match TS behavior.
- Updated `PieceTreeBuffer`/`PieceTreeModel` to consume the new factory output while extending the regression suite with dedicated builder/factory tests (`PieceTreeBuilderTests`, `PieceTreeFactoryTests`, helper utilities, and refreshed AA005 CRLF cases) – total coverage now 95 tests.

## Implementation Notes
1. **Factory + Options:** `PieceTreeBuilder.Finish()` returns `PieceTreeTextBufferFactory`, which materializes chunks (normalizing EOL when requested), computes EOL heuristics, and exposes preview helpers that respect line-length limits while avoiding double counting CR/LF pairs.
2. **Chunk Utilities:** `ChunkUtilities.SplitText/NormalizeChunks` guarantee chunk sizes stay within the TS-defined window and carry CR or trailing surrogate pairs forward so metadata scans remain correct. `TextMetadataScanner` centralizes RTL/unusual terminator detection for reuse by builder/model code paths.
3. **Buffer/Model Integration:** `PieceTreeBuffer` caches the builder options/BOM/metadata returned by the factory, and `PieceTreeModel.Insert` incorporates the new chunk-splitting helpers to keep metadata up to date across incremental edits. Tests exercise chunk splitting, BOM propagation, metadata flags, builder options, and CRLF repair boundaries.

## Validation
```
dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj
```
- Result: **Passed** (95 tests). Build emits a benign MSB3026 copy warning for `xunit.core.dll` on some runs; reruns succeed without intervention.

## Follow-ups / Risks
- Builder options such as `RepairInvalidLines` and `TrimAutoWhitespace` are plumbed through `PieceTreeBuilderOptions` but not yet honored during `PieceTreeModel` edits; track remaining parity work under AA4-006 when rewiring change buffer metadata.
- Metadata flags currently refresh via rebuild paths; once AA4-006 lands, incremental edits should trigger targeted metadata recomputation so `MightContain*` stays accurate without full rebuilds.

## Documentation & References
- Code: `src/PieceTree.TextBuffer/Core` (builder, factory, chunk utilities, metadata scanner, model edit updates) and `src/PieceTree.TextBuffer/PieceTreeBuffer.cs`.
- Tests: `src/PieceTree.TextBuffer.Tests/PieceTreeBuilderTests.cs`, `PieceTreeFactoryTests.cs`, `PieceTreeTestHelpers.cs`, updated `AA005Tests.cs`.
- Tracking: Entry added to `docs/reports/migration-log.md` under AA4-005; CL5 checklist updated to “Porter Complete – Pending QA”. Changefeed publication will occur in OI-011 once QA signs off.
