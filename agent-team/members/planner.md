# Planner Memory

## Role & Mission
- **Focus Area:** Roadmapping、任务分解、`runSubAgent` 粒度排期
- **Primary Deliverables:** 更新 `agent-team/task-board.md`、`docs/sprints/*.md`、会议议程
- **Key Stakeholders:** Investigator-TS、Porter-CS、QA-Automation、DocMaintainer

## Onboarding Summary
- 复盘 `AGENTS.md`、Sprint-00、Sprint OI-01 与两份 2025-11-19 会议纪要，确认当前计划分为 PieceTree 端口与组织改进双轨推进。
- 明确 Planner 需驱动 PT-003 依赖规划与 OI-003 模板化任务，并保持任务板 / Sprint 文档的 runSubAgent 预算同步。
- 即刻优先事项：完成 runSubAgent 输入模板草稿、跟踪类型映射交付节奏，并协调 DocMaintainer + Info-Indexer 输出 OI-001 结果以喂给 Task Board。

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| Backlog & Status | agent-team/task-board.md | 唯一来源，记录 PT/OI 任务与 runSubAgent 预算，Planner 更新节奏需紧贴交付。 |
| Sprint-00 (PieceTree) | docs/sprints/sprint-00.md | 关注 RB Tree / 类型映射交付， Investigator / Porter 输出后立即同步。 |
| Sprint OI-01 | docs/sprints/sprint-org-self-improvement.md | 组织改进目标与行动项，指导 OI-003/OI-004。 |
| Process & Templates | agent-team/ai-team-playbook.md, agent-team/main-loop-methodology.md | runSubAgent 输入模板与主循环 checklist 的落地点，Planner 维护。 |
| Meetings & Decisions | docs/meetings/meeting-20251119-team-kickoff.md, docs/meetings/meeting-20251119-org-self-improvement.md | 最近决策与行动项来源，更新 Task Board / Sprint 时引用。 |
| Global Milestones | AGENTS.md | 完成阶段性计划调整后将成果写入此时间线。 |

## Worklog
- **Last Update:** 2025-11-19
- **Recent Actions (2025-11-19):**
  - 落地 OI-003：在 `agent-team/main-loop-methodology.md` 新增 `runSubAgent Input Template`，引入 ContextSeeds/Objectives/Dependencies/... 结构并强调 changefeed 钩子。
  - 更新 `agent-team/ai-team-playbook.md`，让 Core Artifacts + Workflow 显式引用模板与 `agent-team/indexes/README.md#delta-20251119` checkpoint。
  - 调整 `agent-team/templates/subagent-memory-template.md`，要求 SubAgent 在 Knowledge Index / Worklog 记录消费或产出的 changefeed 与索引引用。
  - 复核 Sprint OI-01 与审计要求，确认 Planner 记忆已捕捉新模板依赖与 Info-Indexer handoff。

## Upcoming Goals (runSubAgent-sized)
1. **OI-003 – Adoption Pass:** 验证各 SubAgent 在下一轮调用中使用新模板并标记 changefeed checkpoint；收集反馈准备后续迭代。
2. **OI-001/OI-004 接口落地：** 与 DocMaintainer、Info-Indexer 共建“索引输入 -> Task Board 精简”流水线，确保审计结果能直接转化为 backlog 调整提案。
3. **PT-003 – Sprint 对齐：** 等待 Investigator-TS 的类型映射扩展后，更新 `docs/sprints/sprint-00.md` 与 `agent-team/task-board.md` 的依赖、预算与节奏。

## Blocking Issues
- 仍需 Investigator-TS 汇总 PieceTree 类型映射/依赖输出，方能重新评估 PT-004 时间表与 runSubAgent 预算。

## Hand-off Checklist
1. Backlog、会议、sprint 文档都已更新。
2. Tests or validations performed? N/A（规划类任务）
3. 下一位接手者请查看 `agent-team/task-board.md` 的最新时间戳。
