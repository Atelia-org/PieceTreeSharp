# Core (PieceTree Fundamentals) 对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 10个核心文件

## 概要
- 完全对齐: 8/10
- 存在偏差: 0/10
- 需要修正: 2/10

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

**分析:**
- 插入/删除流程、RB-Tree 旋转、`ValidateCRLFWithPrev/NextNode`、`FixCRLF` 入口均已移植，`ChunkUtilities.SplitText` 也保证 `CreateNewPieces` 在大文本场景下与 TS 相同地切片。
- Change-buffer 复用路径 (`TryAppendToChangeBufferNode` → `AppendToChangeBufferNode`) 仍沿用缓冲区 0，但少了 TS 针对跨片 `\r\n` 的修正。

**偏差说明:**
1. **漏掉 `hitCRLF` 处理**：TS `appendToNode`（pieceTreeBase.ts L1455-L1476）在旧节点以 `\r` 结尾且追加文本以 `\n` 开始时，会弹出 `_buffers[0].lineStarts` 最后一项并把 `_lastChangeBufferPos` 往前挪 1 行，保证这一对字符仍被视作单个 CRLF。`AppendToChangeBufferNode`（PieceTreeModel.Edit.cs L205-L258）直接调用 `ChunkBuffer.Append`，没有检测/回退 line start，因此跨 append 的 CRLF 会被算成两个换行，`_lastChangeBufferPos` 也偏移。
2. **缺少 `_buffers[0]` 级别的桥接逻辑**：TS `createNewPieces`（pieceTreeBase.ts L1208-L1224）若发现 change buffer 末尾是 `\r`、新文本以 `\n` 开头，会先把 `_lastChangeBufferPos` 列列 +1、将后续 `lineStarts` 整体平移 `startOffset + 1`，并在缓冲区中插入一个哑字符（通过 `this._buffers[0].buffer += '_' + text`）以维持索引。C# `CreateNewPieces`（PieceTreeModel.Edit.cs L700-L733）直接 `changeBuffer.Append(text)`，没有占位/平移，导致 change buffer 中已经存在的 `\r` 无法与随后插入的 `\n` 合并，行统计再次重复。

**修正建议:**
- 在 `AppendToChangeBufferNode` 中移植 TS `hitCRLF` 分支：检查 `EndWithCR(node.Piece)` 与 `StartWithLF(adjusted)`，在必要时 pop 掉 `_buffers[0]` 的最后一个 line start 并把 `_lastChangeBufferPos` 回滚一行。
- 在 `CreateNewPieces` 的 change-buffer 分支中添加与 TS 相同的 `_buffers[0]` 处理：当 `startOffset == lineStarts[^1]` 且 buffer 以 `\r` 结尾而文本以 `\n` 开头时，先推进 `_lastChangeBufferPos`、把生成的 `lineStarts` 全部平移 `startOffset + 1`、往 buffer 里写入占位字符，再将真正的 `text` 紧随其后，并确保新 `PieceSegment` 的 `Start` 跳过该占位符。

---

### 9. PieceTreeModel.Search.cs
**TS源:** pieceTreeBase.ts (Lines 1500-1800) - Search/FindMatches operations
**对齐状态:** ❌需要修正

**分析:**
- `FindMatchesLineByLine()` 结构与TS对齐
- `GetLineContent()` 和 `GetLineRawContent()` 实现逻辑基本正确
- `GetOffsetAt()` 和 `GetAccumulatedValue()` 已实现

**偏差说明:**
1. **`GetAccumulatedValue` 实现差异**：TS使用 `lineStarts` 数组直接计算偏移量，C#实现手动遍历字符计算换行符。这会导致 O(n) 的热点路径，并在跨 piece 的 CRLF 上可能得到与 TS 不同的偏移。

TS实现:
```typescript
private getAccumulatedValue(node: TreeNode, index: number) {
    if (index < 0) {
        return 0;
    }
    const piece = node.piece;
    const lineStarts = this._buffers[piece.bufferIndex].lineStarts;
    const expectedLineStartIndex = piece.start.line + index + 1;
    if (expectedLineStartIndex > piece.end.line) {
        return lineStarts[piece.end.line] + piece.end.column - lineStarts[piece.start.line] - piece.start.column;
    } else {
        return lineStarts[expectedLineStartIndex] - lineStarts[piece.start.line] - piece.start.column;
    }
}
```

C#实现使用手动字符遍历，这与TS的O(1)数组访问不同。

2. **`NodeAt2` 退化为两次遍历**：在 C# 中 `NodeAt2`（PieceTreeModel.Search.cs L174-L188）只是调用 `GetOffsetAt` 再调用 `NodeAt`。TS `nodeAt2`（pieceTreeBase.ts L1230-L1306）结合 `lf_left`/`size_left` 一次性定位节点，并将命中结果写入 `_searchCache`。当前实现不仅多一次树查找，还错过了 TS 中缓存 `nodeStartLineNumber` 的行为，频繁搜索时会额外击穿缓存。

**修正建议:**
- 重写 `GetAccumulatedValue` 以使用 `LineStarts` 数组：
```csharp
private int GetAccumulatedValue(PieceTreeNode node, int index)
{
    if (index < 0)
    {
        return 0;
    }

    var piece = node.Piece;
    var lineStarts = _buffers[piece.BufferIndex].LineStarts;
    var expectedLineStartIndex = piece.Start.Line + index + 1;
    
    if (expectedLineStartIndex > piece.End.Line)
    {
        return lineStarts[piece.End.Line] + piece.End.Column 
               - lineStarts[piece.Start.Line] - piece.Start.Column;
    }
    else
    {
        return lineStarts[expectedLineStartIndex] 
               - lineStarts[piece.Start.Line] - piece.Start.Column;
    }
}
```
- 将 `NodeAt2` 改写为 TS 版本的树遍历：依据 `LineFeedsLeft`/`Piece.LineFeedCount` 判断走向，按需调用 `GetAccumulatedValue` 只计算所需的行偏移，并在命中后向 `_searchCache.Remember(...)` 写入 `nodeStartLineNumber`，避免二次查找。

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
| PieceTreeModel.Edit.cs | Change buffer 未处理跨 append/`CreateNewPieces` 的 CRLF，导致行计数错误 | 高 |
| PieceTreeModel.Search.cs | `GetAccumulatedValue` 退化为 O(n) 且 `NodeAt2` 双遍历，偏离 TS 行定位逻辑 | 高 |

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
- 逐文件对比了 `src/TextBuffer/Core` 与 `ts/src/vs/editor/common/model/pieceTree*`，重点复查 `ChunkUtilities.SplitText`、`PieceTreeBuilder.FinalizeChunks` 与 `PieceTreeModel.NormalizeEOL`，确认这些部分与 TS 保持一致。
- 针对 change buffer CRLF 问题，复查了 `PieceTreeModel.Edit.cs` 的 `AppendToChangeBufferNode` / `CreateNewPieces` 与 TS `appendToNode`/`createNewPieces`（ts lines 1208-1476），确认 `_lastChangeBufferPos` 与 `lineStarts` 回滚逻辑缺失。
- Search 部分重新核查了 `GetAccumulatedValue`、`NodeAt2` 与 TS 实现；虽然 `PieceTreeSearcher` 逻辑吻合，但 `nodeAt2` 的双遍历仍待修正。`'_'` 占位符的语义完全来自 TS，C# 尚未实现，需依赖新增测试验证。
