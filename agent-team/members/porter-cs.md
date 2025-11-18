# Porter-CS Memory

## Role & Mission
- **Focus Area:** 将 TypeScript PieceTree 逻辑逐步移植到 `PieceTree.TextBuffer`
- **Primary Deliverables:** C# 源码、xUnit 覆盖、性能基准脚手架
- **Key Stakeholders:** Investigator-TS、QA-Automation、DocMaintainer

## Onboarding Summary (2025-11-19)
- 阅读/速览：`AGENTS.md` 时间线、`agent-team/ai-team-playbook.md`、`agent-team/main-loop-methodology.md`、两份 2025-11-19 会议纪要、`docs/sprints/sprint-00.md`、`docs/sprints/sprint-org-self-improvement.md`、`agent-team/task-board.md`（PT-004 聚焦）。
- 立即 C# 目标：根据 PT-004 在 `PieceTree.TextBuffer/Core` 完成 PieceTreeNode + 红黑树骨架，并按 Investigator-TS 的类型映射预留接口。
- 代码与测试记录：所有实现/测试日志将写入 `src/PieceTree.TextBuffer/README.md` 的“Porting Log”子节，并在本文件 Worklog 中附指针。

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| Core Library Skeleton | src/PieceTree.TextBuffer/Core | 主要的 PieceTree 结构放置点 |
| Buffer Entry Point | src/PieceTree.TextBuffer/PieceTreeBuffer.cs | 提供公共 API，需逐步替换占位实现 |
| Tests | src/PieceTree.TextBuffer.Tests/UnitTest1.cs | 先期可扩展基础 xUnit 框架 |
| Type Mapping | agent-team/type-mapping.md | TS↔C# 结构别名及字段含义 |
| TS Source | ts/src/vs/editor/common/model/pieceTreeTextBuffer | 迁移源码与参考行为 |

## Worklog
- **2025-11-19**
  - 完成首轮 Onboarding，熟悉 AI Team 运作方式、Sprint 目标与 PT-004 期待成果。
  - 审核当前 C# 骨架，确认 `PieceTreeBuffer` 仍为占位，需从 Core 目录启动红黑树实现。
  - 记录代码/测试日志归档位置（`src/PieceTree.TextBuffer/README.md`）。
- **2025-11-19 – Org Self-Improvement Mtg**
  - 评估 C# 端缺口（仅余 `ChunkBuffer`/`PieceSegment` + `StringBuilder` 缓冲），确认 PT-004 首阶段需先落 `PieceTreeNode`/sentinel/Tree 容器。
  - 与 Planner/Investigator/QA/DocMaintainer 对齐依赖：获取 Builder/Search/PrefixSum 类型映射、runSubAgent 模板拆分、QA 属性测试入口及 Porting Log 写入约定。
  - 承诺交付 Core README + TreeDebug 钩子帮助 QA 复核不变量，并把结构性变更写入 Porting Log。
- **2025-11-19 – PT-004.M2 drop**
  - 将 `PieceTreeBuffer` 接上 `ChunkBuffer` → `PieceTreeBuilder` → `PieceTreeModel` 流水线，`FromChunks`/`Length`/`GetText`/`ApplyEdit` 均以 PieceTree 数据驱动。
  - `ChunkBuffer` 新增 line-start/CRLF 计算与 `Slice` helper，`PieceSegment.Empty`、builder result 等保证 sentinel 元数据，`ApplyEdit` 暂以“重建整棵树”作为 TODO 记录的降级方案。
  - Tests: `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（pass，4 tests：multi-chunk builder + CRLF edit 覆盖）。
  - Risks: 每次编辑仍需重建树（性能/暂时性），Search stub 依旧待 Investigator-TS 完善类型映射后再规划 PT-007。
- **2025-11-19 – PT-004 literal translation spike**
  - 在 `src/PieceTree.TextBuffer/PortingDrafts/PieceTreeBase.literal.cs.txt` 新建 Literal C# 版本，完成 TypeScript `pieceTreeBase.ts` 开头到搜索逻辑的 1:1 结构移植并标注剩余 TODO，供后续增量补全与 Info-Indexer 建立 PortingDrafts 钩子。

- **Upcoming Goals (runSubAgent 粒度):**
  1. **PT-004.G2-next**：消除重建式编辑，接入 change buffer + PieceTree 原生插入/删除，并补齐 `EnumeratePieces`/`LocateLineByOffset` API 供 QA 复用。
  2. **PT-004.G3**：实现长度/位置互转与 chunk-based slicing 的额外断言，扩充 xUnit 覆盖（CR-only、BOM、跨 chunk ranges）。
  3. **OI-SUPPORT.G1**：保持 Porting Log & Core README 更新，并将 search stub 依赖、doc 钩子同步给 DocMaintainer/Planner 以支撑 PT-007 规划。

## Blocking Issues
- 仍需 Investigator-TS 在 `agent-team/type-mapping.md` 中补充 `pieceTreeTextBufferBuilder.ts` / `textModelSearch.ts` / `prefixSumComputer.ts` 字段与缓存语义，避免盲目迁移。
- QA-Automation 尚未锁定属性测试/基准入口，需其在 PT-005 定稿后提供最小断言集合以验证我们暴露的 API。
- DocMaintainer 的迁移日志模板（PT-006）与 Main Agent 的“是否 1:1 复刻 TS 红黑树” 决策待定，此前实现需保持开关便于回滚配置。

## Testing & Validation Plan
- 默认使用 `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` 进行单元测试，按 PT-004 每阶段至少补一个针对 Node/Tree API 的断言。必要时添加 BenchmarkDotNet 基准（待骨架稳定）。
- 关键红黑树操作需辅以调试断言（如节点颜色/黑高），计划构建 Debug-only 验证方法供 QA 复用。

## Hand-off Checklist
1. 所有代码位于 `src/PieceTree.TextBuffer` 并通过 `dotnet test`。
2. Tests or validations performed? 若本轮涉及实现，需提供结果。
3. 下一位接手者读取“Upcoming Goals”并续写实现，同时参考 `src/PieceTree.TextBuffer/README.md` Porting Log 获取代码/测试细节。
