# ALIGN 2025-11-26 Remediation Plan

_Source docs: `docs/reports/alignment-audit/00-summary.md`, `01-core-fundamentals.md`, `02-core-support.md`, `03-cursor.md`, `04-decorations.md`, `07-core-tests.md`, `08-feature-tests.md`._

## Workstream 1 – PieceTreeModel.Search parity (GetAccumulatedValue, offset cache, CRLF)
**Audit references:** 00-summary §P0, 01-core-fundamentals §2, 07-core-tests §PieceTree.

**Scope**
- Restore TS-complete `PieceTreeModel.Search` + change-buffer append parity, including `_buffers[0]` placeholder for CRLF cross-boundaries and `hitCRLF` bookkeeping.
- Reimplement `GetAccumulatedValue` + `NodeAt2` cache so offset lookups reuse subtree metadata instead of O(n) scans; align with TS `pieceTreeTextBuffer.ts` search helpers.
- Harden offset cache invalidation in `PieceTreeModel.Edit` and `PieceTreeSearchCache`; extend deterministic + fuzz harnesses to assert CRLF and reuse cases flagged in 07-core-tests.

**Owners**
| Role | Owner |
| --- | --- |
| Investigator | Mira Chen (Core Fundamentals) |
| Porter | Leo Park (PieceTree) |
| QA | Sasha Patel (Core Tests) |

**Milestones**
1. **M0 – Delta capture (Nov 29)**: Investigator records TS↔C# diff for `GetAccumulatedValue`, `_lastChangeBufferPos`, `hitCRLF` logic.
2. **M1 – Implementation & instrumentation (Dec 6)**: Porter lands core parity patch plus instrumentation hooks (`PieceTreeSearchOffsetCacheTests`).
3. **M2 – Regression harness (Dec 11)**: QA expands deterministic/fuzz suites; add cross-append CRLF + node cache telemetry; gate on CI green.

**Required source/test files**
- C#: `src/TextBuffer/Core/PieceTreeModel.Search.cs`, `src/TextBuffer/Core/PieceTreeModel.Edit.cs`, `src/TextBuffer/Core/PieceTreeSearchCache.cs`, `src/TextBuffer/Core/PieceTreeModel.cs`. 
- TS reference: `ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts`, `pieceTreeBase.ts`.
- Tests: `tests/TextBuffer.Tests/PieceTreeDeterministicTests.cs`, `PieceTreeFuzzHarnessTests.cs`, `PieceTreeSearchOffsetCacheTests.cs`, `PieceTreeSearchTests.cs`.

**Acceptance criteria**
1. CRLF append-within-change-buffer cases match TS cursor offsets (validated via deterministic fixtures + new regression doc).
2. `PieceTreeSearchOffsetCacheTests` exposes cached path reuse counters and achieves parity metrics with TS baseline log.
3. New fuzz runs show no divergence for 10M operations shared seed; `PieceTreeFuzzHarness` updated golden snapshot.

---

## Workstream 2 – Range/Selection helper completeness
**Audit references:** 00-summary §P0, 02-core-support §Range, 03-cursor §Selection, 07-core-tests (helper coverage).

**Scope**
- Port TS APIs: `Range.containsPosition/range`, `intersectRanges`, `plusRange`, `Selection.fromRange/with/delta`, and shared comparison helpers.
- Unify `TextPosition` ordering + delta semantics so Selection/Decoration/Find layers stop duplicating logic.
- Backfill helper-focused deterministic tests and update cursor/docUI callers to use canonical helpers.

**Owners**
| Role | Owner |
| --- | --- |
| Investigator | Harper Lin (Core Support) |
| Porter | Diego Torres (Range/Selection) |
| QA | Erin Blake (DocUI QA) |

**Milestones**
1. **M0 – API gap inventory (Nov 28)**: Investigator maps C# vs TS signatures + behavior docs.
2. **M1 – Helper implementation (Dec 4)**: Porter ports logic + documentation; ensures `TextPosition` struct exposes parity APIs.
3. **M2 – Consumer migration & tests (Dec 9)**: QA + Porter replace bespoke logic in Cursor/DocUI, land new helper test matrix.

**Required source/test files**
- C#: `src/TextBuffer/Core/Range.Extensions.cs`, `src/TextBuffer/Core/Selection.cs`, `src/TextBuffer/TextPosition.cs`, `src/TextBuffer/Cursor/Cursor.cs`, `src/TextBuffer/DocUI/DocUIFindController.cs` (callers).
- TS reference: `ts/src/vs/editor/common/core/range.ts`, `selection.ts`, `position.ts`.
- Tests: `tests/TextBuffer.Tests/CursorTests.cs`, `CursorWordOperationsTests.cs`, `ColumnSelectionTests.cs`, new `RangeExtensionsTests.cs`, `DocUI/DocUIFindControllerTests.cs` cases.

