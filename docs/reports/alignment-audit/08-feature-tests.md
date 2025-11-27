# Feature Tests 模块对齐审查报告

**审查日期:** 2025-11-26  
**审查范围:** 13 个功能测试套件（DocUI Find 栈、Snippet 会话、Cursor/Selection、Decorations/Diff）

## 概要
| 范畴 | 状态 | C# 测试情况 | TS 参考 | 说明 |
| --- | --- | --- | --- | --- |
| DocUI Find 栈 | ⚠️趋近对齐 | Controller 27 + Model 49 + Decorations 9 + Selection 4 | `ts/src/vs/editor/contrib/find/test/browser/findController.test.ts`<br>`ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts`<br>`ts/src/vs/editor/contrib/find/test/browser/find.test.ts` | Batch B3 移植了范围/正则/剪贴板/存储路径；Phase 8 未新增 DocUI feature suite，DocUI diff renderer + snippet parity 仍由 `docs/reports/migration-log.md#ws5-inv` 与 `agent-team/handoffs/WS5-INV-TestBacklog.md` 跟踪，并等待 `#delta-2025-11-26-aa4-cl8-markdown`/`-capture`/`-intl`/`-wordcache` 落地以验证剩余 UI 行为。 |
| Snippet 会话 | ⚠️结构化打桩 | `SnippetControllerTests` 1 个确定性测试 + `SnippetMultiCursorFuzzTests` 单一 fuzz（10 轮） | `ts/src/vs/editor/contrib/snippet/test/browser/snippetController2.test.ts`<br>`ts/src/vs/editor/contrib/snippet/test/browser/snippetSession.test.ts` | 计数已与 Module 03 同步：仅有 BF1 fuzz + 单例 deterministic 覆盖，Stage 0 只触达 `#delta-2025-11-26-aa4-cl7-cursor-core`，而 60+ snippet/session/command 例仍待 `#delta-2025-11-26-aa4-cl7-snippet` 与 `#delta-2025-11-26-aa4-cl7-commands-tests`。 |
| Cursor / Selection | ❌Gap | `ColumnSelection` 3 + `CursorMultiSelection` 2 + `CursorTests` 8 + `CursorWordOperations` 3 | `ts/src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts`<br>`ts/src/vs/editor/test/common/controller/cursorAtomicMoveOperations.test.ts`<br>`ts/src/vs/editor/contrib/wordOperations/test/browser/wordOperations.test.ts` | Stage 0 `CursorCoreTests` 25/25（参见 `docs/reports/alignment-audit/03-cursor.md` 与 `#delta-2025-11-26-aa4-cl7-cursor-core`）已落地，但 C# 仍缺少多光标/word ops/column 导航/locale 语义，对应 `#delta-2025-11-26-aa4-cl7-wordops`、`-column-nav`、`-snippet` backlog。 |
| Decorations & Diff | ⚠️部分对齐 | `DecorationTests` 12 + `DecorationStickinessTests` Theory 4 组合 + `DiffTests` 4 | `ts/src/vs/editor/test/common/model/model.decorations.test.ts`<br>`ts/src/vs/editor/test/common/model/modelDecorations.test.ts`<br>`ts/src/vs/editor/test/common/diff/defaultLinesDiffComputer.test.ts` | Delta 装饰所有者、InjectedText stickiness、Diff wordDiff/ignore trim/Move detection 均有 C# 覆盖，但 DocUI renderer/Markdown diff 路径仍受 `#delta-2025-11-26-aa4-cl8-markdown`/`-capture`/`-intl`/`-wordcache` 阻塞，并与 `docs/reports/migration-log.md#ws5-inv` backlog 的 DocUI diff renderer 里程碑同进。 |

## 详细分析

