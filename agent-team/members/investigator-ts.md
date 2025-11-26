# Investigator-TS Memory

## Role & Mission
- 提供基于 TypeScript 源码的 PieceTree/TextModel/Search/DocUI 分析，确保 Porter-CS 与 QA 在实现前就拿到经过审计的事实与测试计划。
- 维护 Investigator 输出与 `AGENTS.md`、Sprint Log、Info-Indexer changefeed 之间的一致性，方便 DocMaintainer/Planner 即时同步组织记忆。

## Active Hooks
- [`docs/sprints/sprint-04.md`](../../docs/sprints/sprint-04.md) —— Phase 8 目标与 WS1/WS3 交付，锚点 [`#delta-2025-11-26-sprint04`](../indexes/README.md#delta-2025-11-26-sprint04)。
- [`docs/reports/audit-checklist-aa4.md`](../../docs/reports/audit-checklist-aa4.md) CL7/CL8 表格及其占位 changefeed：[`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)。
- [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md) —— 记录 CL7/CL8 Gap 状态与 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) 45/365 rerun 数据。
- [`agent-team/task-board.md`](../task-board.md) 与 `AGENTS.md` —— 发布每次审计/Doc sweep 的摘要与 changefeed 链接。

## Current Focus
- **WS1 Search guardrails：** 继续用 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) 维护 45/45 + 365/365 基线，同时把 Sprint 04 (`#delta-2025-11-26-sprint04`) WS1 backlog 需求回灌到计划与迁移日志。
- **AA4 CL7 backlog：** 按 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) 预留的 cursor-core/wordops/column-nav/snippet/commands-tests 变更拆分实现依赖，并保持 Task Board/TestMatrix CL7 行 Gap。
- **AA4 CL8 DocUI/Markdown：** 跟踪 Markdown renderer 搜索重算、FindModel capture/Intl/word cache 修复，按 [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown) 发布 Porting/QA 交付。
- **Alignment audit & doc sweep：** 任何文档压缩或审计（如 [`#delta-2025-11-26-alignment-audit`](../indexes/README.md#delta-2025-11-26-alignment-audit)）都需引用本快照，确保 DocMaintainer 在发布前完成 Investigator 对齐。

## Key Deliverables
- `agent-team/handoffs/AA4-003-Audit.md`、`agent-team/handoffs/AA4-004-Audit.md` —— CL7/CL8 缺口、验证钩子与 changefeed 拆解。
- `agent-team/handoffs/B3-TextModelSearch-INV.md`、`B3-TextModelSearch-QA.md`、`Review-20251125-Investigator.md` —— [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) 的 45-test 证据链。
- `agent-team/handoffs/B3-PieceTree-Deterministic-Backlog.md`、`B3-PieceTree-Deterministic-CRLF-INV.md`、`B3-PieceTree-SearchOffset-INV.md` —— PieceTree deterministic / search-offset 计划与 QA 钩子。
- `agent-team/handoffs/DocReview-20251126-INV.md`、`DocReview-20251126-R42-INV.md` —— 2025-11-26 文档压缩巡检与跨文档链接确认。

## Open Risks
- Cursor/command/snippet 栈仍缺少 `CursorsController`、WordOperations、ColumnSelectData 与 snippet controller，`#delta-2025-11-26-aa4-cl7-*` 仍是占位；未交付前 CL7 测试/文档不可转为 ✅。
- DocUI Markdown renderer 继续重复搜索并忽略 owner filter；若 `#delta-2025-11-26-aa4-cl8-markdown` 未发布，CL8 仍处高风险并阻挡 Markdown renderer tests。
- Whole-word / Intl segmentation 配置横跨 SearchTypes 与 cursor word ops；若没有统一方案，会让 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) 与 CL7/CL8 修复互相回退。

## Archives / References
历史调查与分批审计均已归档到 `agent-team/handoffs/`：AA4 系列（`AA4-SearchReview-20251125.md`、`AA4-Review-INV.md`、`AA4-FindModel-Review-INV.md`）、DocUI Batch #3（`B3-FC-Review.md`、`B3-DocUI-StagedReview-20251124.md`、`B3-Decor-INV.md`、`B3-Decor-Stickiness-Review.md`），以及 PieceTree 覆盖研究（`B3-PieceTree-Fuzz-INV.md`、`B3-PieceTree-Fuzz-Review-INV.md`、`B3-TestFailures-INV.md`、`B3-Snapshot-INV-20251125.md`）。需要细粒度时间线或测试详情时直接查阅这些 handoff 文档与 Sprint 03 Log。
