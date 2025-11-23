# Meeting: Org Self-Improvement Kickoff
- **Date:** 2025-11-19
- **Participants:** Main Agent, Planner, Investigator-TS, Porter-CS, QA-Automation, DocMaintainer, Info-Indexer (new role proposal)
- **Facilitator:** Main Agent
- **Objective:** 审视基础设施、评估职责划分、决定文档治理方案并形成组织自我完善 sprint

## Agenda
1. 角色与岗位评估
2. 核心文档正交性检查与压缩策略
3. 主循环与 DocMaintainer 协作流程
4. 形成“组织自我完善 Sprint”

## Notes
- **结构评估**：现有 5 角色覆盖规划、调研、实现、测试、文档，但文档相关任务持续增长，DocMaintainer 容量不足。
- **岗位调整**：新增 Info-Indexer 负责信息索引、摘要与知识压缩，DocMaintainer 聚焦核心文档一致性。
- **文档正交性**：
  - `AGENTS.md`：里程碑时间线
  - `agent-team/main-loop-methodology.md`：流程定义
  - `docs/meetings/*.md`：瞬时讨论
  - `docs/sprints/*.md`：短期目标
  - 冗余风险：成员记忆与 Task Board 描述重复。决定让 Info-Indexer 维护 `agent-team/indexes/` 概览以减少重复描述。
- **流程优化**：
  - 在主循环 checklist 中增设“DocMaintainer 调用确认”以及“Info-Indexer 更新索引”步骤。
  - 对 runSubAgent 调用统一提供输入模板片段，由 Planner 主导维护。

## Decisions
| # | Decision | Owner | Related Files |
| --- | --- | --- | --- |
| 1 | 新设 `Info-Indexer` 角色，负责索引/摘要/压缩，DocMaintainer 专注核心文档一致性 | Main Agent | agent-team/members/info-indexer.md |
| 2 | 启动“组织自我完善 Sprint”以执行结构改进和文档治理 | Planner | docs/sprints/sprint-org-self-improvement.md |
| 3 | Task Board 增加 OI 系列任务，runSubAgent 粒度跟踪组织改进 | Planner | agent-team/task-board.md |
| 4 | 在主循环中加入 Info-Indexer 钩子（LoadContext 后、Broadcast 前） | Main Agent | agent-team/main-loop-methodology.md |

## Action Items (runSubAgent-granularity)
| Task | Example Prompt / Inputs | Assignee | Target File(s) | Status |
| --- | --- | --- | --- | --- |
| OI-001 文档正交性审计 | "审阅现有核心文档，输出重复/缺口列表" | DocMaintainer + Info-Indexer | docs/meetings, docs/sprints, AGENTS.md | Planned |
| OI-002 索引与摘要体系 | "创建 indexes/README + 首个索引" | Info-Indexer | agent-team/indexes | Planned |
| OI-003 流程模板化 | "完善 runSubAgent 输入模板与流程指南" | Planner | agent-team/main-loop-methodology.md | Planned |
| OI-004 任务板压缩策略 | "提出 Task Board 精简方案并实施" | DocMaintainer | agent-team/task-board.md | Planned |

## Parking Lot
- 是否需要自动化脚本辅助文档一致性检查？待 Ops/Tooling 能力具备后评估。
- 深入正则/搜索模块移植前，需先完成组织自我完善 sprint，确保流程稳定。

## Participant Statements
### Planner
- Role coverage: DocMaintainer 仍被动承担 backlog 压缩 + 核心文档一致性，Info-Indexer 新设但尚未形成作业清单。我建议在 OI-001 交付前由 Planner 协助 Info-Indexer 建立“索引输入 -> DocMaintainer 精简”流水，并暂由 Planner 复核 Task Board 描述，待 Info-Indexer 熟悉后再回收职责。
- Template improvements: 计划在 OI-003 里把 runSubAgent 输入模板扩展为 `ContextSeeds / Objectives / Dependencies / Hand-off` 四段，并提供 copyable 片段给主循环 checklist 及 `agent-team/templates/subagent-memory-template.md`，保证每次调用都能勾选依赖与复核步骤。
- Dependencies: OI-001 的缺口清单需 DocMaintainer + Info-Indexer 联合提交后我才能锁定 Task Board 精简方案；PT-003 的类型映射更新时间线仍等待 Investigator-TS；QA-Automation 请在模板更新后确认是否需要新增验证栏位。

