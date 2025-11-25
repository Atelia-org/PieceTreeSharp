# Core Tests 模块对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 11个核心测试文件

## 概要
- 完全对齐: 3/11
- 存在偏差: 5/11
- 需要修正: 3/11

## 详细分析

---

### 1. PieceTreeBaseTests.cs
**TS源:** pieceTreeTextBuffer.test.ts (Lines 214-265)
**对齐状态:** ⚠️存在偏差

**分析:**
C# 实现移植了基本的插入/删除测试用例：
- ✅ `BasicInsertDelete` - 对应 TS `basic insert/delete`
- ✅ `MoreInserts` - 对应 TS `more inserts`
- ✅ `MoreDeletes` - 对应 TS `more deletes`
- ✅ 新增了缓存失效测试 `GetLineContent_Cache_Invalidation_Insert/Delete`

**缺失的测试用例:**
1. `random test 1-3` - 随机插入测试
2. `random delete 1-3` - 随机删除测试
3. `random insert/delete \r bug 1-5` - CRLF边界情况测试
4. 树不变量断言 `assertTreeInvariants` 未在所有测试中调用

**修正建议:**
- 添加随机操作测试以覆盖边界情况
- 实现 `AssertTreeInvariants` 辅助方法并在所有测试中调用

---

### 2. PieceTreeBuilderTests.cs
**TS源:** pieceTreeTextBuffer.test.ts (Lines 1500+)
**对齐状态:** ✅完全对齐

**分析:**
C# 实现全面覆盖了 Builder 相关测试：
- ✅ `AcceptChunk_SplitsLargeInputIntoDefaultSizedPieces` - 大块分割
- ✅ `AcceptChunk_RetainsBomAndMetadataFlags` - BOM/RTL/非ASCII标记保留
- ✅ `AcceptChunk_CarriesTrailingCarriageReturn` - 跨块CRLF处理
- ✅ `CreateNewPieces_SplitsLargeInsert` - 大块插入分片

**缺失的测试用例:** 无

**修正建议:** 无需修正

---

### 3. PieceTreeFactoryTests.cs
**TS源:** pieceTreeTextBuffer.test.ts (Lines 100+)
**对齐状态:** ✅完全对齐

**分析:**
C# 实现覆盖了工厂方法测试：
- ✅ `GetFirstAndLastLineTextHonorLineBreaks` - 首尾行文本获取
- ✅ `CreateUsesDefaultEolWhenTextHasNoTerminators` - 默认EOL选择
- ✅ `CreateNormalizesMixedLineEndingsWhenRequested` - 混合换行符规范化

**缺失的测试用例:** 无

**修正建议:** 无需修正

---

### 4. PieceTreeModelTests.cs
**TS源:** pieceTreeTextBuffer.test.ts (全文件)
**对齐状态:** ⚠️存在偏差

**分析:**
C# 实现包含高质量的模型测试：
- ✅ `LastChangeBufferPos_AppendOptimization` - 追加优化测试
- ✅ `AverageBufferSize_InsertLargePayload` - 大块插入测试
- ✅ `CRLF_RepairAcrossChunks` - 跨块CRLF修复
- ✅ `ChangeBufferFuzzTests` - 模糊测试
- ✅ `CRLF_FuzzAcrossChunks` - CRLF模糊测试
- ✅ `CRLFRepair_DoesNotLeaveZeroLengthNodes` - 零长度节点检查
- ✅ `MetadataRebuild_AfterBulkDeleteAndInsert` - 批量操作后元数据重建
- ✅ `SearchCacheInvalidation_Precise` - 搜索缓存精确失效

