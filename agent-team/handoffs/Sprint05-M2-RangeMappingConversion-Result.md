# Sprint05-M2-RangeMappingConversion Result

## 实现摘要
实现 RangeMapping 转换 API，新增 `TextLength`、`TextReplacement`、`DiffTextEdit` 三个辅助类，添加 `RangeMapping.FromEdit()`、`RangeMapping.FromEditJoin()`、`RangeMapping.ToTextEdit()` 和 `DetailedLineRangeMapping.ToTextEdit()` 四个 API。新增 22 个测试。

## 文件变更

### 新增文件
- `src/TextBuffer/Diff/TextLength.cs` — 文本长度类，计算行列偏移
- `src/TextBuffer/Diff/DiffTextEdit.cs` — TextReplacement + DiffTextEdit 类，处理多重替换

### 修改文件
- `src/TextBuffer/Diff/RangeMapping.cs` — 添加 FromEdit/FromEditJoin/Join/ToTextEdit 静态和实例方法
- `tests/TextBuffer.Tests/RangeMappingTests.cs` — 新增 22 个测试用例

## TS 对齐说明

| TS 元素 | C# 实现 | 备注 |
|---------|---------|------|
| `TextLength` class | `TextLength` struct | 完整移植，包括 OfText、CreateRange、AddToPosition |
| `TextReplacement` class | `TextReplacement` sealed class | 简化移植，包含 Range、Text 属性 |
| `TextEdit` class | `DiffTextEdit` sealed class | 命名为 DiffTextEdit 避免与 TextModel.TextEdit 冲突 |
| `TextEdit.getNewRanges()` | `DiffTextEdit.GetNewRanges()` | 完整移植，计算替换后的新范围 |
| `RangeMapping.fromEdit()` | `RangeMapping.FromEdit()` | 静态方法，从 DiffTextEdit 创建 RangeMapping[] |
| `RangeMapping.fromEditJoin()` | `RangeMapping.FromEditJoin()` | 静态方法，返回合并的单个 RangeMapping |
| `RangeMapping.join()` | `RangeMapping.Join()` | 静态方法，合并多个 RangeMapping |
| `RangeMapping.toTextEdit()` | `RangeMapping.ToTextEdit()` | 实例方法，接受 `Func<Range, string>` |
| `DetailedLineRangeMapping.toTextEdit()` | `DetailedLineRangeMapping.ToTextEdit()` | 静态方法，接受 mapping 列表 + getValueOfRange |

## API 设计说明

### 类型命名差异
C# 中已存在简单的 `TextEdit` struct（在 TextModel.cs 中），因此新类命名为 `DiffTextEdit` 以区分。

### getValueOfRange 回调
TS 版本使用 `AbstractText` 接口，C# 版本使用 `Func<Range, string>` 委托，更灵活且不需要引入新接口。

## 测试结果

### Targeted
```
dotnet test --filter RangeMappingTests → 36/36 passed
```

### Full
```
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
→ 909 passed, 9 skipped
```

## 已知差异

1. **TextEdit vs DiffTextEdit**: C# 版本使用 `DiffTextEdit` 命名避免与现有类型冲突
2. **getValueOfRange**: 使用委托而非接口，API 更简洁

## 遗留问题

无

## Changefeed Anchor
`#delta-2025-12-02-sprint05-m2-rangemapping-conversion`
