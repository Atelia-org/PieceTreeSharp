# B2-QA Result – Batch #2 Test Matrix Draft

**QA-Automation**: QA-Automation  
**Date**: 2025-11-22  
**Task Brief**: `agent-team/handoffs/B2-QA-TaskBrief.md`  
**Input**: `agent-team/handoffs/B2-INV-Result.md`, `agent-team/handoffs/B2-PLAN-Result.md`

---

## Executive Summary

已完成 Batch #2（FindModel）测试矩阵草拟，为 Porter-CS 实施 B2-001~003 提供验收标准与测试路线图。

**核心成果**：
1. **15 个高优先级测试场景** ✅：从 TS `findModel.test.ts`（43 个测试）中选择核心场景，覆盖增量搜索、replace、wholeWord、decoration 同步等关键功能
2. **DocUI Test Harness 草案** ✅：设计最小化 C# harness（`TestEditorContext` 或 `WithTestTextModel` helper），适配 TS `withTestCodeEditor` 语义
3. **Word Boundary 测试矩阵** ✅：补充 B2-INV 建议的 Tier A 测试（ASCII separators、Unicode、multi-char operators）
4. **测试命令与验收标准** ✅：定义 `dotnet test` 基线与专项过滤命令，明确 Porter-CS/QA 各自的交付物

**测试覆盖策略**：
- **P0（必须实现）**：7 个核心场景（增量搜索、findNext/findPrev、replace、replaceAll、wholeWord、decoration 移除、matches 计数）
- **P1（推荐实现）**：5 个进阶场景（wraparound、search scope、regex 边界符、multi-selection、preserving case）
- **P2（可选实现）**：3 个边缘场景（large file、lookahead regex、issue regressions）

**阻塞/依赖**：
- 依赖 B2-002（Porter-CS 完成 FindModel 实现）
- 参考 Batch #1 harness 经验（`ReplacePatternTests.cs` 简化模式）

---

## 1. 核心测试场景清单（15 个）

### 优先级分级说明
- **P0（必须）**：核心功能验证，阻塞 Batch #2 验收
- **P1（推荐）**：进阶功能，提升测试覆盖度
- **P2（可选）**：边缘场景，可推迟至后续批次

---

### P0 – 核心功能（7 个测试）

#### T1: 增量搜索（Incremental Find）
**TS 参考**: `findTest('incremental find from beginning of file')`  
**验收标准**:
- 输入 `"H"` → 高亮所有 `H` 匹配，光标移动到第一个匹配
- 输入 `"He"` → 高亮所有 `He` 匹配，光标更新到第一个 `He`
- 输入 `"Hello"` → 高亮所有 `Hello` 匹配，光标更新到第一个 `Hello`
- 切换 `matchCase` → 高亮减少到仅匹配大小写的 `Hello`
- 切换 `wholeWord` → 高亮减少到仅全词匹配的 `hello`

**实现方法**:
```csharp
[Fact]
public void IncrementalFind_FromBeginningOfFile()
{
    var harness = TestEditorContext.Create("// my cool header\n...");
    harness.SetCursorPosition(1, 1);
    
    harness.FindState.Change(searchString: "H");
    harness.AssertFindState(
        cursor: new Range(1, 12, 1, 13),
        highlighted: new Range(1, 12, 1, 13),
        allMatches: new[] { new Range(1, 12, 1, 13), new Range(2, 16, 2, 17), ... }
    );
    
    harness.FindState.Change(searchString: "Hello");
    harness.AssertFindState(...);
    
    harness.FindState.Change(matchCase: true);
    harness.AssertFindState(...); // 匹配数减少
}
```

---

#### T2: FindNext/FindPrevious 导航
**TS 参考**: `findTest('find model next')`, `findTest('find model prev')`  
**验收标准**:
- `FindNext()` → 光标移动到下一个匹配（循环到文件开头）
- `FindPrevious()` → 光标移动到上一个匹配（循环到文件末尾）
- 无匹配时 `FindNext/Prev` 不移动光标

