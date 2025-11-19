namespace PieceTree.TextBuffer.Diff;

public sealed class DiffMove
{
    public DiffMove(int originalStart, int originalLength, int modifiedStart, int modifiedLength, string text)
    {
        OriginalStart = originalStart;
        OriginalLength = originalLength;
        ModifiedStart = modifiedStart;
        ModifiedLength = modifiedLength;
        Text = text;
    }

    public int OriginalStart { get; }
    public int OriginalLength { get; }
    public int ModifiedStart { get; }
    public int ModifiedLength { get; }
    public string Text { get; }
}
