````markdown
# Half-Context Summarization 配置选项方案

> **Status:** Draft
> **Date:** 2025-12-04
> **Related:** 
> - [`docs/plans/half-context-pr-plan.md`](half-context-pr-plan.md)
> - [`agent-team/private/self-enhancement-plan.md`](../../agent-team/private/self-enhancement-plan.md)

## 背景

当前代码中有一个硬编码的常量：

```typescript
// summarizedConversationHistory.tsx:687
const ENABLE_HALF_CONTEXT_SUMMARIZATION = true; // TODO: Make this a configuration option
```

需要将其转为用户可配置的选项。

## 现有配置机制分析

### 1. 配置项类型

根据 `configurationService.ts` 的分析，Copilot Chat 有以下配置层次：

| 类型 | 特点 | 使用场景 |
|------|------|----------|
| `ConfigKey.xxx` | 公开配置，用户可见可改 | 用户偏好 |
| `ConfigKey.Advanced.xxx` | 高级配置，需用户主动探索 | 实验性功能 |
| `ConfigKey.TeamInternal.xxx` | 仅团队成员可配置，外部用户忽略 | 内部调试 |

### 2. 相关现有配置项

```typescript
// 已存在的 agent history summarization 相关配置
ConfigKey.SummarizeAgentConversationHistory  // 开启/关闭整个压缩功能
ConfigKey.Advanced.SummarizeAgentConversationHistoryThreshold  // 触发阈值
ConfigKey.Advanced.AgentHistorySummarizationMode  // 模式 (simple/full)
ConfigKey.Advanced.AgentHistorySummarizationWithPromptCache  // 使用 prompt cache
ConfigKey.Advanced.AgentHistorySummarizationForceGpt41  // 强制用 GPT-4.1
```

### 3. 配置项声明流程

1. **TypeScript 定义** (`configurationService.ts`):
   ```typescript
   export const HalfContextSummarization = defineAndMigrateExpSetting<boolean>(
     'chat.advanced.halfContextSummarization',
     'chat.halfContextSummarization', 
     false  // 默认关闭
   );
   ```

2. **package.json 声明** (公开配置需要):
   ```json
   "github.copilot.chat.halfContextSummarization": {
     "type": "boolean",
     "default": false,
     "tags": ["advanced", "experimental"],
     "markdownDescription": "%github.copilot.config.halfContextSummarization%"
   }
   ```

3. **package.nls.json 描述**:
   ```json
   "github.copilot.config.halfContextSummarization": "Enable half-context summarization for agent conversations."
   ```

---

## 候选方案

### 方案 A: Advanced 配置（推荐用于 PR）

**配置层级:** `ConfigKey.Advanced`

**定义:**
```typescript
// configurationService.ts
export const HalfContextSummarization = defineAndMigrateExpSetting<boolean>(
  'chat.advanced.halfContextSummarization',
  'chat.halfContextSummarization',
  false  // 默认关闭，保守起见
);
```

**使用:**
```typescript
// summarizedConversationHistory.tsx
private getEnableHalfContextSummarization(): boolean {
  return this.configurationService.getExperimentBasedConfig(
    ConfigKey.Advanced.HalfContextSummarization,
    this.experimentationService
  );
}
```

**优点:**
- 符合现有的 AgentHistorySummarization 相关配置的命名惯例
- 可以通过实验服务进行 A/B 测试
- 默认关闭 = 无回归风险

**缺点:**
- 需要在 package.json 中添加声明

---

### 方案 B: TeamInternal 配置（用于自用分支）

**配置层级:** `ConfigKey.TeamInternal`

**定义:**
```typescript
// configurationService.ts
export const HalfContextSummarization = defineTeamInternalSetting<boolean>(
  'chat.advanced.halfContextSummarization',
  ConfigType.Simple,
  { defaultValue: false, teamDefaultValue: true }  // 团队成员默认开启
);
```

**优点:**
- 不需要在 package.json 中添加
- 团队成员自动获得功能
- 外部用户完全透明（配置被忽略）

**缺点:**
- 无法让外部用户使用
- 不符合 PR 的需求

---

### 方案 C: 复用现有配置 AgentHistorySummarizationMode

**思路:** 不新增配置，扩展现有的 `agentHistorySummarizationMode` 的可选值。

