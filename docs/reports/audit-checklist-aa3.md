# AA3 Audit Checklist – Alignment & Audit R3

Purpose: 为 Sprint 01 建立“发现 → 修复 → 验证”流水线，把 Investigator-TS 的差异分析与 Porter-CS/QA 的落地工作隔离到文件中，减少主 Agent 上下文占用。

## Scope & Workflow
1. 每个清单条目（CL#）由 Investigator-TS 通过 `runSubAgent` 产出分析，写入本文档与 `agent-team/handoffs/AA3-00X-*.md`。
2. Porter-CS 根据对应条目下的“Proposed Fixes” 执行实现/测试，必要时增补 `docs/reports/migration-log.md` 与 changefeed。
3. QA-Automation 在“Validation Hooks” 区域登记新增/扩展的测试，并在 run log 中附 `dotnet test` 命令。
4. Info-Indexer / DocMaintainer 在条目完成后更新索引、AGENTS、Task Board。

## Checklist Overview
| ID | Scope | Investigator Output | Porter Output | QA Hooks | Status | Links |
| --- | --- | --- | --- | --- | --- | --- |
| CL1 | TextModel options、语言/缩进元数据、Content change events（TS `textModel.ts` vs C# `TextModel.cs`） | `agent-team/handoffs/AA3-001-Audit.md` | `agent-team/handoffs/AA3-003-Result.md` | `TextModelTests` + `TextModelSearchTests`（CreationOptions/Undo/Language/多选区搜索） | Audit Complete – Fixes Landed | [`AA3-001-Audit`](../../agent-team/handoffs/AA3-001-Audit.md)<br>[`AA3-003-Result`](../../agent-team/handoffs/AA3-003-Result.md) |
| CL2 | TextModel search/replace + regex captures/backreferences（TS `textModelSearch.ts`、`pieceTreeTextBufferSearcher.ts`） | `agent-team/handoffs/AA3-002-Audit.md` | `agent-team/handoffs/AA3-004-Result.md` | `PieceTreeSearchTests`、`TextModelSearchTests` | Audit Complete – Fixes Landed | [`AA3-002-Audit`](../../agent-team/handoffs/AA3-002-Audit.md)<br>[`AA3-004-Result`](../../agent-team/handoffs/AA3-004-Result.md) |
| CL3 | Diff prettify、move detection、word diff metadata（TS `defaultLinesDiffComputer/*.ts`、`rangeMapping.ts`） | `agent-team/handoffs/AA3-005-Audit.md` | `agent-team/handoffs/AA3-006-Result.md` | `DiffTests` / 新增 word move 覆盖 | Audit Complete – Fixes Landed | [`AA3-005-Audit`](../../agent-team/handoffs/AA3-005-Audit.md)<br>[`AA3-006-Result`](../../agent-team/handoffs/AA3-006-Result.md) |
| CL4 | Decorations、IntervalTree stickiness、Markdown DocUI rendering semantics（TS `textModelDecorations.ts`、`modelDecorations.ts`、`markdownRenderer.ts`） | `agent-team/handoffs/AA3-007-Audit.md` | `agent-team/handoffs/AA3-008-Result.md` | `DecorationTests`、`MarkdownRendererTests` | Audit Complete – Fixes Pending | [`AA3-007-Audit`](../../agent-team/handoffs/AA3-007-Audit.md) |

## Detail Sections

### CL1 – TextModel Options & Metadata
- **Investigator Notes:** See `agent-team/handoffs/AA3-001-Audit.md` (F1–F5). Key gaps: missing creation-option parity, no language-configuration wiring, absent attached-editor lifecycle, EditStack lacks undo-service integration, and search APIs cannot take multi-range scopes.
- **Proposed Fixes:** Implement TS-equivalent creation options/resolved options, add language-configuration + attachment events, port the TS edit stack/undo service bridge, and extend `TextModel` search APIs for multi-range scopes (per `AA3-001-Audit.md`).
- **Validation Hooks:** `TextModelTests` + `TextModelSearchTests` （`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`）。

### CL2 – Search & Replace Advanced Features
- **Investigator Notes:** See `agent-team/handoffs/AA3-002-Audit.md` (F1–F3). Current C# search stack uses default .NET regex semantics instead of ECMAScript, treats all Unicode whitespace as word separators (so whole-word matches diverge), and splits surrogate pairs because the regex engine lacks Unicode code-point mode.
- **Porter Result:** `agent-team/handoffs/AA3-004-Result.md` describes the landed fix (ECMAScript regex compilation, TS word separator parity, surrogate-aware wildcard rewrites, and new AA3 regression tests).
- **Validation Hooks:** `PieceTreeSearchTests` / `TextModelSearchTests`.

### CL3 – Diff Prettify & Move Metadata
- **Investigator Notes:** See `agent-team/handoffs/AA3-005-Audit.md` (F1–F4). The current C# `DiffComputer` only emits char-level `DiffChange`s, lacks `DetailedLineRangeMapping`/`hitTimeout`, performs move detection via trimmed string equality, and exposes no whitespace/subword options. Decorations/`MarkdownRenderer` cannot consume diff metadata, so DocUI cannot render word-level diffs or moves.
- **Porter Result:** `agent-team/handoffs/AA3-006-Result.md` implements the TS `LinesDiff` pipeline, move detection heuristics, timeout-aware options, and new diff regression tests. DocUI consumption remains deferred to CL4/AA3-008.
- **Validation Hooks:** `DiffTests` now cover word diff, whitespace-ignore, move detection, and timeout parity; DocUI renderers will get additional coverage under AA3-008.

### CL4 – Decorations & Markdown Rendering
- **Investigator Notes:** See `agent-team/handoffs/AA3-007-Audit.md` (F1–F4). Key gaps: `ModelDecorationOptions` drops overview/minimap/glyph/injected-text metadata, the single-tree storage/event pipeline cannot flag minimap/overview/glyph changes, stickiness/update math diverges from TS `nodeAcceptEdit`, and `MarkdownRenderer` only renders cursor/selection/search so diff/move metadata from AA3-006 never shows up.
- **Proposed Fixes:** Port TS `ModelDecorationOptions`, `DecorationsTrees`, and `DidChangeDecorationsEmitter`, adopt `nodeAcceptEdit`/`acceptReplace` semantics (with `forceMoveMarkers`), and upgrade `MarkdownRenderer` to consume the expanded decoration set (z-index ordering, injected text, diff overlays, glyph/minimap stripes) per AA3-007 findings.
- **Validation Hooks:** `DecorationTests` (options parity, stickiness, event flags) / `MarkdownRendererTests` (diff/injected-text snapshots, glyph & overview rendering).
