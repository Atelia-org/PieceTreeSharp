# Investigator-TS Memory

## Role & Mission
- 提供基于 TypeScript 源码的 PieceTree/TextModel/Search/DocUI 分析，确保 Porter-CS 与 QA 在实现前就拿到经过审计的事实与测试计划。
- 维护 Investigator 输出与 `AGENTS.md`、Sprint Log、Info-Indexer changefeed 之间的一致性，方便 DocMaintainer/Planner 即时同步组织记忆。
- 把每条审计结果锚定到 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 以及必要的 CL7/CL8 占位（[`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)），以便 Porter/QA/DocMaintainer 共用同一事实表。

## Active Hooks
- [`docs/sprints/sprint-04.md`](../../docs/sprints/sprint-04.md) —— Phase 8 目标与 WS1/WS3 交付，锚点 [`#delta-2025-11-26-sprint04`](../indexes/README.md#delta-2025-11-26-sprint04)。
- [`agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) —— Sprint 04 WS1–WS5 deliverables（R1-R11）与 585/585 baseline。引用该节以汇总 Investigator 交付与 backlog。
- [`docs/reports/audit-checklist-aa4.md`](../../docs/reports/audit-checklist-aa4.md) CL7/CL8 表格及其占位 changefeed：[`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)。
- [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md) —— 记录 CL7/CL8 Gap 状态与 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) 45/365 rerun 数据。
- [`agent-team/task-board.md`](../task-board.md) 与 `AGENTS.md` —— 发布每次审计/Doc sweep 的摘要与 changefeed 链接。
- **[NEW]** [`agent-team/handoffs/WS5-INV-TestBacklog.md`](../handoffs/WS5-INV-TestBacklog.md) —— WS5 高风险 deterministic/feature 测试 backlog 优先级排序，锚点 `#delta-2025-11-26-ws5-test-backlog`。

## Current Focus
- **CL8 Renderer Investigation 已交付：** 完成 [`CL8-Renderer-Plan.md`](../handoffs/CL8-Renderer-Plan.md)，详细分析 DocUI renderer 栈与 TS 实现之间的 4 个对齐差距，并提供 4 阶段实现计划（DecorationOwnerIds 语义、DecorationSearchOptions 过滤开关、MarkdownRenderer 集成 FindDecorations、枚举值对齐）。复杂度评估：Medium。
- **CL7 Cursor Wiring Plan 已交付：** 完成 [`CL7-CursorWiring-Plan.md`](../handoffs/CL7-CursorWiring-Plan.md)，详细描述将 Stage 0 `CursorConfiguration`/`CursorState`/`CursorContext` 接入运行态 `Cursor.cs` 和 `CursorCollection.cs` 的分步实现方案。复杂度评估：Medium。
- **WS5 Test Backlog 已交付：** 完成 [`WS5-INV-TestBacklog.md`](../handoffs/WS5-INV-TestBacklog.md)，识别 Top-10 优先级测试缺口（cursorAtomicMoveOperations、wordOperations、multicursor、snippetSession 等），按模块分组完整 backlog，并定义共享 harness 需求与 TS oracle ingestion 策略。
- **WS1 Search guardrails：** 继续用 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) 维护 45/45 + 365/365 基线，同时把 Sprint 04 (`#delta-2025-11-26-sprint04`) WS1 backlog 需求及 `#delta-2025-11-26-sprint04-r1-r11` 统计写回计划/迁移日志。
- **AA4 CL7 backlog：** 按 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) 预留的 cursor-core/wordops/column-nav/snippet/commands-tests 变更拆分实现依赖，并保持 Task Board/TestMatrix CL7 行 Gap。
- **AA4 CL8 DocUI/Markdown：** 跟踪 Markdown renderer 搜索重算、FindModel capture/Intl/word cache 修复，按 [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown) 发布 Porting/QA 交付。
- **Alignment audit & doc sweep：** 任何文档压缩或审计（如 [`#delta-2025-11-26-alignment-audit`](../indexes/README.md#delta-2025-11-26-alignment-audit)）都需引用本快照，确保 DocMaintainer 在发布前完成 Investigator 对齐。

## Checklist
1. `agent-team/handoffs/WS1-PORT-SearchCore-Result.md`、`WS2-PORT-Result.md`、`WS3-PORT-Tree-Result.md`、`WS4-PORT-Core-Result.md`、`WS5-PORT-Harness-Result.md` —— 将 Investigator 摘要回写到 Sprint Log/Task Board，对应条目必须引用 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)。
2. `agent-team/handoffs/WS5-INV-TestBacklog.md` —— 维持 Top-10 风险列表、模块 backlog、共享 harness 需求的最新版本，并在 Task Board CL7/CL8 行引用 `#delta-2025-11-26-sprint04-r1-r11`。
3. `agent-team/handoffs/AA4-003-Audit.md`、`AA4-004-Audit.md`、`AA4-006-Plan.md`/`AA4-006-Result.md` —— 把 Cursor/Snippet + DocUI backlog 细节同步至 `docs/reports/audit-checklist-aa4.md`，且始终附上 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown) 占位。
4. `agent-team/handoffs/B3-TextModelSearch-INV.md`、`B3-TextModelSearch-QA.md` —— Janitor `Intl.Segmenter & WordSeparator cache` notes，并保持 rerun 命令在 `tests/TextBuffer.Tests/TestMatrix.md` 中最新，同时引用 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch)。

