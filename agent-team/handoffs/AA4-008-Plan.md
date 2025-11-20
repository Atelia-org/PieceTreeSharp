# AA4-008 Plan – CL8 DocUI Find/Replace + Decorations parity

## Overview
This plan instructs Porter-CS to implement CL8: porting TS Find/Replace pipelines and owner-aware decoration overlay capabilities for the DocUI.

## Tasks
1) Review `agent-team/handoffs/AA4-004-Audit.md` and ensure understanding of TS `findController.ts`, `findModel.ts`, `findDecorations.ts`, and `replacePattern.ts` behaviors.
2) Implement a stateful FindModel & Controller in C#:
   - Add `FindModelBoundToTextModel` to track `startPosition`, `searchScope`, `currentMatchIndex`, `matchCounts`, and `seedFromSelections`.
   - Add `FindOptions` with `WholeWord`, `PreserveCase`, `Loop`, `FindInSelection` fields.
3) Port `FindDecorations` semantics to C#:
   - Create decoration options `SearchMatch`, `CurrentSearchMatch`, `RangeHighlight`, `FindScope`, `OverviewOnly` with owner tags & z-index similar to TS.
   - Add threshold-based fallback when matches exceed 1000 to degrade to overview-only decorations.
4) Port `ReplacePattern` & `ReplaceCommand` logic; rewire `TextModelSearch` to produce `captureMatches` arrays and pass them as decoration metadata.
5) Update `TextModel`/`MarkdownRenderer` rendering pipeline to consume owner-aware decoration data rather than re-running `FindMatches` per render (improve perf & enable capture preview rendering).
6) Add tests:
   - `TextModelSearchTests.SearchDecorationsIncludeMetadata` (overview/minimap/glyph metadata & 1000+ fallback)
   - `FindControllerTests.ScopeAndReplaceParity` (match indices & replace preview using capture groups)
   - `MarkdownRendererTests.SearchScopeAndReplacePreview` (DocUI renders capture/replace data & owner filter respects disabling overlays)
7) Update migration log, Task Board, & create `agent-team/handoffs/AA4-008-Result.md` with final statuses.

## Validation
- Full test suite must pass and new tests be included in `TestMatrix.md`.
- QA will verify `MarkdownRenderer` rendering (owner filter & replace previews) via snapshots.

## Memory & Reporting
- Porter to append work logs in `agent-team/members/porter-cs.md` and update `agent-team/handoffs/AA4-008-Result.md` on completion.


