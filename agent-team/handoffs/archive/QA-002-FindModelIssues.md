# QA-002: FindModel 测试失败分析

**日期**: 2025-11-22  
**负责人**: QA-Automation  
**状态**: 部分修复，需要 Porter-CS 继续处理

## 测试状态

**当前**: 168/186 通过 (90.3%)  
**上一次**: 168/186 通过  
**目标**: ≥35/39 FindModel 测试通过

## 已修复的问题

### B2-FIX-001: FindDecorations 计数错误

**问题**: 当搜索参数改变时，旧的高亮装饰没有被清除，导致装饰计数比预期多 1。

**症状**:
```
FindDecorations count mismatch: expected 5, actual 6
Expected: [6,14,6,19], [6,27,6,32], [7,14,7,19], [8,14,8,19], [9,14,9,19]
Actual: [1,12,1,14], [6,14,6,19], [6,27,6,32], [7,14,7,19], [8,14,8,19], [9,14,9,19]
```

**根本原因**: 在 `FindDecorations.Set()` 方法中，当替换所有装饰时，没有重置 `_highlightedDecorationId`，导致旧的高亮装饰仍然存在。

**修复**: 在 `FindDecorations.Set()` 中添加：
```csharp
// Reset highlighted decoration ID since we replaced all decorations
_highlightedDecorationId = null;
```

**文件**: `src/PieceTree.TextBuffer/DocUI/FindDecorations.cs` (line 210)

## 未解决的问题

### B2-BUG-002: 搜索参数改变后光标位置不正确

**问题**: 当搜索参数（searchString, matchCase, wholeWord, searchScope）改变时，光标没有正确跳转到第一个匹配位置。

**症状**:
```
Cursor mismatch: expected [6,14,6,19], actual [8,14,8,19]
```

**场景**: 
1. 设置搜索范围为第 8-10 行
2. 搜索 "hello" (case-sensitive)
3. 当前匹配在 [8,14,8,19]（范围内的第一个匹配）
4. 移除搜索范围
5. **预期**: 跳到全局第一个匹配 [6,14,6,19]
6. **实际**: 停留在 [8,14,8,19]

**已尝试的修复**:

1. 在 `FindModel.OnStateChanged()` 中重置 `_currentPosition = (1,1)`
2. 在 `FindReplaceState.ChangeMatchInfo()` 中添加 `clearCurrentMatch` 参数
3. 在 `FindModel.Research()` 中清除 `CurrentMatch` 并简化逻辑

**调试发现**:

通过调试发现，在移除搜索范围后调用 `MoveToNextMatch()` 时：
- `_currentPosition = (1,1)` ✓ (正确重置)
- `allMatches = [1,12], [2,16], [6,14], [6,27], [7,14], [8,14], [9,14]` ✗ (包含错误匹配)

匹配列表中包含 `[1,12]` 和 `[2,16]`，这些不应该匹配 "hello"：
- `[1,12]` 对应 "// my cool header" 中的 "he"
- `[2,16]` 对应 `"#include \"cool.h\""` 中的 "h"

**根本原因（推测）**:

问题可能在于：
1. `FindModel.FindMatches()` 返回了错误的匹配结果
2. 或者 `TextModel.FindMatches()` 的搜索逻辑有 bug
3. 或者搜索参数没有正确传递到底层搜索引擎

**需要进一步调查**:
- 检查 `TextModel.FindMatches()` 的实现
- 验证搜索参数（searchString, matchCase, wholeWord）是否正确传递
- 检查正则表达式构建逻辑是否正确

**影响范围**: 所有 18 个失败的 FindModel 测试都是同类问题

## 失败测试列表

所有 18 个失败测试都来自 `FindModelTests`:

1. Test01_IncrementalFindFromBeginningOfFile
2. Test04_FindModelReactsToPositionChange
3. Test05_FindModelNext
4. Test06_FindModelNextStaysInScope
5. Test12_FindModelNextPrevRespectsCursorPosition
6. Test16_Find_DotStar
7. Test17_FindNext_CaretDotStarDollar
8. Test20_ReplaceHello
9. Test21_ReplaceBla
10. Test22_ReplaceAllHello
11. Test27_ListensToModelContentChanges
12. Test30_Issue1914_NPEWhenThereIsOnlyOneFindMatch
13. Test31_ReplaceWhenSearchStringHasLookAheadRegex
14. Test32_ReplaceWhenSearchStringHasLookAheadRegexAndCursorIsAtLastMatch
15. Test34_ReplaceWhenSearchStringHasLookAheadRegexAndReplaceStringHasCaptureGroups
16. Test39_Issue32522_ReplaceAllWithCaretOnMoreThan1000Matches
17. Test42_Issue3516_ControlBehaviorOfNextOperationsNotLoopingBackToBeginning
18. Test43_Issue3516_ControlBehaviorOfNextOperationsLoopingBackToBeginning

## 修改的文件

1. `src/PieceTree.TextBuffer/DocUI/FindDecorations.cs`
   - 添加 `_highlightedDecorationId = null` 在 `Set()` 方法中

2. `src/PieceTree.TextBuffer/DocUI/FindModel.cs`
   - 在 `OnStateChanged()` 中重置 `_currentPosition`
   - 简化 `Research()` 方法逻辑

3. `src/PieceTree.TextBuffer/DocUI/FindReplaceState.cs`
   - 在 `ChangeMatchInfo()` 中添加 `clearCurrentMatch` 参数

4. `src/PieceTree.TextBuffer.Tests/DocUI/TestEditorContext.cs`
   - 改进错误消息，显示预期和实际的装饰列表

## 建议

1. **立即处理**: Porter-CS 应调查 `TextModel.FindMatches()` 的搜索逻辑，特别是：
   - 搜索参数的处理
   - 正则表达式的构建
   - 匹配结果的过滤

2. **验证**: 创建单元测试直接测试 `TextModel.FindMatches()` 的输入输出

3. **回归测试**: 修复后运行完整测试套件确认没有破坏其他功能

## 下一步

移交给 Porter-CS 处理 B2-BUG-002。
