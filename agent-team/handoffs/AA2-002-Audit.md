# Audit Report: TextModel/TextBuffer Advanced Hooks

**Task**: AA2-002  
**Date**: 2025-11-20  
**Auditor**: GitHub Copilot

## Scope & Inputs
- **TS references**: `ts/src/vs/editor/common/model/textModel.ts`, `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts`, supporting types under `ts/src/vs/editor/common/model.ts` and `ts/src/vs/editor/common/model/editStack.ts`.
- **C# references**: `src/PieceTree.TextBuffer/TextModel.cs`, `src/PieceTree.TextBuffer/Core/*` (notably `PieceTreeModel*.cs`), plus any undo/config helpers under `src/PieceTree.TextBuffer`.

## Findings Overview
| # | Topic | TypeScript Surface | C# Surface | Gap & Risk |
| :- | :-- | :-- | :-- | :-- |
| 1 | Undo / Redo stack APIs | `TextModel.pushStackElement/popStackElement/pushEditOperations/pushEOL/undo/redo` forward to `EditStack` + `IUndoRedoService` (see `textModel.ts` 1244-1601 and `editStack.ts` 394-475). Consumers (cursor, snippet, save participants, etc.) rely on `pushStackElement`. | `TextModel.cs` only exposes `ApplyEdits` (lines 96-149) and a single `OnDidChangeContent` event; there is no undo service, no stack element API, and no redo semantics. | **High** – editors embedding the C# port cannot define undo stop boundaries, cannot undo/redo, and cannot integrate with operations that expect stack discipline. |
| 2 | Per-model language/config options | TS models carry `TextModelResolvedOptions` (`model.ts` 559-640) + `TextModel.updateOptions/detectIndentation` + `onDidChangeOptions` ( `textModel.ts` 640-715, 140-205). Tab size, indent size, `insertSpaces`, default EOL, trim whitespace, bracket colorization all live per model. | C# has no option bag or setters. Aside from a stored `_eol` string (never normalized) there is no way to configure tab size, indentation, or trimming. No change event exists for options. | **High** – features like formatter parity or language-service derived indentation cannot be honored; consumers cannot react to option changes as TS extensions do. |
| 3 | EOL toggles + normalization hooks | TS exposes `pushEOL`/`setEOL`, integrates with undo stack (`textModel.ts` 1234-1268) and normalizes edits inside `_commandManager.pushEOL`. Undo snapshots treat EOL changes as stack elements, and events fire while batching decoration/content changes. | C# exposes a read-only `Eol` property initialized once (lines 70-78) and never mutates the buffer or fires events when changed. No API lets callers toggle `\n`/`\r\n` without rebuilding the model. | **Medium** – editing scenarios that need to respect workspace `files.eol` or quickly toggle default EOL cannot do so, and undo history cannot record such toggles. |
| 4 | Option / language change events | TS `TextModel` wires `onDidChangeOptions`, `onDidChangeLanguage`, `onDidChangeLanguageConfiguration`, plus decoration/tokenization emitters (lines 140-220). Many contributions depend on these to refresh tokenization and guides. | C# only raises `OnDidChangeContent`; there is no language identity, no options change event, and no guide/tokenization events. | **Medium** – downstream components cannot listen for configuration or language changes, blocking parity with TS services that rely on those hooks. |

### Detail per Question
1. **Undo/Redo APIs**: `TextModel` owns an `EditStack` (`_commandManager`, `textModel.ts` 367-395) and simply forwarding `pushStackElement()` is enough for dozens of features (search, multi-cursor, snippet controller, save participants) that call `model.pushStackElement()` in TS sources such as `ts/src/vs/editor/browser/coreCommands.ts`. None of these hooks exist in `TextModel.cs`, so even basic typing cannot create undo stops.
2. **Language configuration**: `TextModel.resolveOptions` (`textModel.ts` 179-216) and `updateOptions` allow per-model overrides and emit `onDidChangeOptions` so bracket pairs, guides, and decorations respond. The C# port lacks `TextModelResolvedOptions`, `updateOptions`, `detectIndentation`, or normalization helpers, so consumers cannot set `tabSize`, `insertSpaces`, or `trimAutoWhitespace` at all.
3. **EOL toggles**: In TS the recommended path to change EOL is `pushEOL`, which wraps `_commandManager.pushEOL` (editStack) ensuring undo snapshots and content/decorations events are batched. C# stores `_eol` but never mutates it or the underlying buffer, so toggling the file-wide EOL means recreating the model outside the undo history.
4. **Events**: TS raises dedicated events for language, options, decorations, tokens, injected text, etc. (`textModel.ts` 140-230). `TextModel.cs` defines only `event EventHandler<TextModelContentChangedEventArgs>? OnDidChangeContent;`, so host services cannot observe option or language transitions, nor can they plug in tokenization.

## Proposed Tests (add under `PieceTree.TextBuffer.Tests`)
1. **Undo/Redo surface parity**: Arrange C# `TextModel`, call `PushStackElement`, apply edits, then call `Undo`/`Redo` to verify content + `AlternativeVersionId` mirror TS behavior. Match TS expectations by porting scenarios from `textModel.test.ts` and `cursor.test.ts` that assert undo stops after multi-cursor operations.
2. **Options round-trip**: Add tests that mutate `tabSize`, `indentSize`, `insertSpaces`, and `trimAutoWhitespace` via a new `UpdateOptions` API, assert `GetOptions()` reflects the change, and that an `OnDidChangeOptions` event fires with the same flags as TS `IModelOptionsChangedEvent`.
3. **EOL toggle integration**: Write a test that calls `PushEol(EndOfLineSequence.CRLF)` (new API), confirms the buffer updates line endings without rebuilding, and that the change is undoable/redone plus triggers a `OnDidChangeContent` notification.
4. **Language/options events**: Once language identity exists, add tests similar to TS `modelService.test.ts` ensuring `SetLanguage` and `UpdateOptions` both fire their respective events exactly once and deliver the expected payloads.

## Remediation Priority
1. **Implement EditStack plumbing (High)**: Port `EditStack` + `_commandManager` integration, surface `PushStackElement/PushEditOperations/Undo/Redo/CanUndo/CanRedo`, and wire changes into `OnDidChangeContent` so embedding editors can rely on consistent undo semantics.
2. **Add per-model options & events (High)**: Introduce `TextModelResolvedOptions` in C#, expose `GetOptions`/`UpdateOptions`/`DetectIndentation`, and raise `OnDidChangeOptions`. This unblocks indentation-aware services and ties into the undo/EOL work.
3. **Support EOL toggles (Medium)**: Implement `PushEol` + pure `SetEol` (without undo) mirroring TS semantics, ensuring the buffer normalizes `\r\n` vs `\n` consistently and fires the right events.
4. **Language + auxiliary events (Medium)**: Track `LanguageId` on the model, expose `OnDidChangeLanguage`/`OnDidChangeLanguageConfiguration`, and ensure consumers can subscribe before adding future services (tokenization, bracket guides, etc.).

_No code changes were made; this deliverable documents the audit findings only._
