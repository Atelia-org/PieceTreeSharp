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

## Active Goals

- **Sprint 04 M2: Cursor & Snippet 完整实现** → [`#delta-2025-11-26-aa4-cl7-cursor-core`](indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core)
  - WS4-PORT-Collection: CursorCollection 完整生命周期 → ✅ Done (`#delta-2025-11-28-sprint04-r13-r18`)
  - WS4-PORT-Snippet: SnippetController/Session parity
    - choice/variable/transform 占位符支持
    - 多光标粘附与 undo/redo 集成
    - → context: [`AA4-007-Plan.md`](handoffs/AA4-007-Plan.md)
  - WS4-QA: Cursor/Snippet deterministic 测试套件 (80% TS coverage)

- **Sprint 04 M2: DocUI MarkdownRenderer 完善** → [`#delta-2025-11-26-aa4-cl8-markdown`](indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)
  - CL8-Phase34 基础枚举与 FindDecorations 集成 → ✅ Done (`#delta-2025-11-28-cl8-phase34`)
  - Intl.Segmenter word segmentation 适配
  - Decoration ingestion 完善（owner filter, metadata）

- **Changefeed 历史清理** (低优先级，可批量处理)
  - PT-004 系列 (LineInfra/Positions/Edit) 发布正式 anchor
  - PT-005.Search anchor
  - PT-008.Snapshot anchor
  - PT-009.LineOpt anchor
  - → context: [`docs/reports/migration-log.md`](../docs/reports/migration-log.md) "Active Items"

- **WS3-PORT-TextModel**: IntervalTree 集成到 TextModel
  - DecorationsTrees 接入 lazy normalize
  - `AcceptReplace` 替代 `AdjustDecorationsForEdit`
  - DocUI perf harness (50k decorations O(log n))
  - → context: [`PORT-IntervalTree-Normalize.md`](handoffs/PORT-IntervalTree-Normalize.md)

---

## Parking Lot (暂缓但需追踪)

- WordSeparator cache/perf backlog → 待 Intl.Segmenter 研究后决定
- WS5 剩余 47 gaps (~106h) → 按 Top-10 优先级逐步消化
