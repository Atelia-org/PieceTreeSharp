# Decorations 模块对齐审查报告

**审查日期:** 2025-11-26  
**审查范围:** 5个装饰相关文件  

## 概要
- 完全对齐: 2/5
- 存在偏差: 2/5
- 需要修正: 1/5

## 详细分析

### 1. DecorationChange.cs
**TS源:** textModel.ts  
**对齐状态:** ⚠️存在偏差（设计差异）

**分析:**

C# 实现创建了一个独立的 `DecorationChange` 结构体和 `DecorationDeltaKind` 枚举来追踪装饰变化。然而，在 TypeScript 原版中：

1. **TS原版设计:** TypeScript 使用 `DidChangeDecorationsEmitter` 类来管理装饰变化事件，通过 `IModelDecorationsChangedEvent` 接口传递变化信息。TS 没有专门的 "DecorationChange" 类型，而是通过事件系统（`affectsMinimap`, `affectsOverviewRuler`, `affectedInjectedTextLines` 等标志）来标识变化。

2. **C# 实现设计:** 
   - `DecorationDeltaKind` 枚举（Added, Removed, Updated）是合理的抽象
   - `DecorationChange` 结构体包含 `Id`, `OwnerId`, `Range`, `Kind`, `Options`, `OldRange`

**偏差说明:**
- C# 实现提供了更结构化的变化追踪机制，这是一个**设计增强**而非直译
- TS 原版使用事件标志方式，C# 使用结构化变化记录方式

**修正建议:** 
无需修正。C# 实现的设计更适合 .NET 生态，提供了更清晰的变化追踪 API。这是一个可接受的语言惯例差异。

---

### 2. DecorationOwnerIds.cs
**TS源:** textModel.ts  
**对齐状态:** ⚠️存在偏差（简化实现）

**分析:**

**TS原版设计:**
在 TypeScript 中，`ownerId` 是一个动态值：
- 调用 `changeDecorations` 或 `deltaDecorations` 时传入 `ownerId` 参数（默认为 0）
- `ownerId` 用于过滤装饰：`filterOwnerId && node.ownerId && node.ownerId !== filterOwnerId`
- 没有预定义的常量，`ownerId = 0` 通常表示"无特定所有者"

**C#实现:**
```csharp
public static class DecorationOwnerIds
{
    public const int Default = 0;
    public const int SearchHighlights = 1;
    public const int Any = -1;
}
```
# Decorations 模块对齐审查报告

**审查日期:** 2025-11-26  
**审查范围:** 7 个装饰与 DocUI 相关组件  

## 概要
- 完全对齐: 3/7
- 存在偏差: 3/7
- 需要修正: 1/7

## 详细分析

### 1. DecorationChange.cs / TextModelDecorationsChangedEventArgs.cs / TextModel.cs
**TS 源:** textModel.ts + textModelEvents.ts  
**对齐状态:** ✅完全对齐

**分析:**
- `src/TextBuffer/Decorations/DecorationChange.cs` 为 TS `IModelDecorationsChangedEvent.changes` 引入结构化记录；`DecorationDeltaKind` 与 TS 事件语义一一对应（Added/Removed/Updated）。
- `src/TextBuffer/TextModel.cs` 的 `RaiseDecorationsChanged` 会把 `DecorationChange` 列表传给 `TextModelDecorationsChangedEventArgs`，并在 `RecordDecorationRange` 中推导 `affectedInjectedTextLines`、`LineHeightChange`、`LineFontChange`，对应 TS `DidChangeDecorationsEmitter.recordLineAffected*` 逻辑。
- `src/TextBuffer/TextModelDecorationsChangedEventArgs.cs` 暴露 `AffectsMinimap/Overview/GlyphMargin/LineNumber` 以及受影响的行集合，参数与 TS `IModelDecorationsChangedEvent` 一致。
- 新增的 `DecorationChange` 结构只是类型层面的增强，不改变 VS Code 事件负载。

**结论:** 当前实现忠实复制了 TS 的事件负载，且 `tests/TextBuffer.Tests/DecorationTests.cs` 覆盖了 `AffectedLineHeights`/`AffectedFontLines` 行为，无需额外调整。

