# Sprint 00
- **Date Range:** 2025-11-19 ~ 2025-11-26
- **Theme / Goal:** 建立 PieceTree 移植的基础设施（理解、架构、流程）
- **Success Criteria:**
  - `agent-team/type-mapping.md` 覆盖 Piece / PieceTreeNode / SearchContext / BufferRange，对 C# 结构与 API 的约束有交叉引用。
  - `src/TextBuffer/Core` 包含可编译的 RB Tree skeleton（PieceTreeNode、PieceTreeModel、balancing helpers）并通过 smoke `dotnet test`。
  - `docs/reports/consistency/` 与 `AGENTS.md` 记录迁移日志、Info-Indexer changefeed 以及 QA 基线，支持 PT-005/006 的复用流程。

**Status Edits Reminder:** 在调整 Sprint 00 状态前，先查阅 `docs/reports/migration-log.md` 以及 `agent-team/indexes/README.md#delta-2025-11-19`，并在更新条目时附上这两处引用以保持与 changefeed 同步。

## Backlog Snapshot
| Priority | Task | Description & Deliverables | runSubAgent Budget | Owner | Target Date | Dependencies | Status / Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| P1 | PT-003 | 扩展 TS↔C# 类型映射（Piece、PieceTreeNode、SearchContext、BufferRange），输出更新后的 `agent-team/type-mapping.md` 及依赖说明，供 Info-Indexer 入索引。 | 1 | Investigator-TS | 2025-11-20 | Planner 提供的 context 包 + Info-Indexer 审阅 | ✅ 2025-11-19：`agent-team/type-mapping.md` 更新完毕，新增 invariants/QA/Porter TODO + Diff Summary，已在 Task Board / Info-Indexer delta登记。 |
| P1 | PT-004 | 将 PieceTree RB Tree skeleton（节点结构、旋转/平衡、基础查询 API）迁移到 `src/TextBuffer/Core`，附带 stub search hook 与 smoke `dotnet test`。 | 2 | Porter-CS | 2025-11-22 | PT-003 通过审阅 | ⏳ 2025-11-19：G1 落地 PieceTreeBuilder + `PieceTreeBuffer` 接线并通过 `dotnet test`；G2 需实现增量编辑/EnumeratePieces/Search stub TODO。 |
| P2 | PT-005 | 建立 QA 测试矩阵与基准计划：更新 `tests/TextBuffer.Tests/UnitTest1.cs`、记录首个 `dotnet test` 输出并在矩阵中登记覆盖面。 | 1 | QA-Automation | 2025-11-23 | PT-004 代码 drop | ✅ 2025-11-19：创建 `TestMatrix.md`、扩展 7 个 Fact 覆盖 Plain/CRLF/Multi-chunk、记录基线测试；S8/S9/S10 TODO 已标注依赖。 |
| P2 | PT-006 | 建立迁移日志与文档更新流程：在 `docs/reports/consistency/` 下提供模板并更新 `AGENTS.md` / Task Board changefeed 钩子。 | 1 | DocMaintainer | 2025-11-24 | PT-003~PT-005 产物 + Info-Indexer delta | 📋 Planned — 等待 QA 基线与 Porting Log 引用，以便定义迁移日志模板字段。 |
| P3 | PT-007 (Parking) | 规划 Search regex/stub 与 instrumentation 范围，明确下一冲刺的验收与依赖，先以文档占位。 | 0 (prep) | Planner → Porter-CS | 2025-11-25 | 取决于 PT-004 skeleton | 🅿️ 等待 Porter 搜索 stub TODO + Investigator WordSeparator mapping，下一冲刺定 scope。 |

## Plan
### Milestone 1 – Type Map Lockdown (Nov 19–20)
- Deliverables: 更新 `agent-team/type-mapping.md`、在 Info-Indexer changefeed 登记差异、补充 Task Board 依赖列。
- Tests / Validation: Planner + Info-Indexer 联合审阅，确认 Piece/PieceTree/Search section 与 TS 源一致。

### Milestone 2 – RB Tree Skeleton (Nov 21–22)
- Deliverables: `src/TextBuffer/Core` 新增 PieceTreeNode/PieceTreeModel/RB helpers，附 stub search API。
- Tests / Validation: `dotnet test`（PieceTree.TextBuffer.sln）+ 代码审查记录在 meeting log/PT-004 runSubAgent 报告。

### Milestone 3 – QA & Doc Hardening (Nov 23–24)
- Deliverables: QA 矩阵 + baseline run log；迁移日志模板、changefeed wiring（Task Board、AGENTS、indexes）。
- Tests / Validation: QA-Automation 存档 baseline `dotnet test` 输出；DocMaintainer 在 Info-Indexer delta 中登记新流程。

## Risks & Mitigations
| Risk | Impact | Mitigation |
| --- | --- | --- |
| PT-003 延迟将直接阻塞 PT-004，导致 sprint 压缩。 | High | 日常 checkpoint；若 11-20 晚前未交付则用 stub map 暂时代替并记录降级。 |
| Search regex 功能复杂度高且依赖未来 API。 | Medium | 通过 PT-007 占位跟踪；Porter-CS 在 PT-004 中 stub search hook 并输出 API 期望。 |
| 多 Agent 未遵循 Info-Indexer changefeed，造成文档分叉。 | Medium | 在每次 runSubAgent 指令中加入 `agent-team/indexes/README.md#delta-2025-11-19` checklist；DocMaintainer 复核。 |

## Demo / Review Checklist
- [ ] `agent-team/type-mapping.md` 新增 Piece/PieceTree/Search sections，并在 Info-Indexer delta 中记录。
- [ ] `src/TextBuffer/Core` 含 PieceTreeNode、PieceTreeModel、RB helpers，`dotnet test` 日志附在 QA 产物或 meeting log 中。
- [ ] `tests/TextBuffer.Tests` 内的 QA 矩阵与 baseline 日志存档，与 PT-005 运行记录互相引用。
- [ ] `docs/reports/consistency/`、`AGENTS.md`、Task Board 记录迁移日志流程，PT-006 勾选完成。
- [ ] Sprint Backlog 更新包含 PT-007 占位并在下一冲刺计划前评审。

## Progress Log
- 2025-11-19：PT-003 完成 —— Type mapping 加入 Piece/PieceTreeNode/SearchContext/BufferRange 区块、invariants/QA hooks、Diff Summary；Task Board 状态更新为 Done。
- 2025-11-19：PT-004.G1 / PT-004.M2（skeleton wiring）—— `PieceTreeBuilder`/`PieceTreeBuffer` 走通 ChunkBuffer→PieceTreeModel，`dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj`（Total 4, Passed 4），Porting Log 记录。
- 2025-11-19：PT-005.G1 —— `tests/TextBuffer.Tests/TestMatrix.md` 建立，`UnitTest1.cs` 扩展至 7 个 Fact 覆盖 Plain/CRLF/Multi-chunk/metadata，并记录基线 `dotnet test`（Total 7, Passed 7）。
