# Feature Tests 模块对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 7个功能测试文件

## 概要
- 完全对齐: 0/7
- 存在偏差: 2/7
- 需要修正: 5/7

## 详细分析

---

### 1. ColumnSelectionTests.cs
**TS源:** `ts/src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts`
**对齐状态:** ❌需要修正

**分析:**

TS原版`multicursor.test.ts`包含两个测试套件：
1. `Multicursor` - 基本多光标操作（3个测试）
2. `Multicursor selection` - 多光标选择功能（约15+个测试）

C#实现`ColumnSelectionTests.cs`仅包含3个测试：
- `VisibleColumn_RoundTrip_WithTabs` - 可见列往返测试
- `VisibleColumn_AcountsForInjectedText_BeforeAndAfter` - 注入文本可见列测试  
- `Cursor_ColumnSelection_Basic` - 基本列选择测试

**缺失的测试用例:**

| TS测试用例 | 状态 |
|-----------|------|
| `issue #26393: Multiple cursors + Word wrap` | ❌缺失 |
| `issue #2205: Multi-cursor pastes in reverse order` | ❌缺失 |
| `issue #1336: Insert cursor below on last line` | ❌缺失 |
| `issue #8817: Cursor position changes when cancelling multicursor` | ❌缺失 |
| `issue #5400: Select All Occurrences with regex` | ❌缺失 |
| `AddSelectionToNextFindMatchAction can work with multiline` | ❌缺失 |
| `issue #6661: touching ranges` | ❌缺失 |
| `issue #23541: Multiline Ctrl+D in CRLF files` | ❌缺失 |
| `AddSelectionToNextFindMatchAction starting with single collapsed selection` | ❌缺失 |
| `AddSelectionToNextFindMatchAction with two selections` | ❌缺失 |
| `AddSelectionToNextFindMatchAction with all collapsed selections` | ❌缺失 |
| `issue #20651: case insensitive` | ❌缺失 |
| `Find state disassociation tests` (多个) | ❌缺失 |

**修正建议:**
1. 补充TS版本中的`InsertCursorAbove`/`InsertCursorBelow`操作测试
2. 补充多光标粘贴顺序测试
3. 补充`AddSelectionToNextFindMatchAction`相关测试
4. 补充CRLF处理测试
5. 补充大小写不敏感搜索测试

---

### 2. CursorMultiSelectionTests.cs
**TS源:** `ts/src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts`
**对齐状态:** ⚠️存在偏差

**分析:**

C#实现包含2个测试：
- `MultiCursor_RendersMultipleCursorsAndSelections` - 多光标渲染
- `MultiCursor_EditAtMultipleCursors` - 多光标编辑

这些测试是原创的C#实现，验证了多光标基本功能，但与TS版本的测试用例不同。

**缺失的测试用例:**
- TS版本的多光标测试主要通过`InsertCursorAbove`/`InsertCursorBelow`操作实现
- 缺少粘贴操作的多光标行为测试
- 缺少光标取消后位置恢复测试

**修正建议:**
1. 保留现有测试（功能正确）
2. 补充与TS版本对应的边界条件测试
3. 添加多光标编辑后撤销行为测试

---

### 3. CursorTests.cs
**TS源:** `ts/src/vs/editor/test/common/controller/cursorAtomicMoveOperations.test.ts`
**对齐状态:** ❌需要修正

**分析:**

TS原版`cursorAtomicMoveOperations.test.ts`专注于**原子Tab移动操作**，包含：
1. `Test whitespaceVisibleColumn` - 空白可见列计算（8个测试用例数据集）
2. `Test atomicPosition` - 原子位置计算（6个测试用例数据集，3个方向）

C#实现`CursorTests.cs`测试内容完全不同：
- `TestCursor_InitialState` - 初始状态
- `TestCursor_MoveRight` - 右移
- `TestCursor_MoveLeft` - 左移
- `TestCursor_MoveDown` - 下移
- `TestCursor_MoveUp` - 上移
- `TestCursor_SelectTo` - 选择
- `TestCursor_StickyColumn` - 粘性列
- `CursorProducesDecorations` - 光标装饰

**严重偏差:** C#版本测试的是基本光标移动，而TS版本测试的是**原子Tab移动操作**（`AtomicTabMoveOperations`）。

