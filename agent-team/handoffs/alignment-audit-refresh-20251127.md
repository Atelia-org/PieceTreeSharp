# Alignment Audit Refresh – 2025-11-27

## Scope
- Re-run the alignment audit (00–08) after the Sprint 04 Phase 8 drop that landed WS1/WS2/WS3/WS4/WS5 deliverables (`docs/reports/migration-log.md#sprint04-r1-r11`).
- Keep all edits cross-referenced with the latest Info-Indexer changefeed entries, especially `agent-team/indexes/README.md#delta-2025-11-26-alignment-audit` and `#delta-2025-11-26-sprint04-r1-r11`.
- Maintain visibility for the open AA4 CL7/CL8 placeholders (`agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core` … `#delta-2025-11-26-aa4-cl8-markdown`) so downstream doc changes don’t prematurely mark gaps as closed.

## Canonical references to pull into every edit
1. `docs/reports/migration-log.md` rows for WS1-PORT-SearchCore / WS1-PORT-CRLF, WS2-PORT, WS3-PORT-Tree, WS4-PORT-Core, WS5-INV, WS5-QA.
2. `agent-team/indexes/README.md#delta-2025-11-26-alignment-audit` for the prior audit baseline and `#delta-2025-11-26-sprint04-r1-r11` for the Phase 8 summary.
3. AA4 placeholders: `#delta-2025-11-26-aa4-cl7-cursor-core`, `#delta-2025-11-26-aa4-cl7-wordops`, `#delta-2025-11-26-aa4-cl7-column-nav`, `#delta-2025-11-26-aa4-cl7-snippet`, `#delta-2025-11-26-aa4-cl7-commands-tests`, plus `#delta-2025-11-26-aa4-cl8-markdown`, `#delta-2025-11-26-aa4-cl8-capture`, `#delta-2025-11-26-aa4-cl8-intl`, `#delta-2025-11-26-aa4-cl8-wordcache`.

---

## File-by-file refresh plan

### docs/reports/alignment-audit/00-summary.md
- **Owner:** DocMaintainer
- **Anchors to cite:** `agent-team/indexes/README.md#delta-2025-11-26-alignment-audit`, `docs/reports/migration-log.md#sprint04-r1-r11`
- **Dependencies:** Needs updated module verdicts from 01–08 before finalizing the totals.
- **Checklist:**
  1. Recompute the table counts (Fully Aligned /偏差/需要修正) once Modules 01–08 incorporate WS1–WS5 deltas; leave a footnote pointing at the Phase 8 baseline (585/585, 1 skip).
  2. Update the P0/P1 lists so `PieceTreeModel.Edit/Search` now reference WS1-PORT-SearchCore + WS1-PORT-CRLF fixes and clarify what remains outstanding (e.g., NodeAt2 tuple reuse still pending CRLF bridge validation).
  3. Refresh the “测试覆盖” section with the new harnesses from WS5-QA (PieceTreeBufferApiTests, PieceTreeSearchRegressionTests, TextModelIndentationTests) and note that baseline moved from 365→585 per `#delta-2025-11-26-sprint04-r1-r11`.
  4. Extend the “Verification Notes” block to mention the 2025-11-27 spot-check inputs (CRLFFuzzTests 16/16, CursorCoreTests 25/25) and keep AA4 CL7/CL8 placeholders flagged as unresolved references rather than Done items.

### docs/reports/alignment-audit/01-core-fundamentals.md
- **Owner:** Investigator-TS
- **Anchors to cite:** `docs/reports/migration-log.md#ws1-port-searchcore`, `docs/reports/migration-log.md#ws1-port-crlf`, `agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`
- **Dependencies:** Info-Indexer has not yet published a standalone changefeed for `WS1-PORT-CRLF`; call this out so DocMaintainer keeps the gap in “Pending verification” until that anchor exists.
- **Checklist:**
  1. Update the `PieceTreeModel.Edit` finding to acknowledge the new hitCRLF + `_` placeholder bridge (11 CRLFFuzz tests) and describe what validation remains (e.g., ensuring `_lastChangeBufferPos` telemetry is referenced via `WS1-PORT-CRLF-Result.md`).
  2. Reword the `PieceTreeModel.Search` gap so it references the O(1) LineStarts path + DEBUG counters delivered in `WS1-PORT-SearchCore`, while keeping the deferred NodeAt2 tuple reuse explicitly tied to `PORT-PT-Search-Plan.md`.
  3. Add a bullet under “需要立即修正” that explains the new dependency between SearchCache diagnostics and CRLF bridge; downstream QA should re-run `PieceTreeSearchRegressionTests` when editing this doc.
  4. Expand the Verification Notes with the 2025-11-26 test evidence: `dotnet test --filter CRLFFuzzTests` (16/16) plus the full suite 451/451.

