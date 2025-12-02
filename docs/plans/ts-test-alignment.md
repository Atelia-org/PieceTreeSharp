# TS Test Alignment Plan

## Goal
建立一套“分阶段移植 VS Code TypeScript 单元测试”方法，作为衡量 PieceTree/TextModel/DocUI 质量对齐度的主线路径。通过先列出原版测试清单，再按可移植性分级迁移，使 C# 代码库逐步获得与 TS 同级的回归保障，同时保持 Task Board / Sprint / changefeed 的一致更新。

## Guiding Principles
1. **完整清单先行**：先 inventory TS 端的测试套件（路径、模块、依赖），即使暂时无法移植，也要在文档中记录目标，避免盲区。
2. **可移植性分级**：按可行程度将测试分为 A/B/C 级（高/中/低）。优先迁移 A 级（纯模型逻辑、依赖少），B 级需适配测试桩或最小化替代品，C 级则记录阻塞条件和所需环境。
3. **阶段性推进**：每个 Sprint 选择一批 A/B 级测试，完成后通过 `docs/reports/migration-log.md` + changefeed 报告成果，并在 `TestMatrix.md` 中登记映射关系和覆盖度。
4. **双向验证**：迁移每批测试前后都运行 TS 原版与 C# 版（如可行），收集团队共用的 Fixture/Snapshot，确认为相同行为后再落入主干。
5. **文档驱动**：所有测试映射、分级评估、迁移状态都写入共享文档（本文 + `TestMatrix.md` + Task Board），保持 Info-Indexer 可引用的唯一事实来源。

## Phased Approach
| Phase | Scope | Key Actions | Deliverables |
| --- | --- | --- | --- |
| P0 – Inventory | 列举 TS 源测试（路径/描述/依赖） | Investigator-TS 收集 TS `*.test.ts` 列表；QA 建立 `docs/plans/ts-test-alignment.md#appendix` 表格 | TS 测试清单 + 初步分级建议 |
| P1 – High-portability Batch | 选择 A 级（核心模型、纯函数）测试迁移 | Porter-CS 迁移逻辑，QA 改写/创建 C# xUnit 用例，DocMaintainer 更新 `TestMatrix` 和 changefeed | 迁移后的测试文件、`dotnet test` 记录、迁移日志条目 |
| P2 – Medium-portability Batch | 处理需要轻量适配（依赖 services/stubs）的测试 | 按需创建 C# stub/service；QA 扩展 harness；记录残余阻塞 | 扩展后的测试、stub 文档、阻塞说明 |
| P3 – Complex/Low-portability Batch | 评估高度依赖 VS Code runtime 的测试 | 制定替代策略（DocUI snapshot、integration mock）；记录无法移植的理由与未来计划 | “不可移植”清单 + 替代验证方案 |
| Continuous | 每次合并一批测试即更新文档/索引 | 更新 `docs/reports/migration-log.md`、changefeed、Task Board/Sprint/AGENTS | 最新 delta + QA 基线 |

## Workflow Checkpoints
1. **Inventory Update**: Investigator-TS 运行 `runSubAgent` 提交 TS 测试列表及初步分级，写入本计划附录 + `TestMatrix.md` “TS Source” 列。
2. **Feasibility Review**: QA/Porter 评估每条记录，确认需要的 harness/stub，若需额外调研则再次调用 SubAgent（例如 DocMaintainer 查 VS Code test infra）。
3. **Execution Tickets**: 每批迁移在 Task Board 上建子任务（如 “AA4-010.TS-Tests.Batch1”），Sprint 文档中列出目标，并引用本计划章节。
4. **QA Verification**: 新测试合入后运行 `dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj` + 目标化 filter，记录命令、通过数、快照路径。
5. **Broadcast**: Info-Indexer 在 changefeed 新增 delta，DocMaintainer 同步 AGENTS/Sprint/Task Board/`TestMatrix`，保证所有文档引用相同的 delta anchor。

