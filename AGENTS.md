## 跨会话记忆文档
本文档(`./AGENTS.md`)会伴随每个 user 消消息注入上下文，是跨会话的外部记忆。完成一个任务、制定或调整计划时务必更新本文件，避免记忆偏差。请及时维护你的外部记忆文件，对齐你的关键认知和后续思路。

## 已知的工具问题
- 需要要删除请用改名替代，因为环境会拦截删除文件操作。
- 不要使用'insert_edit_into_file'工具，经常产生难以补救的错误结果。

## 用户语言
请主要用简体中文与用户交流，对于术语/标识符等实体名称则优先用原始语言。

## 项目概览
**总体目标**是将位于“./ts”目录内的VS Code的无GUI编辑器核心移植为C#类库(dotnet 9.0 + xUnit)。核心目标是“./ts/src/vs/editor/common/model/pieceTreeTextBuffer”, 如果移植顺利后续可以围绕pieceTreeTextBuffer再移植diff/edit/cursor等其他部分。

**用途背景**是为工作在Agent系统中的LLM创建一种DocUI，类似TUI但是不是渲染到2D终端而是渲染为Markdown文本，UI元素被渲染为Markdown元素。渲染出的Markdown会被通过上下文工程注入LLM Context中。可以想象为“把LLM Context作为向LLM 展示信息的屏幕”。这需要高质量的Text建模、编辑、比较、查找、装饰功能。举例说明这里“装饰”的含义，例如我们要创建一个TextBox Widget来向LLM呈现可编辑文本，把原始文本和虚拟行号渲染为Markdown代码围栏，把光标/选区这些overlay元素渲染为插入文本中的Mark，并在代码围栏外用图例注解插入的光标/选区起点/终点Mark。像这些虚拟行号、光标/选区Mark，就是前面所说的“装饰”。后续有望用这条DocUI为LLM Agent 打造更加LLM Native & Friendly的编程IDE。

## 最新进展
- 2025-11-19：已评估 PieceTree 移植可行性并在 `src/` 下创建 `PieceTree.sln`、`PieceTree.TextBuffer` 与 xUnit 测试骨架，提供最小 `PieceTreeBuffer` 占位实现及 README，等待从 TypeScript 逐步迁移核心逻辑。
- 2025-11-19：确认通过 `runSubAgent` 组建 AI Team 的协作模式——主 Agent 负责调度与消息转发，SubAgent 以 `agent-team/` 下的记忆文件维持认知，必要时共享聊天室文件实现“会议”。
- 2025-11-19：建立 AI Team 运作设施：`agent-team/ai-team-playbook.md`、SubAgent 记忆模板、任务看板、TS↔C# 类型映射草稿，以及 `docs/meetings` / `docs/sprints` 模板，便于按 `runSubAgent` 粒度规划执行。
- 2025-11-19：构建首批 SubAgent 角色（Planner、Investigator-TS、Porter-CS、QA-Automation、DocMaintainer），创建各自记忆文件、任务分配、Kickoff 会议纪要与 Sprint-00 计划。
- 2025-11-19：整理主 Agent 主循环与 SubAgent 协作方法论，形成 `agent-team/main-loop-methodology.md`，确保后续按固定迭代流程推进。
- 2025-11-19：在主循环中引入 DocMaintainer 三项职责（Info Proxy、Consistency Gate、Doc Gardener），更新 `agent-team/members/doc-maintainer.md` 以指导文档治理。
- 2025-11-19：召开“Org Self-Improvement”全员会议（`docs/meetings/meeting-20251119-org-self-improvement.md`），决定新增 Info-Indexer 角色、建立索引目录并启动组织自我完善 Sprint。
- 2025-11-19：创建 Info-Indexer 记忆文件、`agent-team/indexes/README.md`、`docs/sprints/sprint-org-self-improvement.md`，在任务板加入 OI 系列任务并更新主循环文档以包含索引钩子。
- 2025-11-19：完成 OI-002/OI-001/OI-003/OI-004，分别交付 `agent-team/indexes/core-docs-index.md`、`docs/reports/consistency/consistency-report-20251119.md`、runSubAgent 提示模板（主循环/Playbook/记忆模板）以及分层版 `agent-team/task-board.md`，全面引入 Info-Indexer changefeed 提醒。
- 2025-11-19：完成 PT-003（类型映射），`agent-team/type-mapping.md` 新增 Piece/PieceTreeNode/SearchContext/BufferRange 映射及 Porter-CS/QA 风险提示，并附 Diff Summary 供 Info-Indexer 消费。
- 2025-11-19：交付 PT-004.G1，`src/PieceTree.TextBuffer/Core` 接入 `PieceTreeBuilder`→`PieceTreeModel` 链路，`PieceTreeBuffer` 改为树驱动 façade 并记录增量编辑 TODO，`dotnet test PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（4/4）通过且 Porting Log 更新。
- 2025-11-19：PT-005.G1 完成 QA 基线，`src/PieceTree.TextBuffer.Tests/TestMatrix.md` 建立，`UnitTest1.cs` 扩展至 7 个 Plain/CRLF/Multi-chunk/metadata Fact，baseline `dotnet test`（7/7）与 S8~S10 TODO 均已记录。
- 2025-11-19：创建 `docs/reports/migration-log.md`，在更新 AGENTS / Sprint / Task Board 状态前须先查阅对应迁移日志行并同步 `agent-team/indexes/README.md#delta-2025-11-19` changefeed。
- 2025-11-19：确定以“Diff Brief → 实现 → TS 测试 → 单条日志”循环推进 PieceTree 迁移，并在 `docs/prompts/diff-driven-porting-prompt.md` 固化提示模板，引用 [`docs/reports/migration-log.md`](docs/reports/migration-log.md) 与 [`agent-team/indexes/README.md#delta-2025-11-19`](agent-team/indexes/README.md#delta-2025-11-19) 追踪每轮变更。
- 2025-11-19：PT-004.Positions 增加 `TextPosition` 与 `PieceTreeBuffer.GetPositionAt/GetOffsetAt` 等 API，并移植 TS prefix-sum 风格测试（详见 [`docs/reports/migration-log.md`](docs/reports/migration-log.md) 对应条目，changefeed 仍为 [`agent-team/indexes/README.md#delta-2025-11-19`](agent-team/indexes/README.md#delta-2025-11-19)）。
- 2025-11-19：PT-010 完成 CRLF Normalization 移植。Investigator-TS 分析了 `normalizeEOL` 与 Builder 差异，Porter-CS 实现了 `PieceTreeNormalizer`、BOM/Split-CRLF 处理及 `_eolNormalized` 优化，并补充了 3 个 TS 对应测试（CRLF 删除与 Normalization 优化验证），Tests (23/23) 通过。

**状态更新提示：** 编辑本文件或引用 PT/OI 进度前，请先核对 `docs/reports/migration-log.md` 与 `agent-team/indexes/README.md#delta-2025-11-19`，并在条目中附上两者的链接。