using System.Collections.Generic;

namespace PieceTree.TextBuffer.Diff;

public sealed class DiffResult
{
    public DiffResult(IReadOnlyList<DiffChange> changes, IReadOnlyList<DiffMove> moves, DiffSummary summary)
    {
        Changes = changes;
        Moves = moves;
        Summary = summary;
    }

    public IReadOnlyList<DiffChange> Changes { get; }
    public IReadOnlyList<DiffMove> Moves { get; }
    public DiffSummary Summary { get; }
}

public sealed class DiffSummary
{
    public bool UsedPrettify { get; internal set; }
    public int MergeCount { get; internal set; }
    public int MoveCount { get; internal set; }
}
