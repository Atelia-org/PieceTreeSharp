# AA-006 Result: Search and Cursor Fixes

**Date:** November 19, 2025
**Author:** GitHub Copilot
**Status:** Completed

## 1. Executive Summary
Addressed high-priority gaps identified in AA-003 and AA-004 regarding Search and Cursor logic.
- **Search**: Implemented "Whole Word" search support using a `WordCharacterClassifier`.
- **Cursor**: Implemented "Sticky Column" memory to preserve horizontal position when moving vertically across lines of different lengths.
- **Tests**: Added unit tests for both features.

## 2. Changes

### 2.1 Word Separators (Search)
- **File**: `src/PieceTree.TextBuffer/Core/SearchTypes.cs`
  - Implemented `WordCharacterClassifier` with `IsWordCharacter` (alphanumeric + underscore) and `IsValidMatch` (boundary check).
  - Updated `SearchParams.ParseSearchRequest` to:
    - Populate `WordSeparators` classifier if requested.
    - Ensure a fallback `Regex` is created even for `SimpleSearch` to support `PieceTreeSearcher` in all contexts.
- **File**: `src/PieceTree.TextBuffer/Core/PieceTreeSearcher.cs`
  - Updated `Next` method to use `_wordSeparators.IsValidMatch` to filter matches.
- **File**: `src/PieceTree.TextBuffer/Core/PieceTreeModel.Search.cs`
  - Updated `FindMatchesInLine` to check word boundaries during `SimpleSearch` optimization.

### 2.2 Cursor Sticky Column
- **File**: `src/PieceTree.TextBuffer/Cursor/Cursor.cs`
  - Added `_stickyColumn` state.
  - Updated `MoveUp` and `MoveDown` to use and preserve `_stickyColumn`.
  - Updated `MoveTo`, `MoveLeft`, `MoveRight` to reset `_stickyColumn`.

### 2.3 Tests
- **File**: `src/PieceTree.TextBuffer.Tests/PieceTreeSearchTests.cs`
  - Added `TestWholeWordFind` to verify whole word search logic.
- **File**: `src/PieceTree.TextBuffer.Tests/CursorTests.cs`
  - Added `TestCursor_StickyColumn` to verify cursor column memory.

## 3. Verification
Ran `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`.
- **Total Tests**: 56
- **Passed**: 56
- **Failed**: 0

## 4. Remaining Work (from AA-003/AA-004)
- **Diff Prettify**: Port `heuristicSequenceOptimizations.ts` to C# (Visual improvement).
- **Decoration Performance**: Upgrade `IntervalTree` to Red-Black Tree (Performance improvement).
- **Cursor**: Implement "Virtual Spaces" (Tab expansion) and atomic soft tabs.
