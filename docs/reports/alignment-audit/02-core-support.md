# Core Support Types 对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 8个支持类型文件

## 概要
- 完全对齐: 1/8
- 存在偏差: 5/8
- 需要修正: 2/8

## 详细分析

### 1. PieceTreeSearchCache.cs
**TS源:** `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` (L207-263)  
**对齐状态:** ⚠️存在偏差

**关键比较**

| 特性 | TS原版 | C#实现 | 状态 |
|-----|--------|--------|------|
| 缓存上限 | 构造函数参数，调用方必须传入 | 构造函数提供 `limit = 1` 默认值 | ⚠️ 默认差异 |
| 查找 API | `get(offset)` / `get2(line)` 返回 `CacheEntry` | `TryGetByOffset` / `TryGetByLine`（`bool + out`） | ✅ 惯用差异 |
| set 行为 | `set(nodePosition)` 直接推入 `CacheEntry` | `Remember(node, offset, line?)` 拆分参数 | ⚠️ API差异 |
| validate | `validate(offset)` 仅检查 `node.parent === null` 或 `nodeStartOffset >= offset` | `Validate(Func,node)` 重新计算偏移，过滤 `IsSentinel/IsDetached`，同时比较 `totalLength` | ⚠️ 逻辑不同 |
| 失效 API | 只有 `set` 时 `shift` | 额外提供 `InvalidateFromOffset/InvalidateRange` | ⚠️ 扩展 |

**偏差说明**
1. 默认 `limit` 与 TS 行为一致（当前 VS Code 仍传 1），但如果后续 TS 端调高缓存上限，C# 默认值需要同步调整，否则容易出现缓存与 TS 行为不符。
2. `Validate` 的语义更严格：TS 仅确认节点仍在树上且未越过 `offset`（L235-257），C# 会再次计算真实偏移并在不匹配或超出 `totalLength` 时剔除。虽然更安全，但会在某些边界情况下过早清缓存。
3. 新增的 `InvalidateRange` 能力在 TS 中不存在，调用方若依赖它需要额外文档说明，避免误以为 JS 端也有相同方法。

---

### 2. PieceTreeSearcher.cs
**TS源:** `ts/src/vs/editor/common/model/textModelSearch.ts` (L494-560)  
**对齐状态:** ⚠️存在偏差

| 特性 | TS原版 | C#实现 | 状态 |
|-----|--------|--------|------|
| 正则状态 | 依赖 `regex.lastIndex` | `_lastIndex` 字段 + `Regex.Match(text,start)` | ⚠️ |
| 正则选项 | `strings.createRegExp(...,{ unicode:true, global:true })` | `RegexOptions.ECMAScript`，无 `unicode` 概念 | ⚠️ |
| 零长度匹配 | `getNextCodePoint` | `UnicodeUtility.AdvanceByCodePoint` | ✅ |
| 结果过滤 | `isValidMatch(...)` | `WordCharacterClassifier.IsValidMatch` | ✅ |

**偏差说明**
- C# 无法复用 JS 的 `unicode`/`global` 标志，现有实现通过 `EnsureEcmaRegex`（L17-L40）强制 ECMAScript 语义，但在代理对分界处仍可能与 TS 不同。
- `_lastIndex` 未在失败后重置，与 TS 逻辑相同，但需要调用方记得在下一次搜索前 `Reset()`；建议在注释中强调。

---

### 3. PieceTreeSnapshot.cs
**TS源:** `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` (L58-149)  
**对齐状态:** ✅完全对齐

- `_pieces` 的生成方式（TS: `tree.iterate`; C#: `EnumeratePiecesInOrder`）等价。
- `read()` 的 BOM 处理、空文档返回顺序、越界条件与 TS 完全相同。

---

### 4. PieceTreeTextBufferFactory.cs
**TS源:** `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBufferBuilder.ts` (L13-108)  
**对齐状态:** ⚠️存在偏差

