# Core Tests 模块对齐审查报告

**审查日期:** 2025-11-26  
**审查范围:** 17个核心测试套件（PieceTree、TextModel、编辑器表层）

## 概要
- 完全对齐 9/17，存在偏差 6/17，需修正 2/17。新增 `PieceTreeDeterministicTests`, `PieceTreeFuzzHarnessTests`, `PieceTreeSearchOffsetCacheTests`, `PieceTreeSnapshotParityTests`, `TextModelSnapshotTests` 以及 WS5-QA 投放的 `PieceTreeBufferApiTests`, `PieceTreeSearchRegressionTests`, `TextModelIndentationTests`，已覆盖 CRLF、快照、随机、搜索缓存、buffer API、缩进矩阵等先前缺失的 TS 套件。
- Sprint 04 Phase 8 基线为 **585/585（1 skip）**，锚定 [`agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`](../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11)；WS5-QA rerun 详情见 [`docs/reports/migration-log.md#ws5-qa`](../migration-log.md#ws5-qa)、[`agent-team/handoffs/WS5-QA-Result.md`](../../agent-team/handoffs/WS5-QA-Result.md) 与 [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md)。
- WS5-QA harness（PieceTree buffer/search + TextModel 缩进）采用共享命令 `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "PieceTreeBufferApiTests\|PieceTreeSearchRegressionTests\|TextModelIndentationTests" --nologo`（44/44 通过，1 skipped）以保证 deterministic 追溯，已写入上述 handoff/TestMatrix。
- 主要风险集中在 Cursor / Snippet 套件（覆盖率不足 10%）、`TextModelTests` 的 `guessIndentation` API 缺口、Decoration/Diff 的长尾覆盖；CursorWordOperations、snippet fuzz、Diff deterministic suite 仍绑定 `AA4 CL7` 占位（`#delta-2025-11-26-aa4-cl7-wordops`, `#delta-2025-11-26-aa4-cl7-snippet`, `#delta-2025-11-26-aa4-cl7-commands-tests`）。
- 下一步应优先扩展 search/diff deterministic matrix、补齐 cursor/snippet 端到端套件，并将 TS buffer API / decoration / diff 余下用例移植到 C#。

### 覆盖状态表

| 区域 | 测试文件 | 状态 | 变化摘要 |
| --- | --- | --- | --- |
| PieceTree | `PieceTreeBaseTests.cs` | ⚠️ | 基础 insert/delete + 缓存测试已对齐，但仍缺 `AssertTreeInvariants`，随机脚本迁移到 fuzz 套件。 |
| PieceTree | `PieceTreeFuzzHarnessTests.cs` | ✅ | 覆盖 TS random test / delete / CR bug / random chunks 套件，并新增 harness 自检。 |
| PieceTree | `PieceTreeDeterministicTests.cs` | ✅ | 新增 prefix sum、getTextInRange、CRLF/centralized lineStarts deterministic 脚本。 |
| PieceTree | `PieceTreeBuilderTests.cs` | ✅ | Chunk 拆分、BOM/RTL flag、跨块 CR 处理完全对齐。 |
| PieceTree | `PieceTreeFactoryTests.cs` | ✅ | 默认 EOL、混合换行归一化、首尾行文本场景齐备。 |
| PieceTree | `PieceTreeModelTests.cs` | ⚠️ | Append/CRLF/search fuzz 已有；buffer API parity 由 `PieceTreeBufferApiTests` 承担，但 `_lastChangeBufferPos`、`NodeAt2` 诊断与 cache instrumentation 仍缺。 |
| PieceTree | `PieceTreeBufferApiTests.cs` | ✅ | WS5-QA 新增 17 个 deterministic 场景（[`docs/reports/migration-log.md#ws5-qa`](../migration-log.md#ws5-qa)）覆盖 `equal`, `GetLineCharCode` (#45735/#47733), `getNearestChunk`; 证据见 [`agent-team/handoffs/WS5-QA-Result.md`](../../agent-team/handoffs/WS5-QA-Result.md) 与 [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md)。 |
| PieceTree | `PieceTreeNormalizationTests.cs` | ✅ | 与 deterministic 套件组合后覆盖 CRLF/centralized lineStarts 全套随机案例。 |
| PieceTree | `PieceTreeSearchTests.cs` | ⚠️ | 主流程覆盖完整，仍缺搜索缓存跨多节点矩阵及 fuzz delta；空模型/节点边界回归由 `PieceTreeSearchRegressionTests` 保障。 |
| PieceTree | `PieceTreeSearchRegressionTests.cs` | ✅ | WS5-QA 投放 9 个 regression（[`docs/reports/migration-log.md#ws5-qa`](../migration-log.md#ws5-qa)）锁定 `#45892` 空缓冲区、`#45770` 节点边界、search-after-edit；同样收录在 [`agent-team/handoffs/WS5-QA-Result.md`](../../agent-team/handoffs/WS5-QA-Result.md) 与 [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md)。 |
| PieceTree | `PieceTreeSearchOffsetCacheTests.cs` | ✅ | 新增 search offset cache 渲染 & EOL 归一化 4 个脚本。 |
| PieceTree | `PieceTreeSnapshotTests.cs` + `PieceTreeSnapshotParityTests.cs` | ✅ | 包含 bug #45564 与 `immutable snapshot 1-3` parity。 |
| TextModel | `TextModelTests.cs` | ⚠️ | Selection/undo 完整，但缺 `TextModelData.fromString`, `getValueLengthInRange`, `guessIndentation` 矩阵、`validatePosition` 与多条 issue 回归。 |
| TextModel | `TextModelIndentationTests.cs` | ⚠️ | WS5-QA 新增 19 个 +1 skipped（[`docs/reports/migration-log.md#ws5-qa`](../migration-log.md#ws5-qa)）覆盖 tab/space 模式与 `ModelRespectsTabSize`; skip 记录 `guessIndentation` API 差异，待 CL7/CL8 backlog 解决。 |
| TextModel | `TextModelSnapshotTests.cs` | ✅ | 覆盖 snapshot chunk 聚合、跳空、重复读取行为。 |
| TextModel | `TextModelSearchTests.cs` | ✅ | Range、word boundary、regex、多 issue 回归均已移植。 |
| 编辑器 | `DecorationTests.cs` | ⚠️ | Delta/stickiness/InjectedText 已覆盖，缺 TS `modelDecorations.test.ts` 中行级断言、change/remove/EOL 组合等 40+ 用例。 |
| 编辑器 | `DiffTests.cs` | ⚠️ | 基础 diff 逻辑存在，但缺 TS `defaultLinesDiffComputer.test.ts` 的参数矩阵、large doc/perf 场景；待 `#delta-2025-11-26-aa4-cl7-commands-tests` 填写 deterministic harness。 |
| 编辑器 | `CursorTests.cs` + `CursorMultiSelectionTests.cs` + `CursorWordOperationsTests.cs` | ❌ | 仅 smoke tests，未覆盖 `cursorAtomicMoveOperations`/`multicursor`/`wordOperations` 中的 tab stop、命令回放、撤销矩阵；CL7 backlog (`#delta-2025-11-26-aa4-cl7-wordops`, `#delta-2025-11-26-aa4-cl7-cursor-core`) 仍是 blockers。 |
| 编辑器 | `SnippetControllerTests.cs` + `SnippetMultiCursorFuzzTests.cs` | ❌ | 缺少 TS `snippetController2.test.ts`/`snippetSession.test.ts` 的嵌套占位符、变量求值、session 合并、undo/redo 等套件；依赖 `#delta-2025-11-26-aa4-cl7-snippet`。 |

## 详细分析

### PieceTree 套件

#### `PieceTreeBaseTests.cs` (⚠️)
- TS 段落: `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` lines 214-265。现已对齐基础 insert/delete/缓存测试并增加 `GetLineContent` 缓存失效验证。
- 缺口: 未统一调用 `AssertTreeInvariants`；随机脚本现由 `PieceTreeFuzzHarnessTests` 负责，但仍建议在基础测试中添加 tree integrity 断言。

#### `PieceTreeFuzzHarnessTests.cs` (✅)
- TS 段落: random test 1-3、random delete 1-3、`random insert/delete \r bug 1-5`、random chunks (lines 271-550, 1668-1725)。
- C# harness 重现全部脚本，并新增 `RunRandomEdits` 与 `HarnessDetectsExternalCorruption`，可通过 `dotnet test ... --filter PieceTreeFuzzHarnessTests` 运行。
- 后续可将 fuzz 日志路径写入 `AssertState` 失败信息以提升可诊断性。

#### `PieceTreeDeterministicTests.cs` (✅)
- TS 段落: prefix sum、offset→position、getTextInRange、CRLF random、centralized lineStarts + random chunk (lines ~560-1589)。
- 当前实现逐条移植脚本，复用了 `PieceTreeDeterministicScripts`。
- 建议在 `AssertState`/`AssertLineStarts` 失败时输出 harness diff，方便排查。

#### `PieceTreeBuilderTests.cs` & `PieceTreeFactoryTests.cs` (✅)
- 与 TS builder/factory 套件一一对应，无缺口。

#### `PieceTreeModelTests.cs` (⚠️)
- 追加优化、chunk split、CRLF fuzz、search cache 失效均已覆盖。
- Buffer API parity 现由 `PieceTreeBufferApiTests` 专门承载（见下文），因此本文件仅跟踪模型级 append/search/normalization；仍需在此补回 `_lastChangeBufferPos`/`NodeAt2` 诊断断言。
- 建议继续抽离 buffer helper 以避免与 `PieceTreeBufferApiTests` 重复，并在模型场景中加入 `PieceTreeSearchRegressionTests` 提供的 repro 以观察缓存互动。

#### `PieceTreeBufferApiTests.cs` (✅)
- 来自 [`docs/reports/migration-log.md#ws5-qa`](../migration-log.md#ws5-qa) 的 WS5-QA harness，17 个 `[Fact]` 覆盖 `equal`, `GetLineCharCode`（issues #45735/#47733）, `getNearestChunk`, post-edit buffer equality。
- 结果记录在 [`agent-team/handoffs/WS5-QA-Result.md`](../../agent-team/handoffs/WS5-QA-Result.md) 与 [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md)；保持 `PIECETREE_DEBUG=0` 以捕获 diff 输出。
- 后续应扩展到多片段 `equal` 与 BOM/RTL 组合，并将 helper 暴露给 `PieceTreeModelTests` 以去重复。

#### `PieceTreeNormalizationTests.cs` (✅)
- 虽本文件内容较少，但结合 `PieceTreeDeterministicTests` 中的 CRLF/centralized lineStarts/random chunk 套件已覆盖 TS 对应段落。
- 仍可考虑将 deterministic CRLF 测试迁入此文件以简化索引。

#### `PieceTreeSearchTests.cs` (⚠️)
- 绝大部分 `textModelSearch.test.ts` 场景已经实现。
- 仍缺 search cache 跨多节点 fuzz、随机 edits 后的 offset 归一化矩阵；针对性 regressions 由 `PieceTreeSearchRegressionTests` 承担。

#### `PieceTreeSearchRegressionTests.cs` (✅)
- WS5-QA harness（[`docs/reports/migration-log.md#ws5-qa`](../migration-log.md#ws5-qa)）实现 9 个 `[Fact]`，逐条覆盖 `#45892` 空缓冲区、`#45770` 节点边界、search-after-edit、search-from-middle 等 TS repro。
- Handoff/TestMatrix 记录命令：`export PIECETREE_DEBUG=0 && dotnet test ... --filter PieceTreeSearchRegressionTests --nologo`（9/9 通过）。
- 后续应把此套件纳入 nightly，以便当 `PieceTreeSearchTests` 增加 fuzz 行为时仍能在 deterministic 层定位问题。

#### `PieceTreeSearchOffsetCacheTests.cs` (✅)
- 新增 deterministic suite 覆盖 render whitespace + normalized insert 脚本，`AssertSearchCachePrimed` 保证缓存一致。
- 可将此断言抽象为公共 helper 供其他搜索测试使用。

#### `PieceTreeSnapshotTests.cs` & `PieceTreeSnapshotParityTests.cs` (✅)
- `PieceTreeSnapshotParityTests` 现已覆盖 TS `bug #45564` 与 `immutable snapshot 1-3`，验证 snapshot/ piece 不可变性。
- `PieceTreeSnapshotTests` 保留基本读取/不可变性 smoke tests。

### TextModel 套件

#### `TextModelTests.cs` (⚠️)
- Selection/Undo/EOL/语言设置等 11 个子区域已实现。
- 仍缺:
  1. `TextModelData.fromString` (单行/多行/Non Basic ASCII/containsRTL)
  2. `getValueLengthInRange` + 不同 EOL variant
  3. `guessIndentation` 全矩阵 (~30 输入)；WS5-QA 的 `TextModelIndentationTests` 覆盖部分典型模式但仍有 skip。
  4. `validatePosition` (NaN、浮点、代理对)
  5. Issue 回归 (#44991,#55818,#70832,#62143,#84217,#71480)
- 建议以 `[Theory]` 覆盖缩进矩阵，利用 `IndentationGuesser.GuessIndentation` 进行断言。

#### `TextModelIndentationTests.cs` (⚠️)
- WS5-QA 投放 19 个 `[Fact]` + 1 skipped，验证 tab/space detection、`ModelRespectsTabSizeOption`、`indentationGuessesAfterEdits` 等路径；记录在 [`docs/reports/migration-log.md#ws5-qa`](../migration-log.md#ws5-qa) 与 [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md)。
- 被跳过的 `GuessIndentationRespectMixedTabs` 仍因 API 行为差异等待 CL7/CL8 修复；需在 `TextModelTests` 中添加覆盖后再打开此测试。
- 建议把数据驱动 helper 下沉至 `TextModelIndentationData`，以便 Services 模块可复用同一表格。

#### `TextModelSnapshotTests.cs` (✅)
- 参照 `textModel.test.ts` 中 `TextModelSnapshot` 用例，验证 chunk 聚合 (64K/32K)、忽略空块、重复读取避免触源等行为。
- 可加入 `EnsureNoDoubleReadAfterNull` 以镜像 TS 最后一条测试。

#### `TextModelSearchTests.cs` (✅)
- 仍保持对 Range scope、word boundary、regex、多 issue (#3623,#27459,#27594,#53415,#74715,#100134) 的覆盖，无缺口。

### 编辑器表层

#### `DecorationTests.cs` (⚠️)
- 已覆盖 owner scopes、collapse on replace、stickiness、options parity、`OnDidChangeDecorations`、InjectedText、`forceMoveMarkers`。
- 未覆盖 `modelDecorations.test.ts` 中的 `lineHasDecorations`、`changeDecorations` 多重 remove/change、EOL 切换、`deltaDecorations` no-op、`TrackedRangeStickiness` 场景与 `modelHasNoDecorations` 等 helper 断言。
- 建议引入 C# 版本的 `modelHasDecorations`/`lineHasDecorations` helper，按 TS 顺序补充剩余 40+ 用例。

#### `DiffTests.cs` (⚠️)
- 当前仅覆盖 word diff 内部变更、忽略尾空白、move detection、超时。
- TS `defaultLinesDiffComputer.test.ts`（需从 VS Code 上游同步）包含 `AlgorithmStrategy`, `unchangedRegions`, `postProcessCharChanges`, 大文档性能与 `computeMoves`/`minCharacters` 组合。
- TODO: 引入 TS 脚本后补齐参数矩阵及大文档性能测试。

#### `CursorTests.cs` / `CursorMultiSelectionTests.cs` / `CursorWordOperationsTests.cs` (❌)
- 仅有基本移动/渲染/word 操作 smoke tests。
- TS `cursorAtomicMoveOperations.test.ts` 中的 tab stop/`Direction.Nearest`、`AtomicTabMoveOperations.whitespaceVisibleColumn` 表格未被移植；`multicursor.test.ts` 的 undo/redo/命令栈也缺失；`wordOperations.test.ts` 中的 delete/transpose/分类矩阵尚未覆盖。
- 需新增专门文件：`CursorAtomicMoveOperationsTests`, `MultiCursorCommandTests`, `WordOperationsTests`，复刻 TS 数据驱动表。

#### `SnippetControllerTests.cs` / `SnippetMultiCursorFuzzTests.cs` (❌)
- 目前只有基本插入与模糊测试。
- 缺少 `snippetController2.test.ts`、`snippetSession.test.ts` 中的嵌套占位符、变量求值 (`TM_FILENAME`, `CLIPBOARD`, `UUID`)、session merge/cancel、undo/redo、placeholder transform、重复触发等。
- 需要实现 snippet 变量 mock（参考 TS `SnippetVariables`）并移植所有核心场景。

## 待办清单

1. **PieceTree buffer/search diagnostics:** 扩展 `PieceTreeBufferApiTests` 以覆盖多片段 equality + BOM/RTL，并把 `_lastChangeBufferPos`/`NodeAt2` 断言加回 `PieceTreeModelTests`; `PieceTreeSearchRegressionTests` 需追加 cache instrumentation 以验证多节点 fuzz。
2. **TextModel parity:** 补齐 `TextModelData.fromString`, `getValueLengthInRange` (+不同 EOL), `guessIndentation` 全矩阵（解除 `TextModelIndentationTests` skip), `validatePosition`, 以及 issue 回归 (#44991,#55818,#70832,#62143,#84217,#71480)。
3. **Cursor/Snippet 套件 (AA4 CL7):** 依次移植 `cursorAtomicMoveOperations.test.ts`, `multicursor.test.ts`, `wordOperations.test.ts`, `snippetController2.test.ts`, `snippetSession.test.ts`，并将结果回填至 `#delta-2025-11-26-aa4-cl7-wordops`/`-snippet`/`-cursor-core` 占位。
4. **Decoration & Diff deterministic:** 复刻 TS `modelDecorations.test.ts` 与 `defaultLinesDiffComputer.test.ts` 全量用例，建立 DocUI/diff deterministic harness，交付给 `#delta-2025-11-26-aa4-cl7-commands-tests`。
5. **Indentation detection matrix:** 将 `TextModelIndentationTests` 的数据表抽象为共享 helper，并在 `TextModelTests`/Services suites 中复用以验证 `guessIndentation` API 修复。

## Verification Notes

- 证据: [`docs/reports/migration-log.md#ws5-qa`](../migration-log.md#ws5-qa)、[`agent-team/handoffs/WS5-QA-Result.md`](../../agent-team/handoffs/WS5-QA-Result.md)、[`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md)、[`agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11`](../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11)。
- 阅读: `tests/TextBuffer.Tests/PieceTreeDeterministicTests.cs`, `PieceTreeFuzzHarnessTests.cs`, `PieceTreeBufferApiTests.cs`, `PieceTreeSearchRegressionTests.cs`, `PieceTreeSearchOffsetCacheTests.cs`, `PieceTreeSnapshotParityTests.cs`, `TextModelIndentationTests.cs`, `TextModelSnapshotTests.cs`。
- 阅读: `tests/TextBuffer.Tests/PieceTreeModelTests.cs`, `PieceTreeSearchTests.cs`, `TextModelTests.cs`, `DecorationTests.cs`, `DiffTests.cs`, `CursorTests.cs`, `CursorMultiSelectionTests.cs`, `CursorWordOperationsTests.cs`, `SnippetControllerTests.cs`, `SnippetMultiCursorFuzzTests.cs`。
- 阅读: TS 源 `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`, `ts/src/vs/editor/test/common/model/textModel.test.ts`, `ts/src/vs/editor/test/common/controller/cursorAtomicMoveOperations.test.ts`, `ts/src/vs/editor/contrib/snippet/test/browser/snippetController2.test.ts`。
- 命令:
  - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeBufferApiTests --nologo` → 17/17（`WS5-QA` 记录）。
  - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSearchRegressionTests --nologo` → 9/9。
  - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelIndentationTests --nologo` → 19/19 通过 + 1 skipped（`guessIndentation` API 差异）。
  - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → 585/585（584 pass + 1 skip, ~62s）锚定 Sprint 04 Phase 8 基线。