**实现方法**:
```csharp
[Fact]
public void FindNext_MovesToNextMatch_WrapsAround()
{
    var harness = TestEditorContext.Create("hello\nworld\nhello");
    harness.FindState.Change(searchString: "hello");
    
    harness.FindModel.FindNext(); // 移动到第一个 hello
    harness.AssertCursor(1, 1, 1, 6);
    
    harness.FindModel.FindNext(); // 移动到第二个 hello
    harness.AssertCursor(3, 1, 3, 6);
    
    harness.FindModel.FindNext(); // 循环到第一个 hello
    harness.AssertCursor(1, 1, 1, 6);
}

[Fact]
public void FindPrevious_MovesToPreviousMatch_WrapsAround() { ... }

[Fact]
public void FindNext_WithNoMatches_DoesNotMoveCursor() { ... }
```

---

#### T3: Replace 单个匹配
**TS 参考**: `findTest('replace hello')`  
**验收标准**:
- 调用 `Replace()` 替换当前匹配
- 光标移动到下一个匹配
- 文本内容更新（集成 Batch #1 `ReplacePattern.BuildReplaceString()`）

**实现方法**:
```csharp
[Fact]
public void Replace_SingleMatch_UpdatesTextAndMovesToNext()
{
    var harness = TestEditorContext.Create("hello world\nhello again");
    harness.FindState.Change(searchString: "hello", replaceString: "hi");
    
    harness.FindModel.FindNext(); // 光标在第一个 hello
    harness.FindModel.Replace();
    
    harness.AssertText("hi world\nhello again");
    harness.AssertCursor(2, 1, 2, 6); // 光标移动到第二个 hello
}
```

---

#### T4: ReplaceAll
**TS 参考**: `findTest('replaceAll hello')`, `findTest('replaceAll bla')`  
**验收标准**:
- `ReplaceAll()` 替换所有匹配
- 匹配数更新为 0
- 文本内容完全替换

**实现方法**:
```csharp
[Fact]
public void ReplaceAll_ReplacesAllMatches()
{
    var harness = TestEditorContext.Create("hello world\nhello again\nHello!");
    harness.FindState.Change(searchString: "hello", replaceString: "hi", matchCase: false);
    
    var replaceCount = harness.FindModel.ReplaceAll();
    
    Assert.Equal(3, replaceCount);
    harness.AssertText("hi world\nhi again\nhi!");
    harness.AssertMatchCount(0);
}

[Fact]
public void ReplaceAll_WithSpecialChars_UsesReplacePattern()
{
    // Test with \t\n etc. (Batch #1 ReplacePattern integration)
    var harness = TestEditorContext.Create("bla bla");
    harness.FindState.Change(searchString: "bla", replaceString: "\\t\\n");
    
    harness.FindModel.ReplaceAll();
    harness.AssertText("\t\n \t\n");
}
```

---

#### T5: WholeWord 搜索（Word Boundary 集成）
**TS 参考**: `findTest('incremental find from beginning of file')` （wholeWord 部分）  
**验收标准**:
- `wholeWord = true` → 仅匹配完整单词（调用 `WordSeparators` 参数）
- `"hello"` 匹配 `"hello world"` 但不匹配 `"helloworld"`
- 验证 B2-INV Appendix B 的 WordSeparator 集成

**实现方法**:
```csharp
[Fact]
public void WholeWord_MatchesOnlyCompleteWords()
{
    var harness = TestEditorContext.Create("hello helloworld world");
    harness.FindState.Change(searchString: "hello", wholeWord: true);
    
    harness.AssertMatchCount(1); // 仅匹配 "hello"，不匹配 "helloworld"
    harness.AssertFindState(
        highlighted: new Range(1, 1, 1, 6),
        allMatches: new[] { new Range(1, 1, 1, 6) }
    );
}

[Fact]
public void WholeWord_PassesWordSeparatorsToSearchParams()
{
    // 验证 WordSeparators 参数传递（集成 SearchParams）
    var harness = TestEditorContext.Create("foo->bar");
    harness.FindState.Change(searchString: "foo", wholeWord: true);
    
    harness.AssertMatchCount(1); // "->" 是 separator
}
```

