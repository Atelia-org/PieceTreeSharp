# AA3-003 Result – TextModel Options & Undo Parity

## Summary
- Ported TS `TextModel` creation/resolved option stack (`detectIndentation`, `defaultEOL`, tab/indent sizes, insert spaces, trim whitespace, bracket colorization flags) and now persist the normalized values on each instance.
- Introduced `ILanguageConfigurationService` + attachment lifecycle events so models raise `OnDidChangeLanguageConfiguration` and `OnDidChangeAttached` when language IDs or editor bindings change.
- Rebuilt the undo/redo plumbing: `EditStack` now bridges an `IUndoRedoService`, tracks cursor states + labels, and `TextModel.PushEditOperations` accepts cursor-state computers/labels just like the TS API.
- Extended `TextModelSearch` with `SearchRangeSet` so `FindMatches/FindNextMatch/FindPreviousMatch` support multi-range (find-in-selection) scopes without breaking legacy callers.

## Implementation Notes
- **Options & Creation Flow:** `TextModelOptions.cs` now defines `TextModelCreationOptions`, `BracketPairColorizationOptions`, and `TextModelResolvedOptions.Resolve(...)`. `TextModel.cs` accepts the new options/services via constructor overloads, auto-runs indentation detection on load, and syncs `_creationOptions` whenever options change.
- **Services:** Added `src/PieceTree.TextBuffer/Services/ILanguageConfigurationService.cs` and `IUndoRedoService.cs` (w/ `InProcUndoRedoService`). `EditStack.cs` consumes these services, tracks open elements, and records labels/cursor states per undo frame.
- **Model Surface:** `TextModel` now exposes `AttachEditor/DetachEditor`, `OnDidChangeLanguageConfiguration`, `OnDidChangeAttached`, and the richer `PushEditOperations` signature. Language configuration subscriptions are refreshed whenever `SetLanguage` runs.
- **Search:** `TextModelSearch.cs` gained `SearchRangeSet`, range-set aware find helpers, and multi-range aware next/previous search flows. New API overloads in `TextModel` surface these capabilities without breaking existing single-range callers.
- **Tests:** `TextModelTests.cs` covers creation options, indentation auto-detect, cursor-state computer invocation, language-configuration events, and attach/detach transitions. New `TextModelSearchTests.cs` verifies multi-range find/next/previous behavior.

## Validation
```
dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj
```
- Result: **Passed** – updated suites exercised `TextModelTests` (new cases) and `TextModelSearchTests`.

## Changefeed & Docs
- Logged in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md) under AA3-003 with links to the modified files and the test command.
- Added AA3-003 delta entry to [`agent-team/indexes/README.md#delta-2025-11-20`](../indexes/README.md#delta-2025-11-20) so Info-Indexer consumers can trace the TextModel/Undo/Search updates.
- `docs/reports/audit-checklist-aa3.md` CL1 row now points at this result, noting the new `TextModelTests` + `TextModelSearchTests` coverage.
