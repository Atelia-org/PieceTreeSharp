# B3-TextModel-Snapshot-PORT

**Date:** 2025-11-25  
**Porter:** GitHub Copilot (GPT-5.1-Codex)  
**Delta Tag:** `#delta-2025-11-25-b3-textmodel-snapshot`

## Scope
- Close F1 from the Investigator/Planner notes: port `TextModelSnapshot` so `TextModel.CreateSnapshot()` returns a 64KB-bounded wrapper instead of exposing the raw `PieceTreeSnapshot` pieces.
- Add regression tests that exercise the wrapper’s aggregation, EOS caching, and empty-chunk skipping semantics.
- Re-run the snapshot suites (new + legacy) to ensure the change integrates cleanly with existing PieceTree snapshot deterministics.

## Implementation
1. **`src/TextBuffer/TextModelSnapshot.cs`** – new C# port of TS `TextModelSnapshot`:
   - Aggregates the underlying `ITextSnapshot` chunks until the combined length exceeds 64KB or EOS, ignoring zero-length reads and caching EOS for future calls.
   - Uses a small string list with a capped `StringBuilder` join so 1-chunk results avoid redundant allocations.
2. **`src/TextBuffer/TextModel.cs`** – `CreateSnapshot(bool preserveBom = false)` now pulls the raw snapshot from `_buffer.InternalModel` and wraps it inside `TextModelSnapshot` before returning.
3. **`tests/TextBuffer.Tests/TextModelSnapshotTests.cs`** – new suite with a fake `ITextSnapshot` source verifying:
   - The public API returns `TextModelSnapshot` instances.
   - 64KB aggregation batches many tiny chunks into two reads.
   - Empty chunks are skipped and EOS returns `null` without further source reads.
4. **`tests/TextBuffer.Tests/TestMatrix.md`** – recorded the new suite and targeted reruns beside the existing snapshot coverage so QA/DocMaintainer have traceability for `#delta-2025-11-25-b3-textmodel-snapshot`.

## Tests
| Command | Result |
| --- | --- |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter TextModelSnapshotTests --nologo` | ✅ 4/4 (2.5s) |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotTests --nologo` | ✅ 2/2 (1.8s) |
| `export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --filter PieceTreeSnapshotParityTests --nologo` | ✅ 4/4 (1.7s) |

## Notes / Follow-ups
- No additional TODOs for this gap; downstream consumers now observe TS-equivalent chunking.
- QA/DocMaintainer references: `tests/TextBuffer.Tests/TestMatrix.md` targeted rerun table + this handoff for evidence.
