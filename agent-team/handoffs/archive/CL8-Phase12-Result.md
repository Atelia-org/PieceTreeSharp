# CL8 Renderer Implementation - Phase 1 & 2 Result

**实施人:** Porter-CS (Felix Novak)  
**日期:** 2025-11-28  
**锚点:** [`#delta-2025-11-28-cl8-phase12`](../indexes/README.md#delta-2025-11-28-cl8-phase12)  
**来源计划:** [`CL8-Renderer-Plan.md`](./CL8-Renderer-Plan.md)

## 概要

完成 CL8-Renderer-Plan.md 中的 Phase 1 和 Phase 2，实现了 `DecorationOwnerIds` 语义修正和 `DecorationSearchOptions` 过滤支持。

## Phase 1: 修复 DecorationOwnerIds 语义

### 问题描述
- TS 中 `ownerId=0` 表示"不过滤"（Any），调用者习惯传 0
- C# 中原来 `Default=0` 被当作特定 owner，`Any=-1` 才表示"不过滤"
- 这导致与 TS 行为不一致

### 实施变更

#### `src/TextBuffer/Decorations/DecorationOwnerIds.cs`
```csharp
// Before:
public const int Default = 0;
public const int SearchHighlights = 1;
public const int Any = -1;
public static bool FiltersAllOwners(int ownerFilter)
    => ownerFilter == Any || ownerFilter == Default;

// After:
public const int Any = 0;  // Now means "no filter", matches TS
public const int SearchHighlights = 1;
internal const int FirstAllocatableOwnerId = 2;
public static bool FiltersAllOwners(int ownerFilter) => ownerFilter == Any;
```

#### `src/TextBuffer/TextModel.cs`
```csharp
// Before:
private int _nextDecorationOwnerId = DecorationOwnerIds.SearchHighlights + 1;

// After:
private int _nextDecorationOwnerId = DecorationOwnerIds.FirstAllocatableOwnerId;
```

### 关联更新
更新了以下文件中的 `DecorationOwnerIds.Default` 引用为 `DecorationOwnerIds.Any`:
- `src/TextBuffer/TextModel.cs` - `AddDecoration` 默认参数
- `tests/TextBuffer.Tests/DecorationTests.cs` - 测试断言和 helper 方法
- `tests/TextBuffer.Tests/IntervalTreeTests.cs` - 所有测试用例
- `tests/TextBuffer.Tests/MarkdownRendererTests.cs` - owner filter predicate

### 测试更新
更新 `IntervalTreeTests.OwnerFiltersReturnExpectedIntervals` 以反映新语义:
- 当 `ownerFilter=101` 时，只返回 `ownerId=101` 的装饰（不再包含 global）
- 当 `ownerFilter=0 (Any)` 时，返回所有装饰
- 添加新的断言验证 `ownerFilter=Any` 行为

## Phase 2: 添加 DecorationsTrees 过滤开关

### 问题描述
- TS `getDecorationsInRange` 支持 `filterOutValidation`, `filterFontDecorations`, `onlyMinimapDecorations`, `onlyMarginDecorations` 参数
- C# `DecorationsTrees.Search()` 仅支持 `ownerFilter` 参数
- NodeFlags 已在 IntervalTree 中实现，但未暴露给查询 API

### 实施变更

#### 新增 `src/TextBuffer/Decorations/DecorationSearchOptions.cs`
```csharp
public readonly record struct DecorationSearchOptions
{
    public static readonly DecorationSearchOptions Default = new();
    
    public int OwnerFilter { get; init; } = DecorationOwnerIds.Any;
    public bool FilterOutValidation { get; init; } = false;
    public bool FilterFontDecorations { get; init; } = false;
    public bool OnlyMinimapDecorations { get; init; } = false;
    public bool OnlyMarginDecorations { get; init; } = false;
    public DecorationTreeScope Scope { get; init; } = DecorationTreeScope.All;
    
    public static DecorationSearchOptions ForOwner(int ownerId) => ...
    public static DecorationSearchOptions ForScope(DecorationTreeScope scope) => ...
}
```

#### `src/TextBuffer/Decorations/DecorationsTrees.cs`
- 将 `DecorationTreeScope` 从 `internal` 改为 `public`
- 添加新的 `Search(TextRange, DecorationSearchOptions)` 重载

#### `src/TextBuffer/Decorations/IntervalTree.cs`
- 添加 `Search(TextRange, DecorationSearchOptions)` 重载
- 添加 `IntervalSearchWithFilters` 私有方法，在节点匹配时应用所有过滤条件:
  - `DecorationOwnerIds.MatchesFilter` - 所有者过滤
  - `options.FilterOutValidation && node.IsForValidation()` - 验证装饰过滤
  - `options.FilterFontDecorations && node.AffectsFont()` - 字体装饰过滤
  - `options.OnlyMarginDecorations && !node.IsInGlyphMargin()` - 仅边距装饰
  - `options.OnlyMinimapDecorations && node.Options?.AffectsMinimap != true` - 仅 minimap 装饰

#### `src/TextBuffer/TextModel.cs`
添加新的公共 API 方法:
```csharp
public IReadOnlyList<ModelDecoration> GetDecorationsInRange(TextRange range, DecorationSearchOptions options)
public IReadOnlyList<ModelDecoration> GetAllDecorations(DecorationSearchOptions options)
public IReadOnlyList<ModelDecoration> GetLineDecorations(int lineNumber, DecorationSearchOptions options)
```

## 验证结果

```bash
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
```

**结果:** 724 passed, 2 skipped, 0 failed (共 726 测试)

## 新增 API 汇总

### 新类型
- `DecorationSearchOptions` - 装饰搜索过滤选项 record struct

### 新常量
- `DecorationOwnerIds.FirstAllocatableOwnerId = 2` (internal)

### 修改的常量
- `DecorationOwnerIds.Any = 0` (原 `Default = 0`, `Any = -1`)
- `DecorationOwnerIds.Default` - **已移除**

### 新方法
- `IntervalTree.Search(TextRange, DecorationSearchOptions)`
- `DecorationsTrees.Search(TextRange, DecorationSearchOptions)`
- `TextModel.GetDecorationsInRange(TextRange, DecorationSearchOptions)`
- `TextModel.GetAllDecorations(DecorationSearchOptions)`
- `TextModel.GetLineDecorations(int, DecorationSearchOptions)`

### 公开化的类型
- `DecorationTreeScope` (从 `internal` 改为 `public`)

## 文件修改清单

| 文件 | 变更类型 | 描述 |
|------|----------|------|
| `DecorationOwnerIds.cs` | 修改 | 重构常量语义，`Any=0`，添加 `FirstAllocatableOwnerId` |
| `DecorationSearchOptions.cs` | 新增 | 过滤选项 record struct |
| `DecorationsTrees.cs` | 修改 | 公开 scope enum，添加 Search 重载 |
| `IntervalTree.cs` | 修改 | 添加 Search 重载和 IntervalSearchWithFilters |
| `TextModel.cs` | 修改 | 添加 GetDecorationsInRange/GetAllDecorations/GetLineDecorations 重载 |
| `DecorationTests.cs` | 修改 | 更新 Default→Any 引用 |
| `IntervalTreeTests.cs` | 修改 | 更新 Default→Any 引用，修正 OwnerFilters 测试 |
| `MarkdownRendererTests.cs` | 修改 | 更新 Default→Any 引用 |

## 后续工作

### Phase 3 (待实施)
- MarkdownRenderer 直接消费 FindDecorations 缓存
- 减少重复计算

### Phase 4 (待实施)
- 对齐 ModelDecoration 枚举常量与 TS 值
- `MinimapPosition`: 0,1 → 1,2
- `GlyphMarginLane`: 0,1,2 → 1,2,3
- `InjectedTextCursorStops`: Flags → Non-flags

## 引用

- 来源计划: [`CL8-Renderer-Plan.md`](./CL8-Renderer-Plan.md)
- Gap 报告: [`docs/reports/alignment-audit/04-decorations.md`](../../docs/reports/alignment-audit/04-decorations.md)
- 依赖: [`WS3-PORT-Tree-Result.md`](./WS3-PORT-Tree-Result.md) - NodeFlags 实现
