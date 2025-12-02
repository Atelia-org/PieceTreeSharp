# WS5-INV — High-Risk Deterministic & Feature Test Backlog

## Intro
- **Changefeed Anchors:** [`#delta-2025-11-26-sprint04-r1-r11`](../../docs/reports/migration-log.md#delta-2025-11-26-sprint04-r1-r11) · [`#delta-2025-11-26-ws5-test-backlog`](../../docs/reports/migration-log.md#delta-2025-11-26-ws5-test-backlog)
- **Traceability:** Findings are captured in [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-ws5-test-backlog) with backlog references cross-linked from [`tests/TextBuffer.Tests/TestMatrix.md`](../../tests/TextBuffer.Tests/TestMatrix.md#ws5).
- **Scope Summary:** Audit Sprint 04 coverage gaps and produce an ordered backlog of deterministic and feature tests required for cursor/snippet/piecetree/diff parity.
- **TS References:** Primary sources include `cursorAtomicMoveOperations.test.ts`, `wordOperations.test.ts`, `pieceTreeTextBuffer.test.ts`, `snippetSession.test.ts`, `diffComputer.test.ts`, and related VS Code harness docs cited below.

## Implementation

### 1. Top-10 优先级列表

基于风险等级、实现就绪度和复杂度的综合评估，以下是最高优先级的 10 个测试缺口：

| 优先级 | 测试名称/描述 | TS 源文件路径 | 覆盖模块 | 风险等级 | 估计工时(h) | 前置依赖 |
| --- | --- | --- | --- | --- | --- | --- |
| **#1** | **cursorAtomicMoveOperations – whitespaceVisibleColumn + atomicPosition** | `ts/src/vs/editor/test/common/controller/cursorAtomicMoveOperations.test.ts` | Cursor | P0 | 4 | `CursorColumns` API, Tab stop 计算 |
| **#2** | **wordOperations – cursorWordLeft/Right/Start/End + delete** | `ts/src/vs/editor/contrib/wordOperations/test/browser/wordOperations.test.ts` | Cursor | P0 | 8 | `WordCharacterClassifier`, `CursorWordOperations` |
| **#3** | **multicursor – InsertCursorAbove/Below + AddSelectionToNextFindMatch** | `ts/src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts` | Cursor | P0 | 6 | `CursorCollection`, `MultiCursorSelection` |
| **#4** | **snippetSession – placeholder navigation + nested + transform** | `ts/src/vs/editor/contrib/snippet/test/browser/snippetSession.test.ts` | Cursor/Snippet | P0 | 10 | `SnippetSession`, `SnippetParser`, variable 解析 |
| **#5** | **snippetController2 – undo/redo + cancel + merge** | `ts/src/vs/editor/contrib/snippet/test/browser/snippetController2.test.ts` | Cursor/Snippet | P0 | 6 | `SnippetController`, EditStack 集成 |
| **#6** | **PieceTree buffer API – equal/getLineCharCode/getNearestChunk** | `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` (lines 1750-1888) | PieceTree | P1 | 3 | `PieceTreeBufferAssertions` |
| **#7** | **PieceTree search regressions – #45892 empty model + #45770 node boundary** | `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` | PieceTree | P1 | 2 | `PieceTreeSearcher` |
| **#8** | **TextModel – guessIndentation matrix (30+ inputs)** | `ts/src/vs/editor/test/common/model/textModel.test.ts` (guessIndentation suite) | TextModel | P1 | 4 | `IndentationGuesser` |
| **#9** | **modelDecorations – lineHasDecorations + change/remove + EOL combo** | `ts/src/vs/editor/test/common/model/modelDecorations.test.ts` | Decorations | P1 | 6 | `DecorationsTrees`, `IntervalTree` |
| **#10** | **defaultLinesDiffComputer – algorithm/params matrix + large doc perf** | `ts/src/vs/editor/test/common/diff/diffComputer.test.ts` | Diff | P2 | 5 | `DiffComputer`, perf harness |

### 详细说明

#### #1 cursorAtomicMoveOperations (P0, 4h)
- **TS 位置:** `ts/src/vs/editor/test/common/controller/cursorAtomicMoveOperations.test.ts`
- **覆盖内容:** 
  - `whitespaceVisibleColumn` 数据驱动表（8 种 lineContent × position → expectedPrevTabStopPosition/VisibleColumn）
  - `atomicPosition` 三方向矩阵（Left/Right/Nearest × 6 种 lineContent）
- **C# 现状:** `ColumnSelectionTests` 仅有 3 个基础测试，未覆盖 tab stop 算法
- **就绪度:** `CursorColumns.VisibleColumnFromColumn` 已实现，需扩展 `AtomicTabMoveOperations`
- **依赖:** 无外部依赖

#### #2 wordOperations (P0, 8h)
- **TS 位置:** `ts/src/vs/editor/contrib/wordOperations/test/browser/wordOperations.test.ts` (1007 行)
- **覆盖内容:**
  - `cursorWordLeft/Right/StartLeft/EndLeft/StartRight/EndRight` 系列
  - `deleteWordLeft/Right/StartLeft/EndLeft/StartRight/EndRight`
  - `deleteInsideWord`
  - Issue 回归: #832, #48046, #169904, #74369, #51119
  - Locale-aware word segmentation (日语等)
- **C# 现状:** `CursorWordOperationsTests` 仅有 3 个 smoke tests
- **就绪度:** `WordCharacterClassifier` 已实现，`CursorWordOperations` 需大幅扩展
- **依赖:** 无外部依赖，但需要 `wordTestUtils` 迁移

#### #3 multicursor (P0, 6h)
- **TS 位置:** `ts/src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts` (597 行)
- **覆盖内容:**
  - `InsertCursorAbove/Below` + word wrap 场景
  - `AddSelectionToNextFindMatchAction` + multiline + touching ranges
  - `SelectHighlightsAction` + regex
  - Issue 回归: #26393, #2205, #1336, #8817, #5400, #6661, #23541
- **C# 现状:** `CursorMultiSelectionTests` 仅有 2 个渲染测试
- **就绪度:** `CursorCollection` 存在但功能有限，需要 `MultiCursorSelectionController`
- **依赖:** `CommonFindController` 集成

#### #4 snippetSession (P0, 10h)
- **TS 位置:** `ts/src/vs/editor/contrib/snippet/test/browser/snippetSession.test.ts` (807 行)
- **覆盖内容:**
  - `normalize whitespace` + tab/space 调整
  - `adjustSelection (overwriteBefore/After)`
  - `text edits & selection` + 多光标
  - `repeated tabstops` + placeholder 导航
  - `selections → next/prev` 循环
  - `selections & typing` 实时编辑
  - Nested snippets, transform, choice
- **C# 现状:** `SnippetControllerTests` 仅有 1 个基础测试 + 1 个 fuzz
- **就绪度:** `SnippetSession` 存在但功能不完整
- **依赖:** `SnippetParser`, `LanguageConfigurationService`

#### #5 snippetController2 (P0, 6h)
- **TS 位置:** `ts/src/vs/editor/contrib/snippet/test/browser/snippetController2.test.ts`
- **覆盖内容:**
  - Undo/redo 与 snippet session 交互
  - Cancel session + 清理装饰
  - Merge sessions (递归 snippet)
  - Variable 求值 (`TM_FILENAME`, `CLIPBOARD`, `UUID`)
  - Issue 回归: #27543 (recursive), #31619 (delete placeholder)
- **C# 现状:** 未覆盖
- **就绪度:** 需要 `SnippetController` 与 `EditStack` 集成
- **依赖:** `IClipboardService`, `IWorkspaceContextService`

#### #6 PieceTree buffer API (P1, 3h)
- **TS 位置:** `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` (lines 1750-1888)
- **覆盖内容:**
  - `buffer.equal(other)` 内容相等比较
  - `getLineCharCode` + issue #45735
  - `getNearestChunk` 优化路径
- **C# 现状:** 未覆盖
- **就绪度:** API 部分存在，需添加断言 helper
- **依赖:** 无

#### #7 PieceTree search regressions (P1, 2h)
- **TS 位置:** `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts`
- **覆盖内容:**
  - Issue #45892: 空缓冲区搜索返回空数组
  - Issue #45770: 节点边界搜索偏移正确性
- **C# 现状:** 未覆盖
- **就绪度:** 高 – 只需添加 2 个 `[Fact]`
- **依赖:** 无

#### #8 TextModel guessIndentation (P1, 4h)
- **TS 位置:** `ts/src/vs/editor/test/common/model/textModel.test.ts`
- **覆盖内容:**
  - 30+ 种输入组合的缩进检测矩阵
  - Tab size / insert spaces 推断
- **C# 现状:** 未覆盖
- **就绪度:** `IndentationGuesser` 存在，需添加 `[Theory]` 数据
- **依赖:** 无

#### #9 modelDecorations (P1, 6h)
- **TS 位置:** `ts/src/vs/editor/test/common/model/modelDecorations.test.ts` (1409 行)
- **覆盖内容:**
  - `lineHasDecorations` / `modelHasDecorations` helper 断言
  - `changeDecorations` 多重 remove/change
  - EOL 切换后装饰范围调整
  - `deltaDecorations` no-op 优化
  - `TrackedRangeStickiness` 完整矩阵
- **C# 现状:** `DecorationTests` 12 个 + `DecorationStickinessTests` 4 组合
- **就绪度:** 基础设施完备，需补齐 40+ 用例
- **依赖:** 无

#### #10 defaultLinesDiffComputer (P2, 5h)
- **TS 位置:** `ts/src/vs/editor/test/common/diff/diffComputer.test.ts` (1092 行)
- **覆盖内容:**
  - `AlgorithmStrategy` 切换 (advanced/legacy)
  - `unchangedRegions` 检测
  - `postProcessCharChanges` 优化
  - `computeMoves` + `minMatchLength` 组合
  - 大文档性能测试
- **C# 现状:** `DiffTests` 4 个基础测试
- **就绪度:** `DiffComputer` 存在，需扩展参数矩阵
- **依赖:** Perf harness 基础设施

---

### 2. 按模块分组的完整 Backlog

### 2.1 PieceTree 相关测试

| 测试名称 | TS 源文件 | 状态 | 优先级 | 工时 |
| --- | --- | --- | --- | --- |
| buffer API (equal/getLineCharCode/getNearestChunk) | `pieceTreeTextBuffer.test.ts:1750-1888` | Gap | P1 | 3h |
| search regression #45892 (empty model) | `pieceTreeTextBuffer.test.ts` | Gap | P1 | 1h |
| search regression #45770 (node boundary) | `pieceTreeTextBuffer.test.ts` | Gap | P1 | 1h |
| AssertTreeInvariants 统一调用 | `pieceTreeTextBuffer.test.ts` | Partial | P2 | 2h |
| random insert/delete \r bug 1-5 | `pieceTreeTextBuffer.test.ts:1668-1725` | ✅ Covered | - | - |
| prefix sum 全套 | `pieceTreeTextBuffer.test.ts:560-700` | ✅ Covered | - | - |
| CRLF normalization | `pieceTreeTextBuffer.test.ts:1054-1589` | ✅ Covered | - | - |
| centralized lineStarts | `pieceTreeTextBuffer.test.ts` | ✅ Covered | - | - |
| immutable snapshot 1-3 | `pieceTreeTextBuffer.test.ts:1888-1998` | ✅ Covered | - | - |
| search offset cache | `pieceTreeTextBuffer.test.ts` | ✅ Covered | - | - |

**小计:** 3 Gap (5h), 2 Partial (2h), 6 Covered

### 2.2 Cursor/Selection 相关测试

| 测试名称 | TS 源文件 | 状态 | 优先级 | 工时 |
| --- | --- | --- | --- | --- |
| cursorAtomicMoveOperations (whitespaceVisibleColumn) | `cursorAtomicMoveOperations.test.ts` | Gap | P0 | 2h |
| cursorAtomicMoveOperations (atomicPosition) | `cursorAtomicMoveOperations.test.ts` | Gap | P0 | 2h |
| wordOperations – cursorWordLeft/Right 系列 | `wordOperations.test.ts` | Gap | P0 | 4h |
| wordOperations – deleteWord 系列 | `wordOperations.test.ts` | Gap | P0 | 2h |
| wordOperations – locale-aware segmentation | `wordOperations.test.ts` | Gap | P1 | 2h |
| multicursor – InsertCursorAbove/Below | `multicursor.test.ts` | Gap | P0 | 2h |
| multicursor – AddSelectionToNextFindMatch | `multicursor.test.ts` | Gap | P0 | 2h |
| multicursor – SelectHighlightsAction | `multicursor.test.ts` | Gap | P0 | 2h |
| multicursor – issue regressions | `multicursor.test.ts` | Gap | P1 | 3h |
| columnSelection – Alt+Drag | `multicursor.test.ts` | Gap | P1 | 2h |
| columnSelection – word wrap | `multicursor.test.ts` | Gap | P2 | 1h |

**小计:** 11 Gap (24h), 0 Covered

### 2.3 Snippet 相关测试

| 测试名称 | TS 源文件 | 状态 | 优先级 | 工时 |
| --- | --- | --- | --- | --- |
| snippetSession – normalize whitespace | `snippetSession.test.ts` | Gap | P0 | 2h |
| snippetSession – adjustSelection | `snippetSession.test.ts` | Gap | P0 | 1h |
| snippetSession – text edits & selection | `snippetSession.test.ts` | Gap | P0 | 2h |
| snippetSession – repeated tabstops | `snippetSession.test.ts` | Gap | P0 | 1h |
| snippetSession – next/prev navigation | `snippetSession.test.ts` | Gap | P0 | 2h |
| snippetSession – typing interaction | `snippetSession.test.ts` | Gap | P0 | 2h |
| snippetController2 – undo/redo | `snippetController2.test.ts` | Gap | P0 | 2h |
| snippetController2 – cancel/merge | `snippetController2.test.ts` | Gap | P0 | 2h |
| snippetController2 – variables | `snippetController2.test.ts` | Gap | P1 | 2h |
| snippetParser | `snippetParser.test.ts` | Gap | P1 | 3h |
| snippetVariables | `snippetVariables.test.ts` | Gap | P1 | 3h |

**小计:** 11 Gap (22h), 0 Covered

### 2.4 Diff/Compare 相关测试

| 测试名称 | TS 源文件 | 状态 | 优先级 | 工时 |
| --- | --- | --- | --- | --- |
| diffComputer – algorithm strategy | `diffComputer.test.ts` | Gap | P2 | 2h |
| diffComputer – unchangedRegions | `diffComputer.test.ts` | Gap | P2 | 1h |
| diffComputer – postProcessCharChanges | `diffComputer.test.ts` | Gap | P2 | 1h |
| diffComputer – computeMoves | `diffComputer.test.ts` | Gap | P2 | 1h |
| diffComputer – large doc perf | `diffComputer.test.ts` | Gap | P2 | 2h |
| word diff inner changes | `diffComputer.test.ts` | ✅ Covered | - | - |
| ignoreTrimWhitespace | `diffComputer.test.ts` | ✅ Covered | - | - |
| move detection | `diffComputer.test.ts` | ✅ Covered | - | - |

**小计:** 5 Gap (7h), 3 Covered

### 2.5 DocUI/Find 相关测试

| 测试名称 | TS 源文件 | 状态 | 优先级 | 工时 |
| --- | --- | --- | --- | --- |
| findController – Mac global clipboard | `findController.test.ts` | Gap | P2 | 2h |
| findController – FindStartFocusAction context keys | `findController.test.ts` | Gap | P2 | 2h |
| findDecorations – matchBeforePosition throttle | `findModel.test.ts` | Gap | P2 | 1h |
| findDecorations – viewport buffer recalc | `findModel.test.ts` | Gap | P2 | 2h |
| findUtilities – Intl.Segmenter fallback | `find.test.ts` | Gap | P1 | 3h |
| findModel – all core scenarios | `findModel.test.ts` | ✅ Covered | - | - |
| findController – core issues | `findController.test.ts` | ✅ Covered | - | - |
| replacePattern | `replacePattern.test.ts` | ✅ Covered | - | - |

**小计:** 5 Gap (10h), 3 Covered

### 2.6 TextModel 相关测试

| 测试名称 | TS 源文件 | 状态 | 优先级 | 工时 |
| --- | --- | --- | --- | --- |
| TextModelData.fromString | `textModel.test.ts` | Gap | P1 | 2h |
| getValueLengthInRange + EOL variants | `textModel.test.ts` | Gap | P1 | 2h |
| guessIndentation 全矩阵 | `textModel.test.ts` | Gap | P1 | 4h |
| validatePosition (NaN/float/surrogate) | `textModel.test.ts` | Gap | P1 | 2h |
| issue regressions (#44991, #55818, #70832, #62143, #84217, #71480) | `textModel.test.ts` | Gap | P1 | 4h |
| Selection/Undo | `textModel.test.ts` | ✅ Covered | - | - |
| EOL/language settings | `textModel.test.ts` | ✅ Covered | - | - |

**小计:** 5 Gap (14h), 2 Covered

### 2.7 Decorations 相关测试

| 测试名称 | TS 源文件 | 状态 | 优先级 | 工时 |
| --- | --- | --- | --- | --- |
| lineHasDecorations helper | `modelDecorations.test.ts` | Gap | P1 | 1h |
| modelHasDecorations helper | `modelDecorations.test.ts` | Gap | P1 | 1h |
| changeDecorations multi remove/change | `modelDecorations.test.ts` | Gap | P1 | 2h |
| EOL toggle decoration adjustment | `modelDecorations.test.ts` | Gap | P1 | 2h |
| deltaDecorations no-op | `modelDecorations.test.ts` | Gap | P2 | 1h |
| TrackedRangeStickiness full matrix | `modelDecorations.test.ts` | Partial | P1 | 2h |
| InjectedText + lineBreak viewport | `modelDecorations.test.ts` | Gap | P2 | 2h |
| basic decoration CRUD | `modelDecorations.test.ts` | ✅ Covered | - | - |
| owner filtering | `modelDecorations.test.ts` | ✅ Covered | - | - |

**小计:** 7 Gap (11h), 1 Partial (2h), 2 Covered

### 2.8 其他特性测试

| 测试名称 | TS 源文件 | 状态 | 优先级 | 工时 |
| --- | --- | --- | --- | --- |
| bracketMatching – pair colorization | `bracketMatching.test.ts` | Gap | P2 | 4h |
| bracketMatching – balanced detection | `bracketMatching.test.ts` | Gap | P2 | 2h |
| intervalTree – TS parity | `intervalTree.test.ts` | Gap | P1 | 3h |
| editStack – undo/redo boundaries | `editStack.test.ts` | Gap | P1 | 2h |
| textChange – operation merge | `textChange.test.ts` | Gap | P2 | 2h |

**小计:** 5 Gap (13h), 0 Covered

---

### 3. 共享 Harness 需求

### 3.1 建议的共用 Fixture/Helper

| Helper 名称 | 用途 | 目标位置 | 优先级 |
| --- | --- | --- | --- |
| **`TestEditorBuilder`** | 构建带有预配置选项的 `TextModel` + `Cursor` 组合，支持多光标初始化 | `tests/TextBuffer.Tests/Helpers/TestEditorBuilder.cs` | P0 |
| **`CursorTestHelper`** | 封装 `Position` 序列化/反序列化（`|` 标记），支持 `cursorWordLeft` 等操作的重复执行与断言 | `tests/TextBuffer.Tests/Helpers/CursorTestHelper.cs` | P0 |
| **`WordTestUtils`** | 迁移 TS `wordTestUtils.ts` 中的 `deserializePipePositions` / `serializePipePositions` / `testRepeatedActionAndExtractPositions` | `tests/TextBuffer.Tests/Helpers/WordTestUtils.cs` | P0 |
| **`SnippetTestContext`** | 创建带有 mock 服务的 snippet session 测试环境，支持 `LabelService`, `LanguageConfigurationService`, `WorkspaceContextService` | `tests/TextBuffer.Tests/Helpers/SnippetTestContext.cs` | P0 |
| **`MultiCursorTestContext`** | 扩展 `TestEditorContext`，支持 `InsertCursorAbove/Below`、`AddSelectionToNextFindMatch` 等多光标操作 | `tests/TextBuffer.Tests/DocUI/MultiCursorTestContext.cs` | P0 |
| **`DecorationTestHelper`** | 封装 `modelHasDecorations` / `lineHasDecorations` / `modelHasNoDecorations` 断言 | `tests/TextBuffer.Tests/Helpers/DecorationTestHelper.cs` | P1 |
| **`DiffPerfHarness`** | 大文档 diff 性能测试框架，支持超时检测和内存跟踪 | `tests/TextBuffer.Tests/Helpers/DiffPerfHarness.cs` | P2 |
| **`IndentationTestData`** | `guessIndentation` 测试的 30+ 输入矩阵数据 | `tests/TextBuffer.Tests/Data/IndentationTestData.cs` | P1 |

### 3.2 TS Oracle Ingestion 策略

#### 3.2.1 脚本数据静态导入
对于确定性测试（如 `cursorAtomicMoveOperations` 的数据表），建议：

1. **直接翻译为 C# 数据类：**
   ```csharp
   public static class AtomicMoveTestData
   {
       public static readonly TestCase[] WhitespaceVisibleColumnCases = new[]
       {
           new TestCase("        ", 4, new[] {-1, 0, 0, 0, 0, 4, 4, 4, 4, -1}, ...),
           new TestCase("  ", 4, new[] {-1, 0, 0, -1}, ...),
           // ... 从 TS 直接翻译
       };
   }
   ```

2. **使用 `[Theory]` + `[MemberData]`：**
   ```csharp
   [Theory]
   [MemberData(nameof(AtomicMoveTestData.WhitespaceVisibleColumnCases))]
   public void WhitespaceVisibleColumn_MatchesTsOracle(TestCase testCase)
   {
       // 断言
   }
   ```

#### 3.2.2 TS 测试脚本自动提取
对于复杂的 fuzz/随机测试序列：

1. **现有模式延续：**
   - 已有 `PieceTreeDeterministicScripts.cs` 模式，包含 TS 随机脚本的 C# 翻译
   - 继续使用此模式添加新脚本

2. **脚本命名约定：**
   ```csharp
   public static class CursorDeterministicScripts
   {
       // 格式: {测试名}_{TS行号范围}
       public static readonly Operation[] WordLeftSimple_L123_140 = ...;
   }
   ```

#### 3.2.3 Golden Output 快照
对于渲染/序列化测试：

1. **存放位置：** `tests/TextBuffer.Tests/Snapshots/{ModuleName}/`
2. **文件格式：** JSON 或纯文本，与 TS `*.expected` 文件对应
3. **验证方式：** `Verify.Xunit` 或自定义快照比较

#### 3.2.4 TS 测试直接执行（未来方向）
长期可考虑：
- 使用 Jint 或类似引擎直接执行 TS 测试逻辑生成 oracle 数据
- 当前阶段不建议，因为维护成本高于手动翻译

---

### 4. 实施建议

### 4.1 Sprint 04 集成计划

| 周次 | 任务 | Owner | 产出 |
| --- | --- | --- | --- |
| W1 (11/27-12/01) | 完成 #1 cursorAtomicMoveOperations | Porter-CS | `CursorAtomicMoveTests.cs` (16 Facts) |
| W1 | 完成 #7 PieceTree search regressions | Porter-CS | 扩展 `PieceTreeSearchTests.cs` (+2 Facts) |
| W1 | 共享 harness 初始化 | Porter-CS | `TestEditorBuilder`, `WordTestUtils` |
| W2 (12/02-12/08) | 完成 #2 wordOperations 核心 | Porter-CS | `CursorWordOperationsTests.cs` (+30 Facts) |
| W2 | 完成 #3 multicursor 核心 | Porter-CS | `MultiCursorTests.cs` (20 Facts) |
| W2 | 完成 #6 PieceTree buffer API | Porter-CS | 扩展 `PieceTreeModelTests.cs` (+5 Facts) |
| W3 (12/09-12/12) | 完成 #4/#5 snippet 核心 | Porter-CS | `SnippetSessionTests.cs` (25 Facts) |
| W3 | 完成 #8 guessIndentation | Porter-CS | 扩展 `TextModelTests.cs` (+30 Theories) |
| W3+ | #9/#10 decorations/diff 扩展 | Porter-CS | 按需 |

### 4.2 风险缓解

1. **Snippet 复杂度：** 可能需要额外 Sprint 周期完成完整 session 管理
2. **Locale 依赖：** `Intl.Segmenter` 对应的 C# 方案（ICU4N）需要评估
3. **Perf harness：** Diff 大文档测试可能需要 CI 资源调整

---

### 5. 更新记录

| 日期 | 更新者 | 内容 |
| --- | --- | --- |
| 2025-11-26 | Investigator-TS (Evan Holt) | 初始版本，完成 WS5-INV 任务 |

## Testing
| Command | Result |
| --- | --- |
| _Not Run_ | Planning-only investigation; no automated tests were executed for this backlog refresh. |

## Open Items
- 向 AI Team Leader 汇报此 backlog 并 confirm prioritization for Sprint 04 execution.
- Update `agent-team/task-board.md` WS5-INV 状态为 Done while spawning WS5-PORT/QA tasks per plan.
- Append the backlog summary to [`docs/reports/migration-log.md`](../../docs/reports/migration-log.md#delta-2025-11-26-ws5-test-backlog) and sync [`agent-team/members/investigator-ts.md`].
- Share harness requirements with WS5-PORT owners so helper scaffolding lands before new test suites begin.
