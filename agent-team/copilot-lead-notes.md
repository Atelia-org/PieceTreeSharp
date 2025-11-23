# Copilot Lead Notes

## Tooling Understanding
- **DMA (Direct Memory Access) Workflow**: The "Zero-Copy" approach is highly effective.
  - **Data Plane**: SubAgents write heavy content (Briefs, Diffs, Results) to `agent-team/handoffs/`. I only read these to verify completion, not to load code into my context.
  - **Control Plane**: I act as the CPU, managing `task-board.md` and triggering `runSubAgent`. My context is kept light (Status, Logs, Plans).
- `runSubAgent` is a powerful "Accelerator". By giving them a specific role (Investigator/Porter) and a file-based interface, they perform complex tasks autonomously.
- **Memory Persistence**: `agent-team/members/` works well for role definition. `handoffs/` works well for task-specific context.

## Team Strategy
- **The "Interrupt" Pattern**: SubAgents write a Result file. I read it. If it's good, I clear the interrupt (update Task Board). If bad, I re-queue the task.
- **Role Specialization**:
  - `Investigator-TS`: Reads TS, writes Briefs. No C# coding.
  - `Porter-CS`: Reads Briefs, writes C#. No TS analysis (relies on Brief).
  - `QA-Automation`: Verifies the Result against the Brief.
- **Info-Indexer Loop**: The `migration-log.md` + `indexes/README.md` changefeed is crucial for keeping the "Context Window" of future sessions clean.

## Immediate Plans
1. **Consolidate PT-004/PT-005**: With PT-010 (Normalization) done, the core PieceTree is likely feature-complete for a "v1".
   - Action: Review `src/TextBuffer` against `PT-004` scope. If `Insert/Delete/Search/Snapshot/Normalize` are present, mark PT-004 Done.
   - Action: Review `TestMatrix.md` against current 23 tests. If coverage is sufficient for v1, mark PT-005 Done.
2. **Next Phase Planning**:
   - Identify what's needed to make this a usable "TextBuffer" for the LLM DocUI.
   - Likely candidates: `TextModel` (events, version id), `Cursor/Selection` logic (if not part of view), or `Diff` algorithm.
3. **Maintain Discipline**: Continue using the DMA prompt. Do not fall back to reading code directly unless debugging a stall.

## runSubAgent Meeting Experience
- **File-Based Handoffs**: Passing `handoffs/PT-xxx-Brief.md` to the Porter is much better than pasting text in the prompt.
- **Explicit "Files to Update"**: Telling SubAgents exactly which file to write prevents them from outputting to the chat (which wastes my tokens).
