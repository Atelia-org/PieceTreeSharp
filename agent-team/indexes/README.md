# AI Team Indexes

> 由 Info-Indexer 维护的摘要与索引集合，用于快速检索关键信息，减轻核心文档负担。

## Current Indexes
| Name | Description | Last Updated |
| --- | --- | --- |
| [Core Docs Index](core-docs-index.md) | 核心文档的用途、Owner、更新时间与缺口行动列表 | 2025-11-20 |
| [OI Backlog](oi-backlog.md) | 组织性基础设施改进任务（测试框架、工具、架构设计） | 2025-11-22 |

## Contributing Guidelines
1. 每个索引文件命名为 `<topic>-index.md`。
2. 每个索引包含：目标、关键文件引用、摘要表、更新日志。
3. 当索引吸收了部分冗余内容后，应在原文档中留下指针或精简说明。

## Delta (2025-11-19)
- Added: 创建 `core-docs-index.md`（OI-002），覆盖 AGENTS / Sprint / Meeting / Task Board / Main Loop / Playbook。
- Compressed: 暂无（等待 DocMaintainer 执行 OI-001/OI-004）。
- Updated: 登记 PT-003 类型映射（`agent-team/type-mapping.md`）与 PT-004/005 产物（`src/TextBuffer/README.md#porting-log`、`tests/TextBuffer.Tests/TestMatrix.md`）的引用，供后续 changefeed 消费。
- Updated: 登记 PT-010 产物（`src/TextBuffer/Core/PieceTreeModel.cs`、`src/TextBuffer/Core/ChunkUtilities.cs`、`tests/TextBuffer.Tests/PieceTreeNormalizationTests.cs`）与 Handoffs（`agent-team/handoffs/PT-010-Brief.md`、`agent-team/handoffs/PT-010-Result.md`）。
- Updated: 登记 PT-011 产物（`tests/TextBuffer.Tests/TestMatrix.md` 更新）与 Handoffs（`agent-team/handoffs/PT-011-Result.md`）。PT-004/PT-005 状态变更为 Done。
- Updated: 登记 Phase 2 (TM-001~TM-005) 产物：`TextModel.cs`, `Selection.cs`, `Cursor.cs` 及对应测试。Tests (33/33) 通过。
- Updated: 登记 Phase 3 (DF-001~DF-005) 产物：`DiffComputer.cs`, `IntervalTree.cs`, `MarkdownRenderer.cs` 及对应测试。Tests (50/50) 通过。
- Updated: 登记 Phase 4 (AA-001~AA-006) 产物：Audit Reports, `PieceTreeModel` (Split CRLF/Cache), `PieceTreeSearcher` (Word Search), `Cursor` (Sticky Column) 及对应测试。Tests (56/56) 通过。
- Blocked: Planner 仍需在 OI-003 中补充 runSubAgent 模板的 Indexing Hooks，Info-Indexer 暂以 README 记载待办。