## Tracking & Documentation
- **Primary Tracker**: 本文件 `Appendix – TS Test Inventory`，记录 TS 路径、描述、可移植级别、C# 对应文件、状态、阻塞。
- **Supporting Docs**:
  - `tests/TextBuffer.Tests/TestMatrix.md`：新增 “TS Source” 列、映射 ID 与完成度。
  - `docs/reports/migration-log.md`：每次迁移一批测试添加行，注明 TS 参考文件与新增 xUnit 文件，记录 `dotnet test` 结果。
  - `agent-team/task-board.md` / `docs/sprints/*.md`：在相关阶段（AA4/AA5…）的任务描述中附 “TS Test Alignment – Batch #” 子弹。
  - `agent-team/indexes/README.md`：Info-Indexer 在 delta 中登记测试迁移状况供 AGENTS 等引用。

## Roles
- **Investigator-TS**：收集 TS 测试清单，分析依赖与行为。
- **Porter-CS**：迁移实现/测试代码，解决 C# harness 需求。
- **QA-Automation**：编写 xUnit、建立 snapshot/fuzz 工具，并维护 `TestMatrix`。
- **DocMaintainer**：保持文档一致性（AGENTS/Sprint/Task Board），审查计划变更。
- **Info-Indexer**：更新 changefeed，确保索引指向最新测试迁移状态。

## Live Checkpoints

### Sprint 04 M2 完成 ✅（2025-12-02）

**测试基线**: 873 passed, 9 skipped（56% 完全对齐）

| 模块 | 测试数 | 状态 | Anchor |
|------|--------|------|--------|
| Snippet | 77 tests (P0-P2) | ✅ 完成 | `#delta-2025-12-02-snippet-deterministic` |
| Cursor | 94 tests | ✅ 完成 | `#delta-2025-11-28-cl7-cursor-wiring` |
| IntervalTree | 20 tests | ✅ 完成 | `#delta-2025-11-26-ws3-qa` |
| FindModel/Controller | 76 tests | ✅ 完成 | `#delta-2025-11-24-b3-*` |
| WordOperations | 41 tests | ✅ 完成 | `#delta-2025-11-28-ws5-wordoperations` |

> 💡 **Sprint 03 及更早的 Checkpoint 历史**已压缩归档到 [`agent-team/archive/ts-alignment-checkpoints-history.md`](../../agent-team/archive/ts-alignment-checkpoints-history.md)。

### 剩余 Backlog

| 项目 | 状态 | 阻塞 |
|------|------|------|
| Intl.Segmenter parity | ⏸️ 延迟 | 需要 ICU4N 或文档化限制 |
| WordSeparator LRU cache | ⏸️ 延迟 | 性能优化，非功能阻塞 |
| DocUI MarkdownRenderer | ⏸️ 延迟 | CL8 收尾后继续 |

## Next Actions

> ✅ **已完成项目** — Sprint 04 M2 中以下任务已完成：
> - Appendix 表格 TS Source/Portability Tier 列已补齐
> - PieceTree/TextModel/Diff/Decorations snapshot 高可移植测试已迁移
> - B3-PieceTree-Fuzz-PLAN 已执行完成

**当前待办**：
1. ~~Investigator-TS 收集 TS 测试列表~~ → ✅ 完成（见 Appendix）
2. ~~TestMatrix 添加 TS Source 列~~ → ✅ 完成
3. ~~创建首批高可移植测试任务~~ → ✅ 完成（873 tests）
4. 继续迁移日志/changefeed 更新流程 → 进行中
5. ~~B3-PieceTree-Fuzz-PLAN 执行~~ → ✅ 完成

**Sprint 05 目标**：
- CL8 DocUI Markdown renderer 收尾
- Intl.Segmenter 替代方案评估（ICU4N 或文档化限制）
- 提升对齐度至 70%

