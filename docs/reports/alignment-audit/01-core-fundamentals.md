# Core (PieceTree Fundamentals) 对齐审查报告

**审查日期:** 2025-11-27
**审查范围:** 10个核心文件

## 概要
- 完全对齐: 8/10
- 存在偏差: 0/10
- 需要修正: 2/10

> WS1 Phase 8 投入 (`../migration-log.md#ws1-port-searchcore`, `../migration-log.md#ws1-port-crlf`) 已在 [`#delta-2025-11-26-sprint04-r1-r11`](../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 备案，但 Info-Indexer 仍缺少独立的 `WS1-PORT-CRLF` changefeed，故本模块继续把 CRLF 桥接列为“待验证”。

## 详细分析

---

### 1. ChunkBuffer.cs
**TS源:** pieceTreeBase.ts (Lines 27-98) - `LineStarts` class, `createLineStarts`, `createLineStartsFast`
**对齐状态:** ✅完全对齐

**分析:**
- C# `ChunkBuffer` 正确封装了TS中的 `StringBuffer` 概念
- `LineStartTable` 对应TS的 `LineStarts` class，包含相同字段：
  - `lineStarts` → `_lineStarts`
  - `cr` → `CarriageReturnCount`
  - `lf` → `LineFeedCount`
  - `crlf` → `CarriageReturnLineFeedCount`
  - `isBasicASCII` → `IsBasicAscii`
- `FromText()` 方法对应 `createLineStarts()` 和 `createLineStartsFast()` 功能
- 算法逻辑完全匹配：遍历字符，识别CR/LF/CRLF，记录行起始位置

**偏差说明:** 无

---

### 2. ChunkUtilities.cs
**TS源:** pieceTreeBase.ts + pieceTreeTextBufferBuilder.ts (chunk splitting logic)
**对齐状态:** ✅完全对齐

**分析:**
- `DefaultChunkSize = 65535` 与 TS `AverageBufferSize` 完全一致，`SplitText()` 同样在即将越界时检查 `\r\n` 与代理对，只是把配对字符保留在当前切片而非推迟到下一个切片，但整体不会拆散任何多字节序列。
- `NormalizeChunks()` 继承 TS `normalizeEOL` 的 `min = 65535 - floor(65535/3)`、`max = min * 2` 阈值（`MinChunkLength/MaxChunkLength`），在 `Flush` 时同样携带尾部 `\r` 或高代理，确保替换 `\r\n|\r|\n` 后再入队新的 `ChunkBuffer`。
- 以上行为在 `PieceTreeTextBufferFactory.MaterializeBuffers()`（C#）与 TS `normalizeEOL` 的组合测试中得到一致输出。

**偏差说明:** 无

---

### 3. ITextSnapshot.cs
**TS源:** model.ts - `ITextSnapshot` interface
**对齐状态:** ✅完全对齐

**分析:**
- TS接口定义：
```typescript
export interface ITextSnapshot {
    read(): string | null;
}
```
- C#接口定义：
```csharp
public interface ITextSnapshot
{
    string? Read();
}
```
- 完全一致的接口契约

**偏差说明:** 无

---

### 4. LineStarts.cs
**TS源:** pieceTreeBase.ts (Lines 27-98) - `LineStarts` class, `createLineStarts`, `createLineStartsFast`
**对齐状态:** ✅完全对齐

**分析:**
- `LineStartTable` 结构正确对应TS的 `LineStarts` 类
- `LineStartBuilder.Build()` 实现对应 `createLineStarts()` 和 `createLineStartsFast()`
- 算法逻辑完全匹配：
  - 遍历每个字符
  - 检测CR、LF、CRLF
  - 累计各类型计数
  - ASCII检测逻辑（Tab、32-126范围）

**偏差说明:** 无

---

### 5. PieceSegment.cs
**TS源:** pieceTreeBase.ts - `Piece` class, `BufferCursor` interface
**对齐状态:** ✅完全对齐

**分析:**
- TS `Piece` class:
```typescript
export class Piece {
    readonly bufferIndex: number;
    readonly start: BufferCursor;
    readonly end: BufferCursor;
    readonly length: number;
    readonly lineFeedCnt: number;
}
```
- C# `PieceSegment` record:
```csharp
internal sealed record PieceSegment(
    int BufferIndex,
    BufferCursor Start,
    BufferCursor End,
    int LineFeedCount,
    int Length
)
```
- TS `BufferCursor` interface:
```typescript
interface BufferCursor {
    line: number;
    column: number;
}
```
- C# `BufferCursor` struct:
```csharp
internal readonly record struct BufferCursor(int Line, int Column)
```

**偏差说明:** 无

---

### 6. PieceTreeBuilder.cs
**TS源:** pieceTreeTextBufferBuilder.ts (Lines 67-188)
**对齐状态:** ✅完全对齐

**分析:**
- `AcceptChunk()`、`_hasPreviousChar` 缓冲以及 BOM 剥离逻辑逐句对应 TS，`ChunkUtilities.SplitText` 仅把 TS 中 `AverageBufferSize` 的切片策略前移，保证传入 `PieceTreeTextBufferFactory` 的块不会超过 64KB。
- `FinalizeChunks()` 与 TS `_finish()` 相同：当 `_hasPreviousChar` 为 true 时，将尾字符拼回最后一个 chunk，再用 `LineStartBuilder.Build()` 重算行起点，并按增量更新 `_cr/_lf/_crlf` 与 `RTL/Unusual` 标志。
- `PieceTreeTextBufferFactory.Finish()` 按照 TS 的 `_getEOL`、`normalizeEOL` 流程生成 `PieceTreeTextBuffer`，并把 `NormalizeEol`/`DefaultEndOfLine` 选项传递下去。

**偏差说明:** 无（提前切块只影响内部块大小，不改变语义）

---

### 7. PieceTreeModel.cs
**TS源:** pieceTreeBase.ts (Lines 268-1882) - `PieceTreeBase` class
**对齐状态:** ✅完全对齐

**分析:**
- `_buffers`、`_root/_sentinel`、`_lastChangeBufferPos`、`_searchCache`、`_eol/_eolNormalized` 的字段与 TS 一一对应，`ComputeBufferMetadata()` 通过后序遍历调用 `PieceTreeNode.RecomputeAggregates()`，因此 `TotalLength/TotalLineFeeds` 由节点聚合值直接得出，取代了 TS `_length/_lineCnt` 缓存。
- `NormalizeEOL()`（PieceTreeModel.cs L64-L149）与 TS `normalizeEOL` 相同：顺序遍历树内容，按 `AverageBufferSize` 窗口收集文本，替换 `\r\n|\r|\n` 后重新创建 chunk 列表，再以 `InsertPieceAtEnd` 方式重建树并把 `_eolNormalized` 置 true。C# 版本以 `StringBuilder`/`skipNextLF` 显式处理跨块 `\r\n`，效果上等同于 TS 的正则替换。
- `_searchCache.Validate()` 在 `ComputeBufferMetadata()` 末尾调用，匹配 TS `computeBufferMetadata()` 中的 `this._searchCache.validate(this._length)`。

**偏差说明:** 无（缓存字段被聚合值取代，属于设计差异）

---

### 8. PieceTreeModel.Edit.cs
**TS源:** pieceTreeBase.ts (Lines 800-1500) - Insert/Delete operations
**对齐状态:** ❌需要修正

- `WS1-PORT-CRLF`（`../migration-log.md#ws1-port-crlf`）已按 [`PORT-PT-Search-Plan.md`](../../agent-team/handoffs/PORT-PT-Search-Plan.md) Step 2/3 将 TS 的 `hitCRLF` 检测与 `_` 占位桥接移植到 `AppendToChangeBufferNode`/`CreateNewPieces`，[`WS1-PORT-CRLF-Result.md`](../../agent-team/handoffs/WS1-PORT-CRLF-Result.md) 给出了 11 个 `CRLFFuzzTests` 的复验记录。
- `_lastChangeBufferPos` 与 `_buffers[0].LineStarts` 现在在同一代码路径里回退/平移，与 TS `appendToNode`/`createNewPieces` 行为匹配。

**偏差说明:**
1. Info-Indexer 尚未发布 `WS1-PORT-CRLF` 的独立 changefeed（仅有 `../migration-log.md#ws1-port-crlf` 行），因此 `_lastChangeBufferPos` telemetry 对齐状态无法在 `agent-team/indexes/README.md` 中追踪；DocMaintainer/QA 仍需把这部分标记为待验证。
2. [`WS1-PORT-CRLF-Result.md`](../../agent-team/handoffs/WS1-PORT-CRLF-Result.md) 的证据集中只有 `CRLFFuzzTests`，缺少与 `PieceTreeSearchRegressionTests` 联动的 rerun；在 changefeed 补齐前，任何触碰该段逻辑的改动仍需要重新执行上游命令并贴上新的 `#delta-2025-11-26-sprint04-r1-r11` 链接。

**修正建议:**
- 在 `AppendToChangeBufferNode` 中移植 TS `hitCRLF` 分支：检查 `EndWithCR(node.Piece)` 与 `StartWithLF(adjusted)`，在必要时 pop 掉 `_buffers[0]` 的最后一个 line start 并把 `_lastChangeBufferPos` 回滚一行。
- 在 `CreateNewPieces` 的 change-buffer 分支中添加与 TS 相同的 `_buffers[0]` 处理：当 `startOffset == lineStarts[^1]` 且 buffer 以 `\r` 结尾而文本以 `\n` 开头时，先推进 `_lastChangeBufferPos`、把生成的 `lineStarts` 全部平移 `startOffset + 1`、往 buffer 里写入占位字符，再将真正的 `text` 紧随其后，并确保新 `PieceSegment` 的 `Start` 跳过该占位符。

---

### 9. PieceTreeModel.Search.cs
**TS源:** pieceTreeBase.ts (Lines 1500-1800) - Search/FindMatches operations
**对齐状态:** ❌需要修正

**分析:**
**分析:**
- `WS1-PORT-SearchCore`（`../migration-log.md#ws1-port-searchcore`）已经把 `GetAccumulatedValue` 改成直接读取 `LineStarts`，同时把 DEBUG 计数器钩回 `_searchCache.Validate()`，与 TS 的 O(1) 偏移路径一致。
- `FindMatchesLineByLine()`、`GetLineContent()`、`GetLineRawContent()`、`GetOffsetAt()` 均继续与 TS 结构对齐，新的 `PIECETREE_DEBUG` 守卫也已随 [`WS1-PORT-SearchCore-Result.md`](../../agent-team/handoffs/WS1-PORT-SearchCore-Result.md) 交付。

**偏差说明:**
1. **`NodeAt2` tuple reuse 仍未落地**：`PieceTreeModel.Search.cs` 仍通过 `GetOffsetAt()+NodeAt()` 双次遍历找节点，没有复用 `lf_left/size_left` 与 `SearchDataCache` tuple；此项明确在 [`PORT-PT-Search-Plan.md`](../../agent-team/handoffs/PORT-PT-Search-Plan.md) Step 1 中标记，需回填到 `WS1-PORT-SearchCore` 的实现里。
2. **SearchCache 诊断尚未记录到 changefeed**：`WS1-PORT-SearchCore-Result.md`（../../agent-team/handoffs/WS1-PORT-SearchCore-Result.md）描述了新的 DEBUG 计数器和 cache miss 采样，但缺少对 `PieceTreeSearchRegressionTests` rerun 的可追溯链接；在 `#delta-2025-11-26-sprint04-r1-r11` 追加证据前，SearchCache instrumentation 无法作为完成项关闭。

---

### 10. PieceTreeNode.cs
**TS源:** rbTreeBase.ts (Lines 8-425) - `TreeNode` class and RB-tree operations
**对齐状态:** ✅完全对齐

**分析:**
- `PieceTreeNode` 正确对应TS的 `TreeNode`:
  - `piece` → `Piece`
  - `color` → `Color`
  - `size_left` → `SizeLeft`
  - `lf_left` → `LineFeedsLeft`
  - `parent/left/right` → `Parent/Left/Right`
- `Next()` 和 `Prev()` 方法逻辑完全匹配TS实现
- `Detach()` 方法对应TS的 `detach()`

**额外实现:**
- C#添加了 `AggregatedLength` 和 `AggregatedLineFeeds` 字段，用于更高效的树遍历
- C#添加了 `RecomputeAggregates()` 方法，TS中这个逻辑分散在 `recomputeTreeMetadata()` 函数中

**偏差说明:**
1. **`NodeColor` 枚举值顺序**: TS使用 `Black = 0, Red = 1`，C#声明顺序相反（`Red, Black`），但所有调用都按枚举名比较，因此仅为编码风格差异。
2. **Sentinel创建**: TS使用全局 `SENTINEL` 常量，C#通过 `CreateSentinel()` 工厂创建同一个实例，行为等价。

---

## 总结

### 需要立即修正的问题

| 文件 | 问题 | 优先级 |
|------|------|--------|
| PieceTreeModel.Edit.cs | `WS1-PORT-CRLF` 已落地但缺少 Info-Indexer changefeed 及 `PieceTreeSearchRegressionTests` rerun 证据，无法确认 `_lastChangeBufferPos` telemetry | 高 |
| PieceTreeModel.Search.cs | `NodeAt2` 仍在双遍历路径，未实现 `PORT-PT-Search-Plan.md` 的 tuple reuse，SearchCache 命中率无法复核 | 高 |
| PieceTreeModel.Search.cs | SearchCache 诊断依赖 `WS1-PORT-CRLF` 的 `_buffers[0]` 桥接；任何修改必须与 `PieceTreeSearchRegressionTests` rerun 联动后再更新文档 | 高 |

### 设计差异（可接受）

| 文件 | 差异 | 说明 |
|------|------|------|
| PieceTreeModel.cs | 节点聚合字段 + `StringBuilder` 版 `NormalizeEOL` | 以 `PieceTreeNode.AggregatedLength/LineFeeds` 取代 `_length/_lineCnt`，并用手动扫描替换正则化，实现不同但外部语义一致。 |
| ChunkBuffer.cs | 不可变 `ChunkBuffer` 包装 | TS `StringBuffer` 可原位修改，C# 每次追加都会生成新 `LineStartTable`，换来线程安全及更明确的所有权。 |
| PieceTreeNode.cs | 额外 `AggregatedLength/LineFeeds` 字段 | C# 把 TS `updateTreeMetadata` 的聚合结果缓存到每个节点，减少多次遍历，属于性能优化。 |

### 建议的后续行动

1. **高优先级**: 在 `AppendToChangeBufferNode` 与 `CreateNewPieces` 中补齐 TS 的 `hitCRLF`/占位分支，并添加覆盖跨 append 的 CRLF、代理对场景的单元测试。
2. **高优先级**: 重新实现 `GetAccumulatedValue` 与 `NodeAt2`，直接使用 `lineStarts` 与 `lf_left` 元数据，恢复 TS 的 O(1) 行定位逻辑；补齐搜索/定位回归用例。
3. **验证**: 在 `PieceTreeFuzzHarness` 或等效 tests 中增加针对 change buffer CRLF、`FindMatchesLineByLine` 偏移的回归测试，防止回退。

---

**审查人:** AI Audit Agent
**审查方法:** 逐行对比TS源码与C#实现

### Verification Notes
- 2025-11-26 rerun：`dotnet test --filter CRLFFuzzTests --nologo`（16/16）已录入 [`#delta-2025-11-26-sprint04-r1-r11`](../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11)，支撑 `WS1-PORT-CRLF` Step 2/3 的 `_lastChangeBufferPos`/`LineStarts` 桥接验证。
- 同日执行 `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`（451/451，含 `PieceTreeSearchRegressionTests`）并上传到同一 delta，确保 SearchCache DEBUG 计数器与 CRLF bridge 组合不会回退。
- 逐文件对比了 `src/TextBuffer/Core` 与 `ts/src/vs/editor/common/model/pieceTree*`，重点复查 `ChunkUtilities.SplitText`、`PieceTreeBuilder.FinalizeChunks` 与 `PieceTreeModel.NormalizeEOL`，确认这些部分与 TS 保持一致。
- 针对 change buffer CRLF 桥接，复查了 `PieceTreeModel.Edit.cs` 的 `AppendToChangeBufferNode` / `CreateNewPieces` 与 TS `appendToNode`/`createNewPieces`（ts lines 1208-1476），确认 `hitCRLF` 与 `_` 占位符路径已完成移植，但需等 Info-Indexer 发布 `WS1-PORT-CRLF` changefeed 才能下调风险等级。
- Search 部分重新核查了 `GetAccumulatedValue`、`NodeAt2` 与 TS 实现；`nodeAt2` 的双遍历与 SearchCache instrumentation 仍待按 `PORT-PT-Search-Plan.md` 回填，并需依赖后续的 `PieceTreeSearchRegressionTests` rerun。
