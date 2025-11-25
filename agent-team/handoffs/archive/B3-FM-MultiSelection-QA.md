# B3-FM-MultiSelection-QA (2025-11-24)

**Role:** QA-Automation  
**Delta Tag:** `#delta-2025-11-24-b3-fm-multisel`  
**Scope:** Validate Porter-CS multi-selection plumbing (DocUI harness + `FindModel.SetSelections`) and re-enabled TS parity tests (Test07/Test08) before Sprint 03 marks FindModel parity complete.

## Validation Commands & Results
| # | Command | Result | Duration | Notes |
| --- | --- | --- | --- | --- |
| 1 | `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.DocUI.FindModelTests.Test07_MultiSelectionFindModelNextStaysInScopeOverlap\|FullyQualifiedName=PieceTree.TextBuffer.Tests.DocUI.FindModelTests.Test08_MultiSelectionFindModelNextStaysInScope" --nologo` | 2/2 passed | 1.7s | Confirms overlapping + disjoint multi-selection scopes stay within the staged selections and wrap correctly. |
| 2 | `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~FindModelTests" --nologo` | 48/48 passed | 3.3s | Full DocUI FindModel suite (Tests01–48) including refreshed scope + replace regressions. |
| 3 | `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | 242/242 passed | 2.9s | Full TextBuffer suite baseline for Sprint 03 QA close-out; log retained for Info-Indexer changefeed. |

## Observations
- Harness upgrades successfully propagate multiple selections; both Test07/08 now exercise the TS navigation tables without additional scaffolding.
- Legacy `FullyQualifiedName~DocUIFindModelTests` alias still resolves to 0 tests; keeping the canonical namespace in the targeted filter avoids silent skips.
- No regressions surfaced in broader FindModel or DocUI suites; telemetry dumped by the full run matches previous durations, indicating no perf regressions from the new selection cloning path.

## Documentation & Handoff
- `tests/TextBuffer.Tests/TestMatrix.md` DocUI FindModel row now lists 43/43 coverage plus the three validation commands (see row labeled "DocUIFindModelTests").
- Please cite this QA note when publishing `#delta-2025-11-24-b3-fm-multisel` so DocMaintainer can propagate the baseline to Sprint/Task Board.

## Blockers / Follow-ups
- None blocking QA sign-off. Recommend Info-Indexer adds an alias deprecation note for `FullyQualifiedName~DocUIFindModelTests` in the changefeed so future scripts adopt the canonical filter.
