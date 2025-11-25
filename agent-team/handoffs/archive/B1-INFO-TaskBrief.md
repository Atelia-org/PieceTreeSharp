# B1-INFO Task Brief – Changefeed Publication

## 你的角色
Info-Indexer（信息索引员）

## 记忆文件位置
- `agent-team/members/info-indexer.md`
- 汇报前**必须更新**记忆文件，记录本次任务成果与下一步

## 任务目标
为 **Batch #1 – ReplacePattern** 发布 changefeed (`agent-team/indexes/README.md#delta-2025-11-22`)，并更新迁移日志。

## 前置条件
- Porter-CS 已交付实现（详见 `agent-team/handoffs/B1-PORTER-Result.md`）
- QA-Automation 已完成验证（详见 `agent-team/handoffs/B1-QA-Result.md`）

## 执行任务

### 1. 发布 Changefeed
在 `agent-team/indexes/README.md` 中创建新的 `#delta-2025-11-22` 章节（如果已存在，则追加内容）：

#### 内容模板
```markdown
## Delta 2025-11-22

### Batch #1 – ReplacePattern Implementation (AA4-008)
- **交付文件**:
  - `src/PieceTree.TextBuffer/DocUI/ReplacePattern.cs` (561 lines)
  - `src/PieceTree.TextBuffer/DocUI/DocUIReplaceController.cs` (119 lines)
  - `src/PieceTree.TextBuffer.Tests/DocUI/DocUIReplacePatternTests.cs` (356 lines, 23 tests)
- **TS 源文件**:
  - `ts/src/vs/editor/contrib/find/browser/replacePattern.ts`
  - `ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts`
- **测试结果**: 142/142 通过 (基线: 119, 新增: 23)
- **QA 报告**: `agent-team/handoffs/B1-QA-Result.md`
- **Porter 交付**: `agent-team/handoffs/B1-PORTER-Result.md`
- **迁移日志**: `docs/reports/migration-log.md` (新增 Batch #1 条目)
- **TestMatrix**: `src/PieceTree.TextBuffer.Tests/TestMatrix.md` (新增 ReplacePattern 行)
- **已知差异**: C#/JavaScript Regex 空捕获组行为（已文档化，非阻塞）
- **TODO 标记**: FindModel 集成、WordSeparator 上下文（Batch #2）
```

### 2. 更新迁移日志
在 `docs/reports/migration-log.md` 中新增一行：

| Date | Task | TS Source | C# Files | Tests Added | Tests Total | Notes | Changefeed |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 2025-11-22 | **Batch #1 – ReplacePattern** (AA4-008) | `ts/src/vs/editor/contrib/find/browser/replacePattern.ts`, `replacePattern.test.ts` | `ReplacePattern.cs`, `DocUIReplaceController.cs`, `DocUIReplacePatternTests.cs` | +23 | 142 | 移植替换模式解析器、大小写保持逻辑、23 个 TS parity 测试；Porter: `B1-PORTER-Result.md`；QA: `B1-QA-Result.md` | [`#delta-2025-11-22`](../agent-team/indexes/README.md#delta-2025-11-22) |

### 3. 索引资产清单（可选）
如果时间允许，在 `agent-team/indexes/README.md` 的资产清单中新增：
- **DocUI 模块**:
  - `ReplacePattern.cs`: 替换模式解析器（$n, $&, $$, 大小写修饰符）
  - `DocUIReplaceController.cs`: 替换控制器（集成 TextModel）

## 交付物清单
1. **Changefeed 更新**: `agent-team/indexes/README.md#delta-2025-11-22`
2. **迁移日志更新**: `docs/reports/migration-log.md`（新增 Batch #1 条目）
3. **汇报文档**: `agent-team/handoffs/B1-INFO-Result.md`
4. **记忆文件更新**: `agent-team/members/info-indexer.md`

## 输出格式
汇报时提供：
1. **Changefeed 链接**: 确认 `agent-team/indexes/README.md#delta-2025-11-22` 可访问
2. **迁移日志摘要**: 新增条目的关键信息
3. **下一步建议**: 给 DocMaintainer 的注意事项（需要引用的 delta anchor）
4. **已更新记忆文件**: 确认更新了 `agent-team/members/info-indexer.md`

---
**执行前检查**：
- [ ] 读取 `agent-team/members/info-indexer.md` 获取上下文
- [ ] 读取 `B1-PORTER-Result.md` 和 `B1-QA-Result.md` 获取详细信息
- [ ] 汇报前更新记忆文件

开始执行！
