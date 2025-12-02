# atelia-copilot-chat LLM 调用架构调查报告

**日期**: 2025-12-02
**调查者**: InvestigatorTS
**任务**: 调查半上下文压缩失败问题的 LLM 调用架构

## 调查摘要

调查了 `atelia-copilot-chat` 的 LLM API 调用架构，从入口点到流式响应处理的完整数据流。识别了三个关键层：LanguageModelAccess (VS Code API 层)、ChatMLFetcher (请求执行层)、SSEProcessor (流式解析层)。发现半上下文压缩使用 `stream: false` 选项，不走流式处理路径。

---

## 关键文件路径

### 1. 入口层 (VS Code API Integration)
| 文件 | 职责 |
|------|------|
| `src/extension/conversation/vscode-node/languageModelAccess.ts` | VS Code `vscode.lm` API 实现，模型注册 |
| `src/extension/prompt/vscode-node/endpointProviderImpl.ts` | 端点提供者，模型选择 |

### 2. 请求执行层 (ChatML Fetcher)
| 文件 | 职责 |
|------|------|
| `src/extension/prompt/node/chatMLFetcher.ts` | **核心请求执行器**，处理重试、错误、遥测 |
| `src/platform/openai/node/fetch.ts` | HTTP 请求封装，`fetchAndStreamChat()` 入口 |
| `src/platform/networking/common/networking.ts` | `postRequest()` 网络层封装 |

### 3. 流式响应处理层 (SSE Processing)
| 文件 | 职责 |
|------|------|
| `src/platform/networking/node/stream.ts` | **SSEProcessor** - SSE 流解析核心 |
| `src/platform/networking/common/fetch.ts` | FinishedCallback 接口定义，delta 类型 |
| `src/platform/endpoint/node/chatEndpoint.ts` | `processResponseFromChatEndpoint()` 响应处理分发 |

### 4. 半上下文压缩 (Summarization)
| 文件 | 职责 |
|------|------|
| `src/extension/prompt/node/summarizer.ts` | `ChatSummarizerProvider` - VS Code API 层摘要入口 |
| `src/extension/prompts/node/agent/summarizedConversationHistory.tsx` | **主力压缩逻辑** - `ConversationHistorySummarizer` |

---

## LLM 调用数据流

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           VS Code API 层                                 │
│   LanguageModelAccess.provideLanguageModelChatResponse()                │
│        │                                                                 │
│        ▼                                                                 │
│   CopilotLanguageModelWrapper.provideLanguageModelResponse()            │
└────────│────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                          ChatML Fetcher 层                               │
│   ChatMLFetcherImpl.fetchMany()                                         │
│        │                                                                 │
│        ├─ 1. preparePostOptions() ← { temperature, top_p, stream: true }│
│        ├─ 2. endpoint.createRequestBody()                               │
│        ├─ 3. isValidChatPayload() 验证                                   │
│        ├─ 4. _requestLogger.logChatRequest() 记录请求                    │
│        │                                                                 │
│        ▼                                                                 │
│   fetchAndStreamChat() ← src/platform/openai/node/fetch.ts              │
│        │                                                                 │
│        ├─ fetchWithInstrumentation() ← 遥测包装                          │
│        │       │                                                         │
│        │       ▼                                                         │
│        │   postRequest() ← src/platform/networking/common/networking.ts │
│        │       │                                                         │
│        │       ▼                                                         │
│        │   IFetcherService.fetch() → HTTP POST                          │
└────────│────────────────────────────────────────────────────────────────┘
         │
         ▼ Response
┌─────────────────────────────────────────────────────────────────────────┐
│                          响应处理层                                       │
│   chatEndpoint.processResponseFromChatEndpoint()                         │
│        │                                                                 │
│        ├─ 流式: SSEProcessor.create() → processSSE()                     │
│        │         │                                                       │
│        │         ├─ for await (chunk of body) ← 逐块读取                 │
│        │         ├─ splitChunk() → 解析 SSE data: lines                 │
│        │         ├─ JSON.parse() → 解析 choice                          │
│        │         ├─ finishedCb(text, index, delta) ← 回调上层            │
│        │         └─ yield FinishedCompletion                            │
│        │                                                                 │
│        └─ 非流式: defaultNonStreamChatResponseProcessor()               │
│                   │                                                       │
│                   ├─ response.text() → JSON.parse()                     │
│                   └─ finishedCb() 一次性回调                             │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 半上下文压缩调用流程