### docs/reports/alignment-audit/02-core-support.md
- **Owner:** Investigator-TS
- **Anchors to cite:** `docs/reports/migration-log.md#ws2-port`, `agent-team/indexes/README.md#delta-2025-11-26-ws2-port`
- **Dependencies:** None blocking, but call out that WS2 changefeed already marked Y so edits can assert completion.
- **Checklist:**
  1. Replace the “Range / Selection helpers 缺失” P0 with a post-WS2 status: list the helpers now available (`ContainsPosition`, `IntersectRanges`, `Selection.FromRange`, `TextPosition.With`/`Delta`) and move remaining gaps (e.g., `RangeMapping` tie-ins) to P1.
  2. Document the 75 new `RangeSelectionHelperTests` and how they change the coverage percentages in the module table.
  3. Cross-reference `WordCharacterClassifierCache` optimizations from `FR-01/FR-02` plus WS5-INV backlog items that still require Intl-aware classifiers, ensuring the doc points at the AA4 CL8 placeholders for locale dependencies.
  4. Refresh Verification Notes to cite the `dotnet test --filter RangeSelectionHelperTests` (75/75) evidence from WS2-PORT.

### docs/reports/alignment-audit/03-cursor.md
- **Owner:** Investigator-TS (with Porter-CS for follow-up code reviews)
- **Anchors to cite:** `docs/reports/migration-log.md#ws4-port-core`, `agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core`, `agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`
- **Dependencies:** Future CL7 deltas (`#delta-2025-11-26-aa4-cl7-wordops`, `-column-nav`, `-snippet`, `-commands-tests`) must stay flagged as blockers.
- **Checklist:**
  1. Update the module summary so Stage 0 deliverables (`CursorConfiguration`, `SingleCursorState`, `CursorContext`, tracked ranges, 25 `CursorCoreTests`) are called out as DONE per WS4-PORT-Core, while the remaining CL7 work is explicitly tied to the placeholders.
  2. Split the P0 table into “Delivered” vs “Outstanding” to avoid double-counting Stage 0 gaps; cite `WS4-PORT-Core-Result.md` for the delivered portion.
  3. Emphasize that Column selection + snippet/session parity still depend on pending deltas, and link each open bullet to its placeholder anchor so Task Board/AGENTS stay aligned.
  4. Extend Verification Notes with the targeted test command `dotnet test --filter CursorCoreTests --nologo` (25/25) and mention the known `IntervalTreePerfTests` exclusions to avoid false alarms.

### docs/reports/alignment-audit/04-decorations.md
- **Owner:** Porter-CS (DocMaintainer to assist with DocUI narrative)
- **Anchors to cite:** `docs/reports/migration-log.md#ws3-port-tree`, `agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown`, `agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`
- **Dependencies:** Waiting on the four CL8 sub-deltas (markdown, capture, intl, wordcache) before reclassifying DocUI gaps.
- **Checklist:**
  1. Rewrite the `IntervalTree` analysis to describe the lazy normalize + NodeFlags rewrite from WS3-PORT-Tree, including metrics (1470 LOC rewrite, DEBUG counters) and the `WS3-QA` verification runs (IntervalTreeTests + Perf tests).
  2. Clarify which decoration-owner issues remain (DocUI renderer still not consuming search decorations) and tie them to `#delta-2025-11-26-aa4-cl8-*` placeholders instead of calling them generic gaps.
  3. Update the action list so the immediate next step is verifying DocUI renderer wiring once the markdown/capture deltas land, citing `FindDecorations` test expansions from B3.
  4. Refresh Verification Notes with the latest targeted commands (`dotnet test --filter DecorationStickinessTests`, `--filter DocUIFindDecorationsTests`) so QA knows which evidence backs the statements.

