# Meeting: Sprint 00 Planning
- **Date:** 2025-11-19
- **Participants:** Main Agent, Planner, Investigator-TS, Porter-CS, QA-Automation, DocMaintainer, Info-Indexer
- **Facilitator:** Main Agent
- **Objective:** Align Sprint 00 backlog, milestones, and risks for PieceTree infrastructure setup.

## Agenda
1. Review PT-003…PT-006 backlog scope and budgets
2. Confirm milestone sequencing, owners, and runSubAgent checkpoints
3. Surface blockers/risks and record parking-lot items

## Notes
- **Context:** Sprint 00 kickstarts the PieceTree C# port; we need concrete deliverables and sequencing before triggering the next wave of runSubAgent calls.
- **Discussion Points:**
  - Planner reiterated the sprint theme and reminded everyone to read `agent-team/indexes/README.md#delta-2025-11-19` before touching Task Board entries.
  - Investigator-TS committed to finishing the Piece/PieceTree/Search mapping update by 2025-11-20, with Info-Indexer reviewing before PT-004 starts.
  - Porter-CS will not start PT-004 until PT-003 is merged; expects two runSubAgent passes to land the RB tree skeleton plus stubbed search hooks.
  - QA-Automation scheduled PT-005 on 2025-11-23 to capture the initial QA matrix and store the first `dotnet test` log under `tests/TextBuffer.Tests`.
  - DocMaintainer will pair with QA to deliver PT-006 right after QA assets exist, wiring the migration log template and changefeed instructions.
  - Info-Indexer will update `agent-team/indexes/core-docs-index.md` once PT-003/PT-006 land so downstream agents read a single pointer.
  - Planner and Porter-CS agreed to add PT-007 as a parking-lot placeholder for the Search regex/instrumentation risk.

## Decisions
| # | Decision | Owner | Related Files |
| --- | --- | --- | --- |
| 1 | Lock PT-003 deadline to 2025-11-20; Investigator-TS must update `agent-team/type-mapping.md` and ping Info-Indexer before closure. | Investigator-TS | agent-team/type-mapping.md |
| 2 | PT-004 starts 2025-11-21 with Porter-CS after PT-003 approval; deliver RB tree skeleton + stub search API in `src/TextBuffer/Core` with smoke `dotnet test`. | Porter-CS | src/TextBuffer/Core |
| 3 | Execute PT-005/006 between 2025-11-23 and 2025-11-24 (QA matrix + migration log template) so documentation keeps pace with code drop. | QA-Automation, DocMaintainer | tests/TextBuffer.Tests, docs/reports/consistency/ |
| 4 | Add PT-007 parking item to Sprint 00 backlog to track Search regex planning for the next sprint. | Planner, Porter-CS | docs/sprints/sprint-00.md |

## Action Items (runSubAgent-granularity)
| Task | Example Prompt / Inputs | Assignee | Target File(s) | Status |
| --- | --- | --- | --- | --- |
| Trigger PT-003 runSubAgent on 2025-11-20 with Planner context packet + type-mapping excerpt; ensure Info-Indexer review is part of the acceptance. | Context=Piece/PieceTree/Search mapping, Goals=extend TS↔C# table, Files=agent-team/type-mapping.md, Output=updated mapping + changefeed note. | Investigator-TS | agent-team/type-mapping.md | Planned |
| Kick off PT-004 runSubAgent once PT-003 merges, feeding in accepted mapping + RB tree plan; produce compilable skeleton and smoke `dotnet test`. | Context=type mapping diff + RB tree design, Goals=implement PieceTreeNode/Tree, Tests=`dotnet test`, Files=src/TextBuffer/Core | Porter-CS | src/TextBuffer/Core | Blocked by PT-003 |
| Schedule PT-005 QA pass for 2025-11-23 to add matrix annotations and capture first `dotnet test` log. | Context=PT-004 code drop, Goals=QA matrix + baseline log, Files=tests/TextBuffer.Tests/UnitTest1.cs | QA-Automation | tests/TextBuffer.Tests | Planned |
| Draft PT-006 migration log template + changefeed steps immediately after QA assets exist. | Context=docs/reports/consistency gap, Goals=log template + hooks, Files=docs/reports/consistency/. | DocMaintainer | docs/reports/consistency/ | Planned |
| Define PT-007 scope and document acceptance criteria for Search regex stubs; prepare backlog entry for next sprint. | Context=Search API risk, Goals=doc placeholder + dependencies, Files=docs/sprints/sprint-00.md. | Planner, Porter-CS | docs/sprints/sprint-00.md | Planned |

## Parking Lot
- PT-007 Search regex/stub plan: finalize deliverables + acceptance once RB tree skeleton stabilizes.
