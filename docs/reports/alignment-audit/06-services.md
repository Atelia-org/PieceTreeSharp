# Services & Top-level 模块对齐审查报告

**审查日期:** 2025-11-26\\
**审查范围:** 10 个服务/模块（TextModel 服务面、搜索/查找堆栈、语言/撤销服务、DocUI 宿主）

## 概要
- ✅ 完全对齐: 4/10
- ⚠️ 存在偏差（设计差异 / 待扩展）: 4/10
- ❌ 需要修正: 2/10

| 区域 | 关键文件 | 状态 | 摘要 |
|------|----------|------|------|
| TextModel 服务面 | `src/TextBuffer/TextModel.cs` | ⚠️存在偏差 | 核心编辑逻辑齐全，但缺少 `validate*`、单词/括号/令牌相关 API 以及 dispose 事件。 |
| Undo/Redo 服务 | `src/TextBuffer/EditStack.cs`, `src/TextBuffer/Services/IUndoRedoService.cs` | ⚠️存在偏差 | 仅支持单模型栈，缺少 `UndoRedoSource`、资源组及快照。 |
| TextModelOptions & defaults | `src/TextBuffer/TextModelOptions.cs` | ✅完全对齐 | 默认值、`WithUpdate`、`Diff` 与 TS 一致。 |
| 装饰事件 | `src/TextBuffer/TextModelDecorationsChangedEventArgs.cs` | ✅完全对齐 | 比 TS 多了 `Changes` 明细和版本元数据。 |
| 搜索栈 | `src/TextBuffer/TextModelSearch.cs`, `src/TextBuffer/Core/SearchTypes.cs`, `src/TextBuffer/Core/PieceTreeSearcher.cs` | ✅完全对齐 | 多范围、词边界、多行正则全部实现并已有测试覆盖。 |
| SearchHighlightOptions DTO | `src/TextBuffer/SearchHighlightOptions.cs` | ✅完全对齐 | DTO 字段与 `SearchParams` 子集一致。 |
| Language Configuration Service | `src/TextBuffer/Services/ILanguageConfigurationService.cs` | ⚠️存在偏差 | 仅支持变更通知，不提供配置注册/解析或缓存。 |
| TextPosition 帮助类 | `src/TextBuffer/TextPosition.cs` | ❌需要修正 | 只有比较操作，缺少 `with/delta/isBefore` 等 API。 |
| DocUI Find 宿主堆栈 | `src/TextBuffer/DocUI/*.cs` | ⚠️存在偏差 | 查找/替换流程已移植，但缺少 VS Code 宿主契约（context keys、视图滚动、Replace 控制器整合）。 |
| Clipboard / Storage shims | `src/TextBuffer/DocUI/DocUIFindController.cs` (Null 实现) | ❌需要修正 | 默认实现忽略所有持久化与全局剪贴板，功能无法生效。 |

---

## 详细分析

### 1. TextModel 服务面 (`src/TextBuffer/TextModel.cs`)
**TS 参考:** `ts/src/vs/editor/common/model/textModel.ts`

- **现状:** AA3-003/-004 已经把选项、语言/装饰事件以及搜索入口移植过来，核心编辑、Undo/Redo、装饰与查找 API 与 TS 一致。
- **偏差/缺漏:**
  - 没有 `dispose()` 与 `onWillDispose`，也未在模型销毁时通知 `IUndoRedoService` 或 `LanguageConfigurationService`。（TS 通过 `DisposableStore` 自动清理）
  - 位置与范围 API 只暴露 `GetDocumentRange()`（私有），缺少 `validatePosition/range`、`modifyPosition`、`getFullModelRange`、`getWordAt/UntilPosition` 等，使得许多 VS Code 辅助函数无法复用。
  - `BracketPairsTextModelPart`、tokenization、自动缩进修剪、注入文本事件等多段 `TextModelPart` 尚未接入；这些特性依赖语言配置和 tokenization 服务，目前都缺失。
- **建议:**
  1. 暴露 `ValidatePosition`/`ValidateRange`/`GetFullModelRange` 并在 `tests/TextBuffer.Tests/TextModelTests.cs` 增加覆盖（可参考 TS 的 `textModel.test.ts`）。
  2. 引入 `Dispose()`，解除 `_languageConfigurationSubscription`、清空 `_editStack`，同时触发新的 `OnWillDispose` 事件，确保 `InProcUndoRedoService` 释放堆栈。
  3. 规划 `TextModelPart` 框架：先实现 `BracketPairsTextModelPart`/`TokenizationTextModelPart` 的存根，再按 `textModel.ts` 的分层逐步接入。
  4. 对于单词 API，可重用现有 `WordCharacterClassifier`（`Core/SearchTypes.cs`）并在 `FindUtilities` 之外提供公共入口。

### 2. Undo/Redo 服务 (`src/TextBuffer/EditStack.cs`, `src/TextBuffer/Services/IUndoRedoService.cs`)
**TS 参考:** `ts/src/vs/platform/undoRedo/common/undoRedo.ts`

