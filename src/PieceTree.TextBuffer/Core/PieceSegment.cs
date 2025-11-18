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
);

internal readonly record struct BufferCursor(int Line, int Column);
