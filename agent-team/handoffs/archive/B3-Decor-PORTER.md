# B3-Decor-PORTER – Range Highlight & Stickiness Parity

## Context
- **Sprint**: Sprint 03 – Batch #3 (DocUI/Decorations track)
- **Delta target**: `#delta-2025-11-23-b3-decor-stickiness`
- **Scope**: Close Investigator R16 gaps around DocUI find decorations, TextModel decoration queries, and tracked range stickiness coverage so FindController work can depend on stable highlight metadata.

## Implementation Summary
1. **DocUI Find Decorations parity**
   - Mirrored `findDecorations.ts` behavior: range highlight trimming for trailing blank lines, overview throttling (approximation decorations when matches > 1000), cached find-scope normalization, and disposal paths covering match/overview/scope/range highlight decorations.
   - Added theme metadata (inline classes, overview ruler + minimap colors) to `ModelDecorationOptions`, ensuring Markdown renderer + future DocUI overlays can style highlights identically to VS Code.
2. **TextModel decoration APIs**
   - Introduced `GetAllDecorations(ownerId)` and `GetLineDecorations(lineNumber, ownerId)` with `ShowIfCollapsed` filtering, plus `GetDecorationIdsByOwner` helper to keep tests/controller code from tracking IDs manually.
   - Reused `DecorationsTrees.EnumerateAll()` so per-line queries stay O(log n) per range.
3. **Test coverage**
   - Expanded `DecorationTests` with per-line queries, owner lifecycle helpers, and event propagation assertions that mirror TS `modelDecorations.test.ts` suites.
   - Added `DecorationStickinessTests` for the tracked-range matrix (four stickiness modes × insert-before/after expectations) and validated `forceMoveMarkers` override behavior.
   - Created `DocUIFindDecorationsTests` exercising range highlight trimming, overview throttling, scope normalization/cache, match index lookup, and wrap-around navigation helpers.
4. **Docs & matrices**
   - Updated `tests/TextBuffer.Tests/TestMatrix.md`, `docs/plans/ts-test-alignment.md`, `docs/reports/migration-log.md`, and `docs/sprints/sprint-03.md` with the new suites plus 233-test baseline reference for follow-up documentation and changefeed publication.

## Validation
| Command | Result |
| --- | --- |
| `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | 235/235 ✔ (2.9s) |
| `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DecorationStickinessTests --nologo` | 4/4 ✔ |
| `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindDecorationsTests --nologo` | 8/8 ✔ |

## Outstanding Risks / Follow-ups
- **Changefeed**: Info-Indexer still needs to publish `#delta-2025-11-23-b3-decor-stickiness`; all docs reference this anchor awaiting broadcast.
- **Layout-aware overview merge**: Current approximation uses a fixed 2-line merge window (TS relies on editor layout height). Acceptable for now but may need revisit once DocUI exposes viewport metrics.
- **Remaining TS suites**: `modelDecorations.test.ts` also includes delete/replace edge cases and validation-owner filtering we have not ported yet; keep backlog item for AA-series follow-up if needed.
- **DocUI integration**: FindController callers should start consuming the new `GetAllDecorations`/`GetLineDecorations` APIs to avoid scanning the full buffer in tests.

## Next Steps
1. Hand off to Info-Indexer to record the changefeed entry and update `agent-team/indexes/README.md`.
2. QA-Automation to monitor CI runtime impact (233 tests) and split long-running decoration suites if needed.
3. Planner/DocMaintainer to reference this handoff when updating AGENTS/Task Board once the delta is live.

## Review Follow-up (CI-1/CI-2/CI-3 + W-1/W-2)
- Investigator R16 review flagged cached scopes, trimmed newline scopes, overview throttling constants, shared owner IDs, and insufficient DocUI tests. Follow-up patch removes `_cachedFindScopes`, preserves raw scope ranges, threads host `ViewportHeightPx` into `FindDecorations`/`FindModel`/`DocUIFindController`, and allocates decoration owners per instance.
- `DocUIFindDecorationsTests` gained `FindScopesPreserveTrailingNewline`, `FindScopesTrackEdits`, and `OverviewThrottlingRespectsViewportHeight`; `TestEditorContextOptions` exposes `ViewportHeightPx` so DocUI harness can simulate layout changes.
- Documentation updated (`tests/TextBuffer.Tests/TestMatrix.md`, `docs/plans/ts-test-alignment.md`, `docs/sprints/sprint-03.md`, `docs/reports/migration-log.md`, `agent-team/task-board.md`, `AGENTS.md`) under `#delta-2025-11-23-b3-decor-stickiness-review`. Validation rerun: full suite 235/235, `--filter DocUIFindDecorationsTests` 8/8, `--filter DecorationStickinessTests` 4/4.
