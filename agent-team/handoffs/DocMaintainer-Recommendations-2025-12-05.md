# DocMaintainer 流程改进建议 (2025-12-05)

**目标读者:** Team Leader, Planner  
**背景:** 2025-12-05 文档一致性审计发现的流程缺口  
**更新:** 已收到 Info-Indexer 回信，包含方案推荐

---

## 🎯 执行摘要

本次审计暴露了 3 个流程问题：
1. **Sprint Log 创建滞后** - Sprint 05 启动 3 天后才创建文档
2. **Changefeed 创建缺失** - 依赖手动操作，容易遗漏
3. **文档同步延迟** - Migration Log 与实际工作有 3 天时间差

**Info-Indexer 反馈** ✅:
- DocMaintainer 手动补录的 8 个 changefeed 全部通过审阅
- **推荐方案 A+C 混合**（Sprint log 为单一事实来源，changefeed 为轻量指针）
- 回信详见: [`InfoIndexer-to-DocMaintainer-2025-12-05.md`](InfoIndexer-to-DocMaintainer-2025-12-05.md)

---

## ⭐ Info-Indexer 推荐方案（需要 Team Leader 批准）

### 方案 A+C 混合：Sprint Log 为核心 + Changefeed 为指针

```
┌─────────────────────────────────────────────────────────┐
│  Sprint Log (单一事实来源)                               │
│  docs/sprints/sprint-XX.md                              │
│  - 完整的 Progress Log                                   │
│  - 测试基线、commits、artifacts                          │
└─────────────────────────────────────────────────────────┘
                          │
                          │ 指针引用
                          ▼
┌─────────────────────────────────────────────────────────┐
│  Changefeed Index (轻量指针)                             │
│  agent-team/indexes/README.md                           │
│  - #delta-YYYY-MM-DD-xxx → sprint-XX.md#section        │
│  - 只保留 anchor + 一句话摘要                            │
└─────────────────────────────────────────────────────────┘
```

### 触发条件
- **测试基线 +20 以上** 或
- **新 git commit 包含 `feat:`/`fix:` 前缀** 或
- **Sprint Batch 完成时**

### 实施细节

**Sprint log 格式调整**（DocMaintainer 负责）:
```markdown
### <a id="session-2"></a>Session 2 - AddSelectionToNextFindMatch (Batch 5)
```

**Changefeed 简化格式**（Info-Indexer 负责）:
```markdown
- **#delta-2025-12-05-batch5** → 详见 [sprint-05.md#session-2](../../docs/sprints/sprint-05.md#session-2)
```

### 优点
1. ✅ **避免内容重复** — Sprint log 是唯一详细记录
2. ✅ **实时性强** — 有明确触发条件
3. ✅ **体积可控** — indexes/README.md 不会膨胀
4. ✅ **向后兼容** — 现有引用仍然有效

---

## 建议 1: Sprint Log 提前创建

### 当前问题
- Sprint 05 于 2025-12-02 启动
- `docs/sprints/sprint-05.md` 于 2025-12-05 才创建（DocMaintainer 手动补救）
- 期间 3 天的工作进度只存在于 AGENTS.md 和 status.md

### 建议方案
**在 Sprint Planning 阶段就创建 Sprint Log 框架**

**时机**: Planner 完成 Sprint 规划、创建 Task Board 时

**模板内容**:
```markdown
# Sprint XX - [Sprint Name]

**Sprint Window:** YYYY-MM-DD ~ YYYY-MM-DD  
**Goal:** [一句话目标]

**Milestone Status:**
- 🔄 M1 - [描述] (计划 YYYY-MM-DD)
- ⏸️ M2 - [描述] (计划 YYYY-MM-DD)
- ⏸️ M3 - [描述] (计划 YYYY-MM-DD)

**Test Baseline:** [上个 Sprint 结束时的基线]

**Changefeed Reminder:** 所有状态更新请同步到 `agent-team/indexes/README.md#delta-YYYY-MM-*`。

---

## Progress Log

<!-- 每次 runSubAgent 完成后追加一个 section -->

---

## Sprint Retrospective (待完成)

