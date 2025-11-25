# Review-20251125-Investigator

## Scope
- Pending git diffs touching search infrastructure: `src/TextBuffer/Core/SearchTypes.cs`, `tests/TextBuffer.Tests/TextModelSearchTests.cs`, `tests/TextBuffer.Tests/PieceTreeSearchTests.cs`.
- Documentation collateral advertised in the status log: `docs/plans/ts-test-alignment.md`, `docs/reports/migration-log.md`, `docs/sprints/sprint-03.md`, `tests/TextBuffer.Tests/TestMatrix.md`, and `agent-team/indexes/README.md`.
- Cross-referenced TS sources under `ts/src/vs/editor/common/model/textModelSearch.test.ts` and `ts/src/vs/editor/common/model/textModelSearch.ts` for behavioral parity.

## Code/Test Delta Snapshot
- **`SearchPatternUtilities` helper** (C# `SearchTypes.cs`, lines 221-304) now exposes `IsMultilineRegexSource` so tests can hit the same logic that TS exports via `isMultilineRegexSource` (`ts/src/vs/editor/common/model/textModelSearch.ts` lines 196-238). Implementation matches the TS scan for literal `\n` characters and escaped `\n`, `\r`, `\W` sequences.
- **`TextModelSearchTests.cs` expanded to 45 `[Fact]` cases**, effectively porting the entire TS suite at `ts/src/vs/editor/test/common/model/textModelSearch.test.ts` (lines 77-822). New C# coverage includes:
  - Word-boundary matrix + issue regressions (`Simple find` through `issue #27594`, TS lines 77-385).
  - Multiline literal/regex search, CRLF normalization, and newline boundary assertions (TS lines 174-347 & 559-593).
  - Capture arrays + navigation helpers (`findMatches/Next/Previous` blocks, TS lines 486-557).
  - Zero-width + Unicode anchor scenarios including issues #74715 and #100134 (TS lines 386-484 & 780-821).
  - `parseSearchRequest` and `isMultilineRegexSource` matrices (TS lines 612-778) with new `ExpectedSearchData` verifier in C#.
- **`tests/TextBuffer.Tests/PieceTreeSearchTests.cs`** only renames the test class (now `PieceTreeSearchTests`). This decouples the new TextModel-focused suites from the PieceTree-centric smoke tests so `dotnet test --filter TextModelSearchTests` hits only the TS-parity additions (45 cases) while the legacy PieceTree search tests stay at 11 cases.

## Findings / Risks
1. **Plan appendix still flags the suite as "gap".** `docs/plans/ts-test-alignment.md` (Appendix table, lines 70-80) still describes TextModelSearch as “R35 gap audit … Next: Porter add TS-style helper + suites”. The code already contains those suites, so the plan is stale and misleads AA4 schedulers who are tracking this row for completion.
2. **Changefeed entry misrepresents the scope.** `agent-team/indexes/README.md#delta-2025-11-25-b3-textmodelsearch` claims the delta only realigned the Issue #53415 fixture, states “no engine code changed”, and lists `--filter TextModelSearchTests --nologo (35/35)`. In reality we:
   - Added `SearchPatternUtilities` inside `SearchTypes.cs`.
   - Brought over the full 45-test TS suite.
   - Ran 45 TextModelSearch tests (not 35).
   The entry also points to `agent-team/handoffs/B3-TextModelSearch-PORT.md`, but that file does not exist.
3. **Migration log duplicates the same inaccuracies.** `docs/reports/migration-log.md` row `B3-TextModelSearch` repeats the false “fixture-only” scope, the `35/35` run count, and references the missing porter handoff. Anyone auditing the log will think no new tests landed and no code moved.
4. **Sprint log R36 summary underplays the drop.** `docs/sprints/sprint-03.md` entry R36 only mentions fixing Issue #53415 and rerunning existing suites. It omits the much larger addition of the TS search battery + helper refactor, so the sprint narrative is out of sync with what actually changed.
5. **Porter memo content is stale.** `agent-team/handoffs/B3-TextModelSearch-PORT.md` claims “no runtime code touched” and logs only 35 TextModelSearch tests, but the current drop adds the helper plus 45 cases. Anyone following the memo will miss the new scope + rerun numbers.

## Open Questions / Follow-ups
1. Should the plan + sprint + changefeed all be updated to describe the real scope (helper refactor + 45-suite import) and correct rerun counts (45 tests under `TextModelSearchTests`)?
2. Can Porter/QA refresh `B3-TextModelSearch-PORT.md` to describe the helper refactor + 45-test suite instead of the older “fixture-only” summary and 35-test counts?
3. Do we still need the dedicated `PieceTreeSearchTests` smoke suite now that the TS parity suite covers most cases? If yes, we should clarify in `TestMatrix.md` which scenarios remain unique to PieceTree so future refactors don’t drop them inadvertently.

## Recommended Actions
1. **DocMaintainer / Planner:** update `docs/plans/ts-test-alignment.md` TextModelSearch row to “Verified (Sprint 03 R36)” and list the newly ported TS buckets + any residual gaps (e.g., Intl.Segmenter backlog) so Appendix stays authoritative.
2. **Info-Indexer:** rewrite `agent-team/indexes/README.md#delta-2025-11-25-b3-textmodelsearch` to describe the helper refactor + 45 tests, fix the rerun counts, and cite the real artifacts (`Review-20251125-Investigator.md` until a PORT memo exists). Same correction is needed in `docs/reports/migration-log.md` and `docs/sprints/sprint-03.md`.
3. **Porter / QA:** either publish the missing `B3-TextModelSearch-PORT.md` (with rerun evidence + scope) or scrub the references from migration log / changefeed / sprint log to avoid broken links.
4. **AA4 leads:** once documentation reflects reality, decide whether AA4 search tasks can be unblocked (WordSeparator/Intl.Segmenter work still outstanding) or if another run of Investigator is needed.

---
*Prepared by Investigator-TS on 2025-11-25.*
