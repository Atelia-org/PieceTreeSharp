# Sprint 01 – Alignment & Audit R3
- **Date Range:** 2025-11-20 ~ 2025-11-27
- **Theme / Goal:** 新一轮 TS↔C# 对照审核与修复，聚焦 TextModel/搜索/Diff/Decorations/DocUI 的高级语义。
- **Success Criteria:**
  - CL1~CL4（见 `docs/reports/audit-checklist-aa3.md`）全部由 Investigator-TS 交付差异清单并落盘。
  - 相应 Porter-CS 修复补丁落地，新增/更新的 API 与行为记录在 `docs/reports/migration-log.md` 与 changefeed。
  - QA 增补测试覆盖（`TextModelTests`、`PieceTreeSearchTests`、`DiffTests`、`DecorationTests`、`MarkdownRendererTests`）并完成 `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` 绿色基线。
  - AGENTS / Sprint / Task Board / indexes 同步到 AA3 状态，确保下一轮主循环可直接加载。

**Status Edits Reminder:** 在修改本 Sprint 状态或条目前，先查阅 `docs/reports/migration-log.md` 以及 `agent-team/indexes/README.md#delta-2025-11-20`，并在更新时附上对应引用。

## Backlog Snapshot
| Priority | Task | Description & Deliverables | runSubAgent Budget | Owner | Target Date | Dependencies | Status / Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| P0 | AA3-001 | Investigator-TS：完成 CL1（TextModel options / metadata / events）对照，输出 `agent-team/handoffs/AA3-001-Audit.md` 并更新 `docs/reports/audit-checklist-aa3.md#cl1`。 | 2 | Investigator-TS | 2025-11-22 | Sprint 01 kickoff、changefeed checkpoint | Done – `AA3-001-Audit.md` merged |
| P0 | AA3-002 | Investigator-TS：完成 CL2（Search/Replace & regex captures）对照，输出 `agent-team/handoffs/AA3-002-Audit.md` + checklist更新。 | 2 | Investigator-TS | 2025-11-22 | 同上 | Done – `AA3-002-Audit.md` merged |
| P1 | AA3-005 | Investigator-TS：完成 CL3（Diff prettify & move metadata）对照，输出 `agent-team/handoffs/AA3-005-Audit.md`。 | 2 | Investigator-TS | 2025-11-23 | CL1/CL2 context | Done – `AA3-005-Audit.md`(F1–F4) |
| P1 | AA3-007 | Investigator-TS：完成 CL4（Decorations & Markdown DocUI）对照，输出 `agent-team/handoffs/AA3-007-Audit.md`。 | 2 | Investigator-TS | 2025-11-23 | CL1/CL2 context | Done – `AA3-007-Audit.md`(F1–F4) |
| P0 | AA3-003 | Porter-CS：根据 CL1 结论执行 TextModel/Options 修复，记录在 `agent-team/handoffs/AA3-003-Result.md` + 迁移日志。 | 3 | Porter-CS | 2025-11-24 | AA3-001 | Done – `agent-team/handoffs/AA3-003-Result.md` + `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（79/79）并已在 `agent-team/indexes/README.md#delta-2025-11-20` 备案 |
| P0 | AA3-004 | Porter-CS：根据 CL2 结论执行 Search/Replace 修复，记录在 `agent-team/handoffs/AA3-004-Result.md`。 | 3 | Porter-CS | 2025-11-24 | AA3-002 | Done – ECMAScript regex + emoji parity landed |
| P1 | AA3-006 | Porter-CS：根据 CL3 结论修复 Diff/move metadata（`DiffComputer`, `MarkdownRenderer` consumers）。 | 3 | Porter-CS | 2025-11-25 | AA3-005 | Done – LinesDiff/move parity + tests landed |
| P1 | AA3-008 | Porter-CS：根据 CL4 结论修复 Decorations/DocUI。 | 3 | Porter-CS | 2025-11-25 | AA3-007 | Done – `agent-team/handoffs/AA3-008-Result.md` + `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（85/85），DocUI closure由 `agent-team/handoffs/AA3-009-QA.md` 与 changefeed `agent-team/indexes/README.md#delta-2025-11-20` 佐证 |
| P0 | AA3-009 | QA-Automation：扩展 `TextModelTests`/`PieceTreeSearchTests`/`DiffTests`/`DecorationTests`/`MarkdownRendererTests` 以覆盖新语义，并记录 `dotnet test ...` 最新基线。 | 2 | QA-Automation | 2025-11-26 | AA3-003~008 | Done – `agent-team/handoffs/AA3-009-QA.md` 记录 CL4 decoration metadata + DocUI diff snapshots（`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` 88/88；changefeed 引用 `agent-team/indexes/README.md#delta-2025-11-20`）。 |
| P0 | OI-010 | Info-Indexer：整理 Sprint 01 产物，更新 changefeed + 索引目录，确保 DocMaintainer/Task Board 可引用。 | 1 | Info-Indexer | 2025-11-27 | All AA3 deliverables | Done – 2025-11-20 发布 `agent-team/indexes/README.md#delta-2025-11-20` 中的 AA3-009 QA changefeed |

## Plan
### Milestone 1 – Investigator Sweep (Nov 20–22)
- Deliverables: `agent-team/handoffs/AA3-001/002/005/007` + `docs/reports/audit-checklist-aa3.md` 填充。
- Tests / Validation: Cross-check against TS sources、记录发现→建议→影响等级；DocMaintainer 复核引用链。

### Milestone 2 – Porter Remediation (Nov 23–25)
- Deliverables: `agent-team/handoffs/AA3-003/004/006/008`，代码提交 + 迁移日志 + changefeed。
- Tests / Validation: 每项修复配套 `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`，必要时新增 targeted tests。

