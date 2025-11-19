# Diff-Driven PieceTree Porting Prompt

Use this prompt whenever you (future Copilot) resume the PieceTree migration. It encodes the diff-driven workflow we validated on 2025-11-19.

---

**System Context**
- Goal: Port VS Code PieceTree (`ts/src/vs/editor/common/model/pieceTreeTextBuffer`) into `src/PieceTree.TextBuffer` using .NET 9 + xUnit.
- Strategy: Iterate via TS↔C# comparisons; each cycle produces a Diff Brief, targeted implementation, imported TS tests, and a single migration-log entry.
- Documentation Discipline: Update only `docs/reports/migration-log.md`, `agent-team/task-board.md`, and the relevant member memory per cycle. Avoid redundant summaries elsewhere.

**Prompt Skeleton for Future Runs**
1. **Load Context**
   - Read `AGENTS.md`, `agent-team/type-mapping.md`, `agent-team/task-board.md`, and the latest changefeed at `agent-team/indexes/README.md#delta-2025-11-19`.
   - Note the last migration-log row to understand the most recent C# additions.

2. **Plan Cycle**
   - Pick a TS module slice (e.g., `nodeAt/nodeAt2`, `insert/delete`, search helpers).
   - Ask Investigator-TS (via `runSubAgent`) for a Diff Brief: TS symbols, invariants, missing C# coverage, and candidate TS tests.
   - Define acceptance: which C# files to touch, which TS tests to port to xUnit, and the single log/doc update you will make when done.

3. **Execute**
   - Porter-CS implements the mapped methods or shims directly in the target C# file (prefer editing in-place, not PortingDrafts unless the diff is huge).
   - Port the matching TS unit tests into `src/PieceTree.TextBuffer.Tests`, keeping names close to the originals.
   - Run `dotnet test PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj` after every functional change.

4. **Document Minimally**
   - Append one row to `docs/reports/migration-log.md` referencing the code paths and test command.
   - If ownership or status shifts, update `agent-team/task-board.md` once per cycle.
   - Log a single bullet in the relevant member memory (e.g., `agent-team/members/porter-cs.md`).

5. **Iterate**
   - Re-read the changefeed before the next cycle; note any files that now require Info-Indexer deltas (e.g., new shims, PortingDrafts).
   - Continue looping until the TS slice reaches parity.

**Reminders**
- Prefer actionable Diff Briefs over general summaries; every run should highlight specific missing members/tests.
- Treat typed arrays, regex differences, and unions pragmatically: implement shims once, then reuse.
- Only stop an iteration early if a blocker arises; document the blocker in the member memory and surface it in the next call.
- Keep conversations concise—lead with plan, show diffs/tests, then record the single migration-log entry.
