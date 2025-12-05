# Task Board - Phase 8: Sprint 05 – LLM-Native Editor Features

> **Sprint 04 归档**: [`task-board-v8-sprint04-archive.md`](task-board-v8-sprint04-archive.md)

**Sprint Window:** 2025-12-02 ~ 2025-12-16  
**Goal:** 基于 LLM-Native 视角精简剩余 gaps，完成 P1/P2 优先级任务，实现测试基线突破 1000。

**Milestone Status:**
- ✅ M1 - Diff 核心修复 & API 补齐 (完成 2025-12-02)
- ✅ M2 - P1 任务清零 (完成 2025-12-04)
- ✅ M3 - P2 任务清零 (完成 2025-12-05)
- 🔄 M4 - P3 选择性实施 (进行中)

**Test Baseline:** 1158 passed, 9 skipped (首次突破 1000! 🎉)

**Changefeed Reminder:** 所有状态更新请同步到 `agent-team/indexes/README.md#delta-2025-12-*`；详细进度见 [`docs/sprints/sprint-05.md`](../docs/sprints/sprint-05.md)。

---

## LLM-Native 功能筛选结果

基于 [`docs/plans/llm-native-editor-features.md`](../docs/plans/llm-native-editor-features.md) 重新评估剩余 gaps：

| 分类 | Gap 数量 | 工时影响 | Status |
|------|---------|---------|--------|
| ❌ 无需移植 | 7 | ~14h 节省 | ✅ 评估完成 |
| 🔄 降级实现 | 8 | ~18h → ~8h | P3 计划中 |
| ✅ 继续移植 | 11 | ~26h | ✅ P1/P2 完成 |

**无需移植的功能**（已明确排除）:
- Sticky Column（人类键盘导航）
- FindStartFocusAction / 焦点管理（无 GUI）
- Mac global clipboard write（平台 hook）
- shouldAnimate / Delayer 节流（视觉动画）
- Bracket pair colorization（纯视觉）
- lineBreak + InjectedText viewport（视口特定）
- Snippet P3 嵌套语法（复杂度高，使用罕见）

---

## P1 任务 (高优先级核心 API) - ✅ 全部完成

