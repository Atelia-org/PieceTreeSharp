# Copilot Lead Notes

## Tooling Understanding
- `runSubAgent` 可在单次调用内运行完整的 LLM Agent 工具循环，具备和我相同的读写权限，但缺少 `runSubAgent` 与跨轮对话上下文。
- 可以通过在 `agent-team/` 下创建特定记忆文件，为每位 SubAgent 提供持久化的“入职资料”，帮助其快速加载必要文件索引与工作流约定。
- 共享对话或会议可通过一个公共的“聊天室文件”实现，由我负责在星形拓扑中转发消息。

## 团队策略
- 我负责总体调度：划分任务、创建/更新记忆文件、触发 `runSubAgent`、整合成果。
- SubAgent 角色建议：例如“PieceTree-Mapper”（专注 TypeScript 端梳理）与“PieceTree-Porter”（专注 C# 实现），后续按模块扩展。
- 每位 SubAgent 需维护：任务简述、关键文件列表、当前阻塞、计划中的下一步，确保随用随取。
- 统一采用 `agent-team/ai-team-playbook.md` 中的流程，所有新文档应参考对应模板以保证信息密度。
- Info-Indexer 与 DocMaintainer 形成“索引增量 -> 文档精简”流水，Planner/QA/Porter/Investigator 在会议纪要中只写结论并指向索引/日志，主循环 checklist 需确认两位守门人是否完成巡检。

## 即刻计划
1. 调度 OI-001~OI-004：依次触发 DocMaintainer+Info-Indexer（审计/索引）、Planner（模板）、DocMaintainer（Task Board 压缩），每次 runSubAgent 前复核 Task Board 预算与依赖。
2. 协助 Planner 完成 runSubAgent 输入模板扩展（ContextSeeds/Objectives/Dependencies/Hand-off）并同步到 `main-loop-methodology.md` 与记忆模板。
3. 监控 Investigator-TS → Porter-CS → QA-Automation 的 PT-003/004/005 依赖链，必要时组织临时会议或在 Task Board 写清阻塞；确保新的实现/测试成果进入 Porting Log、TestMatrix、索引。
4. 所有会议/任务完成后，第一时间更新 `AGENTS.md` 与个人笔记，保持跨会话记忆同步。

## runSubAgent 会议经验
- 会前准备：先定位会议记录文件、最新 Sprint/Task Board、所有成员记忆路径，并在 prompt 中显式列出读取/写入文件，避免 SubAgent 忘记同步自己的 memory。
- Prompt 结构：使用“Context / Goals / Files to inspect / Files to update / Reporting instructions”模板，强调“更新记忆后再汇报”，减少遗漏。
- 顺序调度：按依赖顺序调用（Planner→Investigator→Porter→QA→DocMaintainer→Info-Indexer），让后续角色能够参考前人发言，保证会议纪要自然串联。
- 会后收敛：逐一复核会议文件与记忆文件是否落地、Task Board/Sprint 是否需要更新，再写入 `AGENTS.md` 与本笔记，形成可回溯的主持流程。
