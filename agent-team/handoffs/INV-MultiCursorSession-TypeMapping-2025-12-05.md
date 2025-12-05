# C# 类型系统调研报告：MultiCursorSession 移植

**日期**: 2025-12-05  
**调查员**: InvestigatorTS  
**TS 源码**: `ts/src/vs/editor/contrib/multicursor/browser/multicursor.ts` L275-456

---

## 调查摘要

已调研 PieceTreeSharp 现有类型系统，为 `MultiCursorSession` 移植提供类型适配方案。发现 C# 端已存在初步实现但存在**命名空间引用错误**（引用不存在的 `PieceTree.TextBuffer.Find` 命名空间）。

---

## 已存在的类型

| TS 类型 | C# 类型 | 文件路径 | 状态 |
|---------|---------|----------|------|
| `Selection` | `Selection` (readonly struct) | [src/TextBuffer/Core/Selection.cs](../../src/TextBuffer/Core/Selection.cs) | ✅ 完整 |
| `Position` | `TextPosition` (readonly record struct) | [src/TextBuffer/TextPosition.cs](../../src/TextBuffer/TextPosition.cs) | ✅ 完整 |
| `Range` | `Range` (readonly partial record struct) | [src/TextBuffer/Core/SearchTypes.cs](../../src/TextBuffer/Core/SearchTypes.cs) | ✅ 完整 |
| `FindMatch` | `FindMatch` (sealed class) | [src/TextBuffer/Core/SearchTypes.cs](../../src/TextBuffer/Core/SearchTypes.cs) | ✅ 完整 |
| `SearchParams` | `SearchParams` (sealed class) | [src/TextBuffer/Core/SearchTypes.cs](../../src/TextBuffer/Core/SearchTypes.cs) | ✅ 完整 |
| `FindReplaceState` | `FindReplaceState` (class) | [src/TextBuffer/DocUI/FindReplaceState.cs](../../src/TextBuffer/DocUI/FindReplaceState.cs) | ✅ 完整 |
| `FindModel` | `FindModel` (class) | [src/TextBuffer/DocUI/FindModel.cs](../../src/TextBuffer/DocUI/FindModel.cs) | ✅ 完整 |
| `TextModel.findNextMatch` | `TextModel.FindNextMatch()` | [src/TextBuffer/TextModel.cs](../../src/TextBuffer/TextModel.cs) | ✅ 多重载 |
| `TextModel.findPreviousMatch` | `TextModel.FindPreviousMatch()` | [src/TextBuffer/TextModel.cs](../../src/TextBuffer/TextModel.cs) | ✅ 多重载 |
| `TextModel.findMatches` | `TextModel.FindMatches()` | [src/TextBuffer/TextModel.cs](../../src/TextBuffer/TextModel.cs) | ✅ 多重载 |
| `MultiCursorSessionResult` | `MultiCursorSessionResult` (record) | [src/TextBuffer/Cursor/MultiCursorSessionResult.cs](../../src/TextBuffer/Cursor/MultiCursorSessionResult.cs) | ✅ 已存在 |
| `ScrollType` | `ScrollType` (enum) | [src/TextBuffer/Cursor/MultiCursorSessionResult.cs](../../src/TextBuffer/Cursor/MultiCursorSessionResult.cs) | ✅ 已存在 |

---

## 类型映射细节

### Selection (✅ 完整)
- **TS**: `class Selection extends Range` with `anchor`, `active`, direction
- **C#**: `readonly struct Selection` with `Anchor`, `Active` (both `TextPosition`)
- **API 对齐**:
  - `SelectionStart` / `SelectionEnd` → 对应 TS `selectionStart` / `selectionStartPosition`
  - `Direction` → `SelectionDirection.LTR` / `RTL`
  - `IsEmpty` → 检查 anchor == active
  - `ToRange()` → 转换为 Range
  - 静态工厂: `FromPositions()`, `FromRange()`, `CreateWithDirection()`

### Position / TextPosition (✅ 完整)
- **TS**: `class Position` with `lineNumber`, `column` (1-based)
- **C#**: `readonly record struct TextPosition(int LineNumber, int Column)`
- **API 对齐**:
  - `With()` / `Delta()` → 创建新实例
  - `IsBefore()` / `IsBeforeOrEqual()` → 比较方法
  - `CompareTo()` → 实现 `IComparable<TextPosition>`

### Range (✅ 完整)
- **TS**: `class Range` with `startLineNumber`, `startColumn`, `endLineNumber`, `endColumn`
- **C#**: `readonly partial record struct Range { TextPosition Start; TextPosition End; }`
- **API 对齐**: 构造器自动规范化 (Start <= End)

### FindMatch (✅ 完整)
- **TS**: `interface FindMatch { range: Range; matches: string[] | null; }`
- **C#**: `sealed class FindMatch { Range Range; string[]? Matches; }`

---

## 问题识别

### 🚨 关键问题：不存在的命名空间引用

[MultiCursorSession.cs](../../src/TextBuffer/Cursor/MultiCursorSession.cs) 第 5 行：
```csharp
using PieceTree.TextBuffer.Find;  // ❌ 此命名空间不存在！
```

