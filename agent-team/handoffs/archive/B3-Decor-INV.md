# B3-Decor-INV – Decor Stickiness & Find Decorations Parity

## Context
- **Sprint / Batch**: Sprint 03 · Batch #3 focus item “B3-Decor-Stickiness”. Prior changefeed consumed through [`#delta-2025-11-23-b3-fc-regexseed`](../indexes/README.md#delta-2025-11-23-b3-fc-regexseed).
- **Objective**: Align C# decoration infrastructure and DocUI find overlays with VS Code’s TypeScript implementation before Porter-CS resumes controller work. Output includes behavior inventory, gap analysis, and a concrete porting plan leading to `#delta-2025-11-23-b3-decor-stickiness`.

## Findings
### TS behavior expectations
- `ts/src/vs/editor/contrib/find/browser/findDecorations.ts`
  - Maintains four decoration groups: find matches, current match highlight, overview-ruler approximations (for >1000 matches), and find-scope markers. Every option uses `TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges`, themed `inlineClassName`, minimap+overview colors, and a dedicated `_RANGE_HIGHLIGHT_DECORATION` that trims trailing empty lines.
  - `setCurrentFindMatch` swaps decoration options in-place (no remove/add) and always re-creates the range highlight decoration to shadow multi-line matches.
  - Navigation helpers (`matchBeforePosition`, `matchAfterPosition`) wrap around and skip undefined ranges; `getCurrentMatchesPosition` inspects the model via `getDecorationsInRange` using decoration-option identity to compute the 1-based index.
- `ts/src/vs/editor/test/common/model/modelDecorations.test.ts`
  - Exhaustive coverage for creation/removal APIs, per-line queries (`getLineDecorations`, `getAllDecorations`), change events, and editing effects (insert/delete/replace single vs multi line).
  - Dedicated matrix (`suite('Decorations and editing')`) validates all four stickiness modes against insert/delete/replace combinations with and without `forceMoveMarkers`.
  - Regression tests for `removeAllDecorationsWithOwnerId`, `deltaDecorations` hover updates, issue-specific cases (`#4317`, `#16922`, `#41492`).
- `ts/src/vs/editor/contrib/find/test/browser/findController.test.ts`
  - Uses `withAsyncTestCodeEditor` to ensure controller actions correctly update decoration state (e.g., search scopes cleared on close, Cmd+E multi-line seeds produce multiple find-scope decorations, whitespace seeding still reveals widget).

### Current C# state & deltas
- `src/TextBuffer/DocUI/FindDecorations.cs`
  - Provides only two decoration types (`findMatch`, `currentFindMatch`) and find-scope markers; lacks range highlight & overview-ruler approximations entirely, so large-match throttling and whole-line shading are missing.
  - Decoration option definitions omit `InlineClassName`, `OverviewRuler`, and `Minimap` metadata, so downstream renderers cannot style DocUI overlays like VS Code.
  - `_cachedFindScopes` simply clones the incoming ranges; no normalization of trailing `endColumn === 1` or trimming to line max columns like TS does inside `Research()`.
- `src/TextBuffer/TextModel.cs`
  - Exposes `GetDecorationsInRange`, `GetInjectedTextInLine`, `GetFontDecorationsInRange`, but **no** equivalents for `getAllDecorations()` or `getLineDecorations()`—blocking parity for per-line queries and owner-scoped inspections in tests.
  - `DeltaDecorations` returns materialized `ModelDecoration` objects but does not record the previous ID list; without helper APIs Porter must track IDs manually, making tests harder to port.
- `tests/TextBuffer.Tests/DecorationTests.cs`
  - Only a thin subset of TS cases is migrated (owner scopes, collapseOnReplaceEdit, a single stickiness smoke test, metadata propagation). None of the insert/delete/replace matrices, per-line decoration assertions, or event-count tests exist, leaving `TrackedRangeStickiness` behavior largely unverified.
  - No coverage for `forceMoveMarkers` semantics even though `TextModel.ApplyEdits` implements the knob and `DecorationRangeUpdater` supports it.
- `tests/TextBuffer.Tests/DocUI/TestEditorContext.cs`
  - Because `TextModel` lacks per-line APIs, the harness fetches *all* decorations via `GetDecorationsInRange` spanning the full buffer and filters by CSS class name. There is no way to assert overview/inline-class metadata or range highlight behavior, and owner IDs are ignored.
- `tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs`
  - Current DocUI coverage inspects search scope/selection behavior but never asserts decoration owner counts, range highlight removal, or stickiness when edits occur between navigation steps.

