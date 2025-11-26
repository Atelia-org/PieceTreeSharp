# Diff Algorithms 模块对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 16个Diff算法相关文件

## 概要
- 完全对齐: 9/16
- 存在偏差: 5/16
- 需要修正: 2/16
- DocUI diff 渲染: ❌ 未接入（目前仅有 MarkdownRenderer 测试用装饰，详见后文）

## 详细分析

### 1. DiffAlgorithm.cs
**TS源:** `algorithms/diffAlgorithm.ts`
**对齐状态:** ⚠️存在偏差

**分析:**
- `IDiffAlgorithm` 接口: C# 版本添加了额外的 `equalityScore` 参数 `Func<int, int, double>?`，TS版本没有此参数
- `DiffAlgorithmResult`: 对齐 ✅
- `SequenceDiff`: 对齐 ✅，所有方法（Invert, FromOffsetPairs, Swap, Join, Delta, DeltaStart, DeltaEnd, IntersectsOrTouches, Intersect, GetStarts, GetEndExclusives）均已正确移植
- `OffsetPair`: 对齐 ✅
- `ISequence`: 对齐 ✅，注意 `GetBoundaryScore` 在 TS 中是可选的 (`getBoundaryScore?`)，C# 中改为必须实现
- `ITimeout`: C# 使用属性 `IsValid`，TS 使用方法 `isValid()` - 语义等价
- `InfiniteTimeout`: 对齐 ✅
- `DateTimeout`: 对齐 ✅，C# 使用 `Environment.TickCount64`，TS 使用 `Date.now()` - 合理的运行时差异
- **缺失**: TS 中的 `SequenceDiff.assertSorted()` 静态方法未移植（调试/断言用途，可选）
- **缺失**: TS 中 `DateTimeout.disable()` 方法未移植

**偏差说明:**
1. `IDiffAlgorithm.Compute` 签名中的 `equalityScore` 参数是 C# 特有的扩展
2. `ISequence.GetBoundaryScore` 在 TS 中是可选方法，C# 中是必须实现的接口成员

**修正建议:**
1. 考虑将 `equalityScore` 参数移至特定实现类，或者明确这是 C# 特有的扩展
2. 建议添加 `SequenceDiff.AssertSorted()` 用于调试

---

### 2. DynamicProgrammingDiffing.cs
**TS源:** `algorithms/dynamicProgrammingDiffing.ts` (Lines 10-150)
**对齐状态:** ✅完全对齐

**分析:**
- 核心 DP 算法逻辑、LCS 表、方向追踪、回溯流程均一一对应
- C# 以 if-else 链替代 `Math.max`，但“优先沿对角线”的策略仍由 `extendedSeqScore > newValue` 判定保障，行为与 TS 相同
- `equalityScore` 委托仅在 `DynamicProgrammingDiffing` 中使用，匹配 TS 的可选参数

**结论:** 无需修正

**修正建议:** 无需修正，实现等价

---

### 3. MyersDiffAlgorithm.cs
**TS源:** `algorithms/myersDiffAlgorithm.ts` (Lines 15-200)
**对齐状态:** ✅完全对齐

**分析:**
- Myers 差异算法核心逻辑完全对齐
- `getXAfterSnake` 函数对齐
- V 数组和 paths 数组处理对齐
- 边界条件 (`lowerBound`, `upperBound`) 计算正确
- `SnakePath` 内部类对齐
- `FastInt32Array` 和 `FastArrayNegativeIndices<T>` 辅助类对齐
  - C# 使用 `int[]` + `Array.Resize`，TS 使用 `Int32Array` 带倍增 - 等价实现
- 回溯和结果构建逻辑完全对齐

---

### 4. Array2D.cs
**TS源:** `algorithms/diffAlgorithm.ts` (Lines 200-230) / `utils.ts`
**对齐状态:** ✅完全对齐

**分析:**
- 二维数组实现完全对齐
- 使用一维数组 + 线性索引 `[x + y * Width]`
- `Get` 和 `Set` 方法对齐

---

### 5. ComputeMovedLines.cs
**TS源:** `computeMovedLines.ts` (Lines 20-800)
**对齐状态:** ⚠️存在偏差

