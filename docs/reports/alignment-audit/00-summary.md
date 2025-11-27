# TypeScript → C# 对齐审查汇总报告

**生成日期:** 2025-11-27  
**审查范围:** 90 个文件/套件（原创 C# 逻辑除外）  
**审查方法:** SubAgent 并行逐行对比 + 目标测试复跑

> 对齐结论需与最新变更日志保持一致，请在执行任何计划前先查阅 [`docs/reports/migration-log.md#sprint04-r1-r11`](../migration-log.md#sprint04-r1-r11) 与 Info-Indexer 基准 [`agent-team/indexes/README.md#delta-2025-11-26-alignment-audit`](../../../agent-team/indexes/README.md#delta-2025-11-26-alignment-audit)。当前全量基线：`export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`（585/585 通过，1 skipped，详见 [`#delta-2025-11-26-ws5-qa`](../../../agent-team/indexes/README.md#delta-2025-11-26-ws5-qa)）。

---

## 总体评估

| 模块 | 完全对齐 | 存在偏差 | 需要修正 | 审查文件数 |
|------|----------|----------|----------|------------|
| 01-Core Fundamentals | 8 | 0 | 2 | 10 |
| 02-Core Support | 1 | 7 | 0 | 8 |
| 03-Cursor | 0 | 2 | 7 | 9 |
| 04-Decorations | 4 | 2 | 1 | 7 |
| 05-Diff | 9 | 5 | 2 | 16 |
| 06-Services | 4 | 4 | 2 | 10 |
| 07-Core Tests | 9 | 6 | 2 | 17 |
| 08-Feature Tests | 0 | 9 | 4 | 13 |
| **合计** | **35** | **35** | **20** | **90** |

### 质量画像

- **完全对齐:** 39% (35/90) – PieceTree 架构、Diff 核心算法、TextModel options/快照及绝大部分 deterministic harness
- **存在偏差:** 39% (35/90) – Range/Selection 桥接、Decoration owner 策略、DocUI Find host、Diff renderer、国际化搜索
- **需要修正:** 22% (20/90) – Cursor/Word/Snippet Stage 1、PieceTree nodeAt2/SearchCache、RangeMapping、DocUI diff + Markdown renderer、Language/Undo 服务、Feature test gaps

---

## 模块快照

- **01 Core Fundamentals** (`PieceTreeModel`/Builder/Chunk)：结构与 TS 一致，CRLF/搜索基础通过 `PieceTreeDeterministicTests`/`PieceTreeFuzzHarnessTests` 复核，但 `_lastChangeBufferPos` Telemetry 与 Info-Indexer changefeed（WS1-PORT-CRLF）尚未补齐，`NodeAt2` tuple reuse 仍待按 [`agent-team/handoffs/PORT-PT-Search-Plan.md`](../../agent-team/handoffs/PORT-PT-Search-Plan.md) 实装。
- **02 Core Support** (`Range`/`Selection`/`SearchTypes` 等)：已借由 `WS2-PORT` 引入 75 条 Range/Selection helper 测试（`#delta-2025-11-26-ws2-port`），但 `RangeMapping`、`SelectionRangeMapping`、Intl `WordCharacterClassifier`、`PieceTreeSearchCache.Validate` 默认值仍滞后；`TextMetadataScanner` 额外检测 NEL/RTL，也需记录差异。
- **03 Cursor**：Stage 0 架构 (`CursorConfiguration`, `CursorContext`, `CursorState`) 已通过 `CursorCoreTests` 25/25 验证，Stage 1（`Cursor`, `CursorCollection`, `CursorColumns`, `WordOperations`, `Snippet*`）仍采用旧实现，CL7 占位（`#delta-2025-11-26-aa4-cl7-*`）保持 Gap。
- **04 Decorations**：IntervalTree (`WS3-PORT-Tree`) 及 `DecorationRangeUpdater` parity 已确认，DocUI find 装饰回归 (`B3-Decor-Stickiness-Review`) 通过；但 `DecorationOwnerIds` 的 Any 语义、`ModelDecoration` 常量和 `DecorationsTrees` 过滤开关仍与 TS 不符，DocUI renderer/Markdown/Intl backlog 由 CL8 占位追踪。
- **05 Diff**：Myers/DP 算法、Heuristic 优化、LineRange/Fragment/OffsetRange 对齐；`LineSequence.GetBoundaryScore` 的结尾索引与 TS 不同，`RangeMapping.Inverse/Clip/FromEdit`、`DetailedLineRangeMapping.ToTextEdit`、DocUI diff renderer 等缺失使 revert/move 功能无法复刻。
- **06 Services**：`TextModelOptions`/search stack parity完成；`TextModel` 仍缺 `ValidatePosition/Range`、`GetFullModelRange` 等公开入口，`IUndoRedoService` 无资源/分组/快照，`ILanguageConfigurationService` 仅保存订阅，DocUI Find 控制器默认持久化/剪贴板实现仍是 `Null` stub。
- **07 Core Tests**：PieceTree deterministic/fuzz/search-offset/snapshot 全系测试（WS5 harness + `#delta-2025-11-26-sprint04-r1-r11`）已落地，TextModel snapshot/indentation 亦在 QA 记录中；Cursor/Snippet/Diff suites多数仍缺，`TextModelIndentationTests` 保留 1 个 `GuessIndentation` skip。
- **08 Feature Tests**：DocUI Find Controller/Model/Decorations（27+49+9+4）已对齐 TS，Snippet、Cursor、多选、Diff/Decorations 特性测试仍欠缺 deterministic 覆盖（`#delta-2025-11-26-aa4-cl7-*`, `#delta-2025-11-26-aa4-cl8-*`, `#delta-2025-11-26-ws5-test-backlog`）。

---

## 高优先级修正项 (P0)

1. **PieceTree 搜索/编辑回路** – 文件：`src/TextBuffer/Core/PieceTreeModel.Edit.cs`, `PieceTreeModel.Search.cs`. 需落地 `NodeAt2` tuple reuse、SearchCache diagnostics、Info-Indexer 发布独立 `WS1-PORT-CRLF` changefeed，并以 `CRLFFuzzTests` + `PieceTreeSearchRegressionTests` rerun 佐证（见 [`agent-team/handoffs/WS1-PORT-CRLF-Result.md`](../../agent-team/handoffs/WS1-PORT-CRLF-Result.md)）。
2. **Cursor/Word/Snippet Stage 1（AA4 CL7）** – 文件：`src/TextBuffer/Cursor/*.cs`, `Snippet*.cs`. 必须根据 CL7 占位（`#delta-2025-11-26-aa4-cl7-cursor-core`, `-wordops`, `-column-nav`, `-snippet`, `-commands-tests`）把 Stage 0 plumbing接回命令栈，并扩展 `CursorWordOperationsTests`, `CursorAtomicMoveOperationsTests`, `SnippetControllerTests`。
3. **DocUI Renderer & CL8 Backlog** – 文件：`src/TextBuffer/Rendering/MarkdownRenderer.cs`, `DocUI/*.cs`. `WS3-PORT-Tree` metadata 尚未被 renderer 消费，`FindDecorations` owner 语义/overview 节流的结果也未在 markdown/DocUI diff 中展示；需完成 `#delta-2025-11-26-aa4-cl8-markdown` / `-capture` / `-intl` / `-wordcache` 承诺。
4. **WS5 Top-10 测试缺口** – 套件：`CursorWordOperationsTests`, `CursorMultiSelectionTests`, `SnippetControllerTests`, `DiffTests`. `WS5-INV` backlog（`#delta-2025-11-26-ws5-test-backlog`）尚未被清空，需按计划移植 cursorAtomicMoveOperations、snippetSession、diff renderer deterministic/perf 套件，并将 rerun 写回 `tests/TextBuffer.Tests/TestMatrix.md` 与 [`agent-team/handoffs/WS5-QA-Result.md`](../../agent-team/handoffs/WS5-QA-Result.md)。

---

## 中优先级修正项 (P1)

1. **Range/Selection/Mapping 一致性** – 将 `WS2-PORT` helper 引入 `RangeMapping`, `TextModelSearch`, `DiffComputer`, `DocUIFindModel`，补齐 `RangeMapping.Inverse/Clip/FromEdit`、`DetailedLineRangeMapping.ToTextEdit`，避免 DocUI diff/revert 逻辑二次实现。
2. **Decoration owner 语义** – 修正 `DecorationOwnerIds.Default/Any`、`ModelDecoration.LineHeightCeiling`、minimap/glyph/injectedText 枚举值，与 `WS3-PORT-Tree` 的 NodeFlags 对齐，并暴露 `DecorationsTrees` 过滤参数供 renderer 使用。
3. **Services 层增强** – `TextModel` 公布 `ValidatePosition/Range`、`GetFullModelRange`，`IUndoRedoService` 添加资源组/快照/`UndoRedoGroup`，`ILanguageConfigurationService` 支持注册与缓存；`DocUIFindController` 默认注入 clipboard/storage/context key 实现，解除查找设置丢失问题。
4. **国际化/词边界** – `SearchTypes`, `PieceTreeSearcher`, `DocUIFindSelectionTests` 仍缺 `Intl.Segmenter` 等价实现；参考 `AA4-002-Audit` 与 CL8 占位补齐 word cache/LRU。
5. **Diff & Decorations 测试矩阵** – 按 `WS5-INV-TestBacklog` 建立 diff deterministic/perf suite（`defaultLinesDiffComputer.test.ts` 全量）与 `modelDecorations.test.ts` 行级矩阵，配合 DocUI renderer 验证。
6. **TextModel Indentation / Guess API** – 解除 `TextModelIndentationTests` skip，保证 `GuessIndentation` 与 TS 一致；扩展 `TextModelTests` 以覆盖 `TextModelData.fromString`, `getValueLengthInRange`, `validatePosition` 等 TS 场景。

---

## 测试覆盖与风险

- 新增 harness：`PieceTreeDeterministicTests`、`PieceTreeFuzzHarnessTests`、`PieceTreeSearchOffsetCacheTests`、`PieceTreeSnapshotParityTests`、`TextModelSnapshotTests`、`PieceTreeBufferApiTests`、`PieceTreeSearchRegressionTests`、`TextModelIndentationTests`（`WS5-QA`）均记录在 `tests/TextBuffer.Tests/TestMatrix.md` 并引用 `#delta-2025-11-26-sprint04-r1-r11`。
- 仍为空白的 deterministic 套件：`CursorWordOperationsTests`（仅 3/60 场景）、`CursorMultiSelection`（5/70）、`SnippetControllerTests`（1 deterministic + 1 fuzz）、`DiffTests`（4/40+），以及 DocUI diff renderer/Find context key 行为。
- TextModel 缩进 suite 仍保留 1 个 skip；Intl word cache、DocUI clipboard/storage、Undo/Language 服务等基础设施尚未提供回归测试。

| 套件 | TS 用例 | 当前 C# | 状态 |
|------|---------|---------|------|
| CursorWordOperationsTests | ≈60 | 3 | Stage 1 缺口；待 CL7 word ops inside `#delta-2025-11-26-aa4-cl7-wordops` |
| Cursor/Column MultiSelection | ≈70 | 5 | 未覆盖 `InsertCursorAbove/Below`、`AddSelectionToNextFindMatch`; 与 `#delta-2025-11-26-aa4-cl7-column-nav` 链接 |
| SnippetController/SnippetSession | ≈60 | 1 deterministic + 1 fuzz | 仅验证 BF1 修复；CL7 snippet backlog |
| DiffTests | 40+ | 4 | 未覆盖 `defaultLinesDiffComputer.test.ts` 参数矩阵、DocUI diff renderer |
| TextModelIndentationTests | 20 | 19 pass + 1 skip | `GuessIndentation` 未完成；受 `WS5-QA` skip 约束 |

---

## 可接受的设计差异

| 模块 | 差异 | 说明 |
|------|------|------|
| PieceTreeBuffer | 结构比 TS 更紧凑 | 保留不可变 `ChunkBuffer` 与 `LineStartTable` 以获得线程安全；语义等价 |
| SearchTypes | 额外公开 `IsMultiline/IsCaseSensitive` | 方便 C# 调用者直接消费；TS 端无该字段，已在 doc 中标注 |
| Undo/Language Service 接口 | 自定义 C# API | 由于 C# 平台缺少 VS Code service infrastructure，接口签名不同，计划后续逐步补齐语义 |
| DiffComputer | 增加 `ExtendToWordBoundaries` 调试开关 | 默认与 TS 一致，仅在诊断场景关闭 |
| WordCharacterClassifierCache | 新增 LRU 缓存 | FR-01/02 交付 (`#delta-2025-11-23`)；需在 Intl backlog 完成前记录该差异 |

---

## 详细报告索引

1. [01-core-fundamentals.md](./01-core-fundamentals.md)
2. [02-core-support.md](./02-core-support.md)
3. [03-cursor.md](./03-cursor.md)
4. [04-decorations.md](./04-decorations.md)
5. [05-diff.md](./05-diff.md)
6. [06-services.md](./06-services.md)
7. [07-core-tests.md](./07-core-tests.md)
8. [08-feature-tests.md](./08-feature-tests.md)

---

## 下一步行动建议

### 立即行动（Sprint 04 余量）
1. [ ] `PORT-PT-Search-Plan` Step1/2：恢复 `NodeAt2` tuple reuse + SearchCache 诊断，并 rerun `CRLFFuzzTests` + `PieceTreeSearchRegressionTests` 后在 Info-Indexer 发布补丁 changefeed。
2. [ ] CL7 Stage 1：将 `Cursor`/`CursorCollection` 切换到 `CursorState`，移植 column select/word ops/snippet 行为及对应测试，关闭 `#delta-2025-11-26-aa4-cl7-*` Gap。
3. [ ] CL8 Renderer：把 `DecorationsTrees` NodeFlags/owner 语义接入 Markdown/DocUI renderer，并补齐 DocUI diff/intl capture 测试，关闭 `#delta-2025-11-26-aa4-cl8-*`。 
4. [ ] WS5 Top-10：实现 cursorAtomicMoveOperations、snippetSession、diff deterministic/perf harness，并把 rerun 贴入 `tests/TextBuffer.Tests/TestMatrix.md` + `WS5-QA` 日志。

### 短期（1–2 Sprint）
1. [ ] 发布 `RangeMapping.Inverse/Clip/FromEdit` + DocUI diff renderer，并在 `DiffTests` 中覆盖 moves/unchanged 区域。
2. [ ] 为 `TextModel`/`IUndoRedoService`/`ILanguageConfigurationService` 提供 TS 等价 API，解除 `TextModelIndentationTests` skip 并加入 `validatePosition`/`getValueLengthInRange` 覆盖。
3. [ ] 打通 DocUI clipboard/storage/context key host，使 `DocUIFindControllerTests` 验证多宿主持久化、scope highlight、widget focus。
4. [ ] 将 Intl word cache / Segmenter 能力注入 `WordCharacterClassifier`、`DocUIFindSelectionTests`，确保 CL8 backlog 可收敛。

### 长期
1. [ ] 完整移植 `cursorAtomicMoveOperations.test.ts`、`multicursor.test.ts`、`wordOperations.test.ts`、`snippetController2.test.ts`、`snippetSession.test.ts`，实现 deterministic + fuzz 双轨覆盖。
2. [ ] 在 DocUI renderer 中集成 diff/move/undo 提示、unchanged region 折叠、moved block glyph，与 `DiffResult` 数据模型保持一致。
3. [ ] 将核心/特性测试覆盖率提升至 ≥60%，并继续扩充 PieceTree buffer API、TextModel guessIndentation、bracket matching、DocUI focus/context key 等套件。

---

## Verification Notes

- **2025-11-27**：按照 [`docs/reports/migration-log.md#sprint04-r1-r11`](../migration-log.md#sprint04-r1-r11) rerun `CRLFFuzzTests` (16/16)、`CursorCoreTests` (25/25) 及全量 585/585（1 skipped），并引用 [`#delta-2025-11-26-sprint04-r1-r11`](../../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11)。
- **01-Core Fundamentals**：参照 `PieceTreeModel.Edit.cs`, `PieceTreeModel.Search.cs`, `PieceTreeBuilder.cs` 与 TS `pieceTreeBase.ts`; 结合 `PieceTreeFuzzHarnessTests`, `PieceTreeDeterministicTests`（`#delta-2025-11-24-b3-piecetree-fuzz` 等 changefeed）。
- **02-Core Support**：审阅 `Range.Extensions.cs`, `Selection.cs`, `PieceTreeSearchCache.cs`, `SearchTypes.cs`, `TextMetadataScanner.cs` 与 TS 对应文件；验证范围 helper 通过 `RangeSelectionHelperTests` (75 data rows, `#delta-2025-11-26-ws2-port`)。
- **03-Cursor**：对照 `Cursor.cs`, `CursorCollection.cs`, `CursorColumns.cs`, `CursorState.cs`, `SnippetController.cs`, `SnippetSession.cs`；执行 `CursorCoreTests`, `CursorWordOperationsTests`, `SnippetMultiCursorFuzzTests` 并引用 `#delta-2025-11-26-aa4-cl7-*`。
- **04-Decorations**：检查 `IntervalTree.cs`, `DecorationsTrees.cs`, `DecorationOwnerIds.cs`, `ModelDecoration.cs`；复跑 `IntervalTreeTests`, `DecorationStickinessTests`, `DocUIFindDecorationsTests`（`B3-Decor-Stickiness-Review`）。
- **05-Diff**：核对 `DiffComputer.cs`, `ComputeMovedLines.cs`, `LineSequence.cs`, `RangeMapping.cs`, `DiffMove.cs`; 执行 `DiffTests`（`#delta-2025-11-23`）。
- **06-Services**：复核 `TextModel.cs`, `TextPosition.cs`, `ILanguageConfigurationService.cs`, `IUndoRedoService.cs`, `DocUIFindController.cs`; 参考 `DocUIFindControllerTests`, `TextModelTests`, `TextModelIndentationTests`（WS5-QA）。
- **07-Core Tests / 08-Feature Tests**：浏览 `PieceTree*Tests`, `TextModelSnapshotTests`, `DocUIFind*Tests`, `Cursor*Tests`, `Snippet*Tests`, `DecorationTests`, `DiffTests`，并对照 TS suite (`findController.test.ts`, `snippetSession.test.ts`, `cursorAtomicMoveOperations.test.ts` 等)。

*报告由 AI Team 自动生成*
