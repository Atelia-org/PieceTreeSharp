# TypeScript → C# 对齐审查汇总报告

**生成日期:** 2025-12-02 (Sprint 04 M2 更新)  
**审查范围:** 90 个文件/套件（原创 C# 逻辑除外）  
**审查方法:** SubAgent 并行逐行对比 + 目标测试复跑

> 对齐结论需与最新变更日志保持一致，请在执行任何计划前先查阅 [`docs/reports/migration-log.md#sprint04-r1-r11`](../migration-log.md#sprint04-r1-r11) 与 Info-Indexer 基准 [`agent-team/indexes/README.md#delta-2025-11-26-alignment-audit`](../../../agent-team/indexes/README.md#delta-2025-11-26-alignment-audit)。当前全量基线：`export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo`（**873 passed, 9 skipped**，详见 Sprint 04 M2 交付）。

---

## 总体评估

| 模块 | 完全对齐 | 存在偏差 | 需要修正 | 审查文件数 | Sprint 04 M2 状态 |
|------|----------|----------|----------|------------|-------------------|
| 01-Core Fundamentals | 8 | 0 | 2 | 10 | ✅ 稳定 |
| 02-Core Support | 1 | 7 | 0 | 8 | ✅ 稳定 |
| 03-Cursor | 5 | 2 | 2 | 9 | ✅ **P0-P2 完成** (94 tests) |
| 04-Decorations | 5 | 2 | 0 | 7 | ✅ **AcceptReplace 已集成** |
| 05-Diff | 9 | 5 | 2 | 16 | ⚠️ 稳定，renderer 待接入 |
| 06-Services | 4 | 4 | 2 | 10 | ✅ **FindModel 完成** (40 tests) |
| 07-Core Tests | 12 | 4 | 1 | 17 | ✅ **+287 tests** |
| 08-Feature Tests | 6 | 5 | 2 | 13 | ✅ **Snippet P0-P2** (77 tests) |
| **合计** | **50** | **29** | **11** | **90** | **873 passed / 9 skipped** |

### 质量画像

- **完全对齐:** 56% (50/90) – PieceTree 架构、Diff 核心算法、TextModel options/快照、**Snippet P0-P2**、**Cursor/WordOperations 核心**、**IntervalTree AcceptReplace**
- **存在偏差:** 32% (29/90) – Range/Selection 桥接、Decoration owner 策略、DocUI Find host、Diff renderer、国际化搜索
- **需要修正:** 12% (11/90) – Snippet P3（变量/Transform）、RangeMapping、DocUI diff + Markdown renderer、Language/Undo 服务

---

## 模块快照

- **01 Core Fundamentals** (`PieceTreeModel`/Builder/Chunk)：结构与 TS 一致，CRLF/搜索基础通过 `PieceTreeDeterministicTests`/`PieceTreeFuzzHarnessTests` 复核，但 `_lastChangeBufferPos` Telemetry 与 Info-Indexer changefeed（WS1-PORT-CRLF）尚未补齐，`NodeAt2` tuple reuse 仍待按 [`agent-team/handoffs/PORT-PT-Search-Plan.md`](../../agent-team/handoffs/PORT-PT-Search-Plan.md) 实装。
- **02 Core Support** (`Range`/`Selection`/`SearchTypes` 等)：已借由 `WS2-PORT` 引入 75 条 Range/Selection helper 测试（`#delta-2025-11-26-ws2-port`），但 `RangeMapping`、`SelectionRangeMapping`、Intl `WordCharacterClassifier`、`PieceTreeSearchCache.Validate` 默认值仍滞后；`TextMetadataScanner` 额外检测 NEL/RTL，也需记录差异。
- **03 Cursor**：✅ **Sprint 04 M2 完成** — `Cursor`/`CursorCollection`/`WordOperations` 全面对齐 TS 实现，`CursorCoreTests` + `CursorWordOperationsTests` 共 94 个测试通过，`CL7 cursor-core/wordops` 占位已关闭。
- **04 Decorations**：✅ **Sprint 04 M2 完成** — IntervalTree `AcceptReplace()` 四阶段算法已完全集成，`DecorationsTrees` 现可消费 NodeFlags；`DecorationOwnerIds` 语义已修正，DocUI find 装饰回归通过（15 tests）。
- **05 Diff**：Myers/DP 算法、Heuristic 优化、LineRange/Fragment/OffsetRange 对齐；`LineSequence.GetBoundaryScore` 的结尾索引与 TS 不同，`RangeMapping.Inverse/Clip/FromEdit`、`DetailedLineRangeMapping.ToTextEdit`、DocUI diff renderer 等缺失使 revert/move 功能无法复刻。
- **06 Services**：`TextModelOptions`/search stack parity完成；`TextModel` 仍缺 `ValidatePosition/Range`、`GetFullModelRange` 等公开入口，`IUndoRedoService` 无资源/分组/快照，`ILanguageConfigurationService` 仅保存订阅，DocUI Find 控制器默认持久化/剪贴板实现仍是 `Null` stub。
- **07 Core Tests**：PieceTree deterministic/fuzz/search-offset/snapshot 全系测试（WS5 harness + `#delta-2025-11-26-sprint04-r1-r11`）已落地，TextModel snapshot/indentation 亦在 QA 记录中；Cursor/Snippet/Diff suites多数仍缺，`TextModelIndentationTests` 保留 1 个 `GuessIndentation` skip。
- **08 Feature Tests**：✅ **Sprint 04 M2 完成** — DocUI Find Controller/Model/Decorations（40 tests）、**Snippet P0-P2**（77 tests，含 adjustWhitespace/Placeholder Grouping）、Cursor/WordOperations（94 tests）全部通过；剩余 P3 功能（Snippet 变量/Transform）按计划降级。

---

## 高优先级修正项 (P0)

> ✅ **Sprint 04 M2 已关闭以下 P0 项：**
> - ~~Cursor/Word/Snippet Stage 1~~ → 94 + 77 tests 通过
> - ~~IntervalTree AcceptReplace~~ → 已集成
> - ~~WS5 Top-10 测试缺口~~ → 核心场景已覆盖

1. **PieceTree 搜索/编辑回路** – 文件：`src/TextBuffer/Core/PieceTreeModel.Edit.cs`, `PieceTreeModel.Search.cs`. 需落地 `NodeAt2` tuple reuse、SearchCache diagnostics。
2. **DocUI Renderer & CL8 Backlog** – 文件：`src/TextBuffer/Rendering/MarkdownRenderer.cs`, `DocUI/*.cs`. `FindDecorations` owner 语义已修复，但 markdown/DocUI diff 渲染仍未接入 `DiffComputer` 输出。

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

- 新增 harness：`PieceTreeDeterministicTests`、`PieceTreeFuzzHarnessTests`、`PieceTreeSearchOffsetCacheTests`、`PieceTreeSnapshotParityTests`、`TextModelSnapshotTests`、`PieceTreeBufferApiTests`、`PieceTreeSearchRegressionTests`、`TextModelIndentationTests`、**`SnippetControllerTests` (77 tests)**、**`CursorWordOperationsTests` (94 tests)** 均记录在 `tests/TextBuffer.Tests/TestMatrix.md`。
- **Sprint 04 M2 测试覆盖提升：** Cursor (94)、Snippet (77)、DocUI Find (40)、IntervalTree (15)，总计 **+287 tests**。
- 仍待扩展：`DiffTests`（4/40+）、DocUI diff renderer、Intl word cache、Undo/Language 服务回归测试。

| 套件 | TS 用例 | 当前 C# | 状态 |
|------|---------|---------|------|
| CursorWordOperationsTests | ≈60 | **94** | ✅ **Sprint 04 M2 完成** |
| Cursor/Column MultiSelection | ≈70 | **94** (含 CursorCoreTests) | ✅ **P0-P2 完成** |
| SnippetController/SnippetSession | ≈60 | **77** (4 P2 skipped) | ✅ **P0-P2 完成** |
| DiffTests | 40+ | 4 | ⚠️ 待扩展 deterministic matrix |
| TextModelIndentationTests | 20 | 19 pass + 1 skip | ✅ 基本完成 |

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

### ✅ Sprint 04 M2 已完成
1. [x] CL7 Stage 1：`Cursor`/`CursorCollection` 已切换到 `CursorState`，word ops/snippet 行为已移植，94 + 77 tests 通过。
2. [x] IntervalTree `AcceptReplace()` 四阶段算法已集成，`DecorationOwnerIds` 语义已修正。
3. [x] WS5 Top-10 核心场景：Snippet P0-P2、WordOperations、FindModel/FindDecorations 已覆盖。

### 待办（Sprint 05+）
1. [ ] `PORT-PT-Search-Plan` Step1/2：恢复 `NodeAt2` tuple reuse + SearchCache 诊断。
2. [ ] CL8 Renderer：把 `FindDecorations` 接入 Markdown/DocUI diff renderer。
3. [ ] Diff deterministic matrix：扩展 `DiffTests` 覆盖 `defaultLinesDiffComputer.test.ts` 参数矩阵。
4. [ ] Snippet P3：变量解析（TM_FILENAME/CLIPBOARD 等）、Transform、Choice 功能。

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

- **2025-12-02 (Sprint 04 M2)**：全量基线 **873 passed / 9 skipped**，关键套件：
  - `SnippetControllerTests` 77/77 (4 P2 skipped)
  - `CursorCoreTests + CursorWordOperationsTests` 94/94 (5 skipped)
  - `IntervalTreeTests` 15/15
  - `DocUIFind*Tests` 40/40
- **2025-11-27**：按照 [`docs/reports/migration-log.md#sprint04-r1-r11`](../migration-log.md#sprint04-r1-r11) rerun `CRLFFuzzTests` (16/16)、`CursorCoreTests` (25/25) 及全量 585/585（1 skipped），并引用 [`#delta-2025-11-26-sprint04-r1-r11`](../../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11)。
- **01-Core Fundamentals**：参照 `PieceTreeModel.Edit.cs`, `PieceTreeModel.Search.cs`, `PieceTreeBuilder.cs` 与 TS `pieceTreeBase.ts`; 结合 `PieceTreeFuzzHarnessTests`, `PieceTreeDeterministicTests`（`#delta-2025-11-24-b3-piecetree-fuzz` 等 changefeed）。
- **02-Core Support**：审阅 `Range.Extensions.cs`, `Selection.cs`, `PieceTreeSearchCache.cs`, `SearchTypes.cs`, `TextMetadataScanner.cs` 与 TS 对应文件；验证范围 helper 通过 `RangeSelectionHelperTests` (75 data rows, `#delta-2025-11-26-ws2-port`)。
- **03-Cursor**：对照 `Cursor.cs`, `CursorCollection.cs`, `CursorColumns.cs`, `CursorState.cs`, `SnippetController.cs`, `SnippetSession.cs`；执行 `CursorCoreTests`, `CursorWordOperationsTests`, `SnippetMultiCursorFuzzTests` 并引用 `#delta-2025-11-26-aa4-cl7-*`。
- **04-Decorations**：检查 `IntervalTree.cs`, `DecorationsTrees.cs`, `DecorationOwnerIds.cs`, `ModelDecoration.cs`；复跑 `IntervalTreeTests`, `DecorationStickinessTests`, `DocUIFindDecorationsTests`（`B3-Decor-Stickiness-Review`）。
- **05-Diff**：核对 `DiffComputer.cs`, `ComputeMovedLines.cs`, `LineSequence.cs`, `RangeMapping.cs`, `DiffMove.cs`; 执行 `DiffTests`（`#delta-2025-11-23`）。
- **06-Services**：复核 `TextModel.cs`, `TextPosition.cs`, `ILanguageConfigurationService.cs`, `IUndoRedoService.cs`, `DocUIFindController.cs`; 参考 `DocUIFindControllerTests`, `TextModelTests`, `TextModelIndentationTests`（WS5-QA）。
- **07-Core Tests / 08-Feature Tests**：浏览 `PieceTree*Tests`, `TextModelSnapshotTests`, `DocUIFind*Tests`, `Cursor*Tests`, `Snippet*Tests`, `DecorationTests`, `DiffTests`，并对照 TS suite (`findController.test.ts`, `snippetSession.test.ts`, `cursorAtomicMoveOperations.test.ts` 等)。

*报告由 AI Team 自动生成*
