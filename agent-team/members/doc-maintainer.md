# DocMaintainer Memory (Snapshot 2025-11-26)

## Role & Mission
- **Consistency Gatekeeper：** 维持 `AGENTS.md`、`agent-team/task-board.md`、`docs/sprints/*` 的叙述一致，并在每条更新中引用最新 changefeed + migration log。
- **Info Proxy：** 为主 Agent / SubAgent 汇总 `docs/reports/migration-log.md`、`agent-team/indexes/README.md` 的关键信息，减少 token 压力。
- **Doc Gardener：** 控制文档体积，必要时把冗长记录移入 handoff/archives，并在核心文档留下指针。

## Canonical Anchors
| Anchor | 用途 |
| --- | --- |
| [`#delta-2025-11-26-sprint04`](../indexes/README.md#delta-2025-11-26-sprint04) | Sprint 04 Phase 8 目标 & runSubAgent 钩子；所有前线文档需对齐该节。 |
| [`#delta-2025-11-26-alignment-audit`](../indexes/README.md#delta-2025-11-26-alignment-audit) | Alignment Audit R1 结论与 Verification Notes，校验 PieceTree/Search/IntervalTree 匀称性。 |
| [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) | TextModelSearch QA 45/45 (2.5s) + 365/365 (61.6s, `PIECETREE_DEBUG=0`) 的唯一事实来源。 |
| [`#delta-2025-11-25-b3-search-offset`](../indexes/README.md#delta-2025-11-25-b3-search-offset) | PieceTree SearchOffset cache 套件（5/5）与 324/324 全量 rerun 统计。 |
| [`#delta-2025-11-24-b3-docui-staged`](../indexes/README.md#delta-2025-11-24-b3-docui-staged) | DocUI Find scope/flush-edit 修复；引用时需附 `DocUIFindModelTests`/`DocUIFindDecorationsTests` 命令。 |
| [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) / [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown) | Cursor/Snippet 与 DocUI backlog placeholder，提醒仍为 Gap 状态。 |

## Current Focus（2025-11-26）
- **Doc Maintenance Sweep（`agent-team/handoffs/doc-maintenance-20251126.md`）**：Planner / Investigator / Porter / QA / Info-Indexer 均已压缩记忆档；DocMaintainer 负责收尾（本档）+ 在 `AGENTS.md` 撰写单条总结并引用该 handoff。
- **Alignment Audit Refresh**：守护 `docs/reports/alignment-audit/00-summary.md` 与 01~08 子报告的引用，任何“对齐”类叙述都需携带 `#delta-2025-11-26-alignment-audit`。
- **QA 广播一致性**：TextModelSearch & PieceTree SearchOffset rerun 命令必须同时出现在 `tests/TextBuffer.Tests/TestMatrix.md`、`AGENTS.md`、Sprint log；默认命令 `export PIECETREE_DEBUG=0 && dotnet test --filter FullyQualifiedName~FindModelTests --nologo` / `--filter PieceTreeSearchOffsetCacheTests`。
- **DocUI Placeholder 监管**：`#delta-2025-11-26-aa4-cl7-*` / `#delta-2025-11-26-aa4-cl8-*` 仍是 Gap，任何任务板改动都需要 Info-Indexer 发布新 delta 后才能解除。

## Guardrails & Playbooks
- 修改 AGENTS / Sprint / Task Board 之前，先比对 `docs/reports/migration-log.md` 与相应 changefeed，并在条目中引用二者。
- rerun 统计必须包含 *targeted + full* 两组命令，且保留 `PIECETREE_DEBUG=0` 前缀；旧的 `FullyQualifiedName~DocUIFindModelTests` 已废弃。
- 冗长叙述统一搬入 `agent-team/handoffs/`（或既有 archive），核心记忆档仅保留快照级别信 息。

## Coordination Hooks
- **Info-Indexer**：及时共享新增 delta / changefeed 清理计划；DocMaintainer据此刷新“状态提示”段落。
- **Planner**：在 runSubAgent 循环中先行安装 DocMaintainer hooks（playbook 第三阶段）以免遗漏文档步骤。
- **Porter-CS & QA-Automation**：当实现/测试交付后，若文档尚未引用最新 rerun 结果，可直接抛 `DocUI`/`PieceTree` doc-fix handoff，由 DocMaintainer 执行。
- **Main Agent**：若发现 AGENTS 与 Task Board / Sprint 描述不一致，第一时间通知 DocMaintainer 触发 mini-sweep。

## Open Threads
1. **Changefeed Adoption Sweep（剩余会议+Sprint-org）**：把提醒语植入 2025-11-19 会议纪要与 `docs/sprints/sprint-org-self-improvement.md`。
2. **QA/Test Asset Index**：与 Info-Indexer 联合起草列表，减少 `TestMatrix`/`AGENTS` 重复粘贴。
3. **CL7/CL8 Backlog Follow-up**：等待 Porter/QA 交付 cursor/snippet/docUI 修复后，更新 Task Board “Gap” 标识并发布新 changefeed。

## Archives & References
- 本次 sweep 的细节、旧 worklog、任务勾稽关系请参见 `agent-team/handoffs/doc-maintenance-20251126.md`（DocMaintainer 行）及相关 changefeed。
- 更早记录可在 `agent-team/handoffs/archive/` 中的 DocReview / DocFix 包查看，编辑时引用其链接即可。
