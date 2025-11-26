# TypeScript → C# 对齐审查汇总报告

**生成日期:** 2025-11-26  
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

### 对齐质量评分

- **优秀 (完全对齐):** 38% (34/90)
- **可接受 (存在偏差):** 38% (34/90)  
- **需要修正:** 24% (22/90)

---

## 高优先级修正项 (P0)

### 1. PieceTreeModel.Search / Edit 偏差（新确认）
**文件:** `src/TextBuffer/TextBuffer/PieceTreeModel.Edit.cs`, `src/TextBuffer/TextBuffer/PieceTreeModel.Search.cs`
- Change-buffer 追加路径缺失 TS 的 `hitCRLF` 与 `_buffers[0]` 占位逻辑，跨 append 时 `\r\n` 被拆散，`_lastChangeBufferPos` 失真。
- `GetAccumulatedValue` 退化为 O(n) 字符遍历且忽略 `lineStarts`，`NodeAt2` 通过两次树查找定位，导致搜索、查找和 large paste 的偏移结果与 TS 不一致。
**影响:** 行列定位和 diff/search 相关 API 可能返回错误 offset，现有 fuzz 也无法覆盖这类系统性差异。
**措施:** 补齐 TS 的 `hitCRLF` 占位、`lineStarts` 计算与树遍历，新增 harness 覆盖跨 append CRLF 与 `nodeAt2` cache 回归。

### 2. Cursor 栈架构缺失（持续 P0）
**文件:** `src/TextBuffer/Cursor/*.cs`
- 没有 `SingleCursorState`、`CursorConfiguration`、view/model 双态或 tracked range；`CursorCollection` 仅提供增删，缺少 normalize/批量 state API。
- `CursorColumns`, `WordOperations`, `SnippetController/SnippetSession` 只保留最小骨架；虽已合入 snippet BF1 循环修复，但绝大多数 VS Code 命令（column select、wordPart、choice/变量）仍无法复用。
**影响:** 多光标、列选择、粘列、snippet 导航等高级编辑功能都无法达到 parity，相关测试也难以补齐。
**措施:** 需要按 TS 架构重新建模（`CursorContext` + `SingleCursorState` + `CursorConfiguration`），并补全列选择/词操作/snippet 生命周期后再扩展测试矩阵。

### 3. Range / Selection helpers 缺失（持续 P0）
**文件:** `src/TextBuffer/Core/Range.Extensions.cs`, `src/TextBuffer/Cursor/Selection.cs`
- 缺少 `ContainsPosition/Range`, `IntersectRanges`, `PlusRange`, `Selection.fromRange` 等基础方法，现有实现连同行比较都与 TS 不同。
- 相关帮助类在 VS Code 中被 Selection、Decoration、Diff、Find 等大量复用；没有这些 helper，所有上层功能都需要自造逻辑且易出错。
**措施:** 完整移植 `range.ts` 与 `selection.ts` 的 API，统一 `TextPosition` 比较方式，并为同行边界添加单元测试。

### 4. Decorations IntervalTree 惰性更新缺失（持续 P0）
**文件:** `src/TextBuffer/Decorations/IntervalTree.cs`
- 没有 `delta`/`acceptReplace` 机制，编辑时只能遍历并回写所有装饰；同样缺少 metadata 位掩码以支持 `filterOutValidation` 等开关。
- 大文档 + 多装饰场景下性能退化为 O(n)，DocUI/语法高亮类场景会卡顿，且无法实现 VS Code 的过滤参数。
**措施:** 补齐 TS 的 `delta`、`_normalizeDeltaIfNecessary` 与 metadata 设计，让 `TextModel.AdjustDecorationsForEdit` 只需调用树级批量更新。

---

## 中优先级修正项 (P1)

1. **TextPosition / Position helpers** – `TextPosition.cs` 仍缺 `With`, `Delta`, `IsBefore*` 等方法，导致 Selection、Range 与 Find 无法共享逻辑，需一次性补齐并同步 `Range.Extensions` 的比较语义。
2. **DecorationOwnerIds & ModelDecoration 约束** – `DecorationOwnerIds.Default` 与 `Any` 语义不符，`ModelDecoration.LineHeightCeiling` 仍为 1500（TS 为 300），以及 minimap/glyph/injectedText 枚举值偏差，需统一以免 future API 提交换乱。
3. **Diff 支撑函数缺口** – `LineSequence.GetBoundaryScore` 文末索引错误，`RangeMapping` 缺少 `Inverse/Clip/FromEdit/ToTextEdit`，DocUI 也尚未消费 `DiffResult`，需要同步实现以便后续 diff renderer。
4. **Undo/Redo 与 Language Configuration 服务** – `IUndoRedoService` 只支持单模型栈，缺失 `UndoRedoGroup`/资源概念；`ILanguageConfigurationService` 也仅能订阅事件，无法注册/解析配置，需引入缓存与 API 以支撑括号/缩进逻辑。
5. **DocUI Find 基础设施** – 虽已完成范围/剪贴板/DocUI scope 校验，但默认 `Null` clipboard/storage 意味着真实宿主仍不会持久化设置；需提供实际实现并扩展 context key/focus 测试。
6. **Diff/Decorations 测试矩阵** – `DiffTests` 与 `DecorationTests` 仅覆盖主干路径，尚未移植 TS 参数矩阵（moves, unchanged regions, overview lane 组合）；需要在现有测试工程中补完。

---

## 测试覆盖与质量风险

DocUI find scope/overview throttling 用例（27+49+9+4 个测试）已经到位，但核心编辑与 snippet/cursor 阶段性测试仍严重落后。