**分析:**
- 入口、`ComputeMovesFromSimpleDeletionsToSimpleInsertions`、`FilterMovesByContent`、`JoinCloseConsecutiveMoves`、`RemoveMovesInSameDiff` 与 TS 等价
- C# 在 `ComputeUnchangedMoves` 前新增 `MergeAdjacentChangesForMoves`/`BuildClusters`/`ExpandRange`，会合并临近 diff 并为 hashed 窗口附加额外上下文，使候选 move 集合集不再与 TS 一一对应
- `DetectShiftedBlocks` 会扫描 diff 之间的“无变更区段”并仅根据 offset 差值生成移动映射，TS 并未启用这一启发式
- `AreLinesSimilar` 与 TS 完全一致

**偏差说明:**
1. cluster + expand 逻辑会将多个 diff 合并后再运行 `PossibleMapping`，容易把 VS Code 视为独立 diff 的片段解释成单个 move，影响 move 计数
2. `DetectShiftedBlocks` 可能在 TS 不会报告 move 的情形下产生额外 `LineRangeMapping`，DocUI 的 `MovedBlocksLinesFeature` 会因此与 VS Code 呈现不同

**修正建议:**
- 若要保持与 VS Code 相同的输出，应将 `MergeAdjacentChangesForMoves`/`DetectShiftedBlocks` 置于可选开关或默认禁用，并补齐 move 相关回归测试
- 若保留扩展启发式，需要在文档/选项中说明差异来源与预期收益，避免二次验收时误判

---

### 6. DiffComputer.cs
**TS源:** `defaultLinesDiffComputer.ts` (Lines 30-600)
**对齐状态:** ⚠️存在偏差

**分析:**
- 主入口 `Compute` 对应 TS 的 `computeDiff` 方法
- 行哈希构建 (`BuildLineHashes`): 对齐 ✅
- 算法选择阈值 (1700): 对齐 ✅
- `ComputeEqualityScore`: 对齐 ✅ (`modifiedLine.Length == 0 ? 0.1 : 1 + Math.Log(1 + modifiedLine.Length)`)
- `RefineDiff` 方法对齐 ✅
- `ScanWhitespace` 逻辑对齐 ✅
- 移动检测逻辑对齐 ✅

**偏差说明:**
1. C# 提供 `ExtendToWordBoundaries` 开关，可在调试时禁用；TS 中该行为始终启用
2. `DiffComputer` 仍仅在 `DiffTests` 中被直接调用，DocUI/MarkdownRenderer 等上层并不会消费 `DiffResult` 或 `DiffMove`，因此用户界面仍旧无法显示 diff 结果（详见“DocUI diff 渲染”小节）

**修正建议:**
- `ExtendToWordBoundaries` 默认值已经为 true，应避免对外暴露关闭入口，或至少记录禁用后的行为差异
- 需要将 `DiffComputer` 输出接入 DocUI/MarkdownRenderer 才能真正复现 VS Code diff 体验

---

### 7. DiffComputerOptions.cs
**TS源:** `defaultLinesDiffComputer.ts` + `linesDiffComputer.ts`
**对齐状态:** ⚠️存在偏差

**分析:**
TS `ILinesDiffComputerOptions` 接口:
- `ignoreTrimWhitespace`: ✅ 对应 `IgnoreTrimWhitespace`
- `maxComputationTimeMs`: ✅ 对应 `MaxComputationTimeMs`
- `computeMoves`: ✅ 对应 `ComputeMoves`
- `extendToSubwords?`: ✅ 对应 `ExtendToSubwords`

**偏差说明:**
C# 添加了额外选项:
- `ExtendToWordBoundaries`: TS 中无此选项（TS 总是启用此行为）

**修正建议:**
- 默认值已经与 TS 一致，但若暴露为公开配置需说明“关闭后 diff 将与 VS Code 不同”；建议仅作为内部调试或在 UI 中隐藏该选项

---

### 8. DiffMove.cs
**TS源:** `linesDiffComputer.ts` (Lines 50-80)
**对齐状态:** ⚠️存在偏差

**分析:**
- 对应 TS 的 `MovedText` 类
- `LineRangeMapping` 属性对齐
- `Changes` 属性对齐（对应 TS 的 `changes`）
- 便捷属性 `Original` 和 `Modified` 是 C# 特有的便捷访问器

**缺失:**
- `flip()` 方法未移植（TS 中用于翻转移动方向），因此需要手动构造新 `DiffMove` 才能在 DocUI/测试中交换 original/modified 角色

