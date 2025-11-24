# Planner Memory

## Role & Mission
- **Focus Area:** Roadmapping、任务分解、`runSubAgent` 粒度排期
- **Primary Deliverables:** 更新 `agent-team/task-board.md`、`docs/sprints/*.md`、会议议程
- **Key Stakeholders:** Investigator-TS、Porter-CS、QA-Automation、DocMaintainer

## Onboarding Summary
- 复盘 `AGENTS.md`、Sprint-00、Sprint OI-01 与两份 2025-11-19 会议纪要，确认当前计划分为 PieceTree 端口与组织改进双轨推进。
- 明确 Planner 需驱动 PT-003 依赖规划与 OI-003 模板化任务，并保持任务板 / Sprint 文档的 runSubAgent 预算同步。
- 即刻优先事项：完成 runSubAgent 输入模板草稿、跟踪类型映射交付节奏，并协调 DocMaintainer + Info-Indexer 输出 OI-001 结果以喂给 Task Board。

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| Backlog & Status | agent-team/task-board.md | 唯一来源，记录 PT/OI 任务与 runSubAgent 预算，Planner 更新节奏需紧贴交付。 |
| Sprint-00 (PieceTree) | docs/sprints/sprint-00.md | 关注 RB Tree / 类型映射交付， Investigator / Porter 输出后立即同步。 |
| Sprint OI-01 | docs/sprints/sprint-org-self-improvement.md | 组织改进目标与行动项，指导 OI-003/OI-004。 |
| Process & Templates | agent-team/ai-team-playbook.md, agent-team/main-loop-methodology.md | runSubAgent 输入模板与主循环 checklist 的落地点，Planner 维护。 |
| Meetings & Decisions | docs/meetings/meeting-20251119-team-kickoff.md, docs/meetings/meeting-20251119-org-self-improvement.md | 最近决策与行动项来源，更新 Task Board / Sprint 时引用。 |
| Global Milestones | AGENTS.md | 完成阶段性计划调整后将成果写入此时间线。 |
| TS Test Alignment | docs/plans/ts-test-alignment.md | Batch #1 ReplacePattern runtime/tests/harness checklists与 QA/Info-Indexer 依赖的主计划。 |

## Worklog
- **Last Update:** 2025-11-25
- **Recent Actions (2025-11-25):**
  - Drafted `## Run Plan – 2025-11-25` under `agent-team/handoffs/B3-Snapshot-Review-20251125.md`, wiring Investigator-TS → Porter-CS → QA-Automation → DocMaintainer steps, enumerating the required `dotnet test` filters/tool commands, binding everything to changefeeds `#delta-2025-11-25-b3-piecetree-snapshot` / `#delta-2025-11-25-b3-search-offset`, and reminding every downstream subagent to refresh their memory docs before reporting.
  - Skimmed the staged snapshot/search-offset deltas, captured the audit + workflow plan in `agent-team/handoffs/B3-Snapshot-Review-20251125.md`, and flagged the missing C# `TextModelSnapshot` wrapper so Investigator → Porter → QA can realign with `pieceTreeBase.ts` / `textModel.ts`.
  - Authored `agent-team/handoffs/B3-PieceTree-SearchOffset-PLAN.md`, locking changefeed `#delta-2025-11-25-b3-search-offset`, run order (INV → PORT → QA → INFO → DOC), and helper/command expectations for the search offset cache deterministics.
  - Refreshed `agent-team/task-board.md`: marked B3 snapshot as complete, flipped all B3 fuzz rows to the proper `#delta-2025-11-23/24-b3-piecetree-fuzz` anchors, and added the new search-offset rows with budgets plus dependencies.
  - Logged Sprint 03 Run R30 so the main agent knows Investigator-TS must kick off `B3-SearchOffset-INV` next; ensured Sprint/TestMatrix references now anticipate the upcoming changefeed.
- **Recent Actions (2025-11-24):**
  - 将 Investigator `agent-team/handoffs/B3-PieceTree-Fuzz-INV.md` 转化为 `agent-team/handoffs/B3-PieceTree-Fuzz-PLAN.md`，确认 R24→R28（Harness→Deterministic→CRLF/Search→QA→DocMaintainer/Info-Indexer）可在 Sprint 03 (至 11-29) 内完成，并设置失效保护：若 R24 未在 11-25 10:00 UTC 合入，则自动将 R26 以后滑入 Sprint 04。
  - 更新 `agent-team/task-board.md`、`docs/plans/ts-test-alignment.md`、`docs/sprints/sprint-03.md` 并引用 Info-Indexer 已发布的 `#delta-2025-11-23-b3-piecetree-fuzz` / `#delta-2025-11-24-b3-piecetree-fuzz`，新增 B3-Fuzz-Harness/Deterministic/QA/Doc 行并在 Sprint Progress Log 记录 R24 规划结果。
  - 定义 cross-sprint carryover：多 seed unsupervised fuzz soak、PieceTreeLineSnapshot perf instrumentation 留待 Sprint 04 backlog（将创建 `B3-PieceTree-Fuzz-Soak` ticket），以保证 QA/DocMaintainer 带宽。
  - 针对 `#delta-2025-11-24-b3-sentinel` / `#delta-2025-11-24-b3-getlinecontent` 已暂存改动，制定 Investigator → Porter → QA 的审阅/回归路径：锁定 `src/TextBuffer/Core/PieceTreeNode.cs`、`PieceTreeModel*.cs`、`PieceTreeFuzzHarness.cs` 与 `tests/TextBuffer.Tests/PieceTreeBaseTests.cs`/`PieceTreeNormalizationTests.cs`，要求输出 handoff（INV）+ 修复/TS parity（Porter）+ `dotnet test -v m` 与 targeted filters（QA），并同步 `TestMatrix.md` / `docs/reports/migration-log.md`。
