# WS1-PORT — CRLF Bridge Result

## Intro
- **Changefeed Anchors:** [`#delta-2025-11-26-sprint04-r1-r11`](../../docs/reports/migration-log.md#delta-2025-11-26-sprint04-r1-r11) · [`#delta-2025-11-26-ws1-port-crlf`](../../docs/reports/migration-log.md#delta-2025-11-26-ws1-port-crlf)
- **Traceability:** Status recorded in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-ws1-port-crlf) and [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws1-port).
- **Scope Summary:** Finish Steps 2 & 3 from `PORT-PT-Search-Plan.md`—port the TypeScript CRLF hit detection and placeholder bridge into C#, ensuring `_lastChangeBufferPos` and buffer line starts stay aligned.
- **TS References:** `src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` (lines 1208-1223, 1455-1491) for `appendToNode` hitCRLF handling and CRLF bridge in `createNewPieces`.

## Implementation
| File | Description | TS Reference |
| --- | --- | --- |
| `src/TextBuffer/Core/PieceTreeModel.Edit.cs` | Ported `hitCRLF` detection, added `EndWithCR` helper, kept `_lastChangeBufferPos` in sync, and mirrored the placeholder bridge logic used when creating new pieces across chunk boundaries. | [`pieceTreeBase.ts`](../../ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts) |
| `tests/TextBuffer.Tests/CRLFFuzzTests.cs` | Added eleven new fuzz cases spanning Step 2/Step 3 and edge scenarios (placeholder leakage, cache invalidation, multiple bridges). | [`pieceTreeTextBuffer.test.ts`](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) |

### Step 2 — `AppendToChangeBufferNode`
- Mirrors the TS logic by calculating `hitCRLF = ShouldCheckCRLF() && StartWithLF(adjusted) && EndWithCR(node)`.
- After buffering, pops the last line-start entry, decrements `_lastChangeBufferPos.Line`, recomputes the column, and adjusts CR/LF counters so single `\r` entries merge into CRLF pairs.
- Introduced `EndWithCR(PieceTreeNode node)` to avoid duplicating end-of-node checks.

### Step 3 — `CreateNewPieces`
- Detects when a trailing CR in the change buffer needs the placeholder technique before a leading LF joins.
- Increments `_lastChangeBufferPos.Column`, appends `"_" + text`, and shifts `lineStarts` so the placeholder never surfaces in user content—matching the TS sentinel approach despite `ChunkBuffer` immutability.
- Relied on `ChunkBuffer.Append` to rebuild `LineStartTable` instances, keeping cache invalidation centralized.

## Testing
| Command | Result |
| --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | `Passed! - Failed: 0, Passed: 451, Skipped: 0, Total: 451, Duration: 53s` |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter CRLFFuzzTests --nologo` | `Total: 16, Passed: 16, Failed: 0, Skipped: 0, Duration: 2.6s` |

### New Test Coverage (CRLFFuzzTests)
| Group | Tests | Purpose |
| --- | --- | --- |
| Step 2 Hit Detection | `Step2_AppendToNode_*` (3 tests) | Validates line-count adjustments and multiple bridge handling. |
| Step 3 Bridge | `Step3_CreateNewPieces_*` (4 tests) | Confirms placeholder transitions, `GetLineContent`, and feed counters. |
| Edge Cases | `EdgeCase_*` (4 tests) | Ensures cache invalidation, placeholder secrecy, and CR-only inserts work. |

- Test updates captured in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws1-port).

## Open Items
- **Risk:** ChunkBuffer rebuilds currently allocate new `LineStartTable` instances for every hit, which could regress perf for large CRLF imports.
- **Next-Owner Instructions:**
    1. Prototype a pooled `ChunkBuffer.FromPrecomputed` to amortize Step 2 allocations (tracked via `AA4-PT-ChunkBufferPooling`).
    2. Expand CRLF fuzzing to cover documents that span `AverageBufferSize` to ensure the placeholder technique scales.
    3. Update `src/TextBuffer/README.md` with the CRLF bridge notes so future ports do not regress this logic.
