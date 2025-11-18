# Sprint OI-01 – 组织自我完善
- **Date Range:** 2025-11-19 ~ 2025-11-23
- **Theme / Goal:** 审计并优化 AI Team 的协作结构、文档体系与流程模板
- **Success Criteria:**
  - 核心文档正交性审计完成并生成行动项
  - 至少 1 个索引文件上线，减轻 AGENTS/task-board 描述冗余
  - runSubAgent 输入模板固化，主循环 checklist 更新
  - Task Board 精简策略实施，DocMaintainer/Info-Indexer 分工落地

## Backlog Snapshot
| Priority | Task | Description | runSubAgent Budget | Owner |
| --- | --- | --- | --- | --- |
| P0 | OI-001 | 核查文档正交性与缺口 | 1 | DocMaintainer + Info-Indexer |
| P0 | OI-002 | 建立索引目录与首个索引 | 1 | Info-Indexer |
| P1 | OI-003 | 模板化 runSubAgent 输入 & 流程 | 1 | Planner |
| P1 | OI-004 | Task Board 精简与分层方案 | 1 | DocMaintainer |
| P2 | OI-005 | 更新主循环 & Checklist，加入 Info-Indexer 钩子 | 1 | Main Agent |

## Plan
### Milestone A – 审计与索引 (Day 1-2)
- Deliverables: 文档审计报告、`core-docs-index.md`
- Validation: DocMaintainer 复核并同步 AGENTS

### Milestone B – 模板与流程 (Day 2-3)
- Deliverables: runSubAgent 输入模板、更新后的 main-loop 方法论
- Validation: 通过一次试运行验证模板可用

### Milestone C – Task Board 精简 (Day 3-4)
- Deliverables: 新版任务板结构、精简策略说明
- Validation: Planner/DocMaintainer 联合审阅

## Risks & Mitigations
| Risk | Impact | Mitigation |
| --- | --- | --- |
| 文档精简导致信息丢失 | Medium | Info-Indexer 负责保留索引，DocMaintainer 记录精简日志 |
| 新模板增加任务准备成本 | Low | 提供示例 + checklist，逐步推广 |

## Demo / Review Checklist
- [ ] `docs/meetings/meeting-20251119-org-self-improvement.md` 行动项完成
- [ ] `agent-team/indexes/` 至少包含一个主题索引
- [ ] `agent-team/task-board.md` 采用新结构并标注 OI 任务状态
- [ ] `agent-team/main-loop-methodology.md` 与 runSubAgent 模板更新完毕
- [ ] 成果记录于 `AGENTS.md`
