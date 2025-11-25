# B2-INV Task Brief – WordSeparator & DocUI Widget Test Investigation

## 你的角色
Investigator-TS（TypeScript 调查员）

## 记忆文件位置
- `agent-team/members/investigator-ts.md`
- 汇报前**必须更新**记忆文件，记录本次调查成果与下一步

## 任务目标
为 **Batch #2 – FindModel/FindController** 准备必要的技术规格，重点调研：

### 1. WordSeparator 规格补全
**目标**：明确 C# 需要实现的 WordSeparator/SearchContext 语义。

**调研范围**：
- `ts/src/vs/editor/common/core/wordHelper.ts`
- `ts/src/vs/editor/common/core/wordCharacterClassifier.ts`
- `ts/src/vs/editor/common/model/textModelSearch.ts`
- `ts/src/vs/editor/contrib/find/browser/findModel.ts`（如何消费 WordSeparator）

**输出要求**：
在 `docs/plans/ts-test-alignment.md` 的 Appendix 部分新增或更新章节：
```markdown
### WordSeparator & SearchContext Specification

#### TS 实现概览
- `WordCharacterClassifier`: [简要说明：字符分类器、缓存机制]
- `getWordAtText()`: [API 签名与行为]
- `WordSeparators`: [格式、默认值、语言相关配置]
- FindModel 集成点: [如何在 search 中使用]

#### C# 移植需求
- [ ] 实现 `WordCharacterClassifier`（或等效类）
- [ ] 提供 `WordSeparators` 配置存储
- [ ] 在 `PieceTreeSearcher` / `TextModelSearch` 中集成
- [ ] 测试覆盖：word boundary detection

#### 已知风险
- Intl.Segmenter parity（如有）
- Unicode word break 规则差异
- 性能影响（缓存策略）
```

### 2. DocUI Widget 测试路径定位
**目标**：找到 TS 端 FindWidget 的测试文件，为 Batch #2 测试迁移做准备。

**调研范围**：
- `ts/src/vs/editor/contrib/find/test/browser/` 目录
- 搜索 `findWidget.test.ts` 或类似文件
- 如果没有 widget 测试，记录这一事实并建议 C# 端策略

**输出要求**：
在 `docs/plans/ts-test-alignment.md` Appendix 表格中更新或新增：

| TS Test File | Module Scope | Notes / Dependencies | Portability Tier | Target C# Suite | Status |
| --- | --- | --- | --- | --- | --- |
| `ts/src/vs/editor/contrib/find/test/browser/findWidget.test.ts` (if exists) | FindWidget DOM layout, history, accessibility | [依赖项：DOM/Sash/ContextView] | C | _TODO: DocUI harness_ | Research in progress |

如果找不到 widget 测试，添加 NOTE：
```markdown
**NOTE**: TS 端未发现 FindWidget DOM 测试。建议 C# 端：
1. 使用 Markdown snapshot 测试替代 DOM 测试
2. 重点覆盖 FindModel 逻辑层（已有 findModel.test.ts）
3. 创建最小化的 DocUI harness 用于 controller 测试
```

### 3. Batch #2 依赖清单
**目标**：列出 FindModel/FindController 移植的阻塞项。

**输出要求**：
在汇报文档中创建"Batch #2 Dependencies"章节：
```markdown
## Batch #2 Dependencies

### 阻塞项（必须先实现）
1. WordSeparator/SearchContext 集成（如 FindModel 依赖）
2. [其他阻塞项]

### 可选项（可后续优化）
1. FindWidget DOM harness（如无 TS 测试）
2. [其他可选项]

### 推荐顺序
1. 先实现 WordSeparator 基础设施
2. 再移植 FindModel（逻辑层）
3. 最后处理 FindController（命令层，依赖较多）
```

## 前置条件
- 已有调研基础：`docs/plans/ts-test-alignment.md` Appendix 已包含初步分析
- TS 源码路径：`ts/src/vs/editor/contrib/find/` 与相关 core 模块

## 交付物清单
1. **更新文件**:
   - `docs/plans/ts-test-alignment.md` (Appendix 新增 WordSeparator 规格 + FindWidget 测试定位)
2. **汇报文档**: `agent-team/handoffs/B2-INV-Result.md`，包含：
   - WordSeparator 规格摘要
   - FindWidget 测试路径（或不存在的结论）
   - Batch #2 依赖清单与推荐顺序
3. **记忆文件更新**: `agent-team/members/investigator-ts.md`

## 输出格式
汇报时提供：
1. **WordSeparator 规格摘要**: 关键 API、行为、C# 移植需求
2. **FindWidget 测试定位**: 文件路径或"不存在"结论
3. **Batch #2 依赖清单**: 阻塞项 + 推荐顺序
4. **下一步建议**: 给 Planner 和 Porter-CS 的实施建议
5. **已更新记忆文件**: 确认更新了 `agent-team/members/investigator-ts.md`

---
**执行前检查**：
- [ ] 读取 `agent-team/members/investigator-ts.md` 获取上下文
- [ ] 读取 `docs/plans/ts-test-alignment.md` Appendix 当前状态
- [ ] 汇报前更新记忆文件

开始执行！
