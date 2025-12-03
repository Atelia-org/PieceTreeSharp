# Half-Context Summarization PR Plan

> **Status:** Planning
> **Target:** https://github.com/microsoft/vscode-copilot-chat (upstream)
> **Date:** 2025-12-03

## Overview

将半上下文压缩功能以低入侵性方式贡献给 upstream。

## Design Principles

### 1. Feature Flag 控制

```typescript
// 配置项设计
{
  "github.copilot.chat.experimental.halfContextSummarization": {
    "type": "boolean",
    "default": false,  // 默认关闭
    "description": "Enable experimental half-context summarization for longer conversations"
  }
}
```

### 2. 最小改动原则

- 复用现有 `ConversationHistorySummarizer` 接口
- 只在 `PropsBuilder` 层面做切分逻辑
- 保持原有全上下文压缩作为默认行为

### 3. 代码组织

```
src/extension/prompts/node/agent/
├── summarizedConversationHistory.tsx  # 现有文件，增加 flag 判断
└── halfContextSummarization.ts        # 新文件，封装半上下文逻辑 (可选)
```

### 4. 测试策略

- [ ] 单元测试: PropsBuilder 切分逻辑
- [ ] 集成测试: 端到端压缩流程
- [ ] 回归测试: 全上下文压缩不受影响

## Implementation Steps

### Phase 1: 准备工作
- [ ] 确认 upstream 贡献指南 (CONTRIBUTING.md)
- [ ] 设置 simulation tests cache (需 VS Code 团队协助)
- [ ] 确认 DCO sign-off 要求

### Phase 2: 代码整理
- [ ] 移除调试代码 (`[SUMMARIZE DEBUG]` 等)
- [ ] 添加 feature flag
- [ ] 代码风格对齐 (eslint, tsfmt)

### Phase 3: 测试补全
- [ ] 添加单元测试
- [ ] 运行现有测试确认无回归

### Phase 4: PR 提交
- [ ] 创建清晰的 PR 描述
- [ ] 附带性能数据 (压缩时间、质量对比)
- [ ] 响应 review 反馈

## Blockers

1. **Simulation tests cache** - 需要 VS Code 团队成员重建
2. **Claude Opus 4.5 bug** - 需等待修复或确认 workaround 可接受

## References

- Upstream repo: https://github.com/microsoft/vscode-copilot-chat
- 贡献指南: [`atelia-copilot-chat/CONTRIBUTING.md`](../../atelia-copilot-chat/CONTRIBUTING.md)
- Issue: [待创建](./issue-opus-empty-response.md)
