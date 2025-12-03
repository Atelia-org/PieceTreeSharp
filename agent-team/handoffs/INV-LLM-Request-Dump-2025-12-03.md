# LLM Request Dump 位置调查 Brief

> **日期:** 2025-12-03
> **调查员:** InvestigatorTS
> **请求者:** Team Leader
> **目的:** 找到最佳位置 dump 发送给 LLM API 的完整 HTTP 请求，用于调试半上下文压缩失败问题

## 调查摘要

分析了 `atelia-copilot-chat` 中 LLM 请求的完整调用栈，识别出 **3 个层次** 的 dump 位置，推荐在 `fetchWithInstrumentation` 函数中添加 request body JSON dump。

---

## 关键发现

### 1. 请求调用栈（从高到低）

```
ChatMLFetcherImpl.fetchMany()           ← 业务层：debugName, messages, requestOptions
    ↓
fetchAndStreamChat()                    ← 编排层：创建 requestBody, 调用 fetchWithInstrumentation
    ↓
fetchWithInstrumentation()              ← 最佳 dump 位置 ⭐
    ↓
postRequest()                           ← 网络层：添加 headers, 调用 fetcher.fetch
    ↓
fetcher.fetch()                         ← 底层 HTTP：node-fetch 或 electron net
```

### 2. 推荐 Dump 位置

**位置:** `src/platform/openai/node/fetch.ts` - `fetchWithInstrumentation()` 函数

**行号:** ~320-340 (在调用 `postRequest` 之前)

**原因:**
- 此时 `requestBody` 已完全构建（包含 messages, model, tools, stream 等）
- 可以访问 `ourRequestId` 用于关联请求
- 可以访问 `chatEndpoint` 信息（model, apiType）
- 已有 telemetry 基础设施，容易添加日志

### 3. 区分请求来源的方法

已有多个字段可用于区分：

| 字段 | 来源 | 用途 |
|------|------|------|
| `telemetryProperties.messageSource` | ChatMLFetcherImpl | 等于 `debugName`，标识请求来源 |
| `telemetryProperties.modelCallId` | fetchAndStreamChat | 唯一 UUID，可追踪单次调用 |
| `ourRequestId` | ChatMLFetcherImpl | 请求 ID，发送到服务端 |
| `debugName` | 调用方传入 | 人类可读的请求名称 |

**推测:** 半上下文压缩的 `debugName` 可能类似 `"summarize"` 或 `"compress"`，dry-run 可能有不同的 `debugName` 或通过 `requestOptions` 区分。

### 4. 现有调试探针

已发现 `DEBUG_PROBES.md` 中记录了 5 个探针位置，部分已添加到代码中：

| 探针 | 文件 | 状态 |
|------|------|------|
| PROBE 1 | chatEndpoint.ts:defaultNonStreamChatResponseProcessor | ✅ 已添加 |
| PROBE 3 | networking.ts:postRequest | ✅ 已添加 |
| PROBE 4 | chatEndpoint.ts:processResponseFromChatEndpoint | ✅ 已添加 |
| PROBE 5 | stream.ts:SSEProcessor.processSSEInner | ✅ 已添加 |

---

## 建议的日志格式

### A. 完整 JSON Dump（用于详细调试）

```typescript
// 在 fetchWithInstrumentation() 中，postRequest 调用之前
const dumpData = {
  timestamp: new Date().toISOString(),
  debugName: telemetryProperties?.messageSource || 'unknown',
  requestId: ourRequestId,
  modelCallId: telemetryProperties?.modelCallId,
  model: chatEndpoint.model,
  apiType: chatEndpoint.apiType,
  stream: request.stream,
  messageCount: request.messages?.length,
  toolCount: request.tools?.length,
  // 可选：完整 body（注意 PII）
  // body: JSON.stringify(request, null, 2)
};
console.log(`[LLM-DUMP] ${JSON.stringify(dumpData)}`);
```

### B. MD5 Hash 比较（用于快速对比）

```typescript
import { createHash } from 'crypto';

const bodyHash = createHash('md5')
  .update(JSON.stringify(request))
  .digest('hex');

console.log(`[LLM-DUMP] requestId=${ourRequestId} debugName=${debugName} bodyHash=${bodyHash}`);
```

### C. 写入文件（用于详细分析）

```typescript
import { writeFileSync } from 'fs';
import { join } from 'path';

const dumpPath = join('/tmp', `llm-request-${ourRequestId}.json`);
writeFileSync(dumpPath, JSON.stringify(request, null, 2));
console.log(`[LLM-DUMP] Saved to ${dumpPath}`);
```

---

## 具体代码修改位置

**文件:** `atelia-copilot-chat/src/platform/openai/node/fetch.ts`

**函数:** `fetchWithInstrumentation` (约第 280-360 行)

**插入点:** 在 `return postRequest(...)` 之前

```typescript
// 在 fetchWithInstrumentation() 函数中
async function fetchWithInstrumentation(
    // ... 参数 ...
): Promise<Response> {
    // ... 现有代码 ...
    
    // === 新增：Request Body Dump ===
    const dumpEnabled = process.env.DEBUG_LLM_REQUESTS === 'true';
    if (dumpEnabled) {
        const dumpData = {
            timestamp: new Date().toISOString(),
            debugName: telemetryProperties?.messageSource || 'unknown',
            requestId: ourRequestId,
            model: chatEndpoint.model,
            apiType: chatEndpoint.apiType,
            stream: request.stream,
            messageCount: request.messages?.length || request.input?.length,
            bodyHash: require('crypto').createHash('md5').update(JSON.stringify(request)).digest('hex'),
        };
        console.log(`[LLM-DUMP] ${JSON.stringify(dumpData)}`);
        
        // 可选：保存完整 body 到文件
        // require('fs').writeFileSync(`/tmp/llm-${ourRequestId}.json`, JSON.stringify(request, null, 2));
    }
    // === End Dump ===
    
    return postRequest(
        // ... 现有参数 ...
    );
}
```

---

## 下一步建议

1. **添加环境变量控制:** 使用 `DEBUG_LLM_REQUESTS=true` 启用日志
2. **运行对比测试:**
   - 真实压缩：记录 bodyHash
   - dry-run：记录 bodyHash
   - 比较两者是否一致
3. **如果 Hash 不同:** 保存完整 JSON，使用 diff 工具比较
4. **如果 Hash 相同:** 问题在服务端或响应处理，需检查 stream.ts 的 SSE 处理

---

## 相关文件索引

| 文件 | 职责 |
|------|------|
| `src/extension/prompt/node/chatMLFetcher.ts` | 业务层：ChatMLFetcherImpl |
| `src/platform/openai/node/fetch.ts` | 编排层：fetchAndStreamChat, fetchWithInstrumentation ⭐ |
| `src/platform/networking/common/networking.ts` | 网络层：postRequest |
| `src/platform/networking/node/stream.ts` | SSE 处理：SSEProcessor |
| `src/platform/endpoint/node/chatEndpoint.ts` | Endpoint 逻辑：processResponseFromChatEndpoint |
| `DEBUG_PROBES.md` | 现有调试探针文档 |

---

## Changefeed Anchor

`#delta-2025-12-03-llm-dump-investigation`
