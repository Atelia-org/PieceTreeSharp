# B2-BUG-001 修复报告

**修复人员**: Porter-CS  
**日期**: 2025-11-22  
**Bug 报告**: `agent-team/handoffs/B2-BUG-001-FindModelTests.md`

---

## Bug 总结

TextModelSearch 无法正确匹配最后一行的空行。正则表达式 `^` 应该匹配 12 次（包括最后的空行），但实际只匹配 11 次。

## 根本原因

问题不在 `TextModelSearch.DoFindMatchesLineByLine` 中，而在 `IntervalTree.CollectOverlaps` 方法中。

当查询装饰（decorations）时，对于**零宽度装饰**（如正则匹配 `^` 或 `$`），边界检查逻辑有误。

**原始代码**（错误）：
```csharp
if (currentRange.IsEmpty)
{
    overlaps = currentRange.StartOffset >= range.StartOffset && currentRange.StartOffset < range.EndOffset;
}
```

**问题分析**：
- 文档长度：254 字节
- 第 12 行（空行）位置：offset 254
- 查询范围：`[0, 254)` （左闭右开区间）
- 对于 offset 254 的零宽度装饰：
  - 检查：`254 >= 0 && 254 < 254`
  - 结果：`true && false` = **false** ❌

**修复后**：
```csharp
if (currentRange.IsEmpty)
{
    overlaps = currentRange.StartOffset >= range.StartOffset && currentRange.StartOffset <= range.EndOffset;
}
```

- 检查：`254 >= 0 && 254 <= 254`  
- 结果：`true && true` = **true** ✅

## 修改文件

- `/repos/PieceTreeSharp/src/PieceTree.TextBuffer/Decorations/IntervalTree.cs`（第 145 行）

## TypeScript 参考

TypeScript 实现 (`ts/src/vs/editor/common/model/intervalTree.ts` 第 844 行) 使用：
```typescript
if (nodeEnd >= intervalStart) {
    // There is overlap
```

这相当于使用 `>=` 而不是 `>` 来检查重叠。

## 测试结果

### QA 验证测试

根据 B2-BUG-001 报告的 3 个验证测试：

| 测试 | 状态 |
|------|------|
| Test13_Find_Caret | ✅ 通过 |
| Test18_FindPrev_CaretDotStarDollar | ✅ 通过 |
| Test19_FindPrev_CaretDollar | ✅ 通过 |

**注**: Test15_FindNext_CaretDollar 和 Test17_FindNext_CaretDotStarDollar 涉及其他未解决的问题，超出此 bug 的范围。

### 新增单元测试

创建了 `B2BUG001_LastLineEmptyRegexTests.cs`，包含 2 个测试：
- `TestCaretRegexMatchesAllLines` ✅
- `TestDollarRegexMatchesAllLines` ✅

### 整体测试状态

修复前：
- 总测试：184
- 通过：162  
- 失败：22

修复后：
- 总测试：186 （新增 2 个验证测试）
- 通过：168
- 失败：18

**改进**：修复了 4 个失败的测试（从 22 降至 18）。

## 剩余问题

仍有 18 个 `DocUI.FindModelTests` 测试失败，这些测试失败的原因与此 bug 无关，可能涉及：
- 光标位置管理
- FindModel 状态同步
- 其他边界情况

建议由 QA 或其他团队成员继续调查剩余的失败测试。

---

## 验证步骤

1. 编译项目：
   ```bash
   cd /repos/PieceTreeSharp
   dotnet build
   ```

2. 运行 QA 验证测试：
   ```bash
   dotnet test --filter "FullyQualifiedName~Test13_Find_Caret"
   dotnet test --filter "FullyQualifiedName~Test18_FindPrev_CaretDotStarDollar"
   dotnet test --filter "FullyQualifiedName~Test19_FindPrev_CaretDollar"
   ```

3. 运行新增的单元测试：
   ```bash
   dotnet test --filter "FullyQualifiedName~B2BUG001"
   ```

4. 运行所有测试：
   ```bash
   dotnet test
   ```

---

## 结论

**Bug 已修复**。零宽度装饰（如正则表达式 `^` 和 `$` 的匹配）现在可以正确地在文档末尾位置被查询到。

核心问题是 `IntervalTree.CollectOverlaps` 对零宽度范围的边界检查使用了严格的 `<` 而不是 `<=`，导致位于查询范围结束位置的零宽度装饰被错误地排除。

修复后，所有基础功能测试（145个）仍然通过，且额外修复了 4 个 FindModel 测试。
