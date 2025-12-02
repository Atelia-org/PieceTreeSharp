# AA4-007 Plan – CL7 Cursor word/snippet/multi-select parity

## Overview
This plan directs Porter-CS to implement CL7: Cursor word navigation, multi-selection controller, column selection & snippet session parity. The goal is to port the TS cursor controller, word operations, column selection helpers, and snippet session infrastructure, and to add tests that cover key user flows.

## Tasks
1) Review `agent-team/members/porter-cs.md` and `agent-team/handoffs/AA4-003-Audit.md` (examined by Investigator-TS) to align on the gap list and fix sequence.
2) Implement foundational cursor infra:
   - Add `CursorCollection`, `CursorState`, `CursorContext` (port from TS controller), and integrate multi-cursor handling in `TextModel` and `MarkdownRenderer` eventing/decoration hooks.
   - Expose per-cursor `Selection` objects to enable rendering multiple cursors and selection markers in the DocUI.
3) Port word navigation & delete/selection operations:
   - Implement `WordCharacterClassifier` with `WordNavigationType` and support for `wordPart`/`word` movement and delete/selection variations.
   - Add support for locale-sensitive classifiers or use a robust fallback; expose config via `TextModelOptions`.
4) Add column selection & visible column helpers:
   - Add `CursorConfiguration` / `CursorColumns` helpers in `TextModel`, and methods to convert visible columns to buffer positions.
   - Implement Alt+drag block selection and vertical movement logic with tests.
5) Snippet session/controller:
   - Port `SnippetSession` & `SnippetController` enough to support placeholder insertion, tabstop navigation, and placeholder decorations. Integrate snippet session into the cursor controller and the edit-undo stack.
6) Add tests (xUnit):
   - `CursorMultiSelectionTests`: create multiple cursors, insert text in multiple selection spots, check model invariants & markdown rendering.
   - `CursorWordOperationsTests`: verify `Ctrl+Arrow` move + `Delete word` logic and locale scenarios.
   - `ColumnSelectionTests`: verify vertical selections & visible column conversion on tabs/injected text.
   - `SnippetControllerTests`: verify insertion & tabstop navigation with placeholder decorations, and integration with multi-cursor state.
7) Update `docs/reports/migration-log.md` and `agent-team/task-board.md` as the work completes; create `agent-team/handoffs/AA4-007-Result.md` summarizing results & test baseline.

## Validation & QA
- `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` must pass, include new `Cursor*` & `Snippet*` tests.
- QA to add `MarkdownRendererTests.MultiCursorAndSnippet` to validate DocUI rendering with multiple cursors and snippet placeholders.

## Memory & Reporting
- Porter to append work logs to `agent-team/members/porter-cs.md` and update `agent-team/handoffs/AA4-007-Result.md` on completion.


