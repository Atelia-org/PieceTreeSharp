# SSE Stream Processing Investigation Brief

## 日期
2025-12-02

## 目标
调查 SSE stream 处理的底层代码，找到可以添加 token 级别日志的位置。

## TS 源码位置
- `atelia-copilot-chat/src/platform/networking/node/stream.ts`
- `atelia-copilot-chat/src/platform/log/common/logService.ts`

---

## 关键代码片段

### 1. Stream Chunk 处理位置 (`for await` 循环)

**位置**: `SSEProcessor.processSSEInner()` 方法，约第 265 行

```typescript
// Iterate over arbitrarily sized chunks coming in from the network.
for await (const chunk of this.body) {
    if (this.maybeCancel('after awaiting body chunk')) {
        return;
    }

    // this.logService.public.debug(chunk.toString());  // <-- 已被注释掉的 debug 日志！
    const [dataLines, remainder] = splitChunk(extraData + chunk.toString());
    extraData = remainder;
    // ...
}
```

**🎯 日志添加点 #1**: 在 `for await` 循环内部，`chunk` 变量包含原始的网络数据块。可以取消注释或添加：
```typescript
this.logService.debug(`[SSE RAW CHUNK] ${chunk.toString()}`);
```

### 2. Token/Content 提取位置

**位置**: `APIJsonDataStreaming.append()` 方法，约第 32-47 行

```typescript
append(choice: ExtendedChoiceJSON) {
    if (choice.text) {
        const str = APIJsonDataStreaming._removeCR(choice.text);
        this._text.push(str);
        this._newText.push(str);
    }
    if (choice.delta?.content) {
        const str = APIJsonDataStreaming._removeCR(choice.delta.content);
        this._text.push(str);
        this._newText.push(str);
    }
    if (choice.delta?.function_call && (choice.delta.function_call.name || choice.delta.function_call.arguments)) {
        const str = APIJsonDataStreaming._removeCR(choice.delta.function_call.arguments);
        this._text.push(str);
        this._newText.push(str);
    }
}
```

**🎯 日志添加点 #2**: 这里是 token 内容实际提取的地方。每次 `append` 调用对应一个 token。

### 3. 单个 Choice 日志（现有）

**位置**: `SSEProcessor.logChoice()` 方法，约第 530 行

```typescript
private logChoice(choice: ExtendedChoiceJSON) {
    const choiceCopy: any = { ...choice };
    delete choiceCopy.index;
    delete choiceCopy.content_filter_results;
    delete choiceCopy.content_filter_offsets;
    this.logService.trace(`choice ${JSON.stringify(choiceCopy)}`);  // <-- 现有 trace 日志
}
```

**调用位置**: 在 `processSSEInner` 的 choice 循环中：
```typescript
for (let i = 0; i < json.choices.length; i++) {
    const choice = json.choices[i];
    this.logChoice(choice);  // <-- 每个 choice 都会调用
    // ...
}
```

**💡 已存在 debug 机制**: 使用 `LogLevel.Trace` 级别记录每个 choice。

### 4. Solution Flush 位置

**位置**: `emitSolution` 内部函数，约第 335 行

```typescript
const emitSolution = async (delta?: {...}) => {
    // ...
    finishOffset = await finishedCb(solution.text.join(''), choice.index, {
        text: solution.flush(),  // <-- 获取增量 token 文本
        // ...
    });
    // ...
};
```

**🎯 日志添加点 #3**: `solution.flush()` 返回自上次 flush 以来累积的新 token 文本。

---

## 现有 Debug 日志

| 方法 | 日志级别 | 内容 | 状态 |
|------|----------|------|------|
| `processSSEInner` | debug | `chunk.toString()` | **已注释掉** |
| `logChoice` | trace | `choice JSON` | ✅ 启用 |
| `maybeCancel` | debug | 取消描述 | ✅ 启用 |
| `processSSE` finally | info | `request done: requestId: [...]` | ✅ 启用 |

---

## LogService 级别控制

