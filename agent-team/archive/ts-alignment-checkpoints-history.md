# TS Test Alignment Checkpoint History

> 本文件归档了 `docs/plans/ts-test-alignment.md` 中的历史 checkpoint 详情。
> 当前状态请查看主计划文件的 Live Checkpoints 部分。

## Sprint 03 Checkpoints (2025-11-22 ~ 2025-11-26)

### 2025-11-26（B3-TextModelSearch QA broadcast）
Info-Indexer Run R38 在 `agent-team/indexes/README.md#delta-2025-11-25-b3-textmodelsearch` 固化 QA 45/45（2.5s targeted，`PIECETREE_DEBUG=0`）+ 365/365（61.6s full）统计并引用 `agent-team/handoffs/B3-TextModelSearch-QA.md`；DocMaintainer Run R39 完成 doc sweep。

### 2025-11-24 (Batch #3 系列)
- **B3-FM multi-selection 交付**：`TestEditorContext` 多选区 plumbing、`FindModel.SetSelections()` 主光标排序、`DocUIFindModelTests.Test07/08` 回归与 QA rerun（2/2 + 48/48 → 全量 242/242）。
- **B3-FM scope & scoped replace fix**：DocUIFindModelTests 新增 Test45/46/47，QA rerun 44/44→45/45。
- **B3 DocUI staged fixes**：FindDecorations reset + caret overlap repairs，QA rerun 46/46 + 9/9。
- **PieceTree fuzz 计划**：R25→R29 harness 与 deterministic suites。

### 2025-11-23 (Batch #3 系列)
- **Decor Stickiness Investigation & 交付**：range highlight/overview throttling、stickiness 矩阵、per-line 查询 API。QA 233/233→235/235。
- **B3-FC lifecycle & seeding**：DocUI FindController 行为修正（Ctrl+F reseed、lifecycle disposal）。
- **Batch #3 规划初稿**：定义 B3-FM/FSel/FC-Core/FC-Scope/Decor-Stickiness/PieceTree-Fuzz/Diff-Pretty 子批次。

### 2025-11-22 (Batch #2 规划)
Batch #2 拆解为 5 个 runSubAgent 任务（B2-001~005），移植 FindModel 逻辑层 + findModel.test.ts 核心场景。

---

## Batch #1 (2025-11-22)
ReplacePattern 全链路（Porter→QA→Info-Indexer→DocMaintainer）完成，23 tests / 142/142。

---

*Created: 2025-12-02 by DocMaintainer*
*Source: docs/plans/ts-test-alignment.md Live Checkpoints section*
