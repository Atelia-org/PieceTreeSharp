# PT-005 QA Matrix (2025-11-21)

## TS Test Alignment Map (Batch #1)

| C# Suite | Scope | TS Source | Portability Tier | Status | Notes |
| --- | --- | --- | --- | --- | --- |
| PieceTreeBuilderTests | Builder chunk split, BOM/metadata retention | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) | B | Implemented | Mirrors Builder cases (`AcceptChunk_*`) incl. CRLF carryover per AA4-005. |
| PieceTreeModelTests | Piece insert/delete invariants, CRLF repair, fuzz | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) | B | Implemented | Covers metadata rebuild + CRLF fuzz; extend for invariant asserts once Porter exposes EnumeratePieces API. |
| PieceTreeSearchTests | PieceTree-level search helpers + fuzz harness | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) | B | Implemented | Provides deterministic search + fuzz parity; waiting on PT-005.S9 BufferRange/SearchContext map for full property coverage. |
| TextModelTests | TextModel lifecycle, BOM/EOL options | [ts/src/vs/editor/test/common/model/textModel.test.ts](../../ts/src/vs/editor/test/common/model/textModel.test.ts) | B | Implemented | `TextModelTests.cs` includes initialization + CRLF normalization; services layer stubs still pending for event stream parity. |
| TextModelSearchTests | Regex/word search parameters, CRLF payloads | [ts/src/vs/editor/test/common/model/textModelSearch.test.ts](../../ts/src/vs/editor/test/common/model/textModelSearch.test.ts) | B | Implemented | Core regex + multiline coverage exists; Tier-B gaps = word-boundary + separator maps noted in AA4-008 blockers. |
| DiffTests | DiffComputer heuristics (line/char, trim flags) | [ts/src/vs/editor/test/common/diff/diffComputer.test.ts](../../ts/src/vs/editor/test/common/diff/diffComputer.test.ts) | B | Implemented | Legacy diff logic ported; char-change pretty diff + whitespace flag cases still TODO for parity. |
| DecorationTests | Stickiness, injected text, per-line queries | [ts/src/vs/editor/test/common/model/modelDecorations.test.ts](../../ts/src/vs/editor/test/common/model/modelDecorations.test.ts) | B | Implemented | AA3-009 suites cover metadata + stickiness; need `model.changeDecorations` parity sweep for Tier-B exit. |
| MarkdownRendererTests | DocUI diff overlay + owner routing | [TODO – locate DocUI find widget snapshot/browser smoke tests (ts/test/browser/*)](../../docs/plans/ts-test-alignment.md#appendix-%E2%80%93-ts-test-inventory-placeholder) | C | Implemented (partial) | Snapshot parity for diff markers landed; upstream TS widget tests still unidentified → Info-Indexer to surface canonical path. |
| DocUIFindModelTests (planned) | DocUI find model binding + overlays | [ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts](../../ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts) | C | Not implemented | Planned `src/PieceTree.TextBuffer.Tests/DocUI/DocUIFindModelTests.cs`; blocked on DocUI editor harness + find decoration plumbing. |
| DocUIFindControllerTests (planned) | Command-layer find controller semantics | [ts/src/vs/editor/contrib/find/test/browser/findController.test.ts](../../ts/src/vs/editor/contrib/find/test/browser/findController.test.ts) | C | Not implemented | Target file `DocUI/DocUIFindControllerTests.cs`; needs clipboard/context-key services shims. |
| DocUIFindSelectionTests (planned) | Selection-derived search string heuristics | [ts/src/vs/editor/contrib/find/test/browser/find.test.ts](../../ts/src/vs/editor/contrib/find/test/browser/find.test.ts) | B | Not implemented | Target file `DocUI/DocUIFindSelectionTests.cs`; requires lightweight selection helpers + range serialization. |
| DocUIReplacePatternTests (planned) | ReplacePattern parser + case preservation | [ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts](../../ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts) | A | Not implemented | Batch #1 priority (`DocUI/DocUIReplacePatternTests.cs`); pure-logic suite once baseline harness stands. |

Coverage snapshot for PieceTree buffer scenarios. Dimensions track edit types, text shape nuances, chunk layout, and which validation signals currently execute in xUnit.

| Scenario | Edit Types | Text Shapes | Chunk Layout | Validation Signals | Status | Reference |
| --- | --- | --- | --- | --- | --- | --- |
| PT-005.S1 – Single chunk initialization | Build | Plain | Single | Length, `GetText` | Covered | [InitializesWithProvidedText](UnitTest1.cs#L9) |
| PT-005.S2 – Large payload initialization | Build | Large (16K) Plain | Single | Length, `GetText` | Covered | [LargeBufferRoundTripsContent](UnitTest1.cs#L16) |
| PT-005.S3 – Single chunk replace | Replace | Plain | Single | Length, `GetText` | Covered | [AppliesSimpleEdit](UnitTest1.cs#L26) |
| PT-005.S4 – Multi-chunk assembly (CRLF in middle chunk) | Build | CRLF mix | Multi | Length, `GetText`, CRLF ordering | Covered | [FromChunksBuildsPieceTreeAcrossMultipleBuffers](UnitTest1.cs#L36) |
| PT-005.S5 – Line-feed aggregation across chunks | Build | CRLF + Plain tail | Multi | `PieceTreeModel.TotalLength`, `TotalLineFeeds` | Covered | [PieceTreeModelTracksLineFeedsAcrossChunks](UnitTest1.cs#L46) |
| PT-005.S6 – CRLF replace within single chunk | Replace | CRLF | Single | Length, `GetText`, CRLF preservation | Covered | [ApplyEditHandlesCrLfSequences](UnitTest1.cs#L59) |
| PT-005.S7 – Cross-chunk replace spans multiple pieces | Replace | Plain | Multi | Length, `GetText`, boundary-span coverage | Covered | [ApplyEditAcrossChunkBoundarySpansMultiplePieces](UnitTest1.cs#L70) |
| PT-005.S8 – Piece layout inspection via `EnumeratePieces` | Build & Replace | Plain + CRLF | Multi | Piece ordering, chunk reuse metadata | Verified | [PieceTreeBaseTests.cs](PieceTreeBaseTests.cs) |
| PT-005.S9 – Property-based random edit fuzzing | Mixed edits | Plain + CRLF | Multi | BufferRange/SearchContext invariants | Verified | [PieceTreeSearchTests.cs](PieceTreeSearchTests.cs) |
| PT-005.S10 – Sequential delete→insert validation | Sequential Replace | Plain | Single | Length deltas after back-to-back `ApplyEdit` calls | Verified | [PieceTreeBaseTests.cs](PieceTreeBaseTests.cs) |

## Feature Verification (PT-004 & PT-005)

| Feature | Component | Status | Tests |
| --- | --- | --- | --- |
| **RBTree Skeleton** | `PieceTreeModel` (Insert/Delete) | Verified | `PieceTreeBaseTests.cs` (BasicInsertDelete, MoreInserts, MoreDeletes) |
| **Search** | `PieceTreeSearcher` | Verified | `PieceTreeSearchTests.cs` (BasicStringFind, RegexFind, MultilineFind) |
| **Snapshot** | `PieceTreeSnapshot` | Verified | `PieceTreeSnapshotTests.cs` (SnapshotReadsContent, SnapshotIsImmutable) |
| **Normalization** | `PieceTreeNormalizer` (via Builder) | Verified | `PieceTreeNormalizationTests.cs` (Delete_CR_In_CRLF, Line_Breaks_Replacement) |

## AA3-009 – Decorations & DocUI Regression Coverage

| Scenario | Focus | Signals | Status | Reference |
| --- | --- | --- | --- | --- |
| CL4.F1 – Decoration metadata round-trip & queries | Injected text line buckets, margin/glyph/font helpers, overview/minimap metadata | `DecorationTests.DecorationOptionsParityRoundTripsMetadata`, `DecorationTests.InjectedTextQueriesSurfaceLineMetadata`, `DecorationTests.DecorationsChangedEventIncludesMetadata` | Covered | `src/PieceTree.TextBuffer.Tests/DecorationTests.cs` |
| CL4.F3 – Stickiness & `forceMoveMarkers` parity | `DecorationRangeUpdater` honoring TS semantics for collapsed ranges and forced moves | Covered | `DecorationTests.ForceMoveMarkersOverridesStickinessDefaults` |
| CL4.F4 – DocUI diff snapshot plumbing | Markdown renderer emits diff markers (add/delete/insertion) using decoration metadata | Covered | `MarkdownRendererTests.TestRender_DiffDecorationsExposeGenericMarkers` |

**Total Tests Passing**: 119 (PIECETREE_DEBUG=0 baseline)
**Date**: 2025-11-21

## AA4-005 (CL5) & AA4-006 (CL6) Porter-added tests (2025-11-21)

| Test | CL | Focus | Owner | Status | Reference |
| --- | --- | --- | --- | --- | --- |
| AcceptChunk_SplitsLargeInputIntoDefaultSizedPieces | CL5 | Builder chunk split | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeBuilderTests.AcceptChunk_SplitsLargeInputIntoDefaultSizedPieces` |
| AcceptChunk_RetainsBomAndMetadataFlags | CL5 | BOM + metadata flags | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeBuilderTests.AcceptChunk_RetainsBomAndMetadataFlags` |
| AcceptChunk_CarriesTrailingCarriageReturn | CL5 | CRLF carryover across chunks | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeBuilderTests.AcceptChunk_CarriesTrailingCarriageReturn` |
| CreateNewPieces_SplitsLargeInsert | CL5 | Create>Split large insert into chunks | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeBuilderTests.CreateNewPieces_SplitsLargeInsert` |
| GetFirstAndLastLineTextHonorLineBreaks | CL5 | Factory preview helpers | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeFactoryTests.GetFirstAndLastLineTextHonorLineBreaks` |
| CreateUsesDefaultEolWhenTextHasNoTerminators | CL5 | Default EOL selection | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeFactoryTests.CreateUsesDefaultEolWhenTextHasNoTerminators` |
| CreateNormalizesMixedLineEndingsWhenRequested | CL5 | EOL normalization | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeFactoryTests.CreateNormalizesMixedLineEndingsWhenRequested` |
| TestSplitCRLF | CL5 | CRLF split repair & GetLineRawContent | Porter-CS / QA-Automation | Verified (pass) | `AA005Tests.TestSplitCRLF` |
| TestSplitCRLF_InsertMiddle | CL5 | Mid-string CRLF insertion | Porter-CS / QA-Automation | Verified (pass) | `AA005Tests.TestSplitCRLF_InsertMiddle` |
| TestCacheInvalidation | CL5 | Search cache invalidation/line cache | Porter-CS / QA-Automation | Verified (pass) | `AA005Tests.TestCacheInvalidation` |

| LastChangeBufferPos_AppendOptimization | CL6 | Append optimization / change buffer reuse | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeModelTests.LastChangeBufferPos_AppendOptimization` |
| AverageBufferSize_InsertLargePayload | CL6 | Chunk splitting heuristics for large inserts | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeModelTests.AverageBufferSize_InsertLargePayload` |
| CRLF_RepairAcrossChunks | CL6 | Repair CRLF bridging across chunks | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeModelTests.CRLF_RepairAcrossChunks` |
| CRLFRepair_DoesNotLeaveZeroLengthNodes | CL6 | CRLF repair should not leave tombstone nodes | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeModelTests.CRLFRepair_DoesNotLeaveZeroLengthNodes` |
| MetadataRebuild_AfterBulkDeleteAndInsert | CL6 | Metadata rebuild + line-feed recount after bulk edits | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeModelTests.MetadataRebuild_AfterBulkDeleteAndInsert` |
| CRLF_FuzzAcrossChunks | CL6 | CR/LF fuzzing across chunks with deterministic logs | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeModelTests.CRLF_FuzzAcrossChunks` |
| ChangeBufferFuzzTests | CL6 | Random insert/delete fuzz and invariants | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeModelTests.ChangeBufferFuzzTests` |
| SearchCacheInvalidation_Precise | CL6 | Search cache precision & cache invalidation | Porter-CS / QA-Automation | Verified (pass) | `PieceTreeModelTests.SearchCacheInvalidation_Precise` |

| CRLFFuzzTests.LargeInsert_HugePayload | CL6 | Large payload insert > DefaultChunkSize; chunk split | QA-Automation | Verified (pass) | `CRLFFuzzTests.LargeInsert_HugePayload` |
| CRLFFuzzTests.CRLF_SplitAcrossNodes | CL6 | CRLF bridging across chunk boundary | QA-Automation | Verified (pass) | `CRLFFuzzTests.CRLF_SplitAcrossNodes` |
| CRLFFuzzTests.CRLF_RandomFuzz_1000 | CL6 | Random CR/LF heavy fuzz test, 1000 iterations | QA-Automation | Verified (pass) | `CRLFFuzzTests.CRLF_RandomFuzz_1000` |

### Test baseline (dotnet test)
| Date | Total | Passed | Failed | Duration | Notes |
| --- | ---: | ---: | ---: | ---: | --- |
| 2025-11-21 18:05 UTC | 119 | 119 | 0 | 7.4s | `PIECETREE_DEBUG=0 dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --nologo` (AA4-009 revalidation after Porter-CS drop; deterministic full-suite count recorded for CL5/CL6). |
| 2025-11-21 09:10 UTC | 105 | 105 | 0 | 2.1s | Earlier AA4-006 verification baseline before Porter-CS expanded CL5/CL6 suites (kept for historical comparison). |

### Targeted reruns (AA4-009, 2025-11-21)

| Command | Result | Notes |
| --- | --- | --- |
| `PIECETREE_DEBUG=0 dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --filter "FullyQualifiedName~PieceTreeBuilderTests|FullyQualifiedName~PieceTreeFactoryTests" --nologo` | 7/7 green | Spot check of CL5 builder/factory regressions (AcceptChunk + preview helpers) to ensure Porter-CS changes remain stable. |
| `PIECETREE_DEBUG=0 PIECETREE_FUZZ_LOG_DIR=/tmp/aa4-009-fuzz-logs dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --filter FullyQualifiedName~CRLF_RandomFuzz_1000 --nologo` | 1/1 green | Deterministic CRLF fuzz harness (seed 123). Fuzz logs configured to land under `/tmp/aa4-009-fuzz-logs` via `FuzzLogCollector`; no file emitted because the run completed without failures. |

### Batch #1 (TS Portability) Validation Commands

| Command | Purpose | Notes |
| --- | --- | --- |
| `PIECETREE_DEBUG=0 dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --nologo --logger "trx;LogFileName=TestResults/batch1-full.trx"` | Full-suite baseline before/after TS Batch #1 drops | Captures aggregate parity; TRX stored under `TestResults/` for changefeed attachments. |
| `PIECETREE_DEBUG=0 dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --no-build --nologo --filter FullyQualifiedName~DocUIReplacePatternTests --logger "trx;LogFileName=TestResults/batch1-replacepattern.trx"` | Targeted DocUI replace-pattern port | Awaits `DocUIReplacePatternTests.cs`; command stub recorded so QA can paste once suite lands. |
| `PIECETREE_DEBUG=0 dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj --no-build --nologo --filter FullyQualifiedName~MarkdownRendererDocUI --logger "trx;LogFileName=TestResults/batch1-markdown.trx"` | Markdown renderer overlay regression sweep | Reuses existing Markdown renderer overlay tests; ensures overlays remain portable when TS snapshots change. |

## AA4-007 (CL7) – Cursor Word / Snippet / Multi-select parity (Porter-created tests)

| Test | CL | Focus | Owner | Status | Reference |
| --- | --- | --- | --- | --- | --- |
| CursorMulti_RendersMultipleCursorsAndSelections | CL7 | Multi-cursor rendering / DocUI pipeline | Porter-CS / QA-Automation | Verified (pass) | `CursorMultiSelectionTests.MultiCursor_RendersMultipleCursorsAndSelections` |
| CursorMulti_EditAtMultipleCursors | CL7 | Batch edits at multiple cursors (insert/replace) | Porter-CS / QA-Automation | Verified (pass) | `CursorMultiSelectionTests.MultiCursor_EditAtMultipleCursors` |
| MoveWordRight_BasicWords | CL7 | Word navigation (MoveWordRight/Left) | Porter-CS / QA-Automation | Verified (pass) | `CursorWordOperationsTests.MoveWordRight_BasicWords` |
| MoveWordLeft_BasicWords | CL7 | Word navigation (MoveWordLeft) | Porter-CS / QA-Automation | Verified (pass) | `CursorWordOperationsTests.MoveWordLeft_BasicWords` |
| DeleteWordLeft_Basic | CL7 | Word delete semantics | Porter-CS / QA-Automation | Verified (pass) | `CursorWordOperationsTests.DeleteWordLeft_Basic` |
| VisibleColumn_RoundTrip_WithTabs | CL7 | Column-visible/position conversions (tabs) | Porter-CS / QA-Automation | Verified (pass) | `ColumnSelectionTests.VisibleColumn_RoundTrip_WithTabs` |
| VisibleColumn_AccountsForInjectedText_BeforeAndAfter | CL7 | Injected text influences visible columns | Porter-CS / QA-Automation | Verified (pass) | `ColumnSelectionTests.VisibleColumn_AcountsForInjectedText_BeforeAndAfter` |
| Cursor_ColumnSelection_Basic | CL7 | Column selection (Alt+Drag/vertical) | Porter-CS / QA-Automation | Verified (pass) | `ColumnSelectionTests.Cursor_ColumnSelection_Basic` |
| SnippetInsert_CreatesPlaceholders_AndNavigates | CL7 | Snippet insertion & placeholder navigation | Porter-CS / QA-Automation | Verified (pass) | `SnippetControllerTests.SnippetInsert_CreatesPlaceholders_AndNavigates` |
| MarkdownRenderer_MultiCursorAndSnippet | CL7 | DocUI snapshot for multi-cursor + snippet placeholder rendering | Porter-CS / QA-Automation | Pending | `MarkdownRendererTests.(TestRender_MultiCursorAndSnippet - to be added)` |

**Notes:**
- Ownership: `Porter-CS` implemented initial features & tests; `QA-Automation` verifies and extends test matrix (labels: CL7). 
- The `MarkdownRenderer` test for a combined multi-cursor + snippet rendering snapshot is planned (pending implementation name `TestRender_MultiCursorAndSnippet`) and will be added to strengthen DocUI coverage for CL7.

**Next Steps:** 1) Monitor CL7/CL8 additions and capture new cursor/snippet/search overlays in this matrix once Porter drops land. 2) Keep CRLF fuzz harness wired with `PIECETREE_FUZZ_LOG_DIR=/tmp/aa4-009-fuzz-logs` so future QA runs retain deterministic seeds/log trails. 3) Promote DocUI golden outputs (MarkdownRenderer multi-cursor + snippet) once the pending test lands.

