# B2-002 Task Brief – FindModel Core Logic Implementation

## 你的角色
Porter-CS（C# 移植工程师）

## 记忆文件位置
- `agent-team/members/porter-cs.md`
- 汇报前**必须更新**记忆文件，记录本次任务成果与下一步

## 任务目标
实现 **FindModel 核心逻辑**，完成搜索、替换、装饰管理功能，为 B2-003 测试迁移做准备。

## 前置条件
- B2-001 已完成（`FindReplaceState`、`FindDecorations` stub、`FindModel` 空壳）
- TS 源文件：
  - `ts/src/vs/editor/contrib/find/browser/findModel.ts`（核心实现）
  - `ts/src/vs/editor/contrib/find/browser/findDecorations.ts`（装饰管理）
  - `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts`（43 个测试用例参考）

## 执行任务

### 1. 完成 FindDecorations.cs 实现
**关键方法**（移除 B2-001 的 TODO）：

```csharp
public void SetCurrentMatch(Range range)
{
    // 1. 清除旧的 currentFindMatch 装饰
    // 2. 创建新的 currentFindMatch 装饰（高亮）
    // 3. 更新 _currentMatchDecorationId
}

public void SetAllMatches(Range[] ranges)
{
    // 1. 清除所有旧装饰
    // 2. 为每个 range 创建 findMatch 装饰
    // 3. 更新 _decorationIds 列表
}

public void ClearDecorations()
{
    // 调用 _model.DeltaDecorations() 删除所有装饰
    _decorationIds.Clear();
    _currentMatchDecorationId = null;
}

public Range? GetCurrentMatchRange()
{
    // 通过 _model.GetDecorationRange(_currentMatchDecorationId) 获取
}

public Range[] GetAllMatchRanges()
{
    // 通过 _decorationIds 查询所有装饰范围
}
```

**装饰选项**（参考 TS `findDecorations.ts`）：
```csharp
private static readonly ModelDecorationOptions CurrentMatchDecoration = new()
{
    ClassName = "currentFindMatch",
    StickToDomWhenRendering = true,
    ZIndex = 13  // TS: zIndex = 13
};

private static readonly ModelDecorationOptions FindMatchDecoration = new()
{
    ClassName = "findMatch",
    StickToDomWhenRendering = true,
    ZIndex = 10  // TS: zIndex = 10
};
```

### 2. 实现 FindModel.cs 核心逻辑
**关键方法**（移除 B2-001 的 TODO）：

#### 搜索逻辑
```csharp
private void OnStateChanged(FindStateChangeEventArgs e)
{
    // 1. 检查是否需要重新搜索（SearchString/flags 变化）
    // 2. 调用 PerformSearch()
    // 3. 更新装饰和状态
}

private Range[] PerformSearch()
{
    // 1. 创建 SearchParams（state.CreateSearchParams()）
    // 2. 调用 _model.FindMatches(searchParams)
    // 3. 返回所有匹配的 Range[]
}

public void FindNext()
{
    // 1. 获取当前光标位置
    // 2. 从当前位置向后查找下一个匹配
    // 3. 更新 currentMatch decoration
    // 4. 移动光标到匹配位置
}

public void FindPrevious()
{
    // 类似 FindNext，但向前查找
}
```

#### 替换逻辑
```csharp
public void Replace()
{
    // 1. 获取当前匹配的 Range
    // 2. 调用 ReplacePattern.BuildReplaceString()（Batch #1）
    // 3. 执行 _model.PushEditOperations()
    // 4. FindNext() 跳转到下一个匹配
}

public void ReplaceAll()
{
    // 1. 获取所有匹配的 Range[]
    // 2. 从后往前替换（避免位置偏移）
    // 3. 批量调用 _model.PushEditOperations()
    // 4. 保持光标位置不变（TS issue #3516）
}
```

#### 辅助功能
```csharp
public void SelectAllMatches()
{
    // 1. 获取所有匹配的 Range[]
    // 2. 创建多光标选区（依赖 Cursor API）
    // 3. 如果 Cursor API 未就绪，抛出 NotImplementedException 并标记 TODO(Batch #3)
}
```

### 3. 处理边缘情况（参考 TS 测试）
**重要场景**（从 43 个 TS 测试中提取）：

1. **空匹配**：搜索无结果时，清除装饰并更新 `MatchesCount = 0`
2. **正则边界符**：`^`、`$`、`.*` 等需要正确处理
3. **Lookahead regex**：TS 有专门测试（`'replace when search string has look ahed regex'`），确保 C# regex 兼容
4. **PreserveCase**：集成 Batch #1 的 `ReplacePattern.BuildReplaceStringWithCasePreserved()`
5. **Search scope**：如果 `state.SearchScope` 为 true，限制搜索范围（可能需要 `SearchRange` 参数）
6. **Cursor position**：`FindNext/Prev` 需要尊重当前光标位置（TS 测试：`'find model next/prev respects cursor position'`）

### 4. 集成点检查
**依赖 TextModel API**（确保这些方法存在）：
- `ITextModel.FindMatches(SearchParams)` → 返回 `FindMatch[]`
- `ITextModel.DeltaDecorations(oldDecorations, newDecorations)` → 返回装饰 ID
- `ITextModel.GetDecorationRange(decorationId)` → 返回 `Range?`
- `ITextModel.PushEditOperations(selections, edits, inverseEdits)` → 执行编辑

**集成 ReplacePattern**（Batch #1）：
- `ReplacePattern.BuildReplaceString(matches, replaceString)` → 生成替换文本
- `ReplacePattern.BuildReplaceStringWithCasePreserved(...)` → PreserveCase 替换

## 交付物清单
1. **完成文件**:
   - `src/PieceTree.TextBuffer/DocUI/FindDecorations.cs`（完整实现）
   - `src/PieceTree.TextBuffer/DocUI/FindModel.cs`（核心逻辑，移除所有 TODO(B2-002)）
2. **基础验证测试**:
   - 扩展 `FindModelStubTests.cs`，新增 5-7 个场景（基础搜索、替换、装饰）
   - 或创建 `FindModelCoreTests.cs`（轻量级验证，不移植完整 TS 测试）
3. **确保 `dotnet test` 通过**（新增 5-7 个测试，总数 149 → 154-156）
4. **汇报文档**: `agent-team/handoffs/B2-002-Result.md`
5. **记忆文件更新**: `agent-team/members/porter-cs.md`

## 输出格式
汇报时提供：
1. **实现摘要**: 完成的关键方法列表
2. **测试结果**: `dotnet test` 输出（154-156/154-156）
3. **已知限制**: 标记为 TODO(Batch #3) 的功能（如 SelectAllMatches 多光标）
4. **下一步建议**: 给 B2-003（QA）的集成建议
5. **已更新记忆文件**: 确认更新了 `agent-team/members/porter-cs.md`

---
**执行前检查**：
- [ ] 读取 `agent-team/members/porter-cs.md` 获取上下文
- [ ] 读取 `agent-team/handoffs/B2-001-Result.md` 了解 stubs 状态
- [ ] 参考 TS 源文件确保逻辑一致性
- [ ] 汇报前更新记忆文件

开始执行！
