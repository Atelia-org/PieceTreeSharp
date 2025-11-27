// Source: ts/src/vs/editor/common/diff/linesDiffComputer.ts
// - Class: LinesDiff (Lines: 19-37)
// Ported: 2025-11-19

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
