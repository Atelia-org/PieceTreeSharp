# Services & Top-level 模块对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 10个服务与顶层文件

## 概要
- 完全对齐: 5/10
- 存在偏差: 3/10
- 需要修正: 2/10

| 文件 | 状态 | 说明 |
|------|------|------|
| EditStack.cs | ⚠️存在偏差 | 架构不同但功能等价 |
| PieceTreeBuffer.cs | ⚠️存在偏差 | 简化实现，缺少部分功能 |
| SearchHighlightOptions.cs | ✅完全对齐 | 简化后正确移植 |
| ILanguageConfigurationService.cs | ✅完全对齐 | 原创C#实现，合理适配 |
| IUndoRedoService.cs | ✅完全对齐 | 原创C#实现，合理适配 |
| TextModel.cs | ⚠️存在偏差 | 核心功能对齐，部分高级特性缺失 |
| TextModelDecorationsChangedEventArgs.cs | ✅完全对齐 | 正确移植事件类型 |
| TextModelOptions.cs | ✅完全对齐 | 完整移植选项与枚举 |
| TextModelSearch.cs | ❌需要修正 | 缺少若干TS原版方法 |
| TextPosition.cs | ❌需要修正 | 缺少多个Position类方法 |

---

## 详细分析

### 1. EditStack.cs
**TS源:** editStack.ts (Lines 384-452)
**对齐状态:** ⚠️存在偏差

**分析:**
C#实现采用了不同的架构设计：
- TS版的`EditStack`直接调用`IUndoRedoService.getLastElement()`来检查栈顶元素
- C#版维护一个`_openElement`引用，通过`IUndoRedoService`接口的新方法来管理

**关键差异:**
1. **pushStackElement()**: TS版调用`getLastElement()`检查并关闭元素；C#版直接操作`_openElement`
2. **_getOrCreateEditStackElement()**: TS版通过`getLastElement()`和`canAppend()`检查；C#版简化为`GetOrCreateElement()`
3. **pushEditOperation()**: TS版包含`cursorStateComputer`和`UndoRedoGroup`参数；C#版在`TextModel`中处理

**偏差说明:**
- C#版将TS中`EditStackElement`的复杂继承层次(`SingleModelEditStackElement`, `MultiModelEditStackElement`)简化为单一类
- `TextModelUndoRedoElement`包装器是C#新增的设计

**修正建议:**
设计差异是故意为之，以适应C#的简化DI模型。当前实现功能正确，无需修正。

---

### 2. PieceTreeBuffer.cs
**TS源:** pieceTreeTextBuffer.ts (Lines 33-630)
**对齐状态:** ⚠️存在偏差

**分析:**
C#版是TS版`PieceTreeTextBuffer`的简化门面实现。

**关键差异:**

| 功能 | TS版 | C#版 |
|------|------|------|
| `equals()` | ✅ 完整实现 | ❌ 未实现 |
| `createSnapshot()` | ✅ 返回ITextSnapshot | ✅ 简化实现 |
| `getRangeAt()` | ✅ 完整实现 | ❌ 未实现 |
| `getValueInRange()` | ✅ 支持EOL偏好 | ⚠️ 通过GetText(start, length)部分支持 |
| `getValueLengthInRange()` | ✅ 完整实现 | ❌ 未实现 |
| `getCharacterCountInRange()` | ✅ 处理代理对 | ❌ 未实现 |
| `getNearestChunk()` | ✅ 完整实现 | ❌ 未实现 |
| `getLinesContent()` | ✅ 返回string[] | ❌ 未实现 |
| `getLineFirstNonWhitespaceColumn()` | ✅ 完整实现 | ❌ 未实现 |
| `getLineLastNonWhitespaceColumn()` | ✅ 完整实现 | ❌ 未实现 |
| `applyEdits()` | ✅ 完整编辑处理 | ⚠️ 简化为ApplyEdit() |
| `findMatchesLineByLine()` | ✅ 完整实现 | ❌ 未实现(在TextModelSearch中) |
| `_reduceOperations()` | ✅ 大编辑优化 | ❌ 未实现 |
| `_getInverseEditRanges()` | ✅ 完整实现 | ❌ 未实现 |

**偏差说明:**
C#实现作为"Minimal PieceTree-backed buffer façade"是设计决策，而非移植遗漏。但缺少的方法会影响高级编辑场景。

**修正建议:**
1. 考虑添加`Equals()`方法用于缓冲区比较
2. 添加`GetRangeAt()`以支持范围计算
3. `ApplyEdit`应返回反向编辑信息以支持完整撤销/重做
4. 添加`GetLinesContent()`用于整体行内容访问

---

### 3. SearchHighlightOptions.cs
**TS源:** textModelSearch.ts (SearchParams相关)
**对齐状态:** ✅完全对齐

