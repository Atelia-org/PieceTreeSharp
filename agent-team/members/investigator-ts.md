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
  - 2025-11-19: 完成 PT-003 mapping pass（PieceSegment、PieceTreeNode、Searcher、BufferRange、helpers），在 `agent-team/type-mapping.md` 写入 invariants/QA hints/TODOs 并附 Diff Summary，标出 WordSeparators→.NET 映射仍待定义以便 Porter/QA 提前知晓。
  - 2025-11-19: 产出 PieceTree “type skeleton diff brief”，新增 `Line Infrastructure` / `Search Helpers` / `Builder/Normalizer` 三个映射表（见 `agent-team/type-mapping.md`），方便 Porter-CS 锚定缺口（search shim、builder 元数据、snapshot/cache）。
- **Upcoming Goals (1 runSubAgent per item):**
  1. PT-003.C：与 Planner/Porter 对齐 Searcher/WordSeparators 的最小 stub 方案（截止 2025-11-20），若无结论则在 type-mapping 里落地临时 API 约束。
  2. OI-002.A：起草 `agent-team/indexes/core-ts-piece-tree.md`，引用已更新的 type map，供 Info-Indexer 接入 changefeed。
  3. PT-003.D：回读 `pieceTreeTextBufferBuilder.ts` + `textModelSearch.ts` 的剩余触点，形成 QA/Porter checklist（若缺上下文则在下一 run 请求 Planner 追加资料）。
  4. Sprint-00 meta：在 Info-Indexer/Task Board 上登记本次 type map 变更与待定 search stub，以免 PT-004/005 启动时遗漏依赖。

## Blocking Issues
- 需要 Planner 明确 PT-003 与 OI-002 工时的优先顺序，避免同一 runSubAgent 同时覆盖两条任务。
- 等待 Info-Indexer 提供索引文件命名/结构约束，以确保 Investigator 输出与目录约定一致。
- 需要 Porter-CS 确认 C# RBTree 公共 API（插入/删除、snapshot rebuild、search hook），好在类型映射文件中提前标注依赖。
- Searcher/WordSeparators 对应的 .NET 实现尚未定案；PT-004 skeleton 与 PT-005 QA plan 都需要可调用的 stub 行为（已在 type map Notes 标注 TBD，需 Planner/Porter 在 11-20 前给出指示，否则 PT-004 只能以 no-op search 落地）。

## Hand-off Checklist
1. 研究笔记写入 `agent-team/members/investigator-ts.md`。
2. Tests or validations performed? N/A（分析任务）
3. 下一位执行者请基于“Upcoming Goals”继续推进或更新类型映射表。