**影响**:
- 文件引用了不存在的 `FindModel` (从 `Find` 命名空间)
- 实际 `FindModel` 位于 `PieceTree.TextBuffer.DocUI` 命名空间

### 类型名称不匹配

| 现有代码使用 | 实际 C# 类型 | 需要修改 |
|------------|------------|---------|
| `Position` | `TextPosition` | ✅ 需要 using alias 或改名 |
| `TextRange` | `Range` | ✅ 需要修正 |
| `FindModel` (from Find) | `FindModel` (from DocUI) | ✅ 需要修正 namespace |

---

## 缺失的类型/方法

### 1. FindModel.FindNext / FindPrevious (适配需求)

现有 `DocUI.FindModel` 有：
- `FindNext()` / `FindPrevious()` → 无参数，使用内部状态

MultiCursorSession 需要：
- `FindNext(Position searchStart, bool wrapAround)` → 从指定位置搜索
- `FindPrevious(Position searchStart, bool wrapAround)` → 从指定位置反向搜索

**建议**: 为 `FindModel` 添加带位置参数的搜索重载，或创建简化的搜索适配器。

### 2. FindModel.FindAll()

现有 `FindModel.SelectAllMatches()` 返回 `Selection[]`

MultiCursorSession 调用 `SelectAll()` 期望返回 `IReadOnlyList<FindMatch>`

**建议**: 添加 `FindAll()` 方法返回 FindMatch 列表。

### 3. GetWordAtPosition (部分实现)

MultiCursorSession 中有简化版本（仅支持字母数字+下划线）。

完整版本需要：
- 使用 `WordCharacterClassifier` 
- 支持 `editor.wordSeparators` 配置

---

## 适配建议

### 优先级 1: 修复命名空间引用 (立即可做)

```csharp
// 修改 MultiCursorSession.cs 第 5 行
- using PieceTree.TextBuffer.Find;
+ using PieceTree.TextBuffer.DocUI;
+ using Range = PieceTree.TextBuffer.Core.Range;
+ using Selection = PieceTree.TextBuffer.Core.Selection;
```

### 优先级 2: 修复类型名称 (立即可做)

```csharp
// Position → TextPosition
// TextRange → Range
```

### 优先级 3: FindModel 适配器 (~2h)

创建适配接口或扩展方法：

```csharp
// Option A: 扩展方法
public static class FindModelExtensions
{
    public static FindMatch? FindNext(this FindModel model, TextPosition searchStart, bool wrapAround);
    public static FindMatch? FindPrevious(this FindModel model, TextPosition searchStart, bool wrapAround);
    public static IReadOnlyList<FindMatch> FindAll(this FindModel model);
}

// Option B: 轻量级适配器（推荐）
public sealed class MultiCursorFindAdapter
{
    private readonly TextModel _model;
    private readonly string _searchText;
    private readonly bool _matchCase;
    private readonly string? _wordSeparators;
    
    public FindMatch? FindNext(TextPosition position);
    public FindMatch? FindPrevious(TextPosition position);
    public IReadOnlyList<FindMatch> FindAll();
}
```

### 优先级 4: GetWordAtPosition 完善 (可延后)

集成 `WordCharacterClassifier`（已存在于 `src/TextBuffer/Cursor/WordCharacterClassifier.cs`）。

---

## 实施路线图

| 阶段 | 任务 | 工时估计 | 依赖 |
|-----|------|---------|------|
| 1 | 修复命名空间和类型引用 | 30min | 无 |
| 2 | 创建 `MultiCursorFindAdapter` | 1.5h | TextModel.FindNextMatch 已存在 |
| 3 | 更新 MultiCursorSession 使用适配器 | 1h | 阶段 2 |
| 4 | 添加单元测试 | 2h | 阶段 3 |
| **总计** | | **~5h** | |

---

## Porter-CS 建议

1. **先修复编译问题**：命名空间引用错误优先修复
2. **直接使用 TextModel.FindNextMatch**：不必通过 FindModel，可简化为直接调用 TextModel API
3. **参考 TS 实现细节**：
   - `_getNextMatch()` / `_getPreviousMatch()` 调用 `editor.getModel().findNextMatch()`
   - `selectAll()` 调用 `editorModel.findMatches()`

---

## QA-Automation 建议

测试用例应覆盖：

1. **基础功能**
   - `AddSelectionToNextFindMatch` 正确添加下一个匹配
   - `MoveSelectionToNextFindMatch` 移动（不添加）到下一个匹配
   - `AddSelectionToPreviousFindMatch` / `MoveSelectionToPreviousFindMatch` 反向操作

2. **边界条件**
   - 空选区扩展为单词
   - 文档末尾 wrap-around 到开头
   - 无匹配时返回 null
   - 当前匹配被消费后清空 (`CurrentMatch = null`)

3. **与 Find 系统集成**
   - `wholeWord` 正确传递
   - `matchCase` 正确传递
   - 搜索范围限制 (searchScope)

---

*Investigator-TS @ 2025-12-05*