**Acceptance criteria**
1. Public Range/Selection API surface matches TS signatures & semantics (documented diff table, 0 unresolved TODOs in helper files).
2. Cursor + DocUI helpers consume shared APIs with no duplicate comparison logic (spot-checked via static analysis rule or code search).
3. Added deterministic tests cover boundary inclusion/exclusion, zero-length ranges, and Selection delta semantics; failing before fix, passing after.

---

## Workstream 3 – IntervalTree deferred `requestNormalize`
**Audit references:** 00-summary §P0, 04-decorations §IntervalTree, 06-services (TextModel adjust flow).

**Scope**
- Reintroduce TS `requestNormalize`, `_delta`, `_normalizeDeltaIfNecessary`, and metadata bitmask semantics so decoration updates remain amortized.
- Wire `TextModel.AdjustDecorationsForEdit` to deferred normalize + `acceptReplace`, ensuring large docs avoid O(n) rewrites.
- Surface metadata toggles for `filterOutValidation`, injected text, minimap lanes; align owner semantics with `DecorationOwnerIds` decisions from audit.

**Owners**
| Role | Owner |
| --- | --- |
| Investigator | Noor Rahman (Decorations) |
| Porter | Felix Novak (IntervalTree) |
| QA | Priya Nair (Rendering QA) |

**Milestones**
1. **M0 – Design sync (Nov 30)**: Investigator decomposes TS intervalTree delta contract + identifies required C# struct changes.
2. **M1 – Core tree rewrite (Dec 7)**: Porter lands `_delta` storage + deferred normalize path with benchmarks.
3. **M2 – Metadata + perf validation (Dec 12)**: QA runs DocUI/Decoration perf harness, ensures normalization triggers match TS traces.

**Required source/test files**
- C#: `src/TextBuffer/Decorations/IntervalTree.cs`, `DecorationsTrees.cs`, `TextModel.cs`, `TextModelDecorationsChangedEventArgs.cs`.
- TS reference: `ts/src/vs/editor/common/model/intervalTree.ts`, `textModel.ts` (decorations section).
- Tests: `tests/TextBuffer.Tests/DecorationTests.cs`, `DecorationStickinessTests.cs`, `DocUI/DocUIFindDecorationsTests.cs`, new perf-smoke harness under `tests/TextBuffer.Tests/DocUI`.

**Acceptance criteria**
1. IntervalTree supports deferred normalization with parity metrics (tree depth, op counts) logged vs TS sample; instrumentation doc stored in `docs/reports/alignment-audit/04-decorations.md` addendum.
2. Decorations updates remain O(log n) per edit for 50k ranges scenario (validated via perf test), with `filterOutValidation` + minimap toggles honored.
3. Regression suite passes existing decoration tests plus new perf guard; no GC spikes/regressions flagged by telemetry harness.

---

## Workstream 4 – Cursor/Snippet architecture parity + deterministic tests
**Audit references:** 00-summary §P0, 03-cursor, 08-feature-tests §Cursor/Snippet.

**Scope**
- Rebuild Cursor architecture per TS (`CursorConfiguration`, `SingleCursorState`, tracked ranges, view/model separation) and expose hooks for `CursorCollection` normalization.
- Port Snippet session lifecycle (choice, variables, transforms) and word/column utilities (`CursorColumns`, `WordOperations`), matching TS controllers.
- Expand deterministic coverage beyond fuzz: multi-selection merging, column select, snippet undo/redo, wordPart navigation, snippet placeholder deterministic suites highlighted in audit.

**Owners**
| Role | Owner |
| --- | --- |
| Investigator | Callie Stone (Cursor Architecture) |
| Porter | Viktor Zoric (Cursor/Snippet) |
| QA | Lena Brooks (Feature QA) |

**Milestones**
1. **M0 – Architecture blueprint (Dec 2)**: Investigator documents TS→C# mapping for cursor config/state, snippet session interplay, tracked ranges.
2. **M1 – Core implementation (Dec 13)**: Porter lands Cursor + Snippet parity structures, gating behind feature flag for staged rollout.
3. **M2 – Deterministic test suite (Dec 20)**: QA adds new deterministic suites (column select, snippet transforms, wordPart) + ensures fuzz harness still green.

