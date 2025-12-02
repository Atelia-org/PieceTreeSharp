# WS5 Gap Re-Assessment (2025-12-02)

## 概要

本文档对比 2025-11-26 `WS5-INV-TestBacklog.md` 识别的原 47 gaps (~106h) 与当前状态，评估完成进度并重新排序剩余工作。

**测试基线演进:**
- 2025-11-26: 585 passed
- 2025-12-02: **909 passed, 9 skipped** (+324 tests)

---

## 1. 原 Top-10 项目完成状态

| 优先级 | 原测试名称 | 原估计 | 当前状态 | 实际测试数 | 说明 |
|--------|-----------|--------|----------|-----------|------|
| #1 | cursorAtomicMoveOperations | 4h | ✅ **完成** | 21 tests | `CursorAtomicMoveTests` 覆盖 whitespaceVisibleColumn + atomicPosition 三方向矩阵 |
| #2 | wordOperations | 8h | ✅ **完成** | 38 tests (3 skipped) | `CursorWordOperationsTests` 覆盖 MoveWordLeft/Right/DeleteWord 系列 |
| #3 | multicursor | 6h | ⚠️ **部分** | 基础完成 | InsertCursorAbove/Below 基础已有，但 `SelectHighlightsAction` / `AddSelectionToNextFindMatch` 仍 skipped |
| #4 | snippetSession | 10h | ✅ **完成** | 77 tests (4 P2 skipped) | P0-P2 全部完成，包括 adjustWhitespace、Placeholder Grouping、navigation |
| #5 | snippetController2 | 6h | ✅ **降级完成** | 含在 #4 | Undo/redo 基础已有，variable/transform 降级为 P2 |
| #6 | PieceTree buffer API | 3h | ✅ **完成** | 24 tests | `PieceTreeBufferApiTests` 覆盖 equal/getLineCharCode/getNearestChunk |
| #7 | PieceTree search regressions | 2h | ✅ **完成** | 13 tests | `PieceTreeSearchRegressionTests` 覆盖 #45892/#45770 |
| #8 | TextModel guessIndentation | 4h | ⚠️ **部分** | 14 tests | 基本模式已覆盖，但完整 30+ 矩阵未完成，1 test skipped |
| #9 | modelDecorations | 6h | ✅ **完成** | 71 tests | `DecorationTests` + `IntervalTreeTests` 已覆盖 stickiness/AcceptReplace/change 场景 |
| #10 | defaultLinesDiffComputer | 5h | ✅ **完成** | 40 tests | `DiffTests` + `RangeMappingTests` 覆盖 M1/M2 全部场景 |

**Top-10 完成率: 8/10 完成，2/10 部分完成**

---

## 2. 按模块分组的剩余 Gaps

### 2.1 Cursor/Selection（剩余 ~14h）