### docs/reports/alignment-audit/05-diff.md
- **Owner:** Investigator-TS
- **Anchors to cite:** `agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`, `docs/reports/migration-log.md#ws5-inv`
- **Dependencies:** Pending WS5 test backlog execution for DiffComputer-specific harnesses.
- **Checklist:**
  1. Confirm there were no Phase 8 functional changes under `src/TextBuffer/Diff`, but update the narrative to mention DiffComputer now sits in the WS5 Top-10 backlog (see WS5-INV) so the “Needs Fix” items stay prioritized.
  2. Ensure the test coverage table references the 585-test baseline and calls out that Diff suites remain unchanged since `#delta-2025-11-23`.
  3. Add a blocker note that Diff-specific deterministic/DocUI renderer tests will arrive via the WS5 harness plan; cite `WS5-INV-TestBacklog.md` so downstream owners know where specs live.
  4. Refresh Verification Notes to explain which regressions were (or were not) re-run post Phase 8 (e.g., `dotnet test --filter DiffTests`).

### docs/reports/alignment-audit/06-services.md
- **Owner:** Info-Indexer (given the cross-cutting service references)
- **Anchors to cite:** `docs/reports/migration-log.md#ws2-port` (TextPosition helpers), `docs/reports/migration-log.md#ws5-qa` (TextModelIndentationTests), `agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`
- **Dependencies:** None, but coordinate with DocMaintainer when updating service diagrams to avoid drift from AGENTS.md.
- **Checklist:**
  1. Capture how the new Range/Selection/TextPosition helpers alter the `TextModelOptions` and undo stack sections—call out any service APIs that now share the helpers (e.g., `IUndoRedoService`, `DocUIFindController`).
  2. Reference the new TextModelIndentation tests from WS5-QA when discussing `guessIndentation`/resolved options, even though the GuessIndentation API gap remains (1 skipped test)—document this as a tracked deficit.
  3. Align the Services module’s risk list with the AA4 CL8 placeholders for Intl word segmentation so Search/Find services are consistent with the Cursor + Decorations docs.
  4. Append Verification Notes with the targeted commands used in WS5-QA (TextModelIndentationTests, PieceTreeBufferApiTests) to show coverage evidence.

### docs/reports/alignment-audit/07-core-tests.md
  1. Rebuild the coverage tables so they enumerate the new deterministic suites (PieceTreeBufferApiTests, SearchRegressionTests, TextModelIndentationTests) and explain which TS batteries still remain outstanding per WS5-INV.
  2. Ensure the “baseline” row now cites 585/585 (1 skip) and includes the targeted rerun commands from WS5-QA.
  3. Flag any suites that still lack TS parity (e.g., CursorWordOperations) and tie them to the AA4 CL7 placeholders so the audit stays consistent with Module 03.
  4. Update Verification Notes with the QA evidence URLs/commands from `agent-team/handoffs/WS5-QA-Result.md`.
### docs/reports/alignment-audit/08-feature-tests.md
- **Owner:** QA-Automation (coordinate with Investigator-TS for DocUI vs Cursor splits)
- **Anchors to cite:** `agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core`, `agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown`, `docs/reports/migration-log.md#ws5-inv`
- **Dependencies:** Waiting on AA4 CL7/CL8 drops for cursor/snippet/DocUI parity; keep placeholders visible.
- **Checklist:**
  1. Update the DocUI Find/Controller coverage narrative to mention that Phase 8 did **not** add new feature tests but WS5-INV lists DocUI diff renderer + snippet parity as top priorities—tie each outstanding suite to the corresponding placeholder anchor.
  2. Reconcile the cursor/snippet test counts with Module 03 so the Feature Tests doc no longer reports stale coverage percentages; cite `CursorCoreTests` addition but keep multi-cursor/snippet fuzzing backlog open.
  3. Highlight that Markdown renderer/search decoration tests remain blocked by the CL8 markdown/capture/intl/wordcache placeholders, and list the proofs required once those deltas ship.
  4. In Verification Notes, log which existing feature suites were rerun post-refactor (e.g., `DocUIFindControllerTests`, `SnippetMultiCursorFuzzTests`) and identify any suites intentionally skipped pending new code.

---

