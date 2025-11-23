# 源代码溯源注释任务指南

**任务 ID:** PT-007  
**创建日期:** 2025-11-22  
**负责角色:** Porter / AI 编码助手  
**预计工作量:** 0.5-1.0 call（批量处理）

---

## 1. 任务目标

为 `src/TextBuffer/` 目录下所有 C# 文件添加统一格式的文件头注释，明确标注其对应的 TypeScript 原版实现来源，方便后续维护、对比和审计。

---

## 2. 注释格式规范

### 2.1 标准格式（移植自 TS）

对于从 TypeScript 移植的代码，文件头应包含：

```csharp
// Source: <TS 文件相对路径>
// - Class/Type: <类型名或导出名>
// - Lines: <行号范围>（可选，如能定位）
// Ported: <移植日期 YYYY-MM-DD>
```

**示例：**

```csharp
// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Class: PieceTreeBase
// - Lines: 150-450
// Ported: 2025-11-19

namespace PieceTree.TextBuffer.Core;

public class PieceTreeModel
{
    // ...
}
```

**多源文件合并示例：**

```csharp
// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Class: PieceTreeBase (Lines: 150-450)
// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts
// - Class: TreeNode (Lines: 30-120)
// Ported: 2025-11-19

namespace PieceTree.TextBuffer.Core;

public partial class PieceTreeModel
{
    // ...
}
```

### 2.2 原创代码标注

对于 C# 特有的适配层、扩展方法或全新实现，使用以下格式：

```csharp
// Original C# implementation
// Purpose: <简要说明用途>
// Created: <创建日期 YYYY-MM-DD>
```

**示例：**

```csharp
// Original C# implementation
// Purpose: Extension methods for Range operations
// Created: 2025-11-20

namespace PieceTree.TextBuffer.Core;

public static class RangeExtensions
{
    // ...
}
```

### 2.3 部分移植 + 部分原创

当文件包含移植代码和原创代码时，组合使用两种格式：

```csharp
// Source: ts/src/vs/editor/common/core/range.ts
// - Interface: IRange (Lines: 10-50)
// Ported: 2025-11-18
//
// Original C# implementation
// Purpose: Additional .NET-specific range utilities
// Created: 2025-11-20

namespace PieceTree.TextBuffer.Core;

public readonly struct Range
{
    // Ported properties...
    
    // Original C# methods...
}
```

---

## 3. 执行指南

### 3.1 定位 TS 原版文件

1. **已知对应关系**：参考 `src/TextBuffer/README.md` 和 `docs/reports/migration-log.md` 查找已记录的移植关系。

2. **命名推断**：C# 文件名通常与 TS 文件名对应：
   - `PieceTreeModel.cs` → `pieceTreeBase.ts` 或 `pieceTreeTextBuffer.ts`
   - `Range.cs` → `range.ts`
   - `TextModel.cs` → `textModel.ts`

3. **搜索验证**：使用以下命令在 TS 代码库中搜索类型/函数名：
   ```bash
   grep -r "class PieceTreeBase" ts/src/vs/editor/common/model/
   grep -r "export class TextModel" ts/src/vs/editor/common/
   ```

4. **常见路径映射**：
   | C# 命名空间 | TS 路径前缀 |
   |------------|------------|
   | `PieceTree.TextBuffer.Core` | `ts/src/vs/editor/common/model/pieceTreeTextBuffer/` |
   | `PieceTree.TextBuffer.Cursor` | `ts/src/vs/editor/common/cursor/` |
   | `PieceTree.TextBuffer.Decorations` | `ts/src/vs/editor/common/model/` 或 `viewModel/` |
   | `PieceTree.TextBuffer` (根) | `ts/src/vs/editor/common/model/` |

### 3.2 查找行号范围

**推荐方法（可选，但推荐）：**

1. 打开 TS 源文件
2. 搜索类/接口/函数定义（如 `export class PieceTreeBase`）
3. 找到定义的起始行和类结束的大括号行
4. 记录为 `Lines: <start>-<end>`

**快速方法（不精确但可接受）：**

```bash
grep -n "export class PieceTreeBase" ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
```

如果行号难以准确定位（如多个相关类型），可省略 `Lines` 字段，仅保留文件路径和类型名。

### 3.3 处理特殊情况

#### 情况 1：一个 C# 文件合并多个 TS 文件

列出所有来源，每个来源占一行：

```csharp
// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Class: PieceTreeBase (Lines: 150-450)
// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts
// - Class: TreeNode (Lines: 30-120)
// Ported: 2025-11-19
```

#### 情况 2：一个 TS 文件拆分到多个 C# 文件

每个 C# 文件标注相同的源文件，但指定不同的类型/行号：

**PieceTreeNode.cs:**
```csharp
// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts
// - Class: TreeNode
// - Lines: 30-120
// Ported: 2025-11-19
```

**RBTreeHelpers.cs:**
```csharp
// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts
// - Functions: fixInsert, rbDelete, updateTreeMetadata
// - Lines: 200-450
// Ported: 2025-11-19
```

#### 情况 3：部分移植（混合原创）

明确区分移植部分和原创部分，使用两段注释：

```csharp
// Source: ts/src/vs/editor/common/core/range.ts
// - Interface: IRange (Lines: 10-50)
// Ported: 2025-11-18
//
// Original C# implementation
// Purpose: .NET-specific extension methods and IEquatable support
// Created: 2025-11-20
```

