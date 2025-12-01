# LLM-Native TextEditor 功能规划

> 本文档分析 LLM 作为 TextEditor 用户的独特需求，指导 PieceTree 移植的功能取舍。
> 核心原则：**LLM is a Computer System User, LLM Context is yet another interface**

## 背景

人类使用 TextEditor 的方式：
- 视觉驱动（语法高亮、缩进可视化）
- 手眼协调（鼠标拖拽、键盘快捷键）
- 实时反馈（每个按键即时可见）
- 容错靠 Undo（试错成本低）

LLM 使用 TextEditor 的方式：
- 语义驱动（理解代码结构，不靠"看到"）
- 声明式操作（描述意图，而非过程）
- 批量思维（一次描述完整变更）
- 容错靠预览（执行前确认，避免意外）

---

## 功能分类

### ✅ 保留（核心能力）

| 功能 | 模块 | 理由 | 移植优先级 |
|------|------|------|-----------|
| PieceTree 数据结构 | `PieceTreeModel` | 高效文本存储和编辑的基础 | P0 ✅ 已完成 |
| Position/Range/Selection | `Core/*` | 精确定位能力 | P0 ✅ 已完成 |
| Search（含正则） | `PieceTreeSearcher` | 查找定位 | P0 ✅ 已完成 |
| Diff 算法 | `DiffComputer` | 变更对比，验证编辑结果 | P0 ✅ 已完成 |
| Undo/Redo 栈 | `EditStack` | 回滚能力 | P0 ✅ 已完成 |
| 装饰器系统 | `Decorations/*` | DocUI 渲染 Overlay 的基础 | P0 ✅ 已完成 |
| 行号基础设施 | `PieceTreeModel` | 行号+锚点定位的基础 | P0 ✅ 已完成 |

### 🔄 简化（保留核心，降级实现）

| 功能 | TS 原版 | 简化方向 | 理由 | 实施状态 |
|------|--------|----------|------|----------|
| Cursor 导航 | word/line/paragraph 级移动 | 只保留 goto position/line | LLM 不需要细粒度导航 | ✅ CursorState/Context 已对齐 |
| 多光标 | 实时同步输入、复杂合并逻辑 | 简化为"多选区批量操作" | 不需要实时同步 | ⏳ Snippet 计划中（已标记降级） |
| Sticky Column | 跨行移动保持列位置 | ❌ 不移植 | 人类键盘导航专属 | 🚫 明确不做 |
| Word Boundary | Unicode 分词、CamelCase | 基础实现即可 | 搜索场景仍需要 | ✅ WordOperations 已完成核心 |
| Bracket Matching | 括号配对查找 | 可选实现 | 语义层面有用，视觉层面不需要 | ⏸️ 暂缓，按需添加 |

### ❌ 砍掉（不移植）

| 功能 | 理由 | 验证状态 |
|------|------|----------|
| 键盘快捷键处理 | LLM 不用键盘 | ✅ 未在 TODO |
| 鼠标事件处理 | LLM 不用鼠标 | ✅ 未在 TODO |
| IME 输入法支持 | 不需要 | ✅ 未在 TODO |
| 自动补全 UI | LLM 自己就是补全引擎 | ✅ 未在 TODO |
| 语法高亮 | LLM 理解语义不靠颜色 | ✅ 未在 TODO |
| Minimap/Overview | 不需要视觉概览 | ✅ 未在 TODO |
| 缩进指南线 | 视觉辅助 | ✅ 未在 TODO |
| Smooth scrolling | 没有滚动概念 | ✅ 未在 TODO |
| 代码折叠 UI | 视觉交互，LoD 机制可替代 | ✅ 未在 TODO |
| Hover 提示 | 鼠标交互 | ✅ 未在 TODO |
| 上下文菜单 | 鼠标交互 | ✅ 未在 TODO |
| Sticky Column | 人类键盘导航专属 | ✅ 从简化列表升级为不移植 |

### 🆕 新增（LLM-Native 功能）

| 功能 | 描述 | 优先级 |
|------|------|--------|
| **行号 + 锚点定位** | `(line: 42, anchor: "function foo")` 替代复述全文 | P0 |
| **多匹配交互流程** | 匹配多个时返回带编号预览，LLM 选择后执行 | P0 |
| **编辑预览 + 确认** | 执行前展示 diff，确认后才真正执行 | P1 |
| **LiveWindow 集成** | 只注入当前聚焦区域 + 周边上下文 | P1 |
| **LoD 渲染** | 远离编辑点的代码折叠为摘要/签名 | P1 |
| **Overlay 图例系统** | Markdown 符号表示光标、选区、变更等 | P0 |
| **变更原子性** | 多个相关编辑打包为事务 | P2 |
| **编辑历史语义化** | 可查询的编辑日志，不只是 Undo 栈 | P2 |
| **语义锚点**（需 Roslyn） | `Select("function:calculateTotal.returnType")` | P3 |

---

## 交互模式设想

### 当前模式（str_replace）
```
LLM 构造完整 oldString（含上下文）→ 工具执行 → 成功/失败
```
问题：冗余复述、精确匹配焦虑、无预览、多匹配无力

### 目标模式（行号 + 锚点 + 确认）
```
1. LLM: edit(file, line=42, anchor="oldText", newText="newText")
2. Editor: 
   - 唯一匹配 → 直接执行，返回 diff
   - 多匹配 → 返回带编号预览
   - 无匹配 → 返回相近建议
3. LLM（多匹配时）: confirm(match_id=1)
4. Editor: 执行，返回结果
```

### DocUI 渲染示例
```markdown
## 编辑预览 [line 42-45]

​```diff
- function calculateTotal(): number {
+ function calculateTotal(): bigint {
​```

**匹配情况**: 1 个精确匹配
**操作**: `confirm` 执行 / `cancel` 取消
```

---

## 与 LiveContextProto 的集成点

| LiveContextProto 机制 | TextEditor 集成 |
|----------------------|-----------------|
| Memory Notebook | 编辑历史可作为可查询记忆 |
| Recap | 编辑摘要参与上下文压缩 |
| Level of Detail | 代码按距离编辑点折叠 |
| LiveWindow | 当前文件状态动态注入 |

---

## 待讨论

1. **行号 + 锚点**的具体 API 设计
2. **多匹配预览**的 Markdown 渲染格式
3. **LoD 折叠**的粒度（函数级？类级？）
4. **Roslyn 集成**的边界（哪些语义能力值得引入？）

---

*创建于 2025-11-28，源自 AI Team Leader 与人类伙伴的探讨*
