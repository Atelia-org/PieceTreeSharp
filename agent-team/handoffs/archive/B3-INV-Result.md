# B3-INV-Result – Batch #3 调研与迁移规格

## 1. Summary
Batch #3 目标：在 Sprint 03 剩余时间（至 2025-11-29，尚余 6 日）补齐 DocUI Find 体系与底层未迁移测试：
1) FindModel 剩余多光标用例（`selectAllMatches` & primary cursor 保持），
2) getSelectionSearchString 三类场景（位置、单行选区、多行选区 null），
3) FindController 逻辑分组（导航循环、自动逃逸 regex、选区种子、选项持久化、searchScope 生命周期），
4) Decorations stickiness/per-line 查询，
5) PieceTree fuzz & invariants，
6) Diff char-change / pretty / whitespace / move / timeout 高级案例，
7) TextModelSearch word boundary matrix（补足 Intl.Segmenter 缺失差异标记）。

执行性评估：前 5 组对齐主要依赖轻量 harness 与已有 C# 搜索/装饰实现，复杂度中低；Diff Pretty 与 Fuzz/Invariants 需新辅助工具与随机生成框架，可部分延后并在本批给出规格与最小锚（Tier B/C）。预计：B3-FM,B3-FSel,B3-FC-Core,B3-FC-Scope,B3-Decor-Stickiness 可在当前 Sprint 完成；B3-PieceTree-Fuzz,B3-Diff-Pretty 若遇阻转入 Sprint 04。

