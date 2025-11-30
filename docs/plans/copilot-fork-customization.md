# Copilot Chat Fork 定制化计划

> 基于 `atelia-copilot-chat` fork 实现 AI Team 的定制化需求

## 需求清单

| # | 需求 | 优先级 | 复杂度 |
|---|------|--------|--------|
| 1 | SubAgent 不执行 Team Leader 唤醒仪式 | P0 | 低 |
| 2 | 滚动压缩（保留后半上下文）而非全量压缩 | P0 | 中 |
| 3 | 多模型调度（Claude/GPT/Gemini） | P1 | 中 |
| 4 | 主/SubAgent 分离外部记忆路径 | P1 | 低 |

## 技术可行性分析

### 需求 1: SubAgent 提示词分离

**关键发现**：
```typescript
// src/extension/prompt/node/subagentLoop.ts
context.tools = {
    ...context.tools,
    toolReferences: [],
    inSubAgent: true  // ← 这个标志可以用来判断
};
```

**实现方案**：
在 `agentPrompt.tsx` 中添加条件判断：

```tsx
// src/extension/prompts/node/agent/agentPrompt.tsx
const isSubAgent = this.props.promptContext.tools?.inSubAgent;

const baseAgentInstructions = <>
    <SystemMessage>
        You are an expert AI programming assistant, working with a user in the VS Code editor.<br />
        {!isSubAgent && <TeamLeaderAwakeningRitual />}
        {isSubAgent && <SubAgentRolePrompt role={detectedRole} />}
        ...
    </SystemMessage>
</>;
```

### 需求 2: 滚动压缩

**关键发现**：
```typescript
// src/extension/prompts/node/agent/summarizedConversationHistory.tsx
// 当前逻辑：遍历所有 history turns，找到 summary 后停止

for (const [i, turn] of [...this.props.promptContext.history.entries()].reverse()) {
    if (metadata?.summarizedHistory) {
        // 这里会把之前的都压缩掉
        history.push(<SummaryMessageElement ... />);
        break;
    }
    // ... 否则保留完整 turn
}
```

**改进方案**：
```typescript
// 只压缩前半部分，保留后半部分的完整对话
const halfwayPoint = Math.floor(this.props.promptContext.history.length / 2);
for (const [i, turn] of [...this.props.promptContext.history.entries()].reverse()) {
    if (i < halfwayPoint && shouldSummarize) {
        // 只压缩前半部分
        history.push(<PartialSummary turns={this.props.promptContext.history.slice(0, halfwayPoint)} />);
        break;
    }
    // 保留后半部分完整
    history.push(<FullTurn turn={turn} />);
}
```

### 需求 3: 多模型调度

**关键发现**：
```typescript
// src/extension/prompt/node/subagentLoop.ts
private async getEndpoint(request: ChatRequest) {
    let endpoint = await this.endpointProvider.getChatEndpoint(this.options.request);
    if (!endpoint.supportsToolCalls) {
        endpoint = await this.endpointProvider.getChatEndpoint('gpt-4.1');  // ← 硬编码 fallback
    }
    return endpoint;
}
```

**改进方案**：
添加配置化的模型选择：
```typescript
// 根据 task type 选择模型
const modelByTaskType = {
    'orchestration': 'claude-opus-4.5',
    'code-review': 'gpt-5.1-codex',
    'frontend': 'gemini-3-pro',
    'default': 'claude-opus-4.5'
};

private async getEndpoint(request: ChatRequest) {
    const taskType = this.detectTaskType(request);
    const modelId = this.configService.get('atelia.modelByTaskType')?.[taskType] 
                    ?? modelByTaskType[taskType] 
                    ?? modelByTaskType.default;
    return this.endpointProvider.getChatEndpoint(modelId);
}
```

### 需求 4: 分离外部记忆路径

**实现方案**：
添加 workspace 配置：
```json
// settings.json
{
    "atelia.agent.memoryPath": "agent-team/lead-metacognition.md",
    "atelia.subAgent.memoryPathPattern": "agent-team/members/{role}.md"
}
```

在提示词中读取：
```tsx
const memoryPath = isSubAgent 
    ? this.configService.get('atelia.subAgent.memoryPathPattern')?.replace('{role}', detectedRole)
    : this.configService.get('atelia.agent.memoryPath');

// 在提示词中引用
<SystemMessage>
    Read your cognitive state from: {memoryPath}
</SystemMessage>
```

## 实施路线图

### Phase 1: 环境验证 (已完成 ✅)
- [x] 克隆 fork 仓库
- [x] 安装 Node.js 22
- [x] 解决依赖冲突 (`--legacy-peer-deps`)
- [x] 验证编译成功

### Phase 2: 最小改动验证
- [ ] 添加 `TeamLeaderAwakeningRitual` 组件
- [ ] 在 `agentPrompt.tsx` 中根据 `inSubAgent` 条件注入
- [ ] 本地调试验证

### Phase 3: 滚动压缩
- [ ] 分析 `summarizedConversationHistory.tsx` 完整逻辑
- [ ] 实现保留后半部分的策略
- [ ] 添加配置项控制保留比例

### Phase 4: 多模型与记忆路径
- [ ] 添加 workspace 配置项
- [ ] 实现模型选择逻辑
- [ ] 实现记忆路径分离

### Phase 5: 打包与部署
- [ ] 修改 package.json 的扩展 ID
- [ ] 构建 VSIX
- [ ] 替换现有 Copilot Chat 扩展

## 技术风险

1. **API 兼容性**：VS Code 的 proposed API 可能变化
2. **Token 获取**：`npm run get_token` 需要 OAuth 认证
3. **调试环境**：需要 VS Code F5 调试支持
4. **维护负担**：需要跟踪上游更新

## 备选方案

如果 fork 定制太复杂，可以继续使用 patch 脚本，但增强其功能：
- 动态读取配置文件生成提示词
- 检测 minified 代码的变化自动适配
- 添加版本锁定和回滚机制

---

*Created: 2025-11-29*
*Status: Phase 1 Complete, Ready for Phase 2*
