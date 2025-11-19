# TM-005 Result: Basic Cursor Support

## Summary
Implemented basic `Cursor` class in `src/PieceTree.TextBuffer/Cursor/Cursor.cs` with support for:
- Holding a `TextModel` reference.
- Maintaining `Selection` state (Anchor/Active).
- Movement methods: `MoveLeft`, `MoveRight`, `MoveUp`, `MoveDown`.
- Positioning methods: `MoveTo`, `SelectTo`.

## Implementation Details
- **Cursor Class**: Located in `src/PieceTree.TextBuffer/Cursor/Cursor.cs`.
- **Movement Logic**:
  - `MoveLeft`: Moves to previous character or end of previous line.
  - `MoveRight`: Moves to next character or start of next line.
  - `MoveUp/Down`: Moves vertically, clamping column to line length.
- **Validation**: All movements are validated against the `TextModel` boundaries.

## Verification
- Created unit tests in `src/PieceTree.TextBuffer.Tests/CursorTests.cs`.
- Verified all movement scenarios including boundary conditions (start/end of document, line wrapping, column clamping).
- All tests passed.

## Next Steps
- Implement multi-cursor support.
- Add more complex movement logic (e.g., move by word).
