# Snippet Downgrade Investigation Brief

> **Created**: 2025-12-02  
> **Author**: Investigator-TS  
> **Request**: Team Leader — 分析 TS Snippet 原版功能，为"降级实现"策略提供技术边界

## 1. 目标

根据 `docs/plans/llm-native-editor-features.md` 的"降级实现"策略：
- ✅ 保留基础占位符导航
- ❌ 不做实时多光标同步输入
- ❌ 不做 Sticky Column

本 Brief 分析 TS 原版 Snippet 功能，确定哪些对 LLM 用户有价值、哪些可以跳过。

## 2. TS 源码位置

```
ts/src/vs/editor/contrib/snippet/browser/
├── snippetParser.ts       — AST 解析器 (Scanner, Marker, TextmateSnippet)
├── snippetSession.ts      — 会话管理 (OneSnippet, SnippetSession)
├── snippetVariables.ts    — 变量解析器 (Resolver 集合)
├── snippetController2.ts  — 控制器 (Tab 导航, Choice 补全)
└── snippetSession.css     — 占位符样式
```

## 3. TS 原版功能清单

### 3.1 snippetParser.ts — 解析器 AST

| 功能 | 行号 | 描述 | 降级建议 |
|------|------|------|----------|
| **TokenType 枚举** | L8-23 | 16 种 token 类型 | Keep（解析基础） |
| **Scanner 类** | L31-111 | 词法分析器 | Keep |
| **Marker 抽象类** | L113-176 | AST 节点基类 | Keep |
| **Text 类** | L178-207 | 纯文本节点 | Keep |
| **Placeholder 类** | L211-257 | `${n:text}` 占位符 | Keep |
| **Choice 类** | L259-288 | `${1|opt1,opt2|}` 选项 | **Skip** |
| **Transform 类** | L290-353 | `${1/regex/format/flags}` 变换 | **Skip** |
| **FormatString 类** | L355-420 | Transform 格式（upcase/downcase/camelcase 等） | **Skip** |
| **Variable 类** | L422-461 | `${TM_FILENAME}` 变量 | **Simplify** |
| **TextmateSnippet 类** | L467-545 | 完整 snippet AST | Keep |
| **SnippetParser 类** | L547-823 | 递归下降解析器 | **Simplify** |

### 3.2 snippetSession.ts — 会话管理

| 功能 | 行号 | 描述 | 降级建议 |
|------|------|------|----------|
| **OneSnippet 类** | L30-250 | 单个 snippet 实例 | **Simplify** |
| **TrackedRangeStickiness** | L41-47 | 装饰器粘性配置 | Keep |
| **_initDecorations** | L62-90 | 占位符装饰初始化 | Keep |
| **move(fwd)** | L92-162 | Tab/Shift-Tab 导航 | Keep |
| **Transform 应用** | L99-115 | 在跳转时应用正则变换 | **Skip** |
| **enclosingPlaceholders** | L180 | 嵌套占位符 | **Skip** |
| **computePossibleSelections** | L200-230 | 同 index 占位符同步选区 | **Skip** |
| **activeChoice** | L232-248 | Choice 弹出菜单状态 | **Skip** |
| **merge** | L255-320 | 嵌套 snippet 合并 | **Skip** |
| **SnippetSession.adjustWhitespace** | L326-380 | 缩进对齐 | Keep |
| **SnippetSession.adjustSelection** | L382-400 | overwrite 范围 | Keep |
| **createEditsAndSnippetsFromSelections** | L402-490 | 多光标 snippet 批量创建 | **Simplify** |
| **isSelectionWithinPlaceholders** | L570-630 | 选区边界检测（用于取消 snippet 模式） | Keep |

### 3.3 snippetVariables.ts — 变量解析

