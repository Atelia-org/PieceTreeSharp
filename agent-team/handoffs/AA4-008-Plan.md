# AA4-008 Plan – DocUI Find/Replace & Decorations

## Context
- Sprint 02 (`docs/sprints/sprint-02.md`) + Task Board Phase 7 (`agent-team/task-board.md`) still list CL8 as the remaining AA4 gap.
- Investigator handoff (`agent-team/handoffs/AA4-004-Audit.md`) already enumerates F1–F4 issues; this plan translates them into execution steps for Porter, QA, and Info-Indexer.
- Changefeed baseline: `agent-team/indexes/README.md#delta-2025-11-21` (covers AA4-005/006/007.BF1). Next delta must capture AA4-008 plus DocUI snapshots so AGENTS/Sprint/Task Board stay aligned.

## Investigator Addendum (2025-11-21)
- Re-confirmed TS vs C# gap summary for CL8 (FindDecorations metadata, persistent Find model, ReplacePattern/capture payload, MarkdownRenderer owner path).
- Produced remediation checklist mapping each finding to C# files/tests:
  - F1 Overlay metadata & degrade path → `TextModel.cs`, `Decorations/ModelDecorationOptions.cs`, `Decorations/DecorationOwnerIds.cs`, `MarkdownRenderer.cs`; tests: `TextModelSearchTests`, `TextModelTests`, `MarkdownRendererTests`.
  - F2 Find controller & scopes → new `FindController` class + `SearchHighlightOptions`, `TextModelSearch.cs`, `TextModel.cs`; tests: new `FindControllerTests`, extended `TextModelSearchTests`, `MarkdownRendererTests` scope cases.
  - F3 ReplacePattern & capture payload → new parser, updates to `TextModelSearch.cs`, `TextModel.cs`, decoration metadata; tests: `ReplacePatternTests`, capture round-trip assertions, Markdown renderer preview snapshots.
  - F4 Markdown renderer owner path → stop recomputing search, honor `DecorationOwnerIds.SearchHighlights`, render preview/minimap metadata straight from decorations.
- Highlighted DocMaintainer & Info-Indexer hooks: once AA4-008 lands, update `docs/reports/audit-checklist-aa4.md#cl8`, Task Board, Sprint, AGENTS, and publish a new changefeed delta referencing DocUI snapshots and QA baselines.

## Porter Execution Plan
1. **Sequence:** (1) Overlay options + degrade heuristics, (2) Find controller + scope plumbing, (3) ReplacePattern + capture payload, (4) MarkdownRenderer owner consumption.
2. **Code/Test Touchpoints:**
   - Overlay metadata: extend model/decoration types with `_FIND_MATCH_*` parity, degrade after 1000 matches, expose `ActiveMatchId`. Tests via `TextModelSearchTests.FindMatches_DegradesAfter1000`, `TextModelTests.FindDecorationsExposeOwnerMetadata`, `MarkdownRendererTests.RenderFindOverlaysHonorsOwnerFilters`.
   - Controller/scopes: add `FindController` infrastructure, extend `SearchHighlightOptions`, wire into `TextModel`. Tests: `FindControllerTests`, `TextModelSearchTests.FindMatches_RespectsControllerScope`, renderer overlay scope snapshot.
   - ReplacePattern/capture: port parser, propagate metadata through decorations, surface preview strings. Tests: `ReplacePatternTests`, `TextModelSearchTests.CaptureMatchesRoundTrip`, `MarkdownRendererTests.ReplacePreviewRendersCaptureGroups`.
   - Renderer path: consume owner-filtered decorations, emit degrade banner + preview markers without re-running search; tests ensure owner filters + banner + perf guard.
3. **Risk & Rollback:** add `SearchOverlayV1` feature flag, debug asserts for controller sync, capture metadata pooling, ability to fall back to current renderer path if gating fails. Run CRLF fuzz + `AssertPieceIntegrity` before/after major drops.
4. **Documentation:** prepare `agent-team/handoffs/AA4-008-Result.md`, append migration-log row + changefeed delta, update AGENTS/Sprint/Task Board/TestMatrix once QA signs off; store DocUI snapshots under `docs/reports/docui/AA4-008-find-replace.md`.

## QA Strategy
- **New Suites:**
  - `DocUIOverlayMetadataTests.cs` / `DecorationTests.OverlayMetadataSurvivesChunkSplit_F1` (degrade metadata).
  - `DocUIFindScopeControllerTests.cs` (controller/scopes).
  - `DocUIReplaceCaptureTests.cs` + `DocUIReplaceCaptureFuzzTests.cs` (capture payloads, preserve-case, regex backrefs).
  - `MarkdownRendererDocUIOwnerRouting_F4` / `MarkdownRendererDocUIOverlayMarkdownParity_F4` within `MarkdownRendererTests.cs`.
- **Commands:**
  - Full baseline: `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --nologo` (run twice).
  - Targeted overlays: `dotnet test ... --filter "FullyQualifiedName~DocUIOverlay"`.
  - Controller focus: `dotnet test ... --filter "FullyQualifiedName~DocUIFindScope" --results-directory TestResults/docui-find`.
  - Capture fuzz: `PIECETREE_DEBUG=0 PIECETREE_FUZZ_LOG_DIR=/tmp/docui-fuzz dotnet test ... --filter FullyQualifiedName~DocUIReplaceCaptureFuzzTests --settings tests/RunSettings/Fuzz.runsettings`.
  - Renderer sweep: `dotnet test ... --filter FullyQualifiedName~MarkdownRendererDocUI`.
- **Instrumentation:** snapshot outputs to `src/PieceTree.TextBuffer.Tests/__snapshots__/docui/overlay_*.json`, log fuzz seeds under `/tmp/docui-fuzz/`, capture controller traces via `DOCUI_DEBUG=overlay,controller`, archive doc samples in `resources/docui/` and reference them in `agent-team/handoffs/AA4-008-QA.md`.
- **Exit Criteria:** all suites green twice with instrumentation enabled; >500 fuzz seeds without failures; TestMatrix & QA handoff updated with commands/fixtures; DocUI snapshots reviewed and matched against renderer output; audit checklist + Sprint references point to the same changefeed delta before sign-off.

## Info-Indexer / Broadcast Plan
- Inputs for next delta (`#delta-2025-11-22` placeholder): migration-log rows for AA4-007/AA4-007.BF1/AA4-008, `AA4-008-Result.md`, QA report, DocUI snapshot path, QA commands.
- Update steps: draft changefeed entry with Porter/QA evidence, set migration-log Changefeed flag to Y, synchronize AGENTS/Sprint/Task Board/core-docs-index to new delta anchor, and document work in `agent-team/handoffs/OI-011-Result.md`.
- Verification: grep for the new delta anchor across AGENTS/Sprint/Task Board, ensure migration-log/QA TestMatrix reference identical hashes, confirm DocUI snapshot file is linked from changefeed + result doc, and record the latest `dotnet test` baseline (≥119/119) alongside fuzz logs before closing OI-011.

## Next Steps
1. Porter to start overlay metadata implementation (F1) and prep feature flag scaffolding.
2. QA to scaffold new DocUI test files + snapshot harness directories, wiring commands into `TestMatrix.md`.
3. Info-Indexer to stage OI-011 delta template referencing placeholder `#delta-2025-11-22`, ready to populate once AA4-008 merges.
4. Upon code drop + QA pass, DocMaintainer/Info-Indexer must jointly update AGENTS, Sprint 02, Task Board, migration log, and changefeed in the same commit referencing the new delta anchor.