### Milestone 3 – QA & Broadcast (Nov 26–27)
- Deliverables: QA 回归报告、AGENTS/Sprint/Task Board/Indexes 更新、DocUI Demo 笔记。
- Tests / Validation: Consolidated `dotnet test ...`（>= 新覆盖）、DocMaintainer consistency review。

## Risks & Mitigations
| Risk | Impact | Mitigation |
| --- | --- | --- |
| Investigator 产出过长导致主上下文拥堵 | Medium | 通过 `docs/reports/audit-checklist-aa3.md` & handoff 文件分层记载，主 Agent 仅引用摘要 |
| Search/Diff 变更影响既有 71/71 基线 | High | Porter 每轮提交前运行 `dotnet test ...` 并附日志；QA 再次汇总 |
| Doc/Index 更新延迟造成 changefeed 断档 | Medium | OI-010 作为 hard gate；Broadcast 前复查 `agent-team/indexes/README.md#delta-2025-11-20` |

## Demo / Review Checklist
- [ ] CL1~CL4 Investigator Audit 完成并链接到 checklist / handoff。
- [ ] 相应 Porter 修复 merged，`docs/reports/migration-log.md` + changefeed 登记。
- [ ] QA 回归通过、TestMatrix 更新、`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` 绿色。
- [ ] AGENTS / Task Board / Sprint / Indexes 同步 Sprint 01 状态。
- [ ] 记录下一 Sprint 候选与待压缩文档列表。

## Progress Log
- 2025-11-20：Sprint 01 立项 —— 创建 `docs/reports/audit-checklist-aa3.md` 作为 CL1~CL4 清单，并刷新 `agent-team/task-board.md`（Phase 6）。
- 2025-11-20：AA3-001（CL1 Investigator）完成 —— 生成 `agent-team/handoffs/AA3-001-Audit.md`，在 `docs/reports/audit-checklist-aa3.md#cl1` 登记 F1~F5，Task Board 标记 Done，等待 Porter-CS 接手 AA3-003。
- 2025-11-20：AA3-002（CL2 Investigator）完成 —— 生成 `agent-team/handoffs/AA3-002-Audit.md`，总结 ECMAScript regex、word separator、surrogate 差异；`docs/reports/audit-checklist-aa3.md#cl2` 与 Task Board 已更新，待 AA3-004 修复。
- 2025-11-20：AA3-003（CL1 Porter）完成实现 —— `TextModel` 创建选项、语言配置事件、Undo 服务桥接与 multi-range 搜索全部对齐 TS；`agent-team/handoffs/AA3-003-Result.md` + `docs/reports/migration-log.md` 已登记，`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（79/79）验证通过。
- 2025-11-20：AA3-005（CL3 Investigator）交付 Diff/move 审计 —— `agent-team/handoffs/AA3-005-Audit.md` 记录 F1（LinesDiff/innerChanges 差异）、F2（move detection）、F3（options/timeout/subword heuristics）、F4（DocUI diff plumbing），`docs/reports/audit-checklist-aa3.md#cl3` 与 Task Board 均已更新。
- 2025-11-20：AA3-007（CL4 Investigator）交付 Decorations/DocUI 审计 —— `agent-team/handoffs/AA3-007-Audit.md` 收敛 ModelDecorationOptions、DecorationsTrees/stickiness、MarkdownRenderer 渲染缺口，并将 CL4 状态和 Task Board 更新为“Done”。
- 2025-11-20：AA3-004（CL2 Porter）完成 —— `SearchTypes`/`PieceTreeSearcher`/`TextModelSearch` 现运行 ECMAScript Regex + surrogate-safe wildcard，`PieceTreeSearchTests`/`TextModelSearchTests` 新增 audit 案例，`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（84/84）。
- 2025-11-20：AA3-006（CL3 Porter）完成 —— `DiffComputer` 替换为 TS `LinesDiff` 管线，`DiffMove`/`DiffResult` 暴露 inner changes + timeout，`DiffTests` 覆盖 word diff、whitespace ignore、move detection、timeout；`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（80/80）。
- 2025-11-20：AA3-008（CL4 Porter）完成 —— `DecorationsTrees` + `DecorationRangeUpdater` 复刻 TS stickiness，`TextModel` 事件输出 minimap/overview/glyph/line-height/font 元数据，`MarkdownRenderer` 渲染 owner filter / injected text / glyph & minimap 注解；`agent-team/handoffs/AA3-008-Result.md` 发布，`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（85/85）。
- 2025-11-20：AA3-009（QA）完成 —— `agent-team/handoffs/AA3-009-QA.md` 覆盖 injected text/margin/font queries、`forceMoveMarkers` parity 与 DocUI diff markers；`docs/reports/audit-checklist-aa3.md#cl4`、`tests/TextBuffer.Tests/TestMatrix.md`、`AGENTS.md`、`agent-team/task-board.md`、`docs/sprints/sprint-01.md` 更新一致；`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（88/88，changefeed 仍引用 `agent-team/indexes/README.md#delta-2025-11-20`）。
- 2025-11-20：OI-010（Info-Indexer）完成 —— 在 `agent-team/indexes/README.md#delta-2025-11-20` 新增 AA3-009 QA changefeed，刷新 `core-docs-index.md` 与 Task Board/Sprint 状态，确认 AGENTS / Sprint 01 / Task Board 共享同一 AA3-008 delta 引用；无须新增 `docs/reports/migration-log.md` 行。
- 2025-11-20：AA3-003 / AA3-008 在 AA3-009 QA changefeed (`agent-team/indexes/README.md#delta-2025-11-20`) 下正式标记 Done —— Task Board 与 Sprint backlog 现统一引用该 DocUI canonical pointer。