---

### 9. DiffResult.cs
**TS源:** `linesDiffComputer.ts` (Lines 19-37)
**对齐状态:** ✅完全对齐

**分析:**
- `LinesDiff` 基类对齐
- `DiffResult` 继承自 `LinesDiff`
- `Changes`, `Moves`, `HitTimeout` 属性对齐
- C# 添加了 `IsIdentical` 便捷属性

---

### 10. HeuristicSequenceOptimizations.cs
**TS源:** `heuristicSequenceOptimizations.ts`
**对齐状态:** ✅完全对齐

**分析:**
- `OptimizeSequenceDiffs`: 对齐 ✅（调用 JoinSequenceDiffsByShifting 两次 + ShiftSequenceDiffs）
- `RemoveShortMatches`: 对齐 ✅
- `ExtendDiffsToEntireWordIfAppropriate`: 对齐 ✅
- `RemoveVeryShortMatchingLinesBetweenDiffs`: 对齐 ✅
- `RemoveVeryShortMatchingTextBetweenLongDiffs`: 对齐 ✅
  - 评分函数 `Score` 使用相同的公式: `Math.Pow(Cap(lineCount * 40 + seqLength, max), 1.5)`
  - 阈值计算: `Math.Pow(Math.Pow(max, 1.5), 1.5) * 1.3`
- `JoinSequenceDiffsByShifting`: 对齐 ✅
- `ShiftSequenceDiffs`: 对齐 ✅
- `ShiftDiffToBetterPosition`: 对齐 ✅（包括 maxShiftLimit = 100）
- `MergeSequenceDiffs`: 对齐 ✅
- `ScanWord`: 对齐 ✅

---

### 11. LineRange.cs
**TS源:** `utils.ts` + `core/ranges/lineRange.ts`
**对齐状态:** ✅完全对齐

**分析:**
- `LineRange` 结构体对齐 ✅
- 所有方法对齐: `OfLength`, `Contains`, `Delta`, `DeltaLength`, `Join`, `Intersect`, `IntersectsOrTouches`, `ToOffsetRange`, `ToInclusiveRange`, `ToExclusiveRange`
- `LineRangeSet` 类对齐 ✅（用于高效范围集合操作）
  - `AddRange`: 对齐
  - `Contains`: 对齐
  - `SubtractFrom`: 对齐
  - `GetIntersection`: 对齐
  - `GetWithDelta`: 对齐

---

### 12. LineRangeFragment.cs
**TS源:** `utils.ts` (Lines 30-74)
**对齐状态:** ✅完全对齐

**分析:**
- 字符直方图构建逻辑对齐
- `ComputeSimilarity` 公式对齐: `1 - sumDifferences / (totalCount1 + totalCount2)`
- 字符键映射 (`CharacterKeys`) 对齐

---

### 13. LineSequence.cs
**TS源:** `lineSequence.ts`
**对齐状态:** ❌需要修正

**分析:**
- `GetElement`: 对齐 ✅
- `Length`: 对齐 ✅
- `GetBoundaryScore`: 存在差异
  - TS: `length === this.lines.length ? 0 : getIndentation(this.lines[length])`
  - C#: `Math.Min(length, _lines.Length - 1)` 作为索引
  - **影响**: 当 `length == lines.Length`（文末边界）时，TS 返回 0 以阻止 diff 向文件尾溢出，而 C# 会重复使用最后一行缩进，导致 `ShiftSequenceDiffs`/`ExtendDiffsToEntireWord` 可能把 diff 不必要地向上/向下移动
- `GetText`: 对齐 ✅
- `IsStronglyEqual`: 对齐 ✅
- `GetIndentation`: 对齐 ✅

**修正建议:**
```csharp
// 当前代码:
var indentationAfter = length == _lines.Length ? 0 : GetIndentation(_lines[Math.Min(length, _lines.Length - 1)]);
// 应修正为:
var indentationAfter = length == _lines.Length ? 0 : GetIndentation(_lines[length]);
```

---

### 14. LinesSliceCharSequence.cs
**TS源:** `linesSliceCharSequence.ts`
**对齐状态:** ✅完全对齐