**缺失的测试用例:**

| TS测试用例 | 描述 | 状态 |
|-----------|------|------|
| `whitespaceVisibleColumn` | 8种空白+Tab组合的可见列计算 | ❌完全缺失 |
| `atomicPosition` (Left) | 6种情况的左移原子位置 | ❌完全缺失 |
| `atomicPosition` (Right) | 6种情况的右移原子位置 | ❌完全缺失 |
| `atomicPosition` (Nearest) | 6种情况的最近原子位置 | ❌完全缺失 |

**修正建议:**
1. 实现`AtomicTabMoveOperations`类
2. 添加`whitespaceVisibleColumn`测试用例
3. 添加`atomicPosition`方向性测试
4. 现有的光标移动测试可保留，但应归入不同的测试文件

---

### 4. CursorWordOperationsTests.cs
**TS源:** `ts/src/vs/editor/contrib/wordOperations/test/browser/wordOperations.test.ts`
**对齐状态:** ❌需要修正

**分析:**

TS原版`wordOperations.test.ts`是一个非常全面的测试套件，包含约60+个测试用例，覆盖：
- `cursorWordLeft` - 左移词（多种变体：StartLeft, EndLeft）
- `cursorWordRight` - 右移词（多种变体：StartRight, EndRight）
- `cursorWordAccessibilityLeft/Right` - 无障碍词移动
- `deleteWordLeft/Right` - 删除词（多种变体）
- `deleteInsideWord` - 删除词内部
- 多种语言支持（日语分词等）
- 大量边界条件和issue修复

C#实现仅包含3个测试：
- `MoveWordRight_BasicWords` - 基本右移词
- `MoveWordLeft_BasicWords` - 基本左移词
- `DeleteWordLeft_Basic` - 基本删除左词

**覆盖率估算:** 约5%

**缺失的关键测试用例:**

| TS测试用例 | 状态 |
|-----------|------|
| `cursorWordLeft - simple` | ⚠️部分覆盖 |
| `cursorWordLeft - with selection` | ❌缺失 |
| `cursorWordLeft - issue #832` | ❌缺失 |
| `cursorWordLeft - issue #48046` | ❌缺失 |
| `cursorWordLeft - Recognize words (日语)` | ❌缺失 |
| `cursorWordLeft - issue #169904` | ❌缺失 |
| `cursorWordStartLeft` | ❌缺失 |
| `cursorWordEndLeft` | ❌缺失 |
| `cursorWordRight - simple` | ⚠️部分覆盖 |
| `cursorWordRight - selection` | ❌缺失 |
| `cursorWordRight - issue #832` | ❌缺失 |
| `cursorWordRight - issue #41199` | ❌缺失 |
| `moveWordEndRight` | ❌缺失 |
| `moveWordStartRight` | ❌缺失 |
| `cursorWordAccessibilityLeft` | ❌缺失 |
| `cursorWordAccessibilityRight` | ❌缺失 |
| `deleteWordLeft for non-empty selection` | ❌缺失 |
| `deleteWordLeft for cursor at beginning` | ❌缺失 |
| `deleteWordLeft for cursor at end of whitespace` | ❌缺失 |
| `deleteWordRight` 系列 | ❌缺失 |
| `deleteWordStartLeft/Right` | ❌缺失 |
| `deleteWordEndLeft/Right` | ❌缺失 |
| `deleteInsideWord` 系列（6个测试） | ❌缺失 |

**修正建议:**
1. 实现`cursorWordStartLeft`/`cursorWordEndLeft`变体
2. 添加选择模式的词移动测试
3. 添加所有`deleteWord`变体的测试
4. 添加`deleteInsideWord`测试
5. 考虑添加多语言分词测试

---

### 5. SnippetControllerTests.cs
**TS源:** `snippetController2.test.ts` + `snippetSession.test.ts`
**对齐状态:** ❌需要修正

**分析:**

TS原版包含两个测试文件，共计约60+个测试用例：

**snippetController2.test.ts:**
- 创建、插入、取消
- Tab导航
- 光标移动检测
- 嵌套snippet
- 占位符顺序
- Transform
- 多光标支持
- EOL处理
- 大量Issue修复

**snippetSession.test.ts:**
- 空白规范化
- 选择调整
- 文本编辑与选择
- 重复tabstop
- 嵌套session
- Transform示例
- Tab处理

