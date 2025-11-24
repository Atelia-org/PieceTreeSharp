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
4. **QA Verification**: 新测试合入后运行 `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` + 目标化 filter，记录命令、通过数、快照路径。
5. **Broadcast**: Info-Indexer 在 changefeed 新增 delta，DocMaintainer 同步 AGENTS/Sprint/Task Board/`TestMatrix`，保证所有文档引用相同的 delta anchor。

## Tracking & Documentation
- **Primary Tracker**: 本文件 `Appendix – TS Test Inventory`，记录 TS 路径、描述、可移植级别、C# 对应文件、状态、阻塞。
- **Supporting Docs**:
  - `tests/TextBuffer.Tests/TestMatrix.md`：新增 “TS Source” 列、映射 ID 与完成度。
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

- **进度（截至 2025-11-24）**：Appendix 表格维持 9 条记录 + 1 条 TODO，Investigator-TS 记忆同步了 CL8 DocUI scope/fuzz 阻塞；`tests/TextBuffer.Tests/TestMatrix.md` 已补齐 “TS Source”/“Portability Tier” 列并在 CL4.F5 段落记录 `#delta-2025-11-23-b3-decor-stickiness` 与 `#delta-2025-11-23-b3-decor-stickiness-review` 的 stickiness 基线；`#delta-2025-11-24-find-scope` 与 `#delta-2025-11-24-find-replace-scope` 的 targeted rerun（44/44→45/45）和 QA 命令已写入 DocUIFindModel 行；`#delta-2025-11-24-b3-docui-staged` 所涉 DocUIFindDecorations/FindModel tests 也在 TestMatrix、Task Board、AGENTS、Migration Log、Plan 内完成串联，确保 changefeed+日志链路最新。
- **2025-11-24（Batch #3 – PieceTree fuzz 计划）**：Planner 已将 Investigator 交接 (`agent-team/handoffs/B3-PieceTree-Fuzz-INV.md`) 转化为 `agent-team/handoffs/B3-PieceTree-Fuzz-PLAN.md`，确认 R25→R29（Harness → Deterministic → CRLF/Search → QA → DocMaintainer/Info-Indexer）可在 Sprint 03 剩余 5 天内完成，并在 Info-Indexer 发布的 [`#delta-2025-11-23-b3-piecetree-fuzz`](../agent-team/indexes/README.md#delta-2025-11-23-b3-piecetree-fuzz) / [`#delta-2025-11-24-b3-piecetree-fuzz`](../agent-team/indexes/README.md#delta-2025-11-24-b3-piecetree-fuzz) 两个 changefeed 下跟踪 Harness 与 deterministic 套件；多 seed fuzz soak 与 perf instrumentation 记录为 Sprint 04 carryover。Task Board/Sprint 已添加对应行，Live Checkpoints/Next Actions 跟踪该计划。
- **2025-11-24（Batch #3 – B3-FM multi-selection 交付）**：Jess 执行 `B3-FM-MultiSelection-Plan`，完成 `TestEditorContext` 多选区 plumbing、`FindModel.SetSelections()` 主光标排序、`DocUIFindModelTests.Test07/08` 回归与 QA rerun（2/2 + `FullyQualifiedName~FindModelTests` 48/48 → 全量 242/242）。`tests/TextBuffer.Tests/TestMatrix.md` 现记录 DocUIFindModel 43/43 覆盖，`docs/reports/migration-log.md` “B3-FM-MultiSel” 行及 [`agent-team/indexes/README.md#delta-2025-11-24-b3-fm-multisel`](../agent-team/indexes/README.md#delta-2025-11-24-b3-fm-multisel) 为唯一事实来源，Live Checkpoints 与 Task Board/AGENTS 均指向同一 changefeed。 
- **后续思路**：Batch #1 ReplacePattern 全链路（Porter→QA→Info-Indexer→DocMaintainer）已关闭；Batch #2 FindModel/Controller 规划（B2-001~005）与 OI-012~OI-015 backlog 均待主循环安排，DocMaintainer 继续追踪 Info-Indexer 在 `agent-team/indexes/oi-backlog.md` 的补录；Batch #3 接下来的优先级为 B3-FM multi-selection（B3-FM-MSel-*）、DocUI widget harness、WordSeparator parity 与残余 DocUI snapshot tooling，必要时再触发 Investigator/QA runSubAgent。
- **2025-11-23 (Batch #3 – Decor Stickiness Investigation)**: Investigator-TS 对 `findDecorations.ts` 与 `modelDecorations.test.ts` 进行了逐项比对，罗列出 range highlight/overview throttling、`TrackedRangeStickiness` 矩阵、`TextModel` 行级查询、DocUI harness 偏差等缺口，并在 `agent-team/handoffs/B3-Decor-INV.md` 形成实现+测试迁移计划。目标 delta：`#delta-2025-11-23-b3-decor-stickiness`，交付项包括 FindDecorations 特性补全、`GetLineDecorations`/`GetAllDecorations` API、新增 TS parity 测试套件以及 DocUI find decoration 检查。
- **2025-11-23 (Batch #3 – Decor Stickiness 交付)**: Porter-CS/QA 完成 INV 列出的全部差异：`FindDecorations` 现包含 range highlight trimming、overview throttling、inline/overview/minimap stickiness，并在 TextModel 中新增 `GetAllDecorations` / `GetLineDecorations` / `GetDecorationIdsByOwner` API；DocUI harness 获得 `DocUIFindDecorationsTests`（范围高亮、wrap-around、overview 取整、scope normalization）与 `DecorationStickinessTests`（四种 `TrackedRangeStickiness` × 边界插入矩阵），同时 `DecorationTests` 扩展 per-line 查询/事件生命周期覆盖。QA 记录：`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`（233/233，3.0s），`--filter DecorationStickinessTests`（4/4，1.8s），`--filter DocUIFindDecorationsTests`（6/6，1.8s）。文档/Task Board/Sprint 均指向 `#delta-2025-11-23-b3-decor-stickiness` 以汇集该批交付。
- **2025-11-23 (Batch #3 – Decor Stickiness Review)**: 根据 Investigator `B3-Decor-Stickiness-Review`（CI-1/CI-2/CI-3 + W-1/W-2）执行追加修复：移除 `_cachedFindScopes`、保留 scope 原始换行、让 `FindDecorations` 通过 host options 注入 viewport height 以恢复 `mergeLinesDelta` 计算，并切换到 `TextModel.AllocateDecorationOwnerId()` 以避免 owner 冲突。`DocUIFindDecorationsTests` 新增 `FindScopesPreserveTrailingNewline` / `FindScopesTrackEdits` / `OverviewThrottlingRespectsViewportHeight`，`TestEditorContext` 允许自定义 viewport，`DocUIFindController` 传递 provider，`TestMatrix` 基线提升至 235/235。QA 记录：`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`（235/235，2.9s），`--filter DecorationStickinessTests`（4/4），`--filter DocUIFindDecorationsTests`（9/9）。所有文档改用 `#delta-2025-11-23-b3-decor-stickiness-review` 引用本次修复。

- **2025-11-22 (Batch #2 规划)**: Planner 根据 Investigator-TS 调研成果（WordSeparator 规格、FindWidget 测试不存在），拆解 Batch #2 为 5 个 runSubAgent 任务（B2-001~005）。核心目标：移植 FindModel 逻辑层（FindReplaceState/FindDecorations/FindModel）+ findModel.test.ts 核心场景（15+ tests）；推迟 FindController 至 Batch #3。详见 Task Board 与 `agent-team/handoffs/B2-PLAN-Result.md`。预计时长 5 个工作日（2025-11-23~11-27）。
- **2025-11-23 (Batch #3 规划初稿)**: 定义 Batch #3 子批次：`B3-FM`(FindModel multi-cursor: `selectAllMatches` & primary cursor 保持)、`B3-FSel`(getSelectionSearchString 三类场景)、`B3-FC-Core`(Controller 核心导航/动作：F3 循环、regex 自动逃逸、选区种子、选项持久化)、`B3-FC-Scope`(searchScope 变更/清理/自动更新多选区)、`B3-Decor-Stickiness`(stickiness 四策略 + per-line 查询)、`B3-PieceTree-Fuzz`(随机编辑 + invariants)、`B3-Diff-Pretty`(char-change/whitespace/move/timeout)。预留 delta tags：`#delta-2025-11-23-b3-fm`, `#delta-2025-11-23-b3-fsel`, `#delta-2025-11-23-b3-fc-core`, `#delta-2025-11-23-b3-fc-scope`, `#delta-2025-11-23-b3-decor-stickiness`, `#delta-2025-11-23-b3-piecetree-fuzz`, `#delta-2025-11-23-b3-diff-pretty`。当前评估：Sprint 剩余 6 日，可完成前 5 个子批次（Fuzz/Diff 若进度不足移至 Sprint 04）。
- **2025-11-23 (Batch #3 – B3-FC lifecycle & seeding)**: 落地 Investigator 提醒的 DocUI FindController 行为修正，新增 `#delta-2025-11-23-b3-fc-lifecycle`：1) `StartFindAction` 现在在 host 选项≠Never 时始终以当前选区 reseed（含 Ctrl+F 场景）；2) `StartFindReplaceAction` 遵循 `SeedSearchStringMode.Never`（禁用选区/全局剪贴板 seed）；3) FindModel widget lifecycle（隐藏即 dispose + match count 清零）与 TS Cmd+E 场景 `issue #47400/#109756`（多行/空光标）已在 `DocUIFindControllerTests` 复刻；4) Replace 面板在重新开启 Ctrl+F 时默认折叠（issue #41027）。命令：`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindControllerTests`、`--filter DocUIFindSelectionTests`、全量 `dotnet test`（结果附 `TestMatrix.md` 更新）。
- **2025-11-24 (Batch #3 – B3-FM multi-selection follow-up)**: Created `agent-team/handoffs/B3-FM-MultiSelection-Plan.md` to coordinate Investigator/Porter work on the remaining TS Test07/08 multi-selection scenarios. Task Board now includes `B3-FM-MSel-INV` and `B3-FM-MSel-PORT` entries; Investigator will capture overlap + wrap semantics and emit `B3-FM-MultiSelection-Audit.md`, while Porter will extend `TestEditorContext`/`FindModel` to support multiple selections before porting the tests and updating `TestMatrix.md` to 43/43 parity.
- **2025-11-24 (Batch #3 – B3-FM multi-selection audit)**: Investigator-TS completed `agent-team/handoffs/B3-FM-MultiSelection-Audit.md`, documenting TS Test07/08 scope tables, harness gaps, and the Porter/QA work needed for parity. This run will anchor the future changefeed `#delta-2025-11-24-b3-fm-multisel`; Task Board + TestMatrix updates should reference the same tag once Porter lands the fixes.
- **2025-11-24 (Batch #3 – B3-FM scope & scoped replace fix)**: Porter-CS/QA landed the pending search-scope override + `_normalizeFindScopes` parity and followed up with scoped regex replace hydration. `DocUIFindModelTests` 新增 **Test45_SearchScopeTracksEditsAfterTyping** / **Test46_MultilineScopeIsNormalizedToFullLines** / **Test47_RegexReplaceWithinScopeUsesLiveRangesAfterEdit**；QA reran `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo`（44/44→45/45）并在 TestMatrix CL4.F5 行标记 `#delta-2025-11-24-find-scope` / `#delta-2025-11-24-find-replace-scope`。`docs/reports/migration-log.md` 与 Sprint 02 / AGENTS / Task Board 均引用相同 changefeed。
- **2025-11-24 (Batch #3 – B3 DocUI staged fixes)**: DocMaintainer/QA recorded the staged FindDecorations reset + caret overlap repairs：`FindDecorations.Reset()` 保留 `_startPosition`、`IntervalTree.CollectOverlaps()` 捕获零宽范围，新增 **DocUIFindModelTests.Test48_FlushEditKeepsFindNextProgress** 与 **DocUIFindDecorationsTests.CollapsedCaretAtMatchStartReturnsIndex**。QA rerun：`export PIECETREE_DEBUG=0 && dotnet test ... --filter FullyQualifiedName~FindModelTests --nologo` 46/46、`--filter FullyQualifiedName~DocUIFindDecorationsTests --nologo` 9/9。`docs/reports/migration-log.md`、`agent-team/handoffs/B3-DocUI-StagedFixes-20251124*.md`、Task Board、AGENTS、TestMatrix 与本计划均指向 `agent-team/indexes/README.md#delta-2025-11-24-b3-docui-staged`。

## Next Actions
1. Investigator-TS 发起 `runSubAgent` 收集 PieceTree/TextModel/DocUI 相关 TS 测试列表，填入本文件附录。
2. QA-Automation 在 `TestMatrix.md` 添加 “TS Source” 列并预留 A/B/C 级别标记。
3. Planner/Task Board：P1 期间创建首批高可移植测试任务（例如 PieceTree builder、TextModel search、Diff/Decorations snapshot），并引用本计划。
4. 每次迁移完成后立即更新迁移日志、changefeed、DocUI snapshot，保持文档联动。
5. Planner：驱动 `B3-PieceTree-Fuzz-PLAN`（R25–R29）执行，所有进度引用 `#delta-2025-11-23-b3-piecetree-fuzz`（Harness）与 `#delta-2025-11-24-b3-piecetree-fuzz`（Deterministic），并在 Sprint 04 backlog 中登记 fuzz soak/perf 项以防 scope 膨胀。

## Appendix – TS Test Inventory (placeholder)
| TS Test File | Module Scope | Notes / Dependencies | Portability Tier (A/B/C) | Target C# Suite | Status |
| --- | --- | --- | --- | --- | --- |
| `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` | PieceTree builder, RB-tree invariants, search + snapshot sanity | Uses `PieceTreeTextBuffer/Base/Builder`, `WordCharacterClassifier`, `SearchData`, `createTextModel`, randomized fuzz helpers; blockers: deterministic RNG + word-separator adapter for .NET search hooks | B | `tests/TextBuffer.Tests/PieceTreeBaseTests.cs`, `PieceTreeBuilderTests.cs`, `PieceTreeSearchTests.cs` | Partial parity (C# lacks fuzz + invariant coverage). Priority #2 (search-offset cache) = ✅ Complete via R31–R34 (`INV/PORT/QA/DOC`) under [`#delta-2025-11-25-b3-search-offset`](../agent-team/indexes/README.md#delta-2025-11-25-b3-search-offset); QA logged `export PIECETREE_DEBUG=0 && dotnet test --filter PieceTreeSearchOffsetCacheTests --nologo` (5/5, 4.3s) + full `--nologo` sweep (324/324, 58.2s) in TestMatrix/migration log。 |
| `ts/src/vs/editor/test/common/model/textModel.test.ts` | TextModel lifecycle, BOM/EOL handling, indentation inference, listener contract | Depends on `TextModel`, `createModelServices`, `IInstantiationService`, `PLAINTEXT_LANGUAGE_ID`, `DisposableStore`; blockers: need lightweight instantiation + option plumbing identical to TS defaults | B | `tests/TextBuffer.Tests/TextModelTests.cs` | Basic cases exist; advanced option/events not ported |
| `ts/src/vs/editor/test/common/model/textModelSearch.test.ts` | TextModel regex/whole-word/multiline parity, CRLF compensation | Exercises `SearchParams.parseSearchRequest`, `SearchData`, and `Searcher` boundary helpers from `core/wordHelper.ts`/`wordCharacterClassifier.ts`; verifies `createFindMatch` capture arrays consumed by `contrib/find/browser/replacePattern.ts`. Porting requires reading `wordHelper.ts`, `wordCharacterClassifier.ts`, `common/model.ts` (SearchData), and `textModelSearch.ts`; blockers: shared WordSeparator cache + Intl.Segmenter parity + `RegexOptions` mismatch vs TS `strings.createRegExp`. | B | `tests/TextBuffer.Tests/TextModelSearchTests.cs` | Core search tests ported; need word boundary matrix + multiline/capture suites once WordSeparator map exists |
| `ts/src/vs/editor/contrib/find/test/browser/findWidget.test.ts` | _Expected but not found_ – FindWidget DOM layout, history, accessibility | TS repo only has `find.test.ts`, `findModel.test.ts`, `findController.test.ts`, `replacePattern.test.ts` under `contrib/find/test/browser`. No dedicated FindWidget DOM harness exists; widget tests are implicitly covered by `findController.test.ts` via `withAsyncTestCodeEditor` stubs. | C | _Deferred – DocUI harness needed_ | **Recommendation**: Skip DOM widget tests; focus on FindModel logic + controller commands (existing TS tests sufficient) |
| `ts/src/vs/editor/test/common/diff/diffComputer.test.ts` | Legacy `DiffComputer` line+char heuristics, trim whitespace toggles, edit replay | Depends on `legacyLinesDiffComputer`, `Range`, `createTextModel`, `Constants`; blockers: char-change pretty diff + whitespace flags flagged in `docs/reports/audit-checklist-aa3.md#cl3` | B | `tests/TextBuffer.Tests/DiffTests.cs` | Missing char-change assertions + pretty diff cases |
| `ts/src/vs/editor/test/common/model/modelDecorations.test.ts` | Decorations creation/removal, stickiness, per-line queries | Uses `TextModel`, `EditOperation`, `TrackedRangeStickiness`, `EndOfLineSequence`; blockers: need stickiness model + `model.changeDecorations` adapters per `AA3-007` audit | B | `tests/TextBuffer.Tests/DecorationTests.cs` | Only smoke tests exist; stickiness + per-line cases absent |
| `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts` | DocUI Find model binding (incremental search, highlight sync, replace state) | Needs `FindModelBoundToEditorModel`, `FindReplaceState`, `withTestCodeEditor`, `PieceTreeTextBufferBuilder`, `CoreNavigationCommands`; blocker: no DocUI editor harness or find decoration plumbing in C# (`docs/reports/audit-checklist-aa4.md#cl8`) | C | _TODO_: `DocUIFindModelTests.cs` | Not started – blocked on DocUI harness |
| `ts/src/vs/editor/contrib/find/test/browser/findController.test.ts` | Command-layer find controller (actions, clipboard, context keys) | Uses `CommonFindController`, `EditorAction`, `ServiceCollection`, `ClipboardService`, `platform`, `withAsyncTestCodeEditor`; blocker: missing command/context-key/clipboard services in DocUI host | C | _TODO_: `DocUIFindControllerTests.cs` | Not started – dependent on editor command surface |
| `ts/src/vs/editor/contrib/find/test/browser/find.test.ts` | Selection-derived search string heuristics (`getSelectionSearchString`) | Relies on `withTestCodeEditor`, `Range`, `Position`, `getSelectionSearchString`; blocker: need lightweight selection model + multi-line guard semantics | B | _TODO_: `DocUIFindSelectionTests.cs` | Not started – waiting for selection helper port |
| `ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts` | ReplacePattern parser + case-preserving builder logic | Pure logic: `parseReplaceString`, `ReplacePattern/Piece`, `buildReplaceStringWithCasePreserved`; blocker: none beyond wiring into DocUI replace state | A | `ReplacePatternTests.cs` (23 tests, 142/142) | ✅ Complete |
| `TODO – DocUI find widget DOM/snapshot suites (expected under ts/src/vs/editor/contrib/find/test/browser)` | Editor overlay widget layout/history/accessibility | Today’s repo only includes `find.test.ts`, `findModel.test.ts`, `findController.test.ts`, `replacePattern.test.ts` (all via `withTestCodeEditor`/`withAsyncTestCodeEditor`, `ServiceCollection`, clipboard/context key stubs); no `findWidget` DOM harness surfaced. Need Info-Indexer/DocMaintainer to locate upstream widget snapshot/browser tests (likely `findWidget.test.ts` or playwright harness) and plan how to stub DOM pieces (`FindWidget`, `Sash`, `ContextViewProvider`, history services). | C | _TBD (DocUI find widget harness + snapshot tests)_ | Research required: confirm source files + decide harness strategy before port |

---

## Appendix B – WordSeparator & SearchContext Specification

### TS Implementation Overview

#### 1. Core Components

**`wordHelper.ts`** (`ts/src/vs/editor/common/core/wordHelper.ts`):
- **`USUAL_WORD_SEPARATORS`**: Default string constant `\`~!@#$%^&*()-=+[{]}\\|;:'",.<>/?` 
- **`createWordRegExp(allowInWords)`**: Generates a RegExp for word detection, excluding chars in `allowInWords` from `USUAL_WORD_SEPARATORS`
- **`DEFAULT_WORD_REGEXP`**: Pre-compiled regex for standard word boundaries (includes numeric literals)
- **`ensureValidWordDefinition(wordDefinition)`**: Ensures RegExp has `g` flag, rebuilds if necessary
- **`getWordAtText(column, wordDefinition, text, textOffset, config)`**: 
  - Window-based search with time budget (default 150ms)
  - Recursively searches backward in sliding windows (default windowSize: 15)
  - Returns `IWordAtPosition { word, startColumn, endColumn }` or null
  - Used for "word-under-cursor" features (hover, selection expansion, etc.)

**`wordCharacterClassifier.ts`** (`ts/src/vs/editor/common/core/wordCharacterClassifier.ts`):
- **`WordCharacterClass` enum**: `Regular = 0`, `Whitespace = 1`, `WordSeparator = 2`
- **`WordCharacterClassifier` class**:
  - Constructor: `(wordSeparators: string, intlSegmenterLocales: string[])`
  - Extends `CharacterClassifier<WordCharacterClass>` (base class provides per-char lookup)
  - Sets `WordSeparator` class for each char in `wordSeparators`
  - Sets `Whitespace` class for space/tab
  - **Intl.Segmenter integration** (optional):
    - `_segmenter: Lazy<Intl.Segmenter>` if `intlSegmenterLocales.length > 0`
    - `_cachedLine` + `_cachedSegments[]`: Single-line cache for segmenter results
    - `findPrevIntlWordBeforeOrAtOffset(line, offset)`, `findNextIntlWordAtOrAfterOffset(line, offset)`
    - `_filterWordSegments()`: Filters to `isWordLike: true` segments only
- **`getMapForWordSeparators(wordSeparators, intlLocales)`**: 
  - LRU cache (size: 10) keyed by `"${wordSeparators}/${locales.join(',')}"`
  - Returns singleton `WordCharacterClassifier` instance for given config

**`textModelSearch.ts`** (`ts/src/vs/editor/common/model/textModelSearch.ts`):
- **`SearchParams` class**:
  - Fields: `searchString`, `isRegex`, `matchCase`, `wordSeparators: string | null`
  - `parseSearchRequest()`: Builds `SearchData` with `getMapForWordSeparators(wordSeparators, [])`
- **`SearchData` class**: 
  - Fields: `regex: RegExp`, `wordSeparators: WordCharacterClassifier | null`, `simpleSearch: string | null`
  - Used by `Searcher` and `TextModelSearch.findMatches()`
- **`Searcher` class**:
  - Constructor: `(wordSeparators: WordCharacterClassifier | null, searchRegex: RegExp)`
  - `reset(lastIndex)`: Sets `regex.lastIndex` and clears prev match tracking
  - `next(text)`: Executes regex, validates word boundaries via `isValidMatch()` if `wordSeparators` present
  - Handles zero-length matches (advances by 1–2 code points)
- **Word boundary helpers**:
  - `leftIsWordBounday()`, `rightIsWordBounday()`: Check adjacent char class
  - `isValidMatch()`: Validates both left/right boundaries for whole-word search

**FindModel integration** (`ts/src/vs/editor/contrib/find/browser/findModel.ts`):
- Line 434: `model.findNextMatch(..., this._state.wholeWord ? this._editor.getOption(EditorOption.wordSeparators) : null, ...)`
- Line 508: `model.findMatches(..., this._state.wholeWord ? this._editor.getOption(EditorOption.wordSeparators) : null, ...)`
- Line 530: `new SearchParams(..., this._state.wholeWord ? this._editor.getOption(EditorOption.wordSeparators) : null)`
- **Key pattern**: `wordSeparators` passed only when `wholeWord = true`, sourced from editor option `EditorOption.wordSeparators`

#### 2. Data Flow

```
EditorOption.wordSeparators (string)
    ↓ (when wholeWord = true)
SearchParams.wordSeparators
    ↓
SearchParams.parseSearchRequest()
    ↓
getMapForWordSeparators(wordSeparators, []) → WordCharacterClassifier (cached)
    ↓
SearchData.wordSeparators
    ↓
Searcher(wordSeparators, regex)
    ↓
Searcher.next(text) → isValidMatch() checks left/right boundaries
```

### C# Porting Status

#### Implemented ✅
- **`SearchParams` class** (`src/TextBuffer/Core/SearchTypes.cs`):
  - Fields match TS: `SearchString`, `IsRegex`, `MatchCase`, `WordSeparators`
  - `ParseSearchRequest()` creates `WordCharacterClassifier` when `!string.IsNullOrEmpty(WordSeparators)`
  - Returns `SearchData(regex, classifier, simpleSearch, isMultiline, isCaseSensitive)`
- **`SearchData` class**:
  - Fields: `Regex`, `WordSeparators: WordCharacterClassifier?`, `SimpleSearch`, `IsMultiline`, `IsCaseSensitive`
- **`WordCharacterClassifier` class**:
  - `Dictionary<int, WordCharacterClass>` for per-codepoint lookups
  - `GetClass(codePoint)` returns `WordCharacterClass` enum (Regular/Whitespace/WordSeparator)
  - `IsValidMatch(text, matchStartIndex, matchLength)` validates left/right boundaries
  - `IsSeparatorOrLineBreak(codePoint)` helper for boundary checks
  - Uses `UnicodeUtility` for surrogate pair handling (`TryGetCodePointAt`, `TryGetPreviousCodePoint`)
- **`PieceTreeSearcher` integration** (`src/TextBuffer/TextModelSearch.cs`):
  - Constructor: `(WordCharacterClassifier? wordSeparators, Regex regex)`
  - `Next(text)`: Calls `IsValidMatch()` when `_wordSeparators != null`
  - Used in `TextModelSearch.FindMatches()`, `FindNextMatch()`, `FindPreviousMatch()`

#### Missing / Gaps ��
1. **LRU cache for `WordCharacterClassifier`** ❌:
   - TS uses `getMapForWordSeparators()` with 10-entry LRU cache
   - C# creates new `WordCharacterClassifier` on every `ParseSearchRequest()` (no caching)
   - **Impact**: Minor perf hit for repeated searches with same `wordSeparators` string
   - **Recommendation**: Add `static ConcurrentDictionary` or `MemoryCache` in `SearchParams` or factory helper

2. **Intl.Segmenter parity** ❌:
   - TS supports `intlSegmenterLocales` for Unicode word segmentation (e.g., CJK, Thai)
   - C# has no equivalent to `Intl.Segmenter`
   - **Options**:
     - Use ICU4N library (NuGet: `ICU4N`) for `BreakIterator` API
     - Document limitation and skip for MVP
   - **Current status**: Not implemented, not blocking core scenarios (Western languages work)

3. **`wordHelper.ts` API** (getWordAtText, createWordRegExp) ❌:
   - TS uses `getWordAtText()` for hover/selection; FindModel uses `SearchParams` path instead
   - C# has no equivalent to `getWordAtText()` or `DEFAULT_WORD_REGEXP`
   - **Impact**: None for find/replace (uses `SearchParams`); would block hover/word-at-cursor features
   - **Recommendation**: Defer until cursor/hover features needed (not in current scope)

4. **EditorOption.wordSeparators** source ⚠️:
   - TS: `this._editor.getOption(EditorOption.wordSeparators)` provides default `USUAL_WORD_SEPARATORS` per language
   - C#: No editor options layer in current codebase
   - **Current workaround**: Tests/controllers pass `wordSeparators` string directly to `SearchParams`
   - **TODO**: Define `TextModelOptions` or `EditorConfig` class to hold `WordSeparators` default (defer to DocUI layer)

### C# Migration Checklist (for Batch #2)

- [x] **`WordCharacterClassifier`** class with `GetClass()` and boundary validation
- [x] **`SearchParams.ParseSearchRequest()`** integration with `WordCharacterClassifier`
- [x] **`PieceTreeSearcher.Next()`** word boundary filtering
- [ ] **LRU cache** for `WordCharacterClassifier` instances (optional perf optimization)
- [ ] **Intl.Segmenter** parity for non-Latin scripts (defer to post-MVP or ICU4N)
- [ ] **`getWordAtText()`** API for hover/word-under-cursor (defer to Cursor features)
- [ ] **Editor options layer** to provide default `wordSeparators` per language (defer to DocUI)

### Test Coverage Recommendations

#### Word Boundary Matrix (Tier A – High Priority)
Create xUnit test suite `WordBoundaryTests.cs` covering:
- Basic ASCII separators: space, tab, punctuation from `USUAL_WORD_SEPARATORS`
- Edge cases: start/end of string, empty match, zero-width matches
- Multi-char operators: `->`, `::`, `==` (should split at operator)
- Unicode: emoji boundaries, surrogate pairs, combining diacritics
- CJK/Thai: Document limitation (no Intl.Segmenter) and skip or use ICU4N

#### Whole-Word Search Integration (Tier A)
Extend `TextModelSearchTests.cs`:
- Regex + wholeWord: `\w+` should match word boundaries, not mid-word
- Simple search + wholeWord: `"foo"` should not match `"foobar"`
- Case-insensitive + wholeWord: `"FOO"` matches `"Foo"` only at boundaries
- Multiline + wholeWord: verify CRLF compensation doesn't break boundaries

#### FindModel Binding (Tier B – depends on DocUI harness)
Port `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts`:
- `_state.wholeWord = true` → passes `wordSeparators` to `SearchParams`
- `_state.wholeWord = false` → passes `null`
- Verify `EditorOption.wordSeparators` default (or C# equivalent config)

### Known Risks

1. **Unicode Word Break Algorithm divergence**:
   - TS `Intl.Segmenter` follows UAX #29 (Unicode Standard Annex #29)
   - C# `WordCharacterClassifier` uses manual char class lookups (no segmenter)
   - **Mitigation**: For Western languages, separator-based approach sufficient; for CJK, add ICU4N or document limitation

2. **Performance: No WordCharacterClassifier cache**:
   - Every search re-creates classifier from `wordSeparators` string
   - **Mitigation**: Add static cache keyed by `wordSeparators` (see "Missing" section above)

3. **Regex engine differences (ECMAScript vs .NET)**:
   - TS uses `strings.createRegExp()` with `unicode: true` (ES2018)
   - C# uses `RegexOptions.ECMAScript | RegexOptions.CultureInvariant`
   - **Mitigation**: `SearchParams.ApplyUnicodeWildcardCompatibility()` expands `.` to handle surrogates; monitor test failures for edge cases

4. **Default `wordSeparators` source**:
   - TS: language-specific via `EditorOption.wordSeparators`
   - C#: No options layer yet; callers must explicitly pass string
   - **Mitigation**: Define const `SearchParams.DefaultWordSeparators = USUAL_WORD_SEPARATORS` and use in tests; defer language-specific defaults to editor config layer

---

## Appendix C – Batch #2 Dependencies & Roadmap

### Blocking Items (必须先实现)
1. **WordCharacterClassifier cache** (optional perf): Add static LRU cache in `SearchParams.ParseSearchRequest()` to avoid re-creating classifiers
2. **DocUI editor harness** (for FindModel/FindController tests): Minimal test harness providing `ITextModel`, `FindReplaceState`, decoration hooks
3. **FindModel/FindDecorations stubs** (Tier C tests): If porting `findModel.test.ts`, need C# equivalents of `FindModelBoundToEditorModel`, `FindDecorations`, `FindReplaceState`

### Optional Items (可后续优化)
1. **Intl.Segmenter parity** (ICU4N): For CJK/Thai word segmentation; not needed for Western languages
2. **FindWidget DOM tests**: TS has no widget-specific tests; C# can skip or add Markdown snapshot tests for controller output
3. **`getWordAtText()` API**: For hover/word-under-cursor; defer to future cursor feature work

### Recommended Order for Batch #2
1. **Phase 1 – WordSeparator infrastructure** (Investigator-TS: DONE ✅):
   - Document TS WordSeparator/SearchContext flow (this appendix)
   - Validate C# parity (already exists in `SearchTypes.cs`)
   - Add LRU cache for `WordCharacterClassifier` (Porter-CS task)
2. **Phase 2 – FindModel logic layer** (Porter-CS + QA):
   - Port `findModel.test.ts` core cases (search, replace, match counting)
   - Stub `FindReplaceState`, `FindDecorations` in C# (minimal API for test harness)
   - Write xUnit tests for `FindModel` binding to `TextModelSearch` with `wholeWord` flag
3. **Phase 3 – FindController command layer** (Porter-CS + QA):
   - Port `findController.test.ts` command/action tests (skip clipboard/context-key if no DocUI harness)
   - Create minimal `DocUIFindController` that wraps `FindModel` + `ReplacePattern`
   - Write integration tests for find/replace/replaceAll commands

### Next Steps for Investigator-TS
- [x] Complete WordSeparator规格文档 (Appendix B above)
- [x] Confirm FindWidget测试路径 (no dedicated widget tests in TS)
- [x] Update记忆文件 `agent-team/members/investigator-ts.md`
- [ ] Prepare汇报文档 `agent-team/handoffs/B2-INV-Result.md`
