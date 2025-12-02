# AA4-Review-Porter – DocUI FindModel Scope Regression Fix (2025-11-24)

## Summary
- Added a one-shot search-scope override inside [`FindModel`](../..//src/TextBuffer/DocUI/FindModel.cs) so freshly assigned `_state.SearchScope` values feed the next `Research()` call before we fall back to decoration-tracked scopes. Subsequent searches now hydrate scopes from `_decorations.GetFindScopes()` which survive edits, re-aligning with the TS `findModel.ts` implementation.
- Reintroduced the TS `_normalizeFindScopes` semantics: multi-line scopes now snap to `startColumn=1`, trim trailing blank lines when `endColumn==1`, and extend to `GetLineMaxColumn`. This unlocks issue #27083 parity and ensures regex anchors behave like VS Code.
- Added DocUI regression tests to prove both behaviors: `Test45_SearchScopeTracksEditsAfterTyping` confirms scopes rehydrate after `PushEditOperations`, and `Test46_MultilineScopeIsNormalizedToFullLines` guards the TS #27083 scenario. `TestMatrix.md` + migration log now reference `#delta-2025-11-24-find-scope` for downstream coordination.

## Touched Files
- `src/TextBuffer/DocUI/FindModel.cs` – pending scope override flag/buffer, decoration-first resolution, and normalization helpers.
- `tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs` – new Test45/Test46 covering scope tracking + multi-line normalization.
- `tests/TextBuffer.Tests/TestMatrix.md` – DocUIFindModel row + targeted rerun section annotated with `#delta-2025-11-24-find-scope`.
- `docs/reports/migration-log.md`, `agent-team/indexes/README.md` – new migration/changefeed entries.

## Validation
- `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindModelTests --nologo` *(per AA4 instruction – VSTest reports 0 matches because the class is named `FindModelTests`, command logged for audit)*.
- `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo` → **44/44** green.

## Follow-ups / Risks
- QA should re-run the DocUI Find suites once Investigator replays the AA4 review to confirm no additional scope regressions remain.
- Future DocUI changes that mutate `_state.SearchScope` must continue to call `FindReplaceState.Change(... searchScopeProvided: true ...)` so the pending override fires; document now highlights this requirement.
- Multi-line normalization currently mirrors TS behavior for line-start/line-end anchors; if VS Code ever exposes partial-line scopes at the DocUI layer we will need a compatibility toggle.
