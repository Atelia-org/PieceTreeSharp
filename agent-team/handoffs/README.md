# Handoffs Directory (Shared Memory)

This directory serves as the **Direct Memory Access (DMA)** buffer for the AI Team.
SubAgents use this space to pass high-bandwidth information (Diff Briefs, Test Logs, Implementation Plans) to each other without burdening the Main Agent's context window.

## Protocol
1.  **Naming Convention**: `<TaskID>-<Stage>-<Role>.md`
    *   Example: `PT-010-Brief-Investigator.md`
    *   Example: `PT-010-Result-Porter.md`
2.  **Lifecycle**: Files here are transient. They can be archived to `archive/` when tasks complete, or overwritten in the next cycle.
3.  **Format**: Markdown is preferred for readability.

## Usage
- **Main Agent**: Assigns a Task ID and directs SubAgents to read/write specific files here.
- **Investigator**: Reads source code (via tools), writes analysis to `*-Brief.md`.
- **Porter**: Reads `*-Brief.md`, writes code, appends validation logs to `*-Result.md`.

## Archive
Completed handoffs are periodically moved to the `archive/` subdirectory. See [`archive/README.md`](archive/README.md) for the index.

### Current Active Files (Phase 7 / Sprint 03)
- **AA4-*** – Alignment & Audit R4 (CL5~CL8)
- **DocFix/DocReview-2025112*** – Recent documentation reviews

### Last Archive Date: 2025-11-26
Archived: Phase 2~6 artifacts, Sprint 02 OI, Sprint 03 Batch #1~#3 completed items.
