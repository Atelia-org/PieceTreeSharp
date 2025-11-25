# B2-PLAN Task Brief – Batch #2 Task Decomposition

## 你的角色
Planner（任务规划员）

## 记忆文件位置
- `agent-team/members/planner.md`
- 汇报前**必须更新**记忆文件，记录本次规划成果与下一步

## 任务目标
根据 Investigator-TS 的调研成果（`agent-team/handoffs/B2-INV-Result.md`），将 **Batch #2 – FindModel/FindController** 拆解为可执行的 runSubAgent 任务序列。

## 前置条件
- 已读取 `agent-team/handoffs/B2-INV-Result.md`
- 已知 WordSeparator 规格、FindWidget 测试定位、依赖清单

## 执行任务

### 1. 分析 Batch #2 范围
根据 Investigator 提供的依赖清单，确定 Batch #2 的实施边界：

**核心目标**：
- 移植 `FindModel`（find 逻辑层：search state、decorations、replace）
- 移植 `findModel.test.ts` 的核心测试用例
- 创建 DocUI harness 支持 FindModel 测试

**非核心目标**（可推迟到 Batch #3）：
- FindController（命令层，依赖 EditorAction/ContextKey/Clipboard services）
- FindWidget DOM（无 TS 测试，低优先级）
- WordCharacterClassifier LRU cache（性能优化，非阻塞）

### 2. 拆解任务序列
将 Batch #2 拆解为 **3-5 个 runSubAgent 任务**，每个任务对应一个 AI 员工（或多人协作）。

**推荐结构**（可调整）：
```
B2-PORTER-1: FindModel 基础实施（FindReplaceState + FindDecorations stub）
  ↓
B2-PORTER-2: FindModel 核心逻辑移植（incremental search、replace all）
  ↓
B2-QA: findModel.test.ts 测试迁移 + DocUI harness
  ↓
B2-INFO: Changefeed 发布
  ↓
B2-DOC: 文档同步
```

或者更细粒度：
```
B2-001: Porter-CS 创建 FindReplaceState / FindDecorations stubs
B2-002: Porter-CS 移植 FindModel 核心（search + replace）
B2-003: QA-Automation 迁移 findModel.test.ts（选择高优先级用例）
B2-004: Info-Indexer 发布 changefeed
B2-005: DocMaintainer 同步文档
```

### 3. 登记到 Task Board
在 `agent-team/task-board.md` 中新增 Batch #2 任务行（或更新现有 placeholder）：

| Task ID | Description | Owner | Status | Latest Update | Completed | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| B2-001 | FindModel stubs (FindReplaceState/FindDecorations) | Porter-CS | Planned | — | — | 依赖 B2-INV |
| B2-002 | FindModel core logic (search + replace) | Porter-CS | Planned | — | — | 依赖 B2-001 |
| B2-003 | findModel.test.ts 迁移 + DocUI harness | QA-Automation | Planned | — | — | 依赖 B2-002 |
| B2-004 | Batch #2 changefeed 发布 | Info-Indexer | Planned | — | — | 依赖 B2-003 |
| B2-005 | Batch #2 文档同步 | DocMaintainer | Planned | — | — | 依赖 B2-004 |

### 4. 更新 TS Test Alignment Plan
在 `docs/plans/ts-test-alignment.md` 的 Live Checkpoints 部分追加：

```markdown
- **2025-11-22 (Batch #2 规划)**: Planner 根据 Investigator-TS 调研成果（WordSeparator 规格、FindWidget 测试不存在），拆解 Batch #2 为 5 个 runSubAgent 任务（B2-001~005）。核心目标：移植 FindModel 逻辑层 + findModel.test.ts 测试；推迟 FindController 至 Batch #3。详见 Task Board 与 `agent-team/handoffs/B2-PLAN-Result.md`。
```

### 5. 风险评估
识别 Batch #2 的潜在风险并记录到汇报中：

**已知风险**：
- DocUI harness 复杂度（可能需要额外 stub services）
- findModel.test.ts 依赖 `withTestCodeEditor`（需适配）
- Replace 逻辑与 ReplacePattern 集成（可能需要调整 API）

## 交付物清单
1. **更新文件**:
   - `agent-team/task-board.md`（新增 B2-001~005 任务行）
   - `docs/plans/ts-test-alignment.md`（Live Checkpoints 追加 Batch #2 规划）
2. **汇报文档**: `agent-team/handoffs/B2-PLAN-Result.md`，包含：
   - 任务拆解方案（B2-001~005 描述）
   - 依赖关系图
   - 风险评估
   - 推荐执行顺序
3. **记忆文件更新**: `agent-team/members/planner.md`

## 输出格式
汇报时提供：
1. **任务序列**: B2-001~005 的简要描述与 Owner
2. **依赖关系图**: 哪些任务必须串行，哪些可并行
3. **风险评估**: 已知风险与缓解措施
4. **下一步建议**: 给主 Agent 的启动建议（先启动哪个 runSubAgent）
5. **已更新记忆文件**: 确认更新了 `agent-team/members/planner.md`

---
**执行前检查**：
- [ ] 读取 `agent-team/members/planner.md` 获取上下文
- [ ] 读取 `agent-team/handoffs/B2-INV-Result.md` 获取调研成果
- [ ] 汇报前更新记忆文件

开始执行！
