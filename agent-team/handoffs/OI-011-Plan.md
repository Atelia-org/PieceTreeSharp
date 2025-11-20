# OI-011 Plan – Info-Indexer: Publish AA4 changefeed

## Overview
This plan instructs Info-Indexer to publish the AA4 changefeed delta after QA verifies CL5 & CL6. The changefeed should point to migration log entries and update AGENTS / Sprint / Task Board references.

## Tasks
1) Review `agent-team/members/info-indexer.md` memory file and append a start-of-task entry.
2) Read the following artifacts:
   - `docs/reports/migration-log.md` (AA4-005 & AA4-006 rows updated)
   - `agent-team/handoffs/AA4-005-Result.md`, `agent-team/handoffs/AA4-006-Result.md`
   - `src/PieceTree.TextBuffer.Tests/TestMatrix.md` (ensure new tests are added)
   - `agent-team/task-board.md` and `docs/sprints/sprint-02.md` to know which tasks to mark done.
3) Create a changefeed entry under `agent-team/indexes/README.md#delta-2025-11-21` referencing the AA4-005 & AA4-006 migration log entries and the QA baseline (105/105).
4) Update `agent-team/task-board.md` to mark AA4-005 & AA4-006 as Done (and annotate the changefeed & baseline counts), and AA4-009 as Done.
5) Update `docs/reports/migration-log.md` to set `Changefeed Entry? = Y` for AA4-005 & AA4-006 rows and include the delta pointer to the generated changefeed.
6) Append `agent-team/handoffs/OI-011-Result.md` summarizing the published delta and the AGENTS/Sprint/Task Board updates.
7) Update Info-Indexer memory file `agent-team/members/info-indexer.md` with the final worklog and the changefeed ID.

## Deliverables
- A new changefeed delta in `agent-team/indexes/README.md` with pointers to the AA4-005/AA4-006 migration log entries.
- `agent-team/handoffs/OI-011-Result.md` summarizing the published delta and listing updated files/links for AGENTS.

