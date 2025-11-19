# Task Board - Phase 2: TextModel & Interaction

**Changefeed Reminder:** Read `agent-team/indexes/README.md` before editing any status or owner so Task Board changes stay aligned with the Info-Indexer delta feed.

## TextModel & Interaction (TM/OI)

| ID | Description | Owner | Key Artifacts | runSubAgent Budget | Status | Latest Update |
| --- | --- | --- | --- | --- | --- | --- |
| TM-001 | Analyze TextModel dependencies & update Type Mapping | Investigator-TS | `agent-team/type-mapping.md` | 1 | Planned | - |
| TM-002 | Port `TextModel` skeleton (Events, Version ID) | Porter-CS | `src/PieceTree.TextBuffer/TextModel.cs` | 2 | Planned | - |
| TM-003 | Port `Selection` / `Position` math | Porter-CS | `src/PieceTree.TextBuffer/Core/Position.cs`<br>`src/PieceTree.TextBuffer/Core/Selection.cs` | 1 | Planned | - |
| TM-004 | Implement `onDidChangeContent` event firing | Porter-CS | `src/PieceTree.TextBuffer/TextModel.cs` | 2 | Planned | - |
| TM-005 | Basic Cursor support (Single cursor) | Porter-CS | `src/PieceTree.TextBuffer/Cursor/Cursor.cs` | 2 | Planned | - |
| OI-006 | Phase 2 Documentation & Indexing | Info-Indexer | `agent-team/indexes/core-docs-index.md` | 1 | Planned | - |

## Reference & Logs

- `agent-team/task-board-v1-archive.md` – Phase 1 (PieceTree Core) history.
- `agent-team/indexes/README.md` – canonical Info-Indexer changefeed.
- `agent-team/indexes/core-docs-index.md` – cross-document purpose/owner matrix.
- `docs/reports/migration-log.md` – full migration audit trail.
- `AGENTS.md` – cross-session log.