| 测试名称 | 原状态 | 当前状态 | 剩余工时 | 优先级 |
|---------|--------|----------|---------|--------|
| multicursor – InsertCursorAbove/Below | Gap | ⚠️ 基础完成 | 1h | P1 |
| multicursor – AddSelectionToNextFindMatch | Gap | ❌ Skipped | 2h | P1 |
| multicursor – SelectHighlightsAction | Gap | ❌ Skipped | 2h | P1 |
| multicursor – issue regressions (#26393 等) | Gap | ❌ Gap | 3h | P2 |
| columnSelection – Alt+Drag | Gap | ⚠️ 基础有 | 2h | P2 |
| columnSelection – word wrap | Gap | ❌ Gap | 1h | P3 |
| wordOperations – locale segmentation | Gap | ❌ 已砍 | 0h | - |

**小计:** ~11h 实际剩余（locale segmentation 已砍除）

### 2.2 Snippet（剩余 ~6h）

| 测试名称 | 原状态 | 当前状态 | 剩余工时 | 优先级 |
|---------|--------|----------|---------|--------|
| snippetSession – nested snippets | Gap | ❌ P2 Skipped | 2h | P2 |
| snippetSession – transform | Gap | ❌ P2 Skipped | 2h | P2 |
| snippetController2 – variables (TM_FILENAME 等) | Gap | ❌ P2 Skipped | 2h | P2 |
| snippetController2 – choice | Gap | ❌ P2 Skipped | 含在上 | P2 |

**小计:** ~6h（全部为 P2 降级项）

### 2.3 Diff/Compare（剩余 ~3h）

| 测试名称 | 原状态 | 当前状态 | 剩余工时 | 优先级 |
|---------|--------|----------|---------|--------|
| diffComputer – algorithm strategy switch | Gap | ✅ 完成 | 0h | - |
| diffComputer – unchangedRegions | Gap | ✅ 完成 | 0h | - |
| diffComputer – postProcessCharChanges | Gap | ⚠️ 基础有 | 1h | P2 |
| diffComputer – computeMoves 组合 | Gap | ✅ 完成 | 0h | - |
| diffComputer – 大文档性能测试 | Gap | ❌ Gap | 2h | P2 |

**小计:** ~3h

### 2.4 TextModel（剩余 ~8h）

| 测试名称 | 原状态 | 当前状态 | 剩余工时 | 优先级 |
|---------|--------|----------|---------|--------|
| TextModelData.fromString | Gap | ❌ Gap | 2h | P1 |
| getValueLengthInRange + EOL variants | Gap | ⚠️ 基础有 | 1h | P2 |
| guessIndentation 完整矩阵 | Gap | ⚠️ 部分 | 2h | P1 |
| validatePosition (NaN/float/surrogate) | Gap | ❌ Gap | 1h | P2 |
| issue regressions (#44991 等) | Gap | ❌ Gap | 2h | P2 |

**小计:** ~8h

### 2.5 DocUI/Find（剩余 ~4h）

| 测试名称 | 原状态 | 当前状态 | 剩余工时 | 优先级 |
|---------|--------|----------|---------|--------|
| findController – Mac global clipboard | Gap | ❌ Gap | 1h | P3 |
| findController – FindStartFocusAction | Gap | ❌ Gap | 1h | P3 |
| findDecorations – matchBeforePosition throttle | Gap | ❌ Gap | 1h | P3 |
| findDecorations – viewport buffer recalc | Gap | ⚠️ 基础有 | 1h | P3 |
| findUtilities – Intl.Segmenter fallback | Gap | ❌ 已砍 | 0h | - |

**小计:** ~4h（Intl.Segmenter 已砍除）

### 2.6 其他特性（剩余 ~6h）

| 测试名称 | 原状态 | 当前状态 | 剩余工时 | 优先级 |
|---------|--------|----------|---------|--------|
| bracketMatching – pair colorization | Gap | ❌ Gap | 3h | P3 |
| bracketMatching – balanced detection | Gap | ❌ Gap | 1h | P3 |
| editStack – undo/redo boundaries | Gap | ⚠️ 基础有 | 1h | P2 |
| textChange – operation merge | Gap | ❌ Gap | 1h | P3 |

**小计:** ~6h

---

## 3. 剩余 Gaps 汇总

### 按优先级统计

| 优先级 | Gap 数量 | 估计工时 |
|--------|---------|---------|
| P1 | 5 | ~11h |
| P2 | 12 | ~20h |
| P3 | 9 | ~11h |
| **总计** | **26** | **~42h** |

### 完成进度

- **原始 Gaps:** 47 (~106h)
- **已完成:** 21 (含砍除项)
- **剩余 Gaps:** 26 (~42h)
- **完成率:** ~55% (按任务数) / ~60% (按工时)

---

## 4. 建议的下一批 TODO（5-10 个具体任务）

### 立即可做 (Sprint 05 M3)

1. **[P1] guessIndentation 完整矩阵** (2h)
   - 扩展 `TextModelIndentationTests` 覆盖 TS 30+ 输入组合
   - 解除 skipped test

2. **[P1] TextModelData.fromString 测试** (2h)
   - 新增 `TextModelDataTests` 覆盖单行/多行/Non Basic ASCII/containsRTL

3. **[P1] AddSelectionToNextFindMatch 实现** (2h)
   - 解除 `CursorCoreTests.SelectHighlightsAction_ParityPending` skip
   - 需要 `FindModel` 集成

4. **[P1] MultiCursor Snippet 集成** (2h)
   - 解除 `CursorCoreTests.MultiCursorSnippetIntegration_ParityPending` skip

5. **[P2] Diff 大文档性能测试** (2h)
   - 新增 `DiffPerfTests` 覆盖 10K+ 行文档
   - 验证 `maxComputationTimeMs` 超时行为

### 后续批次

6. **[P2] Snippet nested/transform** (4h)
   - 解除 4 个 P2 skipped tests
   - 需要 `SnippetParser` 扩展

7. **[P2] validatePosition 边界** (1h)
   - 扩展 `TextModelTests` 覆盖 NaN/float/surrogate

8. **[P2] editStack undo boundaries** (1h)
   - 验证 `UndoRedoService` 边界行为

9. **[P3] bracketMatching 基础** (4h)
   - 新增 `BracketMatchingTests` 覆盖平衡检测

10. **[P3] Mac global clipboard** (1h)
    - 仅在有 Mac CI 时处理

---

## 5. 架构建议

### 共享 Harness 已完成

- ✅ `TestEditorBuilder`
- ✅ `CursorTestHelper`
- ✅ `WordTestUtils`
- ✅ `SnapshotTestUtils`

### 待补充 Harness

| Helper | 用途 | 优先级 |
|--------|------|--------|
| `MultiCursorTestContext` | 支持 InsertCursorAbove/Below/AddSelectionToNextFindMatch | P1 |
| `DiffPerfHarness` | 大文档 diff 性能测试框架 | P2 |
| `IndentationTestData` | guessIndentation 完整矩阵数据 | P1 |

---

## 6. Changefeed Anchors

- `#delta-2025-12-02-ws5-gap-assessment` — 本文档
- `#delta-2025-11-26-ws5-test-backlog` — 原始 backlog
- `#delta-2025-11-26-sprint04-r1-r11` — Sprint 04 deliverables

---

## 附录：已砍除项

以下 gaps 已根据 2025-12-02 决策砍除：

1. **Intl.Segmenter & WordSeparator cache** — LLM 用户不需要 CJK/Thai 分词
2. **wordOperations locale-aware segmentation** — 同上
3. **findUtilities Intl.Segmenter fallback** — 同上

这些项目从 backlog 移除，节省 ~5h 工时。
