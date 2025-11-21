# Sprint 02 – Alignment & Audit R4
- **Date Range:** 2025-11-21 ~ 2025-11-28
- **Theme / Goal:** 继续推进 TS↔C# 对照审核，聚焦 PieceTree Builder/ChangeBuffer、增量编辑、Cursor WordOps 与 DocUI Find/Replace 管线，确保 DocUI-ready 体验覆盖 chunk 构建到搜索装饰全链路。
- **Success Criteria:**
  - CL5~CL8（见 `docs/reports/audit-checklist-aa4.md`）完成 Investigator 审计与 Porter 修复交付，差异清单与实现结果分别落地 handoff 文件。
  - Builder/ChangeBuffer/Cursor/DocUI 相关测试补齐，`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` ≥ 92 项保持绿色，并在 `src/PieceTree.TextBuffer.Tests/TestMatrix.md` 登记新覆盖。
  - `docs/reports/migration-log.md` & `agent-team/indexes/README.md` 记录 AA4 变更，AGENTS / Sprint / Task Board 同步至 Info-Indexer 最新 changefeed。
  - DocUI MarkdownRenderer 展现搜索/替换 overlay、chunk 元数据显示、Cursor word selection 标记，形成对 LLM 友好的多层装饰输出。

**Status Edits Reminder:** 在更新本 Sprint 任何状态前先查阅 `docs/reports/migration-log.md` 以及 `agent-team/indexes/README.md#delta-2025-11-20` / `#delta-2025-11-21`，并在备注中引用对应条目。

## Backlog Snapshot
| Priority | Task | Description & Deliverables | runSubAgent Budget | Owner | Target Date | Dependencies | Status / Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| P0 | AA4-001 | Investigator-TS：完成 CL5（PieceTree Builder & Factory）对照，输出 `agent-team/handoffs/AA4-001-Audit.md` 并更新 checklist。 | 2 | Investigator-TS | 2025-11-22 | Sprint 02 kickoff、changefeed checkpoint | Done – `AA4-001-Audit.md` 记录 F1~F6，CL5 checklist 更新待 AA4-005 落地 |
| P0 | AA4-002 | Investigator-TS：完成 CL6（ChangeBuffer/CRLF/large edits）对照，输出 `agent-team/handoffs/AA4-002-Audit.md`。 | 2 | Investigator-TS | 2025-11-22 | 同上 | Done – `AA4-002-Audit.md` 捕捉 change buffer/CRLF/metadata 缺口，CL6 checklist 更新 |
| P1 | AA4-003 | Investigator-TS：完成 CL7（Cursor word/snippet/multi-selection）对照，输出 `agent-team/handoffs/AA4-003-Audit.md`。 | 2 | Investigator-TS | 2025-11-23 | CL5/CL6 context | Planned |
| P1 | AA4-004 | Investigator-TS：完成 CL8（DocUI Find/Replace + Decorations）对照，输出 `agent-team/handoffs/AA4-004-Audit.md`。 | 2 | Investigator-TS | 2025-11-23 | CL5~CL7 context | Planned |
| P0 | AA4-005 | Porter-CS：根据 CL5 差异实现 PieceTree Builder/Factory parity，记录在 `agent-team/handoffs/AA4-005-Result.md` + 迁移日志。 | 3 | Porter-CS | 2025-11-25 | AA4-001 | Done – 2025-11-21 交付 chunk split/BOM/DefaultEOL parity，详见 `docs/reports/migration-log.md` 中 AA4-005 行及 `agent-team/indexes/README.md#delta-2025-11-21`；`PieceTreeBuilderTests`、`PieceTreeFactoryTests`、`AA005Tests` 覆盖同步。 |
| P0 | AA4-006 | Porter-CS：按 CL6 结果实现 ChangeBuffer/CRLF/large edit 修复，更新 tests & 文档。 | 3 | Porter-CS | 2025-11-25 | AA4-002 | Done – 2025-11-21 Porter+QA 验证（见 `agent-team/handoffs/AA4-006-Result.md`、`docs/reports/migration-log.md`「AA4-006 (QA Verified)」行与 `agent-team/indexes/README.md#delta-2025-11-21`）；`PieceTreeModelTests`、`CRLFFuzzTests`、`TestMatrix.md` 已记录。 |
| P1 | AA4-007 | Porter-CS：实现 Cursor word/snippet/multi-select 语义，更新 MarkdownRenderer/DocUI 展示。 | 3 | Porter-CS | 2025-11-26 | AA4-003 | Planned |
| P1 | AA4-008 | Porter-CS：实现 DocUI Find/Replace overlays + capture decorations，`MarkdownRenderer`/`TextModelSearch` 同步。 | 3 | Porter-CS | 2025-11-26 | AA4-004 | Planned |
| P0 | AA4-009 | QA-Automation：扩展 Builder/ChangeBuffer/Cursor/DocUI 测试，记录最新 `dotnet test` 基线与 `TestMatrix` 更新。 | 2 | QA-Automation | 2025-11-27 | AA4-005~008 | Done – QA revalidation 完成（`agent-team/handoffs/AA4-009-QA.md`、`src/PieceTree.TextBuffer.Tests/TestMatrix.md`），`PIECETREE_DEBUG=0 dotnet test ... --nologo` 记录 119/119 基线并在 `agent-team/indexes/README.md#delta-2025-11-21` 广播。 |
| P0 | OI-011 | Info-Indexer：发布 AA4 changefeed、更新 `core-docs-index`、同步 AGENTS/Sprint/Task Board。 | 1 | Info-Indexer | 2025-11-28 | 全部 AA4 deliverables | Planned |

