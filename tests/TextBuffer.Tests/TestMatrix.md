# PT-005 QA Matrix (2025-11-26)

## TS Test Alignment Map (Batch #1)

| C# Suite | Scope | TS Source | Portability Tier | Status | Notes |
| --- | --- | --- | --- | --- | --- |
| PieceTreeBuilderTests | Builder chunk split, BOM/metadata retention | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) | B | Implemented | Mirrors Builder cases (`AcceptChunk_*`) incl. CRLF carryover per AA4-005. |
| PieceTreeModelTests | Piece insert/delete invariants, CRLF repair, fuzz | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) | B | Implemented | Covers metadata rebuild + CRLF fuzz; extend for invariant asserts once Porter exposes EnumeratePieces API. |
| PieceTreeBaseTests | RB-tree basics, cache invalidation, trimmed line content | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) | B | Implemented | `#delta-2025-11-24-b3-getlinecontent`: cache invalidation tests assert trimmed `GetLineContent` and pin raw terminators via `GetLineRawContent`, mirroring TS `splitLines` expectations. |
| PieceTreeBufferBomTests | BOM metadata exposure via `PieceTreeBuffer.GetBom` (UTF-8 preamble + BOM-only chunk) | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) | B | Added (Sprint 03 R34) | `#delta-2025-11-25-b3-bom`: verifies BOM strings remain metadata (`GetBom()`) while trimmed from `GetText()` even when first chunk contains only `\uFEFF`; targeted rerun `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeBufferBomTests --nologo` → 3/3 green. |
| PieceTreeSearchTests | PieceTree-level search helpers + fuzz harness | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) | B | Implemented | Provides deterministic search + fuzz parity; waiting on PT-005.S9 BufferRange/SearchContext map for full property coverage. |
| PieceTreeDeterministicTests | Prefix-sum / offset / range + CRLF normalization + centralized line-start suites | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) | B | Verified (Sprint 03 R29 / 50 Facts) | Adds TS CRLF normalization bug battery + centralized line-start/chunk variants (lines 1054-1589) atop the existing deterministic harness; anchored在 `#delta-2025-11-25-b3-piecetree-deterministic-crlf`，QA reran `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeDeterministicTests --nologo` -> 50/50 green (3.5s, 2025-11-25) per [`agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-QA.md`](../../agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-QA.md). |
| PieceTreeSnapshotParityTests | Snapshot immutability deterministics (`bug #45564`, `immutable snapshot 1/2/3`) | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) (lines 1888-1998) | B | Verified (Sprint 03 R29 / 4 Facts) | Ports the TS snapshot scripts verbatim using `TextModel.CreateSnapshot()` + the new `SnapshotReader` helper so draining snapshots mirrors TS `getValueInSnapshot`. Tagged `#delta-2025-11-25-b3-piecetree-snapshot`; QA reran (Run R29 @ 2025-11-24 18:38:57Z) `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotParityTests --nologo` → 4/4 green (1.7s) followed by the full baseline `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → 312/312 green (53.6s). |
| TextModelTests | TextModel lifecycle, BOM/EOL options | [ts/src/vs/editor/test/common/model/textModel.test.ts](../../ts/src/vs/editor/test/common/model/textModel.test.ts) | B | Implemented | `TextModelTests.cs` includes initialization + CRLF normalization; services layer stubs still pending for event stream parity. |
| TextModelSearchTests | Regex/word search parameters, CRLF payloads, `parseSearchRequest` + navigation parity | [ts/src/vs/editor/test/common/model/textModelSearch.test.ts](../../ts/src/vs/editor/test/common/model/textModelSearch.test.ts) | B | Verified (Sprint 03 R37 – changefeed certification) | `#delta-2025-11-25-b3-textmodelsearch`: QA handoff [`B3-TextModelSearch-QA.md`](../../agent-team/handoffs/B3-TextModelSearch-QA.md) captures Run R37 evidence — targeted `TextModelSearchTests` 45/45 green (2.5s) plus full-suite 365/365 green (61.6s) with `PIECETREE_DEBUG=0`. The TS 45-case battery remains deterministic after Porter’s navigation fixes; backlog: Intl.Segmenter word segmentation + WordSeparator cache perf remain open. |
| DiffTests | DiffComputer heuristics (line/char, trim flags) | [ts/src/vs/editor/test/common/diff/diffComputer.test.ts](../../ts/src/vs/editor/test/common/diff/diffComputer.test.ts) | B | Implemented | Legacy diff logic ported; char-change pretty diff + whitespace flag cases still TODO for parity. |
| DecorationTests | Stickiness, injected text, per-line queries | [ts/src/vs/editor/test/common/model/modelDecorations.test.ts](../../ts/src/vs/editor/test/common/model/modelDecorations.test.ts) | B | Implemented | Coverage now includes metadata round-trips, per-line queries, owner filters, add/change/remove events; stickiness edge cases handled via new `DecorationStickinessTests`. |
| DecorationStickinessTests | `TrackedRangeStickiness` matrix (insert at edges, forceMoveMarkers overrides) | [ts/src/vs/editor/test/common/model/modelDecorations.test.ts](../../ts/src/vs/editor/test/common/model/modelDecorations.test.ts) | B | Implemented | Mirrors TS "Decorations and editing" insert-before/after expectations across the four stickiness modes; targeted suite (`DecorationStickinessTests.cs`) + rerun command documented below. |
| IntervalTreeTests | IntervalTree AcceptReplace scenarios (insert, delete, replace, forceMove, DecorationChange payloads) | [ts/src/vs/editor/common/model/intervalTree.ts](../../ts/src/vs/editor/common/model/intervalTree.ts) | B | Implemented (Sprint 04 WS3-QA) | `#delta-2025-11-26-ws3-qa`: 13 tests covering AcceptReplace scenarios (insert/delete/replace/forceMove), DecorationChange payload verification, CollapseOnReplaceEdit, and regression tests for large decoration counts; targeted rerun `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter IntervalTreeTests --nologo` → 13/13 green. |
| IntervalTreePerfTests | IntervalTree performance harness (10k decorations, batch edits, DEBUG counters) | [ts/src/vs/editor/common/model/intervalTree.ts](../../ts/src/vs/editor/common/model/intervalTree.ts) | B | Implemented (Sprint 04 WS3-QA) | `#delta-2025-11-26-ws3-qa`: 7 performance tests validating O(log n) edit performance with 10k decorations, batch/single/replace operations, query performance, and DEBUG counter accessibility (nodesRemoved, requestNormalizeHits); targeted rerun `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter IntervalTreePerfTests --nologo` → 7/7 green (~7s). |
| PieceTreeBufferApiTests | PieceTree buffer API methods (getLineCharCode, getNearestChunk, buffer equality) | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) (lines 1786-1888) | B | Implemented (Sprint 04 WS5-QA) | `#delta-2025-11-26-ws5-qa`: 17 tests covering GetLineCharCode (issue #45735/#47733), GetCharCode, buffer equality, and buffer API behavior after insert/delete/mixed operations; targeted rerun `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeBufferApiTests --nologo` → 17/17 green. |
| PieceTreeSearchRegressionTests | PieceTree search regression tests (TS issues #45892, #45770) | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) (lines 2070-2118) | B | Implemented (Sprint 04 WS5-QA) | `#delta-2025-11-26-ws5-qa`: 9 tests covering empty buffer search (#45892), findInNode boundary checks (#45770), search from middle, search after multiple edits; targeted rerun `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchRegressionTests --nologo` → 9/9 green. |
| TextModelIndentationTests | TextModel indentation handling (guessIndentation patterns) | [ts/src/vs/editor/test/common/model/textModel.test.ts](../../ts/src/vs/editor/test/common/model/textModel.test.ts) | B | Implemented (Sprint 04 WS5-QA) | `#delta-2025-11-26-ws5-qa`: 19 tests covering tab/space indentation scenarios (Python, YAML, TypeScript, Java styles), ModelRespectsTabSizeOption, plus 1 skipped test documenting GuessIndentation API gap; targeted rerun `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelIndentationTests --nologo` → 19/19 green, 1 skipped. |
| DocUIFindDecorationsTests | DocUI find overlay parity (range highlight trimming, scope resolution, overview throttling, wrap helpers) | [ts/src/vs/editor/contrib/find/browser/findDecorations.ts](../../ts/src/vs/editor/contrib/find/browser/findDecorations.ts) | B | Implemented | Validates range highlight trimming, live scope resolution (newline retention + edit tracking), viewport-aware overview approximation, `GetCurrentMatchesPosition`, and wrap-around helpers; latest delta `#delta-2025-11-24-b3-docui-staged` adds **CollapsedCaretAtMatchStartReturnsIndex** to guard caret overlap + reset scenarios for Porter’s FindDecorations fix. |
| MarkdownRendererTests | DocUI diff overlay + owner routing | [TODO – locate DocUI find widget snapshot/browser smoke tests (ts/test/browser/*)](../../docs/plans/ts-test-alignment.md#appendix-%E2%80%93-ts-test-inventory-placeholder) | C | Implemented (partial) | Snapshot parity for diff markers landed; upstream TS widget tests still unidentified → Info-Indexer to surface canonical path. |
| DocUIFindModelTests | DocUI find model binding + overlays | [ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts](../../ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts) | B | ✅ Complete (43/43 + Tests44-49) | Batch #2 已落地 39 个 TS parity 测试；Batch #3 (`#delta-2025-11-24-find-scope`, `#delta-2025-11-24-find-replace-scope`) 追加 Tests44–48；B3-FM multi-selection (`#delta-2025-11-24-b3-fm-multisel`) 重新启用 **Test07_MultiSelectionFindModelNextStaysInScopeOverlap** 与 **Test08_MultiSelectionFindModelNextStaysInScope**，Harness `DocUI/TestEditorContext.cs` 现支持 `SetSelections` 多光标注入并同步 `FindModel.SetSelections`。最新 delta `#delta-2025-11-24-find-primary` 引入 **Test49_SelectAllMatchesRespectsPrimarySelectionOrder** 并在 2025-11-24 rerun（PIECETREE_DEBUG=0）中记录 49/49 green（2.6s），确认 Tests07/08/49 持续通过。Commands: (1) `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.DocUI.FindModelTests.Test07_MultiSelectionFindModelNextStaysInScopeOverlap|FullyQualifiedName=PieceTree.TextBuffer.Tests.DocUI.FindModelTests.Test08_MultiSelectionFindModelNextStaysInScope" --nologo` → 2/2, 1.7s；(2) `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~FindModelTests" --nologo` → 49/49, 2.6s；(3) `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → 242/242, 2.9s. |
| DocUIFindModelTests – B3-FM subset | SelectAllMatches search-scope ordering + primary cursor fidelity | [ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts](../../ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts) | B | ✅ Complete (2/2) | `DocUIFindModelTests.Test28/Test29` 覆盖 TS `selectAllMatches` + issue #14143 case；匹配多光标并按 range 排序，保持主光标不变。 |
| WordBoundaryTests | Word separator + boundary validation | [ts/src/vs/editor/common/core/wordCharacterClassifier.ts](../../ts/src/vs/editor/common/core/wordCharacterClassifier.ts) | A | Deferred (Batch #3) | 10 个测试覆盖 ASCII separators、Unicode、multi-char operators（`->`、`::`）；文档化 CJK/Thai 限制（无 Intl.Segmenter）。扩展 TextModelSearchTests.cs 添加 5 个 wholeWord 场景（regex/simple/case-insensitive/multiline 组合）。 |
| DocUIFindControllerTests | FindController command parity（issue #1857/#3090/#6149/#41027/#9043/#27083/#58604/#38232 + scope persistence + whitespace Ctrl/Cmd+F3 + Alt+Enter parity） | [ts/src/vs/editor/contrib/find/test/browser/findController.test.ts](../../ts/src/vs/editor/contrib/find/test/browser/findController.test.ts) | B | ✅ Complete (+ OI-013/OI-015) | `DocUIFindControllerTests.cs` + `TestEditorHost`/storage/clipboard stubs cover navigation loops、regex seed auto-escape、scope lifecycle、replace focus、selection-seeded regex、auto find-in-selection fallback (**AutoFindInSelectionAppliesDuringFallbackStart**), backward helpers (**Issue3090_PreviousMatchLoopsWithinSingleLine** / **Issue38232_PreviousSelectionMatchRegex**) 与 Alt+Enter wiring (**SelectAllMatchesActionAppliesSelections**). PreserveCase 默认值/存储回填、EmptyClipboard no-op、SearchScope persistence + whitespace Ctrl/Cmd+F3 regressions tracked via `#delta-2025-11-23-b3-fc-core`、`#delta-2025-11-23-b3-fc-scope`; 最新 `#delta-2025-11-23-b3-fc-lifecycle` 覆盖 Ctrl+F reseed parity、`SeedSearchStringMode.Never` replace 护栏、Cmd+E multi-line/word seeds（issues #47400/#109756）、FindModel lifecycle/disposal 测试；`#delta-2025-11-23-b3-fc-regexseed` 新增 Cmd+E regex 多行选择保持字面文本（27 测试）。 |
| DocUIFindSelectionTests | Selection-derived search string heuristics | [ts/src/vs/editor/contrib/find/test/browser/find.test.ts](../../ts/src/vs/editor/contrib/find/test/browser/find.test.ts) | A | ✅ Complete (+ hyphen regression) | `DocUIFindSelectionTests.cs` ports the 3 `find.test.ts` cases (cursor word seed, single-line selection, multiline null) using `SelectionTestContext` 并新增 **RespectsCustomWordSeparatorsHyphen** 覆盖 OI-014（wordSeparators plumbing）。Tracks `#delta-2025-11-23-b3-fsel`. |
| ReplacePatternTests | ReplacePattern parser + case preservation | [ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts](../../ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts) | A | ✅ Complete | Batch #1 (2025-11-22) – 23 tests covering escape/backslash chains, `$n`/`$&` permutations, `\u/\l/\U/\L` case ops, JS semantics, preserve-case helpers. Files: `ReplacePatternTests.cs`, `Core/ReplacePattern.cs`, `Rendering/DocUIReplaceController.cs`. |
| CursorCoreTests (Stage 1) | Cursor state wiring, tracked ranges, dual-mode behavior | [ts/src/vs/editor/common/cursor/oneCursor.ts](../../ts/src/vs/editor/common/cursor/oneCursor.ts) | B | ✅ Implemented (CL7 Stage 1 QA 2025-11-28) | `#delta-2025-11-28-cl7-cursor-wiring`: 9 new tests for state validation (out-of-bounds clamping), tracked range survival after edit, dual-mode flag behavior, ReadSelectionFromMarkers correctness; targeted rerun `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~CursorCoreTests.Cursor_" --nologo` → 9/9 green (0.5s). |
| CursorCollectionTests | CursorCollection state management, normalize, tracked selection lifecycle | [ts/src/vs/editor/common/cursor/cursorCollection.ts](../../ts/src/vs/editor/common/cursor/cursorCollection.ts) | B | ✅ Implemented (CL7 Stage 1 QA 2025-11-28) | `#delta-2025-11-28-cl7-cursor-wiring`: 33 tests for SetStates creates correct cursors, Normalize merges overlapping/touching selections, MultiCursorMergeOverlapping config, StartTrackingSelections/ReadSelectionFromMarkers lifecycle, LastAddedCursorIndex, KillSecondaryCursors, GetViewPositions/GetTopMostViewPosition/GetBottomMostViewPosition; targeted rerun `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter CursorCollectionTests --nologo` → 33/33 green (1.8s). |
| SharedHarnessExampleTests | Shared harness helper demonstrations (TestEditorBuilder, CursorTestHelper, WordTestUtils, SnapshotTestUtils) | [agent-team/handoffs/WS5-PORT-Harness-Result.md](../../agent-team/handoffs/WS5-PORT-Harness-Result.md) | B | Implemented (Sprint 04 WS5-PORT) | `#delta-2025-11-26-ws5-port-harness` / `#delta-2025-11-26-sprint04-r1-r11`: 28-sample suite showing how to compose the shared harness; targeted rerun `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter SharedHarnessExampleTests --nologo` → 28/28 green (1.5s, 2025-11-26). |
| PieceTreeNormalizationTests | CR/LF normalization edge cases + raw line coverage | [ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts](../../ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts) | B | Implemented | `#delta-2025-11-24-b3-getlinecontent`: normalization suite now expects trimmed `GetLineContent` output and asserts `GetLineRawContent` to ensure CR/LF bytes remain in backing buffers. |

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
| **Snapshot** | `PieceTreeSnapshot` + `TextModel.CreateSnapshot` | Verified | `PieceTreeSnapshotTests.cs` (SnapshotReadsContent, SnapshotIsImmutable) + `PieceTreeSnapshotParityTests.cs` (`bug #45564`, `immutable snapshot 1/2/3`) + `TextModelSnapshotTests.cs` (CreateSnapshotReturnsWrapper, AggregatesChunksUntilThreshold, SkipsEmptyChunks, RepeatedReadsAfterEos) — `#delta-2025-11-25-b3-textmodel-snapshot` |
| **Normalization** | `PieceTreeModel.NormalizeEOL` + `ChunkUtilities.NormalizeChunks` | Verified | `PieceTreeNormalizationTests.cs` (Delete_CR_In_CRLF, Line_Breaks_Replacement) |

## AA3-009 – Decorations & DocUI Regression Coverage

| Scenario | Focus | Signals | Status | Reference |
| --- | --- | --- | --- | --- |
| CL4.F1 – Decoration metadata round-trip & queries | Injected text line buckets, margin/glyph/font helpers, overview/minimap metadata | `DecorationTests.DecorationOptionsParityRoundTripsMetadata`, `DecorationTests.InjectedTextQueriesSurfaceLineMetadata`, `DecorationTests.DecorationsChangedEventIncludesMetadata` | Covered | `tests/TextBuffer.Tests/DecorationTests.cs` |
| CL4.F3 – Stickiness & `forceMoveMarkers` parity | `DecorationRangeUpdater` honoring TS semantics for collapsed ranges and forced moves | Covered | `DecorationTests.ForceMoveMarkersOverridesStickinessDefaults` |
| CL4.F4 – DocUI diff snapshot plumbing | Markdown renderer emits diff markers (add/delete/insertion) using decoration metadata | Covered | `MarkdownRendererTests.TestRender_DiffDecorationsExposeGenericMarkers` |
| CL4.F5 – Find decorations stickiness + TextModel decoration queries | Range highlight trimming, overview throttling, `GetAllDecorations`/`GetLineDecorations` APIs, DocUI navigation helpers | Covered | `DecorationStickinessTests.InsertionsAtEdgesMatchStickinessMatrix`, `DocUIFindDecorationsTests.RangeHighlightTrimsTrailingBlankLines`, `DocUIFindDecorationsTests.FindScopesPreserveTrailingNewline`, `DocUIFindDecorationsTests.FindScopesTrackEdits`, `DocUIFindDecorationsTests.OverviewThrottlingRespectsViewportHeight`, `DecorationTests.GetLineDecorationsReturnsVisibleMetadata` |

**Total Tests Passing**: 585 (`export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`, 62s, 2025-11-26 WS5-QA Sprint 04 — tagged `#delta-2025-11-26-ws5-qa`, evidence in `agent-team/handoffs/WS5-QA-Result.md`; targeted PieceTreeBufferApi/PieceTreeSearchRegression/TextModelIndentation reruns 44/44+1 skipped green per same handoff)
**Date**: 2025-11-26

## WS5 harness

Anchors: `#delta-2025-11-26-sprint04-r1-r11`, `#delta-2025-11-26-ws5-port-harness`. The shared harness helpers (`TestEditorBuilder`, `CursorTestHelper`, `WordTestUtils`, `SnapshotTestUtils`) now ship with executable documentation in `SharedHarnessExampleTests`, ensuring future suites reuse the standardized builder/cursor/word infrastructure before porting new TS batteries.

### Targeted reruns (WS5-PORT Shared Harness, 2025-11-26)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter SharedHarnessExampleTests --nologo` | 28/28 green (1.5s) | Demonstrates shared harness usage (builder + cursor + word + snapshot helpers) and guards the helper surface as part of `#delta-2025-11-26-sprint04-r1-r11` / `#delta-2025-11-26-ws5-port-harness`. |

## B3-PieceTree-Fuzz Harness (Sprint 03 R25 – #delta-2025-11-23-b3-piecetree-fuzz)

| Test | Scope | Focus | Owner | Status | Reference |
| --- | --- | --- | --- | --- | --- |
| PieceTreeFuzzHarnessTests.FuzzHarnessRunsShortDeterministicSequence | R25 | Deterministic harness run (env-seeded RNG) verifying per-iteration inserts/deletes plus range diff snapshots | Porter-CS | Added (pass) | `PieceTreeFuzzHarnessTests.FuzzHarnessRunsShortDeterministicSequence` |
| PieceTreeFuzzHarnessTests.HarnessDetectsExternalCorruption | R25 | Ensures harness detects external mutations by diffing expected vs actual text/logs | Porter-CS | Added (pass) | `PieceTreeFuzzHarnessTests.HarnessDetectsExternalCorruption` |
| PieceTreeFuzzHarnessTests.RandomTestOneMatchesTsScript | CI-1 | Replays TS `random test 1` insert script (lines 271-285) via harness helper, validating deterministic parity (#delta-2025-11-24-b3-piecetree-fuzz) | Porter-CS | Added (pass) | `PieceTreeFuzzHarnessTests.RandomTestOneMatchesTsScript` |
| PieceTreeFuzzHarnessTests.RandomTestTwoMatchesTsScript | CI-1 | Replays TS `random test 2` insert script (lines 285-296) to guard offset ordering (#delta-2025-11-24-b3-piecetree-fuzz) | Porter-CS | Added (pass) | `PieceTreeFuzzHarnessTests.RandomTestTwoMatchesTsScript` |
| PieceTreeFuzzHarnessTests.RandomTestThreeMatchesTsScript | CI-1 | Replays TS `random test 3` (lines 296-312) to ensure chained inserts + CRLF payloads stay deterministic (#delta-2025-11-24-b3-piecetree-fuzz) | Porter-CS | Added (pass) | `PieceTreeFuzzHarnessTests.RandomTestThreeMatchesTsScript` |
| PieceTreeFuzzHarnessTests.RandomDeleteOneMatchesTsScript | CI-1 | Ports TS `random delete 1` mix of inserts/deletes (lines 331-360) (#delta-2025-11-24-b3-piecetree-fuzz) | Porter-CS | Added (pass) | `PieceTreeFuzzHarnessTests.RandomDeleteOneMatchesTsScript` |
| PieceTreeFuzzHarnessTests.RandomDeleteTwoMatchesTsScript | CI-1 | Ports TS `random delete 2` (lines 360-385) to guard delete-vs-insert ordering (#delta-2025-11-24-b3-piecetree-fuzz) | Porter-CS | Added (pass) | `PieceTreeFuzzHarnessTests.RandomDeleteTwoMatchesTsScript` |
| PieceTreeFuzzHarnessTests.RandomDeleteThreeMatchesTsScript | CI-1 | Ports TS `random delete 3` (lines 385-404) including CRLF insert + delete spans (#delta-2025-11-24-b3-piecetree-fuzz) | Porter-CS | Added (pass) | `PieceTreeFuzzHarnessTests.RandomDeleteThreeMatchesTsScript` |
| PieceTreeFuzzHarnessTests.RandomChunksMatchesTsSuite | CI-2 | Multi-chunk seeding (5×1000) fuzz loop w/ deterministic RNG to mirror TS `random chunks` (lines 1668-1708) (#delta-2025-11-24-b3-piecetree-fuzz) | Porter-CS | Added (pass) | `PieceTreeFuzzHarnessTests.RandomChunksMatchesTsSuite` |
| PieceTreeFuzzHarnessTests.RandomChunksTwoMatchesTsSuite | CI-2 | Multi-chunk seeding (1×1000) + per-iteration asserts for TS `random chunks 2` (lines 1708-1725) (#delta-2025-11-24-b3-piecetree-fuzz) | Porter-CS | Added (pass) | `PieceTreeFuzzHarnessTests.RandomChunksTwoMatchesTsSuite` |

## B3-PieceTree-Fuzz Deterministic Suites (Sprint 03 R28 – #delta-2025-11-24-b3-piecetree-fuzz)

| Test Group | Scope | Focus | Owner | Status | Reference |
| --- | --- | --- | --- | --- | --- |
| PrefixSumBasic/Append/Insert/Delete/AddDelete | R28 | Mirrors TS `prefix sum for line feed` baselines to assert line-count, `GetPositionAt`, and `GetOffsetAt` parity after deterministic insert/delete sequences. | Porter-CS | Added (pass) | `PieceTreeDeterministicTests.PrefixSum*` |
| PrefixSumRandomBugScripts & RB-tree regressions | R28 | Replays TS `random insert/delete` + RB-tree bug repros (ts lines ~600-700) ensuring `PieceTreeFuzzHarness` catches CR/LF corruption and invariant drift every step. | Porter-CS | Added (pass) | `PieceTreeDeterministicTests.PrefixSumInsertRandomBug*`, `PrefixSumDeleteRandomBug*` |
| Offset ↔ Position mapping | R28 | Ports TS `offset 2 position` suites so `GetPositionAt` and `GetOffsetAt` remain bijective after mixed edits. | Porter-CS | Added (pass) | `PieceTreeDeterministicTests.OffsetToPositionRandomBugOneMatchesTsScript` |
| GetValueInRange & raw line content | R28 | Recreates TS `get text in range` coverage, validating `GetValueInRange`, `GetLineRawContent`, and CRLF normalization across edits (including empty ranges). | Porter-CS | Added (pass) | `PieceTreeDeterministicTests.GetTextInRange*`, `GetLineRawContent*` |
| Range bug scripts | R28 | Covers random bug suites from TS (lines ~760-940) to guard mixed insert/delete/replace sequences with CR/LF payloads. | Porter-CS | Added (pass) | `PieceTreeDeterministicTests.GetTextInRangeRandomBug*` |
| CRLF normalization & centralized lineStarts | R29 | Ports TS `CRLF` + `centralized lineStarts with CRLF` suites (lines 1054-1589), including delete regressions, random bug scripts, and chunk variants; harness assertions cover line-count, raw contents, and line-start tables. | Porter-CS | Added (QA verified 50/50, 3.5s) | `PieceTreeDeterministicTests.Crlf*`, `CentralizedLineStarts*` — evidence in [`agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-QA.md`](../../agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-QA.md). |

Helper updates: Added `PieceTreeBufferAssertions` (line-count/offset helpers) and `PieceTreeScript` (insert/delete/replace scripting) under `tests/TextBuffer.Tests/Helpers/` to keep deterministic scripts aligned with TS fixtures.

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
| 2025-11-26 (WS5-QA Sprint 04) | 585 | 584 | 0 | 62s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` — certifies WS5-QA high-risk deterministic tests (`#delta-2025-11-26-ws5-qa`); +44 tests from PieceTreeBufferApiTests (17) + PieceTreeSearchRegressionTests (9) + TextModelIndentationTests (19), 1 skipped (GuessIndentation API gap). Evidence: [`agent-team/handoffs/WS5-QA-Result.md`](../../agent-team/handoffs/WS5-QA-Result.md). |
| 2025-11-26 (WS3-QA Sprint 04) | 471 | 471 | 0 | ~10s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "IntervalTreeTests\|IntervalTreePerfTests" --nologo` — certifies WS3-QA IntervalTree + perf tests (`#delta-2025-11-26-ws3-qa`); +20 tests from IntervalTreeTests (13) + IntervalTreePerfTests (7). Evidence: [`agent-team/handoffs/WS3-QA-Result.md`](../../agent-team/handoffs/WS3-QA-Result.md). |
| 2025-11-26 (WS123-QA Sprint 04) | 440 | 440 | 0 | 62.1s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` — certifies WS1/WS2/WS3 implementations (`#delta-2025-11-26-ws1-searchcore`, `#delta-2025-11-26-ws2-port`, `#delta-2025-11-26-ws3-tree`); +75 tests from Range/Selection helpers (WS2). Evidence: [`agent-team/handoffs/WS123-QA-Result.md`](../../agent-team/handoffs/WS123-QA-Result.md). |
| 2025-11-26 (B3-TextModelSearch QA R37) | 365 | 365 | 0 | 61.6s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` — certifies `#delta-2025-11-25-b3-textmodelsearch`; targeted `TextModelSearchTests` 45/45 green (2.5s) in the same run. Evidence: [`agent-team/handoffs/B3-TextModelSearch-QA.md`](../../agent-team/handoffs/B3-TextModelSearch-QA.md). |
| 2025-11-25 (B3-TextModelSearch QA R36) | 365 | 365 | 0 | 56.0s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` — Investigator rerun captured in `agent-team/handoffs/DocReview-20251126-INV.md` (mirrored by `agent-team/handoffs/DocFix-20251126-Porter.md`) confirmed the Issue #53415 fix under changefeed `#delta-2025-11-25-b3-textmodelsearch`, yielding a 365/365 full-suite baseline at 56.0s. |
| 2025-11-25 (B3-Search-Offset QA R31) | 324 | 324 | 0 | 58.2s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` — Full TextBuffer sweep paired with the search offset cache drop; targeted rerun logged below and summarized in `agent-team/handoffs/B3-PieceTree-SearchOffset-QA.md` for changefeed `#delta-2025-11-25-b3-search-offset`. |
| 2025-11-25 (B3-TextModel-Snapshot QA R30) | 321 | 321 | 0 | 59.7s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` — Snapshot wrapper validation run for `#delta-2025-11-25-b3-textmodel-snapshot`; paired with the targeted snapshot/search/deterministic filters listed below and logged in `agent-team/handoffs/B3-TextModel-Snapshot-QA.md`. |
| 2025-11-25 (B3-PieceTree-Snapshot QA R29) | 312 | 312 | 0 | 53.6s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` — Sprint 03 Run R29 verification of `#delta-2025-11-25-b3-piecetree-snapshot`; paired with targeted filter (4/4, 1.7s) and summarized in `agent-team/handoffs/B3-PieceTree-Snapshot-QA.md`. |
| 2025-11-25 (B3-PieceTree-Deterministic-CRLF QA) | 308 | 308 | 0 | 67.2s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – QA reran the full suite after CRLF + centralized line-start expansion; deterministic filter (50/50, 3.5s) logged separately under `Targeted reruns` per [`agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-QA.md`](../../agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-QA.md). |
| 2025-11-24 (B3-PieceTree-Deterministic) | 280 | 280 | 0 | 53.0s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – Adds `PieceTreeDeterministicTests` (22 facts + helpers) mirroring TS prefix-sum/offset/range suites; anchors to `#delta-2025-11-24-b3-piecetree-deterministic`. |
| 2025-11-24 (B3-TestFailures fix) | 253 | 253 | 0 | 59.3s | `export PIECETREE_DEBUG=0 && dotnet test -v m` – Latest rerun confirming per-model sentinel + `GetLineContent` parity fixes (`#delta-2025-11-24-b3-sentinel`, `#delta-2025-11-24-b3-getlinecontent`). |
| 2025-11-24 (Sprint 03 R25 – B3-PieceTree-Fuzz Harness) | 245 | 245 | 0 | 4.1s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – Harness utilities + RB-tree invariant audit (`#delta-2025-11-23-b3-piecetree-fuzz`). |
| 2025-11-24 (Batch #3 – B3-FM MultiSelection) | 242 | 242 | 0 | 2.9s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – DocUI FindModel multi-selection scope parity run（Tests07/08 重启 + prior scope fixes）并作为 Sprint 03 QA 关账基线；`#delta-2025-11-24-b3-fm-multisel`. |
| 2025-11-23 (Batch #3 – B3-Decor Stickiness Review) | 235 | 235 | 0 | 2.9s | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – CI-1/CI-2/CI-3 + W-1/W-2 fixes (live scopes, newline retention, viewport-aware overview, dynamic owner) (`#delta-2025-11-23-b3-decor-stickiness-review`). |
| 2025-11-23 (Batch #3 – B3-Decor Stickiness) | 233 | 233 | 0 | 3.0s | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – Range highlight/overview/stickiness parity run (`#delta-2025-11-23-b3-decor-stickiness`). |
| 2025-11-23 (B3-FC-RegexSeed hotfix) | 218 | 218 | 0 | 3.3s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – Full suite after regex seeding fix (`#delta-2025-11-23-b3-fc-regexseed`). |
| 2025-11-23 (Batch #3 – B3-FC-Core) | 199 | 199 | 0 | 6.9s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – DocUI FindController core parity run (`#delta-2025-11-23-b3-fc-core`). |
| 2025-11-23 (Batch #3 – B3-FSel) | 189 | 189 | 0 | 3.5s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – Find selection helper parity run (`#delta-2025-11-23-b3-fsel`). |
| 2025-11-23 (Batch #3 – B3-FM) | 186 | 186 | 0 | 3.0s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` – B3 SelectAllMatches parity run (#delta-2025-11-23-b3-fm). |
| 2025-11-23 (Batch #2) | 187 | 187 | 0 | 2.9s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --logger "trx;LogFileName=TestResults/batch2-full.trx" --nologo` – B2 FindModel QA baseline (+45 tests from 142) anchored at `#delta-2025-11-23`; evidence: `agent-team/handoffs/archive/B2-Final-QA.md`. |
| 2025-11-22 (Batch #1) | 142 | 142 | 0 | 2.6s | `dotnet test --logger "trx;LogFileName=batch1-full.trx" --nologo` – B1 ReplacePattern QA baseline (+23 tests from 119). TRX: `TestResults/batch1-full.trx`. |
| 2025-11-21 18:05 UTC | 119 | 119 | 0 | 7.4s | `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` (AA4-009 revalidation after Porter-CS drop; deterministic full-suite count recorded for CL5/CL6). |
| 2025-11-21 09:10 UTC | 105 | 105 | 0 | 2.1s | Earlier AA4-006 verification baseline before Porter-CS expanded CL5/CL6 suites (kept for historical comparison). |

### Targeted reruns (WS5-QA High-Risk Deterministic Tests, 2025-11-26)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeBufferApiTests --nologo` | 17/17 green (1.8s) | WS5-QA PieceTree buffer API parity – GetLineCharCode (issue #45735/#47733), GetCharCode, buffer equality, post-edit behavior (`#delta-2025-11-26-ws5-qa`). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchRegressionTests --nologo` | 9/9 green (1.7s) | WS5-QA PieceTree search regressions – empty buffer search (#45892), findInNode boundary (#45770), search from middle, search after edits (`#delta-2025-11-26-ws5-qa`). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelIndentationTests --nologo` | 19/19 green, 1 skipped (1.9s) | WS5-QA TextModel indentation handling – tab/space scenarios (Python, YAML, TypeScript, Java styles), ModelRespectsTabSizeOption; 1 skipped for missing GuessIndentation API (`#delta-2025-11-26-ws5-qa`). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "PieceTreeBufferApiTests\|PieceTreeSearchRegressionTests\|TextModelIndentationTests" --nologo` | 44/44 green, 1 skipped (3.2s) | WS5-QA combined high-risk deterministic suite (`#delta-2025-11-26-ws5-qa`). |

### Targeted reruns (WS3-QA IntervalTree, 2025-11-26)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter IntervalTreeTests --nologo` | 13/13 green (2.2s) | WS3-QA IntervalTree AcceptReplace scenarios – insert/delete/replace/forceMove, DecorationChange payloads, CollapseOnReplaceEdit, regression for large decoration counts (`#delta-2025-11-26-ws3-qa`). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter IntervalTreePerfTests --nologo` | 7/7 green (6.6s) | WS3-QA IntervalTree performance harness – 10k decorations batch/single/replace/query operations, DEBUG counter validation (`#delta-2025-11-26-ws3-qa`). |

### Targeted reruns (CL7 Stage 1 Cursor Wiring QA, 2025-11-28)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter CursorCollectionTests --nologo` | 33/33 green (1.8s) | CL7 Stage 1 CursorCollection tests – SetStates creates/removes cursors, Normalize merges overlapping selections, tracked selection lifecycle, lastAddedCursorIndex, KillSecondaryCursors, view position queries (`#delta-2025-11-28-cl7-cursor-wiring`). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~CursorCoreTests.Cursor_" --nologo` | 9/9 green (0.5s) | CL7 Stage 1 Cursor state wiring tests – state validation (out-of-bounds clamping), tracked range survival after edit, dual-mode behavior (flag on vs off), ReadSelectionFromMarkers correctness (`#delta-2025-11-28-cl7-cursor-wiring`). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~Cursor" --nologo` | 114/114 green, 2 skips (0.5s) | Full Cursor test umbrella (CursorCoreTests + CursorCollectionTests + CursorMultiSelectionTests) after CL7 Stage 1 wiring (`#delta-2025-11-28-cl7-cursor-wiring`). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | 681/681 green, 2 skips (101s) | Full suite validation after CL7 Stage 1 (+42 new tests from 641 baseline) (`#delta-2025-11-28-cl7-cursor-wiring`). |

### Targeted reruns (WS1/WS2/WS3 QA, 2025-11-26)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter RangeSelectionHelperTests --nologo` | 75/75 green (1.6s) | WS2 Range/Selection helper APIs parity run – covers Range static methods, instance methods, Selection static methods, and TextPosition extensions (`#delta-2025-11-26-ws2-port`). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo` | 5/5 green (1.7s) | WS1 PieceTree search offset cache validation – render-whitespace regression + normalized EOL insert scenarios (`#delta-2025-11-26-ws1-searchcore`). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DecorationTests --nologo` | 12/12 green (1.7s) | WS3 IntervalTree/Decoration coverage – metadata round-trips, injected text queries, owner filters, per-line queries (`#delta-2025-11-26-ws3-tree`). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DecorationStickinessTests --nologo` | 4/4 green (1.7s) | WS3 TrackedRangeStickiness matrix – insert at edges behavior across four stickiness modes (`#delta-2025-11-26-ws3-tree`). |

### Targeted reruns (PORT-PT-Search Step12, 2025-11-27)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeDeterministicTests --nologo` | 50/50 green (1.9s) | Confirms deterministic prefix-sum/offset/CRLF suites stay green after NodeAt2 tuple reuse + diagnostics; logged in [`agent-team/handoffs/PORT-PT-Search-Step12-QA.md`](../../agent-team/handoffs/PORT-PT-Search-Step12-QA.md) for upcoming changefeed [`#delta-2025-11-27-ws1-port-search-step12`](../../agent-team/indexes/README.md#delta-2025-11-27-ws1-port-search-step12). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeFuzzHarnessTests --nologo` | 15/15 green (63.6s) | Replays the env-seeded fuzz harness (random/CRLF/chunk scripts) to stress tuple reuse + cache invalidation; QA log mirrors the runtime + confirms no harness corruption. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~CRLFFuzzTests --nologo` | 13/13 green (13.0s) | Validates the CRLF bridge + `_lastChangeBufferPos` rewinds remained intact while wiring the new tuple path. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeSearchRegressionTests --nologo` | 13/13 green (2.1s) | Exercises search-from-middle, boundary, and edit-after-search regressions with the updated cache hits. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeSearchOffsetCacheTests --nologo` | 5/5 green (1.8s) | Ensures the release-visible cache diagnostics still report warm entries and invalidation remains targeted. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | 639/639 green, 2 skips (107.4s) | Full sweep capturing the Step12 runtime (CursorCore skips remain tied to `#delta-2025-11-26-aa4-cl7-*`). Evidence linked in the QA handoff for Info-Indexer to publish the new changefeed. |

### Targeted reruns (B3-Decor Review, 2025-11-23)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindDecorationsTests --nologo` | 9/9 green | Validates live scope tracking (newline retention + edit tracking), viewport-aware overview throttling, plus new **CollapsedCaretAtMatchStartReturnsIndex** guard for caret overlap regressions (`#delta-2025-11-23-b3-decor-stickiness-review`). |

### Targeted reruns (B3-FM, 2025-11-23)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~FindModelTests.Test28|FullyQualifiedName~FindModelTests.Test29" --nologo` | 2/2 green | SelectAllMatches regression sweep for FM-01/FM-02 (`#delta-2025-11-23-b3-fm`). |

### Targeted reruns (B3-FSel, 2025-11-23)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindSelectionTests --nologo` | 4/4 green | Selection-derived seed heuristics (cursor word, single-line selection, multi-line null) + hyphen separator regression run (`#delta-2025-11-23-b3-fsel`). |

### Targeted reruns (B3-FC, 2025-11-23)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindControllerTests --nologo` | 27/27 green | DocUI FindController parity run covering issues #1857/#3090/#6149/#41027/#9043/#27083/#58604/#38232，加上 Ctrl+F reseed、`SeedSearchStringMode.Never` replace、Cmd+E multi-line/word seeds（issues #47400/#109756）、lifecycle disposal（`#delta-2025-11-23-b3-fc-core`、`#delta-2025-11-23-b3-fc-scope`、`#delta-2025-11-23-b3-fc-lifecycle`）及 **regex multi-line seed literal parity** (`#delta-2025-11-23-b3-fc-regexseed`). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~DocUIFind --nologo` | 17/17 green (1.6s, **pre-DocUIFindDecorations baseline**) | Aggregated DocUI find suites (controller/model/selection) re-ran after preserve-case/word-separator/clipboard restage; log mirrored into `agent-team/handoffs/AA4-009-QA.md` (`#delta-2025-11-23-docuifind`). Post-2025-11-24 runs expect 39/39; see section below. |

### Targeted reruns (delta-2025-11-24-find-scope)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo` | 44/44 green | Confirms DocUI FindModel scope overrides hydrate via decorations after edits and multi-line scopes follow TS #27083 normalization (`#delta-2025-11-24-find-scope`). |

### Targeted reruns (delta-2025-11-24-find-replace-scope)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo` | 45/45 green | Extends the scope regression sweep with **Test47_RegexReplaceWithinScopeUsesLiveRangesAfterEdit**, ensuring `FindModel.GetMatchesForReplace` hydrates search scopes from live decorations before computing regex captures (`#delta-2025-11-24-find-replace-scope`). |

### Targeted reruns (delta-2025-11-24-find-flush-edit)

### Targeted reruns (B3-PieceTree-Fuzz Harness, 2025-11-24)

| Command | Result | Notes |
| --- | --- | --- |
| `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeFuzzHarnessTests --nologo` | 2/2 green | Harness smoke: deterministic seed loop + corruption detection assertions (`#delta-2025-11-23-b3-piecetree-fuzz`). |

### Targeted reruns (B3-PieceTree-Deterministic-CRLF, 2025-11-25)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeDeterministicTests --nologo` | 50/50 green (3.5s) | 2025-11-25 QA rerun validating CRLF normalization + centralized line-start deterministic suites; see [`agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-QA.md`](../../agent-team/handoffs/B3-PieceTree-Deterministic-CRLF-QA.md). |
| `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeDeterministicTests --nologo` | 22/22 green | Verifies TS prefix-sum/offset/range deterministic suites via `PieceTreeDeterministicTests`; anchors to `#delta-2025-11-24-b3-piecetree-deterministic`. |

### Targeted reruns (B3-TextModel-Snapshot QA sweep, 2025-11-25)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSnapshotTests --nologo` | 4/4 green (1.6s) | Validates the new `TextModelSnapshot` wrapper aggregation (64KB batching, empty-chunk skip, EOS caching) for `#delta-2025-11-25-b3-textmodel-snapshot`; evidence captured in `agent-team/handoffs/B3-TextModel-Snapshot-QA.md`. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotTests --nologo` | 2/2 green (1.7s) | Confirms legacy `PieceTreeSnapshot` invariants remain intact after the wrapper was introduced; logged in the same QA handoff for changefeed `#delta-2025-11-25-b3-textmodel-snapshot`. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotParityTests --nologo` | 4/4 green (1.7s) | Replays TS `bug #45564` + `immutable snapshot 1/2/3` parity scripts to ensure the wrapper still surfaces identical chunk sequences; evidence in `agent-team/handoffs/B3-TextModel-Snapshot-QA.md`. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo` | 5/5 green (1.7s) | Guards the search offset cache so the new wrapper doesn’t perturb cached offsets or invalidation semantics; tied to `#delta-2025-11-25-b3-textmodel-snapshot`. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeDeterministicTests --nologo` | 50/50 green (1.6s) | Re-ran the deterministic prefix-sum/CRLF harness to prove the snapshot change leaves PieceTree invariants untouched; logged alongside the TextModel snapshot QA evidence. |

### Targeted reruns (#delta-2025-11-25-b3-search-offset)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo` | 5/5 green (4.3s) | Dedicated rerun for the search offset cache drop; evidence captured for changefeed `#delta-2025-11-25-b3-search-offset` and summarized in `agent-team/handoffs/B3-PieceTree-SearchOffset-QA.md`. |

### Targeted reruns (delta-2025-11-25-b3-bom)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeBufferBomTests --nologo` | 3/3 green (2.2s) | Validates that `PieceTreeBuffer.GetBom()` retains the UTF-8 BOM metadata for prefixed strings and BOM-only first chunks while keeping `GetText()` free of the marker; evidence for `#delta-2025-11-25-b3-bom`. |

### Targeted reruns (delta-2025-11-25-b3-textmodelsearch)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName=PieceTree.TextBuffer.Tests.TextModelSearchTests_WordBoundaries.Issue53415_WMatchesLineBreaks --nologo` | 1/1 green (1.9s) | Proves the corrected `\tJust` fixture matches TS Issue #53415 expectations for `\W` spans across tabs + blank lines. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSearchTests --nologo` | 45/45 green (2.5s) | Run R37 targeted sweep recorded in [`agent-team/handoffs/B3-TextModelSearch-QA.md`](../../agent-team/handoffs/B3-TextModelSearch-QA.md); certifies `#delta-2025-11-25-b3-textmodelsearch` remains green under the freshly ported battery. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchTests --nologo` | 11/11 green (1.7s) | Confirms the shared search helpers (regex/word boundary) remain stable once TextModelSearch inputs align with TS sources. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo` | 5/5 green (1.8s) | Spot-checks the cache-backed search flows so the TextModel tweaks do not perturb PieceTree offset caching. |

### Targeted reruns (delta-2025-11-24-b3-docui-staged)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo` | 46/46 green | Adds **Test48_FlushEditKeepsFindNextProgress** to the sweep, guarding Porter’s DocUI flush edit fix so `FindNext` progress survives decoration resets. (Legacy alias `FullyQualifiedName~DocUIFindModelTests` has been retired because the suite compiles as `PieceTree.TextBuffer.Tests.DocUI.FindModelTests`.) |

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo` | 46/46 green | Re-ran the FindModel suite after staging fixes to confirm **Test48_FlushEditKeepsFindNextProgress** stays green alongside Tests45–47; logged under `agent-team/handoffs/B3-DocUI-StagedFixes-QA-20251124.md` and tied to `#delta-2025-11-24-b3-docui-staged`. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~DocUIFindDecorationsTests --nologo` | 9/9 green | Captures the collapsed-caret regression guard (**CollapsedCaretAtMatchStartReturnsIndex**) plus scope tracking checks for the staged FindDecorations reset. See `docs/reports/migration-log.md` B3-DocUI-StagedFixes 行与 `agent-team/indexes/README.md#delta-2025-11-24-b3-docui-staged`. |

### Targeted reruns (DocUIFind umbrella refresh, 2025-11-24)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~DocUIFind --nologo` | 39/39 green | Updated expectation for the DocUI find umbrella filter; now also captures `DocUIFindDecorationsTests` and related suites added after `#delta-2025-11-23-docuifind`. Verified during QA handoff `agent-team/handoffs/AA4-Review-QA.md`. |

### Targeted reruns (delta-2025-11-24-b3-fm-multisel)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.DocUI.FindModelTests.Test07_MultiSelectionFindModelNextStaysInScopeOverlap|FullyQualifiedName=PieceTree.TextBuffer.Tests.DocUI.FindModelTests.Test08_MultiSelectionFindModelNextStaysInScope" --nologo` | 2/2 green (1.7s) | Validates overlapping + disjoint multi-selection scope parity lifted from TS Test07/08; harness now feeds multiple selections before hydrating find scopes. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~FindModelTests" --nologo` | 48/48 green (3.3s) | Confirms the entire DocUI FindModel suite (Tests01–48) including new multi-selection cases. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | 242/242 green (2.9s) | Full TextBuffer sweep used to close QA for B3-FM multi-selection and publish `#delta-2025-11-24-b3-fm-multisel`. |

### Targeted reruns (delta-2025-11-24-find-primary)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~FindModelTests" --nologo` | 49/49 green (2.6s) | Latest 2025-11-24 rerun capturing Tests07/08/49 for QA evidence; confirms **Test49_SelectAllMatchesRespectsPrimarySelectionOrder** stays green post-primary ordering fix per `#delta-2025-11-24-find-primary`. |

### Targeted reruns (#delta-2025-11-24-b3-getlinecontent)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~PieceTreeBaseTests.GetLineContent_Cache_Invalidation" --nologo` | Historical: 2/2 green (1.9s); 2025-11-24 rerun: Total=2, Passed=2, Failed=0, Duration=3.8s | Verifies the cache invalidation tests now expect trimmed `GetLineContent` values and assert `GetLineRawContent` to ensure terminators remain in the backing buffer. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~PieceTreeNormalizationTests" --nologo` | Historical: 3/3 green (1.7s); 2025-11-24 rerun: Total=3, Passed=3, Failed=0, Duration=1.6s | Confirms the normalization suite mirrors TS `splitLines` semantics while still checking raw CR/LF bytes via `GetLineRawContent`. |

### Targeted reruns (#delta-2025-11-24-b3-sentinel)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName=PieceTree.TextBuffer.Tests.PieceTreeFuzzHarnessTests.RandomDeleteThreeMatchesTsScript" --nologo` | Historical: 1/1 green (1.6s); 2025-11-24 rerun: Total=1, Passed=1, Failed=0, Duration=1.6s | Ensures per-model sentinels keep `ValidateTreeInvariants` stable while the fuzz harness exercises delete fixups that previously tripped the shared sentinel. |

### Targeted reruns (AA4-009, 2025-11-21)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~PieceTreeBuilderTests|FullyQualifiedName~PieceTreeFactoryTests" --nologo` | 7/7 green | Spot check of CL5 builder/factory regressions (AcceptChunk + preview helpers) to ensure Porter-CS changes remain stable. |
| `export PIECETREE_DEBUG=0 PIECETREE_FUZZ_LOG_DIR=/tmp/aa4-009-fuzz-logs dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~CRLF_RandomFuzz_1000 --nologo` | 1/1 green | Deterministic CRLF fuzz harness (seed 123). Fuzz logs configured to land under `/tmp/aa4-009-fuzz-logs` via `FuzzLogCollector`; no file emitted because the run completed without failures. |

### Targeted reruns (Batch #1, 2025-11-22)

| Command | Result | Notes |
| --- | --- | --- |
| `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~ReplacePatternTests" --logger "trx;LogFileName=batch1-replacepattern.trx" --nologo` | 23/23 green (1.6s) | ReplacePattern专项测试验证。TRX: `TestResults/batch1-replacepattern.trx`. 覆盖解析、捕获组、大小写操作、JS语义等全部23个测试用例。 |

### Batch #1 (TS Portability) Validation Commands

| Command | Purpose | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo --logger "trx;LogFileName=TestResults/batch1-full.trx"` | Full-suite baseline before/after TS Batch #1 drops | Captures aggregate parity; TRX stored under `TestResults/` for changefeed attachments. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --no-build --nologo --filter FullyQualifiedName~ReplacePatternTests --logger "trx;LogFileName=TestResults/batch1-replacepattern.trx"` | Targeted ReplacePattern parity run | Executes escape/backref/case-modifier/preserve-case matrix from `ReplacePatternTests.cs` (23 inline xUnit tests, no external fixtures). |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --no-build --nologo --filter FullyQualifiedName~MarkdownRendererDocUI --logger "trx;LogFileName=TestResults/batch1-markdown.trx"` | Markdown renderer overlay regression sweep | Reuses existing Markdown renderer overlay tests; ensures overlays remain portable when TS snapshots change. |

### Batch #2 (FindModel) Validation Commands

| Command | Purpose | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo --logger "trx;LogFileName=TestResults/batch2-full.trx"` | Full-suite baseline before/after Batch #2 drops | 实际 142 → 187 测试（+45）；用于验证 FindModel 集成不破坏既有测试。 |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~FindModelTests" --logger "trx;LogFileName=TestResults/batch2-findmodel.trx"` | FindModel 专项测试（15 个核心场景） | 验证增量搜索、findNext/Prev、replace、replaceAll、wholeWord、decorations 同步、matches count 更新。参考 B2-QA-Result.md 的 P0/P1/P2 分级。 |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~WordBoundaryTests" --logger "trx;LogFileName=TestResults/batch2-wordboundary.trx"` | Word boundary 专项测试（10 个边界场景） | **Deferred to Batch #3** – WordBoundary tests尚未登陆 187 基线，保留命令模板以备下批落地。 |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --no-build --nologo --filter "FullyQualifiedName~TextModelSearchTests" --logger "trx;LogFileName=TestResults/batch2-textsearch.trx"` | TextModelSearch 扩展测试（包含 wholeWord 场景） | 验证 wholeWord + regex/simple/case-insensitive/multiline 组合（新增 5 个测试）。 |

### Batch #1 ReplacePattern Checklist

| Checklist Item | TS Coverage | C# Plan | Artifacts |
| --- | --- | --- | --- |
| Escape sequences + literal/backslash tails | `parse replace string` (escapes `$`, `\\`, `\\t`, `\\n`, dangling slash) | `ReplacePatternTests.ParseReplaceString_*` xUnit Facts verify `ReplacePiece` tokens | `ReplacePatternTests.cs` (inline test data) |
| `$n`/`$&` capture semantics + numeric parsing | `parse replace string` + `replace has JavaScript semantics` | `ReplacePatternTests.ReplaceUsingCaptureGroups_*` compare `ReplacePattern` output to TS semantics for `$0..$99` combos | `ReplacePatternTests.cs` (inline test data) |
| Case modifiers `\\u/\\l/\\U/\\L/\\E` | `parse replace string with case modifiers` | `ReplacePatternTests.ParseReplaceString_CaseModifiers*` assert stacked ops and cancellation | `ReplacePatternTests.cs` (inline test data) |
| JS semantics & substring vs. full-match replacements | `get replace string ... complete match` & `... sub-string` blocks | `ReplacePatternTests.ReplaceUsingCaptureGroups_*` ensure lookahead/backreference cases align with JS results | `ReplacePatternTests.cs` (inline test data) |
| Issue #19740 empty capture regression | `issue #19740 ... inserts undefined` | `[Fact] ReplacePatternTests.ReplaceUsingCaptureGroups_EmptyGroupYieldsEmptyString` ensures optional capture renders `{}` not `undefined` | `ReplacePatternTests.cs` (inline test data) |
| Preserve-case helper coverage | `buildReplaceStringWithCasePreserved test` + `preserve case` suites | `ReplacePatternTests.BuildReplaceStringWithCasePreserved_*` validate case-preserving logic | `ReplacePatternTests.cs` (inline test data) |

## AA4-007 (CL7) – Cursor Word / Snippet / Multi-select parity (Porter-created tests)

R40（2025-11-26）将 CL7 cursor/snippet backlog 重新标记为 High Risk：word operations、column 选择、snippet session、DocUI 命令需按 `#delta-2025-11-26-aa4-cl7-*` 发布新的 Porter/QA deltas，下表状态已降级为 Gap。

| Test | CL | Focus | Owner | Status | Reference |
| --- | --- | --- | --- | --- | --- |
| CursorMulti_RendersMultipleCursorsAndSelections | CL7 | Multi-cursor rendering / DocUI pipeline | Porter-CS / QA-Automation | Gap – awaiting AA4-003 deltas (`#delta-2025-11-26-aa4-cl7-*`) | `CursorMultiSelectionTests.MultiCursor_RendersMultipleCursorsAndSelections` |
| CursorMulti_EditAtMultipleCursors | CL7 | Batch edits at multiple cursors (insert/replace) | Porter-CS / QA-Automation | Gap – awaiting AA4-003 deltas (`#delta-2025-11-26-aa4-cl7-*`) | `CursorMultiSelectionTests.MultiCursor_EditAtMultipleCursors` |
| MoveWordRight_BasicWords | CL7 | Word navigation (MoveWordRight/Left) | Porter-CS / QA-Automation | Gap – awaiting AA4-003 deltas (`#delta-2025-11-26-aa4-cl7-*`) | `CursorWordOperationsTests.MoveWordRight_BasicWords` |
| MoveWordLeft_BasicWords | CL7 | Word navigation (MoveWordLeft) | Porter-CS / QA-Automation | Gap – awaiting AA4-003 deltas (`#delta-2025-11-26-aa4-cl7-*`) | `CursorWordOperationsTests.MoveWordLeft_BasicWords` |
| DeleteWordLeft_Basic | CL7 | Word delete semantics | Porter-CS / QA-Automation | Gap – awaiting AA4-003 deltas (`#delta-2025-11-26-aa4-cl7-*`) | `CursorWordOperationsTests.DeleteWordLeft_Basic` |
| VisibleColumn_RoundTrip_WithTabs | CL7 | Column-visible/position conversions (tabs) | Porter-CS / QA-Automation | Gap – awaiting AA4-003 deltas (`#delta-2025-11-26-aa4-cl7-*`) | `ColumnSelectionTests.VisibleColumn_RoundTrip_WithTabs` |
| VisibleColumn_AccountsForInjectedText_BeforeAndAfter | CL7 | Injected text influences visible columns | Porter-CS / QA-Automation | Gap – awaiting AA4-003 deltas (`#delta-2025-11-26-aa4-cl7-*`) | `ColumnSelectionTests.VisibleColumn_AcountsForInjectedText_BeforeAndAfter` |
| Cursor_ColumnSelection_Basic | CL7 | Column selection (Alt+Drag/vertical) | Porter-CS / QA-Automation | Gap – awaiting AA4-003 deltas (`#delta-2025-11-26-aa4-cl7-*`) | `ColumnSelectionTests.Cursor_ColumnSelection_Basic` |
| SnippetInsert_CreatesPlaceholders_AndNavigates | CL7 | Snippet insertion & placeholder navigation | Porter-CS / QA-Automation | Gap – awaiting AA4-003 deltas (`#delta-2025-11-26-aa4-cl7-*`) | `SnippetControllerTests.SnippetInsert_CreatesPlaceholders_AndNavigates` |
| MarkdownRenderer_MultiCursorAndSnippet | CL7 | DocUI snapshot for multi-cursor + snippet placeholder rendering | Porter-CS / QA-Automation | Gap – awaiting AA4-003 deltas (`#delta-2025-11-26-aa4-cl7-*`) | `MarkdownRendererTests.(TestRender_MultiCursorAndSnippet - to be added)` |

**Notes:**
- Ownership：`Porter-CS` 负责提交新的 cursor/word/snippet/DocUI 修复；`QA-Automation` 维护该矩阵并在每个 `#delta-2025-11-26-aa4-cl7-*` patch 落地后更新 rerun 证据。
- MarkdownRenderer snapshot 仍计划中，将随 Porter patch（同一 changefeed 套件）一起补充测试与渲染基线。

**Next Steps:** QA 继续保持上述 CL7 行为 Gap，等待 Porter-CS 根据 `#delta-2025-11-26-aa4-cl7-*` 投递修复后再恢复 Verified，并同步 rerun 命令与 MarkdownRenderer golden 输出。


## Coverage gaps snapshot (2025-11-27)

Matrix adjustments recorded in this pass: baseline counts stay at 585 and the WS5 shared harness suites now have explicit coverage + rerun anchors. Outstanding items remain:

- **TextModelIndentationTests.GuessIndentation_NotImplemented** — remains skipped under `#delta-2025-11-26-ws5-qa`; unblock once the GuessIndentation API is ported so WS5 indentation heuristics achieve full parity.
- **WordBoundaryTests** — still deferred to Batch #3 pending Intl.Segmenter + WordSeparator cache support; rerun command template is staged but no evidence yet (`#delta-2025-11-26-sprint04-r1-r11`).
- **MarkdownRendererTests (DocUI diff & future multi-cursor/snippet snapshots)** — only partial coverage is live because the canonical TS widget snapshot path is unknown; needs the AA4-CL8 changefeed to unblock `DocUI` overlay snapshots.
- **AA4-007 CL7 cursor/word/snippet suites** — all entries remain Gap per `#delta-2025-11-26-aa4-cl7-*`; SharedHarnessExampleTests keep the helper surface exercised, but the real cursor/column/snippet regressions still require implementation + reruns.

## Snippet Deterministic Tests (2025-12-02)

| Test Group | Scope | Focus | Owner | Status | Reference |
| --- | --- | --- | --- | --- | --- |
| SnippetControllerTests (Edge Cases) | Deterministic | Empty snippet, only $0, consecutive placeholders, nested placeholder syntax (skip), plain text only | QA-Automation | 7 pass, 3 skip | `SnippetControllerTests.cs` `#region Edge Cases` |
| SnippetControllerTests (adjustWhitespace) | Deterministic | Tab vs space indentation, mixed indent, empty lines, multiple newlines | QA-Automation | 6 pass | `SnippetControllerTests.cs` `#region adjustWhitespace Extended Tests` |
| SnippetControllerTests (Placeholder Grouping) | Deterministic | 3+ same index, multi-line spread, mixed indexes, empty placeholders grouped | QA-Automation | 6 pass | `SnippetControllerTests.cs` `#region Placeholder Grouping Extended Tests` |
| SnippetControllerTests (Complex Scenarios) | Deterministic | Realistic function, property with backing field, for loop | QA-Automation | 3 pass | `SnippetControllerTests.cs` `#region Complex Scenarios` |

### Targeted reruns (Snippet Deterministic Tests, 2025-12-02)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter SnippetControllerTests --nologo` | 52/52 green, 4 skipped (2.1s) | `#delta-2025-12-02-snippet-deterministic`: P0-P1.5 snippet parity validated; 4 tests skipped for P2 features (nested placeholders, escape handling, placeholder default inheritance). |

### Targeted reruns (MultiCursorSession Tests, 2025-12-05)

| Command | Result | Notes |
| --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter MultiCursorSessionTests --nologo` | 18/18 green (2.1s) | `#delta-2025-12-05-multicursor-session`: MultiCursorSession unit tests for Ctrl+D "Add Selection To Next Find Match" functionality. |

### MultiCursorSession Test Coverage

| Test Category | Count | TS Source Reference |
| --- | --- | --- |
| Basic Add/Move | 6 | `multicursor.test.ts` L78-200 |
| Edge Cases | 4 | `multicursor.test.ts` L113, L183-192 |
| Case Sensitivity | 2 | `multicursor.test.ts` L253 |
| Multi-line/CRLF | 2 | `multicursor.test.ts` L78, L155 |
| Whole Word | 1 | `multicursor.test.ts` L314 |
| Result Properties | 3 | `multicursor.test.ts` various |

**Notes**: Case sensitivity and whole word tests document expected TS parity behavior but may use flexible assertions pending full TextModelSearch matchCase/wholeWord support verification.

### Skipped Snippet Tests (P2 Features)

| Test | Reason | TS Reference |
| --- | --- | --- |
| `SnippetInsert_NestedPlaceholder_ParsesCorrectly` | Nested placeholder expansion requires P2 SnippetParser | `snippetParser.test.ts` |
| `SnippetInsert_NestedPlaceholders_ExpandCorrectly` | Nested placeholder expansion requires P2 SnippetParser | `snippetParser.test.ts` |
| `SnippetInsert_EscapedCharacters` | Escape handling requires P2 SnippetParser | `snippetParser.test.ts` |
| `SnippetInsert_PlaceholderInheritance` | Placeholder default inheritance requires P2 SnippetParser | Issue #31040 |

