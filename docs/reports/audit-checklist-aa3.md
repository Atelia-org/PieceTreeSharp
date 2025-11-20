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
| CL2 | TextModel search/replace + regex captures/backreferences（TS `textModelSearch.ts`、`pieceTreeTextBufferSearcher.ts`） | `agent-team/handoffs/AA3-002-Audit.md` | `agent-team/handoffs/AA3-004-Result.md` | `PieceTreeSearchTests`、`TextModelSearchTests` | Audit Complete | [`AA3-002-Audit`](../../agent-team/handoffs/AA3-002-Audit.md) |
| CL3 | Diff prettify、move detection、word diff metadata（TS `diffComputer.ts`、`linesDecorations.ts`） | `agent-team/handoffs/AA3-005-Audit.md` | `agent-team/handoffs/AA3-006-Result.md` | `DiffTests` / 新增 word move 覆盖 | Planned | - |
| CL4 | Decorations、IntervalTree stickiness、Markdown DocUI rendering semantics（TS `textModelDecorations.ts`、`modelDecorations.ts`、`markdownRenderer.ts`） | `agent-team/handoffs/AA3-007-Audit.md` | `agent-team/handoffs/AA3-008-Result.md` | `DecorationTests`、`MarkdownRendererTests` | Planned | - |

## Detail Sections

### CL1 – TextModel Options & Metadata
- **Investigator Notes:** See `agent-team/handoffs/AA3-001-Audit.md` (F1–F5). Key gaps: missing creation-option parity, no language-configuration wiring, absent attached-editor lifecycle, EditStack lacks undo-service integration, and search APIs cannot take multi-range scopes.
- **Proposed Fixes:** Implement TS-equivalent creation options/resolved options, add language-configuration + attachment events, port the TS edit stack/undo service bridge, and extend `TextModel` search APIs for multi-range scopes (per `AA3-001-Audit.md`).
- **Validation Hooks:** `TextModelTests` + `TextModelSearchTests` （`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`）。

### CL2 – Search & Replace Advanced Features
- **Investigator Notes:** See `agent-team/handoffs/AA3-002-Audit.md` (F1–F3). Current C# search stack uses default .NET regex semantics instead of ECMAScript, treats all Unicode whitespace as word separators (so whole-word matches diverge), and splits surrogate pairs because the regex engine lacks Unicode code-point mode.
- **Proposed Fixes:** (1) run regexes with ECMAScript semantics (or wrap an ECMAScript engine) so `\b/\w/\d` align with VS Code; (2) align `WordCharacterClassifier` with the TS separator table; (3) introduce surrogate-aware token rewriting so `.`/quantifiers consume emoji as single logical characters.
- **Validation Hooks:** `PieceTreeSearchTests` / `TextModelSearchTests`.

### CL3 – Diff Prettify & Move Metadata
- **Investigator Notes:** _TBD_
- **Proposed Fixes:** _TBD_
- **Validation Hooks:** `DiffTests`.

### CL4 – Decorations & Markdown Rendering
- **Investigator Notes:** _TBD_
- **Proposed Fixes:** _TBD_
- **Validation Hooks:** `DecorationTests` / `MarkdownRendererTests`.
