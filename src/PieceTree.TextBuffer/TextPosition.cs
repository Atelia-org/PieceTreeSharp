namespace PieceTree.TextBuffer;

/// <summary>
/// Simple 1-based line/column coordinate used by PieceTreeBuffer high-level APIs.
/// </summary>
public readonly record struct TextPosition(int LineNumber, int Column)
{
    public static readonly TextPosition Origin = new(1, 1);
}
