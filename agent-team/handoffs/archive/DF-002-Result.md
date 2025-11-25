# DF-002: Myers Diff Algorithm Implementation

## Summary
Implemented the Myers Diff Algorithm (O(ND) time, linear space) in C# for `PieceTree.TextBuffer`.

## Changes
1.  **`src/PieceTree.TextBuffer/Diff/DiffChange.cs`**:
    -   Struct `DiffChange` to represent a diff block.
2.  **`src/PieceTree.TextBuffer/Diff/DiffComputer.cs`**:
    -   Class `DiffComputer` with `ComputeDiff` methods.
    -   Implemented `LcsDiff<T>` using Myers' Divide and Conquer algorithm (Linear Space Refinement).
    -   Supports generic `IList<T>` and `string`.
3.  **`src/PieceTree.TextBuffer.Tests/DiffTests.cs`**:
    -   Unit tests covering Insert, Delete, Replace, Empty, Identical, and Complex cases.
    -   Verified correctness of diffs.

## Verification
-   Ran `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`.
-   All tests passed.

## Notes
-   The implementation uses the linear space optimization (Divide and Conquer) to avoid O(N^2) memory usage, making it suitable for large files.
-   The `DiffChange` struct matches the TypeScript implementation's structure.