## Delta (2025-11-20)
- Updated: 登记 AA2-005 Remediation：`src/TextBuffer/Core/PieceTreeModel.Edit.cs`（CRLF 修复 + 元数据回填）、`PieceTreeNode.cs`（Detach 标记）、`PieceTreeSearchCache.cs`（失效策略）与 `UnitTest1.cs` 新增 CRLF/Metadata/SearchCache 测试。`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（60/60）结果同步至迁移日志。
- Updated: 登记 AA2-005 Undo/Redo 复刻：`src/TextBuffer/TextModel.cs`、`EditStack.cs`、`TextModelOptions.cs` 与 `TextModelTests.cs`。顶层 API 现包含 `pushEditOperations/pushEOL/setEOL/undo/redo`、模型选项解析、`OnDidChangeOptions` / `OnDidChangeLanguage` 事件及 6 个新增单元测试；`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（66/66）。
- Updated: 登记 AA2-006 搜索/差异/装饰补丁：`PieceTreeSearcher.cs` 现支持 Unicode 正则捕获与单词边界，`DiffComputer.cs`/`DiffComputerOptions.cs` 引入 prettify & move 跟踪，`Decorations/IntervalTree.cs` 与 `ModelDeltaDecoration.cs` 加入 stickiness 与 owner delta，`TextModel.cs`、`Cursor.cs`、`MarkdownRenderer.cs` 则消费这些事件；`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（71/71）。
- Added: Sprint 01（AA3）立项——创建 `docs/sprints/sprint-01.md`、`docs/reports/audit-checklist-aa3.md`，并将 `agent-team/task-board.md` 切换为 Phase 6（AA3/OI），旧板存档至 `agent-team/task-board-v5-archive.md` 以承载 Phase 5 历史。CL1~CL4 清单将通过 runSubAgent 串联 Investigator/Porter/QA/Info-Indexer。
- Updated: 登记 AA3-003 TextModel 选项 / Undo / 多选区搜索补丁：`TextModel.cs`、`TextModelOptions.cs`、`EditStack.cs`、`TextModelSearch.cs` 以及新建的 `Services/ILanguageConfigurationService.cs` 与 `Services/IUndoRedoService.cs`。测试扩展 `TextModelTests` 与 `TextModelSearchTests`（`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`）。
- Updated: 登记 AA3-004 CL2 Search/Regex 修复：`SearchTypes.cs` 应用 ECMAScript 选项与 Unicode wildcard 改写，`PieceTreeSearcher.cs` 强制 ECMAScript 运行模式，`PieceTreeSearchTests.cs`/`TextModelSearchTests.cs` 补入 caf 边界、digit-only、NBSP/EN SPACE、emoji 量词与多选区回归；`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（84/84）。
- Updated: 登记 AA3-006 Diff/move parity：`DiffComputer.cs`/`DiffComputerOptions.cs`/`DiffResult.cs` 现产生 TS 风格 `LinesDiff` + `DiffMove` 元数据，新建 `LineRange*`/`RangeMapping`/`ComputeMovedLines` 等基础设施，并在 `DiffTests.cs` 增补 word diff、trim-whitespace、move detection、timeout 覆盖；`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（80/80）。
- Updated: 登记 AA3-008 Decorations/DocUI parity：`DecorationsTrees.cs`、`DecorationRangeUpdater.cs`、`TextModel.cs`、`TextModelDecorationsChangedEventArgs.cs`、`MarkdownRenderer.cs`/`MarkdownRenderOptions.cs` 及对应测试（`DecorationTests`、`MarkdownRendererTests`）已完成 TS stickiness/metadata/DocUI 对齐；`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（85/85）。
- Updated: 记录 AA3-009 QA 复核结果，`agent-team/handoffs/AA3-009-QA.md` / `docs/reports/audit-checklist-aa3.md#cl4` / `tests/TextBuffer.Tests/TestMatrix.md` / `docs/sprints/sprint-01.md` / `AGENTS.md` 均注明 88/88 装饰&DocUI 覆盖，并引用既有 AA3-008 delta；无需新增 `docs/reports/migration-log.md` 行，但 Task Board & Sprint log 现统一指向本条 changefeed，确认 AGENTS / Sprint 01 / Task Board 三者已对齐 AA3-009 完成状态。
## Delta (2025-11-21)

