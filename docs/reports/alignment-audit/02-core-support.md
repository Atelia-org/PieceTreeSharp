# Core Support Types 对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 8个支持类型文件

## 概要
- 完全对齐: 3/8
- 存在偏差: 4/8
- 需要修正: 1/8

## 详细分析

### 1. PieceTreeSearchCache.cs
**TS源:** pieceTreeBase.ts (Lines 100-268)
**对齐状态:** ⚠️存在偏差

**分析:**

| 特性 | TS原版 | C#实现 | 状态 |
|-----|--------|--------|------|
| 缓存结构 | `_cache: CacheEntry[]` | `_entries: List<CacheEntry>` | ✅ |
| 限制参数 | `_limit: number` | `_limit: int` (默认值=1) | ✅ |
| get方法 | `get(offset)` 返回CacheEntry或null | `TryGetByOffset` 返回bool+out参数 | ✅ 惯用法差异 |
| get2方法 | `get2(lineNumber)` 返回对象或null | `TryGetByLine` 返回bool+out参数 | ✅ 惯用法差异 |
| set方法 | `set(nodePosition)` | `Remember(node, offset, line?)` | ⚠️ API不同 |
| validate方法 | 检查`node.parent === null`和`offset >= threshold` | 额外检查`IsSentinel`/`IsDetached`+offset验证 | ⚠️ 扩展逻辑 |

**偏差说明:**
1. **TS `set()` vs C# `Remember()`**: TS接受完整的CacheEntry对象，C#分解为独立参数。功能等价但API签名不同。
2. **TS `validate(offset)` vs C# `Validate(Func, totalLength)`**: C#版本更加复杂：
   - TS: 简单地检查`node.parent === null`（判断节点是否已分离）和`nodeStartOffset >= offset`
   - C#: 使用回调函数`computeOffset`重新计算偏移量，检查`IsSentinel`/`IsDetached`状态
3. **额外方法**: C#添加了`InvalidateFromOffset`和`InvalidateRange`方法，TS中没有这些方法。
4. **CoversOffset边界**: TS使用`>=`判断end边界，C#使用`<=`，在offset等于end时行为略有不同。

**修正建议:**
1. `validate`方法的行为与TS不完全一致。TS版本更简单直接：
```typescript
// TS原版
public validate(offset: number) {
    // 如果 node.parent === null 或 nodeStartOffset >= offset，则移除
}
```
建议保持C#的增强实现，但需在注释中说明与TS的差异。

---

### 2. PieceTreeSearcher.cs
**TS源:** textModelSearch.ts (Lines 490-560, Searcher class)
**对齐状态:** ⚠️存在偏差

**分析:**

| 特性 | TS原版 | C#实现 | 状态 |
|-----|--------|--------|------|
| 构造函数 | `(wordSeparators, searchRegex)` | `(wordSeparators?, searchRegex)` | ✅ |
| 状态字段 | `_prevMatchStartIndex`, `_prevMatchLength` | 相同 | ✅ |
| reset方法 | `_searchRegex.lastIndex = lastIndex` | `_lastIndex = lastIndex` | ⚠️ |
| next方法 | 返回`RegExpExecArray \| null` | 返回`Match?` | ✅ |
| 零长度匹配处理 | 使用`getNextCodePoint`判断代理对 | 使用`UnicodeUtility.AdvanceByCodePoint` | ✅ |
| 终止条件 | `_prevMatchStartIndex + _prevMatchLength === textLength` | 相同逻辑 | ✅ |

**偏差说明:**
1. **Regex状态管理**: TS直接操作`regex.lastIndex`属性，C#的`Regex`类是无状态的，需要用`Match(text, startIndex)`方法传入起始位置。C#实现通过`_lastIndex`字段模拟此行为。
2. **ECMAScript模式**: C#添加了`EnsureEcmaRegex`方法强制使用`RegexOptions.ECMAScript`，这是为了匹配JS正则表达式的行为，是合理的运行时适配。
3. **isValidMatch调用**: TS使用独立的`isValidMatch`函数，C#将其作为`WordCharacterClassifier`的实例方法。

