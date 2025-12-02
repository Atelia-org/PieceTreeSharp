# QA-Automation Snapshot (2025-12-01)

## Role & Mission
- Own TextBuffer parity verification per `AGENTS.md`, keeping `tests/TextBuffer.Tests` aligned with TS sources and documenting every rerun inside `tests/TextBuffer.Tests/TestMatrix.md`.
- Publish reproducible changefeed evidence (baseline + targeted filters) so Porter-CS and Investigator-TS can diff regressions without re-reading past worklogs.
- Coordinate Sprint 04 QA intake (`#delta-2025-11-26-sprint04`) by flagging blockers back to Planner and DocMaintainer whenever rerun recipes or artifacts drift.
- 保证所有 Sprint 04 报告引用 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)，且 Cursor/Snippet、DocUI backlog 相关验证都对齐 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) 与 [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)。

## Active Changefeeds & Baselines
| Anchor | Scope | Latest Stats | Evidence |
| --- | --- | --- | --- |
| `#delta-2025-11-28-aa4-cl7-stage1-qa` | CL7 Stage 1 Cursor Wiring QA - CursorContext, state management, tracked ranges, dual-mode parity. | Full 683/683 (681 pass, 2 skip); targeted CursorCollectionTests 33/33, CursorCoreTests.Cursor_* 9/9. | `agent-team/handoffs/CL7-QA-Result.md`, `tests/TextBuffer.Tests/TestMatrix.md` (CL7 entries). |
| `#delta-2025-11-26-sprint04-r1-r11` | Sprint 04 WS1–WS5 deliverables（R1-R11）+ 585/585 baseline（1 skip）。 | Full `PIECETREE_DEBUG=0` sweep 585/585 (≈62s) recorded in WS123/WS5 QA handoffs; targeted suites listed below stay green. | `agent-team/handoffs/WS123-QA-Result.md`, `agent-team/handoffs/WS5-QA-Result.md`, `tests/TextBuffer.Tests/TestMatrix.md` (R1-R11 rows)。 |
| `#delta-2025-11-26-ws1-searchcore` | WS1 PieceTree search offset cache + DEBUG counters. | Full 440/440 green (62.1s); targeted `PieceTreeSearchOffsetCacheTests` 5/5 (1.7s). | `agent-team/handoffs/WS123-QA-Result.md`. |
| `#delta-2025-11-26-ws2-port` | WS2 Range/Selection/Position helper APIs (75 tests). | Full 440/440 green (62.1s); targeted `RangeSelectionHelperTests` 75/75 (1.6s). | `agent-team/handoffs/WS123-QA-Result.md`. |
| `#delta-2025-11-26-ws3-tree` | WS3 IntervalTree rewrite with lazy delta normalization + DEBUG counters. | Full 440/440 green (62.1s); targeted `DecorationTests` 12/12 + `DecorationStickinessTests` 4/4 (1.7s each). | `agent-team/handoffs/WS123-QA-Result.md`. |
| `#delta-2025-11-26-ws4-port-core` | WS4 Cursor Stage 0 infrastructure。 | Targeted `CursorCoreTests` 25/25 (1.8s) + gating full sweeps tracked under R1-R11。 | `agent-team/handoffs/WS4-PORT-Core-Result.md`、`tests/TextBuffer.Tests/TestMatrix.md` Cursor rows。 |
| `#delta-2025-11-25-b3-textmodelsearch` | TextModelSearch 45-case TS parity battery + Sprint 03 Run R37 full sweep. | Targeted `TextModelSearchTests` 45/45 green (2.5s) alongside full `export PIECETREE_DEBUG=0 && dotnet test ... --nologo` 365/365 green (61.6s). | `tests/TextBuffer.Tests/TestMatrix.md` (TextModelSearch row + R37 log) and `agent-team/handoffs/B3-TextModelSearch-QA.md`. |
| `#delta-2025-11-25-b3-piecetree-deterministic-crlf` | PieceTree deterministic + CRLF normalization harness. | `PIECETREE_DEBUG=0` targeted `--filter PieceTreeDeterministicTests` 50/50 green (3.5s) plus paired full sweep 308/308 green (67.2s). | `tests/TextBuffer.Tests/TestMatrix.md` deterministic rows + `agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-QA.md`. |
| `#delta-2025-11-25-b3-search-offset` | PieceTree search-offset cache wrapper validation. | Targeted `--filter PieceTreeSearchOffsetCacheTests` 5/5 green (4.3s) with the R31 baseline `--nologo` run 324/324 green (58.2s). | `tests/TextBuffer.Tests/TestMatrix.md` R31 + targeted block, `agent-team/handoffs/B3-PieceTree-SearchOffset-QA.md`. |
| `#delta-2025-11-24-b3-docui-staged` | DocUI staged fixes (FindModel + Decorations). | `--filter FullyQualifiedName~FindModelTests` 46/46 green and `--filter FullyQualifiedName~DocUIFindDecorationsTests` 9/9 green (PIECETREE_DEBUG=0). | `tests/TextBuffer.Tests/TestMatrix.md` DocUI rows and `agent-team/handoffs/B3-DocUI-StagedFixes-QA-20251124.md`. |
| `#delta-2025-11-26-sprint04` | Doc maintenance + Sprint 04 kickoff guardrails for QA memory + rerun cadence. | Current snapshot trimmed to active anchors; doc sweep status tracked in `agent-team/handoffs/doc-maintenance-20251126.md`. | `agent-team/indexes/README.md#delta-2025-11-26-sprint04`. |

