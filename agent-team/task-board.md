# Task Board - Phase 8: Sprint 04 – Alignment Remediation

**Sprint Window:** 2025-11-27 ~ 2025-12-12  
**Goal:** 把 PieceTree 搜索、装饰树、Range/Cursor/测试 backlog 汇聚为一个冲刺，落实 ALIGN-20251126 工作流的 M0/M1 目标，并为 12 月中旬的 M2 验收准备 QA/DocUI 证据。

**Changefeed Reminder:** 所有状态更新请同步 `agent-team/indexes/README.md#delta-2025-11-26-sprint04`，并在触发 runSubAgent 或完成交付后立刻刷新 `docs/reports/migration-log.md` 与 `tests/TextBuffer.Tests/TestMatrix.md` 的引用。

## Workstream 1 – PieceTreeModel.Search Parity (ALIGN WS1)
| ID | Description | Owner | Key Artifacts / References | runSubAgent Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| WS1-PLAN | Porter 方案基线（`PORT-PT-Search-Plan.md`） | Porter-CS (Leo Park) | `agent-team/handoffs/PORT-PT-Search-Plan.md` | – | ✅ Ready | 2025-11-26 交付，涵盖 tuple cache、CRLF bridge、SearchCache instrumentation。 |
| WS1-PORT-SearchCore | 重写 `GetAccumulatedValue`、`NodeAt2` 缓存与 `PieceTreeSearchCache` tuple reuse | Porter-CS (Leo Park) | `src/TextBuffer/Core/PieceTreeModel.Search.cs`<br>`src/TextBuffer/Core/PieceTreeSearchCache.cs` | 2 | Planned | 对应计划 Step 1 & 4；完成后需在 DocUI/Deterministic 测试前发布 handoff。 |
| WS1-PORT-CRLF | `_lastChangeBufferPos` / `AppendToChangeBufferNode` / `CreateNewPieces` CRLF bridge 实现 | Porter-CS (Leo Park) | `src/TextBuffer/Core/PieceTreeModel.Edit.cs`<br>`tests/TextBuffer.Tests/CRLFFuzzTests.cs` | 2 | Planned | 覆盖计划 Step 2 & 3，需与 cache invalidation 同步 instrumentation。 |
| WS1-QA | 扩展 deterministic/fuzz/SearchOffset 测试并记录 `PIECETREE_DEBUG=0` 命令 | QA-Automation (Sasha Patel) | `tests/TextBuffer.Tests/PieceTreeDeterministicTests.cs`<br>`PieceTreeFuzzHarnessTests.cs`<br>`PieceTreeSearchOffsetCacheTests.cs` | 2 | Planned | 在 Porter 完成实现后运行新脚本、记录搜索 cache 计数，更新 `TestMatrix.md`。 |
| WS1-OPS | Changefeed + 文档同步（Search parity） | Info-Indexer + DocMaintainer | `agent-team/indexes/README.md`<br>`docs/sprints/sprint-04.md`<br>`docs/reports/migration-log.md` | 1 | Planned | 需在 QA 提供证据后发布 delta 并更新 AGENTS/Sprint/Task Board。 |

## Workstream 2 – Range & Selection Helpers (ALIGN WS2)
| ID | Description | Owner | Key Artifacts / References | runSubAgent Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| WS2-INV | Range/Selection API gap inventory (Due 2025-11-28) | Investigator-TS (Harper Lin) | `ts/src/vs/editor/common/core/range.ts`<br>`src/TextBuffer/Core/Range.*`<br>`docs/reports/alignment-audit/02-core-support.md` | 1 | In Progress | 根据 ALIGN 计划输出签名/语义对照与消费者列表。 |
| WS2-PORT | Helper 实现与 `TextPosition` 扩展 | Porter-CS (Diego Torres) | `src/TextBuffer/Core/Range.Extensions.cs`<br>`src/TextBuffer/TextPosition.cs`<br>`src/TextBuffer/Cursor/Cursor.cs` | 2 | Planned | 完成后需要在 Cursor/DocUI 层替换重复逻辑并写 handoff。 |
| WS2-QA | Helper-focused deterministic tests & DocUI/Cursor 适配 | QA-Automation (Erin Blake) | `tests/TextBuffer.Tests/CursorTests.cs`<br>`CursorWordOperationsTests.cs`<br>`DocUI/DocUIFindControllerTests.cs` | 2 | Planned | 目标：覆盖 boundary/zero-length cases，更新 `TestMatrix.md` + Sprint log。 |

