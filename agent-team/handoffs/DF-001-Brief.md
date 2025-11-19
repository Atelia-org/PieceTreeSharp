# DF-001: Diffing & Decorations Analysis

## Goal
Analyze the TypeScript implementation of Diffing and Decorations to prepare for C# porting. This document summarizes the algorithms, data structures, and proposed C# API surface.

## 1. Diff Algorithm Analysis

The core diff algorithm is located in `ts/src/vs/base/common/diff/diff.ts`.

*   **Algorithm**: It implements **Myers' O(ND) Difference Algorithm**.
*   **Optimizations**:
    *   **Divide and Conquer**: Uses a recursive approach (`ComputeRecursionPoint`) to find the middle snake, reducing space complexity to linear O(N) when full history isn't needed.
    *   **History Buffer**: Uses `Int32Array` (`m_forwardHistory`, `m_reverseHistory`) to store diagonal frontiers, minimizing object allocation.
    *   **Hashing**: Strings are hashed (`stringHash`) to speed up equality checks (`Int32Array` comparison vs string comparison).
    *   **Early Exit**: Supports `IContinueProcessingPredicate` to abort long-running diffs (e.g., for large files).
    *   **Prettify**: A post-processing step (`PrettifyChanges`) shifts diff blocks to align with word boundaries or whitespace, making them more human-readable.
    *   **Levenshtein Distance**: Used for fine-grained string comparison, with specialized implementations for short (<= 32 chars) and long strings (using bit vectors).

*   **Data Structure**: `DiffChange` (struct-like class)
    *   `originalStart`, `originalLength`
    *   `modifiedStart`, `modifiedLength`

## 2. Decorations & IntervalTree Analysis

Decorations are stored in an **Augmented Red-Black Tree** (`IntervalTree`) located in `ts/src/vs/editor/common/model/intervalTree.ts`.

*   **Core Structure**: Standard Red-Black Tree properties (Color, Rotation, Insertion, Deletion).
*   **Augmentation**:
    *   `maxEnd`: Stores the maximum end offset in the subtree. Used for efficient O(log N) interval overlap queries (`intervalSearch`).
    *   **Delta Propagation**: This is the critical optimization for text editors.
        *   Nodes contain a `delta` field.
        *   When text is inserted/deleted, we don't update every node. Instead, we update the `delta` of the subtree root.
        *   `delta` is lazily pushed down or accumulated during traversal.
        *   This allows shifting all decorations after an edit point in O(log N) time.
*   **Node Metadata**:
    *   `metadata` (int) packs: Color, Visited, IsForValidation, Stickiness, CollapseOnReplaceEdit, IsMargin, AffectsFont.
    *   `cachedVersionId`, `cachedAbsoluteStart`, `cachedAbsoluteEnd`: Used to cache resolved absolute positions to avoid re-traversing the tree for `delta` resolution on every read.
*   **Usage in TextModel**:
    *   `DecorationsTrees` class manages three instances of `IntervalTree`:
        1.  `_decorationsTree0`: Standard decorations.
        2.  `_decorationsTree1`: Overview ruler decorations (separated for faster rendering of the scroll bar).
        3.  `_injectedTextDecorationsTree`: Injected text (e.g., inline hints).

## 3. Proposed C# API Surface

### Diffing

```csharp
namespace Microsoft.VisualStudio.Text.Diff
{
    public struct DiffChange
    {
        public int OriginalStart;
        public int OriginalLength;
        public int ModifiedStart;
        public int ModifiedLength;

        public int OriginalEnd => OriginalStart + OriginalLength;
        public int ModifiedEnd => ModifiedStart + ModifiedLength;
    }

    public interface IDiffComputer
    {
        DiffResult ComputeDiff(string[] originalLines, string[] modifiedLines, bool pretty);
    }

    public class LcsDiff
    {
        // Core Myers implementation
        public LcsDiff(ISequence original, ISequence modified);
        public DiffResult ComputeDiff(bool pretty);
    }
}
```

### Decorations (TextModel)

```csharp
namespace Microsoft.VisualStudio.Text.Model
{
    public partial class TextModel
    {
        // Core API
        public string[] DeltaDecorations(string[] oldDecorations, ModelDeltaDecoration[] newDecorations, int ownerId = 0);
        public ModelDecoration[] GetDecorationsInRange(TextRange range, int ownerId = 0, bool filterOutValidation = false);
        public ModelDecoration[] GetAllDecorations(int ownerId = 0, bool filterOutValidation = false);

        // Internal Storage
        private DecorationsTrees _decorationsTree;
    }

    internal class DecorationsTrees
    {
        private readonly IntervalTree<DecorationNode> _decorationsTree0;
        private readonly IntervalTree<DecorationNode> _decorationsTree1;
        private readonly IntervalTree<DecorationNode> _injectedTextDecorationsTree;

        public void Insert(DecorationNode node);
        public void Delete(DecorationNode node);
        public IList<DecorationNode> Search(TextRange range, ...);
    }

    // Generic or Specialized IntervalTree
    internal class IntervalTree<T> where T : IntervalNode
    {
        public void Insert(T node);
        public void Delete(T node);
        public IList<T> IntervalSearch(int start, int end);
        
        // Handles the "delta" logic for text edits
        public void AcceptReplace(int offset, int length, int textLength, bool forceMoveMarkers);
    }
}
```

## 4. Implementation Notes for Porter

*   **IntervalTree Complexity**: The `IntervalTree` logic with `delta` normalization and `maxEnd` maintenance is complex and error-prone.
    *   **Recommendation**: Port the `IntervalTree` logic **exactly** as is from TypeScript. Do not try to simplify it with a standard `.NET` collection for v1 if you want to maintain performance parity for large files. A simple list will be O(N) for edits, which is unacceptable for typing in large files with many decorations.
    *   **Bit Manipulation**: The `metadata` field uses bitwise operations. C# `Flags` enum or explicit bit masking can be used.
*   **Diffing**: The `LcsDiff` class is self-contained. It can be ported directly. Ensure `Int32Array` usage is mapped to `int[]` or `Span<int>` for performance.
