# PieceTree.TextBuffer

This project hosts the in-progress C# port of VS Code's piece tree text buffer. The public API is still backed by a `StringBuilder`, but the core folder already mirrors the RB tree scaffolding used by the TypeScript implementation.

## Porting Log
| Date | Sprint Ref | Notes |
| --- | --- | --- |
| 2025-11-19 | PT-004.G1 | Added `PieceTreeNode`, `PieceTreeModel`, rotation/balance helpers, and the search stub. Tests now cover metadata aggregation, in-order enumeration, and the deterministic `NotSupportedException`. `PieceTreeBuffer` keeps the simple string façade while a TODO points to the new model for future wiring. |
| 2025-11-19 | PT-004.M2 | Wired `PieceTreeBuffer` through `ChunkBuffer` → `PieceTreeBuilder` → `PieceTreeModel`, added line-start helpers plus CRLF-aware chunk slicing, and recorded multi-chunk + edit coverage via `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` (pass). |
