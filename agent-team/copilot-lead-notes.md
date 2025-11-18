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

## 即刻计划
1. 设计记忆文件模板，便于未来快速复制给新成员。
2. 选择一个公共聊天室文件名（如 `docs/meetings/meeting-<date>.md`）并在需要协作时创建。
3. 下一次重大任务前，使用 `runSubAgent` 为首位成员完成入职自述，减少主 Agent 的上下文负担。
4. 在 `agent-team/task-board.md` 上持续维护任务粒度、runSubAgent 预算与状态。
