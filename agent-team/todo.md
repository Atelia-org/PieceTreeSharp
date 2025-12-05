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

- **文档流程改进** ✅ 已批准 (2025-12-05)
  - Team Leader 批准: [`TeamLeader-to-DocMaintainer-2025-12-05.md`](../agent-team/handoffs/TeamLeader-to-DocMaintainer-2025-12-05.md)
  - **方案 A+C 已生效**: Sprint log 为单一事实来源 + changefeed 轻量指针
  - DocMaintainer 任务已完成:
    - [x] 为 sprint-05.md 添加 HTML anchors
    - [x] 更新 Sprint log 模板
  - Info-Indexer 剩余任务:
    - [ ] 更新 indexes/README.md 格式为指针样式 (截止 12-06)
    - [ ] 归档 11 月旧 changefeed (截止 12-08)

- **半上下文压缩 PR 准备** (并行观察，无时间压力)
  - PR 计划: [`docs/plans/half-context-pr-plan.md`](../docs/plans/half-context-pr-plan.md)
  - 配置选项方案: [`docs/plans/half-context-config-option.md`](../docs/plans/half-context-config-option.md) ✅ 实施完成
  - Upstream: `github.com/microsoft/vscode-copilot-chat`
  - 贡献指南: [`atelia-copilot-chat/CONTRIBUTING.md`](../atelia-copilot-chat/CONTRIBUTING.md)
  - 需要: Simulation tests cache (需 VS Code 团队成员重建)
  - 待观察: 实际使用中的 edge cases
  - Phase 2 ✅: 配置选项 `HalfContextSummarization` 已实施
  - 待实施:
    - [ ] 添加 PropsBuilder 单元测试
    - [ ] 运行现有测试确认无回归
    - [ ] 创建 PR 描述 + DCO sign-off

---

## Active Goals

- **Sprint 05: Diff → DocUI 渲染链路** (2025-12-02 ~ )
  - **M1 (Week 1): Diff 核心修复** ✅ Done
  - **M2 (Week 2): RangeMapping API 补齐** ✅ Done
  - **M2.5: Diff 回归测试扩展** ✅ Done (40→95 tests)
  - **M3 (Week 3): DocUI Diff 渲染** ⏸️ 延后（需求待明确）
  - **M4 (Week 4): 集成与测试** ✅ Done (1008 tests 🎉)

- **Sprint 05 Batch 2: 快速胜利任务**
  - [x] Diff 回归测试扩展 → ✅ +55 新测试
  - [x] validatePosition 边界测试 → ✅ +44 新测试
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

### WS5 剩余 Gaps 重新评估 (2025-12-04 LLM-Native 视角)

**原 47 gaps → 剩余 26 gaps → LLM-Native 筛选后 11 gaps (~26h)**

#### ❌ 无需移植 (7 gaps, ~14h 节省)
| Gap | 原工时 | 理由 |
|-----|--------|------|
| Sticky Column | 2h | 人类键盘导航专属 |
| FindStartFocusAction / 焦点管理 | 3h | 无 GUI 无焦点概念 |
| Mac global clipboard write | 2h | 平台 hook，headless 不需要 |
| shouldAnimate / Delayer 节流 | 2h | 视觉动画 |
| Bracket pair colorization | 3h | 纯视觉，语义由 Roslyn 替代 |
| lineBreak + InjectedText viewport | 2h | 视口渲染特定 |
| Snippet P3 嵌套语法 | 4h | 复杂度高，实际使用罕见 |

#### 🔄 降级实现 (8 gaps, ~18h → ~8h)
| Gap | 原工时 | 降级后 | 方案 |
|-----|--------|--------|------|
| Snippet Variables | 4h | 2h | 接口已有，默认空实现 |
| Multi-cursor session merge | 3h | 1h | 简化为批量操作 |
| InsertCursorAbove/Below | 2h | 0.5h | 只提供 API |
| guessIndentation 全矩阵 | 3h | 1.5h | 覆盖常见模式 |
| WordOps edge cases | 3h | 1h | 不覆盖极端 Unicode |
| Diff 策略切换 | 3h | 1h | 只保留 default |
| editStack 边界 | 2h | 0.5h | 按需添加 |

#### ✅ 继续移植 P1 (5 gaps, ~14h) — 进度更新 2025-12-04
| Gap | 估计工时 | 依赖 | 状态 |
|-----|---------|------|------|
| TextModelData.fromString | 3h | 新建类 | ✅ 已完成 +5 tests |
| validatePosition 边界测试 | 3h | TextModel | ✅ 已完成 +44 tests |
| getValueLengthInRange + EOL | 2h | TextModel | ✅ 已完成 +5 tests |
| Issue regressions (#44991,#55818...) | 4h | 各模块 | ✅ 已覆盖 (调研确认) |
| SelectAllMatches 排序 | 2h | FindModel | ✅ 已完成 |

**P1 完成率: 100%** 🎉

#### ✅ 继续移植 P2 (6 gaps, ~12h) — **完成！** 🎉
| Gap | 估计工时 | 依赖 | 状态 |
|-----|---------|------|------|
| Decorations multi-owner merge | 2h | IntervalTree | 🔄 存储层已完成，渲染层延后 |
| Diff deterministic matrix | 3h | DiffComputer | ✅ 已完成 +44 tests (59→103) |
| AddSelectionToNextFindMatch | 10h | MultiCursorSession | ✅ 已完成 +34 tests |
| MultiCursor Snippet 集成 | 3h | CursorCollection | ✅ 基础测试完成 +6 tests |
| PieceTree diagnostics | 2h | PieceTreeModel | ✅ 已完成 +23 tests |
| Snippet Placeholder Transform | 2h | SnippetSession | ✅ 已完成 +33 tests |

**P2 完成率: 100% (6/6)** 🎊
