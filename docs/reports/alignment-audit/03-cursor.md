# Cursor 模块对齐审查报告

**审查日期:** 2025-11-26
**审查范围:** 9个光标相关文件

## 概要
- 完全对齐: 0/9
- 存在偏差: 3/9
- 需要修正: 6/9

## 详细分析

---

### 1. Cursor.cs
**TS源:** `oneCursor.ts` (Lines 15-200)
**对齐状态:** ❌需要修正

**分析:**

TypeScript原版的 `Cursor` 类核心设计:
- 维护 `modelState` 和 `viewState` 两个 `SingleCursorState` 对象，分别代表模型坐标和视图坐标
- 使用 `_selTrackedRange` 跟踪选择范围变化
- 通过 `CursorContext` 访问模型和协调转换器
- 核心方法 `_setState` 负责验证和同步 model/view 状态
- 支持选择追踪 (`startTrackingSelection`/`stopTrackingSelection`)

C#实现的主要偏差:
1. **架构设计不同**: C#版本是一个完整的独立cursor类，直接包含移动逻辑(`MoveLeft`, `MoveRight`, `MoveUp`, `MoveDown`, `MoveWordLeft`等)，而TS版本的`Cursor`类只负责状态管理，移动逻辑在其他地方(如`CursorMoveOperations`)
2. **缺少双状态模型**: TS版本维护`modelState`和`viewState`两个状态，C#版本只有一个`_selection`
3. **缺少TrackedRange机制**: TS版本使用`_selTrackedRange`追踪范围变化，C#版本缺少此功能
4. **缺少SingleCursorState**: TS的`SingleCursorState`包含`selectionStart`, `selectionStartKind`, `leftoverVisibleColumns`等，C#完全缺失
5. **缺少CursorContext依赖**: TS版本的所有操作都需要CursorContext，C#版本直接持有TextModel

**偏差说明:**
这是一个**重新设计**而非**直译移植**。C#版本将多个TS类的职责合并到一个类中，虽然功能上可用，但与TS原版架构差异显著。

**修正建议:**
1. 将移动逻辑拆分到单独的`CursorMoveOperations`类
2. 引入`SingleCursorState`类来存储完整的光标状态
3. 实现`modelState`/`viewState`双状态模型
4. 添加`TrackedRange`支持用于选择追踪
5. 重构为依赖`CursorContext`而非直接持有`TextModel`

---

### 2. CursorCollection.cs
**TS源:** `cursorCollection.ts` (Lines 15-250)
**对齐状态:** ❌需要修正

**分析:**

TypeScript原版的 `CursorCollection` 类核心设计:
- 维护 `cursors` 数组，`cursors[0]` 是主光标，其余是次要光标
- 使用 `CursorContext` 管理所有光标
- 实现 `lastAddedCursorIndex` 追踪最后添加的光标
- 提供 `normalize()` 方法合并重叠的光标
- 支持 `setStates()` 批量设置光标状态
- 提供 `getTopMostViewPosition()` / `getBottomMostViewPosition()` 等视图位置查询

C#实现的主要偏差:
1. **缺少lastAddedCursorIndex**: 无法追踪最后添加的光标索引
2. **缺少normalize()方法**: 没有实现合并重叠光标的逻辑
3. **缺少CursorContext**: 直接使用TextModel而非CursorContext
4. **缺少状态批量设置**: 没有`setStates()`和`_setSecondaryStates()`方法
5. **缺少选择追踪**: 没有`startTrackingSelections()`/`stopTrackingSelections()`
6. **缺少视图位置查询**: 没有`getTopMostViewPosition()`等方法
7. **缺少getAll()**: 没有返回所有CursorState的方法

**偏差说明:**
C#版本是一个**极度简化**的实现，只提供基本的创建/删除/获取位置功能，缺少TS版本的大量核心功能。

**修正建议:**
1. 添加 `lastAddedCursorIndex` 字段和 `GetLastAddedCursorIndex()` 方法
2. 实现完整的 `Normalize()` 方法处理重叠光标合并
3. 添加 `SetStates()` 方法支持批量状态设置
4. 实现 `KillSecondaryCursors()` 方法
5. 添加 `GetAll()` 返回所有CursorState
6. 实现视图位置查询方法

---

### 3. CursorColumns.cs
**TS源:** `cursorColumnSelection.ts` (Lines 10-50)
**对齐状态:** ⚠️存在偏差

**分析:**

TypeScript原版是 `ColumnSelection` 类，核心方法:
- `columnSelect()`: 执行列选择，返回多个`SingleCursorState`
- 使用 `config.columnFromVisibleColumn()` 和 `config.visibleColumnFromColumn()` 进行转换
- 处理RTL/LTR方向
- 返回 `IColumnSelectResult` 包含viewStates和toLineNumber/toVisualColumn

