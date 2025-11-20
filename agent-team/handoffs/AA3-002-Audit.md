# AA3-002 Audit – CL2 TextModel Search & Regex

**Date:** 2025-11-20  
**Investigator:** GitHub Copilot  
**Scope:** TS `ts/src/vs/editor/common/model/textModelSearch.ts`, `ts/src/vs/base/common/strings.ts` vs C# `src/PieceTree.TextBuffer/Core/SearchTypes.cs`, `src/PieceTree.TextBuffer/Core/PieceTreeSearcher.cs`, `src/PieceTree.TextBuffer/TextModelSearch.cs`.  
**Baseline Dependencies:** Relies on AA2-006 PieceTree search infra and AA3-001 CL1 notes for TextModel API surface.

## Overview
- Focused on regex construction (`SearchParams.parseSearchRequest`), capture semantics, and whole-word filtering.  
- Compared JS `RegExp` usage (with `unicode` flag) against the C# port that wraps `System.Text.RegularExpressions.Regex`.  
- Identified three blocking gaps: regex flavor drift, divergent whole-word classification in Unicode whitespace, and surrogate-pair handling differences that split emoji into multiple matches.

## Findings

### F1 – Regex Flavor Diverges From ECMAScript (High)
- **TS Reference:** `ts/src/vs/base/common/strings.ts#L204-L237` (`createRegExp` always emits ECMAScript regex with `global`, `unicode`, and optional `i`/`m`).  
- **C# Reference:** `src/PieceTree.TextBuffer/Core/SearchTypes.cs#L60-L106` (`SearchParams.ParseSearchRequest` builds `Regex` with `RegexOptions.Compiled | RegexOptions.CultureInvariant`, optionally `IgnoreCase`/`Multiline`).  
- **Gap:** JS `RegExp` follows ECMAScript definitions for `\b`, `\w`, `\d`, `\s`, etc. The C# port uses the default .NET engine (non-ECMAScript), which treats every Unicode letter/digit as word characters/digits. As a result, boundaries such as `\b` are computed differently, and character classes match far more symbols than VS Code would. Example: searching with `/\bcaf\b/` matches `"café"` in VS Code because `é` is treated as non-word; the .NET regex sees `é` as a word char and refuses the boundary. Likewise `/\d+/` matches Arabic-Indic numerals in C#, while VS Code restricts to ASCII `0-9`.  
- **Impact:** Regex searches/replacements will highlight different ranges, leading to incorrect edits (e.g., replacing `\bcaf\b` does nothing in the C# port even though VS Code updates it). This blocks CL2 parity claims for captures/backreferences and undermines automation that assumes ECMAScript semantics.  
- **Suggested Fix:** Execute searches with an ECMAScript-compatible engine. Minimum stop-gap: instantiate `Regex` with `RegexOptions.ECMAScript` to align character classes/boundaries, then layer compatibility tests to document remaining feature gaps (notably lookbehind). Longer term, consider embedding a dedicated ECMAScript regex runner (e.g., `Microsoft.ClearScript` or a lightweight interpreter) so parity is exact.  
- **Test Hooks:** Add `PieceTreeSearchTests.RegexParity_Escapes` covering `\bcaf\b` and `\d+` cases, asserting match counts align with VS Code behavior.

### F2 – Whole-Word Whitespace Classification Mismatch (Medium)
- **TS Reference:** `ts/src/vs/editor/common/model/textModelSearch.ts#L207-L276` (`leftIsWordBounday`, `rightIsWordBounday`) with classifier defined in `ts/src/vs/editor/common/core/wordCharacterClassifier.ts#L22-L70` (only space, tab, CR, LF plus configured separators are treated as non-word).  
- **C# Reference:** `src/PieceTree.TextBuffer/Core/SearchTypes.cs#L146-L220` (`WordCharacterClassifier` and `IsSeparatorOrLineBreak` call `UnicodeUtility.IsWhitespace`).  
- **Gap:** TS “whole word” checks only honor explicit separators plus ASCII space/tab and CR/LF. The C# classifier treats *all* Unicode whitespace characters (e.g., `\u2002` EN SPACE, `\u00A0` NBSP) as separators via `CharUnicodeInfo`. Hence, searching `bar` with whole-word mode in `"foo\u2002bar"` fails in VS Code (since EN SPACE is considered part of the word boundary), but succeeds in the C# port.  
- **Impact:** Whole-word matches in the C# engine will appear where VS Code shows nothing, causing replacement commands to mutate text that the UI never highlighted. This especially affects Markdown authors using non-breaking spaces or publications that include typographic spaces.  
- **Suggested Fix:** Align the classifier: restrict separator detection to the configured `wordSeparators` set plus the TS defaults (space, tab, CR, LF). If richer Unicode handling is desired later, gate it behind the `wordSegmenterLocales` plumbing so both runtimes make the same decision.  
- **Test Hooks:** Extend `PieceTreeSearchTests.WholeWordSeparators` with samples containing NBSP/EN SPACE to assert no matches appear unless those characters are added to the separator string.

### F3 – Surrogate-Pair Quantifiers Split Matches (High)
- **TS Reference:** `ts/src/vs/base/common/strings.ts#L204-L237` (regex created with `unicode: true`) and `ts/src/vs/editor/common/model/textModelSearch.ts#L287-L340` (`Searcher.next` uses `strings.getNextCodePoint` so `RegExp.lastIndex` advances by full code points).  
- **C# Reference:** `src/PieceTree.TextBuffer/Core/SearchTypes.cs#L60-L106` (no Unicode-mode equivalent) and `src/PieceTree.TextBuffer/Core/PieceTreeSearcher.cs#L9-L63` (`Regex.Match` operates on UTF-16 chars; `_lastIndex = match.Index + match.Length`).  
- **Gap:** With the JS `unicode` flag, regex tokens like `.` or quantifiers treat a surrogate pair (e.g., 😀) as a single logical character—`/^.$/u` matches an emoji once. .NET regex processes UTF-16 `char`s, so the same pattern never matches (string length appears as 2), and `/./` yields two matches for a single emoji. That means `findMatches` in the C# port either double-counts astral characters or fails to find patterns that VS Code highlights, and replacements can split a surrogate pair in half, producing invalid Unicode.  
- **Impact:** Any regex or replace command over emoji, historic scripts, or musical symbols diverges: wrap-search jumps twice, match ranges report incorrect lengths, and captured text may be half of a surrogate pair (breaking downstream rendering).  
- **Suggested Fix:** Provide a Unicode-aware pattern translation layer that rewrites ECMAScript tokens (`.`, character classes, quantifiers) into constructs that consume full surrogate pairs (e.g., replace `.` with `(?:[\uD800-\uDBFF][\uDC00-\uDFFF]|[\u0000-\uD7FF\uE000-\uFFFF])`) before compiling the .NET regex. Alternatively, route regex evaluation through an engine that natively understands code points.  
- **Test Hooks:** Add `PieceTreeSearchTests.RegexEmojiParity` to cover `^.$`, `.`, and `.{2}` matches over emoji strings, ensuring match counts and replacement ranges match VS Code output.

## Suggested Fixes
1. Switch to an ECMAScript-compatible regex engine (or enable `RegexOptions.ECMAScript` with follow-up coverage) so character classes/boundaries match VS Code semantics.  
2. Align the `WordCharacterClassifier` with TS logic by limiting automatic separators to ASCII space/tab/CR/LF plus the configured `wordSeparators`.  
3. Introduce a Unicode-code-point aware translation for regex tokens (or a true ECMAScript engine) so surrogate pairs are consumed as single logical characters.

## Impacts
- Regex searches currently return different sets of matches from VS Code, breaking replace-in-editor parity and risking silent data corruption.  
- Whole-word queries highlight extra regions in the C# backend, meaning any editor UI that trusts engine results will diverge from the VS Code reference implementation.  
- Astral-plane characters can be split or skipped, so CL2 requirements for capture/backreference fidelity are unmet.

## Changefeed Impact
- Once fixes begin, Info-Indexer should flag: `src/PieceTree.TextBuffer/Core/SearchTypes.cs`, `src/PieceTree.TextBuffer/Core/PieceTreeSearcher.cs`, `src/PieceTree.TextBuffer/TextModelSearch.cs`, and new/updated tests under `PieceTree.TextBuffer.Tests`.  
- Porter-CS (AA3-004) should note the regex engine change plus new tests in the changefeed delta referenced by `agent-team/indexes/README.md#delta-2025-11-20`.

## References
- `ts/src/vs/base/common/strings.ts`  
- `ts/src/vs/editor/common/model/textModelSearch.ts`  
- `ts/src/vs/editor/common/core/wordCharacterClassifier.ts`  
- `src/PieceTree.TextBuffer/Core/SearchTypes.cs`  
- `src/PieceTree.TextBuffer/Core/PieceTreeSearcher.cs`  
- `src/PieceTree.TextBuffer/TextModelSearch.cs`

## Next Steps for Porter-CS (AA3-004)
1. Prototype running regex evaluations with ECMAScript semantics (either via `RegexOptions.ECMAScript` or an embedded JS engine) and add regression tests for `\b`, `\w`, and `\d`.  
2. Adjust `WordCharacterClassifier` to mirror TS behavior and add coverage for NBSP/EN SPACE.  
3. Implement surrogate-aware token rewrites (or another Unicode-complete strategy) and validate with emoji-heavy search/replace scenarios.  
4. Document the behavior changes in `docs/reports/migration-log.md` and coordinate with QA for the new parity tests.
