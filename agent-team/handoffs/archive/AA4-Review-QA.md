# AA4-Review-QA — DocUI FindModel Scope Fix (`#delta-2025-11-24-find-scope`)

## Summary
- **Scope of review:** Validated Porter’s DocUI FindModel scope override + normalization fix described in `agent-team/handoffs/AA4-Review-Porter.md` and staged across `src/TextBuffer/DocUI/FindModel.cs`, `tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs`, and `tests/TextBuffer.Tests/TestMatrix.md`.
- **Key observations:**
  - `FindModel` now queues a pending `_state.SearchScope` override, hydrates follow-up scopes via `FindDecorations.GetFindScopes()`, and threads a viewport-height provider through to `FindDecorations` to match the DocUI host options passed by `DocUIFindController` / `TestEditorContext`.
  - DocUI regression tests gained `Test45_SearchScopeTracksEditsAfterTyping` and `Test46_MultilineScopeIsNormalizedToFullLines`, directly covering the AA4 review issues (scope hydration after edits + TS #27083 normalization).
  - `tests/TextBuffer.Tests/TestMatrix.md` already records the rerun command + notes under **Targeted reruns (delta-2025-11-24-find-scope)**, so no documentation patch was required beyond confirming the entry.

## Validation
| Command | Result | Notes |
| --- | --- | --- |
| `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~FindModelTests --nologo` | **44/44** (2.7s) | Confirms FindModel + DocUI scope suites including new Tests 45/46 are green. |
| `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~DocUIFind --nologo` | **39/39** (1.8s) | Matches all DocUI find-related fixtures (controller, model, selection, decorations). Previous expectation of 17 tests is obsolete because the filter now also captures `DocUIFindDecorationsTests` and other DocUI suites landed during Batch #3. |

## Verification Notes
- **Scope override hydration:** Verified that `_state.Change(searchScope: …, searchScopeProvided: true, …)` now sets a pending override consumed on the next `ResolveFindScopes()` call before falling back to decoration-derived scopes, mirroring `findModel.ts` behavior after edits. `Test45` asserts `MatchesCount` remains 1 after inserting text ahead of the scoped range and that navigation still targets the hydrated match.
- **Normalization:** Confirmed `_normalizeFindScopes` parity is restored—multi-line scopes snap to full lines, trim trailing blank columns, and respect `GetLineMaxColumn`. `Test46` reproduces TS issue #27083 and now passes.
- **Documentation:** `tests/TextBuffer.Tests/TestMatrix.md` already links this rerun to `#delta-2025-11-24-find-scope`; migration log row `B3-FM-Scope` and changefeed entry `agent-team/indexes/README.md#delta-2025-11-24-find-scope` were cross-checked for alignment.

## Findings
1. **Filter cardinality drift (medium):** `FullyQualifiedName~DocUIFind` now matches 39 tests instead of the historical 17 because DocUIFindDecorations and additional DocUI suites share the prefix. Future QA instructions should reflect the expanded scope or move to more specific filters to avoid confusion when auditing targeted reruns.
2. **No functional regressions detected:** Both the scoped override and normalization behaviors behave as described by Porter; no additional gaps surfaced during code review or targeted tests.

## Next Steps
1. Decide whether to keep the broader `~DocUIFind` umbrella (since it now covers all DocUI find/decor suites) or document a narrower filter (e.g., `DocUIFindModelTests|DocUIFindControllerTests`) for faster spot checks.
2. Track follow-up Investigator guidance on remaining multi-cursor DocUI FindModel cases so the final Batch #3 parity closure can piggyback on this verified harness.

## References
- Migration log row `docs/reports/migration-log.md` → **B3-FM-Scope**.
- Changefeed entry `agent-team/indexes/README.md#delta-2025-11-24-find-scope`.
- Test matrix section `tests/TextBuffer.Tests/TestMatrix.md#targeted-reruns-delta-2025-11-24-find-scope`.
