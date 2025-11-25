# B3 PieceTree Deterministic CRLF + Centralized LineStarts — Investigator Notes (2025-11-25)

## Scope
- **Objective:** inventory the remaining deterministic blocks from `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` that must be ported to finish the PieceTree deterministic suite, with emphasis on the CRLF normalization bug battery and the centralized line-start tests that sit immediately after the existing prefix-sum/offset/range sections.
- **Current C# coverage:** `PieceTreeDeterministicTests` (prefix sum → range) and `PieceTreeNormalizationTests` (two "delete CR in CRLF" tests + first "Line breaks replacement" case) land in `tests/TextBuffer.Tests`. No other CRLF or centralized-line-start fixtures exist yet.

## 1. Missing TS cases vs. C# status
### 1.1 CRLF suite (TS lines 1054–1292)
| TS test name | TS lines | C# status |
| --- | --- | --- |
| delete CR in CRLF 1 | 1057–1066 | ✅ `PieceTreeNormalizationTests.Delete_CR_In_CRLF_1` already covers it, though not yet wired through the deterministic harness |
| delete CR in CRLF 2 | 1068–1077 | ✅ `PieceTreeNormalizationTests.Delete_CR_In_CRLF_2` |
| random bug 1 | 1079–1096 | ❌ missing |
| random bug 2 | 1097–1113 | ❌ missing |
| random bug 3 | 1114–1135 | ❌ missing |
| random bug 4 | 1137–1156 | ❌ missing |
| random bug 5 | 1157–1184 | ❌ missing |
| random bug 6 | 1185–1210 | ❌ missing |
| random bug 8 | 1211–1227 | ❌ missing |
| random bug 7 | 1229–1244 | ❌ missing |
| random bug 10 | 1247–1267 | ❌ missing |
| random bug 9 | 1270–1290 | ❌ missing |

**Notes**
- Every "random bug" test toggles `createTextBuffer([""], false)` → i.e., CRLF normalization disabled. Each script calls `testLinesContent` and `assertTreeInvariants`, so line-start validation must stay enabled.
- None of these scripts currently exist in C# (grep for "random bug" inside `tests/TextBuffer.Tests` returns only prefix-sum/range entries).

### 1.2 Centralized lineStarts with CRLF suite (TS lines 1294–1589)
| TS test name | TS lines | C# status |
| --- | --- | --- |
| delete CR in CRLF 1 | 1297–1303 | ❌ missing |
| delete CR in CRLF 2 | 1305–1312 | ❌ missing (note: this one uses default `normalizeEOL=true`) |
| random bug 1 | 1315–1331 | ❌ missing |
| random bug 2 | 1332–1345 | ❌ missing |
| random bug 3 | 1348–1364 | ❌ missing |
| random bug 4 | 1370–1387 | ❌ missing |
| random bug 5 | 1389–1413 | ❌ missing |
| random bug 6 | 1416–1438 | ❌ missing |
| random bug 7 | 1441–1455 | ❌ missing |
| random bug 8 | 1458–1471 | ❌ missing |
| random bug 9 | 1474–1492 | ❌ missing |
| random bug 10 | 1495–1513 | ❌ missing |
| random chunk bug 1 | 1516–1532 | ❌ missing |
| random chunk bug 2 | 1534–1554 | ❌ missing |
| random chunk bug 3 | 1557–1573 | ❌ missing |
| random chunk bug 4 | 1576–1588 | ❌ missing |

**Notes**
- These variants start from non-empty CRLF-heavy initial chunks and rely heavily on `getLinesRawContent()` parity plus `testLineStarts`. They exercise centralized line-start tables after the TS refactor; we have zero parity tests on the C# side right now.
- The four "random chunk" cases require `createTextBuffer(chunks[], false)` with multi-chunk bootstrap data, so we must pass the harness `initialChunks` overload and keep `normalizeChunks=false`.

