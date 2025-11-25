# B2-003 Task Brief – TS Test Migration (findModel.test.ts → C#)

## 你的角色
QA-Automation（质量保证自动化工程师）

## 记忆文件位置
- `agent-team/members/qa-automation.md`
- 汇报前**必须更新**记忆文件，记录本次任务成果与下一步

## 任务目标
**直接移植** TS 原版 `findModel.test.ts` 的 **43 个测试用例**到 C#，确保 FindModel 行为与 TS 完全对齐。

## 前置条件
- B2-002 已完成（`FindModel` 核心逻辑实现）
- TS 源文件：`ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts`（2387 行，43 个测试）
- 已读取 `agent-team/handoffs/B2-QA-TaskBrief.md`（测试清单已更新为 43 个 TS 原版测试）

## 执行任务

### 1. 创建 Test Harness（适配 TS `withTestCodeEditor`）
**路径**：`src/PieceTree.TextBuffer.Tests/DocUI/TestEditorContext.cs`

**核心功能**（参考 TS `findModel.test.ts` L34-57）：
```csharp
public class TestEditorContext : IDisposable
{
    public ITextModel Model { get; }
    public FindReplaceState State { get; }
    public FindModel FindModel { get; }
    
    private TestEditorContext(string[] lines)
    {
        // 1. 创建 TextModel（模拟 TS withTestCodeEditor）
        Model = new TextModel(string.Join("\n", lines));
        
        // 2. 创建 FindReplaceState
        State = new FindReplaceState();
        
        // 3. 创建 FindModel（绑定到 Model）
        FindModel = new FindModel(Model, State);
    }
    
    public static void RunTest(string[] lines, Action<TestEditorContext> callback)
    {
        using var ctx = new TestEditorContext(lines);
        callback(ctx);
    }
    
    // 辅助方法（参考 TS _getFindState / assertFindState）
    public void AssertFindState(
        int[] cursor,              // [lineNumber, column, lineNumber, column]
        int[]? highlighted,        // currentFindMatch 范围（nullable）
        int[][] findDecorations    // 所有 findMatch 范围
    )
    {
        // 1. 验证光标位置（Model.GetSelection()）
        // 2. 验证 currentFindMatch 装饰
        // 3. 验证所有 findMatch 装饰
    }
    
    public FindDecorationsState GetFindState()
    {
        // 查询 Model.GetAllDecorations()
        // 返回 { Highlighted: Range[], FindDecorations: Range[] }
    }
}
```

### 2. 移植 43 个 TS 测试用例
**路径**：`src/PieceTree.TextBuffer.Tests/DocUI/DocUIFindModelTests.cs`

**测试模板**（参考 TS `findTest` 函数）：
```csharp
[Fact]
public void IncrementalFindFromBeginningOfFile()
{
    var textArr = new[]
    {
        "// my cool header",
        "#include \"cool.h\"",
        "#include <iostream>",
        "",
        "int main() {",
        "    cout << \"hello world, Hello!\" << endl;",
        "    cout << \"hello world again\" << endl;",
        "    cout << \"Hello world again\" << endl;",
        "    cout << \"helloworld again\" << endl;",
        "}",
        "// blablablaciao",
        ""
    };
    
    TestEditorContext.RunTest(textArr, (ctx) =>
    {
        // 移植 TS 测试逻辑...
        ctx.State.Change(new FindReplaceStateChanges
        {
            SearchString = "hello",
            IsRegex = false,
            IsCaseSensitive = false
        }, moveCursor: false);
        
        ctx.FindModel.FindNext();
        
        ctx.AssertFindState(
            cursor: new[] { 6, 12, 6, 17 },
            highlighted: new[] { 6, 12, 6, 17 },
            findDecorations: new[]
            {
                new[] { 6, 12, 6, 17 },
                new[] { 6, 27, 6, 32 },
                new[] { 7, 12, 7, 17 },
                new[] { 9, 12, 9, 22 }
            }
        );
    });
}
```

