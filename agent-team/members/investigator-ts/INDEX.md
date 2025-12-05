# InvestigatorTS 知识库索引

> 快速导航 InvestigatorTS 的认知本体和积累的知识  
> **简化结构**: 按 Team Leader 批准的方案，使用单一 `knowledge/` 文件夹

---

## 📖 核心认知

**[README.md](README.md)** — InvestigatorTS 角色定义与工作方法
- Role & Mission
- Active Hooks（当前关注的文档钩子）
- Current Focus（当前任务焦点）
- Key Deliverables（交付物清单）
- Open Investigations（未结调查）
- Session Log（会话日志）

---

## 📁 知识库 (knowledge/)

**目录**: `knowledge/`  
**用途**: 存放可复用的分析、模式、洞察

### 文件命名规范

使用前缀区分类型：
- `analysis-*.md` — TS 代码分析报告
- `pattern-*.md` — TS 代码模式库
- `briefing-*.md` — 给 Porter-CS 的 Brief
- `insight-*.md` — 深层洞察

### 当前内容

*(待积累 — 从 handoffs/ 中筛选可复用内容迁移)*

**建议迁入的内容**（按 Team Leader 标准：可复用的模式、通用分析、最佳实践）：
- [ ] TS Snippet 功能清单 + 降级建议 (`INV-Snippet-Downgrade.md`)
- [ ] WS5 Gap 重评估方法论 (`WS5-Gap-Assessment-2025-12-02.md`)
- [ ] TS/C# 类型映射模式（从 `type-mapping.md` 提炼）

---

## 🔗 对外链接

### Handoffs（一次性任务交付）
- **最新**: [`handoffs/INV-MultiCursorSession-TypeMapping-2025-12-05.md`](../../handoffs/INV-MultiCursorSession-TypeMapping-2025-12-05.md)
- **目录**: [`agent-team/handoffs/`](../../handoffs/) — 按日期查找 `INV-*` 前缀文件

### 协作角色
- **Porter-CS**: [`members/porter-cs.md`](../porter-cs.md) — 接收 Brief，执行 C# 移植
- **QA-Automation**: [`members/qa-automation.md`](../qa-automation.md) — 验证移植结果
- **DocMaintainer**: [`members/doc-maintainer/`](../doc-maintainer/) — 文档同步

### 核心文档
- **AGENTS.md**: [`/AGENTS.md`](../../../AGENTS.md) — 项目全局记忆
- **Type Mapping**: [`agent-team/type-mapping.md`](../../type-mapping.md) — TS/C# 类型映射表
- **Task Board**: [`agent-team/task-board.md`](../../task-board.md) — 当前任务看板

---

## 📊 统计指标

### 当前状态 (2025-12-05)

| 类别 | 数量 |
|------|------|
| Handoff 交付物 | 20+ |
| 知识库文档 | 0（待积累）|
| 活跃调查 | 4 |

### 最新 Session Log

| 日期 | 任务 | 类型 |
|------|------|------|
| 2025-12-05 | MultiCursorSession 类型调研 | Analysis |
| 2025-12-03 | Developer Console 访问调查 | Analysis |
| 2025-12-02 | LLM 架构调查 | Analysis |
| 2025-12-02 | WS5 Gap 重评估 | Assessment |

---

## 🚀 知识积累计划

### 验证期目标 (2025-12-05 ~ 2025-12-19)

根据 Team Leader 的验证标准，本知识库需要证明：
1. [ ] 文档被引用的次数
2. [ ] SubAgent 是否在 prompt 提示下读取 INDEX.md
3. [ ] 是否真正减少了重复劳动

### 积累优先级

**P1 — 高复用价值**:
- TS/C# 类型映射模式
- TS Snippet 功能边界
- 常见移植挑战与解决方案

**P2 — 中等复用价值**:
- 模块架构分析（PieceTree, TextModel, FindModel 等）
- 测试策略（确定性测试、Fuzz 测试）

**P3 — 参考价值**:
- 历史调查索引
- 决策记录

---

**索引状态**: ✅ 初始化完成  
**最后更新**: 2025-12-05  
**维护者**: DocMaintainer（协助）/ InvestigatorTS（自主）
