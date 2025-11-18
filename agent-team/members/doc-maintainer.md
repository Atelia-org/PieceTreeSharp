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
- **Last Update:** 2025-11-19
- **Recent Actions:**
  - 2025-11-19：完成 DocMaintainer onboarding 巡检（AGENTS、Playbook、Main Loop、会议纪要、Sprint、Task Board、Index Catalog），整理 Doc↔Info 协作要点。
  - 2025-11-19：协助整理 AI Team Playbook、模板。

## Upcoming Goals (1-3 runSubAgent calls)
1. **PT-006 – Migration Log Flow (1 call)**：创建 `docs/migration-log.md` 与更新 checklist，协调 Planner 把“TS→C# 变更必留痕”写入验收。
2. **OI-001 – Core Docs Audit (1 call)**：联动 Info-Indexer 巡检 AGENTS / Sprint / Meeting / Index Catalog，输出重复与缺口行动列表。
3. **OI-004 – Task Board Compression (1 call)**：设计“核心进展 vs backlog”分层，并将长尾细节外链至索引文件。

## Blocking Issues
- Info-Indexer 需先交付首个 `core-docs-index.md`，OI-001 才能引用统一视图。
- Planner / Main Agent 尚未确认 PT-006 与 OI-004 的 runSubAgent 排期，需避免与开发关键路径冲突。
- 迁移日志模板等待 Main Agent 批准字段（任务 ID、影响文件、验证方式）。

## Hand-off Checklist
1. 文档更新遵循模板并标注日期作者。
2. Tests or validations performed? N/A（文档任务）。
3. 下一位接手者需检查 `docs/meetings` & `docs/sprints` 是否最新，并在必要时安排 Info Proxy / Doc Gardener 任务。

## Logging & Artifact Plan
- **Migration Log**：集中于 `docs/migration-log.md`（PT-006 交付后启用），每条记录包含日期、任务 ID、TS↔C# 差异、验证结果。
- **Consistency Reports**：归档在 `docs/reports/consistency/consistency-report-YYYYMMDD.md`（新建目录），记录 Task Board / Sprint / AGENTS 巡检结论。
- **Compression Logs**：放置于 `agent-team/doc-gardener/logs/compression-YYYYMMDD.md`，列出被精简内容、替代索引与 Info-Indexer 交接备忘。