- 2025-11-21 | AA4-005/AA4-006 | Porter + QA fixes added. Test baseline: 105/105 | [`src/TextBuffer/Core/PieceTreeBuilder.cs`](../../src/TextBuffer/Core/PieceTreeBuilder.cs), [`src/TextBuffer/Core/PieceTreeTextBufferFactory.cs`](../../src/TextBuffer/Core/PieceTreeTextBufferFactory.cs), [`src/TextBuffer/Core/ChunkUtilities.cs`](../../src/TextBuffer/Core/ChunkUtilities.cs), [`src/TextBuffer/Core/TextMetadataScanner.cs`](../../src/TextBuffer/Core/TextMetadataScanner.cs), [`src/TextBuffer/Core/PieceTreeModel.Edit.cs`](../../src/TextBuffer/Core/PieceTreeModel.Edit.cs), [`src/TextBuffer/Core/PieceTreeModel.cs`](../../src/TextBuffer/Core/PieceTreeModel.cs), [`tests/TextBuffer.Tests/AA005Tests.cs`](../../tests/TextBuffer.Tests/AA005Tests.cs), [`tests/TextBuffer.Tests/PieceTreeModelTests.cs`](../../tests/TextBuffer.Tests/PieceTreeModelTests.cs), [`tests/TextBuffer.Tests/CRLFFuzzTests.cs`](../../tests/TextBuffer.Tests/CRLFFuzzTests.cs) | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` (105/105) | Y | Porter fixes for CL5/CL6 (AA4-005/AA4-006) integrated; QA verified baseline and re-ran fuzz/targeted CRLF cases (see [`agent-team/handoffs/AA4-009-QA.md`](../../agent-team/handoffs/AA4-009-QA.md)). Delta recorded in `docs/reports/migration-log.md` rows for AA4-005/AA4-006.
- 2025-11-21 | AA4-007.BF1 | Snippet placeholder navigation now references live `ModelDecoration` ranges, eliminating infinite `NextPlaceholder` loops and keeping placeholder offsets consistent after earlier cursor edits. | [`src/TextBuffer/Cursor/SnippetSession.cs`](../../src/TextBuffer/Cursor/SnippetSession.cs), [`tests/TextBuffer.Tests/SnippetMultiCursorFuzzTests.cs`](../../tests/TextBuffer.Tests/SnippetMultiCursorFuzzTests.cs) | `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.SnippetMultiCursorFuzzTests.SnippetAndMultiCursor_Fuzz_NoCrashesAndInvariantsHold" --nologo` (1/1); `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` (115/115) | Y | Refer to migration log row `AA4-007.BF1`; fuzz hangs are now reproducible via seed 12345 and no longer loop indefinitely.

## Delta (2025-11-22)

### Batch #1 – ReplacePattern Implementation (AA4-008)
- **交付文件**:
  - [`src/TextBuffer/Core/ReplacePattern.cs`](../../src/TextBuffer/Core/ReplacePattern.cs) (561 lines)
  - [`src/TextBuffer/Rendering/DocUIReplaceController.cs`](../../src/TextBuffer/Rendering/DocUIReplaceController.cs) (119 lines)
  - [`tests/TextBuffer.Tests/ReplacePatternTests.cs`](../../tests/TextBuffer.Tests/ReplacePatternTests.cs) (356 lines, 23 tests)
- **TS 源文件**:
  - `ts/src/vs/editor/contrib/find/browser/replacePattern.ts`
  - `ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts`
