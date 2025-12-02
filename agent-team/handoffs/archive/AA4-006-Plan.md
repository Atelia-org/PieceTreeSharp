# AA4-006 Plan – CL6 ChangeBuffer / CRLF / Large Edits

## Overview
This plan documents the Porter-CS tasks for AA4-006 (CL6): porting and hardening ChangeBuffer, CRLF repair, `_lastChangeBufferPos` optimization, AverageBufferSize heuristics, SearchCache integration, and targeted tests.

Primary objective: ensure TS behavior parity for incremental large edits and CRLF bridging across chunk boundaries. This includes reproducing the performance optimizations in `_lastChangeBufferPos` + reuse of change buffers and ensuring CRLF / surrogate pairs are never split incorrectly.

## Requirements & Acceptance Criteria
- Port `_lastChangeBufferPos` semantics for append-optimization (last change buffer reuse) to `PieceTreeModel`/`PieceTreeModel.Edit.cs`.
- Implement AverageBufferSize heuristics to avoid creating excessively small chunk slices during `Insert` operations.
- Ensure robust CRLF bridging: when edits split `\r\n` or surrogate pairs across nodes, repair logic restores pairs without losing characters; coverage includes mid-node splits & chunk boundary splits.
- Recompute & validate metadata (CR/LF counts, `MightContain*` flags) on incremental edits without full rebuilds when feasible; if not possible, document follow-up tasks.
- Keep or raise existing `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` baseline and add targeted tests for change buffer behavior.

## Implementation Plan
1. Study Investigator output `agent-team/handoffs/AA4-002-Audit.md` (ChangeBuffer/CRLF audit) and the `AA4-006` plan here.
2. Add or update `_lastChangeBufferPos` tracking, including:
   - Add `_lastChangeBufferPos` field in `PieceTreeModel`, update on Insert/Delete operations.
   - Use it in `Insert` to append to last change buffer where possible (reduce allocations & inserts)
3. Implement `AverageBufferSize` heuristics for `CreateNewPieces` so that inserts don't fragment the model.
4. Fix CRLF carryover across chunks and across inserted/deleted nodes by improving `ValidateCRLFWithPrevNode`/`FixCRLF` flows and ensuring `Insert`/`Delete` operations check for `EndWithCR`/`StartWithLF` at node boundaries.
5. Hook `SearchCache` invalidation logic to only invalidate ranges affected by edits; ensure `SearchCache.InvalidateFromOffset` is called in `Insert/Delete` only where required.
6. Add unit tests (xUnit):
   - `PieceTreeModelTests.LastChangeBufferPos_AppendOptimization` testing append to last change buffer.
   - `PieceTreeModelTests.AverageBufferSize_InsertLargePayload` asserting buffer counts after inserting large text & ensuring sizes respect chunking heuristics.
   - `PieceTreeModelTests.CRLF_RepairAcrossChunks` with test cases splitting CRLF across nodes & chunks.
   - Additional fuzz tests: `PieceTreeModelTests.ChangeBufferFuzzTests` to replicate random large edits and validate model invariants.
7. Update `docs/reports/migration-log.md` and `agent-team/handoffs/AA4-006-Result.md` on completion.
8. Update `docs/reports/audit-checklist-aa4.md#cl6` to reflect Porter Complete – Pending QA.

## Test & Validation
- Tests must run: `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` with no failing tests.
- Add new tests to `TestMatrix.md` and ensure total tests are >= 92 (prefer 95+). Prioritize correctness over synthetic perf micro-optimizations.

## Dependencies & Blockers
- Requires `AA4-002-Audit.md` to be reviewed for corner-case examples and test fixtures.
- May require rework of `ChunkUtilities`/`CreateNewPieces` to prevent metadata corruption.

## Memory & Reporting
- Please read `agent-team/members/porter-cs.md` first; update your memory file with an end-of-task worklog before reporting completion. Append `AA4-006-Plan.md` as the canonical plan to the handoff folder.


