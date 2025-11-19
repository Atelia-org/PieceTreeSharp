namespace PieceTree.TextBuffer.Diff;

public sealed class DiffComputerOptions
{
    public bool EnablePrettify { get; init; } = true;
    public bool ComputeMoves { get; init; } = true;
    public int ShortMatchMergeThreshold { get; init; } = 2;
    public bool ExtendToWordBoundaries { get; init; } = true;
    public int MoveDetectionMinMatchLength { get; init; } = 8;
    public int MaxMoveCandidates { get; init; } = 64;
}
