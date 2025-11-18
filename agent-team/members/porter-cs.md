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

- **Upcoming Goals (runSubAgent 粒度):**
  1. **PT-004.G1**：完成 `Core/PieceTreeNode`、sentinel、`PieceTree` 管理器与 `TreeDebug` 断言工具，为 Investigator 新字段映射预留钩子。
  2. **PT-004.G2**：将 `PieceTreeBuffer.ApplyEdit/FromChunks` 接上树容器并草拟 `EnumeratePieces`/`LocateLineByOffset` API，便于 QA-Automation 固定测试矩阵输入。
  3. **OI-SUPPORT.G1**：更新 `src/PieceTree.TextBuffer/README.md` Porting Log + 新建 `Core/README.md`，记录每次树操作移植、引用 `type-mapping.md` 章节供 DocMaintainer 编织索引。

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