### DocUI Find 栈（Controller/Model/Decorations/Selection）
- Phase 8 未新增 DocUI feature suite；DocUI diff renderer + snippet parity 仍列在 `docs/reports/migration-log.md#ws5-inv`（WS5-INV）与 `agent-team/handoffs/WS5-INV-TestBacklog.md` backlog，阻塞的 Markdown/renderer/Intl 议题继续映射 `#delta-2025-11-26-aa4-cl8-markdown`、`#delta-2025-11-26-aa4-cl8-capture`、`#delta-2025-11-26-aa4-cl8-intl`、`#delta-2025-11-26-aa4-cl8-wordcache`。
- `tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs` 现有 27 个 `[Fact]`，涵盖 issue #1857/#3090/#6149、regex reseed、Cmd/Ctrl+F3 selection match、Auto find-in-selection、scope 持久化、剪贴板/存储首选项等，已复刻 TS `findController.test.ts` 的大部分回归场景。缺口集中在：Mac 专用的 global clipboard 写入（TS 需真实平台 hook）、`FindStartFocusAction` 上下文键/动画分支以及 `StartFindAction` 与 `FindStartOptions.shouldAnimate` 组合验证。
- `tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs` 统计 49 个 `[Fact]`，比 Batch B2 最初的 39 个额外补齐了多选范围（TS Test07/08）、Replace scope、SelectAllMatches 排序、regex lookahead/capture 组和 scoped ReplaceAll。仍未覆盖 TS 里依赖 `FindDecorations.MatchBeforePosition`/`Delayer` 的节流测试，以及 `highlightFindScope` 等编辑器集成路径。
- `tests/TextBuffer.Tests/DocUI/DocUIFindDecorationsTests.cs` 9 个测试验证范围高亮收缩、overview 合并、scope 跟踪、wrap-around 媒体等，与 `FindDecorations` 行为一致。若要完全对齐 TS，需要加上 `findMatchDecoration` stacking 与 viewport 缓冲区重算的压力测试。
- `tests/TextBuffer.Tests/DocUI/DocUIFindSelectionTests.cs` 4 个测试（wordUnderCursor、单行选择、多行退回 null、自定义分隔符）复刻 `FindUtilities.getSelectionSearchString` 的要点；尚未触及 `WordSeparators` 缓存与 Intl.Segmenter fallback（参见 `agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-intl` 与 `#delta-2025-11-26-aa4-cl8-wordcache` backlog）。

