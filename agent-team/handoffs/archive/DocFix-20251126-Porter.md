# Doc Fix – 2025-11-26 (Porter-CS)

## Context
- Investigator handoff `agent-team/handoffs/DocReview-20251126-INV.md` flagged stale TextModelSearch evidence (35/35 + 355/355) still mentioned in my memory file.
- Requested action: refresh `agent-team/members/porter-cs.md` with the 2025-11-26 rerun totals and document the adjustment.

## Evidence Refresh
| Command | Previously Recorded | Current Evidence (2025-11-26 rerun) | Notes |
| --- | --- | --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSearchTests --nologo` | 35/35 (2.1s) | 45/45 (2.8s) | Investigator reran suite after Issue #53415 fix; no new local execution performed today. |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo` | 355/355 (64.3s) | 365/365 (56.0s) | Full suite totals pulled from the same rerun log. |

## Files Updated
- `agent-team/members/porter-cs.md`
  - Latest Focus → Sprint 03 R36 – B3-TextModelSearch now cites the 45/45 (2.8s) targeted command and the 365/365 (56.0s) baseline from the 2025-11-26 rerun.
  - Worklog → 2025-11-25 B3-TextModelSearch entry already listed the 45/45 + 365/365 verifications; no narrative adjustments besides confirming the Investigator-sourced timings were canonical.
  - Added 2025-11-26 DocReview Sync bullet referencing this file so DocMaintainer/Info-Indexer can trace the documentation fix.
- `agent-team/handoffs/DocFix-20251126-Porter.md` (this memo) recorded the change for DocMaintainer.

## Testing
- No new test executions today; relying on the Investigator-provided rerun logs dated 2025-11-26 under `PIECETREE_DEBUG=0`.

## Follow-ups
- None – downstream docs already cite `#delta-2025-11-25-b3-textmodelsearch` with the updated counts.