- **现状:** `EditStack` + `InProcUndoRedoService` 维护 per-model 栈，Undo/Redo 能在 `TextModel` 层工作。
- **偏差:**
  - 没有 URI/资源概念，无法注册跨模型操作；`UndoRedoGroup`、`UndoRedoSource`、`getElements`、`setElementsValidFlag`、`removeElements` 等 API 缺失。
  - 没有快照 (`createSnapshot/restoreSnapshot`) 以及 `ValidationCallback`，因此无法实现 TS 的 "软失败" 与资源失效逻辑。
  - `_openElement` 逻辑没有对应 `IUndoRedoService.getLastElement()`，导致批量编辑与 `UndoRedoGroup` 无法共享。
- **建议:**
  1. 扩展接口以携带 `Uri`（或 `TextModel.Id`），引入资源集合与 `UndoRedoElementKind`，并在 `EditStack` 中将 `_openElement` 映射到 service 的 `getLastElement/canMerge` 语义。
  2. 添加 `UndoRedoGroup` 与 `UndoRedoSource` 支持，允许依赖方把多模型编辑压成同一撤销点。
  3. 复制 `undoRedo.test.ts` 中的关键用例到新的 `tests/TextBuffer.Tests/UndoRedoServiceTests.cs`，覆盖跨资源、失效与快照情形。

### 3. TextModelOptions (`src/TextBuffer/TextModelOptions.cs`)
- 与 `textModelDefaults.ts` 完全一致，`TextModelResolvedOptions.Resolve/WithUpdate/Diff` 逻辑与 TS 相同。
- **验证:** `tests/TextBuffer.Tests/TextModelTests.UpdateOptionsRaisesChangeEvent` 已覆盖 `OnDidChangeOptions` 通知。无需调整。

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

- **现状:** record struct 仅实现 `CompareTo` 及比较运算符，无法表达 `with/delta/isBefore` 等逻辑。
- **影响:** `DocUI`、Future tokenization、Selection 工具都需要 `Position.isBefore`, `Position.delta` 等 API；目前不得不手写比较，易出错。
- **建议:**
  1. 实现 `With(int? line = null, int? column = null)`, `Delta(int deltaLine, int deltaColumn)`, `IsBefore`, `IsBeforeOrEqual`, `Clone`, `ToString`，并提供静态帮助方法（`Compare`, `Lift`, `IsIPosition`）以对齐 TS。
  2. 在 `tests/TextBuffer.Tests/TextModelTests` 或新增 `TextPositionTests` 中加入覆盖：比较、delta、with、JSON roundtrip。

### 9. DocUI Find 宿主堆栈 (`src/TextBuffer/DocUI/DocUIFindController.cs`, `FindModel.cs`, `FindReplaceState.cs`, `FindDecorations.cs`, `FindUtilities.cs`)
**TS 参考:** `ts/src/vs/editor/contrib/find/browser/*.ts`

- **现状:** 查找/替换流程（自动选区、MatchesLimit、Large replace、Seed search string、Find decorations、范围查找）已经完整移植，`tests/TextBuffer.Tests/DocUI/DocUIFindControllerTests.cs` 复刻了 VS Code 的关键用例。
- **偏差:**
  - 缺少 VS Code 宿主交互：没有 `contextKey`/命令上下文更新，也没有视图滚动 API（TS 通过 `ICodeEditor.revealRange`）。`DocUIFindController` 只能操作 `IEditorHost` 的选区。
  - `DocUIReplaceController` (`src/TextBuffer/Rendering/DocUIReplaceController.cs`) 仍是 TODO，实际的 `Replace`/`ReplaceAll` 逻辑只在 `FindModel` 中实现，无法复用 CLI/DocUI 之外的宿主。
  - `LoadPersistedOptions` 依赖 `IFindControllerStorage`，但默认 `Null` 实现导致实际运行时不会持久化选项；`global find clipboard` 也需要外部注入。
- **建议:**
  1. 定义真正的宿主适配层（例如 `IEditorHost2`），提供 `RevealRange`, `FocusFindWidget`, `UpdateContextKey`，并在 DocUI harness 中实现 stub。
  2. 将 `DocUIReplaceController` 接入 `DocUIFindController` 的 `Replace/ReplaceAll` 管道，以便共享 case-preserve 与 snippet integration。
  3. 在测试中覆盖 `ReplaceAll` 对装饰/匹配位置的更新（TS 的 `findController.test.ts Replace All` 场景尚未移植）。
  4. 记录设计意图：哪些 VS Code 功能（如 find widget UI, context keys）刻意省略，避免误以为是遗漏。

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

- **未解决的 TODO**
  - `src/TextBuffer/TextModel.cs` 约第 565 行：`TODO(FindController)` 关于增量高亮与 stack boundary。
  - `src/TextBuffer/Rendering/DocUIReplaceController.cs`: `TODO(B2)` 提示 Replace pipeline 尚未接入 TextModel。
  - 这些 TODO 需在上述改造中一并解决或重新排期。