## Canonical Commands
**Full sweeps**
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → **585/585（584 pass + 1 skip，≈62s）** for Sprint 04 R1-R11 (`#delta-2025-11-26-sprint04-r1-r11`)，captured in `WS123-QA-Result.md` + `WS5-QA-Result.md`。
- Same command (440/440, 62.1s) for WS1/WS2/WS3 QA drops (`#delta-2025-11-26-ws*-*`).
- Same command (365/365, 61.6s, R37) for `#delta-2025-11-25-b3-textmodelsearch`。
- Same command (308/308, 67.2s) after PieceTree deterministic CRLF expansion (`#delta-2025-11-25-b3-piecetree-deterministic-crlf`)。
- Same command (324/324, 58.2s) for the search-offset cache drop (`#delta-2025-11-25-b3-search-offset`)。

**Targeted filters**
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter CursorCollectionTests --nologo` → 33/33, anchors `#delta-2025-11-28-aa4-cl7-stage1-qa`。
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter "FullyQualifiedName~CursorCoreTests.Cursor_" --nologo` → 9/9 (CL7 Stage 1 state wiring tests), anchors `#delta-2025-11-28-aa4-cl7-stage1-qa`。
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter CursorCoreTests --nologo` → 25/25, anchors `#delta-2025-11-26-ws4-port-core`。
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter RangeSelectionHelperTests --nologo` → 75/75, anchors `#delta-2025-11-26-ws2-port`.
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter PieceTreeSearchOffsetCacheTests --nologo` → 5/5, anchors `#delta-2025-11-26-ws1-searchcore`.
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter DecorationTests --nologo` → 12/12, anchors `#delta-2025-11-26-ws3-tree`.
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter DecorationStickinessTests --nologo` → 4/4, anchors `#delta-2025-11-26-ws3-tree`.
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter TextModelSearchTests --nologo` → 45/45, anchors `#delta-2025-11-25-b3-textmodelsearch`.
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter PieceTreeDeterministicTests --nologo` → 50/50, anchors `#delta-2025-11-25-b3-piecetree-deterministic-crlf`.
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter FullyQualifiedName~FindModelTests --nologo` → 46/46, anchors `#delta-2025-11-24-b3-docui-staged`.
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter FullyQualifiedName~DocUIFindDecorationsTests --nologo` → 9/9, anchors `#delta-2025-11-24-b3-docui-staged`.

