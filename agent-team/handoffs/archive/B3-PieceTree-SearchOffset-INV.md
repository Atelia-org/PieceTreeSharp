# B3 – PieceTree Search Offset Cache Investigator Memo (Run R31)

**Date:** 2025-11-25  
**Changefeed anchor:** `#delta-2025-11-25-b3-search-offset`  
**Upstream plan:** `agent-team/handoffs/B3-PieceTree-SearchOffset-PLAN.md` (Priority #2 in the deterministic backlog)  
**TS source:** `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` (lines 1810–1884)  
**Related backlog:** `agent-team/handoffs/B3-PieceTree-Deterministic-Backlog.md`

## 1. TS script extractions
All scripts below were replayed directly from the TS suite. `createTextBuffer` normalizes inputs to `\n`, so the logs assume normalized EOLs unless noted.

### 1.1 `render white space exception`
- **Initial buffer:** `"class Name{\n\t\n\t\t\tget() {\n\n\t\t\t}\n\t\t}"` (single chunk snapshot)  
- **Purpose:** exercises the search-offset cache through a mixed insert/delete sequence that mimics the VS Code "render whitespace" regression.
- **Operation log:**

| Step | API call | Details |
| --- | --- | --- |
| 1 | `insert(12, 's')` | Inserts `s` after `class Name` header |
| 2 | `insert(13, 'e')` | Adds `e` (forming `se`) |
| 3 | `insert(14, 't')` | Adds `t` (`set`) |
| 4 | `insert(15, '()')` | Appends `()` after `set` |
| 5 | `delete(16, 1)` | Drops the closing `)` of the newly inserted call |
| 6 | `insert(17, '()')` | Adds another `()` after the remaining `)` |
| 7 | `delete(18, 1)` | Removes the leading `(` of that pair |
| 8 | `insert(18, '}')` | Inserts `}` before the next block |
| 9 | `insert(12, '\n')` | Forces a blank line between `Name{` and `set` |
| 10 | `delete(12, 1)` | Immediately removes that newline (cache invalidation) |
| 11 | `delete(18, 1)` | Deletes the `}` inserted at step 8 |
| 12 | `insert(18, '}' )` | Re-inserts `}` (ensures offsets stable) |
| 13 | `delete(17, 2)` | Removes `}` + following char to simulate brace collapse |
| 14 | `delete(16, 1)` | Deletes the remaining `)` |
| 15 | `insert(16, ')')` | Re-adds `)` (forces cache reuse after invalidation) |
| 16 | `delete(15, 2)` | Drops the `()` inserted earlier |

- **TS asserts:**
  - `const content = pieceTable.getLinesRawContent(); assert(content === str);`
  - Implicitly relies on `PieceTreeSearchCache` invalidation via repeated inserts/deletes.

### 1.2 `Line breaks replacement is not necessary when EOL is normalized 2`
- **Initial buffer:** `"abc\n"` (builder already normalized to `\n`).
- **Operation:** `insert(4, 'def\nabc')` (appends after the trailing newline). No deletes occur.
- **Assertions:** `testLineStarts`, `testLinesContent`, `assertTreeInvariants` ensuring cache + prefix sums stay stable when normalized text is appended past an existing `\n` terminator.

### 1.3 `Line breaks replacement is not necessary when EOL is normalized 3`
- **Initial buffer:** `"abc\n"`.
- **Operation:** `insert(2, 'def\nabc')` (inserts mid-line, before `c`).
- **Assertions:** same trio (`testLineStarts`, `testLinesContent`, `assertTreeInvariants`). Validates cache updates when normalized text is inserted before an existing newline boundary.

### 1.4 `Line breaks replacement is not necessary when EOL is normalized 4`
- **Initial buffer:** `"abc\n"`.
- **Operation:** `insert(3, 'def\nabc')` (between `c` and the newline).
- **Assertions:** same trio. Exercises a "within-line" insertion immediately before the buffered newline, ensuring we do not double-count EOLs when the cache splits chunks.

> ✅ Case 1 (base test without suffix) already maps to `PieceTreeNormalizationTests.Line_Breaks_Replacement_Is_Not_Necessary_When_EOL_Is_Normalized`, so only the three suffixed variants plus the render-whitespace script remain for Priority #2.

## 2. Mapping to `PieceTreeSearchOffsetCacheTests`
| TS test name | Proposed C# Fact (namespace `PieceTreeSearchOffsetCacheTests`) | Deterministic script asset | Helpers to invoke | Notes |
| --- | --- | --- | --- | --- |
| `render white space exception` | `RenderWhitespaceScriptPreservesSearchCache()` | `PieceTreeDeterministicScripts.SearchOffset_RenderWhitespace` (new) | `PieceTreeDeterministicScripts`, `PieceTreeFuzzHarness.RunScript`, `PieceTreeBufferAssertions.AssertState/AssertLineStarts` | Needs the full insert/delete log to guarantee cache churn; include `PieceTreeBufferAssertions.AssertState` to compare `getLinesRawContent()` with TS `str`. |
| `Line breaks replacement is not necessary when EOL is normalized 2` | `NormalizedInsert_AppendsAfterTrailingLf()` | `PieceTreeDeterministicScripts.SearchOffset_NormalizedEolCase2` | `PieceTreeFuzzHarness`, `PieceTreeBufferAssertions.AssertLineStarts` | Build script with single `insert(4, 'def\nabc')`; reuse `testLineStarts` logic via assertions. |
| `Line breaks replacement is not necessary when EOL is normalized 3` | `NormalizedInsert_WithinPrefix()` | `PieceTreeDeterministicScripts.SearchOffset_NormalizedEolCase3` | Same as above | Insert at offset 2; verify intermediate `LineStarts` and buffer text. |
| `Line breaks replacement is not necessary when EOL is normalized 4` | `NormalizedInsert_BeforeTrailingLf()` | `PieceTreeDeterministicScripts.SearchOffset_NormalizedEolCase4` | Same as above | Insert at offset 3; ensures cache respects normalized newline before trailing `\n`. |

**Helper expectations:**
- **`PieceTreeFuzzHarness`** handles script playback + invariant hooks.
- **`PieceTreeDeterministicScripts`** must host the four new logs so that QA can reuse them in standalone harness runs.
- **`PieceTreeBufferAssertions`** provides `AssertState` and `AssertLineStarts` parity checks used in TS (`testLinesContent`, `testLineStarts`).

## 3. Helper gaps & clarifications for Porter
1. **Search cache inspection:** We currently lack a friend accessor to confirm `PieceTreeSearchCache` hit/miss counters after the scripted edits. Porter may need either (a) an internal test-only accessor exposing `PieceTreeBuffer.Debug_SearchCacheGeneration` or (b) instrumentation via `PieceTreeBufferAssertions` that verifies the cached node offset matches the latest tree structure.
2. **Deterministic scripts namespace:** `PieceTreeDeterministicScripts` does not yet have a "SearchOffset" region. Create one to host the four JSON-like logs and keep parity with CRLF + snapshot assets (see `#delta-2025-11-24-b3-piecetree-fuzz` and `#delta-2025-11-25-b3-piecetree-snapshot`).
3. **Normalization flag coverage:** Each normalized case depends on the builder’s `normalizeEOL: true` default. Ensure the C# scripts initialize buffers via `PieceTreeScriptBuilder.FromLines()` (or equivalent) so Porter does not reintroduce CRLF conversions that would change offsets.
4. **QA command reuse:** Document the primary rerun hook now—`export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchOffsetCacheTests --nologo`—so QA/DocMaintainer can cite it when the changefeed is published.

## 4. Next steps aligned to `#delta-2025-11-25-b3-search-offset`
1. **Porter-CS (B3-SearchOffset-PORT):**
   - Add the four deterministic script assets + new test fixture (5 Facts: render-whitespace + the three remaining normalized cases + the already-covered baseline for regression safety).
   - Decide whether search-cache assertions require exposing internals or if invariants suffice.
2. **QA-Automation:**
   - Extend `tests/TextBuffer.Tests/TestMatrix.md` with a `PieceTreeSearchOffsetCache` row referencing this memo and the plan.
   - Record rerun evidence for the filtered + full suite commands once Porter lands the tests.
3. **Info-Indexer + DocMaintainer:**
   - When Porter/QA finish, publish `#delta-2025-11-25-b3-search-offset` in the changefeed and update Sprint/Task Board/Plan entries that now reference this memo.

This memo satisfies Run R31 requirements by capturing every outstanding TS script, mapping them to the future `PieceTreeSearchOffsetCacheTests`, and documenting helper expectations for Porter under Priority #2.
