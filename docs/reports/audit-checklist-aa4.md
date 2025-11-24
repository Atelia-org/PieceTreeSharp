# AA4 Audit Checklist – Alignment & Audit R4

Purpose: 为 Sprint 02 建立 CL5~CL8“发现 → 修复 → 验证”流水线，把 Investigator-TS 的差异分析与 Porter-CS/QA 的实现、测试分离记录，持续缩短主 Agent 上下文占用。

## Scope & Workflow
1. 每个清单条目（CL#）由 Investigator-TS 通过 `runSubAgent` 产出分析，写入本文档与 `agent-team/handoffs/AA4-00X-*.md`。
2. Porter-CS 按条目下“Proposed Fixes” 执行实现/测试，完成后更新 `docs/reports/migration-log.md`、`agent-team/indexes/README.md` changefeed 及 handoff。
3. QA-Automation 在“Validation Hooks” 区域登记新增/扩展测试，并在 run log 中附 `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` 结果。
4. Info-Indexer / DocMaintainer 在条目完成后同步 Sprint / Task Board / AGENTS，保持 changefeed 指针一致。

## Checklist Overview
| ID | Scope | Investigator Output | Porter Output | QA Hooks | Status | Links |
| --- | --- | --- | --- | --- | --- | --- |
| CL5 | PieceTree Builder & Factory parity（TS `pieceTreeTextBufferBuilder.ts`、`pieceTreeBase.ts` vs C# `PieceTreeBuilder`/`ChunkBuffer`/`LineStartBuilder`） | `agent-team/handoffs/AA4-001-Audit.md` | `agent-team/handoffs/AA4-005-Result.md` | `PieceTreeBuilderTests`、`PieceTreeFactoryTests`、`AA005Tests`（CRLF carryover） | QA: Failing (CRLF repair & GetLineRawContent) — Needs Porter-CS follow-up | [`AA4-001-Audit`](../../agent-team/handoffs/AA4-001-Audit.md) |
| CL6 | ChangeBuffer/CRLF/large edits（TS `_insert/_delete`逻辑、`_lastChangeBufferPos`、AverageBufferSize） vs `PieceTreeModel.Edit.cs` | `agent-team/handoffs/AA4-002-Audit.md` | `agent-team/handoffs/AA4-006-Result.md` | `PieceTreeModelTests`（change-buffer heuristics/metadata rebuild/CRLF fuzz）、`CRLFFuzzTests`（deterministic logging）、`PieceTreeBaseTests` | Porter+QA Verified：change buffer reuse solidified、metadata rebuild + `AssertPieceIntegrity`、deterministic CRLF fuzz harness logged in AA4-006 Result；AA4-009 rerun (2025-11-21) captured `export PIECETREE_DEBUG=0 && dotnet test ... --nologo` = 119/119 with fuzz logs redirected to `/tmp/aa4-009-fuzz-logs`. | [`AA4-002-Audit`](../../agent-team/handoffs/AA4-002-Audit.md) |
| CL7 | Cursor word/snippet/multi-selection semantics（TS `cursor.ts`、`cursorWordOperations.ts`、`cursorCommon.ts`） vs `Cursor/Cursor.cs` | `agent-team/handoffs/AA4-003-Audit.md` | `agent-team/handoffs/AA4-007-Result.md` | `CursorMultiSelectionTests` / `CursorWordOperationsTests` / `ColumnSelectionTests` / `SnippetControllerTests` / `MarkdownRendererTests.MultiCursorAndSnippet` | Porter Remediated – See Result | [`AA4-003-Audit`](../../agent-team/handoffs/AA4-003-Audit.md) |
| CL8 | DocUI Find/Replace overlays + Decorations（TS `findController.ts`、`findDecorations.ts`、`textModelSearch.ts`） vs `TextModelSearch` + `MarkdownRenderer` | `agent-team/handoffs/AA4-004-Audit.md` | `agent-team/handoffs/AA4-008-Result.md` | `TextModelSearchTests`、`MarkdownRendererTests`（capture markers、owner filters、search overlays） | In progress – Scope tracking (`#delta-2025-11-24-find-scope`) & scoped regex replace (`#delta-2025-11-24-find-replace-scope`) closed；Decor/FindController items still tracked | [`AA4-004-Audit`](../../agent-team/handoffs/AA4-004-Audit.md) |

