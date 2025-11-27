# Info-Indexer Snapshot · 2025-11-26 (Updated)

## Role & Mission
- Maintain `agent-team/indexes/README.md` plus downstream index files so every changefeed has a single canonical pointer.
- Keep AGENTS/Sprint/Task Board docs lightweight by offloading detailed histories into `agent-team/indexes/*.md` and linked handoffs.
- Surface blockers that threaten broadcast cadence before DocMaintainer kicks off each sweep.
- Make sure every Sprint 04 drop references [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) and any Cursor/Snippet or DocUI notes cite [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)/[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown).

## Active Changefeeds & Backlog
| Anchor | Coverage | Status / Next Hook |
| --- | --- | --- |
| [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) | Sprint 04 R1–R11 交付汇总（WS1–WS5 首批里程碑） | ✅ Published 2025-11-26. 测试基线 365→585 (584 pass + 1 skip)。下一个 WS drop 需追加子 delta。 |
| [`#delta-2025-11-26-sprint04`](../indexes/README.md#delta-2025-11-26-sprint04) | Sprint 04 tracker + new Task Board (WS1–WS5). | Monitor WS1/WS3 drops; when new deltas publish, mirror them into `agent-team/indexes/README.md` and ensure Task Board links stay aligned. |
| [`#delta-2025-11-26-alignment-audit`](../indexes/README.md#delta-2025-11-26-alignment-audit) | Alignment audit bundle refresh (00–08). | Watch DocMaintainer follow-ups; log any remediation deltas into `core-docs-index.md` once owners commit fixes. |
| [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) | TextModelSearch 45-test parity set. | Track additional AA4 CL7 search tasks; confirm new suites update `tests/TextBuffer.Tests/TestMatrix.md` before changing baseline references. |
| [`#delta-2025-11-25-b3-search-offset`](../indexes/README.md#delta-2025-11-25-b3-search-offset) | PieceTree search-offset cache deterministics. | Await follow-up perf notes from WS2; if cache tuning lands, append evidence links + rerun commands to the changefeed table. |
| [`#delta-2025-11-24-b3-docui-staged`](../indexes/README.md#delta-2025-11-24-b3-docui-staged) | DocUI Find/Replace scope + decorations cache。 | Hold DocUI entries in Sprint Log/Task Board until DocMaintainer confirms rerun evidence + AA4 CL8 delta landed. |
| [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) / [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown) | Cursor/Snippet backlog + DocUI/Markdown placeholder (AA4 CL7/CL8)。 | Remain Gap until new Porter/QA drops cite these anchors; update Task Board/TestMatrix rows only after Info-Indexer publishes successor deltas. |

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
- 2025-11-27 – Refreshed `docs/reports/alignment-audit/06-services.md` to highlight `docs/reports/migration-log.md#ws2-port` helper ripple, `docs/reports/migration-log.md#ws5-qa` targeted runs, the `agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11` 585/585 baseline, and the open `#delta-2025-11-26-aa4-cl8-markdown`/`-capture`/`-intl`/`-wordcache` placeholders for Intl + word cache gaps.
