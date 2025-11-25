# AA4 Search Review – Porter-CS Handoff (2025-11-25)

## Scope
- Address Investigator findings from `agent-team/handoffs/AA4-SearchReview-20251125.md` (layering regression + missing TS suites) under Sprint 03 Run R36.

## Code Changes
1. **Search pattern helper ownership**
   - `src/TextBuffer/Core/SearchTypes.cs`: Reintroduced a TS-style `SearchPatternUtilities.IsMultilineRegexSource` helper to keep the multiline detection logic inside the core search layer. `SearchParams.IsMultilinePattern` now calls this helper, removing the dependency on `TextModelSearch`. The file retains `using PieceTree.TextBuffer;` only for `TextPosition` and no longer references `TextModelSearch`.
   - `src/TextBuffer/TextModelSearch.cs`: Deleted the redundant `IsMultilineRegexSource` method (the functionality lives in the core helper).

2. **Test coverage parity**
   - `tests/TextBuffer.Tests/TextModelSearchTests.cs`:
     - Added `TextModelSearchTests_ParseSearchRequest`, `TextModelSearchTests_IsMultilineRegexSource`, and `TextModelSearchTests_FindNextMatchNavigation` suites covering TS cases (parseSearchRequest invalid/non-regex/regex, multiline helper matrices, literal/`^line`/multiline/`line$` navigation).
     - Extended `TextModelSearchTestHelper` with `ExpectedSearchData`, `DefaultRegexOptions`, and `AssertParseSearchResult` utilities to keep assertions concise.
   - `tests/TextBuffer.Tests/TestMatrix.md`: Updated the TextModelSearch row (now explicitly lists parseSearchRequest + navigation parity) and refreshed the targeted rerun table entry to reflect the new 45-test total.

## Validation
| Command | Result |
| --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSearchTests --nologo` | 45/45 green (2.0s) |

## Documentation Hooks
- TestMatrix row + targeted rerun entry now point at this drop (`#delta-2025-11-25-b3-textmodelsearch`).
- SearchTypes helper should be indexed under the same changefeed so DocMaintainer/Info-Indexer can mention the new `SearchPatternUtilities` helper when documenting FR-01/FR-02 cache dependencies.

## Follow-ups
- None pending for this drop. If additional TS cases surface (e.g., capture-only navigation variants), reuse the helper scaffolding introduced here.
