# DocMaintainer Memory (Snapshot 2025-11-27)

## Role & Mission
- **Consistency Gatekeeper：** 维持 `AGENTS.md`、`agent-team/task-board.md`、`docs/sprints/*` 的叙述一致，并在每条更新中引用最新 changefeed + migration log。
- **Info Proxy：** 为主 Agent / SubAgent 汇总 `docs/reports/migration-log.md`、`agent-team/indexes/README.md` 的关键信息，减少 token 压力。
- **Doc Gardener：** 控制文档体积，必要时把冗长记录移入 handoff/archives，并在核心文档留下指针。
- **Anchor Steward：** 任何 Sprint 04 / AA4 更新都要引用 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 以及 CL7/CL8 占位 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)，确保 Cursor/Snippet、DocUI、Intl cache 讨论都有 canonical 指针。

## Canonical Anchors
| Anchor | 用途 |
| --- | --- |
| [`#delta-2025-12-05-p2-complete`](../indexes/README.md#delta-2025-12-05-p2-complete) | Sprint 05 P2 任务全部完成（1158 passed），Snippet Transform、MultiCursor、AddSelectionToNextFindMatch。 |
| [`#delta-2025-12-05-snippet-transform`](../indexes/README.md#delta-2025-12-05-snippet-transform) | Snippet Transform + FormatString 完整实现（33 tests）。 |
| [`#delta-2025-12-05-add-selection-to-next-find`](../indexes/README.md#delta-2025-12-05-add-selection-to-next-find) | AddSelectionToNextFindMatch 完整实现（34 tests）。 |
| [`#delta-2025-12-04-p1-complete`](../indexes/README.md#delta-2025-12-04-p1-complete) | Sprint 05 P1 任务全部完成（1085 passed）。 |
| [`#delta-2025-12-04-llm-native-filtering`](../indexes/README.md#delta-2025-12-04-llm-native-filtering) | LLM-Native 功能筛选（7 gaps 无需移植，8 gaps 降级）。 |
| [`#delta-2025-12-02-sprint04-m2`](../indexes/README.md#delta-2025-12-02-sprint04-m2) | Sprint 04 M2 全部完成里程碑（873 passed, 9 skipped），Snippet/Cursor/IntervalTree 交付汇总。 |
| [`#delta-2025-12-02-snippet-p2`](../indexes/README.md#delta-2025-12-02-snippet-p2) | Snippet P0-P2 全部完成（77 tests），Variable Resolver 实现。 |
| [`#delta-2025-12-02-ws3-textmodel`](../indexes/README.md#delta-2025-12-02-ws3-textmodel) | IntervalTree AcceptReplace 集成到 TextModel。 |

## Current Focus（2025-12-05）
- **Sprint 05 P1/P2 完成**：测试基线 **1158 passed, 9 skipped**
- 核心交付：Snippet Transform、MultiCursor Snippet 集成、AddSelectionToNextFindMatch 完整实现
- **文档缺口修复**: 补充了 Sprint 05 文档链、changefeed 和 migration-log 记录

## Coordination Hooks
- **Info-Indexer**：及时共享新增 delta / changefeed 清理计划；DocMaintainer据此刷新"状态提示"段落。
  - ✅ 2025-12-05 反馈：手动补录的 8 个 changefeed 全部通过审阅
  - ✅ **方案 A+C 已批准**：Sprint log 为单一事实来源，changefeed 为轻量指针
  - **触发条件**：测试基线 +20 / feat/fix commits / Batch 完成
  - **Sprint log 格式**：使用 `<a id="batch-N"></a>` HTML anchor
- **Planner**：在 runSubAgent 循环中先行安装 DocMaintainer hooks（playbook 第三阶段）以免遗漏文档步骤。
- **Porter-CS & QA-Automation**：当实现/测试交付后，若文档尚未引用最新 rerun 结果，可直接抛 doc-fix handoff，由 DocMaintainer 执行。

## Checklist
1. **Sprint 05 文档完整性** — ✅ 创建 `docs/sprints/sprint-05.md`，补充 changefeed 和 migration-log
2. **测试基线** — 1158 passed + 9 skipped，所有文档已同步
3. **Task Board 更新** — ✅ 从 Sprint 04 更新为 Sprint 05，添加 P1/P2/P3 任务表
4. **Changefeed 一致性** — ✅ AGENTS.md / status.md / todo.md 所有条目都引用最新 changefeed

## Open Investigations
1. **P3 任务实施** — 9.5h 剩余工作（降级实现，按需完成）
2. **Changefeed Archive Hygiene** — 待按 Info-Indexer 指引归档旧 delta

## Last Update
- **Date**: 2025-12-05
- **Task**: 执行 Team Leader 批准的流程改进任务
- **Result**: ✅ 完成分配给 DocMaintainer 的所有 P0/P1 任务：
  1. ✅ 为 `docs/sprints/sprint-05.md` 添加 HTML anchors（5 个 anchor）
     - `#batch-1` — 2025-12-02 Sprint 启动
     - `#batch-2` — 2025-12-04 P1 清零
     - `#batch-3` — 2025-12-05 全天
     - `#batch-4` — Session 1 Snippet Transform
     - `#batch-5` — Session 2 AddSelectionToNextFindMatch
  2. ✅ 更新 `docs/sprints/sprint-template.md` 为方案 A+C 格式
     - 添加 HTML anchor 使用说明
     - 添加触发条件注释
     - 添加 changefeed 指针格式示例
  3. ✅ 更新 status.md / todo.md 标记任务完成状态
  4. ✅ 补充 sprint-05.md 中的 changefeed 引用链接

**协作成果**: 
- DocMaintainer → Info-Indexer → Team Leader 三方协作完成流程改进
- 从问题发现到方案批准再到执行，全程 1 天内完成
- 建立了可持续的文档治理机制

```