C#实现的主要偏差:
1. **方法签名不同**: C#提供静态工具方法，TS是类方法
2. **缺少columnSelect核心方法**: C#只有辅助转换方法，缺少实际的列选择逻辑
3. **返回类型不同**: TS返回`IColumnSelectResult`包含多个光标状态，C#只返回单个位置
4. **缺少方向处理**: 没有RTL/LTR方向支持
5. **注入文本处理可疑**: C#版本处理注入文本的逻辑与TS不完全一致

**偏差说明:**
C#版本只实现了辅助转换函数，缺少核心的`columnSelect`列选择算法。

**修正建议:**
1. 添加 `ColumnSelect()` 方法实现完整的列选择逻辑
2. 定义 `IColumnSelectResult` 类型
3. 处理RTL/LTR方向
4. 验证注入文本处理逻辑的正确性

---

### 4. CursorContext.cs
**TS源:** `cursorContext.ts` (Lines 10-23)
**对齐状态:** ❌需要修正

**分析:**

TypeScript原版的 `CursorContext` 类:
```typescript
export class CursorContext {
    public readonly model: ITextModel;
    public readonly viewModel: ICursorSimpleModel;
    public readonly coordinatesConverter: ICoordinatesConverter;
    public readonly cursorConfig: CursorConfiguration;
}
```

C#实现的主要偏差:
1. **缺少viewModel**: TS有独立的viewModel用于视图坐标，C#缺失
2. **缺少coordinatesConverter**: 用于model/view坐标转换的关键组件缺失
3. **缺少cursorConfig**: 光标配置(如多光标合并策略等)缺失
4. **ComputeAfterCursorState设计不同**: C#版本的实现只是返回当前位置，而TS版本更复杂

**偏差说明:**
C#版本严重简化，缺少TS版本的核心组件。

**修正建议:**
1. 添加 `ICoordinatesConverter` 接口和实现
2. 添加 `CursorConfiguration` 类
3. 添加 `ViewModel` 属性
4. 实现正确的坐标转换逻辑

---

### 5. CursorState.cs
**TS源:** `cursorCommon.ts` (Lines 271-340)
**对齐状态:** ❌需要修正

**分析:**

TypeScript原版有多个相关类:
- `CursorState`: 包含 `modelState` 和 `viewState` (两个`SingleCursorState`)
- `PartialModelCursorState`: 只有modelState
- `PartialViewCursorState`: 只有viewState
- `SingleCursorState`: 包含 `selectionStart`, `selectionStartKind`, `selectionStartLeftoverVisibleColumns`, `position`, `leftoverVisibleColumns`
- `SelectionStartKind` 枚举: Simple, Word, Line

C#实现的主要偏差:
1. **SingleCursorState完全缺失**: 这是TS中最核心的状态类
2. **设计完全不同**: C#的`CursorState`包含`OwnerId`, `Selection`, `StickyColumn`, `DecorationIds`，与TS设计完全不同
3. **缺少PartialModelCursorState/PartialViewCursorState**: 用于部分状态设置的类缺失
4. **缺少SelectionStartKind枚举**: 用于区分选择开始类型(Simple/Word/Line)
5. **缺少leftoverVisibleColumns**: 用于保持视觉列位置的重要字段
6. **缺少静态工厂方法**: `fromModelState()`, `fromViewState()`, `fromModelSelection()`等

**偏差说明:**
这是**完全不同的设计**，C#版本的CursorState与TS版本几乎没有对应关系。

**修正建议:**
1. 创建 `SingleCursorState` 类，包含所有必要字段
2. 重新设计 `CursorState` 为包含 `modelState` 和 `viewState`
3. 添加 `PartialModelCursorState` 和 `PartialViewCursorState`
4. 添加 `SelectionStartKind` 枚举
5. 实现所有静态工厂方法

---

### 6. SnippetController.cs
**TS源:** `snippetController2.ts` (Lines 30-500)
**对齐状态:** ❌需要修正

**分析:**

TypeScript原版的 `SnippetController2` 是一个完整的编辑器贡献(IEditorContribution):
- 使用上下文键(ContextKey)管理snippet模式状态: `InSnippetMode`, `HasNextTabstop`, `HasPrevTabstop`
- 支持复杂的插入选项: `overwriteBefore`, `overwriteAfter`, `adjustWhitespace`, `undoStopBefore/After`
- 集成补全提供者(CompletionProvider)处理choice元素
- 支持模板合并(merge)
- 提供`finish()`, `cancel()`, `prev()`, `next()`方法
- 完整的状态更新逻辑(`_updateState`)

