# AA-003 Audit Report: Search and Cursor Logic

**Date:** November 19, 2025
**Auditor:** GitHub Copilot
**Scope:** Comparison of TypeScript (VS Code) and C# (PieceTree) implementations of Search and Cursor logic.

## 1. Executive Summary

The audit reveals significant gaps in the C# implementation compared to the TypeScript reference. While the core data structures (`PieceTree`, `Selection`) are present, the interaction logic for Search and Cursor movement is largely incomplete or simplified in C#.

**Key Findings:**
- **Search:** Word separator logic is stubbed out. Regex handling is basic.
- **Cursor:** "Sticky" column memory (virtual space) is missing. Cursor movement methods do not support selection extension (always collapse). Tab expansion for visual columns is absent.

## 2. Gap Analysis

### 2.1 Search Logic

| Feature | TypeScript (`textModelSearch.ts`) | C# (`PieceTreeSearcher.cs`) | Status |
| :--- | :--- | :--- | :--- |
| **Regex Engine** | Uses JS `RegExp` with `strings.createRegExp` handling case, multiline, global, unicode. | Uses .NET `Regex`. | ⚠️ **Partial** (Different engines, potential syntax parity issues). |
| **Multiline** | Explicit detection via `isMultilineRegexSource`. Handles CRLF/LF normalization. | Relies on .NET Regex multiline capabilities. No explicit EOL normalization logic seen in searcher. | ⚠️ **Partial** |
| **Word Separators** | Complex `WordCharacterClassifier`. Checks boundaries (left/right) and inner match characters. | **Stubbed out**. Code exists but is commented out (`// Word separator check stub`). | ❌ **Missing** |
| **Capture Groups** | `createFindMatch` explicitly extracts capture groups into `FindMatch` object. | Returns `Match` object. Caller must handle groups. No explicit mapping utility. | ⚠️ **Partial** |

### 2.2 Cursor Logic

| Feature | TypeScript (`cursor.ts`, `cursorMoveOperations.ts`) | C# (`Cursor.cs`) | Status |
| :--- | :--- | :--- | :--- |
| **Movement (Basic)** | Handles atomic moves, surrogate pairs (via `strings.prevCharLength`). | Basic column increment/decrement. | ⚠️ **Partial** |
| **Sticky Column** | Tracks `leftoverVisibleColumns` to preserve horizontal position when moving vertically across lines of varying lengths. | **No tracking**. Resets column to line length if shorter. Lost when moving back to longer line. | ❌ **Missing** |
| **Virtual Spaces** | `CursorColumns.visibleColumnFromColumn` handles tab expansion (visual vs model column). | Treats columns as raw character indices. No tab expansion logic. | ❌ **Missing** |
| **Selection Direction** | Preserved via `Selection` object. Movement methods support `inSelectionMode` flag to extend/shrink selection. | Preserved in `Selection` struct (`Anchor`/`Active`). **BUT** `Move*` methods (Left/Right/etc.) always call `MoveTo` which **collapses** the selection. No `SelectLeft` etc. | ❌ **Missing** (Interaction logic) |
| **Atomic Tabs** | Supports atomic soft tabs (jumping over spaces as one unit). | No support. | ❌ **Missing** |

## 3. Risk Assessment

- **High Risk:**
  - **Cursor UX:** Users expect the cursor to "remember" its column when moving up/down. The current C# implementation will frustrate users by resetting the cursor position constantly.
  - **Selection Loss:** The inability to extend selections via keyboard (Shift+Arrow) using the `Cursor` class methods is a blocker for any editor input handling.
  - **Search Accuracy:** Missing word separator logic means "Whole Word" search will fail or behave incorrectly, leading to false positives.

- **Medium Risk:**
  - **Regex Parity:** Differences between JS and .NET Regex engines might cause subtle bugs for complex user queries.

## 4. Remediation Plan

### Phase 1: Cursor Fundamentals (Critical)
1.  **Implement `CursorMoveOperations` in C#:**
    -   Port `leftoverVisibleColumns` logic.
    -   Implement `visibleColumnFromColumn` (Tab expansion).
2.  **Update `Cursor` Class:**
    -   Add `SelectLeft`, `SelectRight`, `SelectUp`, `SelectDown` methods (or `bool select` overloads).
    -   Ensure `Move*` methods use the new `CursorMoveOperations` logic instead of direct manipulation.

### Phase 2: Search Parity
1.  **Implement Word Separators:**
    -   Port `WordCharacterClassifier` and `isValidMatch` logic to C#.
    -   Uncomment and hook up the check in `PieceTreeSearcher`.
2.  **Capture Group Handling:**
    -   Ensure the search result object in C# exposes capture groups in a way compatible with the consumer (likely `FindMatch` equivalent).

### Phase 3: Advanced Cursor
1.  **Atomic Soft Tabs:** Implement logic to handle soft tabs as single units.
2.  **Surrogate Pairs:** Ensure movement respects surrogate pairs (don't split emojis).

## 5. Conclusion

The C# `PieceTree.TextBuffer` component is currently a data structure foundation but lacks the sophisticated editing behaviors of VS Code. Significant effort is required to port the "business logic" of cursor movement and search validation to achieve parity.
