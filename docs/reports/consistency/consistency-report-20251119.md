# Consistency Report – 2025-11-19

## Summary
Reviewed `AGENTS.md`, Sprint 00, Sprint OI-01, both 2025-11-19 meeting notes, `agent-team/task-board.md`, `agent-team/main-loop-methodology.md`, `agent-team/ai-team-playbook.md`, and `agent-team/indexes/core-docs-index.md`. Overall scope alignment is intact, but multiple docs either duplicate Task Board content or omit the Info-Indexer changefeed (`agent-team/indexes/README.md#delta-2025-11-19`) that now serves as the canonical delta feed. The table below lists six concrete gaps to unblock OI-001/OI-003/OI-004 follow-ups.

## Findings
| Doc | Issue Type | Details | Action/Owner |
| --- | --- | --- | --- |
| `docs/meetings/meeting-20251119-org-self-improvement.md` | Stale status | Action-items table still marks OI-002 and OI-005 as "Planned" even though Task Board/Sprint mark them Done, and the minutes never point readers to the Info-Indexer changefeed for authoritative deltas. | Update statuses + add "consume `agent-team/indexes/README.md#delta-2025-11-19` before editing" note (DocMaintainer + Info-Indexer, OI-001). |
| `agent-team/task-board.md` | Structural gap | Single table mixes PT/OI work, repeats Sprint descriptions, and stores long Notes instead of pointing to indexes; there is no reminder to ingest the Info-Indexer changefeed before editing rows. | Execute OI-004: split board into Core vs Reference sections, replace verbose Notes with links to `agent-team/indexes/core-docs-index.md`, and add a changefeed checkpoint (DocMaintainer). |
| `agent-team/ai-team-playbook.md` | Missing reference | Core Artifacts omit `agent-team/indexes/` and the changefeed, so onboarding docs never tell contributors to check Info-Indexer outputs before touching AGENTS/Sprints/meetings. | Add an "Indexes & changefeed" bullet referencing `core-docs-index.md` and `agent-team/indexes/README.md#delta-2025-11-19` (Planner + Info-Indexer, OI-003). |
| `agent-team/main-loop-methodology.md` | Process gap | LoadContext vaguely mentions Info-Indexer, but there is no explicit step to consume the changefeed before IntegrateResults/Broadcast, leaving the new hooks underspecified. | Extend Steps 1 & 6 with "Consume Info-Indexer changefeed" checkpoints and cite the delta section (Main Agent + Info-Indexer, OI-005 follow-up). |
| `AGENTS.md` | Missing pointer | Latest progress mentions the Info-Indexer role but never links to `agent-team/indexes/core-docs-index.md` or the changefeed, so future updates risk restating the index content instead of referencing it. | Append a 2025-11-19 log entry linking to the index and instructing readers to pull deltas from the changefeed (DocMaintainer, OI-001). |
| `docs/sprints/sprint-00.md` | Duplication risk | Backlog table restates Task Board assignments yet provides no link to that source of truth or to Info-Indexer deltas, which invites drift once statuses change. | Add a note tying backlog status to `agent-team/task-board.md` and require consuming the changefeed before editing sprint text (Planner, OI-003). |

## Recommendations
- Bake "Consume Info-Indexer changefeed (`agent-team/indexes/README.md#delta-2025-11-19`)" into every doc template that guides status updates (AGENTS, Task Board, Sprints, Meetings).
- Finish OI-004 by layering the Task Board and pointing Notes to the relevant index entries instead of repeating rationale inline.
- Refresh both 2025-11-19 meeting files to track actual task status and backlink to the new consistency report, keeping meeting artifacts as decision logs only.

## Next Steps
- DocMaintainer to update the Org Self-Improvement meeting record, AGENTS log, and Task Board structure per OI-001/OI-004, referencing the changefeed and this report.
- Planner + Info-Indexer to extend the runSubAgent template and AI Team Playbook with an "Indexes & changefeed" section so future prompts mandate consuming the delta feed (OI-003).
- Main Agent + Info-Indexer to amend `agent-team/main-loop-methodology.md` with explicit changefeed checkpoints, ensuring LoadContext/Broadcast always reference the delta feed before broad updates (OI-005 follow-up).
