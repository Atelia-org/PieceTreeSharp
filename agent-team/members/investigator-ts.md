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
- **2025-11-26 (R40):** Reopened AA4-003 CL7 cursor audit (Sprint Run R40). Compared TS `cursor*.ts`/`snippetController2.ts`/`wordOperations*.test.ts` against the current C# stack (`src/TextBuffer/Cursor/*`, `Snippet*`, Markdown renderer/tests) and recorded six blocking gaps (multi-cursor controller, word navigation & Intl segmentation, column-selection helpers, snippet session/controller + stickiness, command/test coverage/TestMatrix accuracy, Intl/fuzz coverage). Updated `agent-team/handoffs/AA4-003-Audit.md`, `docs/reports/audit-checklist-aa4.md#cl7`, and `docs/sprints/sprint-03.md` with the new findings plus recommended changefeed plan (`#delta-2025-11-26-aa4-cl7-*`). Outstanding: Porter-CS to sequence the five deltas, QA to downgrade CL7 rows inside `tests/TextBuffer.Tests/TestMatrix.md` and stage TS test ports, Info-Indexer/DocMaintainer to broadcast each changefeed once code + tests land.
- **2025-11-26 (R41):** Refreshed AA4-004 CL8 DocUI find/replace audit after Batch #3 landed. Re-compared TS `findController.ts`/`findModel.ts`/`findDecorations.ts`/`wordCharacterClassifier.ts` with `src/TextBuffer/DocUI/*`, `SearchTypes.cs`, and `MarkdownRenderer.cs`, confirming scope + scoped replace fixes (`#delta-2025-11-24-find-scope`, `#delta-2025-11-24-find-replace-scope`) hold while four blockers remain: (F1) Markdown renderer still recomputes search and ignores owner filters, (F2) capture metadata & replace previews never leave `FindModel`, (F3) Intl.Segmenter + locale-aware whole-word rules are missing, (F4) WordSeparator configuration/perf doesn’t react to option changes. Updated `agent-team/handoffs/AA4-004-Audit.md`, `docs/reports/audit-checklist-aa4.md#cl8`, and `docs/sprints/sprint-03.md` (Run R41) with new changefeed placeholders `#delta-2025-11-26-aa4-cl8-{markdown,capture,intl,wordcache}`. Next: Porter-CS to stage those deltas, QA to add MarkdownRenderer/Intl tests, DocMaintainer to broadcast once each fix ships.
- **2025-11-26 (R42):** Audited the staged doc set (`AGENTS.md`, `agent-team/indexes/README.md`, role memories, Task Board, TS plan, AA4 checklist, migration log, Sprint 03 log, `tests/TextBuffer.Tests/TestMatrix.md`) to ensure the B3-TextModelSearch QA broadcast (45/45 @ 2.5s targeted + 365/365 @ 61.6s full, `#delta-2025-11-25-b3-textmodelsearch`) is cited consistently. Logged confirmations in `agent-team/handoffs/DocReview-20251126-R42-INV.md`; no corrections required beyond this record.
- **2025-11-26:** Completed staged doc audit (`agent-team/handoffs/DocReview-20251126-INV.md`) covering `agent-team/indexes/README.md`, member memories, and the Sprint/plan/migration docs; verified the new TextModelSearch changefeed references against `SearchTypes.cs` + the 45-test suite, and reran `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~TextModelSearchTests --nologo` (45/45) plus the full `--nologo` sweep (365/365). Flagged `agent-team/members/porter-cs.md` and `tests/TextBuffer.Tests/TestMatrix.md` for still citing 35/35 + 355/355, requested DocMaint/Porter refresh both with the new totals/durations, and noted no other discrepancies.
- **2025-11-25:** Authored `agent-team/handoffs/Review-20251125-Investigator.md` after diffing `SearchTypes.cs`, `TextModelSearchTests.cs`, `PieceTreeSearchTests.cs`, and the associated docs (`docs/plans/ts-test-alignment.md`, `docs/reports/migration-log.md`, `docs/sprints/sprint-03.md`, `tests/TextBuffer.Tests/TestMatrix.md`, `agent-team/indexes/README.md`) against TS `textModelSearch.ts` / `textModelSearch.test.ts`. Flagged the stale plan row (still marked “gap”), the inaccurate changefeed + migration log entries (fixture-only scope, 35/35 counts), the sprint log summary that omits the 45-test suite + helper, and the outdated `B3-TextModelSearch-PORT.md` memo that still claims “fixture only” + 35 tests. Logged follow-ups for DocMaintainer/Info-Indexer/Porter to realign documentation and refresh the PORT memo.
- **2025-11-25:** Completed AA4 Search R36 staged review (`SearchTypes.cs`, `TextModelSearch.cs`, `TextModelSearchTests.cs`, `PieceTreeSearchTests.cs`, `tests/TextBuffer.Tests/TestMatrix.md`). Logged findings in `agent-team/handoffs/AA4-SearchReview-20251125.md`: (1) layering regression where `SearchParams` now calls `TextModelSearch.IsMultilineRegexSource`; (2) missing TS suites for `parseSearchRequest` + `isMultilineRegexSource`; (3) remaining navigation tests (`findNextMatch` literal/boundary cases) still absent despite TestMatrix marking the suite “Verified”. Requested DocMaintainer to keep the TextModelSearch row as “Partial” until these land and pointed Info-Indexer at the TS anchors (`textModelSearch.ts`, `textModelSearch.test.ts`).
- **Last Update:** 2025-11-25
- **2025-11-25:** Completed B3 TextModelSearch coverage audit (Run R35) – compared `ts/src/vs/editor/test/common/model/textModelSearch.test.ts` against `tests/TextBuffer.Tests/TextModelSearchTests.cs` + `PieceTreeSearchTests.cs`, catalogued the five remaining scenario buckets (word-boundary matrix, multiline/CRLF regexes, capture arrays & navigation, `SearchParams` parsing/isMultiline, zero-width + Unicode anchors), filed `agent-team/handoffs/B3-TextModelSearch-INV.md`, updated `docs/plans/ts-test-alignment.md` Appendix row, and logged Sprint entry R35 to unblock Porter/QA/Info-Indexer under the upcoming `#delta-2025-11-25-b3-textmodelsearch` changefeed.
- **2025-11-25:** Reviewed staged PieceTree deterministic helpers + TestMatrix updates; confirmed MIT headers + TS attribution comments now match `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`, and verified TestMatrix plus migration log reference `#delta-2025-11-24-b3-piecetree-deterministic` changefeed.
- **2025-11-25:** Investigator audit for B3 PieceTree deterministic CRLF drop – verified `PieceTreeDeterministicScripts` & `PieceTreeDeterministicTests` cover TS CRLF + centralized line-start suites, confirmed TestMatrix/migration-log counts (50 deterministic facts, 308 total) and QA rerun evidence, and flagged `agent-team/indexes/README.md` + `agent-team/members/porter-cs.md` still describing the drop as “pending QA/306 tests”.
- **2025-11-25:** Captured remaining CRLF + centralized line-start deterministic gaps from `pieceTreeTextBuffer.test.ts` and published Porter/QA plan in `agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-INV.md` (tests to port, helper hints, rerun/doc checklist).
- **2025-11-25:** Reassessed the remaining deterministic backlog (snapshots, search-offset cache, chunk search, unsupervised random scripts, buffer API) and published the prioritised plan in `agent-team/handoffs/B3-PieceTree-Deterministic-Backlog.md` (TS line refs, proposed C# tests, helper deps, QA/doc hooks).
- **2025-11-25:** Completed Run R31 for Priority #2 (search-offset cache): extracted the `render white space exception` log plus the three remaining `Line breaks replacement is not necessary when EOL is normalized` cases from TS (`ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` 1810–1884), documented deterministic scripts + helper mapping in `agent-team/handoffs/B3-PieceTree-SearchOffset-INV.md`, and tagged the work to `#delta-2025-11-25-b3-search-offset` ahead of Porter’s `PieceTreeSearchOffsetCacheTests` implementation.
- **2025-11-25:** Delivered the `B3-Snapshot-INV-20251125` memo (`agent-team/handoffs/B3-Snapshot-INV-20251125.md`) after auditing `PieceTreeSnapshot`/`TextModel.CreateSnapshot` vs TS sources; confirmed chunk slicing & BOM handling parity, but flagged the missing `TextModelSnapshot` wrapper (TS `textModel.ts#L72-L115`) as the remaining gap and outlined Porter/QA follow-ups under `#delta-2025-11-25-b3-piecetree-snapshot`.
- **2025-11-25:** Followed up on the staged snapshot/search-offset diff (post-Porter): verified `PieceTreeSnapshot`, `TextModelSnapshot`, `SnapshotReader`, and the deterministic search-offset suites now mirror their TS counterparts, documented the lingering `_searchCache` coverage limitation plus the need for a `preserveBom` regression in `TextModelSnapshotTests`, and recorded Porter/QA guidance under `agent-team/handoffs/B3-Snapshot-INV-20251125.md#investigator-follow-up-2025-11-25` (`#delta-2025-11-25-b3-piecetree-snapshot`, `#delta-2025-11-25-b3-search-offset`).
- **2025-11-25:** Investigator-TS doc audit (AGENTS/task-board/indexes/migration-log/plan/sprint/members) – verified new BOM/deterministic/snapshot notes, ran `export PIECETREE_DEBUG=0 && dotnet test --nologo` (324/324) plus targeted suites, and logged the mismatches (missing `#delta-2025-11-25-b3-search-offset` changefeed, absent SearchOffset PORT/QA handoffs, stale TestMatrix totals) inside `agent-team/handoffs/doc-review-20251125.md` for Porter/DocMaintainer follow-up.
- **Recent Actions:**
  - 2025-11-24: Completed B3 PieceTree Fuzz Harness staged diff review (`agent-team/handoffs/B3-PieceTree-Fuzz-Review-INV.md`), flagging missing TS random suites, absent line-start/line-content invariants inside the new harness, sentinel metadata gaps in `AssertPieceIntegrity`, RNG distribution drift vs. TS `randomStr()`, and missing changefeed/TestMatrix/doc updates for the B3-Fuzz delta.
  - 2025-11-24: 完成 AA4 FindModel staged review（`AA4-FindModel-Review-INV.md`），核对 `FindModel.SetSelections`/`TestEditorContext.SetSelections` 与 TS `findModel.test.ts` / vscode.d.ts 合约，记录主光标索引与 write-only selection cache 两项差异，并给出 rerun 建议 (`dotnet test --filter FindModelTests`).
  - 2025-11-24: 关闭 AA4 Primary Selection review（`#delta-2025-11-24-b3-fm-multisel`, `#delta-2025-11-24-find-primary`）；复核 `src/TextBuffer/DocUI/FindModel.cs`、`tests/TextBuffer.Tests/DocUI/TestEditorContext.cs`、`DocUIFindModelTests`（Test07/08/49）与 TS `findModel.ts` / `findModel.test.ts`，确认主光标排序与多选 scope 行为对齐，并在 `AA4-FindModel-Review-INV.md` 记录结论。
  - 2025-11-24: 完成 `B3-FM-MultiSelection-Audit`（Test07/08 多选区范围表 + F1/F2/F3 缺口），指导 Porter 扩展 TestEditorContext/FindModel 并预留 changefeed `#delta-2025-11-24-b3-fm-multisel`，同步更新 TS 计划与任务板 TODO。
  - 2025-11-19: 在 `docs/meetings/meeting-20251119-org-self-improvement.md` 提交 Investigator-TS 陈述，记录 PieceTree 覆盖现状、blind spots、协作需求与文档治理建议。
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
    - 对比 `ts/src/vs/editor/common/diff/defaultLinesDiffComputer/*.ts` / `rangeMapping.ts` 与 C# `src/TextBuffer/Diff/*`，梳理缺失的 `LinesDiff`/`DetailedLineRangeMapping` 数据、move detection 与 heuristics/timeout 选项差异。
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
  - 2025-11-21: 以 Investigator-TS 身份完成 AA4 Phase 7 Sprint 02 TS 测试 inventory（PieceTree/TextModel/Diff/DocUI Find），将清单写入 `docs/plans/ts-test-alignment.md#appendix`，标注可移植等级 A/B/C、DocUI harness 阻塞，并引用 `docs/reports/audit-checklist-aa4.md` / `docs/reports/audit-checklist-aa3.md` 记录依赖。
  - 2025-11-22 (B2-INV): 完成 Batch #2 调研任务（WordSeparator规格 + FindWidget测试定位）：
    - **WordSeparator规格**：深入分析 `wordHelper.ts`、`wordCharacterClassifier.ts`、`textModelSearch.ts`，完整梳理 TS 侧的 WordCharacterClassifier 架构（LRU cache、Intl.Segmenter集成、word boundary helpers）与 FindModel 集成点（wholeWord → EditorOption.wordSeparators）。
    - **C# 对齐验证**：确认 `SearchTypes.cs` 已实现核心 API（WordCharacterClassifier、SearchParams、SearchData、PieceTreeSearcher），标注缺口（LRU cache、Intl.Segmenter、getWordAtText API、EditorOption层）。
    - **FindWidget测试路径**：确认 TS 端 `contrib/find/test/browser/` 仅包含 `find.test.ts`、`findModel.test.ts`、`findController.test.ts`、`replacePattern.test.ts`，无专用 FindWidget DOM harness；建议 C# 端跳过 widget DOM 测试，聚焦 FindModel 逻辑层。
    - **文档输出**：在 `docs/plans/ts-test-alignment.md` 新增 Appendix B（WordSeparator规格）和 Appendix C（Batch #2 依赖路线图），更新 Appendix A 表格中 findWidget.test.ts 行（标注为"Expected but not found"）。
    - **记忆更新**：将本次调研成果写入 Investigator-TS 记忆文件，准备向 Planner 汇报。
  - **2025-11-23 (B2-TS-Review): 完成 Batch #2 git 暂存区 TS 对齐性审查**：
    - **审查范围**：`FindReplaceState.cs` (416L), `FindDecorations.cs` (482L), `FindModel.cs` (870L), `TestEditorContext.cs` (244L), `DocUIFindModelTests.cs` (2070L, 39 tests), `EmptyStringRegexTest.cs`, `LineCountTest.cs`, `RegexTest.cs`，以及基础设施修复（`IntervalTree.cs`, `PieceTreeSearcher.cs`, `TextModel.cs`）
    - **Critical Issues 发现** (3 个必须修复):
      - **CI-1**: `IntervalTree.cs:150` 空范围边界检查逻辑错误（`<=` 应为 `<`），影响零宽匹配（如 `^` regex）的 decoration 查询
      - **CI-2**: `FindModel.SetCurrentFindMatch()` 未同步 `MatchesPosition` 到 `FindReplaceState`，导致 "3/5" 计数不准确
      - **CI-3**: `PieceTreeSearcher.Next()` 文本末尾零宽匹配边界检查顺序可优化（非阻塞，但建议对齐 TS 提前退出逻辑）
    - **Warnings 识别** (5 个建议改进):
      - **W-1**: `TestEditorContext` 未实现 TS `withTestCodeEditor` 的工厂模式（PieceTreeBuilder 分块测试）
      - **W-2**: `FindModel.Replace()` 缺少 TS 的 `buildReplaceString` 空字符串边界检查
      - **W-3**: `FindReplaceState.CreateSearchParams()` 使用硬编码 `DefaultWordSeparators`，缺少 TS 的 `EditorOption` 配置层
      - **W-4**: `FindModel._largeReplaceAll()` 未实现 TS 的 `PushStackElement()` 撤销栈边界
      - **W-5**: 缺少 TS 的 `_ignoreModelContentChanged` 标志，replace 操作可能触发不必要的 `Research()`
    - **TS Parity 确认** (15+ 项核心对齐):
      - ✅ FindReplaceState 状态机完整移植（所有字段、Change/ChangeMatchInfo 逻辑、CanNavigate 检查）
      - ✅ FindDecorations 装饰管理（currentFindMatch/findMatch/findScope 类型、批量替换、wrap-around 导航）
      - ✅ FindModel 搜索/替换核心逻辑（MATCHES_LIMIT、零宽匹配调整、Replace/ReplaceAll 分支、selection offset 跟踪）
      - ✅ TestEditorContext 准确模拟 `withTestCodeEditor`（RunTest callback、AssertFindState 三项检查）
      - ✅ 正则表达式边界情况（`^`/`$`/`^$`/`.*`/`^.*$` 零宽匹配、position 调整）
      - ✅ 替换字符串捕获组和大小写修饰符（`$n`/`$&`/`\u/\l/\U/\L`、PreserveCase）
      - ✅ SearchScope 多范围支持（Range[] 归一化、findInSelection 参数）
      - ✅ Decoration stickiness 行为（NeverGrowsWhenTypingAtEdges、Normalize）
      - ✅ 文本模型事件处理（OnDidChangeContent、IsFlush、Research）
      - ✅ 文件头部注释标注 TS 来源（所有新文件包含 TypeScript source reference）
      - ✅ 测试用例命名与 TS 一致（Test01~Test43，39/43 已移植）
      - ✅ IntervalTree decoration 查询（Search/ownerFilter/overlap 逻辑，CI-1 需修复边界）
      - ✅ PieceTreeSearcher 零宽匹配处理（_prevMatch 跟踪、AdvanceForZeroLength、CI-3 建议优化）
      - ✅ TextModel decoration 事件集成（GetDecorationById、DeltaDecorations owner、IsFlush 字段）
      - ✅ LineCount 测试的 trailing empty line 行为（`"a\nb\n"` → 3 行）
    - **输出文档**：`agent-team/handoffs/B2-TS-Review.md` (详细审查报告，包含 TS 参考代码、修复建议、测试验证矩阵)
    - 2025-11-23: second-pass DocUI Find 暂存区 parity sweep（`src/TextBuffer/DocUI/DocUIFindController.cs`, `src/TextBuffer/DocUI/FindModel.cs`, `src/TextBuffer/DocUI/FindUtilities.cs`, `tests/TextBuffer.Tests/DocUI/*`）。记录缺口：① Controller 未实现 `TogglePreserveCase` 与持久化（TS 参考 `ts/src/vs/editor/contrib/find/browser/findController.ts`）；② `FindModel`/`FindReplaceState.CreateSearchParams` 仍以常量分隔符驱动 whole-word 模式，未读取 `IEditorHost` 的 `WordSeparators`（TS `findModel.ts` 依赖 `EditorOption.wordSeparators`）；③ `FindUtilities.GetWordAtPosition` 仅基于 ASCII 字符分类，缺少 TS `getConfiguredWordAtPosition`/`WordCharacterClassifier` 的语言感知边界；④ `DocUIFindController.Start` 在 `UseGlobalFindClipboard` 场景会把空白剪贴板写回 search string，而 TS 对空串直接忽略。待 Porter Batch #3 跟进修复，并在 QA 清单中追踪。
    - 2025-11-23 (B3-FC Review): 完成 Batch #3 DocUI Find Controller staging TS 对齐复审（锚定 docs/sprints/sprint-03.md R14 / AA4-004-Audit.md）。确认 word separator & clipboard plumbing 已修复，但新增 2 个 Warning：W1 `Start()` 在 `UpdateSearchScope=true` 且选区为空时会清空既有 scope（TS `_start` 仅在 `selection.some(!isEmpty)` 时写入）；W2 `NextSelectionMatchFindAction()` 在 caret 位于空白无法 seed 时直接返回 `false`，而 TS `SelectionMatchFindAction.run` 仍会 `start()` 并显示 Find widget。已在 `agent-team/handoffs/B3-FC-Review.md` 记录 Porter 修复建议及需补的 DocUIFindControllerTests（scope persistence + whitespace Ctrl/Cmd+F3）。
    - 2025-11-23 (B3-FC Investigator pass #2): 审看最新 DocUI Find Controller/Model 暂存差异时，发现 3 个 parity 缺口需在 Porter 合入前解决：① `_start` 未像 TS 那样在所有入口（F3/Ctrl+F3 等）强制 `autoFindInSelection` → fallback 流程不会保留多行 scope；② 缺少 `PreviousMatchFindAction`/`PreviousSelectionMatchFindAction` 命令封装，`MoveToPrevMatch()` 无法由 DocUI host 使用；③ 未实现 `SelectAllMatches` 控制器动作，`FindModel.SelectAllMatches()` 结果无法写回 `IEditorHost.SetSelections`，`Alt+Enter`/`editor.action.selectAllMatches` 仍不可用。待 Porter 在 B3-FC 修复并补对照测试。
    - 2025-11-23 (B3-FC Investigator pass #3): DocUI Find Controller re-audit against TS `findController.ts` surfaced new gaps: (1) `StartFindAction` in C# only seeds from selection when `searchString` is empty, diverging from VS Code which always replaces the term unless `find.seedSearchStringFromSelection === 'never'`; (2) `StartFindReplaceAction` ignores the same option, so `Ctrl+H` overwrites the find term even when seeding is disabled; (3) closing the widget never disposes `FindModel` or clears decorations, so highlights linger unlike TS `_onStateChanged` which calls `disposeModel()`; (4) reopening Find after a Replace session keeps the replace UI visible because `_start` never forces `isReplaceRevealed = false` when the widget was hidden; (5) `DocUIFindControllerTests.cs` still lacks the TS `StartFindWithSelection` regressions (issues #47400/ #109756) even though `tests/TextBuffer.Tests/TestMatrix.md` marks the suite “✅ Complete”. Need Porter-CS fixes & QA tests mirroring the TS cases.
    - 2025-11-23 (B3-FC Investigator pass #4): Audited staged DocUI diff (FindController/FindModel/FindUtilities/tests) vs TS references and noted fresh gaps: (a) `DocUIFindController.Start` always escapes seeded text when regex is enabled (even for `SelectionSeedMode.Multiple`), diverging from TS `_start` where multi-line `Cmd+E` seeds are left raw; (b) `FindUtilities.GetWordAtPosition` only honors ASCII separator tables and ignores `EditorOption.wordSegmenterLocales`, whereas TS `getConfiguredWordAtPosition` delegates to `WordOperations` with Intl.Segmenter fallbacks; (c) `DocUIFindControllerTests` still miss the TS persistence tests for default `matchCase`/`wholeWord` plus the `issue #18111`/`#24714` regex replace regressions even though `TestMatrix.md` now flags the suite as “✅ Complete”. Documented fixes + QA asks for Batch #3 follow-up.
- **Upcoming Goals (1 runSubAgent per item):**
  1. ~~PT-003.C~~：已完成 WordSeparator 规格对齐（见 B2-INV）
  2. ~~OI-002.A~~：索引任务已移交 Info-Indexer（见 OI-011/OI-012）
  3. **B2-TS-Review 后续**：等待 Porter-CS 修复 CI-1/CI-2/CI-3，QA-Automation 验证后再次审查（若需要）
  4. **Batch #3 准备**：规划 FindController/multi-cursor 测试迁移策略（依赖 EditorAction/ContextKey/Clipboard services）
  5. **B3-INV 初稿 (2025-11-23)**：收敛 Batch #3 范围（FindModel multi-cursor selectAllMatches & primary cursor 保持、getSelectionSearchString、FindController 核心/搜索域/持久化/自动逃逸、Decorations stickiness、PieceTree fuzz+invariants、Diff pretty/char-change、高级 word boundary matrix），形成迁移分批与 harness 规格。

## Blocking Issues
- 需要 Planner 明确 PT-003 与 OI-002 工时的优先顺序，避免同一 runSubAgent 同时覆盖两条任务。
- 等待 Info-Indexer 提供索引文件命名/结构约束，以确保 Investigator 输出与目录约定一致。
- 需要 Porter-CS 确认 C# RBTree 公共 API（插入/删除、snapshot rebuild、search hook），好在类型映射文件中提前标注依赖。
- Searcher/WordSeparators 对应的 .NET 实现尚未定案；PT-004 skeleton 与 PT-005 QA plan 都需要可调用的 stub 行为（已在 type map Notes 标注 TBD，需 Planner/Porter 在 11-20 前给出指示，否则 PT-004 只能以 no-op search 落地）。
- 缺少公开的 `PieceTreeSearchCache` 检查钩子。Porter/QA 需要一个 friend accessor 或 `PieceTreeBufferAssertions` 扩展来验证 search-offset 缓存是否在脚本后保持一致，否则 `PieceTreeSearchOffsetCacheTests` 只能间接通过 `LineStarts`/content parity来推断缓存健康度。

## Hand-off Checklist
1. 研究笔记写入 `agent-team/members/investigator-ts.md`。
2. Tests or validations performed? N/A（分析任务）
3. 下一位执行者请基于“Upcoming Goals”继续推进或更新类型映射表。

## Latest Focus
### 2025-11-24
- **B3 changefeed gap audit:** 重新跑 `#delta-2025-11-23-b3-piecetree-fuzz`、`#delta-2025-11-24-b3-piecetree-fuzz`、`#delta-2025-11-24-b3-sentinel`、`#delta-2025-11-24-b3-getlinecontent` 的 Info-Indexer 审计，确认 `agent-team/indexes/README.md` 尚未发布对应 changefeed（需列出 `PieceTreeFuzzHarness.cs`/`FuzzLogCollector.cs`/`PieceTreeModel*.cs`/`PieceTreeBuffer.cs`/`PieceTreeBaseTests.cs`/`PieceTreeNormalizationTests.cs` 等文件及 `dotnet test --filter PieceTreeFuzzHarnessTests`, `--filter FullyQualifiedName~PieceTreeModelTests`, `--filter FullyQualifiedName~PieceTreeBaseTests.GetLineContent_Cache_Invalidation`, `--filter FullyQualifiedName~PieceTreeNormalizationTests`, `--filter PieceTreeFuzzHarnessTests.RandomDeleteThreeMatchesTsScript`, `dotnet test -v m` 的 rerun 命令），并要求 `docs/reports/migration-log.md`：① 将 B3-Fuzz-Harness (R25) 行变更为 Changefeed=Y 且指向 `#delta-2025-11-23-b3-piecetree-fuzz`；② 新增 B3-PieceTree-Fuzz-Review 行（记录 multi-chunk/TS random suites +同上测试）；③ 新增 B3-TestFailures 行（记录 per-model sentinel + trimmed `GetLineContent` 测试）。
- **B3 Test Failures triage (this task)**: Reproduced the latest `dotnet test -v m` run, confirmed 5 line-content regressions plus 3 sentinel invariant trips. Logged root causes + TS references in `agent-team/handoffs/B3-TestFailures-INV.md` (tests must expect trimmed `GetLineContent`; sentinel assertions are racing because `PieceTreeNode.Sentinel` is static). Proposed Porter actions: align tests with TS, then either give each `PieceTreeModel` its own sentinel or serialize PieceTree suites until that refactor lands.
- 2025-11-24: Authored `agent-team/handoffs/B3-TestFailures-Review-INV.md` (`#delta-2025-11-24-b3-sentinel`, `#delta-2025-11-24-b3-getlinecontent`) confirming the staged per-model sentinel refactor touches insert/delete/rotation/validation paths and that trimmed `GetLineContent` now matches TS expectations across `TextModel`, `Cursor`, and DocUI consumers; documented QA reruns plus residual monitoring notes.
- **B3 PieceTree Fuzz review**: Audited staged `PieceTreeFuzzHarness` / `FuzzLogCollector` / `PieceTreeModel.AssertPieceIntegrity` updates against TS `pieceTreeTextBuffer.test.ts` / `pieceTreeTextBuffer.ts`, summarized CI-1/CI-2 (missing random suites + dropped `testLinesContent`/`testLineStarts` invariants) and warnings (sentinel metadata + RNG parity + doc chain) in `agent-team/handoffs/B3-PieceTree-Fuzz-Review-INV.md`, and outlined Porter/QA actions (port suites, extend harness assertions, update changefeed/TestMatrix).
- **B3 PieceTree deterministic suite review**: Validated new `PieceTreeDeterministicTests` + CRLF bug scripts in `PieceTreeFuzzHarnessTests` against TS `pieceTreeTextBuffer.test.ts` prefix-sum/offset/range blocks; flagged helper attribution gaps (`tests/TextBuffer.Tests/Helpers/PieceTreeBufferAssertions.cs`, `PieceTreeScript.cs`), TODOs for the remaining TS suites (CRLF + centralized lineStarts sections, ts lines ~940-1400), and reminded Porter to publish a dedicated changefeed instead of the placeholder `#delta-2025-11-24-b3-piecetree-fuzz` note in TestMatrix.
- **B3-PieceTree-Fuzz inventory**: Reviewed TS `pieceTreeTextBuffer.test.ts` fuzz/invariant suites and compared against current C# coverage (`PieceTreeBaseTests`, `PieceTreeModelTests`, `CRLFFuzzTests`, `UnitTest1`, etc.). Captured remaining TS buckets (deterministic insert/delete sequences, prefix-sum/offset invariants, range diffing, CRLF suites, unsupervised fuzz loops, buffer API/search-offset cache, snapshot matrix) plus required harness work (deterministic RNG env hook, enhanced `AssertPieceIntegrity`, range diff helpers, search adapter) in `agent-team/handoffs/B3-PieceTree-Fuzz-INV.md`, anchoring future work under `#delta-2025-11-23-b3-piecetree-fuzz`. Await Planner/Porter scheduling for harness work (R24) before updating Task Board/TestMatrix.
- **2025-11-24 (B3-PieceTree-Fuzz review pass #2)**: Re-ran staged diff review for `PieceTreeModel.Search.cs`, `PieceTreeModel.cs`, `PieceTreeBuffer.cs`, harness helpers/tests, and noted two open gaps vs TS `pieceTreeTextBuffer.test.ts`: (1) only the harness smoke tests (`PieceTreeFuzzHarnessTests`, 2 cases) landed, so none of the TS `random test` / `random chunks` suites (lines 271–1727) are exercising the C# tree; (2) the harness currently seeds from a single `initialText` string (lines 25-33) so there is no way to mirror TS multi-chunk builders used by `random chunks`/`random chunks 2`. Logged both findings plus recommended fixes/tests into `agent-team/handoffs/B3-PieceTree-Fuzz-Review.md` and flagged CI coverage gap for Planner/Porter follow-up.
- **DocUI Find scope复查**：核对 `FindModel.ResolveFindScopes`+`NormalizeScopes`（src/TextBuffer/DocUI/FindModel.cs 194-258）以及 `FindDecorations.GetFindScopes`（src/TextBuffer/DocUI/FindDecorations.cs 126-149），配合 `DocUIFindModelTests.Test45_SearchScopeTracksEditsAfterTyping`、`DocUIFindModelTests.Test46_MultilineScopeIsNormalizedToFullLines` 与 `DocUIFindDecorationsTests.FindScopesTrackEdits`，确认 F2（scope tracking）与 F3（scope normalization）已按 TS `_normalizeFindScopes` 行为恢复；TestMatrix CL4.F5 行附 `#delta-2025-11-24-find-scope`，`export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo` 44/44 绿。
- **仍存缺口**：`FindModel.GetMatchesForReplace` (src/TextBuffer/DocUI/FindModel.cs 915-931) 仍直接把 `_state.SearchScope` 传给 `TextModel.FindNextMatch`；scope 在用户编辑后该数组不会更新，regex replace in selection 场景会重新以旧坐标求捕获组，可能拿到 `null`。
- **AA4 primary-selection close-out**：复核 `FindModel.SetSelections` / `TestEditorContext.SetSelections` 新实现 + DocUIFindModelTests.Test07/08/49，逐项对照 TS `findModel.ts` / `findModel.test.ts`（selectAllMatches、multi-selection scopes），确认主光标排序、选区克隆与 scope hydration 均与 VS Code 一致；`AA4-FindModel-Review-INV.md` 现标注两项差异已解决，暂无新增 TODO。
- **TODO**：Porter 调整 `GetMatchesForReplace` 以复用 `ResolveFindScopes()` 的归一化范围，并补一个“scope 内编辑后依旧能 regex replace” DocUIFindModel 回归；QA 在 `tests/TextBuffer.Tests/TestMatrix.md` CL4.F5 行挂上新测试 ID，并重新执行 `dotnet test --filter FullyQualifiedName~FindModelTests` 保持 rerun 记录。
- **B3 DocUI Staged Review**：完成 `DocUIFindController/FindModel/FindDecorations`、`TextModel` 装饰 API 暂存对比与 TS 源 (`findDecorations.ts`, `findModel.ts`, `textModel.ts`) 差异梳理，归档于 `agent-team/handoffs/B3-DocUI-StagedReview-20251124.md`，落地 CI×2（Reset 错误地重置 `_startPosition`、零宽 selection 无法识别当前 match）与 W×2（>1000 match 仍绘制 inline spans、`GetLineDecorations` 缺少 filter 标志），并提出对应修复/测试建议。

### 2025-11-23
- **日期**: 2025-11-23
- **Batch #3 范围确认**: 覆盖剩余 DocUI Find 相关测试（`findModel.test.ts` 中 `selectAllMatches` + primary cursor (`#14143`)；`find.test.ts` 中 `getSelectionSearchString` 三类场景；`findController.test.ts` 中动作/域/持久化/自动逃逸/多行选区作用域/选区种子/选项持久化 等逻辑分组）以及尚未迁移的底层测试集（PieceTree fuzz & invariants、Diff char-change / pretty heuristics、modelDecorations stickiness/per-line、textModelSearch word boundary matrix）。
- **DocUI FC delta (2025-11-23)**: 复审确认 word separator / clipboard plumbing 已覆盖 Batch #3，但需 Porter 修复 (1) `Start()` 在 `UpdateSearchScope=true` 且当前选区为空时误清空 scope；(2) `NextSelectionMatchFindAction()` 无法 seed 时未调用 `Start()`，导致 Ctrl/Cmd+F3 在空白处无法唤起 Find widget。Fix 后需补 scope persistence 与 whitespace Ctrl/Cmd+F3 两个 DocUIFindControllerTests。
- **Controller Harness 规划**: 定义最小 `IEditorHost`（提供：`Selections[]`、`Options`（含 `WordSeparators`）、`ApplyEdits(edits)`、`SetValue(text)`、`ClipboardStub`、`StorageStub`、`ContextKeyFacade(Enum)`、同步版 `Delayer` 替代）。
- **Clipboard/Storage 策略**: Mac 专用 `IClipboardService` 行为通过条件编译或运行时平台判断；非 Mac 测试标记 `SkipOnNonMac`；Storage 使用内存字典模拟 `IStorageService`（键：`editor.isRegex` / `editor.matchCase` / `editor.wholeWord`）。
- **Selection Heuristics**: `getSelectionSearchString` 在 C# 新文件 `FindUtilities.cs` 中实现：单光标 → word-under-cursor（不跨行）；单行选区 → 直接返回文本；若选区跨行或包含换行 → 返回 `null`；多选暂不取并集（保持与 TS 单选行为一致）。
- **Multi-cursor Primary Preservation**: `selectAllMatches` 保持原主光标：若原主光标所在匹配仍存在，将其置于 `Selections[0]`；新匹配按出现顺序追加；实现时先收集所有匹配，然后 `OrderBy(match == primary ? 0 : 1, startPosition)`。
- **PieceTree Fuzz 计划**: 建立 deterministic RNG（种子注入），操作集：插入/删除/替换/行尾追加；不变量：行数一致、`GetLineContent` 重构一致、搜索结果稳定（对随机文本固定 pattern 集）、装饰区间不重叠（stickiness 扩展前）。
- **Decoration Stickiness**: 拟测试矩阵：`NeverGrowsWhenTypingAtEdges` / `AlwaysGrowsWhenTypingAtEdges` / `GrowsOnlyWhenTypingBefore` / `GrowsOnlyWhenTypingAfter` 四类，在左/右/中间插入、跨行删除、拼接场景；验证范围更新与行号稳定。
- **Diff Pretty / Char-change**: 标记缺口：字符级差异、移动检测、空白折叠、超时策略；C# 需添加 `PrettyDiffTests.cs` 验证 whitespace trim、字符替换聚合、长行超时 fallback、move heuristic（若暂缺则记录 Tier C）。
- **Word Boundary Matrix**: 增补 ASCII/标点/下划线/数字/emoji/CJK；暂未实现 Intl.Segmenter → CJK 行为标记 `ExpectedDifferentBoundary`。
- **Delta Tags 预留**: `#delta-2025-11-23-b3-fm`, `#delta-2025-11-23-b3-fsel`, `#delta-2025-11-23-b3-fc-core`, `#delta-2025-11-23-b3-fc-scope`, `#delta-2025-11-23-b3-decor-stickiness`, `#delta-2025-11-23-b3-piecetree-fuzz`, `#delta-2025-11-23-b3-diff-pretty`。
- **下一步**: 进入 R12（B3-FM）：实现 `selectAllMatches` & primary cursor 保持测试移植与 C# API 对齐；准备 R13 getSelectionSearchString；为 Controller 核心/Scope 子批次预置 harness 桩。
  - 2025-11-23 (B3-Decor-INV): 专注 `findDecorations.ts` / `modelDecorations.test.ts` / `findController.ts` 与 C# `FindDecorations.cs` / `DocUIFindController.cs` / `TextModel.cs` 的 stickiness + decoration parity；梳理出 6 大缺口（range highlight、overview ruler throttling、TrackedRangeStickiness 矩阵、`GetLineDecorations`/`GetAllDecorations` API、DocUI harness 未验证 overlay/owner、QA Matrix 标记错误）并在 `agent-team/handoffs/B3-Decor-INV.md` 写出 Porter/QA/DocMaintainer plan，future delta `#delta-2025-11-23-b3-decor-stickiness`。
    - 2025-11-23 (B3-Decor-Stickiness-Review): 复审 Porter 暂存补丁后确认 scope caching/trim 及 overview throttling 仍与 TS 不符，写入 `agent-team/handoffs/B3-Decor-Stickiness-Review.md`，列出 3×CI（移除 `_cachedFindScopes`、保留原始 scope range、恢复 `mergeLinesDelta` 计算）与 2×W（`FindDecorationsOwnerId` 需动态分配、DocUIFindDecorationsTests/TestMatrix 误报覆盖）。
  - 2025-11-24 (AA4-Review-INV): 巡检 Batch #3 staged patches（FindDecorations/FindModel/TextModel/TestMatrix），在 `agent-team/handoffs/AA4-Review-INV.md` 记录 Scope Tracking + Normalization 两项阻断及 CL4.F5 覆盖偏差，回传 Porter/QA TODO。
  - 2025-11-24 (B3-DocUI-StagedReview): 审查 `DocUIFindController/FindDecorations/FindModel` 最新暂存差异与 `TextModel` 装饰 API 变更，对照 VS Code `findDecorations.ts` / `findModel.ts` / `textModel.ts`，在 `agent-team/handoffs/B3-DocUI-StagedReview-20251124.md` 登记 2×CI（Reset 起点失效、零宽 selection 无法映射 match position）+ 2×W（大结果 inline throttling 未生效、TextModel 缺少 filter 标志）及修复/测试建议。
