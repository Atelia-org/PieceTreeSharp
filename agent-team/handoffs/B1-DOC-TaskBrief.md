# B1-DOC Task Brief – Documentation Synchronization

## 你的角色
DocMaintainer（文档维护员）

## 记忆文件位置
- `agent-team/members/doc-maintainer.md`
- 汇报前**必须更新**记忆文件，记录本次任务成果与下一步

## 任务目标
同步 **Batch #1 – ReplacePattern** 的交付成果到所有相关文档，确保 AGENTS / Sprint / Task Board / Plan 的一致性。

## 前置条件
- Info-Indexer 已发布 changefeed: `agent-team/indexes/README.md#delta-2025-11-22`
- 参考文档：
  - `agent-team/handoffs/B1-INFO-Result.md`
  - `agent-team/handoffs/B1-QA-Result.md`
  - `agent-team/handoffs/B1-PORTER-Result.md`

## 执行任务

### 1. 更新 AGENTS.md
在 `AGENTS.md` 的"最新进展"部分新增一条（按时间倒序插入）：

```markdown
- 2025-11-22：**Batch #1 – ReplacePattern** (AA4-008) 完成。Porter-CS 交付 `ReplacePattern.cs` (561L)、`DocUIReplaceController.cs` (119L) 与 `DocUIReplacePatternTests.cs` (23 tests)；QA-Automation 验证全量测试 142/142 通过（新增 23 个 TS parity 测试）；Info-Indexer 发布 [`#delta-2025-11-22`](agent-team/indexes/README.md#delta-2025-11-22) 并同步迁移日志。详见 [`docs/reports/migration-log.md`](docs/reports/migration-log.md) Batch #1 条目。
```

### 2. 更新 Sprint 03
在 `docs/sprints/sprint-03.md` 的 Deliverables & Tracking 表格中，更新 B1-* 行的状态：

| ID | Status |
| --- | --- |
| B1-PORTER | ✅ Done |
| B1-QA | ✅ Done |
| B1-INFO | ✅ Done |
| B1-DOC | 🚧 In Progress → ✅ Done（汇报时更新） |

### 3. 更新 Task Board
在 `agent-team/task-board.md` 中找到 AA4-008（ReplacePattern）任务，更新状态：

```markdown
| AA4-008 | Batch #1 – ReplacePattern Implementation | Porter-CS / QA-Automation | ✅ Done | [`#delta-2025-11-22`](../indexes/README.md#delta-2025-11-22) | 2025-11-22 | 23 tests, 142/142 通过 |
```

### 4. 更新 TS Test Alignment Plan
在 `docs/plans/ts-test-alignment.md` 的 Live Checkpoints 部分，追加 Batch #1 完成记录：

```markdown
- **2025-11-22 (Batch #1 完成)**: Porter-CS 实现 `ReplacePattern.cs` + `DocUIReplaceController` + 23 个 xUnit 测试，移植 TS `replacePattern.test.ts` 的所有核心场景（$n/$&/$$、大小写修饰符、case-preserving 逻辑）。QA-Automation 验证 142/142 通过（新增 23），更新 `TestMatrix.md` 登记 Tier A 完成状态。Info-Indexer 发布 [`#delta-2025-11-22`](../../agent-team/indexes/README.md#delta-2025-11-22)，同步迁移日志。Appendix 表格中 `replacePattern.test.ts` 行状态更新为 ✅ Complete。
```

同时在 Appendix 表格中更新：

| TS Test File | Status |
| --- | --- |
| `ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts` | ✅ Complete → `DocUIReplacePatternTests.cs` (23 tests, 142/142) |

### 5. 验证统一措辞
确保所有文档中引用 Batch #1 时使用一致的术语：
- 任务 ID: **AA4-008** 或 **Batch #1 – ReplacePattern**
- Changefeed: `#delta-2025-11-22`
- 测试结果: **142/142**（新增 23）
- 文件数: 3 个（2 runtime + 1 test）

## 交付物清单
1. **已更新文件**:
   - `AGENTS.md`
   - `docs/sprints/sprint-03.md`
   - `agent-team/task-board.md`
   - `docs/plans/ts-test-alignment.md`
2. **汇报文档**: `agent-team/handoffs/B1-DOC-Result.md`
3. **记忆文件更新**: `agent-team/members/doc-maintainer.md`

## 输出格式
汇报时提供：
1. **更新文件清单**: 列出所有修改的文件路径
2. **一致性检查结果**: 确认所有 changefeed 引用指向 `#delta-2025-11-22`
3. **发现的文档问题**（如有）: 格式、链接、术语不一致等
4. **下一步建议**: 给 Planner 的 Batch #2 规划建议
5. **已更新记忆文件**: 确认更新了 `agent-team/members/doc-maintainer.md`

---
**执行前检查**：
- [ ] 读取 `agent-team/members/doc-maintainer.md` 获取上下文
- [ ] 读取 `B1-INFO-Result.md` 确认 changefeed anchor
- [ ] 汇报前更新记忆文件

开始执行！
