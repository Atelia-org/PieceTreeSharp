# B3 DocUI Staged Fixes – 2025-11-24

## Summary
- Addressed Investigator-TS critical items from `B3-DocUI-StagedReview-20251124.md`.
- `FindDecorations.Reset()` now mirrors TS `findDecorations.ts` by only clearing owner decorations, keeping `_startPosition` intact so `FindModel` navigation resumes from the last caret even after `IsFlush` edits.
- `IntervalTree.CollectOverlaps()` handles zero-length query ranges as inclusive point lookups, allowing caret selections to intersect decorations just like TS interval tree behavior.
- Added DocUI regression tests to lock both scenarios: `Test48_FlushEditKeepsFindNextProgress` (FindModel) and `CollapsedCaretAtMatchStartReturnsIndex` (FindDecorations).

## Code Touchpoints
1. `src/TextBuffer/DocUI/FindDecorations.cs`
   - Removed `_startPosition` override inside `Reset()`; only `SetStartPosition` mutates the cursor anchor now.
2. `src/TextBuffer/Decorations/IntervalTree.cs`
   - Normalized overlap checks with `queryStart/queryEndExclusive` so empty query ranges expand to `[offset, offset+1)` (clamped at `int.MaxValue`).
   - Reused the normalized bounds for both empty and non-empty decorations.
3. `tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs`
   - Added `Test48_FlushEditKeepsFindNextProgress`, replacing the full document via `PushEditOperations` and asserting `FindNext()` continues at match #5 rather than restarting at line 1.
4. `tests/TextBuffer.Tests/DocUI/DocUIFindDecorationsTests.cs`
   - Added `CollapsedCaretAtMatchStartReturnsIndex`, ensuring caret-only selections report the correct match ordinal.
5. `tests/TextBuffer.Tests/TestMatrix.md`
   - DocUI rows now cite Test48 + caret test; targeted rerun table documents the commands QA executed.

## Validation (see `agent-team/handoffs/B3-DocUI-StagedFixes-QA-20251124.md`)
| Command | Result |
| --- | --- |
| `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo` | 46/46 ✅ (covers Test48 + prior B3-FM regressions) |
| `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~DocUIFindDecorationsTests --nologo` | 9/9 ✅ (includes collapsed caret test) |
| `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~DocUIFindModelTests --nologo` | 0/0 ⚠️ (class emitted as `PieceTree.TextBuffer.Tests.DocUI.FindModelTests`; alias recorded as a follow-up) |

## Follow-ups
- Rename the DocUI FindModel test class or adjust CI filters so `FullyQualifiedName~DocUIFindModelTests` no longer returns zero tests.
- Remaining Investigator warnings (inline decoration throttling + TextModel decoration filters) stay on the backlog for the next DocUI pass.
