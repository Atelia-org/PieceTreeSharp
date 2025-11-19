# PT-010: PieceTree EOL Normalization & Builder Parity

## Context
This task focuses on porting the EOL normalization logic from `PieceTreeBase.ts` to `PieceTreeModel.cs` and upgrading `PieceTreeBuilder.cs` to match `PieceTreeTextBufferBuilder.ts`.

## Analysis

### 1. `PieceTreeBase.ts` vs `PieceTreeModel.cs`
*   **`normalizeEOL(eol)`**: TS implementation rebuilds the tree to normalize line endings. It iterates through the tree, accumulates text into chunks (merging small chunks), replaces line endings with the target EOL, and then re-initializes the tree via `create`.
    *   **C# Status**: Missing. `PieceTreeModel.cs` currently lacks `NormalizeEOL`.
*   **`_EOLNormalized` Flag**: TS maintains an `_EOLNormalized` flag. This flag is used to optimize `getValueInRange` and `getLinesContent` (avoiding regex replaces if already normalized) and to skip CRLF checks in `insert`/`delete` if EOL is `\n` and normalized.
    *   **C# Status**: Missing. `PieceTreeModel` needs `_eolNormalized` field and logic updates in `Insert`/`Delete` to respect/update it.
*   **`create` / Re-initialization**: TS `normalizeEOL` calls `create` to reset the tree with new chunks.
    *   **C# Status**: `PieceTreeModel` constructor takes chunks, but we might need a `Rebuild` or `Reset` method, or `NormalizeEOL` should construct a new `PieceTreeModel` (though `PieceTreeTextBuffer` likely expects to mutate the existing one or swap it). In TS `PieceTreeBase` mutates itself.

### 2. `PieceTreeTextBufferBuilder.ts` vs `PieceTreeBuilder.cs`
*   **BOM Handling**: TS detects and strips BOM.
    *   **C# Status**: Missing.
*   **Split CRLF Handling**: TS handles the edge case where a chunk ends with `\r` and the next starts with `\n` using `_hasPreviousChar`.
    *   **C# Status**: Missing. `PieceTreeBuilder` treats chunks as independent.
*   **EOL Detection**: TS counts `cr`, `lf`, `crlf` during build to determine the dominant EOL.
    *   **C# Status**: Missing.
*   **Normalization during Build**: TS `finish` method accepts `normalizeEOL` boolean and creates a factory that normalizes chunks before creating the buffer.
    *   **C# Status**: Missing.

## Implementation Plan

### C# Changes
1.  **Update `PieceTreeBuilder.cs`**:
    *   Implement `AcceptChunk` logic with `_hasPreviousChar` to handle split CRLF.
    *   Implement BOM detection.
    *   Count `cr`, `lf`, `crlf`.
    *   Implement `Finish(bool normalizeEOL)` which returns a factory/result that can normalize chunks if needed.
2.  **Update `PieceTreeModel.cs`**:
    *   Add `_eolNormalized` field.
    *   Implement `NormalizeEOL(string eol)`. This should likely follow TS pattern: iterate, buffer text, replace EOLs, and rebuild the tree structure (or replace the root).
    *   Update `Insert` and `Delete` to handle `_eolNormalized` (e.g., `shouldCheckCRLF` logic).
    *   Update `GetValueInRange` (if ported) to use `_eolNormalized` optimization.

### Edge Cases
*   **Split CRLF**: `\r` at end of chunk N, `\n` at start of chunk N+1.
*   **Mixed EOLs**: File contains both `\r\n` and `\n`.
*   **Large Files**: `normalizeEOL` in TS chunks text to avoid huge strings. C# should likely do the same or use `StringBuilder` smarts.

## Tests to Port
From `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`:

1.  **CRLF Suite**:
    *   `delete CR in CRLF 1`
    *   `delete CR in CRLF 2`
    *   `random bug 1` (CRLF specific)
2.  **Normalization Optimization**:
    *   `Line breaks replacement is not necessary when EOL is normalized`
3.  **Builder Tests**:
    *   (If any exist specifically for builder edge cases like split CRLF, otherwise create new ones).

## TS Symbols
*   `PieceTreeBase.normalizeEOL`
*   `PieceTreeBase.create`
*   `PieceTreeTextBufferBuilder.acceptChunk`
*   `PieceTreeTextBufferBuilder.finish`
*   `PieceTreeTextBufferFactory.create`
