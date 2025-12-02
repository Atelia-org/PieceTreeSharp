# AA4-FindModel-Review-PORT – Primary Selection Ordering Fix

## Scope
- **Investigator reference:** [`AA4-FindModel-Review-INV.md`](AA4-FindModel-Review-INV.md) flagged (1) incorrect primary-selection defaults (last entry instead of VS Code’s index 0) and (2) dead `_selectionCollection/_primarySelectionIndex` state.
- **Goal:** Match VS Code’s `TextEditor.selections` contract so `FindModel.SetSelections` and the DocUI harness treat the first selection as primary, prevent stale state, and prove `SelectAllMatches()` preserves the active cursor ordering.
- **Changefeed tag:** `#delta-2025-11-24-find-primary` (to be published under `agent-team/indexes/README.md`).

## Implementation Notes
1. **`src/TextBuffer/DocUI/FindModel.cs`**
   - Removed the unused `_selectionCollection` bookkeeping; `SetSelections` now clones the provided list, clamps `primaryIndex`, and always defaults to index `0` when callers omit the parameter.
   - Added a small `CloneRange` helper so external callers can’t mutate `_currentSelection`, and left `SelectAllMatches()` unchanged other than consuming the sanitized `_currentSelection`.
2. **`tests/TextBuffer.Tests/DocUI/TestEditorContext.cs`**
   - Updated `SetSelections` comment + behavior to mark the first entry as primary and forward it explicitly to the model; keeps the default `[ (1,1) ]` path for tests that do not pass any selections.
3. **`tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs`**
   - Added **Test49_SelectAllMatchesRespectsPrimarySelectionOrder** which seeds two selections (first one already matches `hello`) and asserts the returned selection array begins with the supplied primary range while the remaining matches stay sorted.
4. **Docs**
   - `tests/TextBuffer.Tests/TestMatrix.md` now cites the new regression, updates the DocUI row to “Tests44–49,” and records the targeted rerun tied to `#delta-2025-11-24-find-primary`.

## Tests
| Command | Result |
| --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~FindModelTests" --nologo` | 49/49 green (3.4s) |

## Follow-ups
- Info-Indexer: append `#delta-2025-11-24-find-primary` to the changefeed with the affected files/tests/doc touch points listed above, then link this Porter handoff and the migration-log row.
- QA-Automation: no new manual steps (the regression is automated), but please keep using the updated filter (`FullyQualifiedName~FindModelTests`) in rerun scripts.
- DocMaintainer: once the changefeed entry is live, cascade the delta into `AGENTS.md`, Sprint 03 log, and the Task Board as needed; this work closes the outstanding Investigator findings in `AA4-FindModel-Review-INV.md`.