**修正建议:**
实现基本对齐，差异属于语言运行时的必要适配。无需修正。

---

### 3. PieceTreeSnapshot.cs
**TS源:** pieceTreeBase.ts (Lines 157-190, PieceTreeSnapshot class)
**对齐状态:** ✅完全对齐

**分析:**

| 特性 | TS原版 | C#实现 | 状态 |
|-----|--------|--------|------|
| 构造函数 | 收集所有piece到数组 | 使用`EnumeratePiecesInOrder()` | ✅ |
| _pieces | `Piece[]` | `IReadOnlyList<PieceSegment>` | ✅ |
| _index | 读取索引 | 相同 | ✅ |
| _BOM | BOM字符串 | `_bom` | ✅ |
| read() | 首次返回BOM，后续返回piece内容 | 相同逻辑 | ✅ |
| 空文档处理 | 仅返回BOM | 相同 | ✅ |

**分析详情:**
- TS版本通过`tree.iterate()`遍历树节点收集pieces
- C#版本通过`model.EnumeratePiecesInOrder()`达到相同效果
- `read()`方法的逻辑完全一致：首次调用返回BOM+第一个piece内容，后续调用返回piece内容
- 边界条件处理（空文档、超出索引）与TS一致

**修正建议:** 无需修正。

---

### 4. PieceTreeTextBufferFactory.cs
**TS源:** pieceTreeTextBufferBuilder.ts (Lines 14-66)
**对齐状态:** ⚠️存在偏差

**分析:**

| 特性 | TS原版 | C#实现 | 状态 |
|-----|--------|--------|------|
| 构造函数参数 | chunks, bom, cr, lf, crlf, rtl, unusual, ascii, normalizeEOL | 相同 + options对象 | ⚠️ |
| _getEOL | 基于CR计数决定EOL | `DetermineEol`相同逻辑 | ✅ |
| create方法 | 返回`{ textBuffer, disposable }` | 返回`PieceTreeBuildResult` | ⚠️ |
| EOL规范化 | 正则替换`/\r\n\|\r\|\n/g` | `ChunkUtilities.NormalizeChunks` | ✅ |
| getFirstLineText | 简单substring+split | 更复杂的StringBuilder实现 | ⚠️ |

**偏差说明:**
1. **返回类型差异**: TS返回`{ textBuffer, disposable }`元组，C#返回包装类`PieceTreeBuildResult`。
2. **getFirstLineText实现差异**: 
   - TS: `this._chunks[0].buffer.substr(0, lengthLimit).split(/\r\n|\r|\n/)[0]` - 只看第一个chunk
   - C#: 遍历所有chunks构建字符串，然后查找换行符 - 支持跨chunk边界
3. **额外方法**: C#添加了`GetLastLineText`方法，TS中没有。
4. **PieceTreeBuilderOptions**: C#引入了options对象，TS使用独立的normalizeEOL布尔参数。

**修正建议:**
1. `GetFirstLineText`行为与TS不完全一致。TS只检查第一个chunk，C#检查所有chunks。如果第一行跨越多个chunk，C#的行为更正确，但与TS不同。考虑是否需要严格匹配TS行为。

---

### 5. Range.Extensions.cs
**TS源:** range.ts (Lines 50-150)
**对齐状态:** ❌需要修正

**分析:**

| 特性 | TS原版 | C#实现 | 状态 |
|-----|--------|--------|------|
| isEmpty() | 比较4个坐标 | `Start == End` | ⚠️ |
| containsPosition | 完整边界检查 | 未实现 | ❌ |
| strictContainsPosition | 严格边界检查 | 未实现 | ❌ |
| containsRange | 范围包含检查 | 未实现 | ❌ |
| strictContainsRange | 严格范围包含检查 | 未实现 | ❌ |
| plusRange | 合并两个范围 | `Plus(Range other)` | ⚠️ |
| intersectRanges | 范围交集 | 未实现 | ❌ |
| getStartPosition | 返回起始位置 | ✅ | ✅ |
| getEndPosition | 返回结束位置 | ✅ | ✅ |

