using System.Collections.Generic;

namespace PieceTree.TextBuffer.Diff;

public sealed class DiffMove
{
    public DiffMove(LineRangeMapping lineRangeMapping, IReadOnlyList<DetailedLineRangeMapping> changes)
    {
        LineRangeMapping = lineRangeMapping;
        Changes = changes;
    }

    public LineRangeMapping LineRangeMapping { get; }
    public IReadOnlyList<DetailedLineRangeMapping> Changes { get; }

    public LineRange Original => LineRangeMapping.Original;
    public LineRange Modified => LineRangeMapping.Modified;
}
