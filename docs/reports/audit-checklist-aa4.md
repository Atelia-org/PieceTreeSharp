# AA4 Audit Checklist – Alignment & Audit R4

Purpose: 为 Sprint 02 建立 CL5~CL8“发现 → 修复 → 验证”流水线，把 Investigator-TS 的差异分析与 Porter-CS/QA 的实现、测试分离记录，持续缩短主 Agent 上下文占用。

## Scope & Workflow
1. 每个清单条目（CL#）由 Investigator-TS 通过 `runSubAgent` 产出分析，写入本文档与 `agent-team/handoffs/AA4-00X-*.md`。
2. Porter-CS 按条目下“Proposed Fixes” 执行实现/测试，完成后更新 `docs/reports/migration-log.md`、`agent-team/indexes/README.md` changefeed 及 handoff。
3. QA-Automation 在“Validation Hooks” 区域登记新增/扩展测试，并在 run log 中附 `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` 结果。
4. Info-Indexer / DocMaintainer 在条目完成后同步 Sprint / Task Board / AGENTS，保持 changefeed 指针一致。

## Checklist Overview
| ID | Scope | Investigator Output | Porter Output | QA Hooks | Status | Links |
| --- | --- | --- | --- | --- | --- | --- |
| CL5 | PieceTree Builder & Factory parity（TS `pieceTreeTextBufferBuilder.ts`、`pieceTreeBase.ts` vs C# `PieceTreeBuilder`/`ChunkBuffer`/`LineStartBuilder`） | `agent-team/handoffs/AA4-001-Audit.md` | `agent-team/handoffs/AA4-005-Result.md` | Builder chunk tests、factory/EOL heuristics（`PieceTreeBuilderTests` TBD） | Audit Complete – Awaiting Fix | [`AA4-001-Audit`](../../agent-team/handoffs/AA4-001-Audit.md) |
| CL6 | ChangeBuffer/CRLF/large edits（TS `_insert/_delete` logic、`_lastChangeBufferPos`、AverageBufferSize） vs `PieceTreeModel.Edit.cs` | `agent-team/handoffs/AA4-002-Audit.md` | `agent-team/handoffs/AA4-006-Result.md` | `PieceTreeBaseTests` / 新增 ChangeBuffer fuzz & CRLF bridging cases | Audit Complete – Awaiting Fix | [`AA4-002-Audit`](../../agent-team/handoffs/AA4-002-Audit.md) |
| CL7 | Cursor word/snippet/multi-selection semantics（TS `cursor.ts`、`cursorWordOperations.ts`、`cursorCommon.ts`） vs `Cursor/Cursor.cs` | `agent-team/handoffs/AA4-003-Audit.md` | `agent-team/handoffs/AA4-007-Result.md` | `CursorTests` / `MarkdownRendererTests`（word mark、column select、snippet tabstop） | Planned | `AA4-003-Audit` (tbd) |
| CL8 | DocUI Find/Replace overlays + Decorations（TS `findController.ts`、`findDecorations.ts`、`textModelSearch.ts`） vs `TextModelSearch` + `MarkdownRenderer` | `agent-team/handoffs/AA4-004-Audit.md` | `agent-team/handoffs/AA4-008-Result.md` | `TextModelSearchTests`、`MarkdownRendererTests`（capture markers、owner filters、search overlays） | Planned | `AA4-004-Audit` (tbd) |

## Detail Sections

### CL5 – PieceTree Builder & Factory
- **Investigator Notes:** 详见 `agent-team/handoffs/AA4-001-Audit.md`。关键缺口：chunk splitting/CRLF 修复（carryover + AverageBufferSize）、BOM 丢失、`containsRTL/containsUnusualLineTerminators/isBasicASCII` 未暴露、缺少 `PieceTreeTextBufferFactory`（含 `Create(defaultEOL)` / `GetFirstLineText`）、`DefaultEndOfLine` 选举与 Normalize-EOL 流程均偏离 TS。共列出 6 条 F1~F6（High×2、Medium×4）。
- **Proposed Fixes:** 增补 builder/factory 管线、在 `PieceTreeBuildResult` 保留 BOM+metadata、复刻 `_getEOL` 与 normalize window、将 chunk 分片 helper 复用至 `CreateNewPieces`。Porter 交付记录 `agent-team/handoffs/AA4-005-Result.md`。
- **Validation Hooks:** 新增 `PieceTreeBuilderParityTests`（chunk split）、`PieceTreeBuilderMetadataTests`（BOM/flags）、`PieceTreeBuilderDefaultEolTests`、`PieceTreeModelNormalizeEolTests`、`PieceTreeTextBufferFactoryTests.GetFirstLineText`；最终回归 `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` ≥92。 

### CL6 – ChangeBuffer / CRLF / Large Edits
- **Investigator Notes:** 详见 `agent-team/handoffs/AA4-002-Audit.md`。F1~F6 覆盖 change buffer append heuristics 缺失、 `_lastChangeBufferPos` 状态缺口、CRLF repair stub、`AverageBufferSize` 拆 chunk缺失、`GetLineFeedCnt` metadata 偏差与 SearchCache 粒度失配。
- **Proposed Fixes:** 恢复 change buffer 语义（复用 buffer0、跟踪 `_lastChangeBufferPos`）、完整移植 TS CRLF/linefeed 逻辑、实现 chunk splitting + metadata recompute + cache validate（`ComputeBufferMetadata`）。
- **Validation Hooks:** `PieceTreeBaseTests`（change buffer fuzz）、`PieceTreeNormalizationTests`（CRLF insert/delete）、`PieceTreeBuilderTests`/`PieceTreeSearchTests`（AverageBufferSize + cache reuse）、`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`。

### CL7 – Cursor WordOps & Snippet Semantics
- **Investigator Notes:** 待 `AA4-003-Audit`。需比较：
  - Word 左右/上一/下一跳、`wordPartLeft/Right`、`skipWordPart` 等 API
  - `columnSelect` & `selectionGrow/shrink` 行为
  - Snippet tabstop stickiness、InjectedText 对 cursor 的影响
  - 多光标 (primary + secondary) 与装饰 owner 协同
- **Proposed Fixes:** 扩展 `Cursor` 类为多状态机，加入 `WordOperations` 辅助层、`Selection` 批量更新、DocUI renderer 额外标记。
- **Validation Hooks:** `CursorTests` (word、column、snippet)、`MarkdownRendererTests` (multi cursor markers)、DocUI snapshot。

### CL8 – DocUI Find/Replace & Decorations
- **Investigator Notes:** 待 `AA4-004-Audit`。需覆盖：
  - TS `FindController` 的 delta decorations（overview/minimap/glyph lanes）与 `findDecorations.ts` 管线
  - Capture matches、Replace preview、owner 分层策略
  - DocUI MarkdownRenderer / Search overlay 之间的差异（multi-range highlight、limitResultCount、InjectedText interplay）
- **Proposed Fixes:** 扩展 `TextModelSearch` 公开 `FindController` 风格的 `FindDecorations` 数据，升级 `MarkdownRenderer` 以渲染 capture markers/replace preview/glyph/minimap hints。
- **Validation Hooks:** `TextModelSearchTests` (capture + owner filters)、`MarkdownRendererTests` (search overlay & replace preview)、DocUI diff snapshot。
