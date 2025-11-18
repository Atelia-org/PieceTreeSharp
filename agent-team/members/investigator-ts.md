# Investigator-TS Memory

## Role & Mission
- **Focus Area:** 理解 TypeScript `pieceTreeTextBuffer` 及相关依赖，沉淀迁移洞察
- **Primary Deliverables:** 依赖清单、行为说明、迁移注意事项、类型映射建议
- **Key Stakeholders:** Planner、Porter-CS

## Onboarding Summary (2025-11-19)
- 复盘 `AGENTS.md` 时间线与 PT-003 / OI-001 / OI-002 关联，确认 Investigator 输出是 Porter-CS 启动 RBTree 迁移的前置条件。
- 重读 `agent-team/ai-team-playbook.md` 与 `agent-team/main-loop-methodology.md`，锁定 runSubAgent 输入模板、Info-Indexer/DocMaintainer 钩子以及文件级回写要求。
- 查阅 `docs/meetings/meeting-20251119-team-kickoff.md`、`docs/meetings/meeting-20251119-org-self-improvement.md`、`docs/sprints/sprint-00.md`、`docs/sprints/sprint-org-self-improvement.md` 与 `agent-team/task-board.md`，明确当前预算、依赖关系与优先级。

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| pieceTreeTextBuffer orchestrator | ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts | 顶层文本模型适配层，记录 edit API、cursor 交互与依赖注入点 |
| pieceTreeBase data contract | ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts | Node 结构、piece 定义、buffer 索引策略，需提炼类型映射与不变量 |
| rbTreeBase balancing rules | ts/src/vs/editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts | 红黑树旋转/颜色逻辑，映射至 C# Core.RBTree 实现 |
| pieceTreeTextBufferBuilder pipeline | ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBufferBuilder.ts | Snapshot 导入与增量构建流程，决定初始化/恢复 API |
| Range/Position primitives | ts/src/vs/editor/common/core/{position.ts,range.ts,selection.ts} | 统一坐标系统，需在 C# 端建立等价结构 |
| Offset & interval utilities | ts/src/vs/editor/common/model/{intervalTree.ts,prefixSumComputer.ts} | PieceTree 引用的辅助结构，决定性能包络 |
| Search & regex touchpoints | ts/src/vs/editor/common/model/textModelSearch.ts + related core/text modules | 找出 PieceTree 与搜索接口耦合，支撑后续 Searcher 抽象 |
| Mapping sink | agent-team/type-mapping.md | 汇总上方分析结论，驱动 Porter-CS 实现顺序 |

## Planned Output Targets
- `agent-team/type-mapping.md`：记录 TS↔C# 类型/方法对齐（PT-003 主产物）。
- `agent-team/indexes/core-ts-piece-tree.md`（待建）：沉淀 TS 依赖索引供 Info-Indexer 扩展（OI-002）。
- `deepwiki/` 备用：若分析篇幅过长，再与 DocMaintainer 协调发布。

## Worklog
- **Last Update:** 2025-11-19
- **Recent Actions:**
  - 2025-11-19: 在 `docs/meetings/meeting-20251119-org-self-improvement.md` 提交 Investigator-TS 陈述，记录 PieceTree 覆盖现状、blind spots、协作需求与文档治理建议。
  - 完成核心流程/会议/冲刺/任务文档的首轮通读并提取 Investigator 相关里程碑。
  - 列出 PieceTree 及其依赖 TS 文件的研读顺序，标记与类型映射、索引输出的映射关系。
  - 明确将成果写回 `type-mapping.md` 与未来索引文件，便于 Planner 跟踪 PT/OI 交付。
- **Upcoming Goals (1 runSubAgent per item):**
  1. PT-003.A：深入 `pieceTreeBase.ts` 段落，输出节点字段/不变量总结并更新 `agent-team/type-mapping.md`。
  2. PT-003.B：解析 `rbTreeBase.ts` 旋转/重平衡流程，附 C# 端约束笔记，写回 `type-mapping.md`。
  3. OI-002.A：起草 `agent-team/indexes/core-ts-piece-tree.md`，罗列 PieceTree→Core 依赖和推荐阅读顺序。
  4. PT-003.C：补齐 `textModelSearch.ts` 与 `pieceTreeTextBufferBuilder.ts` 的调用链/字段映射，确认哪些接口需要在 Sprint 00 内反映给 Porter-CS。

## Blocking Issues
- 需要 Planner 明确 PT-003 与 OI-002 工时的优先顺序，避免同一 runSubAgent 同时覆盖两条任务。
- 等待 Info-Indexer 提供索引文件命名/结构约束，以确保 Investigator 输出与目录约定一致。
- 需要 Porter-CS 确认 C# RBTree 公共 API（插入/删除、snapshot rebuild、search hook），好在类型映射文件中提前标注依赖。

## Hand-off Checklist
1. 研究笔记写入 `agent-team/members/investigator-ts.md`。
2. Tests or validations performed? N/A（分析任务）
3. 下一位执行者请基于“Upcoming Goals”继续推进或更新类型映射表。
