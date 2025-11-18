# Sprint 00
- **Date Range:** 2025-11-19 ~ 2025-11-26
- **Theme / Goal:** 建立 PieceTree 移植的基础设施（理解、架构、流程）
- **Success Criteria:**
  - 类型映射覆盖 Piece / Tree / Search 关键结构
  - C# 端具备红黑树骨架 + 基础 API
  - 测试与文档流程可复用

## Backlog Snapshot
| Priority | Task | Description | runSubAgent Budget | Owner |
| --- | --- | --- | --- | --- |
| P1 | PT-003 | 扩展 TS↔C# 类型映射 | 1 | Investigator-TS |
| P1 | PT-004 | 迁移 PieceTree RB Tree 骨架 | 2 | Porter-CS |
| P2 | PT-005 | 设计 QA 测试矩阵与基准计划 | 1 | QA-Automation |
| P2 | PT-006 | 建立迁移日志与文档更新流程 | 1 | DocMaintainer |

## Plan
### Milestone 1 (理解与规划)
- Deliverables: 更新后的 type-mapping、依赖说明、会议纪要
- Tests / Validation: 文档审阅

### Milestone 2 (实现与验证)
- Deliverables: C# Core 目录包含 RB Tree 类、基础 API 单元测试
- Tests / Validation: `dotnet test` + 结构性代码审查

## Risks & Mitigations
| Risk | Impact | Mitigation |
| --- | --- | --- |
| Search 功能依赖复杂正则 | Medium | 调研可否先 stub 再补全 |
| 多 Agent 信息不同步 | Medium | 严格执行文档更新流程 |

## Demo / Review Checklist
- [ ] `agent-team/type-mapping.md` 覆盖关键类型
- [ ] `PieceTree.TextBuffer` 出现 PieceTreeNode/RBTree 代码并测试通过
- [ ] `docs/meetings`、`docs/sprints` 与 `AGENTS.md` 更新完毕
- [ ] 下一阶段待办列入 task board
