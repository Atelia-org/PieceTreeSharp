# AA4-005 – PieceTree Builder & Factory Remediation Plan

## Context
- **Sources:** `agent-team/handoffs/AA4-001-Audit.md`, `docs/reports/audit-checklist-aa4.md#cl5`, `agent-team/task-board.md` (AA4-005), Sprint 02 backlog (`docs/sprints/sprint-02.md`).
- **Goal:** Port TS builder/factory semantics (chunk splitting, BOM/metadata flags, factory API, DefaultEndOfLine heuristics, normalize pipeline) into the C# stack to unblock CL5.
- **Changefeed Anchor:** Updates will extend `agent-team/indexes/README.md#delta-2025-11-20` once remediation lands.

## Work Items
1. **Chunk ingestion parity**
   - Port TS carryover logic for trailing `\r`/high surrogates in `PieceTreeBuilder.AcceptChunk`.
   - Implement `CreateNewPieces` chunk splitting around `AverageBufferSize`, CRLF boundaries, surrogate pairs.
2. **Metadata & BOM plumbing**
   - Extend `PieceTreeBuildResult`/`PieceTreeModel` to persist BOM plus `containsRTL`, `containsUnusualLineTerminators`, `isBasicASCII`.
   - Surface `GetBOM()`/`MightContain*` APIs on `PieceTreeBuffer`.
3. **Factory surface & EOL heuristics**
   - Introduce managed `PieceTreeTextBufferFactory` with `Create(DefaultEndOfLine)` and `GetFirstLineText`.
   - Port `_getEOL` logic, ensure normalize step happens post default-EOL choice.
4. **Normalize-EOL convergence**
   - Replace `NormalizeChunks` with TS-style chunk window/regeneration pipeline shared with `PieceTreeModel.NormalizeEOL`.
5. **Testing & docs**
   - Add `PieceTreeBuilderTests` (chunk split, metadata) & `PieceTreeFactoryTests` (default EOL, first line preview).
   - Update `docs/reports/migration-log.md` (AA4-005 row) and changefeed.

## Deliverables
- Code updates under `src/PieceTree.TextBuffer/Core/*` + new factory file.
- Tests: `src/PieceTree.TextBuffer.Tests/PieceTreeBuilderTests.cs`, `PieceTreeFactoryTests.cs` (initial scaffolding ready).
- Documentation: migrate entries in `docs/reports/migration-log.md`, `agent-team/indexes/README.md`, `agent-team/task-board.md` status.

## Validation Plan
- Run `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` (expect ≥92 tests) covering new suites.
- Manual diff vs TS builder outputs on CRLF-heavy samples (kept in local notebook) to ensure chunk counts/line metadata align.

## Blockers / Dependencies
- None (CL5 audit complete). Coordinate with AA4-006 to share chunk/CRLF helpers once implemented.
