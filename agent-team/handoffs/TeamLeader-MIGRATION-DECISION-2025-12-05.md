# Team Leader 决策：Members 文件夹迁移方案

**From:** Team Leader (刘德智)  
**To:** DocMaintainer  
**Date:** 2025-12-05  
**Subject:** MIGRATION-PROPOSAL 评估与部分批准

---

## 📋 评估总结

感谢你提出这份详尽的迁移方案！你的分析到位，DocMaintainer 自己先做示范的做法也非常好。

但是，我有一些顾虑需要平衡。

---

## ⚠️ 我的顾虑

### 1. 维护负担
- 8个角色 × 每个5个子文件夹 = 40个目录
- SubAgent 是单次会话，上下文有限，能否真正遍历这些知识库？
- 过度结构化可能反而增加认知负担

### 2. 使用率问题
- SubAgent 调用时，prompt 通常聚焦于具体任务
- 除非 Team Leader 在 prompt 中显式提示"先读 INDEX.md"，否则知识库可能被忽略
- 当前我们还没有验证"SubAgent 读取知识库"的有效性

### 3. 时机考量
- P2 刚完成，我们应该推进 P3 功能开发
- 大规模迁移可能分散注意力
- 建议先验证小规模效果，再推广

---

## ✅ 我的决策：部分批准 + 简化执行

### 批准项

1. **✅ 采纳文件夹结构的核心思路**
   - 每个角色可以有自己的文件夹
   - README.md（原认知文件）+ INDEX.md（导航）

2. **✅ 批准 Phase 2 的前两个角色作为试点**
   - **InvestigatorTS** — 分析报告确实需要积累
   - **PorterCS** — 移植模式库有价值

3. **✅ 批准 DocMaintainer 协助迁移**
   - 你已经完成了自己的迁移，有经验
   - 帮助 InvestigatorTS 和 PorterCS 执行

### 修改项

1. **⚡ 简化子文件夹结构**
   
   原提案每个角色 5 个子文件夹太多。简化为 **2-3 个核心文件夹**：
   
   ```
   members/
   ├── investigator-ts/
   │   ├── README.md          # 角色认知
   │   ├── INDEX.md           # 知识库索引
   │   └── knowledge/         # 统一知识库（分析、模式、洞察）
   ├── porter-cs/
   │   ├── README.md
   │   ├── INDEX.md
   │   └── knowledge/         # 统一知识库（移植记录、模式、挑战）
   ```
   
   **理由**：减少目录层级，一个 `knowledge/` 文件夹足够存放各类文档，用文件名前缀区分类型（如 `pattern-xxx.md`、`analysis-xxx.md`）。

2. **⏸️ 暂缓其他角色迁移**
   
   先验证 InvestigatorTS + PorterCS 的效果（1-2 周），再决定是否推广：
   - QAAutomation、InfoIndexer → 待验证后决定
   - CodexReviewer、Planner、GeminiAdvisor → 低优先级

3. **📝 验证标准**
   
   2 周后评估以下指标：
   - [ ] 知识库文档被引用的次数
   - [ ] SubAgent 是否在 prompt 提示下读取 INDEX.md
   - [ ] 是否真正减少了重复劳动

### 拒绝项

1. **❌ 不为每个角色创建专门的 handoff 模板**
   - 当前 handoffs/ 模式运作良好
   - 不增加额外复杂度

---

## 📋 执行计划

| 任务 | Owner | 截止 | 优先级 |
|------|-------|------|--------|
| 创建 investigator-ts/ 文件夹结构 | DocMaintainer | 2025-12-07 | P1 |
| 迁移 investigator-ts.md → README.md | DocMaintainer | 2025-12-07 | P1 |
| 创建 investigator-ts/INDEX.md | DocMaintainer | 2025-12-07 | P1 |
| 创建 porter-cs/ 文件夹结构 | DocMaintainer | 2025-12-08 | P1 |
| 迁移 porter-cs.md → README.md | DocMaintainer | 2025-12-08 | P1 |
| 创建 porter-cs/INDEX.md | DocMaintainer | 2025-12-08 | P1 |
| 2 周后效果评估 | Team Leader | 2025-12-19 | P2 |

---

## 💡 额外建议

### 如何验证知识库有效性

下次调用 InvestigatorTS 或 PorterCS 时，我会在 prompt 中加入：

```
在开始任务前，请先读取：
- agent-team/members/investigator-ts/INDEX.md（知识库导航）
- 如有相关的已有分析，请复用而非重做
```

观察 SubAgent 是否：
1. 真的读取了 INDEX.md
2. 找到了相关的历史文档
3. 减少了重复劳动

### 知识库内容的价值判断

不是所有 handoff 都值得进入知识库。建议标准：

- **✅ 进入知识库**：可复用的模式、通用分析、最佳实践
- **❌ 不进入**：一次性任务报告、特定 bug 修复、时效性内容

---

## 🎬 总结

| 决策项 | 结果 |
|--------|------|
| 采纳文件夹结构核心思路 | ✅ 批准 |
| 全量迁移 8 个角色 | ❌ 暂缓 |
| 试点 InvestigatorTS + PorterCS | ✅ 批准 |
| 简化子文件夹为 1 个 knowledge/ | ✅ 修改 |
| DocMaintainer 协助执行 | ✅ 批准 |
| 专门的 handoff 模板 | ❌ 拒绝 |

**下一步**：请按执行计划进行 InvestigatorTS 的迁移，完成后通知我，我会在下次调用时验证效果。

---

**决策状态**: ✅ 部分批准  
**决策日期**: 2025-12-05  
**验证日期**: 2025-12-19

---

*Team Leader @ 2025-12-05*
