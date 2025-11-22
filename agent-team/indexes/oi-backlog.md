# OI Backlog – Organizational Infrastructure Improvements

本文档记录跨 Sprint 的组织性改进任务，优先级低于主线 PieceTree 移植工作，但对长期质量/效率有重要影响。

## Active Backlog

| OI ID | Title | Category | Priority | Assignee | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| OI-012 | DocUI Widget 测试框架设计 | Testing | P2 | QA-Automation | Planned | 为 FindWidget/ReplaceWidget 创建最小化 DOM harness 或 snapshot 工具 |
| OI-013 | Snapshot Tooling（Markdown 快照自动化） | Testing | P2 | QA-Automation | Planned | 支持 `DOCUI_SNAPSHOT_RECORD=1` 模式，自动生成 before/after Markdown |
| OI-014 | WordSeparator Parity（完整对齐） | Parity | P3 | Porter-CS | Planned | 实现 LRU cache、Intl.Segmenter（ICU4N）、`getWordAtText()` API |
| OI-015 | DocUI Harness 标准化设计 | Architecture | P2 | Planner | Planned | 为所有 DocUI widget（Find/Replace/Snippet）设计统一的测试 harness 接口 |

## Completed

（暂无）

## Rejected

（暂无）

---

## 详细说明

### OI-012: DocUI Widget 测试框架设计

**背景**：  
TS 端 `contrib/find` 无专用 FindWidget DOM 测试（仅 findModel/findController/find/replacePattern 测试），C# 端需决定替代方案。

**目标**：
- 评估 Markdown snapshot vs 最小化 DOM harness
- 设计 `DocUITestHarness` 接口（供 FindModel/ReplacePattern/Snippet 共用）
- 创建示例测试（1-2 个）验证可行性

**验收标准**：
- 设计文档：`docs/plans/docui-harness-design.md`
- 示例测试：`DocUIHarnessExampleTests.cs`（可选）

**依赖**：Batch #2 完成后启动

**参考**：
- `B2-INV-Result.md` § 2: FindWidget 测试路径定位
- `ts/src/vs/editor/contrib/find/test/browser/` 测试结构

---

### OI-013: Snapshot Tooling（Markdown 快照自动化）

**背景**：  
Batch #1 手动创建 fixtures，缺乏自动化 snapshot 工具。

**目标**：
- 实现 `DOCUI_SNAPSHOT_RECORD=1` 环境变量支持
- 在测试运行时自动生成 Markdown 快照到 `__snapshots__/` 目录
- 提供 `AssertSnapshot(expected, actual)` 辅助方法

**验收标准**：
- `SnapshotHelper.cs`（录制 + 对比工具）
- 更新 `TestMatrix.md` 说明 snapshot 使用方法

**依赖**：独立任务，可随时启动

**参考**：
- `B1-QA-Result.md` 建议：Consider implementing a snapshot tooling
- Jest snapshot testing 模式

---

### OI-014: WordSeparator Parity（完整对齐）

**背景**：  
B2-INV 调研发现 C# 缺 LRU cache、Intl.Segmenter、`getWordAtText()` API。

**目标**：
- 实现 `WordCharacterClassifierCache`（LRU，容量 10）
- 集成 ICU4N 或文档化 CJK/Thai 分词限制
- 实现 `getWordAtText()` API（用于 hover/word-under-cursor）

**验收标准**：
- `WordCharacterClassifierCache.cs`
- `WordHelperTests.cs`（LRU 行为测试）
- 更新 `docs/reports/migration-log.md` 记录完整对齐

**依赖**：Batch #2 完成后启动（P3 优先级）

**参考**：
- `B2-INV-Result.md` § 1: WordSeparator 规格摘要
- `ts/src/vs/editor/common/model/wordHelper.ts`
- `ts/src/vs/editor/common/model/wordCharacterClassifier.ts`

---

### OI-015: DocUI Harness 标准化设计

**背景**：  
每个 widget 都需要测试 harness，避免重复设计。

**目标**：
- 设计 `IDocUIWidget` 接口（抽象 FindWidget/ReplaceWidget/SnippetSession）
- 定义 `DocUITestHarness<T>` 泛型类
- 提供示例：`DocUIFindHarness`、`DocUIReplaceHarness`

**验收标准**：
- 设计文档：`docs/plans/docui-harness-design.md`
- 接口定义：`IDocUIWidget.cs`、`DocUITestHarness.cs`

**依赖**：OI-012 完成后启动

**参考**：
- `B2-INV-Result.md` § 2: 建议跳过 DOM widget 测试，聚焦 FindModel 逻辑层
- `ts/src/vs/editor/contrib/find/test/browser/findController.test.ts` 的 stub 模式

---

## 更新日志

- **2025-11-22**：创建 OI Backlog，登记 OI-012~015（DocUI 测试框架、Snapshot 工具、WordSeparator 完整对齐、DocUI Harness 标准化），所有任务标记为 Planned 状态。