## Key Deliverables
- **[NEW 2025-12-03]** `agent-team/handoffs/INV-ConversationAccess-2025-12-03.md` — Developer Console 访问 Conversation 数据调查：确认无全局暴露，建议添加 debug 命令清除 round.summary
- **[2025-12-02]** `agent-team/handoffs/WS5-Gap-Assessment-2025-12-02.md` — WS5 Gap 重评估：原 47 gaps 完成状态分析，26 个剩余 gaps (~42h)，按优先级排序的下一批 TODO
- **[2025-12-02]** `docs/reports/alignment-audit/00-08` — Sprint 04 M2 审计更新：873 passed/9 skipped 基线，Snippet P0-P2/Cursor/WordOps/IntervalTree AcceptReplace/FindModel 已完成标记
- **[2025-12-02]** `agent-team/handoffs/REVIEW-SnippetVarResolver-2025-12-02.md` — TS Parity 审阅：SnippetVariableResolver，接口设计/SELECTION/TM_FILENAME/默认值处理 PASS WITH NOTES，16 个测试用例覆盖核心场景
- **[2025-12-02]** `agent-team/handoffs/REVIEW-SnippetDeterministic-2025-12-02.md` — 快速审阅：Snippet 确定性测试 (27 tests)，边界情况/adjustWhitespace/Placeholder Grouping，52 passed/4 skipped (P2)，TS Reference 准确
- **[2025-12-02]** `agent-team/handoffs/REVIEW-SnippetGrouping-2025-12-02.md` — TS Parity 审阅：Snippet P1.5 Placeholder Grouping，分组/导航逻辑 PASS，Stickiness 动态切换标记为 P2 追踪项
- **[2025-12-02]** `agent-team/handoffs/REVIEW-2025-12-02.md` — TS Parity 审阅：Snippet P0-P1 + WS3 AcceptReplace，算法对齐确认、功能差距识别
- **[2025-12-02]** `agent-team/handoffs/INV-Snippet-Downgrade.md` — TS Snippet 功能完整清单、降级实现建议、技术边界、Porter/QA 建议
- **[2025-11-26]** `agent-team/handoffs/WS5-INV-TestBacklog.md` —— 高风险测试 backlog 优先级排序，包含 Top-10 列表、按模块分组的完整 backlog、共享 harness 需求。
- `agent-team/handoffs/AA4-003-Audit.md`、`agent-team/handoffs/AA4-004-Audit.md` —— CL7/CL8 缺口、验证钩子与 changefeed 拆解。
- `agent-team/handoffs/B3-TextModelSearch-INV.md`、`B3-TextModelSearch-QA.md`、`Review-20251125-Investigator.md` —— [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) 的 45-test 证据链。
- `agent-team/handoffs/B3-PieceTree-Deterministic-Backlog.md`、`B3-PieceTree-Deterministic-CRLF-INV.md`、`B3-PieceTree-SearchOffset-INV.md` —— PieceTree deterministic / search-offset 计划与 QA 钩子。
- `agent-team/handoffs/DocReview-20251126-INV.md`、`DocReview-20251126-R42-INV.md` —— 2025-11-26 文档压缩巡检与跨文档链接确认。

