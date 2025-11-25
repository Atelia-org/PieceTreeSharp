# B3-FM Multi-Selection Parity Plan (2025-11-24)

## Summary
- **Gap:** `DocUIFindModelTests` still skips TS Test07/08 ("multi-selection find model next stays in scope" / "multi-selection find model next stays in scope (overlap)"). Those scenarios verify that when multiple disjoint selections exist, `FindModel` cycles matches within the currently active selection scope and respects overlapping ranges.
- **Reason for deferral:** Our `TestEditorContext` harness only supports a single `Range` scope at a time; we also short-circuited `FindModel.SetSelection` to a single `Range`. Multi-selection parity therefore needs explicit instrumentation and spec coverage before Porter can safely port the TS logic.
- **Goal:** Deliver Investigator guidance + Porter implementation/tests so Batch #3 fully mirrors TS findModel multi-selection behavior, unblocking the remaining 2/43 tests.

## Investigator-TS Action Items
1. **Document TS semantics**
   - Capture cursor + decoration expectations for Test07/08 directly from `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts` (include range arrays for each navigation step).
   - Clarify how `FindModelBoundToEditorModel` derives per-selection scopes vs. `FindDecorations` overlays when multiple selections exist.
2. **Map services & APIs**
   - List the TypeScript helpers involved (e.g., `editor.setSelections`, `_startPositionBeforeSelectionScope`, `withTestCodeEditor` multi-selection harness) and provide the C# equivalents or required shims.
3. **Update handoff**
   - Produce `agent-team/handoffs/B3-FM-MultiSelection-Audit.md` summarizing Findings (F1: overlapping scopes, F2: selection order, F3: wrap semantics) and blocking dependencies. Reference this plan + TS lines for traceability.

## Porter-CS Action Items
1. **Harness updates**
   - Extend `TestEditorContextOptions` to accept multiple initial selections + helper methods to set them during a test.
   - Teach `TestEditorContext.AssertFindState` (or new helper) to compare multiple highlighted ranges in deterministic order.
2. **FindModel enhancements**
   - Ensure `SetSelection` handles `Selection[]` arrays, preserving `_currentSelection` per primary selection while respecting `_state.SearchScope` multi-range input.
   - Verify `_decorations.GetFindScopes()` correctly releases multiple scopes and that navigation honors their ordering even when overlapping.
3. **Test migration**
   - Port TS Test07/08 verbatim into `DocUIFindModelTests.cs` (update numbering to keep parity) using the improved harness.
   - Add regression asserts for overlap + wrap-around behavior to prevent future regressions.
4. **Documentation & telemetry**
   - Update `docs/plans/ts-test-alignment.md` and `tests/TextBuffer.Tests/TestMatrix.md` to mark FindModel parity as 43/43 once QA signs off.
5. **TODO – align with `B3-FM-MultiSelection-Audit` (2025-11-24)**
   - Implement the audit’s F1–F3 recommendations (multi-selection harness, `FindModel` API overload, TS scope tables) and tag the eventual changefeed as `#delta-2025-11-24-b3-fm-multisel` when Porter hands results to QA.

## QA Hooks
- Targeted rerun command: `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~DocUIFindModelTests.Test0(7|8)`.
- Capture before/after logs in `agent-team/handoffs/AA4-009-QA.md` with references to this plan + upcoming Investigator/Porter handoffs.

## References
- TS source: `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts` (Test07 & Test08).
- C# harness: `tests/TextBuffer.Tests/DocUI/TestEditorContext.cs`.
- Implementation surface: `src/TextBuffer/DocUI/FindModel.cs`, `src/TextBuffer/DocUI/FindDecorations.cs`.
- Existing changefeed: `agent-team/indexes/README.md#delta-2025-11-24-find-scope` (scope override baseline to build upon).
