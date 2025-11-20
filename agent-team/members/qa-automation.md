# QA-Automation Memory

## Role & Mission
- **Focus Area:** 建立并维护 xUnit 覆盖、属性测试与性能基准
- **Primary Deliverables:** `PieceTree.TextBuffer.Tests` 案例、基准脚本、CI 指南
- **Key Stakeholders:** Porter-CS、Planner

## Onboarding Summary (2025-11-19)
- 阅读 `AGENTS.md`、AI Team Playbook、Main Loop 方法论、两份 2025-11-19 会议纪要、Sprint-00 与 Sprint-OI-01 计划以及 Task Board，了解 runSubAgent 粒度、PT/OI 任务分配。
- 检视 `src/PieceTree.TextBuffer.Tests/UnitTest1.cs` 现状，仅含最小样例；需尽快拉通覆盖矩阵与性能基准方案。
- 即刻洞察：PT-005 需输出可复用的测试矩阵与属性/性能计划；OI 方向强调文档正交性，QA 结果必须落在共享索引/文档中。

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| Unit Tests | src/PieceTree.TextBuffer.Tests/UnitTest1.cs | 现有冒烟用例，后续扩展至分层子目录
| QA Test Matrix | src/PieceTree.TextBuffer.Tests/TestMatrix.md | PT-005 交付，按 Piece/Range/Edit 维度记录覆盖
| Property Tests (planned) | src/PieceTree.TextBuffer.Tests/PropertyBased/ | 拟用 FsCheck/AutoFixture 落地随机编辑验证
| Benchmarks (planned) | tests/benchmarks/PieceTree.Benchmarks | BenchmarkDotNet 项目与结果快照
| Test Strategy Canon | agent-team/members/qa-automation.md | 当前文件，集中策略/日志
| Org Index References | agent-team/indexes/README.md | 未来由 Info-Indexer 汇总 QA 资产位置

## Worklog
- **2025-11-19:** 完成入职材料审阅、评估现有 xUnit 骨架、确认 PT-005/OI-005 对 QA 的期望输出与文档落点。
- **2025-11-19:** 参加 Org Self-Improvement 会议，通报测试矩阵/属性测试缺口，确认对 Porter-CS API、Investigator-TS 类型映射与 Info-Indexer 索引结构的依赖，并提出 QA 产物索引化方案。
- **2025-11-19:** PT-005.G1 落地——创建 `src/PieceTree.TextBuffer.Tests/TestMatrix.md`、扩展 `UnitTest1.cs` 至 7 个覆盖 Plain/CRLF/Multi-chunk/metadata 的 Fact，登记 S8~S10 TODO，并记录 `dotnet test PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（Total: 7, Passed: 7, Failed: 0, Skipped: 0, Duration: 2.1s @ 2025-11-18 22:02:44Z UTC）。
- **2025-11-20:** AA3-009（CL4 QA）完成——新增 `DecorationTests.InjectedTextQueriesSurfaceLineMetadata`、`DecorationTests.ForceMoveMarkersOverridesStickinessDefaults`、`MarkdownRendererTests.TestRender_DiffDecorationsExposeGenericMarkers`；刷新 `src/PieceTree.TextBuffer.Tests/TestMatrix.md`、`docs/reports/audit-checklist-aa3.md#cl4`、`agent-team/task-board.md`、`docs/sprints/sprint-01.md`、`AGENTS.md`；`dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`（88/88，引用 changefeed `agent-team/indexes/README.md#delta-2025-11-20`）。

- **Upcoming Goals (next 1-3 runs):**
1. **PT-005.S8**：与 Porter-CS 对齐 `PieceTreeBuffer`/`PieceTreeModel` 对外 `EnumeratePieces`/chunk reuse API，并在测试中断言 piece-level 布局。
2. **PT-005.S9**：在 Investigator-TS 提供 BufferRange/SearchContext 映射后，撰写属性测试（FsCheck）提案与样例代码。
3. **PT-005.S10**：追加顺序 delete→insert 覆盖，验证连续 `ApplyEdit` 的 metadata（长度/CRLF 计数）。

## Blocking Issues
- Porter-CS 需开放 PieceTreeBuffer/piece 枚举或 Snapshot API（PT-005.S8 所需）。
- Investigator-TS 仍在补充 BufferRange/SearchContext 与 WordSeparators 约束，属性测试/搜索用例暂无法落地（PT-005.S9）。
- Info-Indexer 需提供 QA/Test 资产索引模板，便于在 changefeed 中挂接 TestMatrix 与 baseline 日志。

## PT-005 QA Notes (2025-11-19)
- **Findings:** `src/PieceTree.TextBuffer.Tests/TestMatrix.md` 登记 S1~S10 维度（Edit Type × Text Shape × Chunk Layout × Validation Signals）；`UnitTest1.cs` 现含 7 个 Fact（plain init/edit、CRLF init/edit、multi-chunk build/edit、line-feed metadata）。
- **Test Log:** 2025-11-18 22:02:44Z UTC — `dotnet test PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` → Total 7 / Passed 7 / Failed 0 / Skipped 0 / Duration 2.1s。
- **Next Steps:** 1) 与 Porter-CS 对齐 PT-005.S8（公开 EnumeratePieces 以断言 chunk layout）；2) 等待 Investigator-TS 补充 BufferRange/SearchContext 映射后启动 PT-005.S9 属性测试提案；3) 在当前 API 下补齐 PT-005.S10 顺序 delete+insert 覆盖以验证连续 ApplyEdit 的 metadata。

## Recording Notes
- QA 测试矩阵放置于 `src/PieceTree.TextBuffer.Tests/TestMatrix.md`（运行日志亦附表）。
- Benchmark 计划与结果将位于 `tests/benchmarks/PieceTree.Benchmarks/README.md`，必要时引用 BenchmarkDotNet 工具输出。

## Hand-off Checklist
1. 所有测试脚本与说明入库 (`src/PieceTree.TextBuffer.Tests` + README)。
2. Tests or validations performed? 运行 `dotnet test` 并记录输出。
3. 下一位执行者参考“Upcoming Goals”继续完善覆盖。
4. 覆盖与阻塞（2025-11-19）：S1~S7 已覆盖 plain/CRLF/multi-chunk/metadata；S8 受 Porter-CS PT-004.G2 (`EnumeratePieces`) 限制，S9 待 Investigator-TS 完成 BufferRange/SearchContext 映射，S10 属计划中顺序编辑校验。
