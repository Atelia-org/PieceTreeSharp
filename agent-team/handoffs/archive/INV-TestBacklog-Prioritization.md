# INV-TestBacklog-Prioritization – 2025-11-26
Role: Investigator Evan Holt

## Scoring Method
- **Risk Score** (1–5) combines likelihood of silent regressions and blast radius if broken; 5 = immediate high-risk gap.
- **Impact** highlights user-facing fallout and why the suite matters now.
- References cite the latest audit sources: `docs/reports/alignment-audit/07-core-tests.md`, `docs/reports/alignment-audit/08-feature-tests.md`, and `tests/TextBuffer.Tests/TestMatrix.md`.
- CI targets assume `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` with focused filters so the suites can gate AA4/AA5 drops.

## Ranked Backlog

### 1. Cursor & Snippet Deterministic Suites
- **Risk Score:** 5 (High). Cursor/Snippet rows remain ❌ in `docs/reports/alignment-audit/07-core-tests.md` and `08-feature-tests.md`, leaving <10% coverage over atomic moves, multi-cursor undo, and snippet sessions. Any regression reproduces editor-stopping bugs (#27543, #58267) without alarms.
- **Impact:** Directly affects all multi-cursor typing, tabstops, and snippet completions; blockers for AA4-007 CL7 deliverables per `tests/TextBuffer.Tests/TestMatrix.md` (CL7 gaps table).
- **Harness / Fixture:** Port TS suites (`cursorAtomicMoveOperations.test.ts`, `multicursor.test.ts`, `wordOperations.test.ts`, `snippetController2.test.ts`, `snippetSession.test.ts`) into dedicated `[Theory]`-driven fixtures: `CursorAtomicMoveOperationsTests`, `MultiCursorCommandTests`, `WordOperationsParityTests`, `SnippetSessionParityTests`. Reuse `TestEditorHost` to script command replay, add deterministic snippet variable mocks, and share seeded command transcripts for regression triage.
- **CI & Coverage:** Target >85% statement coverage across `Cursor*` and `Snippet*` folders. Introduce CI lane `dotnet test ... --filter "FullyQualifiedName~Cursor|FullyQualifiedName~Snippet"` gated on AA4-007. Add nightly seeded fuzz (50 seeds) to `SnippetMultiCursorFuzzTests` with log retention when failures occur.
- **Dependencies:** Requires CL7 infra from AA4-007 (cursor word/snippet workstream) plus snippet variable stubs already planned in `agent-team/handoffs/AA4-007-Plan.md`.

### 2. PieceTree Buffer API Parity Suite
- **Risk Score:** 4. `PieceTreeModelTests` flagged ⚠️ in `docs/reports/alignment-audit/07-core-tests.md` for missing `equal`, `getLineCharCode` (#45735), and `getNearestChunk` coverage; buffer APIs feed search/diff features.
- **Impact:** Incorrect API parity corrupts buffer snapshots and downstream search/undo, undermining TS-oracle trust noted in `tests/TextBuffer.Tests/TestMatrix.md` (PT-005 gaps list).
- **Harness / Fixture:** Add `PieceTreeBufferApiTests` referencing `PieceTreeBufferAssertions`. Replay TS deterministic scripts with golden output from `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`. Provide shared helper to record tree structure when parity fails.
- **CI & Coverage:** Goal: 100% method coverage on buffer API surface (`PieceTreeBuffer.*`). Add filter `--filter PieceTreeBufferApiTests` to nightly deterministic lane and ensure env var `PIECETREE_DEBUG=0` is honored for perf parity.
- **Dependencies:** Needs the planned Porter exposure of `EnumeratePieces` (see TestMatrix PT-005.S8 note) plus hooking existing deterministic script harness.

### 3. TextModel GuessIndentation & Position Validation Matrix
- **Risk Score:** 4. `TextModelTests` lack `guessIndentation`, `TextModelData.fromString`, `getValueLengthInRange`, and `validatePosition` coverage per `docs/reports/alignment-audit/07-core-tests.md`.
- **Impact:** Indentation heuristics drive auto-detect in editors; regressions hit every language. Position validation gaps previously manifested as crashers (#44991/#84217) and remain unguarded.
- **Harness / Fixture:** Introduce `TextModelIndentationMatrixTests` with `[Theory]` data mirroring TS matrix (~30 samples). Use seeded fixtures stored under `tests/TextBuffer.Tests/Data/IndentationMatrices.json` for clarity. Extend `TextModelValidationTests` to assert `validatePosition` and `getValueLengthInRange` against TS expectation tables, using CR/LF mixes and surrogate pairs.
- **CI & Coverage:** Requirement: cover 95% of `IndentationGuesser` branches and add `dotnet test ... --filter FullyQualifiedName~TextModelIndentationMatrixTests` to AA4 nightly to keep heuristics deterministic.
- **Dependencies:** Leverage AA4-006 TextModel snapshot harness for bulk text seeding; coordinate with language configuration workstream (AA3-001 follow-up) for Intl segmentation inputs later.

### 4. DocUI Find Context Keys & Focus Flow
- **Risk Score:** 3. `docs/reports/alignment-audit/08-feature-tests.md` notes missing coverage for `FindStartFocusAction`, `CONTEXT_FIND_INPUT_FOCUSED`, Mac clipboard focus, and animation switches.
- **Impact:** Context-key drift silently breaks keyboard shortcuts and accessibility focus across DocUI; regression risk grows with recent clipboard/storage refactors.
- **Harness / Fixture:** Extend `DocUIFindControllerTests` with a UI state harness that simulates focus transitions via `TestEditorHost` plus fake `IContextKeyService`. Mirror TS tests (`findController.test.ts`) for `shouldAnimate` and platform-specific clipboard writes by toggling injected platform trait.
- **CI & Coverage:** Aim for 100% branch coverage on `FindController` focus/context-key methods. Run targeted lane `--filter DocUIFindControllerTests.FindStart*|DocUIFindControllerTests.Focus*` on macOS agent emulator (or Linux with platform flag) to prove determinism.
- **Dependencies:** Requires `B3-FC` workstream stubs already used for clipboard/storage; coordinate with DocUI platform shim owners documented in `agent-team/handoffs/B3-FC-Review.md`.

### 5. DocUI Scope Persistence & Highlight Harness
- **Risk Score:** 3. `docs/reports/alignment-audit/08-feature-tests.md` still lists missing `highlightFindScope`, `FindDecorations.MatchBeforePosition`, and scope throttling scenarios even after Batch B3.
- **Impact:** Scope regressions cause replace-in-selection corruption and inconsistent highlight counts; QA currently lacks deterministic asserts for decoration flush/edit flows.
- **Harness / Fixture:** Build `DocUIFindScopeParityTests` that combine `DocUIFindModelTests` with `DocUIFindDecorationsTests`. Seed scripted edits matching TS `find.test.ts` highlight cases, including delayed updates via a fake `Delayer`. Capture decoration snapshots to JSON for debugging.
- **CI & Coverage:** Target >90% coverage on `FindDecorations` scope code paths and ensure new tests run under `--filter DocUIFindScopeParityTests`. Add requirement that every DocUI scope fix ships with updated JSON snapshots stored under `tests/TextBuffer.Tests/Baselines/DocUIScope`.
- **Dependencies:** Reuses B3 scope harness (`DocUI/TestEditorContext.cs` multi-selection plumbing) and depends on same context-key fixtures from item 4.

### 6. Diff Renderer Parameter & Performance Matrix
- **Risk Score:** 3. `DiffTests` remain ⚠️ per `docs/reports/alignment-audit/07-core-tests.md`/`08-feature-tests.md`, lacking the TS `defaultLinesDiffComputer.test.ts` algorithm/parameter matrix (strategy, unchangedRegions, computeMoves, perf short-circuit).
- **Impact:** Without these tests we cannot verify diff output stability for large files or new heuristics, yet AA4 introduces ongoing diff tuning.
- **Harness / Fixture:** Create `LinesDiffComputerParityTests` that import TS fixture data (JSON) for algorithm permutations. Add large-doc deterministic benchmarks using `TS oracle → serialized expected char changes`. For perf safeguards, add seeded "budget" tests that assert `maxComputationTimeMs` behavior using fake timers.
- **CI & Coverage:** Ensure >80% path coverage through `DefaultLinesDiffComputer`. Run filter `--filter LinesDiffComputerParityTests` in CI and record top-line perf (target <5s on 1MB fixture). Integrate with CodeQL gating before diff workstreams merge.
- **Dependencies:** Align with AA4 diff renderer work; needs fixture export pipeline from `ts/src/vs/editor/test/node/diffing/defaultLinesDiffComputer.test.ts` (tracked in `agent-team/indexes/diff-parity.md`).
