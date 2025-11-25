# B2-001 Task Brief – FindModel Stubs Creation

## 你的角色
Porter-CS（C# 移植工程师）

## 记忆文件位置
- `agent-team/members/porter-cs.md`
- 汇报前**必须更新**记忆文件，记录本次任务成果与下一步

## 任务目标
创建 **FindModel 基础设施**（stubs），为 B2-002 核心逻辑实现铺路。

## 前置条件
- 已读取 `agent-team/handoffs/B2-QA-Result.md`（API 契约）
- 已读取 `agent-team/handoffs/B2-INV-Result.md`（技术规格）
- TS 源文件：
  - `ts/src/vs/editor/contrib/find/browser/findState.ts`（FindReplaceState）
  - `ts/src/vs/editor/contrib/find/browser/findModel.ts`（FindModelBoundToEditorModel）
  - `ts/src/vs/editor/contrib/find/browser/findDecorations.ts`（FindDecorations）

## 执行任务

### 1. 创建 FindReplaceState.cs
**路径**：`src/PieceTree.TextBuffer/DocUI/FindReplaceState.cs`

**核心功能**（参考 TS `findState.ts`）：
```csharp
public class FindReplaceState : IDisposable
{
    // 搜索/替换字符串
    public string SearchString { get; set; }
    public string ReplaceString { get; set; }
    
    // Flags
    public bool IsRegex { get; set; }
    public bool IsWholeWord { get; set; }
    public bool IsCaseSensitive { get; set; }
    public bool PreserveCase { get; set; }
    public bool SearchScope { get; set; }  // 限制搜索范围
    
    // 匹配计数
    public int MatchesCount { get; set; }
    public int CurrentMatch { get; set; }
    
    // 事件
    public event EventHandler<FindStateChangeEventArgs>? OnFindReplaceStateChange;
    
    // 方法
    public void Change(FindReplaceStateChanges changes, bool moveCursor);
    public SearchParams CreateSearchParams();  // 转换为 SearchParams
}
```

**重点**：
- 实现 `CreateSearchParams()` 方法，将 state 转换为 `PieceTreeSearcher` 可用的参数
- 集成 `WholeWord` 支持（调用 `WordCharacterClassifier`，参考 B2-INV 规格）

### 2. 创建 FindDecorations.cs
**路径**：`src/PieceTree.TextBuffer/DocUI/FindDecorations.cs`

**核心功能**（参考 TS `findDecorations.ts`）：
```csharp
public class FindDecorations : IDisposable
{
    private readonly ITextModel _model;
    private List<string> _decorationIds = new();
    private string? _currentMatchDecorationId;
    
    // 方法
    public void SetCurrentMatch(Range range);  // 高亮当前匹配
    public void SetAllMatches(Range[] ranges); // 设置所有匹配装饰
    public void ClearDecorations();            // 清除所有装饰
    public Range? GetCurrentMatchRange();      // 获取当前匹配范围
    public Range[] GetAllMatchRanges();        // 获取所有匹配范围
}
```

**装饰样式**：
- `currentFindMatch`：当前匹配（高亮）
- `findMatch`：其他匹配（次要高亮）

### 3. 创建 FindModel.cs（空壳）
**路径**：`src/PieceTree.TextBuffer/DocUI/FindModel.cs`

**核心功能**（暂时只有接口，B2-002 实现逻辑）：
```csharp
public class FindModel : IDisposable
{
    private readonly ITextModel _model;
    private readonly FindReplaceState _state;
    private readonly FindDecorations _decorations;
    
    // 构造函数
    public FindModel(ITextModel model, FindReplaceState state);
    
    // 核心方法（B2-002 实现）
    public void FindNext();                    // TODO(B2-002)
    public void FindPrevious();                // TODO(B2-002)
    public void Replace();                     // TODO(B2-002)
    public void ReplaceAll();                  // TODO(B2-002)
    public void SelectAllMatches();            // TODO(B2-002)
    
    // 辅助方法（B2-001 实现）
    private void UpdateDecorations(Range[] matches);
    private void UpdateState(int matchesCount, int currentMatch);
}
```

### 4. 创建基础测试（验证 stubs）
**路径**：`src/PieceTree.TextBuffer.Tests/DocUI/FindModelStubTests.cs`

**验证场景**（2-3 个简单测试）：
```csharp
[Fact]
public void FindReplaceState_CreateSearchParams_CreatesValidParams()
{
    var state = new FindReplaceState
    {
        SearchString = "hello",
        IsRegex = false,
        IsWholeWord = true,
        IsCaseSensitive = false
    };
    
    var searchParams = state.CreateSearchParams();
    
    Assert.Equal("hello", searchParams.SearchString);
    Assert.False(searchParams.IsRegex);
    Assert.True(searchParams.WholeWord);
    Assert.False(searchParams.CaseSensitive);
}

[Fact]
public void FindDecorations_SetAllMatches_CreatesDecorations()
{
    var model = new TextModel("line1\nline2\nhello\nworld");
    var decorations = new FindDecorations(model);
    
    var ranges = new[] { new Range(3, 1, 3, 6) };
    decorations.SetAllMatches(ranges);
    
    Assert.Single(decorations.GetAllMatchRanges());
}

[Fact]
public void FindModel_Construction_DoesNotThrow()
{
    var model = new TextModel("test");
    var state = new FindReplaceState();
    var findModel = new FindModel(model, state);
    
    Assert.NotNull(findModel);
}
```

## 交付物清单
1. **新增文件**:
   - `src/PieceTree.TextBuffer/DocUI/FindReplaceState.cs`
   - `src/PieceTree.TextBuffer/DocUI/FindDecorations.cs`
   - `src/PieceTree.TextBuffer/DocUI/FindModel.cs`（空壳 + TODO 标记）
   - `src/PieceTree.TextBuffer.Tests/DocUI/FindModelStubTests.cs`（3 个验证测试）
2. **确保 `dotnet test` 通过**（新增 3 个测试，总数 142 → 145）
3. **汇报文档**: `agent-team/handoffs/B2-001-Result.md`
4. **记忆文件更新**: `agent-team/members/porter-cs.md`

## 输出格式
汇报时提供：
1. **文件清单**: 已创建的文件路径
2. **测试结果**: `dotnet test` 输出（145/145）
3. **TODO 清单**: 在 `FindModel.cs` 中预置的 TODO 标记（供 B2-002 处理）
4. **下一步建议**: 给 B2-002（自己）的实施建议
5. **已更新记忆文件**: 确认更新了 `agent-team/members/porter-cs.md`

---
**执行前检查**：
- [ ] 读取 `agent-team/members/porter-cs.md` 获取上下文
- [ ] 参考 TS 源文件确保接口一致性
- [ ] 汇报前更新记忆文件

开始执行！
