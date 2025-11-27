# TypeScript → C# 对齐审查汇总报告

**生成日期:** 2025-11-27  
**审查范围:** 88个文件 (排除N/A原创C#实现后实际审查约70个文件对)  
**审查方法:** SubAgent并行对比分析

---

## 总体评估

| 模块 | 完全对齐 | 存在偏差 | 需要修正 | 审查文件数 |
|------|----------|----------|----------|------------|
| 01-Core Fundamentals | 8 | 0 | 2 | 10 |
| 02-Core Support | 1 | 5 | 2 | 8 |
| 03-Cursor | 0 | 2 | 7 | 9 |
| 04-Decorations | 3 | 3 | 1 | 7 |
| 05-Diff | 9 | 5 | 2 | 16 |
| 06-Services | 4 | 4 | 2 | 10 |
| 07-Core Tests | 9 | 6 | 2 | 17 |
| 08-Feature Tests | 0 | 9 | 4 | 13 |
| **合计** | **34** | **34** | **22** | **90** |

*基线数据引用 [`docs/reports/migration-log.md#sprint04-r1-r11`](../migration-log.md#sprint04-r1-r11) 与 [`agent-team/indexes/README.md#delta-2025-11-26-alignment-audit`](../../../agent-team/indexes/README.md#delta-2025-11-26-alignment-audit)。Phase 8 rerun：`export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`（585/585 通过，1 skipped，参见 [`#delta-2025-11-26-ws5-qa`](../../../agent-team/indexes/README.md#delta-2025-11-26-ws5-qa) 行）。*

### 对齐质量评分

- **优秀 (完全对齐):** 38% (34/90)
- **可接受 (存在偏差):** 38% (34/90)  
- **需要修正:** 24% (22/90)

---

## 高优先级修正项 (P0)

### 1. PieceTreeModel.Search / Edit（WS1-PORT-SearchCore / WS1-PORT-CRLF 后续）
**文件:** `src/TextBuffer/TextBuffer/PieceTreeModel.Edit.cs`, `src/TextBuffer/TextBuffer/PieceTreeModel.Search.cs`
- [`WS1-PORT-SearchCore` 与 `WS1-PORT-CRLF`](../migration-log.md#sprint04-r1-r11)（亦记于 [`agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`](../../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11)）已经恢复 `GetAccumulatedValue` O(1) 快路径与 CRLF bridge；`export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter CRLFFuzzTests --nologo` (16/16) 现作为回归证据。
- 未完成：`NodeAt2` tuple reuse 仍被禁用（bridge telemetry 尚未写入 SearchCache），`GetSearchCacheDiagnostics()` 也没有消费侧，导致长文档搜索/粘贴仍会触发二次树遍历。
**措施:** 按 `agent-team/handoffs/PORT-PT-Search-Plan.md` 恢复 `NodeAt2` tuple reuse 与 SearchCache instrumentation，并结合 WS5 harness (`PieceTreeSearchRegressionTests`) 在 `PIECETREE_DEBUG=0` 下重新验证命中率。

### 2. Cursor 栈 Stage 1（AA4 CL7 占位未交付）
**文件:** `src/TextBuffer/Cursor/*.cs`, `src/TextBuffer/TextModel.cs`
- `WS4-PORT-Core` 已交付 `CursorConfiguration`、`SingleCursorState`、TrackedRange 等 Stage 0 能力，并通过 `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter CursorCoreTests --nologo` (25/25) 证明骨架可用。
- `CursorColumns`、`WordOperations`、`SnippetController/SnippetSession` 仍缺 Stage 1 行为；CL7 子占位（[`#delta-2025-11-26-aa4-cl7-cursor-core`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-26-aa4-cl7-wordops`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-wordops)、[`#delta-2025-11-26-aa4-cl7-column-nav`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-column-nav)、[`#delta-2025-11-26-aa4-cl7-snippet`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-snippet)、[`#delta-2025-11-26-aa4-cl7-commands-tests`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-commands-tests)) 仍在 changefeed 中标记为 Gap。
**措施:** 逐一实现 CL7 占位并将结果挂入 WS5 harness（`CursorWordOperationsTests`, `CursorAtomicMoveOperationsTests`, `SnippetControllerTests`），以便在 `tests/TextBuffer.Tests/TestMatrix.md` 中解除 Gap 标签。

### 3. DocUI / Markdown renderer（AA4 CL8 占位）
**文件:** `src/TextBuffer/Rendering/MarkdownRenderer.cs`, `src/TextBuffer/DocUI/*`
- `WS3-PORT-Tree` 提供了 IntervalTree NodeFlags/惰性 delta，但 DocUI renderer 仍未接入 markdown/capture/intl/wordcache 扩展；CL8 子占位（[`#delta-2025-11-26-aa4-cl8-markdown`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)、[`#delta-2025-11-26-aa4-cl8-capture`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-capture)、[`#delta-2025-11-26-aa4-cl8-intl`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-intl)、[`#delta-2025-11-26-aa4-cl8-wordcache`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-wordcache)) 依旧是占位。
- DocUI diff/renderer 路径因此无法消费新 metadata，也无法验证 CL8 行为在 markdown/intl 组合下的正确性。
**措施:** 待 Info-Indexer 发布 CL8 drops 后，补全 renderer wiring + DocUI harness（FindDecorations/DocUIFindController/Markdown snapshots）测试，再回收本项 P0。

### 4. WS5 高风险测试矩阵（Top-10 仍未闭环）
**文件 / 套件:** `tests/TextBuffer.Tests/CursorWordOperationsTests.cs`, `CursorMultiSelectionTests.cs`, `SnippetControllerTests.cs`, `DiffTests.cs`
- [`WS5-INV`](../../../agent-team/indexes/README.md#delta-2025-11-26-ws5-test-backlog) 已列出 47 项高风险测试缺口；[`WS5-QA`](../../../agent-team/indexes/README.md#delta-2025-11-26-ws5-qa) 仅交付第一批 harness（PieceTreeBufferApiTests 17/17、PieceTreeSearchRegressionTests 9/9、TextModelIndentationTests 19/19+1 skipped）。
- Cursor/Snippet/Diff 专项 deterministic suites 仍缺，Task Board Gap 无法关闭。
**措施:** 依序执行 WS5 Top-10（cursorAtomicMoveOperations、word operations、snippetSession、diff renderer 等），并把 rerun 结果写回 `tests/TextBuffer.Tests/TestMatrix.md` 与 `agent-team/handoffs/WS5-QA-Result.md`，确保 585/585 基线包含这些新套件。

---

## 中优先级修正项 (P1)

1. **RangeMapping / Selection API 集成** – `WS2-PORT` 已补齐 `Range.Extensions`/`Selection`/`TextPosition` helper（75 tests），但 `RangeMapping.FromEdit/ToTextEdit` 与 `TextModelSearch` 仍在用旧实现。需将新 helper 全量接入 `TextModel`/`Diff`/`DocUI` 层并补写 regression（参考 [`../migration-log.md#sprint04-r1-r11`](../migration-log.md#sprint04-r1-r11)）。
2. **DecorationOwnerIds & ModelDecoration 常量** – `DecorationOwnerIds.Default`/`Any` 语义倒置，`ModelDecoration.LineHeightCeiling` 仍为 1500（TS=300），minimap/glyph/injectedText 枚举值也未对齐；需结合 `WS3-PORT-Tree` 结果校正这些常量，为后续 CL8 drops 做准备。
3. **Diff 支撑函数 / DocUI renderer** – `RangeMapping.Inverse/Clip/FromEdit` 仍缺，DocUI renderer 未消费 `DiffResult` 的 moves/unchangedRegions；需按 `WS5-INV` backlog 对齐 diff plumbing，并为 DocUI diff renderer 预留 API。
4. **Undo/Redo 与 LanguageConfiguration 服务** – 现有服务层还缺 `UndoRedoGroup`/资源组与 `LanguageConfiguration` 缓存；`TextModelIndentationTests` 在 `WS5-QA` 中仍有 1 skipped（GuessIndentation API），需补完服务面向 host 的实现和测试。
5. **DocUI Find 持久化设施** – `DocUIFindController` 仍依赖 `Null` clipboard/storage/context key stub；需实现真实 host 接口，确保查找设置可跨 session 持久，并将命令/焦点测试扩展至多 host 场景。
6. **Diff / Decorations 测试矩阵** – `DiffTests`、`DecorationTests` 还未移植 TS 参数矩阵（moves、unchanged regions、overview lane 组合）。结合 `WS5-INV` Top-10 的测试规范，在 `tests/TextBuffer.Tests/TestMatrix.md` 中补完 deterministic + perf 套件。

---

## 测试覆盖与质量风险

DocUI find scope/overview throttling 用例（27+49+9+4 个测试）已经到位，`WS5-QA` 亦新增 PieceTreeBufferApiTests (17/17)、PieceTreeSearchRegressionTests (9/9) 与 TextModelIndentationTests (19/19 + 1 skipped)，将基线推至 585/585（`../migration-log.md#sprint04-r1-r11`；`../../../agent-team/indexes/README.md#delta-2025-11-26-ws5-qa`）。但 cursor/snippet/diff 阶段性测试仍严重落后，且 TextModel 缩进仍有 skip。

| 套件 | TS 用例 | C# 用例 | 覆盖率 | 备注 |
|------|---------|---------|--------|------|
| CursorWordOperationsTests | ~60 | 3 | ~5% | 仅覆盖 Move/Select/`DeleteWordLeft`; 未涉及 wordPart、accessibility、locale、auto-close。
| Cursor/Column MultiSelection 套件 | ~70 | 5 | ~7% | 缺少 `InsertCursorAbove/Below`, `AddSelectionToNextFindMatch`, normalize/merge、列选 RTL 案例。
| SnippetController + Session | ~60 | 1 deterministic + 1 fuzz | ~3% | BF1 循环 fuzz 已验证，但嵌套、变量、transform、undo/redo 仍无测试。
| DiffTests | 40+ | 4 | ~10% | 还原/unchanged region/`computeMoves` 组合、超大文档性能均未覆盖，DocUI 也尚未消费 diff 输出。
| TextModelIndentationTests | ~20 | 19 通过 + 1 skipped | ~95% (锁定 skip) | `GuessIndentation` API 仍缺（`WS5-QA` 记录的 skip），需要实现 host 行为后才能解除。

后续需要按模块 07/08 的建议新增 PieceTree buffer API 用例（`equal`, `getLineCharCode`, `getNearestChunk`）、TextModel `guessIndentation` 矩阵、Find context-key 行为等，确保新增实现都有可验证的 parity harness。

---

## 已确认的设计决策 (可接受的偏差)

以下偏差是有意为之的架构简化，适应C#运行时环境：

1. **PieceTreeBuffer** 简化了TS版的复杂继承层次
2. **SearchTypes.cs** 添加了额外属性以适应C# API需求
3. **ILanguageConfigurationService/IUndoRedoService** 是原创C#接口设计
4. **DiffComputer** 添加了 `computeMoves` 选项
5. **WordCharacterClassifier** 添加了缓存机制

---

## 详细报告索引

1. [01-core-fundamentals.md](./01-core-fundamentals.md) - Core模块审查
2. [02-core-support.md](./02-core-support.md) - Core Support类型审查
3. [03-cursor.md](./03-cursor.md) - Cursor模块审查
4. [04-decorations.md](./04-decorations.md) - Decorations模块审查
5. [05-diff.md](./05-diff.md) - Diff算法审查
6. [06-services.md](./06-services.md) - Services模块审查
7. [07-core-tests.md](./07-core-tests.md) - 核心测试审查
8. [08-feature-tests.md](./08-feature-tests.md) - 功能测试审查

---

## 下一步行动建议

### 立即行动 (本 Sprint)
1. [ ] 按 `PORT-PT-Search-Plan.md` 恢复 `NodeAt2` tuple reuse + SearchCache instrumentation，并以 `CRLFFuzzTests` + `PieceTreeSearchRegressionTests` rerun 佐证 `WS1-PORT` 修复。
2. [ ] 完成 CL7 Stage 1（column select、word ops、snippet lifecycle、commands tests），解除 [`#delta-2025-11-26-aa4-cl7-*`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) 占位并更新 `CursorWordOperationsTests`/`CursorCoreTests`。
3. [ ] 将 CL8 markdown/capture/intl/wordcache drops 接入 renderer + DocUI harness，关闭 [`#delta-2025-11-26-aa4-cl8-*`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown) Gap。
4. [ ] 执行 `WS5-INV` Top-10 测试项（cursorAtomicMoveOperations、snippetSession、diff renderer 等），把 rerun 结果并入 `tests/TextBuffer.Tests/TestMatrix.md` 与 `WS5-QA` 基线。

### 短期 (1-2 Sprints)
1. [ ] 把 `WS2-PORT` 补齐的 Range/Selection/TextPosition helper 接入 `RangeMapping`, `TextModelSearch`, `DiffComputer` 与 `DocUIFind`，统一位置比较语义。
2. [ ] 完成 `RangeMapping.Inverse/Clip/FromEdit`、`DiffResult`→DocUI renderer wiring，并在 `DiffTests` 中覆盖 moves/unchanged region/大文档组合。
3. [ ] 扩展 `IUndoRedoService` / `ILanguageConfigurationService` 以支持 `UndoRedoGroup`、多资源栈与 `guessIndentation` host 配置，清除 TextModelIndentationTests skip。
4. [ ] 实装 DocUI clipboard/storage/context key host，补充 `DocUIFindControllerTests` 焦点/持久化覆盖。

### 长期
1. [ ] 完成 Cursor/Snippet/WordOperation 端到端 parity（含 deterministic + fuzz 测试），巩固多光标体验。
2. [ ] 将 diff/move 渲染、revert 按钮、unchanged region 折叠集成到 DocUI/markdown 渲染。
3. [ ] 将核心/feature 测试覆盖率提升至 ≥60%，包括 PieceTree buffer API、TextModel `guessIndentation`、bracket matching 等目前缺失的 TS 套件。

---

## Verification Notes
- **2025-11-27 – Sprint 04 Phase 8 spot-check:** 依照 [`docs/reports/migration-log.md#sprint04-r1-r11`](../migration-log.md#sprint04-r1-r11) rerun `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter CRLFFuzzTests --nologo` (16/16) 与 `--filter CursorCoreTests --nologo` (25/25)，确认 WS1-PORT-CRLF + WS4-PORT-Core 落地；随后执行全量 585/585（1 skipped）验证 `WS5-QA` 基线无回归。
- **2025-11-26 – 01 Core Fundamentals:** 重新对照 `PieceTreeModel.Edit.cs`, `PieceTreeModel.Search.cs`, `PieceTreeBuilder.cs`，并回放 `PieceTreeFuzzHarnessTests`、`PieceTreeDeterministicTests`，确认 change-buffer/`nodeAt2` 偏差仍存在。
- **2025-11-26 – 02 Core Support:** 复核 `Range.Extensions.cs`, `Selection.cs`, `PieceTreeSearchCache.cs`, `SearchTypes.cs`，结合 `TextModelSearchTests` 记录 Range/Selection helper 缺口与搜索缓存差异。
- **2025-11-26 – 03 Cursor:** 逐行比对 `Cursor.cs`, `CursorCollection.cs`, `CursorColumns.cs`, `CursorState.cs`, `SnippetController.cs`, `SnippetSession.cs`，并查看 `CursorTests`, `CursorWordOperationsTests`, `SnippetControllerTests`, `SnippetMultiCursorFuzzTests` 现有覆盖。
- **2025-11-26 – 04 Decorations:** 检查 `IntervalTree.cs`, `DecorationOwnerIds.cs`, `ModelDecoration.cs`, `DecorationsTrees.cs`，以及 `DecorationTests`, `DecorationStickinessTests`, `DocUIFindDecorationsTests` 的结果，确认 delta 与 owner 语义问题。
- **2025-11-26 – 05 Diff:** 审阅 `DiffComputer.cs`, `ComputeMovedLines.cs`, `LineSequence.cs`, `RangeMapping.cs` 与 `DiffTests`, 确认 boundary/RangeMapping 缺口和 DocUI 未接 diffs 的状态。
- **2025-11-26 – 06 Services:** 复核 `TextModel.cs`, `TextPosition.cs`, `ILanguageConfigurationService.cs`, `IUndoRedoService.cs`, `DocUIFindController.cs`，并参考 `DocUIFindControllerTests`, `TextModelTests` 验证服务层差异。
- **2025-11-26 – 07 Core Tests:** 重新运行/审阅 `PieceTreeDeterministicTests`, `PieceTreeFuzzHarnessTests`, `PieceTreeSearchOffsetCacheTests`, `TextModelSnapshotTests`，确认新增 parity harness 已落地但 buffer/API/indentation 用例仍缺。
- **2025-11-26 – 08 Feature Tests:** 审查 `DocUIFind*` test 套件、`CursorMultiSelectionTests`, `ColumnSelectionTests`, `CursorWordOperationsTests`, `DiffTests`, `SnippetMultiCursorFuzzTests`，记录 DocUI scope fix 已生效但 cursor/snippet/diff 仍无完整端到端覆盖。

*报告由 AI Team 自动生成*
