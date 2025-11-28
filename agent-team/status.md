# Project Status Snapshot

> Team Leader 认知入口之一。只记录"现在在哪里"的快照指标，不记录待办事项（见 `todo.md`）。
> 每次 runSubAgent 完成或里程碑变化时更新。

## Test Baseline
- **Total:** 796 passed, 2 skipped
- **Command:** `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`
- **Last Verified:** 2025-11-28

## Current Phase & Sprint
- **Phase:** 8 – Alignment Remediation
- **Sprint:** 04 (2025-11-27 ~ 2025-12-12)
- **Milestone:** M1 (WS1~WS3 基础完成) ✅ → M2 (Cursor/Snippet/DocUI) 进行中

## Sprint 04 Workstream Progress
| WS | Focus | Status | Key Delta |
|----|-------|--------|-----------|
| WS1 | PieceTree Search Parity | ✅ Done | `#delta-2025-11-27-ws1-port-search-step12` |
| WS2 | Range/Selection Helpers | ✅ Done | `#delta-2025-11-26-ws2-port` |
| WS3 | IntervalTree Lazy Normalize | ✅ Done (Tree), TextModel Planned | `#delta-2025-11-26-ws3-tree` |
| WS4 | Cursor & Snippet | Core ✅, Collection ✅, Snippet Planned | `#delta-2025-11-28-sprint04-r13-r18` |
| WS5 | High-Risk Tests | ✅ Done (首批 45+WordOps 41) | `#delta-2025-11-28-ws5-wordoperations` |

## Active Changefeed Anchors
> 当前需要关注的 changefeed（完整列表见 `agent-team/indexes/README.md`）

- `#delta-2025-11-28-sprint04-r13-r18` – CL7 Stage1, CursorCollection, AtomicTabMove
- `#delta-2025-11-28-ws5-wordoperations` – WordOperations 全量 + 41 tests
- `#delta-2025-11-28-cl8-phase34` – MarkdownRenderer + enums (30 tests)
- `#delta-2025-11-26-aa4-cl7-cursor-core` – CL7 Stage2 placeholder (WordOps/Snippet backlog)
- `#delta-2025-11-26-aa4-cl8-markdown` – CL8 DocUI Intl/decoration placeholder

## Key References
- Sprint Log: [`docs/sprints/sprint-04.md`](../docs/sprints/sprint-04.md)
- Task Board: [`agent-team/task-board.md`](task-board.md)
- Migration Log: [`docs/reports/migration-log.md`](../docs/reports/migration-log.md)
- Test Matrix: [`tests/TextBuffer.Tests/TestMatrix.md`](../tests/TextBuffer.Tests/TestMatrix.md)
