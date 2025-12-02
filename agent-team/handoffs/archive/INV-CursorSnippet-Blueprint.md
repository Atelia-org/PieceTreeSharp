# INV-CursorSnippet-Blueprint
_By: Investigator Callie Stone • Date: 2025-11-26 • Scope limit: Cursor/Snippet stack (<60K tokens)_

## 1. Context & goals
The C# cursor/snippet stack (`src/TextBuffer/Cursor/*.cs`) diverged from VS Code's architecture (`ts/src/vs/editor/common/cursor/**`, `ts/src/vs/editor/contrib/snippet/browser/**`). The objective is to re-establish structural parity so future fixes/features can be shared verbatim. This document enumerates the architectural requirements, enumerates missing components, captures cross-cutting dependencies, and outlines a staged rollout plus deterministic test expectations.

## 2. Model vs view state architecture requirements
1. **Dual-state cursors.** Mirror `ts/src/vs/editor/common/cursor/oneCursor.ts`: every cursor instance must own both a `SingleCursorState` for the model and one for the view. Each state carries `selectionStart`, `SelectionStartKind`, `selectionStartLeftoverVisibleColumns`, `position`, and `leftoverVisibleColumns`. C# `Cursor.cs` currently stores a single `Selection`, so editing cannot keep sticky columns or selection origins aligned when decorations/injected text shift view columns.
2. **Authoritative `CursorContext`.** Port `CursorContext` so it bundles `TextModel`, `ViewModel` (implements `ICursorSimpleModel`), an `ICoordinatesConverter`, and the active `CursorConfiguration`. All cursor operations—including movement, word nav, and column select—must go through these abstractions instead of calling `TextModel` directly. This is required to keep view-relative operations (wrapping, rtl, injected text) in sync with the model.
3. **Tracked range plumbing.** Every cursor needs a tracked range ID (see `_selTrackedRange` in `oneCursor.ts`) to survive edits. The context must expose `_setTrackedRange`/`_getTrackedRange` equivalents on `TextModel`. Stickiness must default to `TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges` to match snippet/tabstop semantics.
4. **Normalization entrypoints.** Implement `_setState` logic exactly like TS: validate inputs, normalize via the view model, recompute counterpart state if missing, clamp leftover visible columns, and update tracked ranges. The validator must leverage `coordinatesConverter.convertModelPositionToViewPosition` and `convertViewPositionToModelPosition`. Without this loop higher-level commands (column select, drag, snippet) cannot safely set cursor states.
5. **CursorCollection contract.** The collection must own a primary cursor and N secondary ones, expose `setStates`, `getAll`, `readSelectionFromMarkers`, `startTrackingSelections`, `stopTrackingSelections`, `getViewPositions`, and `normalize`. Normalize must respect `CursorConfiguration.multiCursorMergeOverlapping` semantics to merge touching/overlapping ranges. The C# list-based holder must be replaced by the TS parity implementation.
6. **Column selection + visible column math.** Column select commands must call `CursorConfiguration.columnFromVisibleColumn` and `visibleColumnFromColumn` so page-up/down, rtl, injected text, and sticky tab stops behave identically. The helper that loops through `TextModel.GetLineContent` is insufficient because it bypasses view metadata.
7. **Command plumbing.** Cursor move, delete, and type commands are defined in TS (`cursorMoveOperations.ts`, `cursorDeleteOperations.ts`, etc.) and expect `CursorCollection` + `CursorConfiguration`. While the current investigation focuses on architecture, the parity design must leave space for these command helpers to land without refactoring again.

