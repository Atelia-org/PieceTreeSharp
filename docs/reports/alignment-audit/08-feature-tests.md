# Feature Tests 模块对齐审查报告

**审查日期:** 2025-12-02 (Sprint 04 M2 更新)  
**审查范围:** 13 个功能测试套件（DocUI Find 栈、Snippet 会话、Cursor/Selection、Decorations/Diff）

## 概要

> ✅ **Sprint 04 M2 重大进展：**
> - **Snippet P0-P2 完成:** 77 tests passed (4 P2 skipped)
> - **Cursor/WordOperations 完成:** 94 tests passed
> - **FindModel/FindDecorations 完成:** 40 tests passed
| 范畴 | 状态 | C# 测试情况 | TS 参考 | 说明 |
| --- | --- | --- | --- | --- |
| DocUI Find 栈 | ✅ 完成 | Controller 27 + Model 49 + Decorations 9 + Selection 4 = **40** | `findController.test.ts`/`findModel.test.ts` | 范围/正则/剪贴板/存储路径已覆盖 |
| Snippet 会话 | ✅ P0-P2 | **77 passed, 4 P2 skipped** | `snippetController2.test.ts`/`snippetSession.test.ts` | adjustWhitespace/Placeholder Grouping 完成 |
| Cursor / Selection | ✅ 完成 | **94 passed, 5 skipped** | `cursorAtomicMoveOperations.test.ts`/`multicursor.test.ts`/`wordOperations.test.ts` | Move/Select/Delete/WordOps 全套 |
| Decorations & Diff | ⚠️ 部分对齐 | `DecorationTests` 12 + `DecorationStickinessTests` 4 + `DiffTests` 4 | `modelDecorations.test.ts`/`defaultLinesDiffComputer.test.ts` | Diff deterministic matrix 待扩展 |

## 详细分析

### DocUI Find 栈（Controller/Model/Decorations/Selection）
- Phase 8 未新增 DocUI feature suite；DocUI diff renderer + snippet parity 仍列在 `docs/reports/migration-log.md#ws5-inv`（WS5-INV）与 `agent-team/handoffs/WS5-INV-TestBacklog.md` backlog，阻塞的 Markdown/renderer/Intl 议题继续映射 `#delta-2025-11-26-aa4-cl8-markdown`、`#delta-2025-11-26-aa4-cl8-capture`、`#delta-2025-11-26-aa4-cl8-intl`、`#delta-2025-11-26-aa4-cl8-wordcache`。
- `tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs` 现有 27 个 `[Fact]`，涵盖 issue #1857/#3090/#6149、regex reseed、Cmd/Ctrl+F3 selection match、Auto find-in-selection、scope 持久化、剪贴板/存储首选项等，已复刻 TS `findController.test.ts` 的大部分回归场景。缺口集中在：Mac 专用的 global clipboard 写入（TS 需真实平台 hook）、`FindStartFocusAction` 上下文键/动画分支以及 `StartFindAction` 与 `FindStartOptions.shouldAnimate` 组合验证。
- `tests/TextBuffer.Tests/DocUI/DocUIFindModelTests.cs` 统计 49 个 `[Fact]`，比 Batch B2 最初的 39 个额外补齐了多选范围（TS Test07/08）、Replace scope、SelectAllMatches 排序、regex lookahead/capture 组和 scoped ReplaceAll。仍未覆盖 TS 里依赖 `FindDecorations.MatchBeforePosition`/`Delayer` 的节流测试，以及 `highlightFindScope` 等编辑器集成路径。
- `tests/TextBuffer.Tests/DocUI/DocUIFindDecorationsTests.cs` 9 个测试验证范围高亮收缩、overview 合并、scope 跟踪、wrap-around 媒体等，与 `FindDecorations` 行为一致。若要完全对齐 TS，需要加上 `findMatchDecoration` stacking 与 viewport 缓冲区重算的压力测试。
- `tests/TextBuffer.Tests/DocUI/DocUIFindSelectionTests.cs` 4 个测试（wordUnderCursor、单行选择、多行退回 null、自定义分隔符）复刻 `FindUtilities.getSelectionSearchString` 的要点；尚未触及 `WordSeparators` 缓存与 Intl.Segmenter fallback（参见 `agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-intl` 与 `#delta-2025-11-26-aa4-cl8-wordcache` backlog）。

### Snippet 测试 — ✅ P0-P2 完成 (Sprint 04 M2)
- **SnippetControllerTests:** 77 个测试通过，覆盖：
  - adjustWhitespace 各种缩进级别
  - Placeholder Grouping 和导航
  - 多光标 snippet 插入
  - BF1 循环修复验证
- **4 个 P2 skipped:** 变量解析（TM_FILENAME/CLIPBOARD 等）、Transform、Choice 功能降级到后续 Sprint

### Cursor / Selection 套件 — ✅ 完成 (Sprint 04 M2)
- **CursorCoreTests + CursorWordOperationsTests:** 94 个测试通过（5 skipped）
- 覆盖内容：
  - 基本 Move/Select/Sticky Column
  - WordOperations: MoveWordLeft/Right, DeleteWordLeft/Right, SelectWord
  - 多光标操作基础
  - CursorState 双态机验证
- CL7 cursor-core/wordops 占位已关闭

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

- **2025-12-02 (Sprint 04 M2)**：全量基线 **873 passed / 9 skipped**，关键套件：
  - `SnippetControllerTests`: 77/77 (4 P2 skipped)
  - `CursorCoreTests + CursorWordOperationsTests`: 94/94 (5 skipped)
  - `DocUIFind*Tests`: 40/40
  - `IntervalTreeTests`: 15/15
  - `DecorationTests + DecorationStickinessTests`: 16/16
  - `DiffTests`: 4/4
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
