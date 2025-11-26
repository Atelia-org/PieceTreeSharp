# Sprint 04 – Alignment Remediation (WS1/WS2/WS3 Foundations)
- **Date Range:** 2025-11-27 ~ 2025-12-12
- **Changefeed Anchor:** `agent-team/indexes/README.md#delta-2025-11-26-sprint04`
- **Theme:** 把 `PORT-PT-Search-Plan.md`、`PORT-IntervalTree-Normalize.md` 与 `ALIGN-20251126-Plan.md` 融合为统一冲刺，聚焦 PieceTree 搜索、IntervalTree 延迟 normalize 以及 Range/Cursor/Test backlog 的前两阶段里程碑（M0/M1）。
- **RunSubAgent 节奏:** 继续执行“1 次 runSubAgent = 1 个闭环”的制度；任何角色在回报前必须先更新下方 `Progress Log`，并在更新 Task Board / TestMatrix / Migration Log 时引用本 sprint changefeed。

## Objectives
1. **Workstream 1 – PieceTreeModel.Search parity**：完成 tuple 缓存、CRLF bridge、SearchCache instrumentation，并交付 deterministic/fuzz/CRLF 覆盖（参照 `PORT-PT-Search-Plan.md` Step 1~5）。
2. **Workstream 2 & 4 – Helper/Cursor Blueprint**：由 Investigator 完成 Range/Selection（WS2）与 Cursor/Snippet（WS4）蓝图，Porter/QA 锁定实现与测试边界，为 12 月中旬 M2 交付铺路。
3. **Workstream 3 – IntervalTree Lazy Normalize**：Porter 实现 NodeFlags + delta + TextModel 集成，QA 建立 perf harness 并记录 50k decorations O(log n) 的基线。
4. **Workstream 5 – 高风险 deterministic/feature tests**：产出优先级清单、共用 harness、首批 ≥10 个 deterministic/feature 用例，确保 `tests/TextBuffer.Tests/TestMatrix.md` 记录 coverage delta。

## Deliverables & Tracking
| ID | Deliverable | Owner | Related Docs | Status |
| --- | --- | --- | --- | --- |
| WS1-PORT-SearchCore | `GetAccumulatedValue`/`NodeAt2` tuple 缓存 + SearchCache 失效策略 | Porter-CS | `PORT-PT-Search-Plan.md`, `src/TextBuffer/Core/PieceTreeModel.Search.cs` | Planned |
| WS1-PORT-CRLF | `_lastChangeBufferPos`/CRLF bridge（Append & CreateNewPieces） | Porter-CS | `PORT-PT-Search-Plan.md`, `src/TextBuffer/Core/PieceTreeModel.Edit.cs` | Planned |
| WS1-QA | Deterministic/Fuzz/SearchOffset 扩展 + DEBUG 计数验证 | QA-Automation | `PieceTreeDeterministicTests.cs`, `PieceTreeFuzzHarnessTests.cs`, `PieceTreeSearchOffsetCacheTests.cs` | Planned |
| WS3-PORT | IntervalTree NodeFlags/delta + TextModel `AcceptReplace` 集成 | Porter-CS | `PORT-IntervalTree-Normalize.md`, `src/TextBuffer/Decorations/*.cs`, `TextModel.cs` | Planned |
| WS3-QA | 新 `IntervalTreeTests` + DocUI perf harness | QA-Automation | `tests/TextBuffer.Tests/Decoration*.cs`, `DocUI/DocUIFindDecorationsTests.cs` | Planned |
| WS2-INV | Range/Selection API gap inventory | Investigator-TS | `ALIGN-20251126-Plan.md`, `docs/reports/alignment-audit/02-core-support.md` | In Progress |
| WS4-INV | Cursor/Snippet blueprint & rollout plan | Investigator-TS | `ALIGN-20251126-Plan.md`, `agent-team/handoffs/AA4-003-Audit.md` | In Progress |
| WS5-INV | High-risk deterministic test backlog | Investigator | `ALIGN-20251126-Plan.md`, `docs/reports/alignment-audit/07-core-tests.md` | Planned |
| OPS-Index | 发布 `#delta-2025-11-26-sprint04` 之后的变更 feed | Info-Indexer | `agent-team/indexes/README.md` | Planned |
| OPS-Doc | Sprint/TestMatrix/Migration Log 同步 | DocMaintainer | `docs/sprints/sprint-04.md`, `tests/TextBuffer.Tests/TestMatrix.md`, `docs/reports/migration-log.md` | Planned |

## Progress Log
> 规则：任何 runSubAgent 或关键手动操作完成后，负责成员需追加一行，记录日期、任务、输出与下一步。引用所有相关 changefeed。

| Run # | Date | Employee | Task | Result | Next Steps |
| --- | --- | --- | --- | --- | --- |
| R0 | 2025-11-26 | Main Agent | Sprint 04 建立 | 创建 `docs/sprints/sprint-04.md`、刷新 Task Board（Phase 8）并设定 changefeed anchor `#delta-2025-11-26-sprint04` | 等待 WS2-INV / WS4-INV 报告，触发下一轮 runSubAgent |