**分析:**
- 元素构建逻辑（处理空白、范围裁剪）对齐 ✅
- `GetElement`: 对齐 ✅
- `Length`: 对齐 ✅
- `GetBoundaryScore`: 对齐 ✅
  - `CharBoundaryCategory` 枚举对齐
  - 评分逻辑对齐
- `TranslateOffset`: 对齐 ✅（使用二分搜索）
- `TranslateRange`: 对齐 ✅
- `FindWordContaining`: 对齐 ✅
- `FindSubWordContaining`: 对齐 ✅
- `CountLinesIn`: 对齐 ✅
- `IsStronglyEqual`: 对齐 ✅
- `ExtendToFullLines`: 对齐 ✅
- `GetText`: 对齐 ✅
- 辅助函数 `IsWordChar`, `IsUpperCase`, `GetCategory`, `GetCategoryBoundaryScore`: 对齐 ✅

---

### 15. OffsetRange.cs
**TS源:** `core/ranges/offsetRange.ts`
**对齐状态:** ✅完全对齐

**分析:**
- 所有属性对齐: `Start`, `EndExclusive`, `Length`, `IsEmpty`
- 所有方法对齐: `Empty`, `OfLength`, `OfStartAndLength`, `Delta`, `DeltaStart`, `DeltaEnd`, `Contains`, `Join`, `Intersect`, `Intersects`, `IntersectsOrTouches`, `WithMargin`, `Enumerate`, `JoinRightTouching`
- `IEquatable<OffsetRange>` 实现对齐

---

### 16. RangeMapping.cs
**TS源:** `rangeMapping.ts`
**对齐状态:** ❌需要修正

**分析:**
- `RangeMapping`: 对齐 ✅
- `LineRangeMapping`: 对齐 ✅
  - `ToRangeMapping`: 对齐 ✅
  - `ToRangeMapping2`: 对齐 ✅
- `DetailedLineRangeMapping`: 对齐 ✅
- `LineRangeMappingBuilder.FromRangeMappings`: 对齐 ✅
- `LineRangeMappingBuilder.GetLineRangeMapping`: 对齐 ✅

**需要修正:**
1. TS 中的 `LineRangeMapping.inverse()` 静态方法未移植
2. TS 中的 `LineRangeMapping.clip()` 静态方法未移植
3. TS 中的 `RangeMapping.fromEdit()` 和 `RangeMapping.fromEditJoin()` 静态方法未移植
4. TS 中的 `RangeMapping.assertSorted()` 未移植
5. TS 中的 `lineRangeMappingFromChange()` 函数未移植
6. TS `DetailedLineRangeMapping.toTextEdit()` 负责把 diff 结果转换为 `TextEdit`，C# 暂无等价实现

**修正建议:**
添加以下缺失方法:
```csharp
public static class LineRangeMappingExtensions
{
  public static List<LineRangeMapping> Inverse(
    IReadOnlyList<LineRangeMapping> mapping, 
    int originalLineCount, 
    int modifiedLineCount) { ... }
    
  public static List<LineRangeMapping> Clip(
    IReadOnlyList<LineRangeMapping> mapping,
    LineRange originalRange,
    LineRange modifiedRange) { ... }
}
```
并补齐 `RangeMapping.FromEdit*`/`DetailedLineRangeMapping.ToTextEdit()`，否则 diff 结果无法直接驱动 revert/preview 功能（TS diff editor 的 `RevertButtonsFeature` 等都依赖这些 API）。

---

### DocUI diff 渲染（`src/TextBuffer/Rendering/MarkdownRenderer.cs` vs `ts/src/vs/editor/browser/widget/diffEditor/**/*.ts`）
**状态:** ❌缺失

**分析:**
- VS Code 的 diff 视图由 `DiffEditorWidget`、`DiffEditorViewModel`、`MovedBlocksLinesFeature` 等组件驱动，直接消费 `DiffResult`/`DiffMove`/`LineRangeMapping.inverse()` 来绘制 inline diff、moved blocks、revert 按钮
- C# 侧只有 `MarkdownRenderer`，它可以渲染“已有装饰”，但不会主动运行 `DiffComputer`；`MarkdownRendererTests.TestRender_DiffDecorationsExposeGenericMarkers` 只是手工添加 `diff-add` 等装饰以验证渲染
- 搜索 `DiffComputer` 的引用可见只有 `DiffTests` 使用该 API（`rg -n "DiffComputer" src tests`），DocUI/MarkdownRenderer 没有任何调用路径

