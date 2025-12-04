# Half-Context Summarization PR Plan

> **Status:** Planning
> **Target:** https://github.com/microsoft/vscode-copilot-chat (upstream)
> **Date:** 2025-12-03
> **Related Issue:** https://github.com/microsoft/vscode/issues/280898

## Overview

将半上下文压缩功能以低入侵性方式贡献给 upstream。

## Branch Strategy

### 双分支管理

| 分支 | 用途 | 内容 |
|------|------|------|
| `feature/half-context-summarize` | **我们自己用** | 完整版：核心功能 + 调试命令 + Opus workaround + 探针 |
| `pr/half-context-summarize` | **提交 PR 用** | 精简版：仅核心功能 + feature flag |

### 工作流程

```
feature/half-context-summarize (完整版)
    │
    ├── 日常开发、调试、实验
    │
    └── cherry-pick 核心改动 ──→ pr/half-context-summarize (PR版)
                                     │
                                     └── 提交 PR 到 upstream
```

### Git 操作示例

```bash
# 创建 PR 分支（基于 upstream main）
git checkout -b pr/half-context-summarize upstream/main

# 从 feature 分支 cherry-pick 核心改动
git cherry-pick <commit-hash>  # 仅选择核心功能 commit

# 切换回开发分支
git checkout feature/half-context-summarize
```

## 分支内容对比

### feature/half-context-summarize (完整版)

包含：
- ✅ 核心功能：半上下文切分逻辑
- ✅ Developer 命令：`dryRunSummarization`, `clearRoundSummary`, `inspectConversation`, `toggleToolInjection`
- ✅ 调试探针：`[SUMMARIZE DEBUG]` 日志
- ✅ Opus 4.5 Workaround：`__SUMMARIZATION_DEBUG_FLAGS__.injectTools`
- ✅ Request body dump：`/tmp/llm-request-*.json`

### pr/half-context-summarize (PR版)

仅包含：
- ✅ 核心功能：半上下文切分逻辑
- ✅ Feature flag：`github.copilot.chat.experimental.halfContextSummarization`
- ✅ 基础单元测试
- ❌ 无调试命令
- ❌ 无调试探针
- ❌ 无 Workaround（等待 Issue #280898 修复）

## Design Principles

### 1. Feature Flag 控制

```typescript
// 配置项设计
{
  "github.copilot.chat.experimental.halfContextSummarization": {
    "type": "boolean",
    "default": false,  // 默认关闭，需用户主动开启
    "description": "Enable experimental half-context summarization for longer conversations"
  }
}
```

### 2. 最小改动原则

- 复用现有 `ConversationHistorySummarizer` 接口
- 只在 `PropsBuilder` 层面做切分逻辑
- 保持原有全上下文压缩作为默认行为（flag=false 时）

### 3. 代码改动范围

```
PR 涉及文件（预估）:
├── src/extension/prompts/node/agent/summarizedConversationHistory.tsx  # 核心改动
├── package.json  # 添加配置项
└── test/...  # 单元测试
```

## Implementation Steps

### Phase 1: 分支准备 ✅
- [x] 确认双分支策略
- [x] 创建 `pr/half-context-summarize` 分支 (基于 upstream/main e80c6901)
- [x] 应用核心改动 (+140 行，1 个文件)
- [x] 编译验证通过

### Phase 2: 代码整理 ✅
- [x] 从 feature 分支提取核心改动
- [x] 移除所有调试代码 (0 个 console.log, 0 个 DEBUG)
- [x] 添加 feature flag: `ConfigKey.Advanced.HalfContextSummarization`
- [x] 代码风格对齐 (lint-staged 通过)
- [x] 配置项声明 (package.json + package.nls.json)

### Phase 3: 测试补全
- [ ] 添加 PropsBuilder 单元测试
- [ ] 运行现有测试确认无回归
- [ ] (可选) Simulation test

### Phase 4: PR 提交
- [ ] 创建清晰的 PR 描述
- [ ] DCO sign-off
- [ ] 响应 review 反馈

## Current Branch Status

```
pr/half-context-summarize (PR版 - 当前)
├── 50bc0513 feat: add half-context summarization for conversation history
└── e80c6901 (upstream/main) tracer: fix stringifying arrays

feature/half-context-summarize (完整版)
├── 4ab5fc31 dryRunSummarization use ConversationHistorySummarizer.getSummary directly
├── 4483b624 github.copilot.debug.toggleSummarizationToolInjection
├── 2773f6c4 half-context-summarize and dryRunSummarization
└── ... (upstream commits)
```

## Open Questions

1. **Opus 4.5 + Tools 空输出问题**
   - Issue: https://github.com/microsoft/vscode/issues/280898
   - PR 中不包含 workaround，等待官方修复
   - 我们自己的 feature 分支保留 workaround

2. **Simulation Tests**
   - 需要 VS Code 团队协助重建 cache
   - 可先提交 PR，测试问题后续解决

## References

- Upstream repo: https://github.com/microsoft/vscode-copilot-chat
- 贡献指南: [`atelia-copilot-chat/CONTRIBUTING.md`](../../atelia-copilot-chat/CONTRIBUTING.md)
- Opus Bug Issue: https://github.com/microsoft/vscode/issues/280898