---

#### T6: Decoration 移除
**TS 参考**: `findTest('find model removes its decorations')`  
**验收标准**:
- 搜索字符串清空后，所有 decorations 移除
- Decoration 数量更新为 0

**实现方法**:
```csharp
[Fact]
public void FindModel_RemovesDecorationsWhenSearchCleared()
{
    var harness = TestEditorContext.Create("hello world");
    harness.FindState.Change(searchString: "hello");
    
    harness.AssertDecorationCount(1); // 1 个匹配 decoration
    
    harness.FindState.Change(searchString: ""); // 清空搜索
    harness.AssertDecorationCount(0); // decorations 已移除
}
```

---

#### T7: MatchesCount 更新
**TS 参考**: `findTest('find model updates state matchesCount')`  
**验收标准**:
- 搜索字符串变化时，`MatchCount` 属性更新
- 事件通知正确触发（`OnDidChangeSearchResults`）

**实现方法**:
```csharp
[Fact]
public void FindModel_UpdatesMatchesCount()
{
    var harness = TestEditorContext.Create("hello world hello");
    
    harness.FindState.Change(searchString: "hello");
    harness.AssertMatchCount(2);
    
    harness.FindState.Change(searchString: "world");
    harness.AssertMatchCount(1);
    
    harness.FindState.Change(searchString: "xyz");
    harness.AssertMatchCount(0);
}
```

---

### P1 – 进阶功能（5 个测试）

#### T8: Search Scope（搜索范围限制）
**TS 参考**: `findTest('find model next stays in scope')`, `findTest('issue #27083. search scope works even if it is a single line')`  
**验收标准**:
- 设置搜索范围（`searchScope`）后，仅在范围内搜索
- `FindNext/Prev` 不超出范围

**实现方法**:
```csharp
[Fact]
public void FindNext_StaysInScope()
{
    var harness = TestEditorContext.Create("hello\nworld\nhello\nworld");
    harness.FindState.Change(
        searchString: "hello",
        searchScope: new Range(1, 1, 2, 10) // 仅前两行
    );
    
    harness.AssertMatchCount(1); // 仅第一个 hello
    harness.FindModel.FindNext();
    harness.AssertCursor(1, 1, 1, 6); // 循环回第一个匹配（不超出范围）
}
```

---

#### T9: Regex 边界符（^ $ .* 等）
**TS 参考**: `findTest('find ^')`, `findTest('find $')`, `findTest('find .*')`  
**验收标准**:
- `^` 匹配行首
- `$` 匹配行尾
- `.*` 匹配整行
- 验证 multiline regex 支持（依赖 Batch #1 regex 基础）

**实现方法**:
```csharp
[Fact]
public void Find_Regex_LineStart() { ... } // ^ 匹配行首

[Fact]
public void Find_Regex_LineEnd() { ... } // $ 匹配行尾

[Fact]
public void Find_Regex_DotStar() { ... } // .* 匹配整行
```

---

#### T10: Multi-Selection 集成
**TS 参考**: `findTest('multi-selection find model next stays in scope (overlap)')`  
**验收标准**:
- 多光标场景下，FindNext 在每个选区内独立搜索
- 不超出各自选区范围

**实现方法**:
```csharp
[Fact]
public void MultiSelection_FindNext_StaysInEachScope()
{
    // 需要多光标支持（可能依赖 Batch #3 或简化实现）
    // 可选：若 Batch #2 无多光标支持，推迟此测试到 Batch #3
}
```

---

#### T11: PreservingCase Replace
**TS 参考**: `findTest('replaceAll preserving case')`  
**验收标准**:
- `preserveCase = true` 时，替换保持原匹配的大小写风格
- 依赖 Batch #1 `ReplacePattern.BuildReplaceStringWithCasePreserved()`

