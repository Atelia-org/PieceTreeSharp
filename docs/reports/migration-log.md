# Migration Log & Changefeed Checklist

Purpose: capture every PieceTree porting milestone plus its Info-Indexer changefeed delta at `agent-team/indexes/README.md#delta-2025-11-19`, giving AGENTS / Sprints / Task Board editors one audit trail before they update status text.

## How to Append Entries
1. Log the date and Task/Subtask ID exactly as shown on the Task Board.
2. Summarize the delta and cite the concrete files or sections touched using Markdown links (relative paths only).
3. Record the validation evidence (e.g., `dotnet test` command, review notes) and link to the log or README section proving the run.
4. Mark **Changefeed Entry?** = Y only after the update is registered under `agent-team/indexes/README.md#delta-2025-11-19`, and point to that delta inside the Notes column.
5. Future edits to AGENTS / Sprints / Task Board must reference both the relevant row in this table and the matching changefeed entry before saving.

| Date | Task ID | Summary | Key Files | Tests/Validation | Changefeed Entry? (Y/N) | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| 2025-11-19 | PT-003 | Expanded TS↔C# mappings for Piece/PieceTree/SearchContext/BufferRange. | [`agent-team/type-mapping.md`](../../agent-team/type-mapping.md)<br>[`ts/src/vs/editor/common/model/pieceTreeTextBuffer`](../../ts/src/vs/editor/common/model/pieceTreeTextBuffer) | Planner + Info-Indexer review noted in [`agent-team/type-mapping.md`](../../agent-team/type-mapping.md). | Y | Delta recorded at [`agent-team/indexes/README.md#delta-2025-11-19`](../../agent-team/indexes/README.md#delta-2025-11-19); Sprint 00 log cites this row. |
| 2025-11-19 | PT-004.G1 | Wired `PieceTreeBuilder` → `PieceTreeModel` → `PieceTreeBuffer` skeleton with RB helpers. | [`src/PieceTree.TextBuffer/Core`](../../src/PieceTree.TextBuffer/Core)<br>[`src/PieceTree.TextBuffer/README.md#porting-log`](../../src/PieceTree.TextBuffer/README.md#porting-log) | `dotnet test PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` logged in [`src/PieceTree.TextBuffer/README.md#porting-log`](../../src/PieceTree.TextBuffer/README.md#porting-log). | Y | Changefeed proof at [`agent-team/indexes/README.md#delta-2025-11-19`](../../agent-team/indexes/README.md#delta-2025-11-19); Porting Log cross-references. |
| 2025-11-19 | PT-005.G1 | Established QA matrix + 7 Fact baseline across plain/CRLF/multi-chunk cases. | [`src/PieceTree.TextBuffer.Tests/TestMatrix.md`](../../src/PieceTree.TextBuffer.Tests/TestMatrix.md)<br>[`src/PieceTree.TextBuffer.Tests/UnitTest1.cs`](../../src/PieceTree.TextBuffer.Tests/UnitTest1.cs) | `dotnet test PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` results archived in [`src/PieceTree.TextBuffer.Tests/TestMatrix.md`](../../src/PieceTree.TextBuffer.Tests/TestMatrix.md). | Y | QA delta logged under [`agent-team/indexes/README.md#delta-2025-11-19`](../../agent-team/indexes/README.md#delta-2025-11-19) and referenced by the matrix. |
