# AI Team Indexes

> 由 Info-Indexer 维护的摘要与索引集合，用于快速检索关键信息，减轻核心文档负担。

## Current Indexes
| Name | Description | Last Updated |
| --- | --- | --- |
| [Core Docs Index](core-docs-index.md) | 核心文档的用途、Owner、更新时间与缺口行动列表 | 2025-11-20 |

## Contributing Guidelines
1. 每个索引文件命名为 `<topic>-index.md`。
2. 每个索引包含：目标、关键文件引用、摘要表、更新日志。
3. 当索引吸收了部分冗余内容后，应在原文档中留下指针或精简说明。

## Delta (2025-11-19)
- Added: 创建 `core-docs-index.md`（OI-002），覆盖 AGENTS / Sprint / Meeting / Task Board / Main Loop / Playbook。
- Compressed: 暂无（等待 DocMaintainer 执行 OI-001/OI-004）。
- Updated: 登记 PT-003 类型映射（`agent-team/type-mapping.md`）与 PT-004/005 产物（`src/PieceTree.TextBuffer/README.md#porting-log`、`src/PieceTree.TextBuffer.Tests/TestMatrix.md`）的引用，供后续 changefeed 消费。
- Updated: 登记 PT-010 产物（`src/PieceTree.TextBuffer/Core/PieceTreeNormalizer.cs`、`src/PieceTree.TextBuffer.Tests/PieceTreeNormalizationTests.cs`）与 Handoffs（`agent-team/handoffs/PT-010-Brief.md`、`agent-team/handoffs/PT-010-Result.md`）。
- Updated: 登记 PT-011 产物（`src/PieceTree.TextBuffer.Tests/TestMatrix.md` 更新）与 Handoffs（`agent-team/handoffs/PT-011-Result.md`）。PT-004/PT-005 状态变更为 Done。
- Updated: 登记 Phase 2 (TM-001~TM-005) 产物：`TextModel.cs`, `Selection.cs`, `Cursor.cs` 及对应测试。Tests (33/33) 通过。
- Updated: 登记 Phase 3 (DF-001~DF-005) 产物：`DiffComputer.cs`, `IntervalTree.cs`, `MarkdownRenderer.cs` 及对应测试。Tests (50/50) 通过。
- Updated: 登记 Phase 4 (AA-001~AA-006) 产物：Audit Reports, `PieceTreeModel` (Split CRLF/Cache), `PieceTreeSearcher` (Word Search), `Cursor` (Sticky Column) 及对应测试。Tests (56/56) 通过。
- Blocked: Planner 仍需在 OI-003 中补充 runSubAgent 模板的 Indexing Hooks，Info-Indexer 暂以 README 记载待办。

## Delta (2025-11-20)
- Updated: 登记 AA2-005 Remediation：`src/PieceTree.TextBuffer/Core/PieceTreeModel.Edit.cs`（CRLF 修复 + 元数据回填）、`PieceTreeNode.cs`（Detach 标记）、`PieceTreeSearchCache.cs`（失效策略）与 `UnitTest1.cs` 新增 CRLF/Metadata/SearchCache 测试。`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（60/60）结果同步至迁移日志。
- Updated: 登记 AA2-005 Undo/Redo 复刻：`src/PieceTree.TextBuffer/TextModel.cs`、`EditStack.cs`、`TextModelOptions.cs` 与 `TextModelTests.cs`。顶层 API 现包含 `pushEditOperations/pushEOL/setEOL/undo/redo`、模型选项解析、`OnDidChangeOptions` / `OnDidChangeLanguage` 事件及 6 个新增单元测试；`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（66/66）。
- Updated: 登记 AA2-006 搜索/差异/装饰补丁：`PieceTreeSearcher.cs` 现支持 Unicode 正则捕获与单词边界，`DiffComputer.cs`/`DiffComputerOptions.cs` 引入 prettify & move 跟踪，`Decorations/IntervalTree.cs` 与 `ModelDeltaDecoration.cs` 加入 stickiness 与 owner delta，`TextModel.cs`、`Cursor.cs`、`MarkdownRenderer.cs` 则消费这些事件；`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（71/71）。
- Added: Sprint 01（AA3）立项——创建 `docs/sprints/sprint-01.md`、`docs/reports/audit-checklist-aa3.md`，并将 `agent-team/task-board.md` 切换为 Phase 6（AA3/OI），旧板存档至 `agent-team/task-board-v5-archive.md` 以承载 Phase 5 历史。CL1~CL4 清单将通过 runSubAgent 串联 Investigator/Porter/QA/Info-Indexer。
- Updated: 登记 AA3-003 TextModel 选项 / Undo / 多选区搜索补丁：`TextModel.cs`、`TextModelOptions.cs`、`EditStack.cs`、`TextModelSearch.cs` 以及新建的 `Services/ILanguageConfigurationService.cs` 与 `Services/IUndoRedoService.cs`。测试扩展 `TextModelTests` 与 `TextModelSearchTests`（`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`）。
- Updated: 登记 AA3-004 CL2 Search/Regex 修复：`SearchTypes.cs` 应用 ECMAScript 选项与 Unicode wildcard 改写，`PieceTreeSearcher.cs` 强制 ECMAScript 运行模式，`PieceTreeSearchTests.cs`/`TextModelSearchTests.cs` 补入 caf 边界、digit-only、NBSP/EN SPACE、emoji 量词与多选区回归；`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（84/84）。
- Updated: 登记 AA3-006 Diff/move parity：`DiffComputer.cs`/`DiffComputerOptions.cs`/`DiffResult.cs` 现产生 TS 风格 `LinesDiff` + `DiffMove` 元数据，新建 `LineRange*`/`RangeMapping`/`ComputeMovedLines` 等基础设施，并在 `DiffTests.cs` 增补 word diff、trim-whitespace、move detection、timeout 覆盖；`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（80/80）。
- Updated: 登记 AA3-008 Decorations/DocUI parity：`DecorationsTrees.cs`、`DecorationRangeUpdater.cs`、`TextModel.cs`、`TextModelDecorationsChangedEventArgs.cs`、`MarkdownRenderer.cs`/`MarkdownRenderOptions.cs` 及对应测试（`DecorationTests`、`MarkdownRendererTests`）已完成 TS stickiness/metadata/DocUI 对齐；`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（85/85）。
- Updated: 记录 AA3-009 QA 复核结果，`agent-team/handoffs/AA3-009-QA.md` / `docs/reports/audit-checklist-aa3.md#cl4` / `src/PieceTree.TextBuffer.Tests/TestMatrix.md` / `docs/sprints/sprint-01.md` / `AGENTS.md` 均注明 88/88 装饰&DocUI 覆盖，并引用既有 AA3-008 delta；无需新增 `docs/reports/migration-log.md` 行，但 Task Board & Sprint log 现统一指向本条 changefeed，确认 AGENTS / Sprint 01 / Task Board 三者已对齐 AA3-009 完成状态。