### Investigator-TS
- PieceTree TS coverage: `agent-team/type-mapping.md` 目前完整记下 `pieceTreeBase.ts` 节点字段、不变量与 `rbTreeBase.ts` 重平衡步骤，但 `pieceTreeTextBufferBuilder.ts`、`textModelSearch.ts`、`prefixSumComputer.ts` 仍是空白，这些 blind spots 会影响我们评估初始化、搜索与性能路径。
- Blind spot handling: Search/regex 调用链与 Builder 管线尚未建立类型映射，下一轮 PT-003 计划围绕其 call graph 构建字段列表，同时确认是否需要先 stub 还是一次性映射，以免 Porter-CS 的 RBTree 接口等待过久。
- Coordination requests: Info-Indexer 请明确 `agent-team/indexes/` 目录的文件命名与层级（例如 `core-ts-piece-tree.md` 是否放在 `core-*` 子树），这样我能把调查输出转成索引条目，不再在 Task Board/会议中重复描述；Porter-CS 也请列出 C# RBTree 预期公开 API（插入/删除、snapshot rebuild、search hints），我好在类型映射中显式标注相关 TS entry point。
- Doc manageability: 建议让 Investigator 产出的长篇笔记只存在于 `type-mapping.md` 与对应索引文件，`docs/meetings` 只记录结论/依赖变化，再配合 Info-Indexer 的摘要，保持核心文档正交且易于审计。

### Porter-CS
- Assessment: C# 侧目前只有 `PieceTreeBuffer` 的 `StringBuilder` 占位实现与 `Core/ChunkBuffer.cs`、`Core/PieceSegment.cs` 两个数据结构；真正的红黑树组织 (`PieceTreeNode`、sentinel、size/line metadata) 仍缺失，这让 `ApplyEdit` 只能做线性文本替换。PT-004 的首个交付需要在 `Core/` 下建立节点/树容器、暴露 `InsertPiece`/`DeleteSpan`/`LocateLineByOffset` API，并在 `PieceTreeBuffer` 中勾住这些入口以便稍后替换 Builder 与 Search 流程。
- Requests & dependencies: Investigator-TS 需在 PT-003 中补完 `pieceTreeTextBufferBuilder.ts`, `textModelSearch.ts`, `prefixSumComputer.ts` 的字段与回调顺序，才能确定我们在 C# 中维持哪些 size/line cache；Planner 请在 OI-003 模板里记录 PT-004 的两阶段拆分（节点骨架 -> 编辑入口绑定）以便我能并行编写 Core README 与 Porting Log；QA-Automation 希望锁定批量编辑与 snapshot API 以启动 PT-005 的属性测试矩阵，我会优先稳定 `ApplyEdit`, `FromChunks`, `EnumeratePieces`（新接口）并与 QA 协调断言钩子；DocMaintainer 预计在 PT-006/迁移日志模板中引用实现日志，我会把每次结构性变更登记到 `src/TextBuffer/README.md` 的 Porting Log 并抄送到 Task Board Notes。
- Manageability: 拟采用“Porting Log + Core README”双层结构记录每次树操作移植（字段/不变量/测试链接），并在 Task Board 备注中引用具体章节，避免会议纪要/Task Board/索引重复；同时会维护一个 `Core/TreeDebug.cs` 帮助 QA 检查黑高与中序顺序，减少调试噪音并为 DocMaintainer 提供可以引用的验证说明。

