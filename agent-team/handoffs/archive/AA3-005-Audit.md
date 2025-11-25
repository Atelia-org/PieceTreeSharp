# AA3-005 Audit – CL3 Diff Prettify & Move Metadata

**Date:** 2025-11-20  
**Investigator:** GitHub Copilot  
**Scope:** TS `ts/src/vs/editor/common/diff/defaultLinesDiffComputer/*.ts`, `ts/src/vs/editor/common/diff/rangeMapping.ts`, `ts/src/vs/editor/common/diff/documentDiffProvider.ts`; TextModel decoration plumbing in `ts/src/vs/editor/common/model/textModel.ts`. Compared against C# `src/PieceTree.TextBuffer/Diff/*`, `src/PieceTree.TextBuffer/TextModel.cs`, `src/PieceTree.TextBuffer/Decorations/*`, and `src/PieceTree.TextBuffer/Rendering/MarkdownRenderer.cs`.

**Baseline Dependencies:** Relies on CL1 TextModel option parity (events/decorations) and CL2 search metadata so diff-produced decorations can flow through the same event system. AA3-006 (Porter) and AA3-009 (QA) block on these audit findings.

## Overview
The TypeScript implementation produces structured line/word diff metadata (`LinesDiff`, `DetailedLineRangeMapping`, move summaries) and exposes them via document diff providers so decorations, DocUI, and Markdown renderers can visualize moves and inline edits. The C# port currently emits only raw `DiffChange` spans over a single buffer, with simplistic prettify logic, brittle move detection, no timeout/whitespace options, and no consumers capable of rendering the richer metadata. Without alignment, Porter cannot meet CL3 parity and QA lacks hooks to verify diff correctness.

## Findings

### F1 – Diff results lack `DetailedLineRangeMapping` / word-diff metadata (High)
- **TS Reference:** `defaultLinesDiffComputer.ts#L27-L212` builds a `LinesDiff` whose `.changes` are `DetailedLineRangeMapping`s, guaranteeing `innerChanges` are populated and valid (`rangeMapping.ts#L196-L238`).
- **C# Reference:** `DiffComputer.cs#L9-L41` returns `DiffResult` with `List<DiffChange>` (`DiffChange.cs#L3-L20`); `DiffResult.cs#L5-L24` contains no per-line grouping, inner character ranges, or hit-timeout flag.
- **Impact:** Consumers (diff editor, DocUI, Markdown renderer) cannot highlight edits within a line, emit word-level decorations, or derive edits for inline previews. Any attempt to prettify results in Porter-AA3-006 will need to re-run diffing from scratch, and move metadata cannot be tied to specific ranges.
- **Suggested Fix:** Port the TS `LinesDiff` data model: introduce `LineRange`, `RangeMapping`, and `DetailedLineRangeMapping` equivalents, ensure `lineRangeMappingFromRangeMappings` joins character-level spans, and surface `hitTimeout`. Update `DiffResult` to expose the same shape so downstream callers can reason over both line- and character-level spans.
- **Test Hooks:** Extend `PieceTree.TextBuffer.Tests/DiffTests.cs` with `WordDiffProducesInnerChanges` to assert `innerChanges` equals the TS baseline for snippets (e.g., import list example) and `DiffTimeoutSurface` to verify `hitTimeout` toggles when a mocked timeout triggers.

### F2 – Move detection reduces to trimmed string equality (High)
- **TS Reference:** `computeMovedLines.ts#L17-L255` hashes sliding 3-line windows, filters candidates with similarity scoring, extends moves to cover adjacent unchanged lines, and re-diffs each move so `MovedText` includes `DetailedLineRangeMapping`s.
- **C# Reference:** `DiffComputer.cs#L137-L207` only compares trimmed substrings from `DiffChange`s, limits evaluation by a simple candidate counter, and `DiffMove` (`DiffMove.cs#L1-L22`) stores only raw offsets/text.
- **Impact:** Multi-line moves that contain whitespace-only changes or partially overlapping edits are never detected; false positives occur whenever different blocks share identical trimmed text; UI cannot render per-move inline diffs because no `innerChanges` exist per move. Porter-CS cannot satisfy AA3-006 requirements for move glyphs or metadata export.
- **Suggested Fix:** Port `computeMovedLines` end-to-end (hash maps, `LineRangeFragment`, `LineRangeSet` subtraction, similarity scoring, extension heuristics) and extend `DiffMove` to carry the nested `DetailedLineRangeMapping[]` returned by TS (`DefaultLinesDiffComputer.computeMoves` `#L188-L215`). Respect `ILinesDiffComputerOptions.computeMoves` semantics and ensure move candidates exclude overlapping diffs.
- **Test Hooks:** Add `DiffTests.MoveDetectionMatchesVSCode` (reusing TS fixtures for block moves with whitespace) and `DiffTests.MoveCarriesInnerChanges` to ensure `DiffMove` exposes nested mappings used by DocUI.