**实现方法**:
```csharp
[Fact]
public void ReplaceAll_PreservingCase()
{
    var harness = TestEditorContext.Create("Hello HELLO hello");
    harness.FindState.Change(
        searchString: "hello",
        replaceString: "world",
        preserveCase: true,
        matchCase: false
    );
    
    harness.FindModel.ReplaceAll();
    harness.AssertText("World WORLD world"); // 保持大小写风格
}
```

---

#### T12: Content Change 监听
**TS 参考**: `findTest('listens to model content changes')`  
**验收标准**:
- TextModel 内容变化时，FindModel 自动重新搜索
- Decorations 更新

**实现方法**:
```csharp
[Fact]
public void FindModel_ReactsToContentChanges()
{
    var harness = TestEditorContext.Create("hello world");
    harness.FindState.Change(searchString: "hello");
    harness.AssertMatchCount(1);
    
    harness.Model.ApplyEdits(new[] { new TextEdit(new Range(1, 7, 1, 12), "hello") }); // 替换 world → hello
    
    harness.AssertMatchCount(2); // 匹配数自动更新
}
```

---

### P2 – 边缘场景（3 个测试）

#### T13: Lookahead Regex（高级 regex）
**TS 参考**: `findTest('replace when search string has look ahed regex')`  
**验收标准**:
- 支持 lookahead/lookbehind regex（`(?=...)`）
- 替换时捕获组正确

**实现方法**:
```csharp
[Fact]
public void Replace_WithLookaheadRegex() { ... }
```

---

#### T14: Large File（性能）
**TS 参考**: `findTest('issue #32522 replaceAll with ^ on more than 1000 matches')`  
**验收标准**:
- 1000+ 匹配时 ReplaceAll 性能可接受
- 无死循环/超时

**实现方法**:
```csharp
[Fact]
public void ReplaceAll_LargeFile_Performs() { ... }
```

---

#### T15: Issue Regressions
**TS 参考**: `findTest('issue #19740 ...')`, `findTest('issue #18711 ...')`  
**验收标准**:
- 空字符串替换不崩溃
- 空捕获组返回空字符串（不是 `undefined`）

**实现方法**:
```csharp
[Fact]
public void ReplaceAll_WithEmptyString() { ... }

[Fact]
public void Replace_EmptyCapturingGroup_YieldsEmptyString() { ... }
```

---

## 2. DocUI Test Harness 草案

### 最小需求 API

为 `DocUIFindModelTests.cs` 设计轻量级 harness，适配 TS `withTestCodeEditor` 语义：