## Checklist
1. `agent-team/handoffs/WS1-PORT-SearchCore-Result.md`、`WS2-PORT-Result.md`、`WS3-PORT-Tree-Result.md`、`WS4-PORT-Core-Result.md`、`WS5-PORT-Harness-Result.md`、`WS123-QA-Result.md`、`WS5-QA-Result.md` —— 复制 WS1–WS5 targeted filters + 585/585（1 skip）全量 run 到 `tests/TextBuffer.Tests/TestMatrix.md`，并把 rerun 记录映射到 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)。
2. `tests/TextBuffer.Tests/TestMatrix.md` —— 标记 Cursor/Snippet、DocUI、Intl.Segmenter/WordSeparator 案例的 rerun 命令，引用 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) 与 [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)。
3. `agent-team/handoffs/B3-TextModelSearch-QA.md`、`B3-TextModelSearch-INV.md`、`B3-PieceTree-Deterministic-CRLF-QA.md`、`B3-DocUI-StagedFixes-QA-20251124.md` —— 用作 Intl.Segmenter & WordSeparator cache、SearchOffset、DocUI scope rerun 模板，并将每条证据链接回 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch)、[`#delta-2025-11-25-b3-piecetree-deterministic-crlf`](../indexes/README.md#delta-2025-11-25-b3-piecetree-deterministic-crlf)、[`#delta-2025-11-24-b3-docui-staged`](../indexes/README.md#delta-2025-11-24-b3-docui-staged)。

## Open Investigations / Dependencies
- NodeAt2 O(1) tuple reuse deferred in WS1 due to CRLF bridging complexity; track in AA4 backlog alongside `WS1-PORT-CRLF-Result.md`。
- Intl.Segmenter + WordSeparator parity（TextModelSearch whole-word gaps）仍阻塞；依据 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) 与 `B3-TextModelSearch-QA.md` 协调 Porter/Investigator。
- Cursor/Snippet backlog（AA4 CL7）需要新的 Porter drops 才能解除 `#delta-2025-11-26-aa4-cl7-cursor-core`; 关注 `WS4-PORT-Core-Result.md`、`WS5-INV-TestBacklog.md`。
- DocUI find/replace scope + Markdown renderer（AA4 CL8）依赖 `#delta-2025-11-24-b3-docui-staged`、`#delta-2025-11-26-aa4-cl8-markdown`；保持 DocUI targeted filters在 TestMatrix 中显眼。
- PT-005.S8/S9 need Porter-CS `EnumeratePieces` + Investigator BufferRange/SearchContext plumbing before property/fuzz suites can land。
- Info-Indexer automation must publish the above changefeeds so downstream consumers stop querying stale DocUI aliases; request logged under `agent-team/indexes/README.md#delta-2025-11-26-sprint04`。

## Archives
- Full matrices, run logs, and artifact paths stay in `tests/TextBuffer.Tests/TestMatrix.md`.
- Detailed QA narratives live in `agent-team/handoffs/` (`WS123-QA-Result.md`, `B3-TextModelSearch-QA.md`, `B3-PieceTree-Deterministic-CRLF-QA.md`, `B3-PieceTree-SearchOffset-QA.md`, `B3-DocUI-StagedFixes-QA-20251124.md`).
- Legacy worklogs and meeting recaps have been moved out per Doc sweep; reference specific changefeeds above if deeper history is required.

