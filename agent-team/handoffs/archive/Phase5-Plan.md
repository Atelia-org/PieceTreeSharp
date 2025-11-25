# Task Board - Phase 5: Alignment & Audit R2

**Goal:** Run a second-pass audit/remediation cycle for TS parity, catching any missed advanced scenarios (regression coverage, TS feature parity, and high-risk edge cases).

**Changefeed Reminder:** Read `agent-team/indexes/README.md` before editing any status or owner so Task Board changes stay aligned with the Info-Indexer delta feed.

## Alignment & Audit R2 (AA2/OI)

| ID | Description | Owner | Key Artifacts | runSubAgent Budget | Status | Latest Update |
| --- | --- | --- | --- | --- | --- | --- |
| AA2-001 | Audit PieceTree advanced cases (split deletions, multi-line edits, metadata invariants) | Investigator-TS | `src/PieceTree.TextBuffer/PieceTreeModel.cs`<br>`src/PieceTree.TextBuffer/Operations` | 2 | Planned | - |
| AA2-002 | Audit TextModel undo/redo hooks, language configuration (tab size, indent), and EOL toggles | Investigator-TS | `src/PieceTree.TextBuffer/TextModel.cs`<br>`src/PieceTree.TextBuffer/TextModelUndoRedo.cs` | 2 | Planned | - |
| AA2-003 | Audit Search advanced features (regex captures, backreferences, Unicode word boundaries) | Investigator-TS | `src/PieceTree.TextBuffer/Search/PieceTreeSearcher.cs` | 2 | Planned | - |
| AA2-004 | Audit Diff prettify logic and decoration stickiness vs TS | Investigator-TS | `src/PieceTree.TextBuffer/Diff`<br>`src/PieceTree.TextBuffer/Decorations` | 2 | Planned | - |
| AA2-005 | Remediate core issues surfaced in AA2-001/002 | Porter-CS | `src/PieceTree.TextBuffer/Core` | 3 | Planned | - |
| AA2-006 | Remediate feature issues surfaced in AA2-003/004 | Porter-CS | `src/PieceTree.TextBuffer/Search`<br>`src/PieceTree.TextBuffer/Diff` | 3 | Planned | - |
| OI-009 | Update documentation/indexes for Phase 5 (audit deltas, parity notes) | Info-Indexer | `agent-team/indexes/`<br>`docs/reports/` | 1 | Planned | - |

## Reference & Logs

- `agent-team/task-board-v4-archive.md` – Phase 4 (Alignment & Audit) history.
- `agent-team/task-board-v3-archive.md` – Phase 3 (Diffing & Decorations) history.
- `agent-team/task-board-v2-archive.md` – Phase 2 (TextModel & Interaction) history.
- `agent-team/task-board-v1-archive.md` – Phase 1 (PieceTree Core) history.
- `agent-team/indexes/README.md` – canonical Info-Indexer changefeed.
- `docs/reports/migration-log.md` – full migration audit trail.
- `AGENTS.md` – cross-session log.