| 变量类别 | 行号 | 变量名 | 降级建议 |
|---------|------|--------|----------|
| **Selection** | L50-110 | `SELECTION`, `TM_SELECTED_TEXT`, `TM_CURRENT_LINE`, `TM_CURRENT_WORD`, `TM_LINE_INDEX`, `TM_LINE_NUMBER`, `CURSOR_INDEX`, `CURSOR_NUMBER` | **Simplify**（只保留 SELECTION） |
| **Model** | L112-160 | `TM_FILENAME`, `TM_FILENAME_BASE`, `TM_DIRECTORY`, `TM_FILEPATH`, `RELATIVE_FILEPATH` | **Simplify**（只保留 TM_FILENAME） |
| **Clipboard** | L170-195 | `CLIPBOARD` | **Skip** |
| **Comment** | L197-220 | `LINE_COMMENT`, `BLOCK_COMMENT_START`, `BLOCK_COMMENT_END` | **Skip** |
| **Time** | L222-280 | `CURRENT_YEAR`, `CURRENT_MONTH`, `CURRENT_DATE`, `CURRENT_HOUR`, `CURRENT_MINUTE`, `CURRENT_SECOND` 等 | **Skip** |
| **Workspace** | L282-330 | `WORKSPACE_NAME`, `WORKSPACE_FOLDER` | **Skip** |
| **Random** | L332-350 | `RANDOM`, `RANDOM_HEX`, `UUID` | **Skip** |

### 3.4 snippetController2.ts — 控制器

| 功能 | 行号 | 描述 | 降级建议 |
|------|------|------|----------|
| **InSnippetMode/HasNextTabstop/HasPrevTabstop** | L47-49 | Context Keys | **Skip**（LLM 不用键盘） |
| **insert/apply** | L80-130 | 核心插入逻辑 | Keep |
| **_updateState** | L175-210 | 状态管理 & 自动取消 | **Simplify** |
| **_handleChoice** | L212-240 | Choice 补全触发 | **Skip** |
| **prev/next** | L255-265 | Tab 导航 | Keep |
| **Keybinding 注册** | L280-335 | Tab/Shift-Tab/Escape 绑定 | **Skip** |

## 4. 现有 C# 实现分析

`src/TextBuffer/Cursor/SnippetSession.cs` 当前实现：
- ✅ 基础 `${n:text}` 解析（正则匹配）
- ✅ 占位符装饰（TrackedRange）
- ✅ NextPlaceholder / PrevPlaceholder 导航
- ❌ 未实现：Choice, Transform, Variable

**当前实现行数**：~145 行（精简版）

## 5. 降级实现建议

### 5.1 Keep（保留）

| 功能 | 理由 | Porter 优先级 |
|------|------|--------------|
| 基础占位符 `${n:text}` | 核心用户价值 | P0 ✅ 已完成 |
| Tab/Shift-Tab 导航 | 核心用户价值 | P0 ✅ 已完成 |
| 占位符装饰 | 导航需要 TrackedRange | P0 ✅ 已完成 |
| 缩进对齐 `adjustWhitespace` | 多行 snippet 必需 | P1 |
| Final Tabstop `$0` | snippet 退出点 | P1 |

### 5.2 Simplify（简化实现）

| 功能 | 简化策略 | Porter 优先级 |
|------|---------|--------------|
| **Variable 解析** | 仅支持 `TM_FILENAME`、`SELECTION`，其他 fallback 为空字符串 | P2 |
| **嵌套占位符** | 仅支持单层（C# 现有实现已够用） | P2 |
| **多光标 snippet** | 简化为逐个执行，不做同步输入 | P3 |

### 5.3 Skip（不移植）

| 功能 | 跳过理由 |
|------|---------|
| **Choice `${1|a,b,c|}`** | LLM 直接指定值，不需要人类选择 UI |
| **Transform `${1/regex/fmt/}`** | 复杂度高，LLM 可在生成时处理 |
| **FormatString (upcase/camelcase)** | LLM 可直接生成正确 case |
| **Time 变量** | LLM 可自行计算时间 |
| **Clipboard 变量** | LLM 无剪贴板概念 |
| **Workspace 变量** | 可通过 context 注入 |
| **Context Keys & Keybindings** | LLM 不用键盘 |
| **Choice 补全 UI** | 人类交互专属 |
| **实时多光标同步输入** | LLM 批量操作，无需同步 |

