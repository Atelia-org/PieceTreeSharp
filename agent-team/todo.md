# TODO Tree

> Team Leader 认知入口之一。以树形结构表达待完成事项的层次关系。
> 完成即删除，部分完成则替换为剩余子项。详细历史见 `docs/reports/migration-log.md`。

## Maintenance Rules
1. **只记录待完成**：已完成的条目立即删除，不留痕迹
2. **层次表达目标拆分**：粗粒度目标作为父节点，细粒度子任务缩进在下
3. **部分完成时**：删除已完成的子节点，保留未完成的；或将父节点替换为剩余工作描述
4. **上下文指针**：每个叶子节点应附带 handoff/changefeed/migration-log 引用
5. **同步规则**：完成某项后，按顺序更新 migration-log → changefeed → 删除本文件条目 → 同步 AGENTS/Sprint/Task Board

---

## Immediate (新会话优先)

- **半上下文压缩 PR 准备** (并行观察，无时间压力)
  - Upstream: `github.com/microsoft/vscode-copilot-chat`
  - 贡献指南: [`atelia-copilot-chat/CONTRIBUTING.md`](../atelia-copilot-chat/CONTRIBUTING.md)
  - 需要: Simulation tests cache (需 VS Code 团队成员重建)
  - 待观察: 实际使用中的 edge cases
  - 待补充: 测试、文档、代码规范

---

## Active Goals

- **Sprint 05: Diff → DocUI 渲染链路** (2025-12-02 ~ )
  - **M1 (Week 1): Diff 核心修复**
    - [x] LineSequence.GetBoundaryScore 修复 → ✅ Done (2025-12-02)
    - [ ] DiffMove.Flip() 补齐
    - [ ] RangeMapping.Inverse/Clip 实现
  - **M2 (Week 2): RangeMapping API 补齐**
    - [ ] RangeMapping.FromEdit/FromEditJoin
    - [ ] DetailedLineRangeMapping.ToTextEdit
    - [ ] Diff 回归测试扩展 (4 → 20+)
  - **M3 (Week 3): DocUI Diff 渲染**
    - [ ] DiffRenderer interface 设计
    - [ ] MarkdownRenderer DiffDecorations 集成
    - [ ] Inline diff 标记（add/del 行）
  - **M4 (Week 4): 集成与测试**
    - [ ] Move blocks 高亮（可选）
    - [ ] 全量回归测试
    - [ ] 对齐度目标: 56% → 65%

---

## 方向与决策的模糊之处

| 问题 | 说明 | 待决策 |
|------|------|--------|
| **DocUI diff 渲染深度** | VS Code 的 DiffEditorWidget 包含 moved blocks、折叠、revert 按钮等，是否全部复刻？ | 建议先实现最小 diff 渲染（add/del 行标记 + inline diff） |
| **ComputeMovedLines 启发式** | C# 版本有额外启发式，与 TS 输出可能不同 | 添加 parity 测试，明确是"增强"还是"偏差" |
| **Services 层深度** | Undo 服务、Language Configuration 等是否需要完整移植？ | 待 DocUI diff 落地后再评估 |

---

## Parking Lot (暂缓但需追踪)

- WS5 剩余 47 gaps (~106h) → 按 Top-10 优先级逐步消化