**完整测试清单**（43 个，按 TS 顺序移植）：
```
1. 'incremental find from beginning of file'
2. 'find model removes its decorations'
3. 'find model updates state matchesCount'
4. 'find model reacts to position change'
5. 'find model next'
6. 'find model next stays in scope'
7. 'multi-selection find model next stays in scope (overlap)'  // TODO(Batch #3 多光标)
8. 'multi-selection find model next stays in scope'            // TODO(Batch #3 多光标)
9. 'find model prev'
10. 'find model prev stays in scope'
11. 'find model next/prev with no matches'
12. 'find model next/prev respects cursor position'
13. 'find ^'
14. 'find $'
15. 'find next ^$'
16. 'find .*'
17. 'find next ^.*$'
18. 'find prev ^.*$'
19. 'find prev ^$'
20. 'replace hello'
21. 'replace bla'
22. 'replaceAll hello'
23. 'replaceAll two spaces with one space'
24. 'replaceAll bla'
25. 'replaceAll bla with \\t\\n'
26. 'issue #3516: "replace all" moves page/cursor/focus/scroll to the place of the last replacement'
27. 'listens to model content changes'
28. 'selectAllMatches'                                           // TODO(Batch #3 多光标)
29. 'issue #14143 selectAllMatches should maintain primary cursor if feasible'  // TODO(Batch #3)
30. 'issue #1914: NPE when there is only one find match'
31. 'replace when search string has look ahed regex'
32. 'replace when search string has look ahed regex and cursor is at the last find match'
33. 'replaceAll when search string has look ahed regex'
34. 'replace when search string has look ahed regex and replace string has capturing groups'
35. 'replaceAll when search string has look ahed regex and replace string has capturing groups'
36. 'replaceAll when search string has multiline and has look ahed regex and replace string has capturing groups'
37. 'replaceAll preserving case'
38. 'issue #18711 replaceAll with empty string'
39. 'issue #32522 replaceAll with ^ on more than 1000 matches'
40. 'issue #19740 Find and replace capture group/backreference inserts `undefined` instead of empty string'
41. 'issue #27083. search scope works even if it is a single line'
42. 'issue #3516: Control behavior of "Next" operations (not looping back to beginning)'
43. 'issue #3516: Control behavior of "Next" operations (looping back to beginning)'
```

**分阶段实施**（如果时间不够）：
- **Phase 1（优先）**：移植前 30 个测试（跳过 multi-selection 相关的 7/8/28/29）
- **Phase 2（可选）**：移植后 13 个测试（issue 回归测试）
- **Batch #3**：补齐 multi-selection 测试（7/8/28/29）

### 3. 更新 TestMatrix.md
在 `src/PieceTree.TextBuffer.Tests/TestMatrix.md` 中更新 FindModel 行：

| Feature Suite | TS Source | C# Tests | Implementation Files | Status | Notes |
| --- | --- | --- | --- | --- | --- |
| **DocUI – Find Model** | `findModel.test.ts` | `DocUIFindModelTests` | `FindReplaceState.cs`, `FindModel.cs`, `FindDecorations.cs` | ✅ Complete | 39 个测试移植（43 个中跳过 4 个多光标测试，标记 TODO(Batch #3)） |

### 4. 运行测试并记录结果
```bash
# 全量测试
dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj \
  --logger "trx;LogFileName=TestResults/batch2-full.trx" --nologo

# FindModel 专项测试
dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj \
  --filter "FullyQualifiedName~DocUIFindModelTests" \
  --logger "trx;LogFileName=TestResults/batch2-findmodel.trx" --nologo
```

## 交付物清单
1. **新增文件**:
   - `src/PieceTree.TextBuffer.Tests/DocUI/TestEditorContext.cs`（harness）
   - `src/PieceTree.TextBuffer.Tests/DocUI/DocUIFindModelTests.cs`（39-43 个测试）
2. **更新文件**:
   - `src/PieceTree.TextBuffer.Tests/TestMatrix.md`（更新 FindModel 状态为 Complete）
3. **测试结果**:
   - 全量测试通过（156 → 195-199）
   - TRX 文件：`batch2-full.trx`、`batch2-findmodel.trx`
4. **汇报文档**: `agent-team/handoffs/B2-003-Result.md`
5. **记忆文件更新**: `agent-team/members/qa-automation.md`

## 输出格式
汇报时提供：
1. **测试统计**: 移植数量（39-43/43）、通过率、跳过数量（TODO(Batch #3)）
2. **Harness 设计**: `TestEditorContext` 的关键方法
3. **发现的问题**: FindModel 实现缺陷（如有）、需要修复的 bug
4. **下一步建议**: 给 Info-Indexer 和 DocMaintainer 的交付说明
5. **已更新记忆文件**: 确认更新了 `agent-team/members/qa-automation.md`

---
**执行前检查**：
- [ ] 读取 `agent-team/members/qa-automation.md` 获取上下文
- [ ] 读取 `B2-002-Result.md` 了解 FindModel API
- [ ] 逐行参考 TS 测试确保逻辑一致
- [ ] 汇报前更新记忆文件

开始执行！
