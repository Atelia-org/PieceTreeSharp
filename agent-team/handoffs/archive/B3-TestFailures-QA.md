# B3-TestFailures-QA (2025-11-24)

## Context
- Targeted deltas: `#delta-2025-11-24-b3-sentinel`, `#delta-2025-11-24-b3-getlinecontent`.
- Scope: confirm PieceTree sentinel isolation + `GetLineContent` cache invalidation behave deterministically under latest Porter-CS patchset.

## Executed Commands
| # | Command | Total/P/F | Duration | Notes |
| - | --- | --- | --- | --- |
| 1 | `export PIECETREE_DEBUG=0 && dotnet test -v m` | 253/253/0 | 59.3s | Full-suite sweep; provides regression envelope for both deltas with verbose tree dumps enabled. |
| 2 | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~PieceTreeBaseTests.GetLineContent_Cache_Invalidation" --nologo` | 2/2/0 | 3.8s | Focused cache invalidation suite; asserts trimmed `GetLineContent` + raw line audit parity with TS `splitLines`. |
| 3 | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~PieceTreeNormalizationTests" --nologo` | 3/3/0 | 1.6s | Normalization regressions; ensures `GetLineRawContent` retains CR/LF bytes while cache serves trimmed rows. |
| 4 | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.PieceTreeFuzzHarnessTests.RandomDeleteThreeMatchesTsScript" --nologo` | 1/1/0 | 1.6s | Fuzz harness replay for TS Random Delete #3; validates per-model sentinel no longer fires false positives. |

## Conclusion
All reruns are green and line up with migration expectations. Sentinel guards now stay scoped to each fuzz harness instance, and `GetLineContent`/`GetLineRawContent` deliver the trimmed-vs-raw pairing required by TS parity. Recommend unblocking the staging queue for both deltas once Info-Indexer publishes the associated changefeed entries.
