# Sprint XX - [Sprint Name]

> **模板说明**: 在 Sprint Planning 阶段创建此文件。
> - 将 `XX` 替换为 Sprint 编号
> - 将 `[Sprint Name]` 替换为 Sprint 名称
> - 将所有 `YYYY-MM-DD` 替换为实际日期
> 
> **创建者**: Planner (Sprint Planning 时)  
> **维护者**: Porter/QA (Progress Log) + DocMaintainer (一致性检查)

**Sprint Window:** YYYY-MM-DD ~ YYYY-MM-DD  
**Goal:** [一句话描述本 Sprint 的核心目标]

**Milestone Status:**
- 🔄 M1 - [描述] (计划 YYYY-MM-DD)
- ⏸️ M2 - [描述] (计划 YYYY-MM-DD)
- ⏸️ M3 - [描述] (计划 YYYY-MM-DD)
- ⏸️ M4 - [描述] (计划 YYYY-MM-DD)

**Test Baseline:** [上个 Sprint 结束时的基线，例如 "1158 passed, 9 skipped"]

**Changefeed Reminder:** 所有状态更新请同步到 `agent-team/indexes/README.md#delta-YYYY-MM-*`。

---

## Progress Log

<!-- 
每次 Batch/Session 完成后追加一个 section。
使用 HTML anchor 便于 changefeed 引用，格式：<a id="batch-N"></a>

触发条件（满足任一即创建 changefeed 指针）：
1. 测试基线 +20 以上
2. 新 git commit 包含 feat:/fix: 前缀
3. Sprint Batch 完成时
-->

### <a id="batch-1"></a>YYYY-MM-DD - [Batch 描述]
**Focus:** [本次工作的重点]

**Achievements:**
- ✅ [完成项 1]
- ✅ [完成项 2]
- ✅ 测试基线: **XXXX passed** (+NN)

**Artifacts:**
- Commits: `[commit hash]`
- Changefeed: [`#delta-YYYY-MM-DD-xxx`](../../agent-team/indexes/README.md#delta-YYYY-MM-DD-xxx)

---

<!-- 复制上面的模板添加更多 Batch -->

---

## Remaining Tasks

| 任务 | 分类 | 工时估计 | 状态 |
|------|------|---------|------|
| [任务 1] | [分类] | ~Xh | Planned |
| [任务 2] | [分类] | ~Xh | Planned |

---

## Sprint Retrospective

> Sprint 结束时填写此部分

### 完成情况
- **计划**: [原计划目标]
- **实际**: [实际完成情况]
- **测试增长**: [起始] → [结束] (+NN, +X%)

### 关键技术突破
1. [突破 1]
2. [突破 2]

### 流程改进
- [改进建议 1]
- [改进建议 2]

### 下一个 Sprint 重点
1. [重点 1]
2. [重点 2]

---

## References
- Task Board: [`agent-team/task-board.md`](../../agent-team/task-board.md)
- Migration Log: [`docs/reports/migration-log.md`](../reports/migration-log.md)
- Test Matrix: [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md)
- Changefeed Index: [`agent-team/indexes/README.md`](../../agent-team/indexes/README.md)
