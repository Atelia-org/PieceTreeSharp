# Handoffs Archive

This directory contains **archived handoff files** from completed phases and sprints of the PieceTree migration project.

## Archive Date
- **2025-11-26** – Initial archival performed by AI Team Leader

## Contents by Phase

### Phase 2 – TextModel & Interaction (TM-*)
- `TM-001-Brief.md` – TextModel dependency analysis
- `TM-002-Result.md` – TextModel skeleton porting result
- `TM-005-Result.md` – Basic Cursor support result
- `Phase2-Plan.md` – Phase 2 planning document

### Phase 3 – Diffing & Decorations (DF-*)
- `DF-001-Brief.md` – Diff/Decoration dependency analysis
- `DF-002-Result.md` – DiffComputer Myers Algorithm result
- `DF-003-Result.md` – ModelDecoration/IntervalTree result
- `DF-004-Result.md` – TextModel.GetDecorationsInRange result
- `DF-005-Result.md` – MarkdownRenderer prototype result
- `Phase3-Plan.md` – Phase 3 planning document

### Phase 4 – Alignment & Audit R1 (AA-*)
- `AA-001-Audit.md` – Core PieceTree audit
- `AA-002-Audit.md` – TextModel & Buffers audit
- `AA-003-Audit.md` – Search & Interaction audit
- `AA-004-Audit.md` – Diff & Decorations audit
- `AA-005-Result.md` – Core & Model remediation
- `AA-006-Result.md` – Search & Features remediation
- `Phase4-Plan.md` – Phase 4 planning document

### Phase 5 – Alignment & Audit R2 (AA2-*)
- `AA2-001-Audit.md` – PieceTree advanced cases audit
- `AA2-002-Audit.md` – TextModel undo/redo/language audit
- `AA2-003-Audit.md` – Search advanced features audit
- `AA2-004-Audit.md` – Diff prettify/decoration stickiness audit
- `Phase5-Plan.md` – Phase 5 planning document

### Phase 6 – Alignment & Audit R3 (AA3-*)
- `AA3-001-Audit.md` – CL1 Builder/Factory parity audit
- `AA3-002-Audit.md` – CL2 ECMAScript regex audit
- `AA3-003-Result.md` – CL1 Porter remediation
- `AA3-004-Result.md` – CL2 Porter remediation
- `AA3-005-Audit.md` – CL3 Diff LinesDiff audit
- `AA3-006-Result.md` – CL3 Porter remediation
- `AA3-007-Audit.md` – CL4 Decorations/DocUI audit
- `AA3-008-Result.md` – CL4 Porter remediation
- `AA3-009-QA.md` – Phase 6 QA validation

### Sprint 02 – PieceTree Core (PT-*)
- `PT-010-Brief.md` – CRLF Normalization analysis
- `PT-010-Result.md` – CRLF Normalization implementation
- `PT-011-Result.md` – V1 阶段性验收 QA

### Sprint 02 – OI Indexer Tasks
- `OI-011-Plan.md` – AA4 changefeed planning
- `OI-011-Result.md` – AA4 changefeed delivery
- `OI-REFRESH-TaskBrief.md` – OI backlog refresh brief
- `OI-REFRESH-Result.md` – OI backlog refresh result

### Sprint 03 Batch #1 – ReplacePattern (B1-*)
- `B1-PORTER-TaskBrief.md`, `B1-PORTER-Result.md` – Porter work
- `B1-QA-TaskBrief.md`, `B1-QA-Result.md` – QA validation
- `B1-INFO-TaskBrief.md`, `B1-INFO-Result.md` – Info-Indexer changefeed
- `B1-DOC-TaskBrief.md`, `B1-DOC-Result.md` – DocMaintainer sync

### Sprint 03 Batch #2 – FindModel Core (B2-*)
- `B2-INV-TaskBrief.md`, `B2-INV-Result.md` – Investigation
- `B2-PLAN-TaskBrief.md`, `B2-PLAN-Result.md` – Planning
- `B2-001-*` through `B2-003-*` – Porter/QA subtasks
- `B2-QA-Result.md`, `B2-QA-Result-OLD.md` – QA validation
- `B2-Final-QA.md` – Final QA summary
- `BATCH2_SUMMARY.md` – Batch summary
- Bug fixes: `B2-BUG-001-*`, `B2-Porter-*`, `B2-Info-Update.md`, `B2-TS-Review.md`

### Sprint 03 Batch #3 – DocUI Find & Decorations (B3-*)
- **FindModel:** `B3-FM-Result.md`, `B3-FSel-Result.md`, `B3-INV-Result.md`
- **FindController:** `B3-FC-Result.md`, `B3-FC-Review.md`
- **Decorations:** `B3-Decor-INV.md`, `B3-Decor-PORTER.md`, `B3-Decor-Stickiness-Review.md`
- **MultiSelection:** `B3-FM-MultiSelection-*.md` (4 files)
- **DocUI Staged:** `B3-DocUI-StagedFixes-20251124.md`, `B3-DocUI-StagedReview-20251124.md`, `B3-DocUI-StagedFixes-QA-20251124.md`
- **PieceTree Fuzz:** `B3-PieceTree-Fuzz-*.md` (7 files)
- **PieceTree Deterministic:** `B3-PieceTree-Deterministic-*.md` (3 files)
- **SearchOffset Cache:** `B3-PieceTree-SearchOffset-*.md` (4 files)
- **Snapshot:** `B3-PieceTree-Snapshot-*.md`, `B3-Snapshot-*.md`, `B3-TextModel-Snapshot-*.md`
- **TestFailures:** `B3-TestFailures-*.md` (4 files)
- **TextModelSearch:** `B3-TextModelSearch-*.md` (3 files)

### Miscellaneous
- `QA-002-FindModelIssues.md` – QA issue tracking
- `doc-review-20251124.md`, `doc-review-20251125.md` – Documentation reviews
- `Review-20251125-Investigator.md` – Investigator review

## Notes
- Files are archived when their corresponding tasks are **Done** and no longer actively referenced by `agent-team/task-board.md`.
- To access archived content, navigate to this directory or use relative links like `archive/AA-001-Audit.md`.
- Migration log references in `docs/reports/migration-log.md` may still point to original paths; consider updating to `archive/` prefix if needed.
