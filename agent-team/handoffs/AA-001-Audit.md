# Audit Report: C# PieceTree Core vs TypeScript Original

**Task**: AA-001
**Date**: 2025-11-19
**Auditor**: GitHub Copilot

## Executive Summary
The C# port of the PieceTree text buffer (`PieceTree.TextBuffer`) captures the core Red-Black Tree structure and basic editing operations but lacks critical production-readiness features present in the TypeScript original. The most significant gaps are in **CRLF normalization/handling**, **search caching**, and **specific performance optimizations**.

## Gap Analysis

| Feature Area | TypeScript (`vs/editor/common/model/pieceTreeTextBuffer`) | C# (`PieceTree.TextBuffer/Core`) | Impact |
| :--- | :--- | :--- | :--- |
| **CRLF Handling** | Extensive logic to handle split CRLF across pieces (`validateCRLFWithPrevNode`, `fixCRLF`, `shouldCheckCRLF`). | **Missing**. Marked as `// TODO: CRLF checks` in `Insert` and `InsertContentToNode...`. | **High**. Risk of data corruption or incorrect line counts with mixed line endings. |
| **Tree Metadata Update** | `recomputeTreeMetadata` uses delta propagation and stops early if possible. | `RecomputeMetadataUpwards` recomputes aggregates for the entire path to root. | **Medium**. Write performance degradation on large files. |
| **Line Content Cache** | `getLineRawContent` checks `_searchCache.get2(lineNumber)` to avoid O(log N) traversal. | `GetLineRawContent` **does not** use the cache. It traverses from root every time. | **Medium**. Read performance degradation during rendering/search. |
| **Insert Optimization** | Checks `_lastChangeBufferPos` to append to the last node if possible, avoiding tree operations. | **Missing**. Marked as `// TODO: Implement _lastChangeBufferPos tracking`. | **Medium**. Slower sequential typing performance. |
| **Search API** | `findMatchesLineByLine` is fully integrated. | `ExecuteSearch` throws `NotSupportedException`. Logic exists in `Search.cs` but is not hooked up to the public API. | **High**. Search functionality is effectively broken. |
| **Line Feed Counting** | `getLineFeedCnt` handles edge cases where `\r` and `\n` are in different pieces. | `GetLineFeedCnt` is simplified and lacks character access to check for `\r` at piece boundaries. | **High**. Incorrect line counting. |

## Detailed Findings

### 1. RBTree Balancing & Metadata
*   **Logic Match**: The core RBTree rotations (`RotateLeft`, `RotateRight`) and fixups (`InsertFixup`, `DeleteFixup`) appear to match the TS logic correctly.
*   **Inefficiency**: C# uses `RecomputeAggregates` which recalculates `AggregatedLength` and `AggregatedLineFeeds` from children. TS calculates `size_left` and `lf_left` using deltas. The C# approach is safer but slower (O(log N) vs O(1) best case for TS updates).

### 2. Cache Invalidation
*   **Missing Read Cache**: The `_searchCache` exists in C# but is only used in `NodeAt` (offset-based lookup). It is **ignored** in `GetLineRawContent` (line-based lookup), forcing a full tree traversal for every line retrieval.
*   **Missing Write Optimization**: The `_lastChangeBufferPos` optimization is critical for typing latency. Its absence means every character typed triggers a full tree insertion/split logic instead of a simple buffer append.

### 3. Memory & Structs
*   **PieceSegment**: Correctly implemented as a `record struct`. This is a good choice for C# to reduce GC pressure compared to TS `Piece` class.
*   **PieceTreeNode**: Implemented as a class, which is correct. It stores `AggregatedLength` which TS does not (TS calculates it). This increases memory footprint slightly per node but speeds up `TotalLength` access.

### 4. CRLF & Line Endings
*   **Critical Gap**: The TS implementation goes to great lengths to ensure that a `\r` at the end of one piece and a `\n` at the start of the next are treated as a single line break. The C# implementation currently ignores this, which will lead to incorrect line counts and potential "ghost" lines if a file has `\r\n` split across chunks.

## Remediation Plan (For AA-005)

**Priority 1: Correctness (CRLF & Search)**
1.  **Implement CRLF Logic**: Port `validateCRLFWithPrevNode`, `fixCRLF`, and `adjustCarriageReturnFromNext` from `pieceTreeBase.ts`.
2.  **Fix `GetLineFeedCnt`**: Ensure it can access the underlying buffer to check for `\r` when the cursor is at a boundary.
3.  **Hook up Search**: Implement `ExecuteSearch` in `PieceTreeModel.cs` to call `FindMatchesLineByLine`.

**Priority 2: Performance (Caching & Optimization)**
4.  **Enable Line Cache**: Update `GetLineRawContent` to use `_searchCache.TryGetByLine` before traversing.
5.  **Implement Append Optimization**: Add `_lastChangeBufferPos` tracking to `Insert` to allow fast-path appending to the change buffer.
6.  **Optimize Metadata Update**: Refactor `RecomputeMetadataUpwards` to stop propagation if values haven't changed (delta approach), matching TS `recomputeTreeMetadata`.

**Priority 3: Cleanup**
7.  **Remove TODOs**: Clear out the identified TODO comments in `PieceTreeModel.cs` and `PieceTreeModel.Edit.cs`.
