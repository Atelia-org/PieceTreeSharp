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
  - **M1 (Week 1): Diff 核心修复** ✅ Done
  - **M2 (Week 2): RangeMapping API 补齐** ✅ Done
  - **M2.5: Diff 回归测试扩展** 🔄 进行中
    - [ ] UnchangedRegions 测试 (10 cases)
    - [ ] PostProcessCharChanges 测试 (5 cases)
    - [ ] 边界 cases（空文档、单行、大 diff）
    - [ ] 大文档性能基准 (10K/50K 行)
    - 目标: 40 → 55+ tests
  - **M3 (Week 3): DocUI Diff 渲染** ⏸️ 延后（需求待明确）
  - **M4 (Week 4): 集成与测试** ✅ 基本完成 (909 tests)

- **Sprint 05 Batch 2: 快速胜利任务** (可并行)
  - [ ] Diff 回归测试扩展 (~2h) → 15+ 新测试
  - [ ] validatePosition NaN/float 测试 (~1h)
  - [ ] 解除 SelectHighlightsAction skipped test (~2h)
  - [ ] 解除 MultiCursorSnippet skipped test (~2h)

---

## 已决策事项 (2025-12-02)

| 问题 | 决策 | 理由 |
|------|------|------|
| **DocUI diff 渲染深度** | ⏸️ **延后** | 缺乏需求调研，目前作为 headless 库，外层 DocUI 和 LLM Agent 对接未准备好。先完成 Diff 核心 API |
| **ComputeMovedLines 启发式** | ✅ **保留增强** | 如果已有实现优于原版，就尽量保留。这是处理 TS/C# 不一致的基本模式 |
| **Services 层深度** | ⏸️ **延后** | 待 DocUI diff 落地后再评估需求，避免过早设计 |

---

## Parking Lot (暂缓但需追踪)

### WS5 剩余 Gaps 清单 (2025-12-02 评估)

**原 47 gaps → 剩余 26 gaps (~42h)**，完成率 55%

#### P1 优先 (5 gaps, ~11h)
| Gap | 估计工时 | 依赖 | 状态 |
|-----|---------|------|------|
| TextModelData.fromString | 3h | 新建类 | 待实施 |
| AddSelectionToNextFindMatch | 4h | MultiCursorController | 待设计 |
| MultiCursor Snippet 集成 | 3h | CursorCollection | 待实施 |
| validatePosition (NaN/float) | 1h | 无 | 待实施 |

#### P2 优先 (12 gaps, ~20h)
- Snippet P3: nested/escape/inheritance (4 skipped tests)
- findController Mac clipboard/context keys
- bracketMatching pair colorization
- editStack undo/redo boundaries
- textChange operation merge

#### P3 低优先 (9 gaps, ~11h)
- WordOps edge cases (3 skipped tests: Issue51119/64810/74188)
- intervalTree TS parity
- columnSelection word wrap
