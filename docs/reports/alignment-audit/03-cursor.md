# Cursor 模块对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 9个光标与词法/Snippet相关文件（`src/TextBuffer/Cursor/**`）及其 TypeScript 对应实现（`ts/src/vs/editor/common/cursor/**`, `ts/src/vs/editor/contrib/snippet/browser/**`）

## 概要
- 完全对齐: 0/9
- ⚠️存在偏差: 2/9（`WordCharacterClassifier.cs`, `WordOperations.cs` 仅覆盖基本词边界）
- ❌需要修正: 7/9（`Cursor.cs`, `CursorCollection.cs`, `CursorColumns.cs`, `CursorContext.cs`, `CursorState.cs`, `SnippetController.cs`, `SnippetSession.cs`）
- 🚫尚未移植: `CursorConfiguration`（TS: `cursorCommon.ts`，C# 无同名文件）
- 关键差异集中在：缺失 model/view 双态与 `SingleCursorState`/`CursorConfiguration`、`CursorCollection` 与 `CursorContext` 没有视图/归一化管线、列选择/词导航/Snippet 仅保留极简骨架。唯一已解决的问题是 `SnippetSession` 的 BF1 多光标循环补丁，其余功能仍与 VS Code 有显著鸿沟。

## 详细分析

---

### 1. Cursor.cs
**TS源:** `ts/src/vs/editor/common/cursor/oneCursor.ts`
**C#文件:** `src/TextBuffer/Cursor/Cursor.cs`
**对齐状态:** ❌需要修正

**差异要点:**
- TS `Cursor` 只负责状态同步并依赖 `_setState` 与 `CursorMoveOperations`，而 C# 把 `MoveLeft/Right/Up/Down`, `MoveWord*`, `DeleteWordLeft` 等逻辑全部塞进 `Cursor`，与 VS Code 的职责划分完全不同。
- TS 维护 `modelState` 与 `viewState`（`SingleCursorState`），通过 `_selTrackedRange` 和 `CursorContext` 的 `coordinatesConverter` 在编辑后恢复选择；C# 只有 `_selection` 和 `_stickyColumn`，既无 view state 也无 tracked range，编辑后无法校正漂移。
- 粘列信息在 TS 中写入 `leftoverVisibleColumns` 并跟随 `CursorState` 序列化；C# 的 `_stickyColumn` 为局部字段，`CursorState` record 也没有该属性，多光标或撤销重建后就丢失。
- `StartColumnSelection` 仅调用 `CursorColumns.GetVisibleColumnFromPosition` 等 helper，未通过 `CursorConfiguration.columnFromVisibleColumn` 校正行最小列和 RTL，可视/模型不一致。
- `Cursor` 直接引用 `TextModel` 并在 `UpdateDecorations()` 中调用 `DeltaDecorations`，跳过了 `CursorContext` 提供的 viewModel/coordinatesConverter，导致视图与模型不可分层。

**建议:**
1. 恢复 TS 架构，让 `Cursor` 只承载状态，移动逻辑交给 `CursorMoveOperations`/`CursorWordOperations`。
2. 引入 `SingleCursorState`/`CursorState` 双态机，并通过 `CursorContext` 验证模型/视图坐标。
3. 移植 `_selTrackedRange` 与 `TrackedRangeStickiness`，确保编辑后选择可追踪。
4. 将粘列(`leftoverVisibleColumns`) 与选择起点写入状态对象，为 `CursorCollection`/snippet/undo 公用。
5. 让列选择使用 `CursorConfiguration` 的转换方法，避免注入文本/RTL 情况下偏移。

---

### 2. CursorCollection.cs
**TS源:** `ts/src/vs/editor/common/cursor/cursorCollection.ts`
**C#文件:** `src/TextBuffer/Cursor/CursorCollection.cs`
**对齐状态:** ❌需要修正

**差异要点:**
- TS 维持主/次光标、`lastAddedCursorIndex`、`normalize()`、`getTopMostViewPosition()` 等，而 C# 版本只有 `CreateCursor`, `RemoveCursor`, `GetCursorPositions`，缺少所有状态批量管理 API。
- 没有 `setStates()`/`_setSecondaryStates()`，无法套用命令计算出的 `PartialCursorState`；`killSecondaryCursors()`、`getAll()`、`readSelectionFromMarkers()` 等全部缺席。
- 缺乏 `normalize()` 导致 `multiCursorMergeOverlapping` 选项无处落地，重合/接触的选择不会合并。
- 未实现 `startTrackingSelections`/`stopTrackingSelections`，与 `CursorContext` 完全脱钩，tracked range 和视图坐标管线断裂。
- 无视图 API（`getViewPositions`, `getBottomMostViewPosition` 等），上层命令无法基于视图顺序排序或滚动。

