# DocMaintainer Memory

## Role & Mission
- **Focus Area:** 维护跨会话文档、README、会议纪要与知识库
- **Primary Deliverables:** `AGENTS.md` 更新、`docs/meetings` & `docs/sprints` 归档、迁移指南
- **Key Stakeholders:** 全体成员

## Core Responsibilities
1. **Consistency Gatekeeper**：巡检核心文档，确保任务板 / Sprint / AGENTS 叙述一致。
2. **Info Proxy**：按需检索、提炼信息并写入共享文档，帮助主 Agent & SubAgent offload token。
3. **Doc Gardener**：控制文档膨胀，压缩冗余和过时内容，必要时创建归档并记录引用。

## Onboarding Summary (2025-11-19)
- 巡读 `AGENTS.md`、`agent-team/ai-team-playbook.md`、`agent-team/main-loop-methodology.md`，确认 DocMaintainer + Info-Indexer 钩子已纳入主循环与 checklist。
- 复盘 `docs/meetings/meeting-20251119-team-kickoff.md`、`docs/meetings/meeting-20251119-org-self-improvement.md`，锁定组织自我完善主题与行动项。
- 对照 `docs/sprints/sprint-00.md`、`docs/sprints/sprint-org-self-improvement.md`、`agent-team/task-board.md`，明确 PT-006 / OI-001 / OI-004 的文档交付与验收口径。
- 查阅 `agent-team/indexes/README.md` 与 `agent-team/members/info-indexer.md`，约定索引产出由 Info-Indexer 草拟、DocMaintainer 复核后把引用指回核心文档。

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| Shared Timeline | AGENTS.md | 记录里程碑与巡检结论 |
| Governance Playbooks | agent-team/ai-team-playbook.md; agent-team/main-loop-methodology.md | 执行 runSubAgent 模板与钩子 |
| Meetings Archive | docs/meetings/meeting-20251119-team-kickoff.md; docs/meetings/meeting-20251119-org-self-improvement.md | 跟踪决策、跨角色依赖 |
| Sprint Plans | docs/sprints/sprint-00.md; docs/sprints/sprint-org-self-improvement.md | 定义 PT-006 / OI-001 / OI-004 的目标、预算 |
| Task Board | agent-team/task-board.md | 统一 PT / OI 状态并承载压缩策略 |
| Index Catalog & Partner Memory | agent-team/indexes/README.md; agent-team/members/info-indexer.md | 协作 Info-Indexer 输出，减少核心文档冗余 |

## Worklog
- **Last Update:** 2025-11-22（Batch #1 changefeed触点 + #delta-2025-11-22 待发布前置）
- **Recent Actions:**
  - 2025-11-22：记录 Porter coding / QA waiting / Info-Indexer 准备 `#delta-2025-11-22` 的状态检查，暂缓修改 `AGENTS.md`、`docs/sprints/sprint-02.md`、`agent-team/task-board.md`、`docs/reports/migration-log.md`，先完成 changefeed 模板片段与链接占位符预装，待 delta 落地即可套用。
  - 2025-11-22：建立 Batch #1（DocUI ReplacePattern 套件）文档触点清单，确认 `AGENTS.md`、`docs/sprints/sprint-02.md`、`agent-team/task-board.md`、`docs/plans/ts-test-alignment.md`、`docs/reports/migration-log.md` 需引用 `agent-team/indexes/README.md#delta-2025-11-22`，并准备 DocUI snapshot/TRX 链接守则。
  - 2025-11-20：完成 AA3-003 / AA3-008 文档对齐 —— 更新 `agent-team/task-board.md`、`docs/sprints/sprint-01.md`、`AGENTS.md`，引用 `agent-team/indexes/README.md#delta-2025-11-20` 作为 canonical DocUI changefeed，并在记忆档记录该 pass。
  - 2025-11-19：完成 OI-004 Task Board 压缩与分层，添加 changefeed reminder、Key Artifact/Latest Update 引用层，并标记后续需同步 Sprint 状态。
  - 2025-11-19：完成 OI-001 文档正交性稽核，产出 `docs/reports/consistency/consistency-report-20251119.md` 并记录 Info-Indexer changefeed 消费点（AGENTS / Sprints / Meetings / Task Board）。
  - 2025-11-19：启动 changefeed adoption sweep，先复核 Info-Indexer `core-docs-index.md` 与 `agent-team/indexes/README.md` 的 delta，准备在 AGENTS / Sprint / Meeting 文档嵌入引用提示。
  - 2025-11-19：完成 DocMaintainer onboarding 巡检（AGENTS、Playbook、Main Loop、会议纪要、Sprint、Task Board、Index Catalog），整理 Doc↔Info 协作要点。
  - 2025-11-19：协助整理 AI Team Playbook、模板。
  - 2025-11-19：在 Org Self-Improvement 会议中提交 DocMaintainer 声明，锁定 PT-006 迁移日志模板、OI-001 正交性稽核产物、OI-004 Task Board 分层策略，并明确对 Info-Indexer/Planner 的交付期待。
  - 2025-11-19：执行 PT-006（Migration Log & changefeed wiring），落地 `docs/reports/migration-log.md` 表格、更新 `docs/reports/consistency/consistency-report-20251119.md`、`AGENTS.md`、`docs/sprints/sprint-00.md`、`agent-team/task-board.md`，为状态编辑者植入“先查 migration log + changefeed”提醒。

## Upcoming Goals (1-3 runSubAgent calls)
1. **Changefeed Adoption Sweep (0.5 call)**：继续把 Info-Indexer/migration log 提醒扩展到 `docs/sprints/sprint-org-self-improvement.md` 与 2025-11-19 会议纪要，统一状态更新协议。
2. **Migration Log Automation (0.5 call)**：与 Info-Indexer/Planner 对齐 runSubAgent checklist，让新 PT/OI 记录自动生成 migration log + changefeed 链接模版。
3. **QA/Test Assets Index Assist (0.5 call)**：与 Info-Indexer 协同新增 QA/Test 资产索引（OI backlog），验证引用最小化并在 Task Board “Latest Update” 列挂钩。

## Blocking Issues
- Planner / Main Agent 需确认 Changefeed Adoption Sweep 剩余文档（Sprint-org、Meetings）的 runSubAgent 排期，避免与 PieceTree 迁移冲突。
- QA/Test 资产索引尚未立项，需 Info-Indexer 提供初稿以便 DocMaintainer 校验引用策略。
- 需要持续收到 Info-Indexer changefeed delta（增删/压缩）记录，才可在 Task Board “Latest Update” 列保持单一事实来源。

## Hand-off Checklist
1. 文档更新遵循模板并标注日期作者。
2. Tests or validations performed? N/A（文档任务）。
3. 下一位接手者需检查 `docs/meetings` & `docs/sprints` 是否最新，并在必要时安排 Info Proxy / Doc Gardener 任务。

## Logging & Artifact Plan
- **Migration Log**：集中于 `docs/reports/migration-log.md`（PT-006 交付后启用），每条记录包含日期、任务 ID、TS↔C# 差异、验证结果。
- **Consistency Reports**：归档在 `docs/reports/consistency/consistency-report-YYYYMMDD.md`（新建目录），记录 Task Board / Sprint / AGENTS 巡检结论。
- **Compression Logs**：放置于 `agent-team/doc-gardener/logs/compression-YYYYMMDD.md`，列出被精简内容、替代索引与 Info-Indexer 交接备忘。
