# Meeting: Org Self-Improvement Kickoff
- **Date:** 2025-11-19
- **Participants:** Main Agent, Planner, Investigator-TS, Porter-CS, QA-Automation, DocMaintainer, Info-Indexer (new role proposal)
- **Facilitator:** Main Agent
- **Objective:** 审视基础设施、评估职责划分、决定文档治理方案并形成组织自我完善 sprint

## Agenda
1. 角色与岗位评估
2. 核心文档正交性检查与压缩策略
3. 主循环与 DocMaintainer 协作流程
4. 形成“组织自我完善 Sprint”

## Notes
- **结构评估**：现有 5 角色覆盖规划、调研、实现、测试、文档，但文档相关任务持续增长，DocMaintainer 容量不足。
- **岗位调整**：新增 Info-Indexer 负责信息索引、摘要与知识压缩，DocMaintainer 聚焦核心文档一致性。
- **文档正交性**：
  - `AGENTS.md`：里程碑时间线
  - `agent-team/main-loop-methodology.md`：流程定义
  - `docs/meetings/*.md`：瞬时讨论
  - `docs/sprints/*.md`：短期目标
  - 冗余风险：成员记忆与 Task Board 描述重复。决定让 Info-Indexer 维护 `agent-team/indexes/` 概览以减少重复描述。
- **流程优化**：
  - 在主循环 checklist 中增设“DocMaintainer 调用确认”以及“Info-Indexer 更新索引”步骤。
  - 对 runSubAgent 调用统一提供输入模板片段，由 Planner 主导维护。

## Decisions
| # | Decision | Owner | Related Files |
| --- | --- | --- | --- |
| 1 | 新设 `Info-Indexer` 角色，负责索引/摘要/压缩，DocMaintainer 专注核心文档一致性 | Main Agent | agent-team/members/info-indexer.md |
| 2 | 启动“组织自我完善 Sprint”以执行结构改进和文档治理 | Planner | docs/sprints/sprint-org-self-improvement.md |
| 3 | Task Board 增加 OI 系列任务，runSubAgent 粒度跟踪组织改进 | Planner | agent-team/task-board.md |
| 4 | 在主循环中加入 Info-Indexer 钩子（LoadContext 后、Broadcast 前） | Main Agent | agent-team/main-loop-methodology.md |

## Action Items (runSubAgent-granularity)
| Task | Example Prompt / Inputs | Assignee | Target File(s) | Status |
| --- | --- | --- | --- | --- |
| OI-001 文档正交性审计 | "审阅现有核心文档，输出重复/缺口列表" | DocMaintainer + Info-Indexer | docs/meetings, docs/sprints, AGENTS.md | Planned |
| OI-002 索引与摘要体系 | "创建 indexes/README + 首个索引" | Info-Indexer | agent-team/indexes | Planned |
| OI-003 流程模板化 | "完善 runSubAgent 输入模板与流程指南" | Planner | agent-team/main-loop-methodology.md | Planned |
| OI-004 任务板压缩策略 | "提出 Task Board 精简方案并实施" | DocMaintainer | agent-team/task-board.md | Planned |

## Parking Lot
- 是否需要自动化脚本辅助文档一致性检查？待 Ops/Tooling 能力具备后评估。
- 深入正则/搜索模块移植前，需先完成组织自我完善 sprint，确保流程稳定。
