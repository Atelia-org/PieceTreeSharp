````markdown
# B3-FSel Result – Sprint 03 R13 (Selection Search String Parity)

## Summary
- Delivered **B3-FSel (`#delta-2025-11-23-b3-fsel`)** by porting VS Code's `getSelectionSearchString` helper into `FindUtilities` along with a lightweight `IEditorSelectionContext` and `WordAtPosition` contract so DocUI layers can seed Find text from the caret or current selection.
- Added `DocUIFindSelectionTests` (3 Facts) that mirror `find.test.ts` scenarios: empty selection uses word-under-cursor, single-line selections return the exact text, and multiline selections return `null`.
- Documented the new capability across Sprint 03 log, TestMatrix, and Migration Log so QA/DocMaintainer can trace the change.

## Implementation Highlights
- `src/TextBuffer/DocUI/FindUtilities.cs`
  - `GetSelectionSearchString()` mirrors TS behavior, including the 524,288 character guard and ASCII word-separator heuristics when selections are empty.
  - `GetWordAtPosition()` exposes a reusable caret-word probe (default separators match SearchParams) for upcoming FindController work.
  - `SelectionSeedMode`, `IEditorSelectionContext`, and `WordAtPosition` provide the minimal surface required by DocUI hosts/tests.
- `tests/TextBuffer.Tests/DocUI/DocUIFindSelectionTests.cs`
  - Introduces `SelectionTestContext` harness and the 3 parity tests ported from `find.test.ts`.
- Docs
  - `tests/TextBuffer.Tests/TestMatrix.md`: marks DocUIFindSelectionTests as ✅ Tier A with delta reference; updates baseline to 189 tests.
  - `docs/sprints/sprint-03.md`: Run R13 entry covering B3-FSel.
  - `docs/reports/migration-log.md`: migration row with key files + command.

## Tests
- `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → **189/189** passing (includes the new DocUI selection cases).

## Follow-ups / Dependencies
- Upcoming Sprint 03 items (R14/R15) will call into `FindUtilities` from the forthcoming DocUI FindController implementation.
- Word-separator Unicode parity remains tracked under OI-014; current helper intentionally focuses on ASCII parity per B3 scope.
````