# Snippet P1.5 Placeholder Grouping - Port Result

**日期**: 2025-12-02  
**执行者**: PorterCS  
**任务**: Implement Snippet Placeholder Grouping (P1.5)

---

## 1. 实现摘要

实现了 Snippet Placeholder Grouping 功能，使同 index 的多个占位符能够同步编辑。主要改动包括：
1. 添加 `_placeholderGroups` 字典按 index 分组占位符
2. 修改 `NextPlaceholder()`/`PrevPlaceholder()` 按 unique index 导航（跳过同 index 重复项）
3. 添加 `GetCurrentPlaceholderRanges()` 返回当前 index 的所有 ranges
4. 修复 placeholder decoration stickiness 为 `NeverGrowsWhenTypingAtEdges`（与 TS 对齐）

---

## 2. 文件变更

### 源代码

| 文件 | 变更说明 |
|------|----------|
| `src/TextBuffer/Cursor/SnippetSession.cs` | 添加 `_placeholderGroups` 字典，`GroupPlaceholdersByIndex()` 方法，`GetCurrentPlaceholderRanges()` / `GetPlaceholderRangesByIndex()` / `ComputePossibleSelections()` 方法，修改 `NextPlaceholder()`/`PrevPlaceholder()` 按组导航，修复 stickiness 设置 |
| `src/TextBuffer/Cursor/SnippetController.cs` | 添加 `GetCurrentPlaceholderRanges()` / `GetAllSelectionsForCurrentPlaceholder()` / `ComputePossibleSelections()` 辅助方法 |

### 测试代码

| 文件 | 变更说明 |
|------|----------|
| `tests/TextBuffer.Tests/SnippetControllerTests.cs` | 添加 8 个 P1.5 placeholder grouping 测试 |

---

## 3. TS 对齐说明

| TS 元素 | C# 实现 | 备注 |
|---------|---------|------|
| `OneSnippet._placeholderGroups` (L48) | `SnippetSession._placeholderGroups` | 使用 `Dictionary<int, List<ModelDecoration>>` 实现 |
| `groupBy(placeholders, Placeholder.compareByIndex)` | `GroupPlaceholdersByIndex()` | 在 `InsertSnippet` 末尾调用 |
| `OneSnippet.computePossibleSelections()` (L200-230) | `SnippetSession.ComputePossibleSelections()` | 返回 `IReadOnlyDictionary<int, IReadOnlyList<(TextPosition, TextPosition)>>` |
| `_placeholderGroupsIdx` 组索引遍历 | `NextPlaceholder()`/`PrevPlaceholder()` 跳过同 index | 确保导航按 unique index 进行 |
| `OneSnippet._decor.inactive` (L38) | `Stickiness = NeverGrowsWhenTypingAtEdges` | 确保编辑时 placeholder 正确移动/追踪 |

---

## 4. 测试结果

### Targeted Tests
```bash
export PIECETREE_DEBUG=0 && dotnet test --filter SnippetControllerTests --nologo
# Result: 29/29 (≈2s)
```

### Full Test Suite
```bash
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
# Result: 831/831 (826 pass + 5 skip, ≈100s)
```

---

## 5. 新增测试用例

| 测试名 | 描述 |
|--------|------|
| `SnippetInsert_SameIndexPlaceholders_GroupedCorrectly` | 验证同 index 占位符正确分组 |
| `SnippetInsert_SameIndexPlaceholders_DifferentDefaults` | 同 index 不同默认值分组 |
| `SnippetInsert_ThreeSameIndexPlaceholders` | 三个同 index 占位符分组 |
| `SnippetInsert_MixedPlaceholders_OnlyCurrentGrouped` | 混合 index 只返回当前组 |
| `GetAllSelectionsForCurrentPlaceholder_ReturnsTextRanges` | 返回 TextRange 格式 |
| `ComputePossibleSelections_ReturnsAllGroups` | 返回所有组 |
| `ComputePossibleSelections_ExcludesFinalTabstop` | 排除 $0 final tabstop |
| `PlaceholderGrouping_TracksEdits` | 编辑后 ranges 正确追踪 |

---

## 6. 已知差异

| 项目 | TS 原版 | C# 实现 | 原因 |
|------|---------|---------|------|
| 导航语义 | `_placeholderGroupsIdx` 是组索引 | `_current` 是 `_placeholders` 列表索引 | C# 复用现有数据结构，通过跳过同 index 实现等效行为 |

---

## 7. 遗留问题

无

---

## 8. Changefeed Anchor

`#delta-2025-12-02-snippet-p1.5`

---

## 9. 参考

- TS `snippetSession.ts`: Lines 30-50 (`OneSnippet._placeholderGroups` 初始化)
- TS `snippetSession.ts`: Lines 200-230 (`computePossibleSelections`)
- TS `snippetSession.ts`: Lines 34-42 (`_decor` stickiness 设置)
- Review: `agent-team/handoffs/REVIEW-2025-12-02.md` Section 2.3
