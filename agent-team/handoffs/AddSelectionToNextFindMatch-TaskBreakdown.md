# AddSelectionToNextFindMatch 任务分解

**创建时间**: 2025-12-05  
**预计工时**: ~10h  
**优先级**: P2  
**状态**: 设计阶段

## 背景

AddSelectionToNextFindMatch (Ctrl+D) 是多光标编辑的核心功能，用于快速选择下一个相同文本。VS Code 通过 `MultiCursorSession` 和 `MultiCursorSelectionController` 实现此功能。

## TS 原版架构

### 核心类 (ts/src/vs/editor/contrib/multicursor/)

1. **MultiCursorSelectionController** (`multicursor.ts` L100-250)
   - 入口点：`addSelectionToNextFindMatch()` 方法
   - 管理 session 生命周期
   - 协调 find model 和 cursor state

2. **MultiCursorSession** (`multicursor.ts` L25-98)
   - 核心状态机：`add()`, `moveLeft()`, `moveRight()`
   - 维护当前选区和目标文本
   - 返回 `SessionResult` 包含新选区列表

3. **SessionResult** (简单结构体)
   ```typescript
   interface SessionResult {
       selections: Selection[];
       revealRange: Range;
       revealScrollType: ScrollType;
   }
   ```

### 依赖关系

```
FindModel (已实现) ───→ MultiCursorSession (待实现)
                              ↓
                        MultiCursorSelectionController (待实现)
                              ↓
                        CursorCollection (已实现)
```

## 任务拆分

### Task 1: SessionResult 结构体 (~0.5h)
**文件**: `src/TextBuffer/Cursor/MultiCursorSessionResult.cs`

```csharp
public record MultiCursorSessionResult(
    IReadOnlyList<Selection> Selections,
    Range RevealRange,
    ScrollType RevealScrollType
);

public enum ScrollType
{
    Smooth,
    Immediate
}
```

**测试**: 结构体无需独立测试，集成测试覆盖

---

### Task 2: MultiCursorSession (~3h)
**文件**: `src/TextBuffer/Cursor/MultiCursorSession.cs`

#### 核心方法

```csharp
public class MultiCursorSession
{
    private readonly TextModel _model;
    private readonly FindModel _findModel;
    private Selection _currentSelection;
    private string _searchText;
    
    public MultiCursorSession(TextModel model, Selection initialSelection);
    
    // 核心 API
    public SessionResult? Add();           // 添加下一个匹配到选区
    public SessionResult? MoveLeft();      // 移动到左侧匹配
    public SessionResult? MoveRight();     // 移动到右侧匹配
    public SessionResult? SelectAll();     // 选择所有匹配
    
    // 辅助方法
    private Range? FindNextMatch(Position startFrom);
    private List<Selection> GetAllMatches();
}
```

#### 直译要点
1. 使用 `FindModel.FindNext()` 查找下一个匹配
2. 维护选区列表（去重）
3. 处理环绕搜索（wrap around）

**测试**: `tests/TextBuffer.Tests/MultiCursorSessionTests.cs` (+10 tests)
- `Add_FindsNextOccurrence`
- `Add_WrapsAround`
- `Add_SkipsCurrent`
- `MoveLeft_NavigatesToPrevious`
- `SelectAll_FindsAllMatches`

---

### Task 3: MultiCursorSelectionController (~3h)
**文件**: `src/TextBuffer/Cursor/MultiCursorSelectionController.cs`

#### 核心方法

```csharp
public class MultiCursorSelectionController
{
    private readonly TextModel _model;
    private readonly FindModel _findModel;
    private MultiCursorSession? _session;
    
    public MultiCursorSelectionController(TextModel model, FindModel findModel);
    
    // TS: addSelectionToNextFindMatch() (multicursor.ts L150-180)
    public SessionResult? AddSelectionToNextFindMatch();
    
    // TS: moveSelectionToNextFindMatch() (multicursor.ts L182-210)
    public SessionResult? MoveSelectionToNextFindMatch();
    
    // TS: selectHighlights() (multicursor.ts L212-240)
    public SessionResult? SelectAllMatches();
}
```

#### 直译要点
1. 创建/复用 `MultiCursorSession`
2. 从 `FindModel` 获取当前搜索文本
3. 处理空 session 情况

**测试**: `tests/TextBuffer.Tests/MultiCursorSelectionControllerTests.cs` (+8 tests)

---

### Task 4: 集成测试 (~2h)
**文件**: `tests/TextBuffer.Tests/MultiCursorIntegrationTests.cs`

#### TS 测试覆盖 (multicursor.test.ts)
- `test('addSelectionToNextFindMatch', ...)` (~Line 50)
- `test('issue #2205: Multi-cursor paste should work in empty lines', ...)` (~Line 120)
- `test('issue #43244: Find next selection matches should use the current cursor position', ...)` (~Line 180)

**测试场景**:
- 基础 Ctrl+D 流程（选中 → Ctrl+D → 添加下一个）
- 环绕搜索
- 大小写敏感
- 边界情况（文档开头/结尾）

---

### Task 5: 代码审阅与提交 (~1.5h)
1. 对照 TS 原版审阅
2. 确保注释标注 TS 源位置
3. 验证全量测试通过
4. 提交变更

---

## 实施顺序

```
Day 1 (4h):
  ├─ Task 1: SessionResult (0.5h)
  ├─ Task 2: MultiCursorSession (3h)
  └─ Task 2 测试验证 (0.5h)

Day 2 (4h):
  ├─ Task 3: MultiCursorSelectionController (3h)
  └─ Task 4: 集成测试 (1h)

Day 3 (2h):
  ├─ Task 4 完成 (1h)
  └─ Task 5: 审阅与提交 (1h)
```

## TS 源文件参考

- **实现**: `ts/src/vs/editor/contrib/multicursor/browser/multicursor.ts`
- **测试**: `ts/src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts`

## 验收标准

- [ ] MultiCursorSession 实现完整（Add/MoveLeft/MoveRight/SelectAll）
- [ ] MultiCursorSelectionController 集成 FindModel
- [ ] +18 tests 全部通过
- [ ] 代码头部注释标注 TS 源位置
- [ ] Issue regressions 覆盖（#2205, #43244）

---

**下一步**: 开始 Task 1 - SessionResult 结构体
