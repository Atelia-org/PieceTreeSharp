# AA3-001 Audit – CL1 TextModel Options & Events

**Date:** 2025-11-20  
**Investigator:** GitHub Copilot  
**Scope:** TS `ts/src/vs/editor/common/model/textModel.ts` & `textModelSearch.ts` vs C# `src/PieceTree.TextBuffer/TextModel.cs`, `TextModelOptions.cs`, `EditStack.cs`, `TextModelSearch.cs`.  
**Baseline Dependencies:** AA2-005 / AA2-006 deliver Undo/Redo foundation and PieceTree search parity; reused without re-audit.

## Overview
- Reviewed creation options, language configuration wiring, attachment events, and search interfaces at the `TextModel` layer.  
- Confirmed PieceTreeSearcher behaviors were already covered in AA2-006; scope limited to `TextModel` entry points and option resolution.  
- Identified five blocking gaps (two High, three Medium) that prevent the C# port from matching the TS model contract required by CL1.

## Findings

### F1 – Creation Options & Resolved Options Not Parity (High)
- **TS Reference:** `ts/src/vs/editor/common/model/textModel.ts#L74-L210` (`DEFAULT_CREATION_OPTIONS`, `TextModel.resolveOptions`).  
- **C# Reference:** `src/PieceTree.TextBuffer/TextModel.cs#L45-L90`, `src/PieceTree.TextBuffer/TextModelOptions.cs#L14-L84`.  
- **Gap:** TS resolves per-model options such as `detectIndentation`, `indentSize`, `trimAutoWhitespace`, `defaultEOL`, and `bracketPairColorizationOptions`, respecting `largeFileOptimizations` and `isForSimpleWidget`. The C# constructor hard-codes `TextModelResolvedOptions.CreateDefault` (tabSize 4, insertSpaces true, trimAutoWhitespace false) and exposes no creation options. The manual `DetectIndentation` helper is never invoked automatically, and there is no storage for bracket colorization or large-file flags.  
- **Impact:** Editors opening files with custom indentation settings, default EOL preferences, or bracket colorization configs will diverge immediately, meaning Porter-CS cannot round-trip settings-driven scenarios or honor workspace defaults.  
- **Suggested Fix:** Introduce a `TextModelCreationOptions` struct mirroring TS (detectIndentation, tabSize/indentSize, insertSpaces, trimAutoWhitespace, defaultEOL, largeFileOptimizations, bracketPairColorizationOptions, isForSimpleWidget). Port `TextModel.resolveOptions` logic (including `guessIndentation`) and persist auxiliary flags on the C# model so downstream components can read them.  
- **Test Hooks:** Extend `TextModelTests` to cover creation with custom tab sizes, detect-indentation toggles, trim-auto-whitespace persistence, and default EOL transitions.

### F2 – Language Configuration Events Not Wired (High)
- **TS Reference:** `ts/src/vs/editor/common/model/textModel.ts#L214-L320` (language change events, registration with `ILanguageConfigurationService`).  
- **C# Reference:** `src/PieceTree.TextBuffer/TextModel.cs#L209-L250` (`SetLanguage`) – only raises `OnDidChangeLanguage`.  
- **Gap:** TS exposes `onDidChangeLanguageConfiguration`, keeps `TokenizationTextModelPart`, `BracketPairsTextModelPart`, `GuidesTextModelPart`, and listens to the language configuration service to refresh metadata. The C# port lacks any configuration-service bridge, has no `onDidChangeLanguageConfiguration`, and therefore cannot react when language-specific indentation, brackets, or auto-closing rules change.  
- **Impact:** Features such as updated bracket pairs, indentation rules, or guide rendering never refresh, and consumers cannot observe configuration-updates, causing stale metadata after extensions modify language settings.  
- **Suggested Fix:** Add a lightweight language-configuration service abstraction on the C# side (even if backed by callbacks) so `TextModel` can register, expose `OnDidChangeLanguageConfiguration`, and notify bracket/guide/tokenization components. Ensure `requestRichLanguageFeatures` equivalent is called when language id changes.  
- **Test Hooks:** Add unit tests ensuring language configuration change callbacks trigger `OnDidChangeLanguageConfiguration` and update dependent metadata caches.

### F3 – Attached Editor Lifecycle Missing (Medium)
- **TS Reference:** `ts/src/vs/editor/common/model/textModel.ts#L214-L320` (`_onDidChangeAttached`, `_attachedEditorCount`, `AttachedViews`).  
- **C# Reference:** No attach/detach tracking in `src/PieceTree.TextBuffer/TextModel.cs`.  
- **Gap:** TS maintains `_attachedEditorCount`, fires `onDidChangeAttached`, and keeps `AttachedViews` for tokenization/view state. The C# model exposes no API for clients to signal attach/detach, so features that depend on attachment state (lazy tokenization, decoration lifetimes, textModel disposal heuristics) cannot be implemented.  
- **Impact:** The runtime cannot know when a model is actively viewed, so it cannot delay expensive work until an editor attaches, nor can it release resources when the last view detaches—leading to either wasted work or missing refresh events.  
- **Suggested Fix:** Mirror `addRef`/`release` semantics: track an `AttachedEditorCount`, raise an `OnDidChangeAttached` event, and forward attachment state into any future tokenization/bracket providers.  
- **Test Hooks:** Add tests confirming attach/detach transitions increment/decrement counts and fire events exactly once per boundary crossing.

