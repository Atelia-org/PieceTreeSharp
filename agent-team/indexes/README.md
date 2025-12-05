# AI Team Indexes

> 由 Info-Indexer 维护的最小摘要，目标是让 AGENTS / Sprint / Task Board 编辑可以快速定位 changefeed、迁移日志与 handoff。详细过程与代码差异请跳转对应 handoff 或 `docs/reports/migration-log.md`。

## Current Indexes
| Name | Description | Last Updated |
| --- | --- | --- |
| [Core Docs Index](core-docs-index.md) | 核心文档的用途、Owner、更新时间与缺口行动列表 | 2025-11-20 |
| [OI Backlog](oi-backlog.md) | 组织性基础设施改进任务（测试框架、工具、架构设计） | 2025-11-22 |
| [Delta Ledger](#delta-ledger-overview) | 所有活跃 changefeed anchor 的时间线索引 | 2025-12-05 |

## Contributing Guidelines
1. 每个索引文件命名为 `<topic>-index.md`。
2. 索引内只保存结论、指针与少量上下文；冗长说明放回手册/hand-off/迁移日志。
3. 当索引吸收了原文档的内容，记得在原文档留下指针或简述，并在此处更新 changefeed。
4. **新格式（2025-12-05 起）**：Changefeed 采用「Batch 指针 + Sprint log 链接」格式，详细内容统一放在 Sprint log。

## Delta Ledger Overview
以下条目按照时间顺序列出所有活跃 anchor。若需要命令、文件或测试列表，请在 `docs/reports/migration-log.md` 中查找对应行，或打开列出的 handoff。

### 2025-11-19 – Foundations
- **#delta-2025-11-19** – PT/TM/DF Phase 0–4 骨架、类型映射及 56 项基础测试，确立 `PieceTreeBuilder→PieceTreeModel→PieceTreeBuffer` 流程与首批 QA 基线。

### 2025-11-20 – AA2/AA3 Remediation
- **#delta-2025-11-20** – AA2-005/006 与 AA3 CL1~CL4 的 CRLF 修复、Undo/EOL、TextModel 搜索、多范围装饰与 Diff parity；测试扩展到 85/85。

### 2025-11-21 – AA4 CL5~CL7
- **#delta-2025-11-21** – Builder/Factory（AA4-005）、ChangeBuffer/CRLF（AA4-006）与 Cursor/Snippet 骨架（AA4-007 + BF1 Fuzz 修复）全部落地，QA rerun 105/105 + fuzz 115/115。

### 2025-11-22 – Batch #1 & OI Backlog
- **#delta-2025-11-22** – ReplacePattern 移植（23 测试）、文档纠错与 OI backlog 初始化；TestMatrix/ts-plan 对齐。

### 2025-11-23 – DocUI & Decorations
- **#delta-2025-11-23** – Batch #2 FindModel、LineCount/Regex/EmptyString tests + FR-01/FR-02 缓存优化（187/187）。
- **DocUI FindController stack** (`#delta-2025-11-23-b3-fm`, `#delta-2025-11-23-b3-fsel`, `#delta-2025-11-23-b3-fc-*`) – SelectAllMatches、FindUtilities、控制器命令、scope/regex seeding/lifecycle（DocUIFindControllerTests 10→27）。
- **Decorations parity** (`#delta-2025-11-23-b3-decor-stickiness*`) – 范围裁剪、owner-aware 查询、stickiness QA。
- **#delta-2025-11-23-b3-piecetree-fuzz** – env-seeded `PieceTreeFuzzHarness` 首版。

### 2025-11-24 – DocUI Scope & PieceTree Reliability
- **Scoped FindModel** (`#delta-2025-11-24-find-scope`, `#delta-2025-11-24-find-replace-scope`, `#delta-2025-11-24-find-primary`, `#delta-2025-11-24-b3-fm-multisel`) – 装饰范围回填、Regex replace scope、primary cursor 语义与多选区顺序全对齐。
- **#delta-2025-11-24-b3-docui-staged** – DocUI staged fixes：`FindDecorations.Reset`、零宽区间、flush-edit 行为以及 `DocUIFindDecorationsTests`/FindModel 追加案例。
- **PieceTree Reliability wave** (`#delta-2025-11-24-b3-piecetree-fuzz`, `#delta-2025-11-24-b3-piecetree-deterministic`, `#delta-2025-11-24-b3-sentinel`, `#delta-2025-11-24-b3-getlinecontent`) – 扩展 fuzz harness、TS deterministic suites、per-model sentinel、`GetLineContent`/`GetLineRawContent` 缓存断言。

### 2025-11-25 – Deterministic Suites & Snapshots
- **CRLF Deterministic** (`#delta-2025-11-25-b3-piecetree-deterministic-crlf`) – 50/50 CRLF + chunk 脚本重现 TS bug battery。
- **Snapshot Stack** (`#delta-2025-11-25-b3-piecetree-snapshot`, `#delta-2025-11-25-b3-textmodel-snapshot`) – `PieceTreeSnapshot.Read()` 单次遍历、`TextModelSnapshot` 包装器、`SnapshotReader` helper 与 parity tests。
- **#delta-2025-11-25-b3-bom** – `PieceTreeBuffer.GetBom()` 断言 UTF-8 BOM 元数据不污染 `GetText()`。
- **#delta-2025-11-25-b3-search-offset** – TS search-offset cache 套件移植；`PieceTreeSearchOffsetCacheTests` + helper assert。
- **#delta-2025-11-25-b3-textmodelsearch** – 45 项 TextModelSearch parity（word boundary、multiline、Unicode anchors）。

### 2025-11-26 – Sprint 04 R1–R11 & Alignment
- **#delta-2025-11-26-alignment-audit** – 8 份 alignment 报告刷新 + 风险标记。
- **#delta-2025-11-26-sprint04-r1-r11** – Phase 8 里程碑（tests 365→585）涵盖 WS1~WS5、CRLF、Cursor 栈与 IntervalTree 重写。
- **WS Baselines** (`#delta-2025-11-26-ws1-searchcore`, `#delta-2025-11-26-ws2-port`, `#delta-2025-11-26-ws3-tree`, `#delta-2025-11-26-ws4-port-core`) – 搜索累计值混合实现、Range/Selection helpers、IntervalTree lazy normalize、CursorConfiguration/State/Context。
- **WS5 Backlog & QA** (`#delta-2025-11-26-ws5-test-backlog`, `#delta-2025-11-26-ws5-qa`) – 高风险测试优先级清单 + 首批 44+1 skip deterministic suites。
- **Gap markers** (`#delta-2025-11-26-aa4-cl7-cursor-core`, `#delta-2025-11-26-aa4-cl7-*`, `#delta-2025-11-26-aa4-cl8-markdown`, `#delta-2025-11-26-aa4-cl8-*`) – CL7 WordOps/Snippet 与 CL8 DocUI Markdown 仍在 follow-up，Task Board/Sprint 需引用这些 placeholder。

### 2025-11-27 – Search Step12 & Build Hygiene
- **#delta-2025-11-27-ws1-port-search-step12** – NodeAt2 tuple 重用、SearchCache 诊断计数器、CRLF fuzz/regression rerun（639/639）。
- **#delta-2025-11-27-build-warnings** – `dotnet build` warning 清零；snapshot helper、IntervalTree tests、PieceTree harness、`.editorconfig` 调整。

### 2025-11-28 – Sprint 04 R13–R18 & WordOps
- **#delta-2025-11-28-sprint04-r13-r18** – CL7 Stage1 完成，CursorCollection + QA、AtomicTabMove 与 Cursor feature flag `EnableVsCursorParity` 上线；测试 639→724。
- **#delta-2025-11-28-ws5-wordoperations** – `WordOperations.cs` 全量重写、`CursorWordOperationsTests`（41 用例）及 3 个 pending skip。
- **#delta-2025-11-28-cl8-phase34** – MarkdownRenderer 接入 FindDecorations、Minimap/GlyphMargin/InjectedText 枚举与 30 个枚举/renderer 覆盖。

### 2025-12-02 – Sprint 04 M2 完成
- **#delta-2025-12-02-sprint04-m2** – Sprint 04 M2 全部完成里程碑。测试基线 873 passed, 9 skipped (+287 since Sprint 03)。Snippet P0-P2 (77 tests)、Cursor/WordOps (94 tests)、IntervalTree AcceptReplace 集成完成。Related: `#delta-2025-12-02-snippet-p2`, `#delta-2025-12-02-ws3-textmodel`。
- **#delta-2025-12-02-snippet-p2** – Snippet 功能 P0-P2 全部完成（77 passed, 4 skipped）：Final Tabstop、adjustWhitespace、Placeholder Grouping、Variable Resolver (SELECTION, TM_FILENAME)。Files: `SnippetSession.cs`, `SnippetController.cs`, `SnippetVariableResolver.cs`。
- **#delta-2025-12-02-ws3-textmodel** – IntervalTree AcceptReplace 集成到 TextModel：`DecorationsTrees.AcceptReplace` 包装方法、TextModel 使用新 API、NormalizeDelta 更新 Decoration.Range。Files: `IntervalTree.cs`, `DecorationsTrees.cs`, `TextModel.cs`。

### 2025-12-04 – Sprint 05 启动 & LLM-Native 筛选
- **#delta-2025-12-04-sprint05-start** – Sprint 05 启动，测试基线突破 1000 达到 1008 passed。
- **#delta-2025-12-04-llm-native-filtering** – LLM-Native 功能筛选完成：7 gaps 无需移植（~14h 节省），8 gaps 降级实现（~18h→~8h），11 gaps 继续移植（~26h）。计划文档：`docs/plans/llm-native-editor-features.md`。
- **#delta-2025-12-04-p1-complete** – P1 任务全部完成：TextModelData.fromString、getValueLengthInRange + EOL variants、validatePosition 边界测试、Issue regressions 调研。测试基线 1085 passed (+77)。

### 2025-12-05 – Snippet Transform & MultiCursor 完整实现
- **#delta-2025-12-05-batch4** — Snippet Transform + MultiCursor 集成 (+39 tests)
  - 详见: [sprint-05.md#2025-12-05](../../docs/sprints/sprint-05.md#2025-12-05---snippet-transform--multicursor-完成)
  - Commit: `9515be1`
  - 子项: `#delta-2025-12-05-snippet-transform`, `#delta-2025-12-05-multicursor-snippet`
- **#delta-2025-12-05-batch5** — P2 完成 + AddSelectionToNextFindMatch (+34 tests)
  - 详见: [sprint-05.md#session-2](../../docs/sprints/sprint-05.md#session-2---addselectiontonextfindmatch-batch-5)
  - Commits: `4101981`, `575cfb2`
  - 子项: `#delta-2025-12-05-add-selection-to-next-find`, `#delta-2025-12-05-p2-complete`
- **#delta-2025-12-05-p2-complete** — **P2 任务全部完成！** 测试基线 1158 passed (+73 本日)，P2 完成率 100% (6/6)。

## Active Placeholders & Follow-Ups
- **CL7 Stage 2** (`#delta-2025-11-26-aa4-cl7-*`) – WordOps/Snippet/CursorCollection 已通过 Sprint 04 M2 完成，参见 `#delta-2025-12-02-sprint04-m2`。
- **CL8 Markdown & Intl** (`#delta-2025-11-26-aa4-cl8-*`) – DocUI MarkdownRenderer 的 Intl/decoration ingestion 延迟中，所有相关 PR 需引用 `#delta-2025-11-28-cl8-phase34`。
- **Intl.Segmenter parity** – 需要 ICU4N 或文档化限制，延迟至后续 Sprint。

## Usage Tips
- 在撰写 AGENTS / Sprint / Task Board 更新前，先在本文件找到对应 anchor，再打开 `docs/reports/migration-log.md` 获取验证命令。
- 若需要文件/测试列表，请使用 anchor → handoff 的映射（命名均在 `agent-team/handoffs/` 下），避免把长说明重新写回索引。
- 任何新变更应遵循「Migration Log → Changefeed → AGENTS/Sprint」顺序，保持三个文档的一致性。
