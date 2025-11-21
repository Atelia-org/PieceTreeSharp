# B1-QA Task Brief – ReplacePattern QA Verification

## 你的角色
QA-Automation（质量保证自动化工程师）

## 记忆文件位置
- `agent-team/members/qa-automation.md`
- 汇报前**必须更新**记忆文件，记录本次任务成果与下一步

## 任务目标
对 Porter-CS 交付的 **Batch #1 – ReplacePattern** 实现进行 QA 验证，产出测试报告与快照。

## 前置条件
- Porter-CS 已交付：
  - `src/PieceTree.TextBuffer/DocUI/ReplacePattern.cs`
  - `src/PieceTree.TextBuffer/DocUI/DocUIReplaceController.cs`
  - `src/PieceTree.TextBuffer.Tests/DocUI/DocUIReplacePatternTests.cs`
  - 23 个测试用例，`dotnet test` 142/142 通过
- 参考文档：`agent-team/handoffs/B1-PORTER-Result.md`

## 执行任务

### 1. 全量测试验证
运行以下命令并记录结果：

```bash
# 基线测试（全量）
dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --logger "trx;LogFileName=batch1-full.trx"

# ReplacePattern 专项测试
dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj \
  --filter "FullyQualifiedName~ReplacePatternTests" \
  --logger "trx;LogFileName=batch1-replacepattern.trx"
```

### 2. 测试矩阵更新
更新 `src/PieceTree.TextBuffer.Tests/TestMatrix.md`：
- 在 DocUI 部分新增 **ReplacePattern** 行
- 记录：
  - TS Source: `ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts`
  - C# Tests: `DocUIReplacePatternTests.cs`
  - Test Count: 23
  - Portability Tier: A
  - Status: ✅ Complete

### 3. 边界测试建议（可选）
如果有时间，考虑补充以下测试用例：
- Emoji 和 Unicode 字符的替换
- 超大捕获组编号（`$99`, `$100`）
- 空字符串替换
- 嵌套 `$` 符号的边界情况

记录建议到汇报中，供后续 Sprint 处理。

### 4. 快照准备（可选）
如果 Porter 已实现 Markdown snapshot 集成：
- 创建 `src/PieceTree.TextBuffer.Tests/__snapshots__/docui/replace-pattern/` 目录
- 运行 snapshot 记录命令（如果有 `DOCUI_SNAPSHOT_RECORD=1` 环境变量支持）
- 验证生成的 Markdown 快照文件

如果未实现，记录到"下一步建议"中。

## 交付物清单
1. **QA 报告**：`agent-team/handoffs/B1-QA-Result.md`，包含：
   - 测试命令与结果（TRX 文件路径）
   - 通过率统计（baseline 与 ReplacePattern 专项）
   - 已更新的 `TestMatrix.md` 截图或摘要
   - 边界测试建议（如有）
   - 快照验证结果（如有）
2. **更新记忆文件**：`agent-team/members/qa-automation.md`

## 输出格式
汇报时提供：
1. **测试结果摘要**：通过数/总数、TRX 文件路径
2. **TestMatrix 更新确认**：已新增的行内容
3. **发现的问题**（如有）：失败测试、性能问题、覆盖度缺口
4. **下一步建议**：给 Info-Indexer 和 DocMaintainer 的注意事项
5. **已更新记忆文件**：确认更新了 `agent-team/members/qa-automation.md`

---
**执行前检查**：
- [ ] 读取 `agent-team/members/qa-automation.md` 获取上下文
- [ ] 读取 `agent-team/handoffs/B1-PORTER-Result.md` 了解 Porter 交付内容
- [ ] 汇报前更新记忆文件

开始执行！