---

### 2. DecorationOwnerIds.cs + 查询 API
**TS 源:** textModel.ts (`ownerId` 过滤语义)  
**对齐状态:** ⚠️存在偏差（语义差异）

**分析:**
- TS 默认以 `ownerId = 0` 表示“不过滤”，只有在 `ownerId > 0` 时才会按精确 owner 过滤（`filterOwnerId && node.ownerId && node.ownerId !== filterOwnerId`).
- C# 常量位于 `src/TextBuffer/Decorations/DecorationOwnerIds.cs`：`Default = 0`, `SearchHighlights = 1`, `Any = -1`。`TextModel.GetDecorationsInRange` 等 API（`src/TextBuffer/TextModel.cs`）把缺省值设成 `DecorationOwnerIds.Any`，但如果调用方显式传入 `0`（仿照 TS 传参），C# 版本会只返回 `ownerId == 0` 的装饰，而非“全部”。
- `GetAllDecorations`, `GetLineDecorations`、`GetFontDecorationsInRange` 等所有查询 API 都重复了这一判断，因此 DocUI 之外的移植调用若沿用 “传 0 获取全部” 的写法会得到不同结果。

**建议:**
- 在所有查询入口把 `ownerIdFilter == DecorationOwnerIds.Default` 视为 `Any`（或直接把 `Any` 常量改为 0，并将 `_nextDecorationOwnerId` 初始化为 2 以避免碰撞 `SearchHighlights = 1`）。
- 在文档中注明当前行为，避免新的调用点误用。

---

### 3. DecorationRangeUpdater.cs + stickiness 行为
**TS 源:** intervalTree.ts (`adjustMarkerBeforeColumn`, `nodeAcceptEdit`)  
**对齐状态:** ✅完全对齐

**分析:**
- `src/TextBuffer/Decorations/DecorationRangeUpdater.cs` 逐段移植了 `MarkerMoveSemantics`、stickiness 与 `collapseOnReplaceEdit` 判定，`forceMoveMarkers` 亦被保留。
- `TextModel.AdjustDecorationsForEdit`（同文件 1319 行附近）调用 `ApplyEdit` 后立刻 `Reinsert` 节点，从而保持区间树排序。
- `tests/TextBuffer.Tests/DecorationStickinessTests.cs` 和 `DecorationTests.ForceMoveMarkersOverridesStickiness` 覆盖了四种 stickiness 组合以及 `forceMoveMarkers` 的极端案例。

**结论:** 行为与 VS Code 完全一致，无需改动。

---

### 4. DecorationsTrees.cs + TextModel 查询面
**TS 源:** textModel.ts (`DecorationsTrees`、`getDecorationsInRange` 等)  
**对齐状态:** ⚠️存在偏差（功能缺口）

**分析:**
- C# `src/TextBuffer/Decorations/DecorationsTrees.cs` 也维护 regular/overview/injected 三棵树，并在 `TextModel` 的 `GetInjectedTextInLine`、`GetAllMarginDecorations`、`GetFontDecorationsInRange` 等函数中被调用，DocUI 和 `HighlightSearchMatches` 现在可以依赖 owner 过滤及范围查询。
- 但 TS 查询 API 提供了一组过滤开关（`filterOutValidation`, `filterFontDecorations`, `onlyMinimapDecorations`, `onlyMarginDecorations`），C# 版本完全缺席。`GetLineDecorations`、`GetDecorationsInRange` 只接受 owner 过滤，并在调用后手动丢弃 `ShowIfCollapsed` 为 false 的项，无法重现 VS Code “只取非验证装饰”或“只拉 minimap 装饰”的路径。
- 同时 C# 目前没有 `DecorationProvider`（TS 用来注入括号配色装饰），意味着未来要对齐 view-model 装饰时还需要补这一层。

**建议:**
- 在 `DecorationsTrees.Search` 中加入布尔过滤，或让 `TextModel` 层暴露与 TS 等价的参数，以便 ViewModel 层移植不需要改逻辑。
- 若短期内仍不接入 `DecorationProvider`，可在报告中明确范围，以免误判为已完成。

