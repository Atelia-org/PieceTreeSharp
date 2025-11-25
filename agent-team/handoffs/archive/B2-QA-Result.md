# B2-QA-Result – QA-Automation 验证报告

**日期**: 2025-11-23  
**验证人**: QA-Automation  
**来源**: [B2-Porter-Fixes.md](./B2-Porter-Fixes.md)  
**验证范围**: Batch #2 的 3 个 Critical Issues (CI-1, CI-2, CI-3)

---

## Executive Summary

**测试结果**: ❌ **7/187 测试失败** – CI-3 修复引入回归  
**根本原因**: `PieceTreeSearcher.Next()` 中的零宽匹配边界检查逻辑与 TS 原版不一致  
**发布建议**: ⚠️ **Blocked** – 需要回滚 CI-3 部分修改并重新实施

---

## 测试结果摘要

### 1. 全量测试
```bash
PIECETREE_DEBUG=0 dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --nologo
```

**结果**:
- Total: 187
- Passed: 180
- **Failed: 7**
- Skipped: 0
- Duration: 2.9s

### 2. FindModel 专项测试
```bash
PIECETREE_DEBUG=0 dotnet test --filter "FullyQualifiedName~DocUIFindModelTests" --nologo
```

**失败测试**:
1. `Test13_Find_Caret` – 预期 12 个 `^` 匹配，实际 11 个（缺失第 12 行）
2. `Test14_Find_Dollar` – 预期 12 个 `$` 匹配，实际 11 个
3. `Test15_FindNext_CaretDollar` – 预期 2 个 `^$` 匹配，实际 1 个
4. `Test16_Find_DotStar` – 预期 12 个 `^.*$` 匹配，实际 11 个
5. `Test17_FindNext_CaretDotStarDollar` – 预期 12 个匹配，实际 11 个
6. `Test18_FindPrev_CaretDotStarDollar` – 预期 12 个匹配，实际 11 个
7. `Test19_FindPrev_CaretDollar` – 预期 2 个匹配，实际 1 个

**共性特征**: 所有失败测试均与**文本末尾空行的零宽匹配**相关（第 12 行 `[12,1,12,1]` 未匹配）

---

## 场景验证表

| Scenario | Test | Pass? | 备注 |
|----------|------|-------|------|
| CI-1 IntervalTree 边界 | Test13_Find_Caret | ❌ | 缺失 [12,1,12,1] 匹配（CI-3 副作用） |
| CI-2 FindModel 同步 | Test01_Incremental | ✅ | `MatchesPosition` 同步正常 |
| CI-3 末尾零宽匹配 | Test13_Find_Caret | ❌ | **引入回归** – 文本末尾首次零宽匹配被跳过 |
| CI-3 末尾零宽匹配 | Test14_Find_Dollar | ❌ | 同上 |
| CI-3 末尾零宽匹配 | Test15_FindNext_CaretDollar | ❌ | 同上 |

---

## 根本原因分析

### CI-3 修复的问题

**文件**: `src/PieceTree.TextBuffer/Core/PieceTreeSearcher.cs:47`

**修复前逻辑**:
```csharp
if (_lastIndex > textLength)
{
    return null;
}

if (_prevMatchStartIndex >= 0 && _prevMatchStartIndex + _prevMatchLength == textLength)
{
    return null;
}
```

**CI-3 修复后**:
```csharp
// TS Parity: Check zero-width match at text end before _lastIndex boundary check
if (_prevMatchStartIndex >= 0 && _prevMatchStartIndex + _prevMatchLength == textLength)
{
    return null;
}

if (_lastIndex > textLength)
{
    return null;
}
```

### 引入的回归

**问题**: 检查顺序调整导致**文本末尾的首次零宽匹配被误判为"已到达末尾"**

**场景复现**:
1. 文本内容: `"line1\nline2\n...\nline11\n"` (12 行，第 12 行为空)
2. 搜索正则: `^` (匹配行首)
3. 多行模式下，`text` 为整个文档，`textLength = 文档总长度`
4. 当搜索到第 11 行末尾 `\n` 后:
   - `_prevMatchStartIndex = offset(11,末尾)`
   - `_prevMatchLength = 0` (零宽匹配)
   - `_prevMatchStartIndex + _prevMatchLength == textLength` → **true**
5. 下一次循环时，**在执行 `Match()` 前就返回 `null`**，跳过第 12 行 `[12,1,12,1]` 的匹配