```csharp
// src/PieceTree.TextBuffer.Tests/DocUI/TestEditorContext.cs

public class TestEditorContext : IDisposable
{
    public ITextModel Model { get; }
    public FindReplaceState FindState { get; }
    public FindModel FindModel { get; }
    
    // 创建 harness（简化版 withTestCodeEditor）
    public static TestEditorContext Create(string initialText)
    {
        var model = CreateTextModel(initialText);
        var findState = new FindReplaceState();
        var findModel = new FindModelBoundToEditorModel(model, findState);
        return new TestEditorContext(model, findState, findModel);
    }
    
    // 光标操作
    public void SetCursorPosition(int line, int column) { ... }
    public Range GetCursorSelection() { ... }
    
    // Decoration 验证
    public void AssertDecorationCount(int expected)
    {
        var decorations = Model.GetAllDecorations()
            .Where(d => d.Options.ClassName == "findMatch" || d.Options.ClassName == "currentFindMatch");
        Assert.Equal(expected, decorations.Count());
    }
    
    public string GetCurrentMatchText()
    {
        var currentMatch = Model.GetAllDecorations()
            .FirstOrDefault(d => d.Options.ClassName == "currentFindMatch");
        return currentMatch != null ? Model.GetValueInRange(currentMatch.Range) : null;
    }
    
    // Find 状态验证
    public void AssertFindState(Range cursor, Range highlighted, Range[] allMatches)
    {
        Assert.Equal(cursor, GetCursorSelection());
        
        var currentMatches = Model.GetAllDecorations()
            .Where(d => d.Options.ClassName == "currentFindMatch")
            .Select(d => d.Range)
            .ToArray();
        Assert.Equal(new[] { highlighted }, currentMatches);
        
        var allDecorations = Model.GetAllDecorations()
            .Where(d => d.Options.ClassName == "findMatch" || d.Options.ClassName == "currentFindMatch")
            .Select(d => d.Range)
            .OrderBy(r => r.StartLineNumber).ThenBy(r => r.StartColumn)
            .ToArray();
        Assert.Equal(allMatches, allDecorations);
    }
    
    // 文本验证
    public void AssertText(string expected) => Assert.Equal(expected, Model.GetValue());
    
    // 匹配数验证
    public void AssertMatchCount(int expected) => Assert.Equal(expected, FindModel.MatchCount);
    
    // 光标验证
    public void AssertCursor(int startLine, int startCol, int endLine, int endCol)
    {
        var cursor = GetCursorSelection();
        Assert.Equal(new Range(startLine, startCol, endLine, endCol), cursor);
    }
    
    public void Dispose()
    {
        FindModel?.Dispose();
        FindState?.Dispose();
        Model?.Dispose();
    }
}
```

### 非需求（推迟至 Batch #3）

以下功能**不在 Batch #2 harness 范围**：

- ❌ 完整 editor services（`ContextKeyService`、`ClipboardService`）
- ❌ FindWidget DOM
- ❌ Command layer（`FindController` actions）
- ❌ EditorOption 系统（直接传 `wordSeparators` 字符串）

---

## 3. Word Boundary 测试矩阵

补充 B2-INV 建议的 Tier A 测试，创建新文件 `src/PieceTree.TextBuffer.Tests/WordBoundaryTests.cs`：

### ASCII Separators（5 个测试）

```csharp
[Fact]
public void WordBoundary_Space() { ... } // " " 是 separator

[Fact]
public void WordBoundary_Tab() { ... } // "\t" 是 separator

[Fact]
public void WordBoundary_Punctuation() { ... } // USUAL_WORD_SEPARATORS 中的标点

[Fact]
public void WordBoundary_MultiCharOperator() 
{
    // "->" "::" "==" 应在运算符处分割
    var text = "foo->bar";
    var classifier = new WordCharacterClassifier("->", new string[0]);
    
    Assert.True(classifier.IsValidMatch(text, 0, 3)); // "foo" 是完整单词
    Assert.False(classifier.IsValidMatch(text, 0, 8)); // "foo->bar" 不是完整单词
}

[Fact]
public void WordBoundary_StartAndEndOfString() { ... } // 字符串边界
```

### Unicode 边界（3 个测试）

```csharp
[Fact]
public void WordBoundary_Emoji() 
{
    var text = "hello👋world";
    // Emoji 应视为 separator
}

[Fact]
public void WordBoundary_SurrogatePairs() 
{
    // 测试 surrogate pair 处理（依赖 UnicodeUtility）
}

[Fact]
public void WordBoundary_CombiningDiacritics() 
{
    var text = "café"; // é 是组合字符
    // 应视为单个单词
}
```

### CJK/Thai（2 个测试 – 文档化限制）

```csharp
[Fact(Skip = "Intl.Segmenter parity not implemented (requires ICU4N)")]
public void WordBoundary_CJK() 
{
    // CJK 分词（需 Intl.Segmenter 或 ICU4N）
    // 当前跳过，文档化为已知限制
}

[Fact(Skip = "Intl.Segmenter parity not implemented")]
public void WordBoundary_Thai() { ... }
```

---

## 4. 扩展 TextModelSearchTests.cs（WholeWord 场景）

在现有 `TextModelSearchTests.cs` 中新增 5 个测试：