| 特性 | TS原版 | C#实现 | 状态 |
|-----|--------|--------|------|
| `create()` 返回值 | `{ textBuffer, disposable }` | `PieceTreeBuildResult`（封装更多元数据） | ⚠️ |
| EOL 规范化 | 就地 `replace(/\r\n|\r|\n/g, eol)` | `ChunkUtilities.NormalizeChunks` 生成新 `ChunkBuffer` | ✅ |
| `getFirstLineText` | 仅读取第一个 chunk 并 `split(/\r\n|\r|\n/)` (TS L59-61) | 将多个 chunk 拼接再寻找换行 | ⚠️ |
| 额外 API | 无 | `GetLastLineText`, `PieceTreeBuilderOptions` | ⚠️ |

**偏差说明**
1. 预览逻辑：TS 首行预览只依赖 `_chunks[0]`，C# 允许首行跨多个 chunk。这改变了前端展示的一致性，应在 API 文档中说明或加开关。
2. 结果类型不同，若上层期望 `{ textBuffer, disposable }` 结构，需要再包装，避免破坏既有接口。
3. `PieceTreeBuilderOptions.NormalizeEol` 默认为 true，与 TS `finish(normalizeEOL = true)` 相同，但调用者若想跳过，需要显式设置，文档中需强调。

---

### 5. Range.Extensions.cs
**TS源:** `ts/src/vs/editor/common/core/range.ts` (L52-195)  
**对齐状态:** ❌需要修正

| API | TS行为 | C#现状 |
|-----|--------|--------|
| `containsPosition` / `strictContainsPosition` | 包含/严格包含某个 `IPosition` | 未实现 |
| `containsRange` / `strictContainsRange` | 范围包含与严格包含 | 未实现 |
| `intersectRanges` | 返回交集或 `null` | 未实现 |
| `plusRange` | 先比较行号，再比较列号（TS L179-213） | 仅比较 `TextPosition`，忽略同行细节 |
| 静态 helper | `Range.isEmpty(range)` 等 | C# 只保留实例属性 `IsEmpty` |

**影响**
- 大量下游逻辑（Selection、Decorations、Diff）依赖这些 helper；没有它们无法 1:1 复用 VS Code 算法。
- 现有 `Plus` 在同行范围会产生错误的 `startColumn/endColumn`。

**建议**
- 补齐所有静态/实例方法，并为 `Plus/IntersectRanges` 编写单元测试，验证同行/跨行边界。
- 统一 `TextPosition` 比较方式，避免在 Range 外部重复编写 `<=`/`>=` 判断。

---

### 6. SearchTypes.cs
**TS源:** `ts/src/vs/editor/common/model/textModelSearch.ts` (L1-166) + `ts/src/vs/editor/common/core/wordCharacterClassifier.ts` (L1-120) + `ts/src/vs/editor/common/model.ts` (L1535-1570)  
**对齐状态:** ⚠️存在偏差

**关键差异**
1. **SearchData 结构**：TS 仅持有 `regex`, `wordSeparators`, `simpleSearch`；C# 额外公开 `IsMultiline`, `IsCaseSensitive`（L46-59）。这些扩展对 C# 有用，但 TS 端没有，需要在文档注明。
2. **正则构建**：TS 使用 `strings.createRegExp`，自动开启 `unicode/global` 并遵守 ECMAScript 语义；C# 依赖 `Regex` + `ApplyUnicodeWildcardCompatibility` 替换 `.`。仍可能在带代理对的 `\b`、`\w` 等场景出现差距。
3. **WordCharacterClassifier 能力**：TS 继承 `CharacterClassifier` 且支持 `Intl.Segmenter` 缓存（L11-62）；C# 只使用 `Dictionary<int, WordCharacterClass>`，缺少国际化分词辅助，导致“单词边界”判断有限。
4. **Boundary 判定**：TS 的 `isValidMatch` 可结合 `Intl` 结果与 `wordSeparators`；C# 仅依据字符类别与换行符判断，遇到 emoji/复杂脚本会与 TS 不一致。

**建议**
- 将 `SearchData` 扩展字段标注为 C# 专属，并确认调用方不会把它们同步回 TypeScript。
- 视需求评估 `Intl.Segmenter` 支持；或至少在 `WordCharacterClassifier` 注释中注明“不支持 locale-aware 的单词边界”。

