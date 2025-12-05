# 文档一致性审计 Checklist

> **用途**: DocMaintainer 定期执行的一致性检查流程  
> **频率**: Sprint 结束时 / 重大里程碑后 / 收到不一致报告时  
> **目标**: 确保核心文档的事实一致性，维护团队的集体记忆

---

## 阶段 1: 准备工作

### 1.1 获取最新 Changefeed Anchors
- [ ] 读取 `agent-team/indexes/README.md`
- [ ] 列出最近 5 个 changefeed delta
- [ ] 记录最新的测试基线数字

### 1.2 确定审计范围
- [ ] 确定需要检查的 Sprint（当前 + 上一个）
- [ ] 确定需要检查的日期范围
- [ ] 列出相关的 Handoff 文件

---

## 阶段 2: 核心三文档一致性检查

### 2.1 AGENTS.md
- [ ] 读取 `AGENTS.md` 的"最新进展"部分
- [ ] 提取测试基线数字
- [ ] 提取最近的 Batch/Session 描述
- [ ] 提取 changefeed 引用

### 2.2 indexes/README.md
- [ ] 读取 `agent-team/indexes/README.md` 的 Delta Ledger
- [ ] 检查最近的 changefeed 是否包含：
  - [ ] Anchor 命名（格式 `#delta-YYYY-MM-DD-xxx`）
  - [ ] 简要描述
  - [ ] 测试基线变化
  - [ ] Commit hash（如有）
  - [ ] Sprint log 链接（方案 A+C 格式）

### 2.3 migration-log.md
- [ ] 读取 `docs/reports/migration-log.md` 的 Timeline
- [ ] 检查最近的条目是否包含：
  - [ ] 日期范围
  - [ ] Focus 描述
  - [ ] Outcome 摘要
  - [ ] Anchors 引用

### 2.4 交叉验证
- [ ] **测试基线一致性**: AGENTS.md == indexes/README.md == migration-log.md
- [ ] **日期一致性**: 同一事件的日期在三个文档中匹配
- [ ] **Changefeed 引用**: AGENTS.md 引用的 anchor 在 indexes/README.md 中存在
- [ ] **描述一致性**: 同一事件的描述在不同文档中不矛盾

---

## 阶段 3: Sprint Log 一致性检查

### 3.1 Sprint Log 存在性
- [ ] 当前 Sprint 的 `docs/sprints/sprint-XX.md` 已创建
- [ ] Sprint log 包含 Progress Log 部分
- [ ] 最近的 Batch/Session 已记录

### 3.2 Sprint Log 格式（方案 A+C）
- [ ] 每个 Batch/Session 有 HTML anchor（`<a id="batch-N"></a>`）
- [ ] Changefeed 链接指向 indexes/README.md
- [ ] 测试基线增长已记录
- [ ] Commits 已列出（如有）

### 3.3 Task Board 同步
- [ ] `agent-team/task-board.md` 引用当前 Sprint
- [ ] Task Board 的测试基线与其他文档一致
- [ ] 已完成的任务已标记 ✅
- [ ] Changefeed 链接有效

---

## 阶段 4: 验证实际测试基线

### 4.1 运行测试
```bash
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
```

### 4.2 验证结果
- [ ] 实际 Passed 数 == 文档中的基线
- [ ] 实际 Skipped 数 == 文档中的基线
- [ ] 如有差异，调查原因（新 commit? 测试变更?）

---

## 阶段 5: 发现不一致时的修复流程

### 5.1 记录不一致
创建问题清单，格式：
```
| 文档 | 字段 | 期望值 | 实际值 | 严重性 |
|------|------|--------|--------|--------|
| AGENTS.md | 测试基线 | 1158/9 | 1150/9 | High |
```

### 5.2 追溯根源
- [ ] 检查最近的 git commits
- [ ] 检查最近的 Handoffs
- [ ] 咨询相关 Team Member（如 Info-Indexer, Porter）

### 5.3 执行修复
- [ ] 更新不一致的文档（按正确顺序：migration-log → indexes → AGENTS/Sprint）
- [ ] 如需补充 changefeed，与 Info-Indexer 协作
- [ ] 验证修复后的一致性

### 5.4 文档修复
- [ ] 创建 Handoff 记录本次审计和修复
- [ ] 更新 `doc-maintainer/README.md` 的 Last Update
- [ ] 通知 Team Leader（如发现重大不一致）

---

## 阶段 6: 生成审计报告

### 6.1 简要报告（无问题时）
```markdown
## 文档一致性审计 (YYYY-MM-DD)

**审计范围**: Sprint XX, YYYY-MM-DD ~ YYYY-MM-DD  
**核心三文档**: ✅ 一致  
**测试基线**: ✅ 已验证 (XXXX passed, X skipped)  
**Sprint Log**: ✅ 格式正确  
**Task Board**: ✅ 已同步  

**结论**: 文档健康度优秀，无需修复。
```

### 6.2 详细报告（发现问题时）
```markdown
## 文档一致性审计 (YYYY-MM-DD)

**审计范围**: Sprint XX, YYYY-MM-DD ~ YYYY-MM-DD

**发现的不一致**: X 处

| 文档 | 问题 | 严重性 | 修复状态 |
|------|------|--------|----------|
| ... | ... | ... | ... |

**修复行动**:
1. ...
2. ...

**根因分析**: ...

**预防措施**: ...
```

### 6.3 保存报告
- [ ] 保存到 `doc-maintainer/insights/consistency-audit-YYYY-MM-DD.md`
- [ ] 如发现流程问题，更新本 Checklist

---

## 高级审计（可选）

### 7.1 Handoff 归档率
```bash
cd agent-team/handoffs
find . -name "*.md" -not -path "./archive/*" | wc -l  # 活跃
find archive -name "*.md" | wc -l                     # 归档
```
- [ ] 计算归档率（归档 / 总数）
- [ ] 如归档率 < 80%，建议归档旧 handoffs

### 7.2 Changefeed 覆盖率
- [ ] 最近 10 个 Handoffs 是否都有对应的 changefeed?
- [ ] 重大功能（测试 +20）是否都有 changefeed?

### 7.3 文档体积控制
```bash
wc -l AGENTS.md agent-team/status.md agent-team/todo.md
```
- [ ] AGENTS.md < 500 行？（如超出，考虑归档旧进展）
- [ ] status.md < 200 行？
- [ ] todo.md < 150 行？

---

## Checklist 维护

### 何时更新本 Checklist
- 发现新的审计项
- 流程改进（如方案 A+C 生效）
- 发现 Checklist 中的遗漏

### 版本历史
- **v1.0** (2025-12-05) — 初始版本，基于项目健康度探索经验创建

---

**Checklist 状态**: ✅ Ready to Use  
**下次审计**: Sprint 05 结束时 (预计 2025-12-16)
