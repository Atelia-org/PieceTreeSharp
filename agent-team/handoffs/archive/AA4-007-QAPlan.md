# AA4-007 QA Plan – Cursor word/snippet/multi-select

## Overview
QA steps to validate AA4-007 (CL7) parity:
- Validate multi-cursor support & DocUI rendering
- Validate word navigation & delete/selection behaviors
- Validate snippet insertion & tabstop navigation with multi-cursors

## Steps
1) Read `agent-team/members/qa-automation.md` memory and add a start-of-task entry.
2) Read `agent-team/handoffs/AA4-007-Result.md` for the Porter summary; review UI rendering expectations in `docs/reports/audit-checklist-aa4.md#cl7`.
3) Update `src/PieceTree.TextBuffer.Tests/TestMatrix.md` adding new tests keys and expected results.
4) Execute `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` to capture the baseline; run the new multi-cursor & snippet tests twice to ensure non-flakiness.
5) Add tests if necessary to validate complex multi-cursor overlays in `MarkdownRendererTests` (snapshot testing) including: multi-cursor positions, snippet placeholder display, placeholder editing with snippet tabstops.
6) Run additional fuzz tests focusing on multi-cursor insertion and snippet insertion across multiple cursors.
7) If any tests fail, create `agent-team/handoffs/AA4-007-QA.md` summarizing the failing cases and reproduction, then notify `Porter-CS`.
8) If green, mark test matrix & update migration log with `dotnet test` baseline, and prepare changefeed for `OI-012` or next sequence.

## Deliverables
- `agent-team/handoffs/AA4-007-QA.md` with results
- `src/PieceTree.TextBuffer.Tests/TestMatrix.md` updated for new tests and baseline
- `docs/reports/migration-log.md` updated and ready for Info-Indexer ingestion

