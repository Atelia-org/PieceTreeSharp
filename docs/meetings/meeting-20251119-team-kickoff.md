# Meeting: AI Team Kickoff
- **Date:** 2025-11-19
- **Participants:** Main Agent, Planner, Investigator-TS, Porter-CS, QA-Automation, DocMaintainer
- **Facilitator:** Main Agent
- **Objective:** 明确角色分工与近期交付，确定 runSubAgent 粒度计划

## Agenda
1. 角色确认
2. 核心工件回顾
3. Sprint-00 草案

## Notes
- **Context:** 需要在 PieceTree 迁移前建立可持续的 AI Team 流程。
- **Discussion Points:**
  - Star topology 要求所有跨 Agent 信息通过主 Agent 调度；离线信息写入共享文档。
  - runSubAgent 预算将绑定任务板条目，完成后更新状态。
  - 近期聚焦：类型映射、TS 依赖分析、C# 红黑树骨架、测试策略、文档同步。

## Decisions
| # | Decision | Owner | Related Files |
| --- | --- | --- | --- |
| 1 | 设立 Planner / Investigator-TS / Porter-CS / QA-Automation / DocMaintainer 五个角色 | Main Agent | agent-team/members/*.md |
| 2 | Sprint-00 覆盖“梳理依赖 + C# 骨架 + 文档同步” | Planner | docs/sprints/sprint-00.md |
| 3 | 使用 `agent-team/task-board.md` 管理 runSubAgent 粒度任务 | Planner | agent-team/task-board.md |

## Action Items (runSubAgent-granularity)
| Task | Example Prompt / Inputs | Assignee | Target File(s) | Status |
| --- | --- | --- | --- | --- |
| PT-003: 类型映射扩展 | "分析 pieceTreeBase 类型并更新 type-mapping.md" | Investigator-TS | agent-team/type-mapping.md | Planned |
| RBTree C# 骨架 | "按照 Investigator 输出实现 PieceTreeNode + RB tree" | Porter-CS | src/PieceTree.TextBuffer/Core | Planned |
| 测试策略草案 | "编写 QA 测试矩阵" | QA-Automation | src/PieceTree.TextBuffer.Tests | Planned |
| 迁移日志文档 | "创建 migration-log.md 并首批记录" | DocMaintainer | docs/ | Planned |

## Parking Lot
- 正则搜索(`Searcher`)是否需先抽象？等待 Investigator-TS 的分析报告。