**Required source/test files**
- C#: `src/TextBuffer/Cursor/Cursor.cs`, `src/TextBuffer/Cursor/CursorCollection.cs`, `src/TextBuffer/Cursor/CursorColumns.cs`, `src/TextBuffer/Cursor/CursorState.cs`, `src/TextBuffer/Cursor/WordOperations.cs`, `src/TextBuffer/Cursor/SnippetController.cs`, `src/TextBuffer/Cursor/SnippetSession.cs`.
- TS reference: `ts/src/vs/editor/common/cursorCommon.ts`, `ts/src/vs/editor/common/cursor/cursor.ts`, `ts/src/vs/editor/common/cursor/cursorCollection.ts`, `ts/src/vs/editor/common/cursor/cursorColumns.ts`, `ts/src/vs/editor/contrib/snippet/browser/snippetController2.ts`, `ts/src/vs/editor/contrib/snippet/browser/snippetSession.ts`.
- Tests: `tests/TextBuffer.Tests/CursorTests.cs`, `CursorMultiSelectionTests.cs`, `CursorWordOperationsTests.cs`, `ColumnSelectionTests.cs`, `SnippetControllerTests.cs`, `SnippetMultiCursorFuzzTests.cs` (plus new deterministic suites under same folder).

**Acceptance criteria**
1. Feature parity checklist derived from `03-cursor.md` marked complete (multi-cursor stack, tracked ranges, snippet variable resolution, wordPart nav) with demo recording.
2. Deterministic tests cover at least 80% of TS scenarios enumerated in audit (§03, §08), and run in <2 min locally.
3. All cursor/snippet fuzz + deterministic suites pass on CI twice consecutively before feature flag removal.

---

## Workstream 5 – High-risk deterministic & feature test backlog (per 07/08)
**Audit references:** 07-core-tests, 08-feature-tests, 00-summary §Testing risks.

**Scope**
- Build backlog of deterministic + feature tests for modules called out as high-risk: PieceTree buffer APIs (`equal`, `getLineCharCode`, `getNearestChunk`), TextModel `guessIndentation`, Find context keys, Diff renderer (moves/unchanged regions), DocUI scope.
- Derive TS test fixtures + expected outputs; create parity harness to compare C# vs TS logs (where feasible) for reproducible seeds.
- Integrate coverage reporting (per-test tags) into `tests/TextBuffer.Tests/TestMatrix.md` and CI dashboards.

**Owners**
| Role | Owner |
| --- | --- |
| Investigator | Evan Holt (Test Strategy) |
| Porter | Morgan Lee (Test Harness) |
| QA | Priya Nair (Feature QA) |

**Milestones**
1. **M0 – Test inventory & prioritization (Nov 30)**: Investigator maps missing suites from 07/08 reports into backlog with risk score.
2. **M1 – Harness extensions (Dec 8)**: Porter adds deterministic harness utilities (shared fixture loader, TS oracle ingestion) and wires into CI.
3. **M2 – Test implementation (Dec 18)**: QA + Porter land top-10 high-risk tests, update TestMatrix + coverage gates.

**Required source/test files**
- C#: `tests/TextBuffer.Tests/PieceTreeBaseTests.cs`, `PieceTreeDeterministicTests.cs`, `PieceTreeSearchTests.cs`, `TextModelTests.cs`, `TextModelSearchTests.cs`, `DocUI/DocUIFindControllerTests.cs`, `DiffTests.cs`, `MarkdownRendererTests.cs`.
- TS reference: `ts/src/vs/editor/common/model/pieceTreeTextBuffer/*.ts`, `ts/src/vs/editor/common/model/textModel.ts`, `ts/src/vs/editor/contrib/find/browser/findController.ts`, `ts/src/vs/editor/common/diff/defaultLinesDiffComputer/lineSequence.ts`, `ts/src/vs/editor/common/diff/rangeMapping.ts`.
- Supporting docs: `tests/TextBuffer.Tests/TestMatrix.md` (update), `docs/reports/alignment-audit/07-core-tests.md`, `08-feature-tests.md` for parity checklist.

**Acceptance criteria**
1. New deterministic suites cover every P0/P1 gap flagged in 07/08 with explicit traceable test IDs (logged in `TestMatrix.md`).
2. CI publishes coverage delta demonstrating ≥20% increase for high-risk areas (PieceTree, Cursor, Diff, Snippet) relative to current baseline.
3. Each new suite documents TS oracle or reasoning inside test file header, ensuring future audits can revalidate parity quickly.

---

## Next Actions (trigger via runSubAgent once approved)
- [ ] Investigator Mira Chen – `runSubAgent("INV-PT-Search-DeepDive")` to capture PieceTree diff + instrumentation notes by Nov 29.
- [ ] Porter Leo Park – `runSubAgent("PORT-PT-Search-Impl")` to draft CRLF/offset cache patch plan post-investigation.
- [ ] Investigator Harper Lin – `runSubAgent("INV-RangeSelection-GapReport")` to finalize helper comparison table.
- [ ] Porter Felix Novak – `runSubAgent("PORT-IntervalTree-Normalize")` to spike deferred normalize prototype.
- [ ] Investigator Callie Stone – `runSubAgent("INV-CursorSnippet-Blueprint")` outlining cursor/snippet architecture mapping.
- [ ] Investigator Evan Holt – `runSubAgent("INV-TestBacklog-Prioritization")` to deliver risk-ranked deterministic test list and CI coverage goals.
