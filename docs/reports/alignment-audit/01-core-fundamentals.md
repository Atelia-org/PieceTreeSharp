# Core (PieceTree Fundamentals) 对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 10个核心文件

## 概要
- 完全对齐: 5/10
- 存在偏差: 4/10
- 需要修正: 1/10

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
**对齐状态:** ⚠️存在偏差

**分析:**
- `DefaultChunkSize = 65535` 正确匹配TS的 `AverageBufferSize = 65535`
- `SplitText()` 方法正确实现文本分块逻辑，处理CRLF边界和高代理对
- `NormalizeChunks()` 实现EOL规范化

**偏差说明:**
1. TS中的 `normalizeEOL` 方法在 `PieceTreeBase` 类中（Lines 280-310），使用 `min = averageBufferSize - Math.floor(averageBufferSize / 3)` 和 `max = min * 2` 计算分块阈值
2. C#的 `MinChunkLength` 和 `MaxChunkLength` 计算方式与TS一致，无问题
3. **潜在偏差**: TS的 `SplitText` 在 `createNewPieces` 中处理大文本时使用 `AverageBufferSize - 1` 作为分割点检查高代理对；C#实现使用 `DefaultChunkSize` 直接分割，边界处理略有不同

**修正建议:**
确保 `SplitText` 边界检查与TS完全一致：
```csharp
// TS: if (lastChar === CharCode.CarriageReturn || (lastChar >= 0xD800 && lastChar <= 0xDBFF))
// C#当前实现正确，保留上一个字符以避免拆分CRLF或代理对
```

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
**对齐状态:** ⚠️存在偏差

**分析:**
- 整体结构对齐良好
- `AcceptChunk()` 正确实现BOM处理、尾字符缓存逻辑
- RTL和不寻常行终止符检测已实现

**偏差说明:**
1. **`_finish()` 方法差异**: TS在 `_finish()` 中处理 `_hasPreviousChar` 时直接修改最后一个chunk的buffer和lineStarts，并只增加 `cr` 计数（如果previousChar是CR）。C#实现重新构建整个 `LineStartTable`，可能导致计数差异。

2. **分块策略**: TS的 `_acceptChunk2` 直接使用 `createLineStarts(this._tmpLineStarts, chunk)` 创建行起始数组，C#的 `AddChunk` 使用 `ChunkUtilities.SplitText` 预先分割大块。

**修正建议:**
```csharp
// FinalizeChunks 中应该只增加 CR 计数（如果 _previousChar 是 '\r'），而不是重新计算整个LineStartTable
private void FinalizeChunks()
{
    if (_hasPreviousChar)
    {
        _hasPreviousChar = false;
        if (_chunks.Count == 0)
        {
            AddChunk(_previousChar.ToString());
        }
        else
        {
            var lastIndex = _chunks.Count - 1;
            var lastChunk = _chunks[lastIndex];
            var merged = string.Concat(lastChunk.Buffer, _previousChar);
            var newLineStarts = LineStartBuilder.Build(merged);
            _chunks[lastIndex] = ChunkBuffer.FromPrecomputed(merged, newLineStarts);
            // TS只在previousChar是CR时增加cr计数
            if (_previousChar == '\r')
            {
                _cr++;
            }
        }
    }
    // ...
}
```

---

### 7. PieceTreeModel.cs
**TS源:** pieceTreeBase.ts (Lines 268-1882) - `PieceTreeBase` class
**对齐状态:** ⚠️存在偏差

**分析:**
- 核心数据结构对齐良好：
  - `_buffers` 对应 `_buffers`（TS中索引0是change buffer）
  - `_root` 对应 `root`
  - `_sentinel` 对应 `SENTINEL`
  - `_lastChangeBufferPos` 对应 `_lastChangeBufferPos`
  - `_searchCache` 对应 `_searchCache`
  - `_eol` 对应 `_EOL`

**偏差说明:**
1. **缺少 `_lineCnt` 和 `_length` 字段**: TS维护 `_lineCnt` 和 `_length` 作为缓存字段，C#使用 `TotalLength` 和 `TotalLineFeeds` 从树聚合计算。这是合理的设计差异，但需要确保 `computeBufferMetadata()` 被正确调用。

2. **`_lastVisitedLine` 缓存**: TS使用 `{ lineNumber: number; value: string }` 对象，C#使用 `struct LastVisitedLine`，功能等价。

3. **缺少 `_EOLNormalized` 对应的完整语义**: TS的 `_EOLNormalized` 用于标记EOL是否已规范化，影响多处逻辑（如 `shouldCheckCRLF()`）。C#有 `_eolNormalized` 但某些边界情况可能处理不完整。

4. **`NormalizeEOL` 实现差异**: TS的 `normalizeEOL` 使用正则替换 `/\r\n|\r|\n/g`，C#实现手动遍历字符。逻辑等价但实现方式不同。

