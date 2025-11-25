# AA4-FindModel-Review-INV

## Scope & References
- **C# files inspected**: `src/TextBuffer/DocUI/FindModel.cs`, `tests/TextBuffer.Tests/DocUI/TestEditorContext.cs`, `tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs`.
- **TS references**: `ts/src/vs/editor/contrib/find/browser/findModel.ts` (FindModelBoundToEditorModel), `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts` (multi-selection tests around lines 505-620), and API contract `ts/src/vscode-dts/vscode.d.ts` (primary selection defined at lines 1260-1270).

## Findings
1. **Medium – Primary selection ordering diverges from VS Code contract.**
   - Files: `tests/TextBuffer.Tests/DocUI/TestEditorContext.cs` (lines 90-101) and `src/TextBuffer/DocUI/FindModel.cs` (lines 88-112).
   - Details: Both the harness and `FindModel.SetSelections` assume the *last* entry in the supplied selection list is the primary selection (`_primarySelectionIndex = _selections.Length - 1` and `primaryIndex ?? (selections.Count - 1)`). In VS Code the primary selection is always `selections[0]` (see `TextEditor.selection` doc in `ts/src/vscode-dts/vscode.d.ts`). The new multi-selection tests currently call `SetPosition` right after `SetSelections`, so the mismatch is masked, but any future tests (or runtime wiring of `SetSelections`) will diverge from TS behaviour and can hide bugs in select-all-matches ordering or seeded-search-string logic.
   - Recommendation: Align with TS by treating index `0` as the default primary selection, and expose an explicit `primaryIndex` parameter on the harness for the rare cases that need to override it. Add an assertion-based test that fails if `SetSelections` reorders the primary cursor relative to TypeScript expectations.

2. **Low – `_selectionCollection` is write-only state right now.**
   - File: `src/TextBuffer/DocUI/FindModel.cs` (lines 34-110).
   - Details: The new `_selectionCollection` and `_primarySelectionIndex` fields are populated but never read anywhere in the DocUI stack; controller code still calls `SetSelection` and the rest of `FindModel` only looks at `_currentSelection`. Keeping dead state risks the collection going stale (e.g., when edits change selection count) and makes it harder to audit parity because the TS reference (`findModel.ts`) does not keep such state.
   - Recommendation: Either remove the extra storage until we have a real consumer (e.g., upcoming multi-selection select-all-matches work) or plumb `SetSelections` through `DocUIFindController`/`TestEditorHost` immediately so that `_selectionCollection` mirrors the editor’s state.

## Suggested Follow-up / Tests
- After fixing the primary-selection ordering, add a regression in `DocUIFindModelTests` that invokes `FindModel.SelectAllMatches` after calling `SetSelections` to prove the first selection remains primary, mirroring `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts` select-all cases.
- Re-run `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~FindModelTests" --nologo` to ensure the 48-test suite stays green.

## Resolution Update – 2025-11-24

- **Finding 1 (Primary selection ordering)** – ✅ **Resolved via** `#delta-2025-11-24-find-primary` + `#delta-2025-11-24-b3-fm-multisel`. `FindModel.SetSelections` now defaults the primary index to `0`, clamps explicit overrides, and only tracks `_currentSelection`. Harness changes in `tests/TextBuffer.Tests/DocUI/TestEditorContext.cs` mirror VS Code (`TextEditor.selection`) semantics so the first entry is always primary. `DocUIFindModelTests.Test49_SelectAllMatchesRespectsPrimarySelectionOrder` guards the behaviour against TS `selectAllMatches` + issue #14143 expectations from `ts/src/vs/editor/contrib/find/browser/findModel.ts` and `findModel.test.ts`. Evidence captured in `agent-team/handoffs/AA4-FindModel-Review-PORT.md` and the changefeed note above.
- **Finding 2 (Dead multi-selection state)** – ✅ **Resolved in** `#delta-2025-11-24-b3-fm-multisel`. The unused `_selectionCollection/_primarySelectionIndex` storage was removed; `TestEditorContext.SetSelections` now feeds live selections directly into `FindModel.SetSelections`, and callers retrieve sanitized copies via `GetSelections()` when constructing search scopes. TS parity is enforced by re-enabling `Test07_MultiSelectionFindModelNextStaysInScopeOverlap` and `Test08_MultiSelectionFindModelNextStaysInScope`, which map 1:1 to the corresponding VS Code tests (lines ~535–590 in `findModel.test.ts`).
- **Verification** – `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~FindModelTests" --nologo` now reports 49/49, covering the restored multi-selection cases plus the primary-selection regression. No open follow-up items remain for AA4-FindModel primary ordering/dead state.
