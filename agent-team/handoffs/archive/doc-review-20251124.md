# Doc Review – 2025-11-24

## Findings & Actions
1. **Changefeed anchor missing for `#delta-2025-11-23-b3-decor-stickiness`**  
   - Evidence: `AGENTS.md` (B3-Decor entries), `docs/plans/ts-test-alignment.md` (Live Checkpoints), `docs/sprints/sprint-03.md` (Progress Log R16/R17), `tests/TextBuffer.Tests/TestMatrix.md` (targeted rerun section), `docs/reports/migration-log.md` ("B3-Decor" row), and `agent-team/members/porter-cs.md` all point to `agent-team/indexes/README.md#delta-2025-11-23-b3-decor-stickiness`, but that anchor is not defined inside `agent-team/indexes/README.md`.  
   - Action: add a `### delta-2025-11-23-b3-decor-stickiness` block to `agent-team/indexes/README.md` capturing the original B3-Decor drop (233/233 baseline, targeted `DocUIFindDecorationsTests` 6/6 and `DecorationStickinessTests` 4/4, plus doc/test references). Once the anchor exists, re-check the referencing docs to ensure their changefeed links resolve.

2. **DocUIFindDecorationsTests count is now 9, but docs still state 8**  
   - Evidence: `tests/TextBuffer.Tests/DocUI/DocUIFindDecorationsTests.cs` contains 9 `[Fact]` tests. However `AGENTS.md`, `agent-team/indexes/README.md` (delta `-b3-decor-stickiness-review`), `agent-team/members/porter-cs.md`, `agent-team/task-board.md` (B3-Decor row), `docs/plans/ts-test-alignment.md` (Live Checkpoints), `docs/sprints/sprint-03.md` (Progress Log R18), and `docs/reports/migration-log.md` ("B3-Decor-Stickiness-Review" row) all describe the targeted command as `DocUIFindDecorationsTests --nologo (8/8)`.  
   - Action: update every spot above to record the correct `9/9` result so the documentation matches the actual test suite size.

3. **Migration log still marks `B3-DocUI-StagedFixes` as lacking a changefeed entry**  
   - Evidence: `docs/reports/migration-log.md` row "B3-DocUI-StagedFixes" shows `Changefeed Entry? = N` and notes "Changefeed TBD", yet `agent-team/indexes/README.md` now exposes `#delta-2025-11-24-b3-docui-staged`.  
   - Action: flip the row to `Changefeed Entry? = Y` and cite `agent-team/indexes/README.md#delta-2025-11-24-b3-docui-staged` so consumers know the delta is live.

4. **Status reminders omit the newest changefeed anchors**  
   - Evidence: `AGENTS.md` "状态更新提示" only lists `#delta-2025-11-19` through `#delta-2025-11-23`, and `docs/sprints/sprint-02.md` "Status Edits Reminder" mentions only `#delta-2025-11-20` / `#delta-2025-11-21`. Both files now describe work tied to `#delta-2025-11-24-find-scope`, `#delta-2025-11-24-find-replace-scope`, and `#delta-2025-11-24-b3-docui-staged`.  
   - Action: extend those reminder lists to include the 2025-11-22, 2025-11-23, and 2025-11-24 anchors so editors check the full set of deltas before making further edits.

5. **`tests/TextBuffer.Tests/TestMatrix.md` headline date is stale**  
   - Evidence: the document title still reads `# PT-005 QA Matrix (2025-11-23)` even though the body logs Nov 24 deltas (Test45–Test48, 235/235 baseline).  
   - Action: update the heading date to `2025-11-24` (or the latest date reflected in the content) to avoid confusion about the currency of the test matrix.