## Plan
### Milestone 1 – Investigator Sweep (Nov 21–23)
- Deliverables: `agent-team/handoffs/AA4-001/002/003/004`, `docs/reports/audit-checklist-aa4.md` CL5~CL8 填充。
- Tests / Validation: 每个 handoff 包含 TS vs C# 行级比较、风险评级、建议修复列表，DocMaintainer 复核引用链。

### Milestone 2 – Porter Remediation (Nov 23–26)
- Deliverables: `agent-team/handoffs/AA4-005/006/007/008`, 代码补丁 + 迁移日志 + changefeed delta。
- Tests / Validation: 各主题需附 `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`，必要时新增目标化测试（Builder chunk、ChangeBuffer CRLF、Cursor wordOps、DocUI search overlay）。

### Milestone 3 – QA & Broadcast (Nov 26–28)
- Deliverables: AA4-009 QA 报告、`TestMatrix.md` 更新、AGENTS/Sprint/Task Board/Indexes 同步、DocUI demo 片段。
- Tests / Validation: Consolidated `dotnet test ...`（≥92），DocMaintainer 运行 consistency gate，Info-Indexer 发布 changefeed。

## Risks & Mitigations
| Risk | Impact | Mitigation |
| --- | --- | --- |
| Builder/ChangeBuffer 差异牵涉 chunk metadata，若实现顺序错误易导致 PieceTree 结构损坏 | High | Investigator 在 handoff 中附 TS 调用链（`PieceTreeTextBufferBuilder.acceptChunk/_finish`、`PieceTreeBase._insert`），Porter 逐段移植并配 fuzz tests |
| Cursor word/snippet 语义复杂，DocUI 叠加 injected text 可能出现事件风暴 | Medium | Porter 在实现前补充设计笔记，QA 增加 injected text + multi-cursor cases，DocMaintainer 验证事件引用 |
| DocUI Find overlays 涉及 captureMatches & ownerId 分层，若未与 Decorations 同步会导致渲染错乱 | Medium | 将 `MarkdownRenderer` 扩展分两步：先接 SearchRange API，再渲染 overlay；QA 提供 golden 输出 |
| changefeed 更新滞后导致 AGENTS/Sprint 描述失真 | Medium | OI-011 作为硬门槛；Broadcast 前检查 `agent-team/indexes/README.md` 是否包含最新 delta |

## Demo / Review Checklist
- [ ] CL5~CL8 Investigator handoff 可溯源（含差异截图/段落）。
- [ ] Porter 修复合入、`docs/reports/migration-log.md` + changefeed 更新。
- [ ] QA 报告列出新增测试及 `dotnet test` 绿线（≥92）。
- [ ] DocUI MarkdownRenderer 展示搜索/替换 overlay、Cursor word selection markers。
- [ ] AGENTS / Sprint 02 / Task Board / Indexes 一致，记录下一阶段候选。

## Progress Log
- 2025-11-20：Sprint 02 立项 —— 更新 `agent-team/task-board.md` 至 Phase 7 (AA4)，创建本文件与 `docs/reports/audit-checklist-aa4.md` 计划 CL5~CL8，等待 Investigator runSubAgent。