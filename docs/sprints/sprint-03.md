# Sprint 03 – TS Test Alignment & DocUI Execution
- **Date Range:** 2025-11-22 ~ 2025-11-29
- **Theme:** 完成 AA4 Batch #1（DocUI ReplacePattern）交付并启动 TS Test Alignment 下一阶段，确保 Porter→QA→Info-Indexer→DocMaintainer 通过 runSubAgent 循环推进，每次循环结束都在本文件登记进度。
- **RunSubAgent 周期:** 本 Sprint 以“1 次 runSubAgent = 1 个多步循环”计数。每当某位 AI 员工被调用，必须在回报前先更新 `docs/sprints/sprint-03.md` 的 `Progress Log`，标注任务、成果与下一步。管理者（主 Agent）在汇报后同步 `docs/plans/ts-test-alignment.md` 的 Live Checkpoints。

## Objectives
1. **Batch #1 – ReplacePattern**
   - Porter-CS：落地 `ReplacePattern.cs`、`DocUIReplaceController`、`ReplacePatternTests.cs`（测试数据内联，无需 fixtures/snapshots）。
   - QA-Automation：运行 Batch #1 命令（全量 `dotnet test`、`ReplacePatternTests` filter），产出 TRX 与 QA 报告。
   - Info-Indexer：发布 `agent-team/indexes/README.md#delta-2025-11-22`，同步迁移日志。
   - DocMaintainer：引用新 delta 更新 AGENTS / Sprint / Task Board / 计划。
2. **TS Test Alignment – Batch #2 Ready**
   - Investigator-TS：补齐 WordSeparator/SearchContext 规格与 DocUI widget 测试路径调研成果，写入 `docs/plans/ts-test-alignment.md` Appendix。
   - Planner：拆解 Batch #2（FindModel/FindController）runSubAgent 顺序并登记到 Task Board。
   - QA-Automation：草拟 Batch #2 测试矩阵条目与命令草案。
3. **OI Backlog Refresh**
   - Info-Indexer：在 `agent-team/indexes/OI-backlog.md`（若无则创建）登记 OI-012~OI-015（DocUI widget 测试、Snapshot tooling、WordSeparator parity、DocUI harness 设计）。

## Deliverables & Tracking
| ID | Deliverable | Owner | RunSubAgent Step | Evidence / Files | Status |
| --- | --- | --- | --- | --- | --- |
| B1-PORTER | ReplacePattern runtime/controller/tests 实现 | Porter-CS | 1 | `ReplacePattern.cs`, `DocUIReplaceController.cs`, `ReplacePatternTests.cs` | ✅ Done |
| B1-QA | Batch #1 QA 运行 + TRX + snapshots | QA-Automation | 2 | TRX (`batch1-full.trx`, `batch1-replacepattern.trx`), `B1-QA-Result.md` | ✅ Done |
| B1-INFO | Changefeed `#delta-2025-11-22` 发布 | Info-Indexer | 3 | `agent-team/indexes/README.md`, `docs/reports/migration-log.md` | ✅ Done |
| B1-DOC | AGENTS/Sprint/Task Board 更新 | DocMaintainer | 4 | `AGENTS.md`, `docs/sprints/sprint-03.md`, `agent-team/task-board.md`, `docs/plans/ts-test-alignment.md` | ✅ Done |
| B2-INV | WordSeparator spec & DocUI widget 测试定位 | Investigator-TS | 5 | `docs/plans/ts-test-alignment.md` Appendix, notes | Planned |
| B2-PLAN | Batch #2 调度方案 | Planner | 6 | `agent-team/task-board.md`, `docs/plans/ts-test-alignment.md` | Planned |
| B2-QA | Batch #2 测试矩阵扩展 | QA-Automation | 7 | `tests/TextBuffer.Tests/TestMatrix.md`, QA memo | ✅ Done |
| OI-REFRESH | OI backlog 记录 OI-012~015 | Info-Indexer | 8 | `agent-team/indexes/oi-backlog.md` | ✅ Done |
| B3-FM | SelectAllMatches 多光标语义 + FM-01/FM-02 测试 | Porter-CS | 9 | `src/TextBuffer/DocUI/FindModel.cs`, `tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs`, `agent-team/handoffs/B3-FM-Result.md` | ✅ Done |
| B3-FC | DocUI FindController core（host/clipboard/storage + commands + tests） | Porter-CS | 10 | `src/TextBuffer/DocUI/DocUIFindController.cs`, `tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs`, `agent-team/handoffs/B3-FC-Result.md` | ✅ Done |
| B3-FC-Scope | FindController searchScope lifecycle + Ctrl/Cmd+F3 whitespace fix | Porter-CS | 11 | `src/TextBuffer/DocUI/DocUIFindController.cs`, `tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs`, `agent-team/handoffs/B3-FC-Review.md` | ✅ Done |

