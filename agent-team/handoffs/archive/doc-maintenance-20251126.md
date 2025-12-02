# Doc Maintenance Sweep – 2025-11-26

## Objective
Compress and refresh cognition docs (`AGENTS.md` + every `agent-team/members/*.md`) so that only actionable, still-relevant knowledge remains while historical context is summarized. Each role owner must:

1. Re-read `AGENTS.md`, their role memory file, and any linked changefeeds referenced there.
2. Flag outdated or low-signal blocks (obsolete worklogs, duplicate checklists, stale TODOs) and either summarize, archive, or delete per Doc Gardener policy.
3. Update their memory file with the streamlined content plus a short "Fresh Snapshot" that highlights current mission, latest focus, and open blockers.
4. Document the adjustments in this handoff (Status column) and ensure `AGENTS.md` reflects cross-role outcomes once DocMaintainer completes the sweep.

## Changefeed Checkpoints
- Primary: [agent-team/indexes/README.md#delta-2025-11-26-sprint04](../indexes/README.md#delta-2025-11-26-sprint04)
- Recently referenced: [agent-team/indexes/README.md#delta-2025-11-26-alignment-audit](../indexes/README.md#delta-2025-11-26-alignment-audit)

Please confirm these anchors (and any newer ones) before editing.

## Task Tracker
| Role | Memory File | Scope Notes | Status |
| --- | --- | --- | --- |
| Planner | `agent-team/members/planner.md` | Trim dated sprint references, keep current planning heuristics and backlog pointers. | 2025-11-26 – Snapshot compressed, tied to `#delta-2025-11-26-sprint04` / `#delta-2025-11-26-alignment-audit`. |
| Investigator-TS | `agent-team/members/investigator-ts.md` | Collapse long investigation logs into highlights + open risks. | 2025-11-26 – Investigator snapshot refreshed w/ `#delta-2025-11-26-sprint04`, `#delta-2025-11-26-aa4-cl7-cursor-core`, `#delta-2025-11-26-aa4-cl8-markdown`. |
| Porter-CS | `agent-team/members/porter-cs.md` | Focus on active porting guidelines, move closed batch narratives to archive. | 2025-11-26 – Snapshot compressed + linked to `#delta-2025-11-26-sprint04`, `#delta-2025-11-26-aa4-cl7-cursor-core`, `#delta-2025-11-26-aa4-cl8-markdown`. |
| QA-Automation | `agent-team/members/qa-automation.md` | Keep canonical test matrix links and latest rerun policies. | 2025-11-26 – Snapshot trimmed; anchors `#delta-2025-11-25-b3-textmodelsearch`, `#delta-2025-11-25-b3-piecetree-deterministic-crlf`, `#delta-2025-11-25-b3-search-offset`, `#delta-2025-11-24-b3-docui-staged`, `#delta-2025-11-26-sprint04` captured. |
| DocMaintainer | `agent-team/members/doc-maintainer.md` | Ensure Doc Gardener duties summarize compression strategy, migrate historical worklogs into single recap. | 2025-11-26 – Snapshot condensed; references `#delta-2025-11-26-sprint04`/`#delta-2025-11-26-alignment-audit`/`#delta-2025-11-25-b3-textmodelsearch`/`#delta-2025-11-25-b3-search-offset` + CL7/CL8 placeholders. |
| Info-Indexer | `agent-team/members/info-indexer.md` | Highlight current changefeed cadence + backlog, remove redundant delta listings. | 2025-11-26 – Snapshot condensed; cites `#delta-2025-11-26-sprint04`, `#delta-2025-11-26-alignment-audit`, `#delta-2025-11-25-b3-textmodelsearch`, `#delta-2025-11-25-b3-search-offset`. |
| AGENTS.md | `AGENTS.md` | Summarize this sweep + clarify where detailed histories now live. | 2025-11-26 – Added Doc Maintenance Sweep bullet referencing handoff + `#delta-2025-11-26-sprint04` / `#delta-2025-11-26-alignment-audit`. |

## Reporting Instructions
- Each SubAgent must update their own memory file **before** reporting completion, and note the edit inside this handoff table (`Status` column).
- Use `agent-team/handoffs/` for any supplementary notes to avoid bloating AGENTS/role files.
- When removing content, mention where the detailed history can be found (e.g., archived handoffs, migration log rows).
- After all roles finish, DocMaintainer should add a single bullet to `AGENTS.md` capturing the sweep outcome plus a link back to this handoff.