## 6. 技术边界总结

```
┌─────────────────────────────────────────────────────────────┐
│                    LLM Snippet Boundary                     │
├─────────────────────────────────────────────────────────────┤
│  ✅ IN SCOPE                  │  ❌ OUT OF SCOPE            │
│  ─────────────────────────────│──────────────────────────── │
│  ${n:text} 占位符解析         │  ${1|a,b,c|} Choice        │
│  $0 Final Tabstop             │  ${1/regex/fmt/} Transform │
│  NextPlaceholder/PrevPlaceholder │  FormatString 变换       │
│  TrackedRange 装饰            │  Time/Clipboard/Workspace  │
│  adjustWhitespace 缩进        │  实时多光标同步输入        │
│  TM_FILENAME, SELECTION       │  Context Keys & Keybindings│
└─────────────────────────────────────────────────────────────┘
```

## 7. Porter-CS 实现建议

### 7.1 短期（P0-P1）

1. **补充 Final Tabstop 支持**：在 `InsertSnippet` 中识别 `$0` 并作为最后一个导航点
2. **实现 adjustWhitespace**：参考 TS `SnippetSession.adjustWhitespace` (L326-380)
3. **支持嵌套占位符默认值**：`${1:${2:nested}}` → 解析嵌套结构

### 7.2 中期（P2）

1. **Variable 解析框架**：创建 `ISnippetVariableResolver` 接口
2. **实现 TM_FILENAME/SELECTION**：最常用的两个变量
3. **Unknown Variable Fallback**：未知变量返回空字符串，不报错

### 7.3 可选（P3）

1. **多光标 snippet**：简化为循环执行，无同步输入
2. **Snippet 嵌套合并**：在活动占位符处插入新 snippet

## 8. QA-Automation 测试建议

### 8.1 基础覆盖

```csharp
[Theory]
[InlineData("${1:hello} ${2:world}", "hello world", 2)]   // 基础占位符
[InlineData("$0", "", 1)]                                  // Final tabstop
[InlineData("${1:first}${1:second}", "first", 2)]         // 同 index 占位符
[InlineData("func(${1:arg1}, ${2:arg2})", "func(arg1, arg2)", 2)]
public void SnippetParsing_PlaceholderCount(string snippet, string expected, int phCount) { ... }
```

### 8.2 降级验证

```csharp
[Fact]
public void SnippetParsing_Choice_FallbacksToFirstOption()
{
    // ${1|one,two,three|} → 解析为 ${1:one}（降级）
}

[Fact]
public void SnippetParsing_Transform_PassThrough()
{
    // ${1/regex/fmt/} → 解析为普通占位符（跳过变换）
}

[Fact]
public void SnippetParsing_UnknownVariable_Empty()
{
    // ${TM_UNKNOWN} → 解析为空字符串
}
```

## 9. 相关锚点

- `#delta-2025-11-26-aa4-cl7-cursor-core` — Cursor/Snippet backlog
- `docs/plans/llm-native-editor-features.md` — LLM 功能规划
- `agent-team/handoffs/INV-CursorSnippet-Blueprint.md` — Cursor+Snippet 整体蓝图

## 10. 开放问题

1. **Variable 扩展性**：是否需要 `ISnippetVariableResolver` 接口供外部注入？
2. **Snippet 格式兼容性**：是否需要解析但忽略 Choice/Transform 以避免解析失败？
3. **多行 snippet 缩进**：`adjustWhitespace` 实现复杂度如何？

---

*Generated by Investigator-TS, 2025-12-02*
