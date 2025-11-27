// Source: ts/src/vs/editor/common/core/position.ts
// - Interface: IPosition (Lines: 9-21)
// - Class: Position (Lines: 23-200+)
// Ported: 2025-11-18
// Updated: 2025-11-26 (WS2-PORT: Range/Selection Helper APIs)

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

    #region With / Delta methods

    /// <summary>
    /// Create a new position from this position with optional new line and/or column.
    /// If parameters are null, the current values are used.
    /// </summary>
    public TextPosition With(int? newLineNumber = null, int? newColumn = null)
    {
        int line = newLineNumber ?? LineNumber;
        int col = newColumn ?? Column;
        if (line == LineNumber && col == Column)
        {
            return this;
        }
        return new TextPosition(line, col);
    }

    /// <summary>
    /// Derive a new position from this position by applying deltas.
    /// Results are clamped to be >= 1.
    /// </summary>
    public TextPosition Delta(int deltaLineNumber = 0, int deltaColumn = 0)
    {
        return With(
            Math.Max(1, LineNumber + deltaLineNumber),
            Math.Max(1, Column + deltaColumn));
    }

    #endregion

    #region Comparison methods

    /// <summary>
    /// Test if this position is before other position.
    /// If the two positions are equal, the result will be false.
    /// </summary>
    public bool IsBefore(TextPosition other) => IsBefore(this, other);

    /// <summary>
    /// Test if position a is before position b.
    /// If the two positions are equal, the result will be false.
    /// </summary>
    public static bool IsBefore(TextPosition a, TextPosition b)
    {
        if (a.LineNumber < b.LineNumber)
        {
            return true;
        }
        if (b.LineNumber < a.LineNumber)
        {
            return false;
        }
        return a.Column < b.Column;
    }

    /// <summary>
    /// Test if this position is before or equal to other position.
    /// If the two positions are equal, the result will be true.
    /// </summary>
    public bool IsBeforeOrEqual(TextPosition other) => IsBeforeOrEqual(this, other);

    /// <summary>
    /// Test if position a is before or equal to position b.
    /// If the two positions are equal, the result will be true.
    /// </summary>
    public static bool IsBeforeOrEqual(TextPosition a, TextPosition b)
    {
        if (a.LineNumber < b.LineNumber)
        {
            return true;
        }
        if (b.LineNumber < a.LineNumber)
        {
            return false;
        }
        return a.Column <= b.Column;
    }

    /// <summary>
    /// A function that compares positions, useful for sorting.
    /// </summary>
    public static int Compare(TextPosition a, TextPosition b)
    {
        int aLineNumber = a.LineNumber;
        int bLineNumber = b.LineNumber;

        if (aLineNumber == bLineNumber)
        {
            int aColumn = a.Column;
            int bColumn = b.Column;
            return aColumn - bColumn;
        }

        return aLineNumber - bLineNumber;
    }

    /// <summary>
    /// Test if position a equals position b (null-safe).
    /// </summary>
    public static bool Equals(TextPosition? a, TextPosition? b)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return true;
        }
        if (!a.HasValue || !b.HasValue)
        {
            return false;
        }
        return a.Value.LineNumber == b.Value.LineNumber
            && a.Value.Column == b.Value.Column;
    }

    #endregion
}