## Progress Log
> 规则：每次 runSubAgent 调用结束前，由该员工在下表新增一行，包含序号（Run #）、日期、员工、任务、结果、下一步。

| Run # | Date | Employee | Task | Result | Next Steps |
| --- | --- | --- | --- | --- | --- |
| R0 | 2025-11-22 | Main Agent | Sprint 03 建立 | 创建本文件，定义目标/RunSubAgent 节奏/跟踪规则 | 启动 B1-PORTER |
| R1 | 2025-11-22 | Porter-CS | B1-PORTER ReplacePattern 实现 | 交付 `ReplacePattern.cs`, `DocUIReplaceController.cs`, `ReplacePatternTests.cs` (23 tests)；dotnet test 142/142 通过；详见 `agent-team/handoffs/B1-PORTER-Result.md` | 启动 B1-QA |
| R2 | 2025-11-22 | QA-Automation | B1-QA ReplacePattern 验证 | 全量测试 142/142，专项测试 23/23，100% 通过；已更新 `TestMatrix.md`；TRX 文件已生成；详见 `agent-team/handoffs/B1-QA-Result.md` | 启动 B1-INFO |
| R3 | 2025-11-22 | Info-Indexer | B1-INFO Changefeed 发布 | 发布 `#delta-2025-11-22`，更新迁移日志；交叉引用已验证；详见 `agent-team/handoffs/B1-INFO-Result.md` | 启动 B1-DOC |
| R4 | 2025-11-22 | DocMaintainer | B1-DOC 文档同步 | 更新 AGENTS/Sprint/Task Board/Plan 4 个文档；Changefeed 引用统一为 `#delta-2025-11-22`；一致性检查通过；详见 `agent-team/handoffs/B1-DOC-Result.md` | **Batch #1 完成** |
| R4 | 2025-11-22 | DocMaintainer | B1-DOC 文档同步 | 更新 `AGENTS.md`、`docs/sprints/sprint-03.md`、`agent-team/task-board.md`、`docs/plans/ts-test-alignment.md`；所有 changefeed 引用已统一为 `#delta-2025-11-22`；详见 `agent-team/handoffs/B1-DOC-Result.md` | Batch #1 完成，准备 B2-INV |
| R5 | 2025-11-22 | Investigator-TS | B2-INV WordSeparator 规格调研 | 补全 WordSeparator 规格（Appendix B），确认 FindWidget 测试不存在，列出 Batch #2 依赖清单；详见 `agent-team/handoffs/B2-INV-Result.md` | 启动 B2-PLAN |
| R6 | 2025-11-22 | Planner | B2-PLAN Batch #2 任务拆解 | 拆解为 B2-001~005（FindModel stubs → 核心逻辑 → 测试 → changefeed → 文档）；登记 Task Board；详见 `agent-team/handoffs/B2-PLAN-Result.md` | 并行启动 B2-QA + OI-REFRESH |
| R7 | 2025-11-22 | QA-Automation | B2-QA 测试矩阵草拟 | 选择 15 个核心测试场景（P0/P1/P2 分级），设计 `TestEditorContext` harness，定义 Porter-CS API 契约；详见 `agent-team/handoffs/B2-QA-Result.md` | 等待 B2-001 启动 |
| R8 | 2025-11-22 | Info-Indexer | OI-REFRESH Backlog 创建 | 创建 `oi-backlog.md`，登记 OI-012~015（Widget 测试框架、Snapshot 工具、WordSeparator parity、Harness 标准化）；详见 `agent-team/handoffs/OI-REFRESH-Result.md` | Sprint 03 准备工作完成 |
| R9 | 2025-11-22 | Porter-CS | B2-001 FindModel Stubs 创建 | 创建 `FindReplaceState.cs`、`FindDecorations.cs`（stub）、`FindModel.cs`（空壳）；7 个验证测试，dotnet test 149/149 通过；详见 `agent-team/handoffs/B2-001-Result.md` | 启动 B2-002 |
| R10 | 2025-11-22 | Porter-CS | B2-002 FindModel 核心逻辑 | 完成 `FindDecorations.cs`、`FindModel.cs` 核心功能（搜索/导航/替换）；9 个功能测试，dotnet test 156/156 通过；详见 `agent-team/handoffs/B2-002-Result.md` | 启动 B2-003 |
| R11 | 2025-11-23 | Investigator-TS | B3-INV | Batch #3 规格 & 调度表输出 | 启动 R12 (B3-FM) |
| R12 | 2025-11-23 | Porter-CS | B3-FM SelectAllMatches parity (R12) | 完成 `FindModel.SelectAllMatches` TS 对齐，新增 FM-01/FM-02 测试（搜索范围排序 + 主光标保持），`PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` 186/186 绿；详见 `agent-team/handoffs/B3-FM-Result.md` | 移交 QA/DocMaintainer 处理剩余 Batch #3 场景（FindController/WordBoundary、多光标可视化） |
| R13 | 2025-11-23 | Porter-CS | B3-FSel getSelectionSearchString (R13) | 新增 `FindUtilities` + `IEditorSelectionContext`，`DocUIFindSelectionTests` 覆盖 TS `find.test.ts` 三场景；`PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` 189/189 绿；详见 `agent-team/handoffs/B3-FSel-Result.md`（`#delta-2025-11-23-b3-fsel`） | 启动 R14 – B3-FC-Core（Porter-CS，对应 FindController 导航/Regex 逃逸） |
| R14 | 2025-11-23 | Porter-CS | B3-FC-Core FindController 实现 | 交付 `DocUIFindController` + host/stub（storage/clipboard/selection context）并移植 issue #1857/#3090/#6149/#41027/#9043/#27083/#58604/#38232 测试；`PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindControllerTests --nologo` 10/10，`dotnet test ... --nologo` 199/199。详见 `agent-team/handoffs/B3-FC-Result.md`（`#delta-2025-11-23-b3-fc-core`）。 | R15 聚焦 searchScope lifecycle + Mac 全局剪贴板/富文本 seed（B3-FC-Scope），R16 回到 Decorations stickiness。 |
| R15 | 2025-11-23 | Porter-CS | B3-FC-Scope scope lifecycle + Ctrl/Cmd+F3 regression | 修复 `BuildSearchScope()` 空返回时清空范围的问题（仅在非空范围时更新 `_state.SearchScope`）并让 `NextSelectionMatchFindAction()` 在失败后必定 `Start()` + 重试，新增 `SearchScopePersistsWhenSelectionCollapses` 与 `NextSelectionMatchOnWhitespaceRevealsWidget`，`PIECETREE_DEBUG=0 dotnet test ... --filter DocUIFindControllerTests --nologo` 15/15。详见 `docs/reports/migration-log.md` / `agent-team/indexes/README.md#delta-2025-11-23-b3-fc-scope`。 | 继续 R15 backlog：Mac 全局剪贴板富文本 seed + searchScope UI 提示；R16 进入 Decorations stickiness / DocUI overlay capture。 |
