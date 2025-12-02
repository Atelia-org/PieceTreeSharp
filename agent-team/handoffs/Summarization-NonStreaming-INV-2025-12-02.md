# 半上下文压缩非流式请求调查 Brief

## 调查目标
深入分析半上下文压缩的 `stream: false` 非流式请求路径，识别超时处理机制和日志点位置。

## TS 源码位置
- `atelia-copilot-chat/src/extension/prompts/node/agent/summarizedConversationHistory.tsx` — 压缩核心逻辑
- `atelia-copilot-chat/src/extension/prompt/node/chatMLFetcher.ts` — ChatML 请求处理
- `atelia-copilot-chat/src/platform/endpoint/node/chatEndpoint.ts` — 端点实现
- `atelia-copilot-chat/src/platform/networking/common/networking.ts` — 网络请求超时配置
- `atelia-copilot-chat/src/platform/openai/node/fetch.ts` — 流式/非流式响应处理

---

## 关键发现

### 1. `stream: false` 请求路径 (第 510-521 行)

```typescript
// summarizedConversationHistory.tsx 第 510-521 行
summaryResponse = await endpoint.makeChatRequest2({
    debugName: `summarizeConversationHistory-${mode}`,
    messages: ToolCallingLoop.stripInternalToolCallIds(summarizationPrompt),
    finishedCb: undefined,
    location: ChatLocation.Other,
    requestOptions: {
        temperature: 0,
        stream: false,  // <-- 非流式请求！
        ...toolOpts
    },
    telemetryProperties: associatedRequestId ? { associatedRequestId } : undefined,
    enableRetryOnFilter: true
}, this.token ?? CancellationToken.None);
```

### 2. 请求流程链

```
summarizedConversationHistory.tsx::getSummary()
  ↓ endpoint.makeChatRequest2()
  ↓ ChatEndpoint._makeChatRequest2() [chatEndpoint.ts 第 286-289 行]
  ↓ _chatMLFetcher.fetchOne()
  ↓ ChatMLFetcherImpl.fetchMany() [chatMLFetcher.ts]
  ↓ fetchAndStreamChat() [fetch.ts 第 89-170 行]
  ↓ postRequest() [networking.ts]
  ↓ networkRequest() — timeout: 30秒
```

### 3. `stream: false` 被覆盖的问题

**关键发现**：在 `AbstractChatMLFetcher.preparePostOptions()` (chatMLFetcher.ts 第 50-57 行) 中：

```typescript
protected preparePostOptions(requestOptions: OptionalChatRequestParams): OptionalChatRequestParams {
    return {
        temperature: this.options.temperature,
        top_p: this.options.topP,
        // we disallow `stream=false` because we don't support non-streamed response
        ...requestOptions,
        stream: true  // <-- 强制覆盖为 true！
    };
}
```

然而，在 `ChatEndpoint.interceptBody()` (chatEndpoint.ts 第 195-203 行) 中：

```typescript
interceptBody(body: IEndpointBody | undefined): void {
    // 如果模型不支持流式传输，则设为 false
    if (body && !this._supportsStreaming) {
        body.stream = false;
    }
    // ...
}
```

**结论**：`stream: false` 只有在模型不支持流式传输时才会生效。对于常规模型（如 gpt-4.1），`stream` 会被强制设为 `true`。

### 4. 非流式响应处理

在 `chatEndpoint.ts` 第 61-96 行，定义了 `defaultNonStreamChatResponseProcessor()`：

```typescript
export async function defaultNonStreamChatResponseProcessor(
    response: Response, 
    finishCallback: FinishedCallback, 
    telemetryData: TelemetryData
) {
    const textResponse = await response.text();  // <-- 等待整个响应！
    const jsonResponse = JSON.parse(textResponse);
    // ...处理完成...
}
```

**这是超时问题的根源**：`await response.text()` 会阻塞等待整个 LLM 响应完成。

### 5. 网络超时配置

在 `networking.ts` 第 52 行：

```typescript
const requestTimeoutMs = 30 * 1000; // 30 seconds
```

**问题**：30 秒对于大型对话压缩可能不够！

### 6. StopWatch 用法 (第 452-474 行)

