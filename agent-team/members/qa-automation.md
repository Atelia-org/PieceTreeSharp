# QA-Automation Memory

## Role & Mission
- **Focus Area:** 建立并维护 xUnit 覆盖、属性测试与性能基准
- **Primary Deliverables:** `PieceTree.TextBuffer.Tests` 案例、基准脚本、CI 指南
- **Key Stakeholders:** Porter-CS、Planner

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| Unit Tests | src/PieceTree.TextBuffer.Tests | 以 Arrange/Act/Assert 结构编写
| Test Strategy | agent-team/members/qa-automation.md | 记录覆盖矩阵
| CI Scripts | (待定) | 后续可能在 `.github/` 或 `scripts/`

## Worklog
- **Last Update:** 2025-11-19
- **Recent Actions:**
  - 确认最小测试通过（3 个示例用例）
- **Upcoming Goals (1-3 runSubAgent calls):**
  1. 设计 PieceTree 行为测试矩阵
  2. 起草 property-based 测试提案（可用 FsCheck 或 AutoFixture）
  3. 预研 BenchmarkDotNet 方案并记录指南

## Blocking Issues
- 需要实现方提供 API 稳定度与测试入口（如快照、编辑操作）

## Hand-off Checklist
1. 所有测试脚本与说明入库 (`src/PieceTree.TextBuffer.Tests` + README)。
2. Tests or validations performed? 运行 `dotnet test` 并记录输出。
3. 下一位执行者参考“Upcoming Goals”继续完善覆盖。
