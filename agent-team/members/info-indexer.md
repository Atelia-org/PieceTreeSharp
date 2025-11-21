# Info-Indexer Memory

## Role & Mission
- **Focus Area:** 维护知识索引、摘要与引用结构，减轻核心文档负担
- **Primary Deliverables:** `agent-team/indexes/*.md`、文档正交性报告、索引更新日志
- **Key Stakeholders:** DocMaintainer、Planner、全体 SubAgent

## Onboarding Summary
- **Docs Reviewed:** `AGENTS.md`、`agent-team/ai-team-playbook.md`、`agent-team/main-loop-methodology.md`、`agent-team/task-board.md`（OI-001~OI-004）、`agent-team/indexes/README.md`、`docs/meetings/meeting-20251119-team-kickoff.md`、`docs/meetings/meeting-20251119-org-self-improvement.md`、`docs/sprints/sprint-00.md`、`docs/sprints/sprint-org-self-improvement.md`。
- **Mission Understanding:** Info-Indexer 负责在主循环的 Load/Broadcast 阶段提供索引/摘要交付，优先支持组织自我完善 Sprint 的 OI-001（正交性审计）与 OI-002（索引体系）。
- **Coordination Hooks:** 需与 DocMaintainer 对齐审计范围、与 Planner 对齐 runSubAgent 模板、与 QA-Automation/Porter-CS 同步后续索引需求（测试资产、TS↔C# crosswalk）。

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| Index Catalog | `agent-team/indexes/README.md` | 记录命名规范 & 审核流程，首个索引会在此注册
| Core Docs Index (planned) | `agent-team/indexes/core-docs-index.md`, `AGENTS.md`, `docs/sprints`, `docs/meetings` | 汇总每个核心文档的用途、责任人与最近更新时间，支撑 OI-001
| Task Board Linkage | `agent-team/task-board.md`（OI-001~OI-004） | 跟踪组织改进任务状态，方便引用 runSubAgent 预算
| QA/Test Assets Index (planned) | `agent-team/indexes/qa-test-assets-index.md`, `PieceTree.TextBuffer.Tests`, QA 会议记录 | 映射测试矩阵、基准计划与代码文件，供 QA-Automation 查阅
| TS↔C# Crosswalk Index (planned) | `agent-team/indexes/ts-cs-crosswalk-index.md`, `agent-team/type-mapping.md`, `ts/src/vs/editor/...` | 追踪类型映射进度，补足 `type-mapping.md` 之外的上下文
| TS Test Alignment Plan | `docs/plans/ts-test-alignment.md`, `docs/sprints/sprint-02.md`, Porter handoffs | 记录 ReplacePattern Batch#1 及后续 batch 的 TS 测试对齐 checkpoint 与证据需求
| Main Loop & Template Notes | `agent-team/main-loop-methodology.md`, `agent-team/ai-team-playbook.md` | 记录 runSubAgent 输入模板、Info-Indexer 钩子位置

