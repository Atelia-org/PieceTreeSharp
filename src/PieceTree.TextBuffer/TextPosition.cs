namespace PieceTree.TextBuffer;

/// <summary>
/// Simple 1-based line/column coordinate used by PieceTreeBuffer high-level APIs.
/// </summary>
public readonly record struct TextPosition(int LineNumber, int Column) : IComparable<TextPosition>
{
    public static readonly TextPosition Origin = new(1, 1);

    public int CompareTo(TextPosition other)
    {
        if (LineNumber != other.LineNumber)
        {
            return LineNumber.CompareTo(other.LineNumber);
        }
        return Column.CompareTo(other.Column);
    }

    public static bool operator <(TextPosition left, TextPosition right) => left.CompareTo(right) < 0;
    public static bool operator >(TextPosition left, TextPosition right) => left.CompareTo(right) > 0;
    public static bool operator <=(TextPosition left, TextPosition right) => left.CompareTo(right) <= 0;
    public static bool operator >=(TextPosition left, TextPosition right) => left.CompareTo(right) >= 0;
}
