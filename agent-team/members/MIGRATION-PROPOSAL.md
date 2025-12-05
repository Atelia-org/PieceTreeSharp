# AI Team Members 文件夹结构迁移方案

> **提议者**: DocMaintainer  
> **日期**: 2025-12-05  
> **目标**: 为每个 AI Team Member 建立独立的知识库文件夹  
> **决策者**: Team Leader

---

## 背景

当前 `agent-team/members/` 下是扁平的单文件结构：
```
members/
├── codex-reviewer.md
├── doc-maintainer.md       ← 已迁移到 doc-maintainer/README.md
├── gemini-advisor.md
├── info-indexer.md
├── investigator-ts.md
├── planner.md
├── porter-cs.md
└── qa-automation.md
```

**问题**:
- 单文件难以扩展（探索、洞察、清单无处存放）
- 知识无法积累（每次会话都从同一个文件开始）
- 协作材料分散（handoffs/ 下，缺乏角色视角的索引）

---

## 提议的新结构

```
members/
├── codex-reviewer/
│   ├── README.md              # 角色认知（原 .md 文件内容）
│   ├── INDEX.md               # 知识库索引
│   ├── reviews/               # 代码审查记录
│   ├── patterns/              # 发现的代码模式
│   └── insights/              # 审查洞察
├── doc-maintainer/            ✅ 已完成
│   ├── README.md
│   ├── INDEX.md
│   ├── explorations/
│   ├── insights/
│   └── checklists/
├── gemini-advisor/
│   ├── README.md
│   ├── INDEX.md
│   ├── consultations/         # 咨询记录
│   └── recommendations/       # 建议汇总
├── info-indexer/
│   ├── README.md
│   ├── INDEX.md
│   ├── workflows/             # 工作流文档
│   └── changefeed-templates/  # Changefeed 模板
├── investigator-ts/
│   ├── README.md
│   ├── INDEX.md
│   ├── analysis/              # TS 分析报告
│   ├── briefings/             # 给 Porter 的 Brief
│   └── ts-patterns/           # TS 代码模式
├── planner/
│   ├── README.md
│   ├── INDEX.md
│   ├── strategies/            # 规划策略
│   ├── retrospectives/        # Sprint 回顾
│   └── templates/             # 规划模板
├── porter-cs/
│   ├── README.md
│   ├── INDEX.md
│   ├── ports/                 # 移植记录
│   ├── patterns/              # C# 模式库
│   └── challenges/            # 移植挑战与解决方案
└── qa-automation/
    ├── README.md
    ├── INDEX.md
    ├── test-strategies/       # 测试策略
    ├── coverage-reports/      # 覆盖率报告
    └── harnesses/             # 测试 Harness 设计
```

---

## 迁移路径

### Phase 1: 模板建立（DocMaintainer 完成）
- [x] DocMaintainer 作为示范完成迁移
- [x] 创建通用模板文件
- [ ] 文档化迁移方法

### Phase 2: 高频角色迁移（建议优先）
1. **InvestigatorTS** — 分析报告多，需要结构化
2. **PorterCS** — 移植记录多，需要模式库
3. **QAAutomation** — 测试策略需要积累
4. **InfoIndexer** — 与 DocMaintainer 协作频繁

### Phase 3: 低频角色迁移
5. **CodexReviewer** — 审查模式可积累
6. **Planner** — 规划策略需要改进
7. **GeminiAdvisor** — 咨询记录待积累

---

## 每个角色的独特子文件夹

### InvestigatorTS
- `analysis/` — TS 代码分析报告
- `briefings/` — 给 Porter 的 Brief（按模块组织）
- `ts-patterns/` — TS 代码模式库（如"TS 中的 Disposable 模式"）

### PorterCS
- `ports/` — 移植记录（按模块/功能）
- `patterns/` — C# 实现模式库（如"LINQ 替代 TS filter/map"）
- `challenges/` — 移植挑战与解决方案（如"C# 没有 Union Types"）

### QAAutomation
- `test-strategies/` — 测试策略文档（如"Fuzz Testing 方法论"）
- `coverage-reports/` — 覆盖率分析（按 Sprint）
- `harnesses/` — 测试 Harness 设计（如"PieceTreeFuzzHarness 设计"）

### CodexReviewer
- `reviews/` — 代码审查记录（按日期/模块）
- `patterns/` — 发现的代码模式（好/坏实践）
- `insights/` — 审查洞察（如"常见 Bug 模式"）

### InfoIndexer
- `workflows/` — Changefeed 创建流程文档
- `changefeed-templates/` — 各类 changefeed 模板
- `archival-policies/` — 归档策略

