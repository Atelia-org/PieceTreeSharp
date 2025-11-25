# B3-Decor-Stickiness-Review

## Scope
- Audited staged changes for `FindDecorations`, new TextModel decoration helpers, and the accompanying DocUI/Decoration test additions (`git status --cached` on 2025-11-23).
- Cross-referenced with upstream `ts/src/vs/editor/contrib/find/browser/findDecorations.ts` and `model.decorations.test.ts` to verify parity.

## Critical Issues (must fix before merge)

1. **CI-1 – Cached find scopes never track edits**  
   - `src/TextBuffer/DocUI/FindDecorations.cs` lines 123-148 & 300-312 clone `Range[]` into `_cachedFindScopes` and immediately short-circuit future calls. Once a scope is set, edits that move decorations no longer update the cached coordinates.  
   - Upstream TS (`ts/src/vs/editor/contrib/find/browser/findDecorations.ts` lines 65-74) always resolves scopes from live decoration IDs, so the selection-following behavior stays correct after every edit.  
   - Impact: after enabling find-in-selection, any subsequent typing inside the selection causes `FindModel`/DocUI to operate on stale offsets (search scope, highlighting, preservations, telemetry). DocUI tests never cover this path, so regressions ship silently.  
   - Fix: drop `_cachedFindScopes` entirely (or invalidate it on every decorations change event) and compute scopes from `GetDecorationById` each call. Add a regression test that edits inside the scope and asserts the returned ranges move with the decoration.

2. **CI-2 – Find scopes are incorrectly trimmed**  
   - `Set()` pipes scopes through `NormalizeFindScopes()`/`NormalizeHighlightRange()` (lines 302-312 & 541-599) which lop off trailing blank lines whenever the selection ends at column 1.  
   - TS (`findDecorations.ts` lines 218-225) feeds `findScopes` verbatim into `addDecoration`, meaning full-line selections keep their exact boundaries (including the newline at the end).  
   - Impact: selecting whole lines via Shift+Down (ends at the next line’s column 1) now drops the newline from the search scope. Patterns that rely on that newline (`\n`, `^$`, multi-line regex) or DocUI range highlights no longer match VS Code. The new test `DocUIFindDecorationsTests.FindScopesAreNormalizedAndCached` codifies the regression.  
   - Fix: remove the normalization step and port TS’s raw range behavior; update/replace the test to assert against the real scope (including newline) and ensure multi-line selections still show the highlight trimming only for the current match, not for scopes.

3. **CI-3 – Overview throttling never scales for large files**  
   - `BuildOverviewDecorations()` hard-codes `mergeLinesDelta = 2` (lines 604-634).  
   - TS calculates `mergeLinesDelta = Math.max(2, ceil(3 / approxPixelsPerLine))` (lines 163-197) using the editor’s viewport height and line count so that 100k+ matches collapse into a few dozen overview decorations instead of O(N).  
   - Impact: on any file with >1000 matches and tens of thousands of lines, the C# port still emits thousands of `find-match-only-overview` decorations, exactly the perf cliff the TS optimization fixed (overview ruler painting still O(N) and `DeltaDecorations` allocates per match). We need to either plumb DocUI layout height or compute a similar heuristic from the host to keep decoration counts bounded.

## Warnings / Follow-ups

1. **W-1 – Shared owner ID will collide across controllers**  
   - `FindDecorations` hard-codes `FindDecorationsOwnerId = 1000` (line 25). Every instance on the same `TextModel` will remove the other’s decorations because `TextModel.DeltaDecorations` filters by owner. TS doesn’t have owner IDs, but in C# each controller should call `model.AllocateDecorationOwnerId()` so DocUI, future global Find widgets, and QA harnesses can coexist.

2. **W-2 – Tests/doc now enshrine the regressions**  
   - `DocUIFindDecorationsTests.FindScopesAreNormalizedAndCached` (lines 79-90) asserts trimmed scopes and doesn’t cover edits after scopes are set, so fixing CI-1/CI-2 will break the test even though TS expects that behavior.  
   - `tests/TextBuffer.Tests/TestMatrix.md` lines 13-16 & 55 mark CL4.F5 “Covered” based on these tests, so the change-log and QA matrix now give false confidence. Update the suite to mirror TS (scope equals selection, no caching) and amend the matrix once parity is restored.

## Suggested Next Steps
- Drop `_cachedFindScopes` / normalization logic, port TS behavior, and add regression tests (edit-after-scope, newline-boundary).  
- Reintroduce dynamic overview throttling by plumbing DocUI viewport metrics (or approximate) so large files don’t reintroduce freezes; add a stress test asserting the number of overview decorations stays bounded (< height/3).  
- Allocate decoration owner IDs per `FindDecorations` instance and adjust cleanup logic accordingly.  
- Update `DocUIFindDecorationsTests` and TestMatrix entries once parity and tests are corrected.  
- Re-run `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindDecorationsTests` (and full suite) after fixes.

## Resolution (2025-11-23)
- `FindDecorations` now resolves scope ranges from live decoration IDs, preserves trailing newlines, computes `mergeLinesDelta` using a host-provided `ViewportHeightPx`, and requests owner IDs via `TextModel.AllocateDecorationOwnerId()`. `FindModel`/`DocUIFindController`/`TestEditorContext` were updated to pass the viewport provider through the DocUI pipeline.
- `DocUIFindDecorationsTests` added `FindScopesPreserveTrailingNewline`, `FindScopesTrackEdits`, and `OverviewThrottlingRespectsViewportHeight`; TestMatrix baseline increased to 235/235 with targeted `--filter DocUIFindDecorationsTests` 8/8 and `--filter DecorationStickinessTests` 4/4. All documents now reference `#delta-2025-11-23-b3-decor-stickiness-review`.