## 3. Missing components and targeted reintroductions
### 3.1 CursorConfiguration (absent in C#)
- **Responsibility:** expose editor + model options (tabSize, indentSize, insertSpaces, stickyTabStops, wordSeparators, multiCursor settings, autoClosing/autoSurround, pageSize, electric chars, overtype). It also owns helpers `visibleColumnFromColumn` and `columnFromVisibleColumn` that wrap `CursorColumns` logic while enforcing line min/max columns.
- **Implementation plan:** port `CursorConfiguration` from `ts/src/vs/editor/common/cursorCommon.ts`. Wire it to `TextModelResolvedOptions` and `TextModelOptions` (C#) plus an editor-level configuration object (existing options live in `TextModelOptions` and `TextModel`). Ensure `ILanguageConfigurationService` parity exists (or stub) for auto-closing/electric support. This config becomes part of `CursorContext` and is cached per model version.

### 3.2 SingleCursorState normalization + partial states
- **Requirements:** introduce `SingleCursorState`, `SelectionStartKind`, `CursorState`, `PartialModelCursorState`, `PartialViewCursorState` identical to TS. Update `CursorState.cs` to carry both model and view states instead of the decoration-only record. Implement `CursorState.fromModelSelection(s)` for command interop.
- **Normalization:** clone `_setState` from TS to: (a) validate incoming model/view ranges using `TextModel.ValidateRange/ValidatePosition`, (b) recompute the missing side via `coordinatesConverter`, and (c) update tracked ranges. Sticky column info (`leftoverVisibleColumns`) must survive undo/redo and snippet jumps.
- **Selection tracking:** `Cursor.cs` can no longer own `_selection`; instead, commands mutate states and rely on `CursorCollection.setStates`. Decorations move to a lightweight cursor decoration service keyed by `ownerId` but driven by cursor states, not bespoke fields.

### 3.3 Tracked range plumbing & CursorContext integration
- **Model hooks:** `TextModel` already has decoration APIs; extend it with tracked range helpers (`AllocateTrackedRangeId`, `_setTrackedRange`, `_getTrackedRange`). These map to hidden decorations with invisible options.
- **CursorContext responsibilities:** funnel `TextModel`, `ICursorSimpleModel` (view side), `ICoordinatesConverter`, and `CursorConfiguration`. Provide `ComputeAfterCursorState(inverseChanges)` that asks each cursor to read its tracked range, revalidate, and rebuild states—matching `cursorContext.ts`.
- **Lifecycle:** `CursorCollection.startTrackingSelections()` is called before edits, `stopTrackingSelections()` after. Undo/redo flows keep consistent tracked ranges so snippet controllers and multi-step commands can restore selections.

### 3.4 Snippet lifecycle completeness
- **Controller parity:** port `SnippetController2` features: context keys (`InSnippetMode`, `HasNextTabstop`, `HasPrevTabstop`), undo stop management, insert/merge paths, choice completion provider registration, `finish`, `cancel(resetSelection)`, and `isInSnippetObservable`. Map to editor services in C# (likely `TextBuffer.Services` or `DocUI`).
- **Session structure:** split `SnippetSession` into `SnippetSession` + `OneSnippet` equivalents. `OneSnippet` manages placeholder decorations, transforms, merges, and `computePossibleSelections`. `SnippetSession` orchestrates insertion, merge, navigation, and enclosure computations. Support `ISnippetInsertOptions` (`overwriteBefore/After`, `adjustWhitespace`, `undoStop*`, `clipboardText`, `overtypingCapturer`, `reason`).
- **Parser & variables:** reuse the existing TypeScript logic by porting `SnippetParser`, `TextmateSnippet`, `Placeholder`, `Choice`, and variable resolvers (model, clipboard, selection, comment, time, workspace, random). Without these, snippet placeholders cannot mirror TS behavior, and deterministic tests in §08 cannot pass.
- **Lifecycle interplay with cursors:** snippet navigation relies on `CursorCollection.setStates` and `computePossibleSelections`; placeholders track ranges with `TrackedRangeStickiness` (active vs inactive). Ensure the BF1 fix (sentinel `_current = _placeholders.Count`) remains while integrating the full feature set.

## 4. Dependencies on Range/Selection helpers & word operations
1. **Range helpers:** Cursor normalization, snippet placeholder math, and `CursorCollection.normalize` depend on `Range.containsPosition`, `Range.plusRange`, `Range.intersectRanges`, and comparison helpers. Coordination with Workstream 2 is mandatory: ensure `src/TextBuffer/Core/Range*.cs` exposes the same APIs as `ts/src/vs/editor/common/core/range.ts`. Without them we cannot port TS algorithms verbatim.
2. **Selection helpers:** `Selection.fromRange`, `Selection.getDirection`, delta helpers, and `Selection.equalsSelection` must exist. Several TS paths expect `Selection.liftSelection` semantics. Update `src/TextBuffer/Core/Selection.cs` accordingly before wiring cursor states.
3. **Word helpers:** `WordCharacterClassifier.cs` and `WordOperations.cs` require the full TS surface (Intl aware segmentation, camelCase/wordPart navigation, deletion contexts). Cursor move commands call into `WordOperations.moveWord*`, snippet placeholders rely on `wordPart` motion when deciding default tabstop selection. Port `WordOperations` plus `WordPartOperations` to keep behavior deterministic across languages.
4. **Column conversions:** `CursorColumns` must expose `columnSelect*` logic built on `CursorConfiguration`. Additional dependencies include `ICursorSimpleModel.getLineMinColumn/getLineMaxColumn`, `getLineFirstNonWhitespaceColumn`, etc. Reuse the TS `ColumnSelection` verbatim to prevent drift.

## 5. Migration strategy (feature flag + rollout)
1. **Stage 0 – Foundations (Dec 2 target).**
   - Land `CursorConfiguration`, `SingleCursorState`, tracked range helpers, and the new `CursorContext`. Guard the new stack behind `TextModelOptions.EnableVsCursorParity` (default false). Provide shims so existing `Cursor.cs` continues to function when the flag is off.
   - Introduce parity-friendly abstractions (`ICoordinatesConverter`, `ICursorSimpleModel`). Even if the view model layer is incomplete, stub implementations can proxy to `TextModel` until DocUI supplies a real view model.
2. **Stage 1 – CursorCollection + movement (Dec 9 target).**
   - Replace `Cursor.cs`/`CursorCollection.cs` logic with TS equivalents under the flag. Implement column selection, word navigation hooks, and normalization. Provide dual pathways: the old API (for consumers not yet migrated) and the new `CursorController` for feature-flagged scenarios.
   - Update deterministic cursor tests to call both implementations (flag on/off) to validate parity.
3. **Stage 2 – Snippet lifecycle (Dec 13 target).**
   - Port `SnippetController`/`SnippetSession`, integrate with the new cursor stack, and ensure `Next/Prev` tabstop commands use `CursorCollection`. Keep the feature flag engaged so QA can compare old vs new snippet behavior.
   - Hook context keys or equivalent state so DocUI and command routing know when snippet mode is active.
4. **Stage 3 – Flag bake & removal (Dec 20+).**
   - Run deterministic + fuzz suites under the flag for multiple CI cycles. Once green + manual sign-off from QA (Lena Brooks), flip the feature flag default to on and schedule removal of legacy cursor codepaths in January.

## 6. Deterministic test requirements
Align with `tests/TextBuffer.Tests` backlog noted in the ALIGN plan:
- **Cursor core (`CursorTests.cs`, `CursorMultiSelectionTests.cs`).** Cover dual-state normalization (model vs view), tracked range recovery after insert/delete, sticky column persistence, `CursorCollection.normalize` merging, and `CursorColumns.columnSelect*` outputs. Each test should assert both model and view coordinates plus tracked range IDs.
- **Word navigation (`CursorWordOperationsTests.cs`, `ColumnSelectionTests.cs`).** Validate `word` vs `wordPart` motion across ASCII, camelCase, emoji, combined grapheme clusters, and injected text scenarios. Include delete/selection variants.
- **Snippet lifecycle (`SnippetControllerTests.cs`).** Add tests for `insert`, `merge`, `cancel`, `finish`, `choice` completions, placeholder transforms, whitespace adjustment, clipboard variables, and undo/redo boundaries. Use deterministic templates mirrored from TS tests.
- **Snippet multi-cursor fuzz (`SnippetMultiCursorFuzzTests.cs`).** Port the TS fuzz harness seeds for nested snippets, verifying no infinite loops (BF1) and consistent cursor restoration.
- **Regression harness integration.** Each deterministic suite must run under both flag states until legacy paths are deleted. Capture golden traces (model/view selections, placeholder ranges) in `tests/TextBuffer.Tests/TestMatrix.md` to document TS parity assumptions.

## 7. Risks & mitigations
- **Range/Selection backlog coupling.** Workstream 2 delivers helper parity; without it we cannot finish `_setState`. Mitigation: sync milestone dates so helper APIs land before Stage 1.
- **View model availability.** Currently only `TextModel` exists. Short-term mitigation is to implement an `IdentityCoordinatesConverter` that assumes 1:1 model/view mapping until DocUI view model ports arrive; structure code so swapping in the real converter later is non-breaking.
- **Snippet parser complexity.** Porting the TextMate snippet parser is non-trivial. Consider temporarily reusing the TS parser via TypeScript transpilation into .NET (e.g., through TypeScript AST to C#) or designate a dedicated porting sprint.

## 8. Deliverables checklist
- [ ] `CursorConfiguration` + dependent services in `src/TextBuffer/Cursor/CursorConfiguration.cs` (new file).
- [ ] `SingleCursorState` + `CursorState` parity implementation updating `CursorState.cs` and `Cursor.cs`.
- [ ] `CursorCollection` reimplementation with normalization + tracked ranges.
- [ ] `CursorColumns` column select suite + `IColumnSelectResult`/`IColumnSelectData` structs.
- [ ] Snippet controller/session parity including parser, variables, and placeholder transforms.
- [ ] Feature flag plumbing + documentation in `docs/reports/alignment-audit/03-cursor.md` addendum upon completion.
- [ ] Deterministic test suites outlined above with TS-linked expectations recorded in `TestMatrix`.
