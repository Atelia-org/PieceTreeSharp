# B3-TextModelSearch Investigation (R35 · 2025-11-25)

**Owner:** Investigator-TS  \
**TS Source:** `ts/src/vs/editor/test/common/model/textModelSearch.test.ts`  \
**C# Targets:** `tests/TextBuffer.Tests/TextModelSearchTests.cs`, `tests/TextBuffer.Tests/PieceTreeSearchTests.cs`, `src/TextBuffer/TextModelSearch.cs`, `src/TextBuffer/Core/SearchTypes.cs`, `src/TextBuffer/Core/WordCharacterClassifier.cs`

## Summary of Remaining Gaps (grouped by theme)

| Theme | TS Coverage (reference tests) | Current C# Coverage | Gap / Impact |
| --- | --- | --- | --- |
| **1. Whole-word & boundary matrix** | `Simple find`, `Case sensitive find`, `Whole words find`, `issue #3623` (non-latin), `issue #27459`, `issue #27594`, `issue #53415` (`\\W` across line breaks) | `PieceTreeSearchTests` only covers literal find, one whole-word check, and ASCII-only separator toggles; `TextModelSearchTests` has no word-boundary assertions | Missing verification that `WordCharacterClassifier` honors mixed-case literals, non-Latin symbols, punctuation adjacency, and `\W` interpretation across CR/LF. No regression tests proving `wordSeparators` caching & `TextModelSearch.FindNext/FindPrevious` respect boundaries when `findInSelection` toggles whole-word mode. |
| **2. Multiline regex & CRLF normalization** | `multiline find 1-4`, `multiline find with line beginning`, `matching empty lines via ^\s*$`, `matching lines starting with A...`, `multiline find with line ending`, `issue #4836 (^.*$)`, `multiline find for non-regex string`, `\\n matches \\r\\n`, `\\r can never be found` | Only `PieceTreeSearchTests.FindMatches_MultilineLiteralAcrossCrLf` touches literal `"alpha\nbeta"` on CRLF; no regex spanning assertions | Need assurance that `TextModelSearch` normalizes CR/LF for regex searches, handles start/end anchors across selections, and refuses raw `\r` tokens (TS guarantees). Missing coverage for zero-content lines, boundary-only matches, or `EndOfLineSequence.CRLF` toggling. |
| **3. Capture arrays, backreferences & navigation APIs** | `findMatches capturing`, `findMatches multiline capturing`, `findNextMatch/PreviousMatch with capturing`, begin-/end-boundary navigation suites | C# only validates `FindMatches_ProvidesCaptureGroups`; no tests request capture data from `FindNextMatch` / `FindPreviousMatch`, nor multi-line capture arrays | Risk of regressions in `SearchParams.captureMatches` plumbing (`captureMatches` flag currently ignored in navigation). Need to assert `FindNext/Previous` preserve capture groups, wrapping semantics, and multi-range search results. |
| **4. SearchParams parsing & regex metadata** | `parseSearchRequest` (regex + literal variants), `isMultilineRegexSource` (single vs multi), `Simple find using unicode escape sequences` | No dedicated tests exercise `SearchParams.ParseSearchRequest()` or `TextModelSearch.IsMultilineRegexSource` equivalent logic | Without TS parity we cannot guarantee `.NET Regex` flags (`ECMAScript`, `CultureInvariant`, `Multiline`) or unicode-escape handling match TS `strings.createRegExp`. Also missing regression for `isMultilineRegexSource` heuristics used by DocUI for replace preview. |
| **5. Zero-width + Unicode edge cases** | `issue #74715 (\\d*)`, `issue #100134 (surrogate pairs & ZWJ)`, `/^$/`, `/^/`, `/$/`, `/.*/`, `^.*$`, `matching empty lines`, `\W` windowed cases | `PieceTreeSearchTests` covers zero-length lookahead on emoji and basic emoji quantifiers; nothing checks repeated zero-length matches, surrogate pairs with ZWJ, or anchor-only regexes | Need regression guard for infinite loops / cursor advancement around zero-width regex (TS added after #74715). Also missing anchor-only tests to ensure `FindNext/Previous` start/end positions align with TS behavior. |

## Dependencies & Harness Notes
- **Word boundary data** – rely on `WordCharacterClassifier` in `src/TextBuffer/Core/WordCharacterClassifier.cs`; tests must pass explicit `wordSeparators` to `TextModel.FindMatches`/`FindNextMatch` and optionally toggle `TextModel.Options.EndOfLine` to cover CRLF vs LF. No new harness needed, but we should add a small helper inside `tests/TextBuffer.Tests/TextModelSearchTests.cs` to replicate TS `assertFindMatches()` behavior (including CRLF pass).
- **Model creation** – `TextModel` already supports `SetEol()` via `TextModel.SetText()` calls; we can mimic TS `model2.setEOL(EndOfLineSequence.CRLF)` by calling `model.SetEol("\r\n")` once a test is constructed.
- **Capture arrays** – C# `FindNextMatch` / `FindPreviousMatch` currently return `FindMatch` (same struct as `FindMatches`). We simply need to invoke them with `captureMatches: true` and assert `Matches` payload. No extra hooks required.
- **SearchParams** – parser lives in `src/TextBuffer/Core/SearchTypes.cs`; to mirror TS tests we should instantiate `new SearchParams(...)` and call `ParseSearchRequest()` directly in dedicated unit tests (could live in a new `SearchParamsTests.cs`).
- **isMultilineRegexSource** – function exists at `TextModelSearch.IsMultilineRegexSource` (mirrors TS `isMultilineRegexSource`). We can unit-test this static helper without touching `TextModel`.
- **Intl.Segmenter parity** – TS classifier optionally uses `Intl.Segmenter`; .NET version lacks this. Current investigation treats this as known limitation; document it in tests (skip/mark as expected difference for CJK word boundaries) so QA can justify missing cases.

## Proposed Plan – Porter-CS
1. **Reorganize `tests/TextBuffer.Tests/TextModelSearchTests.cs`**
   - Introduce TS-like helper `AssertFindMatches(string text, string pattern, bool isRegex, bool matchCase, string? wordSeparators, params (int sl, int sc, int el, int ec)[] expected)` to avoid duplication.
   - Port TS "Simple/Case sensitive/Whole words" tests plus anchor-only suites (`/^/`, `/$/`, `/^$/`, `/.*/`, `^.*$`).
2. **Add `TextModelSearchMultilineTests` section**
   - Create a nested class or new file covering TS multiline suites (line beginning/ending regex, boundary expressions, `text\nJust`, `^\s*$\n`, `matching lines starting with A...`, `matching lines ending with B`, `\n` vs CRLF, `\r` not findable).
   - Ensure tests run against both `\n` and forced `\r\n` EOL by duplicating with `.SetEol`. Reference `TextModel.SetEndOfLine(EndOfLine.CRLF)` once available (if not, call `TextModel.SetText` with `UseCrlf`).
3. **Capture & navigation parity**
   - Extend `PieceTreeSearchTests` or new `TextModelSearchCaptureTests` to cover `FindNextMatch`/`FindPreviousMatch` with `captureMatches: true` for single-line and multi-line regex. Validate wrap-around semantics and that `Matches` arrays align with TS `FindMatch` expectations.
   - Add tests for selection-scoped navigation (mirroring TS `findNextMatch` with repeating begins) to ensure `_findMatchesInRange` logic matches TS `TextModelSearch`. File: `tests/TextBuffer.Tests/TextModelSearchTests.cs`.
4. **SearchParams & metadata suites**
   - Create `tests/TextBuffer.Tests/SearchParamsTests.cs` verifying `ParseSearchRequest()` for literal vs regex strings, unicode escapes, newline escapes, invalid regex cases, and word separator map injection.
   - Add `TextModelSearchIsMultilineTests` for `IsMultilineRegexSource` coverage (single vs multi-line patterns) referencing TS dataset.
5. **Zero-width & Unicode regression tests**
   - Port `issue #74715` and `issue #100134` verbatim, ensuring zero-length matches progress across surrogate pairs/ZWJ sequences.
   - Add tests for `matching empty lines using boundary expression`, ensuring zero-length matches across CRLF.
6. **Documentation & attribution**
   - Annotate each new test block with TS source comment (`// Source: textModelSearch.test.ts – <test name>`). Update `tests/TextBuffer.Tests/TestMatrix.md` TextModelSearch row to reflect new cases and changefeed.
   - Prepare patch for `docs/reports/migration-log.md` referencing a new delta `#delta-2025-11-25-b3-textmodelsearch` once Porter work lands.

## QA-Automation Plan
1. **Targeted filters**
   - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSearchTests --nologo`
   - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchTests --nologo`
   - New suites: `SearchParamsTests`, `TextModelSearchMultilineTests` (if split). Provide individual filter commands in `TestMatrix.md`.
2. **CRLF verification**
   - Record separate runs where `TextModel` EOL forced to CRLF (mirrors TS double-pass). Document this in QA memo (two sub-runs per suite if necessary).
3. **Regression hooks**
   - Add `TestMatrix` row enumerating TS cases covered (IDs referencing this memo). Update `docs/reports/migration-log.md#b3-textmodelsearch` once Porter commits.
4. **Automation artifacts**
   - Provide TRX logs for both targeted suites + full run (expected >324 tests). Keep evidence in `agent-team/handoffs/B3-TextModelSearch-QA.md` when work completes.

## Changefeed / Migration-Log Checklist
- Reserve `#delta-2025-11-25-b3-textmodelsearch` for this audit; Info-Indexer to add Investigator row pointing to this memo.
- When Porter lands the parity tests, add `#delta-2025-11-26-b3-textmodelsearch-port` (or reuse the same delta if single drop) with code/test paths, targeted commands, and QA evidence.
- Update `docs/reports/migration-log.md` with new entry under Sprint 03 (TextModelSearch parity) referencing the changefeed; ensure `tests/TextBuffer.Tests/TestMatrix.md` TextModelSearch row cites the same delta.
- DocMaintainer to mirror the delta in `AGENTS.md`, `docs/plans/ts-test-alignment.md`, `agent-team/task-board.md`, and Sprint log once QA signs off.

## Assumptions & Open Questions
- **Intl.Segmenter parity** remains out of scope; tests referencing non-Latin whole-word behavior (issue #3623) will use ASCII-style classifier and document limitation if behavior deviates.
- `TextModel.SetEol()` API parity: if not yet exposed, Porter may need to add a helper or temporarily re-create the model with CRLF text to mimic TS `model.setEOL` pass.
- TS `assertFindMatches` enforces both `findMatches`, `findNextMatch`, and `findPreviousMatch` parity. C# helper should do the same; confirm we can call `model.Dispose()` deterministically between passes to avoid shared state.
- Confirm whether future DocUI tests depend on `SearchParams.isMultilineRegexSource`. If yes, coordinate timeline so DocUI harness changes consume the new coverage.
- Need clarity on whether Info-Indexer prefers a single combined delta or separate `INV/PORT/QA` anchors for this workstream; placeholder above assumes combined `#delta-2025-11-25-b3-textmodelsearch` but can split if necessary.