## Blocking dependency log
1. **Info-Indexer delta for WS1-PORT-CRLF:** migration log row exists but changefeed entry is still pending; note this before marking the CRLF gap fixed in Module 01.
2. **AA4 CL7 placeholders:** no `#delta-2025-11-26-aa4-cl7-*` entries have been fulfilled yet—Modules 03 & 08 must keep these tagged as blockers for word ops, column select, snippet, and test coverage.
3. **AA4 CL8 placeholders:** markdown/capture/intl/wordcache deltas remain placeholders; Modules 04, 06, 08 should keep their DocUI/Markdown findings in “Gap” until those anchors have concrete changefeed rows.

## Progress
- Module 00 – updated counts/P0/P1 per [`docs/reports/migration-log.md#sprint04-r1-r11`](../../docs/reports/migration-log.md#sprint04-r1-r11) + [`agent-team/indexes/README.md#delta-2025-11-26-alignment-audit`](../indexes/README.md#delta-2025-11-26-alignment-audit); citations include CL7/CL8 placeholders (`#delta-2025-11-26-aa4-cl7-*`, `#delta-2025-11-26-aa4-cl8-*`) and `WS5-QA` baseline (`#delta-2025-11-26-ws5-qa`).
- Module 01 – `docs/reports/alignment-audit/01-core-fundamentals.md` 现已引用 `../reports/migration-log.md#ws1-port-searchcore` 与 `#ws1-port-crlf`、[`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)，并把 NodeAt2/SearchCache 剩余项串回 [`agent-team/handoffs/PORT-PT-Search-Plan.md`](../handoffs/PORT-PT-Search-Plan.md)、[`WS1-PORT-SearchCore-Result.md`](../handoffs/WS1-PORT-SearchCore-Result.md)、[`WS1-PORT-CRLF-Result.md`](../handoffs/WS1-PORT-CRLF-Result.md)；等待 Info-Indexer 发布 CRLF changefeed 后再降低风险级别。
- Module 02 – `docs/reports/alignment-audit/02-core-support.md` 记录了 WS2-PORT 引入的 Range/Selection/TextPosition helper（`docs/reports/migration-log.md#ws2-port`, [`agent-team/indexes/README.md#delta-2025-11-26-ws2-port`](../indexes/README.md#delta-2025-11-26-ws2-port)）与 FR-01/FR-02 的 `WordCharacterClassifierCache` LRU (`docs/reports/migration-log.md#fr-01-02`, [`agent-team/indexes/README.md#delta-2025-11-23`](../indexes/README.md#delta-2025-11-23))，同时把 Intl/word cache backlog 指向 `#delta-2025-11-26-aa4-cl8-markdown`。Verification Notes 引用 `dotnet test --filter RangeSelectionHelperTests --nologo` 复测结果（109/109 data rows，TestMatrix 仍标注原先 75/75）。
- Module 03 – `docs/reports/alignment-audit/03-cursor.md` 现将 Stage 0 (`CursorConfiguration`/`CursorState`/`CursorContext` + `CursorCoreTests` 25/25) 与 Stage 1 backlog 分离，引用 [`docs/reports/migration-log.md#ws4-port-core`](../../docs/reports/migration-log.md#ws4-port-core) 与 [`agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)，并把 CL7 placeholders（`#delta-2025-11-26-aa4-cl7-cursor-core`、`-wordops`、`-column-nav`、`-snippet`、`-commands-tests`）写入概要/P0 表；Verification Notes 记录 `dotnet test --filter CursorCoreTests --nologo` 结果（39 通过 / 0 失败 / 2 skip，25/25 Stage 0 case 仍绿）。
- Module 04 – `docs/reports/alignment-audit/04-decorations.md` 现聚焦 [`WS3-PORT-Tree`](../../docs/reports/migration-log.md#ws3-port-tree)（引用 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) + [`WS3-PORT-Tree-Result.md`](../handoffs/WS3-PORT-Tree-Result.md) 数据点）、`B3-Decor-Stickiness-Review.md` stickiness 验证，以及 CL8 DocUI placeholders（`#delta-2025-11-26-aa4-cl8-markdown`/`-capture`/`-intl`/`-wordcache`）以标记剩余 renderer/owner gaps；Verification Notes 明确 `dotnet test --filter IntervalTreeTests --nologo` 与 `--filter DocUIFindDecorationsTests --nologo` 作为 WS3-QA 及 DocUI rerun 证据，并记录 Legacy 2025-11-26 附录已清理避免重复引用。
- Module 05 – `docs/reports/alignment-audit/05-diff.md` 重申 Phase 8（[`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)）未改动 `src/TextBuffer/Diff`, 把 Diff backlog 锚定到 [`../reports/migration-log.md#ws5-inv`](../../docs/reports/migration-log.md#ws5-inv)/[`WS5-INV-TestBacklog.md`](../handoffs/WS5-INV-TestBacklog.md) Top-10，并引用 [`WS5-PORT-Harness-Result.md`](../handoffs/WS5-PORT-Harness-Result.md) 说明 DiffPerfHarness 仍待填充；文档同时注明 [`#delta-2025-11-23`](../indexes/README.md#delta-2025-11-23) 之后 Info-Indexer 无新增 diff changefeed，Verification Notes 记录 2025-11-27 `export PIECETREE_DEBUG=0 && dotnet test ... --filter DiffTests --nologo` (4/4, 1.9s) 作为最新证据。
- Module 06 – `docs/reports/alignment-audit/06-services.md` 已刷新服务层表格/风险，显式引用 [`../reports/migration-log.md#ws2-port`](../../docs/reports/migration-log.md#ws2-port)、[`../reports/migration-log.md#ws5-qa`](../../docs/reports/migration-log.md#ws5-qa) 与 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)，新增“服务层波及”小节覆盖 TextModelOptions/Undo 栈/DocUI find ripple，并在摘要/DocUI 段保留 AA4 CL8 占位链接（`#delta-2025-11-26-aa4-cl8-markdown`/`-capture`/`-intl`/`-wordcache`）。Verification Notes 追加 WS5-QA targeted 命令（`export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "PieceTreeBufferApiTests\|PieceTreeSearchRegressionTests\|TextModelIndentationTests" --nologo`）与 Sprint04 基线 rerun。 
- Module 06 (2025-11-27 follow-up) – `docs/reports/alignment-audit/06-services.md` 现明确描述 WS2 helper 如何穿透 `TextModelOptions.Diff/WithUpdate`、`IUndoRedoService` 与 DocUI Find 服务，并把 [WS5-QA](../../docs/reports/migration-log.md#ws5-qa) targeted rerun（TextModelIndentation/PieceTreeBufferApi/SearchRegression）和 [Sprint04 R1-R11](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 585/585 基线一起写入 Verification Notes，同时保持 `#delta-2025-11-26-aa4-cl8-markdown`/`-capture`/`-intl`/`-wordcache` Intl/word cache 缺口可见。 
- Module 07 – `../../docs/reports/alignment-audit/07-core-tests.md` 已对齐 `WS5-QA` harness（`PieceTreeBufferApiTests`/`PieceTreeSearchRegressionTests`/`TextModelIndentationTests`）并在覆盖表+Verification Notes 中引用 [`../../docs/reports/migration-log.md#ws5-qa`](../../docs/reports/migration-log.md#ws5-qa)、[`../handoffs/WS5-QA-Result.md`](WS5-QA-Result.md)、[`../../tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md) 与 [`../indexes/README.md#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)；未完成的 CursorWordOperations/snippet fuzz/diff deterministic 套件保持 `#delta-2025-11-26-aa4-cl7-wordops`/`-snippet`/`-commands-tests` 占位并提供 targeted rerun 命令。
- Module 08 – `../../docs/reports/alignment-audit/08-feature-tests.md` 现记录 Phase 8 未新增 DocUI feature suite，DocUI diff renderer + snippet parity backlog 直接引用 [`../../docs/reports/migration-log.md#ws5-inv`](../../docs/reports/migration-log.md#ws5-inv) 与 [`../handoffs/WS5-INV-TestBacklog.md`](../handoffs/WS5-INV-TestBacklog.md)，并复用 Module 03 Stage 0 `CursorCoreTests` 计数（`#delta-2025-11-26-aa4-cl7-cursor-core`）。文档将未完成工作映射到 `#delta-2025-11-26-aa4-cl7-wordops`/`-column-nav`/`-snippet`/`-commands-tests` 以及 `#delta-2025-11-26-aa4-cl8-markdown`/`-capture`/`-intl`/`-wordcache`，Verification Notes 增补 Sprint 04 585/585 基线 (`#delta-2025-11-26-sprint04-r1-r11`) 与针对性命令（DocUIFindControllerTests、SnippetMultiCursorFuzzTests、CursorCoreTests、Decoration/Diff suites）。