---

### 5. IntervalTree.cs + TextModel.AdjustDecorationsForEdit
**TS 源:** intervalTree.ts（`delta` 惰性位移、`acceptReplace`）  
**对齐状态:** ❌需要修正（性能与 metadata 缺口）

**分析:**
- `src/TextBuffer/Decorations/IntervalTree.cs` 缺少 `delta` 字段与 `_normalizeDeltaIfNecessary`，节点的 start/end 在每次编辑后被立即改写。`TextModel.AdjustDecorationsForEdit` 通过 `_decorationTrees.Search`+`EnumerateFrom(offset)` 遍历所有“受影响以及位于编辑点之后”的装饰，再逐个调用 `DecorationRangeUpdater.ApplyEdit` 并 `Reinsert`。这保证了正确性，但当文档存在上万装饰时会退化为 O(n)；VS Code 通过 `IntervalTree.acceptReplace` 在树层面批量平移 start/end，复杂度为 O(log n + k)。
- Node metadata 同样被省略：TS `metadata` 位掩码（是否验证、是否 glyphMargin、affectsFont、stickiness、collapseOnReplaceEdit）用于在搜索过程中快速剔除验证装饰或字体装饰。C# 只在 `CollectOverlaps` 里按 `ownerId` 过滤，导致上层无法重现 VS Code 的过滤组合。

**建议（高优先级）:**
- 按 TS 设计为节点添加 `Delta` 字段与 `_normalizeDeltaIfNecessary` 机制，并实现 `AcceptReplace(offset, removedLength, insertedLength, forceMoveMarkers)`，让 `TextModel` 只需调用树方法即可完成批量更新。
- 给节点补充 `Flags` 或独立布尔字段，以便后续实现 `filterOutValidation`、`filterFontDecorations`。

---

### 6. ModelDecoration.cs / ModelDeltaDecoration.cs
**TS 源:** model.ts (`ModelDecorationOptions`, 枚举常量)  
**对齐状态:** ⚠️存在偏差（常量/枚举未对齐）

**分析:**
- `src/TextBuffer/Decorations/ModelDecoration.cs` 把 `LineHeightCeiling` 设为 1500，并在 `NormalizeLineHeight` 中 `Math.Clamp(value, 1, 1500)`。TS `LINE_HEIGHT_CEILING` 为 300，因此 C# 允许 VS Code 会拒绝的高度，渲染/排版结果会偏离。
- `MinimapPosition`, `GlyphMarginLane`, `InjectedTextCursorStops` 数值与 TS 枚举不同（TS: Inline/Gutter=1/2, Glyph lanes=1/2/3, CursorStops=Both/Right/Left/None=0..3；C#: 从 0 起且 `InjectedTextCursorStops` 被建模成 Flags）。虽然目前仅在 C# 内部使用，但一旦需要与 JSON 或远端协议共享描述，就会不兼容。
- `ModelDecorationMinimapOptions.SectionHeaderStyle`、`SectionHeaderText` 在 TS 中分别是 `MinimapSectionHeaderStyle` 枚举与 `string | null`，C# 直接用 `string`；同样缺少 `HideInCommentTokens/HideInStringTokens` 的消费方。
- `ModelDeltaDecoration` 已经把 `ModelDecorationOptions.Normalize()` 内联，整体没问题。

**建议:**
- 将 `LineHeightCeiling` 下降到 300 以匹配 VS Code 行高安全阈值。
- 令 `MinimapPosition`, `GlyphMarginLane`, `InjectedTextCursorStops` 与 TS 数值一致（不需要 Flags，直接跟随 0..n 的枚举即可）。
- 如果 `MinimapSectionHeaderStyle` 功能将来需要端到端支持，应当把它建模成枚举而非任意字符串。

---

### 7. DocUI Find 栈（FindDecorations / FindModel / DocUIFindController）
**TS 源:** findDecorations.ts, findModel.ts, findController.ts  
**对齐状态:** ✅完全对齐