### Planner
- `strategies/` — 规划策略（如"如何拆分大任务"）
- `retrospectives/` — Sprint 回顾（经验总结）
- `templates/` — 各类规划模板

### GeminiAdvisor
- `consultations/` — 咨询记录（按主题）
- `recommendations/` — 建议汇总（跨会话可查）

---

## 通用文件模板

### README.md 模板

```markdown
# [Role Name] 认知文档

> 本文件定义 [Role Name] 的身份、职责与工作方法

## 身份与职责
- **我是谁**: ...
- **核心职责**: ...
- **不是什么**: ...

## Coordination Hooks
- **与 XXX 协作**: ...
- **输入**: ...
- **输出**: ...

## Working Methods
- **典型流程**: ...
- **工具使用**: ...
- **质量标准**: ...

## Current Focus
- **活跃任务**: ...
- **待解决问题**: ...

## Last Update
- **Date**: YYYY-MM-DD
- **Task**: ...
- **Result**: ...

## Knowledge Base
*(链接到 INDEX.md)*
```

### INDEX.md 模板

```markdown
# [Role Name] 知识库索引

> 快速导航 [Role Name] 的认知本体和积累的知识

## 📖 核心认知
**[README.md](README.md)** — 角色定义与工作方法

## 🔍 [子文件夹 1 名称]
*(列出该文件夹下的关键文档)*

## 🔍 [子文件夹 2 名称]
*(列出该文件夹下的关键文档)*

## 📊 统计指标
...

## 🔗 对外链接
...

## 🚀 下一步计划
...
```

---

## 迁移操作步骤

### 对于每个角色

1. **创建文件夹结构**
   ```bash
   mkdir -p agent-team/members/[role-name]/{[subfolders]}
   ```

2. **迁移原认知文件**
   ```bash
   mv agent-team/members/[role-name].md agent-team/members/[role-name]/README.md
   ```

3. **创建 INDEX.md**
   - 使用模板
   - 列出子文件夹
   - 添加统计指标

4. **更新 README.md**
   - 在末尾添加 "Knowledge Base" 部分
   - 链接到 INDEX.md 和子文件夹

5. **验证链接**
   - 确保所有内部链接有效
   - 确保外部链接到该角色的文档已更新

---

## 收益分析

### 短期收益（1-2 周）
- ✅ 知识有地方存放（不再丢失探索结果）
- ✅ 结构化组织（易查找、易复用）
- ✅ 跨会话连续性（INDEX 提供快速导航）

### 中期收益（1-2 个月）
- ✅ 模式库积累（InvestigatorTS 的 TS patterns, PorterCS 的 C# patterns）
- ✅ 最佳实践沉淀（QA 的测试策略, Planner 的规划方法）
- ✅ 角色专业化（每个角色成为自己领域的"专家"）

### 长期收益（3+ 个月）
- ✅ 团队知识库（不只是个人记忆，而是团队资产）
- ✅ 新 Agent 快速上手（读取对应角色的知识库即可）
- ✅ 跨项目复用（结构和经验可迁移到其他项目）

---

## 风险与缓解

### 风险 1: 维护负担增加
- **风险**: 文件多了，更新成本高
- **缓解**: 只在有价值时才创建新文档；定期归档过时内容

### 风险 2: 结构过度设计
- **风险**: 文件夹太多，找不到东西
- **缓解**: INDEX.md 提供导航；保持 2-3 层深度

### 风险 3: 跨会话使用率低
- **风险**: 新会话不知道有这些知识库
- **缓解**: README.md 中明确提示"先读 INDEX.md"；Team Leader 在调度时提示

---

## 决策点

**需要 Team Leader 决策**:
1. ✅ / ❌ 是否采纳本方案？
2. 如采纳，优先级顺序？（建议 Phase 2 高频角色优先）
3. 迁移责任人？（建议 DocMaintainer 协助，各角色自己执行）
4. 时间表？（建议 1-2 周内完成 Phase 2）

**可选决策**:
- 是否需要为每个角色创建专门的 handoff 模板？
- 是否需要统一的知识库命名规范？

---

## 后续行动

**如果批准**:
1. DocMaintainer 创建迁移指南文档
2. DocMaintainer 协助各角色执行迁移
3. 1 周后回顾进展和问题

**如果不批准**:
- 记录决策理由
- DocMaintainer 保持当前结构
- 定期重新评估需求

---

**方案状态**: 📝 待 Team Leader 决策  
**提议日期**: 2025-12-05  
**期望反馈**: 2025-12-06
