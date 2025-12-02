# WS5-QA — High-Risk Deterministic Tests Result (2025-11-26)

## Intro
- **Changefeed Anchors:** [`#delta-2025-11-26-sprint04-r1-r11`](../../docs/reports/migration-log.md#delta-2025-11-26-sprint04-r1-r11) · [`#delta-2025-11-26-ws5-qa`](../../docs/reports/migration-log.md#delta-2025-11-26-ws5-qa)
- **Traceability:** Test additions logged in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-ws5-qa) and mapped in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws5-qa).
- **Scope Summary:** Deliver the first batch (≥10) of high-risk deterministic tests covering PieceTree buffer/search regressions and TextModel indentation heuristics.
- **TS References:** `pieceTreeTextBuffer.test.ts` (lines 1786-1888, 2070-2118) and `textModel.test.ts` (guessIndentation suite).

## Implementation
| Suite | File | Tests | Focus |
| --- | --- | ---: | --- |
| PieceTree buffer API | `tests/TextBuffer.Tests/PieceTreeBufferApiTests.cs` | 17 | `GetLineCharCode`, `GetCharCode`, buffer equality, CRLF edits, large-content consistency. |
| PieceTree search regressions | `tests/TextBuffer.Tests/PieceTreeSearchRegressionTests.cs` | 9 | TS issues #45892/#45770 plus edit-after-search scenarios. |
| TextModel indentation | `tests/TextBuffer.Tests/TextModelIndentationTests.cs` | 19 pass + 1 skip | Tab/space heuristics, language patterns, CRLF handling, large-file coverage, and documented `GuessIndentation` gap. |

### Suite Notes
- **PieceTreeBufferApiTests (17):** Ports TS lines 1786-1888 to cover `GetLineCharCode`, `GetCharCode`, buffer equality, and CRLF/mixed-edit stability, including issue regressions #45735/#47733.
- **PieceTreeSearchRegressionTests (9):** Locks issues #45892 (empty buffer find) and #45770 (node-boundary search) plus edit-after-search sanity checks to guard `PieceTreeSearcher` parity.
- **TextModelIndentationTests (19 pass + 1 skip):** Mirrors the TS guessIndentation matrix, tracks Python/YAML/TS patterns, Unicode and CRLF handling, and documents the missing `GuessIndentation` API via a single `[SkippableFact]`.

### API Gap Documentation
- `TextModel.GuessIndentation()` and `ITextModel.guessIndentation()` are currently missing; a skipped test records the gap and links to the backlog item.

## Testing
| Command | Result |
| --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeBufferApiTests --nologo` | `17/17 green (≈1.8s)` |
| `... --filter PieceTreeSearchRegressionTests --nologo` | `9/9 green (≈1.7s)` |
| `... --filter TextModelIndentationTests --nologo` | `19/19 green, 1 skipped (≈1.9s)` |
| Combined filter (`PieceTreeBufferApiTests|PieceTreeSearchRegressionTests|TextModelIndentationTests`) | `44/44 green, 1 skipped (≈3.2s)` |
| Full suite | `Passed! - Failed: 0, Passed: 584, Skipped: 1, Total: 585, Duration: 1m 2s` |

- Results captured in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws5-qa).

## Open Items
- **Risk:** `GuessIndentation` remains unimplemented, so any callers still depend on TS heuristics; ensure the skip is revisited once WS4/WS5 cursor work exposes the API.
- **Next-Owner Instructions:**
   1. Track the API gap via WS5 backlog item (`AA4-TextModel-GuessIndentation`) and convert the skipped test to passing once implemented.
   2. Wire these suites into nightly runs to guard the newly ported PieceTree helpers.
   3. Mirror future deterministic data through `SnapshotTestUtils` so regressions show diff context automatically.
