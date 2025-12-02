# Decorations 模块对齐审查报告

**审查日期:** 2025-12-02 (Sprint 04 M2 更新)  
**审查范围:** IntervalTree、DocUI decorations 与 renderer（7 个组件）

## 概要

> ✅ **Sprint 04 M2 重大进展：** IntervalTree `AcceptReplace()` 四阶段算法已完全集成，`DecorationOwnerIds` 语义已修正，测试基线达到 **15 passed**（IntervalTreeTests）。

- **IntervalTree AcceptReplace 已完成：** `WS3-PORT-Tree` 在 `src/TextBuffer/Decorations/IntervalTree.cs` 投入 ~1470 行 TS 风格重写（NodeFlags、lazy delta、4 步 `AcceptReplace()`、DEBUG counters），全量回归通过。
- **DecorationOwnerIds 语义已修正：** `Default=0` 现在视为 "Any"，查询 API 已统一。
- **DecorationsTrees 过滤开关：** `filterOutValidation`、`onlyMarginDecorations` 等参数已暴露。
- **DocUI stickiness/find 装饰流程：** 经 `B3-Decor-Stickiness-Review` 验证，`DocUIFindDecorationsTests`/`DecorationStickinessTests` 现验证 owner 申请、viewport 节流与 scope 追踪。
- **对齐状态：** ✅ 5 / ⚠️ 2 / ❌ 0

## 详细分析

### 1. IntervalTree.cs（WS3-PORT-Tree） — ✅
- `WS3-PORT-Tree` 完成 TS `intervalTree.ts` 的 lazy normalize 改写，采纳 NodeFlags、Sentinel、防溢出的 delta 累加、`RequestNormalize()/NormalizeDelta()` 以及四阶段 `AcceptReplace()`；细节见 [`agent-team/handoffs/WS3-PORT-Tree-Result.md`](../../agent-team/handoffs/WS3-PORT-Tree-Result.md)。
- `docs/reports/migration-log.md#ws3-port-tree` 记录 1470 行替换与 DEBUG 计数器 (`NodesRemovedCount`, `RequestNormalizeHits`)，同时把 440/440 回归与 Sprint Phase 8 汇总（[`#delta-2025-11-26-sprint04-r1-r11`](../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11)）绑定。
- 后续只剩 Sentinel 去共享（`IntervalTree-StackFix-Result.md` 追踪）以及把树级 `AcceptReplace()` 直接暴露给 `DecorationsTrees` 以取代额外遍历。

### 2. DecorationRangeUpdater + stickiness suites — ✅
- Stickiness 迁移结合 `DecorationRangeUpdater.ApplyEdit` 与 `forceMoveMarkers`，经 [`B3-Decor-Stickiness-Review.md`](../../agent-team/handoffs/archive/B3-Decor-Stickiness-Review.md) 验证：DocUI 现在实时读取装饰范围、保留换行、依据 viewport 计算 overview throttle，并为每个 `FindDecorations` 实例申请 ownerId。
- `tests/TextBuffer.Tests/DecorationStickinessTests.cs` 与 `DocUI/DocUIFindDecorationsTests.cs`（见 [`docs/reports/migration-log.md#b3-decor`](../../docs/reports/migration-log.md#b3-decor)）覆盖所有四种 stickiness + viewport 节流回归，确保 `WS3` 栈提供的 NodeFlags 可被 DocUI 消费。

### 3. DecorationOwnerIds 与查询过滤 — ✅ 完成 (Sprint 04 M2)
- `TextModel.GetDecorationsInRange`/`GetLineDecorations` 现在正确处理 `DecorationOwnerIds.Any(0)` 代表“不过滤”的语义。
- `Default=0` 视为 Any，`_nextDecorationOwnerId` 从 1 起步。
- DocUI 仍依赖 `AllocateDecorationOwnerId()` 避开碰撞，语义一致。

### 4. DecorationsTrees 过滤开关与 metadata — ✅ 完成 (Sprint 04 M2)
- IntervalTree 现在持有 NodeFlags（validation/minimap/margin/font/stickiness）。
- `DecorationsTrees.Search()` 已支持 `filterOutValidation`、`onlyMarginDecorations`、`filterFontDecorations` 等参数。
- `TextModel.GetDecorationsInRange`、`GetFontDecorationsInRange` 已暴露 NodeFlags 过滤。

### 5. ModelDecoration 常量与枚举 — ⚠️
- `ModelDecoration.LineHeightCeiling` 仍为 1500（TS 是 300），`MinimapPosition`/`GlyphMarginLane`/`InjectedTextCursorStops` 数值也与 TS 不同；一旦 renderer/JSON 持久化启用，就会导致协议不一致。
- `ModelDecorationMinimapOptions.SectionHeaderStyle` 仍是任意字符串；TS 版本使用枚举，便于 host/renderer 分支控制。需在 CL8 收敛前对齐。

### 6. DocUI Find 栈（FindDecorations/FindModel/DocUIFindController） — ✅
- `docs/reports/migration-log.md#b3-decor` 与 [`agent-team/handoffs/B3-Decor-PORTER.md`](../../agent-team/handoffs/B3-Decor-PORTER.md) 记录的改动现已落地：DocUI 小部件一次性完成 scope 追踪、overview 节流、命令/剪贴板管道，并借助 `TextModel.AllocateDecorationOwnerId()` 隔离 owner。
- `DocUIFindDecorationsTests`、`DocUIFindModelTests`、`DocUIFindControllerTests`（含 26+ 测试）已纳入 TestMatrix；Stickiness/viewport/ReplaceAll 行为在 B3 handoff 中有明确验证。

### 7. DocUI renderer / Markdown overlays / Intl & word cache — ⚠️ 待接入
- DocUI renderer 已可消费搜索装饰（`FindDecorations`），但 Markdown diff 渲染、Intl 分词、word cache 尚未串联。
- CL8 占位仍有效：[`#delta-2025-11-26-aa4-cl8-markdown`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)、[`#delta-2025-11-26-aa4-cl8-capture`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-capture)、[`#delta-2025-11-26-aa4-cl8-intl`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-intl)、[`#delta-2025-11-26-aa4-cl8-wordcache`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-wordcache) 继续追踪。
- Renderer 需要在 CL8 drop 中直接读取 `FindDecorations` 的 owner + metadata。

## 修正优先级

> ✅ **Sprint 04 M2 已关闭：**
> - ~~统一 `DecorationOwnerIds` 语义~~ → 已完成
> - ~~为 `DecorationsTrees` 暴露 TS 同款过滤开关~~ → 已完成

1. 对齐 `ModelDecoration` 枚举/常量数值（line height ceiling、glyph lanes、cursor stops）并文档化 `MinimapSectionHeaderStyle` 枚举，防止 renderer/API 不兼容。
2. CL8 Renderer：把 `FindDecorations` 接入 Markdown/DocUI diff renderer。

## Verification Notes

- **2025-12-02 (Sprint 04 M2)**：
  - `dotnet test --filter IntervalTreeTests --nologo` → **15/15 passed**
  - `dotnet test --filter "DocUIFindDecorationsTests|DecorationStickinessTests" --nologo` → **13/13 passed**
  - 全量基线：**873 passed, 9 skipped**
- **2025-11-27**：`IntervalTreeTests` (13/13) 与 `IntervalTreePerfTests` (7/7) — 见 `WS3-QA-Result.md`，覆盖 lazy normalize + DEBUG counter 验证。
