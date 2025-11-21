# B1-PORTER Task Brief – ReplacePattern Implementation

## 你的角色
Porter-CS（C# 移植工程师）

## 记忆文件位置
- `agent-team/members/porter-cs.md`
- 汇报前**必须更新**记忆文件，记录本次任务成果与下一步

## 任务目标
实现 TS Test Alignment Plan 的 **Batch #1 – ReplacePattern**，交付以下文件：

### 1. Runtime 实现
- **`src/PieceTree.TextBuffer/DocUI/ReplacePattern.cs`**
  - 移植 `ts/src/vs/editor/contrib/find/browser/replacePattern.ts` 的 `parseReplaceString`、`ReplacePattern`、`ReplacePatternPiece`
  - 实现 `buildReplaceStringWithCasePreserved` 逻辑（保持大小写变换）
  - 参考 TS 源文件，确保 parser 状态机与 TS 一致

- **`src/PieceTree.TextBuffer/DocUI/DocUIReplaceController.cs`**
  - 创建 DocUI 替换控制器，封装 `ReplacePattern` + `TextModel` 交互
  - 提供 `Replace(pattern, searchResult)` 和 `ReplaceAll(pattern, searchResults)` 方法
  - 集成到 `TextModel` 的事件流（OnDidChangeContent 触发装饰更新）

### 2. 测试实现
- **`src/PieceTree.TextBuffer.Tests/DocUI/DocUIReplacePatternTests.cs`**
  - 移植 `ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts` 的核心用例
  - 至少包含以下场景（参考 TS 测试）：
    - 基础变量替换（`$1`, `$2`...）
    - Case-preserving 逻辑（`$&` 保持捕获组大小写）
    - 边界情况（空匹配、无捕获组、嵌套 `$`）
  - 使用 **xUnit Fact**，每个测试独立验证一个场景

### 3. 测试数据 & Harness
- **`resources/docui/replace-pattern/fixtures.json`**（或类似结构）
  - 提供测试用的 JSON fixtures：`{ "pattern": "...", "input": "...", "captures": [...], "expected": "..." }`
  - 从 TS 测试中提取典型案例

- **Markdown Snapshot 准备**
  - 在 `src/PieceTree.TextBuffer.Tests/__snapshots__/docui/replace-pattern/` 下准备目录
  - 测试中调用 `MarkdownRenderer` 生成 before/after 对比快照（可选，QA 阶段完善）

### 4. TODO 标记
在以下位置预置 TODO，供后续 Batch #2 处理：
- `// TODO(B2): Integrate with FindModel state for incremental replace`
- `// TODO(B2): Add WordSeparator context for $w placeholder (if TS supports)`

## 交付物清单
1. 新增文件：
   - `src/PieceTree.TextBuffer/DocUI/ReplacePattern.cs`
   - `src/PieceTree.TextBuffer/DocUI/DocUIReplaceController.cs`
   - `src/PieceTree.TextBuffer.Tests/DocUI/DocUIReplacePatternTests.cs`
   - `resources/docui/replace-pattern/fixtures.json`
2. 确保 `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` 全量通过
3. 记录以下信息供 QA：
   - 新增测试数量
   - `dotnet test` 命令与结果
   - Commit hash（如果你能看到，否则记录文件路径）
4. 更新 `agent-team/members/porter-cs.md` 记忆文件

## 参考资料
- TS 源文件：`ts/src/vs/editor/contrib/find/browser/replacePattern.ts`
- TS 测试：`ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts`
- 类型映射：`agent-team/type-mapping.md`
- 现有 DocUI 实现：`src/PieceTree.TextBuffer/DocUI/MarkdownRenderer.cs`、`Cursor.cs`

## 输出格式
汇报时提供：
1. **文件清单**：已创建/修改的文件路径
2. **测试结果**：`dotnet test` 输出（通过数/总数）
3. **下一步建议**：给 QA-Automation 的注意事项（例如需要补充的 fixture）
4. **已更新记忆文件**：确认更新了 `agent-team/members/porter-cs.md`

---
**执行前检查**：
- [ ] 读取 `agent-team/members/porter-cs.md` 获取上下文
- [ ] 参考 TS 源文件确保逻辑一致性
- [ ] 汇报前更新记忆文件

开始执行！
