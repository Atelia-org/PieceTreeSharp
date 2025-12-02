# WS5-PORT — Shared Test Harness Result

## Intro
- **Changefeed Anchors:** [`#delta-2025-11-26-sprint04-r1-r11`](../../docs/reports/migration-log.md#delta-2025-11-26-sprint04-r1-r11) · [`#delta-2025-11-26-ws5-port-harness`](../../docs/reports/migration-log.md#delta-2025-11-26-ws5-port-harness)
- **Traceability:** Harness artifacts logged in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-ws5-port-harness) and referenced in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws5-harness).
- **Scope Summary:** Implement WS5 shared test harness recommendations—builder helpers, cursor utilities, word-data helpers, snapshot infra, and exemplar tests to demonstrate usage.
- **TS References:** `cursorAtomicMoveOperations.test.ts`, `wordTestUtils.ts`, VS Code snapshot harness docs indicated in `WS5-INV-TestBacklog.md`.

## Implementation
| Artifact | File | Purpose |
| --- | --- | --- |
| Test editor builder | `tests/TextBuffer.Tests/Helpers/TestEditorBuilder.cs` | Fluent TextModel builder with option toggles, marked content parsing, and context creation helpers. |
| Cursor utilities | `tests/TextBuffer.Tests/Helpers/CursorTestHelper.cs` | Structured cursor/selection asserts, multi-cursor helpers, pipe-position parsing, cursor-state factories. |
| Word data helpers | `tests/TextBuffer.Tests/Helpers/WordTestUtils.cs` | Port of TS `wordTestUtils`, providing ASCII/CamelCase/CJK datasets and `[MemberData]` generators. |
| Snapshot utilities | `tests/TextBuffer.Tests/Helpers/SnapshotTestUtils.cs` + `tests/TextBuffer.Tests/Snapshots/**` | Golden-output infra with opt-in updates via `UPDATE_SNAPSHOTS`. |
| Usage examples | `tests/TextBuffer.Tests/SharedHarnessExampleTests.cs` | 28-sample suite showing builder/helper/snapshot usage (theories + facts). |

### Helper Highlights
- `TestEditorBuilder` supports `.WithMarkedContent("hello| world")`, `.WithLines(...)`, `.WithSelection(...)`, `.WithCRLF()`, producing either plain models or `TestEditorContext` bundles.
- `CursorTestHelper` centralizes cursor assertions and `CursorState` creation while providing pipe-position (| marker) serialization helpers used by TS tests.
- `WordTestUtils` exposes ready-made strings and `[MemberData]` sequences for visible column data, atomic move sequences, and locale word boundaries.
- `SnapshotTestUtils` stores golden files under `Snapshots/{Module}/` and auto-updates when `UPDATE_SNAPSHOTS=1`.

### Snapshot Layout
- `tests/TextBuffer.Tests/Snapshots/README.md` documents update etiquette and `UPDATE_SNAPSHOTS=1` workflows.
- Module-specific folders (`PieceTree/`, `Cursor/`, `Diff/`, `Decorations/`, `DocUI/`, `TextModel/`) keep artifacts isolated to avoid accidental reuse.
- Snapshot helpers generate deterministic paths so future suites (cursor atomic move, diff perf, snippet sessions) can share the same plumbing without copy/paste.

### Example Coverage
- `tests/TextBuffer.Tests/SharedHarnessExampleTests.cs` hosts 28 illustrative facts/theories using every helper plus `[MemberData]` patterns for TS data tables.
- Example tests include builder-only cases (7), cursor helpers (7), word-data generators (5), snapshot assertions (3), theory/member-data demos (2), and integration combos (4) to guide future suites.
- Baseline increased from 496 → 540 passing tests; the example suite alone adds 28 guardrails to keep the helpers stable.

## Testing
| Command | Result |
| --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | `Passed! - Failed: 0, Passed: 540, Skipped: 0` (includes 44 new harness tests). |
| `... --filter SharedHarnessExampleTests` | `28/28 green` |

- Coverage entries captured in [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws5-harness).

## Open Items
- **Risk:** Snapshot infra currently relies on manual JSON/text updates; without CI enforcement it can drift.
- **Next-Owner Instructions:**
    1. Wire `SharedHarnessExampleTests` into CI smoke jobs so helper regressions fail fast.
    2. Fold cursorAtomicMove/wordOperation data (from WS5 backlog) into the new helpers as the next suites are ported.
    3. Extend `SnapshotTestUtils` with optional diff attachments for PR review once we add diff/snapshot suites.
