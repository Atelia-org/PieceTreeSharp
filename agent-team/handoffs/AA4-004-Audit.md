# AA4-004 Audit – CL8 DocUI Find/Replace + Decorations

**Date:** 2025-11-20  
**Investigator:** GitHub Copilot  
**Scope:** TS `ts/src/vs/editor/contrib/find/browser/{findController.ts,findModel.ts,findDecorations.ts,replacePattern.ts}`, `ts/src/vs/editor/common/model/textModelSearch.ts`; C# `src/PieceTree.TextBuffer/{TextModelSearch.cs,TextModel.cs,SearchHighlightOptions.cs}`, `src/PieceTree.TextBuffer/Decorations/ModelDecoration.cs`, `src/PieceTree.TextBuffer/Rendering/MarkdownRenderer.cs`.  
**Prereqs:** Builds on CL5–CL7 outcomes (builder/change-buffer/cursor parity) and AA3-008 decoration storage so that DocUI overlay work can reuse owner-aware `DecorationsTrees`.

## Findings

### F1 – Search decorations drop overlay metadata & degrade path (High)
- **Summary:** VS Code registers dedicated decoration options for regular matches, the current match, find scope shading, range highlights, and overview/minimap-only fallbacks (see `_FIND_MATCH_DECORATION`, `_CURRENT_FIND_MATCH_DECORATION`, `_FIND_SCOPE_DECORATION`, `_RANGE_HIGHLIGHT_DECORATION` in `findDecorations.ts`). When matches exceed 1 000, it automatically switches to `no-overview` + `overview-only` decorations so minimap/overview lanes stay responsive. The C# port (`TextModel.HighlightSearchMatches` + `ModelDecorationOptions.CreateSearchMatchOptions`) emits a single bare decoration with no overview/minimap/glyph metadata, no z-index separation for the active match, no scope highlight, and no fallback for large result sets.
- **TS Reference:** `ts/src/vs/editor/contrib/find/browser/findDecorations.ts` (`set()`, `_FIND_MATCH_*` definitions) & `findModel.ts` (`_decorations.set`).
- **C# Gap:** `src/PieceTree.TextBuffer/TextModel.cs#L429-L458` builds `ModelDeltaDecoration` objects that always use `ModelDecorationOptions.CreateSearchMatchOptions()` (`Decorations/ModelDecoration.cs#L82-L118`), which only sets `RenderKind = SearchMatch`. Overview/minimap colors, glyph/margin metadata, range highlight rows, and find-scope decorations are never created, and matches >1 000 still try to instantiate full decorations, risking DocUI slowdowns.
- **Impact:** DocUI lacks minimap/overview/glyph cues for search hits, cannot distinguish the current match, cannot visualize the “find in selection” scope, and will churn thousands of decorations when the TS side would collapse into aggregated overview bars. Owner layering for search overlays never materializes, so Porter-AA4-008 has no data to render and QA-AA4-009 cannot assert overlay parity.
- **Remediation Guidance:** Port the TS `FindDecorations` option set: introduce `SearchMatch`, `CurrentSearchMatch`, `RangeHighlight`, `FindScope`, `OverviewOnly` decoration options with the same stickiness, z-index and lane metadata; update `HighlightSearchMatches` to (a) emit the scope highlight and range highlight around the active match, (b) downgrade to overview-only decorations when matches >1000, and (c) tag minimap/overview colors identical to VS Code. Surface the active-match id so DocUI consumers can pin it when rendering.
- **Validation Hooks:**
  - `TextModelSearchTests.SearchDecorationsIncludeOverviewMetadata` – assert new decorations carry overview/minimap/glyph settings and degrade when count > 1000.
  - `TextModelTests.FindScopeDecorationsRespectSelections` – selection-scoped searches produce the scope shading decoration only for the requested ranges.
  - `MarkdownRendererTests.SearchHighlightZOrder` – ensure DocUI renders current match (`zIndex` 13) above regular hits and that overview/minimap annotations appear in the footer legend.

