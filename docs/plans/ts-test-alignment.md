# TS Test Alignment Plan

## Goal
建立一套“分阶段移植 VS Code TypeScript 单元测试”方法，作为衡量 PieceTree/TextModel/DocUI 质量对齐度的主线路径。通过先列出原版测试清单，再按可移植性分级迁移，使 C# 代码库逐步获得与 TS 同级的回归保障，同时保持 Task Board / Sprint / changefeed 的一致更新。

## Guiding Principles
1. **完整清单先行**：先 inventory TS 端的测试套件（路径、模块、依赖），即使暂时无法移植，也要在文档中记录目标，避免盲区。
2. **可移植性分级**：按可行程度将测试分为 A/B/C 级（高/中/低）。优先迁移 A 级（纯模型逻辑、依赖少），B 级需适配测试桩或最小化替代品，C 级则记录阻塞条件和所需环境。
3. **阶段性推进**：每个 Sprint 选择一批 A/B 级测试，完成后通过 `docs/reports/migration-log.md` + changefeed 报告成果，并在 `TestMatrix.md` 中登记映射关系和覆盖度。
4. **双向验证**：迁移每批测试前后都运行 TS 原版与 C# 版（如可行），收集团队共用的 Fixture/Snapshot，确认为相同行为后再落入主干。
5. **文档驱动**：所有测试映射、分级评估、迁移状态都写入共享文档（本文 + `TestMatrix.md` + Task Board），保持 Info-Indexer 可引用的唯一事实来源。

## Phased Approach
| Phase | Scope | Key Actions | Deliverables |
| --- | --- | --- | --- |
| P0 – Inventory | 列举 TS 源测试（路径/描述/依赖） | Investigator-TS 收集 TS `*.test.ts` 列表；QA 建立 `docs/plans/ts-test-alignment.md#appendix` 表格 | TS 测试清单 + 初步分级建议 |
| P1 – High-portability Batch | 选择 A 级（核心模型、纯函数）测试迁移 | Porter-CS 迁移逻辑，QA 改写/创建 C# xUnit 用例，DocMaintainer 更新 `TestMatrix` 和 changefeed | 迁移后的测试文件、`dotnet test` 记录、迁移日志条目 |
| P2 – Medium-portability Batch | 处理需要轻量适配（依赖 services/stubs）的测试 | 按需创建 C# stub/service；QA 扩展 harness；记录残余阻塞 | 扩展后的测试、stub 文档、阻塞说明 |
| P3 – Complex/Low-portability Batch | 评估高度依赖 VS Code runtime 的测试 | 制定替代策略（DocUI snapshot、integration mock）；记录无法移植的理由与未来计划 | “不可移植”清单 + 替代验证方案 |
| Continuous | 每次合并一批测试即更新文档/索引 | 更新 `docs/reports/migration-log.md`、changefeed、Task Board/Sprint/AGENTS | 最新 delta + QA 基线 |

## Workflow Checkpoints
1. **Inventory Update**: Investigator-TS 运行 `runSubAgent` 提交 TS 测试列表及初步分级，写入本计划附录 + `TestMatrix.md` “TS Source” 列。
2. **Feasibility Review**: QA/Porter 评估每条记录，确认需要的 harness/stub，若需额外调研则再次调用 SubAgent（例如 DocMaintainer 查 VS Code test infra）。
3. **Execution Tickets**: 每批迁移在 Task Board 上建子任务（如 “AA4-010.TS-Tests.Batch1”），Sprint 文档中列出目标，并引用本计划章节。
4. **QA Verification**: 新测试合入后运行 `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` + 目标化 filter，记录命令、通过数、快照路径。
5. **Broadcast**: Info-Indexer 在 changefeed 新增 delta，DocMaintainer 同步 AGENTS/Sprint/Task Board/`TestMatrix`，保证所有文档引用相同的 delta anchor。

## Tracking & Documentation
- **Primary Tracker**: 本文件 `Appendix – TS Test Inventory`，记录 TS 路径、描述、可移植级别、C# 对应文件、状态、阻塞。
- **Supporting Docs**:
  - `src/PieceTree.TextBuffer.Tests/TestMatrix.md`：新增 “TS Source” 列、映射 ID 与完成度。
  - `docs/reports/migration-log.md`：每次迁移一批测试添加行，注明 TS 参考文件与新增 xUnit 文件，记录 `dotnet test` 结果。
  - `agent-team/task-board.md` / `docs/sprints/*.md`：在相关阶段（AA4/AA5…）的任务描述中附 “TS Test Alignment – Batch #” 子弹。
  - `agent-team/indexes/README.md`：Info-Indexer 在 delta 中登记测试迁移状况供 AGENTS 等引用。

