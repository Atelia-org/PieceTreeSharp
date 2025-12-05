# PorterCS 知识库索引

> 快速导航 PorterCS 的认知本体和积累的知识  
> **简化结构**: 按 Team Leader 批准的方案，使用单一 `knowledge/` 文件夹

---

## 📖 核心认知

**[README.md](README.md)** — PorterCS 角色定义与工作方法
- Role & Mission
- Current Focus（当前实现焦点）
- Key Deliverables（交付物清单）
- Test Baselines（测试基线和常用命令）
- Open Investigations（未结依赖）
- Activity Log（活动日志）

---

## 📁 知识库 (knowledge/)

**目录**: `knowledge/`  
**用途**: 存放可复用的移植模式、挑战解决方案、最佳实践

### 文件命名规范

使用前缀区分类型：
- `pattern-*.md` — C# 实现模式（如 LINQ 替代 TS filter/map）
- `challenge-*.md` — 移植挑战与解决方案（如 C# 没有 Union Types）
- `port-*.md` — 模块移植记录（可复用的移植笔记）
- `insight-*.md` — 深层洞察

### 当前内容

*(待积累 — 从 handoffs/ 中筛选可复用内容迁移)*

**建议迁入的内容**（按 Team Leader 标准：可复用的模式、通用分析、最佳实践）：
- [ ] IntervalTree 延迟 Normalize 模式 (`WS3-PORT-Tree-Result.md`)
- [ ] Cursor Stage 0/1 移植架构 (`WS4-PORT-Core-Result.md`, `CL7-*-Result.md`)
- [ ] Snippet 功能分级实现 (`Snippet-P1/P1.5/P2-Result.md`)
- [ ] TS/C# 类型映射最佳实践（从 `type-mapping.md` 提炼）

---

## 🔗 对外链接

### Handoffs（一次性任务交付）
- **最新**: [`handoffs/PORT-MultiCursorSelectionController-2025-12-05.md`](../../handoffs/PORT-MultiCursorSelectionController-2025-12-05.md)
- **目录**: [`agent-team/handoffs/`](../../handoffs/) — 按日期查找 `PORT-*`/`WS*-PORT-*` 前缀文件

### 协作角色
- **InvestigatorTS**: [`members/investigator-ts/`](../investigator-ts/) — 提供 Brief，分析 TS 代码
- **QA-Automation**: [`members/qa-automation.md`](../qa-automation.md) — 验证移植结果
- **DocMaintainer**: [`members/doc-maintainer/`](../doc-maintainer/) — 文档同步

### 核心文档
- **AGENTS.md**: [`/AGENTS.md`](../../../AGENTS.md) — 项目全局记忆
- **Type Mapping**: [`agent-team/type-mapping.md`](../../type-mapping.md) — TS/C# 类型映射表
- **Task Board**: [`agent-team/task-board.md`](../../task-board.md) — 当前任务看板
- **Migration Log**: [`docs/reports/migration-log.md`](../../../docs/reports/migration-log.md) — 移植进度时间线

---

## 📊 统计指标

### 当前状态 (2025-12-05)

| 类别 | 数量 |
|------|------|
| Handoff 交付物 | 30+ |
| 知识库文档 | 0（待积累）|
| 测试基线 | 1158 pass + 9 skip |

### 测试命令快速参考

```bash
# Full sweep
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo

# 常用 filters
--filter DiffTests                    # 59 tests
--filter RangeMappingTests            # 36 tests
--filter SnippetControllerTests       # 80 tests
--filter CursorCoreTests              # 25 tests
--filter CursorWordOperationsTests    # 41 tests
--filter TextModelSearchTests         # 45 tests
```

### 最新 Activity Log

| 日期 | 任务 | 结果 |
|------|------|------|
| 2025-12-05 | MultiCursorSelectionController | 5 API, 1142+9 |
| 2025-12-05 | MultiCursorSession-Fix | 编译修复, 1124+9 |
| 2025-12-02 | TextModel-ValidatePosition | 44 新测试 |
| 2025-12-02 | Sprint05-M3 Diff Regression | 4→59 tests |

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
- IntervalTree 实现模式
- Cursor 架构移植笔记

**P2 — 中等复用价值**:
- Snippet 分级实现策略
- Diff API 实现笔记
- 测试迁移技巧

**P3 — 参考价值**:
- 特定 Bug 修复记录
- 性能优化笔记

---

**索引状态**: ✅ 初始化完成  
**最后更新**: 2025-12-05  
**维护者**: DocMaintainer（协助）/ PorterCS（自主）