## Detail Sections

### CL5 – PieceTree Builder & Factory
- **Investigator Notes:** 详见 `agent-team/handoffs/AA4-001-Audit.md`。关键缺口：chunk splitting/CRLF 修复（carryover + AverageBufferSize）、BOM 丢失、`containsRTL/containsUnusualLineTerminators/isBasicASCII` 未暴露、缺少 `PieceTreeTextBufferFactory`（含 `Create(defaultEOL)` / `GetFirstLineText`）、`DefaultEndOfLine` 选举与 Normalize-EOL 流程均偏离 TS。共列出 6 条 F1~F6（High×2、Medium×4）。
- **Proposed Fixes:** 增补 builder/factory 管线、在 `PieceTreeBuildResult` 保留 BOM+metadata、复刻 `_getEOL` 与 normalize window、将 chunk 分片 helper 复用至 `CreateNewPieces`。Porter 交付记录 `agent-team/handoffs/AA4-005-Result.md`。
- **Porter Status (2025-11-20):** `PieceTreeBuilder` 现通过 `ChunkUtilities.SplitText/NormalizeChunks` 分片，`PieceTreeTextBufferFactory` 负责 BOM/EOL/metadata 输出，`PieceTreeBuffer` 缓存 builder 选项并在 `PieceTreeModel.Insert` 中复用新 helper。新增 `PieceTreeBuilderTests`（chunk + BOM + CR carryover）、`PieceTreeFactoryTests`（preview/EOL heuristics）、`PieceTreeTestHelpers` 以及扩展的 `AA005Tests`（CRLF 修复）。
- **Validation Hooks:** `PieceTreeBuilderTests`, `PieceTreeFactoryTests`, `AA005Tests` 均纳入 `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（95/95）。QA-AA4-009 需在回归中扩展 builder/metadata fuzz cases后再行收口。 

### CL6 – ChangeBuffer / CRLF / Large Edits
- **Investigator Notes:** 详见 `agent-team/handoffs/AA4-002-Audit.md`。F1~F6 覆盖 change buffer append heuristics 缺失、 `_lastChangeBufferPos` 状态缺口、CRLF repair stub、`AverageBufferSize` 拆 chunk缺失、`GetLineFeedCnt` metadata 偏差与 SearchCache 粒度失配。
- **Proposed Fixes:** 恢复 change buffer 语义（复用 buffer0、跟踪 `_lastChangeBufferPos`）、完整移植 TS CRLF/linefeed 逻辑、实现 chunk splitting + metadata recompute + cache validate（`ComputeBufferMetadata`）。
- **Validation Hooks:** `PieceTreeBaseTests`（change buffer fuzz）、`PieceTreeNormalizationTests`（CRLF insert/delete）、`PieceTreeBuilderTests`/`PieceTreeSearchTests`（AverageBufferSize + cache reuse）、`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`。
- **QA Rerun (2025-11-21, AA4-009):** `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` produced 119/119 passing; targeted commands (`--filter FullyQualifiedName~PieceTreeBuilderTests|PieceTreeFactoryTests`, `FullyQualifiedName~CRLF_RandomFuzz_1000`) also green with fuzz logs redirected to `/tmp/aa4-009-fuzz-logs` (seed 123).

### CL7 – Cursor WordOps & Snippet Semantics
- **Investigator Notes:** `agent-team/handoffs/AA4-003-Audit.md` 登记 F1–F4：①缺失 `CursorsController`/`CursorCollection`/`CursorContext` 等多光标基建，DocUI 只有一个光标；②没有 `WordOperations`/`WordCharacterClassifier`，Ctrl+Arrow/删除/wordPart 均退化为逐字符；③未实现 `ColumnSelection`、`CursorConfiguration`、可见列换算以及 `TextModel` 的行辅助接口，导致 Alt+拖拽/垂直移动无法对齐；④完全缺少 `SnippetController2`/`SnippetSession`，`Cursor.cs` 里仍是 TODO，DocUI 也无法渲染 tabstop/choice 装饰。
- **Proposed Fixes:** 按 `AA4-003-Audit` 的主题顺序推进：先复刻 TS 多光标管线（含 `CursorState`、事件恢复、MarkdownRenderer owner），再移植 `WordOperations` + `WordNavigationType` + 分词器，随后补齐列选择/垂直移动所需的 `CursorConfiguration` 与 `TextModel` 行级 helper，最后实现 snippet session/controller 并暴露占位符装饰。
- **Validation Hooks:** 新增 `CursorMultiSelectionTests`、`CursorWordOperationsTests`、`ColumnSelectionTests`、`SnippetControllerTests` 以及 `MarkdownRendererTests.MultiCursorAndSnippet`，外加 DocUI snapshot 样例以覆盖多光标 + snippet 输出。

### CL8 – DocUI Find/Replace & Decorations
- **Investigator Notes:** `AA4-004-Audit.md` 记录 F1–F4：① `HighlightSearchMatches` 仅生成裸 `SearchMatch` 装饰，缺少 TS `FindDecorations` 的 current-match/range-highlight/overview-only 语义，也不在 1k+ 命中时降级，从而没有 minimap/glyph lanes；② 没有持久化的 `FindModel`/search-scope 状态，`SearchHighlightOptions` 也缺少 `wholeWord/preserveCase/findInSelection` 等字段，DocUI 无法呈现 scope overlay 与 match index；③ `ReplacePattern`/`ReplaceCommand` 尚未移植，captureMatches 虽由 `TextModelSearch` 产出却被丢弃，导致 replace preview/preserveCase/backreference 缺失；④ `MarkdownRenderer` 重新执行查找并无视 owner filters/capture 数据，因此 DocUI 无法叠加 search owner、replace 预览或 capture 注释。
- **Proposed Fixes:** （a）移植 `FindDecorations` 选项与降级策略，新增 current-match/overview-only/find-scope/range highlight decorations，并让 DocUI 直接消费 `DecorationOwnerIds.SearchHighlights`；（b）实现 C# 版 `FindModel` + 扩展 `SearchHighlightOptions`（whole word、PreserveCase、loop、scope ranges），以便维护 `_startPosition`、match index、find-in-selection；（c）引入 `ReplacePattern`/`ReplaceAllCommand` 等替换管线，将 capture 数组附加到装饰元数据供 DocUI 呈现；（d）让 `MarkdownRenderer` 读取 search owner 装饰并渲染 minimap/overview/glyph 注解与 replace preview，而非重复 Find。
- **Status Notes (2025-11-24):** Scope tracking + normalization landed via `#delta-2025-11-24-find-scope`（FindModel.Research uses live decorations, DocUI tests Test45/Test46），scoped regex replace capture parity via `#delta-2025-11-24-find-replace-scope`（`GetMatchesForReplace` hydration + Test47）。Decorations/FindController backlog items (overview throttling, clipboard/focus, multi-selection scopes) remain on B3-FM/B3-FC/B3-Decor tracks.
- **Validation Hooks:** `TextModelSearchTests.SearchDecorationsIncludeMetadata`（overview/minimap/owner 过滤）、`FindControllerTests.ScopeAndReplaceParity`、`MarkdownRendererTests.SearchScopeAndReplacePreview`、DocUI 快照涵盖 owner filter/replace 预览/limit≥1000 场景。
