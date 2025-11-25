# B3 – TextModelSearch Porter Memo (Run R36)

**Date:** 2025-11-25  \
**Owner:** Porter-CS  \
**Changefeed anchor:** `#delta-2025-11-25-b3-textmodelsearch`

## Scope
- Restored the TS `isMultilineRegexSource` helper inside `src/TextBuffer/Core/SearchTypes.cs` (`SearchPatternUtilities.IsMultilineRegexSource`) so `SearchParams.ParseSearchRequest()` no longer depends on `TextModelSearch`. The helper scans literal `\n` tokens plus escaped `\n`, `\r`, and `\W` sequences, matching the TS parity block at `textModelSearch.ts` lines 196-238.
- Ported the full 45-test TS battery from `ts/src/vs/editor/test/common/model/textModelSearch.test.ts` into `tests/TextBuffer.Tests/TextModelSearchTests.cs`, including:
  - Parse-search matrices (`parseSearchRequest` invalid/literal/regex, unicode wildcard handling) and the new `ExpectedSearchData` + `AssertParseSearchResult` helpers.
  - Word-boundary + issue regressions (`Simple find`, `Whole words find`, issues #3623/#27459/#27594) plus literal navigation suites.
  - Multiline literal/regex buckets (`^line`, `line$`, multiline spans, CRLF normalization) and zero-width / Unicode anchor cases (issues #74715, #100134).
  - Capture-aware navigation (`FindMatches`, `FindNext`, `FindPrevious`) with shared helper scaffolding so TS expectations can be asserted verbatim.
- Updated QA collateral (TestMatrix row, sprint/migration log hooks, this memo) to call out the helper refactor + imported suites so downstream teams see the true TS-parity scope instead of the prior “fixture-only” description.

## Verification
- `cd /repos/PieceTreeSharp && export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~TextModelSearchTests --nologo`
  - Result: **45/45 passed**, duration **2.0s** (PIECETREE_DEBUG=0).

## Next Steps / Dependencies
- **Docs/Changefeed refresh:** Update `docs/plans/ts-test-alignment.md`, `docs/reports/migration-log.md`, `docs/sprints/sprint-03.md`, and `agent-team/indexes/README.md` to reflect the helper refactor + 45-test suite (replacing the older “fixture-only / 35 tests” narrative).
- **Intl.Segmenter backlog:** Investigator still tracks the Intl.Segmenter-aware word-boundary work plus the remaining TS search edge cases (Segmenter-driven case folding + ICU-specific regex flags). Plan to address those in the next AA4 search tranche now that the base suite is green.
- **QA follow-up:** Once docs are updated, QA-Automation can incorporate the new 45-test evidence into `tests/TextBuffer.Tests/TestMatrix.md` so future regressions keep the TS parity filters on the dashboard.

## References
- Investigator brief: `agent-team/handoffs/Review-20251125-Investigator.md`
- Source updates: `src/TextBuffer/Core/SearchTypes.cs`, `tests/TextBuffer.Tests/TextModelSearchTests.cs`
- Evidence log: `tests/TextBuffer.Tests/TestMatrix.md`, `docs/reports/migration-log.md`, `docs/sprints/sprint-03.md`, `agent-team/indexes/README.md` (pending refresh)
