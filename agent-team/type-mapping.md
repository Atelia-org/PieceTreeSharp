# Type Mapping (TS → C#)

| Concept | TypeScript Source | Proposed C# Type | Notes |
| --- | --- | --- | --- |
| Position | `Position` (ts/src/vs/editor/common/core/position.ts) | `struct Position { int LineNumber; int Column; }` | Immutable struct with validation |
| Range | `Range` | `struct TextRange` | Provide helpers for offsets |
| Piece | `Piece` | `PieceSegment` | Already scaffolded under `Core/` |
| StringBuffer | `StringBuffer` | `ChunkBuffer` | Accepts string + line starts |
| TreeNode | `TreeNode` | `PieceTreeNode` | Contains metadata for RB tree |
| Searcher | `Searcher` | `PieceTreeSearcher` | Wrap regex/compiled patterns |

_Add rows as components are ported._
