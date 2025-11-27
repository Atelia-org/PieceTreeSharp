# CL8 Renderer Implementation Plan

**调查人:** Investigator-TS (Harper Lin)  
**日期:** 2025-11-28  
**锚点:** [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown), [`#delta-2025-11-26-aa4-cl8-capture`](../indexes/README.md#delta-2025-11-26-aa4-cl8-capture)

## 概要

本文档详细分析了 DocUI renderer 栈与 TypeScript 实现之间的对齐差距，并提供分步实现计划以消除 `docs/reports/alignment-audit/04-decorations.md` 中标识的 Gap。

## 现状分析

### Gap 1: `DecorationOwnerIds.Default` vs `Any` 语义不一致

**问题:**
- TS 中 `ownerId=0` 表示"不过滤"（Any），调用者习惯传 0
- C# 中 `Default=0` 被当作特定 owner，`Any=-1` 才表示"不过滤"
- 当前 `FiltersAllOwners()` 同时接受 0 和 -1，但这破坏了"仅返回 owner==0 的装饰"语义

**TS 行为 (model.ts:891-895):**
```typescript
getLineDecorations(lineNumber: number, ownerId?: number, filterOutValidation?: boolean, filterFontDecorations?: boolean): IModelDecoration[];
// ownerId 未传或传 0 时返回所有装饰
```

**C# 当前实现 (DecorationOwnerIds.cs:11-13):**
```csharp
public const int Default = 0;
public const int SearchHighlights = 1;
public const int Any = -1;

public static bool FiltersAllOwners(int ownerFilter)
    => ownerFilter == Any || ownerFilter == Default;  // 问题：Default 同时是有效 owner
```

### Gap 2: `DecorationsTrees` 缺少 TS 过滤开关

**问题:**
- TS `getDecorationsInRange` 支持 `filterOutValidation`, `filterFontDecorations`, `onlyMinimapDecorations`, `onlyMarginDecorations` 参数
- C# `DecorationsTrees.Search()` 仅支持 `ownerFilter` 参数
- NodeFlags 已在 IntervalTree 中实现（`IsForValidation`, `AffectsFont`, `IsInGlyphMargin`），但未暴露给查询 API

**TS 签名 (model.ts:899-902):**
```typescript
getDecorationsInRange(range: IRange, ownerId?: number, filterOutValidation?: boolean, 
    filterFontDecorations?: boolean, onlyMinimapDecorations?: boolean, onlyMarginDecorations?: boolean): IModelDecoration[];
```

**C# 当前签名 (DecorationsTrees.cs:36):**
```csharp
public IReadOnlyList<ModelDecoration> Search(TextRange range, int ownerFilter = DecorationOwnerIds.Any, 
    DecorationTreeScope scope = DecorationTreeScope.All)
```

### Gap 3: MarkdownRenderer 未消费 FindDecorations

**问题:**
- `MarkdownRenderer.Render()` 调用 `model.GetDecorationsInRange()` 重新搜索装饰
- `FindDecorations` 已管理搜索匹配装饰，但 renderer 未直接读取其缓存
- 这导致重复计算与潜在的状态不一致

**当前流程:**
```
FindDecorations.Set() → 创建装饰 → 存入 TextModel
MarkdownRenderer.Render() → model.GetDecorationsInRange() → 重新搜索
```

**期望流程:**
```
FindDecorations.Set() → 创建装饰 → 存入 TextModel
MarkdownRenderer.Render() → FindDecorations.GetAllMatchRanges() → 直接读取
                         ↓ 或
                         → model.GetDecorationsInRange(ownerId=findOwnerId) → 按 owner 过滤
```

### Gap 4: ModelDecoration 常量与枚举值差异

| 常量 | C# 值 | TS 值 | 影响 |
|------|-------|-------|------|
| `LineHeightCeiling` | 300 | 300 | ✅ 一致 |
| `MinimapPosition.Inline` | 0 | 1 | ❌ 不一致 |
| `MinimapPosition.Gutter` | 1 | 2 | ❌ 不一致 |
| `GlyphMarginLane.Left` | 0 | 1 | ❌ 不一致 |
| `GlyphMarginLane.Center` | 1 | 2 | ❌ 不一致 |
| `GlyphMarginLane.Right` | 2 | 3 | ❌ 不一致 |
| `InjectedTextCursorStops` | Flags enum | Non-flags | ⚠️ 语义差异 |
| `MinimapSectionHeaderStyle` | string | enum (1, 2) | ⚠️ 类型差异 |

## 实现计划

### Phase 1: 修复 DecorationOwnerIds 语义（低风险）

**目标:** 使 `ownerId=0` 映射为"不过滤"，与 TS 行为一致。

**文件修改:**

1. **`src/TextBuffer/Decorations/DecorationOwnerIds.cs`**
```csharp
public static class DecorationOwnerIds
{
    /// <summary>
    /// When passed as filter, matches all decorations (equivalent to TS passing 0 or undefined).
    /// Also used as the default owner for global decorations visible to all editors.
    /// </summary>
    public const int Any = 0;
    
    public const int SearchHighlights = 1;
    
    /// <summary>
    /// Start of allocatable owner IDs. Internal use only.
    /// </summary>
    internal const int FirstAllocatableOwnerId = 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool FiltersAllOwners(int ownerFilter) => ownerFilter == Any;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MatchesFilter(int ownerFilter, int ownerId)
    {
        if (FiltersAllOwners(ownerFilter))
            return true;
        return ownerId == ownerFilter;
    }
}
```

2. **`src/TextBuffer/TextModel.cs`** - 更新 `_nextDecorationOwnerId` 初始值
```csharp
private int _nextDecorationOwnerId = DecorationOwnerIds.FirstAllocatableOwnerId;
```

**测试用例:**
- `DecorationOwnerIdSemantics_ZeroMeansAny` - 验证 ownerId=0 返回所有装饰
- `DecorationOwnerIdSemantics_SpecificOwnerFilters` - 验证 ownerId>0 仅返回对应 owner 的装饰
- `AllocateDecorationOwnerId_StartsFromTwo` - 验证分配从 2 开始

### Phase 2: 添加 DecorationsTrees 过滤开关（中等风险）

**目标:** 暴露 NodeFlags 到查询 API，支持 TS 同款过滤参数。

**文件修改:**

1. **`src/TextBuffer/Decorations/DecorationSearchOptions.cs`** (新文件)
```csharp
namespace PieceTree.TextBuffer.Decorations;

/// <summary>
/// Options for filtering decorations during search.
/// Mirrors TS getDecorationsInRange parameters.
/// </summary>
public readonly record struct DecorationSearchOptions
{
    public static readonly DecorationSearchOptions Default = new();
    
    /// <summary>
    /// Filter decorations by owner. 0 or negative means no filter (all owners).
    /// </summary>
    public int OwnerFilter { get; init; } = DecorationOwnerIds.Any;
    
    /// <summary>
    /// If true, excludes validation decorations (squiggly-error, squiggly-warning, squiggly-info).
    /// </summary>
    public bool FilterOutValidation { get; init; } = false;
    
    /// <summary>
    /// If true, excludes decorations that affect font rendering.
    /// </summary>
    public bool FilterFontDecorations { get; init; } = false;
    
    /// <summary>
    /// If true, only returns decorations that render in the minimap.
    /// </summary>
    public bool OnlyMinimapDecorations { get; init; } = false;
    
    /// <summary>
    /// If true, only returns decorations that render in the glyph margin.
    /// </summary>
    public bool OnlyMarginDecorations { get; init; } = false;
    
    /// <summary>
    /// Tree scopes to search.
    /// </summary>
    public DecorationTreeScope Scope { get; init; } = DecorationTreeScope.All;
}
```

2. **`src/TextBuffer/Decorations/IntervalTree.cs`** - 添加过滤搜索
```csharp
/// <summary>
/// Search with NodeFlags filtering support.
/// </summary>
public IReadOnlyList<ModelDecoration> Search(TextRange range, DecorationSearchOptions options)
{
    if (_root == Sentinel) return Array.Empty<ModelDecoration>();
    
    List<ModelDecoration> result = [];
    IntervalSearchWithFilters(_root, range.StartOffset, range.EndOffset, options, result, 0);
    return result;
}

private void IntervalSearchWithFilters(IntervalNode node, int intervalStart, int intervalEnd, 
    DecorationSearchOptions options, List<ModelDecoration> result, int delta)
{
    // ... existing traversal logic with added filter checks:
    
    // In the node handling section:
    if (node.Decoration != null)
    {
        if (!DecorationOwnerIds.MatchesFilter(options.OwnerFilter, node.OwnerId))
            continue;
        if (options.FilterOutValidation && node.IsForValidation())
            continue;
        if (options.FilterFontDecorations && node.AffectsFont())
            continue;
        if (options.OnlyMarginDecorations && !node.IsInGlyphMargin())
            continue;
        if (options.OnlyMinimapDecorations && node.Options?.AffectsMinimap != true)
            continue;
            
        result.Add(node.Decoration);
    }
}
```

3. **`src/TextBuffer/Decorations/DecorationsTrees.cs`** - 扩展 Search 方法
```csharp
public IReadOnlyList<ModelDecoration> Search(TextRange range, DecorationSearchOptions options)
{
    IReadOnlyList<ModelDecoration> regular = options.Scope.HasFlag(DecorationTreeScope.Regular)
        ? _regular.Search(range, options)
        : Empty;
    IReadOnlyList<ModelDecoration> overview = options.Scope.HasFlag(DecorationTreeScope.Overview)
        ? _overview.Search(range, options)
        : Empty;
    IReadOnlyList<ModelDecoration> injected = options.Scope.HasFlag(DecorationTreeScope.InjectedText)
        ? _injected.Search(range, options)
        : Empty;

    return MergeOrdered(regular, overview, injected);
}

// Keep backward compatible overload
public IReadOnlyList<ModelDecoration> Search(TextRange range, int ownerFilter = DecorationOwnerIds.Any, 
    DecorationTreeScope scope = DecorationTreeScope.All)
{
    return Search(range, new DecorationSearchOptions { OwnerFilter = ownerFilter, Scope = scope });
}
```

4. **`src/TextBuffer/TextModel.cs`** - 添加带过滤参数的方法
```csharp
public IReadOnlyList<ModelDecoration> GetDecorationsInRange(TextRange range, DecorationSearchOptions options)
    => _decorationTrees.Search(range, options);

public IReadOnlyList<ModelDecoration> GetLineDecorations(int lineNumber, DecorationSearchOptions options)
{
    // ... existing range calculation ...
    return _decorationTrees.Search(range, options);
}

public IReadOnlyList<ModelDecoration> GetAllDecorations(DecorationSearchOptions options)
{
    // ... existing implementation with filter support ...
}
```

**测试用例:**
- `Search_FilterOutValidation_ExcludesValidationDecorations`
- `Search_FilterFontDecorations_ExcludesFontDecorations`
- `Search_OnlyMarginDecorations_ReturnsOnlyMarginDecorations`
- `Search_OnlyMinimapDecorations_ReturnsOnlyMinimapDecorations`
- `Search_CombinedFilters_AppliesAllFilters`

### Phase 3: MarkdownRenderer 直接消费 FindDecorations（低风险）

**目标:** 减少重复计算，提供可选的 FindDecorations 集成路径。

**文件修改:**

1. **`src/TextBuffer/Rendering/MarkdownRenderOptions.cs`** - 添加 FindDecorations 选项
```csharp
public record class MarkdownRenderOptions
{
    // ... existing properties ...
    
    /// <summary>
    /// Optional FindDecorations instance to use for search match rendering.
    /// When provided, the renderer will use cached match data instead of re-querying the model.
    /// </summary>
    public FindDecorations? FindDecorations { get; init; }
    
    /// <summary>
    /// When true and FindDecorations is provided, skip querying model for find-related decorations.
    /// </summary>
    public bool UseDirectFindDecorations { get; init; } = true;
}
```

2. **`src/TextBuffer/Rendering/MarkdownRenderer.cs`** - 集成 FindDecorations
```csharp
public string Render(TextModel model, MarkdownRenderOptions? options = null)
{
    // ... existing setup ...
    
    FindDecorations? findDecorations = options?.FindDecorations;
    bool useDirect = options?.UseDirectFindDecorations ?? true;
    int? findOwnerId = findDecorations != null ? GetFindOwnerId(findDecorations) : null;
    
    for (int lineNumber = viewport.StartLine; lineNumber <= viewport.EndLine; lineNumber++)
    {
        // ... existing line rendering ...
        
        IReadOnlyList<ModelDecoration> decorations;
        if (useDirect && findDecorations != null)
        {
            // Use FindDecorations directly for search matches
            decorations = GetDecorationsWithFindOverlay(model, lineRange, ownerQuery, findDecorations, findOwnerId.Value);
        }
        else
        {
            decorations = model.GetDecorationsInRange(lineRange, ownerQuery);
        }
        
        // ... rest of rendering ...
    }
}

private static IReadOnlyList<ModelDecoration> GetDecorationsWithFindOverlay(
    TextModel model, TextRange lineRange, int ownerQuery, 
    FindDecorations findDecorations, int findOwnerId)
{
    // Get non-find decorations from model
    var options = new DecorationSearchOptions
    {
        OwnerFilter = ownerQuery,
        // Exclude find decorations - we'll get them from FindDecorations directly
    };
    var modelDecorations = model.GetDecorationsInRange(lineRange, options)
        .Where(d => d.OwnerId != findOwnerId)
        .ToList();
    
    // Add find decorations from cached source
    Range[] findRanges = findDecorations.GetAllMatchRanges();
    foreach (Range range in findRanges)
    {
        if (RangeOverlapsLine(range, lineRange))
        {
            // Create or retrieve decoration for this match
            // ...
        }
    }
    
    return modelDecorations;
}
```

**测试用例:**
- `Render_WithFindDecorations_UsesCachedMatches`
- `Render_WithFindDecorations_IncludesCurrentMatchHighlight`
- `Render_WithoutFindDecorations_QueriesModelDirectly`

### Phase 4: 对齐 ModelDecoration 常量与枚举（中等风险）

**目标:** 使枚举值与 TS 一致，确保 JSON 序列化/跨进程通信兼容。

**文件修改:**

1. **`src/TextBuffer/Decorations/ModelDecoration.cs`** - 对齐枚举值
```csharp
/// <summary>
/// Position in the minimap to render the decoration.
/// Values match TS MinimapPosition enum.
/// </summary>
public enum MinimapPosition
{
    Inline = 1,  // Changed from 0
    Gutter = 2,  // Changed from 1
}

/// <summary>
/// Vertical Lane in the glyph margin of the editor.
/// Values match TS GlyphMarginLane enum.
/// </summary>
public enum GlyphMarginLane
{
    Left = 1,    // Changed from 0
    Center = 2,  // Changed from 1
    Right = 3,   // Changed from 2
}

/// <summary>
/// Configures cursor stops around injected text.
/// Non-flags enum to match TS InjectedTextCursorStops.
/// </summary>
public enum InjectedTextCursorStops
{
    Both = 0,
    Right = 1,
    Left = 2,
    None = 3,
}

/// <summary>
/// Section header style for minimap decorations.
/// </summary>
public enum MinimapSectionHeaderStyle
{
    Normal = 1,
    Underlined = 2,
}
```

2. **`src/TextBuffer/Decorations/ModelDecorationMinimapOptions.cs`** - 使用枚举
```csharp
public sealed record class ModelDecorationMinimapOptions
{
    public string? Color { get; init; }
    public string? DarkColor { get; init; }
    public MinimapPosition Position { get; init; } = MinimapPosition.Inline;
    public MinimapSectionHeaderStyle? SectionHeaderStyle { get; init; }  // Changed from string
    public string? SectionHeaderText { get; init; }
}
```

**迁移注意事项:**
- 现有代码中使用 `MinimapPosition.Inline` (=0) 的地方需要更新
- 现有代码中使用 `GlyphMarginLane.Left` (=0) 的地方需要更新
- 需要添加 JSON 序列化测试确保向后兼容性

**测试用例:**
- `MinimapPosition_ValuesMatchTypeScript`
- `GlyphMarginLane_ValuesMatchTypeScript`
- `InjectedTextCursorStops_NonFlagsSemantics`
- `MinimapSectionHeaderStyle_EnumValues`
- `ModelDecorationOptions_JsonSerialization_RoundTrip`

## 新增 API 汇总

### 新类型
- `DecorationSearchOptions` - 装饰搜索过滤选项
- `MinimapSectionHeaderStyle` - minimap section header 样式枚举

### 新方法
- `DecorationsTrees.Search(TextRange, DecorationSearchOptions)`
- `IntervalTree.Search(TextRange, DecorationSearchOptions)`
- `TextModel.GetDecorationsInRange(TextRange, DecorationSearchOptions)`
- `TextModel.GetLineDecorations(int, DecorationSearchOptions)`
- `TextModel.GetAllDecorations(DecorationSearchOptions)`

### 修改的常量
- `DecorationOwnerIds.Any` - 值改为 0
- `DecorationOwnerIds.FirstAllocatableOwnerId` - 新增，值为 2
- `MinimapPosition` - 值从 0,1 改为 1,2
- `GlyphMarginLane` - 值从 0,1,2 改为 1,2,3
- `InjectedTextCursorStops` - 从 Flags 改为普通 enum

## 测试计划

### 单元测试（新增 15-20 个）

| 测试类 | 测试用例 | 验证目标 |
|--------|----------|----------|
| `DecorationOwnerIdTests` | `ZeroMeansAny` | ownerId=0 返回所有装饰 |
| `DecorationOwnerIdTests` | `SpecificOwnerFilters` | ownerId>0 仅返回对应 owner |
| `DecorationOwnerIdTests` | `AllocateStartsFromTwo` | 分配从 2 开始 |
| `DecorationSearchOptionsTests` | `FilterOutValidation` | 排除验证装饰 |
| `DecorationSearchOptionsTests` | `FilterFontDecorations` | 排除字体装饰 |
| `DecorationSearchOptionsTests` | `OnlyMarginDecorations` | 仅返回 margin 装饰 |
| `DecorationSearchOptionsTests` | `OnlyMinimapDecorations` | 仅返回 minimap 装饰 |
| `DecorationSearchOptionsTests` | `CombinedFilters` | 组合过滤 |
| `MarkdownRendererTests` | `WithFindDecorations` | 使用缓存匹配 |
| `MarkdownRendererTests` | `CurrentMatchHighlight` | 当前匹配高亮 |
| `ModelDecorationEnumTests` | `MinimapPositionValues` | 值与 TS 一致 |
| `ModelDecorationEnumTests` | `GlyphMarginLaneValues` | 值与 TS 一致 |
| `ModelDecorationEnumTests` | `CursorStopsSemantics` | 非 Flags 语义 |
| `ModelDecorationEnumTests` | `JsonSerialization` | 序列化兼容性 |

### 回归测试

运行现有测试套件确保无破坏：
```bash
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
```

预期：585/585 通过（或 586 如果新增测试）

### 集成测试

验证 DocUI 完整流程：
```bash
dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindDecorationsTests --nologo
dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter MarkdownRendererTests --nologo
```

## 风险评估

| 风险 | 影响 | 缓解策略 |
|------|------|----------|
| 枚举值变更破坏现有代码 | 高 | 分阶段发布，先添加新值后弃用旧值 |
| ownerId 语义变更导致过滤失效 | 中 | 在 Phase 1 完成后运行全量回归 |
| 性能下降（额外过滤逻辑） | 低 | NodeFlags 已在树级别计算，查询时仅读取 |
| FindDecorations 集成复杂度 | 低 | Phase 3 可选，不影响现有渲染路径 |

## 复杂度评估

**整体复杂度: Medium**

- Phase 1 (OwnerIds): 简单，仅常量和逻辑调整
- Phase 2 (过滤开关): 中等，需要修改多个文件但模式清晰
- Phase 3 (Renderer): 简单，可选集成
- Phase 4 (枚举对齐): 中等，需要仔细处理迁移

预计工作量：2-3 个开发周期（每周期 4 小时）

## 依赖项

- [`WS3-PORT-Tree-Result.md`](./WS3-PORT-Tree-Result.md) - NodeFlags 实现
- [`B3-Decor-PORTER.md`](./B3-Decor-PORTER.md) - FindDecorations 实现
- [`docs/reports/alignment-audit/04-decorations.md`](../../docs/reports/alignment-audit/04-decorations.md) - Gap 来源

## 验收标准

1. `ownerId=0` 返回所有装饰（与 TS 行为一致）
2. `DecorationSearchOptions` 支持所有 TS 过滤参数
3. `MarkdownRenderer` 可选地使用 `FindDecorations` 缓存
4. 枚举值与 TS 完全一致
5. 全量回归 585/585 通过
6. 新增测试 15+ 全部通过

## 附录：TS 源码参考

### viewModelDecorations.ts (L97-L102)
```typescript
private _getDecorationsInRange(viewRange: Range, onlyMinimapDecorations: boolean, onlyMarginDecorations: boolean): IViewDecorationsCollection {
    const modelDecorations = this._linesCollection.getDecorationsInRange(
        viewRange, this.editorId, 
        filterValidationDecorations(this.configuration.options), 
        filterFontDecorations(this.configuration.options), 
        onlyMinimapDecorations, onlyMarginDecorations);
```

### model.ts (GlyphMarginLane)
```typescript
export enum GlyphMarginLane {
    Left = 1,
    Center = 2,
    Right = 3,
}
```

### model.ts (MinimapPosition)
```typescript
export const enum MinimapPosition {
    Inline = 1,
    Gutter = 2
}
```
