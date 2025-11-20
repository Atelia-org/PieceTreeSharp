# AA3-004 Result – CL2 Search/Regex Fixes

## Summary
- Ported CL2 search paths (`SearchParams`, `PieceTreeSearcher`, `TextModelSearch`) to execute ECMAScript-compatible regexes, aligning `\b`, `\d`, and `\w` semantics with VS Code’s `strings.createRegExp` helper.
- Rebuilt the word-boundary classifier so whole-word scans only treat the configured separator set plus ASCII space/tab/CR/LF as delimiters, matching the TS `WordCharacterClassifier` contract.
- Added a Unicode-aware wildcard translator so tokens such as `.` and `.{n}` consume complete code points; zero-length loops still advance via surrogate-aware helpers, preventing emoji from being split.
- Extended `PieceTreeSearchTests`/`TextModelSearchTests` with AA3 audit cases covering `\bcaf\b`, `\d+`, NBSP/EN SPACE whole-word searches, emoji quantifiers, and multi-range selection flows.

## Implementation Notes
- **ECMAScript Regex Engine:** `SearchParams.ParseSearchRequest` now compiles regexes with `RegexOptions.ECMAScript | CultureInvariant | Compiled`, and `PieceTreeSearcher` defends this invariant by re-wrapping incoming regex instances when necessary. The parser also expands `\u{...}` escapes before applying escapes.
- **Unicode Wildcards:** Added `ApplyUnicodeWildcardCompatibility` which rewrites naked `.` tokens into `(?:[\uD800-\uDBFF][\uDC00-\uDFFF]|[^\u000A\u000D\u2028\u2029])`, mirroring the TS `unicode` flag so wildcard quantifiers step over entire astral-plane characters without matching ECMAScript line terminators.
- **Word Separators:** `WordCharacterClassifier` now limits automatic whitespace to SPACE/TAB/CR/LF and relies solely on the provided separator string for any other characters, eliminating the prior blanket `CharUnicodeInfo.IsWhitespace` check.
- **Tests:** `PieceTreeSearchTests` gained assertions for ECMAScript word boundaries, ASCII-only `\d`, Unicode separator opt-in, and emoji wildcard behavior; `TextModelSearchTests` now verifies multi-range regex searches capture `café` matches within selection scopes.

## Validation
```
dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj
```
- Result: **Passed** (84 tests) – new search suites exercise the AA3-004 regressions.

## Known Limitations
- .NET’s `RegexOptions.ECMAScript` still lacks support for modern ECMAScript-only constructs (e.g., lookbehind, named capture backreferences). Those patterns now fail fast with a documented exception; fallback strategies will be tracked under a follow-up AA3 work item if parity is required.

## Docs & Changefeed
- Logged this work under the AA3-004 row in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md) with file/test references.
- Added a dedicated AA3-004 bullet to [`agent-team/indexes/README.md#delta-2025-11-20`](../indexes/README.md#delta-2025-11-20) so Info-Indexer consumers can see the search-stack delta.
