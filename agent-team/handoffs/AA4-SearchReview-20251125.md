# AA4 Search Review – 2025-11-25

## Context
- Sprint 03 Run R36 focus: search/test parity per `docs/sprints/sprint-03.md` and changefeed checkpoint [`agent-team/indexes/README.md#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) (reminder: FR-01/FR-02 search cache micro-optimizations still rely on SearchParams parity coverage).
- Reviewed the staged diff touching `SearchTypes`, `TextModelSearch`, and the expanded `TextModelSearchTests` / `PieceTreeSearchTests` / `TestMatrix.md`. TS reference set: `ts/src/vs/editor/common/model/textModelSearch.ts` + `ts/src/vs/editor/test/common/model/textModelSearch.test.ts`.

## Findings
1. **High – Layering regression (`src/TextBuffer/Core/SearchTypes.cs` L214-226, `src/TextBuffer/TextModelSearch.cs` L178-205)**  
   - `SearchParams.IsMultilinePattern` now calls `TextModelSearch.IsMultilineRegexSource`. This introduces a circular dependency between the core search types (`PieceTree.TextBuffer.Core`) and the text-model search engine (`PieceTree.TextBuffer`), whereas TS keeps `SearchParams` + `isMultilineRegexSource` co-located inside `textModelSearch.ts` without crossing layers. Any consumer that only needs `SearchParams` (DocUI FindModel, FR-01/FR-02 caches) now pulls in the entire TextModelSearch surface, making it impossible to reuse the core search primitives independently or in other assemblies.  
   - **TS reference:** `ts/src/vs/editor/common/model/textModelSearch.ts` lines 24-108 keep `SearchParams` + `isMultilineRegexSource` encapsulated to avoid such a dependency.  
   - **Fix:** Move `IsMultilineRegexSource` back into `SearchTypes` (or a neutral helper under `Core`) and expose it the same way TS does, so `SearchParams` remains layer-agnostic while `TextModelSearch` can still reuse the helper without inverting dependencies.

2. **Medium – `SearchParams.parseSearchRequest` still untested (tests/TextBuffer.Tests/TextModelSearchTests.cs)**  
   - The new `TextModelSearchTests` cover range scopes, word boundaries, multiline regex, captures, and zero-width cases, but the TS suites for `parseSearchRequest` + `isMultilineRegexSource` (see `textModelSearch.test.ts` lines 620-706) are still missing. That means the FR-01/FR-02 search cache work and Unicode escape parsing remain unguarded—regressions in `SearchParams` (e.g., regex compilation flags, whole-word classifier caching, Unicode escape expansion) would still go unnoticed despite the “Verified (Sprint 03 R36)” label in `TestMatrix`.  
   - **Fix:** Port the `parseSearchRequest` positive/negative cases plus the dedicated `isMultilineRegexSource` patterns into `TextModelSearchTests` (or a new `SearchParamsTests`), and update `TestMatrix` once those assertions are actually present.

3. **Medium – Navigation suites from TS still absent (tests/TextBuffer.Tests/TextModelSearchTests.cs)**  
   - TS includes targeted navigation regressions (`"findNextMatch without regex", "findNextMatch with ^line", "findNextMatch with ending boundary", multiline navigation variants`) around lines 400-520 to ensure wrap-around + boundary semantics behave identically. The staged C# file only ports the selection-range navigation (wrapping within multi-range scope) and the capture-navigation variants, leaving literal & boundary navigation untested. This gap matters because `TextModelSearch.FindNextMatch` recently diverged twice (run R34) and the missing tests would have caught the regressions.  
   - **Fix:** Port the remaining TS navigation tests into a dedicated class (e.g., `TextModelSearchTests_NavigationBasics`) so we cover literal, `^`, `$`, multiline, and wrap-around scenarios before we mark the suite fully verified.

## Recommendations & Next Steps
- Decouple `SearchTypes` from `TextModelSearch` by relocating the multiline helper back into the core namespace; keep signatures identical to the TS helper so DocUI / FR-01/FR-02 consumers remain layer-agnostic.
- Extend `TextModelSearchTests` with the missing `parseSearchRequest` + `isMultilineRegexSource` (TS reference lines 620-706) and the literal/boundary navigation suites (TS lines 400-520). Once ported, re-run `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSearchTests --nologo` and refresh the targeted rerun table in `tests/TextBuffer.Tests/TestMatrix.md`.
- Until the above land, DocMaintainer should note that the TextModelSearch row in `TestMatrix.md` is not yet fully verified despite the current R36 label.

## Testing / QA Hooks
- Current staged tests: `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSearchTests --nologo` (35/35 per R36 log) and `--filter PieceTreeSearchTests --nologo` (11/11). Re-run both after addressing the findings to keep the changefeed evidence up to date.

## Doc & Indexing Notes
- DocMaintainer follow-up: adjust `tests/TextBuffer.Tests/TestMatrix.md` (TextModelSearch row) back to “Partial” until the missing TS suites land; mention the outstanding `parseSearchRequest` + navigation work in docs/sprints/migration-log to keep AA4 scope honest.
- Info-Indexer references to add once fixes land: `tests/TextBuffer.Tests/TextModelSearchTests.cs` (new sections) and TS anchors `textModelSearch.ts#L24-L108`, `textModelSearch.test.ts#L400-L706`.

```diff
TS sources consulted for this audit:
- ts/src/vs/editor/common/model/textModelSearch.ts
- ts/src/vs/editor/test/common/model/textModelSearch.test.ts
```
