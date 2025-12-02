// Source: ts/src/vs/editor/common/diff/linesDiffComputer.ts
// - Interface: MovedText
// - Lines: 50-80
// - Method: flip() (Lines: 53-55)
// Ported: 2025-11-21
// Updated: 2025-12-02 (Added Flip method)

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

    /// <summary>
    /// Returns a new DiffMove with original and modified swapped.
    /// </summary>
    /// <remarks>
    /// TS Source: ts/src/vs/editor/common/diff/linesDiffComputer.ts
    /// MovedText.flip() (Lines 53-55)
    /// </remarks>
    public DiffMove Flip()
    {
        return new DiffMove(
            LineRangeMapping.Flip(),
            Changes.Select(c => (DetailedLineRangeMapping)c.Flip()).ToArray()
        );
    }
}
