# Task Board - Phase 4: Alignment & Audit

**Changefeed Reminder:** Read `agent-team/indexes/README.md` before editing any status or owner so Task Board changes stay aligned with the Info-Indexer delta feed.

## Alignment & Audit (AA/OI)

| ID | Description | Owner | Key Artifacts | runSubAgent Budget | Status | Latest Update |
| --- | --- | --- | --- | --- | --- | --- |
| AA-001 | Audit Core PieceTree (RBTree, Cache, Optimizations) | Investigator-TS | `src/PieceTree.TextBuffer/PieceTreeModel.cs` | 2 | Planned | - |
| AA-002 | Audit TextModel & Buffers (Line starts, BOM/EOL, Versioning) | Investigator-TS | `src/PieceTree.TextBuffer/TextModel.cs` | 2 | Planned | - |
| AA-003 | Audit Search & Interaction (Regex, Word separators, Cursor) | Investigator-TS | `src/PieceTree.TextBuffer/Search/PieceTreeSearcher.cs` | 2 | Planned | - |
| AA-004 | Audit Diff & Decorations (Myers, IntervalTree vs List) | Investigator-TS | `src/PieceTree.TextBuffer/Diff/DiffComputer.cs` | 2 | Planned | - |
| AA-005 | Remediation - Core & Model (Fix high-priority gaps from AA-001/002) | Porter-CS | `src/PieceTree.TextBuffer/` | 3 | Planned | - |
| AA-006 | Remediation - Search & Features (Fix high-priority gaps from AA-003/004) | Porter-CS | `src/PieceTree.TextBuffer/` | 3 | Planned | - |
| OI-008 | Phase 4 Documentation & Indexing | Info-Indexer | `agent-team/indexes/core-docs-index.md` | 1 | Planned | - |

## Reference & Logs

- `agent-team/task-board-v3-archive.md` – Phase 3 (Diffing & Decorations) history.
- `agent-team/task-board-v2-archive.md` – Phase 2 (TextModel & Interaction) history.
- `agent-team/task-board-v1-archive.md` – Phase 1 (PieceTree Core) history.
- `agent-team/indexes/README.md` – canonical Info-Indexer changefeed.
- `docs/reports/migration-log.md` – full migration audit trail.
- `AGENTS.md` – cross-session log.
