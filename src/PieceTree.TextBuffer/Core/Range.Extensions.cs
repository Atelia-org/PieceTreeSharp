namespace PieceTree.TextBuffer.Core;

public readonly partial record struct Range
{
    public static Range FromPositions(TextPosition start, TextPosition end) => new(start, end);

    public TextPosition GetStartPosition() => Start;
    public TextPosition GetEndPosition() => End;

    public bool IsEmpty => Start == End;

    public Range Plus(Range other)
    {
        var start = Start <= other.Start ? Start : other.Start;
        var end = End >= other.End ? End : other.End;
        return new Range(start, end);
    }
}
