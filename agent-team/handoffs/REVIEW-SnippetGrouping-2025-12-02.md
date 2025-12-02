# Snippet Placeholder Grouping - TS Parity Review

**日期**: 2025-12-02  
**审阅者**: InvestigatorTS  
**状态**: **PASS WITH NOTES**

---

## 变更范围

`src/TextBuffer/Cursor/SnippetSession.cs` 新增：
- `_placeholderGroups` 字典
- `GroupPlaceholdersByIndex()` 方法
- `GetCurrentPlaceholderRanges()` / `GetPlaceholderRangesByIndex()` / `ComputePossibleSelections()` 方法
- 修改 `NextPlaceholder()` / `PrevPlaceholder()` 按组导航

## TS 参考

- `ts/src/vs/editor/contrib/snippet/browser/snippetSession.ts`
- `OneSnippet.computePossibleSelections()` (Lines 200-230)
- `OneSnippet._placeholderGroups` (Line 32)
- `groupBy()` constructor 调用 (Line 48)

---

## 审阅结论

| 检查项 | 状态 | 说明 |
|--------|------|------|
| 分组逻辑 | ✅ PASS | `GroupPlaceholdersByIndex()` 与 TS `groupBy()` 语义等价 |
| 导航语义 | ✅ PASS | `NextPlaceholder()`/`PrevPlaceholder()` 正确跳过同 index 项 |
| `computePossibleSelections()` | ✅ PASS | 正确跳过 final tabstop ($0) |
| Stickiness 设置 | ⚠️ NOTE | 仅使用 `NeverGrowsWhenTypingAtEdges`，缺少 active/inactive 切换 |

---

## 详细分析

### 1. 分组逻辑 ✅

**TS (Line 48):**
```typescript
this._placeholderGroups = groupBy(_snippet.placeholders, Placeholder.compareByIndex);
```

**C#:**
```csharp
private void GroupPlaceholdersByIndex()
{
    _placeholderGroups.Clear();
    foreach (var entry in _placeholders)
    {
        if (!_placeholderGroups.TryGetValue(entry.Index, out var group))
        {
            group = [];
            _placeholderGroups[entry.Index] = group;
        }
        group.Add(entry.Decoration);
    }
}
```

**结论**: 语义等价。`groupBy()` 返回 `Placeholder[][]`，C# 使用 `Dictionary<int, List<>>` 达到相同效果。

### 2. 导航语义 ✅

**TS `move(fwd)` (Lines 96-140):**
- `_placeholderGroupsIdx` 是**组索引**，而非 placeholder 索引
- 每次导航移动整个组，返回组内所有 placeholder 的 Selection

**C# `NextPlaceholder()`:**
```csharp
int currentIndex = _current >= 0 ? _placeholders[_current].Index : -1;
do {
    _current++;
} while (_current < _placeholders.Count && _placeholders[_current].Index == currentIndex);
```

**结论**: 正确。C# 在排序后的 `_placeholders` 列表上操作，跳过同 index 项等价于 TS 的组索引递增。

### 3. Stickiness 设置 ⚠️ NOTE

**TS 动态切换 (Lines 38-43, 134-150):**
```typescript
// 创建时用 inactive
const options = placeholder.isFinalTabstop ? OneSnippet._decor.inactiveFinal : OneSnippet._decor.inactive;

// move() 时切换到 active
accessor.changeDecorationOptions(id, placeholder.isFinalTabstop ? OneSnippet._decor.activeFinal : OneSnippet._decor.active);
```

**C# 当前实现:**
```csharp
// 始终使用 NeverGrowsWhenTypingAtEdges
Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
```

**影响**:
- **TS**: 活跃 placeholder 使用 `AlwaysGrowsWhenTypingAtEdges`，用户输入时 placeholder 范围会自动扩展
- **C#**: 所有 placeholder 都不会在输入时扩展

**建议**: 这是一个功能差距，但不影响 P1.5 分组导航的核心功能。可作为 P2 优化项追踪：
- 新增 `ActivatePlaceholder(int index)` 方法
- 在导航时调用 `_model.ChangeDecorationOptions()` 切换 stickiness

### 4. `computePossibleSelections()` ✅

**TS (Lines 200-230):**
```typescript
for (const placeholder of placeholdersWithEqualIndex) {
    if (placeholder.isFinalTabstop) { break; }
    // ...
}
```

**C#:**
```csharp
if (index == 0) { continue; } // Skip final tabstop
```

**结论**: 语义等价。TS 在循环内 `break`，C# 直接跳过 index=0 的组。

---

## Porter-CS 建议

1. **P1.5 可以合并** — 分组和导航逻辑正确
2. **P2 追踪项** — Stickiness 动态切换，创建 issue 追踪

## QA-Automation 建议

需要新增测试用例：
1. 多个同 index placeholder（mirrors）的导航测试
2. `ComputePossibleSelections()` 返回值验证
3. `GetCurrentPlaceholderRanges()` 多 mirror 场景

---

## Changefeed

- `#delta-2025-12-02-snippet-p1.5-grouping-review`
