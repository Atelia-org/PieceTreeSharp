# Phase 3: Diffing & Decorations

**Goal**: Implement text comparison (Diff) and a basic Decoration system to support the "DocUI" vision (rendering overlays like cursors/selections as Markdown).

## Current Status
- **Phase 1 (Core)**: Complete.
- **Phase 2 (TextModel & Interaction)**: Complete.
- **Phase 3 (Diffing & Decorations)**: **In Progress**.

## Tasks

### Analysis & Design
- [ ] **DF-001**: Analyze Diff & Decoration dependencies & update Type Mapping.
    - Review `MyersDiffAlgorithm` in `ts/src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/myersDiffAlgorithm.ts`.
    - Review `IntervalTree` in `ts/src/vs/editor/common/model/intervalTree.ts`.
    - Update `agent-team/type-mapping.md` with new types (`IDiffAlgorithm`, `ISequence`, `IntervalNode`, `ModelDecorationOptions`).

### Implementation: Diffing
- [ ] **DF-002**: Port `DiffComputer` / `DiffChange` (Basic line/char diff).
    - Implement `ISequence` interface for text models.
    - Implement `MyersDiffAlgorithm` (O(ND) diffing).
    - Create `DiffComputer` to drive the diffing process.

### Implementation: Decorations
- [ ] **DF-003**: Implement `ModelDecoration` structure and storage.
    - Implement `IntervalNode` with metadata bitmask (color, stickiness).
    - Implement `IntervalTree` (Red-Black tree) for efficient range storage.
    - Integrate `IntervalTree` into `TextModel`.
- [ ] **DF-004**: Implement `TextModel.GetDecorationsInRange`.
    - Add methods to query decorations overlapping a range.
    - Ensure efficient querying using the interval tree.

### Prototyping
- [ ] **DF-005**: Prototype `MarkdownRenderer`.
    - Create a utility to take a `TextModel` and a set of `Decorations`.
    - Render the text with decorations applied as Markdown (e.g., bold, highlights, or custom markers).
    - This supports the "DocUI" vision.

### Documentation & Misc
- [ ] **OI-008**: Update Architecture docs with Diff & Decoration details.
- [ ] **OI-009**: Create "How to use Decorations" guide for sub-agents.

## Reference & Logs
- Previous Phase: [Phase 2 Archive](agent-team/task-board-v2-archive.md)
- Core Docs Index: [indexes/core-docs-index.md](indexes/core-docs-index.md)