### TS 原版逻辑对比

**TS 代码** (`ts/src/vs/editor/common/model/textModelSearch.ts:514-550`):
```typescript
public next(text: string): RegExpExecArray | null {
    const textLength = text.length;

    let m: RegExpExecArray | null;
    do {
        if (this._prevMatchStartIndex + this._prevMatchLength === textLength) {
            // Reached the end of the line
            return null;
        }

        m = this._searchRegex.exec(text);
        if (!m) {
            return null;
        }

        const matchStartIndex = m.index;
        const matchLength = m[0].length;
        if (matchStartIndex === this._prevMatchStartIndex && matchLength === this._prevMatchLength) {
            if (matchLength === 0) {
                // Advance for zero-width
                ...
                continue;
            }
            return null;
        }
        this._prevMatchStartIndex = matchStartIndex;
        this._prevMatchLength = matchLength;

        if (!this._wordSeparators || isValidMatch(...)) {
            return m;
        }

    } while (m);

    return null;
}
```

**关键差异**:
- TS: `if (_prevMatchStartIndex + _prevMatchLength === textLength)` 在 `exec()` **之前**检查，但使用 `do...while` 循环保证**至少执行一次** `exec()`
- C# (CI-3 后): 使用 `while(true)` 循环，提前检查导致**首次匹配被跳过**

### TS Parity 验证

**TS 测试用例** (`ts/src/vs/editor/test/common/model/textModelSearch.test.ts:306-325`):
```typescript
test('issue #4836 - ^.*$', () => {
    assertFindMatches(
        [
            'Just some text text',
            '',                    // Line 2 - empty
            'some text again',
            '',                    // Line 4 - empty
            'again some text'
        ].join('\n'),
        '^.*$', true, false, null,
        [
            [1, 1, 1, 20],
            [2, 1, 2, 1],         // ← 空行零宽匹配
            [3, 1, 3, 16],
            [4, 1, 4, 1],         // ← 空行零宽匹配
            [5, 1, 5, 16],
        ]
    );
});
```

**C# 测试对应** (`Test13_Find_Caret` 等):
- 预期: 12 行均应匹配到 `^` (包括第 12 行空行 `[12,1,12,1]`)
- 实际: 仅匹配到 11 行，第 12 行被跳过

---

## 风险评估

### 高风险

#### CI-3 修复引入的回归
**影响范围**:
- 所有涉及**文本末尾零宽匹配**的场景（`^`, `$`, `\b` 等）
- 多行搜索模式下，文档末尾的空行无法被正确匹配
- 影响 FindModel、Replace、Decorations 等依赖搜索的功能

**业务影响**:
- 用户搜索 `^` / `$` 时，最后一行（如果为空）会被漏掉
- DocUI 渲染时，行首/行尾装饰可能在文档末尾缺失

### 低风险

#### CI-1 & CI-2 修复
**状态**: ✅ **验证通过**
- CI-1: IntervalTree 边界检查正确（`<` 替换 `<=`）
- CI-2: FindModel MatchesPosition 同步正常

---

## 推荐修复方案

### 方案 1: 回滚 CI-3 检查顺序（推荐）

**恢复原始顺序**，但添加注释说明：

```csharp
public Match? Next(string text)
{
    var textLength = text.Length;

    while (true)
    {
        if (_lastIndex > textLength)
        {
            return null;
        }

        // TS Parity: Check if previous match reached text end (after _lastIndex check)
        // This allows first match at text end, but prevents infinite loop on zero-width
        // Reference: ts/src/vs/editor/common/model/textModelSearch.ts:519-522
        if (_prevMatchStartIndex >= 0 && _prevMatchStartIndex + _prevMatchLength == textLength)
        {
            return null;
        }

        var match = _searchRegex.Match(text, _lastIndex);
        // ... rest of logic
    }
}
```

**理由**:
- 保持与原始实现一致（修复前的顺序）
- 仅当**前一次匹配已到达末尾**时退出，允许**首次匹配**在末尾发生
- 性能影响可忽略（仅多一次边界检查）

### 方案 2: 改用 `do...while` 循环（更接近 TS）

