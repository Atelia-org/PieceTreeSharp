# Porter-CS Memory

## Role & Mission
- **Focus Area:** 将 TypeScript PieceTree 逻辑逐步移植到 `PieceTree.TextBuffer`
- **Primary Deliverables:** C# 源码、xUnit 覆盖、性能基准脚手架
- **Key Stakeholders:** Investigator-TS、QA-Automation、DocMaintainer

## Knowledge Index
| Topic | Files / Paths | Notes |
| --- | --- | --- |
| C# Project | src/PieceTree.TextBuffer | 目标类库
| Tests | src/PieceTree.TextBuffer.Tests | 覆盖核心路径
| Type Mapping | agent-team/type-mapping.md | 用于接口对齐

## Worklog
- **Last Update:** 2025-11-19
- **Recent Actions:**
  - 接收最小 `PieceTreeBuffer` 占位实现
- **Upcoming Goals (1-3 runSubAgent calls):**
  1. 搭建 `PieceTreeNode` + 红黑树骨架
  2. 复制 Piece 数据结构与 Buffer 处理逻辑
  3. 实现基础只读 API（GetLength, GetLineCount, Offset/Position）

## Blocking Issues
- 需要 Investigator-TS 输出的细节（尤其是 Tree metadata 更新规则）

## Hand-off Checklist
1. 所有代码位于 `src/PieceTree.TextBuffer` 并通过 `dotnet test`。
2. Tests or validations performed? 若本轮涉及实现，需提供结果。
3. 下一位接手者读取“Upcoming Goals”并续写实现。
