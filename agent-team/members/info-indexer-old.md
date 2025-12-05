# Info-Indexer Snapshot · 2025-12-05 (Updated)

## Role & Mission
- Maintain `agent-team/indexes/README.md` plus downstream index files so every changefeed has a single canonical pointer.
- Keep AGENTS/Sprint/Task Board docs lightweight by offloading detailed histories into `agent-team/indexes/*.md` and linked handoffs.
- Surface blockers that threaten broadcast cadence before DocMaintainer kicks off each sweep.
- Make sure every Sprint 05 drop references corresponding changefeeds (e.g. `#delta-2025-12-05-*`).

## Active Changefeeds & Backlog
| Anchor | Coverage | Status / Next Hook |
| --- | --- | --- |
| [`#delta-2025-12-05-p2-complete`](../indexes/README.md#delta-2025-12-05-p2-complete) | Sprint 05 P2 任务全部完成，测试基线 1158 passed | ✅ Published. **当前最新基线证据**。 |
| [`#delta-2025-12-05-snippet-transform`](../indexes/README.md#delta-2025-12-05-snippet-transform) | Snippet Transform + FormatString（+33 tests）| ✅ Published. Commit: `9515be1`。 |
| [`#delta-2025-12-05-multicursor-snippet`](../indexes/README.md#delta-2025-12-05-multicursor-snippet) | MultiCursor Snippet 集成（+6 tests）| ✅ Published. |
| [`#delta-2025-12-05-add-selection-to-next-find`](../indexes/README.md#delta-2025-12-05-add-selection-to-next-find) | AddSelectionToNextFindMatch 完整实现（+34 tests）| ✅ Published. Commits: `4101981`, `575cfb2`。 |
| [`#delta-2025-12-04-p1-complete`](../indexes/README.md#delta-2025-12-04-p1-complete) | P1 任务全部完成：TextModelData.fromString、validatePosition 等 | ✅ Published. 测试基线 1085 passed。 |
| [`#delta-2025-12-04-llm-native-filtering`](../indexes/README.md#delta-2025-12-04-llm-native-filtering) | LLM-Native 功能筛选，~20% 工时节省 | ✅ Published. 计划文档：`docs/plans/llm-native-editor-features.md`。 |
| [`#delta-2025-12-04-sprint05-start`](../indexes/README.md#delta-2025-12-04-sprint05-start) | Sprint 05 启动，测试突破 1000 达到 1008 | ✅ Published. |
| [`#delta-2025-12-02-sprint04-m2`](../indexes/README.md#delta-2025-12-02-sprint04-m2) | Sprint 04 M2 全部完成，测试基线 873→1008 | ✅ Published. 里程碑完成。 |
| [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown) | DocUI/Markdown placeholder | ⚠️ Gap - 仍需 Intl/decoration ingestion 后续工作。 |

## Current Focus
- **OPS-Index 完成**：`#delta-2025-11-26-sprint04-r1-r11` 列出 WS1–WS5 交付（WS1-PORT-SearchCore/CRLF、WS2-PORT、WS3-PORT-Tree/QA、WS4-PORT-Core、WS5-INV/PORT/QA）。后续 delta 需以 R1-R11 记录为基础增量发布。
- **Sprint 04 广播纪律**：协助 DocMaintainer/Planner 在 Task Board、`AGENTS.md`、`docs/sprints/sprint-04.md` 中引用 `#delta-2025-11-26-sprint04` + R1-R11 汇总，防止 WS 状态漂移。
- **AA4 CL7/CL8 占位监管**：所有 Cursor/Snippet/DocUI 讨论必须携带 `#delta-2025-11-26-aa4-cl7-cursor-core` / `#delta-2025-11-26-aa4-cl8-markdown`，并指向 `docs/reports/audit-checklist-aa4.md`。
- **Baseline 证据对齐**：继续把 TextModelSearch/SearchOffset rerun 命令写入 `tests/TextBuffer.Tests/TestMatrix.md`，并将 585/585 全量 run 记录关联到 `agent-team/handoffs/WS123-QA-Result.md` & `WS5-QA-Result.md`。
- **Member docs alignment**：2025-11-27 对 DocMaintainer/Investigator-TS/Porter-CS/QA-Automation 快照执行 anchor + checklist review，确认它们都指向 `#delta-2025-11-26-sprint04-r1-r11` 与 AA4 CL7/CL8 placeholder。

## Open Dependencies
- Planner owes an updated runSubAgent template that reserves a block for changefeed pointers from Sprint04 and Alignment Audit anchors.
- DocMaintainer to confirm which audit actions become WS4 backlog so Info-Indexer can precreate entries under `oi-backlog.md`.
- QA-Automation to flag any rerun drift on TextModelSearch/SearchOffset so we can refresh the commands recorded under the respective anchors.

## Checklist
1. `agent-team/handoffs/WS1-PORT-SearchCore-Result.md`、`WS2-PORT-Result.md`、`WS3-PORT-Tree-Result.md`、`WS4-PORT-Core-Result.md`、`WS5-PORT-Harness-Result.md` —— 将每个 handoff 的 “Verification / QA” 行映射回 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 表格，并在 indexes/Task Board 中保持顺序一致。
2. `agent-team/handoffs/WS123-QA-Result.md`、`WS5-QA-Result.md`、`tests/TextBuffer.Tests/TestMatrix.md` —— 提取 585/585（1 skip）`PIECETREE_DEBUG=0` rerun 指令写入 changefeed 表，持续证明 Sprint 04 R1-R11 的单一 baseline。
3. `agent-team/handoffs/B3-TextModelSearch-INV.md`、`B3-TextModelSearch-QA.md` —— 记录 Intl.Segmenter & WordSeparator cache 未结项，始终链接到 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) 并交叉引用 Task Board/Index backlog。
4. `agent-team/handoffs/WS5-INV-TestBacklog.md`、`docs/reports/audit-checklist-aa4.md`、`agent-team/handoffs/AA4-006-Plan.md`、`AA4-006-Result.md`、`AA4-008-Result.md` —— Cursor/Snippet backlog 与 DocUI scope 变更必须同步 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-24-b3-docui-staged`](../indexes/README.md#delta-2025-11-24-b3-docui-staged)、[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)。

## Open Investigations
- **Intl.Segmenter & WordSeparator cache**：挂在 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch)，等待 Investigator/Porter 给出统一 cache 策略后发布新的 delta。
- **Cursor/Snippet backlog (CL7)**：`agent-team/handoffs/WS4-PORT-Core-Result.md` + `WS5-INV-TestBacklog.md` 标记的 ColumnSelectData/SnippetController 仍未落地；保持 `#delta-2025-11-26-aa4-cl7-cursor-core` 为 Gap。
- **DocUI find/replace scope + Markdown renderer (CL8)**：`B3-FC-Result.md` 与 `B3-DocUI-StagedFixes-QA-20251124.md` 仅覆盖第一批修复，Intl cache/WordSeparator backlog 仍需在 `#delta-2025-11-26-aa4-cl8-markdown` 下跟踪。

## Archives
- Full worklogs, onboarding notes, and prior deltas now live in `agent-team/handoffs/` (per-run files) and the historical rows inside `agent-team/indexes/README.md`. Older memory snapshots remain in repo history if needed.

## Log
> 2025-11 活动历史已压缩。详见 `agent-team/archive/info-indexer-log-202511.md`。

- 2025-12-01 – 团队角色测试谈话：确认 changefeed 索引管理员角色