```typescript
private async getSummary(mode: SummaryMode, propsInfo: ISummarizedConversationHistoryInfo): Promise<FetchSuccess<string>> {
    const stopwatch = new StopWatch(false);  // <-- 开始计时
    // ...渲染 prompt...
    this.logInfo(`summarization prompt rendered in ${stopwatch.elapsed()}ms.`, mode);
    // ...请求...
    return this.handleSummarizationResponse(summaryResponse, mode, stopwatch.elapsed());  // <-- 记录总耗时
}
```

遥测发送位置在 `sendSummarizationTelemetry()` (第 570-641 行)：

```typescript
this.telemetryService.sendMSFTTelemetryEvent('summarizedConversationHistory', {
    // ...
}, {
    duration: elapsedTime,  // <-- stopwatch.elapsed()
    // ...
});
```

---

## 超时处理现状

### 现有机制
1. **网络层超时**：30 秒硬编码超时 (networking.ts 第 52 行)
2. **CancellationToken**：可从外部取消请求
3. **重试机制**：`enableRetryOnFilter: true` 允许内容过滤后重试

### 缺失机制
1. **无专门的压缩超时**：压缩请求使用通用网络超时
2. **无非流式响应超时**：`response.text()` 无独立超时控制
3. **无阶段性进度报告**：只有开始和结束的日志

---

## 推荐的日志添加位置

### 高优先级（立即诊断超时问题）

| 位置 | 文件 | 行号 | 建议添加的日志 |
|------|------|------|----------------|
| 1 | `summarizedConversationHistory.tsx` | 509-510 | `[Summarizer] Starting request, mode=${mode}` |
| 2 | `summarizedConversationHistory.tsx` | 522 | `[Summarizer] Request completed in ${stopwatch.elapsed()}ms` |
| 3 | `chatEndpoint.ts` | 61-62 | `[NonStreamProcessor] Waiting for response.text()...` |
| 4 | `chatEndpoint.ts` | 63 | `[NonStreamProcessor] Got text response, length=${textResponse.length}` |

### 中优先级（详细诊断）

| 位置 | 文件 | 行号 | 建议添加的日志 |
|------|------|------|----------------|
| 5 | `networking.ts` | 270-275 | `[Network] POST request started, timeout=${requestTimeoutMs}ms` |
| 6 | `fetch.ts` | 145-150 | `[FetchChat] Response received, status=${response.status}` |

### 现有日志点

| 位置 | 行号 | 现有日志 |
|------|------|----------|
| `summarizedConversationHistory.tsx` | 418-419 | `[Summarizer] getSummaryWithFallback called` |
| `summarizedConversationHistory.tsx` | 426-427 | `[Summarizer] Attempting Full mode...` |
| `summarizedConversationHistory.tsx` | 428 | `[Summarizer] Full mode succeeded` |
| `summarizedConversationHistory.tsx` | 476 | `summarization prompt rendered in ${stopwatch.elapsed()}ms` |

---

## 技术建议

### 短期修复
1. **增加非流式请求超时**：将 30 秒增加到 60-120 秒
2. **添加进度日志**：在 `response.text()` 前后添加日志

### 中期优化
1. **保持流式传输**：即使不需要实时显示，流式可避免长时间阻塞
2. **实现分段超时**：连接超时 vs 读取超时分离

### 长期架构
1. **后台队列**：将压缩任务放入后台队列，避免阻塞主请求
2. **增量压缩**：在 token 接近限制前就开始压缩，而不是超限后再压缩

---

## 相关文件清单

| 文件 | 职责 |
|------|------|
| `summarizedConversationHistory.tsx` | 压缩 prompt 构建、请求发送、结果处理 |
| `chatMLFetcher.ts` | ChatML 请求封装、`stream: true` 强制 |
| `chatEndpoint.ts` | 端点实现、`interceptBody()` 处理 |
| `networking.ts` | 网络请求、30秒超时配置 |
| `fetch.ts` | `fetchAndStreamChat()` 请求执行 |
| `stopwatch.ts` | 耗时测量工具类 |

---

## Changefeed 锚点
`#delta-2025-12-02-summarization-nonstreaming-inv`

---

*Generated by InvestigatorTS @ 2025-12-02*