### Test coverage gap summary
| Area | TS coverage | C# coverage | Gap impact |
| --- | --- | --- | --- |
| Per-line decoration queries | `lineHasDecorations`, `getLineDecorations` suites | No API/tests | Cannot verify line-level rendering or sticky spans per row. |
| Stickiness matrix | `Decorations and editing` (insert/delete/replace × 4 strategies × forceMoveMarkers) | Absent | Regression risk when `forceMoveMarkers` toggled by DocUI or future cursor features. |
| Delta events | Add/change/remove + edit-triggered event count tests | Only a metadata smoke test | Hard to ensure DocUI listens for updates to hide stale highlights. |
| Find decoration lifecycle | `FindDecorations` implicitly tested through `findModel.test.ts` and controller tests | Not directly tested | Range highlights, overview throttling, and match index computations could drift unnoticed. |

## Action Items (toward `#delta-2025-11-23-b3-decor-stickiness`)
1. **Bring FindDecorations feature parity** (Porter-CS)
   - Add `_overviewRulerApproximateDecorationIds` + `_rangeHighlightDecorationId` fields mirroring TS semantics, including merge logic for >1000 matches and trimmed whole-line range highlight. Reuse `ModelDecorationOptions` to register `rangeHighlight` + `find-scope` options with `IsWholeLine` flags and `OverviewRuler/Minimap` colors.
   - Normalize cached find scopes (convert trailing column==1 to previous line max column) so `DocUIFindController` search scopes match VS Code when multi-line selections end at column 1.
   - Ensure `Dispose()/Reset` remove every decoration type, not only `_decorationIds`, to avoid orphaned highlights.
2. **Extend TextModel decoration APIs** (Porter-CS)
   - Implement `GetAllDecorations(int ownerFilter = DecorationOwnerIds.Any)` plus `GetLineDecorations(int lineNumber, int ownerFilter = ...)` to mirror TS surface area used in tests. Both should honor `ShowIfCollapsed` and owner filters to keep harness results deterministic.
   - Provide helper returning decoration IDs for an owner to simplify `deltaDecorations` tests (e.g., `GetDecorationIdsByOwner`).
3. **Port TS decoration test suites (QA/Porter pairing)**
   - Recreate the following groups inside `tests/TextBuffer.Tests/DecorationTests.cs` (or new files per group):
     - Basic creation/removal/per-line assertions (`single character`, `line decoration`, `multiple line`).
     - Event firing tests for add/change/remove and edit-driven updates.
     - Editing impact suites (“decorations are updated when inserting/deleting/replacing…”) covering before/start/inside/end/after scenarios.
     - Full stickiness matrix from `suite('Decorations and editing')`, including `forceMoveMarkers` rows.
     - `removeAllDecorationsWithOwnerId`, `deltaDecorations` regressions, `issue #16922` overlapping-range query, `issue #41492` collapseOnReplaceEdit.
   - Acceptance: mirror TS expected ranges verbatim; use new API helpers rather than raw `GetDecorationsInRange`.
4. **Add DocUI-specific coverage** (QA)
   - New suite `DocUIFindDecorationsTests` leveraging `TestEditorContext` to assert `FindDecorations` contract: match counts, current highlight swapping, range highlight lines, `matchBefore/AfterPosition` wrap-around, large-match overview behavior (can simulate by injecting >1000 matches via repeated text).
   - Extend `DocUIFindControllerTests` with cases verifying find-scope decorations survive/clear based on controller actions (toggle search scope, close widget, edit model between navigations) and that editing at edges respects `TrackedRangeStickiness` semantics.
5. **DocMaintainer / Info-Indexer hooks**
   - Document the new APIs and test suites in `docs/plans/ts-test-alignment.md` Live Checkpoints (anchor `#delta-2025-11-23-b3-decor-stickiness`).
   - Once Porter/QA deliverables land, publish changefeed entry and update Sprint log referencing this investigation.

## Risks
- **API churn**: Introducing `GetAllDecorations`/`GetLineDecorations` alters the public `TextModel` surface; downstream callers (Cursor, MarkdownRenderer) must remain stable.
- **Performance**: Recreating range highlight & overview decorations per search may increase `deltaDecorations` churn; need benchmarks on large files to ensure no regressions.
- **Feature gaps**: Minimaps/overview rulers are not currently rendered anywhere in PieceTreeSharp; we still need to store metadata for parity even if rendering is deferred.
- **Test volume**: Porting the entire TS matrix adds dozens of xUnit cases (~150). CI time will increase; consider splitting into focused classes to keep execution parallelizable.

## References
- TS sources: `ts/src/vs/editor/contrib/find/browser/findDecorations.ts`, `ts/src/vs/editor/test/common/model/modelDecorations.test.ts`, `ts/src/vs/editor/contrib/find/test/browser/findController.test.ts`.
- C# sources: `src/TextBuffer/DocUI/FindDecorations.cs`, `src/TextBuffer/TextModel.cs`, `src/TextBuffer/Decorations/*`.
- Tests/harness: `tests/TextBuffer.Tests/DecorationTests.cs`, `tests/TextBuffer.Tests/DocUI/TestEditorContext.cs`, `tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs`.
- Plan & sprint docs: `docs/plans/ts-test-alignment.md`, `docs/sprints/sprint-03.md`.
