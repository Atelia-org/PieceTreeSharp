# WS4-PORT — Cursor Core Result

## Intro
- **Changefeed Anchors:** [`#delta-2025-11-26-sprint04-r1-r11`](../../docs/reports/migration-log.md#delta-2025-11-26-sprint04-r1-r11) · [`#delta-2025-11-26-ws4-port-core`](../../docs/reports/migration-log.md#delta-2025-11-26-ws4-port-core)
- **Traceability:** Logged in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-ws4-port-core); coverage sits inside [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws4-port).
- **Scope Summary:** Stage 0 cursor infrastructure—port the TS cursor configuration/state/context stack plus TextModel/Decoration helpers so later cursor features have parity.
- **TS References:** `src/vs/editor/common/controller/cursorCommon.ts`, `cursorColumns.ts`, `cursorContext.ts`, and `textModel.ts` tracked in `WS4-PORT-Core` plan.

## Implementation
| File | Description | TS Reference |
| --- | --- | --- |
| `src/TextBuffer/Cursor/CursorConfiguration.cs` | New ~400-line port of `CursorConfiguration`, related enums (`EditOperationType`, `PositionAffinity`), interfaces (`ICursorSimpleModel`), and helpers (`CursorColumnsHelper`). | [`cursorCommon.ts`](../../ts/src/vs/editor/common/controller/cursorCommon.ts) + [`cursorColumns.ts`](../../ts/src/vs/editor/common/controller/cursorColumns.ts) |
| `src/TextBuffer/Cursor/CursorState.cs` | Rewritten ~370-line dual-state cursor representation (model/view states, `SelectionStartKind`, `SingleCursorState`, `Partial*State`). | [`cursorState.ts`](../../ts/src/vs/editor/common/controller/cursorState.ts) |
| `src/TextBuffer/Cursor/CursorContext.cs` | Rewritten ~260-line context wrapper exposing `ICoordinatesConverter` and adapters for `TextModel`. | [`cursorContext.ts`](../../ts/src/vs/editor/common/controller/cursorContext.ts) |
| `src/TextBuffer/TextModel.cs` | Added `ValidatePosition/Range`, tracked range plumbing (`AllocateTrackedRangeId`, `_set/_getTrackedRange`). | [`textModel.ts`](../../ts/src/vs/editor/common/model/textModel.ts) |
| `src/TextBuffer/Decorations/ModelDecoration.cs` | Added `DecorationRenderKind.None` and helper for hidden decoration options. | [`modelDecoration.ts`](../../ts/src/vs/editor/common/model/modelDecoration.ts) |
| `tests/TextBuffer.Tests/CursorCoreTests.cs` | New 25-test suite validating configuration, state transitions, tracked ranges, and helper enums. | [`cursorCore.test.ts`](../../ts/src/vs/editor/test/common/controller/cursorCore.test.ts) |

### Highlights
- Stage 0 infrastructure introduces `CursorConfiguration`, dual-state `CursorState`, and `CursorContext` so later cursor operations can share the same abstractions as VS Code.
- Feature flag `TextModelOptions.EnableVsCursorParity` keeps the new stack opt-in until cursor commands are fully ported.
- Hidden decorations (`DecorationRenderKind.None`) back the tracked-range implementation for parity with TS `TrackedRangeStickiness`.

## Testing
| Command | Result |
| --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | `Passed! - Failed: 0, Passed: 496, Skipped: 0, Total: 496` |
| `... --filter CursorCoreTests` | `25/25 green`; `CursorTests` legacy suite remains `23/23 green`. |

- Entries logged in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws4-port).

## Open Items
- **Risk:** IntervalTree perf regressions (from WS3) still cause occasional perf test failures; not a WS4 regression but blocks enabling the new cursor stack in CI.
- **Next-Owner Instructions:**
  1. After WS3 stack fix stabilizes, flip `EnableVsCursorParity` on for nightly cursor suites.
  2. Begin Stage 1 (CursorMoveCommands + OneCursor) using this infrastructure.
  3. Align doc updates—`src/TextBuffer/README.md` needs a cursor architecture section referencing the new files.
