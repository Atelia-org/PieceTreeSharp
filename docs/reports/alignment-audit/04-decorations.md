# Decorations 模块对齐审查报告

**审查日期:** 2025-11-27  
**审查范围:** IntervalTree、DocUI decorations 与 renderer（7 个组件）

## 概要
- [`WS3-PORT-Tree`](../../docs/reports/migration-log.md#ws3-port-tree) / [`#delta-2025-11-26-sprint04-r1-r11`](../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 在 `src/TextBuffer/Decorations/IntervalTree.cs` 投入 ~1470 行 TS 风格重写（NodeFlags、lazy delta、4 步 `AcceptReplace()`、DEBUG counters）并通过 440/440 全量回归，使树级别语义与 TS 持平。
- DocUI stickiness / find 装饰流程已由 [`B3-Decor-Stickiness-Review.md`](../../agent-team/handoffs/archive/B3-Decor-Stickiness-Review.md) + [`B3-Decor-PORTER.md`](../../agent-team/handoffs/B3-Decor-PORTER.md) 覆盖，`DocUIFindDecorationsTests`/`DecorationStickinessTests` 现验证 owner 申请、viewport 节流与 scope 追踪。
- 仍待解决：`DecorationOwnerIds.Default` 与 `Any` 语义不一致、`DecorationsTrees` 尚未公开 TS 过滤开关，以及 DocUI renderer 尚未消化 markdown/capture/intl/wordcache 装饰；这些 Gap 继续锚定 [`#delta-2025-11-26-aa4-cl8-markdown`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)、[`#delta-2025-11-26-aa4-cl8-capture`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-capture)、[`#delta-2025-11-26-aa4-cl8-intl`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-intl)、[`#delta-2025-11-26-aa4-cl8-wordcache`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-wordcache)。
- 对齐状态：✅ 4 / ⚠️ 2 / ❌ 1。

## 详细分析

### 1. IntervalTree.cs（WS3-PORT-Tree） — ✅
- `WS3-PORT-Tree` 完成 TS `intervalTree.ts` 的 lazy normalize 改写，采纳 NodeFlags、Sentinel、防溢出的 delta 累加、`RequestNormalize()/NormalizeDelta()` 以及四阶段 `AcceptReplace()`；细节见 [`agent-team/handoffs/WS3-PORT-Tree-Result.md`](../../agent-team/handoffs/WS3-PORT-Tree-Result.md)。
- `docs/reports/migration-log.md#ws3-port-tree` 记录 1470 行替换与 DEBUG 计数器 (`NodesRemovedCount`, `RequestNormalizeHits`)，同时把 440/440 回归与 Sprint Phase 8 汇总（[`#delta-2025-11-26-sprint04-r1-r11`](../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11)）绑定。
- 后续只剩 Sentinel 去共享（`IntervalTree-StackFix-Result.md` 追踪）以及把树级 `AcceptReplace()` 直接暴露给 `DecorationsTrees` 以取代额外遍历。

### 2. DecorationRangeUpdater + stickiness suites — ✅
- Stickiness 迁移结合 `DecorationRangeUpdater.ApplyEdit` 与 `forceMoveMarkers`，经 [`B3-Decor-Stickiness-Review.md`](../../agent-team/handoffs/archive/B3-Decor-Stickiness-Review.md) 验证：DocUI 现在实时读取装饰范围、保留换行、依据 viewport 计算 overview throttle，并为每个 `FindDecorations` 实例申请 ownerId。
- `tests/TextBuffer.Tests/DecorationStickinessTests.cs` 与 `DocUI/DocUIFindDecorationsTests.cs`（见 [`docs/reports/migration-log.md#b3-decor`](../../docs/reports/migration-log.md#b3-decor)）覆盖所有四种 stickiness + viewport 节流回归，确保 `WS3` 栈提供的 NodeFlags 可被 DocUI 消费。

### 3. DecorationOwnerIds 与查询过滤 — ⚠️
- `TextModel.GetDecorationsInRange`/`GetLineDecorations` 预期调用方传入 `DecorationOwnerIds.Any(-1)` 代表“不过滤”，但 TS 调用习惯是传 0；若 Port 侧沿用 0，当前实现只会返回 owner==0 的装饰。`WS3-PORT-Tree-Result.md`/`B3-Decor-Stickiness-Review.md` 已记录此风险，DocUI 依赖 `AllocateDecorationOwnerId()` 避开碰撞，但 API 仍会让未来的 view model/renderer 调用踩坑。
- 建议把 `Default (0)` 视为 Any（或直接把 Any=0，并把 `_nextDecorationOwnerId` 从 2 起步），并在 `TextModel` 层补充“不过滤”路径；在完成前，该项保持 ⚠️。

### 4. DecorationsTrees 过滤开关与 metadata — ⚠️
- 虽然 IntervalTree 现在持有 NodeFlags（validation/minimap/margin/font/stickiness），`DecorationsTrees.Search()` 仍只允许 ownerId 过滤。TS 的 `filterOutValidation`、`onlyMarginDecorations`、`filterFontDecorations` 等参数尚未移植，因此 DocUI/diff renderer 仍需手动遍历并丢弃不需要的装饰。
- `WS3-PORT-Tree` 的 metadata 尚未暴露到 `DecorationsTrees`, `TextModel.GetDecorationsInRange`, `GetFontDecorationsInRange`，导致 CL8 DocUI 任务无法直接消费 NodeFlags。

### 5. ModelDecoration 常量与枚举 — ⚠️
- `ModelDecoration.LineHeightCeiling` 仍为 1500（TS 是 300），`MinimapPosition`/`GlyphMarginLane`/`InjectedTextCursorStops` 数值也与 TS 不同；一旦 renderer/JSON 持久化启用，就会导致协议不一致。
- `ModelDecorationMinimapOptions.SectionHeaderStyle` 仍是任意字符串；TS 版本使用枚举，便于 host/renderer 分支控制。需在 CL8 收敛前对齐。

### 6. DocUI Find 栈（FindDecorations/FindModel/DocUIFindController） — ✅
- `docs/reports/migration-log.md#b3-decor` 与 [`agent-team/handoffs/B3-Decor-PORTER.md`](../../agent-team/handoffs/B3-Decor-PORTER.md) 记录的改动现已落地：DocUI 小部件一次性完成 scope 追踪、overview 节流、命令/剪贴板管道，并借助 `TextModel.AllocateDecorationOwnerId()` 隔离 owner。
- `DocUIFindDecorationsTests`、`DocUIFindModelTests`、`DocUIFindControllerTests`（含 26+ 测试）已纳入 TestMatrix；Stickiness/viewport/ReplaceAll 行为在 B3 handoff 中有明确验证。

### 7. DocUI renderer / Markdown overlays / Intl & word cache — ❌
- DocUI renderer 仍未消费搜索装饰（目前重新运行 search 获取 overlays），Markdown capture/Intl 分词/word cache 也尚未串联，阻塞 CL8 验收。`docs/reports/migration-log.md#aa4-cl8-gap` 与 `agent-team/indexes/README.md` 下列占位（[`#delta-2025-11-26-aa4-cl8-markdown`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)、[`#delta-2025-11-26-aa4-cl8-capture`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-capture)、[`#delta-2025-11-26-aa4-cl8-intl`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-intl)、[`#delta-2025-11-26-aa4-cl8-wordcache`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-wordcache)) 仍为「未交付」状态，DocMaintainer 需要这些锚点才能将 Module 04 设为完全对齐。
- Renderer 需要在 CL8 drop 中直接读取 `FindDecorations` 的 owner + metadata，以避免重复计算、同时为 markdown/intl capture 提供缓存入口。

## 修正优先级
1. 统一 `DecorationOwnerIds` 语义并扩展所有查询 API 使 `ownerId=0` 自动映射为“Any”，以避免未来 view-model/renderer port 误用；提交时引用 `WS3-PORT-Tree-Result.md` 汲取 NodeFlags 设计。
2. 为 `DecorationsTrees` 暴露 TS 同款过滤开关，并把 NodeFlags/metadata 流向 DocUI/diff renderer，这也是链接 [`#delta-2025-11-26-aa4-cl8-*`](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown) 的首要依赖。
3. 对齐 `ModelDecoration` 枚举/常量数值（line height ceiling、glyph lanes、cursor stops）并文档化 `MinimapSectionHeaderStyle` 枚举，防止 renderer/API 不兼容。

## Verification Notes
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter IntervalTreeTests --nologo`（13/13）与 `--filter IntervalTreePerfTests --nologo`（7/7）— 见 [`agent-team/handoffs/WS3-QA-Result.md`](../../agent-team/handoffs/WS3-QA-Result.md)，覆盖 lazy normalize + DEBUG counter 验证。
- `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindDecorationsTests --nologo`（9/9）与 `--filter DecorationStickinessTests --nologo`（4/4）— 记录于 [`agent-team/handoffs/archive/B3-Decor-Stickiness-Review.md`](../../agent-team/handoffs/archive/B3-Decor-Stickiness-Review.md)，证明 DocUI owner 分配与 stickiness 行为保持绿灯。
- 全量基线 `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`（585/585，1 skip）在 [`#delta-2025-11-26-sprint04-r1-r11`](../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 中备案；在修补 CL8 前保持该 rerun 作为引用。
