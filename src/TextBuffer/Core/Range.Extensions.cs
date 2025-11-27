// Source: ts/src/vs/editor/common/core/range.ts
// - Interface: IRange extension methods
// - Lines: 50-150
// Ported: 2025-11-18
// Updated: 2025-11-26 (WS2-PORT: Range/Selection Helper APIs)

namespace PieceTree.TextBuffer.Core;

public readonly partial record struct Range
{
    /// <summary>
    /// Create a Range from two TextPosition values.
    /// </summary>
    public static Range FromPositions(TextPosition start, TextPosition? end = null)
        => new(start, end ?? start);

    /// <summary>
    /// Return the start position (which will be before or equal to the end position).
    /// </summary>
    public TextPosition GetStartPosition() => Start;

    /// <summary>
    /// Return the end position (which will be after or equal to the start position).
    /// </summary>
    public TextPosition GetEndPosition() => End;

    /// <summary>
    /// Test if this range is empty.
    /// </summary>
    public bool IsEmpty => Start == End;

    /// <summary>
    /// Test if this range spans a single line.
    /// </summary>
    public bool IsSingleLine => StartLineNumber == EndLineNumber;

    #region Instance methods

    /// <summary>
    /// A reunion of the two ranges.
    /// The smallest position will be used as the start point, and the largest one as the end point.
    /// </summary>
    public Range Plus(Range other) => PlusRange(this, other);

    /// <summary>
    /// Test if position is in this range. If the position is at the edges, will return true.
    /// </summary>
    public bool ContainsPosition(TextPosition position) => ContainsPosition(this, position);

    /// <summary>
    /// Test if range is in this range. If the range is equal to this range, will return true.
    /// </summary>
    public bool ContainsRange(Range range) => ContainsRange(this, range);

    /// <summary>
    /// A intersection of the two ranges.
    /// </summary>
    public Range? IntersectRanges(Range range) => IntersectRanges(this, range);

    /// <summary>
    /// Create a new range using this range's start position, and using new values as the end position.
    /// Note: The result is normalized so start <= end.
    /// </summary>
    public Range SetEndPosition(int endLineNumber, int endColumn)
        => Normalize(StartLineNumber, StartColumn, endLineNumber, endColumn);

    /// <summary>
    /// Create a new range using this range's end position, and using new values as the start position.
    /// Note: The result is normalized so start <= end.
    /// </summary>
    public Range SetStartPosition(int startLineNumber, int startColumn)
        => Normalize(startLineNumber, startColumn, EndLineNumber, EndColumn);

    /// <summary>
    /// Create a new empty range using this range's start position.
    /// </summary>
    public Range CollapseToStart() => CollapseToStart(this);

    /// <summary>
    /// Create a new empty range using this range's end position.
    /// </summary>
    public Range CollapseToEnd() => CollapseToEnd(this);

    /// <summary>
    /// Moves the range by the given amount of lines.
    /// </summary>
    public Range Delta(int lineCount)
        => new Range(StartLineNumber + lineCount, StartColumn, EndLineNumber + lineCount, EndColumn);

    /// <summary>
    /// Moves the range by the given deltas.
    /// </summary>
    public Range Delta(int deltaStartLineNumber, int deltaStartColumn, int deltaEndLineNumber, int deltaEndColumn)
        => new Range(
            StartLineNumber + deltaStartLineNumber,
            StartColumn + deltaStartColumn,
            EndLineNumber + deltaEndLineNumber,
            EndColumn + deltaEndColumn);

    #endregion

    #region Static methods

    /// <summary>
    /// Normalize a range so that start <= end.
    /// </summary>
    public static Range Normalize(int startLineNumber, int startColumn, int endLineNumber, int endColumn)
    {
        if ((startLineNumber > endLineNumber) || (startLineNumber == endLineNumber && startColumn > endColumn))
        {
            return new Range(endLineNumber, endColumn, startLineNumber, startColumn);
        }
        return new Range(startLineNumber, startColumn, endLineNumber, endColumn);
    }

    /// <summary>
    /// Test if position is in range. If the position is at the edges, will return true.
    /// </summary>
    public static bool ContainsPosition(Range range, TextPosition position)
    {
        if (position.LineNumber < range.StartLineNumber || position.LineNumber > range.EndLineNumber)
        {
            return false;
        }
        if (position.LineNumber == range.StartLineNumber && position.Column < range.StartColumn)
        {
            return false;
        }
        if (position.LineNumber == range.EndLineNumber && position.Column > range.EndColumn)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Test if position is in range. If the position is at the edges, will return false.
    /// </summary>
    public static bool StrictContainsPosition(Range range, TextPosition position)
    {
        if (position.LineNumber < range.StartLineNumber || position.LineNumber > range.EndLineNumber)
        {
            return false;
        }
        if (position.LineNumber == range.StartLineNumber && position.Column <= range.StartColumn)
        {
            return false;
        }
        if (position.LineNumber == range.EndLineNumber && position.Column >= range.EndColumn)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Test if otherRange is in range. If the ranges are equal, will return true.
    /// </summary>
    public static bool ContainsRange(Range range, Range otherRange)
    {
        if (otherRange.StartLineNumber < range.StartLineNumber || otherRange.EndLineNumber < range.StartLineNumber)
        {
            return false;
        }
        if (otherRange.StartLineNumber > range.EndLineNumber || otherRange.EndLineNumber > range.EndLineNumber)
        {
            return false;
        }
        if (otherRange.StartLineNumber == range.StartLineNumber && otherRange.StartColumn < range.StartColumn)
        {
            return false;
        }
        if (otherRange.EndLineNumber == range.EndLineNumber && otherRange.EndColumn > range.EndColumn)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Test if otherRange is strictly in range (must start after, and end before). If the ranges are equal, will return false.
    /// </summary>
    public static bool StrictContainsRange(Range range, Range otherRange)
    {
        if (otherRange.StartLineNumber < range.StartLineNumber || otherRange.EndLineNumber < range.StartLineNumber)
        {
            return false;
        }
        if (otherRange.StartLineNumber > range.EndLineNumber || otherRange.EndLineNumber > range.EndLineNumber)
        {
            return false;
        }
        if (otherRange.StartLineNumber == range.StartLineNumber && otherRange.StartColumn <= range.StartColumn)
        {
            return false;
        }
        if (otherRange.EndLineNumber == range.EndLineNumber && otherRange.EndColumn >= range.EndColumn)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// A reunion of the two ranges.
    /// The smallest position will be used as the start point, and the largest one as the end point.
    /// </summary>
    public static Range PlusRange(Range a, Range b)
    {
        int startLineNumber;
        int startColumn;
        int endLineNumber;
        int endColumn;

        if (b.StartLineNumber < a.StartLineNumber)
        {
            startLineNumber = b.StartLineNumber;
            startColumn = b.StartColumn;
        }
        else if (b.StartLineNumber == a.StartLineNumber)
        {
            startLineNumber = b.StartLineNumber;
            startColumn = Math.Min(b.StartColumn, a.StartColumn);
        }
        else
        {
            startLineNumber = a.StartLineNumber;
            startColumn = a.StartColumn;
        }

        if (b.EndLineNumber > a.EndLineNumber)
        {
            endLineNumber = b.EndLineNumber;
            endColumn = b.EndColumn;
        }
        else if (b.EndLineNumber == a.EndLineNumber)
        {
            endLineNumber = b.EndLineNumber;
            endColumn = Math.Max(b.EndColumn, a.EndColumn);
        }
        else
        {
            endLineNumber = a.EndLineNumber;
            endColumn = a.EndColumn;
        }

        return new Range(startLineNumber, startColumn, endLineNumber, endColumn);
    }

    /// <summary>
    /// A intersection of the two ranges. Returns null if no intersection.
    /// </summary>
    public static Range? IntersectRanges(Range a, Range b)
    {
        int resultStartLineNumber = a.StartLineNumber;
        int resultStartColumn = a.StartColumn;
        int resultEndLineNumber = a.EndLineNumber;
        int resultEndColumn = a.EndColumn;
        int otherStartLineNumber = b.StartLineNumber;
        int otherStartColumn = b.StartColumn;
        int otherEndLineNumber = b.EndLineNumber;
        int otherEndColumn = b.EndColumn;

        if (resultStartLineNumber < otherStartLineNumber)
        {
            resultStartLineNumber = otherStartLineNumber;
            resultStartColumn = otherStartColumn;
        }
        else if (resultStartLineNumber == otherStartLineNumber)
        {
            resultStartColumn = Math.Max(resultStartColumn, otherStartColumn);
        }

        if (resultEndLineNumber > otherEndLineNumber)
        {
            resultEndLineNumber = otherEndLineNumber;
            resultEndColumn = otherEndColumn;
        }
        else if (resultEndLineNumber == otherEndLineNumber)
        {
            resultEndColumn = Math.Min(resultEndColumn, otherEndColumn);
        }

        // Check if selection is now empty
        if (resultStartLineNumber > resultEndLineNumber)
        {
            return null;
        }
        if (resultStartLineNumber == resultEndLineNumber && resultStartColumn > resultEndColumn)
        {
            return null;
        }
        return new Range(resultStartLineNumber, resultStartColumn, resultEndLineNumber, resultEndColumn);
    }

    /// <summary>
    /// Test if the two ranges are touching in any way.
    /// </summary>
    public static bool AreIntersectingOrTouching(Range a, Range b)
    {
        // Check if `a` is before `b`
        if (a.EndLineNumber < b.StartLineNumber || (a.EndLineNumber == b.StartLineNumber && a.EndColumn < b.StartColumn))
        {
            return false;
        }

        // Check if `b` is before `a`
        if (b.EndLineNumber < a.StartLineNumber || (b.EndLineNumber == a.StartLineNumber && b.EndColumn < a.StartColumn))
        {
            return false;
        }

        // These ranges must intersect
        return true;
    }

    /// <summary>
    /// Test if the two ranges are intersecting. If the ranges are touching it returns true.
    /// </summary>
    public static bool AreIntersecting(Range a, Range b)
    {
        // Check if `a` is before `b`
        if (a.EndLineNumber < b.StartLineNumber || (a.EndLineNumber == b.StartLineNumber && a.EndColumn <= b.StartColumn))
        {
            return false;
        }

        // Check if `b` is before `a`
        if (b.EndLineNumber < a.StartLineNumber || (b.EndLineNumber == a.StartLineNumber && b.EndColumn <= a.StartColumn))
        {
            return false;
        }

        // These ranges must intersect
        return true;
    }

    /// <summary>
    /// Test if the two ranges overlap without merely touching.
    /// </summary>
    public static bool AreOnlyIntersecting(Range a, Range b)
    {
        if (a.EndLineNumber < (b.StartLineNumber - 1) || (a.EndLineNumber == b.StartLineNumber && a.EndColumn < (b.StartColumn - 1)))
        {
            return false;
        }

        if (b.EndLineNumber < (a.StartLineNumber - 1) || (b.EndLineNumber == a.StartLineNumber && b.EndColumn < (a.StartColumn - 1)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Create a new empty range using range's start position.
    /// </summary>
    public static Range CollapseToStart(Range range)
        => new Range(range.StartLineNumber, range.StartColumn, range.StartLineNumber, range.StartColumn);

    /// <summary>
    /// Create a new empty range using range's end position.
    /// </summary>
    public static Range CollapseToEnd(Range range)
        => new Range(range.EndLineNumber, range.EndColumn, range.EndLineNumber, range.EndColumn);

    /// <summary>
    /// Test if range a equals b (null-safe).
    /// </summary>
    public static bool EqualsRange(Range? a, Range? b)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return true;
        }
        if (!a.HasValue || !b.HasValue)
        {
            return false;
        }
        return a.Value.StartLineNumber == b.Value.StartLineNumber
            && a.Value.StartColumn == b.Value.StartColumn
            && a.Value.EndLineNumber == b.Value.EndLineNumber
            && a.Value.EndColumn == b.Value.EndColumn;
    }

    /// <summary>
    /// A function that compares ranges, useful for sorting ranges.
    /// It will first compare ranges on the startPosition and then on the endPosition.
    /// </summary>
    public static int CompareRangesUsingStarts(Range? a, Range? b)
    {
        if (a.HasValue && b.HasValue)
        {
            int aStartLineNumber = a.Value.StartLineNumber;
            int bStartLineNumber = b.Value.StartLineNumber;

            if (aStartLineNumber == bStartLineNumber)
            {
                int aStartColumn = a.Value.StartColumn;
                int bStartColumn = b.Value.StartColumn;

                if (aStartColumn == bStartColumn)
                {
                    int aEndLineNumber = a.Value.EndLineNumber;
                    int bEndLineNumber = b.Value.EndLineNumber;

                    if (aEndLineNumber == bEndLineNumber)
                    {
                        int aEndColumn = a.Value.EndColumn;
                        int bEndColumn = b.Value.EndColumn;
                        return aEndColumn - bEndColumn;
                    }
                    return aEndLineNumber - bEndLineNumber;
                }
                return aStartColumn - bStartColumn;
            }
            return aStartLineNumber - bStartLineNumber;
        }
        int aExists = a.HasValue ? 1 : 0;
        int bExists = b.HasValue ? 1 : 0;
        return aExists - bExists;
    }

    /// <summary>
    /// A function that compares ranges, useful for sorting ranges.
    /// It will first compare ranges on the endPosition and then on the startPosition.
    /// </summary>
    public static int CompareRangesUsingEnds(Range a, Range b)
    {
        if (a.EndLineNumber == b.EndLineNumber)
        {
            if (a.EndColumn == b.EndColumn)
            {
                if (a.StartLineNumber == b.StartLineNumber)
                {
                    return a.StartColumn - b.StartColumn;
                }
                return a.StartLineNumber - b.StartLineNumber;
            }
            return a.EndColumn - b.EndColumn;
        }
        return a.EndLineNumber - b.EndLineNumber;
    }

    /// <summary>
    /// Test if the range spans multiple lines.
    /// </summary>
    public static bool SpansMultipleLines(Range range)
        => range.EndLineNumber > range.StartLineNumber;

    #endregion
}