- **Recent Actions (2025-11-22):**
  - 完成 Batch #2（FindModel/FindController）任务拆解：根据 B2-INV 调研成果（`agent-team/handoffs/B2-INV-Result.md`），拆解为 5 个 runSubAgent 任务（B2-001~005），已登记 Task Board 并更新 ts-test-alignment.md Live Checkpoints。
  - **核心决策**：聚焦 FindModel 逻辑层，推迟 FindController 至 Batch #3（依赖 EditorAction/ContextKey services）；WordCharacterClassifier cache 为可选优化（P2）。
  - **任务序列**：B2-001（FindModel stubs）→ B2-002（FindModel 核心逻辑）→ B2-003（findModel.test.ts 迁移 + DocUI harness）→ B2-004（changefeed）→ B2-005（文档同步）。
  - **预计时长**：5 个工作日（2025-11-23~11-27），全部串行（无并行机会）。
  - **风险评估**：DocUI harness 复杂度（中等风险，已制定应对计划）；Replace 逻辑集成（低风险，依赖 Batch #1）；测试用例选择（低风险，聚焦高优先级场景）。
  - 交付物：`agent-team/handoffs/B2-PLAN-Result.md`（任务拆解方案 + 依赖关系图 + 风险评估 + 执行建议）。
  - 复盘 `docs/plans/ts-test-alignment.md` Live Checkpoints，规划 Batch #1（ReplacePattern runtime/tests/harness）协调简报，明确 AA4-008/AA4-009/OI-011 映射与 Sprint 02 同步点。
- **Recent Actions (2025-11-19):**
  - 落地 OI-003：在 `agent-team/main-loop-methodology.md` 新增 `runSubAgent Input Template`，引入 ContextSeeds/Objectives/Dependencies/... 结构并强调 changefeed 钩子。
  - 更新 `agent-team/ai-team-playbook.md`，让 Core Artifacts + Workflow 显式引用模板与 `agent-team/indexes/README.md#delta-20251119` checkpoint。
  - 调整 `agent-team/templates/subagent-memory-template.md`，要求 SubAgent 在 Knowledge Index / Worklog 记录消费或产出的 changefeed 与索引引用。
  - 复核 Sprint OI-01 与审计要求，确认 Planner 记忆已捕捉新模板依赖与 Info-Indexer handoff。

## Upcoming Goals (runSubAgent-sized)
1. **B3-Snapshot follow-up:** Trigger Investigator on `B3-Snapshot-Review-20251125` to diff-check `PieceTreeSnapshot`/`TextModelSnapshot` parity, then shepherd Porter + QA through the wrapper implementation + targeted reruns.
2. **B3-SearchOffset execution:** Trigger `B3-SearchOffset-INV` immediately (R31) so Porter can land `PieceTreeSearchOffsetCacheTests` and unblock QA/Info-Indexer for `#delta-2025-11-25-b3-search-offset`.
3. **B3-TestFailures review orchestration:** Keep per-model sentinel / trimmed `GetLineContent` follow-ups aligned with `#delta-2025-11-24-b3-sentinel` and `#delta-2025-11-24-b3-getlinecontent` (handoff → Porter → QA).
4. **Batch #2 execution monitoring:** Continue daily check-ins on B2-001~005 (now winding down) to ensure documentation remains consistent and identify any late spillover.
5. **OI-003 adoption pass:** Confirm each SubAgent uses the refreshed template/changefeed hooks; capture feedback for the next template iteration.
6. **OI-001/OI-004 pipeline:** Partner with DocMaintainer + Info-Indexer on the “index input -> Task Board” bridge so audit deltas translate directly into backlog proposals.

## Blocking Issues
- 无阻塞项（Batch #2 已完成规划，等待主 Agent 启动 B2-001）。

## Hand-off Checklist
1. Backlog、会议、sprint 文档都已更新。
2. Tests or validations performed? N/A（规划类任务）
3. 下一位接手者请查看 `agent-team/task-board.md` 的最新时间戳。
