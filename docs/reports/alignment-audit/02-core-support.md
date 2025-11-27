# Core Support Types 对齐审查报告

**审查日期:** 2025-11-27
**审查范围:** 8个支持类型文件

## 概要
- 完全对齐: 1/8
- 存在偏差: 7/8 （Range/Selection 由 ⚠️ 接手，剩余差异降级为 P1）
- 需要修正: 0/8 （WS2-PORT 交付已覆盖 Range/Selection/TextPosition P0 helper）

WS2-PORT (`docs/reports/migration-log.md#ws2-port`, [`agent-team/indexes/README.md#delta-2025-11-26-ws2-port`](../../agent-team/indexes/README.md#delta-2025-11-26-ws2-port)) 把 Range/Selection/TextPosition helper 与 75 条 `RangeSelectionHelperTests` 带入主干；FR-01/FR-02 (`docs/reports/migration-log.md#fr-01-02`, [`agent-team/indexes/README.md#delta-2025-11-23`](../../agent-team/indexes/README.md#delta-2025-11-23)) 又补齐了 `WordCharacterClassifierCache` 的 LRU 逻辑。仍需关注 `AA4-CL8` 占位（[`agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)）以追踪 Intl word cache / Segmenter backlog。

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
**对齐状态:** ⚠️存在偏差（P0 helper 已齐全，映射型 helper 仍待实现）

**WS2-PORT 交付 (P0 helper) –** 见 `docs/reports/migration-log.md#ws2-port` 与 [`agent-team/indexes/README.md#delta-2025-11-26-ws2-port`](../../agent-team/indexes/README.md#delta-2025-11-26-ws2-port)：
- 静态/实例 helper 与 TS 同步：`ContainsPosition/StrictContainsPosition/ContainsRange/StrictContainsRange`、`IntersectRanges`、`PlusRange`、`AreIntersecting*`、`CollapseToStart/End`、`Normalize`、`CompareRangesUsingStarts/Ends`、`SpansMultipleLines`、`Delta`、`SetStart/EndPosition`。行列比较逻辑已完全复刻 TS 的“先行号再列号”排序。
- `TextPosition` 同步获得 `With`, `Delta`, `IsBefore`, `IsBeforeOrEqual`, `Compare`, `Equals` 等扩展（`src/TextBuffer/TextPosition.cs`），因此下游调用不再需要自定义比较器。
- `tests/TextBuffer.Tests/RangeSelectionHelperTests.cs` 新增 75 条（现在因 `[Theory]` 展开会执行 100+ data rows）断言，覆盖跨行/同行交集、包含、`Plus`、`Collapse`、`Normalize`、`Compare` 等场景；证据见 [`agent-team/handoffs/WS123-QA-Result.md`](../../agent-team/handoffs/WS123-QA-Result.md) 与 TestMatrix `#delta-2025-11-26-ws2-port` 记录。

**剩余差异**
1. `RangeMapping` / `SelectionRangeMapping` / `TrackedRange` bridge 仍缺（TS `range.ts` L210+）。WS2-PORT 仅交付基础 helper，仍需一个对标 `RangeMapping` 的结构来让 `Selection`、`Decorations`、`TrackedRange` 重用增量更新逻辑。
2. `Range` 与 `Selection` 之间仍无共享接口/继承关系（TS `Selection` extends `Range`），导致调用方无法在不复制字段的情况下传递 Selection。这个差异已降级为 P1，但在 Cursor/DocUI 合并（WS4/AA4-CL7）前仍需保留说明。

---

### 6. SearchTypes.cs
**TS源:** `ts/src/vs/editor/common/model/textModelSearch.ts` (L1-166) + `ts/src/vs/editor/common/core/wordCharacterClassifier.ts` (L1-120) + `ts/src/vs/editor/common/model.ts` (L1535-1570)  
**对齐状态:** ⚠️存在偏差

**关键差异**
1. **SearchData 结构**：TS 仅持有 `regex`, `wordSeparators`, `simpleSearch`；C# 额外公开 `IsMultiline`, `IsCaseSensitive`（L46-59）。这些扩展对 C# 有用，但 TS 端没有，需要在文档注明。
2. **正则构建**：TS 使用 `strings.createRegExp`，自动开启 `unicode/global` 并遵守 ECMAScript 语义；C# 依赖 `Regex` + `ApplyUnicodeWildcardCompatibility` 替换 `.`。仍可能在带代理对的 `\b`、`\w` 等场景出现差距。
3. **WordCharacterClassifierCache**：FR-01/FR-02 (`docs/reports/migration-log.md#fr-01-02`, [`agent-team/indexes/README.md#delta-2025-11-23`](../../agent-team/indexes/README.md#delta-2025-11-23)) 已移植 10-entry LRU 缓存，并在 `SearchTypes.ParseSearchRequest` / `PieceTreeSearcher` 中复用，与 TS `getMapForWordSeparators` 逻辑对齐。但缓存仍只基于 ASCII word separators，尚未接入 `Intl.Segmenter`。
4. **Intl/多语边界**：TS 的 `isValidMatch` 可结合 `Intl.Segmenter` 返回值与 `wordSeparators`；C# 目前仍只依据字符类别（`WordCharacterClassifier`）与换行符判断。跨语言/emoji 边界仍会偏差，且 `AA4 CL8` 占位（[`agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)）明确要求将 Intl 分词、word cache、DocUI capture 同步。

**建议**
- 将 `SearchData` 扩展字段标注为 C# 专属，并确认调用方不会把它们同步回 TypeScript。
- 视需求评估 `Intl.Segmenter` 支持；或至少在 `WordCharacterClassifier` 注释中注明“不支持 locale-aware 的单词边界”。

---

### 7. Selection.cs
**TS源:** `ts/src/vs/editor/common/core/selection.ts` (L1-200)  
**对齐状态:** ⚠️存在偏差（核心 API 已补全，Range 继承/接口仍不一致）

**WS2-PORT 成果 –** 引用同上 `docs/reports/migration-log.md#ws2-port` / [`agent-team/indexes/README.md#delta-2025-11-26-ws2-port`](../../agent-team/indexes/README.md#delta-2025-11-26-ws2-port)：
- `Selection` 结构现包含 TS 同名 helper：`SelectionStart/End`/`Start/End` alias、`SelectionDirection`、`IsEmpty`、`Contains`、`SetStartPosition`、`SetEndPosition`、`GetSelectionStart`、`GetPosition`、`GetDirection`、`CollapseToStart/End`。
- 静态工厂/比较：`FromPositions`, `FromRange`, `CreateWithDirection`, `LiftSelection`, `SelectionsEqual`, `SelectionsArrEqual`, `EqualsSelection` 等均按 TS 行为移植。
- 与 Range helper 共用的 75 条 `RangeSelectionHelperTests` 也覆盖 Selection（对锚点/活动端的方向性断言 + `SelectionsArrEqual`），再加上 `tests/TextBuffer.Tests/RangeSelectionHelperTests.cs` 中 LTR/RTL 样例验证，从而复用 VS Code 的 multi-cursor 语义。

**残留差异（P1）**
1. C# `Selection` 仍是独立 `struct`，未继承 `Range`，因此调用方在需要 Range 时必须显式转换；这影响 `CursorCollection`/`SelectionHelper` 等 TS API 的泛型约束。
2. TS `Selection.isISelection(obj)` 尚未实现（C# 只提供 `LiftSelection`）。DocUI/Cursor 侧如果需要类型守卫，还得复制逻辑。
3. `Selection` 与未来的 `RangeMapping/TrackedRange`（见上一节）尚无桥接，导致多选/粘滞范围更新仍需单独实现；这将由后续 AA4-CL7 工作流与 RangeMapping 一并收敛。

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

### 存在偏差 (7/8)
- `PieceTreeSearchCache.cs`
- `PieceTreeSearcher.cs`
- `PieceTreeTextBufferFactory.cs`
- `Range.Extensions.cs`（RangeMapping/Selection bridge 未落地）
- `SearchTypes.cs`（Intl/word cache backlog）
- `Selection.cs`（未继承 Range，缺 `isISelection`）
- `TextMetadataScanner.cs`

### 需要修正 (0/8)
- 无 – Range/Selection 已由 WS2-PORT 交付核心 helper，剩余差异降级为 P1 并记录在“存在偏差”清单。

## 建议优先级
- **高:** 继续 WS2-PORT 后续项：实现 `RangeMapping`/`SelectionRangeMapping`/`TrackedRange` 桥接，并评估让 `Selection` 暴露 `isISelection` 或共有接口，方便 Cursor/DocUI/Decorations 共用（关联 `AA4-CL7` backlog）。
- **中:** 扩展 `WordCharacterClassifierCache` 以支撑 locale-aware 边界（`Intl.Segmenter`、词缓存多语言配置），并将 SearchTypes/DocUI/Find stack 统一回 `#delta-2025-11-26-aa4-cl8-markdown` 占位。
- **低:** 为 C# 端特有的行为（PieceTreeSearchCache limit 默认值、TextMetadataScanner 的 NEL 检测、PieceTreeTextBufferFactory 的 build result 包装）补充注释和变更日志，避免后续审计重复记录。

## 验证记录
- 对照阅读 `src/TextBuffer/Core/*.cs` 与对应 TypeScript：如 `pieceTreeBase.ts L207-263`（搜索缓存）、`selection.ts L1-200`、`strings.ts L674-696` 等。
- 使用 `rg -n "class Searcher" ts/src/vs/editor/common/model/textModelSearch.ts`、`rg -n "getFirstLineText" ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBufferBuilder.ts` 等命令定位 TS 行号。
- 核查 `SearchTypes` 与 `wordCharacterClassifier.ts`，确认 `.NET Regex` 设置、`Intl.Segmenter` 缺失以及 `WordCharacterClassifier` 的功能差异。
- `2025-11-27` 复核命令：`export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter RangeSelectionHelperTests --nologo`（109/109 数据行；`WS123-QA-Result.md` 仍记录原始 75/75 断言）——用于确认 Range/Selection/TextPosition helper 在最新主干仍全部通过。
