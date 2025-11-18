# Task Board

| ID | Description | Owner | Files / Artifacts | runSubAgent Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| PT-001 | Assess TS PieceTree dependencies | Main Agent | ts/src/vs/editor/common/model/pieceTreeTextBuffer | 1 | Done | Summary in AGENTS.md |
| PT-002 | Draft AI Team process & templates | Main Agent | agent-team/, docs/meetings, docs/sprints | 1 | Done | Playbook + templates committed |
| PT-003 | Expand TS↔C# type mapping | Investigator-TS | agent-team/type-mapping.md | 1 | In Progress | Needs pieceTreeBase coverage |
| PT-004 | Port RBTree skeleton to C# | Porter-CS | src/PieceTree.TextBuffer/Core | 2 | Planned | Depends on PT-003 insights |
| PT-005 | Draft QA test matrix & perf plan | QA-Automation | src/PieceTree.TextBuffer.Tests | 1 | Planned | Outline coverage + benchmarks |
| PT-006 | Establish migration log & doc flow | DocMaintainer | docs/, AGENTS.md | 1 | Planned | Ensure every change logged |
| OI-001 | Audit core docs orthogonality | DocMaintainer + Info-Indexer | AGENTS.md, docs/sprints, docs/meetings | 1 | Planned | Output action list |
| OI-002 | Create index catalog + first index | Info-Indexer | agent-team/indexes | 1 | Planned | Reduce duplication |
| OI-003 | Template runSubAgent inputs & checklist | Planner | agent-team/main-loop-methodology.md | 1 | Planned | Provide reusable snippet |
| OI-004 | Task Board compression & layering | DocMaintainer | agent-team/task-board.md | 1 | Planned | Keep table manageable |
| OI-005 | Integrate Info-Indexer hooks into main loop | Main Agent | agent-team/main-loop-methodology.md | 1 | Done | Hooks added 2025-11-19 |