---

### 7. Selection.cs
**TS源:** `ts/src/vs/editor/common/core/selection.ts` (L1-200)  
**对齐状态:** ❌需要修正

- TS `Selection` 继承 `Range` 并额外暴露 `selectionStartLineNumber/Column`, `positionLineNumber/Column`；C# 只保留 `Anchor/Active`，没有 Range 关系。
- TS 提供 `getSelectionStart`, `getPosition`, `setStartPosition`, `setEndPosition`, `fromPositions`, `fromRange`, `liftSelection`, `selectionsEqual`, `selectionsArrEqual`, `isISelection` 等大量 helper；C# 仅实现 `Contains`, `CollapseToStart/End`, `ToString`。
- 由于缺少这些工厂和比较方法，无法直接复用 VS Code 的 Selection 逻辑（例如多光标复制、撤销记录等）。

**建议**
- 补齐核心 API，并考虑让 `Selection` 提供 `GetDirection()`、`GetSelectionStart()` 等与 TS 同名的方法，或实现一个共享接口，降低迁移成本。
- 评估是否需要让 `Selection` 组合 `Range`，以便与 TS 模型交互。

---

### 8. TextMetadataScanner.cs
**TS源:** `ts/src/vs/base/common/strings.ts` (L674-696)  
**对齐状态:** ⚠️存在偏差

| 功能 | TS实现 | C#实现 | 差异 |
|------|--------|--------|------|
| `containsRTL` | 预生成 Unicode 正则 + 缓存 | 通过固定区间数组遍历 | 覆盖范围较粗，可能漏判 |
| `containsUnusualLineTerminators` | 检查 `\u2028/\u2029` | 额外检测 `\u0085` (NEL) | 语义更宽 |
| `isBasicASCII` | `/^[\t\n\r\x20-\x7E]*$/` | `ch > 0x7F` 检查 | ✅ |

**建议**
- 若需要完全对齐 VS Code 的“可能包含 RTL”判断，应直接使用 TS 正则或生成等价查表；当前区间缺少部分符号（例如阿迪格语补充块）。
- `NEL` 检测属增强功能，如保持该行为需在上层记录与 TS 差异，避免错误触发“包含异常行终止符”。

---

## 总结

### 完全对齐 (1/8)
- `PieceTreeSnapshot.cs`

### 存在偏差 (5/8)
- `PieceTreeSearchCache.cs`
- `PieceTreeSearcher.cs`
- `PieceTreeTextBufferFactory.cs`
- `SearchTypes.cs`
- `TextMetadataScanner.cs`

### 需要修正 (2/8)
- `Range.Extensions.cs`
- `Selection.cs`

## 建议优先级
- **高:** 补齐 `Range.Extensions` 与 `Selection` 所有 TS 同名方法，修正 `Plus` 的同行比较，补全选择工厂与比较逻辑。
- **中:** 为 `PieceTreeSearchCache`, `PieceTreeTextBufferFactory`, `SearchTypes`, `TextMetadataScanner` 明确标注行为差异，评估是否需要开关或注释保持可读性。
- **低:** 如果决定保留 C# 扩展能力（如缓存 invalidation、NEL 检测），需在 README/注释中添加 parity 说明，避免后续重复调查。

## 验证记录
- 对照阅读 `src/TextBuffer/Core/*.cs` 与对应 TypeScript：如 `pieceTreeBase.ts L207-263`（搜索缓存）、`selection.ts L1-200`、`strings.ts L674-696` 等。
- 使用 `rg -n "class Searcher" ts/src/vs/editor/common/model/textModelSearch.ts`、`rg -n "getFirstLineText" ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBufferBuilder.ts` 等命令定位 TS 行号。
- 核查 `SearchTypes` 与 `wordCharacterClassifier.ts`，确认 `.NET Regex` 设置、`Intl.Segmenter` 缺失以及 `WordCharacterClassifier` 的功能差异。
