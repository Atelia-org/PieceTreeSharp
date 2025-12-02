// Source: ts/src/vs/editor/common/core/text/textLength.ts
// - Class: TextLength (Lines: 1-130)
// Ported: 2025-12-02 (Sprint05-M2-RangeMappingConversion)

using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Diff;

/// <summary>
/// Represents a non-negative length of text in terms of line and column count.
/// </summary>
public readonly struct TextLength : IEquatable<TextLength>, IComparable<TextLength>
{
    /// <summary>
    /// Zero-length text.
    /// </summary>
    public static readonly TextLength Zero = new(0, 0);

    /// <summary>
    /// The number of complete lines (newlines counted).
    /// </summary>
    public int LineCount { get; }

    /// <summary>
    /// The number of columns after the last newline.
    /// </summary>
    public int ColumnCount { get; }

    public TextLength(int lineCount, int columnCount)
    {
        LineCount = lineCount;
        ColumnCount = columnCount;
    }

    /// <summary>
    /// Returns true if this length is zero.
    /// </summary>
    public bool IsZero => LineCount == 0 && ColumnCount == 0;

    /// <summary>
    /// Computes the text length of a given string by counting newlines.
    /// </summary>
    public static TextLength OfText(string text)
    {
        int line = 0;
        int column = 0;
        foreach (char c in text)
        {
            if (c == '\n')
            {
                line++;
                column = 0;
            }
            else
            {
                column++;
            }
        }
        return new TextLength(line, column);
    }

    /// <summary>
    /// Computes the text length between two positions.
    /// </summary>
    public static TextLength BetweenPositions(TextPosition position1, TextPosition position2)
    {
        if (position1.LineNumber == position2.LineNumber)
        {
            return new TextLength(0, position2.Column - position1.Column);
        }
        else
        {
            return new TextLength(position2.LineNumber - position1.LineNumber, position2.Column - 1);
        }
    }

    /// <summary>
    /// Computes the text length of a range.
    /// </summary>
    public static TextLength OfRange(Range range)
    {
        return BetweenPositions(range.GetStartPosition(), range.GetEndPosition());
    }

    /// <summary>
    /// Adds two text lengths.
    /// </summary>
    public TextLength Add(TextLength other)
    {
        if (other.LineCount == 0)
        {
            return new TextLength(LineCount, ColumnCount + other.ColumnCount);
        }
        else
        {
            return new TextLength(LineCount + other.LineCount, other.ColumnCount);
        }
    }

    /// <summary>
    /// Creates a Range starting at the given position with this length.
    /// </summary>
    public Range CreateRange(TextPosition startPosition)
    {
        if (LineCount == 0)
        {
            return new Range(
                startPosition.LineNumber, startPosition.Column,
                startPosition.LineNumber, startPosition.Column + ColumnCount);
        }
        else
        {
            return new Range(
                startPosition.LineNumber, startPosition.Column,
                startPosition.LineNumber + LineCount, ColumnCount + 1);
        }
    }

    /// <summary>
    /// Adds this length to a position.
    /// </summary>
    public TextPosition AddToPosition(TextPosition position)
    {
        if (LineCount == 0)
        {
            return new TextPosition(position.LineNumber, position.Column + ColumnCount);
        }
        else
        {
            return new TextPosition(position.LineNumber + LineCount, ColumnCount + 1);
        }
    }

    public bool IsLessThan(TextLength other)
    {
        if (LineCount != other.LineCount)
        {
            return LineCount < other.LineCount;
        }
        return ColumnCount < other.ColumnCount;
    }

    public bool IsGreaterThan(TextLength other)
    {
        if (LineCount != other.LineCount)
        {
            return LineCount > other.LineCount;
        }
        return ColumnCount > other.ColumnCount;
    }

    public int CompareTo(TextLength other)
    {
        if (LineCount != other.LineCount)
        {
            return LineCount - other.LineCount;
        }
        return ColumnCount - other.ColumnCount;
    }

    public bool Equals(TextLength other)
    {
        return LineCount == other.LineCount && ColumnCount == other.ColumnCount;
    }

    public override bool Equals(object? obj)
    {
        return obj is TextLength other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LineCount, ColumnCount);
    }

    public static bool operator ==(TextLength left, TextLength right) => left.Equals(right);
    public static bool operator !=(TextLength left, TextLength right) => !left.Equals(right);
    public static bool operator <(TextLength left, TextLength right) => left.IsLessThan(right);
    public static bool operator >(TextLength left, TextLength right) => left.IsGreaterThan(right);
    public static bool operator <=(TextLength left, TextLength right) => !left.IsGreaterThan(right);
    public static bool operator >=(TextLength left, TextLength right) => !left.IsLessThan(right);

    public override string ToString() => $"{LineCount},{ColumnCount}";
}