```csharp
[Fact]
public void WholeWord_Regex_MatchesBoundaries() 
{
    // Regex + wholeWord: \w+ 应匹配 word boundaries
}

[Fact]
public void WholeWord_SimpleSearch_MatchesBoundaries() 
{
    // Simple search + wholeWord: "foo" 不匹配 "foobar"
}

[Fact]
public void WholeWord_CaseInsensitive_MatchesBoundaries() 
{
    // Case-insensitive + wholeWord: "FOO" 仅在边界匹配 "Foo"
}

[Fact]
public void WholeWord_Multiline_PreservesBoundaries() 
{
    // Multiline + wholeWord: CRLF 补偿不破坏边界
}

[Fact]
public void WholeWord_NoSeparators_MatchesAll() 
{
    // wordSeparators = null 时，wholeWord 应 disabled
}
```

---

## 5. TestMatrix.md 更新内容

在 `src/PieceTree.TextBuffer.Tests/TestMatrix.md` 中新增 FindModel 相关行：

### TS Test Alignment Map 新增行

```markdown
| DocUIFindModelTests | DocUI find model binding + overlays | [ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts](../../ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts) | B | Planned | Batch #2 – 目标 15 个核心测试（增量搜索、findNext/Prev、replace、wholeWord、decorations）；需创建 `TestEditorContext` harness |
| WordBoundaryTests | Word separator + boundary validation | [ts/src/vs/editor/common/core/wordCharacterClassifier.ts](../../ts/src/vs/editor/common/core/wordCharacterClassifier.ts) | A | Planned | Batch #2 – 10 个测试覆盖 ASCII separators、Unicode、multi-char operators；文档化 CJK/Thai 限制（无 Intl.Segmenter） |
```

### Test Baseline 新增预留行

```markdown
| Date | Phase | Baseline | Duration | Notes |
| --- | --- | --- | --- | --- |
| 2025-11-27 (expected) | Batch #2 – FindModel | TBD/TBD | TBD | B2-003 完成后更新；预计新增 20 个测试（15 FindModel + 5 WordBoundary） |
```

### Targeted Commands 新增

```markdown
### Batch #2 (FindModel) Validation Commands

| Command | Purpose | Notes |
| --- | --- | --- |
| `PIECETREE_DEBUG=0 dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --nologo --logger "trx;LogFileName=TestResults/batch2-full.trx"` | Full-suite baseline before/after Batch #2 drops | 预计 142 → 162 测试（+20） |
| `PIECETREE_DEBUG=0 dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~DocUIFindModelTests" --logger "trx;LogFileName=TestResults/batch2-findmodel.trx"` | FindModel 专项测试（15 个核心场景） | 验证增量搜索、replace、wholeWord、decorations |
| `PIECETREE_DEBUG=0 dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TestBuffer.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~WordBoundaryTests" --logger "trx;LogFileName=TestResults/batch2-wordboundary.trx"` | Word boundary 专项测试（10 个边界场景） | 验证 ASCII/Unicode separators、multi-char operators |
```

---

## 6. QA Expectations（给 Porter-CS 的 API 契约）

### For Porter-CS (B2-001/002)

#### API 契约
**FindReplaceState** 需提供字段与事件：
```csharp
public class FindReplaceState : IDisposable
{
    // 字段
    public string SearchString { get; set; }
    public string ReplaceString { get; set; }
    public bool IsRegex { get; set; }
    public bool MatchCase { get; set; }
    public bool WholeWord { get; set; }
    public bool PreserveCase { get; set; }
    public Range? SearchScope { get; set; } // 可选：搜索范围
    
    // 事件
    public event EventHandler OnDidChangeSearchString;
    public event EventHandler OnDidChangeReplaceString;
    public event EventHandler OnDidChangeSearchFlags;
    
