# Task Board - Phase 3: Diffing & Decorations

**Changefeed Reminder:** Read `agent-team/indexes/README.md` before editing any status or owner so Task Board changes stay aligned with the Info-Indexer delta feed.

## Diffing & Decorations (DF/OI)

| ID | Description | Owner | Key Artifacts | runSubAgent Budget | Status | Latest Update |
| --- | --- | --- | --- | --- | --- | --- |
| DF-001 | Analyze Diff & Decoration dependencies & update Type Mapping | Investigator-TS | `agent-team/type-mapping.md` | 1 | Done | `agent-team/handoffs/DF-001-Brief.md` created |
| DF-002 | Port `DiffComputer` / `DiffChange` (Myers Algorithm) | Porter-CS | `src/TextBuffer/Diff/DiffComputer.cs` | 2 | Done | `agent-team/handoffs/DF-002-Result.md` |
| DF-003 | Implement `ModelDecoration` structure & IntervalTree storage | Porter-CS | `src/TextBuffer/Decorations/IntervalTree.cs` | 2 | Done | `agent-team/handoffs/DF-003-Result.md` (List-based v1) |
| DF-004 | Implement `TextModel.GetDecorationsInRange` | Porter-CS | `src/TextBuffer/TextModel.cs` | 1 | Done | `agent-team/handoffs/DF-004-Result.md` |
| DF-005 | Prototype `MarkdownRenderer` (DocUI Core) | Porter-CS | `src/TextBuffer/Rendering/MarkdownRenderer.cs` | 2 | Done | `agent-team/handoffs/DF-005-Result.md` |
| OI-007 | Phase 3 Documentation & Indexing | Info-Indexer | `agent-team/indexes/core-docs-index.md` | 1 | Planned | - |

## Reference & Logs

- `agent-team/task-board-v2-archive.md` – Phase 2 (TextModel & Interaction) history.
- `agent-team/task-board-v1-archive.md` – Phase 1 (PieceTree Core) history.
- `agent-team/indexes/README.md` – canonical Info-Indexer changefeed.
- `docs/reports/migration-log.md` – full migration audit trail.
- `AGENTS.md` – cross-session log.
