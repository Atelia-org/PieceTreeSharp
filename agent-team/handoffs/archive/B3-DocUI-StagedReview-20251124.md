# B3 DocUI Staged Review – 2025-11-24

## Summary
- Reviewed staged DocUI changes covering `src/TextBuffer/DocUI/DocUIFindController.cs`, `src/TextBuffer/DocUI/FindDecorations.cs`, `src/TextBuffer/DocUI/FindModel.cs`.
- Cross-checked supporting APIs added in `src/TextBuffer/TextModel.cs` and the new DocUI/decoration tests under `tests/TextBuffer.Tests/**`.
- Compared behaviors against VS Code sources (`ts/src/vs/editor/contrib/find/browser/findDecorations.ts`, `findModel.ts`, and `ts/src/vs/editor/common/model/textModel.ts`).

## Findings

### Critical Issues (Must fix before merge)

1. **Reset blows away find start position**  \
   - **C#**: `src/TextBuffer/DocUI/FindDecorations.cs:L344-L348`  \
   - **TS reference**: `ts/src/vs/editor/contrib/find/browser/findDecorations.ts:L25-L51`  \
   - **Risk**: `FindDecorations.Reset()` now clears `_startPosition` to `(1,1)`. When the model fires an `IsFlush` change (e.g., `SetValue` or Ctrl+H replace-all), `FindModel.OnModelContentChanged` already captured the cursor by calling `SetStartPosition`, but the subsequent `Reset()` call immediately overwrites it. The next `FindNext` therefore starts at the top of the file rather than the user’s last caret, diverging from VS Code and breaking wrap semantics after large edits.  \
   - **Fix / Tests**: Stop mutating `_startPosition` inside `Reset()` (leave it for `SetStartPosition`). Add a regression test to `DocUIFindModelTests` that 1) seeds a search mid-document, 2) simulates a flush via `IEditorHost.SetValue`/`TextModel.SetValue`, and 3) asserts that `FindNext()` continues from the pre-flush selection instead of line 1.

2. **Zero-length selections never map to a match position**  \
   - **C#**: `src/TextBuffer/DocUI/FindDecorations.cs:L391-L410` + `src/TextBuffer/Decorations/IntervalTree.cs:L137-L170`  \
   - **TS reference**: `ts/src/vs/editor/contrib/find/browser/findDecorations.ts:L102-L112`  \
   - **Risk**: `GetCurrentMatchesPosition` queries `_model.GetDecorationsInRange` with a zero-length `TextRange` when the selection is just a caret. Our interval tree treats query ranges as `(start,end)` intervals and requires `start < end`, so a caret placed at the beginning of a match never overlaps the decoration. As a result the widget reports “?/N” and keyboard navigation thinks you are off-match while VS Code still reports the correct ordinal. This also affects `SelectAllMatches` because the primary selection cannot be preserved reliably.  \
   - **Fix / Tests**: Treat caret queries as point-in-range lookups (e.g., expand the search window to `[offset, offset+1)` when `StartOffset == EndOffset`, or update `IntervalTree.CollectOverlaps` to treat zero-length query ranges inclusively). Add a DocUI test that places the cursor at the start of a known match and asserts `_state.MatchesPosition == 1` after `FindNext()`.

### Warnings (Follow-up recommended)

1. **Large-result throttle still paints inline spans**  \
   - **C#**: `src/TextBuffer/DocUI/FindDecorations.cs:L72-L87`  \
   - **TS reference**: `ts/src/vs/editor/contrib/find/browser/findDecorations.ts:L303-L333`  \
   - **Risk**: `_FIND_MATCH_NO_OVERVIEW_DECORATION` keeps `InlineClassName = "findMatchInline"`, so even when match counts exceed 1 000 we still materialize per-character inline decorations. VS Code intentionally drops the inline class (and minimap color) in this mode to avoid DOM churn; leaving it in negates the perf win the throttling path is supposed to deliver.  \
   - **Fix / Tests**: Remove the inline/minimap styling from the "no-overview" options so high-volume searches only emit overview approximations. Extend `DocUIFindDecorationsTests.LargeResultSets...` to assert that `FindMatchDescription == "find-match-no-overview"` decorations have `InlineClassName == null`.

2. **TextModel decoration queries lack VS Code filters**  \
   - **C#**: `src/TextBuffer/TextModel.cs:L405-L460`  \
   - **TS reference**: `ts/src/vs/editor/common/model/textModel.ts:L1762-L1818`  \
   - **Risk**: The new `GetAllDecorations`/`GetLineDecorations` helpers only accept an owner filter, while the VS Code APIs also expose `filterOutValidation` and `filterFontDecorations`. Upcoming DocUI features (diffs, diagnostics, minimap) rely on these knobs to avoid mixing validation markers into find-scope queries. Without the filters we cannot faithfully port callers such as `getLineDecorations(0, 0, true /*filter validation*/)` and will end up over-reporting decorations.  \
   - **Fix / Tests**: Add the missing optional booleans and plumb them through `DecorationsTrees.Search` just like `textModel.ts` does. Provide unit tests in `DecorationTests` that create font/validation decorations and verify they are filtered when the flags are set.

## Questions / Dependencies
- Need a reliable way to trigger an `IsFlush` in tests (e.g., expose `TextModel.SetValue` or a helper on `TestEditorContext`) to cover the start-position regression noted above.
- When DocUI host surfaces dynamic viewport heights, do we plan to pass a live `viewportHeightProvider` into `DocUIFindController`? Current default always reads the static option; confirm whether that is intentional or if a follow-up is planned.
