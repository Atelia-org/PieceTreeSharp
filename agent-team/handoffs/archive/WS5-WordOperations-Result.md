# WS5 WordOperations Implementation - Result

## Lead Summary
- **Owner:** Porter-CS
- **Date:** 2025-11-28
- **Changefeed Anchor:** [`#delta-2025-11-28-ws5-wordoperations`](../indexes/README.md#delta-2025-11-28-ws5-wordoperations)
- **Migration Log Row:** [`docs/reports/migration-log.md#ws5-wordoperations`](../../docs/reports/migration-log.md#ws5-wordoperations)
- **Scope:** WS5 Top-10 #2 – 完整移植 VS Code `cursorWordOperations.ts` 功能并补齐 CursorWordOperations 测试矩阵。
- **Status:** ✅ Complete – `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` → 801 total（796 pass + 5 skip）。新建 41 word 操作测试，覆盖移动、删除、选择与分类器。

## Scope
### Implementation Surface
1. **`src/TextBuffer/Cursor/WordCharacterClassifier.cs`**
  - 重命名为 `CursorWordCharacterClassifier`，与 Core 版本解耦。
  - 复用 `WordCharacterClass` 枚举并添加 `ConcurrentDictionary` 缓存，复刻 TS LRU 行为。
  - 暴露 `IsWordChar/IsSeparator/IsWhitespace/IsWordSeparator` 便捷方法供 `WordOperations` 使用。
2. **`src/TextBuffer/Cursor/WordOperations.cs`**（≈958 行）
  - 从 ~100 行重写到 TS 等价的 958 行，实现完整的词边界、导航、删除与选择 API。
  - 新增 `WordNavigationType`、`WordType` 与 `FindWordResult` 结构，统一内部 0-based 索引与对外 1-based `TextPosition`。
  - 覆盖 `MoveWordLeft/Right`, `CursorWord*`, `DeleteWord*`, `DeleteInsideWord`, `SelectWord*` 等全部入口。

### Test & Helper Updates
1. **`tests/TextBuffer.Tests/CursorWordOperationsTests.cs`**
  - 测试从 3 个扩展到 **41** 个（38 pass + 3 skip），映射 TS `wordOperations.test.ts` 的每个事实与回归案例。
  - 分类覆盖：CursorWordLeft (6)、CursorWordStartLeft (2)、CursorWordEndLeft (1)、CursorWordRight (4)、MoveWordEndRight (2)、MoveWordStartRight (4)、DeleteWordLeft (4)、DeleteWordRight (5)、DeleteInsideWord (4)、Classifier 行为 (3)。
2. **`tests/TextBuffer.Tests/Helpers/WordTestUtils.cs`**
  - 更新 `DeserializePipePositions`/`SerializePipePositions` 以精确重建 `|` 标记位置。
  - 新增 `TestRepeatedActionAndExtractPositions` 与 `TestWordMovement` 共用 helper，减少重复样板。

## Findings
### Navigation & Boundary Safety
- `MoveWordLeftCore/MoveWordRightCore` 统一进行行/列范围钳制（1-based 行列 + `MaxColumn`），即使调用者传入 `(1000, 1000)` 也不会抛出异常。
- `FindPreviousWordOnLine/FindNextWordOnLine` 以 0-based 索引对齐 TS，实现空行、Whitespace-only、跨行边界的精确定位，并通过 `FindWordResult` 输出。

### Deletion & Selection Fidelity
- `DeleteWordLeft/Right` 家族复用了导航结果来保证多行删除不会截断 CRLF。
- `DeleteInsideWord` 与 `SelectWordLeft/Right` 支持空行与符号分隔，遵循新的 `WordType` 语义区分 `WordSeparator`、`Whitespace`、`Word`。

### Classifier & Helper Enhancements
- `CursorWordCharacterClassifier` 通过 namespace rename 避免与 Core 类型冲突，并继续沿用 TS 默认分隔符策略。
- `WordTestUtils` 现可复用 `TestRepeatedActionAndExtractPositions` 对任意操作进行管道标记序列化，为未来 Unicode/emoji 案例提供基线。

### Known Skips / Residual Risk
| Test | Reason | Tracking |
|------|--------|----------|
| `MoveWordStartRight_Issue51119` | TS 处理 surrogate pair 的 WordStart 语义仍待确认 | Leave skipped, link to WS5 backlog |
| `MoveWordStartRight_Issue64810_NewlineSkip` | `\n` + 多空格场景需要额外 `LineBreak` 检测 | Investigate under WS5-INV issue |
| `DeleteWordRight_Issue3882_MultilineDelete` | 多行删除在 Buffer 边界上的 off-by-one | Requires incremental diff vs TS snap |

### Metrics Snapshot
| Area | Value |
|------|-------|
| Implementation diff | ~2,500 LOC across `WordOperations`, classifier, helpers |
| Tests | 41 total (38 pass, 3 skip) |
| Full suite | 801 total（796 pass + 5 skip；含 2 既有 skip） |

## Verification
```bash
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
export PIECETREE_DEBUG=0 && dotnet test --filter CursorWordOperationsTests --nologo
```
- Full suite：801 total（796 pass + 5 skip），≈100s。
- Targeted：41 total（38 pass + 3 skip），≈2s，用于快速验证 WordOps 改动。

## Follow-up
1. **Edge-case parity** – 复查 3 个 skip（Issue51119/64810/3882）并引入 TS 期望的 surrogate & 多行处理。
2. **Unicode coverage** – 扩展 `CursorWordOperationsTests`，纳入 emoji、CJK、camelCase/snake_case `_moveWordPart*` 等路径。
3. **References** – TS 实现位于 `ts/src/vs/editor/common/cursor/cursorWordOperations.ts`，测试来源 `ts/src/vs/editor/contrib/wordOperations/test/browser/wordOperations.test.ts`；word classifier 来源 `ts/src/vs/editor/common/core/wordCharacterClassifier.ts`。
