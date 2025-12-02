# Services & Top-level 模块对齐审查报告

**审查日期:** 2025-12-02 (Sprint 04 M2 更新)  
**审查范围:** 10 个服务/模块（TextModel 服务面、搜索/查找堆栈、语言/撤销服务、DocUI 宿主）

## 概要

> ✅ **Sprint 04 M2 重大进展：** FindModel/FindDecorations 已完成，**40 个 DocUI Find 测试通过**。

- ✅ 完全对齐: 4/10
- ⚠️ 存在偏差（设计差异 / 待扩展）: 4/10
- ❌ 需要修正: 2/10

| 区域 | 关键文件 | 状态 | 摘要 |
|------|----------|------|------|
| TextModel 服务面 | `src/TextBuffer/TextModel.cs` | ⚠️存在偏差 | 核心编辑逻辑齐全，但仍未公开 `Validate*`、`GetFullModelRange`、word helpers 或 dispose 事件，导致 [WS2-PORT](../migration-log.md#ws2-port) 新增的 Range/Selection/TextPosition API 仍无法被 `TextModelOptions.Diff/WithUpdate`、Undo 栈与 DocUI 服务直接复用。 |
| Undo/Redo 服务 | `src/TextBuffer/EditStack.cs`, `src/TextBuffer/Services/IUndoRedoService.cs` | ⚠️存在偏差 | 仅支持单模型栈，缺少 `UndoRedoSource`、资源组及快照，即便 [WS2-PORT](../migration-log.md#ws2-port) 已提供共享的 Range/Selection 帮助类，也无法在服务层公开复用。 |
| TextModelOptions & defaults | `src/TextBuffer/TextModelOptions.cs` | ✅完全对齐 | 默认值、`WithUpdate`、`Diff` 与 TS 一致，[WS5-QA](../migration-log.md#ws5-qa) 的 `TextModelIndentationTests` 已验证 tab/space 组合，不过 `GuessIndentation` API 仍以 1 skipped test 记录。 |
| 装饰事件 | `src/TextBuffer/TextModelDecorationsChangedEventArgs.cs` | ✅完全对齐 | 比 TS 多了 `Changes` 明细和版本元数据。 |
| 搜索栈 | `src/TextBuffer/TextModelSearch.cs`, `src/TextBuffer/Core/SearchTypes.cs`, `src/TextBuffer/Core/PieceTreeSearcher.cs` | ✅完全对齐 | 多范围、词边界、多行正则全部实现并已有测试覆盖。 |
| SearchHighlightOptions DTO | `src/TextBuffer/SearchHighlightOptions.cs` | ✅完全对齐 | DTO 字段与 `SearchParams` 子集一致。 |
| Language Configuration Service | `src/TextBuffer/Services/ILanguageConfigurationService.cs` | ⚠️存在偏差 | 仅支持变更通知，不提供配置注册/解析或缓存。 |
| TextPosition 帮助类 | `src/TextBuffer/TextPosition.cs` | ⚠️存在偏差 | [WS2-PORT](../migration-log.md#ws2-port) 新增 `With`/`Delta`/`IsBefore*`/`Compare`，但 `Lift`、`IsIPosition`、JSON roundtrip 及 Selection/Range 集成尚未同步，DocUI/Undo 仍需手写比较。 |
| DocUI Find 宿主堆栈 | `src/TextBuffer/DocUI/*.cs` | ✅ 完成 | 查找/替换流程已移植，**40 个测试通过**；Context keys/viewport reveal 待扩展 |
| Clipboard / Storage shims | `src/TextBuffer/DocUI/DocUIFindController.cs` (Null 实现) | ❌需要修正 | 默认实现忽略所有持久化与全局剪贴板，功能无法生效。 |

### 服务层波及（WS2-PORT / WS5-QA / Sprint04 R1-R11）
- [WS2-PORT](../migration-log.md#ws2-port) 解锁 Range/Selection/TextPosition helpers，使 `TextModelOptions.Diff/WithUpdate`、`EditStack.ApplyEdits` 与 DocUI 查找/替换栈能共享统一位置语义；但由于 `Validate*`/`GetFullModelRange` 仍未公开，外部服务仍只得依赖内部 helper，无法通过接口重用这些定位工具。
- [WS5-QA](../migration-log.md#ws5-qa) 的 targeted rerun（`TextModelIndentationTests`, `PieceTreeBufferApiTests`, `PieceTreeSearchRegressionTests`）验证 TextModelOptions、Undo 栈和 DocUI Find 服务在 TabSize/EOL 与搜索缓存场景下的协作，同时暴露 `GuessIndentation` 缺口（1 skipped test）与 DocUI Intl/word cache backlog 的缺失证据。
- [Sprint04 R1-R11](../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11) 将上述交付整合进统一基线，DocUI Find 服务虽已复用 Range helpers，却仍缺少 context keys、viewport reveal 与真实剪贴板代理。
- **AA4 CL8 占位仍有效：** [#delta-2025-11-26-aa4-cl8-markdown](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)、[#delta-2025-11-26-aa4-cl8-capture](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-capture)、[#delta-2025-11-26-aa4-cl8-intl](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-intl)、[#delta-2025-11-26-aa4-cl8-wordcache](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-wordcache) 继续托管 Intl 搜索服务、Markdown renderer、capture telemetry 与 word cache backlog，本模块刷新时必须保留引用。

---

## 详细分析

### 1. TextModel 服务面 (`src/TextBuffer/TextModel.cs`)
**TS 参考:** `ts/src/vs/editor/common/model/textModel.ts`

- **现状:** AA3-003/-004 已经把选项、语言/装饰事件以及搜索入口移植过来，核心编辑、Undo/Redo、装饰与查找 API 与 TS 一致。
- **偏差/缺漏:**
  - 没有 `dispose()` 与 `onWillDispose`，也未在模型销毁时通知 `IUndoRedoService` 或 `LanguageConfigurationService`。（TS 通过 `DisposableStore` 自动清理）
  - 位置与范围 API 只暴露 `GetDocumentRange()`（私有），缺少 `validatePosition/range`、`modifyPosition`、`getFullModelRange`、`getWordAt/UntilPosition` 等，使得许多 VS Code 辅助函数无法复用。
  - `BracketPairsTextModelPart`、tokenization、自动缩进修剪、注入文本事件等多段 `TextModelPart` 尚未接入；这些特性依赖语言配置和 tokenization 服务，目前都缺失。
  - [WS2-PORT](../migration-log.md#ws2-port) 已经把 Range/Selection/TextPosition helpers 带进 C#，但由于 `TextModel` 既不公开 `Validate*` 也不复用 `Range` 工具层，Undo 栈、DocUI Find 服务以及 TextModelOptions diff 仍需绕过核心 API 自行校验位置。
- **建议:**
  1. 暴露 `ValidatePosition`/`ValidateRange`/`GetFullModelRange` 并在 `tests/TextBuffer.Tests/TextModelTests.cs` 增加覆盖（可参考 TS 的 `textModel.test.ts`）。
  2. 引入 `Dispose()`，解除 `_languageConfigurationSubscription`、清空 `_editStack`，同时触发新的 `OnWillDispose` 事件，确保 `InProcUndoRedoService` 释放堆栈。
  3. 规划 `TextModelPart` 框架：先实现 `BracketPairsTextModelPart`/`TokenizationTextModelPart` 的存根，再按 `textModel.ts` 的分层逐步接入。
  4. 对于单词 API，可重用现有 `WordCharacterClassifier`（`Core/SearchTypes.cs`）并在 `FindUtilities` 之外提供公共入口。

### 2. Undo/Redo 服务 (`src/TextBuffer/EditStack.cs`, `src/TextBuffer/Services/IUndoRedoService.cs`)
**TS 参考:** `ts/src/vs/platform/undoRedo/common/undoRedo.ts`

- **现状:** `EditStack` + `InProcUndoRedoService` 维护 per-model 栈，Undo/Redo 能在 `TextModel` 层工作。
  [WS5-QA](../migration-log.md#ws5-qa) 的 `TextModelIndentationTests` 也在单模型路径上验证了 TabSize/EOL 选项变更会通过同一 Undo 管线回放。 
- **偏差:**
  - 没有 URI/资源概念，无法注册跨模型操作；`UndoRedoGroup`、`UndoRedoSource`、`getElements`、`setElementsValidFlag`、`removeElements` 等 API 缺失。
  - 没有快照 (`createSnapshot/restoreSnapshot`) 以及 `ValidationCallback`，因此无法实现 TS 的 "软失败" 与资源失效逻辑。
  - `_openElement` 逻辑没有对应 `IUndoRedoService.getLastElement()`，导致批量编辑与 `UndoRedoGroup` 无法共享。
  - 虽然 [WS2-PORT](../migration-log.md#ws2-port) 已提供 Range/Selection helpers，但 `IUndoRedoService` 仍无公开入口可重用它们来校验编辑位置。
- **建议:**
  1. 扩展接口以携带 `Uri`（或 `TextModel.Id`），引入资源集合与 `UndoRedoElementKind`，并在 `EditStack` 中将 `_openElement` 映射到 service 的 `getLastElement/canMerge` 语义。
  2. 添加 `UndoRedoGroup` 与 `UndoRedoSource` 支持，允许依赖方把多模型编辑压成同一撤销点。
  3. 复制 `undoRedo.test.ts` 中的关键用例到新的 `tests/TextBuffer.Tests/UndoRedoServiceTests.cs`，覆盖跨资源、失效与快照情形。

### 3. TextModelOptions (`src/TextBuffer/TextModelOptions.cs`)
- 与 `textModelDefaults.ts` 完全一致，`TextModelResolvedOptions.Resolve/WithUpdate/Diff` 逻辑与 TS 相同；`Range`/`TextPosition` helpers 已可在内部构造 diff，但外部仍无法直接访问（见上文 TextModel gap）。
- [WS2-PORT](../migration-log.md#ws2-port) 带来的 Range/Selection/TextPosition helpers 已经被 `TextModelOptions.Diff` 和 Undo 栈内部调用，用于在服务层比对 TabSize/EOL 变更并写入 DocUI 查找状态，然而缺乏公开 `Validate*` 入口让其他宿主无法共享这些 helper。 
- **验证:** `tests/TextBuffer.Tests/TextModelTests.UpdateOptionsRaisesChangeEvent` 已覆盖 `OnDidChangeOptions` 通知；[WS5-QA](../migration-log.md#ws5-qa) 的 targeted rerun `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "PieceTreeBufferApiTests\|PieceTreeSearchRegressionTests\|TextModelIndentationTests" --nologo`（44/44 通过 + 1 skipped，`GuessIndentation` 待实现）验证 TabSize/InsertSpaces 行为与 TS 一致。

### 4. 装饰事件 (`src/TextBuffer/TextModelDecorationsChangedEventArgs.cs`)
- 100% 对齐 `textModelEvents.ts`，并额外暴露 `Changes`、`ModelVersionId`、行高/字体变更列表，使得诊断更详细。
- `TextModel` 在 `RaiseDecorationsChanged` 中填充这些字段，迭代器与 TS 行为一致。无需改动。

### 5. 搜索栈 (`src/TextBuffer/TextModelSearch.cs`, `src/TextBuffer/Core/SearchTypes.cs`, `src/TextBuffer/Core/PieceTreeSearcher.cs`)
- `SearchRangeSet`、`LineFeedCounter`、`SearchParams.ParseSearchRequest`、`WordCharacterClassifier` 以及 `PieceTreeSearcher` 均对应 TS 原版实现。
- 多行/多范围/空匹配推进逻辑与 VS Code 行为一致，`tests/TextBuffer.Tests/TextModelSearchTests.cs` 基本复刻 `textModelSearch.test.ts`。
- **建议:** 保持同步即可，后续若 TS 引入新的 `regex unicode escapes` 行为，直接更新 `SearchPatternUtilities`。

### 6. SearchHighlightOptions (`src/TextBuffer/SearchHighlightOptions.cs`)
- 简单 DTO，与 `SearchParams` 字段一一对应，`OwnerId` 默认使用 `DecorationOwnerIds.SearchHighlights`，与 TS 的 `SearchDecorator` 用法一致。无需调整。

### 7. Language Configuration Service (`src/TextBuffer/Services/ILanguageConfigurationService.cs`)
**TS 参考:** `ts/src/vs/editor/common/languages/languageConfigurationRegistry.ts`

- **现状:** 只有 `Subscribe(languageId, callback)` 与 `OnDidChange` 事件，默认实现 `LanguageConfigurationService` 只负责追踪订阅。
- **偏差:**
  - 没有 `register(languageId, configuration)`、`getLanguageConfiguration`、`getWordDefinition`、`onDidChange` 的语言级事件聚合，也没有 `ResolvedLanguageConfiguration` 缓存。
  - 因为没有配置数据，`TextModel` 及未来的括号/自动缩进功能都无法访问语言规则。
- **建议:**
  1. 扩展接口以支持 `Register(languageId, LanguageConfiguration)` 并返回 `IDisposable`，内部缓存 `ResolvedLanguageConfiguration`。
  2. 暴露 `GetConfiguration(languageId)`/`GetWordDefinition(languageId)` 等访问器，与 TS 保持一致。
  3. 在 `tests/TextBuffer.Tests` 下新增 `LanguageConfigurationServiceTests`，覆盖注册、变更、释放与默认语言 (`plaintext`) fallback。

### 8. TextPosition (`src/TextBuffer/TextPosition.cs`)
**TS 参考:** `ts/src/vs/editor/common/core/position.ts`

- **现状:** [WS2-PORT](../migration-log.md#ws2-port) 已补齐 `With`、`Delta`、`IsBefore*`、`Compare` 等核心 API，但 `Lift`/`IsIPosition`/`Clone`/`ToString` 仍缺失，也未向 `Selection`/`Range` 暴露 JSON 序列化 helper；DocUI/Undo 只好复制自定义 struct。
- **影响:** DocUI、tokenization、Selection 工具仍需要 `IPosition` 形态与 `Lift`/`delta` 组合来实现 host 互操作，否则像 `DocUIFindController` 这样的服务层无法直接消费 `TextPosition`。
- **建议:**
  1. 补齐剩余 static helper（`Lift`, `IsIPosition`, `IsBeforeOrEqual`, `Clone`, `ToString`）并把 `TextPosition` 作为 `Range`/`Selection` 的默认参数类型，以便服务层直接共享 `WS2-PORT` helper。
  2. 在 `tests/TextBuffer.Tests/TextModelTests` 或新增 `TextPositionTests` 中加入 roundtrip 覆盖，并扩展 DocUI find / Undo 测试以证明服务垂直栈能消费这些 API。

### 9. DocUI Find 宿主堆栈 (`src/TextBuffer/DocUI/DocUIFindController.cs`, `FindModel.cs`, `FindReplaceState.cs`, `FindDecorations.cs`, `FindUtilities.cs`)
**TS 参考:** `ts/src/vs/editor/contrib/find/browser/*.ts`

- **现状:** 查找/替换流程（自动选区、MatchesLimit、Large replace、Seed search string、Find decorations、范围查找）已经完整移植，并在 Sprint04 R1-R11 基线下复用 [WS2-PORT](../migration-log.md#ws2-port) 的 Range/Selection/TextPosition helpers；`tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs` 复刻了 VS Code 的关键用例，且可通过 [WS5-QA](../migration-log.md#ws5-qa) 命令串联 PieceTree search regressions 以验证 host 行为。
- **偏差:**
  - 缺少 VS Code 宿主交互：没有 `contextKey`/命令上下文更新，也没有视图滚动 API（TS 通过 `ICodeEditor.revealRange`）。`DocUIFindController` 只能操作 `IEditorHost` 的选区。
  - `DocUIReplaceController` (`src/TextBuffer/Rendering/DocUIReplaceController.cs`) 仍是 TODO，实际的 `Replace`/`ReplaceAll` 逻辑只在 `FindModel` 中实现，无法复用 CLI/DocUI 之外的宿主。
  - `LoadPersistedOptions` 依赖 `IFindControllerStorage`，但默认 `Null` 实现导致实际运行时不会持久化选项；`global find clipboard` 也需要外部注入。
  - Intl word 缓存 / Markdown renderer 整合 / capture telemetry 仍挂在 [#delta-2025-11-26-aa4-cl8-markdown](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-markdown)、[#delta-2025-11-26-aa4-cl8-capture](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-capture)、[#delta-2025-11-26-aa4-cl8-intl](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-intl)、[#delta-2025-11-26-aa4-cl8-wordcache](../../agent-team/indexes/README.md#delta-2025-11-26-aa4-cl8-wordcache) 占位；在占位关闭前，DocUI 服务不能宣称对齐。
- **建议:**
  1. 定义真正的宿主适配层（例如 `IEditorHost2`），提供 `RevealRange`, `FocusFindWidget`, `UpdateContextKey`，并在 DocUI harness 中实现 stub。
  2. 将 `DocUIReplaceController` 接入 `DocUIFindController` 的 `Replace/ReplaceAll` 管道，以便共享 case-preserve 与 snippet integration。
  3. 在测试中覆盖 `ReplaceAll` 对装饰/匹配位置的更新（TS 的 `findController.test.ts Replace All` 场景尚未移植）。
  4. 记录设计意图：哪些 VS Code 功能（如 find widget UI, context keys）刻意省略，避免误以为是遗漏。
  5. 在 AA4 CL8 占位落地前，将 Intl word cache/Markdown renderer/capture telemetry backlog 保持为公开风险，并把 `WS5-QA` rerun 记录附在 `DocUIFindControllerTests` 说明里，避免误判为闭环。

### 10. Clipboard / Storage shims (`src/TextBuffer/DocUI/DocUIFindController.cs` 内的 `NullFindControllerStorage`、`NullFindControllerClipboard`)
- **现状:** 若宿主未显式注入实现，则查找选项不会落盘，也不会与系统/全局查找剪贴板同步；DocUI harness 之外的消费者将观察到“选项永不记忆、剪贴板永不写入”的行为。
- **建议:**
  1. 在生产路径提供默认实现（例如包装 `SystemClipboard` 与简单文件/内存存储），并将 `FindControllerHostOptions.UseGlobalFindClipboard` 默认为 ON。
  2. 在 `DocUIFindControllerTests` 中增加覆盖，确保默认实现确实持久化/同步。
  3. 文档化接口：需要何种生命周期、线程安全保证，方便宿主注入。

---

## 总结

- **高优先级**
  1. 扩展 `TextPosition` 与 `TextModel` 位置 API，并补充相应单元测试，解锁后续 Token/括号/Find 逻辑的复用。
  2. 升级 Undo/Redo 服务至 TS 等效（资源、分组、快照、UndoRedoSource），否则多模型编辑和跨资源撤销无法实现。
  3. 为 `ILanguageConfigurationService` 提供注册/解析能力与缓存，后续的括号/缩进行为才能落地。
  4. 将 DocUI find pipeline 接入真实 clipboard/storage，实现默认情况下的选项持久化与全局查找剪贴板。

- **中优先级**
  - 规划 `TextModelPart` 架构（BracketPairs、Tokenization），并定义宿主适配接口（context key、reveal）以便日后支持完整 VS Code 行为。
  - 将 `DocUIReplaceController` 从 TODO 状态推进为可复用组件。

---

## Verification Notes

- **检查的 C# 文件**
  - `src/TextBuffer/TextModel.cs`, `TextModelOptions.cs`, `TextModelDecorationsChangedEventArgs.cs`
  - `src/TextBuffer/TextModelSearch.cs`, `src/TextBuffer/Core/SearchTypes.cs`, `src/TextBuffer/Core/PieceTreeSearcher.cs`
  - `src/TextBuffer/EditStack.cs`, `src/TextBuffer/Services/IUndoRedoService.cs`, `src/TextBuffer/Services/ILanguageConfigurationService.cs`
  - `src/TextBuffer/TextPosition.cs`
  - `src/TextBuffer/DocUI/DocUIFindController.cs`, `FindModel.cs`, `FindReplaceState.cs`, `FindDecorations.cs`, `FindUtilities.cs`
  - `src/TextBuffer/Rendering/DocUIReplaceController.cs`（TODO 状态）

- **参照的 TS 源**
  - `ts/src/vs/editor/common/model/textModel.ts`
  - `ts/src/vs/platform/undoRedo/common/undoRedo.ts`
  - `ts/src/vs/editor/common/model/textModelSearch.ts`、`ts/src/vs/editor/common/core/wordCharacterClassifier.ts`
  - `ts/src/vs/editor/contrib/find/browser/*.ts`
  - `ts/src/vs/editor/common/languages/languageConfigurationRegistry.ts`

- **审阅的测试**
  - `tests/TextBuffer.Tests/TextModelTests.cs`
  - `tests/TextBuffer.Tests/TextModelSearchTests.cs`
  - `tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs`
  - `tests/TextBuffer.Tests/ReplacePatternTests.cs`

- **WS5-QA targeted rerun（验证 TextModelOptions / DocUI 服务）**
  - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter "PieceTreeBufferApiTests\|PieceTreeSearchRegressionTests\|TextModelIndentationTests" --nologo` （44/44 通过 + 1 skipped），详见 [WS5-QA](../migration-log.md#ws5-qa)。

- **Sprint04 R1-R11 全量基线**
  - `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` （585/585 通过 + 1 skipped），引用 [#delta-2025-11-26-sprint04-r1-r11](../../agent-team/indexes/README.md#delta-2025-11-26-sprint04-r1-r11)。

- **未解决的 TODO**
  - `src/TextBuffer/TextModel.cs` 约第 565 行：`TODO(FindController)` 关于增量高亮与 stack boundary。
  - `src/TextBuffer/Rendering/DocUIReplaceController.cs`: `TODO(B2)` 提示 Replace pipeline 尚未接入 TextModel。
  - 这些 TODO 需在上述改造中一并解决或重新排期。
