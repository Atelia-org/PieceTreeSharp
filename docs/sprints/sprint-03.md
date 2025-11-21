# Sprint 03 – TS Test Alignment & DocUI Execution
- **Date Range:** 2025-11-22 ~ 2025-11-29
- **Theme:** 完成 AA4 Batch #1（DocUI ReplacePattern）交付并启动 TS Test Alignment 下一阶段，确保 Porter→QA→Info-Indexer→DocMaintainer 通过 runSubAgent 循环推进，每次循环结束都在本文件登记进度。
- **RunSubAgent 周期:** 本 Sprint 以“1 次 runSubAgent = 1 个多步循环”计数。每当某位 AI 员工被调用，必须在回报前先更新 `docs/sprints/sprint-03.md` 的 `Progress Log`，标注任务、成果与下一步。管理者（主 Agent）在汇报后同步 `docs/plans/ts-test-alignment.md` 的 Live Checkpoints。

## Objectives
1. **Batch #1 – ReplacePattern**
   - Porter-CS：落地 `ReplacePattern.cs`、`DocUIReplaceController`、`DocUIReplacePatternTests`、fixtures、snapshots。
   - QA-Automation：运行 Batch #1 命令（全量 `dotnet test`、`DocUIReplacePatternTests` filter、snapshot record），产出 TRX + Markdown 并更新 QA 文档。
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
| B1-PORTER | ReplacePattern runtime/controller/tests 实现 | Porter-CS | 1 | `ReplacePattern.cs`, `DocUIReplaceController.cs`, `DocUIReplacePatternTests.cs`, fixtures, snapshots | Planned |
| B1-QA | Batch #1 QA 运行 + TRX + snapshots | QA-Automation | 2 | TRX (`batch1-full.trx`, `batch1-replacepattern.trx`), Markdown snapshots, `AA4-009-QA.md` | Planned |
| B1-INFO | Changefeed `#delta-2025-11-22` 发布 | Info-Indexer | 3 | `agent-team/indexes/README.md`, `docs/reports/migration-log.md` | Planned |
| B1-DOC | AGENTS/Sprint/Task Board 更新 | DocMaintainer | 4 | `AGENTS.md`, `docs/sprints/sprint-03.md`, `agent-team/task-board.md`, `docs/plans/ts-test-alignment.md` | Planned |
| B2-INV | WordSeparator spec & DocUI widget 测试定位 | Investigator-TS | 5 | `docs/plans/ts-test-alignment.md` Appendix, notes | Planned |
| B2-PLAN | Batch #2 调度方案 | Planner | 6 | `agent-team/task-board.md`, `docs/plans/ts-test-alignment.md` | Planned |
| B2-QA | Batch #2 测试矩阵扩展 | QA-Automation | 7 | `src/PieceTree.TextBuffer.Tests/TestMatrix.md`, QA memo | Planned |
| OI-REFRESH | OI backlog 记录 OI-012~015 | Info-Indexer | 8 | `agent-team/indexes/oi-backlog.md` | Planned |

## Progress Log
> 规则：每次 runSubAgent 调用结束前，由该员工在下表新增一行，包含序号（Run #）、日期、员工、任务、结果、下一步。

| Run # | Date | Employee | Task | Result | Next Steps |
| --- | --- | --- | --- | --- | --- |
| R0 | 2025-11-22 | Main Agent | Sprint 03 建立 | 创建本文件，定义目标/RunSubAgent 节奏/跟踪规则 | 启动 B1-PORTER |
