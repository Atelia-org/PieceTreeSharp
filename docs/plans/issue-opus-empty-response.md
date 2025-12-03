# Issue Report: Claude Opus 4.5 Empty Response Bug

> **Status:** Ready to Submit ✅
> **Target:** https://github.com/microsoft/vscode → New Issue (with label request for chat-oss-issue)
> **Date:** 2025-12-03

## Issue Title

`[Bug] Claude Opus 4.5 returns empty response when tools are present in summarization requests (tool_choice: "none")`

## Issue Body (Draft)

### Summary

When using Claude Opus 4.5 for conversation history summarization, the model frequently returns an empty response (8 completion tokens, content: null) when tools are included in the request, even with `tool_choice: "none"`. This bug does not affect other models (GPT-4.1, Claude Sonnet 4.5, Claude Haiku 4.5, Gemini 3 Pro, etc.).

### Environment

- **VS Code Version:** Release version (Windows)
- **Copilot Chat Extension Version:** Release version
- **OS:** Windows / Linux (both affected)
- **Model:** Claude Opus 4.5 (Preview)

### Steps to Reproduce

1. Start a long conversation in Copilot Chat (exceeding ~50% of context limit)
2. Wait for automatic conversation summarization to trigger
3. If using Claude Opus 4.5 as the model, the summarization request fails silently with empty output

**Reproduction rate:** ~66% failure (2 out of 3 attempts in testing)

### Expected Behavior

The summarization request should return a valid conversation summary, similar to other models.

### Actual Behavior

The request completes with:
- `finish_reason: "stop"` (appears successful)
- `completion_tokens: 8` (some internal tokens)
- `content: null` (no actual output)

When it does succeed, the output quality may be degraded (e.g., 128K input compressed to only 1.5K output).

### Reproduction on Stock VS Code

**Confirmed:** This bug reproduces on stock VS Code (Windows release version), not just development builds. Tested 3 times:
- Attempt 1: ❌ Empty output (8 tokens)
- Attempt 2: ❌ Empty output (8 tokens)  
- Attempt 3: ⚠️ Output present but unusually short (1.5K for 128K input)

### Diagnostic Data

#### Failing Request (Claude Opus 4.5 + Tools)
```json
{
  "model": "claude-opus-4.5",
  "requestBody.messages.length": 102,
  "requestBody JSON size": "189,619 bytes",
  "postOptions": {
    "temperature": 0,
    "tool_choice": "none",
    "tools": [/* 62 tool definitions */]
  }
}
```
**Response:**
```json
{
  "completion_tokens": 8,
  "prompt_tokens": 61003,
  "finish_reason": "stop",
  "content": null
}
```

#### Successful Request (Claude Opus 4.5 WITHOUT Tools)
```json
{
  "model": "claude-opus-4.5",
  "requestBody.messages.length": 102,
  "requestBody JSON size": "134,024 bytes",
  "postOptions": {
    "temperature": 0
    // No tools
  }
}
```
**Response:**
```json
{
  "completion_tokens": 2807,
  "prompt_tokens": 45208,
  "finish_reason": "stop",
  "content": "<analysis>...(9228 chars)...</analysis><summary>...</summary>"
}
```

### Comparison with Other Models (All with Tools Injected)

| Model | completion_tokens | Result |
|-------|-------------------|--------|
| claude-opus-4.5 | 8 | ❌ Empty |
| claude-sonnet-4.5 | ~2500 | ✅ Normal |
| claude-haiku-4.5 | ~2000 | ✅ Normal |
| gpt-4.1 | ~2000 | ✅ Normal |
| gpt-5.1-codex | ~2200 | ✅ Normal |
| gemini-3-pro-preview | ~2100 | ✅ Normal |

### Root Cause Analysis

The issue appears to be specific to Claude Opus 4.5's handling of:
1. `tool_choice: "none"` combined with
2. A large number of tool definitions (62 tools)
3. In a summarization context (no tool invocation expected)

The 8 completion tokens suggest the model may be generating internal tokens (possibly `<thinking>` or similar) that get filtered, resulting in empty visible output.

### Workaround

Removing tools from the summarization request (while keeping them for regular chat) resolves the issue.

### Additional Context

This bug was discovered while developing [half-context summarization](https://github.com/Atelia-org/atelia-copilot-chat/tree/feature/half-context-summarize), an experimental feature for better long conversation handling. The bug exists in the upstream summarization flow as well.

---

## Checklist Before Submission

- [x] Reproduce with upstream (full-context) summarization to confirm it's not half-context specific
- [x] Confirm on stock VS Code release (Windows)
- [x] Confirm issue URL format for vscode repo
- [ ] Final review before submission

## Log Files Reference

- `chat-log/half-context-summarize/251203-3a-fail.log` - Real passive summarization failure
- `chat-log/dry-run/251203-4-with-tools-fail.log` - Dry-run with tools (fail)
- `chat-log/dry-run/251203-4-without-tools-success.log` - Dry-run without tools (success)
- `chat-log/half-context-summarize/251205-5-without-tools-success.log` - Real passive success without tools
