````markdown
# B3-FC Result – Sprint 03 R14 (FindController Core)

## Summary
- Delivered **B3-FC-Core (#delta-2025-11-23-b3-fc-core)**: introduced `DocUIFindController` with command helpers (`StartFind*`, `NextMatch`, `NextSelectionMatch`, `Replace/ReplaceAll`, toggle actions) plus focus management, storage persistence, and optional global clipboard support.
- Added lightweight DocUI host/harness shims (`TestEditorHost`, `TestFindControllerStorage`, `TestFindControllerClipboard`) so controller logic can run without VS Code services, keeping selections/search scopes/edits observable inside xUnit.
- Ported the first batch of `findController.test.ts` scenarios (issues #1857, #3090, #6149, #41027, #9043, #27083, #58604, #38232) into `DocUIFindControllerTests.cs`, ensuring parity for navigation loops, regex auto-escape, scope lifecycle, and selection-seeded regex navigation.
- Updated TestMatrix, Sprint 03 log (Run R14), Migration Log, AGENTS, and Info-Indexer changefeed to anchor the new delta; created this handoff for downstream QA/DocMaintainer review.

## Tests
- `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindControllerTests --nologo` → **Passed (10/10)** covering the imported issue-focused scenarios.
- `PIECETREE_DEBUG=0 dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → **Passed (199/199)** full-suite baseline after integrating controller + tests.

## File Highlights
- `src/TextBuffer/DocUI/DocUIFindController.cs`: new controller tied to `FindReplaceState`/`FindModel`, implements command helpers, search scope updates, selection seeding via `FindUtilities`, storage persistence, and optional global clipboard writes. Includes `FindControllerHostOptions`, `FindFocusBehavior`, storage/clipboard interfaces, and default no-op fallbacks.
- `tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs`: introduces `TestEditorHost` + storage/clipboard stubs and 10 parity tests mirroring TS issues (#1857/#3090/#6149/#41027/#9043/#27083/#58604/#38232). Validates selection seeding, regex escaping, scope auto-updates, replace focus handling, and selection-seeded regex navigation.
- `tests/TextBuffer.Tests/TestMatrix.md`: adds DocUIFindController row (TS source, coverage, delta) plus refreshed baseline entry (199/199) and targeted command reference.

## Known Limitations / Follow-ups
- **R15 – B3-FC-Scope:** still need to implement full search scope lifecycle (multi-selection reopen behavior, visual annotations) and Mac-only global find clipboard sync (rich text + preserved history). These remain deferred but called out in Sprint 03 log.
- **Advanced clipboard / focus context keys:** simplified host currently omits context keys + DOM focus; revisit once OI-012 harness design lands to model VS Code's context-key service and focus telemetry.
- **Decorations/overlay capture (R16):** controller output is wired for DocUI, but overlay snapshots still rely on Markdown renderer; integrate once Decorations stickiness capture tasks resume.

## Changefeed & Migration Log
- Logged under **2025-11-23 · B3-FC-Core** in `docs/reports/migration-log.md` with command evidence and file list.
- Info-Indexer published [`agent-team/indexes/README.md#delta-2025-11-23-b3-fc-core`](../indexes/README.md#delta-2025-11-23-b3-fc-core); AGENTS/Sprint/TestMatrix now link to this delta.
````