# Project Status Snapshot

> Team Leader 认知入口之一。只记录"现在在哪里"的快照指标，不记录待办事项（见 `todo.md`）。
> 每次 runSubAgent 完成或里程碑变化时更新。

## Test Baseline
- **Total:** 807 passed, 5 skipped
- **Command:** `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`
- **Last Verified:** 2025-11-30

## Current Phase & Sprint
- **Phase:** 8 – Alignment Remediation
- **Sprint:** 04 (2025-11-27 ~ 2025-12-12)
- **Milestone:** M1 (WS1~WS3 基础完成) ✅ → M2 (Cursor/Snippet/DocUI) 进行中

## AI Team 技术状态
- **协作架构:** ✅ CustomAgent + 持久认知完整 (`.github/agents/` 9 agents + `agent-team/members/` 8 files)
- **模型多样性:** Claude Opus 4.5 (主力) + GPT-5.1-Codex (审查) + Gemini 3 Pro (顾问)
- **CustomAgent 验证:** ✅ 2025-12-01 团队谈话全员通过（8/8 成员正常响应）
- **输出顺序纪律:** ✅ 已修复 + 优化（保留 CoT 思维链，只约束最终汇报）
- **半上下文压缩:** ✅ 实战验证成功（2025-12-01 团队谈话期间无感知认知断裂）
- **记忆维护纪律:** ✅ 所有 Agent 都有汇报前保存认知的规范
- **决策方法论:** ✅ Planner 多采样 + "先事实-后分析-再观点" 思维纪律
- **团队重组研究:** 🔄 观察期 — InfoIndexer/DocMaintainer 合并待评估

## Sprint 04 Workstream Progress
| WS | Focus | Status | Key Delta |
|----|-------|--------|-----------|
| WS1 | PieceTree Search Parity | ✅ Done | `#delta-2025-11-27-ws1-port-search-step12` |
| WS2 | Range/Selection Helpers | ✅ Done | `#delta-2025-11-26-ws2-port` |
| WS3 | IntervalTree Lazy Normalize | ✅ Done (Tree), TextModel Planned | `#delta-2025-11-26-ws3-tree` |
| WS4 | Cursor & Snippet | Core ✅, Collection ✅, Snippet +9 Tests | `#delta-2025-11-30-snippet-tests` |
| WS5 | High-Risk Tests | ✅ Done (首批 45+WordOps 41) | `#delta-2025-11-28-ws5-wordoperations` |

## Active Changefeed Anchors
> 当前需要关注的 changefeed（完整列表见 `agent-team/indexes/README.md`）

- `#delta-2025-11-30-snippet-tests` – Snippet 测试增强 (+9 tests, empty placeholder fix)
- `#delta-2025-11-28-sprint04-r13-r18` – CL7 Stage1, CursorCollection, AtomicTabMove
- `#delta-2025-11-28-ws5-wordoperations` – WordOperations 全量 + 41 tests
- `#delta-2025-11-28-cl8-phase34` – MarkdownRenderer + enums (30 tests)

## Key References
- Sprint Log: [`docs/sprints/sprint-04.md`](../docs/sprints/sprint-04.md)
- Task Board: [`agent-team/task-board.md`](task-board.md)
- Migration Log: [`docs/reports/migration-log.md`](../docs/reports/migration-log.md)
- Test Matrix: [`tests/TextBuffer.Tests/TestMatrix.md`](../tests/TextBuffer.Tests/TestMatrix.md)
