## 跨会话记忆文档
本文档(`./AGENTS.md`)会伴随每个 user 消息注入上下文，是跨会话的外部记忆。完成一个任务、制定或调整计划时务必更新本文件，避免记忆偏差。

## Team Leader 认知入口
新会话唤醒时，按以下顺序读取认知文件：
1. **[`agent-team/lead-metacognition.md`](agent-team/lead-metacognition.md)** — 元认知与方法论（我是谁、如何工作、如何成长）
2. **[`agent-team/status.md`](agent-team/status.md)** — 项目当前状态快照（测试基线、里程碑进度、活跃 changefeed）
3. **[`agent-team/todo.md`](agent-team/todo.md)** — 待办任务树（层次化目标拆分，完成即删除）

详细追踪仍在 Task Board / Sprint / Migration Log，但上述三个文件是**认知恢复的第一优先级**。

## 已知的工具问题
- 需要要删除请用改名替代，因为环境会拦截删除文件操作。
- 不要使用'insert_edit_into_file'工具，经常产生难以补救的错误结果。
- 当需要临时设置环境变量时，要显式用`export PIECETREE_DEBUG=0 && dotnet test ...`这样的写法，避免使用`PIECETREE_DEBUG=0 dotnet test ...`写法，后者会触发自动审批的命令行解析问题。

## 用户语言
请主要用简体中文与用户交流，对于术语/标识符等实体名称则优先用原始语言。

## 项目概览
**总体目标**是将位于“./ts”目录内的VS Code的无GUI编辑器核心移植为C#类库(dotnet 9.0 + xUnit)。核心目标是“./ts/src/vs/editor/common/model/pieceTreeTextBuffer”, 如果移植顺利后续可以围绕pieceTreeTextBuffer再移植diff/edit/cursor等其他部分。

## 移植方法宗旨
我们优先选择直接翻译TS原版而非自己重新实现一遍，适用于单元测试也适用于实现本身。仅当为了适配语言和运行时差异，不适宜“直译”TS原版的思路和关键设计时，才“custom reimplementation”。C#文件如果有TS原版对应，则应在文件头部注释说明。遇到冲突时，首先考虑与原版VS Code的TS代码对齐，包括单元测试、设计思路与关键实现。


**用途背景**是为工作在Agent系统中的LLM创建一种DocUI，类似TUI但是不是渲染到2D终端而是渲染为Markdown文本，UI元素被渲染为Markdown元素。渲染出的Markdown会被通过上下文工程注入LLM Context中。可以想象为"把LLM Context作为向LLM 展示信息的屏幕"。这需要高质量的Text建模、编辑、比较、查找、装饰功能。举例说明这里"装饰"的含义，例如我们要创建一个TextBox Widget来向LLM呈现可编辑文本，把原始文本和虚拟行号渲染为Markdown代码围栏，把光标/选区这些overlay元素渲染为插入文本中的Mark，并在代码围栏外用图例注解插入的光标/选区起点/终点Mark。像这些虚拟行号、光标/选区Mark，就是前面所说的"装饰"。后续有望用这条DocUI为LLM Agent 打造更加LLM Native & Friendly的编程IDE。

## 最新进展

### Phase 1-4 (2025-11-19): 核心移植完成
- PieceTree/TextModel/Cursor/Decorations/Diff 核心模块移植完成
- AI Team 协作架构（runSubAgent + agent-team/）建立
- 测试基线达到 56 passed

### Phase 5-6 (2025-11-20): 对齐审计 AA2/AA3
- 完成 CRLF/Search/Undo/Decorations 多轮修复
- Sprint 01（AA3）CL1~CL4 全部审计与修复交付
- 测试基线达到 88 passed

### Sprint 03 (2025-11-22~25): FindModel & PieceTree 深度对齐
- FindModel/FindDecorations/DocUIFindController 全套实现
- ReplacePattern/FindReplaceState TS parity 完成
- PieceTree Fuzz/Deterministic/Snapshot 测试覆盖（50+ deterministic scripts）
- 测试基线达到 365 passed

### Sprint 04 (2025-11-26~28): Cursor/Snippet/WordOps
- Range/Selection helpers（75 个方法）、IntervalTree lazy normalize
- CursorCollection、AtomicTabMoveOperations、WordOperations 完整实现
- MarkdownRenderer 集成 FindDecorations + 枚举值 TS 对齐
- BUILD-WARNINGS 清零
- 测试基线达到 807 passed

### Sprint 05 (2025-12-02): Diff API & 测试扩展 🎉
- Snippet P0-P2 全部完成（Final Tabstop, adjustWhitespace, Placeholder Grouping）
- Diff 核心 API: LineSequence 修复, DiffMove.Flip, RangeMapping.Inverse/Clip/FromEdit/ToTextEdit
- 大规模文档维护（Handoffs 归档 57 文件，认知文件压缩 54%）
- WS5 Gap 重新评估（47→26 gaps，完成率 55%）
- **测试基线达到 1008 passed（首次突破 1000！）**

### LLM-Native 功能筛选 (2025-12-04)
- 基于 `docs/plans/llm-native-editor-features.md` 重新评估剩余 gaps
- **无需移植**: 7 gaps (~14h 节省) — Sticky Column, 焦点管理, 视觉动画, Bracket colorization 等
- **降级实现**: 8 gaps (~18h → ~8h) — Snippet P3/Variables, 极端 Unicode, Diff 策略切换等
- **继续移植**: 11 gaps (~26h) — TextModelData.fromString, validatePosition, multi-owner decorations 等
- 预计总工时从 ~42h 降至 ~34h（节省 ~20%）

### Sprint 05 Batch 3 (2025-12-04): P1 清零 + P2 推进 🚀
- **P1 任务全部完成**:
  - TextModelData.fromString (+5 tests)
  - getValueLengthInRange + EOL variants (+5 tests)
  - Issue regressions 调研确认已覆盖
- **P2 任务进展**:
  - Diff deterministic matrix (+44 tests, 59→103)
  - PieceTree diagnostics (+23 tests)
- **测试基线达到 1085 passed**（本会话 +77）

### Sprint 05 Batch 4 (2025-12-05): Snippet Transform + MultiCursor 🎯
- **直译 TS 原版原则强化**: 优先直译而非重新实现
- **Snippet Transform 完成**:
  - `src/TextBuffer/Snippet/Transform.cs` 直译 snippetParser.ts
  - FormatString 支持 upcase/downcase/capitalize/pascalcase/camelcase
  - Transform 支持 regex 替换和条件分支
  - +32 测试全部通过
- **MultiCursor Snippet 集成**:
  - 多光标 snippet 插入测试 (+6 tests)
  - 基础功能验证通过
- **P2 完成率: 83% (5/6)** — 剩余 AddSelectionToNextFindMatch (~10h)
- **测试基线达到 1123 passed**（本会话 +38）

---
**状态更新提示：** 编辑本文件前请先核对 [`docs/reports/migration-log.md`](docs/reports/migration-log.md) 与 [`agent-team/indexes/README.md`](agent-team/indexes/README.md) 的最新 changefeed delta。
