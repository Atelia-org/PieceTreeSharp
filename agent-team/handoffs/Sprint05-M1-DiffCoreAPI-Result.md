# Sprint 05 M1: Diff 核心 API 补齐

## 实现摘要

实现 Diff 核心 API 三个方法：`DiffMove.Flip()`、`LineRangeMapping.Inverse()`、`LineRangeMapping.Clip()`。全部 API 与 TS 源码对齐，添加 14 个单元测试覆盖边界情况。

## 文件变更

| 文件 | 变更类型 | 描述 |
|-----|---------|------|
| `src/TextBuffer/Diff/DiffMove.cs` | 修改 | 添加 `Flip()` 实例方法，交换 original/modified 并 flip 内部 changes |
| `src/TextBuffer/Diff/RangeMapping.cs` | 修改 | 添加 `Inverse()` 和 `Clip()` 静态方法 |
| `tests/TextBuffer.Tests/RangeMappingTests.cs` | 新建 | 14 个新测试覆盖三个 API |

## TS 对齐说明

| TS 元素 | C# 实现 | 备注 |
|---------|---------|------|
| `MovedText.flip()` (linesDiffComputer.ts:53-55) | `DiffMove.Flip()` | 返回新实例，flip lineRangeMapping 和 changes |
| `LineRangeMapping.inverse()` (rangeMapping.ts:19-45) | `LineRangeMapping.Inverse()` 静态方法 | 计算未变更区域的映射 |
| `LineRangeMapping.clip()` (rangeMapping.ts:47-57) | `LineRangeMapping.Clip()` 静态方法 | 裁剪映射到指定范围 |

## 测试结果

### Targeted
```bash
dotnet test --filter "RangeMappingTests" → 14/14 passed
dotnet test --filter "DiffTests" → 4/4 passed
```

### Full
```bash
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
→ 887 passed, 9 skipped (原 873+9)
```

## 已知差异

无。所有实现与 TS 语义完全对齐。

## 遗留问题

无。

## Changefeed Anchor

`#delta-2025-12-02-sprint05-m1-diff-core-api`
