# Sprint05-M3-DiffRegressionTests-Result

## 实现摘要

扩展 Diff 回归测试套件，从 40 tests 扩展到 95 tests（DiffTests 59 + RangeMappingTests 36）。新增测试覆盖 UnchangedRegions、PostProcessCharChanges、边界 Cases、性能测试和 TS 回归测试。

## 文件变更

| 文件 | 变更类型 | 描述 |
|------|----------|------|
| `tests/TextBuffer.Tests/DiffTests.cs` | 扩展 | 从 4 tests 扩展到 59 tests |

## TS 对齐说明

| TS 测试 | C# 实现 | 备注 |
|---------|---------|------|
| `diffComputer.test.ts` 各种 insertions/deletions | `Insertions_Various_*`, `Deletions_Various_*` | Theory + InlineData 参数化 |
| `two lines changed 1/2/3` | `TwoLinesChangedToOne_*`, `TwoLinesChangedInMiddle_*` | 完全对齐 |
| `big change part 1/2` | `BigChangePart1_*`, `BigChangePart2_*` | 完全对齐 |
| `pretty diff 3` | `PrettyDiff_MethodInsertion_*` | IgnoreTrimWhitespace 模式 |
| `issue #12122`, `#43922`, `#42751`, `#119051`, `#169552` | 各种 `Issue_*` 测试 | 回归测试 |
| `LineRangeMapping.inverse()` | `UnchangedRegions_*` | 10 个用例 |

## 新增测试分类

### 1. UnchangedRegions 测试 (10 cases)
- `UnchangedRegions_IdenticalDocuments_ReturnsEntireDocument`
- `UnchangedRegions_SingleInsertion_ComputesBeforeAndAfterRegions`
- `UnchangedRegions_SingleDeletion_ComputesBeforeAndAfterRegions`
- `UnchangedRegions_SingleLineChange_ComputesCorrectRegions` (Theory, 3 cases)
- `UnchangedRegions_MultipleChanges_ComputesGapsBetween`
- `UnchangedRegions_LargeUnchangedBlock_PreservesCorrectly`
- `UnchangedRegions_ConsecutiveChanges_MergesCorrectly`
- `UnchangedRegions_AllChanged_NoUnchangedRegions`
- `UnchangedRegions_InterleavedChanges_ComputesCorrectGaps`

### 2. PostProcessCharChanges 测试 (8 cases)
- `CharChanges_EmptyChange_NoInnerChanges`
- `CharChanges_FullLineChange_SingleInnerChange`
- `CharChanges_PartialLineChange_CorrectRange` (Theory, 3 cases)
- `CharChanges_MultipleCharChangesInLine_AllReported`
- `CharChanges_CrossLineChange_SpansMultipleLines`
- `CharChange_SingleCharModification_CorrectRange` (Theory, 3 cases)

### 3. 边界 Cases 测试 (9 cases)
- `BoundaryCase_EmptyDocuments_NoChanges`
- `BoundaryCase_EmptyToNonEmpty_SingleChange`
- `BoundaryCase_NonEmptyToEmpty_SingleChange`
- `BoundaryCase_SingleLineDocument_CorrectDiff`
- `BoundaryCase_LineEndingDifference_HandledCorrectly` (Theory, 2 cases)
- `BoundaryCase_WhitespaceOnlyDifference_RespectsSetting` (Theory, 4 cases)

### 4. 性能测试 (5 cases)
- `Performance_10KLines_CompletesWithoutTimeout`
- `Performance_50KLines_CompletesOrTimesOut`
- `Performance_TimeoutBoundary_RespectsLimit` (Theory, 3 cases)

### 5. TS 回归测试 (23 cases)
- `Insertions_Various_ProducesCorrectDiff` (Theory, 3 cases)
- `Deletions_Various_ProducesCorrectDiff` (Theory, 3 cases)
- `PrettyDiff_MethodInsertion_AlignedCorrectly`
- `Issue_HasOwnProperty_NotFunction`
- `TwoLinesChangedToOne_ProducesCorrectMapping`
- `TwoLinesChangedInMiddle_ProducesCorrectMapping`
- `ThreeLinesChanged_ProducesCorrectMapping`
- `BigChangePart1_InsertionAndModification`
- `BigChangePart2_InsertionModificationAndDeletion`
- `LongMatchingLines_PreferredOverShort`
- `FewerDiffHunks_Preferred`
- `NestedBlockInsertion_ProducesCorrectDiff`
- `LeadingAndTrailingWhitespaceDiff_HandledCorrectly`
- `Issue43922_YarnInstallDiff`
- `Issue42751_IndentationChange`

## 测试结果

### Targeted
```bash
dotnet test --filter "DiffTests|RangeMappingTests"
```
→ **95/95 passed** (59 DiffTests + 36 RangeMappingTests)

### Full Suite
```bash
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
```
→ **964 passed + 9 skipped = 973 total** (≈1m48s)

## 测试覆盖统计

| 类别 | 之前 | 之后 | 增量 |
|------|------|------|------|
| DiffTests | 4 | 59 | +55 |
| RangeMappingTests | 36 | 36 | 0 |
| Diff 相关总计 | 40 | 95 | +55 |
| 全量总计 | 918 | 973 | +55 |

## 已知差异

无有意差异 - 完全对齐 TS 测试语义。

## 遗留问题

1. **UnchangedRegions 单元测试**：当前使用 `LineRangeMapping.Inverse()` API 测试。如果未来需要更细粒度的 `UnchangedRegion` 类，需要额外实现。
2. **性能测试稳定性**：`Performance_TimeoutBoundary_RespectsLimit(1ms)` 和 `(10ms)` 在慢速 CI 环境下可能表现不同。

## Changefeed Anchor

`#delta-2025-12-02-sprint05-m3-diff-regression-tests`
