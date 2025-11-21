# Porter-CS Memory

## Role & Mission
- **Focus Area:** 将 TypeScript PieceTree 逻辑逐步移植到 `PieceTree.TextBuffer`
- **Primary Deliverables:** C# 源码、xUnit 覆盖、性能基准脚手架
- **Key Stakeholders:** Investigator-TS、QA-Automation、DocMaintainer

## Onboarding Summary (2025-11-19)
- 阅读/速览：`AGENTS.md` 时间线、`agent-team/ai-team-playbook.md`、`agent-team/main-loop-methodology.md`、两份 2025-11-19 会议纪要、`docs/sprints/sprint-00.md`、`docs/sprints/sprint-org-self-improvement.md`、`agent-team/task-board.md`（PT-004 聚焦）。
- 立即 C# 目标：根据 PT-004 在 `PieceTree.TextBuffer/Core` 完成 PieceTreeNode + 红黑树骨架，并按 Investigator-TS 的类型映射预留接口。
- 代码与测试记录：所有实现/测试日志将写入 `src/PieceTree.TextBuffer/README.md` 的“Porting Log”子节，并在本文件 Worklog 中附指针。

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| Core Library Skeleton | src/PieceTree.TextBuffer/Core | 主要的 PieceTree 结构放置点 |
| Buffer Entry Point | src/PieceTree.TextBuffer/PieceTreeBuffer.cs | 提供公共 API，需逐步替换占位实现 |
| Tests | src/PieceTree.TextBuffer.Tests/UnitTest1.cs | 先期可扩展基础 xUnit 框架 |
| Type Mapping | agent-team/type-mapping.md | TS↔C# 结构别名及字段含义 |
| TS Source | ts/src/vs/editor/common/model/pieceTreeTextBuffer | 迁移源码与参考行为 |

## Worklog
- **2025-11-19**
  - 完成首轮 Onboarding，熟悉 AI Team 运作方式、Sprint 目标与 PT-004 期待成果。
  - 审核当前 C# 骨架，确认 `PieceTreeBuffer` 仍为占位，需从 Core 目录启动红黑树实现。
  - 记录代码/测试日志归档位置（`src/PieceTree.TextBuffer/README.md`）。
- **2025-11-19 – Org Self-Improvement Mtg**
  - 评估 C# 端缺口（仅余 `ChunkBuffer`/`PieceSegment` + `StringBuilder` 缓冲），确认 PT-004 首阶段需先落 `PieceTreeNode`/sentinel/Tree 容器。
  - 与 Planner/Investigator/QA/DocMaintainer 对齐依赖：获取 Builder/Search/PrefixSum 类型映射、runSubAgent 模板拆分、QA 属性测试入口及 Porting Log 写入约定。
  - 承诺交付 Core README + TreeDebug 钩子帮助 QA 复核不变量，并把结构性变更写入 Porting Log。
- **2025-11-19 – PT-004.M2 drop**
  - 将 `PieceTreeBuffer` 接上 `ChunkBuffer` → `PieceTreeBuilder` → `PieceTreeModel` 流水线，`FromChunks`/`Length`/`GetText`/`ApplyEdit` 均以 PieceTree 数据驱动。
  - `ChunkBuffer` 新增 line-start/CRLF 计算与 `Slice` helper，`PieceSegment.Empty`、builder result 等保证 sentinel 元数据，`ApplyEdit` 暂以“重建整棵树”作为 TODO 记录的降级方案。
  - Tests: `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（pass，4 tests：multi-chunk builder + CRLF edit 覆盖）。
  - Risks: 每次编辑仍需重建树（性能/暂时性），Search stub 依旧待 Investigator-TS 完善类型映射后再规划 PT-007。
- **2025-11-19 – PT-004 literal translation spike**
  - 在 `src/PieceTree.TextBuffer/PortingDrafts/PieceTreeBase.literal.cs.txt` 新建 Literal C# 版本，完成 TypeScript `pieceTreeBase.ts` 开头到搜索逻辑的 1:1 结构移植并标注剩余 TODO，供后续增量补全与 Info-Indexer 建立 PortingDrafts 钩子。

- **2025-11-19 – PT-004 line infra/cache drop**
  - 按类型映射要求实现 `LineStartTable`/`LineStartBuilder`（`src/PieceTree.TextBuffer/Core/LineStarts.cs`）并让 `ChunkBuffer` 保存 CR/LF/CRLF 计数与 `IsBasicAscii` 标志，PieceTreeBuilder 重用该元数据。
  - 新增 `PieceTreeSearchCache`（`src/PieceTree.TextBuffer/Core/PieceTreeSearchCache.cs`）及 `PieceTreeModel` 缓存钩子，后续 `nodeAt`/`getLineContent` 可复用缓存且在插入时自动失效。
  - Tests: `dotnet test PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（pass，7 tests）。