```
SummarizedConversationHistory.render()
    │
    ├─ triggerSummarize = true ?
    │       │
    │       ▼
    │   ConversationHistorySummarizer.summarizeHistory()
    │       │
    │       ▼
    │   getSummaryWithFallback()
    │       │
    │       ├─ getSummary(SummaryMode.Full)   ← 尝试完整模式
    │       │       │
    │       │       ▼
    │       │   renderPromptElement() → 生成 summarizationPrompt
    │       │       │
    │       │       ▼
    │       │   endpoint.makeChatRequest2({
    │       │       stream: false,          ← ⚠️ 关键: 非流式请求
    │       │       temperature: 0,
    │       │       debugName: 'summarizeConversationHistory-full'
    │       │   })
    │       │       │
    │       │       ▼
    │       │   handleSummarizationResponse()
    │       │
    │       └─ 失败时: getSummary(SummaryMode.Simple) ← 降级简单模式
    │
    └─ 返回 summary → 添加到 history
```

### 关键发现

1. **压缩请求使用 `stream: false`**:
   ```typescript
   // summarizedConversationHistory.tsx:375
   requestOptions: {
       temperature: 0,
       stream: false,  // ← 非流式！
       ...toolOpts
   }
   ```

2. **非流式响应处理路径**:
   - `defaultNonStreamChatResponseProcessor()` 在 `chatEndpoint.ts:63-104`
   - 使用 `response.text()` 一次性读取，而非流式解析

3. **超时可能发生的位置**:
   - `postRequest()` 中的 `requestTimeoutMs = 30 * 1000` (30秒)
   - 但 `response.text()` 没有独立超时控制

---

## 推荐日志添加位置

### 优先级 P0 (立即添加)

1. **`summarizedConversationHistory.tsx:362-380`** - 压缩请求发起前后
   ```typescript
   // 添加位置: getSummary() 方法内
   this.logService.info(`[Summarizer] Sending request. mode=${mode}, endpoint=${endpoint.model}`);
   const startTime = Date.now();
   
   summaryResponse = await endpoint.makeChatRequest2({ ... });
   
   this.logService.info(`[Summarizer] Request returned. elapsed=${Date.now() - startTime}ms`);
   ```

2. **`src/platform/openai/node/fetch.ts:145-155`** - `fetchWithInstrumentation()` 返回后
   ```typescript
   // 添加位置: postRequest().then() 内
   this.logService.info(`[Fetch] Response headers received. status=${response.status}, elapsed=${totalTimeMs}ms`);
   ```

3. **`src/platform/networking/node/stream.ts:356`** - SSE 流开始/结束
   ```typescript
   // 添加位置: processSSEInner() 循环前后
   this.logService.info(`[SSE] Starting stream processing. requestId=${this.requestId.headerRequestId}`);
   // ... for await ...
   this.logService.info(`[SSE] Stream finished. requestId=${this.requestId.headerRequestId}`);
   ```

### 优先级 P1 (辅助诊断)

4. **`src/platform/endpoint/node/chatEndpoint.ts:274-290`** - 响应类型分发
   ```typescript
   // 添加位置: processResponseFromChatEndpoint() 开头
   this.logService.debug(`[ChatEndpoint] Processing response. streaming=${this._supportsStreaming}, useResponsesApi=${this.useResponsesApi}`);
   ```

5. **`src/extension/prompt/node/chatMLFetcher.ts:152`** - 请求验证后
   ```typescript
   // 添加位置: fetchAndStreamChat() 调用前
   this.logService.info(`[ChatMLFetcher] Starting fetch. debugName=${debugName}, stream=${postOptions.stream}`);
   ```

---

## 问题假设

### 可能原因 1: 非流式响应超时

**现象**: 日志显示 3 秒，实际 3:30

**假设**: `response.text()` 在等待完整响应时卡住，但没有独立超时
- `requestTimeoutMs = 30000` 只控制 `fetch()` 初始连接
- 一旦连接建立，`response.text()` 可能无限等待

**验证方法**: 在 `defaultNonStreamChatResponseProcessor()` 添加超时包装

### 可能原因 2: 日志时间戳错误

**假设**: `StopWatch` 或 `Date.now()` 在某些代码路径没有正确记录
- `summarizedConversationHistory.tsx:336` 使用 `StopWatch(false)` 但未调用 `start()`

**验证方法**: 检查 `StopWatch` 用法是否正确

### 可能原因 3: 重试逻辑隐藏耗时

**假设**: `getSummaryWithFallback()` 中 Full → Simple 的降级可能发生多次重试
- 每次重试都是 3 分钟级别的超时

**验证方法**: 检查 `getSummaryWithFallback()` 的重试次数

---

## 后续建议

### 对 Team Leader
1. 在关键路径添加 P0 日志后复现问题
2. 检查 `StopWatch(false)` 是否正确使用 (`false` 参数意味着不自动启动)
3. 考虑为非流式请求添加独立超时控制

### 对 Porter-CS
如果需要移植压缩逻辑，注意:
- 非流式请求路径单独处理
- 需要实现独立的超时控制机制

### 对 QA-Automation
- 构建超时场景测试用例
- 模拟 LLM 响应延迟验证日志完整性

---

## 参考链接

- `deepwiki/4_Language_Model_Integration.md` - 架构概览
- `deepwiki/8_Chat_ML_Fetcher_and_Request_Execution.md` - 请求执行详解
