# WS2-PORT — Range & Selection Helper APIs

## Intro
- **Changefeed Anchors:** [`#delta-2025-11-26-sprint04-r1-r11`](../../docs/reports/migration-log.md#delta-2025-11-26-sprint04-r1-r11) · [`#delta-2025-11-26-ws2-port`](../../docs/reports/migration-log.md#delta-2025-11-26-ws2-port)
- **Traceability:** Logged migration and coverage in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-ws2-port) and [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws2-port).
- **Scope Summary:** Port every P0 Range, Selection, and TextPosition helper from `INV-RangeSelection-GapReport.md` to establish full TS parity.
- **TS References:** `src/vs/editor/common/core/range.ts`, `selection.ts`, and `position.ts` plus the VS Code test suites referenced in the gap report.

## Implementation
| File | Description | TS Reference |
| --- | --- | --- |
| `src/TextBuffer/Core/Range.Extensions.cs` | Added the full static + instance API surface for containment, intersection, normalization, collapse, and comparison helpers. | [`range.ts`](../../ts/src/vs/editor/common/core/range.ts) |
| `src/TextBuffer/Core/Selection.cs` | Added factory methods, direction-aware setters, and selection equality helpers mirroring TS. | [`selection.ts`](../../ts/src/vs/editor/common/core/selection.ts) |
| `src/TextBuffer/TextPosition.cs` | Added `With`, `Delta`, comparison helpers, and null-safe equality variants. | [`position.ts`](../../ts/src/vs/editor/common/core/position.ts) |
| `tests/TextBuffer.Tests/RangeSelectionHelperTests.cs` | New 75-test suite covering all APIs listed below. | [`range.test.ts`](../../ts/src/vs/editor/test/common/core/range.test.ts) + [`selection.test.ts`](../../ts/src/vs/editor/test/common/core/selection.test.ts) |

### API Coverage
#### Range Static Methods (P0)
| API | Notes |
| --- | --- |
| `ContainsPosition`, `StrictContainsPosition`, `ContainsRange`, `StrictContainsRange`, `IntersectRanges`, `AreIntersecting*`, `PlusRange`, `Normalize`, `CollapseTo*`, `EqualsRange`, `CompareRangesUsingStarts/Ends`, `SpansMultipleLines` | Logic matches TS semantics including inclusive/exclusive edge rules. |

#### Range Instance Methods (P0)
`SetStartPosition`, `SetEndPosition`, `CollapseTo*`, `Delta`, `IsSingleLine`, instance containment/intersection helpers.

#### Selection APIs (P0)
`Selection.FromPositions`, `FromRange`, `CreateWithDirection`, direction-preserving setters, `GetPosition`, `GetSelectionStart`, `GetDirection`, equality helpers.

#### TextPosition Extensions (P0)
`With`, `Delta`, `IsBefore*`, static `Compare`/`Equals` helpers.

## Testing
| Command | Result |
| --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | `Passed! - Failed: 0, Passed: 440, Skipped: 0, Total: 440, Duration: 53s` |

### RangeSelectionHelperTests Breakdown
| Category | Count |
| --- | ---: |
| Range Contains / StrictContains | 22 |
| Range Intersections / Normalize / PlusRange | 14 |
| Range Instance operations | 8 |
| Selection static helpers | 8 |
| Selection equality + direction helpers | 7 |
| TextPosition extensions | 12 |
| Sorting/comparison helpers | 4 |
| **Total** | **75** |

- Entries captured in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws2-port).

## Open Items
- **Risk:** Downstream call sites (TextModelSearch, Cursor columns, FindDecorations) still use bespoke logic; until they migrate, bugs can reappear.
- **Next-Owner Instructions:**
   1. Stage refactors so `TextModelSearch` and `CursorColumns` adopt these helpers (tracked in task board WS2 follow-ups).
   2. Add P1/P2 APIs from the gap report once WS5 harness data is available.
   3. Keep `RangeSelectionHelperTests` wired into nightly CI to catch regressions as other workstreams refactor onto these helpers.
