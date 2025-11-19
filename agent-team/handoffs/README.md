# Handoffs Directory (Shared Memory)

This directory serves as the **Direct Memory Access (DMA)** buffer for the AI Team.
SubAgents use this space to pass high-bandwidth information (Diff Briefs, Test Logs, Implementation Plans) to each other without burdening the Main Agent's context window.

## Protocol
1.  **Naming Convention**: `<TaskID>-<Stage>-<Role>.md`
    *   Example: `PT-010-Brief-Investigator.md`
    *   Example: `PT-010-Result-Porter.md`
2.  **Lifecycle**: Files here are transient. They can be archived to `docs/reports/` if valuable, or overwritten in the next cycle.
3.  **Format**: Markdown is preferred for readability.

## Usage
- **Main Agent**: Assigns a Task ID and directs SubAgents to read/write specific files here.
- **Investigator**: Reads source code (via tools), writes analysis to `*-Brief.md`.
- **Porter**: Reads `*-Brief.md`, writes code, appends validation logs to `*-Result.md`.
