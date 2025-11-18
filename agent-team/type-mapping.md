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
| `SENTINEL` singleton | `static readonly PieceTreeNode Sentinel` | Invariant: `Parent/Left/Right` self-reference so null checks stay branchless; Edge: `rbDelete` mutates sentinel links, so reset logic must run after every delete; TODO Porter: allocate one sentinel per tree instance to keep metadata local. |
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
| `LineStarts` + `createLineStartsFast` (`pieceTreeBase.ts`) | `sealed class LineStartsInfo { int[] Starts; int CR; int LF; int CRLF; bool IsBasicAscii; }` | Invariant: `Starts[0] = 0` and `Starts` length equals number of line breaks + 1; Edge: CR-only and CRLF sequences tracked separately for telemetry; QA: include files with only `\r`, mixed EOLs, and non-ASCII to confirm `IsBasicASCII`; TODO Porter: provide fast-path builder for change buffer just like TS (no allocations when appending). |
| `PieceTreeSearchCache` | `struct NodeSearchCache { int Limit; CacheEntry[] Entries; }` | Invariant: caches last traversed node by offset and line; Edge: invalidated when edits touch offsets >= cached start; QA: add perf-oriented tests to ensure cache hits survive inserts preceding cached nodes; TODO Porter: start with `Limit=1` (TS default) and expose config once profiler data exists. |
| `PieceTreeSnapshot` (`ITextSnapshot`) | `PieceTreeSnapshot : ITextSnapshot` | Invariant: captures BOM + in-order piece list; Edge: snapshot assumes `TreeNode.piece` immutable during read, so the C# port must clone or freeze nodes before multi-threaded use; QA: simulate concurrent edits by interleaving snapshot enumeration with inserts (should still produce pre-edit content). |
| `LineFeedCounter` (`textModelSearch.ts`) | `LineFeedCounter` helper | Invariant: precomputes LF offsets to adjust CRLF ranges when multiline regex runs against LF-normalized text; Edge: only used when buffer EOL is CRLF; QA: tests for multiline regex hits crossing 
 boundaries; TODO Porter: this helper feeds PT-005 QA matrix for regex coverage—mark as TBD if regex instrumentation slips to PT-007. |

## Diff Summary
- Replaced the single-row table with sprint-aligned sections for PieceSegment, PieceTreeNode, SearchContext, BufferRange, and helper structs so each PT-004/005 consumer can lift the relevant contract quickly.
- Added invariants, risky edge cases, QA prompts, and explicit TODOs (Porter stubs + WordSeparators TBD) per row to satisfy Sprint 00 review criteria.
- Flagged remaining unknowns (WordCharacterClassifier mapping, multiline regex instrumentation) so Info-Indexer and Planner can track blockers while Porter proceeds with skeleton work.
