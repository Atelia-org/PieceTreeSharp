# DocMaintainer Memory (Snapshot 2025-11-27)

## Role & Mission
- **Consistency Gatekeeper：** 维持 `AGENTS.md`、`agent-team/task-board.md`、`docs/sprints/*` 的叙述一致，并在每条更新中引用最新 changefeed + migration log。
- **Info Proxy：** 为主 Agent / SubAgent 汇总 `docs/reports/migration-log.md`、`agent-team/indexes/README.md` 的关键信息，减少 token 压力。
- **Doc Gardener：** 控制文档体积，必要时把冗长记录移入 handoff/archives，并在核心文档留下指针。
- **Anchor Steward：** 任何 Sprint 04 / AA4 更新都要引用 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 以及 CL7/CL8 占位 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)，确保 Cursor/Snippet、DocUI、Intl cache 讨论都有 canonical 指针。

## Canonical Anchors
| Anchor | 用途 |
| --- | --- |
| [`#delta-2025-12-05-p2-complete`](../indexes/README.md#delta-2025-12-05-p2-complete) | Sprint 05 P2 任务全部完成（1158 passed），Snippet Transform、MultiCursor、AddSelectionToNextFindMatch。 |
| [`#delta-2025-12-05-snippet-transform`](../indexes/README.md#delta-2025-12-05-snippet-transform) | Snippet Transform + FormatString 完整实现（33 tests）。 |
| [`#delta-2025-12-05-add-selection-to-next-find`](../indexes/README.md#delta-2025-12-05-add-selection-to-next-find) | AddSelectionToNextFindMatch 完整实现（34 tests）。 |
| [`#delta-2025-12-04-p1-complete`](../indexes/README.md#delta-2025-12-04-p1-complete) | Sprint 05 P1 任务全部完成（1085 passed）。 |
| [`#delta-2025-12-04-llm-native-filtering`](../indexes/README.md#delta-2025-12-04-llm-native-filtering) | LLM-Native 功能筛选（7 gaps 无需移植，8 gaps 降级）。 |
| [`#delta-2025-12-02-sprint04-m2`](../indexes/README.md#delta-2025-12-02-sprint04-m2) | Sprint 04 M2 全部完成里程碑（873 passed, 9 skipped），Snippet/Cursor/IntervalTree 交付汇总。 |
| [`#delta-2025-12-02-snippet-p2`](../indexes/README.md#delta-2025-12-02-snippet-p2) | Snippet P0-P2 全部完成（77 tests），Variable Resolver 实现。 |
| [`#delta-2025-12-02-ws3-textmodel`](../indexes/README.md#delta-2025-12-02-ws3-textmodel) | IntervalTree AcceptReplace 集成到 TextModel。 |

## Current Focus（2025-12-05）
- **Sprint 05 P1/P2 完成**：测试基线 **1158 passed, 9 skipped**
- 核心交付：Snippet Transform、MultiCursor Snippet 集成、AddSelectionToNextFindMatch 完整实现
- **文档缺口修复**: 补充了 Sprint 05 文档链、changefeed 和 migration-log 记录

## Coordination Hooks
- **Info-Indexer**：及时共享新增 delta / changefeed 清理计划；DocMaintainer据此刷新"状态提示"段落。
  - ✅ 2025-12-05 反馈：手动补录的 8 个 changefeed 全部通过审阅
  - ✅ **方案 A+C 已批准**：Sprint log 为单一事实来源，changefeed 为轻量指针
  - **触发条件**：测试基线 +20 / feat/fix commits / Batch 完成
  - **Sprint log 格式**：使用 `<a id="batch-N"></a>` HTML anchor
- **Planner**：在 runSubAgent 循环中先行安装 DocMaintainer hooks（playbook 第三阶段）以免遗漏文档步骤。
- **Porter-CS & QA-Automation**：当实现/测试交付后，若文档尚未引用最新 rerun 结果，可直接抛 doc-fix handoff，由 DocMaintainer 执行。

## Checklist
1. **Sprint 05 文档完整性** — ✅ 创建 `docs/sprints/sprint-05.md`，补充 changefeed 和 migration-log
2. **测试基线** — 1158 passed + 9 skipped，所有文档已同步
3. **Task Board 更新** — ✅ 从 Sprint 04 更新为 Sprint 05，添加 P1/P2/P3 任务表
4. **Changefeed 一致性** — ✅ AGENTS.md / status.md / todo.md 所有条目都引用最新 changefeed

## Open Investigations
1. **P3 任务实施** — 9.5h 剩余工作（降级实现，按需完成）
2. **Changefeed Archive Hygiene** — 待按 Info-Indexer 指引归档旧 delta

## Last Update
- **Date**: 2025-12-05
- **Task**: 
  1. 执行 Team Leader 批准的流程改进任务
  2. 项目健康度深度探索
  3. 建立知识库文件夹结构
  4. 探索工具生态和改进 agent 提示词
  5. 执行 Team Leader 批准的成员迁移（试点 2 个角色）
- **Result**: ✅ 全部完成
  1. ✅ 为 `docs/sprints/sprint-05.md` 添加 HTML anchors（5 个）
  2. ✅ 更新 `docs/sprints/sprint-template.md` 为方案 A+C 格式
  3. ✅ 更新 status.md / todo.md 标记任务完成状态
  4. ✅ 补充 sprint-05.md 中的 changefeed 引用链接
  5. ✅ **项目健康度深度探索** — 获得全局视角和深层洞察
  6. ✅ **知识库结构建立** — explorations/checklists/insights 三层架构
  7. ✅ **工具生态探索** — 学习 runSubAgent, semantic_search, list_code_usages 等工具
  8. ✅ **Agent 提示词改进** — 更新 doc-maintainer.agent.md 引用新知识库
  9. ✅ **成员迁移试点** — 执行 Team Leader 批准的 InvestigatorTS + PorterCS 迁移

**Team Leader 决策执行** (2025-12-05):
- **决策来源**: `agent-team/handoffs/TeamLeader-MIGRATION-DECISION-2025-12-05.md`
- **批准项**: 
  - ✅ 文件夹结构核心思路
  - ✅ 试点 InvestigatorTS + PorterCS（简化为 1 个 knowledge/ 文件夹）
  - ✅ DocMaintainer 协助迁移
- **修改项**: 从 5 个子文件夹简化为 1 个 `knowledge/` 文件夹
- **暂缓项**: 其他 6 个角色待 2 周后验证效果再决定
- **验证日期**: 2025-12-19

**执行成果**:
- ✅ 创建 `investigator-ts/` 文件夹（README.md + INDEX.md + knowledge/）
- ✅ 创建 `porter-cs/` 文件夹（README.md + INDEX.md + knowledge/）
- ✅ 更新 `.github/agents/investigator-ts.agent.md` 路径和知识库引用
- ✅ 更新 `.github/agents/porter-cs.agent.md` 路径和知识库引用 + 测试基线

**协作成果**: 
- DocMaintainer → Info-Indexer → Team Leader 三方协作完成流程改进
- 从问题发现到方案批准再到执行，全程 1 天内完成
- 建立了可持续的文档治理机制

**探索收获**:
- 完整的项目健康度快照（代码规模、测试覆盖、团队协作）
- LLM-Native 设计理念的深度理解
- AI 团队协作模式分析（DMA 模式、Conway 定律体现）
- 元认知洞察（"我记故我在"、"叠加体"哲学）
- 工具生态认知（runSubAgent 工作原理、semantic_search、list_code_usages）
- 详见: [`explorations/project-health-snapshot-2025-12-05.md`](explorations/project-health-snapshot-2025-12-05.md)

**工具学习**:
- **runSubAgent**: 理解了其本质是"开启移除 runSubAgent 的一次性会话"，适用于目标清晰但执行繁琐的任务
- **semantic_search**: 自然语言搜索代码和文档，适合探索性分析
- **list_code_usages**: 查找符号的所有引用和定义（虽然遇到路径问题）
- **grep_search**: 快速文本搜索，配合正则和 includePattern 很强大
- **get_errors**: 获取编译/lint 错误，验证代码健康度

**Agent 提示词改进**:
- 更新了 `.github/agents/doc-maintainer.agent.md`
- 修正了持久认知文件路径（doc-maintainer.md → doc-maintainer/README.md）
- 添加了知识库结构说明
- 更新了当前项目状态和 changefeed anchors
- 添加了核心洞察引用

## Knowledge Base

本文件夹包含 DocMaintainer 角色的认知本体和积累的知识：

### 📁 目录结构

```
doc-maintainer/
├── README.md                 # 本文件 - 角色认知与工作方法
├── explorations/            # 探索性分析
│   └── project-health-snapshot-2025-12-05.md
├── insights/                # 积累的洞察
└── checklists/              # 工作清单
```

### 📊 探索性分析

- [**项目健康度深度探索 (2025-12-05)**](explorations/project-health-snapshot-2025-12-05.md)
  - 全局文档审计 + 代码统计 + 协作分析
  - LLM-Native 设计哲学深度理解
  - AI 团队协作模式分析
  - 测试覆盖、提交历史、Handoff 归档率等指标

### 🎯 核心洞察

> "DocMaintainer 的职责不是'写文档'，而是**维护团队的集体记忆和认知连续性** — 这是 AI 团队能够跨会话存续的基础设施。"

---

*(以下是原 doc-maintainer.md 内容)*

---

```