### QA-Automation
- Coverage status & needs: `tests/TextBuffer.Tests` 仍只有 `UnitTest1` 冒烟路径，当前未能覆盖 `InsertPiece`/`DeleteSpan`/批量编辑或任何基准。PT-005 需先交付 `TestMatrix.md`（按操作类型 × 数据量 × builder/search 入口列出预期断言），再拉通 FsCheck 属性测试和 BenchmarkDotNet 基线；若 Porter-CS 在 PT-004 阶段即可暴露 `ApplyEdit`/`EnumeratePieces`/`Snapshot` 草稿接口，我可以同步起草 property harness 并在 sprint 内跑一次 `dotnet test` + smoke benchmark，避免上线后补测。
- Dependencies & hand-offs: Porter-CS 提供的 API 面需标注可观察指标（piece count、line delta、RB tree black-height）以便测试钩子稳定；Investigator-TS 的类型映射请注明哪些缓存字段必须在 C# 端保持一致，否则无法生成属性断言；DocMaintainer 希望能在 PT-006 的迁移日志中预留“测试引用”段落让我回链每次验证；Info-Indexer 如能在首个索引里列出 QA 工件（TestMatrix、property proposal、benchmark README）的路径/更新时间，我就能停止在会议纪要重复链接。
- QA artifact discoverability: 建议将 QA 资产集中到 `tests/TextBuffer.Tests/TestMatrix.md`（概述 + 最新矩阵链接）并采用 `tests/benchmarks/*/RESULTS.md` 记录性能快照，主文档仅引用索引条目。Info-Indexer 可在 `agent-team/indexes/README.md` 内新增 “QA Assets” 子表，DocMaintainer 则在 Task Board Notes 中指向该索引，从而让 QA 细节可追踪但不挤占核心文档篇幅。

### DocMaintainer
- Load & capacity: 本 sprint 内 PT-006、OI-001 与 OI-004 全部落在 DocMaintainer 上，3 个 runSubAgent 配额已经占满，我只能每个任务投入一次集中的巡检/交付窗口；若出现额外文档请求，请由 Main Agent 在 Task Board 排期后再派发，以免核心文档检查被抢占。
- Migration/consistency/compression plan: PT-006 将在 `docs/migration-log.md` 启动“迁移日志 + 验证引用”模板，并把 QA-Automation 的断言链接纳入字段；OI-001 的正交性巡检会结合 Info-Indexer 的 `core-docs-index.md` diff，输出 `docs/reports/consistency/consistency-report-20251119.md` 的首份稽核；OI-004 则把 Task Board 拆成“PT/OI 核心”与“Reference”两段，并将冗长描述迁入索引或迁移日志，避免 AGENTS / Sprint / Meeting 三处重复。
- Expectations to stay orthogonal: Info-Indexer 需在每次索引更新后发布“新增/被压缩”清单并在 `agent-team/indexes/README.md` 标注，让我能在 OI-001 报告中直接引用；Planner 请在 runSubAgent 输入模板中加入“Doc touchpoints”段，强制调用者声明是否修改 AGENTS/Sprint/Task Board，以便我确认是否需要同步；Porter-CS、QA-Automation、Investigator-TS 若要引用长篇实现或测试细节，请优先写入索引或迁移日志并在会议纪要放指针，保持核心文档只承载结论与责任链。

### Info-Indexer
- Scope & deliverables: OI-002 将在 48 小时内交付 `agent-team/indexes/core-docs-index.md` v0，列出 AGENTS / Sprint / Meeting / Task Board 的职责、最近更新时间与压缩状态；同批次我会整理 QA-Automation 提供的测试资产清单，汇成 `qa-test-assets-index.md` 的首张表（接口、文件、负责人、复核节奏），以便 Task Board 只引用索引条目。
- Coordination: DocMaintainer 每次触发 OI-001 审计前可以直接 consume 我输出的“核心文档状态”行级 diff；Planner 在 OI-003 模板内加入 `Indexing Hooks` 段后，我会附上 copy-ready 片段，确保 runSubAgent prompt 自动携带最新索引路径；另外会和 QA-Automation 对齐资产表字段，并与 Investigator-TS 约定 `ts-cs-crosswalk` 索引的命名与存档节奏，避免在会议记录重复铺陈。
- Changefeed & delta summaries: 每个索引更新都会在 `agent-team/indexes/README.md` 新增 `Added / Compressed / Blocked` 三行 delta 摘要，并同步在会议或 Task Board 中只贴指针；长篇差异写在索引文件的 Update Log，DocMaintainer 仅需引用时间戳即可复核，从而保持核心文档正交又可追溯。