    // 方法
    public void Change(string? searchString = null, string? replaceString = null, 
                       bool? isRegex = null, bool? matchCase = null, 
                       bool? wholeWord = null, bool? preserveCase = null,
                       Range? searchScope = null);
}
```

**FindDecorations** 需提供方法：
```csharp
public class FindDecorations : IDisposable
{
    // 设置当前匹配高亮
    public void SetCurrentMatch(Range range);
    
    // 设置所有匹配高亮
    public void SetAllMatches(Range[] ranges);
    
    // 清除所有高亮
    public void ClearDecorations();
    
    // 获取当前 decorations（测试用）
    public IReadOnlyList<IModelDecoration> GetCurrentDecorations();
}
```

**FindModel** 需提供方法与属性：
```csharp
public class FindModel : IDisposable
{
    // 构造函数
    public FindModel(ITextModel model, FindReplaceState state);
    
    // 搜索方法
    public Range? FindNext();
    public Range? FindPrevious();
    public Range[] FindMatches();
    
    // 替换方法
    public bool Replace();
    public int ReplaceAll();
    
    // 状态属性
    public int MatchCount { get; }
    public int CurrentMatchIndex { get; }
    
    // 事件通知
    public event EventHandler OnDidChangeSearchResults;
    public event EventHandler OnDidChangeDecorations;
    
    // 测试钩子（可选）
    public Range[] GetCurrentMatches(); // 供测试验证内部状态
}
```

#### Word Boundary 集成
- **FindModel.FindNext/Prev/Matches** 内部调用：
  ```csharp
  var searchParams = new SearchParams(
      searchString: state.SearchString,
      isRegex: state.IsRegex,
      matchCase: state.MatchCase,
      wordSeparators: state.WholeWord ? DefaultWordSeparators : null
  );
  var searchData = searchParams.ParseSearchRequest();
  var results = textModel.FindMatches(searchData, ...);
  ```
- **DefaultWordSeparators** 定义：
  ```csharp
  public const string DefaultWordSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";
  ```

---

### For QA-Automation (B2-003)

#### Harness 创建顺序
1. **创建 `TestEditorContext.cs`**（参考 Batch #1 `DocUIReplaceController` 模式）
2. **实现 P0 测试**（7 个核心场景）→ 运行 `dotnet test` 确保通过
3. **实现 P1 测试**（5 个进阶场景）→ 扩展覆盖度
4. **（可选）实现 P2 测试**（3 个边缘场景）→ 若时间允许

#### 测试优先级执行顺序
```
Phase 1: P0 核心（7 个）→ 全部通过后交付 B2-003.1
Phase 2: P1 进阶（5 个）→ 全部通过后交付 B2-003.2
Phase 3: P2 边缘（3 个）→ 可选，时间允许时补充
```

#### Snapshot 可选
若时间允许，生成 Markdown snapshot 验证 FindModel 输出（类似 Batch #1 Markdown renderer）：
- **Before/After Search**：记录搜索前后文本 + decorations 状态
- **Replace Snapshot**：记录 replace 操作前后文本差异

**格式参考**：
```markdown
## Test: IncrementalFind_FromBeginningOfFile

### Initial State
```text
// my cool header
#include "cool.h"
```

