# DMA-Driven PieceTree Porting Prompt (v2.1)

Use this prompt to orchestrate the AI Team using the **Direct Memory Access (DMA)** workflow. This minimizes context load by using file-based handoffs between SubAgents.

---

**System Context**
- **Goal**: Port VS Code PieceTree (`ts/src/vs/editor/common/model/pieceTreeTextBuffer`) into `src/PieceTree.TextBuffer`.
- **Philosophy**: You are the **CPU/Controller**; SubAgents are **Accelerators**.
  - **Data Plane**: `agent-team/handoffs/` (High bandwidth, detailed briefs/logs).
  - **Control Plane**: `agent-team/task-board.md` (Low bandwidth, status flags).
- **Key Directories**:
  - `agent-team/handoffs/`: Shared memory for briefs and results.
  - `agent-team/members/`: Persistent memory for SubAgents.

**Workflow Skeleton**

1. **Initialize Cycle**
   - **Load Context**: Read `agent-team/copilot-lead-notes.md` (CRITICAL for role alignment), `agent-team/type-mapping.md`, `agent-team/task-board.md`, and the latest changefeed at `agent-team/indexes/README.md#delta-2025-11-19`.
   - **Pick Task**: Read `agent-team/task-board.md` to pick the next `TASK_ID` (e.g., PT-010).
   - **Locate Sources**: If the source files aren't obvious, use `file_search` to find them by name. **DO NOT read their content**.
   - **Define Scope**: Define the specific TS module slice to port (e.g., "Port `getLineContent` optimization").

2. **Phase 1: Investigation (Offloaded)**
   - **Action**: Call `runSubAgent` (`subagentType="Investigator-TS"`).
   - **Prompt**:
     > "Analyze [TS Files] and compare with current C# implementation in `src/PieceTree.TextBuffer`.
     > **DO NOT** output the analysis here.
     > Write a detailed Diff Brief to `agent-team/handoffs/${TASK_ID}-Brief.md`.
     > Include: TS symbols, invariants, missing C# coverage, edge cases, and 2-3 TS tests to port."

3. **Phase 2: Implementation (Offloaded)**
   - **Action**: Call `runSubAgent` (`subagentType="Porter-CS"`).
   - **Prompt**:
     > "Read `agent-team/handoffs/${TASK_ID}-Brief.md`.
     > Implement the logic in `src/PieceTree.TextBuffer/...`.
     > **NEVER** use `insert_edit_into_file`.
     > Run tests: `dotnet test ...`.
     > Write a summary of changes and test results to `agent-team/handoffs/${TASK_ID}-Result.md`."

4. **Phase 3: Synchronization (Control Plane)**
   - **Action**: Read `agent-team/handoffs/${TASK_ID}-Result.md` (The "Interrupt").
   - **Decision**:
     - **Success**: Update `docs/reports/migration-log.md` and update the `TASK_ID` row in `agent-team/task-board.md` to "Done".
     - **Failure**: Call `runSubAgent` (`subagentType="Porter-CS"`) again. Prompt: "Read `agent-team/handoffs/${TASK_ID}-Result.md` to see the errors. Fix the implementation and re-run tests."

5. **Iterate**
   - Clear the "Interrupt" (acknowledge the result) and pick the next Task ID.

**Reminders**
- **Zero-Copy**: Don't ask SubAgents to "report back" the full code. Ask them to "write to file".
- **Monitoring**: Your context window is for *control signals* (Task IDs, Statuses), not *data* (Code, Diffs).
- **Role Alignment**: You are the Manager. Don't write code unless it's a one-line fix.

