# B3-TestFailures-PORT
_Date: 2025-11-24_

## Completed in this drop
- **GetLineContent parity (#delta-2025-11-24-b3-getlinecontent):** `PieceTreeBaseTests.GetLineContent_Cache_Invalidation_{Insert,Delete}` and the three `PieceTreeNormalizationTests` cases now assert trimmed `GetLineContent` values, then use `GetLineRawContent` to pin the raw CR/LF bytes. The TestMatrix documents the delta plus the targeted reruns so QA has a bread-crumb when TS adds more `testLinesContent` coverage.
- **Per-model sentinel guard (#delta-2025-11-24-b3-sentinel):** `PieceTreeNode.CreateSentinel()` now returns a model-local sentinel that flows through every `PieceTreeNode` constructor/reset path. `PieceTreeModel` owns its `_sentinel`, edit helpers pass it to each new node, tests query `model.Sentinel`, and `agent-team/type-mapping.md` reflects the new contract. This stops `PieceTreeFuzzHarnessTests.RandomDeleteThreeMatchesTsScript` from observing another model’s temporary sentinel metadata while `ValidateTreeInvariants` runs under parallel xUnit.

## Validation
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~PieceTreeBaseTests.GetLineContent_Cache_Invalidation" --nologo` → ✅ (2/2, 1.9s)
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~PieceTreeNormalizationTests" --nologo` → ✅ (3/3, 1.7s)
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.PieceTreeFuzzHarnessTests.RandomDeleteThreeMatchesTsScript" --nologo` → ✅ (1/1, 1.6s)
- `export PIECETREE_DEBUG=0 && dotnet test -v m` → ✅ (253/253, 54.5s)

## Notes
- `tests/TextBuffer.Tests/TestMatrix.md` now carries both the `#delta-2025-11-24-b3-getlinecontent` alignment row and the sentinel-targeted rerun log so QA/Investigator can trace the parity swap.
- `agent-team/members/porter-cs.md` (Worklog) and `agent-team/type-mapping.md` include the same delta tags for future cross-reference.
