# Task Board - Phase 8: Sprint 04 – Alignment Remediation

**Sprint Window:** 2025-11-27 ~ 2025-12-12  
**Goal:** 把 PieceTree 搜索、装饰树、Range/Cursor/测试 backlog 汇聚为一个冲刺，落实 ALIGN-20251126 工作流的 M0/M1 目标，并为 12 月中旬的 M2 验收准备 QA/DocUI 证据。

**Changefeed Reminder:** 所有状态更新请同步 `agent-team/indexes/README.md#delta-2025-11-26-sprint04`；涉及 WS1 Step12（NodeAt2 tuple reuse + SearchCache 诊断）的内容需额外引用 `agent-team/indexes/README.md#delta-2025-11-27-ws1-port-search-step12`，并在触发 runSubAgent 或完成交付后立刻刷新 `docs/reports/migration-log.md` 与 `tests/TextBuffer.Tests/TestMatrix.md` 的引用。

**CL7/CL8 Gap Reminder:** `WS4`/DocUI 相关编辑前，先复核 [`docs/reports/migration-log.md#aa4-cl7-gap`](../docs/reports/migration-log.md#aa4-cl7-gap) / [`docs/reports/migration-log.md#aa4-cl8-gap`](../docs/reports/migration-log.md#aa4-cl8-gap) 与 [`#delta-2025-11-26-aa4-cl7-cursor-core`](indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core) / [`#delta-2025-11-26-aa4-cl8-markdown`](indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)。

## Workstream 1 – PieceTreeModel.Search Parity (ALIGN WS1)
| ID | Description | Owner | Key Artifacts / References | runSubAgent Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| WS1-PLAN | Porter 方案基线（`PORT-PT-Search-Plan.md`） | Porter-CS (Leo Park) | `agent-team/handoffs/PORT-PT-Search-Plan.md` | – | ✅ Ready | 2025-11-26 交付，涵盖 tuple cache、CRLF bridge、SearchCache instrumentation。 |
| WS1-PORT-SearchCore | 重写 `GetAccumulatedValue`、`NodeAt2` 缓存与 `PieceTreeSearchCache` tuple reuse | Porter-CS (Leo Park) | `src/TextBuffer/Core/PieceTreeModel.Search.cs`<br>`src/TextBuffer/Core/PieceTreeSearchCache.cs`<br>[`migration-log`](../docs/reports/migration-log.md#ws1-port-searchcore)<br>[`changefeed`](indexes/README.md#delta-2025-11-26-ws1-searchcore) | 2 | ✅ Done | 2025-11-26 完成：混合实现 + DEBUG 计数器（CacheHit/CacheMiss/ClearedAfterEdit）。 |
| WS1-PORT-CRLF | `_lastChangeBufferPos` / `AppendToChangeBufferNode` / `CreateNewPieces` CRLF bridge 实现 | Porter-CS (Leo Park) | `src/TextBuffer/Core/PieceTreeModel.Edit.cs`<br>`tests/TextBuffer.Tests/CRLFFuzzTests.cs`<br>[`migration-log`](../docs/reports/migration-log.md#ws1-port-crlf)<br>[`changefeed`](indexes/README.md#delta-2025-11-26-sprint04-r1-r11) | 2 | ✅ Done | 2025-11-26 完成：hitCRLF 检测 + `_` 占位符技术 + 11 新测试。 |
| WS1-QA | 扩展 deterministic/fuzz/SearchOffset 测试并记录 `PIECETREE_DEBUG=0` 命令 | QA-Automation (Sasha Patel) | `tests/TextBuffer.Tests/PieceTreeDeterministicTests.cs`<br>`PieceTreeFuzzHarnessTests.cs`<br>`PieceTreeSearchOffsetCacheTests.cs`<br>[`migration-log`](../docs/reports/migration-log.md#ws123-qa)<br>[`changefeed`](indexes/README.md#delta-2025-11-26-sprint04-r1-r11) | 2 | ✅ Done | 2025-11-26 完成：440/440 全量 + targeted reruns 验证，TestMatrix 更新。 |
| WS1-PORT-Step12 | NodeAt2 tuple reuse + SearchCache diagnostics（PORT-PT-Search Step12） | Porter-CS (Leo Park) | `src/TextBuffer/Core/PieceTreeModel.Search.cs`<br>`src/TextBuffer/Core/PieceTreeSearchCache.cs`<br>`agent-team/handoffs/PORT-PT-Search-Step12-INV.md`<br>`agent-team/handoffs/PORT-PT-Search-Step12-QA.md`<br>[`changefeed`](indexes/README.md#delta-2025-11-27-ws1-port-search-step12) | 2 | ✅ Done | 2025-11-27 完成：NodeAt2 tuple 重用、SearchCache DiagnosticsView 暴露、QA rerun deterministic/fuzz/CRLF/search suites + 全量 639/639（2 skip）。 |
| WS1-OPS | Changefeed + 文档同步（Search parity） | Info-Indexer + DocMaintainer | `agent-team/indexes/README.md`<br>`docs/sprints/sprint-04.md`<br>`docs/reports/migration-log.md` | 1 | ✅ Done | 2025-11-27 发布 `#delta-2025-11-27-ws1-port-search-step12` 并同步 AGENTS/Sprint/TestMatrix/Task Board。 |

## Workstream 2 – Range & Selection Helpers (ALIGN WS2)
| ID | Description | Owner | Key Artifacts / References | runSubAgent Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| WS2-INV | Range/Selection API gap inventory (Due 2025-11-28) | Investigator-TS (Harper Lin) | `ts/src/vs/editor/common/core/range.ts`<br>`src/TextBuffer/Core/Range.*`<br>`docs/reports/alignment-audit/02-core-support.md` | 1 | In Progress | 根据 ALIGN 计划输出签名/语义对照与消费者列表。 |
| WS2-PORT | Helper 实现与 `TextPosition` 扩展 | Porter-CS (Diego Torres) | `src/TextBuffer/Core/Range.Extensions.cs`<br>`src/TextBuffer/TextPosition.cs`<br>`src/TextBuffer/Cursor/Cursor.cs`<br>[`migration-log`](../docs/reports/migration-log.md#ws2-port)<br>[`changefeed`](indexes/README.md#delta-2025-11-26-ws2-port) | 2 | ✅ Done | 2025-11-26 完成：75 个 Range/Selection/TextPosition helpers，440/440 通过。 |
| WS2-QA | Helper-focused deterministic tests & DocUI/Cursor 适配 | QA-Automation (Erin Blake) | `tests/TextBuffer.Tests/CursorTests.cs`<br>`CursorWordOperationsTests.cs`<br>`DocUI/DocUIFindControllerTests.cs` | 2 | Planned | 目标：覆盖 boundary/zero-length cases，更新 `TestMatrix.md` + Sprint log。 |

## Workstream 3 – IntervalTree Lazy Normalize (ALIGN WS3)
| ID | Description | Owner | Key Artifacts / References | runSubAgent Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| WS3-PLAN | Porter 方案基线（`PORT-IntervalTree-Normalize.md`） | Porter-CS (Felix Novak) | `agent-team/handoffs/PORT-IntervalTree-Normalize.md` | – | ✅ Ready | 方案覆盖 NodeFlags、delta、`AcceptReplace`、TextModel 集成与 perf harness。 |
| WS3-PORT-Tree | IntervalTree Node/Delta/`ResolveState` 重写 | Porter-CS (Felix Novak) | `src/TextBuffer/Decorations/IntervalTree.cs`<br>[`migration-log`](../docs/reports/migration-log.md#ws3-port-tree)<br>[`changefeed`](indexes/README.md#delta-2025-11-26-ws3-tree) | 3 | ✅ Done | 2025-11-26 完成：NodeFlags/delta/ResolveState/AcceptReplace 全部实现（~1470 行），DEBUG counters 已加入。 |
| WS3-PORT-TextModel | DecorationsTrees/TextModel 接入 lazy 范围、`AcceptReplace`、filter toggles | Porter-CS (Felix Novak) | `src/TextBuffer/Decorations/DecorationsTrees.cs`<br>`src/TextBuffer/TextModel.cs` | 2 | Planned | 完成后取代 `AdjustDecorationsForEdit`，准备 DocUI perf harness。 |
| WS3-QA | Perf harness + IntervalTreeTests | QA-Automation (Priya Nair) | `tests/TextBuffer.Tests/DecorationTests.cs`<br>`DecorationStickinessTests.cs`<br>`DocUI/DocUIFindDecorationsTests.cs`<br>`tests/TextBuffer.Tests/IntervalTreeTests.cs` *(new)*<br>[`migration-log`](../docs/reports/migration-log.md#sprint04-r1-r11)<br>[`changefeed`](indexes/README.md#delta-2025-11-26-sprint04-r1-r11) | 2 | ✅ Done | 2025-11-26 完成：IntervalTreeTests 13/13 + IntervalTreePerfTests 7/7，DEBUG counters 可访问。 |
| WS3-OPS | Changefeed + Audit addendum | Info-Indexer + DocMaintainer | `docs/reports/alignment-audit/04-decorations.md`<br>`docs/reports/migration-log.md` | 1 | Planned | 发布 delta、更新 audit “Verification Notes” & Sprint log。 |

## Workstream 4 – Cursor & Snippet Architecture (ALIGN WS4)
| ID | Description | Owner | Key Artifacts / References | runSubAgent Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| WS4-INV | Cursor/Snippet blueprint (Due 2025-12-02) | Investigator-TS (Callie Stone) | `agent-team/handoffs/AA4-003-Audit.md`<br>`ts/src/vs/editor/common/cursor/*.ts` | 2 | In Progress | 输出 CursorConfig/SingleCursorState/SnippetSession 映射与分阶段交付列表。Gap refs: [`migration-log`](../docs/reports/migration-log.md#aa4-cl7-gap) / [`#delta-2025-11-26-aa4-cl7-cursor-core`](indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)。 |
| WS4-PORT-Core | Stage 0 Cursor 基础架构 (config/state/context) | Porter-CS (Viktor Zoric) | `src/TextBuffer/Cursor/CursorConfiguration.cs`<br>`CursorState.cs`<br>`CursorContext.cs`<br>`tests/TextBuffer.Tests/CursorCoreTests.cs`<br>[`migration-log`](../docs/reports/migration-log.md#ws4-port-core)<br>[`changefeed`](indexes/README.md#delta-2025-11-26-ws4-port-core) | 3 | ✅ Done | 2025-11-26 完成：CursorConfiguration、SingleCursorState/CursorState、ICoordinatesConverter、TextModel tracked ranges、25 unit tests。Stage 1~4 后续 WS4-PORT-Full。 |
| WS4-PORT-Snippet | Snippet controller/session parity + placeholders | Porter-CS (Viktor Zoric) | `src/TextBuffer/Cursor/SnippetController.cs`<br>`SnippetSession.cs` | 2 | Planned | 目标：choice/variable/transform、多光标粘附、undo/redo 集成。 |
| WS4-QA | Deterministic Cursor/Snippet suites + fuzz soak | QA-Automation (Lena Brooks) | `tests/TextBuffer.Tests/CursorTests.cs`<br>`CursorMultiSelectionTests.cs`<br>`SnippetControllerTests.cs`<br>`SnippetMultiCursorFuzzTests.cs` | 3 | Planned | 80% TS coverage，运行列选择/wordPart/placeholder deterministic 套件 < 2 min。 |

## Workstream 5 – High-Risk Deterministic & Feature Tests (ALIGN WS5)
| ID | Description | Owner | Key Artifacts / References | runSubAgent Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| WS5-INV | Test backlog prioritization (Due 2025-11-30) | Investigator (Evan Holt) | `docs/reports/alignment-audit/07-core-tests.md`<br>`08-feature-tests.md`<br>`agent-team/handoffs/WS5-INV-TestBacklog.md`<br>[`migration-log`](../docs/reports/migration-log.md#ws5-inv)<br>[`changefeed`](indexes/README.md#delta-2025-11-26-ws5-test-backlog) | 1 | ✅ Done | 2025-11-26 完成：Top-10 优先级列表、按模块分组的完整 backlog（47 gaps, ~106h）、共享 harness 需求与 TS oracle ingestion 策略。 |
| WS5-PORT | Harness extensions（shared fixtures + TS oracle ingestion） | Porter (Morgan Lee) | `tests/TextBuffer.Tests/Helpers/*`<br>`tests/TextBuffer.Tests/*.cs`<br>[`migration-log`](../docs/reports/migration-log.md#sprint04-r1-r11)<br>[`changefeed`](indexes/README.md#delta-2025-11-26-sprint04-r1-r11) | 2 | ✅ Done | 2025-11-26 完成：TestEditorBuilder/CursorTestHelper/WordTestUtils/SnapshotTestUtils + 44 新测试。 |
| WS5-QA | Implement & document high-risk suites | QA-Automation (Priya Nair) | `tests/TextBuffer.Tests/TestMatrix.md`<br>`docs/plans/ts-test-alignment.md`<br>`tests/TextBuffer.Tests/PieceTreeBufferApiTests.cs`<br>`tests/TextBuffer.Tests/PieceTreeSearchRegressionTests.cs`<br>`tests/TextBuffer.Tests/TextModelIndentationTests.cs`<br>[`migration-log`](../docs/reports/migration-log.md#ws5-qa)<br>[`changefeed`](indexes/README.md#delta-2025-11-26-ws5-qa) | 2 | ✅ Done | 2025-11-26 完成：45 tests (44 pass + 1 skipped) 涵盖 PieceTree buffer API (#6: 17 tests)、search regressions (#7: 9 tests)、TextModel indentation (#8: 19 tests + 1 skipped)。Evidence: `agent-team/handoffs/WS5-QA-Result.md`。 |
| WS5-WordOps | Top-10 #2: wordOperations test suite & implementation | Porter-CS | `src/TextBuffer/Cursor/WordOperations.cs`<br>`src/TextBuffer/Cursor/WordCharacterClassifier.cs`<br>`tests/TextBuffer.Tests/CursorWordOperationsTests.cs`<br>`tests/TextBuffer.Tests/Helpers/WordTestUtils.cs`<br>`agent-team/handoffs/WS5-WordOperations-Result.md`<br>[`migration-log`](../docs/reports/migration-log.md#ws5-wordoperations)<br>[`changefeed`](indexes/README.md#delta-2025-11-28-ws5-wordoperations) | 2 | ✅ Done | 2025-11-28 完成：41 tests (38 pass + 3 skipped edge cases)，WordOperations.cs ~960 lines，完整的 MoveWordLeft/Right、DeleteWordLeft/Right、DeleteInsideWord、SelectWord 实现。 |

## Cross-Stream Ops & Tracking
| ID | Description | Owner | Key Artifacts | Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| OPS-SprintLog | 维护 `docs/sprints/sprint-04.md` Progress Log（每次 runSubAgent 前后更新） | DocMaintainer | `docs/sprints/sprint-04.md` | – | Planned | 本任务随 Sprint 生命周期持续存在。 |
| OPS-TestMatrix | 确保新测试套件的命令/统计记入 `tests/TextBuffer.Tests/TestMatrix.md` | QA-Automation + DocMaintainer | `tests/TextBuffer.Tests/TestMatrix.md` | – | Planned | 与 changefeed/迁移日志保持一致，避免 alias 0/0。 |
| OPS-Index | Info-Indexer changefeed & archive 管理 | Info-Indexer | `agent-team/indexes/README.md` | 1 | Planned | 每个 Workstream 交付后追加 delta，老板迁移到 archive。 |

## References
- `agent-team/handoffs/PORT-PT-Search-Plan.md`
- `agent-team/handoffs/PORT-IntervalTree-Normalize.md`
- `agent-team/handoffs/ALIGN-20251126-Plan.md`
- `docs/reports/alignment-audit/*.md`
- `docs/reports/migration-log.md`
- `tests/TextBuffer.Tests/TestMatrix.md`
