# DocMaintainer Memory (Snapshot 2025-11-27)

## Role & Mission
- **Consistency Gatekeeper：** 维持 `AGENTS.md`、`agent-team/task-board.md`、`docs/sprints/*` 的叙述一致，并在每条更新中引用最新 changefeed + migration log。
- **Info Proxy：** 为主 Agent / SubAgent 汇总 `docs/reports/migration-log.md`、`agent-team/indexes/README.md` 的关键信息，减少 token 压力。
- **Doc Gardener：** 控制文档体积，必要时把冗长记录移入 handoff/archives，并在核心文档留下指针。
- **Anchor Steward：** 任何 Sprint 04 / AA4 更新都要引用 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 以及 CL7/CL8 占位 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)，确保 Cursor/Snippet、DocUI、Intl cache 讨论都有 canonical 指针。

## Canonical Anchors
| Anchor | 用途 |
| --- | --- |
| [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) | Sprint 04 R1-R11 批量完成（365→585 测试），覆盖 WS1/WS2/WS3/WS4-Core/WS5 deliverables。 |
| [`#delta-2025-11-26-sprint04`](../indexes/README.md#delta-2025-11-26-sprint04) | Sprint 04 Phase 8 目标 & runSubAgent 钩子；所有前线文档需对齐该节。 |
| [`#delta-2025-11-26-alignment-audit`](../indexes/README.md#delta-2025-11-26-alignment-audit) | Alignment Audit R1 结论与 Verification Notes，校验 PieceTree/Search/IntervalTree 匀称性。 |
| [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) | TextModelSearch QA 45/45 (2.5s) + 365/365 (61.6s, `PIECETREE_DEBUG=0`) 的唯一事实来源；Intl.Segmenter & WordSeparator cache 仍在此 anchor 下追踪。 |
| [`#delta-2025-11-25-b3-search-offset`](../indexes/README.md#delta-2025-11-25-b3-search-offset) | PieceTree SearchOffset cache rerun（targeted 5/5 + full 324/324）与 Porter/QA handoff 的引用锚点。 |
| [`#delta-2025-11-24-b3-docui-staged`](../indexes/README.md#delta-2025-11-24-b3-docui-staged) | DocUI Find/Replace scope & decorations backlog；DocUI find/replace scope 调整必须引用此 anchor。 |
| [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) / [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown) | CL7 Cursor/Snippet 与 CL8 DocUI/Markdown placeholder，任何解除 Gap 的提交都需对其背书。 |

## Current Focus（2025-11-27）
- **Link Parity Sweep**：AGENTS / Sprint 04 / Task Board 现都在同一条目中并列 [`docs/reports/migration-log.md#sprint04-r1-r11`](../../docs/reports/migration-log.md#sprint04-r1-r11) 与 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)；后续交付需按该格式追加 MIG+changefeed 配对。
- **测试基线确认**：585 总数（584 通过 + 1 跳过）已由 `agent-team/handoffs/WS123-QA-Result.md` 与 `agent-team/handoffs/WS5-QA-Result.md` 记录，`tests/TextBuffer.Tests/TestMatrix.md` baseline 已同步。
- 修改 AGENTS / Sprint / Task Board 之前，先比对 `docs/reports/migration-log.md` 与相应 changefeed，并在条目中引用二者。
- rerun 统计必须包含 *targeted + full* 两组命令，且保留 `PIECETREE_DEBUG=0` 前缀。
- 冗长叙述统一搬入 `agent-team/handoffs/`（或既有 archive），核心记忆档仅保留快照级别信息。
- 任何提及 Cursor/Snippet 或 DocUI backlog 的段落，都必须携带 AA4 CL7/CL8 placeholder anchor 以及对应 handoff（`AA4-003/004-Audit`, `AA4-006-Plan`, `AA4-006-Result`）。

## Coordination Hooks
- **Info-Indexer**：及时共享新增 delta / changefeed 清理计划；DocMaintainer据此刷新"状态提示"段落。
- **Planner**：在 runSubAgent 循环中先行安装 DocMaintainer hooks（playbook 第三阶段）以免遗漏文档步骤。
- **Porter-CS & QA-Automation**：当实现/测试交付后，若文档尚未引用最新 rerun 结果，可直接抛 doc-fix handoff，由 DocMaintainer 执行。

## Checklist
1. `agent-team/handoffs/WS1-PORT-SearchCore-Result.md`、`WS2-PORT-Result.md`、`WS3-PORT-Tree-Result.md`、`WS4-PORT-Core-Result.md`、`WS5-PORT-Harness-Result.md` —— 在 `AGENTS.md`、`agent-team/task-board.md`、`docs/sprints/sprint-04.md` 的每个 Sprint 04 摘要中同步这些 handoff 链接，并指回 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)。
2. `agent-team/handoffs/WS123-QA-Result.md`、`WS5-QA-Result.md`、`tests/TextBuffer.Tests/TestMatrix.md` —— 以 `PIECETREE_DEBUG=0` rerun 585/585（1 skip）为基准，保持迁移日志/Task Board 统计与 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 一致。
3. `agent-team/handoffs/B3-TextModelSearch-INV.md`、`B3-TextModelSearch-QA.md` —— 在记录 Intl.Segmenter & WordSeparator cache 调查时引用 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch)，并提示任何 rerun 命令写回 `TestMatrix`。
4. `agent-team/handoffs/WS5-INV-TestBacklog.md`、`docs/reports/audit-checklist-aa4.md`、`agent-team/handoffs/AA4-006-Plan.md`、`AA4-006-Result.md`、`AA4-008-Result.md` —— 当 Cursor/Snippet backlog 或 DocUI scope 有更新时，同时引用 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) 与 [`#delta-2025-11-24-b3-docui-staged`](../indexes/README.md#delta-2025-11-24-b3-docui-staged)/[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown) 占位。

## Open Investigations
1. **Intl.Segmenter & WordSeparator cache**：参考 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) + `agent-team/handoffs/B3-TextModelSearch-INV.md`，防止 word boundary 文档回退。
2. **Cursor/Snippet backlog**：跟踪 `WS4-PORT-Core` 与 CL7 placeholder (`#delta-2025-11-26-aa4-cl7-cursor-core`)，确保 `agent-team/task-board.md` CL7 行保留 Gap 说明。
3. **DocUI find/replace scope**：`#delta-2025-11-24-b3-docui-staged` 与 `#delta-2025-11-26-aa4-cl8-markdown` 仍为占位；等待 DocUI 找词范围/Intl cache 修复，并引用 `agent-team/handoffs/B3-FC-Result.md`、`AA4-008-Result.md`。
4. **Changefeed Archive Hygiene**：`agent-team/indexes/README.md` 持续增长，需按 Info-Indexer 指引把 R37 以前的 delta 归档到 `agent-team/indexes/archive/`。

## Last Update
- **Date**: 2025-11-27
- **Task**: Sprint 04 R1-R11 doc sweep（AGENTS / Task Board / Sprint / Migration Log link parity）
- **Result**: ✅ 各文档均加入 `migration-log` + `changefeed` 成对引用，并补写 AA4 CL7/CL8 Gap 提醒；Task Board/Progress Log 统计与 585 baseline 对齐。

- **Date**: 2025-11-26
- **Task**: OPS-Doc（Sprint/TestMatrix/Migration Log/AGENTS 同步）
- **Result**: ✅ 完成所有文档更新，引用 `#delta-2025-11-26-sprint04-r1-r11`