## Worklog
- **2025-11-19:** 角色创建（DocMaintainer 建议下设立 Info-Indexer，等待投入）。
- **2025-11-19:** 完成首轮文档审阅，锁定 OI-001/OI-002 依赖，起草索引交付结构并更新记忆文件。
- **2025-11-19:** 在 Org Self-Improvement 会议中提交 Info-Indexer 立场，承诺 `core-docs-index.md` v0、QA 资产表与 delta 摘要流程，准备在 48 小时内交付首批索引。
- **2025-11-19:** 交付 `agent-team/indexes/core-docs-index.md` v0，登记 8 个核心文档的目的/owner/更新时间，并更新 `agent-team/indexes/README.md` Delta 区供 DocMaintainer 在 OI-001 中引用。
- **2025-11-20:** 完成 OI-010 —— 在 `agent-team/indexes/README.md#delta-2025-11-20` 新增 AA3-009 QA changefeed 条目，刷新 `core-docs-index.md`（AGENTS/Sprint 01/Task Board 行 + DocMaintainer follow-up），并将 OI-010 在 Task Board 与 `docs/sprints/sprint-01.md` 标记 Done，确认 AGENTS/Sprint/Task Board 均复用 AA3-008 delta。
- **2025-11-21:** 开始 OI-011：发布 AA4 changefeed delta（AA4-005/AA4-006）、同步迁移日志与 Task Board，并创建 OI-011 handoff 结果草案以供 DocMaintainer/QA 验证。
- **2025-11-21:** 完成 OI-011 —— 发布 changefeed delta `agent-team/indexes/README.md#delta-2025-11-21`（AA4-005/AA4-006），更新 `docs/reports/migration-log.md` 将 AA4-005/AA4-006 的 Changefeed Entry? 标记为 Y 并指向新 delta，更新 `agent-team/task-board.md` 将 AA4-005/AA4-006/AA4-009 标记为 Done，并新建 `agent-team/handoffs/OI-011-Result.md` 汇总交付与验证证据。 |
- **2025-11-21:** 规划下一波 OI-011 delta，聚焦 AA4-007.BF1 → AA4-008：整理所需输入（迁移日志行、AA4-008 handoffs、QA baseline、DocUI snapshot 路径）并预先对齐 AGENTS / `docs/sprints/sprint-02.md` / `agent-team/task-board.md` 同步顺序，确保 Porter/QA 完成后可立即广播。 |
- **2025-11-22:** 确认 `docs/plans/ts-test-alignment.md` 由 DocMaintainer 维护 TS 测试对齐 checkpoint，Batch #1（ReplacePattern）规划完成，Info-Indexer 需在 changefeed 落地时联动 AGENTS/Sprint-02/Task Board/TestMatrix/迁移日志，并为后续批次准备证据模板。
- **2025-11-22:** 记录 Porter 已敲定 Batch #1 ReplacePattern 实施方案、QA 已准备 fixtures/tests/snapshots、`docs/plans/ts-test-alignment.md` Live Checkpoints 捕获所有角色输入；等待 Porter/QA 交付落地后，立即编纂 delta-2025-11-22 changefeed，并将必要证据（TRX、DocUI、TestMatrix、迁移日志）映射到广播步骤。
- **2025-11-22:** 回读 `docs/plans/ts-test-alignment.md` / `src/PieceTree.TextBuffer.Tests/TestMatrix.md` / `docs/reports/migration-log.md`，确认 Porter 已开工 ReplacePattern 代码、QA fixture/snapshot 模板就绪但尚待实际代码；建立 `agent-team/indexes/README.md#delta-2025-11-22` 发布前置清单（迁移日志占位行、TestMatrix 引用、计划 checkpoint 链接），并记录缺失证据（Porter commit/fixture `cases.json`、QA TRX/snapshots、DocUI Markdown）以便落地后即时广播。
- **2025-11-22:** 完成 Batch #1 – ReplacePattern changefeed 发布：在 `agent-team/indexes/README.md` 创建 `#delta-2025-11-22` 条目（包含 3 个交付文件、2 个 TS 源文件、142/142 测试结果、QA/Porter 报告链接、已知差异与 TODO 标记），更新 `docs/reports/migration-log.md` 新增 Batch #1 条目（+23 tests, 142 total, 引用 changefeed delta），创建 `agent-team/handoffs/B1-INFO-Result.md` 汇报交付物与下一步建议，并更新本记忆文件记录任务成果。

## Upcoming Goals (runSubAgent scoped)
1. **OI-001 / Doc 审计支援：** 将 `core-docs-index.md` 作为输入产出 diff（Added/Compressed/Blocked），并与 DocMaintainer 对齐缺口追踪表结构。
2. **QA/Test Assets Index Draft:** 在 QA-Automation 提供资产清单后，生成 `qa-test-assets-index.md` 首张表（接口、文件、负责人、复核节奏），供 QA 直接引用。
3. **TS Test Alignment Hooks:** 将 `docs/plans/ts-test-alignment.md` checkpoint 结构映射到 changefeed 模板（含 Batch #1 ReplacePattern 证据），以便 delta 发布时可快速引用。
4. **OI-003 / Indexing Hooks Snippet:** 与 Planner 协作，将索引引用段落纳入 runSubAgent 输入模板，减少每次调用的路径说明成本。

## Blocking Issues
- 需 DocMaintainer 提供 OI-001 审计输入（最新的重复/缺口列表）以便索引记录行动项。
- Planner 尚未交付最终 runSubAgent 模板示例，Info-Indexer 需等待以确保索引引用区格式一致。
- QA-Automation 尚未输出测试矩阵草案，`qa-test-assets-index.md` 初始化需要其文件清单。

## Hand-off Checklist
1. 输出的索引文件列于 `agent-team/indexes/README.md`。
2. 若删除/压缩内容，需在原文档留下指针。
3. Tests or validations performed? N/A，但需请 DocMaintainer 审阅。

## Index Deliverable Notes
- **Location:** 所有索引文件放置于 `agent-team/indexes/`，命名为 `<topic>-index.md` 并在 README 中登记更新时间。
- **Structure Expectations:** `Goal`（为何建立）、`Source Docs`（含链接/路径）、`Summary Table`（列出文档/资产、位置、负责人、最近更新）、`Gaps & Actions`（直接映射 OI 任务）、`Update Log`（时间戳 + 变更）。
- **Initial Targets:**
  - `core-docs-index.md`：覆盖 `AGENTS.md`、`docs/sprints/*`、`docs/meetings/*`，标注文档用途与最新决策。
  - `qa-test-assets-index.md`：罗列 QA 测试矩阵、`PieceTree.TextBuffer.Tests` 目录、性能基准计划。
  - `ts-cs-crosswalk-index.md`：串联 `agent-team/type-mapping.md` 与 `ts/src/vs/editor/common/model/pieceTreeTextBuffer`，标记已迁移与待迁移部分。