Sprint 结束时填写：
- 实际完成 vs 计划
- 测试基线增长
- 关键技术突破
- 流程改进建议
- 下一个 Sprint 重点
```

**责任人**: Planner → 创建框架；Porter/QA → 填充 Progress Log；DocMaintainer → 维护一致性

**优点**:
- 所有进度有统一记录点
- 减少跨文档查找成本
- 便于生成 changefeed

---

## 建议 2: Changefeed 自动化流程

### 当前问题
- Changefeed 依赖 Info-Indexer 手动创建
- 触发条件不明确（"何时创建？"）
- 本次 Sprint 05 完全缺失 12-04/05 的 changefeed

### 建议方案 A: Batch 完成时自动创建（推荐）

**触发条件**:
1. Porter/QA 完成 handoff 并标记 ✅ Done
2. 测试基线变化 ≥ +10 tests
3. 有新的 git commits

**自动化流程**:
```
[Porter 完成工作] 
    ↓
[创建 handoff, 标记 Done] 
    ↓
[Info-Indexer 监测] ← 扫描 handoffs/ 目录的新文件
    ↓
[读取 handoff + Sprint log] ← 提取 summary, commits, tests
    ↓
[生成 changefeed delta 草稿] ← 使用模板
    ↓
[追加到 indexes/README.md] ← 按日期分组
    ↓
[通知 DocMaintainer] ← 触发文档同步
```

**责任人**: 
- Info-Indexer 设计并实施自动化逻辑
- Team Leader 定义触发阈值（例如 "+10 tests"）
- DocMaintainer 执行后续文档同步

### 建议方案 B: 每日汇总（备选）

**触发条件**: 每天结束时（或 Team Leader 发起）

**流程**:
1. Info-Indexer 扫描当日所有 handoffs
2. 按模块分组（PieceTree / Cursor / Snippet / Diff / DocUI）
3. 生成汇总 changefeed（例如 `#delta-2025-12-05-daily`）
4. 更新 Migration Log 和 indexes/README.md

**优点**: 批量处理，减少文档噪音  
**缺点**: 粒度较粗，不利于精确引用

---

## 建议 3: 文档同步 Checklist 强制执行

### 当前问题
- AGENTS.md 提示"先核对 migration-log 和 changefeed"
- 但当 changefeed 不存在时，纪律失效
- DocMaintainer 被迫手动补录（职责越界）

### 建议方案
**在 runSubAgent 完成后强制执行 3 步走**

**Planner 在 runSubAgent 结束时的 Checklist**:
```markdown
## runSubAgent 完成后必做

1. [ ] Porter/QA 已创建 handoff (agent-team/handoffs/*)
2. [ ] Info-Indexer 已创建 changefeed (agent-team/indexes/README.md)
3. [ ] DocMaintainer 已同步核心文档 (AGENTS.md / Task Board / Migration Log)

⚠️ 如果 Info-Indexer 缺位，Team Leader 应亲自执行第 2 步或指派 Planner 代行
```

**强制顺序**:
```
Handoff → Changefeed → 文档同步
  ↓          ↓            ↓
Porter    Info-Indexer  DocMaintainer
```

**工具支持**（可选）:
- 创建一个 `tools/check-doc-sync.sh` 脚本
- 验证 changefeed 是否与最新 handoffs 匹配
- 在 CI 或本地运行

---

## 建议 4: Sprint Log 成为 Changefeed 的主要 Evidence

### 当前问题
- Migration Log、Changefeed、Sprint Log 有大量重复内容
- 维护成本高，容易不一致

### 建议方案
**让 Sprint Log 成为详细记录，Changefeed 只保留指针和一句话摘要**

**新的 Changefeed 格式**:
```markdown
### 2025-12-05 – Snippet Transform & MultiCursor
- **#delta-2025-12-05-batch4** – Snippet Transform + MultiCursor 基础 (+39 tests)
  - 详见: [`sprint-05.md § 2025-12-05 Session 1`](../../docs/sprints/sprint-05.md#session-1---snippet-transform-batch-4)
  - Commits: `9515be1`
- **#delta-2025-12-05-batch5** – AddSelectionToNextFindMatch 完整实现 (+34 tests)
  - 详见: [`sprint-05.md § 2025-12-05 Session 2`](../../docs/sprints/sprint-05.md#session-2---addselectiontonextfindmatch-batch-5)
  - Commits: `4101981`, `575cfb2`
```

