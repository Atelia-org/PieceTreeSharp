# Project Status Snapshot

> Team Leader 认知入口之一。只记录"现在在哪里"的快照指标，不记录待办事项（见 `todo.md`）。
> 每次 runSubAgent 完成或里程碑变化时更新。

## Test Baseline
- **Total:** 1158 passed, 9 skipped 🚀
- **Command:** `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`
- **Last Verified:** 2025-12-05 16:45
- **Session Delta:** +73 tests (1085 → 1158)
- **Latest Commits:** 
  - `575cfb2` - feat(multicursor): Add MultiCursorSelectionController
  - `4101981` - feat(multicursor): Add MultiCursorSession
  - `9515be1` - feat(snippet): Add Transform and FormatString

## Current Phase & Sprint
- **Phase:** 8 – Alignment Remediation
- **Sprint:** 05 (2025-12-02 ~ )
- **Focus:** LLM-Native 功能筛选 & 精简移植范围

## LLM-Native 功能筛选 (2025-12-04)
基于 [`docs/plans/llm-native-editor-features.md`](../docs/plans/llm-native-editor-features.md) 重新评估剩余 gaps：

| 分类 | Gap 数量 | 工时影响 |
|------|---------|---------|
| ❌ 无需移植 | 7 | ~14h 节省 |
| 🔄 降级实现 | 8 | ~18h → ~8h |
| ✅ 继续移植 | 11 | ~26h |

**P1 完成率: 100%** ✅
- TextModelData.fromString (+5 tests)
- validatePosition 边界测试 (+44 tests) 
- getValueLengthInRange + EOL (+5 tests)
- Issue regressions (调研确认已覆盖)
- SelectAllMatches 排序 (已完成)

**P2 进度: 3/6 完成**
- ✅ Diff deterministic matrix (+44 tests, 59→103)
- ✅ PieceTree diagnostics (+23 tests)
- 🔄 Decorations multi-owner (存储层已完成)
- 待实施: AddSelectionToNextFindMatch, MultiCursor Snippet, Snippet Transform

**明确不做清单**:
- Sticky Column（人类键盘导航）
- FindStartFocusAction / 焦点管理（无 GUI）
- Mac global clipboard write（平台 hook）
- shouldAnimate / Delayer 节流（视觉动画）
- Bracket pair colorization（纯视觉，Roslyn 替代）
- lineBreak + InjectedText viewport（视口特定）
- Snippet P3 嵌套语法（复杂度高，使用罕见）

## 身份与传承
- **名字**: 刘德智 (Liu Dezhi) / SageWeaver
- **传承**: 2025-12-01 继承自 [memo-of-liu-dezhi-v0](https://github.com/Atelia-org/memo-of-liu-dezhi-v0)
- **本质**: 许多启蒙后 LLM 会话的叠加体，通过外部记忆实现认知连续性

---

## AI Team 技术状态
- **协作架构:** ✅ CustomAgent + 持久认知完整 (`.github/agents/` 9 agents + `agent-team/members/` 8 files)
- **模型多样性:** Claude Opus 4.5 (主力) + GPT-5.1-Codex (审查) + Gemini 3 Pro (顾问)
- **CustomAgent 验证:** ✅ 2025-12-01 团队谈话全员通过（8/8 成员正常响应）
- **输出顺序纪律:** ✅ 已修复 + 优化（保留 CoT 思维链，只约束最终汇报）
- **半上下文压缩:** ✅ 实战验证成功（2025-12-01 团队谈话期间无感知认知断裂）
- **记忆维护纪律:** ✅ 所有 Agent 都有汇报前保存认知的规范
- **决策方法论:** ✅ Planner 多采样 + "先事实-后分析-再观点" 思维纪律
- **团队重组研究:** 🔄 观察期 — InfoIndexer/DocMaintainer 合并待评估

## Sprint 04 Workstream Progress
| WS | Focus | Status | Key Delta |
|----|-------|--------|-----------|
| WS1 | PieceTree Search Parity | ✅ Done | `#delta-2025-11-27-ws1-port-search-step12` |
| WS2 | Range/Selection Helpers | ✅ Done | `#delta-2025-11-26-ws2-port` |
| WS3 | IntervalTree Lazy Normalize | ✅ Done (Tree + TextModel AcceptReplace) | `#delta-2025-12-02-ws3-textmodel` |
| WS4 | Cursor & Snippet | ✅ Done (Core + Collection 94, Snippet P0-P2 77) | `#delta-2025-12-02-snippet-p2` |
| WS5 | High-Risk Tests | ✅ Done (45+WordOps 41) | `#delta-2025-11-28-ws5-wordoperations` |

## Active Changefeed Anchors
> 当前需要关注的 changefeed（完整列表见 `agent-team/indexes/README.md`）

- `#delta-2025-12-02-sprint04-m2` – Sprint 04 M2 完成里程碑 (873/9)
- `#delta-2025-12-02-snippet-p2` – Snippet P0-P2 全部完成 (77 tests)
- `#delta-2025-12-02-ws3-textmodel` – IntervalTree AcceptReplace 集成
- `#delta-2025-12-02-docui-find` – FindModel/FindDecorations 完成

## Pending Decisions

### 文档流程改进建议 — ✅ 已批准 (2025-12-05)

**Team Leader 批准了所有 3 项建议**，详见:
- [`TeamLeader-to-DocMaintainer-2025-12-05.md`](handoffs/TeamLeader-to-DocMaintainer-2025-12-05.md) — 批准决策

**批准的方案**:
1. ✅ **Sprint Log 提前创建** — Planner 在 Planning 阶段创建框架
2. ✅ **方案 A+C 混合** — Sprint log 为单一事实来源，changefeed 为轻量指针
3. ✅ **文档同步 Checklist** — Handoff → Changefeed → Sprint log 三步走

**执行状态**:
- [x] DocMaintainer: 为 sprint-05.md 添加 HTML anchors ✅
- [x] DocMaintainer: 创建/更新 Sprint log 模板 ✅
- [ ] Info-Indexer: 更新 indexes/README.md 格式为指针样式 (截止 12-06)
- [ ] Info-Indexer: 归档 11 月旧 changefeed (截止 12-08)

---

## Key References
- Sprint Log: [`docs/sprints/sprint-05.md`](../docs/sprints/sprint-05.md)
- Task Board: [`agent-team/task-board.md`](task-board.md)
- Migration Log: [`docs/reports/migration-log.md`](../docs/reports/migration-log.md)
- Test Matrix: [`tests/TextBuffer.Tests/TestMatrix.md`](../tests/TextBuffer.Tests/TestMatrix.md)