**缺失的测试用例:**
1. TS中的 `prefix sum for line feed` 套件 (basic, append, insert, delete, add+delete 1)
2. TS中的 `offset 2 position` 套件
3. TS中的 `get text in range` 套件 (getContentInRange, random test value in range, get line content)
4. TS中的 `buffer api` 套件 (equal, getLineCharCode - issue #45735, getNearestChunk)
5. TS中的 `search offset cache` 套件

**修正建议:**
- 添加 `GetPositionAt/GetOffsetAt` 精确映射测试
- 添加 `GetValueInRange` 范围取值测试
- 添加 `Equal` 缓冲区比较测试
- 添加 `GetLineCharCode` 字符码获取测试

---

### 5. PieceTreeNormalizationTests.cs
**TS源:** pieceTreeTextBuffer.test.ts (Lines 1730+)
**对齐状态:** ⚠️存在偏差

**分析:**
C# 实现覆盖了基本的CRLF规范化测试：
- ✅ `Delete_CR_In_CRLF_1` - 删除CRLF中的CR (测试1)
- ✅ `Delete_CR_In_CRLF_2` - 删除CRLF中的CR (测试2)
- ✅ `Line_Breaks_Replacement_Is_Not_Necessary_When_EOL_Is_Normalized` - EOL规范化后无需替换

**缺失的测试用例:**
1. TS中的 `CRLF` 套件 - 10个 `random bug 1-10` 测试
2. TS中的 `centralized lineStarts with CRLF` 套件 - 10个测试
3. `random chunk bug 1-4` 测试

**修正建议:**
- 添加CRLF随机bug复现测试（至少5个关键案例）
- 添加跨块CRLF处理测试

---

### 6. PieceTreeSearchTests.cs
**TS源:** textModelSearch.test.ts
**对齐状态:** ⚠️存在偏差

**分析:**
C# 实现覆盖了核心搜索功能：
- ✅ `FindMatches_ReturnsLiteralHits` - 字面量匹配
- ✅ `FindMatches_ProvidesCaptureGroups` - 捕获组
- ✅ `FindMatches_MultilineLiteralAcrossCrLf` - 跨行字面量
- ✅ `FindMatches_WholeWordHonorsSeparators` - 整词匹配
- ✅ `FindMatches_CustomSeparatorsSupportUnicode` - Unicode分隔符
- ✅ `FindMatches_ZeroLengthRegexOnAstralAdvances` - 零宽度正则
- ✅ `FindNextAndPrevious_WrapAroundDocument` - 环绕搜索
- ✅ `Regex_WordBoundaryHonorsEcmaDefinition` - 单词边界
- ✅ `Regex_DigitsRestrictToAscii` - ASCII数字限制
- ✅ `WholeWord_IgnoresUnicodeSpacesUnlessExplicit` - Unicode空格处理
- ✅ `Regex_EmojiQuantifiersConsumeCodePoints` - Emoji代码点

**缺失的测试用例:**
1. TS `#45892` - 空缓冲区搜索
2. TS `#45770` - 节点边界搜索

**修正建议:**
- 添加空缓冲区搜索边界测试
- 添加节点边界不跨越测试

---

### 7. PieceTreeSnapshotTests.cs
**TS源:** pieceTreeTextBuffer.test.ts (snapshot suite)
**对齐状态:** ❌需要修正

**分析:**
C# 实现仅有基本的快照测试：
- ✅ `SnapshotReadsContent` - 读取内容
- ✅ `SnapshotIsImmutable` - 不可变性

**缺失的测试用例:**
1. TS `bug #45564, piece tree pieces should be immutable` - Piece不可变性
2. TS `immutable snapshot 1-3` - 多种编辑后快照不可变性验证

**修正建议:**
- 添加 Piece 级别不可变性测试
- 添加多次编辑后快照内容验证测试
- 测试快照在复杂编辑序列后的正确性

---

### 8. TextModelTests.cs
**TS源:** textModel.test.ts
**对齐状态:** ⚠️存在偏差

**分析:**
C# 实现覆盖了丰富的 TextModel 功能：
- ✅ Selection逻辑测试
- ✅ 创建和编辑测试
- ✅ Decoration追踪测试
- ✅ Undo/Redo测试
- ✅ 选项更新测试
- ✅ 缩进检测测试
- ✅ EOL推送测试
- ✅ 语言切换测试
- ✅ 创建选项应用测试
- ✅ 语言配置事件测试
- ✅ 附加事件测试

**缺失的测试用例:**
1. TS `TextModelData.fromString` 套件 (one line text, multiline text, Non Basic ASCII, containsRTL)
2. TS `getValueLengthInRange` 测试
3. TS `getValueLengthInRange different EOL` 测试
4. TS `guess indentation` 完整矩阵（约30个测试用例）
5. TS `validatePosition` 测试
6. TS `validatePosition around high-low surrogate pairs` 测试
7. TS `validatePosition handle NaN/floats` 测试
8. 多个Issue回归测试 (#44991, #55818, #70832, #62143, #84217, #71480)

**修正建议:**
- 添加 `GetValueLengthInRange` 系列测试
- 添加完整的缩进检测矩阵测试
- 添加位置验证边界测试（包括代理对、NaN、浮点数）
- 添加Issue回归测试

---

### 9. TextModelSearchTests.cs
**TS源:** textModelSearch.test.ts
**对齐状态:** ✅完全对齐

**分析:**
C# 实现非常全面，包含多个测试类：
- ✅ `TextModelSearchTests_RangeScopes` - 范围作用域测试
- ✅ `TextModelSearchTests_WordBoundaries` - 单词边界矩阵
- ✅ `TextModelSearchTests_MultilineRegex` - 多行正则测试
- ✅ `TextModelSearchTests_CaptureNavigation` - 捕获组导航
- ✅ `TextModelSearchTests_ZeroWidthAndUnicode` - 零宽度和Unicode
- ✅ `TextModelSearchTests_ParseSearchRequest` - 搜索请求解析
- ✅ `TextModelSearchTests_IsMultilineRegexSource` - 多行正则源检测
- ✅ `TextModelSearchTests_FindNextMatchNavigation` - 导航测试

覆盖了TS中所有主要测试场景，包括Issue回归测试(#3623, #27459, #27594, #53415, #74715, #100134)

**缺失的测试用例:** 无显著缺失

**修正建议:** 无需修正

---

### 10. DecorationTests.cs
**TS源:** model.decorations.test.ts
**对齐状态:** ❌需要修正

**分析:**
C# 实现覆盖了Decoration核心功能：
- ✅ `DeltaDecorationsTrackOwnerScopes` - Owner作用域追踪
- ✅ `CollapseOnReplaceEditShrinksRange` - 替换时折叠
- ✅ `StickinessHonorsInsertions` - 粘性处理
- ✅ `DecorationOptionsParityRoundTripsMetadata` - 选项元数据往返
- ✅ `DecorationsChangedEventIncludesMetadata` - 变更事件元数据
- ✅ `GetLineDecorationsReturnsVisibleMetadata` - 行装饰可见性
- ✅ `GetAllDecorationsFiltersByOwner` - Owner过滤
- ✅ `DecorationIdsByOwnerReflectsLifecycle` - 生命周期追踪
- ✅ `DecorationsRaiseEventsForAddUpdateRemove` - 事件触发
- ✅ `EditsPropagateDecorationChangeEvents` - 编辑传播事件
- ✅ `InjectedTextQueriesSurfaceLineMetadata` - 注入文本查询
- ✅ `ForceMoveMarkersOverridesStickinessDefaults` - 强制移动标记

**缺失的测试用例:**
注意：TS源文件 `model.decorations.test.ts` 在仓库中不存在，无法进行完整对比。根据C#代码注释，这些测试是基于TS原版设计的。

**修正建议:**
- 确认TS源文件是否在其他位置或需要从上游获取
- 如有TS源，进行详细对比并补充缺失测试

---

### 11. DiffTests.cs
**TS源:** defaultLinesDiffComputer.test.ts
**对齐状态:** ❌需要修正

**分析:**
C# 实现覆盖了基本Diff功能：
- ✅ `WordDiffProducesInnerChanges` - 词级别内部变更
- ✅ `IgnoreTrimWhitespaceTreatsTrailingSpacesAsEqual` - 忽略尾部空白
- ✅ `MoveDetectionEmitsNestedMappings` - 移动检测嵌套映射
- ✅ `DiffRespectsTimeoutFlag` - 超时标记

**缺失的测试用例:**
注意：TS源文件 `defaultLinesDiffComputer.test.ts` 在仓库中不存在，无法进行完整对比。

**修正建议:**
- 确认TS源文件是否在其他位置或需要从上游获取
- 如有TS源，对比并补充：
  - 更多词级别diff边界情况
  - 大文件diff性能测试
  - 移动检测精确性测试

---

## 总体评估

### 优势
1. **搜索测试**：`TextModelSearchTests.cs` 和 `PieceTreeSearchTests.cs` 实现非常完整，覆盖了所有主要TS测试场景
2. **Builder测试**：`PieceTreeBuilderTests.cs` 和 `PieceTreeFactoryTests.cs` 完全对齐
3. **模型测试**：`TextModelTests.cs` 覆盖了丰富的功能测试

### 需要改进
1. **随机/模糊测试**：多个TS套件包含大量随机测试（random bug 1-10等），C#仅有部分覆盖
2. **边界情况**：CRLF处理、代理对、位置验证等边界测试需要补充
3. **缩进检测**：TS有完整的缩进检测矩阵（约30个测试），C#覆盖不足
4. **缺失TS源**：Decoration和Diff测试的TS源文件不在仓库中，无法完整验证

### 建议优先级

**高优先级:**
1. 补充 `PieceTreeSnapshotTests.cs` 的不可变性测试
2. 补充 `TextModelTests.cs` 的位置验证和缩进检测测试
3. 确认Decoration和Diff的TS源文件位置

**中优先级:**
1. 添加 `PieceTreeModelTests.cs` 中的 `prefix sum` 和 `buffer api` 测试
2. 添加 `PieceTreeNormalizationTests.cs` 中的CRLF随机bug测试

**低优先级:**
1. 添加 `PieceTreeBaseTests.cs` 中的随机操作测试
2. 添加Issue回归测试
