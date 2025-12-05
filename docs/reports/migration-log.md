# Migration Log & Changefeed Checklist

本日志只保留对后续工作仍有价值的状态摘要，用于在更新 `AGENTS.md`、Sprint 文档与 Task Board 之前快速确认对应 changefeed (`agent-team/indexes/README.md`) 是否已登记。若需要具体代码/测试细节，请跳转到 handoff 或相关源文件。

## Logging Procedure
- 记录任务 ID 与日期，并链接到相应 handoff / 源文件 / 测试矩阵。
- 只写对下一步协作仍有意义的事实（结果、验证命令、剩余风险）；详细过程统一放在 handoff。
- 只有在 Info-Indexer (`agent-team/indexes/README.md`) 已添加 delta 时才可声称完成；否则把事项放入「Active Items」并说明缺口。
- 编辑 AGENTS / Sprint / Task Board 前，先找到对应的 changefeed anchor 与本表里的记录再动笔。

## Active Items Awaiting Changefeed
| Task | Scope Snapshot | Current Proof | Next Step |
| --- | --- | --- | --- |
| PT-004.LineInfra | `LineStartTable` / `ChunkBuffer` 元数据 + `PieceTreeSearchCache` 钩子 | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` (7/7) | Info-Indexer 需要新 delta 覆盖行起始缓存与 search cache 诊断，随后才能在 Task Board 标记完成。 |
| PT-004.Positions | `TextPosition` 与 `PieceTreeBuffer` offset/position API | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` (10/10) | 发布 changefeed，引用 `TextPosition.cs`、`PieceTreeBuffer.cs` 和 `UnitTest1.cs` 的位置互换测试。 |
| PT-004.Edit | 插入/删除/RB 旋转 + Buffer 增量编辑管线 | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` (13/13) | 将 `PieceTreeModel.Edit.cs` / `PieceTreeBuffer.cs` 的增量实现登记到 Info-Indexer，供后续审计引用。 |
| PT-005.Search | `PieceTreeSearcher` + `SearchTypes` 基础查找 | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` (16/16) | 用 Info-Indexer delta 绑定 SearchTypes 与 `PieceTreeSearchTests.cs`，否则无法追踪 search 行为变更。 |
| PT-008.Snapshot | `PieceTreeSnapshot` / `ITextSnapshot` / `PieceTreeModel.CreateSnapshot` | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` (18/18) | 需要 changefeed 说明 snapshot 语义与 `PieceTreeSnapshotTests.cs` 证据，供 TextModel Snapshot 依赖。 |
| PT-009.LineOpt | `_lastVisitedLine` 顺序访问缓存 | `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` (20/20) | 发布缓存优化 delta，并在 Info-Indexer 说明 `PieceTreeBaseTests` 缓存失效验证。 |

## Timeline Snapshot (Condensed)
| Date Range | Focus | Outcome Snapshot | Anchors |
| --- | --- | --- | --- |
| 2025-11-19 | Phase 0–4 bootstrap（PieceTree Builder、TextModel、Diff/Decor、首轮审计） | Type mapping、PieceTree façade、基础测试 50→56，全局流程与 handoff 建立。 | `#delta-2025-11-19` |
| 2025-11-20 | AA2/AA3 Remediation | CRLF 修复、Undo/EOL 选项、TextModel 搜索、Diff/Decor parity；测试 56→85。 | `#delta-2025-11-20` |
| 2025-11-21 | AA4 CL5–CL7 + Snippet Hotfix | Builder/Factory、ChangeBuffer/CRLF、Cursor/Snippet skeleton 与 QA 通过；测试 85→115。 | `#delta-2025-11-21` |
| 2025-11-22 | Batch #1 ReplacePattern + 文档修复 + OI Backlog | ReplacePattern 全量移植（142/142）及文档纠错，OI backlog 初始化。 | `#delta-2025-11-22` |
| 2025-11-23 | DocUI Find/Decor & Batch #2 成果 | Batch #2 FindModel parity、FR-01/02、SelectAllMatches、多场景 FindController、FindSelection、Decor stickiness、首版 Piecetree fuzz。 | `#delta-2025-11-23` / `#delta-2025-11-23-b3-fm` / `#delta-2025-11-23-b3-fsel` / `#delta-2025-11-23-b3-fc-core` / `#delta-2025-11-23-b3-fc-scope` / `#delta-2025-11-23-b3-fc-regexseed` / `#delta-2025-11-23-b3-fc-lifecycle` / `#delta-2025-11-23-b3-decor-stickiness` / `#delta-2025-11-23-b3-decor-stickiness-review` / `#delta-2025-11-23-b3-piecetree-fuzz` |
| 2025-11-24 | DocUI Scope/Replace & PieceTree Reliability | Scope hydration、Replace scope、DocUI staged fixes、多选区 parity、Find primary selection、Fuzz harness v2、Deterministic suites、Sentinel、更严格的 GetLineContent 断言。 | `#delta-2025-11-24-find-scope` / `#delta-2025-11-24-find-replace-scope` / `#delta-2025-11-24-find-primary` / `#delta-2025-11-24-b3-docui-staged` / `#delta-2025-11-24-b3-fm-multisel` / `#delta-2025-11-24-b3-piecetree-fuzz` / `#delta-2025-11-24-b3-piecetree-deterministic` / `#delta-2025-11-24-b3-sentinel` / `#delta-2025-11-24-b3-getlinecontent` |
| 2025-11-25 | Deterministic & Snapshot wave | CRLF deterministic（50/50）、PieceTree & TextModel snapshot 管线、BOM + Search offset cache、TextModel Search 45/45。 | `#delta-2025-11-25-b3-piecetree-deterministic-crlf` / `#delta-2025-11-25-b3-piecetree-snapshot` / `#delta-2025-11-25-b3-textmodel-snapshot` / `#delta-2025-11-25-b3-bom` / `#delta-2025-11-25-b3-search-offset` / `#delta-2025-11-25-b3-textmodelsearch` |
| 2025-11-26 | Sprint 04 R1–R11、Alignment Refresh、WS Upgrades | Alignment audit、WS1/2/3 核心移植、Cursor Stage0、WS5 backlog+QA、CRLF 击穿、Sprint04 里程碑、CL7/CL8 gap 标记。 | `#delta-2025-11-26-alignment-audit` / `#delta-2025-11-26-sprint04-r1-r11` / `#delta-2025-11-26-ws1-searchcore` / `#delta-2025-11-26-ws2-port` / `#delta-2025-11-26-ws3-tree` / `#delta-2025-11-26-ws4-port-core` / `#delta-2025-11-26-ws5-test-backlog` / `#delta-2025-11-26-ws5-qa` / `#delta-2025-11-26-aa4-cl7-cursor-core` / `#delta-2025-11-26-aa4-cl7-*` / `#delta-2025-11-26-aa4-cl8-markdown` / `#delta-2025-11-26-aa4-cl8-*` |
| 2025-11-27 | WS1 Search Step12 + Build Hygiene | NodeAt2 tuple & SearchCache 诊断、CRLF fuzz rerun、`dotnet build` warning 清零，测试 639/639。 | `#delta-2025-11-27-ws1-port-search-step12` / `#delta-2025-11-27-build-warnings` |
| 2025-11-28 | Sprint 04 R13–R18、Word Ops、CL8 Renderer | Cursor EnableVsCursorParity Stage1、CursorCollection + QA、AtomicTabMove、WordOperations 重写、CL8 Phase3/4 完成；测试 724→796。 | `#delta-2025-11-28-sprint04-r13-r18` / `#delta-2025-11-28-ws5-wordoperations` / `#delta-2025-11-28-cl8-phase34` |
| 2025-12-02 | Sprint 04 M2 完成 & Sprint 05 启动 | Sprint 04 M2 全部完成（873/9），Snippet P0-P2、Cursor/WordOps、IntervalTree AcceptReplace 集成；Sprint 05 启动，测试突破 1000 达到 1008。 | `#delta-2025-12-02-sprint04-m2` / `#delta-2025-12-02-snippet-p2` / `#delta-2025-12-02-ws3-textmodel` / `#delta-2025-12-04-sprint05-start` |
| 2025-12-04 | LLM-Native 筛选 & P1 清零 | LLM-Native 功能筛选（7 gaps 无需移植，8 gaps 降级，11 gaps 继续）；P1 任务全部完成（TextModelData.fromString、validatePosition、getValueLengthInRange 等）；测试 1008→1085。 | `#delta-2025-12-04-llm-native-filtering` / `#delta-2025-12-04-p1-complete` |
| 2025-12-05 | Snippet Transform & P2 清零 | Snippet Transform + FormatString（+33 tests）、MultiCursor Snippet 集成（+6 tests）、AddSelectionToNextFindMatch 完整实现（+34 tests）；P2 任务全部完成；测试 1085→1158。 | `#delta-2025-12-05-snippet-transform` / `#delta-2025-12-05-multicursor-snippet` / `#delta-2025-12-05-add-selection-to-next-find` / `#delta-2025-12-05-p2-complete` |

## Notes
- `#delta-2025-11-26-aa4-cl7-*` 与 `#delta-2025-11-26-aa4-cl8-*` 是仍在执行的占位符；任何 Cursor Stage2 或 DocUI Markdown 收尾任务都必须先补齐对应 changefeed。
- Step12（`#delta-2025-11-27-ws1-port-search-step12`）与 `#delta-2025-11-27-build-warnings` 已成为最新「绿色基线」证据；回归测试时沿用 `PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`（639/639）。
- 当需要详细测试命令或逐文件 diff，可直接查阅 handoff（例如 `agent-team/handoffs/B3-Decor-Stickiness-Review.md`）或 `tests/TextBuffer.Tests/TestMatrix.md`，本文件仅负责目录式索引。