## Workstream 3 – IntervalTree Lazy Normalize (ALIGN WS3)
| ID | Description | Owner | Key Artifacts / References | runSubAgent Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| WS3-PLAN | Porter 方案基线（`PORT-IntervalTree-Normalize.md`） | Porter-CS (Felix Novak) | `agent-team/handoffs/PORT-IntervalTree-Normalize.md` | – | ✅ Ready | 方案覆盖 NodeFlags、delta、`AcceptReplace`、TextModel 集成与 perf harness。 |
| WS3-PORT-Tree | IntervalTree Node/Delta/`ResolveState` 重写 | Porter-CS (Felix Novak) | `src/TextBuffer/Decorations/IntervalTree.cs` | 3 | Planned | 对应计划 “Node layout + Lazy deltas + RequestNormalize”。需引入 DEBUG counters。 |
| WS3-PORT-TextModel | DecorationsTrees/TextModel 接入 lazy 范围、`AcceptReplace`、filter toggles | Porter-CS (Felix Novak) | `src/TextBuffer/Decorations/DecorationsTrees.cs`<br>`src/TextBuffer/TextModel.cs` | 2 | Planned | 完成后取代 `AdjustDecorationsForEdit`，准备 DocUI perf harness。 |
| WS3-QA | Perf harness + IntervalTreeTests | QA-Automation (Priya Nair) | `tests/TextBuffer.Tests/DecorationTests.cs`<br>`DecorationStickinessTests.cs`<br>`DocUI/DocUIFindDecorationsTests.cs`<br>`tests/TextBuffer.Tests/IntervalTreeTests.cs` *(new)* | 2 | Planned | 验证 50k decorations O(log n) 行为并收集 normalize telemetry。 |
| WS3-OPS | Changefeed + Audit addendum | Info-Indexer + DocMaintainer | `docs/reports/alignment-audit/04-decorations.md`<br>`docs/reports/migration-log.md` | 1 | Planned | 发布 delta、更新 audit “Verification Notes” & Sprint log。 |

## Workstream 4 – Cursor & Snippet Architecture (ALIGN WS4)
| ID | Description | Owner | Key Artifacts / References | runSubAgent Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| WS4-INV | Cursor/Snippet blueprint (Due 2025-12-02) | Investigator-TS (Callie Stone) | `agent-team/handoffs/AA4-003-Audit.md`<br>`ts/src/vs/editor/common/cursor/*.ts` | 2 | In Progress | 输出 CursorConfig/SingleCursorState/SnippetSession 映射与分阶段交付列表。 |
| WS4-PORT-Core | Cursor pipeline（config/state/collection）落地 | Porter-CS (Viktor Zoric) | `src/TextBuffer/Cursor/*.cs` | 3 | Planned | 需引入 tracked ranges、view/model Δ、word ops parity。 |
| WS4-PORT-Snippet | Snippet controller/session parity + placeholders | Porter-CS (Viktor Zoric) | `src/TextBuffer/Cursor/SnippetController.cs`<br>`SnippetSession.cs` | 2 | Planned | 目标：choice/variable/transform、多光标粘附、undo/redo 集成。 |
| WS4-QA | Deterministic Cursor/Snippet suites + fuzz soak | QA-Automation (Lena Brooks) | `tests/TextBuffer.Tests/CursorTests.cs`<br>`CursorMultiSelectionTests.cs`<br>`SnippetControllerTests.cs`<br>`SnippetMultiCursorFuzzTests.cs` | 3 | Planned | 80% TS coverage，运行列选择/wordPart/placeholder deterministic 套件 < 2 min。 |

## Workstream 5 – High-Risk Deterministic & Feature Tests (ALIGN WS5)
| ID | Description | Owner | Key Artifacts / References | runSubAgent Budget | Status | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| WS5-INV | Test backlog prioritization (Due 2025-11-30) | Investigator (Evan Holt) | `docs/reports/alignment-audit/07-core-tests.md`<br>`08-feature-tests.md` | 1 | Planned | 列出 top-10 deterministic/feature gaps（PieceTree, Cursor, Diff, DocUI）。 |
| WS5-PORT | Harness extensions（shared fixtures + TS oracle ingestion） | Porter (Morgan Lee) | `tests/TextBuffer.Tests/Helpers/*`<br>`tests/TextBuffer.Tests/*.cs` | 2 | Planned | 构建 loader + snapshot utilities 以支撑新 deterministic suites。 |
| WS5-QA | Implement & document high-risk suites | QA-Automation (Priya Nair) | `tests/TextBuffer.Tests/TestMatrix.md`<br>`docs/plans/ts-test-alignment.md` | 2 | Planned | 交付 ≥10 个新 deterministic/feature tests，报告 coverage delta ≥20%。 |

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
