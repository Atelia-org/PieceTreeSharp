# B3-FM-MultiSelection-PORT – Multi-Cursor Scope & Primary Cursor Parity

## Context
- **Sprint:** Sprint 03 – Batch #3 (DocUI FindModel track)
- **Delta Tag:** `#delta-2025-11-24-b3-fm-multisel`
- **Objective:** Close Investigator R20 audit items (F1–F3) by wiring true multi-selection inputs through `FindModel`, preserving the primary cursor when synthesizing selections, and porting the remaining TS `findModel.test.ts` multi-scope suites (Tests 07 & 08).

## Files & Implementation Notes
1. **Selection plumbing (`src/TextBuffer/DocUI/FindModel.cs`)**
   - Added `_selectionCollection`, `_primarySelectionIndex`, and `_currentSelection` tracking so `FindModel` can hydrate pending scopes from either the state or live decorations without dropping secondary cursors.
   - `SetSelections(IReadOnlyList<Range> selections, int? primaryIndex)` now clones every range, clamps the primary index, seeds `_decorations.SetStartPosition`, and resets match info so navigation picks up the correct anchor when the widget starts in multi-cursor mode.
   - `SelectAllMatches()` sorts all matches by range start, hoists the match that coincides with the primary cursor to index 0, and returns `Selection[]` that mirror TS multi-cursor ordering.
2. **Test harness upgrades (`tests/TextBuffer.Tests/DocUI/TestEditorContext.cs`)**
   - `SetSelections` accepts arbitrary ranges, treats the last provided range as the primary cursor (matching VS Code), and forwards the collection to `FindModel.SetSelections`.
   - Added helpers (`GetSelections`, `CloneRanges`) so tests can snapshot scopes before calling into state, which keeps the TS-style “use editor selections as find scope” parity intact.
3. **Regression coverage (`tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs`)**
   - Re-enabled and ported the TS multi-selection scope regressions:
     - **Test07_MultiSelectionFindModelNextStaysInScopeOverlap** – overlapping scopes retain wrap-around ordering.
     - **Test08_MultiSelectionFindModelNextStaysInScope** – disjoint scopes stay isolated even as we cycle through matches.
   - Ensured both tests drive the new harness by staging scopes via `ctx.SetSelections(...)`, cloning them into `searchScope`, and asserting navigation results.
4. **Docs & coordination assets**
   - Authored this porter handoff plus the Sprint/Migration log updates so QA/Info-Indexer/DocMaintainer have a single reference point when broadcasting `#delta-2025-11-24-b3-fm-multisel`.

## Validation Commands
| Command | Result |
| --- | --- |
| `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~DocUIFindModelTests.Test0(7|8)" --nologo` | 0/0 – VSTest warns because compiled type is `...DocUI.FindModelTests.*`; kept output per audit request. |
| `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~DocUIFindModelTests" --nologo` | 0/0 – legacy alias still unresolved; logged warning again for traceability. |
| `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | 242/242 ✔ (full TextBuffer suite, 2.9s). |

> **Note:** The first two commands remain mandatory per Batch #3 checklist even though the historic filter alias no longer matches `PieceTree.TextBuffer.Tests.DocUI.FindModelTests`. Both warnings are referenced in the migration log so Info-Indexer can decide when to retire the alias.

## TODOs / Follow-ups
- **QA-Automation**
  - Mirror these results inside `tests/TextBuffer.Tests/TestMatrix.md` once the alias cleanup lands, and confirm Test07/Test08 appear under the DocUI row when the filter string is updated to `FullyQualifiedName~FindModelTests`.
  - Evaluate whether we need explicit multi-selection fuzz cases before handing off to Investigator for word-boundary verification.
- **Info-Indexer**
  - Publish the `#delta-2025-11-24-b3-fm-multisel` entry in `agent-team/indexes/README.md` referencing this handoff, all three commands, and the migration-log row.
  - Flag the deprecated `DocUIFindModelTests` alias in the changefeed so future runbooks use the correct fully qualified name.
- **DocMaintainer**
  - Once the changefeed post is live, cascade the delta into `AGENTS.md`, `agent-team/task-board.md`, and `docs/plans/ts-test-alignment.md` so Batch #3 status reflects the completed multi-selection track.
  - Coordinate with QA before updating `tests/TextBuffer.Tests/TestMatrix.md` (per instructions, Porter work stops short of editing the matrix).