- **2025-11-19 – PT-004 positions/API drop**
  - 增加 `TextPosition` 结构与 `PieceTreeBuffer` 的 `GetPositionAt` / `GetOffsetAt` / `GetLineLength` / `GetLineCharCode` / `GetCharCode` API，暂以全文快照+`LineStartBuilder` 计算坐标，后续将替换为 tree-aware 实现。
  - 在 `PieceTree.TextBuffer.Tests/UnitTest1.cs` 移植 TS `prefix sum` 风格断言，覆盖 offset→position round trip、CRLF 行长与行内字符编码，测试总数扩展至 10。
  - Tests: `dotnet test PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（pass，10 tests）。

- **2025-11-19 – PT-004 insert/delete drop**
  - 实现 `PieceTreeModel.Edit.cs`，包含 `Insert`、`Delete`、`RbDelete`、`DeleteFixup` 等核心红黑树编辑逻辑，替换了之前的重建树方案。
  - `PieceTreeNode` 增加 `Next()`、`Detach()` 及属性 setter 以支持树操作。
  - `PieceTreeBuffer.ApplyEdit` 更新为调用 `_model.Delete` 和 `_model.Insert`。
  - 移植 TS 基础编辑测试至 `PieceTreeBaseTests.cs`，覆盖 `BasicInsertDelete`、`MoreInserts`、`MoreDeletes`。
  - Tests: `dotnet test PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（pass，13 tests）。

