# Porter-CS Snapshot (2025-12-02)

## Role & Mission
- Own the TS → C# PieceTree/TextModel port, keep `src/TextBuffer` and `tests/TextBuffer.Tests` aligned with upstream semantics, and surface deltas through handoffs plus migration logs.
- Partner with Investigator-TS and QA-Automation so every drop has a documented changefeed + reproducible rerun recipe before Info-Indexer broadcasts the delta.
- Stamp every Sprint 04 handoff with [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) and keep Cursor/Snippet、DocUI backlog work tied to [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) / [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)。

## Current Focus
- **Snippet-P2-Variables** (完成): 实现 Snippet Variable Resolver 框架。创建 `ISnippetVariableResolver` 接口，实现 `SelectionVariableResolver`（SELECTION, TM_SELECTED_TEXT）、`ModelVariableResolver`（TM_FILENAME）、`CompositeVariableResolver`（组合多 resolver）、`FallbackVariableResolver`（unknown 返回空字符串）。添加 `${VAR}` 和 `${VAR:default}` 语法解析。新增 24 个测试。详见 [Snippet-P2-Variables-Result.md](../handoffs/Snippet-P2-Variables-Result.md)。882/882 测试通过（873 pass + 9 skip）。
- **Snippet-P1.5** (完成): 实现 Placeholder Grouping 功能。添加 `_placeholderGroups` 字典按 index 分组占位符，修改 `NextPlaceholder()`/`PrevPlaceholder()` 按 unique index 导航，添加 `GetCurrentPlaceholderRanges()` / `ComputePossibleSelections()` 方法，修复 stickiness 为 `NeverGrowsWhenTypingAtEdges`。新增 8 个测试。详见 [Snippet-P1.5-Result.md](../handoffs/Snippet-P1.5-Result.md)。831/831 测试通过（826 pass + 5 skip）。
- **WS3-PORT-TextModel** (完成): 将 IntervalTree 的 AcceptReplace 集成到 TextModel。新增 `DecorationsTrees.AcceptReplace` 方法聚合 3 个 scope tree。修改 `IntervalTree.AcceptReplace` 返回 `IReadOnlyList<DecorationChange>`，步骤 (3) 后强制 normalize 确保 `Decoration.Range` 值始终正确。修改 `NormalizeDelta` 同时更新 `Decoration.Range`。删除 `TextModel.AdjustDecorationsForEdit`（~40 行），在 `ApplyPendingEdits` 中调用新的 `_decorationTrees.AcceptReplace`。详见 [WS3-PORT-TextModel-Result.md](../handoffs/WS3-PORT-TextModel-Result.md)。818/823 测试通过（818 pass + 5 skip）。
- **Snippet-P1** (完成): 实现 Snippet P0-P1 功能。Final Tabstop `$0` 支持 + `adjustWhitespace` 缩进对齐。重构 `SnippetSession.cs` (145 → 442 行)，扩展 `SnippetController.cs` (50 → 74 行)，新增 16 个测试到 `SnippetControllerTests.cs`。详见 [Snippet-P1-Result.md](../handoffs/Snippet-P1-Result.md)。823/823 测试通过（818 pass + 5 skip）。
- **WS5-WordOperations** (完成): WordOperations 测试套件扩展 (#2 Priority from WS5-INV)。重构 `WordOperations.cs` (958 行)，重命名 `WordCharacterClassifier` 为 `CursorWordCharacterClassifier` 避免命名空间冲突，创建 `CursorWordOperationsTests.cs` (38 passing + 3 skipped)。详见 [WS5-WordOperations-Result.md](../handoffs/WS5-WordOperations-Result.md) + [`migration-log#ws5-wordoperations`](../../docs/reports/migration-log.md#ws5-wordoperations)。796/801 测试通过（796 pass + 5 skip）。
- **CL8-Phase34** (完成): Renderer 栈实现 Phase 3 & 4。MarkdownRenderer 集成 FindDecorations，枚举值对齐（MinimapPosition、GlyphMarginLane、InjectedTextCursorStops、MinimapSectionHeaderStyle）。详见 [CL8-Phase34-Result.md](../handoffs/CL8-Phase34-Result.md) + [`migration-log#cl8-phase34`](../../docs/reports/migration-log.md#cl8-phase34)。763/763 测试覆盖（761 pass + 2 skip）。
- **CL8-Phase12** (完成): Renderer 栈实现 Phase 1 & 2。修复 `DecorationOwnerIds` 语义（`Any=0` 匹配 TS），添加 `DecorationSearchOptions` 过滤支持。详见 [CL8-Phase12-Result.md](../handoffs/CL8-Phase12-Result.md)。726/726 测试通过（724 pass + 2 skip）。
- **WS5-CursorAtomicMove** (完成): 实现 cursorAtomicMoveOperations 测试套件 (#1 Priority from WS5-INV)。创建 `AtomicTabMoveOperations.cs` + 43 个测试用例。详见 [WS5-CursorAtomicMove-Result.md](../handoffs/WS5-CursorAtomicMove-Result.md)。726/726 测试通过（724 pass + 2 skip）。
- **CL7-Stage1** (完成): Cursor Wiring Stage 1 全部三个 Phase 完成。详见 [CL7-Cursor-Phase1-Result.md](../handoffs/CL7-Cursor-Phase1-Result.md) + [CL7-CursorCollection-Result.md](../handoffs/CL7-CursorCollection-Result.md)。实现 `EnableVsCursorParity` feature flag，`Cursor.cs` dual-mode state fields + `_setState` + tracked ranges，`CursorCollection.cs` 完整重构包括 `SetStates`、`Normalize`、tracked selection lifecycle。641/641 测试通过（639 pass + 2 skip）。
- **WS5-PORT** (完成): 共享测试 Harness 扩展。创建 `TestEditorBuilder`, `CursorTestHelper`, `WordTestUtils`, `SnapshotTestUtils` 四个 helper 类 + Snapshots 目录结构。详见 [WS5-PORT-Harness-Result.md](../handoffs/WS5-PORT-Harness-Result.md)。
- **WS4-PORT-Core** (完成): Stage 0 cursor infrastructure（`CursorConfiguration`, `CursorState`, `CursorContext`）+ 25 `CursorCoreTests`。详见 [WS4-PORT-Core-Result.md](../handoffs/WS4-PORT-Core-Result.md)。
- **WS3-PORT-Tree** (完成): IntervalTree 延迟 Normalize 核心重写。实现 TS 风格 Node 布局、Lazy Delta 语义、Normalization、AcceptReplace 四阶段算法。详见 [WS3-PORT-Tree-Result.md](../handoffs/WS3-PORT-Tree-Result.md)。
- **WS2-PORT** (完成): 实现 P0 Range/Selection Helper APIs，对齐 TS 的 Range/Selection/Position 辅助方法。详见 [WS2-PORT-Result.md](../handoffs/WS2-PORT-Result.md)。
- **WS1-PORT-SearchCore** (完成): 优化 `GetAccumulatedValue` 以支持 LineStarts 快速路径，添加 DEBUG 计数器到 `PieceTreeSearchCache`。详见 [WS1-PORT-SearchCore-Result.md](../handoffs/WS1-PORT-SearchCore-Result.md)。

## Key Deliverables
- **Snippet-P2-Variables** → [Snippet-P2-Variables-Result.md](../handoffs/Snippet-P2-Variables-Result.md): Variable Resolver 框架，`ISnippetVariableResolver` 接口，`SelectionVariableResolver`/`ModelVariableResolver`/`CompositeVariableResolver`/`FallbackVariableResolver` 实现，`${VAR}` 和 `${VAR:default}` 语法解析，24 个新测试，锚点 `#delta-2025-12-02-snippet-p2-variables`。
- **Snippet-P1.5** → [Snippet-P1.5-Result.md](../handoffs/Snippet-P1.5-Result.md): Placeholder Grouping 功能，`_placeholderGroups` 字典，`GetCurrentPlaceholderRanges()` / `ComputePossibleSelections()` 方法，按组导航，stickiness 修复 `NeverGrowsWhenTypingAtEdges`，8 个新测试，锚点 `#delta-2025-12-02-snippet-p1.5`。
- **WS3-PORT-TextModel** → [WS3-PORT-TextModel-Result.md](../handoffs/WS3-PORT-TextModel-Result.md): IntervalTree AcceptReplace 集成到 TextModel，`DecorationsTrees.AcceptReplace` 聚合器，`NormalizeDelta` 更新 `Decoration.Range`，删除旧 `AdjustDecorationsForEdit`，锚点 `#delta-2025-12-02-ws3-port-textmodel`。
- **Snippet-P1** → [Snippet-P1-Result.md](../handoffs/Snippet-P1-Result.md): Final Tabstop `$0` 支持 + `adjustWhitespace` 缩进对齐，重构 `SnippetSession.cs` (442 行) + `SnippetController.cs` (74 行) + 21 测试，锚点 `#delta-2025-12-02-snippet-p1`。
- **WS5-WordOperations** → [WS5-WordOperations-Result.md](../handoffs/WS5-WordOperations-Result.md): 完整 WordOperations 实现 (`WordOperations.cs` 958 行) + `CursorWordCharacterClassifier` + 41 测试（38 pass + 3 skip），锚点 `#delta-2025-11-28-ws5-wordoperations`；迁移日志 [`docs/reports/migration-log.md#ws5-wordoperations`](../../docs/reports/migration-log.md#ws5-wordoperations)。
- **CL8-Phase34** → [CL8-Phase34-Result.md](../handoffs/CL8-Phase34-Result.md): MarkdownRenderer 集成 FindDecorations + 枚举值对齐（MinimapPosition/GlyphMarginLane/InjectedTextCursorStops/MinimapSectionHeaderStyle），锚点 `#delta-2025-11-28-cl8-phase34`；迁移日志 [`docs/reports/migration-log.md#cl8-phase34`](../../docs/reports/migration-log.md#cl8-phase34)。
- **CL8-Phase12** → [CL8-Phase12-Result.md](../handoffs/CL8-Phase12-Result.md): `DecorationOwnerIds` 语义修正（`Any=0`）+ `DecorationSearchOptions` 过滤选项 + 新 API 方法（`GetDecorationsInRange`/`GetAllDecorations`/`GetLineDecorations` 重载），锚点 `#delta-2025-11-28-cl8-phase12`。
- **WS5-CursorAtomicMove** → [WS5-CursorAtomicMove-Result.md](../handoffs/WS5-CursorAtomicMove-Result.md): `AtomicTabMoveOperations.cs` + `CursorAtomicMoveTests.cs` (43 tests), 726/726 总测试通过，锚点 `#delta-2025-11-28-ws5-cursoratomicmove`。
- **CL7-Stage1** → [CL7-Cursor-Phase1-Result.md](../handoffs/CL7-Cursor-Phase1-Result.md) + [CL7-CursorCollection-Result.md](../handoffs/CL7-CursorCollection-Result.md): Phase 1-3 完整实现，`Cursor.cs` + `CursorCollection.cs` 完全对齐 TS cursorCollection.ts/oneCursor.ts，641/641 测试通过。
- **WS5-PORT** → [WS5-PORT-Harness-Result.md](../handoffs/WS5-PORT-Harness-Result.md): 共享测试 Harness (`TestEditorBuilder`, `CursorTestHelper`, `WordTestUtils`, `SnapshotTestUtils`) + Snapshots 目录，540/540 总测试通过。
- **WS4-PORT-Core** → [WS4-PORT-Core-Result.md](../handoffs/WS4-PORT-Core-Result.md): CursorConfiguration/CursorState/CursorContext Stage 0 基础 + 25/25 `CursorCoreTests`，锚点 `#delta-2025-11-26-ws4-port-core`。
- **WS3-PORT-Tree** → [WS3-PORT-Tree-Result.md](../handoffs/WS3-PORT-Tree-Result.md): IntervalTree 延迟 Normalize 核心重写完成，NodeFlags/IntervalNode/Sentinel/Delta语义/Normalization/AcceptReplace 全部实现, 440/440 总测试通过。
- **WS2-PORT** → [WS2-PORT-Result.md](../handoffs/WS2-PORT-Result.md): Range/Selection/TextPosition P0 Helper APIs 移植完成，75 测试，440/440 总测试通过。
- **WS1-PORT-SearchCore** → [WS1-PORT-SearchCore-Result.md](../handoffs/WS1-PORT-SearchCore-Result.md): `GetAccumulatedValue` 优化 + cache DEBUG 计数器, 365/365 测试通过。
- B3 TextModelSearch parity → [B3-TextModelSearch-PORT.md](../handoffs/B3-TextModelSearch-PORT.md), [B3-TextModelSearch-QA.md](../handoffs/B3-TextModelSearch-QA.md), changefeed [#delta-2025-11-25-b3-textmodelsearch](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch).
- Snapshot + deterministic infra → [B3-PieceTree-Snapshot-PORT.md](../handoffs/B3-PieceTree-Snapshot-PORT.md), [B3-TextModel-Snapshot-PORT.md](../handoffs/B3-TextModel-Snapshot-PORT.md), [B3-PieceTree-Deterministic-CRLF-QA.md](../handoffs/B3-PieceTree-Deterministic-CRLF-QA.md); changefeeds `#delta-2025-11-25-b3-piecetree-snapshot`, `#delta-2025-11-25-b3-textmodel-snapshot`, `#delta-2025-11-25-b3-piecetree-deterministic-crlf`.
- Fuzz + search cache hardening → [B3-PieceTree-Fuzz-Harness.md](../handoffs/B3-PieceTree-Fuzz-Harness.md), [B3-PieceTree-Fuzz-Review-PORT.md](../handoffs/B3-PieceTree-Fuzz-Review-PORT.md), [B3-PieceTree-SearchOffset-PORT.md](../handoffs/B3-PieceTree-SearchOffset-PORT.md); changefeeds `#delta-2025-11-23-b3-piecetree-fuzz`, `#delta-2025-11-24-b3-piecetree-fuzz`, `#delta-2025-11-25-b3-search-offset`.
- DocUI Find stack parity → [B3-FC-Result.md](../handoffs/B3-FC-Result.md), [AA4-008-Result.md](../handoffs/AA4-008-Result.md), and the `docs/reports/migration-log.md` rows for `#delta-2025-11-23-b3-fc-core`, `#delta-2025-11-24-find-scope`, `#delta-2025-11-24-b3-docui-staged`.

## Test Baselines
**全量**: `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → **873 pass + 9 skip** (≈100s)

**关键 targeted filters**:
- `--filter SnippetControllerTests` → 80/80 (76p+4s)
- `--filter CursorWordOperationsTests` → 41/41 (38p+3s)
- `--filter CursorCoreTests` → 25/25
- `--filter TextModelSearchTests` → 45/45

## Checklist
1. `agent-team/handoffs/WS1-PORT-SearchCore-Result.md`、`WS1-PORT-CRLF-Result.md`、`WS2-PORT-Result.md`、`WS3-PORT-Tree-Result.md`、`WS4-PORT-Core-Result.md`、`WS5-PORT-Harness-Result.md`、`CL7-Cursor-Phase1-Result.md`、`CL7-CursorCollection-Result.md` —— 把实现备注与 rerun 记录同步到 `docs/reports/migration-log.md` 并指回 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)。
2. `agent-team/handoffs/WS123-QA-Result.md`、`WS5-QA-Result.md`、`tests/TextBuffer.Tests/TestMatrix.md` —— 维护 641/641（639 pass + 2 skip）`PIECETREE_DEBUG=0` 基线并记录 targeted filters，确保 Porter drops、Task Board、changefeed 表格一致。
3. `agent-team/handoffs/B3-TextModelSearch-PORT.md`、`B3-TextModelSearch-QA.md`、`B3-TextModelSearch-INV.md`、`B3-PieceTree-SearchOffset-PORT.md`、`B3-PieceTree-SearchOffset-QA.md` —— 追踪 Intl.Segmenter & WordSeparator cache、SearchOffset cache 调整，所有引用必须指向 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) 与 [`#delta-2025-11-25-b3-search-offset`](../indexes/README.md#delta-2025-11-25-b3-search-offset)。
4. `agent-team/handoffs/WS4-PORT-Core-Result.md`、`WS5-INV-TestBacklog.md`、`AA4-003-Audit.md`、`AA4-004-Audit.md`、`AA4-006-Plan.md`、`AA4-006-Result.md`、`AA4-008-Result.md`、`B3-DocUI-StagedFixes-QA-20251124.md` —— 在 Cursor/Snippet backlog 与 DocUI scope 更新时引用 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-24-b3-docui-staged`](../indexes/README.md#delta-2025-11-24-b3-docui-staged)、[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)。

## Open Investigations / Dependencies
- **CL7-Stage2** (Next): 扩展 `CursorCollectionTests` 测试套件，集成 movement methods 使用 `_setState` 路径，准备 feature flag 默认值切换。
- **CreateNewPieces CRLF 桥接**：TS 使用 `_` 占位符技术处理 CRLF 跨 buffer 边界，C# 版本尚未实现，导致 `GetAccumulatedValue` 无法始终使用快速路径。
- **Intl.Segmenter & WordSeparator cache**：挂在 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch)，需要 Investigator-TS 提供统一 cache 方案后才能扩展非 ASCII 覆盖。
- **Cursor/Snippet backlog (AA4 CL7)**：CL7-Stage1 完成，`#delta-2025-11-26-aa4-cl7-cursor-core` 状态从 Gap 变为 Partial；`WS4-PORT-Core-Result.md` + `WS5-INV-TestBacklog.md` 列出的 ColumnSelectData、SnippetSession、CommandExecutor 尚未移植。
- **AA4-006 CRLF repair follow-ups**：新 heuristics 落地后必须由 QA rerun `CRLFFuzzTests`；保留 `B3-PieceTree-Deterministic-CRLF-QA.md` 作为 rollback baseline。
- **DocUI find/replace scope & Markdown overlays (AA4 CL8)**：CL8 全部 4 个 Phase 完成！Phase 1-2（DecorationOwnerIds 语义修正 + DecorationSearchOptions 过滤支持）、Phase 3（MarkdownRenderer 集成 FindDecorations）、Phase 4（枚举值对齐）均已实现。

## Archives
- Detailed run-by-run notes, rerun transcripts, and migrated worklogs now live in [agent-team/handoffs/](../handoffs/) and `docs/reports/migration-log.md`; cite those records (not this snapshot) for historical context or audit evidence.

## Activity Log
> 2025-11 活动历史已压缩归档到 `agent-team/archive/porter-cs-log-202511.md`。

| 日期 | 任务 | 结果 |
| --- | --- | --- |
| 2025-12-02 | Sprint 05 技术评估 | 为 Team Leader 提供 Diff/DocUI/Services 优先级建议 |
| 2025-12-02 | Snippet-P2-Variables | SnippetVariableResolver.cs (225 行)，24 新测试 |
| 2025-12-02 | Snippet-P1.5 | Placeholder Grouping，stickiness 修复，8 新测试 |
| 2025-12-02 | WS3-PORT-TextModel | IntervalTree AcceptReplace 集成 |
| 2025-12-02 | Snippet-P1 | Final Tabstop + adjustWhitespace，16 新测试 |
| 2025-12-01 | Team Talk | 角色定位、协作流程确认 |

详细历史见 `agent-team/handoffs/`。
