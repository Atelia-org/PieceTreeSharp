# AI Team Playbook

## Guiding Principles
1. **Star Topology Communication** – 主 Agent 负责转发所有跨 Agent 的消息，SubAgent 通过共享文件实现信息同步；避免隐式依赖。
2. **File-Centric Memory** – 每个知识域只维护一个核心文档，避免碎片化；更新时写清“现状 / 决策 / 下一步”。
3. **runSubAgent 粒度计划** – 以单次 `runSubAgent` 可完成的工作量衡量任务切片，保持每轮调用都有明确输入、产出与交付验收步骤。
4. **模板驱动** – SubAgent 记忆、会议记录、冲刺计划均使用统一模板，减少上下文恢复成本。
5. **Continuous Sync** – 重要结论必须写回共享文件（`AGENTS.md` 或 agent-team 资料），即使当前会话结束也可继续推进。

## Core Artifacts
- `AGENTS.md`：公司级记忆与时间轴。
- `agent-team/copilot-lead-notes.md`：主 Agent 私有笔记。
- `agent-team/templates/subagent-memory-template.md`：SubAgent 记忆模板。
- `agent-team/main-loop-methodology.md#runSubAgent-Input-Template`：标准 `runSubAgent` 输入骨架（ContextSeeds/Objectives/...），含 changefeed checkpoint [agent-team/indexes/README.md#delta-20251119](agent-team/indexes/README.md#delta-20251119)。
- `agent-team/task-board.md`：全队任务分解、责任、状态。
- `docs/meetings/*.md`：会议纪要。
- `docs/sprints/*.md`：阶段性计划。
- `agent-team/type-mapping.md`：TS ↔ C# 类型桥接表。

## Workflow Overview
1. **Scoping** – 主 Agent 在 `task-board.md` 中登记任务，指定预期 `runSubAgent` 次数与交付物。
2. **Assignment** – 为 SubAgent 复制模板，写入任务简介、依赖文件、当前状态。
3. **Execution** – 触发 `runSubAgent`，按 `agent-team/main-loop-methodology.md#runSubAgent-Input-Template` 填写 ContextSeeds/Objectives/Dependencies，并在草稿前确认 changefeed checkpoint [agent-team/indexes/README.md#delta-20251119](agent-team/indexes/README.md#delta-20251119) 已消费，最后列出交付与记忆写入路径。
4. **Review** – 完成后检查产物，必要时在会议文件中组织评审；将结论同步到 `AGENTS.md`。
5. **Planning** – 周期性会议（`docs/meetings/meeting-YYYYMMDD.md`）输出下一阶段计划；必要时更新 `docs/sprints/sprint-XX.md`。

## Collaboration Tips
- 用“输入 / 处理 / 输出”格式编写任务简述，让 SubAgent 能快速理解边界与验收标准。
- 当任务存在阻塞时，SubAgent 应在其记忆文件的“Blocking Issues”段记录原因，便于主 Agent 协调。
- 若需要多名 SubAgent 协同，先在会议文件中列出接口契约，再分别执行各自部分。