**影响:** DocUI 仍无法展示 diff（包括移动块、隐藏未更改区域、revert 按钮、accessibility diff view），与 VS Code 用户体验存在根本差距

**修正建议:**
1. 设计一个 adapter，将 `DiffComputer.Compute(...)` 的 `DiffResult` 转换成 `TextModel` 装饰或 MarkdownRenderer 可消费的数据（diff add/del markers、move 区域等）
2. 在 DocUI/MarkdownRenderer 测试中加入真实 diff 运行（而不是手工装饰），并比对 `ts/src/vs/editor/browser/widget/diffEditor` 产生的结构
3. 规划 DocUI 侧的 API（例如 `DocUIDiffRenderer`）以重用 `MovedBlocksLinesFeature` 的语义，确保 `DiffMove`、`RangeMapping.inverse` 等方法被真正使用

---

## 总结

### 完全对齐的文件 (9/16):
1. MyersDiffAlgorithm.cs
2. Array2D.cs
3. DiffResult.cs
4. HeuristicSequenceOptimizations.cs
5. LineRange.cs
6. LineRangeFragment.cs
7. LinesSliceCharSequence.cs
8. OffsetRange.cs
9. DynamicProgrammingDiffing.cs

### 存在偏差但可接受的文件 (5/16):
1. **DiffAlgorithm.cs** - `equalityScore` 参数扩展、缺少断言方法
2. **ComputeMovedLines.cs** - 扩展 cluster/shifted-blocks 启发式
3. **DiffComputer.cs** - `ExtendToWordBoundaries` 可被关闭，且尚未接 DocUI
4. **DiffComputerOptions.cs** - 额外调试选项
5. **DiffMove.cs** - 缺少 `Flip()`

### 需要修正的文件 (2/16):
1. **LineSequence.cs** - `GetBoundaryScore` 文末处理错误
2. **RangeMapping.cs** - 缺少 inverse/clip/fromEdit/toTextEdit 等静态方法

### 建议优先级:

**高优先级:**
1. 修复 `LineSequence.GetBoundaryScore` 文末索引（否则 diff 会被错误地推移）
2. 补齐 `LineRangeMapping.Inverse`/`Clip`、`RangeMapping.FromEdit*`、`DetailedLineRangeMapping.ToTextEdit`，为 revert/preview/DocUI 功能打基础
3. 设计并实现 `DiffComputer`→DocUI/MarkdownRenderer 的桥接，真正渲染 diff/move（可复用 `DiffMove`/`LineRangeMapping` 数据）

**中优先级:**
1. 为 `ComputeMovedLines` 的 cluster/shifted-blocks 启发式提供开关或 parity 测试，防止与 VS Code 结果分歧
2. 添加 `SequenceDiff.AssertSorted()` 及 `DateTimeout.Disable()`，改善调试与诊断体验
3. 为 `DiffMove` 添加 `Flip()`，简化 DocUI/测试中“交换原/改范围”的路径

**低优先级:**
1. 仅将 `ExtendToWordBoundaries` 暴露在内部；若必须公开需提供文档或 UI 提示
2. 视需要补齐 `lineRangeMappingFromChange` 等辅助函数，减少未来差异审计工作量

### Verification Notes
- 对照 `src/TextBuffer/Diff/**/*.cs` 与 `ts/src/vs/editor/common/diff/**/*.ts`，确认 AA3-006/008 已补齐 `LinesDiff`/`HeuristicSequenceOptimizations`/`DiffComputer` 主干
- 通过 `rg -n "DiffComputer" src tests` 验证该 API 目前只被 `DiffTests` 调用，DocUI/MarkdownRenderer 尚未接入
- Spot-check `ComputeMovedLines`/`MoveDetection` 与 `computeMovedLines.ts`：确认 cluster/`DetectShiftedBlocks` 为 C# 特有扩展，需要 parity 策略
- 检查 `LineSequence.GetBoundaryScore`、`RangeMapping.cs`、`DiffMove.cs` 当前实现与 TS 对照，记录缺失方法与修复建议
- TODO: 一旦 `DiffComputer` 接入 DocUI，应补充端到端测试（MarkdownRenderer/DocUIDiffRenderer）并回归 move detection parity
