# AA-004 Audit Report: Diff and Decorations

## 1. Executive Summary
This audit compares the TypeScript (VS Code) and C# (PieceTree.TextBuffer) implementations of Diff computation and Decorations management.
**Key Findings**:
- **Diff**: The C# implementation correctly implements the core Myers algorithm but **completely lacks** the extensive post-processing and "prettification" logic present in VS Code. This will result in mathematically correct but visually suboptimal diffs.
- **Decorations**: The C# implementation uses a simple `List<T>` (O(N)) instead of a Red-Black Tree (O(log N)), posing a performance risk for documents with many decorations. It also misses the `CollapseOnReplaceEdit` feature.

## 2. Diff Audit

### 2.1. Core Algorithm (Myers)
- **TS**: `myersDiffAlgorithm.ts` implements the standard O(ND) Myers algorithm with optimizations (bit vector arrays).
- **C#**: `DiffComputer.cs` implements a recursive Divide-and-Conquer version of Myers (Linear Space LCS).
- **Verdict**: **Compatible**. The C# implementation is actually more memory-efficient (O(N) space) than the standard O(ND) implementation, which is good. The core logic is sound.

### 2.2. Post-Processing (Cleanup/Prettify)
- **TS**: `heuristicSequenceOptimizations.ts` contains extensive logic to make diffs human-readable:
    - `joinSequenceDiffsByShifting`: Merges adjacent changes.
    - `shiftSequenceDiffs`: Aligns changes to word boundaries (e.g., `[IArr, ]IBar` instead of `I[Arr, I]Bar`).
    - `removeShortMatches`: Ignores coincidental short matches to reduce noise.
    - `extendDiffsToEntireWordIfAppropriate`: Expands diffs to cover whole words.
- **C#**: **Missing**. The C# implementation returns the raw LCS result.
- **Gap**: **High**. Users accustomed to VS Code's "smart" diffs will find the C# diffs "noisy" or "ugly" (e.g., matching a single comma or bracket in the middle of a changed block).

## 3. Decorations Audit

### 3.1. Data Structure
- **TS**: `intervalTree.ts` implements a fully augmented **Red-Black Tree**.
    - Complexity: O(log N) for Insert, Delete, Search, and Shift (Delta propagation).
- **C#**: `IntervalTree.cs` uses a `List<ModelDecoration>`.
    - Complexity: O(N) for all operations.
- **Gap**: **High**. For files with thousands of decorations (common in large files with colorizers, linters, and git lens), O(N) updates on every keystroke will cause noticeable typing lag.

### 3.2. `AcceptReplace` Logic
- **TS**: Handles complex edge cases:
    - `TrackedRangeStickiness` (AlwaysGrows, NeverGrows, GrowsBefore, GrowsAfter).
    - `CollapseOnReplaceEdit`: Allows a decoration to shrink to a point if fully covered by a replacement.
    - `forceMoveMarkers`: Special handling for specific edit types.
- **C#**:
    - **Stickiness**: Implemented correctly for the standard cases (`AlwaysGrows`, `NeverGrows`, etc.). Logic for point insertions matches TS behavior.
    - **CollapseOnReplaceEdit**: **Missing**. Decorations will always adjust boundaries based on stickiness, never collapsing purely due to being "overwritten" if the option was set.
    - **Edge Cases**: The linear scan implementation handles basic insertion/deletion correctly, but the lack of `CollapseOnReplaceEdit` is a functional gap.

## 4. Risk Assessment

| Area | Risk Level | Impact |
| :--- | :--- | :--- |
| **Diff Quality** | High | Diffs will look "broken" or "dumb" compared to VS Code. Lowers user trust in the tool. |
| **Decorations Perf** | High | Typing latency in files with many decorations (e.g., 5k+). |
| **Decorations Logic** | Medium | Missing `CollapseOnReplaceEdit` might break specific extensions that rely on it (e.g., snippets, some refactorings). |

## 5. Remediation Plan

### Phase 1: Diff Quality (Priority: High)
- **Task**: Port `heuristicSequenceOptimizations.ts` to C#.
- **Details**: Implement `ShiftSequenceDiffs`, `JoinSequenceDiffsByShifting`, and `RemoveShortMatches`. This is pure algorithmic code and should be straightforward to port.

### Phase 2: Decorations Performance (Priority: Medium/High)
- **Task**: Upgrade `IntervalTree.cs` to use a Red-Black Tree.
- **Details**: Port the RB-Tree implementation from `intervalTree.ts`. Ensure "Delta" propagation is implemented to allow O(log N) shifting of ranges during edits.

### Phase 3: Decorations Features (Priority: Low)
- **Task**: Add `CollapseOnReplaceEdit` support.
- **Details**: Update `ModelDecorationOptions` and `AcceptReplace` logic to respect this flag.