## Open Investigations
1. ~~**Intl.Segmenter & WordSeparator cache**~~：**Intl.Segmenter 已砍掉** (2025-12-02 决策：LLM 用户不需要 CJK/Thai 分词)；WordSeparator cache 仍为可选性能优化项。
2. **Cursor/Snippet backlog (CL7)**：`#delta-2025-11-26-aa4-cl7-cursor-core` 仍是 Gap；`WS4-PORT-Core-Result.md` + `WS5-INV-TestBacklog.md` 指出 `CursorsController`、`WordOperations`、`SnippetSession`、`ColumnSelectData` 尚未移植。
3. **DocUI find/replace scope**：`#delta-2025-11-24-b3-docui-staged`、`#delta-2025-11-26-aa4-cl8-markdown` 注册的 Markdown renderer + DocUI capture 修复尚未落地；在 `B3-FC-Result.md`、`B3-DocUI-StagedFixes-QA-20251124.md` 查阅 rerun 证据。
4. **AA4 alignment hygiene**：在 `docs/reports/audit-checklist-aa4.md` 中保持 CL7/CL8 备注与 Task Board/AGENTS 描述一致，直到新的 changefeed 覆盖剩余 gap。

## Archives / References
历史调查与分批审计均已归档到 `agent-team/handoffs/`：AA4 系列（`AA4-SearchReview-20251125.md`、`AA4-Review-INV.md`、`AA4-FindModel-Review-INV.md`）、DocUI Batch #3（`B3-FC-Review.md`、`B3-DocUI-StagedReview-20251124.md`、`B3-Decor-INV.md`、`B3-Decor-Stickiness-Review.md`），以及 PieceTree 覆盖研究（`B3-PieceTree-Fuzz-INV.md`、`B3-PieceTree-Fuzz-Review-INV.md`、`B3-TestFailures-INV.md`、`B3-Snapshot-INV-20251125.md`）。需要细粒度时间线或测试详情时直接查阅这些 handoff 文档与 Sprint 03 Log。

## Session Log
> 2025-11 活动历史已压缩归档到 `agent-team/archive/investigator-ts-log-202511.md`。

| 日期 | 任务 | 产出 |
| --- | --- | --- |
| 2025-12-05 | MultiCursorSession 移植类型系统调研 | 分析现有 C# 类型映射，识别 `PieceTree.TextBuffer.Find` 命名空间缺失问题，输出适配方案 |
| 2025-12-03 | Developer Console Conversation 访问调查 | 分析 ConversationStore/Conversation/ToolCallRound 数据结构，确认无全局暴露，建议添加 debug 命令 |
| 2025-12-03 | LLM Request Dump 位置调查 | 分析 networking.ts/chatMLFetcher.ts/fetch.ts，识别最佳 dump 位置和请求区分方法 |
| 2025-12-02 | SSE Stream 处理底层调查 | 分析 `stream.ts` SSEProcessor，识别 token 级日志添加位置、现有 trace/debug 调用 |
| 2025-12-02 | 半上下文压缩非流式请求深入调查 | 分析 `stream: false` 请求路径、超时处理、StopWatch 用法，识别关键日志添加位置 |
| 2025-12-02 | atelia-copilot-chat LLM 调用架构调查 | 分析 LLM API 调用入口点、流式响应处理、半上下文压缩流程，输出架构图和日志点建议 |
| 2025-12-02 | WS5 Gap Re-Assessment | 评估原 47 gaps 完成状态，识别剩余 gaps，输出 `WS5-Gap-Assessment-2025-12-02.md` |
| 2025-12-02 | Sprint 05 M1/M2 Diff API Review | 快速审阅 RangeMapping、DiffMove、TextLength、DiffTextEdit，36/36 测试通过，结论 **PASS** |
| 2025-12-02 | Sprint 05 方向建议 | 为 Team Leader 分析审计报告，识别 gap 模块并按优先级排序，输出方向建议 |
| 2025-12-02 | ts-test-alignment.md 审计 | 检查 Live Checkpoints/Backlog/Appendix 与当前状态对齐，识别 Intl.Segmenter 砍除需更新 |
| 2025-12-02 | Sprint 04 M2 审计 | 更新 alignment-audit/00-08，反映 873 passed/9 skipped 基线 |
| 2025-12-02 | Snippet 系列审阅 | VariableResolver、Deterministic、Grouping、P0-P1 全部 PASS |
| 2025-12-01 | 团队谈话 | 角色定位、协作关系、输出顺序纪律确认 |