**文件**: `atelia-copilot-chat/src/platform/log/common/logService.ts`

```typescript
export enum LogLevel {
    Off = 0,
    Trace = 1,   // 最详细
    Debug = 2,
    Info = 3,
    Warning = 4,
    Error = 5
}
```

**ConsoleLog 默认行为**:
```typescript
export class ConsoleLog implements ILogTarget {
    constructor(private readonly prefix?: string, private readonly minLogLevel: LogLevel = LogLevel.Warning) { }
    
    logIt(level: LogLevel, metadataStr: string, ...extra: any[]) {
        // 只有 Error 和 Warning 会输出到 console
        // Trace/Debug/Info 不会输出到 console，但会存入 LogMemory
    }
}
```

**⚠️ 注意**: 默认情况下 `ConsoleLog` 只输出 Warning 和 Error 级别！要看到 Trace/Debug，需要：
1. 修改 `minLogLevel` 参数
2. 或者检查 VS Code 的 Output Channel（`LogOutputChannel`）

---

## 建议的日志添加位置

### 位置 1: 原始 Chunk（最底层）
```typescript
// 在 processSSEInner 的 for await 循环内，约第 267 行
for await (const chunk of this.body) {
    if (this.maybeCancel('after awaiting body chunk')) { return; }
    
    // 添加这行：
    this.logService.debug(`[SSE CHUNK] len=${chunk.length} raw=${chunk.toString().substring(0, 200)}...`);
    
    const [dataLines, remainder] = splitChunk(extraData + chunk.toString());
    // ...
}
```

### 位置 2: 解析后的 Token（推荐）
```typescript
// 在 APIJsonDataStreaming.append() 内部，需要传入 logService
// 或者在 SSEProcessor 中，solution.append(choice) 之后：
solution.append(choice);
this.logService.debug(`[TOKEN] idx=${choice.index} content="${choice.delta?.content ?? choice.text ?? ''}" flush="${solution.flush()}"`);
```

### 位置 3: 使用现有的 logChoice（已存在）
```typescript
// logChoice 已经记录了完整的 choice JSON
// 将 trace 改为 debug 或 info 即可看到更多输出
this.logService.debug(`choice ${JSON.stringify(choiceCopy)}`);
```

---

## 环境变量/开关

**❌ 当前没有发现专用的环境变量或开关来启用 SSE debug 模式。**

**可能的启用方式**:
1. VS Code 扩展设置：Developer: Set Log Level
2. 修改 `ConsoleLog` 构造时的 `minLogLevel` 参数
3. 通过代码添加条件判断：
   ```typescript
   const SSE_DEBUG = process.env.SSE_DEBUG === 'true';
   if (SSE_DEBUG) {
       this.logService.info(`[SSE DEBUG] ...`);
   }
   ```

---

## Porter/QA 建议

### 对 Atelia 项目
1. **取消注释 `chunk.toString()` 日志**：最快的方式是取消第 267 行的注释
2. **添加环境变量控制**：建议添加 `COPILOT_SSE_DEBUG` 环境变量
3. **LogLevel 控制**：考虑在开发模式下将 ConsoleLog 的 minLogLevel 设为 Trace

### 日志输出查看
- VS Code: View → Output → 选择 "GitHub Copilot Chat" 频道
- 设置 Developer: Set Log Level 为 Trace 或 Debug

---

## 附录：关键数据流

```
Network
   ↓
[for await (chunk of body)]  ← 日志点 #1: 原始 chunk
   ↓
splitChunk() → dataLines[]
   ↓
JSON.parse(lineWithoutData) → json.choices[]
   ↓
[for (choice of choices)]
   ├→ logChoice(choice)      ← 现有 trace 日志
   ↓
solution.append(choice)       ← 日志点 #2: token 提取
   ↓
emitSolution() → finishedCb(solution.flush())  ← 日志点 #3: token 发送
   ↓
yield FinishedCompletion
```

---

**调查完成时间**: 2025-12-02
**调查员**: InvestigatorTS
