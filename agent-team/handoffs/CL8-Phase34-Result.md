# CL8 Phase 3 & 4 Result

## Lead Summary
- **Owner:** Porter-CS (Leo Park)
- **Date:** 2025-11-28
- **Changefeed Anchor:** [`#delta-2025-11-28-cl8-phase34`](../indexes/README.md#delta-2025-11-28-cl8-phase34)
- **Migration Log Row:** [`docs/reports/migration-log.md#cl8-phase34`](../../docs/reports/migration-log.md#cl8-phase34)
- **Scope:** 完成 CL8 Renderer 栈 Phase 3（MarkdownRenderer 消费 FindDecorations）与 Phase 4（所有 ModelDecoration 枚举与 TS 数值对齐）。
- **Verification Snapshot:** `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → 763 total（761 pass + 2 skip）。新增 30 项测试（25 枚举对齐 + 5 MarkdownRenderer 覆盖）。

## Scope
### Phase 3 – MarkdownRenderer 集成 FindDecorations
| 文件 | 修改说明 |
|------|----------|
| `src/TextBuffer/Rendering/MarkdownRenderOptions.cs` | 添加 `FindDecorations?` 与 `UseDirectFindDecorations`，用于直接复用搜索缓存 |
| `src/TextBuffer/Rendering/MarkdownRenderer.cs` | 实现 `AppendFindDecorationsMarkers`，在提供缓存时跳过 `TextModel` 查询 |

### Phase 4 – 枚举值对齐
| 文件 | 修改说明 |
|------|----------|
| `src/TextBuffer/Decorations/ModelDecoration.cs` | 对齐 `MinimapPosition`、`GlyphMarginLane`、`InjectedTextCursorStops` 数值并新增 `MinimapSectionHeaderStyle` |
| `tests/TextBuffer.Tests/MarkdownRendererTests.cs` | 将 `SectionHeaderStyle` 断言改为枚举，覆盖 FindDecorations 新路径 |

## Findings
### Renderer 集成成果
- `MarkdownRenderOptions` 可注入可选 `FindDecorations`，`UseDirectFindDecorations` 默认开启以复用缓存并减少模型扫描。
- `MarkdownRenderer` 在 FindDecorations 模式下只渲染缓存命中并保留 legacy 路径，确保无缓存时保持原有行为。

### 枚举值与 API 更新
#### MinimapPosition
| 成员 | 旧值 | 新值 | TS 值 |
|------|------|------|-------|
| Inline | 0 | 1 | 1 |
| Gutter | 1 | 2 | 2 |

#### GlyphMarginLane
| 成员 | 旧值 | 新值 | TS 值 |
|------|------|------|-------|
| Left | 0 | 1 | 1 |
| Center | 1 | 2 | 2 |
| Right | 2 | 3 | 3 |

#### InjectedTextCursorStops（去除 `[Flags]` 语义）
| 成员 | 旧值 | 新值 | TS 值 |
|------|------|------|-------|
| Both | 3 (Before\|After) | 0 | 0 |
| Right | - | 1 | 1 |
| Left | - | 2 | 2 |
| None | 0 | 3 | 3 |
| Before | 1 | 移除 | - |
| After | 2 | 移除 | - |

#### MinimapSectionHeaderStyle（新增）
| 成员 | C# 值 | TS 值 |
|------|-------|-------|
| Normal | 1 | 1 |
| Underlined | 2 | 2 |

### 新增 API
```csharp
public sealed class MarkdownRenderOptions
{
    public FindDecorations? FindDecorations { get; init; }
    public bool UseDirectFindDecorations { get; init; } = true;
}

public sealed record class ModelDecorationMinimapOptions
{
    public MinimapSectionHeaderStyle? SectionHeaderStyle { get; init; }
}
```

### Test Additions
- **`tests/TextBuffer.Tests/DecorationEnumAlignmentTests.cs`** – 25 tests 覆盖 MinimapPosition (3)、GlyphMarginLane (4)、InjectedTextCursorStops (6)、MinimapSectionHeaderStyle (3)、默认值 (3)、JSON round-trip (4)、OverviewRulerLane (2)。
- **`tests/TextBuffer.Tests/MarkdownRendererTests.cs`** – 5 tests 覆盖缓存路径、当前匹配高亮、回退查询、禁用路径与向后兼容。

### 兼容性
- 默认行为保持：未提供 `FindDecorations` 时仍查询 `TextModel`，record 默认值未改变。
- 破坏性提示：`ModelDecorationMinimapOptions.SectionHeaderStyle` 改为枚举，`InjectedTextCursorStops` 不再允许 `Before|After` 组合（需要改为 `Both`）。

## Verification
```bash
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
```
- 结果：763 total（761 pass + 2 skip），≈100s。
- 重点回归：`DecorationEnumAlignmentTests`（25/25）与 `MarkdownRendererTests`（Phase 3 新增路径 5/5）。

## Follow-up
1. **Info-Indexer**：已准备好在 [`docs/reports/migration-log.md#cl8-phase34`](../../docs/reports/migration-log.md#cl8-phase34) 记录 delta，并引用同一 changefeed 锚点。
2. **Renderer 优化**：可选实现 `RenderKind = SearchMatch`，让 FindDecorations 输出更容易被 Markdown 小部件消费。
3. **References**：实施依据 [`CL8-Renderer-Plan.md`](./CL8-Renderer-Plan.md)、Phase 1&2 结果在 [`CL8-Phase12-Result.md`](./CL8-Phase12-Result.md)；TS 枚举源位于 `ts/src/vs/editor/common/model/model.ts`。
