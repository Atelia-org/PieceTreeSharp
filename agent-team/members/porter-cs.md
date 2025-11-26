# Porter-CS Snapshot (2025-11-26)

## Role & Mission
- Own the TS → C# PieceTree/TextModel port, keep `src/TextBuffer` and `tests/TextBuffer.Tests` aligned with upstream semantics, and surface deltas through handoffs plus migration logs.
- Partner with Investigator-TS and QA-Automation so every drop has a documented changefeed + reproducible rerun recipe before Info-Indexer broadcasts the delta.

## Current Focus
- Sprint 04 Workstream alignment ([#delta-2025-11-26-sprint04](../indexes/README.md#delta-2025-11-26-sprint04)): maintain the porting velocity chart, ensure every new drop references TestMatrix rows, and unblock DocMaintainer during the doc compression sweep.
- Cursor/Snippet backlog cleanup ([#delta-2025-11-26-aa4-cl7-cursor-core](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)): finish AA4-007 follow-ups (word boundary helpers, snippet navigation parity) and prep the next Porter slice once Investigator closes CL7 review notes.
- DocUI/Markdown placeholder ([#delta-2025-11-26-aa4-cl8-markdown](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)): keep DocUI Find/Replace + Markdown renderer hooks ready for AA4-008 while referencing this condensed snapshot + DocMaintainer sweep outputs.

## Key Deliverables
- B3 TextModelSearch parity → [B3-TextModelSearch-PORT.md](../handoffs/B3-TextModelSearch-PORT.md), [B3-TextModelSearch-QA.md](../handoffs/B3-TextModelSearch-QA.md), changefeed [#delta-2025-11-25-b3-textmodelsearch](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch).
- Snapshot + deterministic infra → [B3-PieceTree-Snapshot-PORT.md](../handoffs/B3-PieceTree-Snapshot-PORT.md), [B3-TextModel-Snapshot-PORT.md](../handoffs/B3-TextModel-Snapshot-PORT.md), [B3-PieceTree-Deterministic-CRLF-QA.md](../handoffs/B3-PieceTree-Deterministic-CRLF-QA.md); changefeeds `#delta-2025-11-25-b3-piecetree-snapshot`, `#delta-2025-11-25-b3-textmodel-snapshot`, `#delta-2025-11-25-b3-piecetree-deterministic-crlf`.
- Fuzz + search cache hardening → [B3-PieceTree-Fuzz-Harness.md](../handoffs/B3-PieceTree-Fuzz-Harness.md), [B3-PieceTree-Fuzz-Review-PORT.md](../handoffs/B3-PieceTree-Fuzz-Review-PORT.md), [B3-PieceTree-SearchOffset-PORT.md](../handoffs/B3-PieceTree-SearchOffset-PORT.md); changefeeds `#delta-2025-11-23-b3-piecetree-fuzz`, `#delta-2025-11-24-b3-piecetree-fuzz`, `#delta-2025-11-25-b3-search-offset`.
- DocUI Find stack parity → [B3-FC-Result.md](../handoffs/B3-FC-Result.md), [AA4-008-Result.md](../handoffs/AA4-008-Result.md), and the `docs/reports/migration-log.md` rows for `#delta-2025-11-23-b3-fc-core`, `#delta-2025-11-24-find-scope`, `#delta-2025-11-24-b3-docui-staged`.

## Test Baselines
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSearchTests --nologo` → 45/45 (≈2.5s) for the canonical search stack verification.
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeDeterministicTests --nologo` → 50/50 (≈3.5s) guarding CRLF + centralized random scripts.
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → latest full-suite count (365/365 as of R37) before promoting any Sprint 04 changefeed.

## Open Risks / Dependencies
- Intl.Segmenter parity + WordSeparator cache cost remain open under `#delta-2025-11-25-b3-textmodelsearch`; coordinate with Investigator before expanding non-ASCII coverage.
- AA4-006 CRLF repair follow-ups need QA to replay `CRLFFuzzTests` once the new heuristics land; keep `B3-PieceTree-Deterministic-CRLF-QA.md` as the rollback baseline.
- DocUI replace/markdown overlays depend on DocMaintainer delivering compressed references plus renderer samples; blockers tracked on the Sprint 04 task board.

## Archives
- Detailed run-by-run notes, rerun transcripts, and migrated worklogs now live in [agent-team/handoffs/](../handoffs/) and `docs/reports/migration-log.md`; cite those records (not this snapshot) for historical context or audit evidence.

Detailed histories live in `agent-team/handoffs/`.
