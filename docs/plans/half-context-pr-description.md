# PR Description: Half-Context Summarization

> **Target**: https://github.com/microsoft/vscode-copilot-chat
> **Branch**: `pr/half-context-summarize`
> **Related Issue**: microsoft/vscode-copilot-release#11103

---

## Title

```
feat(prompts/agent): add experimental half-context conversation summarization
```

## Description

### Summary

Implements an experimental half-context summarization strategy for long conversations. Instead of compressing the entire conversation at once, this feature summarizes only the **first half** of unsummarized rounds while preserving recent context intact.

### Problem

Current full-context summarization can lose important recent context when triggered, causing the agent to:
- Restart understanding the codebase from scratch
- Forget what changes it has already made
- Enter loops re-attempting the same solutions

This issue is tracked in [microsoft/vscode-copilot-release#11103](https://github.com/microsoft/vscode-copilot-release/issues/11103), where users report "all the context it had gathered before summarization is lost."

### Solution

Introduce a more granular approach:
1. **Flatten all rounds** across historical turns and current turn
2. **Skip already-summarized rounds** to avoid redundant compression
3. **Split at midpoint**: summarize older half, keep recent half intact
4. Handle edge cases: interrupted rounds, Turn boundary crossing, fallback to legacy logic

This enables fine-grained compression that can cut through Turn boundaries while preserving recent working context.

### Changes

| File | Change |
|------|--------|
| `summarizedConversationHistory.tsx` | New `getPropsHalfContext()` method, `FlattenedRound` interface |
| `configurationService.ts` | New config key `HalfContextSummarization` |
| `package.json` / `package.nls.json` | Feature flag definition |
| `halfContextSummarization.spec.ts` | 20 unit tests (new file) |

**Total**: ~880 lines added across 5 files

### Feature Flag

```json
{
  "github.copilot.chat.halfContextSummarization": {
    "type": "boolean",
    "default": false,
    "description": "Experimental: Enable half-context summarization that preserves more recent context"
  }
}
```

- **Default: disabled** — Users must opt-in via settings
- Fully backward-compatible with existing behavior when disabled

### Testing

- [x] 20 unit tests covering:
  - Basic split logic (2-6 rounds)
  - Turn boundary crossing scenarios
  - Already-summarized rounds handling
  - Interrupted round protection (maxToolCallsExceeded)
  - Feature flag toggle behavior
  - Fallback to legacy logic
- [x] Manual testing with long agent conversations
- [ ] Simulation tests (need VS Code team to rebuild cache)

### Notes

- **Related Issue**: This addresses user feedback in microsoft/vscode-copilot-release#11103
- **Opus 4.5 Known Issue**: There's a separate issue (#280898) about empty output with tools on Claude Opus 4.5 — this PR does not include workarounds for that
- **Simulation Tests**: Per CONTRIBUTING.md, cache population must be done by VS Code team members

---

## Checklist

- [x] Code compiles without errors
- [x] Unit tests pass (20/20)
- [x] No console.log or debug code
- [x] Feature flag controls the behavior
- [x] DCO sign-off on commits
- [x] Backward compatible (flag=false uses legacy logic)

---

## How to Test

1. Enable the feature flag:
   ```json
   "github.copilot.chat.halfContextSummarization": true
   ```
2. Start a long agent conversation with many tool calls
3. Observe that when summarization triggers, recent context is preserved
4. Verify the agent continues its task without restarting from scratch

---

## Screenshots / Recordings

_(Optional: Add comparison of before/after behavior if helpful)_