### 1.3 Other deterministic suites still outstanding
| Suite | TS lines | Highlights | Current C# coverage |
| --- | --- | --- | --- |
| `random is unsupervised` | 1592–1740 | Long-running deterministic scripts that stress line-start + content invariants via seeded pseudo-random ops | ❌ none (only two harness smoke tests exist; fuzz suite still pending per `B3-PieceTree-Fuzz-INV`) |
| `buffer api` | 1742–1836 | `equal`/`getLineCharCode`/`getNearestChunk` regression tests | ⚠️ partial: `tests/TextBuffer.Tests/UnitTest1.cs` touches `GetLineCharCode`, but no equality or `getNearestChunk` parity tests |
| `search offset cache` | 1838–1953 | `render white space exception` + four "Line breaks replacement is not necessary when EOL is normalized" cases | ⚠️ only the first "Line breaks..." test exists (`PieceTreeNormalizationTests.Line_Breaks_Replacement...`); remaining 3 + the whitespace exception test are missing |
| `snapshot` / `chunk based search` | 1968+ | Snapshot immutability + search chunk behavior | ❌ not yet in C# test suite |

These suites are out-of-scope for the immediate CRLF push but should stay on the deterministic backlog once Porter-CS finishes the CRLF + centralized line-start work.

## 2. Porter-CS implementation plan
### 2.1 Layout & ordering
1. **Extend `PieceTreeDeterministicTests`** with two new regions:
   - `#region CRLF normalization (TS lines 1054-1292)` → 10 new `[Fact]` methods named `CrlfRandomBug01MatchesTsScript` ... `CrlfRandomBug10MatchesTsScript` (preserving TS numbering; include `_08` and `_07` order as in TS). Keep the existing `Delete_CR_In_CRLF_*` tests in `PieceTreeNormalizationTests` but call out in comments that CRLF block is split across files.
   - `#region Centralized lineStarts with CRLF (TS lines 1294-1589)` → 14 `[Fact]` methods: two delete tests, ten `CentralizedLineStartsRandomBug##MatchesTsScript`, and four `CentralizedLineStartsRandomChunkBug##MatchesTsScript`.
2. **Harness usage:**
   - Instantiate via `new PieceTreeFuzzHarness(testName, initialChunks: new[] { seed }, normalizeChunks: normalizeEolFlag)`. For TS calls that used `createTextBuffer([''], false)` keep `normalizeChunks:false`; for the single case that omitted `false`, pass `normalizeChunks:true`.
   - Feed scripts via `PieceTreeScript.RunScript(...)`. For tests that compare the final buffer text to `str`, switch to `PieceTreeScript.RunScriptWithMirror(...)` and assert `Assert.Equal(expected, harness.Buffer.GetText());`.
   - Call `harness.AssertState("crlf-random-bugX-final")` at the end to mirror TS `assertTreeInvariants` + `testLineStarts`.
3. **Naming + order guidelines:**
   - Mirror TS order to keep diff review simple (CRLF random bug 1→10, then centralized delete tests followed by random bug 1→10, then chunk bug 1→4).
   - Example skeleton:
     ```csharp
     [Fact]
     public void CrlfRandomBug01MatchesTsScript()
     {
         using var harness = new PieceTreeFuzzHarness(nameof(CrlfRandomBug01MatchesTsScript), initialChunks: new[] { string.Empty }, normalizeChunks: false);
         PieceTreeScript.RunScript(
             harness,
             InsertStep(0, "\n\n\r\r", "crlf-random-bug1-insert-1"),
             InsertStep(1, "\r\n\r\n", "crlf-random-bug1-insert-2"),
             DeleteStep(5, 3, "crlf-random-bug1-delete-1"),
             DeleteStep(2, 3, "crlf-random-bug1-delete-2"));
         harness.AssertState("crlf-random-bug1-final");
     }
     ```
4. **Centralized line-start chunk cases:** add helper `CreateHarnessFromChunks(string testName, bool normalize, params string[] chunks)` (internal to the test class) to avoid repeating the constructor boilerplate. Example usage for `random chunk bug 2`:
   ```csharp
   using var harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomChunkBug02MatchesTsScript), false,
       "\n\r\n\n\n\r\n\r\n\r\r\n\n\n\r\r\n\r\n");
   var expected = PieceTreeScript.RunScriptWithMirror(harness,
       InsertStep(16, "\r\n\r\r", "cls-random-chunk-2-insert-1"),
       ...);
   Assert.Equal(expected, harness.Buffer.GetText());
   harness.AssertState("cls-random-chunk-2-final");
   ```
5. **Document use of `PieceTreeBufferAssertions`** for the two centralized delete tests (asserting `LineCount == 2`) so we keep parity with TS expectations.

