# WS5-CursorAtomicMove-Result

## Summary
**Task:** WS5 - Implement cursorAtomicMoveOperations Tests (#1 Priority)  
**Status:** ✅ Complete  
**Date:** 2025-11-28  
**Owner:** Porter-CS (Viktor Zoric)

## Changefeed Anchors
- [`#delta-2025-11-28-ws5-cursoratomicmove`](../indexes/README.md#delta-2025-11-28-ws5-cursoratomicmove)

## Implementation

### New Files Created

1. **`src/TextBuffer/Cursor/AtomicTabMoveOperations.cs`**
   - Complete port of TS `cursorAtomicMoveOperations.ts`
   - `Direction` enum: `Left`, `Right`, `Nearest`
   - `WhitespaceVisibleColumn()`: Calculates visible column within whitespace-only prefix
   - `AtomicPosition()`: Returns atomic tab stop position for cursor movement

2. **`tests/TextBuffer.Tests/Cursor/CursorAtomicMoveTests.cs`**
   - Complete port of TS `cursorAtomicMoveOperations.test.ts`
   - Namespace: `PieceTree.TextBuffer.Tests.CursorOperations` (避免与 `Cursor` 类型冲突)
   - 43 test cases total

### Test Coverage

| Test Category | Test Count | Description |
|--------------|-----------|-------------|
| `WhitespaceVisibleColumn_AllPositions` | 8 (Theory) | Data-driven tests for all 8 TS test cases |
| `WhitespaceVisibleColumn_*` (Facts) | 5 | Individual position tests |
| `AtomicPosition_Left_AllPositions` | 6 (Theory) | Left direction for all 6 TS test cases |
| `AtomicPosition_Right_AllPositions` | 6 (Theory) | Right direction for all 6 TS test cases |
| `AtomicPosition_Nearest_AllPositions` | 6 (Theory) | Nearest direction for all 6 TS test cases |
| `AtomicPosition_*` (Facts) | 12 | Individual edge case tests |
| **Total** | **43** | All passing |

### Test Data Tables Ported

#### `whitespaceVisibleColumn` Data (8 cases)
| LineContent | TabSize | Description |
|-------------|---------|-------------|
| `"        "` | 4 | 8 spaces |
| `"  "` | 4 | 2 spaces |
| `"\t"` | 4 | Single tab |
| `"\t "` | 4 | Tab + space |
| `" \t\t "` | 4 | Space + tab + tab + space |
| `" \tA"` | 4 | Space + tab + non-whitespace |
| `"A"` | 4 | Non-whitespace only |
| `""` | 4 | Empty string |

#### `atomicPosition` Data (6 cases)
| LineContent | TabSize | Description |
|-------------|---------|-------------|
| `"        "` | 4 | 8 spaces |
| `" \t"` | 4 | Space + tab |
| `"\t "` | 4 | Tab + space |
| `" \t "` | 4 | Space + tab + space |
| `"        A"` | 4 | 8 spaces + text |
| `"      foo"` | 4 | 6 spaces + text (partial indentation) |

## Verification

```bash
export PIECETREE_DEBUG=0 && dotnet test --filter CursorAtomicMoveTests --nologo
```

**Result:** 43/43 tests pass

### Full Suite Verification
```bash
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
```

**Result:** 726 total (724 pass + 2 skip)

## Dependencies

### Pre-existing Used
- `CursorColumnsHelper.NextRenderTabStop()` in `CursorConfiguration.cs`

### No New Dependencies Added
The `AtomicTabMoveOperations` class is self-contained and only relies on the existing `CursorColumnsHelper.NextRenderTabStop()` method which was already implemented.

## Notes

### Namespace Choice
Used `PieceTree.TextBuffer.Tests.CursorOperations` instead of `PieceTree.TextBuffer.Tests.Cursor` to avoid namespace conflict with `PieceTree.TextBuffer.Cursor.Cursor` class references in existing test files.

### Theory vs MemberData
The test data uses `[Theory]` with `[MemberData]` pattern. xUnit reports "Non-serializable data" warnings because the test case classes aren't IXunitSerializable, but this doesn't affect test execution.

## Open Items
- Consider implementing `IXunitSerializable` on test case classes to enable proper test discovery display names (low priority)

## References
- TS Source: `ts/src/vs/editor/test/common/controller/cursorAtomicMoveOperations.test.ts`
- TS Implementation: `ts/src/vs/editor/common/cursor/cursorAtomicMoveOperations.ts`
- Backlog: [WS5-INV-TestBacklog.md](WS5-INV-TestBacklog.md) (#1 Priority)