```csharp
public Match? Next(string text)
{
    var textLength = text.Length;

    Match? match;
    do
    {
        if (_prevMatchStartIndex >= 0 && _prevMatchStartIndex + _prevMatchLength == textLength)
        {
            return null;
        }

        if (_lastIndex > textLength)
        {
            return null;
        }

        match = _searchRegex.Match(text, _lastIndex);
        // ... rest of logic
    } while (match != null && !match.Success);
}
```

**理由**:
- 完全对齐 TS `do...while` 结构
- 保证至少执行一次 `Match()`

**缺点**:
- 需要重构循环逻辑，影响范围较大

---

## 建议修复步骤

1. **Porter-CS 回滚 CI-3 检查顺序**
   - 恢复为 `_lastIndex > textLength` 在前
   - 更新注释说明为何此顺序必要（允许文本末尾首次匹配）

2. **QA 重新验证**
   - 运行全量测试确保 187/187 通过
   - 特别验证 `Test13_Find_Caret` ~ `Test19_FindPrev_CaretDollar`

3. **更新 Handoff 文档**
   - 在 B2-Porter-Fixes.md 中标注 CI-3 为"部分回滚"
   - 添加"文本末尾零宽匹配"场景到测试矩阵

---

## TS Parity 对齐状态

| Issue | TS 行为 | C# 行为 (修复后) | Parity |
|-------|---------|------------------|--------|
| CI-1 空范围边界 | `[start, end)` | `[start, end)` ✅ | ✅ 100% |
| CI-2 状态同步 | `matchPosition` 传递 | `matchPosition` 传递 ✅ | ✅ 100% |
| CI-3 末尾零宽匹配 | 允许首次匹配 | **禁止首次匹配** ❌ | ❌ 0% |

**总体对齐度**: 66.7% (2/3)

---

## 发布建议

### ⚠️ **Blocked** – 需修复 CI-3 回归

**阻塞原因**:
- 7 个关键测试失败，影响核心搜索功能
- 引入的 bug 会导致用户可见的行为异常（搜索漏掉最后一行）

**解除条件**:
1. Porter-CS 实施推荐修复方案（回滚 CI-3 检查顺序）
2. QA 验证全量测试 187/187 通过
3. 确认 TS parity 达到 100%

**时间估计**:
- 修复时间: 30 分钟（代码改动小）
- 验证时间: 15 分钟（运行测试 + 场景复查）
- 总计: 45 分钟

---

## 后续工作

### 高优先级
1. **CI-3 修复** – 立即处理（阻塞发布）
2. **添加回归测试** – 在 `PieceTreeSearcherTests.cs` 中增加"文本末尾零宽匹配"专项测试
3. **更新文档** – 在 B2-Porter-Fixes.md 记录修复过程

### 中优先级
4. **性能验证** – 确认检查顺序调整对性能无影响（基准测试）
5. **TS 测试移植** – 将 `issue #4836 - ^.*$` 测试移植到 C#

### 低优先级
6. **B2-TS-Review Warnings** – 处理 W-1 ~ W-5 非阻塞建议

---

## 附录：失败测试详情

### Test13_Find_Caret
```
Expected: [1,1,1,1], [2,1,2,1], ..., [11,1,11,1], [12,1,12,1]  (12 个)
Actual:   [1,1,1,1], [2,1,2,1], ..., [11,1,11,1]                (11 个)
Missing:  [12,1,12,1]  ← 文本末尾空行
```

### Test14_Find_Dollar
```
Expected: [1,18,1,18], [2,18,2,18], ..., [11,17,11,17], [12,1,12,1]  (12 个)
Actual:   [1,18,1,18], [2,18,2,18], ..., [11,17,11,17]                (11 个)
Missing:  [12,1,12,1]  ← 文本末尾空行行尾
```

### Test15_FindNext_CaretDollar
```
Expected: [4,1,4,1], [12,1,12,1]  (2 个空行)
Actual:   [4,1,4,1]                (1 个)
Missing:  [12,1,12,1]  ← 文本末尾空行
```

**共性**: 所有缺失的匹配均为 `[12,1,12,1]`（文档最后一行的空行零宽匹配）

---

**验证完成时间**: 2025-11-23  
**下一步**: 等待 Porter-CS 修复 CI-3 回归  
**联系人**: QA-Automation  
**参考文档**:
- [B2-Porter-Fixes.md](./B2-Porter-Fixes.md)
- [B2-TS-Review.md](./B2-TS-Review.md)
- TS 测试: `ts/src/vs/editor/test/common/model/textModelSearch.test.ts:306-325`
