# DF-003 Result: Decoration Storage Implementation

## Summary
Implemented the decoration storage system for `PieceTree.TextBuffer`.

## Decisions
**Decision**: Implemented a **Simplified Version (List)** for v1 instead of the full Red-Black Tree.
**Reasoning**: The TypeScript `IntervalTree` implementation is complex, involving augmented Red-Black Tree logic with delta propagation and specific rebalancing rules. Porting this in a single step carries a high risk of bugs. The List-based implementation provides a correct, testable baseline that fulfills the functional requirements (adding, searching, and shifting decorations on edits) immediately.

## Implementation Details
1.  **ModelDecoration**: Created `src/PieceTree.TextBuffer/Decorations/ModelDecoration.cs`.
    *   Includes `TextRange` struct.
    *   Includes `ModelDecorationOptions` and `TrackedRangeStickiness` enum.
2.  **IntervalTree**: Created `src/PieceTree.TextBuffer/Decorations/IntervalTree.cs`.
    *   **Current State**: Uses `List<ModelDecoration>` (O(N) for edits/search).
    *   **API**: `Insert`, `Delete`, `Search`, `AcceptReplace`.
    *   **Delta Propagation**: Implemented logic to shift, expand, or collapse decorations based on text edits and stickiness rules.
3.  **Tests**: Created `src/PieceTree.TextBuffer.Tests/DecorationTests.cs`.
    *   Verified adding decorations.
    *   Verified shifting when text is inserted/deleted before.
    *   Verified expansion when text is inserted inside.
    *   Verified stickiness behavior (AlwaysGrows vs NeverGrows).

## Future Work (v2)
*   **TODO**: Port the full Red-Black Tree implementation from `ts/src/vs/editor/common/model/intervalTree.ts` to `src/PieceTree.TextBuffer/Decorations/IntervalTree.cs` to achieve O(log N) performance for large files with many decorations.
*   **TODO**: Integrate `IntervalTree` into `TextModel` to automatically update decorations on `ApplyEdits`.

## Verification
Ran `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`.
**Result**: All 46 tests passed (including 7 new decoration tests).