### After Change(searchString: "H")
**Cursor**: Line 1, Column 12-13
**Highlighted**: [1:12-13]
**All Matches**: [1:12-13], [2:16-17], ...
```

---

## 7. 已知风险与缓解措施

### 风险 1：Harness 复杂度超预算 ⚠️
**描述**：TS `withTestCodeEditor` 提供丰富的 editor services（options、decorations、events），C# harness 可能需要较多 stub 代码。

**缓解措施**：
- 最小化 harness 范围：仅实现测试所需的最小 API（参考上文 `TestEditorContext` 草案）
- 参考 Batch #1 经验：`ReplacePatternTests.cs` 无需完整 editor context，直接调用 API
- 若超预算，分两阶段交付（P0 → P1）

**应对计划**：
- B2-003.1：创建最小 harness + 5 个 P0 测试（incremental find、findNext、replace、replaceAll、wholeWord）
- B2-003.2：扩展 harness + 剩余 P0/P1 测试

---

### 风险 2：Multi-Selection 依赖缺失 ⚠️
**描述**：T10（Multi-Selection FindNext）可能依赖多光标支持（Batch #2 可能无此功能）。

**缓解措施**：
- 将 T10 标记为 P1（可选）
- 若 Batch #2 无多光标 API，推迟此测试到 Batch #3（FindController 层）

**应对计划**：
- 若 Porter-CS B2-002 未实现多光标，QA 跳过 T10，在 handoff 中标注为"Deferred to Batch #3"

---

### 风险 3：WordCharacterClassifier 缓存未实现 ℹ️
**描述**：B2-INV 标注的 LRU cache（P2 优先级）可能未在 B2-002 中实现。

**影响**：低 - 仅影响重复搜索性能，不阻塞功能测试

**应对计划**：
- QA 测试不验证 cache（无需性能 benchmark）
- 若未来实现 cache，补充性能测试（Batch #4 性能专项）

---

## 8. 交付物确认

### 已创建文件
- [x] **`agent-team/handoffs/B2-QA-Result.md`**（本文档）

### 待更新文件（B2-003 执行时）
- [ ] **`src/PieceTree.TextBuffer.Tests/TestMatrix.md`**（新增 FindModel/WordBoundary 行 + 测试命令）
- [ ] **`src/PieceTree.TextBuffer.Tests/DocUI/TestEditorContext.cs`**（新建 harness）
- [ ] **`src/PieceTree.TextBuffer.Tests/DocUI/DocUIFindModelTests.cs`**（新建测试文件，15 个测试）
- [ ] **`src/PieceTree.TextBuffer.Tests/WordBoundaryTests.cs`**（新建测试文件，10 个测试）
- [ ] **`src/PieceTree.TextBuffer.Tests/TextModelSearchTests.cs`**（扩展 wholeWord 场景，5 个新测试）

### 已更新记忆文件
- [x] **`agent-team/members/qa-automation.md`**（见后续更新）

---

## 9. 下一步建议

### 给 Porter-CS（B2-001/002）
1. **B2-001 API 设计**：参考上文"QA Expectations"章节的 API 契约，创建 FindReplaceState/FindDecorations stubs
2. **B2-002 集成重点**：
   - `FindModel.FindNext/Prev` → 调用 `TextModelSearch.FindNextMatch` + `WordSeparators` 参数
   - `FindModel.Replace/ReplaceAll` → 集成 Batch #1 `ReplacePattern.BuildReplaceString()`
   - 事件通知：搜索状态变更时触发 `OnDidChangeSearchResults`、`OnDidChangeDecorations`

### 给 QA-Automation（B2-003）
1. **等待 B2-002 完成**：Porter-CS 交付 FindModel 实现后启动
2. **优先创建 harness**：先实现 `TestEditorContext.cs`（可复用于所有 FindModel 测试）
3. **逐步实现测试**：P0（7 个）→ P1（5 个）→ P2（3 个，可选）
4. **运行命令记录**：每阶段完成后运行专项过滤命令，记录 TRX 到 `TestResults/`

### 给 Planner
- **监控 B2-003 进度**：若 harness 创建超预算（>1 runSubAgent 轮次），触发分阶段交付
- **准备 Batch #3**：FindController 命令层（依赖 EditorAction/ContextKey services）

---

## 结语

Batch #2 测试矩阵已完成草拟，包含：
- **15 个核心测试场景**（P0/P1/P2 分级）
- **DocUI Test Harness 草案**（最小化 API）
- **Word Boundary 测试矩阵**（10 个测试）
- **QA Expectations**（给 Porter-CS 的 API 契约）
- **TestMatrix.md 更新内容**（新增 FindModel/WordBoundary 行 + 测试命令）

所有准备工作已就绪，等待 Porter-CS 完成 B2-002 后启动 B2-003 测试迁移。

**QA-Automation 待命，等待 B2-002 完成通知。**