### F2 – No find-state controller or search-scope overlays (High)
- **Summary:** TS keeps a persistent `FindModelBoundToEditorModel` that remembers `_startPosition`, `searchScope` (possibly multiple selections), `loop` configuration, and exposes helpers such as `getCurrentMatchesPosition`, `matchBeforePosition`, `setCurrentFindMatch`, and `getFindScopes()` for DocUI consumers. The C# layer exposes only stateless helpers (`TextModel.FindMatches` and `HighlightSearchMatches`) plus `SearchHighlightOptions` (query/isRegex/matchCase). There is no way to feed per-selection scopes, no ability to update the start position/current match, no owner segregation between match types, and no hook for DocUI to query match indexes.
- **TS Reference:** `ts/src/vs/editor/contrib/find/browser/findModel.ts` (`FindModelBoundToEditorModel`, `_decorations.getFindScopes()`, `_setCurrentFindMatch`, `_findMatches`, `_getNextMatch`).
- **C# Gap:** `src/PieceTree.TextBuffer/SearchHighlightOptions.cs` lacks `WholeWord`, `PreserveCase`, `Loop`, or scope ranges; `TextModel.HighlightSearchMatches` always searches the entire model and wipes/recreates decorations per call; there is no controller akin to `FindModel` to hold `_decorations`, `_startPosition`, or expose match indices. `SearchRangeSet` exists for `FindMatches`, but DocUI highlighting never uses it, so “find in selection” overlays are impossible.
- **Impact:** DocUI cannot mirror VS Code’s `FindWidget` behavior: the “Find in Selection” toggle, selection-constrained matches, and “current match” navigation cues never materialize. `Porter` has no hook to coordinate replace previews with the active match, and QA cannot assert match index/loop behavior. Cursor parity work from CL7 also cannot highlight search scopes, undermining multi-selection parity.
- **Remediation Guidance:** Add a managed `FindController`/state class (mirroring `FindModelBoundToEditorModel`) that stores search state, start position, scopes, and match counts on top of `TextModelSearch`. Extend `SearchHighlightOptions` (or introduce a richer `FindOptions`) with `WholeWord`, `PreserveCase`, `Loop`, `FindInSelection` + `Ranges`, and `SeedFromSelections`. `HighlightSearchMatches` should accept/export scope ranges and return the active match id so DocUI/MarkdownRenderer can render both the scope shading and the current hit.
- **Validation Hooks:**
  - `FindControllerTests.ScopeAndLoopParity` – prove multi-selection scopes remain constrained and match positions update as the cursor moves.
  - `TextModelSearchTests.MatchIndexAccounting` – assert the controller reports the same match index/total as VS Code when seeding from selections.
  - `MarkdownRendererTests.FindInSelectionOverlay` – snapshot verifying the rendered Markdown shows scope braces + current match bracket exactly where TS does.

### F3 – Replace preview & captureMatches never surface (High)
- **Summary:** VS Code parses replace strings with `ReplacePattern` (`replacePattern.ts`) so regex groups, `\`, `$1`, and case-preserving `\u/\l` sequences can be previewed and executed consistently. `FindModel` always requests `captureMatches` when needed and pipes the match array into both the preview UI and `ReplaceCommand`/`ReplaceAllCommand`. In C#, `TextModelSearch.CreateFindMatch` does capture group extraction but no caller consumes it: `HighlightSearchMatches` drops the arrays, `MarkdownRenderer` never shows replacements, there is no `ReplacePattern` parser, and no `Replace/ReplaceAll` helpers exist on `TextModel`.
- **TS Reference:** `ts/src/vs/editor/contrib/find/browser/findModel.ts#_getReplacePattern/replace/replaceAll`, `replacePattern.ts`, `replaceAllCommand.ts`.
- **C# Gap:** `src/PieceTree.TextBuffer/TextModel.cs` exposes no replace APIs; `SearchHighlightOptions` cannot carry `replaceString` or `PreserveCase`; there is no counterpart to `ReplaceCommand`/`ReplaceAllCommand`. Even though `TextModelSearch.CreateFindMatch` (`TextModelSearch.cs#L617-L639`) collects `string[] matches`, DocUI/renderer never receives that data, so captureMatches and replace previews are dead ends.
- **Impact:** CL8’s “DocUI Find/Replace parity” goal cannot be met—DocUI cannot show per-match replacement previews, Porter cannot implement regex replace semantics (preserveCase, backreferences), and QA cannot verify replace workflows. Any future `FindWidget` built on the C# stack would silently ignore replace metadata, leading to wrong outputs for `$1`/`\u` sequences.
- **Remediation Guidance:** Port `ReplacePattern` and the `ReplaceCommand`/`ReplaceAllCommand` flow, including preserve-case handling and large-result fallbacks. Extend the find state/DocUI pipeline so `captureMatches` arrays travel with the search decorations (e.g., embed preview text/capture indexes inside `ModelDecorationOptions.Description` or a new metadata bag). Add `replaceString` + `PreserveCase` + `ReplaceInSelection` knobs to `SearchHighlightOptions`, and expose a DocUI API that returns the preview string for each match so MarkdownRenderer can annotate it.
- **Validation Hooks:**
  - `ReplacePatternTests.BackreferenceAndCaseParity` – feed the same fixtures VS Code uses to assert `$0/$1`, `\u/\l`, and escape handling.
  - `TextModelSearchTests.CaptureMatchesRoundTrip` – ensure capture arrays survive the highlight pipeline and can be queried by owner id.
  - `MarkdownRendererTests.ReplacePreviewAnnotations` – snapshot showing DocUI markers for the computed replacement text (ensuring capture substitution matches TS output).