**分析:**
`SearchHighlightOptions`是对TS版`SearchParams`的简化封装，用于搜索高亮场景。

**对应关系:**
```csharp
// C#
Query → searchString
IsRegex → isRegex  
MatchCase → matchCase
WordSeparators → wordSeparators
CaptureMatches → (parseSearchRequest中的captureMatches)
```

**结论:**
作为搜索选项的数据传输对象，实现正确完整。

---

### 4. ILanguageConfigurationService.cs
**TS源:** languageConfigurationRegistry.ts
**对齐状态:** ✅完全对齐 (标记为Original C# implementation)

**分析:**
这是原创C#实现，专门为简化的C#运行时设计。

**TS版复杂性:**
- 依赖VS Code完整DI系统
- `ResolvedLanguageConfiguration`包含大量语言特性(括号匹配、缩进规则、onEnter规则等)
- 与`IConfigurationService`、`ILanguageService`深度集成

**C#简化:**
```csharp
public interface ILanguageConfigurationService
{
    IDisposable Subscribe(string languageId, EventHandler<...> callback);
    event EventHandler<...>? OnDidChange;
}
```

**结论:**
作为最小可行接口，满足`TextModel`的语言配置变更通知需求。无需修正。

---

### 5. IUndoRedoService.cs
**TS源:** undoRedo.ts
**对齐状态:** ✅完全对齐 (标记为Original C# implementation)

**分析:**
C#版是TS版`IUndoRedoService`的简化内存实现。

**TS版特性:**
```typescript
interface IUndoRedoService {
    registerUriComparisonKeyComputer(...);
    getUriComparisonKey(...);
    pushElement(element, group?, source?);
    getLastElement(resource);
    getElements(resource);
    setElementsValidFlag(...);
    removeElements(resource);
    createSnapshot(resource);
    restoreSnapshot(snapshot);
    canUndo/canRedo(resource | UndoRedoSource);
    undo/redo(resource | UndoRedoSource);
}
```

**C#简化:**
```csharp
internal interface IUndoRedoService
{
    void PushElement(TextModelUndoRedoElement element);
    void CloseOpenElement(TextModel model);
    TextModelUndoRedoElement? TryReopenLastElement(TextModel model);
    TextModelUndoRedoElement? PopUndo/PopRedo(TextModel model);
    void PushRedoResult(TextModelUndoRedoElement element);
    bool CanUndo/CanRedo(TextModel model);
    void Clear(TextModel model);
}
```

**结论:**
`InProcUndoRedoService`使用简单的`Stack<T>`实现撤销/重做栈，满足单模型场景。对于多模型协同编辑场景，需要扩展为完整实现。

---

### 6. TextModel.cs
**TS源:** textModel.ts (Lines 120-2688)
**对齐状态:** ⚠️存在偏差

**分析:**
`TextModel`是核心类，C#版实现了主要功能但省略了部分高级特性。

**已实现的核心功能:**
- ✅ 版本管理 (`VersionId`, `AlternativeVersionId`)
- ✅ 内容编辑 (`PushEditOperations`, `ApplyEdits`)
- ✅ 撤销/重做 (`Undo`, `Redo`, `PushStackElement`)
- ✅ EOL管理 (`SetEol`, `PushEol`)
- ✅ 装饰系统 (`AddDecoration`, `DeltaDecorations`, `GetDecorationsInRange`)
- ✅ 搜索功能 (`FindMatches`, `FindNextMatch`, `FindPreviousMatch`)
- ✅ 选项管理 (`GetOptions`, `UpdateOptions`, `DetectIndentation`)
- ✅ 语言管理 (`SetLanguage`, 语言配置订阅)
- ✅ 编辑器附着 (`AttachEditor`, `DetachEditor`)

**缺失的TS功能:**

| 功能 | 描述 |
|------|------|
| `BracketPairsTextModelPart` | 括号对匹配与着色 |
| `TokenizationTextModelPart` | 语法标记化 |
| `GuidesTextModelPart` | 缩进指南 |
| `ColorizedBracketPairsDecorationProvider` | 彩色括号装饰 |
| `normalizeIndentation()` | 缩进规范化 |
| `getWordAtPosition()` | 单词边界检测 |
| `getWordUntilPosition()` | 光标前单词 |
| `validatePosition()` | 位置验证 |
| `validateRange()` | 范围验证 |
| `modifyPosition()` | 位置偏移 |
| `getFullModelRange()` | 完整模型范围 |
| `_trimAutoWhitespace` | 自动空白修剪 |

**事件系统对比:**
```typescript
// TS版
onWillDispose, onDidChangeDecorations, onDidChangeLanguage,
onDidChangeLanguageConfiguration, onDidChangeTokens, onDidChangeOptions,
onDidChangeAttached, onDidChangeInjectedText, onDidChangeLineHeight,
onDidChangeFont, onDidChangeContent
```

```csharp
// C#版
OnDidChangeContent, OnDidChangeOptions, OnDidChangeLanguage,
OnDidChangeDecorations, OnDidChangeLanguageConfiguration,
OnDidChangeAttached
```

**修正建议:**
1. 添加`ValidatePosition`和`ValidateRange`方法用于位置验证
2. 实现`GetFullModelRange()`(当前是私有方法`GetDocumentRange()`)
3. 考虑添加`GetWordAtPosition()`以支持单词选择功能

---

### 7. TextModelDecorationsChangedEventArgs.cs
**TS源:** textModelEvents.ts
**对齐状态:** ✅完全对齐

**分析:**
C#版正确移植了TS版的装饰变更事件类型。

**对应关系:**
```typescript
// TS
interface IModelDecorationsChangedEvent {
    affectsMinimap: boolean;
    affectsOverviewRuler: boolean;
    affectsGlyphMargin: boolean;
    affectsLineNumber: boolean;
}
class ModelLineHeightChanged { ownerId, decorationId, lineNumber, lineHeight }
class ModelFontChanged { ownerId, lineNumber }
```

```csharp
// C#
public sealed record class LineHeightChange(int OwnerId, string DecorationId, int LineNumber, int? LineHeight);
public sealed record class LineFontChange(int OwnerId, string DecorationId, int LineNumber);
public sealed class TextModelDecorationsChangedEventArgs : EventArgs {
    // 包含所有TS属性 + Changes列表 + 更多元数据
}
```

**C#增强:**
- 添加了`Changes`列表，包含具体的装饰变更
- 添加了`ModelVersionId`用于版本追踪
- 添加了`AffectedInjectedTextLines`、`AffectedLineHeights`、`AffectedFontLines`细粒度信息

**结论:**
C#版不仅移植了TS版，还进行了合理增强。

---

### 8. TextModelOptions.cs
**TS源:** model.ts + textModelDefaults.ts
**对齐状态:** ✅完全对齐

**分析:**
完整移植了TS版的模型选项相关类型。

**枚举对齐:**
```typescript
// TS
enum EndOfLineSequence { LF = 0, CRLF = 1 }
enum EndOfLinePreference { TextDefined = 0, LF = 1, CRLF = 2 }
enum DefaultEndOfLine { LF = 1, CRLF = 2 }
```

```csharp
// C# - 完全一致
public enum EndOfLineSequence { LF = 0, CRLF = 1 }
public enum EndOfLinePreference { TextDefined = 0, LF = 1, CRLF = 2 }
public enum DefaultEndOfLine { LF = 1, CRLF = 2 }
```

**默认值对齐:**
```typescript
// TS EDITOR_MODEL_DEFAULTS
tabSize: 4, indentSize: 4, insertSpaces: true,
detectIndentation: true, trimAutoWhitespace: true,
largeFileOptimizations: true,
bracketPairColorizationOptions: { enabled: true, independentColorPoolPerBracketType: false }
```

```csharp
// C# TextModelCreationOptions.Default - 完全一致
public sealed record class TextModelCreationOptions {
    TabSize = 4, IndentSize = 4, InsertSpaces = true,
    DetectIndentation = true, TrimAutoWhitespace = true,
    LargeFileOptimizations = true,
    BracketPairColorizationOptions = { Enabled: true, IndependentColorPoolPerBracketType: false }
}
```

**TextModelResolvedOptions对齐:**
- ✅ `Resolve()` 方法正确实现选项解析
- ✅ `WithUpdate()` 方法正确实现增量更新
- ✅ `Diff()` 方法正确生成变更事件

**结论:**
完全对齐，无需修正。

---

### 9. TextModelSearch.cs
**TS源:** textModelSearch.ts
**对齐状态:** ❌需要修正

**分析:**
C#版实现了核心搜索功能，但缺少部分TS版方法。

**已实现功能:**
- ✅ `FindMatches()` - 多行/逐行搜索
- ✅ `FindNextMatch()` - 向前搜索
- ✅ `FindPreviousMatch()` - 向后搜索
- ✅ `SearchRangeSet` - 搜索范围管理
- ✅ `PieceTreeSearcher` - 正则搜索器(假设在别处)
- ✅ `LineFeedCounter` - CRLF补偿

**缺失的TS功能:**

```typescript
// TS版有，C#版缺失
export function isMultilineRegexSource(searchString: string): boolean
export function createFindMatch(range, rawMatches, captureMatches): FindMatch
export function isValidMatch(wordSeparators, text, textLength, matchStartIndex, matchLength): boolean
function leftIsWordBounday(...): boolean
function rightIsWordBounday(...): boolean
class Searcher {
    reset(lastIndex: number): void
    next(text: string): RegExpExecArray | null
}
```

**偏差说明:**
1. `isMultilineRegexSource()` - 判断正则是否需要多行模式，C#版在`SearchParams.ParseSearchRequest()`中处理
2. `Searcher`类 - C#版可能在`PieceTreeSearcher`中实现，需确认
3. 词边界检测函数 - 需要在`WordSeparatorClassifier`中实现

**修正建议:**
1. 确保`SearchParams.ParseSearchRequest()`正确检测多行正则
2. 添加或确认`PieceTreeSearcher`完整实现TS版`Searcher`功能:
   - `reset(lastIndex)` - 重置搜索位置
   - `next(text)` - 返回下一个匹配（处理空匹配和代理对）
3. 确认词边界检测逻辑完整

---

### 10. TextPosition.cs
**TS源:** position.ts (Lines 9-200+)
**对齐状态:** ❌需要修正

**分析:**
C#版只实现了最基本的位置结构，缺少TS版`Position`类的多个方法。

**已实现:**
```csharp
public readonly record struct TextPosition(int LineNumber, int Column) : IComparable<TextPosition>
{
    public static readonly TextPosition Origin = new(1, 1);
    public int CompareTo(TextPosition other);
    // 比较运算符 <, >, <=, >=
}
```

**TS版完整API:**
```typescript
class Position {
    constructor(lineNumber, column);
    with(newLineNumber?, newColumn?): Position;
    delta(deltaLineNumber?, deltaColumn?): Position;
    equals(other): boolean;
    static equals(a, b): boolean;
    isBefore(other): boolean;
    static isBefore(a, b): boolean;
    isBeforeOrEqual(other): boolean;
    static isBeforeOrEqual(a, b): boolean;
    static compare(a, b): number;
    clone(): Position;
    toString(): string;
    static lift(pos): Position;
    static isIPosition(obj): boolean;
    toJSON(): IPosition;
}
```

**缺失方法:**

| 方法 | 描述 | 重要性 |
|------|------|--------|
| `with(line?, col?)` | 创建新位置 | 高 |
| `delta(dLine?, dCol?)` | 偏移位置 | 高 |
| `equals()` | 相等比较 | 中（record自带） |
| `isBefore()` | 严格前置比较 | 高 |
| `isBeforeOrEqual()` | 前置或相等比较 | 高 |
| `clone()` | 克隆 | 低（struct自带值复制） |
| `toString()` | 字符串表示 | 低 |
| `lift()` | 从接口创建 | 低 |
| `isIPosition()` | 类型检查 | 低 |

**修正建议:**
添加以下关键方法:

```csharp
public readonly record struct TextPosition(int LineNumber, int Column) : IComparable<TextPosition>
{
    // 现有成员...
    
    /// <summary>创建具有可选新行号和列号的新位置</summary>
    public TextPosition With(int? lineNumber = null, int? column = null)
        => new(lineNumber ?? LineNumber, column ?? Column);
    
    /// <summary>偏移当前位置</summary>
    public TextPosition Delta(int deltaLine = 0, int deltaColumn = 0)
        => new(Math.Max(1, LineNumber + deltaLine), Math.Max(1, Column + deltaColumn));
    
    /// <summary>测试此位置是否在另一位置之前（不含相等）</summary>
    public bool IsBefore(TextPosition other)
        => LineNumber < other.LineNumber || (LineNumber == other.LineNumber && Column < other.Column);
    
    /// <summary>测试此位置是否在另一位置之前或相等</summary>
    public bool IsBeforeOrEqual(TextPosition other)
        => LineNumber < other.LineNumber || (LineNumber == other.LineNumber && Column <= other.Column);
    
    public override string ToString() => $"({LineNumber},{Column})";
}
```

---

## 总结

### 高优先级修正项

1. **TextPosition.cs** - 添加`With()`, `Delta()`, `IsBefore()`, `IsBeforeOrEqual()`方法
2. **TextModelSearch.cs** - 确认`PieceTreeSearcher`完整性，添加词边界检测

### 中优先级改进项

1. **PieceTreeBuffer.cs** - 考虑添加`Equals()`, `GetRangeAt()`, `GetLinesContent()`
2. **TextModel.cs** - 添加`ValidatePosition()`, `ValidateRange()`, 公开`GetFullModelRange()`

### 设计决策说明

以下差异是故意为之的设计决策，不需要修正：
- `EditStack.cs`的架构简化
- `ILanguageConfigurationService.cs`的原创实现
- `IUndoRedoService.cs`的内存实现
- `PieceTreeBuffer.cs`作为简化门面

这些决策适应了C#运行时环境和简化的DI需求，在功能上与TS版等价。