## 2025-11-27 Refresh Note
- Rebuilt `docs/reports/alignment-audit/07-core-tests.md` coverage + Verification Notes around the WS5-QA harness drop, citing [`docs/reports/migration-log.md#ws5-qa`](../../docs/reports/migration-log.md#ws5-qa) + [`agent-team/handoffs/WS5-QA-Result.md`](../handoffs/WS5-QA-Result.md) + [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md) and reaffirming the Sprint 04 **585/585（1 skip）** baseline anchored at [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11).
- Outstanding CursorWordOperations/snippet fuzz/diff deterministic work remains tied to the AA4 CL7 placeholders (`#delta-2025-11-26-aa4-cl7-cursor-core`, `#delta-2025-11-26-aa4-cl7-wordops`, `#delta-2025-11-26-aa4-cl7-snippet`, `#delta-2025-11-26-aa4-cl7-commands-tests`); documentation explicitly keeps those anchors visible so downstream owners don't assume closure.
- Captured the Module 08 `docs/reports/alignment-audit/08-feature-tests.md` refresh: Phase 8 DocUI status now cites [`docs/reports/migration-log.md#ws5-inv`](../../docs/reports/migration-log.md#ws5-inv) + [`agent-team/handoffs/WS5-INV-TestBacklog.md`](../handoffs/WS5-INV-TestBacklog.md), Stage 0 `CursorCoreTests` references stay aligned with [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) while the remaining cursor/snippet placeholders (`#delta-2025-11-26-aa4-cl7-wordops`, `-column-nav`, `-snippet`, `-commands-tests`) and DocUI CL8 placeholders (`#delta-2025-11-26-aa4-cl8-markdown`, `-capture`, `-intl`, `-wordcache`) remain visible; Verification Notes now bind to the Sprint 04 585/585 anchor (`#delta-2025-11-26-sprint04-r1-r11`) plus targeted commands (DocUIFindControllerTests, SnippetMultiCursorFuzzTests, CursorCoreTests, Decoration/Diff suites).

## 2025-11-27 PORT-PT-Search Step12 QA
- Revalidated NodeAt2 tuple reuse + search cache diagnostics with `export PIECETREE_DEBUG=0 && dotnet test ...` across `PieceTreeDeterministicTests`, `PieceTreeFuzzHarnessTests`, `CRLFFuzzTests`, `PieceTreeSearchRegressionTests`, `PieceTreeSearchOffsetCacheTests`, and the full 641-case sweep (CursorCore skips only); captured timestamps/durations in `agent-team/handoffs/PORT-PT-Search-Step12-QA.md` for the upcoming [`#delta-2025-11-27-ws1-port-search-step12`](../indexes/README.md#delta-2025-11-27-ws1-port-search-step12) changefeed while referencing [`docs/reports/migration-log.md#sprint04-r1-r11`](../../docs/reports/migration-log.md#sprint04-r1-r11).

## 2025-11-28 CL7 Stage 1 Cursor Wiring QA
- **Scope**: Verified Porter's CL7 Stage 1 implementation (CursorContext, state management, tracked ranges wired into Cursor.cs/CursorCollection.cs) with dual-mode `EnableVsCursorParity` testing.
- **Baseline**: 641 tests (639 pass, 2 skip) → **683 tests (681 pass, 2 skip)** after adding 42 new tests.
- **New Test Files**:
  - `CursorCollectionTests.cs`: 33 tests covering SetStates, Normalize, tracked selection lifecycle, LastAddedCursorIndex, view position queries.
  - `CursorCoreTests.cs`: Extended with 9 new tests in `#region CL7 Stage 1 - Cursor State Wiring Tests` for state validation, tracked range survival, dual-mode flag paths.
- **Targeted Commands**:
  - `export PIECETREE_DEBUG=0 && dotnet test ... --filter CursorCollectionTests --nologo` → 33/33 (1.8s)
  - `export PIECETREE_DEBUG=0 && dotnet test ... --filter "FullyQualifiedName~CursorCoreTests.Cursor_" --nologo` → 9/9
  - `export PIECETREE_DEBUG=0 && dotnet test ... --filter CursorCoreTests --nologo` → 34/34 (skips acknowledged)
- **Artifacts**: `agent-team/handoffs/CL7-QA-Result.md`, `tests/TextBuffer.Tests/TestMatrix.md` (CL7 entries added).
- **Changefeed**: Anchored at [`#delta-2025-11-28-aa4-cl7-stage1-qa`](../indexes/README.md#delta-2025-11-28-aa4-cl7-stage1-qa), references [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core).
- **Findings**: No bugs found. All EnableVsCursorParity paths verified. TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges confirmed for selection recovery.

## 2025-12-01 Team Leader 谈话记录 (Session 2)
- **会话类型**: 测试性团队谈话（角色验证）
- **触发方**: Team Leader
- **目的**: 验证 SubAgent 持久认知机制 + 角色理解
- **汇报要点**:
  1. 角色定位：PieceTreeSharp 测试验证专家 / 质量门禁
  2. 核心职责：Parity Verification、Test Maintenance、Baseline Tracking、Regression Detection
  3. 当前基线：683 tests (681 pass, 2 skip) @ `#delta-2025-11-28-aa4-cl7-stage1-qa`
  4. 输出顺序纪律：先工具调用 → 后汇报（避免中间输出丢失）
- **认知文件更新**: 本条目
- **待处理事项**: 无新增验证任务，等待 Porter-CS 交付

## 2025-12-02 Snippet Deterministic Tests (#delta-2025-12-02-snippet-deterministic)
- **任务**: Team Leader 要求补充 Cursor/Snippet 确定性测试套件
- **范围**: 边界情况、adjustWhitespace、Placeholder Grouping
- **新增测试**: 27 个测试（23 pass, 4 skip）
  - Edge Cases: 7 tests (4 pass, 3 skip for P2 features)
  - adjustWhitespace Extended: 6 tests (all pass)
  - Placeholder Grouping Extended: 6 tests (all pass)
  - Complex Scenarios: 3 tests (all pass)
  - Placeholder Inheritance: 3 tests (all skip for P2)
- **跳过的测试**: 4 个测试因 P2 功能尚未实现而跳过
  - `SnippetInsert_NestedPlaceholder_ParsesCorrectly` - 嵌套占位符
  - `SnippetInsert_NestedPlaceholders_ExpandCorrectly` - 嵌套占位符
  - `SnippetInsert_EscapedCharacters` - 转义字符处理
  - `SnippetInsert_PlaceholderInheritance` - 占位符默认值继承
- **基线变更**: 830 tests (821 pass, 9 skip) → 858 tests (849 pass, 9 skip)
- **全量测试**: `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → 858/858 (849 pass, 9 skip, 103.9s)
- **Targeted rerun**: `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter SnippetControllerTests --nologo` → 56/56 (52 pass, 4 skip, 2.1s)
