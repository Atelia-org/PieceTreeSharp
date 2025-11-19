# PT-010: PieceTree EOL Normalization & Builder Parity - Result

## Changes Implemented

### 1. `PieceTreeBuilder.cs`
*   Refactored to an instance class to maintain state during chunk acceptance.
*   Implemented `AcceptChunk` to handle BOM detection (UTF-8) and split CRLF sequences across chunks.
*   Implemented `Finish(bool normalizeEOL)` to detect dominant EOL and optionally normalize chunks.
*   Updated `BuildFromChunks` to use the new builder logic (defaulting to `normalizeEOL: false` to match existing test expectations).

### 2. `PieceTreeModel.cs`
*   Added `_eolNormalized` and `_eol` fields.
*   Updated constructor to accept `eolNormalized` and `eol`.
*   Implemented `NormalizeEOL(string eol)` which:
    *   Iterates over all pieces.
    *   Normalizes line endings to the target EOL.
    *   Rebuilds the tree with normalized chunks.
    *   Updates `_eolNormalized` to true.

### 3. `PieceTreeModel.Edit.cs`
*   Updated `Insert` to invalidate `_eolNormalized` if the inserted text contains line endings that conflict with the current normalized EOL.

### 4. `PieceTreeBuffer.cs`
*   Added `FromChunks` overload that accepts `normalizeEOL` parameter to support testing normalization.

## Test Results
*   Ran `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`.
*   All 23 tests passed (20 existing + 3 new normalization tests).
*   Added `src/PieceTree.TextBuffer.Tests/PieceTreeNormalizationTests.cs` covering:
    *   `delete CR in CRLF 1`
    *   `delete CR in CRLF 2`
    *   `Line breaks replacement is not necessary when EOL is normalized`

## Notes
*   `PieceTreeBuilder.BuildFromChunks` currently defaults to `normalizeEOL: false` because existing tests expect raw content preservation. Consumers who need normalization should use `PieceTreeBuilder` instance and call `Finish(true)`.
*   BOM handling was added but requires the first chunk to start with `\uFEFF`.
