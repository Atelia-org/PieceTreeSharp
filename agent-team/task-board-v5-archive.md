# Task Board - Phase 5: Alignment & Audit R2

**Goal:** Run a second-pass audit/remediation cycle for TS parity, catching any missed advanced scenarios (regression coverage, TS feature parity, and high-risk edge cases).

**Changefeed Reminder:** Read `agent-team/indexes/README.md` before editing any status or owner so Task Board changes stay aligned with the Info-Indexer delta feed.

## Alignment & Audit R2 (AA2/OI)

| ID | Description | Owner | Key Artifacts | runSubAgent Budget | Status | Latest Update |
| --- | --- | --- | --- | --- | --- | --- |
| AA2-001 | Audit PieceTree advanced cases (split deletions, multi-line edits, metadata invariants) | Investigator-TS | `src/TextBuffer/Core/PieceTreeModel.cs`<br>`src/TextBuffer/Core/PieceTreeModel.Edit.cs` | 2 | Done | `agent-team/handoffs/AA2-001-Audit.md` |
| AA2-002 | Audit TextModel undo/redo hooks, language configuration (tab size, indent), and EOL toggles | Investigator-TS | `src/TextBuffer/TextModel.cs`<br>`src/TextBuffer/EditStack.cs`<br>`src/TextBuffer/Services/IUndoRedoService.cs` | 2 | Done | `agent-team/handoffs/AA2-002-Audit.md` |
| AA2-003 | Audit Search advanced features (regex captures, backreferences, Unicode word boundaries) | Investigator-TS | `src/TextBuffer/Core/PieceTreeSearcher.cs`<br>`src/TextBuffer/TextModelSearch.cs` | 2 | Done | `agent-team/handoffs/AA2-003-Audit.md` |
| AA2-004 | Audit Diff prettify logic and decoration stickiness vs TS | Investigator-TS | `src/TextBuffer/Diff`<br>`src/TextBuffer/Decorations` | 2 | Done | `agent-team/handoffs/AA2-004-Audit.md` |
| AA2-005 | Remediate core issues surfaced in AA2-001/002 | Porter-CS | `src/TextBuffer/Core` | 3 | Done | CRLF/metadata/cache 修复与 TextModel undo/redo + options/EOL API 已落地，详见 [`docs/reports/migration-log.md`](../docs/reports/migration-log.md)（AA2-005）。|
| AA2-006 | Remediate feature issues surfaced in AA2-003/004 | Porter-CS | `src/TextBuffer/Core/PieceTreeSearcher.cs`<br>`src/TextBuffer/TextModelSearch.cs`<br>`src/TextBuffer/Diff`<br>`src/TextBuffer/Decorations` | 3 | Done | Search/diff/decor parity landed（详见 [`docs/reports/migration-log.md`](../docs/reports/migration-log.md) AA2-006 + [`agent-team/indexes/README.md#delta-2025-11-20`](indexes/README.md#delta-2025-11-20)；`dotnet test ...` 71/71）。 |
| OI-009 | Update documentation/indexes for Phase 5 (audit deltas, parity notes) | Info-Indexer | `agent-team/indexes/`<br>`docs/reports/` | 1 | Planned | - |

## Reference & Logs

- `agent-team/task-board-v4-archive.md` – Phase 4 (Alignment & Audit) history.
- `agent-team/task-board-v3-archive.md` – Phase 3 (Diffing & Decorations) history.
- `agent-team/task-board-v2-archive.md` – Phase 2 (TextModel & Interaction) history.
- `agent-team/task-board-v1-archive.md` – Phase 1 (PieceTree Core) history.
- `agent-team/indexes/README.md` – canonical Info-Indexer changefeed.
- `docs/reports/migration-log.md` – full migration audit trail.
- `AGENTS.md` – cross-session log.
