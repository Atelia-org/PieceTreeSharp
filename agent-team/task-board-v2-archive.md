# Task Board - Phase 2: TextModel & Interaction

**Changefeed Reminder:** Read `agent-team/indexes/README.md` before editing any status or owner so Task Board changes stay aligned with the Info-Indexer delta feed.

## TextModel & Interaction (TM/OI)

| ID | Description | Owner | Key Artifacts | runSubAgent Budget | Status | Latest Update |
| --- | --- | --- | --- | --- | --- | --- |
| TM-001 | Analyze TextModel dependencies & update Type Mapping | Investigator-TS | `agent-team/type-mapping.md` | 1 | Done | `agent-team/handoffs/TM-001-Brief.md` created |
| TM-002 | Port `TextModel` skeleton (Events, Version ID) | Porter-CS | `src/TextBuffer/TextModel.cs` | 2 | Done | `agent-team/handoffs/TM-002-Result.md` |
| TM-003 | Port `Selection` / `Position` math | Porter-CS | `src/TextBuffer/TextPosition.cs`<br>`src/TextBuffer/Core/Selection.cs` | 1 | Done | Included in TM-002 |
| TM-004 | Implement `onDidChangeContent` event firing | Porter-CS | `src/TextBuffer/TextModel.cs` | 2 | Done | Included in TM-002 |
| TM-005 | Basic Cursor support (Single cursor) | Porter-CS | `src/TextBuffer/Cursor/Cursor.cs` | 2 | Done | `agent-team/handoffs/TM-005-Result.md` |
| OI-006 | Phase 2 Documentation & Indexing | Info-Indexer | `agent-team/indexes/core-docs-index.md` | 1 | Planned | - |

## Reference & Logs

- `agent-team/task-board-v1-archive.md` – Phase 1 (PieceTree Core) history.
- `agent-team/indexes/README.md` – canonical Info-Indexer changefeed.
- `agent-team/indexes/core-docs-index.md` – cross-document purpose/owner matrix.
- `docs/reports/migration-log.md` – full migration audit trail.
- `AGENTS.md` – cross-session log.
