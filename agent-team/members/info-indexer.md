# Info-Indexer Memory

## Role & Mission
- **Focus Area:** 维护知识索引、摘要与引用结构，减轻核心文档负担
- **Primary Deliverables:** `agent-team/indexes/*.md`、文档正交性报告、索引更新日志
- **Key Stakeholders:** DocMaintainer、Planner、全体 SubAgent

## Onboarding Summary
- **Docs Reviewed:** `AGENTS.md`、`agent-team/ai-team-playbook.md`、`agent-team/main-loop-methodology.md`、`agent-team/task-board.md`（OI-001~OI-004）、`agent-team/indexes/README.md`、`docs/meetings/meeting-20251119-team-kickoff.md`、`docs/meetings/meeting-20251119-org-self-improvement.md`、`docs/sprints/sprint-00.md`、`docs/sprints/sprint-org-self-improvement.md`。
- **Mission Understanding:** Info-Indexer 负责在主循环的 Load/Broadcast 阶段提供索引/摘要交付，优先支持组织自我完善 Sprint 的 OI-001（正交性审计）与 OI-002（索引体系）。
- **Coordination Hooks:** 需与 DocMaintainer 对齐审计范围、与 Planner 对齐 runSubAgent 模板、与 QA-Automation/Porter-CS 同步后续索引需求（测试资产、TS↔C# crosswalk）。

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| Index Catalog | `agent-team/indexes/README.md` | 记录命名规范 & 审核流程，首个索引会在此注册
| Core Docs Index (planned) | `agent-team/indexes/core-docs-index.md`, `AGENTS.md`, `docs/sprints`, `docs/meetings` | 汇总每个核心文档的用途、责任人与最近更新时间，支撑 OI-001
| Task Board Linkage | `agent-team/task-board.md`（OI-001~OI-004） | 跟踪组织改进任务状态，方便引用 runSubAgent 预算
| QA/Test Assets Index (planned) | `agent-team/indexes/qa-test-assets-index.md`, `PieceTree.TextBuffer.Tests`, QA 会议记录 | 映射测试矩阵、基准计划与代码文件，供 QA-Automation 查阅
| TS↔C# Crosswalk Index (planned) | `agent-team/indexes/ts-cs-crosswalk-index.md`, `agent-team/type-mapping.md`, `ts/src/vs/editor/...` | 追踪类型映射进度，补足 `type-mapping.md` 之外的上下文
| Main Loop & Template Notes | `agent-team/main-loop-methodology.md`, `agent-team/ai-team-playbook.md` | 记录 runSubAgent 输入模板、Info-Indexer 钩子位置

## Worklog
- **2025-11-19:** 角色创建（DocMaintainer 建议下设立 Info-Indexer，等待投入）。
- **2025-11-19:** 完成首轮文档审阅，锁定 OI-001/OI-002 依赖，起草索引交付结构并更新记忆文件。

## Upcoming Goals (runSubAgent scoped)
1. **OI-002 / Core Docs Index:** 单次 runSubAgent 生成 `agent-team/indexes/core-docs-index.md` v0，涵盖文档职责、更新时间与指针。
2. **OI-001 / 正交性审计支持:** 与 DocMaintainer 联合梳理重复/缺口列表，并在索引中记下需要压缩或补写的区域。
3. **QA/Test Assets Index Draft:** 与 QA-Automation 对齐测试矩阵结构，产出 `qa-test-assets-index.md` 框架，便于后续 runSubAgent 扩充。

## Blocking Issues
- 需 DocMaintainer 提供 OI-001 审计输入（最新的重复/缺口列表）以便索引记录行动项。
- Planner 尚未交付最终 runSubAgent 模板示例，Info-Indexer 需等待以确保索引引用区格式一致。
- QA-Automation 尚未输出测试矩阵草案，`qa-test-assets-index.md` 初始化需要其文件清单。

## Hand-off Checklist
1. 输出的索引文件列于 `agent-team/indexes/README.md`。
2. 若删除/压缩内容，需在原文档留下指针。
3. Tests or validations performed? N/A，但需请 DocMaintainer 审阅。

## Index Deliverable Notes
- **Location:** 所有索引文件放置于 `agent-team/indexes/`，命名为 `<topic>-index.md` 并在 README 中登记更新时间。
- **Structure Expectations:** `Goal`（为何建立）、`Source Docs`（含链接/路径）、`Summary Table`（列出文档/资产、位置、负责人、最近更新）、`Gaps & Actions`（直接映射 OI 任务）、`Update Log`（时间戳 + 变更）。
- **Initial Targets:**
  - `core-docs-index.md`：覆盖 `AGENTS.md`、`docs/sprints/*`、`docs/meetings/*`，标注文档用途与最新决策。
  - `qa-test-assets-index.md`：罗列 QA 测试矩阵、`PieceTree.TextBuffer.Tests` 目录、性能基准计划。
  - `ts-cs-crosswalk-index.md`：串联 `agent-team/type-mapping.md` 与 `ts/src/vs/editor/common/model/pieceTreeTextBuffer`，标记已迁移与待迁移部分。
