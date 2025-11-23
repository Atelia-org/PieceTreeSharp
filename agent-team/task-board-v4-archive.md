# Task Board - Phase 4: Alignment & Audit

**Changefeed Reminder:** Read `agent-team/indexes/README.md` before editing any status or owner so Task Board changes stay aligned with the Info-Indexer delta feed.

## Alignment & Audit (AA/OI)

| ID | Description | Owner | Key Artifacts | runSubAgent Budget | Status | Latest Update |
| --- | --- | --- | --- | --- | --- | --- |
| AA-001 | Audit Core PieceTree (RBTree, Cache, Optimizations) | Investigator-TS | `src/TextBuffer/Core/PieceTreeModel.cs` | 2 | Done | `agent-team/handoffs/AA-001-Audit.md` |
| AA-002 | Audit TextModel & Buffers (Line starts, BOM/EOL, Versioning) | Investigator-TS | `src/TextBuffer/TextModel.cs` | 2 | Done | `agent-team/handoffs/AA-002-Audit.md` |
| AA-003 | Audit Search & Interaction (Regex, Word separators, Cursor) | Investigator-TS | `src/TextBuffer/Core/PieceTreeSearcher.cs`<br>`src/TextBuffer/TextModelSearch.cs` | 2 | Done | `agent-team/handoffs/AA-003-Audit.md` |
| AA-004 | Audit Diff & Decorations (Myers, IntervalTree vs List) | Investigator-TS | `src/TextBuffer/Diff/DiffComputer.cs` | 2 | Done | `agent-team/handoffs/AA-004-Audit.md` |
| AA-005 | Remediation - Core & Model (Fix gaps from AA-001/002) | Porter-CS | `src/TextBuffer/Core` | 2 | Done | `agent-team/handoffs/AA-005-Result.md` |
| AA-006 | Remediation - Search & Features (Fix gaps from AA-003/004) | Porter-CS | `src/TextBuffer/Core/PieceTreeSearcher.cs`<br>`src/TextBuffer/TextModelSearch.cs`<br>`src/TextBuffer/Decorations` | 2 | Done | `agent-team/handoffs/AA-006-Result.md` |
| OI-008 | Phase 4 Documentation & Indexing | Info-Indexer | `agent-team/indexes/core-docs-index.md` | 1 | Planned | - |

## Reference & Logs

- `agent-team/task-board-v3-archive.md` – Phase 3 (Diffing & Decorations) history.
- `agent-team/task-board-v2-archive.md` – Phase 2 (TextModel & Interaction) history.
- `agent-team/task-board-v1-archive.md` – Phase 1 (PieceTree Core) history.
- `agent-team/indexes/README.md` – canonical Info-Indexer changefeed.
- `docs/reports/migration-log.md` – full migration audit trail.
- `AGENTS.md` – cross-session log.