C#实现的主要偏差:
1. **不是编辑器贡献**: C#版本是独立类，不集成到编辑器系统
2. **缺少上下文键**: 没有InSnippetMode等状态管理
3. **缺少插入选项**: 没有overwriteBefore/After、adjustWhitespace等
4. **缺少补全集成**: 没有choice元素的补全支持
5. **缺少模板合并**: 没有merge功能支持嵌套snippet
6. **缺少完整的状态管理**: _updateState逻辑缺失
7. **缺少finish/cancel**: 只有基本的创建和导航

**偏差说明:**
C#版本是**最小化实现**，缺少TS版本的大部分功能。

**修正建议:**
1. 添加snippet模式状态管理
2. 实现完整的插入选项支持
3. 添加`Finish()`, `Cancel(resetSelection)`, `IsInSnippet()`方法
4. 实现状态更新逻辑
5. 考虑choice元素补全支持

---

### 7. SnippetSession.cs
**TS源:** `snippetSession.ts` (Lines 30-600)
**对齐状态:** ❌需要修正

**分析:**

TypeScript原版有两个类:
- `OneSnippet`: 单个snippet实例，管理placeholder装饰、移动、合并
- `SnippetSession`: 管理多个OneSnippet，处理编辑和光标选择

`OneSnippet` 核心功能:
- 使用`_placeholderDecorations` Map管理placeholder到装饰ID的映射
- `_placeholderGroups`: 按索引分组的placeholder数组
- `move(fwd)`: 移动到下一个/上一个placeholder，处理transformation
- 装饰选项: active/inactive/activeFinal/inactiveFinal
- 支持嵌套snippet合并(merge)
- 计算可能的选择(`computePossibleSelections`)
- 处理choice元素

`SnippetSession` 核心功能:
- 静态方法`adjustWhitespace`: 调整缩进
- 静态方法`adjustSelection`: 处理overwriteBefore/After
- 静态方法`createEditsAndSnippetsFromSelections`: 从选择创建编辑
- 变量解析器集成(ModelBased, Clipboard, Selection, Comment, Time, Workspace, Random)
- 完整的snippet解析和插入逻辑

C#实现的主要偏差:
1. **缺少OneSnippet类**: C#只有SnippetSession
2. **简化的placeholder解析**: 只支持`${n:text}`格式，不支持完整的TextMate snippet语法
3. **缺少placeholder分组**: 没有按索引分组
4. **缺少变量解析**: 没有任何变量解析器
5. **缺少transformation支持**: placeholder transform缺失
6. **缺少缩进调整**: adjustWhitespace逻辑缺失
7. **缺少嵌套合并**: merge功能缺失
8. **装饰选项简化**: 没有active/inactive区分
9. **缺少choice支持**: 没有处理choice元素

**偏差说明:**
C#版本是**极度简化**的实现，只支持最基本的numbered placeholder。

**修正建议:**
1. 实现完整的TextMate snippet解析器
2. 添加`OneSnippet`类
3. 实现placeholder分组和导航逻辑
4. 添加基本变量解析器
5. 实现缩进调整逻辑
6. 区分active/inactive装饰状态

---

### 8. WordCharacterClassifier.cs
**TS源:** `wordCharacterClassifier.ts` (Lines 20-150)
**对齐状态:** ⚠️存在偏差

**分析:**

TypeScript原版的 `WordCharacterClassifier`:
- 继承自 `CharacterClassifier<WordCharacterClass>`
- 使用 `WordCharacterClass` 枚举: Regular=0, Whitespace=1, WordSeparator=2
- 支持 Intl.Segmenter 进行国际化词分割
- 缓存行内容和分段结果以提高性能
- 提供 `findPrevIntlWordBeforeOrAtOffset` 和 `findNextIntlWordAtOrAfterOffset`
- 有全局缓存 `getMapForWordSeparators`

C#实现的主要偏差:
1. **不继承CharacterClassifier**: TS版本继承自通用字符分类器
2. **缺少WordCharacterClass枚举**: 只用bool判断
3. **缺少Intl.Segmenter支持**: 没有国际化词分割
4. **缺少缓存**: 没有行内容和分段结果缓存
5. **缺少Intl词查找方法**: `findPrevIntlWordBeforeOrAtOffset`等缺失
6. **缺少全局缓存**: 没有`GetMapForWordSeparators`工厂方法
7. **分类逻辑简化**: 使用`char.IsPunctuation`而非精确分类

**偏差说明:**
C#版本是**简化实现**，对于基本的ASCII文本可以工作，但缺少国际化支持。

**修正建议:**
1. 添加 `WordCharacterClass` 枚举
2. 实现继承自基础CharacterClassifier的设计
3. 添加LRU缓存和全局工厂方法
4. 考虑.NET的国际化词分割支持(如ICU)

---

