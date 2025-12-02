# AA4-006 Result (CL6) – Porter-CS

Summary
-------
- Hardened change-buffer reuse: `_lastChangeBufferPos/_lastChangeBufferOffset` now gate appends, and large inserts split via `ChunkUtilities.SplitText` so small typing reuses the change buffer while bulk pastes allocate standalone chunks.
- CRLF repair no longer leaves zero-length nodes; `FixCRLF`/`AdjustCarriageReturnFromNext` recompute metadata and issue targeted cache invalidations, and `PieceTreeModel.AssertPieceIntegrity` is exposed for tests.
- Search cache invalidation tightened to the mutated span for insert/delete/CRLF fixes so sequential lookups stay hot while still remaining correct.
- Tests now cover change-buffer heuristics, large insert chunking, metadata rebuilds, CRLF boundary repairs, and deterministic CRLF fuzzing with log redirection.

Files Touched
-------------
- `src/PieceTree.TextBuffer/Core/PieceTreeModel.Edit.cs` – change-buffer append heuristics, CRLF repair cleanup, precise cache invalidations.
- `src/PieceTree.TextBuffer/Core/PieceTreeModel.cs` – `AverageBufferSize` plumbing and new `AssertPieceIntegrity` helper.
- `src/PieceTree.TextBuffer/Core/PieceTreeSearchCache.cs` – reused tighter `InvalidateRange` paths.
- `src/PieceTree.TextBuffer.Tests/PieceTreeModelTests.cs`, `src/PieceTree.TextBuffer.Tests/CRLFFuzzTests.cs`, `src/PieceTree.TextBuffer.Tests/Helpers/FuzzLogCollector.cs` – deterministic fuzz logging, metadata/CRLF regression cases.
- `src/PieceTree.TextBuffer.Tests/TestMatrix.md`, `docs/reports/migration-log.md`, `agent-team/task-board.md`, `agent-team/members/porter-cs.md` – documentation/test-matrix/task updates recorded.

Tests
-----
- `PieceTreeModelTests` expanded with: `CRLFRepair_DoesNotLeaveZeroLengthNodes`, `MetadataRebuild_AfterBulkDeleteAndInsert`, refreshed `LastChangeBufferPos_AppendOptimization`, `AverageBufferSize_InsertLargePayload`, `CRLF_FuzzAcrossChunks`, and `ChangeBufferFuzzTests` integrity hooks.
- `CRLFFuzzTests` now deterministic via `FuzzLogCollector`; logs redirect to `$PIECETREE_FUZZ_LOG`/`$PIECETREE_FUZZ_LOG_DIR` instead of stdout.
- Test matrix updated to reflect the new scenarios under the CL6 section.

Build & Tests
-------------
- `PIECETREE_DEBUG=0 dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` — **PASS (119/119)**.

Follow Ups
----------
1. Coordinate with AA4-007 to ensure cursor/snippet work continues to respect the tightened metadata invariants (especially when cursor multi-edit injects CR/LF near chunk boundaries).
2. For AA4-008, reuse the new `AssertPieceIntegrity` helper inside DocUI/search regression tests whenever CRLF-normalized decorations are applied.
3. Info-Indexer still needs to record the final CL6 delta under `agent-team/indexes/README.md` once QA signs off on the refreshed fuzz harness output.

Handoff
-------
- Documentation (`migration-log`, `task-board`, Porter memory) references this result; QA can now consume the deterministic fuzz logs for repros. Outstanding risks now track to AA4-007/AA4-008 parity items listed above.

