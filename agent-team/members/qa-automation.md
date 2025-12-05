# QA-Automation Snapshot (2025-12-05)

## Role & Mission
- Own TextBuffer parity verification per `AGENTS.md`, keeping `tests/TextBuffer.Tests` aligned with TS sources and documenting every rerun inside `tests/TextBuffer.Tests/TestMatrix.md`.
- Publish reproducible changefeed evidence (baseline + targeted filters) so Porter-CS and Investigator-TS can diff regressions without re-reading past worklogs.
- Coordinate Sprint 04 QA intake (`#delta-2025-11-26-sprint04`) by flagging blockers back to Planner and DocMaintainer whenever rerun recipes or artifacts drift.
- 保证所有 Sprint 04 报告引用 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)，且 Cursor/Snippet、DocUI backlog 相关验证都对齐 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) 与 [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)。

## Active Changefeeds & Baselines
| Anchor | Scope | Latest Stats | Evidence |
| --- | --- | --- | --- |
| `#delta-2025-12-05-multicursor-controller` | MultiCursorSelectionController integration tests | 16 new tests, **1167 total (1158p+9s)** | `tests/TextBuffer.Tests/TestMatrix.md` |
| `#delta-2025-12-05-multicursor-session` | MultiCursorSession unit tests (Ctrl+D) | 18 new tests, **1151 total (1142p+9s)** | `tests/TextBuffer.Tests/TestMatrix.md` |
| `#delta-2025-12-02-sprint04-m2` | Sprint 04 M2 全部完成，Snippet/Cursor/IntervalTree 交付 | **873 passed + 9 skip** | `tests/TextBuffer.Tests/TestMatrix.md` |
| `#delta-2025-11-28-aa4-cl7-stage1-qa` | CL7 Stage 1 Cursor Wiring QA | 683/683 (681p+2s) | `agent-team/handoffs/CL7-QA-Result.md` |
| `#delta-2025-11-26-ws1-searchcore` | WS1 PieceTree search offset cache + DEBUG counters. | Full 440/440 green (62.1s); targeted `PieceTreeSearchOffsetCacheTests` 5/5 (1.7s). | `agent-team/handoffs/WS123-QA-Result.md`. |
| `#delta-2025-11-26-ws2-port` | WS2 Range/Selection/Position helper APIs (75 tests). | Full 440/440 green (62.1s); targeted `RangeSelectionHelperTests` 75/75 (1.6s). | `agent-team/handoffs/WS123-QA-Result.md`. |
| `#delta-2025-11-26-ws3-tree` | WS3 IntervalTree rewrite with lazy delta normalization + DEBUG counters. | Full 440/440 green (62.1s); targeted `DecorationTests` 12/12 + `DecorationStickinessTests` 4/4 (1.7s each). | `agent-team/handoffs/WS123-QA-Result.md`. |
| `#delta-2025-11-26-ws4-port-core` | WS4 Cursor Stage 0 infrastructure。 | Targeted `CursorCoreTests` 25/25 (1.8s) + gating full sweeps tracked under R1-R11。 | `agent-team/handoffs/WS4-PORT-Core-Result.md`、`tests/TextBuffer.Tests/TestMatrix.md` Cursor rows。 |
| `#delta-2025-11-25-b3-textmodelsearch` | TextModelSearch 45-case TS parity battery + Sprint 03 Run R37 full sweep. | Targeted `TextModelSearchTests` 45/45 green (2.5s) alongside full `export PIECETREE_DEBUG=0 && dotnet test ... --nologo` 365/365 green (61.6s). | `tests/TextBuffer.Tests/TestMatrix.md` (TextModelSearch row + R37 log) and `agent-team/handoffs/B3-TextModelSearch-QA.md`. |

## Canonical Commands
**Full sweeps**: `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → **1158 passed + 9 skip** (≈110s)

**Key targeted filters**:
- `--filter MultiCursorSelectionControllerTests` → 16/16
- `--filter MultiCursorSessionTests` → 18/18
- `--filter SnippetControllerTests` → 80/80 (76p+4s)
- `--filter CursorCollectionTests` → 33/33
- `--filter CursorCoreTests` → 25/25
- `--filter TextModelSearchTests` → 45/45

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

## 历史活动（2025-11-27 ~ 2025-11-28）
> 详细历史已压缩归档到 `agent-team/archive/qa-automation-log-202511.md`。

**关键里程碑**:
- 2025-11-28: CL7 Stage 1 QA — 641→683 tests, CursorCollectionTests (33) + CursorCoreTests 扩展 (9)
- 2025-11-27: PORT-PT-Search Step12 QA, Alignment Audit Module 07/08 刷新

## 近期活动
| 日期 | 任务 | 结果 |
| --- | --- | --- |
| 2025-12-05 | MultiCursorSelectionController Integration Tests | 16 新测试创建并全部通过，基线 → **1158 passed + 9 skipped** |
| 2025-12-05 | MultiCursorSession Unit Tests | 18 新测试创建并全部通过，基线 → **1142 passed + 9 skipped** |
| 2025-12-02 | Snippet Deterministic Tests | 27 新测试 (23p+4s)，基线 → **873 passed + 9 skipped** |
| 2025-12-01 | Team Leader 谈话 | 角色验证，基线确认 |
