# TM-002 & TM-003 Result: TextModel & Selection Implementation

## Summary
Implemented the `TextModel` skeleton and `Selection` primitives in C#, wrapping the existing `PieceTreeBuffer`.

## Changes

### 1. Selection Primitive
- Created `src/PieceTree.TextBuffer/Core/Selection.cs`.
- Implemented `Selection` struct with `Anchor`, `Active`, `Start`, `End` properties.
- Added `SelectionDirection` enum.
- Implemented logic for `Contains`, `CollapseToStart`, `CollapseToEnd`.
- Updated `TextPosition` in `src/PieceTree.TextBuffer/TextPosition.cs` to implement `IComparable<TextPosition>` and comparison operators (`<`, `>`, `<=`, `>=`).

### 2. TextModel Implementation
- Created `src/PieceTree.TextBuffer/TextModel.cs`.
- Wraps `PieceTreeBuffer`.
- Implemented properties: `VersionId`, `AlternativeVersionId`, `Eol`.
- Implemented methods: `GetValue()`, `GetLineContent(int)`, `GetLength()`, `GetLineCount()`.
  - `GetLineContent` strips EOL characters to match VS Code API behavior.
- Implemented `ApplyEdits(TextEdit[])`:
  - Sorts edits descending to preserve offsets.
  - Delegates to `PieceTreeBuffer.ApplyEdit`.
  - Increments `VersionId` and `AlternativeVersionId`.
  - Fires `OnDidChangeContent` event.
- Defined `TextEdit`, `TextChange`, and `TextModelContentChangedEventArgs`.

### 3. Tests
- Created `src/PieceTree.TextBuffer.Tests/TextModelTests.cs`.
- Added tests for:
  - `Selection` logic (direction, normalization, containment).
  - `TextModel` creation and read operations.
  - `TextModel` editing and version incrementing.
  - `TextModel` event firing.
  - Multiple edits application.
- Verified all tests pass (including existing tests).

## Verification
Ran `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`.
Result: **Passed** (27 tests).