#### 情况 4：纯原创 C# 代码

使用原创格式，说明用途：

```csharp
// Original C# implementation
// Purpose: Dependency injection services for TextBuffer
// Created: 2025-11-21
```

---

## 4. 执行流程

### 步骤 1：盘点待处理文件

```bash
find src/TextBuffer -name "*.cs" -type f | grep -v "obj/" | grep -v "bin/"
```

### 步骤 2：批量处理

对于每个 C# 文件：

1. **判断来源类型**：
   - 查看文件内容和类型名
   - 参考 `docs/reports/migration-log.md` 或 `README.md`
   - 搜索 TS 代码库确认对应关系

2. **定位 TS 源**（如适用）：
   - 找到 TS 文件路径（相对于 `ts/src/`）
   - 找到对应的类/接口/函数名
   - （可选）查找行号范围

3. **添加注释**：
   - 在文件顶部、namespace 声明之前（在 using 之后）添加溯源注释
   - 使用上述格式规范
   - 保持一致的缩进和换行

### 步骤 3：验证

- 确保所有 C# 文件都有溯源注释
- 检查格式一致性（缩进、换行、字段顺序）
- 验证 TS 文件路径确实存在（抽查）

### 步骤 4：更新文档

在 `docs/reports/migration-log.md` 中记录：

```markdown
| Date | Task | File | TS Source | Status |
|------|------|------|-----------|--------|
| 2025-11-22 | PT-007 | All C# files | Source attribution headers added | ✅ Complete |
```

---

## 5. 验收标准

### 必须达成：

- ✅ 所有 `src/TextBuffer/**/*.cs` 文件（除自动生成文件）都有文件头注释
- ✅ 移植代码标注了 TS 源文件路径和类型名
- ✅ 原创代码标注了 "Original C# implementation" 和用途
- ✅ 格式统一，遵循本文档规范

### 推荐但非必需：

- 🎯 90%+ 的移植代码包含行号范围
- 🎯 所有注释经过抽查验证（TS 文件路径存在且类型名匹配）

### 排除范围：

- 自动生成的文件（如 `obj/`、`bin/`、`*.Designer.cs`）
- 项目文件（`.csproj`）
- 纯配置或属性文件（如 `AssemblyInfo.cs`）

---

## 6. 示例清单

### 6.1 需要处理的文件类型

- ✅ `Core/*.cs` - 核心数据结构（PieceTreeModel, PieceTreeNode, Range, etc.）
- ✅ `Cursor/*.cs` - 光标相关逻辑
- ✅ `Decorations/*.cs` - 装饰器系统
- ✅ `Services/*.cs` - 服务层
- ✅ `*.cs` (根目录) - TextModel, TextBuffer 等公共 API

### 6.2 无需处理的文件

- ❌ `obj/`, `bin/` - 构建产物
- ❌ `*.csproj` - 项目文件
- ❌ `Properties/AssemblyInfo.cs` - 程序集元数据（如有）

---

## 7. 工具和自动化提示

### 快速查找 TS 对应文件

```bash
# 在 TS 代码库中搜索类型名
function find_ts_source() {
    local typename=$1
    grep -r "export class $typename\|export interface $typename\|class $typename" ts/src/vs/editor/
}

# 示例
find_ts_source "PieceTreeBase"
```

### 批量添加注释脚本模板

如果文件数量较多，可考虑编写脚本辅助：

1. 读取 C# 文件列表
2. 对于每个文件，提取类型名
3. 在 TS 代码库中搜索匹配
4. 生成注释模板
5. 人工审核后应用

（具体脚本实现可按需创建）

---

## 8. 参考资料

- **TS 源代码路径:** `ts/src/vs/editor/`
- **C# 实现路径:** `src/TextBuffer/`
- **移植日志:** `docs/reports/migration-log.md`
- **项目 README:** `src/TextBuffer/README.md`
- **类型映射参考:** `agent-team/type-mapping.md`

---

## 9. 常见问题

**Q: 如果找不到对应的 TS 文件怎么办？**  
A: 标记为原创实现，使用 "Original C# implementation" 格式。如果不确定，可在注释中添加 `// TODO: Verify TS source`。

**Q: TS 文件路径应该用相对路径还是绝对路径？**  
A: 使用相对于仓库根目录的路径，如 `ts/src/vs/editor/common/model/...`，保持一致。

**Q: 行号范围必须精确吗？**  
A: 不必须。大致范围即可，重点是能快速定位到相关代码。如果难以确定，可省略行号。

**Q: 已有部分文件有注释，格式不统一怎么办？**  
A: 统一替换为新格式，确保整个项目一致。

**Q: 注释应该放在文件的哪个位置？**  
A: 放在 `using` 语句之后、`namespace` 声明之前，或者作为文件的第一行（在版权声明之后，如有）。

---

## 10. 完成后检查清单

- [ ] 所有目标 C# 文件都添加了溯源注释
- [ ] 注释格式统一，符合规范
- [ ] 至少抽查 10 个文件，验证 TS 路径和类型名正确
- [ ] 更新 `docs/reports/migration-log.md`
- [ ] 更新 `agent-team/task-board.md` 任务状态为 Done
- [ ] 提交代码并创建 PR（如适用）

---

**任务负责人:** Porter / AI 编码员  
**审核人:** QA / 技术负责人  
**预计完成时间:** 1-2 小时（批量处理）