**修正建议:**
建议添加 `_lineCnt` 缓存以匹配TS行为，避免每次访问时遍历树：
```csharp
private int _lineCnt = 1;  // TS: this._lineCnt = 1;
private int _length = 0;   // TS: this._length = 0;
```

---

### 8. PieceTreeModel.Edit.cs
**TS源:** pieceTreeBase.ts (Lines 800-1500) - Insert/Delete operations
**对齐状态:** ⚠️存在偏差

**分析:**
- `Insert()` 和 `Delete()` 核心逻辑与TS对齐
- CRLF边界处理（`ValidateCRLFWithPrevNode`, `ValidateCRLFWithNextNode`, `FixCRLF`）已实现
- `CreateNewPieces()` 正确处理大文本分块和change buffer追加

**偏差说明:**
1. **`appendToNode` vs `AppendToChangeBufferNode`**: TS的 `appendToNode` 直接修改buffer 0的内容并更新lineStarts，C#实现使用 `ChunkBuffer.Append()` 创建新ChunkBuffer。这可能导致内存使用差异。

2. **`hitCRLF` 边界处理**: TS在 `appendToNode` 中处理 `hitCRLF` 场景时会pop最后一个lineStart并调整 `_lastChangeBufferPos`，C#实现可能缺少此逻辑。

3. **`createNewPieces` 的 `_` 字符插入**: TS在检测到CRLF跨越边界时会在buffer中插入下划线字符 `'_'` 作为占位符，C#实现没有这个逻辑。

**修正建议:**
需要在 `AppendToChangeBufferNode` 中添加CRLF边界处理：
```csharp
// TS逻辑 (appendToNode):
// if (hitCRLF) {
//     const prevStartOffset = this._buffers[0].lineStarts[this._buffers[0].lineStarts.length - 2];
//     (<number[]>this._buffers[0].lineStarts).pop();
//     this._lastChangeBufferPos = { line: this._lastChangeBufferPos.line - 1, column: startOffset - prevStartOffset };
// }
```

---

### 9. PieceTreeModel.Search.cs
**TS源:** pieceTreeBase.ts (Lines 1500-1800) - Search/FindMatches operations
**对齐状态:** ❌需要修正

**分析:**
- `FindMatchesLineByLine()` 结构与TS对齐
- `GetLineContent()` 和 `GetLineRawContent()` 实现逻辑基本正确
- `GetOffsetAt()` 和 `GetAccumulatedValue()` 已实现

**偏差说明:**
1. **`GetAccumulatedValue` 实现差异**: TS使用 `lineStarts` 数组直接计算偏移量，C#实现手动遍历字符计算换行符。这会导致性能差异和潜在的不一致。

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

2. **`GetContentFromNode` 复杂度**: C#实现的向前/向后遍历逻辑可能与TS的 `getLineRawContent` 存在边界条件差异。

**修正建议:**
重写 `GetAccumulatedValue` 以使用 `LineStarts` 数组：
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
1. **`NodeColor` 枚举值**: TS使用 `Black = 0, Red = 1`，C#使用 `Red, Black`（默认 Red=0, Black=1）。需要确认这不影响逻辑。

2. **Sentinel创建**: TS使用全局 `SENTINEL` 常量，C#使用实例方法 `CreateSentinel()`。设计差异但功能等价。

**修正建议:**
建议将 `NodeColor` 枚举值与TS对齐：
```csharp
internal enum NodeColor
{
    Black = 0,
    Red = 1
}
```

---

## 总结

### 需要立即修正的问题

| 文件 | 问题 | 优先级 |
|------|------|--------|
| PieceTreeModel.Search.cs | `GetAccumulatedValue` 实现与TS不一致 | 高 |
| PieceTreeModel.Edit.cs | CRLF边界处理缺少 `hitCRLF` 逻辑 | 中 |
| PieceTreeBuilder.cs | `FinalizeChunks` CR计数逻辑差异 | 中 |
| PieceTreeNode.cs | `NodeColor` 枚举值顺序与TS不一致 | 低 |

### 设计差异（可接受）

| 文件 | 差异 | 说明 |
|------|------|------|
| PieceTreeModel.cs | 使用聚合属性替代缓存字段 | C#惯用设计，性能影响需评估 |
| ChunkBuffer.cs | 不可变设计 | 比TS更安全，但内存开销略高 |
| PieceTreeNode.cs | 添加 `AggregatedLength/LineFeeds` | 优化遍历性能 |

### 建议的后续行动

1. **高优先级**: 修复 `GetAccumulatedValue` 以使用 `LineStarts` 数组索引
2. **中优先级**: 审查 `AppendToChangeBufferNode` 的CRLF边界处理
3. **低优先级**: 统一 `NodeColor` 枚举值顺序
4. **测试**: 增加针对CRLF边界场景的单元测试

---

**审查人:** AI Audit Agent
**审查方法:** 逐行对比TS源码与C#实现
