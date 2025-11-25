# DF-004 Result: TextModel Decoration Integration

## Summary
Integrated `IntervalTree` (DecorationStorage) into `TextModel`. Decorations are now automatically updated when text edits are applied to the model.

## Decisions
**Decision**: Integrated `IntervalTree` directly into `TextModel` as a private field `_decorations`.
**Reasoning**: This encapsulates the decoration logic within the model, ensuring that any edit applied to the model automatically updates the decorations. This prevents desynchronization between the text buffer and the decorations.

## Implementation Details
1.  **TextModel.cs**:
    *   Added `_decorations` field (type `IntervalTree`).
    *   Implemented `AddDecoration(TextRange range, ModelDecorationOptions options)`: Adds a decoration to the tree and returns it.
    *   Implemented `GetDecorationsInRange(TextRange range)`: Proxies to `IntervalTree.Search`.
    *   Updated `ApplyEdits`:
        *   Iterates through sorted edits.
        *   Calls `_decorations.AcceptReplace` for each edit *before* applying the edit to the buffer (though the order for `AcceptReplace` logic relies on offsets which are calculated before the buffer edit).
        *   Ensures decorations shift correctly with text changes.

2.  **Tests**:
    *   Updated `src/PieceTree.TextBuffer.Tests/TextModelTests.cs`.
    *   Added `TestTextModel_Decorations`: Verifies that a decoration on "World" shifts correctly when "Beautiful " is inserted before it.

## Verification
Ran `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`.
**Result**: All 47 tests passed (including the new decoration integration test).

## Future Work
*   **Performance**: As noted in DF-003, the underlying `IntervalTree` is currently a list (O(N)). Upgrading it to a Red-Black Tree will improve performance for `AddDecoration` and `AcceptReplace` when many decorations exist.
*   **Events**: Consider adding an event `OnDidChangeDecorations` to `TextModel` so that views (like the editor) can redraw when decorations change.
