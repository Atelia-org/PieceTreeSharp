# B3 TextModelSearch QA — Run R37

## Scope
- Certify `#delta-2025-11-25-b3-textmodelsearch` using the freshly ported TextModelSearch battery plus a full-suite regression to ensure no collateral regressions in `tests/TextBuffer.Tests`.
- All runs executed with `PIECETREE_DEBUG=0` and `dotnet test --nologo` per sprint QA protocol (QA-Automation, Sprint 03 Run R37).

## Commands & Outcomes
| Step | Command | Total | Passed | Failed | Skipped | Duration |
| --- | --- | --- | --- | --- | --- | --- |
| Targeted | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~TextModelSearchTests --nologo` | 45 | 45 | 0 | 0 | 2.5s |
| Full Suite | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | 365 | 365 | 0 | 0 | 61.6s |

## Notes
- PieceTree diagnostic dump (`PieceTreeModel Dump`) appeared during the full run; this is the expected CRLF telemetry spam and does not indicate a failure.
- No test regressions or flakiness observed; the TextModelSearch battery remains deterministic after the changefeed.

## Follow-ups
- None. QA sign-off granted for `#delta-2025-11-25-b3-textmodelsearch`.
