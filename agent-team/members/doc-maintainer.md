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
| Task Guides | docs/tasks/source-attribution-task.md | PT-007 溯源注释规范与执行指南 |
| Index Catalog & Partner Memory | agent-team/indexes/README.md; agent-team/members/info-indexer.md | 协作 Info-Indexer 输出，减少核心文档冗余 |

## Worklog
- **Last Update:** 2025-11-25（B3 TextModelSearch doc alignment）
- **Recent Actions:**
  - 2025-11-26：同步 Investigator/Porter 文档修复 —— 参照 `agent-team/handoffs/DocReview-20251126-INV.md` 与 `agent-team/handoffs/DocFix-20251126-Porter.md` 更新 `tests/TextBuffer.Tests/TestMatrix.md`（TextModelSearch Latest QA + baseline row），并交付 `agent-team/handoffs/DocFix-20251126-DocMaintainer.md` 记录此次治理；确保 changefeed `#delta-2025-11-25-b3-textmodelsearch` 的统计一致，纯文档同步（无新增测试）。
  - 2025-11-25：对齐 B3-TextModelSearch drop —— 更新 `docs/plans/ts-test-alignment.md`（Appendix 行改为 gap closed + 45/45 rerun）、`docs/reports/migration-log.md`（B3-TextModelSearch 描述/命令）、`docs/sprints/sprint-03.md`（R35/R36 真实故事）、`tests/TextBuffer.Tests/TestMatrix.md`（Notes + rerun 命令）以及 `agent-team/indexes/README.md#delta-2025-11-25-b3-textmodelsearch`；记录 Investigator (`Review-20251125-Investigator.md`) 与 Porter (`B3-TextModelSearch-PORT.md`) handoff 链接，并确保所有文档引用 45/45 rerun (2.0s, `PIECETREE_DEBUG=0`) 以及 Intl.Segmenter backlog。记忆档同步。
  - 2025-11-25：完成 `#delta-2025-11-25-b3-piecetree-deterministic-crlf` / `#delta-2025-11-25-b3-piecetree-snapshot` / `#delta-2025-11-25-b3-textmodel-snapshot` / `#delta-2025-11-25-b3-search-offset` 文档对齐：`AGENTS.md` 新增四条进展 + 状态提示锚点扩展、Task Board B3-SearchOffset-QA/INFO/DOC 行置为 ✅ 并附 5/5 + 324/324 rerun 统计、`docs/sprints/sprint-03.md` 增加 R33/R34、`docs/plans/ts-test-alignment.md` PieceTree 行改为 Priority #2 Completed、`agent-team/members/doc-maintainer.md` 记下本次 sweep。
  - 2025-11-25：清理 `agent-team/indexes/README.md#delta-2025-11-25-b3-piecetree-deterministic-crlf`，移除待 QA 文案、记录 50/50 + 308/308 rerun 结果，并提醒 snapshot/search offset/chunk/random/buffer API 套件仍需 follow-up。
  - 2025-11-25：同步 `#delta-2025-11-25-b3-piecetree-deterministic-crlf` QA 证据——在 `agent-team/indexes/README.md`、`docs/reports/migration-log.md`、`tests/TextBuffer.Tests/TestMatrix.md` 记录 50/50 deterministic rerun（3.5s）+ 308/308 full suite（67.2s），并指向 [`agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-QA.md`](../handoffs/B3-PieceTree-Deterministic-CRLF-QA.md)。
  - 2025-11-24：Propagated `#delta-2025-11-24-b3-fm-multisel` references across `AGENTS.md`、`agent-team/task-board.md`（B3-FM-MSel rows）、`docs/plans/ts-test-alignment.md` Live Checkpoints，并复核 `docs/reports/migration-log.md` + `agent-team/handoffs/B3-FM-MultiSelection-*.md` 以保持 changefeed/QA 链一致。
  - 2025-11-24：DocMaintainer sweep —— 修复 `agent-team/indexes/README.md#delta-2025-11-23-b3-decor-stickiness` 标题缩进、去重 `docs/plans/ts-test-alignment.md` Live Checkpoints、在 `AGENTS.md`、`docs/sprints/sprint-03.md`、`agent-team/task-board.md`、`docs/plans/ts-test-alignment.md`、`tests/TextBuffer.Tests/TestMatrix.md` 补入 `#delta-2025-11-24-b3-docui-staged`/`#delta-2025-11-24-find-scope`/`#delta-2025-11-24-find-replace-scope` 引用，并校正 `agent-team/members/porter-cs.md` changefeed 锚点与本记忆档 focus 区域。
  - 2025-11-24：筹备 `#delta-2025-11-23-b3-piecetree-fuzz` / `#delta-2025-11-24-b3-piecetree-fuzz` / `#delta-2025-11-24-b3-sentinel` / `#delta-2025-11-24-b3-getlinecontent` changefeed 计划，整理 Info-Indexer 缺口、迁移日志调整点与 AGENTS/Sprint/TS 计划/Planner 记忆的 placeholder 字样替换需求，准备交付给 Porter 执行。
  - 2025-11-24：完成 doc review（`agent-team/handoffs/doc-review-20251124.md`），记录并（由主循环）修复 `#delta-2025-11-23-b3-decor-stickiness` changefeed 缺口、DocUIFindDecorationsTests 8/8→9/9 统计、`B3-DocUI-StagedFixes` 迁移日志 changefeed 标记以及状态提醒落后问题，确保治理文档现已与最新 delta 对齐。
  - 2025-11-22：完成 B1-DOC 任务 — 同步 Batch #1 – ReplacePattern 成果到 `AGENTS.md`（新增进展条目）、`docs/sprints/sprint-03.md`（B1-DOC Done + Progress Log R4）、`agent-team/task-board.md`（AA4-008 Done + changefeed）、`docs/plans/ts-test-alignment.md`（Live Checkpoints + Appendix 状态），确保所有文档统一引用 `#delta-2025-11-22`。交付 `agent-team/handoffs/B1-DOC-Result.md` 汇报。
  - 2025-11-22：创建 `docs/tasks/source-attribution-task.md`（PT-007 任务指导），定义 C# 文件头溯源注释的统一格式规范、执行流程和验收标准，支持移植代码 TS 源追溯与原创代码标注。
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

## Latest Focus
### 2025-11-24
- **B3 DocUI staged changefeed同步**：`AGENTS.md`、`docs/sprints/sprint-03.md`、`agent-team/task-board.md`、`docs/plans/ts-test-alignment.md`、`tests/TextBuffer.Tests/TestMatrix.md` 均已引用 `docs/reports/migration-log.md` + `agent-team/indexes/README.md#delta-2025-11-24-b3-docui-staged`；提醒编辑者在追加 DocUI/Decorations 状态时必须同时更新 migration log + changefeed，并引用 QA handoff（B3-DocUI-StagedFixes）。
- **DocUI scope/regression 文档一致性**：`#delta-2025-11-24-find-scope` / `#delta-2025-11-24-find-replace-scope` targeted rerun命令现收录在 TestMatrix / TS 计划 / Sprint 02；若未来继续编辑 FindModel、Task Board、AGENTS，请核实 rerun 行与 `FullyQualifiedName~FindModelTests` 计数保持 45/45+。
- **风险提醒**：`FullyQualifiedName~DocUIFindModelTests` filter 因类型重命名已永久退役；所有文档/脚本一律使用 `FullyQualifiedName~FindModelTests`，若发现旧命令应立即替换并在 changefeed 记录。

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