| 套件 | TS 用例 | C# 用例 | 覆盖率 | 备注 |
|------|---------|---------|--------|------|
| CursorWordOperationsTests | ~60 | 3 | ~5% | 仅覆盖 Move/Select/`DeleteWordLeft`; 未涉及 wordPart、accessibility、locale、auto-close。
| Cursor/Column MultiSelection 套件 | ~70 | 5 | ~7% | 缺少 `InsertCursorAbove/Below`, `AddSelectionToNextFindMatch`, normalize/merge、列选 RTL 案例。
| SnippetController + Session | ~60 | 1 deterministic + 1 fuzz | ~3% | BF1 循环 fuzz 已验证，但嵌套、变量、transform、undo/redo 仍无测试。
| DiffTests | 40+ | 4 | ~10% | 还原/unchanged region/`computeMoves` 组合、超大文档性能均未覆盖，DocUI 也尚未消费 diff 输出。
| Range/Selection helpers | 30+ | 0 | 0% | 缺少任何单元测试，`Range.Extensions`/`Selection` API 暂未实现同名功能。

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
1. [ ] 修复 `PieceTreeModel.Edit/Search` 的 change-buffer CRLF 与 `NodeAt2` 逻辑，更新 offset/line cache 测试。
2. [ ] 补齐 `Range.Extensions` + `Selection` + `TextPosition` helper API，并添加同行/边界单元测试。
3. [ ] 为 `IntervalTree` 实现 `delta/acceptReplace` 与 metadata 位掩码，恢复大文档编辑性能与过滤能力。
4. [ ] 对齐 `DecorationOwnerIds` 语义（0 == Any）并下调 `ModelDecoration.LineHeightCeiling` 到 300。

### 短期 (1-2 Sprints)
1. [ ] 建立 Cursor 双态/配置（`CursorConfiguration`, `SingleCursorState`, tracked ranges），为列选/多光标/word 操作补齐 API。
2. [ ] 修复 `LineSequence.GetBoundaryScore`、`RangeMapping` 缺失方法，并把 `DiffResult` 接入 DocUI/renderer 流程。
3. [ ] 扩展 `IUndoRedoService`（资源组、UndoRedoGroup、snapshot）与 `ILanguageConfigurationService`（注册/解析/缓存）。
4. [ ] 提供 DocUI 默认 clipboard/storage/context-key 实现，并补 Find controller 焦点/动画测试。

### 长期
1. [ ] 完成 Cursor/Snippet/WordOperation 端到端 parity（含 deterministic + fuzz 测试），巩固多光标体验。
2. [ ] 将 diff/move 渲染、revert 按钮、unchanged region 折叠集成到 DocUI/markdown 渲染。
3. [ ] 将核心/feature 测试覆盖率提升至 ≥60%，包括 PieceTree buffer API、TextModel `guessIndentation`、bracket matching 等目前缺失的 TS 套件。

---

## Verification Notes
- **2025-11-26 – 01 Core Fundamentals:** 重新对照 `PieceTreeModel.Edit.cs`, `PieceTreeModel.Search.cs`, `PieceTreeBuilder.cs`，并回放 `PieceTreeFuzzHarnessTests`、`PieceTreeDeterministicTests`，确认 change-buffer/`nodeAt2` 偏差仍存在。
- **2025-11-26 – 02 Core Support:** 复核 `Range.Extensions.cs`, `Selection.cs`, `PieceTreeSearchCache.cs`, `SearchTypes.cs`，结合 `TextModelSearchTests` 记录 Range/Selection helper 缺口与搜索缓存差异。
- **2025-11-26 – 03 Cursor:** 逐行比对 `Cursor.cs`, `CursorCollection.cs`, `CursorColumns.cs`, `CursorState.cs`, `SnippetController.cs`, `SnippetSession.cs`，并查看 `CursorTests`, `CursorWordOperationsTests`, `SnippetControllerTests`, `SnippetMultiCursorFuzzTests` 现有覆盖。
- **2025-11-26 – 04 Decorations:** 检查 `IntervalTree.cs`, `DecorationOwnerIds.cs`, `ModelDecoration.cs`, `DecorationsTrees.cs`，以及 `DecorationTests`, `DecorationStickinessTests`, `DocUIFindDecorationsTests` 的结果，确认 delta 与 owner 语义问题。
- **2025-11-26 – 05 Diff:** 审阅 `DiffComputer.cs`, `ComputeMovedLines.cs`, `LineSequence.cs`, `RangeMapping.cs` 与 `DiffTests`, 确认 boundary/RangeMapping 缺口和 DocUI 未接 diffs 的状态。
- **2025-11-26 – 06 Services:** 复核 `TextModel.cs`, `TextPosition.cs`, `ILanguageConfigurationService.cs`, `IUndoRedoService.cs`, `DocUIFindController.cs`，并参考 `DocUIFindControllerTests`, `TextModelTests` 验证服务层差异。
- **2025-11-26 – 07 Core Tests:** 重新运行/审阅 `PieceTreeDeterministicTests`, `PieceTreeFuzzHarnessTests`, `PieceTreeSearchOffsetCacheTests`, `TextModelSnapshotTests`，确认新增 parity harness 已落地但 buffer/API/indentation 用例仍缺。
- **2025-11-26 – 08 Feature Tests:** 审查 `DocUIFind*` test 套件、`CursorMultiSelectionTests`, `ColumnSelectionTests`, `CursorWordOperationsTests`, `DiffTests`, `SnippetMultiCursorFuzzTests`，记录 DocUI scope fix 已生效但 cursor/snippet/diff 仍无完整端到端覆盖。

*报告由 AI Team 自动生成*
