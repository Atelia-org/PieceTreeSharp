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
| QA Test Matrix (planned) | src/PieceTree.TextBuffer.Tests/TestMatrix.md | PT-005 交付，按 Piece/Range/Edit 维度记录覆盖
| Property Tests (planned) | src/PieceTree.TextBuffer.Tests/PropertyBased/ | 拟用 FsCheck/AutoFixture 落地随机编辑验证
| Benchmarks (planned) | tests/benchmarks/PieceTree.Benchmarks | BenchmarkDotNet 项目与结果快照
| Test Strategy Canon | agent-team/members/qa-automation.md | 当前文件，集中策略/日志
| Org Index References | agent-team/indexes/README.md | 未来由 Info-Indexer 汇总 QA 资产位置

## Worklog
- **2025-11-19:** 完成入职材料审阅、评估现有 xUnit 骨架、确认 PT-005/OI-005 对 QA 的期望输出与文档落点。
- **2025-11-19:** 参加 Org Self-Improvement 会议，通报测试矩阵/属性测试缺口，确认对 Porter-CS API、Investigator-TS 类型映射与 Info-Indexer 索引结构的依赖，并提出 QA 产物索引化方案。

- **Upcoming Goals (1-3 runSubAgent calls):**
1. **PT-005.G1**：生成 `TestMatrix.md`（覆盖 Piece/Insert/Delete/Undo/Decoration）并校验与 Planner 的 Sprint-00 验收一致。
2. **PT-005.G2**：提交 FsCheck 驱动的属性测试提案（含 API 依赖、样例伪代码、落地步骤），等待 Porter-CS 暴露 `ApplyEdit`/`EnumeratePieces` 草稿接口即可启动。
3. **OI-SUPPORT.G1**：与 Info-Indexer 对齐 QA 资产索引写入 Convention（索引引用 + 产物路径），确保 Task Board 精简后仍可定位测试与基准文件。

## Blocking Issues
- Porter-CS 需明确 PieceTreeBuffer API 稳定面（批量编辑、片段导出、Snapshot/EnumeratePieces）以便属性测试生成器固定断言钩子。
- Investigator-TS 需要在类型映射中标注 C# 侧必须保持的缓存字段（line count、accumulated length等），否则 QA 无法制定属性断言。
- Info-Indexer 待交付索引模板与 QA section 占位，才能将 QA 资产挂载至 org 索引，暂以本记忆文件记录位置。

## Recording Notes
- QA 测试矩阵放置于 `src/PieceTree.TextBuffer.Tests/TestMatrix.md`（运行日志亦附表）。
- Benchmark 计划与结果将位于 `tests/benchmarks/PieceTree.Benchmarks/README.md`，必要时引用 BenchmarkDotNet 工具输出。

## Hand-off Checklist
1. 所有测试脚本与说明入库 (`src/PieceTree.TextBuffer.Tests` + README)。
2. Tests or validations performed? 运行 `dotnet test` 并记录输出。
3. 下一位执行者参考“Upcoming Goals”继续完善覆盖。