## 2. Test Gap Inventory
| ID | TS Source / Case | Behavior Focus | Dependencies（服务/选项/模型） | Portability Tier (A/B/C) | Blocking Risks | Recommended Harness Strategy |
| --- | --- | --- | --- | --- | --- | --- |
| FM-01 | findModel.test.ts: selectAllMatches | 批量选择所有匹配 | TextModelSearch, FindReplaceState, FindDecorations | A | 需多选区集合 API 稳定 | 提供 `IEditorHost.GetAllMatches()` → 归并后创建 `Selections[]`，保持顺序 |
| FM-02 | findModel.test.ts: issue #14143 primary cursor | 主光标保持与排序 | 多光标排序逻辑 | A | 若排序不稳定导致期望错位 | 在执行前记录 primary match，排序：primary 优先 + 起始位置 |
| FSel-01 | find.test.ts: search string at position | 光标处单词派生 | Position→word-under-cursor | A | 缺少 word boundary helper 精确度 | 静态 `FindUtilities.GetWordAtPosition()`，ASCII 分界 |
| FSel-02 | find.test.ts: search string with selection | 单行选区返回文本 | Selection 单行判断 | A | 多光标未来扩展差异 | 现支持单光标；多选暂忽略其余 |
| FSel-03 | find.test.ts: multiline selection null | 跨行选区返回 null | Selection 跨行检测 | A | 行尾换行解析不一致 | 判断 `startLine != endLine` 或包含换行字符 |
| FC-Core-Nav | findController.test.ts: #1857/#3090 | F3/Next/Prev 循环逻辑 | CommonFindController, FindModel, cursor pos | B | 缺少 `EditorAction` 注册与异步启动 `_start()` | 提供同步包装：`StartFind()` + `MoveNext()` + `MovePrev()` |
| FC-Core-RegexEscape | findController.test.ts: #6149 | 自动逃逸选区为正则输入 | isRegex 切换, selection seed | B | 转义策略与 .NET Regex 差异 | 复用现有 `SearchParams.CreateRegExp` 替换；测试预期按转义字符集比对 |
| FC-Core-Seed | findController.test.ts: CMD+E (#47400/#109756) | 选区种子多行 + 空选区处理 | Clipboard/Selection, FindReplaceState | B | Mac 剪贴板分支行为 | 提供平台判定；非 Mac 跳过相关剪贴板写入断言 |
| FC-Core-ReplaceEdge | findController.test.ts: #18111/#24714 | 正则替换空格/行首 | Regex lookahead/boundary | B | .NET 与 JS 边界差异 | 使用 `RegexOptions.Multiline` 模式；若行为差异标记 `ExpectedDifferentBoundary` |
| FC-Core-SelectionRegex | findController.test.ts: #38232 | Regex 模式下 Next Selection | 多选区 + isRegex | B | 多选区顺序与 TS 不一致 | 捕获初选区后仅导航到下一匹配，比较位置 |
| FC-Options-Persist | findController.test.ts: matchCase/wholeWord/regex toggling | 选项持久化至 storage | InMemoryStorageService | A | Storage 键命名不对齐 | 键：`editor.isRegex` 等映射常量 |
| FC-Scope-Lifecycle | findController.test.ts: #9043/#27083/#58604 | searchScope 更新/清理/自动更新 | Selections[], autoFindInSelection, state.searchScope | B | 多选区归一化与空选区策略 | 归一化为非空 ranges；空选区不更新；关闭后清空 |
| DEC-01 | modelDecorations.test.ts stickiness matrix | 插入/删除对范围影响 | Decorations stickiness enum + changeDecorations | B | 缺少完整 stickiness 映射 | 建立枚举映射 + 4 场景表驱动测试 |
| DEC-02 | modelDecorations.test.ts per-line queries | 行级装饰查询 | IntervalTree / line index | B | IntervalTree 边界 bug（CI-1） | 先修复 CI-1 后添加行过滤测试 |
| DEC-03 | modelDecorations.test.ts overlap & collapse | 重叠合并/稳定性 | DeltaDecorations 操作序 | B | 排序策略不一致 | 明确排序：startLine/startColumn 决定输出序 |
| PTF-01 | pieceTreeTextBuffer.test.ts fuzz edits | 随机编辑稳定性 | PieceTree insert/delete, snapshot | C | 需 deterministic RNG + 回放工具 | `FuzzRunner(seed, ops)` + 不变量断言 |
| PTF-02 | pieceTreeTextBuffer.test.ts invariants | 树高度/行缓存/offset 索引 | RBTree/line cache | C | 未有高度/缓存公开 API | 暴露诊断方法仅在测试可见（`Internals`） |
| PTF-03 | pieceTreeTextBuffer.test.ts snapshot immutability | 快照只读行为 | Snapshot factory | B | 快照共享引用导致写入逃逸 | 创建深复制 piece 列表供枚举 |
| DIFF-01 | diffComputer.test.ts char-change pretty cases | 字符级美化 diff | Pretty diff heuristics | C | 未实现移动检测/字符聚合 | 升级后测试；当前标记 SkipIfMissingFeature |
| DIFF-02 | diffComputer.test.ts whitespace trim | 选项影响 diff 结果 | Trim whitespace flags | B | 配置层缺失 | 注入 `DiffOptions` 对象默认值 |
| DIFF-03 | diffComputer.test.ts move detection | 行移动映射 | Move heuristic | C | 尚无实现 | 规格占位；测试写为期待空 move 列表 |
| DIFF-04 | diffComputer.test.ts timeout large input | 超时 fallback 行级 diff | Timeout config | B | 需模拟长文本 & 超时 | 人工缩短阈值配置 + 断言 fallback 标记 |
| TMS-01 | textModelSearch.test.ts word boundary matrix | wholeWord 边界准确性 | WordCharacterClassifier, Searcher | B | Intl.Segmenter 缺失 | 标记 CJK 行为差异；ASCII 正常 |
| TMS-02 | textModelSearch.test.ts multiline regex | 多行匹配起止校准 | Regex multiline, CRLF | B | CRLF 正规化差异 | 双路径（\n vs \r\n）比较 |
| TMS-03 | textModelSearch.test.ts capture groups | 捕获组索引与替换 | Regex groups, ReplacePattern | A | None | 使用现有 ReplacePattern 测试辅助 |

说明：Portability Tier A=直接迁移；B=需最小桩/配置；C=需新工具或较大环境模拟。

## 3. Prioritized Batches
| 子批次 | 描述 | 预计新增测试数 | 复杂度 | 负责人 | 前置（C# 类/Stub） |
| --- | --- | --- | --- | --- | --- |
| B3-FM | FindModel 多光标：FM-01, FM-02 | 2–4 | 低 | Porter-CS | 已有 FindModel/FindReplaceState；补 `SelectAllMatches()` 主光标策略 |
| B3-FSel | getSelectionSearchString：FSel-01~03 | 6–8 | 低 | Porter-CS / QA-Automation | 新建 `FindUtilities.cs` + selection helper |
| B3-FC-Core | Controller 核心导航/选区种子/regex 逃逸/选项持久化：FC-Core-* + FC-Options-Persist | 12–15 | 中 | Porter-CS | `DocUIFindController.cs` + `IEditorHost` stub + `StorageStub` |
| B3-FC-Scope | searchScope 生命周期与自动更新：FC-Scope-Lifecycle | 5–7 | 中 | Porter-CS / QA-Automation | 在 Core 完成后扩展 `UpdateSearchScope()` |
| B3-Decor-Stickiness | Stickiness & per-line：DEC-01~03 | 10–12 | 中 | QA-Automation / Porter-CS | 修复 IntervalTree CI-1；定义 Stickiness enum + mapping |
| B3-PieceTree-Fuzz | Fuzz & invariants：PTF-01~03 | 15–20 | 高 | QA-Automation | 新建 `PieceTreeFuzzRunner.cs` + Internals 访问器 |
| B3-Diff-Pretty | Diff pretty/whitespace/move/timeout：DIFF-01~04 | 12–16 | 高 | Porter-CS / QA-Automation | 扩展 `DiffOptions` + Pretty heuristics stub |

建议本 Sprint 优先：B3-FM → B3-FSel → B3-FC-Core → B3-FC-Scope → B3-Decor-Stickiness；其余两批若时间不足提供代码骨架与空测试占位（记录 Skip 标签与 Tier C）。

## 4. Harness Requirements
最小抽象层（DocUI Find 测试）：
- `IEditorHost`：属性 `Selections`, 方法 `SetSelections(Selection[])`, `GetValue()`, `SetValue(string)`, `ApplyEdits(Edit[])`, `GetWordAt(Position)`，事件模拟可同步触发（无需真正异步）。
- `FindController`（C#）: 映射 TS `CommonFindController` 核心 API：`Start(FindStartOptions)`, `Close()`, `MoveNext()`, `MovePrev()`, `Replace()`, `ReplaceAll()`, `ToggleRegex()`, `SetSearchString(string)`。
- 上下文键简化：以枚举 `FindContextKey { None, InputFocused }` 替代 `IContextKeyService`；在测试中直接赋值验证状态。
- Clipboard Stub：接口 `IClipboardService { string? ReadFindText(); void WriteFindText(string); }`；Mac 分支条件检测（若非 Mac → 相关测试 Skip）。
- Storage Stub：字典封装 `IStorageService`：键 `editor.isRegex`, `editor.matchCase`, `editor.wholeWord`；方法 `GetBool(key)`, `SetBool(key, value)`。
- Delayer：TS 使用 `Delayer(50)`；C# 测试同步执行即可，延迟相关行为（如历史更新）直接调用逻辑（记录差异注释）。
- `FindUtilities.GetSelectionSearchString(IEditorHost host)`：逻辑：若单光标无选区 → word-under-cursor；若单行选区 → 返回文本；若跨行或包含换行 → 返回 null；多选暂取第一个。多行返回 null 条件：`Selection.StartLine != Selection.EndLine` 或 文本包含 `\n`。
- Primary Cursor 保持：`SelectAllMatches()`：收集所有 match → 若存在与 `host.Selection` 等值匹配，排序优先；否则保持顺序并首元素设为第一个匹配（与 TS 保持）。
- SearchScope 更新：`UpdateSearchScope(Selection[] selections, bool autoMode)`：忽略空（start==end）选区；多选合并为数组；关闭 Find 时清空。

## 5. Risks & Mitigations
| 风险 | 描述 | 缺口影响 | Mitigation 最小策略 |
| --- | --- | --- | --- |
| Mac 剪贴板差异 | 仅 Mac 写入/读取 find buffer | 非 Mac 行为不同 | 测试条件：`if(!IsMac) Skip("Clipboard")`；核心逻辑仍验证 searchString 种子 |
| Storage 持久化 | 未实现持久层/作用域 | 选项轮换后不保存 | 内存字典模拟；测试前预置初始值 |
| 上下文键 | TS 使用 ContextKeyService | 影响 Focus 行为断言 | 简化为枚举属性；仅验证 Boolean 状态 |
| 异步启动 `_start()` | TS Promise + Delayer | 可能次序差异 | 使用同步实现，必要时人工调用 `ProcessPendingHistory()` |
| 多选 searchScope | 交叠/空选区处理复杂 | 作用域错误或未清除 | 归一化过滤空区间；关闭时强制清空 |
| 正则自动转义 | JS 与 .NET Regex 差异 | 匹配数不同 | 集中测试用例，若差异标注 `ExpectedDifferentBoundary` |
| Stickiness 映射 | TS 枚举差异 | 范围扩张缩减不一致 | 建立严格映射表 + 表驱动测试 |
| Fuzz 非确定性 | 随机序列不可重现 | 难复现失败 | 固定种子 + 序列日志输出（JSON） |
| Diff move/pretty 未实现 | 测试失败或行为空 | 影响覆盖度 | 标记 `SkipIfMissingFeature`，提供空实现占位 |
| Word boundary Unicode | 缺 Intl.Segmenter | CJK 行为不同 | 标记差异，添加可选 ICU4N 后启用扩展测试 |

## 6. Proposed RunSubAgent Sequence
| Run # | Task ID | SubAgent | Deliverable | Exit Criteria |
| --- | --- | --- | --- | --- |
| R12 | B3-FM | Porter-CS | `DocUIFindModelTests.cs` 新增 2–4 测试 + `FindModel.SelectAllMatches` 主光标保留实现 | 所有测试通过；生成 delta `#delta-2025-11-23-b3-fm` |
| R13 | B3-FSel | Porter-CS / QA-Automation | `FindUtilities.cs` + `DocUIFindSelectionTests.cs` 6–8 测试 | 单/多行逻辑正确；delta `#delta-2025-11-23-b3-fsel` |
| R14 | B3-FC-Core | Porter-CS | `DocUIFindController.cs` 核心 API + Core 导航/regex/种子/选项测试 | 12+ 测试通过；storage 持久化验证；delta `#delta-2025-11-23-b3-fc-core` |
| R15 | B3-FC-Scope | Porter-CS / QA-Automation | searchScope 生命周期测试集 | 5+ 测试通过（清理/自动更新）；delta `#delta-2025-11-23-b3-fc-scope` |
| R16 | B3-Decor-Stickiness | QA-Automation / Porter-CS | Stickiness & per-line & overlap 测试 | 10+ 测试；CI-1 修复验证；delta `#delta-2025-11-23-b3-decor-stickiness` |
| R17 | B3-PieceTree-Fuzz | QA-Automation | Fuzz runner + invariants 15+ 用例 | 稳定种子通过；日志保存；delta `#delta-2025-11-23-b3-piecetree-fuzz` |
| R18 | B3-Diff-Pretty | Porter-CS / QA-Automation | Pretty diff/whitespace/timeout 基础测试 | 基础用例通过；未实现特性标记 Skip；delta `#delta-2025-11-23-b3-diff-pretty` |
| R19 (可选) | B3-Finish | DocMaintainer / Info-Indexer | 汇总与文档同步 | 所有 delta 引用更新至 AGENTS/Task Board/Sprint/Indexes |

## 7. Update Instructions
| 角色 | 子批次完成后需更新 | 变更锚点 / 内容 |
| --- | --- | --- |
| Porter-CS | 新增/修改的 C# 源与测试文件 | 在提交描述中引用对应 delta tag（如 `#delta-2025-11-23-b3-fm`） |
| QA-Automation | `TestMatrix.md` 新增行、TRX 结果、Fuzz 日志 | 在矩阵行添加 Port/Tier/Delta 列；Fuzz 日志路径与种子记录 |
| DocMaintainer | `docs/sprints/sprint-03.md` Progress Log 行、`agent-team/task-board.md` | 为每个完成的子批次追加行与任务完成状态 |
| Info-Indexer | `agent-team/indexes/README.md` 新增 changefeed 区段 | 针对每个 delta 添加文件路径列表与测试覆盖度摘要 |
| Planner | 若批次延期（Fuzz/Diff）更新下个 Sprint 计划 | 在下个 sprint 文档添加迁移延后原因与新 ETA |
| Investigator-TS | 更新记忆文件 Latest Focus & 记录差异点 | 添加下一阶段调研点（如 ICU4N / move heuristic） |

### Delta Tag 统一命名（已预留）
```
#delta-2025-11-23-b3-fm
#delta-2025-11-23-b3-fsel
#delta-2025-11-23-b3-fc-core
#delta-2025-11-23-b3-fc-scope
#delta-2025-11-23-b3-decor-stickiness
#delta-2025-11-23-b3-piecetree-fuzz
#delta-2025-11-23-b3-diff-pretty
```

### 后续验证建议
- 在 B3-FM 完成后立即运行现有 FindModel 测试集合，确认新增多光标逻辑不影响已迁移测试。
- B3-FC-Core 引入后对比 TS 源行为：尤其 F3 循环与自动逃逸 regex；记录任何差异并加注释。
- Stickiness 测试完成后与 TS 期望范围字符串快照比对（必要时引入简化范围序列化 helper）。
- Fuzz 日志保留最近 N 次失败用例（若有），供重放：`PieceTreeFuzzRunner --replay <seed>`。

---
产物到此，供主 Agent 直接调度后续 runSubAgent 序列。