**建议:**
1. 让集合持有 `CursorContext`，实现 `getAll/setStates/_setSecondaryStates/killSecondaryCursors`。
2. 抄写 `normalize()` 与 `lastAddedCursorIndex` 策略，保证 Ctrl+点击/拖拽体验一致。
3. 提供视图位置/选择查询，使滚动和渲染逻辑可共享。
4. 在添加/删除光标时更新 tracked range，保持与 TS 兼容。

---

### 3. CursorColumns.cs
**TS源:** `ts/src/vs/editor/common/cursor/cursorColumnSelection.ts`
**C#文件:** `src/TextBuffer/Cursor/CursorColumns.cs`
**对齐状态:** ❌需要修正

**差异要点:**
- TS `ColumnSelection` 提供 `columnSelect/columnSelectLeft/Right/Up/Down` 并返回 `IColumnSelectResult`（多 `SingleCursorState` + 方向信息）；C# 仅有 `GetVisibleColumnFromPosition` 与 `GetPositionFromVisibleColumn`，核心列选择算法完全缺失。
- 不存在 `IColumnSelectResult`/`IColumnSelectData`，上层无法缓存列选择状态，也无法表达反转/可视列范围。
- TS 依赖 `CursorConfiguration`（tabSize/pageSize/stickyTabStops）以及 `ICursorSimpleModel` 的 `getLineMinColumn`/`getLineMaxColumn`；C# 缺少这些输入，列选择无法尊重可视行边界或 RTL。
- 注入文本处理只是简单地把 `Before/After` 内容长度加到可视列上，未调用 VS Code 的转换函数，会与视图渲染产生偏差。

**建议:**
1. 完整移植 `ColumnSelection` 类及 `IColumnSelectResult`，产出 `SingleCursorState`（或等价）数组。
2. 引入 `CursorConfiguration` 并使用其 `visibleColumnFromColumn/columnFromVisibleColumn` 实现页翻列选。
3. 使用 `ICoordinatesConverter`/`ICursorSimpleModel`，而非直接对 `TextModel` 逐字符遍历。

---

### 4. CursorContext.cs
**TS源:** `ts/src/vs/editor/common/cursor/cursorContext.ts`
**C#文件:** `src/TextBuffer/Cursor/CursorContext.cs`
**对齐状态:** ❌需要修正

**差异要点:**
- TS Context 暴露 `model`, `viewModel`, `coordinatesConverter`, `cursorConfig`，为 `Cursor`/`CursorCollection` 提供全部依赖；C# 只有 `TextModel` 与 `CursorCollection`，完全没有视图或配置。
- `ComputeAfterCursorState()` 在 TS 中依赖 inverse edits、`ICoordinatesConverter` 和 tracked range 恢复光标；C# 直接调用 `GetCursorPositions()` 返回当前 active 位置信息，对编辑后的位移毫无校正。
- 因缺少 `CursorConfiguration`，其它组件无法读取 `multiCursorMergeOverlapping`, `pageSize`, `wordSeparators`, `emptySelectionClipboard` 等编辑器选项。
- 没有 `ICursorSimpleModel` 导致列选择、视图归一化、`CursorMoveOperations` 等都无从实现。

**建议:**
1. 定义并注入 `ICoordinatesConverter` 与 `ICursorSimpleModel`，承接 view/model 坐标转换。
2. 移植 `CursorConfiguration` 并挂到 context 上。
3. 扩展 `ComputeAfterCursorState`，利用 inverse changes 和 tracked range 重新计算所有光标。

---

### 5. CursorState.cs
**TS源:** `ts/src/vs/editor/common/cursorCommon.ts`
**C#文件:** `src/TextBuffer/Cursor/CursorState.cs`
**对齐状态:** ❌需要修正

**差异要点:**
- TS 定义 `CursorState`, `SingleCursorState`, `PartialModelCursorState`, `PartialViewCursorState`, `SelectionStartKind`，而 C# 仅有包含 `OwnerId/Selection/StickyColumn/DecorationIds` 的 record，无法描述 model/view 双态。
- 缺少 `selectionStart`, `selectionStartKind`, `leftoverVisibleColumns`，因此行/词选择与粘列信息无法序列化或回放。
- 没有 `Partial*` 类型，也没有 `CursorState.fromModelSelections()` 等工厂，`CursorCollection` 与命令栈无法共享状态。
- 现有 record 仅为装饰使用，与 TS `CursorState` 在 undo/redo、snippet、命令之间传递的语义完全不同。

