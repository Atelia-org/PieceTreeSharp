using System.Collections.Generic;

namespace PieceTree.TextBuffer.Diff;

public class LinesDiff
{
    public LinesDiff(IReadOnlyList<DetailedLineRangeMapping> changes, IReadOnlyList<DiffMove> moves, bool hitTimeout)
    {
        Changes = changes;
        Moves = moves;
        HitTimeout = hitTimeout;
    }

    public IReadOnlyList<DetailedLineRangeMapping> Changes { get; }
    public IReadOnlyList<DiffMove> Moves { get; }
    public bool HitTimeout { get; }

    public bool IsIdentical => Changes.Count == 0 && Moves.Count == 0;
}

public sealed class DiffResult : LinesDiff
{
    public DiffResult(IReadOnlyList<DetailedLineRangeMapping> changes, IReadOnlyList<DiffMove> moves, bool hitTimeout)
        : base(changes, moves, hitTimeout)
    {
    }
}
