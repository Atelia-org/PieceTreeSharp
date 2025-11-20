# Task Board - Phase 6: Alignment & Audit R3

**Goal:** 启动第三轮 TS↔C# 对照审核（TextModel/搜索/Diff/Decorations/DocUI），通过“发现→修复→验证”链条确保 DocUI-ready 质量。

**Changefeed Reminder:** 在修改任务状态前读取 `agent-team/indexes/README.md#delta-2025-11-20`，必要时请求 Info-Indexer 发布新 delta，避免与 `docs/reports/migration-log.md` 失联。

## Alignment & Audit R3 (AA3/OI)

| ID | Description | Owner | Key Artifacts | runSubAgent Budget | Status | Latest Update |
| --- | --- | --- | --- | --- | --- | --- |
| AA3-001 | Investigator：CL1（TextModel options / metadata / events）对照，生成差异清单 | Investigator-TS | `ts/src/vs/editor/common/model/textModel.ts`<br>`src/PieceTree.TextBuffer/TextModel.cs`<br>`docs/reports/audit-checklist-aa3.md`<br>`agent-team/handoffs/AA3-001-Audit.md` | 2 | Done | `AA3-001-Audit.md` 列出 F1~F5（creation options、language config、attachment、EditStack、multi-range search）；CL1 清单已更新。 |
| AA3-002 | Investigator：CL2（Search/Replace + regex captures）对照，生成差异清单 | Investigator-TS | `ts/src/vs/editor/common/model/textModelSearch.ts`<br>`src/PieceTree.TextBuffer/Core/PieceTreeSearcher.cs`<br>`agent-team/handoffs/AA3-002-Audit.md` | 2 | Done | `AA3-002-Audit.md` 记录 F1~F3（ECMAScript regex、word separators、surrogate pairs）；CL2 清单已更新。 |
| AA3-005 | Investigator：CL3（Diff prettify & move metadata）对照 | Investigator-TS | `ts/src/vs/editor/common/diff/diffComputer.ts`<br>`src/PieceTree.TextBuffer/Diff/DiffComputer.cs`<br>`agent-team/handoffs/AA3-005-Audit.md` | 2 | Done | `AA3-005-Audit.md` enumerates F1–F4（LinesDiff/innerChanges、move detection、options/timeout、DocUI plumbing）。 |
| AA3-007 | Investigator：CL4（Decorations & Markdown DocUI）对照 | Investigator-TS | `ts/src/vs/editor/common/model/textModelDecorations.ts`<br>`src/PieceTree.TextBuffer/Decorations`<br>`agent-team/handoffs/AA3-007-Audit.md` | 2 | Done | `AA3-007-Audit.md` covers ModelDecoration options, decoration trees/events, stickiness, MarkdownRenderer gaps。 |
| AA3-003 | Porter：落实 CL1 修复（TextModel options/metadata），更新 tests & 迁移日志 | Porter-CS | `src/PieceTree.TextBuffer/TextModel.cs`<br>`docs/reports/audit-checklist-aa3.md#cl1`<br>`agent-team/handoffs/AA3-003-Result.md` | 3 | In Review | TextModel options + undo parity landed (`AA3-003-Result.md`); `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` 79/79 green. |
| AA3-004 | Porter：落实 CL2 修复（Search/Replace/regex） | Porter-CS | `src/PieceTree.TextBuffer/Core/PieceTreeSearcher.cs`<br>`PieceTree.TextBuffer.Tests/PieceTreeSearchTests.cs`<br>`agent-team/handoffs/AA3-004-Result.md` | 3 | Done | `AA3-004-Result.md`: ECMAScript regex + surrogate-safe wildcards + word-separator parity; tests `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` (84/84). |
| AA3-006 | Porter：落实 CL3 修复（Diff/move metadata + consumers） | Porter-CS | `src/PieceTree.TextBuffer/Diff`<br>`src/PieceTree.TextBuffer/Rendering/MarkdownRenderer.cs`<br>`agent-team/handoffs/AA3-006-Result.md` | 3 | Done | `AA3-006-Result.md`: LinesDiff + move heuristics + timeout options landed；`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` 80/80. |
| AA3-008 | Porter：落实 CL4 修复（Decorations/DocUI） | Porter-CS | `src/PieceTree.TextBuffer/Decorations`<br>`src/PieceTree.TextBuffer/Rendering/MarkdownRenderer.cs`<br>`agent-team/handoffs/AA3-008-Result.md` | 3 | In Review | 2025-11-20 – Delivered DecorationsTrees/DecorationRangeUpdater, metadata-rich `OnDidChangeDecorations`, and DocUI renderer upgrades (`agent-team/handoffs/AA3-008-Result.md`); `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` (85/85). |
| AA3-009 | QA：扩展 TextModel/Search/Diff/Decoration/Markdown Tests，记录 `dotnet test ...` 基线 | QA-Automation | `src/PieceTree.TextBuffer.Tests/`<br>`docs/reports/audit-checklist-aa3.md`<br>`agent-team/handoffs/AA3-009-QA.md` | 2 | Planned | Dependent on AA3-003~008 |
| OI-010 | Info-Indexer：同步 Sprint 01 产物至 changefeed/索引，维护 AGENTS/Sprint/Task Board 一致性 | Info-Indexer | `agent-team/indexes/README.md`<br>`docs/sprints/sprint-01.md`<br>`docs/reports/migration-log.md` | 1 | Planned | Gated on AA3 deliverables |

## Reference & Logs

- `agent-team/task-board-v5-archive.md` – Phase 5 (Alignment & Audit R2) 历史。
- `agent-team/task-board-v4-archive.md` – Phase 4 历史。
- `agent-team/task-board-v3-archive.md` – Phase 3 历史。
- `agent-team/task-board-v2-archive.md` – Phase 2 历史。
- `agent-team/task-board-v1-archive.md` – Phase 1 历史。
- `docs/sprints/sprint-01.md` – Sprint 01 目标/计划。
- `docs/reports/audit-checklist-aa3.md` – CL1~CL4 清单。
- `agent-team/indexes/README.md` – canonical Info-Indexer changefeed。
- `docs/reports/migration-log.md` – full migration audit trail。
- `AGENTS.md` – cross-session log。