- **2025-11-19 – PT-005 Search**
  - 实现 `PieceTreeSearcher` (C# Regex wrapper) 与 `SearchTypes` (SearchData, FindMatch, Range)。
  - 实现 `PieceTreeModel.Search.cs`，包含 `FindMatchesLineByLine`、`FindMatchesInNode`、`FindMatchesInLine` 等核心搜索逻辑。
  - 移植 TS 搜索逻辑，包括多行搜索、简单字符串搜索优化、Regex 搜索。
  - 新增 `PieceTreeSearchTests.cs`，覆盖基本字符串搜索、Regex 搜索、多行搜索。
  - Tests: `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` (pass, 16 tests)。

- **2025-11-19 – PT-008 Snapshot**
  - 创建 `ITextSnapshot` 接口与 `PieceTreeSnapshot` 实现，支持基于 `PieceTreeModel` 的不可变快照读取。
  - 更新 `PieceTreeModel` 以暴露 `Buffers` 并提供 `CreateSnapshot` 方法。
  - 新增 `PieceTreeSnapshotTests.cs`，覆盖快照读取与不可变性验证（即使 Model 变更，Snapshot 内容保持不变）。
  - Tests: `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` (pass, 18 tests)。

- **2025-11-19 – PT-009 Line Optimization**
  - 在 `PieceTreeModel.cs` 中引入 `LastVisitedLine` 结构与 `_lastVisitedLine` 字段，实现单行缓存。
  - 更新 `PieceTreeModel.Search.cs` 中的 `GetLineContent` 以利用缓存，并在 `PieceTreeModel.Edit.cs` 的 `Insert`/`Delete` 中失效缓存。
  - 在 `PieceTreeBuffer` 中暴露 `GetLineContent` 以供测试。
  - 新增 `PieceTreeBaseTests.cs` 测试用例 `GetLineContent_Cache_Invalidation_Insert` 和 `GetLineContent_Cache_Invalidation_Delete`，验证缓存失效逻辑。
  - Tests: `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` (pass, 20 tests)。
- **2025-11-20 – AA3-004 CL2 Search Fixes**
  - 将 `SearchTypes.ParseSearchRequest` 切换为 `RegexOptions.ECMAScript` 并添加 Unicode wildcard 改写辅助，`PieceTreeSearcher` 也确保 Regex 处于 ECMAScript 模式。
  - 收紧 `WordCharacterClassifier`（仅接受配置的符号 + SPACE/TAB/CR/LF），恢复 TS word-boundary 行为并避免 NBSP/EN SPACE 误判。
  - 新增 AA3 审计覆盖：`\bcaf\b` 边界、ASCII-only digits、Unicode 分隔符、emoji 量词、多选区 regex；记录于 `PieceTreeSearchTests.cs` 与 `TextModelSearchTests.cs`。
  - 文档：创建 `agent-team/handoffs/AA3-004-Result.md`，更新 `docs/reports/migration-log.md` 与 `agent-team/indexes/README.md#delta-2025-11-20`。
  - Tests: `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（84/84）。

- **2025-11-20 – AA3-008 Decorations/DocUI**
  - 复刻 TS decoration 存储：引入 `DecorationsTrees`（regular/overview/injected）与共享 `DecorationRangeUpdater` stickiness 逻辑，`TextModel` 现可查询字体/注入文本/边距装饰并在 `OnDidChangeDecorations` 事件中输出 minimap/overview/glyph/line号/行高/字体元数据。
  - 升级 `MarkdownRenderer` 与选项结构，支持多 owner filter、z-index 排序、注入文本 markers、glyph/margin/overview/minimap 注记，DocUI 行尾附带注解标签。
  - Tests：在 `DecorationTests` 添加 metadata round-trip & 事件断言，在 `MarkdownRendererTests` 覆盖 owner filter 列表、注入文本、glyph/minimap 注解；`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（85/85）。
  - 文档：创建 `agent-team/handoffs/AA3-008-Result.md`，更新 Task Board / Sprint / AGENTS / Migration Log / Changefeed。

- **Upcoming Goals (runSubAgent 粒度):**
  1. **PT-005.Search**：实现 `PieceTreeSearch` 逻辑，支持 Find/Match 等操作。
  2. **PT-004.G3**：实现长度/位置互转与 chunk-based slicing 的额外断言，扩充 xUnit 覆盖（CR-only、BOM、跨 chunk ranges）。
  3. **OI-SUPPORT.G1**：保持 Porting Log & Core README 更新，并将 search stub 依赖、doc 钩子同步给 DocMaintainer/Planner 以支撑 PT-007 规划。

## Blocking Issues
- 仍需 Investigator-TS 在 `agent-team/type-mapping.md` 中补充 `pieceTreeTextBufferBuilder.ts` / `textModelSearch.ts` / `prefixSumComputer.ts` 字段与缓存语义，避免盲目迁移。
- QA-Automation 尚未锁定属性测试/基准入口，需其在 PT-005 定稿后提供最小断言集合以验证我们暴露的 API。
- DocMaintainer 的迁移日志模板（PT-006）与 Main Agent 的“是否 1:1 复刻 TS 红黑树” 决策待定，此前实现需保持开关便于回滚配置。

## Active AA4-006 Worklog
- **2025-11-21 09:00 UTC**: Start work on AA4-006 (CL6) addressing change buffer append heuristics, AverageBufferSize chunk creation, CRLF repair logic across chunks, and SearchCache invalidation precision. Implemented candidate heuristics and tests; next step refine CRLF handling across chunk boundaries.
- **2025-11-21 16:30 UTC**: Begin AA4-006 Fix1 Plan (Porter-CS): reproducing failing tests `TestSplitCRLF` and `CRLF_RepairAcrossChunks`; will add debug helpers and iterate on fixes (Update: added debug printing and first reproduction run).
- **2025-11-21 23:55 UTC**: AA4-006 wrap-up – finished chunk-append fix + change-buffer reuse tracking (`_lastChangeBufferPos/_lastChangeBufferOffset`), tightened search cache range invalidations, exposed `PieceTreeModel.AssertPieceIntegrity`, and ported deterministic CRLF fuzz logging via `FuzzLogCollector`. Updated `PieceTreeModelTests`, `CRLFFuzzTests`, `TestMatrix`, `migration-log`, `task-board`, and this memory doc with the final CL6 handoff.
## End of Worklog (2025-11-21)
- **2025-11-21 15:40 UTC**: Completed porting `ChangeBuffer` append optimization (`_lastChangeBufferPos` tracking + append to `_buffers[0]`), AverageBufferSize splitting using `ChunkUtilities.SplitText`, and targeted SearchCache invalidation updating. Added unit tests for Append optimization, chunk splitting and SearchCache validation. Ran `dotnet test` and recorded results.
 - **2025-11-21 18:00 UTC**: Started AA4-007 (CL7) – cursor word/snippet/multi-select parity. Plan: implement `CursorCollection`/`CursorState`/`CursorContext`, `WordCharacterClassifier` + `WordOperations`, `CursorColumns`, `SnippetSession`/`SnippetController`, update `MarkdownRenderer` doc output; add tests and remediations.
 - **2025-11-21 22:30 UTC**: Completed AA4-007 implementation prototype: added `CursorCollection`, `CursorState`, `CursorContext`, `WordCharacterClassifier`, `WordOperations`, `CursorColumns`, `SnippetSession`, and `SnippetController`. Implemented `Cursor` word methods, integrated `CursorCollection` into the model via `CreateCursorCollection()`, and added unit tests: `CursorMultiSelectionTests`, `CursorWordOperationsTests`, `ColumnSelectionTests`, `SnippetControllerTests`, and updated `MarkdownRendererTests` with `TestRender_MultiCursorAndSnippet`. Ran `dotnet test` and all `PieceTree.TextBuffer` tests passed (113/113). See `agent-team/handoffs/AA4-007-Result.md` for details.
- **2025-11-21 23:20 UTC**: Reviewed Investigator AA4-008 (CL8 DocUI overlays) addendum; cataloged F1–F4 remediation surfaces, align degrade heuristics (>1k matches), capture metadata plumbing, and doc/changefeed obligations ahead of execution planning.
- **Follow-ups**:
  - Carry AA4-007 cursor/snippet work forward using the new metadata invariants (multi-cursor edits near CR/LF boundaries).
  - AA4-008 DocUI/search overlay work should reuse the deterministic CRLF fuzz harness + `AssertPieceIntegrity` to guard owner-specific decorations.
- **Blockers**:
  - `FixCRLF` behavior interacts with `ChunkUtilities` splitting technique such that initial insertion of `\r\n` as a change-buffer piece or change buffer append clobbers boundaries; need to carefully unify chunk splitting & CRLF rejoin logic. 
  - Due to time constraints, CRLF fixes require further coordinated test coverage and a detailed review vs TS `pieceTreeTextBufferBase` logic.

- **2025-11-22 – Sprint 02 Phase 7 (AA4) Alignment**
  - Synced with Investigator-TS + QA-Automation on TS test inventory (`TestMatrix.md`) and the new plan at `docs/plans/ts-test-alignment.md`; Batch #1 target is `replacePattern.test.ts` parity plus DocUI harness prep.
  - Action items: draft DocUI `replacePattern` execution plan (deliverable/test/dependency map), capture WordSeparator + DocUI selection helper gaps, note harness scaffolding requirements, and ensure outputs flow into migration log, changefeed, TestMatrix, and plan checkpoints.
  - New directive (AA4 Batch #1 – ReplacePattern): before implementation deliver a checklist covering touched files (`ReplacePattern.cs`, DocUI controllers, fixtures, harness JSON/tests), API surface synopsis, migration-log entry template (include QA commands & DocUI snapshots), and risk/dependency plan (WordSeparator cache, harness substitutes). Output must reference Planner checkpoints and broadcast feed `#delta-2025-11-22` once artifacts land.
 - **2025-11-22 – Batch #1 ReplacePattern Kickoff**
   - Began scoping C# runtime drop for `ReplacePattern` (port TS `replacePattern.ts` helpers + `ReplacePatternResult`/`ReplacePatternRequest` types) and lined up DocUI harness needs (`DocUITestHost`, `DocUIReplacePatternTests`, `DocUIReplacePatternFixtures`).
   - TODO next session: map TS `replacePattern.test.ts` cases to `PieceTree.TextBuffer.Tests/DocUIReplacePatternTests.cs`, stub runtime entry in `src/PieceTree.TextBuffer/Search/ReplacePattern.cs`, scaffold DocUI harness under `src/PieceTree.TextBuffer.Tests/DocUI/` with test JSON ingestion, update `docs/plans/ts-test-alignment.md#Batch-1` checkpoints.
   - Dependencies/blockers: need Investigator-TS to confirm WordSeparator + regex expansion semantics, confirm DocUI harness telemetry path, ensure `DocUIHarness.json` sample assets merge cleanly with `ts/test/` snapshots.

## Testing & Validation Plan
- 默认使用 `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` 进行单元测试，按 PT-004 每阶段至少补一个针对 Node/Tree API 的断言。必要时添加 BenchmarkDotNet 基准（待骨架稳定）。
- 关键红黑树操作需辅以调试断言（如节点颜色/黑高），计划构建 Debug-only 验证方法供 QA 复用。

## Hand-off Checklist
1. 所有代码位于 `src/PieceTree.TextBuffer` 并通过 `dotnet test`。
2. Tests or validations performed? 若本轮涉及实现，需提供结果。
3. 下一位接手者读取“Upcoming Goals”并续写实现，同时参考 `src/PieceTree.TextBuffer/README.md` Porting Log 获取代码/测试细节。
