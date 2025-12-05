# [MultiCursorSelectionController] Port Result

## 实现摘要
实现 `MultiCursorSelectionController.cs` 控制器层，提供 MultiCursor 操作的对外 API。对齐 TS `multicursor.ts` L458-550，简化设计去除 FindController/Editor 依赖。

## 文件变更
| 文件 | 类型 | 描述 |
|------|------|------|
| `src/TextBuffer/Cursor/MultiCursorSelectionController.cs` | **NEW** | 控制器实现，~330 行 |

## TS 对齐说明
| TS 元素 | C# 实现 | 备注 |
|---------|---------|------|
| `MultiCursorSelectionController` (L458-550) | `MultiCursorSelectionController` | 简化版本 |
| `_beginSessionIfNeeded()` (L476-501) | `EnsureSession()` | 私有方法，管理 Session 生命周期 |
| `_endSession()` (L503-514) | `ResetSession()` | 公开方法，重置 Session |
| `addSelectionToNextFindMatch()` (L536-547) | `AddSelectionToNextFindMatch()` | 委托给 Session |
| `moveSelectionToNextFindMatch()` (L549-552) | `MoveSelectionToNextFindMatch()` | 委托给 Session |
| `addSelectionToPreviousFindMatch()` (L554-557) | `AddSelectionToPreviousFindMatch()` | 委托给 Session |
| `moveSelectionToPreviousFindMatch()` (L559-562) | `MoveSelectionToPreviousFindMatch()` | 委托给 Session |
| `selectAll()` (L564-598) | `SelectAllMatches()` | 包含 primary cursor 保持逻辑 |
| `_expandEmptyToWord()` (L516-524) | `GetWordAtPosition()` | 简化实现 |

## 简化设计
相比 TS 原版，我们的实现做了以下简化：

1. **无 FindController 集成** - 直接使用 `TextModel.FindMatches`
2. **无 Editor focus 检查** - 超出 TextBuffer 范围
3. **无 isDisconnectedFromFindController 逻辑** - 简化状态管理
4. **无 EditorDecorationsCollection** - 高亮由调用层处理
5. **无事件订阅** - 无 `onDidChangeCursorSelection`/`onDidBlurEditorText` 监听

## 核心 API

```csharp
public sealed class MultiCursorSelectionController
{
    public MultiCursorSelectionController(TextModel model, MultiCursorSelectionOptions? options = null);
    
    // 添加下一个匹配到选区 (Ctrl+D)
    public MultiCursorSessionResult? AddSelectionToNextFindMatch(IReadOnlyList<Selection> currentSelections);
    
    // 移动最后选区到下一个匹配 (Ctrl+K Ctrl+D)
    public MultiCursorSessionResult? MoveSelectionToNextFindMatch(IReadOnlyList<Selection> currentSelections);
    
    // Previous 方向
    public MultiCursorSessionResult? AddSelectionToPreviousFindMatch(IReadOnlyList<Selection> currentSelections);
    public MultiCursorSessionResult? MoveSelectionToPreviousFindMatch(IReadOnlyList<Selection> currentSelections);
    
    // 选中所有匹配 (Ctrl+Shift+L)
    public MultiCursorSessionResult? SelectAllMatches(IReadOnlyList<Selection> currentSelections);
    
    // Session 管理
    public void ResetSession();
    public string? CurrentSearchText { get; }
    public bool HasActiveSession { get; }
}
```

## 使用示例

```csharp
// 创建控制器
var controller = new MultiCursorSelectionController(model);

// 当前只有一个空选区（光标）
var selections = new List<Selection> { 
    new Selection(1, 5, 1, 5) // 光标在 "hello" 单词内
};

// 第一次 Ctrl+D：扩展到当前单词
var result1 = controller.AddSelectionToNextFindMatch(selections);
// result1.Selections = [Selection(1, 1, 1, 6)] // 选中 "hello"

// 第二次 Ctrl+D：添加下一个匹配
selections = result1.Selections.ToList();
var result2 = controller.AddSelectionToNextFindMatch(selections);
// result2.Selections = [Selection(1, 1, 1, 6), Selection(2, 1, 2, 6)] // 两个 "hello"
```

## 测试结果
- **Build**: `dotnet build src/TextBuffer/TextBuffer.csproj` → Success ✅
- **Full**: `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → **1142 pass + 9 skip** ✅

## 已知差异
1. **Word Detection** - 使用简化的 `IsWordChar()` 而非完整的 `WordCharacterClassifier`
2. **无 Wrap Around** - Session 的 wrap-around 逻辑在 `MultiCursorSession` 中实现
3. **无高亮** - TS 的 `SelectionHighlighter` 未移植

## 遗留问题
1. **完整 WordCharacterClassifier 集成** - 当前使用简化版本
2. **测试覆盖** - 需要添加 `MultiCursorSelectionControllerTests`
3. **SearchScope 支持** - `SelectAllMatches` 未实现 searchScope 参数

## Changefeed Anchor
`#delta-2025-12-05-multicursor-controller`

## 下一步
1. 添加 `MultiCursorSelectionControllerTests` 测试套件
2. 集成完整的 `CursorWordCharacterClassifier`
3. 考虑添加 SearchScope 参数支持