**分析:**
- `src/TextBuffer/DocUI/FindDecorations.cs` 重现了 VS Code 的三套装饰（当前匹配、其它匹配、范围高亮），并在 `Set` 中复用 `>1000` 阈值切换到 overview-only 模式；`CalculateMergeLinesDelta` 也使用了 viewport 高度以控制 approx 装饰数量。`_ownerId` 通过 `TextModel.AllocateDecorationOwnerId` 独立管理。
- `src/TextBuffer/DocUI/FindModel.cs` 绑定 `TextModel` 与 `FindDecorations`：`Research()` 会调用 `TextModel.FindMatches` + `FindDecorations.Set` 并用 `_decorations.GetCurrentMatchesPosition` 更新 `FindReplaceState`，`ReplaceAll` 与 `_largeReplaceAll` 亦包含。
- `src/TextBuffer/DocUI/DocUIFindController.cs` 负责命令与状态机，`Start`/`MoveToNextMatch`/`SelectAllMatches` 的行为、查找作用域、全局剪贴板播种逻辑都和 TS 一致。
- `tests/TextBuffer.Tests/DocUI/DocUIFindDecorationsTests.cs`、`DocUIFindModelTests.cs` 等验证了范围高亮裁剪尾行、overview throttling、匹配循环、ReplaceAll 结果，确保 DocUI 管道能在 C# 模型上工作。

**结论:** DocUI find-decorations 管道（Sprint 03 R16-R19）现已到位，可视作 aligned。

---

## 修正优先级汇总

1. **高优先级** – 为 `src/TextBuffer/Decorations/IntervalTree.cs` 增加 `delta/acceptReplace` 机制，避免每次编辑遍历全量装饰；同时补齐节点 metadata，以便实现 VS Code 的过滤路径。
2. **中优先级** – 对齐 `src/TextBuffer/Decorations/DecorationOwnerIds.cs` 与 `TextModel` 过滤语义（让 0/Any 等价），并把 `ModelDecoration.LineHeightCeiling` 从 1500 降到 300。
3. **低优先级** – 统一 `MinimapPosition`、`GlyphMarginLane`、`InjectedTextCursorStops` 的数值；在 `DecorationsTrees`/`TextModel` 上补充 `filterOutValidation` 等参数，为未来的 ViewModel 移植预留空间。

## 结论

装饰子系统的大部分核心（事件、stickiness、DocUI 管道）已与 VS Code 对齐，并且 `GetAllDecorations`、`GetLineDecorations`、`GetInjectedTextInLine` 等 API 现已可用，DocUI 测试覆盖也说明“scope throttling / owner filters / overview metadata” 已落地。仍需关注的主线是：

1. **树层性能与过滤能力** 尚未达到 TS 的水准（缺少 delta、metadata、过滤开关）。
2. **常量与枚举**（lineHeight ceiling、ownerId sentinel、minimap/glyph/injectedText 编码）与 VS Code 不一致，未来一旦需要跨组件通信会暴露问题。
3. 一些 TS 配置（minimap section header 样式、隐藏验证装饰等）尚未被消费，需要在后续阶段补齐。

## Verification Notes
- 对照文件：`src/TextBuffer/Decorations/*.cs`, `src/TextBuffer/TextModel*.cs`, `src/TextBuffer/DocUI/*.cs`、`tests/TextBuffer.Tests/**`, 以及 `ts/src/vs/editor/common/model/*.ts`、`ts/src/vs/editor/contrib/find/browser/*.ts`。
- 重点查验：`LineHeightCeiling`、`DecorationsTrees` 分支逻辑、`IntervalTree` 的 delta/metadata、`DocUI` overview throttling、大型 ReplaceAll、`ModelDecorationsChangedEvent` 负载。
- 未决问题：是否要让 `DecorationOwnerIds.Any` 与 `Default (0)` 等价？`MinimapSectionHeaderStyle` 是否需要枚举化？`DecorationsTrees` 过滤开关计划在何时补齐？这些需要产品/性能评审进一步确认。
1. **高优先级 - 实现 delta 机制:** 
