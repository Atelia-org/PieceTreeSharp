# Audit Report: TextModel & PieceTreeBuffer (AA-002)

**Date**: 2025-11-19
**Auditor**: GitHub Copilot
**Scope**: Comparison of TypeScript `TextModel`/`PieceTreeTextBuffer` vs C# `TextModel`/`PieceTreeBuffer`.

## Executive Summary
The C# implementation of `TextModel` and `PieceTreeBuffer` is currently a functional prototype but lacks the performance characteristics and feature completeness of the TypeScript reference. The most critical issue is that `PieceTreeBuffer` flattens the entire text buffer into a string for line/column operations, negating the performance benefits of the Piece Table data structure.

## Gap Analysis

### 1. Line Starts & Buffer Performance
| Feature | TypeScript (`PieceTreeTextBuffer`) | C# (`PieceTreeBuffer`) | Gap Severity |
| :--- | :--- | :--- | :--- |
| **Line/Column Lookup** | Uses RB-Tree metadata (`lf_left`) for $O(\log N)$ access. | Flattens text to string & builds `LineStartTable` ($O(N)$). | **Critical** |
| **GetLineContent** | Retrieves only relevant chunks from tree. | Rebuilds entire string snapshot first. | **Critical** |
| **Tree Metadata** | `PieceTreeNode` tracks line feeds. | `PieceTreeNode` tracks line feeds, but `PieceTreeBuffer` ignores them. | High |

**Observation**: `PieceTreeModel.cs` (internal) has the necessary `LineFeedsLeft` and `AggregatedLineFeeds` fields to support efficient lookups, but the public `PieceTreeBuffer` class ignores them, opting for a naive string-based approach.

### 2. BOM & EOL Handling
| Feature | TypeScript | C# | Gap Severity |
| :--- | :--- | :--- | :--- |
| **BOM Preservation** | Explicitly handled in `PieceTreeTextBuffer` and `TextModel`. | `PieceTreeBuffer` has `CreateSnapshot(bom)` but `TextModel` lacks explicit BOM management. | Medium |
| **EOL Normalization** | Configurable (LF/CRLF/TextDefined). `applyEdits` normalizes inserted text. | `PieceTreeModel.NormalizeEOL` exists. `TextModel.GetLineContent` does manual/fragile stripping. | Medium |

### 3. Versioning & Undo/Redo
| Feature | TypeScript | C# | Gap Severity |
| :--- | :--- | :--- | :--- |
| **Version IDs** | `versionId` (monotonic), `alternativeVersionId` (undo-aware). | `versionId` & `alternativeVersionId` both monotonic. | High |
| **Undo/Redo** | Integrated with `IUndoRedoService` & `EditStack`. | **Missing**. No undo/redo history tracking. | **Critical** |

### 4. Events
| Feature | TypeScript | C# | Gap Severity |
| :--- | :--- | :--- | :--- |
| **Content Changes** | `IModelContentChangedEvent` with detailed range/offset info. | `TextModelContentChangedEventArgs` exists with `TextChange` list. | Low |
| **Event Trigger** | Batched, rich events. | Synchronous, basic events. | Medium |

## Risk Assessment
- **Performance**: The current C# `PieceTreeBuffer` will fail catastrophically on large files (e.g., >1MB) due to $O(N)$ string allocation on every edit/read.
- **Data Integrity**: Lack of robust EOL/BOM handling may lead to file corruption (mixed line endings, lost BOM).
- **Feature Parity**: Missing Undo/Redo makes the editor unusable for editing tasks.

## Remediation Plan

### Phase 1: Core Performance (High Priority)
1.  **Implement Tree-Based Lookups**:
    - Add `GetPositionAt(offset)` and `GetOffsetAt(position)` to `PieceTreeModel` using tree traversal (utilizing `LineFeedsLeft`).
    - Remove `_cachedSnapshot` and `_cachedLineMap` from `PieceTreeBuffer`.
2.  **Optimize `GetLineContent`**:
    - Implement `GetLineContent(lineNumber)` in `PieceTreeModel` by traversing to the line start and collecting chunks until the line end.

### Phase 2: Versioning & Undo (High Priority)
1.  **Port `EditStack`**: Implement the undo/redo stack logic from `vs/editor/common/model/editStack.ts`.
2.  **Integrate Undo/Redo**: Connect `TextModel` to the new `EditStack`.
3.  **Fix `alternativeVersionId`**: Ensure it reverts to previous values on undo.

### Phase 3: Robustness (Medium Priority)
1.  **BOM Support**: Add `_bom` field to `TextModel` and pass it to `PieceTreeBuffer`.
2.  **EOL Normalization**: Port `countEOL` and `normalizeEOL` logic from TS to ensure inserted text matches buffer EOL preference.

## References
- TS: `vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts`
- TS: `vs/editor/common/model/textModel.ts`
- C#: `src/PieceTree.TextBuffer/PieceTreeBuffer.cs` (PieceTreeBuffer)
- C#: `src/PieceTree.TextBuffer/Core/PieceTreeModel.cs`