| ID | Description | Owner | Tests | Changefeed |
|----|-------------|-------|-------|------------|
| P1-1 | TextModelData.fromString | Porter-CS | +5 | [`#delta-2025-12-04-p1-complete`](indexes/README.md#delta-2025-12-04-p1-complete) |
| P1-2 | validatePosition 边界测试 | QA-Automation | +44 | [`#delta-2025-12-04-p1-complete`](indexes/README.md#delta-2025-12-04-p1-complete) |
| P1-3 | getValueLengthInRange + EOL | Porter-CS | +5 | [`#delta-2025-12-04-p1-complete`](indexes/README.md#delta-2025-12-04-p1-complete) |
| P1-4 | Issue regressions 调研 | Investigator-TS | N/A | [`#delta-2025-12-04-p1-complete`](indexes/README.md#delta-2025-12-04-p1-complete) |
| P1-5 | SelectAllMatches 排序 | Porter-CS | ✅ | (Sprint 04 完成) |

**P1 测试增长**: +54 tests  
**P1 完成日期**: 2025-12-04

---

## P2 任务 (重要测试与特性) - ✅ 全部完成

| ID | Description | Owner | Tests | Changefeed |
|----|-------------|-------|-------|------------|
| P2-1 | Diff deterministic matrix | QA-Automation | +44 | [`#delta-2025-12-04-p1-complete`](indexes/README.md#delta-2025-12-04-p1-complete) |
| P2-2 | PieceTree diagnostics | Porter-CS | +23 | [`#delta-2025-12-04-p1-complete`](indexes/README.md#delta-2025-12-04-p1-complete) |
| P2-3 | Decorations multi-owner | Porter-CS | 🔄 存储层 | [`#delta-2025-12-02-ws3-textmodel`](indexes/README.md#delta-2025-12-02-ws3-textmodel) |
| P2-4 | AddSelectionToNextFindMatch | Porter-CS | +34 | [`#delta-2025-12-05-add-selection-to-next-find`](indexes/README.md#delta-2025-12-05-add-selection-to-next-find) |
| P2-5 | MultiCursor Snippet 集成 | QA-Automation | +6 | [`#delta-2025-12-05-multicursor-snippet`](indexes/README.md#delta-2025-12-05-multicursor-snippet) |
| P2-6 | Snippet Transform | Porter-CS | +33 | [`#delta-2025-12-05-snippet-transform`](indexes/README.md#delta-2025-12-05-snippet-transform) |

**P2 测试增长**: +140 tests  
**P2 完成日期**: 2025-12-05  
**P2 关键交付**:
- Snippet Transform + FormatString（直译 TS snippetParser.ts）
- MultiCursorSession + MultiCursorSelectionController
- Diff deterministic matrix（59→103 tests）

---

## P3 任务 (降级实现 & 选择性完成) - 🔄 进行中

| ID | Description | 分类 | 工时估计 | Owner | Status |
|----|-------------|------|---------|-------|--------|
| P3-1 | 解除 SelectHighlightsAction skipped test | 降级实现 | ~2h | TBD | Planned |
| P3-2 | 解除 MultiCursorSnippet skipped test | 降级实现 | ~2h | TBD | Planned |
| P3-3 | Snippet Variables 扩展 | 降级实现 | ~2h | TBD | Planned |
| P3-4 | Multi-cursor session merge | 降级实现 | ~1h | TBD | Planned |
| P3-5 | InsertCursorAbove/Below | 降级实现 | ~0.5h | TBD | Planned |
| P3-6 | guessIndentation 扩展 | 降级实现 | ~1.5h | TBD | Planned |
| P3-7 | editStack 边界测试 | 降级实现 | ~0.5h | TBD | Planned |

**预计总工时:** ~9.5h  
**降级原则**: 只实现 LLM-Native 场景必需的功能，不追求完整 VS Code parity

---

## Cross-Sprint 持续任务

| ID | Description | Owner | Status | Notes |
|----|-------------|-------|--------|-------|
| OPS-1 | 维护 Sprint 05 Progress Log | DocMaintainer | 🔄 持续 | [`docs/sprints/sprint-05.md`](../docs/sprints/sprint-05.md) |
| OPS-2 | Changefeed 及时创建 | Info-Indexer | 🔄 待流程优化 | 见 [`handoffs/DocMaintainer-to-InfoIndexer-2025-12-05.md`](handoffs/DocMaintainer-to-InfoIndexer-2025-12-05.md) |
| OPS-3 | TestMatrix 同步更新 | QA-Automation | 🔄 持续 | [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md) |

---

## Sprint 04 快速回顾

**完成时间**: 2025-11-27 ~ 2025-12-02  
**测试增长**: 585 → 873 passed (+288)  
**关键交付**:
- WS1-WS5 全部完成（PieceTree Search、Range/Selection Helpers、IntervalTree、Cursor/Snippet、高风险测试）
- Snippet P0-P2 实现（77 tests）
- CursorCollection + WordOperations（94 tests）
- IntervalTree AcceptReplace 集成

**详细记录**: [`task-board-v8-sprint04-archive.md`](task-board-v8-sprint04-archive.md)

---

## References
- **Sprint Log**: [`docs/sprints/sprint-05.md`](../docs/sprints/sprint-05.md)
- **Migration Log**: [`docs/reports/migration-log.md`](../docs/reports/migration-log.md)
- **Changefeed Index**: [`agent-team/indexes/README.md`](indexes/README.md)
- **Test Matrix**: [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md)
- **LLM-Native Features**: [`docs/plans/llm-native-editor-features.md`](../docs/plans/llm-native-editor-features.md)

---

_Sprint 04 的详细 workstreams (WS1-WS5) 和 Cross-Stream Ops 已归档至 [`task-board-v8-sprint04-archive.md`](task-board-v8-sprint04-archive.md)。_