## Appendix – TS Test Inventory (placeholder)
| TS Test File | Module Scope | Notes / Dependencies | Portability Tier (A/B/C) | Target C# Suite | Status |
| --- | --- | --- | --- | --- | --- |
| `ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts` | PieceTree builder, RB-tree invariants, search + snapshot sanity | Uses `PieceTreeTextBuffer/Base/Builder`, `WordCharacterClassifier`, `SearchData`, `createTextModel`, randomized fuzz helpers; blockers: deterministic RNG + word-separator adapter for .NET search hooks | B | `tests/TextBuffer.Tests/PieceTreeBaseTests.cs`, `PieceTreeBuilderTests.cs`, `PieceTreeSearchTests.cs` | Partial parity (C# lacks fuzz + invariant coverage). Priority #2 (search-offset cache) = ✅ Complete via R31–R34 (`INV/PORT/QA/DOC`) under [`#delta-2025-11-25-b3-search-offset`](../agent-team/indexes/README.md#delta-2025-11-25-b3-search-offset); QA logged `export PIECETREE_DEBUG=0 && dotnet test --filter PieceTreeSearchOffsetCacheTests --nologo` (5/5, 4.3s) + full `--nologo` sweep (324/324, 58.2s) in TestMatrix/migration log。 |
| `ts/src/vs/editor/test/common/model/textModel.test.ts` | TextModel lifecycle, BOM/EOL handling, indentation inference, listener contract | Depends on `TextModel`, `createModelServices`, `IInstantiationService`, `PLAINTEXT_LANGUAGE_ID`, `DisposableStore`; blockers: need lightweight instantiation + option plumbing identical to TS defaults | B | `tests/TextBuffer.Tests/TextModelTests.cs` | Basic cases exist; advanced option/events not ported |
| `ts/src/vs/editor/test/common/model/textModelSearch.test.ts` | TextModel regex/whole-word/multiline parity, CRLF compensation | Exercises `SearchParams.parseSearchRequest`, `SearchData`, and `Searcher` boundary helpers from `core/wordHelper.ts`/`wordCharacterClassifier.ts`; verifies `createFindMatch` capture arrays consumed by `contrib/find/browser/replacePattern.ts`. Porting requires reading `wordHelper.ts`, `wordCharacterClassifier.ts`, `common/model.ts` (SearchData), and `textModelSearch.ts`; blockers: shared WordSeparator cache + Intl.Segmenter parity + `RegexOptions` mismatch vs TS `strings.createRegExp`. | B | `tests/TextBuffer.Tests/TextModelSearchTests.cs` | Gap closed (Sprint 03 R36 / `#delta-2025-11-25-b3-textmodelsearch`): Investigator brief [`Review-20251125-Investigator.md`](../../agent-team/handoffs/Review-20251125-Investigator.md) 与 Porter memo [`B3-TextModelSearch-PORT.md`](../../agent-team/handoffs/B3-TextModelSearch-PORT.md) 证明 `SearchPatternUtilities.IsMultilineRegexSource` 已回归且 TS 45 项测试矩阵 (word boundary、multiline/CRLF、capture arrays、`parseSearchRequest`/`isMultiline`) 悉数移植到 `TextModelSearchTests.cs`。QA 路线：Porter-CS Run R36 记录 `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter FullyQualifiedName~TextModelSearchTests --nologo` → 45/45 green (2.0s)，QA-Automation Run R37 于 [`agent-team/handoffs/B3-TextModelSearch-QA.md`](../../agent-team/handoffs/B3-TextModelSearch-QA.md) 中复跑同一 filter（45/45，2.5s）并执行全量 `--nologo`（365/365，61.6s）以刷新 TestMatrix 总计；两条 rerun 均挂 `#delta-2025-11-25-b3-textmodelsearch`。Backlog：Intl.Segmenter word segmentation + WordSeparator cache/perf 仍列入 AA4 待办。 |
| `ts/src/vs/editor/contrib/find/test/browser/findWidget.test.ts` | _Expected but not found_ – FindWidget DOM layout, history, accessibility | TS repo only has `find.test.ts`, `findModel.test.ts`, `findController.test.ts`, `replacePattern.test.ts` under `contrib/find/test/browser`. No dedicated FindWidget DOM harness exists; widget tests are implicitly covered by `findController.test.ts` via `withAsyncTestCodeEditor` stubs. | C | _Deferred – DocUI harness needed_ | **Recommendation**: Skip DOM widget tests; focus on FindModel logic + controller commands (existing TS tests sufficient) |
| `ts/src/vs/editor/test/common/diff/diffComputer.test.ts` | Legacy `DiffComputer` line+char heuristics, trim whitespace toggles, edit replay | Depends on `legacyLinesDiffComputer`, `Range`, `createTextModel`, `Constants`; blockers: char-change pretty diff + whitespace flags flagged in `docs/reports/audit-checklist-aa3.md#cl3` | B | `tests/TextBuffer.Tests/DiffTests.cs` | Missing char-change assertions + pretty diff cases |
| `ts/src/vs/editor/test/common/model/modelDecorations.test.ts` | Decorations creation/removal, stickiness, per-line queries | Uses `TextModel`, `EditOperation`, `TrackedRangeStickiness`, `EndOfLineSequence` | B | `tests/TextBuffer.Tests/DecorationTests.cs` | ✅ Complete (Sprint 04) — stickiness + per-line queries implemented |
| `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts` | DocUI Find model binding (incremental search, highlight sync, replace state) | `FindModelBoundToEditorModel`, `FindReplaceState`, `withTestCodeEditor` | B | `DocUIFindModelTests.cs` | ✅ Complete (76 tests) |
| `ts/src/vs/editor/contrib/find/test/browser/findController.test.ts` | Command-layer find controller (actions, clipboard, context keys) | `CommonFindController`, `EditorAction`, `ClipboardService` | B | `DocUIFindControllerTests.cs` | ✅ Complete (27 tests) |
| `ts/src/vs/editor/contrib/find/test/browser/find.test.ts` | Selection-derived search string heuristics (`getSelectionSearchString`) | `withTestCodeEditor`, `Range`, `Position` | A | `DocUIFindSelectionTests.cs` | ✅ Complete (4 tests) |
| `ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts` | ReplacePattern parser + case-preserving builder logic | Pure logic: `parseReplaceString`, `ReplacePattern/Piece`, `buildReplaceStringWithCasePreserved`; blocker: none beyond wiring into DocUI replace state | A | `ReplacePatternTests.cs` (23 tests, 142/142) | ✅ Complete |
| `TODO – DocUI find widget DOM/snapshot suites (expected under ts/src/vs/editor/contrib/find/test/browser)` | Editor overlay widget layout/history/accessibility | Today’s repo only includes `find.test.ts`, `findModel.test.ts`, `findController.test.ts`, `replacePattern.test.ts` (all via `withTestCodeEditor`/`withAsyncTestCodeEditor`, `ServiceCollection`, clipboard/context key stubs); no `findWidget` DOM harness surfaced. Need Info-Indexer/DocMaintainer to locate upstream widget snapshot/browser tests (likely `findWidget.test.ts` or playwright harness) and plan how to stub DOM pieces (`FindWidget`, `Sash`, `ContextViewProvider`, history services). | C | _TBD (DocUI find widget harness + snapshot tests)_ | Research required: confirm source files + decide harness strategy before port |

---

## Appendix B – WordSeparator & SearchContext Specification

### TS Implementation Overview

#### 1. Core Components

**`wordHelper.ts`** (`ts/src/vs/editor/common/core/wordHelper.ts`):
- **`USUAL_WORD_SEPARATORS`**: Default string constant `\`~!@#$%^&*()-=+[{]}\\|;:'",.<>/?` 
- **`createWordRegExp(allowInWords)`**: Generates a RegExp for word detection, excluding chars in `allowInWords` from `USUAL_WORD_SEPARATORS`
- **`DEFAULT_WORD_REGEXP`**: Pre-compiled regex for standard word boundaries (includes numeric literals)
- **`ensureValidWordDefinition(wordDefinition)`**: Ensures RegExp has `g` flag, rebuilds if necessary
- **`getWordAtText(column, wordDefinition, text, textOffset, config)`**: 
  - Window-based search with time budget (default 150ms)
  - Recursively searches backward in sliding windows (default windowSize: 15)
  - Returns `IWordAtPosition { word, startColumn, endColumn }` or null
  - Used for "word-under-cursor" features (hover, selection expansion, etc.)

**`wordCharacterClassifier.ts`** (`ts/src/vs/editor/common/core/wordCharacterClassifier.ts`):
- **`WordCharacterClass` enum**: `Regular = 0`, `Whitespace = 1`, `WordSeparator = 2`
- **`WordCharacterClassifier` class**:
  - Constructor: `(wordSeparators: string, intlSegmenterLocales: string[])`
  - Extends `CharacterClassifier<WordCharacterClass>` (base class provides per-char lookup)
  - Sets `WordSeparator` class for each char in `wordSeparators`
  - Sets `Whitespace` class for space/tab
  - **Intl.Segmenter integration** (optional):
    - `_segmenter: Lazy<Intl.Segmenter>` if `intlSegmenterLocales.length > 0`
    - `_cachedLine` + `_cachedSegments[]`: Single-line cache for segmenter results
    - `findPrevIntlWordBeforeOrAtOffset(line, offset)`, `findNextIntlWordAtOrAfterOffset(line, offset)`
    - `_filterWordSegments()`: Filters to `isWordLike: true` segments only
- **`getMapForWordSeparators(wordSeparators, intlLocales)`**: 
  - LRU cache (size: 10) keyed by `"${wordSeparators}/${locales.join(',')}"`
  - Returns singleton `WordCharacterClassifier` instance for given config

**`textModelSearch.ts`** (`ts/src/vs/editor/common/model/textModelSearch.ts`):
- **`SearchParams` class**:
  - Fields: `searchString`, `isRegex`, `matchCase`, `wordSeparators: string | null`
  - `parseSearchRequest()`: Builds `SearchData` with `getMapForWordSeparators(wordSeparators, [])`
- **`SearchData` class**: 
  - Fields: `regex: RegExp`, `wordSeparators: WordCharacterClassifier | null`, `simpleSearch: string | null`
  - Used by `Searcher` and `TextModelSearch.findMatches()`
- **`Searcher` class**:
  - Constructor: `(wordSeparators: WordCharacterClassifier | null, searchRegex: RegExp)`
  - `reset(lastIndex)`: Sets `regex.lastIndex` and clears prev match tracking
  - `next(text)`: Executes regex, validates word boundaries via `isValidMatch()` if `wordSeparators` present
  - Handles zero-length matches (advances by 1–2 code points)
- **Word boundary helpers**:
  - `leftIsWordBounday()`, `rightIsWordBounday()`: Check adjacent char class
  - `isValidMatch()`: Validates both left/right boundaries for whole-word search

**FindModel integration** (`ts/src/vs/editor/contrib/find/browser/findModel.ts`):
- Line 434: `model.findNextMatch(..., this._state.wholeWord ? this._editor.getOption(EditorOption.wordSeparators) : null, ...)`
- Line 508: `model.findMatches(..., this._state.wholeWord ? this._editor.getOption(EditorOption.wordSeparators) : null, ...)`
- Line 530: `new SearchParams(..., this._state.wholeWord ? this._editor.getOption(EditorOption.wordSeparators) : null)`
- **Key pattern**: `wordSeparators` passed only when `wholeWord = true`, sourced from editor option `EditorOption.wordSeparators`

#### 2. Data Flow

```
EditorOption.wordSeparators (string)
    ↓ (when wholeWord = true)
SearchParams.wordSeparators
    ↓
SearchParams.parseSearchRequest()
    ↓
getMapForWordSeparators(wordSeparators, []) → WordCharacterClassifier (cached)
    ↓
SearchData.wordSeparators
    ↓
Searcher(wordSeparators, regex)
    ↓
Searcher.next(text) → isValidMatch() checks left/right boundaries
```

### C# Porting Status

#### Implemented ✅
- **`SearchParams` class** (`src/TextBuffer/Core/SearchTypes.cs`):
  - Fields match TS: `SearchString`, `IsRegex`, `MatchCase`, `WordSeparators`
  - `ParseSearchRequest()` creates `WordCharacterClassifier` when `!string.IsNullOrEmpty(WordSeparators)`
  - Returns `SearchData(regex, classifier, simpleSearch, isMultiline, isCaseSensitive)`
- **`SearchData` class**:
  - Fields: `Regex`, `WordSeparators: WordCharacterClassifier?`, `SimpleSearch`, `IsMultiline`, `IsCaseSensitive`
- **`WordCharacterClassifier` class**:
  - `Dictionary<int, WordCharacterClass>` for per-codepoint lookups
  - `GetClass(codePoint)` returns `WordCharacterClass` enum (Regular/Whitespace/WordSeparator)
  - `IsValidMatch(text, matchStartIndex, matchLength)` validates left/right boundaries
  - `IsSeparatorOrLineBreak(codePoint)` helper for boundary checks
  - Uses `UnicodeUtility` for surrogate pair handling (`TryGetCodePointAt`, `TryGetPreviousCodePoint`)
- **`PieceTreeSearcher` integration** (`src/TextBuffer/TextModelSearch.cs`):
  - Constructor: `(WordCharacterClassifier? wordSeparators, Regex regex)`
  - `Next(text)`: Calls `IsValidMatch()` when `_wordSeparators != null`
  - Used in `TextModelSearch.FindMatches()`, `FindNextMatch()`, `FindPreviousMatch()`

#### Missing / Gaps
1. **LRU cache for `WordCharacterClassifier`** ❌:
   - TS uses `getMapForWordSeparators()` with 10-entry LRU cache
   - C# creates new `WordCharacterClassifier` on every `ParseSearchRequest()` (no caching)
   - **Impact**: Minor perf hit for repeated searches with same `wordSeparators` string
   - **Recommendation**: Add `static ConcurrentDictionary` or `MemoryCache` in `SearchParams` or factory helper

2. **Intl.Segmenter parity** ❌:
   - TS supports `intlSegmenterLocales` for Unicode word segmentation (e.g., CJK, Thai)
   - C# has no equivalent to `Intl.Segmenter`
   - **Options**:
     - Use ICU4N library (NuGet: `ICU4N`) for `BreakIterator` API
     - Document limitation and skip for MVP
   - **Current status**: Not implemented, not blocking core scenarios (Western languages work)

3. **`wordHelper.ts` API** (getWordAtText, createWordRegExp) ❌:
   - TS uses `getWordAtText()` for hover/selection; FindModel uses `SearchParams` path instead
   - C# has no equivalent to `getWordAtText()` or `DEFAULT_WORD_REGEXP`
   - **Impact**: None for find/replace (uses `SearchParams`); would block hover/word-at-cursor features
   - **Recommendation**: Defer until cursor/hover features needed (not in current scope)

4. **EditorOption.wordSeparators** source ⚠️:
   - TS: `this._editor.getOption(EditorOption.wordSeparators)` provides default `USUAL_WORD_SEPARATORS` per language
   - C#: No editor options layer in current codebase
   - **Current workaround**: Tests/controllers pass `wordSeparators` string directly to `SearchParams`
   - **TODO**: Define `TextModelOptions` or `EditorConfig` class to hold `WordSeparators` default (defer to DocUI layer)

### C# Migration Checklist (for Batch #2)

- [x] **`WordCharacterClassifier`** class with `GetClass()` and boundary validation
- [x] **`SearchParams.ParseSearchRequest()`** integration with `WordCharacterClassifier`
- [x] **`PieceTreeSearcher.Next()`** word boundary filtering
- [ ] **LRU cache** for `WordCharacterClassifier` instances (optional perf optimization)
- [ ] **Intl.Segmenter** parity for non-Latin scripts (defer to post-MVP or ICU4N)
- [ ] **`getWordAtText()`** API for hover/word-under-cursor (defer to Cursor features)
- [ ] **Editor options layer** to provide default `wordSeparators` per language (defer to DocUI)

### Test Coverage Recommendations

#### Word Boundary Matrix (Tier A – High Priority)
Create xUnit test suite `WordBoundaryTests.cs` covering:
- Basic ASCII separators: space, tab, punctuation from `USUAL_WORD_SEPARATORS`
- Edge cases: start/end of string, empty match, zero-width matches
- Multi-char operators: `->`, `::`, `==` (should split at operator)
- Unicode: emoji boundaries, surrogate pairs, combining diacritics
- CJK/Thai: Document limitation (no Intl.Segmenter) and skip or use ICU4N

#### Whole-Word Search Integration (Tier A)
Extend `TextModelSearchTests.cs`:
- Regex + wholeWord: `\w+` should match word boundaries, not mid-word
- Simple search + wholeWord: `"foo"` should not match `"foobar"`
- Case-insensitive + wholeWord: `"FOO"` matches `"Foo"` only at boundaries
- Multiline + wholeWord: verify CRLF compensation doesn't break boundaries

#### FindModel Binding (Tier B – depends on DocUI harness)
Port `ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts`:
- `_state.wholeWord = true` → passes `wordSeparators` to `SearchParams`
- `_state.wholeWord = false` → passes `null`
- Verify `EditorOption.wordSeparators` default (or C# equivalent config)

### Known Risks

1. **Unicode Word Break Algorithm divergence**:
   - TS `Intl.Segmenter` follows UAX #29 (Unicode Standard Annex #29)
   - C# `WordCharacterClassifier` uses manual char class lookups (no segmenter)
   - **Mitigation**: For Western languages, separator-based approach sufficient; for CJK, add ICU4N or document limitation

2. **Performance: No WordCharacterClassifier cache**:
   - Every search re-creates classifier from `wordSeparators` string
   - **Mitigation**: Add static cache keyed by `wordSeparators` (see "Missing" section above)

3. **Regex engine differences (ECMAScript vs .NET)**:
   - TS uses `strings.createRegExp()` with `unicode: true` (ES2018)
   - C# uses `RegexOptions.ECMAScript | RegexOptions.CultureInvariant`
   - **Mitigation**: `SearchParams.ApplyUnicodeWildcardCompatibility()` expands `.` to handle surrogates; monitor test failures for edge cases

4. **Default `wordSeparators` source**:
   - TS: language-specific via `EditorOption.wordSeparators`
   - C#: No options layer yet; callers must explicitly pass string
   - **Mitigation**: Define const `SearchParams.DefaultWordSeparators = USUAL_WORD_SEPARATORS` and use in tests; defer language-specific defaults to editor config layer

---

## Appendix C – Batch #2 Dependencies & Roadmap

### Blocking Items (必须先实现)
1. **WordCharacterClassifier cache** (optional perf): Add static LRU cache in `SearchParams.ParseSearchRequest()` to avoid re-creating classifiers
2. **DocUI editor harness** (for FindModel/FindController tests): Minimal test harness providing `ITextModel`, `FindReplaceState`, decoration hooks
3. **FindModel/FindDecorations stubs** (Tier C tests): If porting `findModel.test.ts`, need C# equivalents of `FindModelBoundToEditorModel`, `FindDecorations`, `FindReplaceState`

### Optional Items (可后续优化)
1. **Intl.Segmenter parity** (ICU4N): For CJK/Thai word segmentation; not needed for Western languages
2. **FindWidget DOM tests**: TS has no widget-specific tests; C# can skip or add Markdown snapshot tests for controller output
3. **`getWordAtText()` API**: For hover/word-under-cursor; defer to future cursor feature work

### Recommended Order for Batch #2
1. **Phase 1 – WordSeparator infrastructure** (Investigator-TS: DONE ✅):
   - Document TS WordSeparator/SearchContext flow (this appendix)
   - Validate C# parity (already exists in `SearchTypes.cs`)
   - Add LRU cache for `WordCharacterClassifier` (Porter-CS task)
2. **Phase 2 – FindModel logic layer** (Porter-CS + QA):
   - Port `findModel.test.ts` core cases (search, replace, match counting)
   - Stub `FindReplaceState`, `FindDecorations` in C# (minimal API for test harness)
   - Write xUnit tests for `FindModel` binding to `TextModelSearch` with `wholeWord` flag
3. **Phase 3 – FindController command layer** (Porter-CS + QA):
   - Port `findController.test.ts` command/action tests (skip clipboard/context-key if no DocUI harness)
   - Create minimal `DocUIFindController` that wraps `FindModel` + `ReplacePattern`
   - Write integration tests for find/replace/replaceAll commands

### Next Steps for Investigator-TS
- [x] Complete WordSeparator规格文档 (Appendix B above)
- [x] Confirm FindWidget测试路径 (no dedicated widget tests in TS)
- [x] Update记忆文件 `agent-team/members/investigator-ts.md`
- [ ] Prepare汇报文档 `agent-team/handoffs/B2-INV-Result.md`
