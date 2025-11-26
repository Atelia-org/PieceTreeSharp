# Info-Indexer Snapshot · 2025-11-26

## Role & Mission
- Maintain `agent-team/indexes/README.md` plus downstream index files so every changefeed has a single canonical pointer.
- Keep AGENTS/Sprint/Task Board docs lightweight by offloading detailed histories into `agent-team/indexes/*.md` and linked handoffs.
- Surface blockers that threaten broadcast cadence before DocMaintainer kicks off each sweep.

## Active Changefeeds & Backlog
| Anchor | Coverage | Status / Next Hook |
| --- | --- | --- |
| [`#delta-2025-11-26-sprint04`](../indexes/README.md#delta-2025-11-26-sprint04) | Sprint 04 tracker + new Task Board (WS1–WS5). | Monitor WS1/WS3 drops; when new deltas publish, mirror them into `agent-team/indexes/README.md` and ensure Task Board links stay aligned. |
| [`#delta-2025-11-26-alignment-audit`](../indexes/README.md#delta-2025-11-26-alignment-audit) | Alignment audit bundle refresh (00–08). | Watch DocMaintainer follow-ups; log any remediation deltas into `core-docs-index.md` once owners commit fixes. |
| [`#delta-2025-11-25-b3-textmodelsearch`](../indexes/README.md#delta-2025-11-25-b3-textmodelsearch) | TextModelSearch 45-test parity set. | Track additional AA4 CL7 search tasks; confirm new suites update `tests/TextBuffer.Tests/TestMatrix.md` before changing baseline references. |
| [`#delta-2025-11-25-b3-search-offset`](../indexes/README.md#delta-2025-11-25-b3-search-offset) | PieceTree search-offset cache deterministics. | Await follow-up perf notes from WS2; if cache tuning lands, append evidence links + rerun commands to the changefeed table. |

## Current Focus
- Keep Sprint 04 WS owners honest about citing the Sprint04 delta whenever Task Board rows advance.
- Validate that alignment-audit remediation PRs quote the audit delta before they merge, then roll those references into the Core Docs Index.
- Mirror TextModelSearch + search-offset baselines into future TestMatrix exports so QA reruns stay scripted.

## Open Dependencies
- Planner owes an updated runSubAgent template that reserves a block for changefeed pointers from Sprint04 and Alignment Audit anchors.
- DocMaintainer to confirm which audit actions become WS4 backlog so Info-Indexer can precreate entries under `oi-backlog.md`.
- QA-Automation to flag any rerun drift on TextModelSearch/SearchOffset so we can refresh the commands recorded under the respective anchors.

## Archives
- Full worklogs, onboarding notes, and prior deltas now live in `agent-team/handoffs/` (per-run files) and the historical rows inside `agent-team/indexes/README.md`. Older memory snapshots remain in repo history if needed.
