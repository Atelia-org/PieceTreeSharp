# B3 Test Failures Review – 2025-11-24

## Scope & Inputs
- Reviewed staged per-model sentinel refactor and `GetLineContent` trimming across:
  - `src/TextBuffer/Core/PieceTreeNode.cs`, `PieceTreeModel.cs`, `PieceTreeModel.Edit.cs`, `PieceTreeModel.Search.cs`.
  - Tests in `tests/TextBuffer.Tests/{PieceTreeBaseTests,PieceTreeNormalizationTests,PieceTreeModelTests,UnitTest1}.cs` plus `tests/TextBuffer.Tests/TestMatrix.md` docs.
- TS references: `ts/src/vs/editor/common/model/pieceTreeTextBuffer/{pieceTreeBase.ts,rbTreeBase.ts}` (sentinel + `getLineContent`) and `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` (line-content expectations).
- Changefeeds: `#delta-2025-11-24-b3-sentinel`, `#delta-2025-11-24-b3-getlinecontent`.

## Confirmed Fixes
1. **Per-model sentinel parity**  
   - Each `PieceTreeModel` now instantiates its own sentinel via `PieceTreeNode.CreateSentinel()` (`src/TextBuffer/Core/PieceTreeModel.cs:17-37`) and exposes it for tests via the new `Sentinel` getter.  
   - `PieceTreeNode` stores the owning sentinel reference, so `Next/Prev/Detach/ResetLinks` never read/write another model’s sentinel (`src/TextBuffer/Core/PieceTreeNode.cs:12-120`).  
   - Insert/Delete/RB rotations accept the instance sentinel: `InsertPieceAtEnd`, `RbInsertLeft/Right`, `InsertFixup`, `RotateLeft/Right`, `RbDelete`, `DeleteFixup`, and `ValidateTreeInvariants` all gate on `_sentinel` (`src/TextBuffer/Core/PieceTreeModel.Edit.cs:59-979`, `PieceTreeModel.cs:220-610`).  
   - This matches TS `rbTreeBase.ts` semantics where `SENTINEL.parent` is touched only within one tree, eliminating the stochastic invariant failures reported in `PieceTreeFuzzHarnessTests`.  
   - Tests now compare against `model.Sentinel` instead of the removed global singleton (`tests/TextBuffer.Tests/PieceTreeModelTests.cs:14-23`, `tests/TextBuffer.Tests/UnitTest1.cs:209-218`).

2. **`GetLineContent` now mirrors TS trimming rules**  
   - `PieceTreeModel.GetLineContent` trims trailing CR/LF unless reading the final logical line, matching `pieceTreeBase.getLineContent` (`src/TextBuffer/Core/PieceTreeModel.Search.cs:258-311`, TS lines 610-632).  
   - New tests assert both the trimmed public API and the raw content via `GetLineRawContent`, covering cache invalidation and normalization scenarios (`tests/TextBuffer.Tests/PieceTreeBaseTests.cs:65-103`, `PieceTreeNormalizationTests.cs:28-86`).  
   - `TextModel.GetLineContent`, `Cursor` navigation, and DocUI callers already expect trimmed content (same as VS Code), so no further C# adjustments were required.  
   - `tests/TextBuffer.Tests/TestMatrix.md` documents the semantic shift under `#delta-2025-11-24-b3-getlinecontent` and records the targeted reruns for cache/normalization suites.

3. **QA/Doc plumbing updated**  
   - Test Matrix now includes the sentinel & line-content changefeeds, baseline `dotnet test -v m` results (253/253), and targeted reruns for `PieceTreeFuzzHarnessTests.RandomDeleteThreeMatchesTsScript` plus the cache/normalization suites to guard both fixes.

## Risks & TODOs
- **Regression guardrails**: future code that instantiates `PieceTreeNode` must continue to pass the owning sentinel. Porter-CS should keep an eye on new helpers (e.g., builder/factory paths) to avoid reintroducing the shared static sentinel (no action needed now, just a watch item).  
- **QA**: rerun `export PIECETREE_DEBUG=0 && dotnet test -v m` when integrating adjacent PieceTree refactors to ensure `ValidateTreeInvariants` still catches cross-tree misuse (record under `#delta-2025-11-24-b3-sentinel`).

## Validation Commands
- Full sweep: `export PIECETREE_DEBUG=0 && dotnet test -v m` (already recorded as 253/253 for the queued changefeed).  
- Targeted smoke (documented in TestMatrix):
  1. `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~PieceTreeBaseTests.GetLineContent_Cache_Invalidation" --nologo`  
  2. `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~PieceTreeNormalizationTests" --nologo`  
  3. `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.PieceTreeFuzzHarnessTests.RandomDeleteThreeMatchesTsScript" --nologo`

## References
- C#: `src/TextBuffer/Core/PieceTreeNode.cs`, `PieceTreeModel.cs`, `PieceTreeModel.Edit.cs`, `PieceTreeModel.Search.cs`.  
- Tests: `tests/TextBuffer.Tests/PieceTreeBaseTests.cs`, `PieceTreeNormalizationTests.cs`, `PieceTreeModelTests.cs`, `UnitTest1.cs`, `TestMatrix.md`.  
- TS: `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts`, `rbTreeBase.ts`, and `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`.