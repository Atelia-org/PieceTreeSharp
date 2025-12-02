# DocMaintainer Memory (Snapshot 2025-11-27)

## Role & Mission
- **Consistency Gatekeeper：** 维持 `AGENTS.md`、`agent-team/task-board.md`、`docs/sprints/*` 的叙述一致，并在每条更新中引用最新 changefeed + migration log。
- **Info Proxy：** 为主 Agent / SubAgent 汇总 `docs/reports/migration-log.md`、`agent-team/indexes/README.md` 的关键信息，减少 token 压力。
- **Doc Gardener：** 控制文档体积，必要时把冗长记录移入 handoff/archives，并在核心文档留下指针。
- **Anchor Steward：** 任何 Sprint 04 / AA4 更新都要引用 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 以及 CL7/CL8 占位 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)，确保 Cursor/Snippet、DocUI、Intl cache 讨论都有 canonical 指针。

## Canonical Anchors
| Anchor | 用途 |
| --- | --- |
| [`#delta-2025-12-02-sprint04-m2`](../indexes/README.md#delta-2025-12-02-sprint04-m2) | Sprint 04 M2 全部完成里程碑（873 passed, 9 skipped），Snippet/Cursor/IntervalTree 交付汇总。 |
| [`#delta-2025-12-02-snippet-p2`](../indexes/README.md#delta-2025-12-02-snippet-p2) | Snippet P0-P2 全部完成（77 tests），Variable Resolver 实现。 |
| [`#delta-2025-12-02-ws3-textmodel`](../indexes/README.md#delta-2025-12-02-ws3-textmodel) | IntervalTree AcceptReplace 集成到 TextModel。 |
| [`#delta-2025-11-28-sprint04-r13-r18`](../indexes/README.md#delta-2025-11-28-sprint04-r13-r18) | CL7 Stage1 完成，WordOperations 重写。 |
| [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) | Sprint 04 R1-R11 批量完成（365→585 测试），覆盖 WS1-WS5 deliverables。 |
| [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown) | CL8 DocUI/Markdown placeholder，延迟中。 |

## Current Focus（2025-12-02）
- **Sprint 04 M2 完成**：测试基线 **873 passed, 9 skipped**
- 核心交付：Snippet P0-P2、CursorCollection/WordOperations、IntervalTree AcceptReplace、FindModel/FindDecorations
- **文档纪律**：修改 AGENTS / Sprint / Task Board 之前，先比对 migration-log 与 changefeed，在条目中引用二者

## Coordination Hooks
- **Info-Indexer**：及时共享新增 delta / changefeed 清理计划；DocMaintainer据此刷新"状态提示"段落。
- **Planner**：在 runSubAgent 循环中先行安装 DocMaintainer hooks（playbook 第三阶段）以免遗漏文档步骤。
- **Porter-CS & QA-Automation**：当实现/测试交付后，若文档尚未引用最新 rerun 结果，可直接抛 doc-fix handoff，由 DocMaintainer 执行。

## Checklist
1. **Sprint 04 Handoffs** — WS1-WS5 PORT/QA 结果已同步到 AGENTS/Task Board，引用 `#delta-2025-11-26-sprint04-r1-r11`
2. **测试基线** — 873 passed + 9 skipped，维护 TestMatrix 与 migration-log 一致性
3. **AA4 CL7/CL8** — Cursor/Snippet/DocUI backlog 引用 `#delta-2025-11-26-aa4-cl7-*` / `#delta-2025-11-26-aa4-cl8-*`

## Open Investigations
1. **Intl.Segmenter & WordSeparator cache**：延迟中，参考 `#delta-2025-11-25-b3-textmodelsearch`
2. **Changefeed Archive Hygiene**：待按 Info-Indexer 指引归档旧 delta

## Last Update
- **Date**: 2025-12-02
- **Task**: Team Member 认知文件维护（Team Leader 请求）
- **Result**: ✅ 完成 8 个文件的精简更新：
  - 更新测试基线为 873 passed + 9 skipped
  - 压缩 11 月活动历史到归档文件
  - 删除冗余的 Checklist 项目
  - 保持格式一致性

```
