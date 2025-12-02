# Type Mapping (TS → C#)

Sprint 00 (PT-003) aims to unblock Porter-CS before 2025-11-20 by locking down the PieceTree core, search hooks, and range primitives. Tables below capture the current TypeScript contracts and the proposed C# counterparts, with notes that highlight invariants, risky edges, QA hooks, and TODOs for Porter.

## PieceSegment

| TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- |
| `Piece` (ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts) | `struct PieceSegment { int BufferId; BufferCursor Start; BufferCursor End; int LineFeedCount; int Length; }` | Invariant: `Length == Offset(End) - Offset(Start)` and `LineFeedCount` mirrors `getLineFeedCnt`; Edge: CR/LF pairs can straddle nodes so `PieceSegment` must expose helpers akin to `startWithLF/endWithCR`; QA: exercise insert/delete around `\r\n`, surrogate pairs, and >64K payload splits (`AverageBufferSize` = 65535); TODO Porter: keep `BufferId=0` reserved for the mutable change buffer and recycle read-only chunks to avoid string copies. |
| `BufferCursor` (`line`,`column`) | `struct BufferCursor { int Line; int Column; }` | Invariant: 0-based coordinates within the owning buffer (`lineStarts[line] + column` equals absolute offset); Edge: reused objects during hot paths (`positionInBuffer`) so prefer stack structs; QA: verify conversions near CRLF boundaries and final line without newline. |
| `StringBuffer` (`buffer`, `lineStarts`) | `sealed class ChunkBuffer` | Invariant: `lineStarts.Length == lineFeedCount + 1`; Edge: `createLineStartsFast` swaps between `Uint16Array` and `Uint32Array` after column 65535, so C# port should auto-upcast to `int[]`; QA: cover `normalizeEOL` re-chunking and ensure BOM handling stays external; TODO Porter: expose factory that accepts precomputed line starts to avoid recomputation in PT-004 builder. |

## PieceTreeNode & Balancing Metadata

| TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- |
| `TreeNode` (`rbTreeBase.ts`) | `sealed class PieceTreeNode` | Invariant: red-black tree node storing `PieceSegment` plus metadata (`SizeLeft`, `LineFeedLeft`); Edge: nodes may temporarily hold zero-length pieces during CRLF fixes, so deletion must tolerate empty payloads; QA: assert metadata sums after random insert/delete batches; TODO Porter: implement `Next()`/`Prev()` helpers mirroring TS inorder traversal for iterators and snapshots. |
| `NodeColor` enum | `enum NodeColor { Black = 0, Red = 1 }` | Invariant: sentinel always black; ensure rotations recolor per RB rules; QA: unit tests covering consecutive inserts plus delete-from-root scenarios. |
| `SENTINEL` (per model) | `PieceTreeNode.CreateSentinel()` → `_sentinel` field on `PieceTreeModel` | `#delta-2025-11-24-b3-sentinel`: every model now owns its sentinel, keeping `Parent/Left/Right` self-referential without leaking metadata across trees while delete fixups temporarily reuse sentinel links. |
| `leftRotate/rightRotate`, `fixInsert`, `rbDelete`, `updateTreeMetadata`, `recomputeTreeMetadata` | `static class PieceTreeBalancer` | Invariant: `SizeLeft` and `LineFeedLeft` must be recomputed whenever left subtrees change; Edge: `rbDelete` calls `calculateSize/LF` when removing the in-order successor, so avoid O(n) scans by caching subtree totals; QA: add regression tests that validate offsets after rotations plus CRLF fix-ups; TODO Porter: instrument internal asserts to catch negative metadata early. |

## SearchContext & Searcher

| TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- |
| `SearchParams` + `SearchData` (`textModelSearch.ts`) | `record SearchContext { Regex Pattern; WordClassifier? WordSeparators; string? SimpleSearch; bool IsMultiline; }` | Invariant: `Pattern` is compiled with global+unicode flags and reused; Edge: TS demotes to string search when `isRegex=false` and string lacks casing ambiguity, so the C# port should mirror that optimization; QA: cover invalid regex inputs and multiline vs non-multiline conversions; TODO Porter: decide how to represent `WordCharacterClassifier` in .NET (TBD until PT-007 defines shared word-separator tables). |
| `Searcher` (used by `PieceTreeBase.findMatches*`) | `class PieceTreeSearcher` | Invariant: `reset(lastIndex)` must run before each traversal; Edge: zero-length matches advance manually via `strings.getNextCodePoint` to avoid infinite loops—C# implementation must inspect surrogate pairs similarly; QA: add tests for empty-pattern behavior and boundary-limited `limitResultCount`; TODO Porter: expose `Next(string text)` that enforces the same word-boundary checks even while `WordSeparators` mapping is pending (stub allowed but mark TODO in PT-004). |
| `FindMatch` / `createFindMatch` | `struct FindMatch { TextRange Range; string[]? Captures; }` | Invariant: `Captures` only populated when `captureMatches` true; Edge: `Range` references user-visible coordinates so CRLF normalisation must already be applied; QA: ensure capture arrays align with JS regex groups when .NET regex differs (documented delta acceptable if flagged as TBD). |

## BufferRange & Range Mapping

| TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- |
| `Position` (`core/position.ts`) | `readonly struct Position { int LineNumber; int Column; }` | Invariant: 1-based line/column; Edge: `.with`/`.delta` clamp to 1 in TS, so mirror guard clauses; QA: verify round-trip between offsets and positions at document edges. |
| `Range` (`core/range.ts`) | `readonly struct TextRange { Position Start; Position End; }` | Invariant: constructor swaps endpoints to enforce `Start <= End`; Edge: functions like `containsRange` assume inclusive end, so keep semantics when exposing to .NET callers; QA: include multi-line, touching, and empty ranges. |
| `NodePosition` (`pieceTreeBase.ts`) | `struct NodeHit { PieceTreeNode Node; int Remainder; int NodeStartOffset; }` | Invariant: `0 <= Remainder <= Node.Piece.Length` and `NodeStartOffset` is absolute document offset; Edge: reused by `nodeAt`/`nodeAt2`, so ensure pooling or stack allocation; QA: tests for lookups near CRLF boundaries and at document end; TODO Porter: expose both offset- and line-based resolvers to avoid recomputing inside getters. |
| `PieceTreeBase.getOffsetAt / getPositionAt / getValueInRange` | `PieceTreeBuffer` methods | Invariant: rely on `SizeLeft`/`LineFeedLeft` being accurate; Edge: `getValueInRange` normalizes EOL when requested, so C# port must preserve the `eol` parameter semantics; QA: compare outputs between TS and C# for randomly generated ranges (fuzz). |

## Helper Structs for PT-004/005

| TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- |
| `PieceTreeSnapshot` (`ITextSnapshot`) | `PieceTreeSnapshot : ITextSnapshot` | Invariant: captures BOM + in-order piece list; Edge: snapshot assumes `TreeNode.piece` immutable during read, so the C# port must clone or freeze nodes before multi-threaded use; QA: simulate concurrent edits by interleaving snapshot enumeration with inserts (should still produce pre-edit content). |
| `LineFeedCounter` (`textModelSearch.ts`) | `LineFeedCounter` helper | Invariant: precomputes LF offsets to adjust CRLF ranges when multiline regex runs against LF-normalized text; Edge: only used when buffer EOL is CRLF; QA: tests for multiline regex hits crossing 
 boundaries; TODO Porter: this helper feeds PT-005 QA matrix for regex coverage—mark as TBD if regex instrumentation slips to PT-007. |

## Line Infrastructure

| TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- |
| `createLineStartsFast` / `createLineStarts` (ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts) | `LineStartBuilder.Build(string text, bool readonlyFlag)` → produces `LineStartTable` (new static helper under `PieceTree.TextBuffer.Core`) | Invariant: entry `0` must exist and array length equals `lineFeedCnt + 1`; CR/LF/CRLF counters plus `isBasicASCII` flag accompany the offsets so builders can decide when to upgrade typed arrays; C# currently recomputes via `ChunkBuffer.ComputeLineStarts` but drops the counters—Porter needs a shim that returns `int[]` plus metadata to avoid double scans and to emulate Uint16→Uint32 promotion. |
| `StringBuffer` (ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts) | `ChunkBuffer` (`src/TextBuffer/Core/ChunkBuffer.cs`) | Invariant: `lineStarts[last] + column == buffer.length` for each piece; C# version already keeps immutable text + `int[]` starts but lacks stored CR/LF telemetry and flags like `containsRTL` / `containsUnusualLineTerminators`; builder must extend `ChunkBuffer` so Porter can surface BOM + ASCII hints just like TS `StringBuffer`. |
| `PieceTreeSnapshot implements ITextSnapshot` (ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts) | `PieceTreeSnapshot : ITextSnapshot` (new, under `PieceTree.TextBuffer`) | Invariant: snapshot captures ordered list of `Piece` instances and replays BOM exactly once; pieces must be treated as immutable during enumeration; C# layer currently lacks this type so Porter needs a read-only iterator over `PieceTreeModel.EnumeratePiecesInOrder()` plus a guard so normalization doesn’t mutate while snapshots stream. |
| `PieceTreeSearchCache` (ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts) | `PieceTreeSearchCache` (new struct caching last node by offset/line) | Invariant: cache only holds nodes that remain attached (`parent !== null`) and entries invalidate whenever edits land at or before cached offsets; not yet present in C#, so random line fetches currently walk the tree each time—Porter should port this LRU (limit=1 default) to keep parity with TS perf characteristics. |

## Search Helpers

| TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- |
| `TextModelSearch` (ts/src/vs/editor/common/model/textModelSearch.ts) | `PieceTreeSearchService` (new façade bridging `PieceTreeModel` and search callers) | Invariant: multiline searches operate on LF-normalized strings and adjust offsets back to CRLF via `LineFeedCounter`; C# needs a service that can accept `SearchParams`-like inputs and delegate to either multiline or per-line scans on `PieceTreeModel`, otherwise PT-005 cannot validate `findMatches` parity. |
| `SearchParams` (`textModelSearch.ts`) | `SearchContext` (`PieceTree.TextBuffer.Search` namespace) | Invariant: empty `searchString` returns null; string-based search path only allowed when regex=false and query lacks newline; existing `SearchContext` record in mapping captures partial data but C# still lacks enforcement of `isRegex`/`matchCase` toggles—Porter should add validation + `.Parse()` static just like TS `parseSearchRequest`. |
| `Searcher` (`textModelSearch.ts`) | `PieceTreeSearcher` (new, wraps `System.Text.RegularExpressions.Regex`) | Invariant: tracks `_prevMatchStartIndex`/`_prevMatchLength` to prevent zero-length loops, respects word separators when provided, and exposes `reset(lastIndex)` before each scan; C# needs equivalent stateful matcher plus shim for surrogate-pair advancement (`strings.getNextCodePoint`); no implementation exists yet. |
| `createFindMatch` + `FindMatch` (ts/src/vs/editor/common/model/textModelSearch.ts & model.js) | `FindMatchFactory` → `FindMatchDto` (struct storing `TextRange` + captures) | Invariant: capture arrays only materialize when `captureMatches=true`; `Range` must already be normalized; C# can reuse the planned `PieceTreeSearchResult` but must add capture storage so Porter doesn’t drop JS regex semantics. |

## Builder/Normalizer

| TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- |
| `PieceTreeTextBufferBuilder` (ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBufferBuilder.ts) | `PieceTreeBuilder` (`src/TextBuffer/Core/PieceTreeBuilder.cs`) | Invariant: `_acceptChunk2` batches CR/LF counters, ASCII flags, and splits large strings at 64KB without breaking CRLF pairs or surrogate halves; current C# builder lacks chunk splitting, BOM stripping, and metadata accumulation, so Porter must extend it before PT-004 uses the change buffer path. |
| `PieceTreeTextBufferFactory` (same file) | `PieceTreeBuildResult` + future `PieceTreeTextBuffer` factory | Invariant: `_getEOL` picks the dominant line ending (ties resolved via `DefaultEndOfLine`) and optionally normalizes every chunk before instantiating `PieceTreeTextBuffer`; no C# analogue exists, so implementor must store BOM/RTL/ASCII flags and expose `Create(defaultEol)` just like TS. |
| `PieceTreeBase.normalizeEOL` (ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts) | `PieceTreeModel.NormalizeEOL(string targetEol)` / `ChunkUtilities.NormalizeChunks(IEnumerable<string> source, string eol)` | Invariant: flushes ~64KB temp chunks, replaces all `\r\n|\r|\n` with `targetEol`, and rebuilds the tree while toggling `_eolNormalized`; C# uses `PieceTreeModel.NormalizeEOL` (builder rebuild) backed by `ChunkUtilities.NormalizeChunks`. |

## Diff Summary
- Replaced the single-row table with sprint-aligned sections for PieceSegment, PieceTreeNode, SearchContext, BufferRange, and helper structs so each PT-004/005 consumer can lift the relevant contract quickly.
- Added invariants, risky edge cases, QA prompts, and explicit TODOs (Porter stubs + WordSeparators TBD) per row to satisfy Sprint 00 review criteria.
- Flagged remaining unknowns (WordCharacterClassifier mapping, multiline regex instrumentation) so Info-Indexer and Planner can track blockers while Porter proceeds with skeleton work.
- Added dedicated sections for Line Infrastructure, Search Helpers, and Builder/Normalizer to capture the remaining TS contracts (builder metadata, snapshot semantics, search shims) Porter-CS needs before PT-004/005 coding starts.

## Phase 2: TextModel & Interaction

| TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- |
| `TextModel` (`model/textModel.ts`) | `class TextModel` | Invariant: Wraps `PieceTreeTextBuffer`; manages `_versionId` (monotonic) and `_alternativeVersionId` (undo-aware); Edge: `setValue` destroys decorations/undo stack; QA: Verify version increments on edits and undo/redo behavior. |
| `Selection` (`core/selection.ts`) | `readonly struct Selection` | Invariant: `Anchor` (selectionStart) and `Active` (position) define the selection; `Start`/`End` (Range) are derived (normalized); Edge: `Selection` in TS extends `Range`, but C# struct cannot inherit. Composition preferred: `Selection { Position Anchor; Position Active; }`. |
| `Position` (`core/position.ts`) | `readonly struct Position` | Invariant: 1-based line/column. Already defined in Phase 1, but ensure `CompareTo` and equality operators are robust. |
| `IModelContentChangedEvent` (`textModelEvents.ts`) | `class TextModelContentChangedEventArgs : EventArgs` | Invariant: `Changes` ordered reverse-document order (safe to apply); `VersionId` matches model after change; Edge: `IsFlush` indicates total reset; `Eol` change is a separate flag. |
| `SingleCursorState` (`cursorCommon.ts`) | `struct SingleCursorState` | Invariant: Tracks `SelectionStart` (Anchor Range), `Position` (Active), and `LeftoverVisibleColumns` (for vertical navigation); Edge: `SelectionStartKind` (Simple/Word/Line) determines expansion behavior. |

## Phase 2.5: Cursor & Snippet (Sprint 04 M2)

| TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- |
| `CursorCollection` (`cursor/cursorCollection.ts`) | `class CursorCollection` | ✅ 2025-12-02 完成（94 tests）。Invariant: 管理多选区 normalization、二级光标；Edge: cursor merge 需保持 primary cursor 稳定；QA: CursorMultiSelectionTests + CursorTests。 |
| `WordOperations` (`cursor/cursorWordOperations.ts`) | `static class WordOperations` | ✅ 2025-11-28 完成（41 tests）。MoveWordLeft/Right、DeleteWordLeft/Right、DeleteInsideWord、SelectWord 全部实现。 |
| `WordCharacterClassifier` (`cursorCommon.ts`) | `class WordCharacterClassifier` | ✅ 与 WordOperations 一同完成。Invariant: 字符分类器支持自定义分隔符；Edge: CJK + emoji 需要特殊处理。 |
| `SnippetSession` (`snippet/snippetSession.ts`) | `class SnippetSession` | ✅ 2025-12-02 完成（P0-P2 全部，77 tests）。Invariant: 管理 placeholder 遍历和嵌套 snippet；Edge: 空 placeholder 和 final tabstop 处理。 |
| `SnippetController` (`snippet/snippetController.ts`) | `class SnippetController` | ✅ 2025-12-02 完成。负责 snippet 生命周期管理和 decoration 跟踪。 |
| `SnippetVariableResolver` (`snippet/snippetVariables.ts`) | `class SnippetVariableResolver` | ✅ 2025-12-02 新增。Invariant: 解析 `$TM_*`、`$CLIPBOARD` 等内置变量；Edge: 支持自定义变量 resolver 扩展。 |
| `SnippetParser` (`snippet/snippetParser.ts`) | `class SnippetParser` | ✅ 2025-12-02 完成。Invariant: 解析 TextMate snippet 语法；Edge: transform、choice 需完整支持。 |

## Phase 3: Diffing & Decorations

| TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- |
| `IDiffComputer` (`diff/diffComputer.ts`) | `interface IDiffComputer` | Invariant: Computes diffs between two line arrays; Edge: `computeDiff` returns `IDiffResult` containing `DiffChange[]`; C# port should support `pretty` flag for cleanup. |
| `DiffChange` (`base/common/diff/diffChange.ts`) | `struct DiffChange` | Invariant: `originalStart`, `originalLength`, `modifiedStart`, `modifiedLength`; Edge: Represents a single change block; used by `LcsDiff` and `DiffComputer`. |
| `LcsDiff` (`base/common/diff/diff.ts`) | `class LcsDiff` | Invariant: Implements Myers' O(ND) algorithm; Edge: Supports `ContinueProcessingPredicate` for timeout/early exit; Uses `Int32Array` for history to save memory. |
| `IModelDecoration` (`model.ts`) | `class ModelDecoration` | Invariant: `id`, `ownerId`, `range`, `options`; Edge: `range` is dynamic (tracked via `IntervalTree`); `options` define stickiness and rendering. |
| `IntervalTree` (`model/intervalTree.ts`) | `class IntervalTree<T>` | Invariant: Augmented Red-Black Tree; Nodes store `delta` for efficient shifting; `maxEnd` for overlap search; Edge: `T` is `IntervalNode`; C# generic implementation preferred or specialized `DecorationIntervalTree`. |
| `IntervalNode` (`model/intervalTree.ts`) | `class IntervalNode` | Invariant: `start`, `end`, `delta`, `maxEnd`, `metadata` (color, visited, etc.); Edge: `cachedVersionId` for lazy range resolution; `metadata` packs booleans/enums into `int`. |
| `DecorationsTrees` (`model/textModel.ts`) | `class DecorationsTrees` | Invariant: Manages 3 trees (normal, overview, injected); Edge: `deltaDecorations` updates trees; `getDecorationsInRange` queries them; Handles `ownerId` filtering. |
