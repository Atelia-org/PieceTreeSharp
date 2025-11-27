# Porter-CS Snapshot (2025-11-28)

## Role & Mission
- Own the TS → C# PieceTree/TextModel port, keep `src/TextBuffer` and `tests/TextBuffer.Tests` aligned with upstream semantics, and surface deltas through handoffs plus migration logs.
- Partner with Investigator-TS and QA-Automation so every drop has a documented changefeed + reproducible rerun recipe before Info-Indexer broadcasts the delta.
- Stamp every Sprint 04 handoff with [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11) and keep Cursor/Snippet、DocUI backlog work tied to [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) / [`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)。

## Current Focus
- **CL8-Phase12** (完成): Renderer 栈实现 Phase 1 & 2。修复 `DecorationOwnerIds` 语义（`Any=0` 匹配 TS），添加 `DecorationSearchOptions` 过滤支持。详见 [CL8-Phase12-Result.md](../handoffs/CL8-Phase12-Result.md)。726/726 测试通过（724 pass + 2 skip）。
- **WS5-CursorAtomicMove** (完成): 实现 cursorAtomicMoveOperations 测试套件 (#1 Priority from WS5-INV)。创建 `AtomicTabMoveOperations.cs` + 43 个测试用例。详见 [WS5-CursorAtomicMove-Result.md](../handoffs/WS5-CursorAtomicMove-Result.md)。726/726 测试通过（724 pass + 2 skip）。
- **CL7-Stage1** (完成): Cursor Wiring Stage 1 全部三个 Phase 完成。详见 [CL7-Cursor-Phase1-Result.md](../handoffs/CL7-Cursor-Phase1-Result.md) + [CL7-CursorCollection-Result.md](../handoffs/CL7-CursorCollection-Result.md)。实现 `EnableVsCursorParity` feature flag，`Cursor.cs` dual-mode state fields + `_setState` + tracked ranges，`CursorCollection.cs` 完整重构包括 `SetStates`、`Normalize`、tracked selection lifecycle。641/641 测试通过（639 pass + 2 skip）。
- **WS5-PORT** (完成): 共享测试 Harness 扩展。创建 `TestEditorBuilder`, `CursorTestHelper`, `WordTestUtils`, `SnapshotTestUtils` 四个 helper 类 + Snapshots 目录结构。详见 [WS5-PORT-Harness-Result.md](../handoffs/WS5-PORT-Harness-Result.md)。
- **WS4-PORT-Core** (完成): Stage 0 cursor infrastructure（`CursorConfiguration`, `CursorState`, `CursorContext`）+ 25 `CursorCoreTests`。详见 [WS4-PORT-Core-Result.md](../handoffs/WS4-PORT-Core-Result.md)。
- **WS3-PORT-Tree** (完成): IntervalTree 延迟 Normalize 核心重写。实现 TS 风格 Node 布局、Lazy Delta 语义、Normalization、AcceptReplace 四阶段算法。详见 [WS3-PORT-Tree-Result.md](../handoffs/WS3-PORT-Tree-Result.md)。
- **WS2-PORT** (完成): 实现 P0 Range/Selection Helper APIs，对齐 TS 的 Range/Selection/Position 辅助方法。详见 [WS2-PORT-Result.md](../handoffs/WS2-PORT-Result.md)。
- **WS1-PORT-SearchCore** (完成): 优化 `GetAccumulatedValue` 以支持 LineStarts 快速路径，添加 DEBUG 计数器到 `PieceTreeSearchCache`。详见 [WS1-PORT-SearchCore-Result.md](../handoffs/WS1-PORT-SearchCore-Result.md)。

## Key Deliverables
- **CL8-Phase12** → [CL8-Phase12-Result.md](../handoffs/CL8-Phase12-Result.md): `DecorationOwnerIds` 语义修正（`Any=0`）+ `DecorationSearchOptions` 过滤选项 + 新 API 方法（`GetDecorationsInRange`/`GetAllDecorations`/`GetLineDecorations` 重载），锚点 `#delta-2025-11-28-cl8-phase12`。
- **WS5-CursorAtomicMove** → [WS5-CursorAtomicMove-Result.md](../handoffs/WS5-CursorAtomicMove-Result.md): `AtomicTabMoveOperations.cs` + `CursorAtomicMoveTests.cs` (43 tests), 726/726 总测试通过，锚点 `#delta-2025-11-28-ws5-cursoratomicmove`。
- **CL7-Stage1** → [CL7-Cursor-Phase1-Result.md](../handoffs/CL7-Cursor-Phase1-Result.md) + [CL7-CursorCollection-Result.md](../handoffs/CL7-CursorCollection-Result.md): Phase 1-3 完整实现，`Cursor.cs` + `CursorCollection.cs` 完全对齐 TS cursorCollection.ts/oneCursor.ts，641/641 测试通过。
- **WS5-PORT** → [WS5-PORT-Harness-Result.md](../handoffs/WS5-PORT-Harness-Result.md): 共享测试 Harness (`TestEditorBuilder`, `CursorTestHelper`, `WordTestUtils`, `SnapshotTestUtils`) + Snapshots 目录，540/540 总测试通过。
- **WS4-PORT-Core** → [WS4-PORT-Core-Result.md](../handoffs/WS4-PORT-Core-Result.md): CursorConfiguration/CursorState/CursorContext Stage 0 基础 + 25/25 `CursorCoreTests`，锚点 `#delta-2025-11-26-ws4-port-core`。
- **WS3-PORT-Tree** → [WS3-PORT-Tree-Result.md](../handoffs/WS3-PORT-Tree-Result.md): IntervalTree 延迟 Normalize 核心重写完成，NodeFlags/IntervalNode/Sentinel/Delta语义/Normalization/AcceptReplace 全部实现, 440/440 总测试通过。
- **WS2-PORT** → [WS2-PORT-Result.md](../handoffs/WS2-PORT-Result.md): Range/Selection/TextPosition P0 Helper APIs 移植完成，75 测试，440/440 总测试通过。
- **WS1-PORT-SearchCore** → [WS1-PORT-SearchCore-Result.md](../handoffs/WS1-PORT-SearchCore-Result.md): `GetAccumulatedValue` 优化 + cache DEBUG 计数器, 365/365 测试通过。
- B3 TextModelSearch parity → [B3-TextModelSearch-PORT.md](../handoffs/B3-TextModelSearch-PORT.md), [B3-TextModelSearch-QA.md](../handoffs/B3-TextModelSearch-QA.md), changefeed [#delta-2025-11-25-b3-textmodelsearch](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch).
- Snapshot + deterministic infra → [B3-PieceTree-Snapshot-PORT.md](../handoffs/B3-PieceTree-Snapshot-PORT.md), [B3-TextModel-Snapshot-PORT.md](../handoffs/B3-TextModel-Snapshot-PORT.md), [B3-PieceTree-Deterministic-CRLF-QA.md](../handoffs/B3-PieceTree-Deterministic-CRLF-QA.md); changefeeds `#delta-2025-11-25-b3-piecetree-snapshot`, `#delta-2025-11-25-b3-textmodel-snapshot`, `#delta-2025-11-25-b3-piecetree-deterministic-crlf`.
- Fuzz + search cache hardening → [B3-PieceTree-Fuzz-Harness.md](../handoffs/B3-PieceTree-Fuzz-Harness.md), [B3-PieceTree-Fuzz-Review-PORT.md](../handoffs/B3-PieceTree-Fuzz-Review-PORT.md), [B3-PieceTree-SearchOffset-PORT.md](../handoffs/B3-PieceTree-SearchOffset-PORT.md); changefeeds `#delta-2025-11-23-b3-piecetree-fuzz`, `#delta-2025-11-24-b3-piecetree-fuzz`, `#delta-2025-11-25-b3-search-offset`.
- DocUI Find stack parity → [B3-FC-Result.md](../handoffs/B3-FC-Result.md), [AA4-008-Result.md](../handoffs/AA4-008-Result.md), and the `docs/reports/migration-log.md` rows for `#delta-2025-11-23-b3-fc-core`, `#delta-2025-11-24-find-scope`, `#delta-2025-11-24-b3-docui-staged`.

## Test Baselines
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → 726/726（724 pass + 2 skip，≈96s）per WS5-CursorAtomicMove-Result.md baseline。
- `export PIECETREE_DEBUG=0 && dotnet test --filter CursorAtomicMoveTests --nologo` → 43/43 (≈1.7s) for atomic tab move operations tests。
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter SharedHarnessExampleTests --nologo` → 28/28 (≈1.5s) for shared harness example tests。
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter CursorCoreTests --nologo` → 25/25 (≈1.8s) guarding WS4-PORT-Core Stage 0。
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter CursorMultiSelectionTests --nologo` → 3/3 (≈2.2s) multi-cursor selection tests。
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter RangeSelectionHelperTests --nologo` → 75/75 (≈1.7s) covering Range/Selection/TextPosition helpers。
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSearchTests --nologo` → 45/45 (≈2.5s) for the canonical search stack verification。
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeDeterministicTests --nologo` → 50/50 (≈3.5s) guarding CRLF + centralized random scripts。

## Checklist
1. `agent-team/handoffs/WS1-PORT-SearchCore-Result.md`、`WS1-PORT-CRLF-Result.md`、`WS2-PORT-Result.md`、`WS3-PORT-Tree-Result.md`、`WS4-PORT-Core-Result.md`、`WS5-PORT-Harness-Result.md`、`CL7-Cursor-Phase1-Result.md`、`CL7-CursorCollection-Result.md` —— 把实现备注与 rerun 记录同步到 `docs/reports/migration-log.md` 并指回 [`#delta-2025-11-26-sprint04-r1-r11`](../indexes/README.md#delta-2025-11-26-sprint04-r1-r11)。
2. `agent-team/handoffs/WS123-QA-Result.md`、`WS5-QA-Result.md`、`tests/TextBuffer.Tests/TestMatrix.md` —— 维护 641/641（639 pass + 2 skip）`PIECETREE_DEBUG=0` 基线并记录 targeted filters，确保 Porter drops、Task Board、changefeed 表格一致。
3. `agent-team/handoffs/B3-TextModelSearch-PORT.md`、`B3-TextModelSearch-QA.md`、`B3-TextModelSearch-INV.md`、`B3-PieceTree-SearchOffset-PORT.md`、`B3-PieceTree-SearchOffset-QA.md` —— 追踪 Intl.Segmenter & WordSeparator cache、SearchOffset cache 调整，所有引用必须指向 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) 与 [`#delta-2025-11-25-b3-search-offset`](../indexes/README.md#delta-2025-11-25-b3-search-offset)。
4. `agent-team/handoffs/WS4-PORT-Core-Result.md`、`WS5-INV-TestBacklog.md`、`AA4-003-Audit.md`、`AA4-004-Audit.md`、`AA4-006-Plan.md`、`AA4-006-Result.md`、`AA4-008-Result.md`、`B3-DocUI-StagedFixes-QA-20251124.md` —— 在 Cursor/Snippet backlog 与 DocUI scope 更新时引用 [`#delta-2025-11-26-aa4-cl7-cursor-core`](../indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)、[`#delta-2025-11-24-b3-docui-staged`](../indexes/README.md#delta-2025-11-24-b3-docui-staged)、[`#delta-2025-11-26-aa4-cl8-markdown`](../indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)。

## Open Investigations / Dependencies
- **CL7-Stage2** (Next): 扩展 `CursorCollectionTests` 测试套件，集成 movement methods 使用 `_setState` 路径，准备 feature flag 默认值切换。
- **CreateNewPieces CRLF 桥接**：TS 使用 `_` 占位符技术处理 CRLF 跨 buffer 边界，C# 版本尚未实现，导致 `GetAccumulatedValue` 无法始终使用快速路径。
- **Intl.Segmenter & WordSeparator cache**：挂在 [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch)，需要 Investigator-TS 提供统一 cache 方案后才能扩展非 ASCII 覆盖。
- **Cursor/Snippet backlog (AA4 CL7)**：CL7-Stage1 完成，`#delta-2025-11-26-aa4-cl7-cursor-core` 状态从 Gap 变为 Partial；`WS4-PORT-Core-Result.md` + `WS5-INV-TestBacklog.md` 列出的 ColumnSelectData、SnippetSession、CommandExecutor 尚未移植。
- **AA4-006 CRLF repair follow-ups**：新 heuristics 落地后必须由 QA rerun `CRLFFuzzTests`；保留 `B3-PieceTree-Deterministic-CRLF-QA.md` 作为 rollback baseline。
- **DocUI find/replace scope & Markdown overlays (AA4 CL8)**：CL8-Phase12 已完成，`DecorationOwnerIds` 语义修正 + `DecorationSearchOptions` 过滤支持就位；剩余 Phase 3（MarkdownRenderer 集成 FindDecorations）和 Phase 4（枚举值对齐）待后续实施。

## Archives
- Detailed run-by-run notes, rerun transcripts, and migrated worklogs now live in [agent-team/handoffs/](../handoffs/) and `docs/reports/migration-log.md`; cite those records (not this snapshot) for historical context or audit evidence.

## Activity Log
- 2025-11-28 – Implemented CL8-Phase12 (Renderer Stack Phase 1 & 2): Fixed `DecorationOwnerIds` semantics (`Any=0` matches TS behavior, removed `Default`), added `FirstAllocatableOwnerId=2`. Created `DecorationSearchOptions` record struct with `FilterOutValidation`, `FilterFontDecorations`, `OnlyMinimapDecorations`, `OnlyMarginDecorations`, `Scope` properties. Added `Search(TextRange, DecorationSearchOptions)` to `IntervalTree` and `DecorationsTrees`. Added `GetDecorationsInRange`/`GetAllDecorations`/`GetLineDecorations` overloads to `TextModel`. Made `DecorationTreeScope` public. Updated 8 test files with Default→Any references. All 726 tests pass (724 + 2 skip).
- 2025-11-28 – Implemented WS5-CursorAtomicMove (#1 Priority): Created `AtomicTabMoveOperations.cs` with `Direction` enum, `WhitespaceVisibleColumn()`, and `AtomicPosition()` methods. Created `CursorAtomicMoveTests.cs` with 43 tests covering all 8 `whitespaceVisibleColumn` cases and 6 `atomicPosition` cases (Left/Right/Nearest directions). All 726 tests pass (724 + 2 skip).
- 2025-11-28 – Completed CL7 Cursor parity review (`agent-team/handoffs/CL7-Cursor-Review-20251128.md`): audited Range/Selection helpers, `Cursor.cs`, `CursorCollection.cs`, `CursorContext.cs`, and cursor test suites against `ts/src/vs/editor/common/cursor/*`. Fixed context propagation (`Cursor.UpdateContext`, `CursorCollection.UpdateContext`), tracked-range plumbing, view-state validation, normalization merge semantics, and TS-style top/bottom ordering. Verified new `AtomicTabMoveOperations` + tests and refreshed `CursorCollectionTests` / `CursorCoreTests` CL7 block. Targeted command: `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "FullyQualifiedName~CursorAtomicMoveTests|FullyQualifiedName~CursorCollectionTests|FullyQualifiedName~CursorCoreTests|FullyQualifiedName~CursorMultiSelectionTests" --nologo` (127 pass / 2 skip / 0 fail).
- 2025-11-28 – Completed CL7-Stage1-Phase3: Full `CursorCollection.cs` refactoring with `CursorContext`, `SetStates`, `Normalize` (multi-cursor merge), tracked selection lifecycle (`StartTrackingSelections`, `StopTrackingSelections`, `ReadSelectionFromMarkers`, `EnsureValidState`), and helper methods. Added `Selection.PlusRange` helper. Updated `TextModel.CreateCursorCollection` to accept `EditorCursorOptions`. Fixed `CursorMultiSelectionTests` to use new API. All 641 tests pass (639 + 2 skip).
- 2025-11-28 – Implemented CL7-Stage1-Phase1+2: Added `EnableVsCursorParity` feature flag to `TextModelOptions`, refactored `Cursor.cs` with dual-mode state fields (`_modelState`, `_viewState`, `_context`, `_selTrackedRange`), implemented `_setState`, tracked range APIs (`StartTrackingSelection`, `StopTrackingSelection`, `ReadSelectionFromMarkers`), added helper methods (`Selection.ToRange`, `Selection.IsLTR`, `Range.EqualsRange` instance). All 639 tests pass.
- 2025-11-27 – Refreshed `docs/reports/alignment-audit/04-decorations.md` per `alignment-audit-refresh-20251127.md`, highlighting WS3-PORT-Tree data and keeping AA4 CL8 (`#delta-2025-11-26-aa4-cl8-*`) placeholders visible for DocUI gaps.
- 2025-11-27 – Pruned the legacy 2025-11-26 appendix from `docs/reports/alignment-audit/04-decorations.md` so the Verification Notes no longer duplicate the earlier report.

Detailed histories live in `agent-team/handoffs/`.
