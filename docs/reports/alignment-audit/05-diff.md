# Diff Algorithms 模块对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 16个Diff算法相关文件

## 概要
- 完全对齐: 10/16
- 存在偏差: 5/16
- 需要修正: 1/16

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
**对齐状态:** ⚠️存在偏差

**分析:**
- 核心 DP 算法逻辑对齐 ✅
- LCS 计算、方向追踪、回溯逻辑均正确
- `equalityScore` 参数处理正确

**偏差说明:**
1. TS 版本使用 `Math.max(horizontalLen, verticalLen, extendedSeqScore)` 确定方向
2. C# 版本使用 if-else 链手动比较，逻辑正确但顺序不同:
   - TS: 当 `newValue === extendedSeqScore` 时优先选择对角线
   - C#: 使用显式 direction 变量，先设为水平，再检查垂直，最后检查对角线
   - **两者语义等价**，因为 C# 的 `if (extendedSeqScore > newValue)` 确保了相同的优先级

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
- `ComputeMovedLines` 主函数结构对齐
- `ComputeMovesFromSimpleDeletionsToSimpleInsertions`: 对齐 ✅
- `ComputeUnchangedMoves`: 基本对齐，但有差异:
  - C# 版本额外添加了 `referenceChanges` 和 `analysisChanges` 参数（合并相邻变更后的分析）
  - C# 版本使用 `BuildClusters` 来分组变更，TS 版本直接迭代变更
  - C# 版本添加了 `ExpandRange` 和上下文行处理逻辑
- `AreLinesSimilar`: 对齐 ✅
- `JoinCloseConsecutiveMoves`: 对齐 ✅
- `RemoveMovesInSameDiff`: 对齐 ✅
- `FilterMovesByContent`: 对齐 ✅
- `DetectShiftedBlocks`: **C# 新增功能**，TS 中不存在

**偏差说明:**
1. C# 版本添加了 `MergeAdjacentChangesForMoves` 和 `DetectShiftedBlocks` 额外功能
2. 窗口键使用 3 行哈希，与 TS 一致
3. `PossibleMapping` 和 `WindowKey` 辅助类型对齐

**修正建议:**
- 如果需要严格对齐，应移除 `DetectShiftedBlocks` 和相关的扩展逻辑
- 当前实现是 TS 版本的超集，功能上兼容

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
1. C# 使用静态方法，TS 使用实例方法
2. C# 的 `DiffComputerOptions` 添加了 `ExtendToWordBoundaries` 选项（默认 true），TS 无此选项
3. C# `RefineDiff` 中根据 `options.ExtendToWordBoundaries` 条件调用 `ExtendDiffsToEntireWordIfAppropriate`，TS 版本无条件调用

**修正建议:**
- 如需严格对齐，应将 `ExtendToWordBoundaries` 默认行为改为始终启用（与 TS 一致）

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
- 可保留作为 C# 特有的调试/配置选项，但需在文档中说明

---

### 8. DiffMove.cs
**TS源:** `linesDiffComputer.ts` (Lines 50-80)
**对齐状态:** ✅完全对齐

**分析:**
- 对应 TS 的 `MovedText` 类
- `LineRangeMapping` 属性对齐
- `Changes` 属性对齐（对应 TS 的 `changes`）
- 便捷属性 `Original` 和 `Modified` 是 C# 特有的便捷访问器

**缺失:**
- `flip()` 方法未移植（TS 中用于翻转移动方向）

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
**对齐状态:** ⚠️存在偏差

**分析:**
- `GetElement`: 对齐 ✅
- `Length`: 对齐 ✅
- `GetBoundaryScore`: 存在差异
  - TS: `length === this.lines.length ? 0 : getIndentation(this.lines[length])`
  - C#: `Math.Min(length, _lines.Length - 1)` 作为索引
  - **偏差**: 当 `length == lines.Length` 时，TS 返回 0，C# 访问最后一行
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

---

## 总结

### 完全对齐的文件 (10/16):
1. MyersDiffAlgorithm.cs
2. Array2D.cs
3. DiffMove.cs
4. DiffResult.cs
5. HeuristicSequenceOptimizations.cs
6. LineRange.cs
7. LineRangeFragment.cs
8. LinesSliceCharSequence.cs
9. OffsetRange.cs
10. DynamicProgrammingDiffing.cs

### 存在偏差但可接受的文件 (5/16):
1. **DiffAlgorithm.cs** - `equalityScore` 参数扩展、缺少断言方法
2. **ComputeMovedLines.cs** - 添加了额外的 shifted blocks 检测功能
3. **DiffComputer.cs** - `ExtendToWordBoundaries` 选项扩展
4. **DiffComputerOptions.cs** - 额外配置选项
5. **LineSequence.cs** - `GetBoundaryScore` 边界处理略有差异

### 需要修正的文件 (1/16):
1. **RangeMapping.cs** - 缺少多个静态工厂方法

### 建议优先级:

**高优先级:**
1. 修复 `LineSequence.GetBoundaryScore` 的边界处理逻辑
2. 添加 `LineRangeMapping.Inverse()` 和 `LineRangeMapping.Clip()` 方法

**中优先级:**
1. 添加 `SequenceDiff.AssertSorted()` 调试方法
2. 添加 `DiffMove.Flip()` 方法

**低优先级:**
1. 添加 `DateTimeout.Disable()` 方法
2. 添加 `RangeMapping.FromEdit()` 相关方法（如果需要编辑功能）
