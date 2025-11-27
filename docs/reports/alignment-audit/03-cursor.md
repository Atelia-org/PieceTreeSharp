# Cursor 模块对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 9个光标与词法/Snippet相关文件（`src/TextBuffer/Cursor/**`）及其 TypeScript 对应实现（`ts/src/vs/editor/common/cursor/**`, `ts/src/vs/editor/contrib/snippet/browser/**`）

## 概要
- **Stage 0 基础设施已落地:** `WS4-PORT-Core` 引入 `CursorConfiguration`、双态 `CursorState`、`CursorContext`、tracked range plumbing，以及 25/25 Stage 0 `CursorCoreTests`（命令 `dotnet test --filter CursorCoreTests --nologo` 现报 39 通过 / 0 失败 / 2 占位跳过且保持 25/25 case 全绿）；该交付记录于 [`docs/reports/migration-log.md#ws4-port-core`](../migration-log.md#ws4-port-core) 且纳入 Sprint 04 Phase 8 汇总 [`agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`](../../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11)。
- **Stage 0 仍待接线:** `Cursor.cs`/`CursorCollection.cs` 继续走旧的单态实现，`TextModelOptions.EnableVsCursorParity` 也保持关闭，因此 Stage 0 骨架尚未驱动运行态行为，需要把命令/集合接入新 `CursorContext` 并启用 tracked range 装饰。
- **Stage 1 backlog 按 CL7 占位追踪:** WordOps、ColumnSelection、Snippet、commands/tests 依旧对应 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-26-aa4-cl7-wordops`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-wordops)、[`#delta-2025-11-26-aa4-cl7-column-nav`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-column-nav)、[`#delta-2025-11-26-aa4-cl7-snippet`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-snippet)、[`#delta-2025-11-26-aa4-cl7-commands-tests`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-commands-tests)。
- **对齐度（以运行态为准）:** 完全对齐 0/9、⚠️存在偏差 2/9（`WordCharacterClassifier.cs`, `WordOperations.cs` 仍是最小实现）、❌需要修正 7/9（`Cursor.cs`, `CursorCollection.cs`, `CursorColumns.cs`, `CursorContext.cs`, `CursorState.cs`, `SnippetController.cs`, `SnippetSession.cs`）。尽管 Stage 0 文件已存在，但在接线前仍按“需要修正”对待。
- 关键差异依旧集中在：`Cursor`/`CursorCollection` 未采用双态 `SingleCursorState`，列选择/词操作/Snippet 缺乏 TS parity，且命令/测试覆盖远低于 VS Code。

## Stage 0 vs Stage 1 状态
- **Stage 0 已交付内容:** `CursorConfiguration.cs`、`CursorState.cs`、`CursorContext.cs` 以及 `TextModel` tracked range/隐藏装饰支持已更新；`CursorCoreTests` 命令 (`dotnet test --filter CursorCoreTests --nologo`) 目前 39 通过 / 0 失败 / 2 占位跳过，25/25 Stage 0 case 仍与 [`docs/reports/migration-log.md#ws4-port-core`](../migration-log.md#ws4-port-core) 记录一致。
- **Stage 0 待收尾:** `Cursor.cs`、`CursorCollection.cs` 尚未切换到 `SingleCursorState`/`CursorContext`，`TextModelOptions.EnableVsCursorParity` 默认仍为 false；需在 `#delta-2025-11-26-aa4-cl7-cursor-core` 覆盖中完成接线后再开放。
- **Stage 1 范围:** Column selection (`#delta-2025-11-26-aa4-cl7-column-nav`)、Word operations (`#delta-2025-11-26-aa4-cl7-wordops`)、Snippet controller/session (`#delta-2025-11-26-aa4-cl7-snippet`)、命令与测试矩阵 (`#delta-2025-11-26-aa4-cl7-commands-tests`) 继续作为 P0 gap 存在。

## 详细分析

---

### 0. CursorConfiguration.cs
**TS源:** `ts/src/vs/editor/common/controller/cursorCommon.ts`
**C#文件:** `src/TextBuffer/Cursor/CursorConfiguration.cs`
**对齐状态:** ⚠️存在偏差（类型已到位，但尚未接入命令）

**现状:** `WS4-PORT-Core` 按 TS 结构实现了 `CursorConfiguration`, `ICursorSimpleModel`, `CursorColumnsHelper`, 以及 `EditOperationType`/`PositionAffinity` 等枚举，但这些配置对象尚未被 `Cursor`, `CursorCollection`, `CursorColumns` 或 `CursorMoveOperations` 使用。`TextModelOptions.EnableVsCursorParity` 仍默认 false，也没有 host 将 `IdentityCoordinatesConverter` 以外的实现注入。

**风险:** 由于调用栈仍绕过配置层，tabSize/pageSize/stickyTabStop、`multiCursorMergeOverlapping`, `emptySelectionClipboard`, `columnFromVisibleColumn` 等编辑器选项在 C# 里依旧不可配置，列选/多光标行为与 TS 差距不变。

**建议:** 将 `Cursor.cs` 命令入口、`CursorCollection` 正规化逻辑与 `CursorColumns` 可视列计算改为依赖 `CursorConfiguration`，然后移除旧的手动 tabSize/projection 代码。完成后即可在 `#delta-2025-11-26-aa4-cl7-cursor-core` 关闭 Stage 0 接线部分。

---

### 1. Cursor.cs
**TS源:** `ts/src/vs/editor/common/cursor/oneCursor.ts`
**C#文件:** `src/TextBuffer/Cursor/Cursor.cs`
**对齐状态:** ❌需要修正

**差异要点:**
- TS `Cursor` 只负责状态同步并依赖 `_setState` 与 `CursorMoveOperations`，而 C# 把 `MoveLeft/Right/Up/Down`, `MoveWord*`, `DeleteWordLeft` 等逻辑全部塞进 `Cursor`，与 VS Code 的职责划分完全不同。
- 即便 Stage 0 已提供 `SingleCursorState`，本类仍直接持有 `_selection`/`_stickyColumn`，没有 `_setState`/`TrackedRangeStickiness` 流程；多光标编辑后无法借助 `CursorContext` 恢复位置。
- 粘列信息在 TS 中写入 `leftoverVisibleColumns` 并跟随 `CursorState` 序列化；虽然 Stage 0 已提供这些字段，但 `Cursor` 仍把 `_stickyColumn` 当作临时字段，`CursorCollection`/Snippet/Undo 无法共享。
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
- TS 维持主/次光标、`lastAddedCursorIndex`、`normalize()`、`getTopMostViewPosition()` 等，而 C# 版本只有 `CreateCursor`, `RemoveCursor`, `GetCursorPositions`，缺少全部集合 API；Stage 0 新 `CursorState` 也未被持有。
- 没有 `setStates()`/`_setSecondaryStates()`，无法套用命令计算出的 `PartialCursorState`；`killSecondaryCursors()`、`getAll()`、`readSelectionFromMarkers()` 等全部缺席，`CursorState`/tracked range 数据无法落地。
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
**对齐状态:** ⚠️存在偏差（结构已到位，调用方未接入）

**差异要点:**
- Stage 0 已实现 `ICoordinatesConverter`（含 `IdentityCoordinatesConverter`）与 `ICursorSimpleModel` 适配器，但 `TextModel.CreateCursorCollection()` 仍直接 new `CursorCollection(this)`，没有创建 `CursorContext` 或将配置注入命令栈。
- `CursorContext` 目前只是一组属性，没有 TS `computeCursorState()`/`getTrackedSelection` 等协作点；`Cursor`/`CursorCollection` 依旧绕过上下文管理 tracked range，因此 `CursorState` 中的双态数据无法重新计算。
- 因未实例化 `CursorContext.FromModel()`，`CursorColumns`、`WordOperations`、Snippet command 仍无法获取 `CursorConfiguration` 的 pageSize/stickyTabStop/wordSeparators 设置（即使配置类型已经存在）。

**建议:**
1. 在 `TextModel.CreateCursorCollection()`/`CursorCollection` 构造函数中创建 `CursorContext` 并传入 `Cursor`，让所有命令都依赖 `CoordinatesConverter`/`CursorConfig`。
2. 按 TS `cursorContext.ts` 补齐 `GetViewPositions()`, `ComputeCursorStateAfterCommand()` 等 helper，使 tracked range/视图位置恢复逻辑可以共享。
3. 接线完成后，在 `#delta-2025-11-26-aa4-cl7-cursor-core` 中记录 feature flag 切换，确保 Stage 0 能真正驱动 Stage 1 命令。

---

### 5. CursorState.cs
**TS源:** `ts/src/vs/editor/common/cursorCommon.ts`
**C#文件:** `src/TextBuffer/Cursor/CursorState.cs`
**对齐状态:** ⚠️存在偏差（类型 parity 已完成，但未被消费者使用）

**差异要点:**
- Stage 0 已包含 `SingleCursorState`, `CursorState`, `PartialModelCursorState`, `PartialViewCursorState`, `SelectionStartKind` 与 leftovers 字段；不过 `Cursor` 依旧维护 `_selection`/`_stickyColumn` 私有字段，`CursorCollection` 也不持有这些新对象。
- 没有任何命令调用 `CursorState.Move()`/`CursorState.FromModelSelections()`，因此 tracked range/粘列数据虽然可序列化，却不会在 undo/redo、Snippet、列选流程中共享。
- `CursorCoreTests` 仅覆盖 Stage 0 构造/转换逻辑，缺乏与 `CursorCollection.setStates()`、`CursorWordOperations` 的互操作测试，使 `#delta-2025-11-26-aa4-cl7-cursor-core` 仍旧保持 Gap。

**建议:**
1. 调整 `Cursor` 与 `CursorCollection`，让状态更新完全通过 `CursorState`/`SingleCursorState` 驱动，而非手写 `Selection` 字段。
2. 把 tracked range/sticky column 流程放入 `CursorCollection.setStates()`，并为 snippet/command 管线提供 `Partial*` 构造函数入口。
3. 扩展 `CursorCoreTests` 以涵盖 state ↔ command 循环，再结合 `CursorAtomicMoveOperationsTests` 在 `#delta-2025-11-26-aa4-cl7-commands-tests` 解除测试缺口。

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
- **🚫 缺失:** _暂无_（`CursorConfiguration` 已在 `WS4-PORT-Core` 引入，但未接入运行路径）

> 说明：`CursorConfiguration`/`CursorContext`/`CursorState` 虽完成 Stage 0 port，但由于运行态尚未接入，仍在此列表中跟踪。

### 优先级建议

#### P0 – Stage 拆分矩阵
| Placeholder | Delivered (Stage 0) | Outstanding (Stage 1) |
| --- | --- | --- |
| [`#delta-2025-11-26-aa4-cl7-cursor-core`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) | `WS4-PORT-Core` 已交付 `CursorConfiguration`/`CursorState`/`CursorContext`、tracked range/隐藏装饰支持，以及 25/25 Stage 0 `CursorCoreTests`（当前命令 39 通过 / 0 失败 / 2 跳过；见 [`docs/reports/migration-log.md#ws4-port-core`](../migration-log.md#ws4-port-core)）。 | 将 `Cursor`/`CursorCollection`/`CursorContext` 接线、启用 `TextModelOptions.EnableVsCursorParity`、实现 `_setState`/tracked range 恢复，并在 `agent-team/indexes` 记录 Stage 1 关闭。 |
| [`#delta-2025-11-26-aa4-cl7-column-nav`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-column-nav) | 仅保留早期 `CursorColumns.GetVisibleColumn*` 辅助函数，缺少 `ColumnSelection` state plumbing。 | Port `IColumnSelectResult`/`ColumnSelection.columnSelect*`，将 `CursorConfiguration.columnFromVisibleColumn` 接入列选命令与 `CursorCollection.normalize()`。 |
| [`#delta-2025-11-26-aa4-cl7-wordops`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-wordops) | `WordOperations` 仅覆盖 Move/Select/DeleteWordLeft，`WordCharacterClassifier` 仍是最小实现。 | 引入 `_createWord`/`DeleteWordContext`/word-part、Intl heuristics、auto-closing pair 逻辑及 TS 对应测试。 |
| [`#delta-2025-11-26-aa4-cl7-snippet`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-snippet) | 现有 SnippetSession 修复了 BF1 循环，但仍是 `${n:text}` 级别解析。 | Port `OneSnippet`、placeholder group、变量/transform/choice、merge/undo 生命周期，并把状态绑定 `CursorState`。 |
| [`#delta-2025-11-26-aa4-cl7-commands-tests`](../../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-commands-tests) | `CursorCoreTests` (25) + 旧 `CursorTests` (23) 是唯一覆盖；未新增 column select/word ops/snippet 测试。 | 补齐 `CursorWordOperationsTests`, `CursorAtomicMoveOperationsTests`, `ColumnSelectionTests`, `SnippetControllerTests` TS 矩阵，并把 rerun 写入 `tests/TextBuffer.Tests/TestMatrix.md`。 |

#### P1
- Column selection 页面/注入文本/RTL 兼容性：当 Stage 1 command ready 后，需要实现 `ICoordinatesConverter` aware 的 `columnSelectLeft/Right/Up/Down` 以及 `multiCursorMergeOverlapping` normalize。
- Word navigation 删除策略：完成 Stage 1 主要命令后，将 auto-closing pair、camelCase/snake_case、Intl Segmenter hooks 纳入 `WordCharacterClassifierCache`。
- Snippet lifecycle 基础：在 Stage 1 SnippetController 成熟后，加上上下文键、undo/redo/clipboard 选项，并与 completion 管线对齐。

#### P2
- Snippet 变量/transform/choice merge、嵌套 session、`InsertSnippetOptions` 完整实现。
- Intl word cache + accessibility word ops，支撑屏幕阅读器/wordPart 命令。
- 将 column selection + snippet 命令加入 DocUI/renderer 交互测试，确保 Stage 1 行为不会在 UI 层发生偏差。

### 移植质量评估
- 当前 Cursor 栈仍偏向**重新实现**：虽然 Stage 0 已有 `CursorConfiguration`/`CursorState`/`CursorContext`，但运行态命令尚未接线，列选择、word ops、snippet 依旧是最小骨架。
- 若不先完成 `CursorCollection.setStates/normalize` 与 `Cursor` → `CursorState` 的接线，TS bugfix/feature（sticky column、多光标 merge、snippet choice）无法复用，`#delta-2025-11-26-aa4-cl7-*` 占位也无法关闭。
- 完成 Stage 0 落地后，再逐步对齐 column select（`cursorColumnSelection.ts`）、word operations、snippet lifecycle 并补足测试矩阵。

## Verification Notes
- **2025-11-27 – Stage 0 spot-check:** `dotnet test --filter CursorCoreTests --nologo`（39 通过 / 0 失败 / 2 占位跳过）复测 `WS4-PORT-Core` 交付并确认 25/25 Stage 0 case 仍绿（参见 [`docs/reports/migration-log.md#ws4-port-core`](../migration-log.md#ws4-port-core)）；此运行跳过 `IntervalTreePerfTests`（既知 WS3 性能问题），以免干扰 Cursor 结果。
- 逐一阅读 `docs/reports/alignment-audit/03-cursor.md` 旧版、`src/TextBuffer/Cursor/*.cs` 以及 `ts/src/vs/editor/common/cursor/*.ts`、`ts/src/vs/editor/contrib/snippet/browser/*.ts`，确认功能覆盖差距。
- 特别验证了 `SnippetSession.NextPlaceholder/PrevPlaceholder` 的 BF1 哨兵逻辑、`Cursor.cs` 缺乏 `SingleCursorState`、`CursorCollection` 未实现 `normalize`、`CursorColumns` 只有转换 helper。
- Stage 0 文件（`CursorConfiguration`, `CursorState`, `CursorContext`, `ICoordinatesConverter`）已查验完毕，但尚未被 `Cursor`/`CursorCollection` 引用；需在 `#delta-2025-11-26-aa4-cl7-cursor-core` 交付前明确它们的接入顺序与命名。