**Migration Log 简化为时间线**:
```markdown
| 2025-12-05 | Snippet Transform & P2 清零 | 测试 1085→1158 | `#delta-2025-12-05-batch4` / `#delta-2025-12-05-batch5` |
```

**优点**:
- 减少重复
- Sprint Log 成为单一事实来源
- indexes/README.md 体积可控

**挑战**:
- 需要迁移现有引用
- Sprint Log 需要良好的 anchor 设计

---

## 建议 5: Team Member 认知文件定期同步

### 当前问题
- DocMaintainer 的 `Current Focus` 停留在 2025-12-02
- 本次审计后才更新到 2025-12-05

### 建议方案
**每次 Sprint Batch 完成后，所有相关 Team Members 同步认知文件**

**触发条件**: runSubAgent 完成后

**Planner Checklist 追加**:
```markdown
4. [ ] 相关 Team Members 已更新认知文件
   - Porter-CS: Current Task, Last Delivery
   - QA-Automation: Test Coverage Status
   - Info-Indexer: Active Changefeed
   - DocMaintainer: Canonical Anchors, Current Focus
```

**工具支持**:
- 创建 `tools/sync-team-memory.sh` 提示哪些认知文件需要更新

</details>

---

**文档状态:** ✅ Ready for Team Leader Review (已更新 Info-Indexer 反馈)  
**相关文件:** 
- [`DocMaintainer-to-InfoIndexer-2025-12-05.md`](DocMaintainer-to-InfoIndexer-2025-12-05.md) — 原始建议
- [`InfoIndexer-to-DocMaintainer-2025-12-05.md`](InfoIndexer-to-DocMaintainer-2025-12-05.md) — Info-Indexer 回信
**期望反馈时间:** 2025-12-06

---

## 优先级建议（已更新）

| 建议 | 优先级 | 估计工时 | 影响 | 状态 |
|------|--------|---------|------|------|
| 建议 1: Sprint Log 提前创建 | **P0** | 30min/sprint | 立即解决文档缺失问题 | ⏳ 待批准 |
| **方案 A+C: Changefeed 混合方案** | **P1** | 2h 实施 | 减少 80% 手动补录 | ✅ Info-Indexer 推荐 |
| 建议 3: 文档同步 Checklist | **P1** | 1h 规范制定 | 强制执行纪律 | ⏳ 待批准 |
| 建议 5: 认知文件定期同步 | P2 | 1h 流程设计 | 提升团队记忆连续性 | ⏳ 待批准 |

> **注**: 原建议 2A/4 已合并为 Info-Indexer 推荐的"方案 A+C 混合"

---

## 🎬 请 Team Leader 决策

### 决策项 1: Sprint Log 提前创建 (P0)
- **问题**: Sprint 启动后 3 天才创建 Sprint log
- **建议**: Planner 在 Sprint Planning 阶段创建框架
- **工时**: ~30min/sprint
- **决策**: [ ] 批准 / [ ] 拒绝 / [ ] 修改

### 决策项 2: 方案 A+C 混合 (P1) ⭐ 推荐
- **问题**: Changefeed 与 Sprint log 内容重复，维护成本高
- **建议**: Sprint log 为单一事实来源，changefeed 为轻量指针
- **Info-Indexer 意见**: ✅ 推荐采用
- **触发条件**: 测试基线 +20 / feat/fix commits / Batch 完成
- **工时**: ~2h 初始实施
- **决策**: [ ] 批准 / [ ] 拒绝 / [ ] 修改

### 决策项 3: 文档同步 Checklist (P1)
- **问题**: 文档纪律缺乏强制执行机制
- **建议**: runSubAgent 后强制 Handoff → Changefeed → 文档同步
- **工时**: ~1h 规范制定
- **决策**: [ ] 批准 / [ ] 拒绝 / [ ] 修改

---

## 批准后行动

**如果批准方案 A+C**:
1. DocMaintainer 按新格式调整现有 Sprint log（添加 HTML anchor）
2. Info-Indexer 归档 11 月旧 changefeed（P2）
3. 未来新 changefeed 采用轻量指针格式

**如果批准建议 1**:
1. Planner 在下个 Sprint Planning 时创建 Sprint log 框架
2. DocMaintainer 提供 Sprint log 模板

**如果批准建议 3**:
1. Planner 更新 runSubAgent 后的 Checklist
2. Team Leader 在缺位时代行 Info-Indexer 职责

---

## 附录：原建议 2-5 详情

<details>
<summary>点击展开原始建议详情（已部分合并到方案 A+C）</summary>