## Roles
- **Investigator-TS**：收集 TS 测试清单，分析依赖与行为。
- **Porter-CS**：迁移实现/测试代码，解决 C# harness 需求。
- **QA-Automation**：编写 xUnit、建立 snapshot/fuzz 工具，并维护 `TestMatrix`。
- **DocMaintainer**：保持文档一致性（AGENTS/Sprint/Task Board），审查计划变更。
- **Info-Indexer**：更新 changefeed，确保索引指向最新测试迁移状态。

## Live Checkpoints

- **2025-11-22 (Batch #1 完成)**: Porter-CS 实现 `ReplacePattern.cs` + `DocUIReplaceController` + 23 个 xUnit 测试，移植 TS `replacePattern.test.ts` 的所有核心场景（$n/$&/$$、大小写修饰符、case-preserving 逻辑）。QA-Automation 验证 142/142 通过（新增 23），更新 `TestMatrix.md` 登记 Tier A 完成状态。Info-Indexer 发布 [`#delta-2025-11-22`](../../agent-team/indexes/README.md#delta-2025-11-22)，同步迁移日志。Appendix 表格中 `replacePattern.test.ts` 行状态更新为 ✅ Complete。

- **关键认知 (2025-11-22 规划)**：Investigator-TS 已完成首轮 TS 测试清单，并在 2025-11-22 新增 WordSeparator/SearchContext 细化说明：TS 的 `wordHelper.ts` + `WordCharacterClassifier.ts` + `TextModelSearch.ts` 形成共享缓存，C# 仍缺统一 `WordSeparatorMap`/`SearchContext`；DocUI find widget 测试集中于 `ts/src/vs/editor/contrib/find/test/browser`（`findModel.test.ts`、`findController.test.ts`、`find.test.ts`、`replacePattern.test.ts`），尚无 DOM snapshot 版本，需自建 harness。Porter-CS 已将 Batch #1 ReplacePattern API/控制器/测试清单固化（含 parser/case-preserver、`DocUIReplaceController`、JSON fixtures、Markdown snapshots）；QA 具备相应测试模板；Info-Indexer/DocMaintainer 已筹划 `#delta-2025-11-22` changefeed 与文档同步流程。
- **进度**：Appendix 表格新增 9 条记录 + 1 条 TODO；Investigator 记忆文件更新，锁定 CL8 (DocUI Find) 依赖和 fuzz/WordSeparator 阻塞；`src/PieceTree.TextBuffer.Tests/TestMatrix.md` 现包含 “TS Source” 与 “Portability Tier” 列，已为 PieceTree/TextModel/Diff/Decoration/Markdown 套件以及 DocUI placeholders 建档，并写入 Batch #1 QA 命令（`dotnet test` baseline + `ReplacePatternTests`/`MarkdownRendererDocUI` filters，测试数据当前内联，暂无 DocUI 子目录/fixtures/snapshots）。Porter-CS 规划了 Batch #1 交付（`ReplacePattern.cs`, `DocUIReplaceController`, harness/fixtures、`ReplacePatternTests.cs`，并注明测试数据保持内联），并定义文档/日志落点；Info-Indexer 完成 Batch #1 changefeed 工作流设计并细化从收集 Porter/QA artifact 到发布 `agent-team/indexes/README.md#delta-2025-11-22` 的操作流程；Planner 于 2025-11-22 协调简报中将 deliverables 映射到 Task Board（AA4-008/009/OI-011）并拟定 11/23–11/26 节点；QA-Automation 拆解 TS `replacePattern.test.ts` 并固化命令/snapshot 流程（沿用内联测试数据，无 DocUI 子目录/fixtures/snapshots）；DocMaintainer 列出了 Batch #1 文档接入点（AGENTS/Sprint/Task Board/Plan/迁移日志）与统一措辞，等待 changefeed 发布后执行。
- **进度**：Appendix 表格新增 9 条记录 + 1 条 TODO；Investigator 记忆文件更新，锁定 CL8 (DocUI Find) 依赖和 fuzz/WordSeparator 阻塞；`src/PieceTree.TextBuffer.Tests/TestMatrix.md` 现包含 “TS Source” 与 “Portability Tier” 列，已为 PieceTree/TextModel/Diff/Decoration/Markdown 套件以及 DocUI placeholders 建档，并写入 Batch #1 QA 命令（`dotnet test` baseline + `ReplacePatternTests`/`MarkdownRendererDocUI` filters，测试数据当前内联，暂无 DocUI 子目录/fixtures/snapshots）。Porter-CS 已启动 Batch #1 实作：准备新增 `ReplacePattern.cs`、`DocUIReplaceController.cs` 与 `ReplacePatternTests.cs`（测试数据保持内联，无需 DocUI 子目录/fixtures/snapshots）、在关键位置预置 TODO 标记，并列出后续需要 Investigator 提供 WordSeparator 规格、QA 输出 fixture JSON；Info-Indexer 完成 Batch #1 changefeed 工作流设计并细化从收集 Porter/QA artifact 到发布 `agent-team/indexes/README.md#delta-2025-11-22` 的操作流程；Planner 于 2025-11-22 协调简报中将 deliverables 映射到 Task Board（AA4-008/009/OI-011）并拟定 11/23–11/26 节点；QA-Automation 拆解 TS `replacePattern.test.ts` 并固化命令/snapshot 流程（沿用内联测试数据，无 DocUI 子目录/fixtures/snapshots）；DocMaintainer 列出了 Batch #1 文档接入点（AGENTS/Sprint/Task Board/Plan/迁移日志）与统一措辞，等待 changefeed 发布后执行。
- **后续思路**：Batch #1 已完成全链路交付（Porter→QA→Info-Indexer→DocMaintainer）。接下来启动 Batch #2（FindModel/FindController）规划：Investigator-TS 补齐 WordSeparator/SearchContext 规格，Planner 拆解 runSubAgent 顺序，QA-Automation 草拟测试矩阵。Info-Indexer 需在 `agent-team/indexes/oi-backlog.md` 登记 OI-012~015（DocUI widget 测试路径、Snapshot tooling、WordSeparator parity、DocUI harness 设计）为下一批 TS 测试迁移扫清阻碍。

## Next Actions
1. Investigator-TS 发起 `runSubAgent` 收集 PieceTree/TextModel/DocUI 相关 TS 测试列表，填入本文件附录。
2. QA-Automation 在 `TestMatrix.md` 添加 “TS Source” 列并预留 A/B/C 级别标记。
3. Planner/Task Board：P1 期间创建首批高可移植测试任务（例如 PieceTree builder、TextModel search、Diff/Decorations snapshot），并引用本计划。
4. 每次迁移完成后立即更新迁移日志、changefeed、DocUI snapshot，保持文档联动。

## Appendix – TS Test Inventory (placeholder)
| TS Test File | Module Scope | Notes / Dependencies | Portability Tier (A/B/C) | Target C# Suite | Status |
| --- | --- | --- | --- | --- | --- |
| `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` | PieceTree builder, RB-tree invariants, search + snapshot sanity | Uses `PieceTreeTextBuffer/Base/Builder`, `WordCharacterClassifier`, `SearchData`, `createTextModel`, randomized fuzz helpers; blockers: deterministic RNG + word-separator adapter for .NET search hooks | B | `src/PieceTree.TextBuffer.Tests/PieceTreeBaseTests.cs`, `PieceTreeBuilderTests.cs`, `PieceTreeSearchTests.cs` | Partial parity (C# lacks fuzz + invariant coverage) |
| `ts/src/vs/editor/test/common/model/textModel.test.ts` | TextModel lifecycle, BOM/EOL handling, indentation inference, listener contract | Depends on `TextModel`, `createModelServices`, `IInstantiationService`, `PLAINTEXT_LANGUAGE_ID`, `DisposableStore`; blockers: need lightweight instantiation + option plumbing identical to TS defaults | B | `src/PieceTree.TextBuffer.Tests/TextModelTests.cs` | Basic cases exist; advanced option/events not ported |
| `ts/src/vs/editor/test/common/model/textModelSearch.test.ts` | TextModel regex/whole-word/multiline parity, CRLF compensation | Exercises `SearchParams.parseSearchRequest`, `SearchData`, and `Searcher` boundary helpers from `core/wordHelper.ts`/`wordCharacterClassifier.ts`; verifies `createFindMatch` capture arrays consumed by `contrib/find/browser/replacePattern.ts`. Porting requires reading `wordHelper.ts`, `wordCharacterClassifier.ts`, `common/model.ts` (SearchData), and `textModelSearch.ts`; blockers: shared WordSeparator cache + Intl.Segmenter parity + `RegexOptions` mismatch vs TS `strings.createRegExp`. | B | `src/PieceTree.TextBuffer.Tests/TextModelSearchTests.cs` | Core search tests ported; need word boundary matrix + multiline/capture suites once WordSeparator map exists |
| `ts/src/vs/editor/test/common/diff/diffComputer.test.ts` | Legacy `DiffComputer` line+char heuristics, trim whitespace toggles, edit replay | Depends on `legacyLinesDiffComputer`, `Range`, `createTextModel`, `Constants`; blockers: char-change pretty diff + whitespace flags flagged in `docs/reports/audit-checklist-aa3.md#cl3` | B | `src/PieceTree.TextBuffer.Tests/DiffTests.cs` | Missing char-change assertions + pretty diff cases |
| `ts/src/vs/editor/test/common/model/modelDecorations.test.ts` | Decorations creation/removal, stickiness, per-line queries | Uses `TextModel`, `EditOperation`, `TrackedRangeStickiness`, `EndOfLineSequence`; blockers: need stickiness model + `model.changeDecorations` adapters per `AA3-007` audit | B | `src/PieceTree.TextBuffer.Tests/DecorationTests.cs` | Only smoke tests exist; stickiness + per-line cases absent |
| `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts` | DocUI Find model binding (incremental search, highlight sync, replace state) | Needs `FindModelBoundToEditorModel`, `FindReplaceState`, `withTestCodeEditor`, `PieceTreeTextBufferBuilder`, `CoreNavigationCommands`; blocker: no DocUI editor harness or find decoration plumbing in C# (`docs/reports/audit-checklist-aa4.md#cl8`) | C | _TODO_: `DocUIFindModelTests.cs` | Not started – blocked on DocUI harness |
| `ts/src/vs/editor/contrib/find/test/browser/findController.test.ts` | Command-layer find controller (actions, clipboard, context keys) | Uses `CommonFindController`, `EditorAction`, `ServiceCollection`, `ClipboardService`, `platform`, `withAsyncTestCodeEditor`; blocker: missing command/context-key/clipboard services in DocUI host | C | _TODO_: `DocUIFindControllerTests.cs` | Not started – dependent on editor command surface |
| `ts/src/vs/editor/contrib/find/test/browser/find.test.ts` | Selection-derived search string heuristics (`getSelectionSearchString`) | Relies on `withTestCodeEditor`, `Range`, `Position`, `getSelectionSearchString`; blocker: need lightweight selection model + multi-line guard semantics | B | _TODO_: `DocUIFindSelectionTests.cs` | Not started – waiting for selection helper port |
| `ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts` | ReplacePattern parser + case-preserving builder logic | Pure logic: `parseReplaceString`, `ReplacePattern/Piece`, `buildReplaceStringWithCasePreserved`; blocker: none beyond wiring into DocUI replace state | A | `ReplacePatternTests.cs` (23 tests, 142/142) | ✅ Complete |
| `TODO – DocUI find widget DOM/snapshot suites (expected under ts/src/vs/editor/contrib/find/test/browser)` | Editor overlay widget layout/history/accessibility | Today’s repo only includes `find.test.ts`, `findModel.test.ts`, `findController.test.ts`, `replacePattern.test.ts` (all via `withTestCodeEditor`/`withAsyncTestCodeEditor`, `ServiceCollection`, clipboard/context key stubs); no `findWidget` DOM harness surfaced. Need Info-Indexer/DocMaintainer to locate upstream widget snapshot/browser tests (likely `findWidget.test.ts` or playwright harness) and plan how to stub DOM pieces (`FindWidget`, `Sash`, `ContextViewProvider`, history services). | C | _TBD (DocUI find widget harness + snapshot tests)_ | Research required: confirm source files + decide harness strategy before port |