**建议:**
1. 引入 `SingleCursorState` 与 `SelectionStartKind`，并让 `CursorState` 同时持有 model/view state。
2. 实现 `PartialModelCursorState`/`PartialViewCursorState` 及对应工厂。
3. 将 `Cursor` 的 `_selection`、`_stickyColumn` 等字段迁移到状态类，确保可在 `CursorCollection`/Snippet/Undo 之间传递。

---

### 6. SnippetController.cs
**TS源:** `ts/src/vs/editor/contrib/snippet/browser/snippetController2.ts`
**C#文件:** `src/TextBuffer/Cursor/SnippetController.cs`
**对齐状态:** ❌需要修正

**差异要点:**
- TS 以 `IEditorContribution` 形式集成，控制上下文键（`InSnippetMode`, `HasNextTabstop`, `HasPrevTabstop`），C# 只有 `CreateSession/InsertSnippetAt/Next/Prev`，没有 `Finish/Cancel/IsInSnippetMode`。
- 插入 API 缺少 `overwriteBefore/After`, `undoStopBefore/After`, `adjustWhitespace`, `clipboardText`, `merge` 等选项，无法与 VS Code 的编辑栈协作。
- 没有 choice/completion 集成，也未通知 `CompletionProvider`。
- 不参与 undo stop，也没把 snippet 状态分发给 `Cursor` 或输入法，导致 tabstop 导航难以复用现有命令。

**建议:**
1. 将控制器注册为编辑器服务，暴露完整生命周期方法及上下文键。
2. 支持 VS Code 的 `InsertSnippetOptions`，处理 whitespace/overwrite/undo。
3. 引入 choice/completion hook，并与 `SnippetSession` 状态同步。

---

### 7. SnippetSession.cs
**TS源:** `ts/src/vs/editor/contrib/snippet/browser/snippetSession.ts`
**C#文件:** `src/TextBuffer/Cursor/SnippetSession.cs`
**对齐状态:** ❌需要修正

**差异要点:**
- TS 拆分 `OneSnippet` 与 `SnippetSession`，包含 placeholder 分组、transform、变量解析、choice、嵌套 merge、`computePossibleSelections`；C# 只有 `SnippetSession` 一个类，靠正则 `\$\{(\d+):([^}]+)\}` 解析 `${n:text}`，其余语法全部缺失。
- 没有变量解析器（模型、剪贴板、时间、文件、注释、随机等）和 `adjustWhitespace`/`overwriteBefore/After` 逻辑。
- Placeholder 装饰只有统一的 `snippet-placeholder`，没有 active/inactive/final 样式；也没有 placeholder group 或 transformation。
- 不支持 merge/stack，连续插入 snippet 会相互覆盖。
- 多光标循环 bug (BF1) 已修复：`NextPlaceholder()` 现在在越界时把 `_current` 设为 `_placeholders.Count`，`PrevPlaceholder()` 也能从该哨兵回跳，防止多光标无限循环；但除了该哨兵补丁外，功能仍停留在最小实现。

**建议:**
1. 引入 `OneSnippet`、placeholder group 和 active/inactive 装饰管理。
2. 实现 TextMate snippet 语法解析（变量、transform、choice）。
3. 在插入时执行 whitespace/overwrite 调整并暴露 `SnippetInsertOptions`。
4. 在保留 BF1 哨兵逻辑的基础上，实现完整的 `move(fwd)`/`merge` 路径。

---

### 8. WordCharacterClassifier.cs
**TS源:** `ts/src/vs/editor/common/core/wordCharacterClassifier.ts`
**C#文件:** `src/TextBuffer/Cursor/WordCharacterClassifier.cs`
**对齐状态:** ⚠️存在偏差

**差异要点:**
- TS 继承 `CharacterClassifier<WordCharacterClass>`，缓存行内容并支持 `Intl.Segmenter`；C# 只有 `IsWordChar`/`IsSeparator`，通过 `string.Contains` 判断，无缓存且不区分 Regular/Separator/Whitespace。
- 缺少 `WordCharacterClass` 枚举与 `getMapForWordSeparators()`，每次操作都重新解析分隔符。
- 未实现 `findPrevIntlWordBeforeOrAtOffset` 与 `findNextIntlWordAtOrAfterOffset`，Unicode/emoji 词边界无法匹配 VS Code。
- 行级缓存与 `wordSeparators` map 不存在，频繁调用将产生额外分配。

