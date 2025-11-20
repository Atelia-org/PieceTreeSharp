# AA4-006 Fix1 Plan – CRLF Edge Case Repro & Repair

## Overview
Porter-CS: This follow-up plan focuses on the two QA failing tests related to CRLF split across chunk boundaries (`TestSplitCRLF`, `CRLF_RepairAcrossChunks`). The goal is to ensure correctness for CRLF pairs and to eliminate negative substring length and off-by-one line count issues.

## Problem Reproduction
- Minimal reproducible command for both failures:
  - dotnet test --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.AA005Tests.TestSplitCRLF" --no-build
  - Failure reproduces in Insert(0, "A\r") followed by Insert(2, "\nB") and then GetLineRawContent asserts.

## Tasks & Implementation Steps
1. Add diagnostic helpers (in debug-only code) to dump nodes & their piece buffer indices, Start/End bytes, computed offsets, and `LineStart` arrays per buffer; create `PieceTreeModelTestHelpers.DebugDumpModel(model)`.
2. Focus on these functions:
   - `PositionInBuffer(node, remainder)` — ensure returned BufferCursor is within valid buffer offsets and correct for merged pieces.
   - `OffsetInBuffer(bufferIndex, BufferCursor)` — verify return value is non-negative and less or equal to buffer length.
   - `CreateNewPieces` & `AcceptChunk` — ensure chunking logic preserves CRLF pairs and never splits surrogate pair.
   - `FixCRLF(prevNode, nextNode)` — ensure the function performs consistent removal and reinsertion and re-computes `LineStart`/`LineFeed` counts adequately.
3. Cross-validate `LineStart`/`LineFeed` counters between input (builder/factory) and post-edit recompute: add asserts to detect mismatches in dev runs and make them test-friendly only in debug mode, or add unit tests that verify counts after operations.
4. Update `GetLineRawContent`, `GetLineContent`, `GetLineLength` to ensure they handle Start/End offsets robustly even if nodes are being merged or split during operation, and avoid negative substring lengths via safe clamping.
5. Add regression tests for the exact failing scenario and more: repeat the failing scenario but also randomize the chunk boundaries and buffer splits.
6. Run `dotnet test` and ensure those two failing tests become green; add (or update) debug log only on test failure for easier triage.
7. If the tests still fail after attempts at fix, escalate: instrument a debug run that logs piece boundaries and offsets during the operations and include the log in the handoff.

## Deliverables
- Code fixes to `PieceTreeModel.Edit.cs`, `FixCRLF`, `PositionInBuffer`, and `OffsetInBuffer` to eliminate off-by-one and negative length errors.
- New/updated tests ensuring CRLF correctness across chunk boundary splits.
- Updated `agent-team/handoffs/AA4-006-Result.md` with a final status (pass/fail) and a note for QA.

## Memory & Reporting
- Porter to add start/end work logs in `agent-team/members/porter-cs.md`.
- Update the migration log on completion and indicate whether QA step was satisfied.