### F3 – Prettify heuristics, whitespace controls, and timeouts missing (High)
- **TS Reference:** `linesDiffComputer.ts#L11-L38` exposes `ignoreTrimWhitespace`, `maxComputationTimeMs`, `computeMoves`, `extendToSubwords`. `defaultLinesDiffComputer.ts#L47-L245` selects dynamic-programming vs Myers, scans equal lines for whitespace-only edits, runs `optimizeSequenceDiffs`, `extendDiffsToEntireWordIfAppropriate`, optional subword extension, `removeShortMatches`, and `removeVeryShortMatchingTextBetweenLongDiffs`. Word boundary logic delegates to `LinesSliceCharSequence` (`linesSliceCharSequence.ts#L20-L170`) for camelCase/subwords.
- **C# Reference:** `DiffComputerOptions.cs#L3-L10` only exposes `EnablePrettify`, `ExtendToWordBoundaries`, and numeric thresholds. Implementation (`DiffComputer.cs#L60-L135`) merges adjacent gaps and extends ASCII word characters without whitespace toggles, subword awareness, or timeout signaling.
- **Impact:** Diff output frequently splits tokens (`fooBar` vs `fooBarBaz`), cannot honor VS Code's "ignore trim whitespace" toggle, and may hang or return incomplete results for large files because there is no timeout or dynamic-programming cutoff. Users see noisy diffs, Porter cannot wire option switches, and QA lacks parity guarantees.
- **Suggested Fix:** Align `DiffComputerOptions` with `ILinesDiffComputerOptions` (include whitespace flag, timeout budget, `extendToSubwords`). Port the heuristics from TS (sequence optimization, word/subword expansion, short-match suppression). Introduce a timeout abstraction so long-running diffs fail fast and surface `hitTimeout`.
- **Test Hooks:** Add `DiffTests.IgnoreTrimWhitespaceMatchesTS` (document containing indentation-only edits), `DiffTests.SubwordExtension`, and `DiffTests.TimeoutFlag` (using artificially small `maxComputationTimeMs`) mirroring VS Code fixtures.

### F4 – Decorations/renderer cannot represent diff metadata (Medium)
- **TS Reference:** `rangeMapping.ts#L196-L238` stores both original and modified line ranges plus `innerChanges`, enabling diff consumers to convert mappings into decorations or text edits. `defaultLinesDiffComputer.ts#L169-L185` asserts every change has inner data before returning.
- **C# Reference:** Decorations are single-buffer ranges (`ModelDecoration.cs#L32-L69` with `TextRange` only) and `MarkdownRenderer` only renders cursors/selections/search (`MarkdownRenderer.cs#L14-L136`). No structure exists to pair original vs. modified ranges or to associate move metadata with decorations/events emitted from `TextModel`.
- **Impact:** Even after fixing the diff engine, there is no storage or rendering path for diff/move metadata. Porter-AA3-006 would have to invent a parallel plumbing layer, while QA-AA3-009 cannot validate DocUI diff output.
- **Suggested Fix:** Introduce diff-specific decoration/annotation types that carry both original & modified spans (e.g., `DiffDecoration { Range original, Range modified, RangeMapping[] innerChanges, MoveId }`) and raise them through `TextModelDecorationsChangedEventArgs`. Extend `MarkdownRenderer` to honor new render kinds for additions/deletions/moves so DocUI snapshots reflect diff metadata.
- **Test Hooks:** Add `MarkdownRendererTests.RenderDiffBlocks` and `DecorationTests.DiffMetadataRoundTrip` to confirm diff decorations survive edits and render both ranges.

## Changefeed Impact
- `src/PieceTree.TextBuffer/Diff/*` – replace the current char-only `DiffComputer` with the TS-aligned `LinesDiff` engine, add options/timeouts, move metadata, and expanded result types.
- `src/PieceTree.TextBuffer/TextModel.cs` & `src/PieceTree.TextBuffer/Decorations/*` – extend decoration storage/events to carry diff/move metadata and dual-buffer ranges.
- `src/PieceTree.TextBuffer/Rendering/MarkdownRenderer.cs` – add renderers for diff results (inline additions/deletions, move callouts) consuming the new metadata.
- `src/PieceTree.TextBuffer.Tests/*` – grow `DiffTests`, `DecorationTests`, `MarkdownRendererTests` with TS parity fixtures (word diff, whitespace ignore, moves, DocUI snapshots).

## References
- `ts/src/vs/editor/common/diff/defaultLinesDiffComputer/defaultLinesDiffComputer.ts`
- `ts/src/vs/editor/common/diff/defaultLinesDiffComputer/computeMovedLines.ts`
- `ts/src/vs/editor/common/diff/defaultLinesDiffComputer/linesSliceCharSequence.ts`
- `ts/src/vs/editor/common/diff/rangeMapping.ts`
- `src/PieceTree.TextBuffer/Diff/DiffComputer.cs`
- `src/PieceTree.TextBuffer/Diff/DiffComputerOptions.cs`
- `src/PieceTree.TextBuffer/Diff/DiffChange.cs`
- `src/PieceTree.TextBuffer/Diff/DiffResult.cs`
- `src/PieceTree.TextBuffer/Decorations/ModelDecoration.cs`
- `src/PieceTree.TextBuffer/Rendering/MarkdownRenderer.cs`

## Next Steps for Porter-CS (AA3-006)
1. **Diff Engine Rewrite:** Implement `LinesDiff`/`DetailedLineRangeMapping` plus timeout-aware heuristics and expose TS-identical options so callers can request whitespace-ignore, subword extension, and move detection.
2. **Move Metadata Plumbing:** Port `computeMovedLines`, extend `DiffMove` with nested mappings, and thread move summaries through `TextModel` events/decorations so DocUI can visualize them.
3. **DocUI Integration:** Design diff-specific decoration/render kinds and teach `MarkdownRenderer` (and future viewers) to consume them, ensuring the metadata matches TS semantics for additions/deletions/moves.
4. **Testing & QA Contracts:** Add the parity tests outlined above and document required fixtures so QA-AA3-009 can automate regression coverage once the fixes land.