**建议:**
1. 复制 `CharacterClassifier` + `WordCharacterClass` 设计，并缓存最近访问的行和分段结果。
2. 借助 .NET `System.Globalization.StringInfo` 或 ICU 提供 `Intl.Segmenter` 等价能力。
3. 暴露国际化词查找 API，供 `WordOperations` 使用。

---

### 9. WordOperations.cs
**TS源:** `ts/src/vs/editor/common/cursor/cursorWordOperations.ts`
**C#文件:** `src/TextBuffer/Cursor/WordOperations.cs`
**对齐状态:** ⚠️存在偏差

**差异要点:**
- TS 版本约 800 行，涵盖移动/删除/选词/word-part/国际化/自动闭合对；C# 仅实现 `MoveWordLeft/Right`, `SelectWordLeft/Right`, `DeleteWordLeft`，`WordNavigationType` 虽含 `WordPart` 却没有对应实现。
- 缺失 `_findPreviousWordOnLine`, `_findNextWordOnLine`, `_findStartOfWord`, `_createWord`, `DeleteWordContext`、`WordType`、`word()`、`getWordAtPosition`、`deleteWordRight`, `deleteInsideWord`, `WordPartOperations` 等核心模块。
- 不支持 camelCase/snake_case 或 Unicode word-part 切分，也没有触发 auto-closing pair 的删/移 heuristics。
- 没有 `Intl` 分词或 `whitespaceHeuristics`，行为仅等价于“跳到下一串非分隔符字符”。

**建议:**
1. 移植 `_createWord` 系列与 `DeleteWordContext`，实现 `WordNavigationType.WordStart/WordEnd/Accessibility`。
2. 添加 `WordType`、`WordPartOperations` 以及 `deleteWordRight/deleteInsideWord` 等命令。
3. 集成国际化分段与 auto-closing 逻辑，确保与 `WordCharacterClassifier` 一致。

---

## 总结

### 严重程度分类
- **🔴 需要重大重构 (7个文件):** `Cursor.cs`, `CursorCollection.cs`, `CursorColumns.cs`, `CursorContext.cs`, `CursorState.cs`, `SnippetController.cs`, `SnippetSession.cs`
- **🟡 需要补充功能 (2个文件):** `WordCharacterClassifier.cs`, `WordOperations.cs`
- **🚫 缺失:** `CursorConfiguration`（尚未在 C# 中实现）

### 优先级建议
- **P0:** 移植 `CursorConfiguration` + `SingleCursorState`/`CursorState` 双态，并让 `CursorContext`/`CursorCollection` 使用该状态机；补齐 tracked range 与 normalize。
- **P1:** 补足列选择 (`CursorColumns.columnSelect*`)、词导航/删除主路径、`SnippetController` 基础生命周期。
- **P2:** 扩展 snippet（变量/choice/merge）、完善 `WordCharacterClassifier` 的 Intl 支持、实现选择追踪/视图 API。

### 移植质量评估
- 当前 Cursor 栈属于**重新实现**而非**逐行移植**：缺乏 model/view 状态机、上下文转换、列选择、变量解析等关键能力。
- 若不先补齐核心结构，将难以从 VS Code 同步 bugfix/feature（例如 sticky column、multi-cursor merge、snippet choice）。
- 建议先完成 `CursorConfiguration` + `SingleCursorState` + `CursorCollection.setStates/normalize`，再逐步对齐 column select、word operations 与 snippet 功能。

## Verification Notes
- 逐一阅读 `docs/reports/alignment-audit/03-cursor.md` 旧版、`src/TextBuffer/Cursor/*.cs` 以及 `ts/src/vs/editor/common/cursor/*.ts`、`ts/src/vs/editor/contrib/snippet/browser/*.ts`，确认功能覆盖差距。
- 特别验证了 `SnippetSession.NextPlaceholder/PrevPlaceholder` 的 BF1 哨兵逻辑、`Cursor.cs` 缺乏 `SingleCursorState`、`CursorCollection` 未实现 `normalize`、`CursorColumns` 只有转换 helper。
- 尚未发现任何 `CursorConfiguration` 或 `ICoordinatesConverter` 的 C# 实现，也没有 `CursorMoveOperations` 等配套文件——需要明确这些组件计划部署的位置，以及 `Cursor` 是否会继续直接操作 `TextModel`。
