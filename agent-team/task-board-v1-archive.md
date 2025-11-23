# Task Board

**Changefeed Reminder:** Read `agent-team/indexes/README.md#delta-2025-11-19` before editing any status or owner so Task Board changes stay aligned with the Info-Indexer delta feed.

## Core Delivery (PT/OI)

| ID | Description | Owner | Key Artifacts | runSubAgent Budget | Status | Latest Update |
| --- | --- | --- | --- | --- | --- | --- |
| PT-001 | Assess TS PieceTree dependencies | Main Agent | `ts/src/vs/editor/common/model/pieceTreeTextBuffer` | 1 | Done | `AGENTS.md#最新进展` (2025-11-19 feasibility log) |
| PT-002 | Draft AI Team process & templates | Main Agent | `agent-team/ai-team-playbook.md` | 1 | Done | `AGENTS.md#最新进展` (AI Team facilities log 2025-11-19) |
| PT-003 | Expand TS↔C# type mapping | Investigator-TS | `agent-team/type-mapping.md` | 1 | Done | `agent-team/type-mapping.md#piece` (2025-11-19 PT-003 deliverable logged) |
| PT-004 | Port RBTree skeleton to C# | Porter-CS | `src/TextBuffer/Core` | 2 | Done | `docs/reports/migration-log.md` (2025-11-19 PT-011 Verified) |
| PT-005 | Draft QA test matrix & perf plan | QA-Automation | `tests/TextBuffer.Tests/UnitTest1.cs`<br>`tests/TextBuffer.Tests/TestMatrix.md` | 1 | Done | `tests/TextBuffer.Tests/TestMatrix.md` (2025-11-19 PT-011 Verified) |
| PT-006 | Establish migration log & doc flow | DocMaintainer | `docs/sprints/sprint-org-self-improvement.md#milestone-c`<br>`docs/reports/consistency/consistency-report-20251119.md#findings` | 1 | Planned | `docs/reports/consistency/consistency-report-20251119.md#recommendations` (logging gap flagged) |
| PT-007 | Port Search Logic | Porter-CS | `src/TextBuffer/Core/PieceTreeSearcher.cs` | 2 | Done | `docs/reports/migration-log.md` (2025-11-19 PT-005.Search implemented) |
| PT-008 | Port Snapshot Logic | Porter-CS | `src/TextBuffer/Core/PieceTreeSnapshot.cs` | 1 | Done | `docs/reports/migration-log.md` (2025-11-19 PT-008.Snapshot implemented) |
| PT-009 | Line Content Optimization | Porter-CS | `src/TextBuffer/Core/PieceTreeModel.cs` | 1 | Done | `docs/reports/migration-log.md` (2025-11-19 PT-009.LineOpt implemented) |
| PT-010 | CRLF Normalization | Porter-CS | `src/TextBuffer/Core/PieceTreeModel.cs`<br>`src/TextBuffer/Core/ChunkUtilities.cs` | 2 | Done | `docs/reports/migration-log.md` (2025-11-19 PT-010 implemented) |
| PT-011 | Consolidate PT-004/PT-005 & Finalize v1 | QA-Automation | `tests/TextBuffer.Tests/TestMatrix.md` | 1 | Done | `docs/reports/migration-log.md` (2025-11-19 PT-011 Verified) |
| OI-001 | Audit core docs orthogonality | DocMaintainer + Info-Indexer | `docs/reports/consistency/consistency-report-20251119.md` | 1 | Done | `docs/sprints/sprint-org-self-improvement.md#backlog-snapshot` (Done 2025-11-19) |
| OI-002 | Create index catalog + first index | Info-Indexer | `agent-team/indexes/core-docs-index.md` | 1 | Done | `agent-team/indexes/README.md#delta-2025-11-19` (index creation logged) |
| OI-003 | Template runSubAgent inputs & checklist | Planner | `agent-team/main-loop-methodology.md`<br>`agent-team/ai-team-playbook.md` | 1 | Done | `docs/sprints/sprint-org-self-improvement.md#backlog-snapshot` (Done 2025-11-19) |
| OI-004 | Task Board compression & layering | DocMaintainer | `agent-team/task-board.md`<br>`agent-team/indexes/README.md#delta-2025-11-19` | 1 | Done | `agent-team/task-board.md` (layered delivery 2025-11-19) |
| OI-005 | Integrate Info-Indexer hooks into main loop | Main Agent | `agent-team/main-loop-methodology.md` | 1 | Done | `docs/sprints/sprint-org-self-improvement.md#backlog-snapshot` (Done 2025-11-19) |

## Reference & Logs

- `agent-team/indexes/README.md#delta-2025-11-19` – canonical Info-Indexer changefeed to consume before touching statuses.
- `agent-team/indexes/core-docs-index.md` – cross-document purpose/owner matrix referenced by PT/OI deliverables.
- `docs/reports/consistency/consistency-report-20251119.md` – audit evidence + follow-up queue for PT-006/OI-001/OI-004.
- `docs/sprints/sprint-00.md` – PT backlog scope, budgets, and demo checklist.
- `docs/sprints/sprint-org-self-improvement.md` – OI backlog, milestones, and acceptance criteria.
- `docs/meetings/meeting-20251119-org-self-improvement.md` – meeting-level decisions that feed these tasks.
- `AGENTS.md` – cross-session log capturing when PT/OI entries were last updated.
- `agent-team/main-loop-methodology.md` / `agent-team/ai-team-playbook.md` – templates + process hooks consuming this board.
- `docs/reports/migration-log.md` – full migration audit trail; cite the matching row plus `agent-team/indexes/README.md#delta-2025-11-19` in every status edit before saving.