C#实现仅包含1个测试：
- `SnippetInsert_CreatesPlaceholders_AndNavigates`

**覆盖率估算:** 约1.5%

**缺失的关键测试用例:**

| TS测试用例 | 状态 |
|-----------|------|
| `creation` | ⚠️部分覆盖 |
| `insert, insert -> abort` | ❌缺失 |
| `insert, insert -> tab, tab, done` | ❌缺失 |
| `insert, insert -> cursor moves out (left/right)` | ❌缺失 |
| `insert, insert -> cursor moves out (up/down)` | ❌缺失 |
| `insert, insert -> cursors collapse` | ❌缺失 |
| `insert, insert plain text -> no snippet mode` | ❌缺失 |
| `insert, delete snippet text` | ❌缺失 |
| `insert, nested trivial snippet` | ❌缺失 |
| `insert, nested snippet` | ❌缺失 |
| `issue #27898: Nested snippets final placeholder` | ❌缺失 |
| `issue #27543: Recursive snippets tab behavior` | ❌缺失 |
| `issue #23728: Tabstop selecting variable content` | ❌缺失 |
| `issue #32211: HTML Snippets Combine` | ❌缺失 |
| `Placeholders order #58267` | ❌缺失 |
| `Must tab through deleted tab stops #31619` | ❌缺失 |
| `Cancelling snippet mode #68512` | ❌缺失 |
| `snippet with variables` | ❌缺失 |
| `snippets, transform` | ❌缺失 |
| `SnippetSession.adjustWhitespace` | ❌缺失 |
| `SnippetSession.adjustSelection` | ❌缺失 |
| `createEditsAndSnippetsFromEdits` | ❌缺失 |

**修正建议:**
1. 实现完整的SnippetController和SnippetSession测试
2. 添加嵌套snippet支持
3. 添加Transform功能测试
4. 添加多光标snippet测试
5. 添加空白规范化测试

---

### 6. UnitTest1.cs (PieceTreeBufferTests)
**TS源:** `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`
**对齐状态:** ⚠️存在偏差

**分析:**

TS原版是一个非常全面的测试套件（2126行），包含：
- `inserts and deletes` - 基本插入删除（约15个测试）
- `prefix sum for line feed` - 行偏移计算（约20个测试）
- `offset 2 position` - 偏移到位置转换
- `get text in range` - 范围文本获取（约10个测试）
- `CRLF` - CRLF处理（约10个测试）
- `centralized lineStarts with CRLF` - CRLF行起始（约10个测试）
- `random is unsupervised` - 随机测试
- `buffer api` - 缓冲区API（equal, getLineCharCode等）
- `search offset cache` - 搜索缓存
- `snapshot` - 快照不可变性
- `chunk based search` - 分块搜索

C#实现包含约15个测试，覆盖核心功能：
- `InitializesWithProvidedText` ✅
- `LargeBufferRoundTripsContent` ✅
- `AppliesSimpleEdit` ✅
- `FromChunksBuildsPieceTreeAcrossMultipleBuffers` ✅
- `PieceTreeModelTracksLineFeedsAcrossChunks` ✅
- `ApplyEditHandlesCrLfSequences` ✅
- `ApplyEditAcrossChunkBoundarySpansMultiplePieces` ✅
- `PositionLookupMatchesTsPrefixSumExpectations` ✅
- `LineCharCodeFollowsCrlfBoundaries` ✅
- `CharCodeClampedWithinDocument` ✅
- `DeleteAcrossCrlfRepairsBoundary` ✅
- `MetadataRecomputesAfterMultiLineDelete` ✅
- `PieceCountTracksTreeMutations` ✅
- `SearchCacheDropsDetachedNodes` ✅

**覆盖率估算:** 约15-20%

**缺失的关键测试用例:**