### Snippet 测试
- Module 03（`docs/reports/alignment-audit/03-cursor.md`）已登记 Stage 0 `CursorCoreTests` 25/25（`#delta-2025-11-26-aa4-cl7-cursor-core`），本模块复用同一计数，但 snippet/session/commands deterministic 套件仍待 `#delta-2025-11-26-aa4-cl7-snippet` 与 `#delta-2025-11-26-aa4-cl7-commands-tests`。
- `SnippetControllerTests.SnippetInsert_CreatesPlaceholders_AndNavigates` 仅验证占位符创建 + `NextPlaceholder` 顺序；未覆盖 `Cancel`, `Tab` 导航、嵌套、可变占位符或 undo/redo。
- `SnippetMultiCursorFuzzTests.SnippetAndMultiCursor_Fuzz_NoCrashesAndInvariantsHold`（10 次随机迭代）增量验证多光标插入 snippet、占位符装饰同步和 model length 期望，提供了 BF1 fuzz 保障。但 fuzz 运行只检查“无崩溃+长度匹配”，无法替代 TS 中 60+ deterministic 例对 placeholder 顺序、变量重写、Transform、TabStop 恢复等细节的断言。
- 缺失：`SnippetSession.insert/merge/cancel` 全流程、`snippetVariables`、`Tabstop order #58267`、recursive snippet (#27543)、删除占位符后继续导航 (#31619) 等。需要依托 `AA4-007 Plan – CL7 Cursor word/snippet/multi-select parity` 中步骤 5-6 来补齐。

### Cursor / Selection 套件
- Stage 0 `CursorCoreTests`（25/25）随 WS4-PORT-Core 交付，细节在 Module 03 与 `agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-cursor-core`；现有 Feature suite 必须在此基线之上补齐 `#delta-2025-11-26-aa4-cl7-wordops`、`#delta-2025-11-26-aa4-cl7-column-nav`、`#delta-2025-11-26-aa4-cl7-snippet` 所罗列的多光标/word ops backlog。
- `ColumnSelectionTests` 3 个场景（可见列往返、InjectedText、基本列选）无法覆盖 TS `multicursor.test.ts` 中的 Alt+Drag、Word wrap、CRLF、触碰区间、`AddSelectionToNextFindMatchAction`。
- `CursorMultiSelectionTests` 仅验证 Markdown renderer 输出两个竖线以及一次批量编辑；缺少 TS 中 `InsertCursorAbove/Below`、`MultiCursorSelection`、取消多光标后的位置恢复、Regex Select All 等。
- `CursorTests` 关注基本 Move/Select/Sticky Column，与 TS `cursorAtomicMoveOperations.test.ts`（`whitespaceVisibleColumn` + `atomicPosition`）脱节；未验证 `AtomicTabMoveOperations`、`VisibleColumn` 精度或 `typeCommand` 原子性。
- `CursorWordOperationsTests` 只有 Move/Left/Right/DeleteWordLeft 基线，未覆盖 `cursorWordStart/End` 变体、`cursorWordAccessibility*`、wordPart/locale、`deleteInsideWord`、`issue #41199`/`#48046` 等 60+ 例。所有这些差距都已在 `agent-team/handoffs/AA4-003-Audit.md` 和 `AA4-007 Plan` 中标记为 High Risk。

### Decorations 与 Diff
- DocUI renderer 与 Markdown diff 覆盖仍列在 `docs/reports/migration-log.md#ws5-inv`/`agent-team/handoffs/WS5-INV-TestBacklog.md` backlog，下游验证需等待 `#delta-2025-11-26-aa4-cl8-markdown`、`-capture`、`-intl`、`-wordcache` changefeed 发布。
- `DecorationTests` 12 个 `[Fact]` 覆盖 `DeltaDecorations` owner scope、`CollapseOnReplaceEdit`、Stickiness、DecorationOptions round-trip、事件回调（minimap/overview/glyph/line number/line height/injected text）、owner 过滤、`ForceMoveMarkers` 等，基本对齐 `model.decorations.test.ts` 的主干。但缺失：多 owner merges、line range 合并顺序、`InjectedText` 与 `lineBreak` 混合的 viewport 结果等。
- `DecorationStickinessTests` 使用 `[Theory]` 覆盖四种 `TrackedRangeStickiness` 边界插入，映射 TS `modelDecorations` stickiness 矩阵；仍需增加 `Before/After` 注入、不同 `forceMoveMarkers` 与 collapsed ranges 的组合。
- `DocUIFindDecorationsTests`（见上）覆盖 DocUI overlay 特性，补足 Batch B3 `#delta-2025-11-23-b3-decor-stickiness-review` 的 QA 要求。
- `DiffTests` 4 个 `[Fact]` 复刻 TS `defaultLinesDiffComputer.test.ts` 的 word diff inner changes、IgnoreTrimWhitespace、Move detection、超时旗标，但缺乏 `maxComputationTimeMs` 与 `computeMoves` 组合、`algorithm=advanced/legacy` 切换、`minMatchLength` 边界等。

### 缺失的功能级测试（需新增）
1. **Multi-cursor word/command flows** – TS `multicursor.test.ts` 中的 `InsertCursorAbove/Below`, `AddSelectionToNextFindMatchAction`, `issue #26393/#2205/#23541` 等仍无 C# 覆盖；应按照 `AA4-007 Plan` 第 3/4/6 步，补全 word movement、column selection 和 command 流程自动化测试，并落到 `agent-team/indexes/README.md#delta-2025-11-26-aa4-cl7-wordops` 与 `#delta-2025-11-26-aa4-cl7-column-nav` 所列的占位。
2. **Bracket pair colorization & matching** – TS `ts/src/vs/editor/contrib/bracketMatching/test/browser/bracketMatching.test.ts` 验证 `bracketPairColorizationOptions`、平衡对检测和配色，C# 目前仅在 `TextModelTests` 覆盖配置布尔值，缺乏任何行为测试；可对照 `agent-team/handoffs/archive/AA3-001-Audit.md` 中的 gap 说明安排专门套件。
3. **Snippet session deterministic battery** – 依赖 `snippetController2.test.ts`/`snippetSession.test.ts` 中的嵌套、transform、变量、撤销、`createEditsAndSnippetsFromEdits`；需在 `AA4-007` 的 snippet 子任务里落地，并关闭 `#delta-2025-11-26-aa4-cl7-snippet`/`#delta-2025-11-26-aa4-cl7-commands-tests`。
4. **DocUI Find focus/context-key behaviors** – TS suite 对 `FindStartFocusAction`、`CONTEXT_FIND_INPUT_FOCUSED`、`hasFocus`、`StartFindAction` + clipboard 互操作有 tests，而 C# 尚未覆盖；缺口与 `docs/reports/migration-log.md#ws5-inv` backlog 的 DocUI diff renderer 项联动，需在 `#delta-2025-11-26-aa4-cl8-markdown`/`-capture`/`-intl`/`-wordcache` 落地时一并验证，详见 `agent-team/handoffs/B3-FC-Review.md`。

### 整改建议（关联具体工作项）
1. **AA4-007 Plan – CL7 Cursor word/snippet/multi-select parity**：扩展 `CursorWordOperationsTests`、`ColumnSelectionTests`、`CursorMultiSelectionTests`，并根据 plan 的任务 3/5/6 移植 wordPart + snippet deterministic 套件，同时把 fuzz harness 产物纳入 `TestMatrix.md`，以实质关闭 `#delta-2025-11-26-aa4-cl7-wordops`、`-column-nav`、`-snippet`、`-commands-tests`。
2. **AA3-001 Audit follow-up（language config / bracket options）**：基于 `agent-team/handoffs/archive/AA3-001-Audit.md` 与 `ts/src/vs/editor/contrib/bracketMatching/test/browser/bracketMatching.test.ts` 创建新的 bracket pair colorization 测试，验证 `TextModelBracketPairs`、`BalancedBracketMap` 与 `BracketPairColorizationOptions`。
3. **AA4-004 / B3-FC backlog**：参考 `agent-team/handoffs/B3-FC-Review.md`，补齐 FindController 焦点/上下文键测试、Delayed history 更新与 Mac 剪贴板写入，保持与 `ts/src/vs/editor/contrib/find/test/browser/findController.test.ts` 的剩余差异一致，并确保验证结果反映 `#delta-2025-11-26-aa4-cl8-markdown`、`-capture`、`-intl`、`-wordcache` 的 DocUI 依赖。

## Verification Notes
- 基线：`export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → **585/585（1 skip）**，锚定 `agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`，详见 `tests/TextBuffer.Tests/TestMatrix.md` 与 `agent-team/handoffs/WS5-QA-Result.md`。
- 目标命令（全部在 `PIECETREE_DEBUG=0` 下执行，结果记录于 TestMatrix）：
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindControllerTests --nologo`（27/27）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindModelTests --nologo`（49/49）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindDecorationsTests --nologo`（9/9）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DocUIFindSelectionTests --nologo`（4/4）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter SnippetControllerTests --nologo`（1/1）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter SnippetMultiCursorFuzzTests --nologo`（10 fuzz iterations, pass）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter CursorCoreTests --nologo`（25/25）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter CursorWordOperationsTests --nologo`（3/3）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter ColumnSelectionTests --nologo`（3/3）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter CursorMultiSelectionTests --nologo`（2/2）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DecorationTests --nologo`（12/12）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DecorationStickinessTests --nologo`（4/4）。
	- `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter DiffTests --nologo`（4/4）。
- 查阅文件：`tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs`、`DocUIFindModelTests.cs`、`DocUIFindDecorationsTests.cs`、`DocUIFindSelectionTests.cs`、`SnippetControllerTests.cs`、`SnippetMultiCursorFuzzTests.cs`、`ColumnSelectionTests.cs`、`CursorMultiSelectionTests.cs`、`CursorTests.cs`、`CursorWordOperationsTests.cs`、`DecorationTests.cs`、`DecorationStickinessTests.cs`、`DiffTests.cs`。
- TS 侧对照：`ts/src/vs/editor/contrib/find/test/browser/*.test.ts`、`ts/src/vs/editor/contrib/snippet/test/browser/*.test.ts`、`ts/src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts`、`ts/src/vs/editor/test/common/controller/cursorAtomicMoveOperations.test.ts`、`ts/src/vs/editor/contrib/wordOperations/test/browser/wordOperations.test.ts`、`ts/src/vs/editor/contrib/bracketMatching/test/browser/bracketMatching.test.ts`、`ts/src/vs/editor/test/common/diff/defaultLinesDiffComputer.test.ts`。
