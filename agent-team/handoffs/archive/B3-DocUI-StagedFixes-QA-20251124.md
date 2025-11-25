# B3 DocUI Staged Fixes QA — 2025-11-24

## Scope
- Validated Porter-CS DocUI fixes for find decorations reset + caret overlap and `FindModel` flush-edit progress (tests: `DocUIFindModelTests.Test48_FlushEditKeepsFindNextProgress`, `DocUIFindDecorationsTests.CollapsedCaretAtMatchStartReturnsIndex`).
- Reviewed team memory (`agent-team/members/qa-automation.md`) and AI Team Playbook before execution per AA4 QA protocol.

## Commands & Results
| # | Command | Purpose | Result | Duration | Logs |
| --- | --- | --- | --- | --- | --- |
| 1 | `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~DocUIFindModelTests --nologo` | Follow spec-provided filter to exercise DocUI FindModel suite | **0/0** (filter mismatch) | 2.0s | Console only; VSTest warning recorded indicating alias mismatch |
| 2 | `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo` | Actual DocUI FindModel suite (emitted as `PieceTree.TextBuffer.Tests.DocUI.FindModelTests`) covering Test48 + prior scope regressions | **46/46** passed | 2.9s | Console output (Terminal). No TRX captured |
| 3 | `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~DocUIFindDecorationsTests --nologo` | DocUI find decorations regression sweep including caret-overlap guard | **9/9** passed | 1.9s | Console output (Terminal). No TRX captured |

## Findings
- `Test48_FlushEditKeepsFindNextProgress` stays green and confirms `FindModel` preserves `MatchesPosition` after a full document flush edit (Porter fix for find progress reset).
- `CollapsedCaretAtMatchStartReturnsIndex` now runs inside the DocUI decorations sweep (suite size increased to 9) ensuring collapsed carets at a match boundary still receive an accurate index after decoration reset.
- Spec-mandated `FullyQualifiedName~DocUIFindModelTests` filter returns zero tests because the class is compiled as `PieceTree.TextBuffer.Tests.DocUI.FindModelTests`; documented for follow-up.

## Remaining Risks / Follow-ups
1. **Filter alias drift** – Test runners/scripts referencing `DocUIFindModelTests` must update to `FindModelTests` (or rename the class) to avoid silent 0-test executions. Recommend adding a façade trait or rename in the next PR.
2. **Multi-cursor parity gap** – Two TS multi-cursor cases remain TODO in DocUI FindModel; no new coverage yet. Track via Batch #3 backlog.
3. **Artifacts** – No TRX or log copies captured; if CI needs evidence, rerun commands with `--logger trx` or attach console logs.

## References
- TestMatrix updates: `tests/TextBuffer.Tests/TestMatrix.md` (DocUIFindModel/DocUIFindDecorations rows + targeted rerun tables).
- Memory update: `agent-team/members/qa-automation.md` (2025-11-24 entry documenting this validation).