### F4 – Markdown renderer bypasses decoration owners & capture data (Medium)
- **Summary:** Rather than using the owner-aware decorations emitted by `HighlightSearchMatches`, `MarkdownRenderer.CollectSearchMarkers` re-runs `model.FindMatches` every render, inserts bare `<`/`>` markers, ignores capture data, and disregards `OwnerFilter`. That means search overlays always render—even when owner filters disable `DecorationOwnerIds.SearchHighlights`—and there is no hook to show capture/replace previews or per-lane metadata.
- **TS Reference:** The TS side relies on `ITextModel.deltaDecorations` + `FindDecorations` (see `findModel.ts` w/ `this._decorations.set(findMatches, findScopes);`) and downstream renderers query decorations per owner; they never recompute matches in parallel to DocUI.
- **C# Gap:** `src/PieceTree.TextBuffer/Rendering/MarkdownRenderer.cs#L54-L98` calls `model.FindMatches` directly and unconditionally injects `<`/`>` markers into the Markdown. The `OwnerFilter` applied to regular decorations is not honored for search markers, capture arrays from `FindMatch.Matches` are discarded, and there is no structure to attach replace preview text, glyph/minimap annotations, or owner-specific styling.
- **Impact:** DocUI can’t opt-in/out of search overlays per owner, cannot display capture group data or replace previews, and now pays the cost of re-running search twice (once for highlighting, once for rendering). Porter-AA4-008 can’t implement owner layering, and QA-AA4-009 has no reliable knob to assert overlay filtering.
- **Remediation Guidance:** Remove the ad-hoc `CollectSearchMarkers` path and render search overlays directly from `ModelDecoration`s belonging to `DecorationOwnerIds.SearchHighlights`. Extend the decoration metadata (from F1/F3) with capture/preview payloads so MarkdownRenderer can print e.g., `<<replace:TEXT>>` marks adjacent to `<…>` just like VS Code’s widget tooltips. Respect the existing `OwnerFilter` when printing search overlays and surface minimap/overview data inside the footer legend.
- **Validation Hooks:**
  - `MarkdownRendererTests.OwnerFilterHidesSearch` – prove that disabling the search owner removes `<`/`>` markers without touching other owners.
  - `MarkdownRendererTests.ReplacePreviewFromDecorations` – confirm capture/replace metadata is rendered using decoration data, not an ad-hoc search.
  - `Perf regression test` (benchmark) – ensure renderer no longer re-runs find for every frame and leverages cached decorations.

## Porter-CS & QA Dependencies
- **DocUI overlay semantics & owner layering (F1, F4):** Porter-AA4-008 must extend `ModelDecorationOptions` + renderer so search overlays carry minimap/overview/glyph metadata and honor `DecorationOwnerIds.SearchHighlights`. QA-AA4-009 needs snapshots covering minimap bars, glyph annotations, and owner-filter toggles.
- **Find-state controller & scope enforcement (F2):** Porter must introduce a persistent find controller/state, including scope ranges, match indices, and start-position tracking so DocUI can render scope shading + current match cues. QA should add controller-level tests mirroring VS Code’s loop + selection fixtures.
- **Replace preview & captureMatches (F3):** Porter has to port `ReplacePattern` + commands and pipe capture arrays into decorations so DocUI can display preview text. QA should extend `TextModelSearchTests`/`MarkdownRendererTests` with regex replace cases (backrefs, preserveCase, `$0` escapes).
- **End-to-end DocUI verification:** Once the above land, QA-AA4-009 should add Markdown snapshots that include search scope shading, current match brackets, replace preview labels, and owner-filter toggles, keeping `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` ≥ 92 green.

## References
- `ts/src/vs/editor/contrib/find/browser/findController.ts`
- `ts/src/vs/editor/contrib/find/browser/findModel.ts`
- `ts/src/vs/editor/contrib/find/browser/findDecorations.ts`
- `ts/src/vs/editor/contrib/find/browser/replacePattern.ts`
- `ts/src/vs/editor/common/model/textModelSearch.ts`
- `src/PieceTree.TextBuffer/TextModelSearch.cs`
- `src/PieceTree.TextBuffer/TextModel.cs`
- `src/PieceTree.TextBuffer/SearchHighlightOptions.cs`
- `src/PieceTree.TextBuffer/Decorations/ModelDecoration.cs`
- `src/PieceTree.TextBuffer/Rendering/MarkdownRenderer.cs`