### 9. WordOperations.cs
**TS源:** `cursorWordOperations.ts` (Lines 50-800)
**对齐状态:** ⚠️存在偏差

**分析:**

TypeScript原版的 `WordOperations` 是一个庞大的类(866行):
- 私有方法: `_createWord`, `_createIntlWord`, `_findPreviousWordOnLine`, `_doFindPreviousWordOnLine`, `_findEndOfWord`, `_findNextWordOnLine`, `_doFindNextWordOnLine`, `_findStartOfWord`
- 移动方法: `moveWordLeft`, `moveWordRight`, `_moveWordPartLeft`, `_moveWordPartRight`
- 删除方法: `deleteWordLeft`, `deleteWordRight`, `deleteInsideWord`, `_deleteWordPartLeft`, `_deleteWordPartRight`
- 辅助方法: `getWordAtPosition`, `word`(双击选词)
- `WordNavigationType` 枚举: WordStart, WordEnd, WordStartFast, WordAccessibility
- `WordType` 枚举: None, Regular, Separator
- 复杂的`DeleteWordContext`上下文对象
- 支持自动闭合对处理

`WordPartOperations` 子类:
- `deleteWordPartLeft`, `deleteWordPartRight`
- `moveWordPartLeft`, `moveWordPartRight`

C#实现的主要偏差:
1. **大量方法缺失**: 只实现了`MoveWordLeft`, `MoveWordRight`, `SelectWordLeft`, `SelectWordRight`, `DeleteWordLeft`
2. **缺少WordNavigationType完整支持**: C#只有Word/WordPart，TS有WordStart/WordEnd/WordStartFast/WordAccessibility
3. **缺少WordType枚举**: 用于区分Regular和Separator词
4. **缺少_findPreviousWordOnLine/_findNextWordOnLine**: 核心词查找逻辑缺失
5. **缺少DeleteWordContext**: 复杂删除上下文缺失
6. **缺少自动闭合对处理**: 删除时的自动闭合对检测缺失
7. **缺少whitespaceHeuristics**: 空白处理启发式逻辑缺失
8. **缺少deleteInsideWord**: 删除词内部逻辑缺失
9. **缺少getWordAtPosition**: 获取光标处单词
10. **缺少word()选词方法**: 双击选词逻辑缺失
11. **缺少WordPartOperations**: camelCase/snake_case词部分操作缺失
12. **算法简化**: 当前实现的词边界判断逻辑比TS版本简单很多

**偏差说明:**
C#版本只实现了TS版本约**15%**的功能，缺少大量核心逻辑。

**修正建议:**
1. 添加 `WordType` 枚举
2. 扩展 `WordNavigationType` 枚举
3. 实现 `_findPreviousWordOnLine` 和 `_findNextWordOnLine` 核心方法
4. 添加 `DeleteWordContext` 类
5. 实现完整的 `moveWordLeft`/`moveWordRight` 支持所有导航类型
6. 添加 `deleteWordRight`, `deleteInsideWord` 方法
7. 实现 `getWordAtPosition` 方法
8. 添加 `WordPartOperations` 类

---

## 总结

### 严重程度分类

**🔴 需要重大重构 (6个文件):**
1. `Cursor.cs` - 架构设计完全不同
2. `CursorCollection.cs` - 缺少大量核心功能
3. `CursorContext.cs` - 缺少关键组件
4. `CursorState.cs` - 设计完全不同
5. `SnippetController.cs` - 最小化实现
6. `SnippetSession.cs` - 极度简化

**🟡 需要补充功能 (3个文件):**
1. `CursorColumns.cs` - 缺少核心列选择方法
2. `WordCharacterClassifier.cs` - 缺少国际化和缓存
3. `WordOperations.cs` - 只实现了约15%功能

### 优先级建议

**P0 - 阻塞性问题:**
1. 实现 `SingleCursorState` 类
2. 实现 `CursorContext` 的完整组件
3. 重构 `CursorState` 为双状态模型

**P1 - 核心功能:**
1. 完善 `WordOperations` 的词查找和导航逻辑
2. 实现 `CursorCollection.Normalize()` 
3. 补充 `WordCharacterClassifier` 的完整分类逻辑

**P2 - 扩展功能:**
1. Snippet相关功能增强
2. 国际化词分割支持
3. 列选择完整实现

### 移植质量评估

当前C# Cursor模块的移植质量为 **不合格**。大部分文件是重新设计而非直译移植，虽然提供了基本可用的功能，但与TS原版的架构和API差异显著，这将导致:
1. 未来同步TS更新困难
2. 行为不一致的边缘情况
3. 扩展功能时需要重新设计

建议在继续开发前，先建立与TS版本一致的核心数据结构(`SingleCursorState`, `CursorState`, `CursorContext`)，然后在此基础上逐步对齐其他类的实现。
