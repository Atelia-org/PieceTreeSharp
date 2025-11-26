# QA-Automation Snapshot (2025-11-26)

## Role & Mission
- Own TextBuffer parity verification per `AGENTS.md`, keeping `tests/TextBuffer.Tests` aligned with TS sources and documenting every rerun inside `tests/TextBuffer.Tests/TestMatrix.md`.
- Publish reproducible changefeed evidence (baseline + targeted filters) so Porter-CS and Investigator-TS can diff regressions without re-reading past worklogs.
- Coordinate Sprint 04 QA intake (`#delta-2025-11-26-sprint04`) by flagging blockers back to Planner and DocMaintainer whenever rerun recipes or artifacts drift.

## Active Changefeeds & Baselines
| Anchor | Scope | Latest Stats | Evidence |
| --- | --- | --- | --- |
| `#delta-2025-11-25-b3-textmodelsearch` | TextModelSearch 45-case TS parity battery + Sprint 03 Run R37 full sweep. | Targeted `TextModelSearchTests` 45/45 green (2.5s) alongside full `export PIECETREE_DEBUG=0 && dotnet test ... --nologo` 365/365 green (61.6s). | `tests/TextBuffer.Tests/TestMatrix.md` (TextModelSearch row + R37 log) and `agent-team/handoffs/B3-TextModelSearch-QA.md`. |
| `#delta-2025-11-25-b3-piecetree-deterministic-crlf` | PieceTree deterministic + CRLF normalization harness. | `PIECETREE_DEBUG=0` targeted `--filter PieceTreeDeterministicTests` 50/50 green (3.5s) plus paired full sweep 308/308 green (67.2s). | `tests/TextBuffer.Tests/TestMatrix.md` deterministic rows + `agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-QA.md`. |
| `#delta-2025-11-25-b3-search-offset` | PieceTree search-offset cache wrapper validation. | Targeted `--filter PieceTreeSearchOffsetCacheTests` 5/5 green (4.3s) with the R31 baseline `--nologo` run 324/324 green (58.2s). | `tests/TextBuffer.Tests/TestMatrix.md` R31 + targeted block, `agent-team/handoffs/B3-PieceTree-SearchOffset-QA.md`. |
| `#delta-2025-11-24-b3-docui-staged` | DocUI staged fixes (FindModel + Decorations). | `--filter FullyQualifiedName~FindModelTests` 46/46 green and `--filter FullyQualifiedName~DocUIFindDecorationsTests` 9/9 green (PIECETREE_DEBUG=0). | `tests/TextBuffer.Tests/TestMatrix.md` DocUI rows and `agent-team/handoffs/B3-DocUI-StagedFixes-QA-20251124.md`. |
| `#delta-2025-11-26-sprint04` | Doc maintenance + Sprint 04 kickoff guardrails for QA memory + rerun cadence. | Current snapshot trimmed to active anchors; doc sweep status tracked in `agent-team/handoffs/doc-maintenance-20251126.md`. | `agent-team/indexes/README.md#delta-2025-11-26-sprint04`. |

## Canonical Commands
**Full sweeps**
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → 365/365 (61.6s, R37) for `#delta-2025-11-25-b3-textmodelsearch`.
- Same command (308/308, 67.2s) after PieceTree deterministic CRLF expansion (`#delta-2025-11-25-b3-piecetree-deterministic-crlf`).
- Same command (324/324, 58.2s) for the search-offset cache drop (`#delta-2025-11-25-b3-search-offset`).

**Targeted filters**
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter TextModelSearchTests --nologo` → 45/45, anchors `#delta-2025-11-25-b3-textmodelsearch`.
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter PieceTreeDeterministicTests --nologo` → 50/50, anchors `#delta-2025-11-25-b3-piecetree-deterministic-crlf`.
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter PieceTreeSearchOffsetCacheTests --nologo` → 5/5, anchors `#delta-2025-11-25-b3-search-offset`.
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter FullyQualifiedName~FindModelTests --nologo` → 46/46, anchors `#delta-2025-11-24-b3-docui-staged`.
- `export PIECETREE_DEBUG=0 && dotnet test ... --filter FullyQualifiedName~DocUIFindDecorationsTests --nologo` → 9/9, anchors `#delta-2025-11-24-b3-docui-staged`.

## Open Risks / Dependencies
- Intl.Segmenter + WordSeparator parity (TextModelSearch whole-word gaps) still blocked on Investigator-TS specs; track inside `tests/TextBuffer.Tests/TestMatrix.md` TextModelSearch notes.
- PT-005.S8/S9 need Porter-CS `EnumeratePieces` + Investigator BufferRange/SearchContext plumbing before property/fuzz suites can land.
- Info-Indexer automation must publish the above changefeeds so downstream consumers stop querying stale DocUI aliases; request logged under `agent-team/indexes/README.md#delta-2025-11-26-sprint04`.

## Archives
- Full matrices, run logs, and artifact paths stay in `tests/TextBuffer.Tests/TestMatrix.md`.
- Detailed QA narratives live in `agent-team/handoffs/` (`B3-TextModelSearch-QA.md`, `B3-PieceTree-Deterministic-CRLF-QA.md`, `B3-PieceTree-SearchOffset-QA.md`, `B3-DocUI-StagedFixes-QA-20251124.md`).
- Legacy worklogs and meeting recaps have been moved out per Doc sweep; reference specific changefeeds above if deeper history is required.