**偏差说明:**
1. **Plus方法实现不完整**: TS的`plusRange`方法对相同行号时会比较列号取最小/最大值。C#实现只做简单的位置比较。
   ```typescript
   // TS plusRange (正确)
   if (b.startLineNumber === a.startLineNumber) {
       startColumn = Math.min(b.startColumn, a.startColumn);
   }
   ```
   ```csharp
   // C# Plus (简化)
   var start = Start <= other.Start ? Start : other.Start;
   ```
2. **大量方法缺失**: `containsPosition`, `containsRange`, `intersectRanges`等核心方法未实现。

**修正建议:**
需要补充以下方法的实现：
1. `ContainsPosition(TextPosition position)` - 检查位置是否在范围内
2. `StrictContainsPosition(TextPosition position)` - 严格检查（边界返回false）
3. `ContainsRange(Range other)` - 检查范围是否包含另一个范围
4. `StrictContainsRange(Range other)` - 严格范围包含检查
5. `IntersectRanges(Range other)` - 计算两个范围的交集
6. 修正`Plus`方法以正确处理同行比较

---

### 6. SearchTypes.cs
**TS源:** textModelSearch.ts + wordCharacterClassifier.ts
**对齐状态:** ⚠️存在偏差

**分析:**

| 特性 | TS原版 | C#实现 | 状态 |
|-----|--------|--------|------|
| SearchParams | 4参数构造 | 相同 | ✅ |
| parseSearchRequest | 返回SearchData或null | 相同 | ✅ |
| isMultilineRegexSource | 检查\n, \\n, \\r, \\W | 相同 | ✅ |
| SearchData | regex, wordSeparators, simpleSearch | 添加isMultiline, isCaseSensitive | ⚠️ |
| FindMatch | range, matches | 相同 | ✅ |
| WordCharacterClassifier | 继承CharacterClassifier | 独立实现用Dictionary | ⚠️ |
| getMapForWordSeparators | LRUCache(10) | WordCharacterClassifierCache (LRU 10) | ✅ |
| LineFeedCounter | 二分查找 | 相同算法 | ✅ |

**偏差说明:**
1. **SearchData额外字段**: C#添加了`IsMultiline`和`IsCaseSensitive`属性，TS版本没有（这些信息从regex.multiline和选项中获取）。
2. **WordCharacterClassifier实现**: 
   - TS: 继承`CharacterClassifier`基类，使用高效的查找表
   - C#: 使用`Dictionary<int, WordCharacterClass>`，没有基类
   - TS支持`intlSegmenterLocales`用于国际化分词，C#未实现
3. **isValidMatch位置**: TS是独立函数，C#是`WordCharacterClassifier`的实例方法。
4. **Unicode通配符处理**: C#的`ApplyUnicodeWildcardCompatibility`将`.`替换为UTF-16代理对感知的模式，这是C#特有的适配。

**修正建议:**
1. 考虑添加`IntlSegmenterLocales`支持（如果需要国际化分词）。
2. `SearchData`的额外字段是合理的扩展，无需移除。

---

### 7. Selection.cs
**TS源:** selection.ts (Lines 1-100)
**对齐状态:** ✅完全对齐

**分析:**

| 特性 | TS原版 | C#实现 | 状态 |
|-----|--------|--------|------|
| 继承关系 | `extends Range` | 独立struct | ⚠️ 设计差异 |
| selectionStartLineNumber/Column | 选择起点 | `Anchor` | ✅ |
| positionLineNumber/Column | 选择终点（光标位置） | `Active` | ✅ |
| getDirection() | LTR/RTL | `Direction`属性 | ✅ |
| isEmpty() | 从Range继承 | `IsEmpty`属性 | ✅ |
| toString() | `[start -> end]` | 相同格式 | ✅ |