- **测试结果**: 142/142 通过 (基线: 119, 新增: 23)
- **QA 报告**: [`agent-team/handoffs/B1-QA-Result.md`](../../agent-team/handoffs/B1-QA-Result.md)
- **Porter 交付**: [`agent-team/handoffs/B1-PORTER-Result.md`](../../agent-team/handoffs/B1-PORTER-Result.md)
- **迁移日志**: [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md) (新增 Batch #1 条目)
- **TestMatrix**: [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md) (新增 ReplacePattern 行)
- **已知差异**: C#/JavaScript Regex 空捕获组行为（已文档化，非阻塞）
- **TODO 标记**: FindModel 集成、WordSeparator 上下文（Batch #2）

### Batch #1 文档修正 (QA Follow-up)
- **问题级别**: Medium × 2
- **修复内容**:
  1. **TestMatrix.md / ts-test-alignment.md**: 移除不存在的 `DocUIReplacePatternTests` 类名、`resources/docui/replace-pattern/*.json` fixtures 、`__snapshots__/docui/replace-pattern/*.md` 引用；更正为实际的 `ReplacePatternTests.cs`（inline 测试数据）。
  2. **DocUIReplaceController.cs**: `ExecuteReplace` 从静默 no-op 改为 `throw new NotImplementedException(...)`，避免调用者误以为替换已执行。
- **验证**: `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~ReplacePatternTests" --nologo` (23/23 通过)
- **相关文件**:
  - [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md)
  - [`docs/plans/ts-test-alignment.md`](../../docs/plans/ts-test-alignment.md)
  - [`src/TextBuffer/Rendering/DocUIReplaceController.cs`](../../src/TextBuffer/Rendering/DocUIReplaceController.cs)
- **迁移日志**: 已添加 "Batch #1 文档修正" 条目

## Delta (2025-11-22 - OI Backlog)

### OI Backlog 创建 (OI-REFRESH)
- **新增文件**: [`oi-backlog.md`](./oi-backlog.md)
- **登记任务**: OI-012~015（4 项 Active Backlog）
  - **OI-012**: DocUI Widget 测试框架设计（P2, QA-Automation）
  - **OI-013**: Snapshot Tooling - Markdown 快照自动化（P2, QA-Automation）
  - **OI-014**: WordSeparator Parity - 完整对齐（P3, Porter-CS）
  - **OI-015**: DocUI Harness 标准化设计（P2, Planner）
- **技术债来源**: 参考 `B2-INV-Result.md` 调研成果（FindWidget 测试路径、WordSeparator 规格缺口）
- **下一步**: Batch #2 完成后评估 OI-012/OI-015 启动时机；OI-013 可随时启动；OI-014 在 Batch #2 后纳入技术债清理

## Changefeed

### delta-2025-11-23
**Batch #2 – FindModel 完成 (Final QA 187/187)**

交付物：
- 测试文件 (4): [`DocUIFindModelTests.cs`](../../tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs) (39 tests), [`LineCountTest.cs`](../../tests/TextBuffer.Tests/DocUI/LineCountTest.cs), [`RegexTest.cs`](../../tests/TextBuffer.Tests/DocUI/RegexTest.cs), [`EmptyStringRegexTest.cs`](../../tests/TextBuffer.Tests/DocUI/EmptyStringRegexTest.cs)
- 实现文件 (3): [`FindModel.cs`](../../src/TextBuffer/DocUI/FindModel.cs), [`FindDecorations.cs`](../../src/TextBuffer/DocUI/FindDecorations.cs), [`FindReplaceState.cs`](../../src/TextBuffer/DocUI/FindReplaceState.cs)
- Harness: [`TestEditorContext.cs`](../../tests/TextBuffer.Tests/DocUI/TestEditorContext.cs)

CI 修复列表：
- CI-1 / CI-2 / CI-3 零宽 (^ / $ / ^.*$/^$) 末尾空行匹配与装饰残留问题全部修复；范围扩展至 `model length + 1` 捕获末尾零宽装饰，Search 控制流与 TS `do...while` 行为对齐。

测试基线：
- 142 → 187 (+45) 全量测试通过。FindModel 专项 39/39；辅助测试 4/4。

Handoff 文件：
- [`B2-TS-Review.md`](../handoffs/B2-TS-Review.md)
- [`B2-Porter-Fixes.md`](../handoffs/B2-Porter-Fixes.md)
- [`B2-QA-Result.md`](../handoffs/B2-QA-Result.md)
- [`B2-Porter-CI3-Fix.md`](../handoffs/B2-Porter-CI3-Fix.md)
- [`B2-Final-QA.md`](../handoffs/B2-Final-QA.md)

迁移日志条目：
- 见 [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md) 中 2025-11-23 / Batch #2 行（FindModel Tests, 187/187）。

后续计划：
- Batch #3 处理剩余 4 个多光标/选择相关 FindModel parity 测试（select all matches / multi-cursor navigation）。
- 补充 DocUI FindController 层与 selection-derived search 行为测试。 

性能与一致性微优化 (FR-01 / FR-02):
- 修改：[`FindModel.cs`](../../src/TextBuffer/DocUI/FindModel.cs) 增加 `_ignoreModelContentChanged` 标志位，在 Replace / ReplaceAll 内包裹 `PushEditOperations` 期间抑制双重 `Research()`，与 TS 行为对齐（避免重复 recompute）。
- 修改：[`SearchTypes.cs`](../../src/TextBuffer/Core/SearchTypes.cs) 引入 `WordCharacterClassifierCache`（10-entry LRU），`ParseSearchRequest` 复用缓存，移植 TS `getMapForWordSeparators` 语义，降低频繁 whole-word 匹配构造开销。
- 测试：全量基线保持 187/187，通过快速 re-run 验证无回归；FindModel 专项测试未需变更（逻辑透明优化）。
- 迁移日志：新增行见 [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md) 2025-11-23 FR-01 / FR-02。
- 后续：计划在 Batch #3 加入缓存命中率统计测试 & ReplaceAll undo 分组 parity（单步撤销）。

### delta-2025-11-23-b3-fm
**Sprint 03 R12 – B3-FM SelectAllMatches 多光标语义对齐**

交付内容：
- [`src/TextBuffer/DocUI/FindModel.cs`](../../src/TextBuffer/DocUI/FindModel.cs) – `SelectAllMatches()` 现使用 `SelectionInfo` 聚合、范围排序与主光标保持逻辑，完全映射 TS `findModel.test.ts` `selectAllMatches` 行为。
- [`tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs`](../../tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs) – 新增 FM-01 (`Test28_SelectAllMatchesHonorsSearchScopeOrdering`) / FM-02 (`Test29_SelectAllMatchesMaintainsPrimaryCursorInDuplicates`) parity 覆盖。
- [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md) – 更新 DocUI FindModel 行（41/43）并记录 B3-FM 子集、Batch #3 基线 (186/186)。
- [`agent-team/handoffs/B3-FM-Result.md`](../handoffs/B3-FM-Result.md) – Porter handoff，含变更、测试、风险、后续动作。

验证：
- `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – 186/186，全量绿；FM-01/FM-02 断言覆盖搜索范围排序 + 主光标不变。

文档/索引更新：
- `agent-team/members/porter-cs.md#Latest Focus (2025-11-23)`、`docs/sprints/sprint-03.md` (R12)、`docs/reports/migration-log.md`（B3-FM 行）同步引用本 delta。
- Sprint 03 交付矩阵新增 `B3-FM` 行；TestMatrix 基线与 QA 命令链接到此 delta。

后续计划：
- Batch #3 余项（FindController/WordBoundary 多光标场景、DocUI selection heuristics）将沿用本 delta 的 multi-cursor harness；Info-Indexer 继续跟踪 `#delta-2025-11-23-b3-fm` 在 Task Board / AGENTS / Sprint 三处的引用情况。

### delta-2025-11-23-b3-fsel
**Sprint 03 R13 – B3-FSel Selection Search String Parity**

交付内容：
- [`src/TextBuffer/DocUI/FindUtilities.cs`](../../src/TextBuffer/DocUI/FindUtilities.cs) – 新增 `SelectionSeedMode`, `IEditorSelectionContext`, `WordAtPosition`，并实现 `GetSelectionSearchString()` / `GetWordAtPosition()`（524,288 长度上限 + ASCII word separator 行为与 TS 对齐）。
- [`tests/TextBuffer.Tests/DocUI/DocUIFindSelectionTests.cs`](../../tests/TextBuffer.Tests/DocUI/DocUIFindSelectionTests.cs) – 3 个 parity Tests（空选区→word、单行选区→文本、跨行/含换行→null）+ `SelectionTestContext` harness。
- 文档更新：`tests/TextBuffer.Tests/TestMatrix.md`（DocUIFindSelectionTests 行记为 ✅、基线 189/189）、`docs/sprints/sprint-03.md`（R13 记录）、`docs/reports/migration-log.md`（B3-FSel 行）。

验证：
- `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – 189/189 绿色（新增 3 个 DocUI selection 测试，见 TestMatrix `#delta-2025-11-23-b3-fsel` 行）。

Handoff / 参考：
- [`agent-team/handoffs/B3-FSel-Result.md`](../handoffs/B3-FSel-Result.md) – Porter 交付说明、测试命令、后续行动。
- Sprint log / Migration log / TestMatrix 已引用该 delta，DocMaintainer 变更完成。

后续计划：
- R14（B3-FC-Core）将复用 `IEditorSelectionContext` + `FindUtilities` 为 DocUI FindController 注入 selection seed 逻辑、regex 逃逸与导航命令。
- WordSeparator Unicode parity (OI-014) 与多光标/多选区 scope 逻辑继续跟踪 B3-FC-Scope / R15。

### delta-2025-11-23-b3-fc-core
**Sprint 03 R14 – B3-FC Core FindController Parity**

交付内容：
- [`src/TextBuffer/DocUI/DocUIFindController.cs`](../../src/TextBuffer/DocUI/DocUIFindController.cs) – 新建 DocUI controller，端到端移植 TS `CommonFindController` 行为（`StartFindAction`, `StartFindReplaceAction`, `NextMatchFindAction`, `NextSelectionMatchFindAction`, `Start` helper），实现 selection seeding、自动 regex 逃逸、search scope 持久化、focus 代理、存储/剪贴板接口 (`IFindControllerStorage`, `IFindControllerClipboard`).
- [`tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs`](../../tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs) – 10 个 issue regression tests（#1857/#3090/#6149/#41027/#9043/#27083/#58604/#38232 等），含 `TestEditorHost`, `TestFindControllerStorage`, `TestFindControllerClipboard` stubs，用于模拟 VS Code host 行为。
- Harness/Data：`FindControllerHostOptions`, selection auto-escape helpers、`TrySeedSearchString`, `NormalizeSeededSearchString`，并扩展 `FindUtilities` 以服务 controller。
- 文档：`docs/sprints/sprint-03.md`（R14 B3-FC 行）、`tests/TextBuffer.Tests/TestMatrix.md`（DocUIFindControllerTests 行 + rerun 命令）、`docs/reports/migration-log.md`（B3-FC 行）、`agent-team/handoffs/B3-FC-Result.md`（交付 & deferral 记录）、`AGENTS.md` / `agent-team/members/porter-cs.md` 最新 focus。

验证：
- `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindControllerTests --nologo` – 10/10。
- `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – 199/199；TestMatrix 更新 baseline。

已知风险 / 后续：
- 搜索范围生命周期（B3-FC-Scope / R15）与 Mac 系统剪贴板复刻尚未完成；DocUI overlay focus 状态持久化亦推迟到 R15。Deferrals 记录在 `agent-team/handoffs/B3-FC-Result.md`。
- 现有 controller 依赖最简 host options；OI-015（DocUI Harness 标准化）将提炼通用 host。

### delta-2025-11-23-b3-fc-scope
**Sprint 03 R14/R15 – B3-FC Scope & Ctrl/Cmd+F3 Regression Fixes**

交付内容：
- [`DocUIFindController.cs`](../../src/TextBuffer/DocUI/DocUIFindController.cs) – `Start()` 仅在 `BuildSearchScope()` 返回非空时更新 `_state.SearchScope`，避免自动 find-in-selection 在光标折叠时清空范围（W1）。`NextSelectionMatchFindAction()` 在 `MoveToNextMatch()` 失败时必定调用 `Start(...)` 并重试（即使没有 seed），确保 Ctrl/Cmd+F3 在空白上也会打开 widget（W2）。
- [`DocUIFindControllerTests.cs`](../../tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs) – 新增 **SearchScopePersistsWhenSelectionCollapses**（覆盖 W1）与 **NextSelectionMatchOnWhitespaceRevealsWidget**（覆盖 W2）。
- [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md) – DocUIFindController 行与 targeted rerun 命令更新为 15/15 + `#delta-2025-11-23-b3-fc-scope` delta 标签。
- [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md) / [`agent-team/handoffs/B3-FC-Review.md`](../handoffs/B3-FC-Review.md) / [`agent-team/members/porter-cs.md`](../members/porter-cs.md) – 记录 W1/W2 问题关闭、测试命令与 reviewer 状态。

验证：
- `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindControllerTests --nologo` – 15/15 绿色，覆盖新增 W1/W2 tests。

文档/索引更新：
- Sprint 03（R14/R15）与 QA Matrix 均引用 `#delta-2025-11-23-b3-fc-scope`。Migration log 新增行并链接至本 changefeed。B3-FC Review 文件将 W1/W2 标记为 resolved。

后续：
- 继续跟踪 R15 余项（Mac 全局剪贴板富文本、Find widget focus state persistence）与 R16 Decorations stickiness；OI-015 仍计划在下一批 DocUI harness 更新中整合。

### delta-2025-11-23-b3-fc-lifecycle
**Sprint 03 R15 – B3-FC Lifecycle & Reseed Parity**

交付内容：
- [`src/TextBuffer/DocUI/DocUIFindController.cs`](../../src/TextBuffer/DocUI/DocUIFindController.cs) – Ctrl+F 现只要 host 选项≠ `SeedSearchStringMode.Never` 就会重新从当前选区 seed；`StartFindReplaceAction` 遵循 `Never`（禁用选区+剪贴板 seed）；FindModel 改为 lazy create 并在 widget 隐藏时 dispose + 清空 match info；`Start()` 会在重新显示 widget 时折叠 replace 面板，避免 ESC 后 `IsReplaceRevealed` 卡住。
- [`tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs`](../../tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs) – 新增 Ctrl+F reseed、`SeedSearchStringMode.Never` replace、防止隐藏后继续搜索、issue #41027 replace 折叠、Cmd+E multi-line/空光标（TS issues #47400/#109756）等 8 个回归测试。
- [`tests/TextBuffer.Tests/DocUI/DocUIFindSelectionTests.cs`](../../tests/TextBuffer.Tests/DocUI/DocUIFindSelectionTests.cs) – 复用 selection harness 以验证 Cmd+E 行为；[`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md) 更新 DocUI 行、QA 命令与总数 217/217。
- 文档：`docs/plans/ts-test-alignment.md` 新增 Live Checkpoint，`docs/reports/migration-log.md` 记录 B3-FC-Lifecycle 行，Sprint 03 与 AGENTS 更新指向 `#delta-2025-11-23-b3-fc-lifecycle`。

验证：
- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindControllerTests --nologo` (26/26)
- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindSelectionTests --nologo` (4/4)
- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` (217/217 全量)

后续：
- Mac 全局剪贴板 + focus sticky（B3-FC-R16）仍在计划中；Info-Indexer 继续监控 Cmd+E 相关的多选区/clipboard 场景。
- Migration log 行已标记为 Done 并引用本 changefeed；DocMaintainer 将据此更新 Task Board / Sprint / AGENTS。

### delta-2025-11-23-b3-fc-regexseed
**Sprint 03 R15 – B3-FC Regex Seed Fix**

- [`src/TextBuffer/DocUI/DocUIFindController.cs`](../../src/TextBuffer/DocUI/DocUIFindController.cs) 现仅在 `SelectionSeedMode.Single`（TS “single”）且 regex 启用时对 seeded 文本执行 `Regex.Escape`，`StartFindWithSelection`/Cmd+E 在 regex 模式下保持多行/括号字面文本，`Next/PreviousSelectionMatch` 复用该逻辑。
- [`tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs`](../../tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs) 新增 **StartFindWithSelectionDoesNotEscapeRegexCharacters** 回归，并在 `tests/TextBuffer.Tests/TestMatrix.md` 记录 27/27 rerun + `#delta-2025-11-23-b3-fc-regexseed` 标签。
- 验证：`PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindControllerTests --nologo` (27/27)；`PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` (218/218) 作为最新全量基线。
- 文档：`docs/reports/migration-log.md` 新增 B3-FC-RegexSeed 行，AGENTS / Sprint 03 / TestMatrix 指向本 changefeed，更新 Cmd+E regex 多行 seed 修复状态。