| TS测试套件 | 状态 |
|-----------|------|
| `basic insert/delete` | ✅覆盖 |
| `more inserts/deletes` | ⚠️部分覆盖 |
| `random test 1-3` | ❌缺失 |
| `random delete 1-3` | ❌缺失 |
| `random insert/delete \r bug 1-5` | ❌缺失 |
| `prefix sum basic/append/insert/delete` | ⚠️部分覆盖 |
| `insert random bug 1-2` | ❌缺失 |
| `delete random bug rb tree 1-3` | ❌缺失 |
| `getContentInRange` | ❌缺失 |
| `random test value in range` | ❌缺失 |
| `get line content` 系列 | ⚠️部分覆盖 |
| `CRLF delete CR 1-2` | ⚠️部分覆盖 |
| `random bug 1-10 (CRLF)` | ❌缺失 |
| `buffer api equal` | ❌缺失 |
| `getLineCharCode issue #45735/#47733` | ❌缺失 |
| `getNearestChunk` | ❌缺失 |
| `search offset cache` | ❌缺失 |
| `snapshot immutable 1-3` | ❌缺失 |
| `chunk based search` | ❌缺失 |

**修正建议:**
1. 添加随机化压力测试
2. 添加红黑树不变量验证
3. 添加完整的CRLF边界条件测试
4. 添加缓冲区等价性测试
5. 添加快照不可变性测试
6. 添加分块搜索测试

---

### 7. PieceTreeTestHelpers.cs
**TS源:** `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`
**对齐状态:** ❌需要修正

**分析:**

这是一个辅助类，包含`ReconstructText`方法。

TS原版中有多个辅助函数：
- `randomChar()`
- `randomInt(bound)`
- `randomStr(len)`
- `trimLineFeed(text)`
- `testLinesContent(str, pieceTable)`
- `testLineStarts(str, pieceTable)`
- `createTextBuffer(val, normalizeEOL)`
- `assertTreeInvariants(T)`
- `depth(n)`
- `assertValidNode(n)`
- `assertValidTree(T)`
- `getValueInSnapshot(snapshot)`

C#实现仅有：
- `ReconstructText(result)` - 基本文本重建

**缺失的辅助函数:**

| TS辅助函数 | 状态 | 用途 |
|-----------|------|------|
| `randomChar/randomInt/randomStr` | ❌缺失 | 随机测试数据生成 |
| `trimLineFeed` | ❌缺失 | 行尾处理 |
| `testLinesContent` | ❌缺失 | 行内容验证 |
| `testLineStarts` | ❌缺失 | 行起始位置验证 |
| `assertTreeInvariants` | ❌缺失 | 红黑树不变量验证 |
| `depth` | ❌缺失 | 树深度计算 |
| `assertValidNode` | ❌缺失 | 节点有效性验证 |
| `assertValidTree` | ❌缺失 | 树有效性验证 |
| `getValueInSnapshot` | ❌缺失 | 快照值获取 |

**修正建议:**
1. 添加随机测试数据生成函数
2. 实现红黑树不变量验证函数
3. 添加行内容/行起始位置验证辅助函数
4. 添加快照相关辅助函数

---

## 总结

### 严重程度排序

1. **CursorWordOperationsTests.cs** - 覆盖率约5%，缺失大量核心功能测试
2. **SnippetControllerTests.cs** - 覆盖率约1.5%，仅有1个基础测试
3. **CursorTests.cs** - 测试目标完全不同于TS版本
4. **ColumnSelectionTests.cs** - 缺失大部分多光标测试
5. **UnitTest1.cs** - 核心功能有覆盖，但缺失边界条件和随机测试
6. **PieceTreeTestHelpers.cs** - 缺失大部分辅助验证函数
7. **CursorMultiSelectionTests.cs** - 功能正确但测试用例不对应

### 优先修复建议

1. **高优先级:**
   - 补充`AtomicTabMoveOperations`测试
   - 补充`WordOperations`完整测试套件
   - 补充`SnippetController`核心功能测试

2. **中优先级:**
   - 补充PieceTree随机化测试和不变量验证
   - 补充多光标选择测试
   - 补充CRLF边界条件测试

3. **低优先级:**
   - 添加测试辅助函数
   - 添加快照不可变性测试
   - 添加分块搜索测试

### 代码质量评估

| 指标 | 评分 | 说明 |
|------|------|------|
| 功能覆盖 | 3/10 | 大部分TS测试用例未移植 |
| API对齐 | 5/10 | 核心API有覆盖，但变体缺失 |
| 边界条件 | 2/10 | 几乎无边界条件测试 |
| 随机测试 | 1/10 | 无随机化压力测试 |
| 回归测试 | 2/10 | TS中的issue修复测试几乎全部缺失 |