**当前:**
```typescript
enum SummaryMode {
  Simple = 'simple',
  Full = 'full'
}
```

**扩展为:**
```typescript
enum SummaryMode {
  Simple = 'simple',
  Full = 'full',
  HalfFull = 'half-full',    // 半上下文 + Full 模式
  HalfSimple = 'half-simple' // 半上下文 + Simple 模式
}
```

**优点:**
- 无需新增配置项
- package.json 无需修改
- 概念上更统一（mode 控制压缩策略）

**缺点:**
- 语义混淆：`half-*` 描述的是切分策略，而 `simple/full` 描述的是摘要详细程度
- 改动范围较大（需要修改 `getSummaryWithFallback` 逻辑）

---

## 推荐方案

### 对于 PR 版本：方案 A

```diff
// configurationService.ts (添加在 AgentHistorySummarizationForceGpt41 附近)
+ export const HalfContextSummarization = defineAndMigrateExpSetting<boolean>(
+   'chat.advanced.halfContextSummarization',
+   'chat.halfContextSummarization',
+   false
+ );
```

```diff
// package.json (添加在 agentHistorySummarizationForceGpt41 后面)
+ "github.copilot.chat.halfContextSummarization": {
+   "type": "boolean",
+   "default": false,
+   "markdownDescription": "%github.copilot.config.halfContextSummarization%",
+   "tags": [
+     "advanced",
+     "experimental",
+     "onExp"
+   ]
+ },
```

```diff
// package.nls.json
+ "github.copilot.config.halfContextSummarization": "Enable half-context summarization for agent conversations. When enabled, only half of the unsummarized conversation history is compressed at a time, preserving more recent context."
```

```diff
// summarizedConversationHistory.tsx
- const ENABLE_HALF_CONTEXT_SUMMARIZATION = true;
+ // 移除硬编码常量

  export class SummarizedConversationHistoryPropsBuilder {
    constructor(
      @IPromptPathRepresentationService private readonly _promptPathRepresentationService: IPromptPathRepresentationService,
      @IWorkspaceService private readonly _workspaceService: IWorkspaceService,
+     @IConfigurationService private readonly _configurationService: IConfigurationService,
+     @IExperimentationService private readonly _experimentationService: IExperimentationService,
    ) { }

    getProps(props: SummarizedAgentHistoryProps): ISummarizedConversationHistoryInfo {
-     if (!ENABLE_HALF_CONTEXT_SUMMARIZATION) {
+     const enableHalfContext = this._configurationService.getExperimentBasedConfig(
+       ConfigKey.Advanced.HalfContextSummarization,
+       this._experimentationService
+     );
+     if (!enableHalfContext) {
        return this.getPropsLegacy(props);
      }
      return this.getPropsHalfContext(props);
    }
    // ...
  }
```

### 对于自用分支：方案 B 覆盖

在 `feature/half-context-summarize` 分支中，使用 TeamInternal 配置使团队成员默认开启：

```typescript
export const HalfContextSummarization = defineTeamInternalSetting<boolean>(
  'chat.advanced.halfContextSummarization',
  ConfigType.Simple,
  { defaultValue: false, teamDefaultValue: true, internalDefaultValue: true }
);
```

---

## 实施检查清单

### PR 版本
- [ ] 在 `configurationService.ts` 中定义 `HalfContextSummarization`
- [ ] 在 `package.json` 中添加配置声明
- [ ] 在 `package.nls.json` 中添加描述字符串
- [ ] 修改 `SummarizedConversationHistoryPropsBuilder` 注入服务
- [ ] 添加单元测试验证配置切换
- [ ] 更新 `half-context-pr-plan.md` 的 Phase 2

### 自用版本
- [ ] Cherry-pick PR 版本的改动
- [ ] 修改默认值为 `{ defaultValue: false, teamDefaultValue: true }`
- [ ] 验证 Extension Host 重启后默认开启

---

## 风险评估

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| 注入服务导致测试失败 | 中 | 需要更新 `createInstance` 调用 |
| 配置项命名不被接受 | 低 | 可在 PR review 阶段讨论调整 |
| 默认关闭影响体验 | 低 | 可通过实验服务灰度开启 |

````