### F4 – EditStack Lacks Undo/Redo Service Integration (High)
- **TS Reference:** `ts/src/vs/editor/common/model/editStack.ts` (integration with `IUndoRedoService`, `SingleModelEditStackElement`, grouped redo entries, cursor state preservation).  
- **C# Reference:** `src/PieceTree.TextBuffer/EditStack.cs`.  
- **Gap:** The TS edit stack persists `Selection` info, groups edits under labels, interoperates with workspace-level undo/redo, and records EOL/state deltas so redo can be replayed across models. The C# edit stack is an isolated LIFO list lacking grouping, labels, cursor state, or integration with a global undo service. `TextModel.PushEol` in C# cannot participate in undo grouping, and multi-model edits (common in rename/refactor) are unsupported.  
- **Impact:** Undo/redo semantics diverge: cross-model edits cannot be grouped, redo loses cursor state, and features such as `UndoRedoGroup`, snapshot restore, or extension-provided labels are impossible—failing CL1’s parity requirement.  
- **Suggested Fix:** Port `EditStack`’s cooperation with an `IUndoRedoService` analogue: create `SingleModelEditStackElement` equivalents that capture text changes, selections, and EOL transitions, expose `PushStackElement/PopStackElement` semantics, and ensure `TextModel.pushEditOperations` records changes through the shared service.  
- **Test Hooks:** Extend `TextModelTests` / new `EditStackTests` to cover grouped undo, redo after EOL changes, and cursor-state restoration.

### F5 – Search Scope API Omits Multi-Range Support (Medium)
- **TS Reference:** `ts/src/vs/editor/common/model/textModel.ts#L1115-L1185` (`findMatches` handling of `rawSearchScope`, merging overlapping ranges).  
- **C# Reference:** `src/PieceTree.TextBuffer/TextModel.cs#L120-L190` (`FindMatches`).  
- **Gap:** TS accepts boolean or multiple `IRange` scopes, normalizes/merges them, and only searches inside those regions (used by "find in selection" and editable-range filtering). The C# API only supports an optional single `Range`, so callers cannot pass multiple disjoint selections or signal the "use selections" boolean path, and overlap deduplication is impossible.  
- **Impact:** Any feature relying on multi-range searches (multi-cursor find, search in selection, diff-side filtering) cannot be implemented on the C# TextModel, limiting editor parity.  
- **Suggested Fix:** Add an overload or argument that accepts `IEnumerable<Range>` plus the boolean flag, port the range-normalization logic from TS, and ensure it flows into `TextModelSearch`.  
- **Test Hooks:** `TextModelSearchTests` should cover multi-range selection searches and ensure overlapping ranges do not duplicate matches.

## Suggested Fixes
1. Implement `TextModelCreationOptions` + `TextModel.resolveOptions` parity (covers F1).  
2. Introduce a language-configuration service bridge and expose `OnDidChangeLanguageConfiguration` (F2).  
3. Add attached-editor lifecycle APIs/events and propagate into future tokenization parts (F3).  
4. Port the TS edit stack’s integration with an undo/redo service, including cursor/EOL state capture (F4).  
5. Expand `TextModel.FindMatches/FindNextMatch` signatures to accept multi-range scopes and deduplicate them (F5).

## Impacts
- Without option parity, models ignore workspace/user indentation defaults, leading to persistent formatting drift.  
- Lack of language-configuration wiring blocks bracket pair, guide, and tokenization updates after extension changes.  
- Missing attachment events hinder resource management and can lead to regressions in lazy tokenization strategies.  
- Undo/redo behavior deviates from VS Code expectations, breaking composite commands and redo-cursor consistency.  
- Search UI features (find in selection, multi-range find/replace) cannot be implemented against the current API surface.

## Changefeed Impact
- Info-Indexer should queue `src/PieceTree.TextBuffer/TextModel.cs`, `TextModelOptions.cs`, `EditStack.cs`, `TextModelSearch.cs`, and `tests/TextModelTests.cs` for the next `agent-team/indexes/README.md#delta-2025-11-20` entry once fixes land.  
- Porter-CS should note any new test files (e.g., `TextModelSearchTests`) so AA3-003 can register them in the changefeed.

## References
- `ts/src/vs/editor/common/model/textModel.ts`  
- `ts/src/vs/editor/common/model/editStack.ts`  
- `ts/src/vs/editor/common/model/textModelSearch.ts`  
- `src/PieceTree.TextBuffer/TextModel.cs`  
- `src/PieceTree.TextBuffer/TextModelOptions.cs`  
- `src/PieceTree.TextBuffer/EditStack.cs`  
- `src/PieceTree.TextBuffer/TextModelSearch.cs`

## Next Steps for Porter-CS
1. Scope and implement creation-option parity plus unit tests (F1).  
2. Add language-configuration wiring alongside attach/detach tracking (F2, F3).  
3. Port EditStack/undo service integration and validate via new tests (F4).  
4. Extend search APIs to handle multi-range scopes and add coverage in `TextModelSearchTests` (F5).  
5. Coordinate with Info-Indexer to log updated files/tests once patches are staged.
