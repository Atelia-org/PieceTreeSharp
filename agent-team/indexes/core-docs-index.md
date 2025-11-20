# Core Docs Index

## Goal
- 为主 Agent 与 DocMaintainer 提供核心文档的一览表，说明各自职责、负责人与更新时间，降低 LoadContext 阶段的查阅成本。
- 在组织自我完善 Sprint（OI 系列）期间作为压缩对照表，指示哪些信息已被索引、哪些仍需在原文件中维护。

## Source Docs
- `AGENTS.md` - 跨会话时间线与团队状态说明。
- `docs/sprints/sprint-00.md` - Sprint 00 的目标、任务与风险。
- `docs/sprints/sprint-org-self-improvement.md` - Sprint OI-01（组织自我完善）的计划。
- `docs/sprints/sprint-01.md` - Alignment & Audit R3（AA3）冲刺的范围、成功标准与 changefeed 对齐点。
- `docs/meetings/meeting-20251119-team-kickoff.md` - AI Team 启动会议纪要。
- `docs/meetings/meeting-20251119-org-self-improvement.md` - 组织自我完善会议纪要。
- `agent-team/task-board.md` - runSubAgent 粒度任务状态板。
- `agent-team/main-loop-methodology.md` - 主循环流程与 DocMaintainer/Info-Indexer 钩子。
- `agent-team/ai-team-playbook.md` - 团队行动守则与协作模板。

## Summary Table
| Doc | Purpose | Owner | Last Updated | Path |
| --- | --- | --- | --- | --- |
| AGENTS.md | 记录跨会话记忆、里程碑与工具约束，用作全局时间线与工作记忆入口。 | Main Agent / DocMaintainer | 2025-11-20 | `AGENTS.md` |
| Sprint 00 | 定义 PieceTree 基建冲刺的范围、成功标准与风险缓解。 | Planner | 2025-11-19 | `docs/sprints/sprint-00.md` |
| Sprint OI-01 | 指挥组织自我完善行动（OI-001~OI-005），列出交付物与校验点。 | Planner（协同 Info-Indexer） | 2025-11-19 | `docs/sprints/sprint-org-self-improvement.md` |
| Sprint 01 | 记录 Alignment & Audit R3（AA3）的范围、成功标准与 changefeed 对齐点。 | Planner（协同 Info-Indexer） | 2025-11-20 | `docs/sprints/sprint-01.md` |
| AI Team Kickoff Meeting | 首次团队启动会议记录，明确角色、决定与行动项。 | Main Agent | 2025-11-19 | `docs/meetings/meeting-20251119-team-kickoff.md` |
| Org Self-Improvement Meeting | OI Sprint 启动会议记录，新增角色与索引策略。 | Main Agent（Info-Indexer 参与） | 2025-11-19 | `docs/meetings/meeting-20251119-org-self-improvement.md` |
| Task Board | 追踪 PT/OI 任务状态、runSubAgent 预算与备注，是工作调度事实表。 | Planner（各 Owner 更新） | 2025-11-20 | `agent-team/task-board.md` |
| Main Loop Methodology | 主循环执行手册，列出 Info-Indexer/DocMaintainer 集成节点与 checklists。 | Main Agent | 2025-11-19 | `agent-team/main-loop-methodology.md` |
| AI Team Playbook | 协作原则、核心工件列表与 runSubAgent 工作流指导。 | Main Agent | 2025-11-19 | `agent-team/ai-team-playbook.md` |

## Gaps & Actions
- **OI-001**（DocMaintainer + Info-Indexer）：利用本索引过一遍核心文档正交性，生成缺口/重复表并抄送 `docs/reports/consistency/`（待 DocMaintainer 创建）。
- **OI-003**（Planner）：在 runSubAgent 模板中加入 `Indexing Hooks` 段并链接本表，确保每次调用都声明受影响的核心文档。
- **OI-004**（DocMaintainer）：重构 `agent-team/task-board.md` 为“核心 / 参考”两段，并在 Notes 栏引用索引条目替代长描述。
- **DocMaintainer Follow-up：** 在压缩 CL4 相关描述时同步引用 `agent-team/handoffs/AA3-009-QA.md`，以便 AGENTS / Sprint 01 / Task Board 的 AA3-009 文案都指向同一 QA 证据与 changefeed 条目。

## Update Log
- **2025-11-19 - OI-002 / Info-Indexer：** 创建 `core-docs-index.md` v0，登记 8 个核心 文档的目的/负责人/更新时间，并准备交付给 DocMaintainer 作为 OI-001 输入。今后每次核心文档压缩后更新此表与 README Delta。
- **2025-11-20 - OI-010 / Info-Indexer：** 更新 AGENTS / Sprint 01 / Task Board 行的时间戳，并新增 Sprint 01 条目 + DocMaintainer QA 引用，支撑 AA3-009 changefeed 对齐。
