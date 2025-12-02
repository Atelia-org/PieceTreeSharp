# AA4-007 Result – CL7 Cursor Word/Snippet/Multi-Select

## Summary
Implemented CL7 parity features for cursor word navigation, multi-selection controller, column selection helpers, and snippet session/controller. Minimal implementations were ported from the TS logic to C#, with unit tests and MarkdownRenderer integration for DocUI visualization.

## Changes
- Added `CursorState`, `CursorContext`, and `CursorCollection` to manage multi-cursor scenarios (`src/PieceTree.TextBuffer/Cursor`).
- Implemented `WordCharacterClassifier` and `WordOperations` with basic word navigation (move/select/delete) semantics and unit tests (`src/PieceTree.TextBuffer/Cursor` and `CursorWordOperationsTests`).
- Added `CursorColumns` helpers to convert between visible columns and buffer positions (partial support only; tabs + injected text accounted in simplified ways).
- Added `SnippetSession` and `SnippetController` to parse simple `${n:placeholder}` snippets, insert placeholders as decorations and provide tabstop navigation APIs.
- Integrated multi-cursor creation into the `TextModel` via `CreateCursorCollection` and `CreateSnippetController` helpers.
- Extended `Cursor` with word movement/select/delete methods and minimal column/visibility support where applicable.
- Extended `MarkdownRenderer` tests with multi-cursor + snippet decorations visualization.

## Tests Added
- `CursorMultiSelectionTests` – tests for multiple cursors rendering and batch edits
- `CursorWordOperationsTests` – tests for move/select/delete word ops and custom separators
- `ColumnSelectionTests` – tests for visible column conversion and injected text adjustments
- `SnippetControllerTests` – tests for snippet insertion & tabstop placeholder navigation
- Augmented `MarkdownRendererTests` with `TestRender_MultiCursorAndSnippet` test.

## Test Results
- Ran: `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`
- Result: Passed 113 tests, 0 failed.

## Notes & Limitations
- This implementation is intentionally minimal. It uses a simplified `WordCharacterClassifier` that uses a `wordSeparators` string; locale-specific behavior and segmentation are not fully implemented.
- Column/Visible column conversions are approximated (handles tabs + injection lengths) but do not fully replicate TS injected-text-aware sticky column behaviors.
- `SnippetSession` supports basic `${n:placeholder}` insertion and placeholder navigation; more advanced features (choice completions, placeholder editing merging, placeholder undo semantics, and multi-cursor placeholder mapping) require further iteration.
- `CursorContext` only computes after-edit cursor states by returning the current active positions; advanced edit-time transformations and recovery heuristics are not implemented yet.

## Follow-ups
1. Port and integrate TS `cursorMoveOperations.ts` fully (wordPart/camelCase splits), add Intl.Segmenter fallback for locale-aware word boundaries.
2. Implement full column-selection semantics with `SelectionStartKind`, sticky visible columns across multi-cursors, and Alt+drag block selection rendering in MarkdownRenderer.
3. Extend `SnippetSession` to support `Choice` placeholders, placeholder editing merging, and proper undo/redo boundaries; integrate with suggestion/completion infra.
4. Add tests covering multi-cursor snippet insertion & deletion, and DocUI snapshots for complex cases (choice placeholders + multi cursors).

## Migration Log
- This change is recorded under migration logs and QA changefeed for the CL7 milestone. Please see `docs/reports/migration-log.md` and `agent-team/indexes/README.md#delta-2025-11-21` for references.

## QA Notes
- QA should validate multi-cursor behavior with Alt+Click (simulated via `CursorCollection`), `Ctrl+Arrow` movements, Delete Word operations, and snippet insertion placeholder navigation with multiple cursors present.
