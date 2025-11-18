# Main Loop Methodology

> 目标：让主 Agent 以可重复的流程驱动 SubAgent，借助外部文档实现“记忆化主循环”，从而持续推进 PieceTree 移植。

## 0. 工具回顾
- **runSubAgent**：唯一的执行单元；一次调用 = 一次“函数调用”，必须提供清晰输入与写入目标。
- **外部文档**：`AGENTS.md`（全局时间线）、`docs/sprints/*.md`（目标/计划）、`docs/meetings/*.md`（沟通/决策）、`agent-team/members/*.md`（个体记忆）、`agent-team/task-board.md`（任务状态）。

## 1. 主循环视角
把自己视作“调度器(main)”：
1. **LoadContext**：读取 `AGENTS.md`、当前 Sprint、任务板，并先消费 changefeed（详见 [agent-team/indexes/README.md#delta-20251119](agent-team/indexes/README.md#delta-20251119)），必要时请 Info-Indexer 产出摘要，确认无遗漏后再进入 PlanStep。
2. **PlanStep**：判断下一次迭代目标（依 Sprint 与任务优先级），决定需要触发哪些 SubAgent。
3. **PrepPayload**：为每个 SubAgent 汇总输入：
   - 任务描述 + 期望输出
   - 相关文件路径
   - 需要更新的记忆文档
   - runSubAgent 预算与验收方式（测试/文档/代码）
4. **ExecuteSubAgent**：调用 `runSubAgent`。若任务大于一次预算，拆分为多次迭代。
5. **IntegrateResults**：验证输出（代码编译/测试/文档审阅），并回写：
   - `agent-team/task-board.md` → 状态与备注
   - 相关记忆文件 / README / type mapping 等
  - 触发 DocMaintainer 进行一致性巡检（详见第 3 章），必要时压缩陈旧内容
6. **Broadcast**：当达到阶段成果或有决策需要共享，更新 `AGENTS.md` 和 sprint/meeting 文档；在写入前再次对照 [agent-team/indexes/README.md#delta-20251119](agent-team/indexes/README.md#delta-20251119) 的 changefeed checkpoint，确认发布信息已包含最新 delta，必要时请求 Info-Indexer 发布索引更新。
7. **Feedback**：评估流程是否高效，记录改进项，必要时创建下一次会议或 sprint 调整。

该循环在每个“主 Agent 回合”内执行一次，保证所有信息都落在文件中，便于下次加载时恢复状态。

## 2. SubAgent 交互契约
- **输入格式**（建议贴在 runSubAgent prompt 内）：
  - Context: <任务背景 + Sprint 引用>
  - Goals: <清单>
  - Files to inspect
  - Files to write
  - Acceptance: <测试命令/文档要求>
  - Memory updates: <具体路径>
- **输出期望**：SubAgent 完成执行后，需：
  1. 在指定文件写入成果。
  2. 若无法完成，记录阻塞于其记忆文件 `Blocking Issues` 区段。
  3. 给出下一步建议，方便主 Agent 决策。

## runSubAgent Input Template
为避免遗漏上下文与审计指出的 changefeed hook，Planner 维护以下可复制模板，贴入 `runSubAgent` prompt 作为固定骨架：

```
ContextSeeds:
- Sprint / meeting anchors:
- Changefeed checkpoint: [agent-team/indexes/README.md#delta-20251119](agent-team/indexes/README.md#delta-20251119)
- Prior runSubAgent links:

Objectives:
- <ordered list of deliverables>

Dependencies:
- People, tools, or upstream files needed before work starts

Files to Inspect:
- <list of source files / docs to read>

Files to Update:
- <authoritative paths + acceptance notes>

Acceptance & Reporting:
- Tests / linters / review targets
- Memory + task board updates expected

Indexing Hooks:
- Consume latest changefeed delta before drafting plan
- Note any new references for [agent-team/indexes/README.md#delta-20251119](agent-team/indexes/README.md#delta-20251119)
```

使用指引：
1. **LoadContext** 结束前必须确认 [agent-team/indexes/README.md#delta-20251119](agent-team/indexes/README.md#delta-20251119) 的 changefeed 列表已被读取，若有新增/冲突需在 `ContextSeeds` 中引用；`PrepPayload` 阶段只接受这些“已确认”的 delta。
2. **Broadcast** 阶段在发布更新或下一轮调用前再度检查 changefeed（可请求 Info-Indexer 提供 diff），并在 `Indexing Hooks` 小节注明“已消费至 <日期/commit>”。
3. 若 SubAgent 引入新的可索引内容，需在输出中说明是否需要 DocMaintainer/Info-Indexer 写回增量，避免遗漏 `delta-20251119` 提到的 changefeed 钩子。

## 3. DocMaintainer 集成节点
DocMaintainer 与 Info-Indexer 分工如下：
1. **Info Proxy（LoadContext 之后）**：若主 Agent 需要大规模信息检索或摘要，优先触发 DocMaintainer 调用，产出写入其记忆或指定临时文件，以减少主上下文占用。
2. **Consistency Gate（IntegrateResults 阶段）**：在代码/知识更新后，DocMaintainer 负责复核核心文档是否自洽（`AGENTS.md`、Sprint、任务板、README 等），并执行交叉引用校验。
3. **Doc Gardener（Broadcast & Feedback 之前）**：定期整理、压缩或存档过时内容，确保关键文件保持紧凑。必要时记录“精简”行动与理由，防止信息丢失。
4. **Info-Indexer Hooks**：负责在 Load/Broadcast 阶段提供索引增量与引用表，帮助其他成员快速定位信息并减少重复描述。

## 4. Sprint 与任务对接
1. Sprint 文件 (`docs/sprints/sprint-XX.md`) 定义一周目标、任务、runSubAgent 预算。
2. Task Board 为实时状态表；任何任务状态变动必须写回此表。
3. Planner 角色负责在每次主循环的 PlanStep 中，确保 Sprint/Task Board 一致。

## 5. 会议驱动的阶段性调整
- **触发条件**：出现跨角色依赖、重大阻塞、阶段总结。
- **流程**：
  1. 创建 `docs/meetings/meeting-YYYYMMDD-*.md`。
  2. 事先写好 Agenda 与需要的输入。
  3. 会议结束后，列出 Decisions / Action Items（runSubAgent 粒度）。
  4. 更新 Sprint & Task Board。

## 6. Example Iteration
1. Load: 读取 Sprint-00，发现 PT-003 进行中。
2. Plan: 需要 Investigator-TS 扩展类型映射。
3. Prep: 在 prompt 中指定 `ts/...` 文件、目标 `agent-team/type-mapping.md`。
4. Execute: runSubAgent(Inves-TS)。
5. Integrate: 审阅新映射，更新 task board → PT-003 = Done；DocMaintainer 把结果写入 AGENTS。
6. Feedback: 记录“下一步是 PT-004”到 Planner 记忆。

## 7. 持续改进
- 在 `agent-team/main-loop-methodology.md`（本文）维护流程描述；每次发现痛点，添加“改动记录”段落（待后续补充）。
- 可考虑维护一个 `Known Issues` 区域（例如：当多个 SubAgent 需要同一文件时如何协调），并约定解决方案。

## 8. Checklist（每次主循环）
1. [ ] Sprint & Task Board 同步
2. [ ] changefeed checkpoint [agent-team/indexes/README.md#delta-20251119](agent-team/indexes/README.md#delta-20251119) 已消费（LoadContext 前 + Broadcast 前各一次），并在 `ContextSeeds / Indexing Hooks` 记录时间戳
3. [ ] runSubAgent 调用目标明确、输入齐备
4. [ ] 输出已验收（测试/审阅）
5. [ ] 文档更新：Task Board / Memory / AGENTS / 会议 / Sprint（必要时调用 DocMaintainer）
6. [ ] 记录下一次循环的 TODO 或阻塞

按照以上流程，可在多次会话中保持“主 Agent = 中控 + 记忆协调者”的角色，SubAgent 则聚焦具体产出，最终实现 PieceTree 迁移的系统化推进。
