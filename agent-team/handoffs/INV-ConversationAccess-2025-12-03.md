# Investigation Brief: Developer Console 访问 Copilot Conversation 数据

## 目标
调查如何在 VS Code Developer Console 中访问和修改 Copilot Chat 的 Conversation 数据，特别是重置某个 `ToolCallRound.summary` 属性。

## TS 源码位置
- `atelia-copilot-chat/src/extension/conversationStore/node/conversationStore.ts` — ConversationStore 服务
- `atelia-copilot-chat/src/extension/prompt/common/conversation.ts` — Conversation, Turn 类
- `atelia-copilot-chat/src/extension/prompt/common/toolCallRound.ts` — ToolCallRound 类
- `atelia-copilot-chat/src/extension/prompt/common/intents.ts` — IToolCallRound 接口
- `atelia-copilot-chat/src/extension/prompts/vscode-node/summarization.contribution.ts` — Debug 命令

## 关键发现

### 1. ConversationStore 未暴露到全局
- `ConversationStore` 是一个内部服务，通过 DI 容器（`IInstantiationService`）管理
- 没有 `globalThis`、`window` 或其他全局对象的赋值
- 无法直接在 Developer Console 访问

### 2. 数据结构关系
```
Conversation
  └─ turns: Turn[]
       └─ resultMetadata: IResultMetadata
            └─ toolCallRounds: IToolCallRound[]
                 └─ summary?: string  ← 目标属性
```

**重要**：`Turn.rounds` 是一个 getter，实际上从 `resultMetadata.toolCallRounds` 读取：
```typescript
get rounds(): readonly IToolCallRound[] {
    const metadata = this.resultMetadata;
    const rounds = metadata?.toolCallRounds;
    // ...
    return rounds;
}
```

### 3. ToolCallRound.summary 是可变的
```typescript
// toolCallRound.ts
export class ToolCallRound implements IToolCallRound {
    public summary: string | undefined;  // ← 可直接赋值
    // ...
}
```

### 4. 已有的 Debug 命令
在 `summarization.contribution.ts` 中已注册：
- `github.copilot.debug.inspectConversation` — 查看当前 conversation 结构
- `github.copilot.debug.dryRunSummarization` — 使用真实数据测试压缩
- `github.copilot.debug.dryRunSummarizationMock` — 使用 mock 数据测试压缩

## 结论与建议

### ❌ 无法直接在 Console 访问
由于 ConversationStore 是内部服务，没有暴露到全局命名空间，因此**无法在 Developer Console 中直接访问和修改**。

### ✅ 建议方案：添加 Debug 命令

最简单的方法是在 `summarization.contribution.ts` 中添加一个新命令：

```typescript
// 添加到 registerCommands() 方法
vscode.commands.registerCommand('github.copilot.debug.clearRoundSummary', async () => {
    const outputChannel = this.outputChannel;
    outputChannel.show(true);
    
    const conversation = this.conversationStore.lastConversation;
    if (!conversation) {
        vscode.window.showWarningMessage('No active conversation found.');
        return;
    }
    
    // 让用户输入要清除的 Round ID
    const roundId = await vscode.window.showInputBox({
        prompt: 'Enter the Round ID to clear summary',
        placeHolder: 'e.g., aed30277-4027-4f57-b7b6-fc5547f7c630'
    });
    
    if (!roundId) return;
    
    // 遍历所有 turns 和 rounds 查找目标
    for (const turn of conversation.turns) {
        for (const round of turn.rounds) {
            if (round.id === roundId) {
                const oldSummary = round.summary;
                round.summary = undefined;
                outputChannel.info(`Cleared summary for round ${roundId}`);
                outputChannel.info(`Old summary: ${oldSummary}`);
                vscode.window.showInformationMessage(`Cleared summary for round ${roundId}`);
                return;
            }
        }
    }
    
    vscode.window.showWarningMessage(`Round ${roundId} not found.`);
}),
```

### ✅ 使用步骤

1. **先运行 inspect 命令查看 Round ID**：
   - 命令面板：`GitHub Copilot: Debug - Inspect Conversation`
   - 在输出面板查看所有 rounds 及其 ID 和 summary 状态

2. **运行 clear 命令**（需要先添加上述代码）：
   - 命令面板：`GitHub Copilot: Debug - Clear Round Summary`
   - 输入目标 Round ID：`aed30277-4027-4f57-b7b6-fc5547f7c630`

3. **验证**：
   - 再次运行 inspect 命令确认 summary 已清除

## 实现复杂度
- **添加命令**：~15 行代码，5 分钟
- **测试**：运行 inspect → clear → inspect 验证

## Handoff 信息
- **任务来源**：用户请求调查 Developer Console 访问
- **交付物**：本 Brief
- **后续**：Porter 可直接将建议代码添加到 `summarization.contribution.ts`
