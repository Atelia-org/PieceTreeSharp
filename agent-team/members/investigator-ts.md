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
  - 2025-11-19: 完成 PT-008 Snapshot 分析。
    - 分析了 `pieceTreeBase.ts` 中的 `PieceTreeSnapshot` 。
    - 确定了 `ITextSnapshot` 接口（简单的 `read(): string | null`）。
    - 确认了线程安全性依赖于不可变的 `Piece` 和仅追加的缓冲区。
    - 为 C# 移植准备了 Diff 简报：
      - 类：实现了 `ITextSnapshot` 的 `PieceTreeSnapshot` 。
      - 构造函数：捕获树状态（piece 列表）。
      - `Read()`：迭代 pieces。
      - 工厂： `PieceTreeBase.CreateSnapshot(string bom)` 。
      - 测试：在 `pieceTreeTextBuffer.test.ts` 中识别 `bug #45564` 和不可变快照测试。
  - 2025-11-19: 完成 PT-009 Line Optimization 分析。
    - 分析了 `pieceTreeBase.ts` 中的 `_lastVisitedLine` 缓存逻辑。
    - 确定了 `_lastVisitedLine` 是一个简单的单行缓存（lineNumber, value），在 `getLineContent` 中检查，在 `insert`/`delete` 中失效。
    - 为 C# 移植准备了 Diff 简报：
      - 结构：在 `PieceTreeModel` 中添加 `LastVisitedLine` 结构体和字段。
      - 方法：更新 `GetLineContent` 以使用缓存。
      - 失效：在 `Insert` 和 `Delete` 方法中重置缓存。
      - 测试：建议添加访问同一行多次、修改后访问的测试用例。
  - 2025-11-20: 完成 AA3-005 CL3 审计（Diff prettify & move metadata）。
    - 对比 `ts/src/vs/editor/common/diff/defaultLinesDiffComputer/*.ts` / `rangeMapping.ts` 与 C# `src/PieceTree.TextBuffer/Diff/*`，梳理缺失的 `LinesDiff`/`DetailedLineRangeMapping` 数据、move detection 与 heuristics/timeout 选项差异。
    - 评估 `TextModel`/`Decorations`/`MarkdownRenderer` 消费路径，确认 DocUI 目前无法携带 diff/move 元数据，并在 `agent-team/handoffs/AA3-005-Audit.md` 给出 F1–F4 建议与 QA 钩子。
    - 更新 `docs/reports/audit-checklist-aa3.md` CL3 行状态，提示 Porter/QA 后续依赖。
  - 2025-11-20: 完成 AA3-007 CL4 审计（Decorations & DocUI）。
    - 对比 TS `textModel.ts`/`intervalTree.ts`/`textModelTokens.ts` 与 C# `Decorations/*`、`TextModel.cs`、`Rendering/MarkdownRenderer.cs`，梳理装饰元数据、stickiness、DocUI 渲染缺口，写入 `agent-team/handoffs/AA3-007-Audit.md`（F1–F4）。
    - 标记 DocUI/MarkdownRenderer 对 AA3-006 diff metadata 与 AA3-008 Porter 修复的依赖，并整理 Porter next steps + QA hooks。
    - 更新 `docs/reports/audit-checklist-aa3.md` CL4 行（状态改为 “Audit Complete – Fixes Pending”）。
  - 2025-11-20: 完成 AA4-003 CL7 审计（Cursor word/snippet/multi-selection parity）。
    - 研读 TS `cursor.ts`/`cursorCollection.ts`/`cursorWordOperations.ts`/`cursorColumnSelection.ts` 与 `snippetController2.ts`，对比 C# `Cursor.cs`、`TextModel.cs`、`MarkdownRenderer.cs`。
    - 在 `agent-team/handoffs/AA4-003-Audit.md` 写入 F1–F4（多光标基建缺失、WordOperations 缺口、列选择/可见列 helper 缺口、Snippet session 缺口）+ Blockers/Validation hooks，作为 Porter-CS AA4-007 的输入。
    - 更新 `docs/reports/audit-checklist-aa4.md#cl7`（状态改为 “Audit Complete – Awaiting Fix” 并填充要点）。
  - 2025-11-20: 完成 AA4-004 CL8 审计（DocUI Find/Replace + Decorations parity）。
    - 复盘 TS `findController.ts`/`findModel.ts`/`findDecorations.ts`/`replacePattern.ts` 与 `textModelSearch.ts`，比对 C# `TextModelSearch`、`TextModel.HighlightSearchMatches`、`MarkdownRenderer`、`SearchHighlightOptions`。
    - 在 `agent-team/handoffs/AA4-004-Audit.md` 写入 F1–F4（search overlay metadata、FindModel state、replace preview/captureMatches、MarkdownRenderer owner filtering）并列出 Porter/QA 依赖。
    - 更新 `docs/reports/audit-checklist-aa4.md#cl8`（状态切换为 “Audit Complete – Awaiting Fix”，同步主要差异与测试钩子）。
  - 2025-11-21: 汇总 Sprint 02 Phase 7（CL8）最新 delta，与 `docs/sprints/sprint-02.md` / `agent-team/task-board.md` / `docs/reports/migration-log.md` / `agent-team/indexes/README.md#delta-2025-11-21` 对齐；编写 Porter-CS AA4-008 交付 addendum，明确 TS vs C# 差异、文件级 TODO、测试挂钩与 DocMaintainer/Info-Indexer changefeed 依赖。
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
