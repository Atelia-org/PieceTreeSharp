# B2-QA Task Brief – Batch #2 Test Matrix Draft

## 你的角色
QA-Automation（质量保证自动化工程师）

## 记忆文件位置
- `agent-team/members/qa-automation.md`
- 汇报前**必须更新**记忆文件，记录本次任务成果与下一步

## 任务目标
为 **Batch #2 – FindModel** 草拟测试矩阵条目与 QA 命令，为后续 Porter-CS 实施提供验收标准。

## 前置条件
- 已读取 `agent-team/handoffs/B2-INV-Result.md`（FindModel 规格）
- 已读取 `agent-team/handoffs/B2-PLAN-Result.md`（任务拆解方案）

## 执行任务

### 1. 扩展 TestMatrix.md
在 `src/PieceTree.TextBuffer.Tests/TestMatrix.md` 中新增 FindModel 相关测试行：

#### TS Test Alignment Map 部分
| Feature Suite | TS Source | C# Tests | Implementation Files | Status | Notes |
| --- | --- | --- | --- | --- | --- |
| **DocUI – Find Model** | `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts` | `DocUIFindModelTests` (planned) | `FindReplaceState.cs`, `FindModel.cs`, `FindDecorations.cs` | Spec ready | B2-001~003；目标 15 个核心测试（增量搜索、replace、wholeWord） |

#### Test Baseline 表格
（预留行，B2-003 完成后填入）
| Date | Phase | Baseline | Duration | Notes |
| --- | --- | --- | --- | --- |
| 2025-11-27 (expected) | Batch #2 – FindModel | TBD/TBD | TBD | B2-003 交付后更新 |

### 2. 草拟测试命令
创建 Batch #2 专项测试命令（供 B2-003 使用）：

```bash
# 全量测试（包含 FindModel）
dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj \
  --logger "trx;LogFileName=batch2-full.trx" --nologo

# FindModel 专项测试
dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj \
  --filter "FullyQualifiedName~DocUIFindModelTests" \
  --logger "trx;LogFileName=batch2-findmodel.trx" --nologo
```

### 3. 核心测试场景清单
**重要**：直接移植 TS 原版 `findModel.test.ts` 的 43 个测试用例，**不要重新发明测试**。

根据 TS 测试文件，完整测试清单：
```
'incremental find from beginning of file'
'find model removes its decorations'
'find model updates state matchesCount'
'find model reacts to position change'
'find model next'
'find model next stays in scope'
'multi-selection find model next stays in scope (overlap)'
'multi-selection find model next stays in scope'
'find model prev'
'find model prev stays in scope'
'find model next/prev with no matches'
'find model next/prev respects cursor position'
'find ^'
'find $'
'find next ^$'
'find .*'
'find next ^.*$'
'find prev ^.*$'
'find prev ^$'
'replace hello'
'replace bla'
'replaceAll hello'
'replaceAll two spaces with one space'
'replaceAll bla'
'replaceAll bla with \\t\\n'
'issue #3516: "replace all" moves page/cursor/focus/scroll to the place of the last replacement'
'listens to model content changes'
'selectAllMatches'
'issue #14143 selectAllMatches should maintain primary cursor if feasible'
'issue #1914: NPE when there is only one find match'
'replace when search string has look ahed regex'
'replace when search string has look ahed regex and cursor is at the last find match'
'replaceAll when search string has look ahed regex'
'replace when search string has look ahed regex and replace string has capturing groups'
'replaceAll when search string has look ahed regex and replace string has capturing groups'
'replaceAll when search string has multiline and has look ahed regex and replace string has capturing groups'
'replaceAll preserving case'
'issue #18711 replaceAll with empty string'
'issue #32522 replaceAll with ^ on more than 1000 matches'
'issue #19740 Find and replace capture group/backreference inserts `undefined` instead of empty string'
'issue #27083. search scope works even if it is a single line'
'issue #3516: Control behavior of "Next" operations (not looping back to beginning)'
'issue #3516: Control behavior of "Next" operations (looping back to beginning)'
```

**优先级排序**（分阶段移植）：
- **Phase 1（B2-003.P1）**：15 个核心测试（前 15 个）
- **Phase 2（B2-003.P2）**：15 个进阶测试（16-30）
- **Phase 3（B2-003.P3）**：13 个边缘测试（31-43）

### 4. Harness 需求草案
为 `DocUIFindModelTests` 设计轻量级 harness：

**最小需求**：
```csharp
// DocUI harness（最小化版本）
class DocUITestHarness
{
    ITextModel Model { get; }
    FindReplaceState State { get; }
    FindModel FindModel { get; }
    
    // 简化版 withTestCodeEditor 语义
    static DocUITestHarness Create(string initialText);
    void AssertDecorationCount(int expected);
    string GetCurrentMatchText();
}
```

**非需求**（推迟至 Batch #3）：
- 完整 editor services（ContextKeyService、ClipboardService）
- FindWidget DOM
- Command layer（FindController）

### 5. QA Memo（给 Porter-CS）
在汇报中创建"QA Expectations for B2-002/003"章节：

```markdown
## QA Expectations for B2-002/003

### For Porter-CS (B2-001/002)
- **API 契约**：`FindModel` 需提供 `Find()`, `Replace()`, `ReplaceAll()`, `UpdateDecorations()` 方法
- **事件通知**：`OnDidChangeSearchString`, `OnDidChangeSearchResults`, `OnDidChangeDecorations`
- **测试钩子**：提供 `GetCurrentMatches()` 供测试验证内部状态

### For QA-Automation (B2-003)
- **Harness 创建**：参考 Batch #1 `DocUIReplaceController` 模式
- **测试优先级**：先实现 P0（7 个测试），再扩展 P1/P2
- **Snapshot 可选**：若时间允许，生成 Markdown snapshot（before/after search）
```

## 交付物清单
1. **更新文件**:
   - `src/PieceTree.TextBuffer.Tests/TestMatrix.md`（新增 FindModel 行 + 测试命令）
2. **汇报文档**: `agent-team/handoffs/B2-QA-Result.md`，包含：
   - 核心测试场景清单（15 个）
   - Harness 需求草案
   - QA Expectations（给 Porter 与 QA 自己）
   - 测试命令草案
3. **记忆文件更新**: `agent-team/members/qa-automation.md`

## 输出格式
汇报时提供：
1. **测试矩阵摘要**: 新增的 FindModel 行内容
2. **核心场景清单**: 15 个测试用例（P0/P1/P2 分级）
3. **Harness 草案**: 最小需求 API
4. **下一步建议**: 给 Porter-CS 的 API 设计建议
5. **已更新记忆文件**: 确认更新了 `agent-team/members/qa-automation.md`

---
**执行前检查**：
- [ ] 读取 `agent-team/members/qa-automation.md` 获取上下文
- [ ] 读取 `B2-INV-Result.md` 和 `B2-PLAN-Result.md` 获取规格
- [ ] 汇报前更新记忆文件

开始执行！
