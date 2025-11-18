namespace PieceTree.TextBuffer.Core;

/// <summary>
/// Mirrors the Piece structure from VS Code's piece tree to ease the upcoming port.
/// </summary>
internal sealed record PieceSegment(
    int BufferIndex,
    BufferCursor Start,
    BufferCursor End,
    int LineFeedCount,
    int Length
)
{
    public static PieceSegment Empty { get; } = new PieceSegment(
        PieceTreeModel.ChangeBufferId,
        BufferCursor.Zero,
        BufferCursor.Zero,
        0,
        0
    );
}

internal readonly record struct BufferCursor(int Line, int Column)
{
    public static BufferCursor Zero { get; } = new BufferCursor(0, 0);
}