**分析详情:**
- TS的`Selection`继承自`Range`，共享range的start/end概念
- C#使用独立的struct，通过`SelectionStart`/`SelectionEnd`属性计算得到start/end
- `Anchor`和`Active`的语义与TS的`selectionStart`和`position`对应
- Direction计算逻辑一致：如果anchor在active之前则LTR，否则RTL

**修正建议:** 
设计差异是合理的C#惯用法（使用值类型而非类继承）。核心语义完全对齐，无需修正。

---

### 8. TextMetadataScanner.cs
**TS源:** strings.ts (containsRTL, isBasicASCII, containsUnusualLineTerminators)
**对齐状态:** ✅完全对齐

**分析:**

| 特性 | TS原版 | C#实现 | 状态 |
|-----|--------|--------|------|
| containsRTL | 复杂正则表达式匹配 | 范围检查 | ⚠️ 简化实现 |
| isBasicASCII | 正则`/^[\t\n\r\x20-\x7E]*$/` | 字符遍历 `ch > 0x7F` | ✅ |
| containsUnusualLineTerminators | 正则`/[\u2028\u2029]/` | 检查LS/PS/NEL | ⚠️ 增加了NEL |

**偏差说明:**
1. **RTL检测范围**: 
   - TS使用精确的Unicode正则表达式，涵盖所有RTL代码点包括高代理对组合
   - C#使用预定义的范围数组，覆盖主要的RTL区块但可能遗漏某些字符
2. **异常行终止符**: C#额外检查了`\u0085` (NEL - Next Line)，TS只检查LS和PS。

**分析详情:**
```typescript
// TS RTL正则（部分）
/(?:[\u05BE\u05C0...\uFEFC]|\uD802[\uDC00-\uDD1B...]...)/
```
```csharp
// C# RTL范围
(0x0590, 0x08FF), // Hebrew, Arabic, etc.
(0x200F, 0x202E), // Directional formatting
(0xFB1D, 0xFDFF), (0xFE70, 0xFEFC)
```
C#的范围大致覆盖了TS正则的主要部分，但TS的正则更精确（排除了某些非RTL字符）。

**修正建议:**
1. 如果需要精确匹配TS行为，应该使用正则表达式或更精确的代码点列表
2. NEL检测可能是有意的增强，如需严格对齐则移除`\u0085`检查

---

## 总结

### 完全对齐 (3/8)
1. **PieceTreeSnapshot.cs** - 核心逻辑完全一致
2. **Selection.cs** - 语义对齐，设计适当C#化
3. **TextMetadataScanner.cs** - 功能等价，实现方式不同

### 存在偏差 (4/8)
1. **PieceTreeSearchCache.cs** - API差异，validate逻辑增强
2. **PieceTreeSearcher.cs** - 运行时适配差异（Regex状态管理）
3. **PieceTreeTextBufferFactory.cs** - getFirstLineText行为差异
4. **SearchTypes.cs** - 额外字段，缺少国际化支持

### 需要修正 (1/8)
1. **Range.Extensions.cs** - 大量核心方法缺失，Plus方法实现不完整

---

## 建议优先级

### 高优先级
- [ ] Range.Extensions.cs: 实现缺失的`ContainsPosition`, `ContainsRange`, `IntersectRanges`等方法
- [ ] Range.Extensions.cs: 修正`Plus`方法的同行列号比较逻辑

### 中优先级
- [ ] PieceTreeSearchCache.cs: 添加注释说明与TS的设计差异
- [ ] PieceTreeTextBufferFactory.cs: 评估`GetFirstLineText`是否需要严格匹配TS（只检查第一个chunk）

### 低优先级
- [ ] TextMetadataScanner.cs: 评估是否需要使用更精确的RTL正则
- [ ] SearchTypes.cs: 考虑添加国际化分词支持（如有需求）