### 2.2 Helper adjustments
- Consider adding `PieceTreeDeterministicData` (new internal static class) that returns the `PieceTreeScriptStep[]` arrays per TS test. This keeps `PieceTreeDeterministicTests` readable and makes it easy to cross-check against TS by referencing shared constants.
- Add a tiny assertion helper inside the test class:
  ```csharp
  private static void AssertFinalText(PieceTreeFuzzHarness harness, string expected, string phase)
  {
      Assert.Equal(expected, harness.Buffer.GetText());
      harness.AssertState(phase);
  }
  ```
  This ensures we keep both text parity and invariants in sync.
- No core production changes required; all work stays in tests + optional helper classes.

### 2.3 Dependencies & sequencing
- Finish CRLF block first (shared empty-initial buffer). Centralized line-start tests rely on the same helpers but add chunk inputs; doing CRLF first validates the overall approach before layering chunk coverage.
- After both blocks land, revisit `PieceTreeNormalizationTests` to either:
  1. Move the existing delete tests into the deterministic file (so all CRLF parity lives together), **or**
  2. Leave them in place but update comments to reference the new deterministic block, preventing future engineers from assuming the CRLF suite is done.
- Keep `Line breaks replacement ... 2/3/4` and `search offset cache` tests in backlog—they share helpers with the centralized suite and can piggy-back on the same harness infrastructure once this work is merged.

## 3. QA & documentation guidance
### 3.1 Test commands
| Command | Purpose | Expected |
| --- | --- | --- |
| `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeDeterministicTests` | Runs all deterministic parity tests end-to-end | 50 Facts (22 existing + 28 new). Suite should take <2s locally. |
| `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeNormalizationTests` | Captures the legacy CRLF normalization tests (sanity check after any refactor/move) | 3 Facts today; will grow if we relocate tests. |
| `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~PieceTreeFuzzHarnessTests` | Ensures the deterministic harness smoke cases still pass with the new script coverage | 2 Facts currently; acts as guard rail for harness regressions. |

### 3.2 Documentation & bookkeeping
- **Changefeed:** add `#delta-2025-11-25-b3-piecetree-deterministic-crlf` to `agent-team/indexes/README.md`, pointing at the modified test files + rerun commands above.
- **Migration log:** append a row in `docs/reports/migration-log.md` describing "B3 PieceTree deterministic CRLF + centralized lineStarts" with the same delta ID and spec lines (TS 1054-1589).
- **Test matrix:** update `tests/TextBuffer.Tests/TestMatrix.md` under the PieceTree section to flip the CRLF + centralized line-start rows to ✅ once Porter merges the tests. Include references to the new Fact names for traceability.
- **TS alignment plan:** modify `docs/plans/ts-test-alignment.md` to mark the CRLF suite and centralized line-start suite as "Ported" (previously flagged under Tier A). Note that `search offset cache` and `buffer api` remain open.

### 3.3 Reporting
- Capture a short summary in the changefeed referencing both this memo and the deterministic test file(s) touched, so downstream auditors can trace which TS cases are covered.
- Ensure TRX artifacts from CI capture the expanded suite counts (50 deterministic Facts) for future regressions.

## 4. Risks & open questions
1. **Script transcription error potential:** 28 new deterministic tests equal ~250 insert/delete ops to transcribe. Strongly recommend staging the script data in a helper (see §2.2) so reviewers can diff the literal strings against TS once and re-use them.
2. **Normalization flag parity:** Several centralized tests rely on the default `normalizeEOL=true`. Double-check the harness constructor semantics (string ctor vs. chunk ctor) so we set `normalizeChunks` correctly. Consider adding explicit unit coverage that fails if normalization is wrong.
3. **Unported deterministic suites:** `buffer api`, `search offset cache`, and the remaining "Line breaks replacement" cases are still outstanding and should remain on the deterministic backlog after this task closes.
4. **TestMatrix accuracy:** Today the matrix marks "PieceTree deterministic" as ✅ even though the CRLF/centralized suites are missing. Update notes immediately after Porter lands the new tests to prevent future misreporting.

---
**Next steps for Porter-CS:** follow §2 to add the missing tests, then hand back rerun logs + doc updates per §3. QA can key off the `dotnet test --filter PieceTreeDeterministicTests` run (expect 50 tests total) to validate.
