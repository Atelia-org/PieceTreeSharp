// Source: ts/src/vs/editor/common/core/selection.ts
// - Class: Selection
// - Lines: 1-100
// Ported: 2025-11-19
// Updated: 2025-11-26 (WS2-PORT: Range/Selection Helper APIs)

using System;

namespace PieceTree.TextBuffer.Core;

public enum SelectionDirection
{
    LTR,
    RTL
}

public readonly struct Selection
{
    public readonly TextPosition Anchor;
    public readonly TextPosition Active;

    public Selection(TextPosition anchor, TextPosition active)
    {
        Anchor = anchor;
        Active = active;
    }

    public Selection(int anchorLine, int anchorColumn, int activeLine, int activeColumn)
        : this(new TextPosition(anchorLine, anchorColumn), new TextPosition(activeLine, activeColumn))
    {
    }

    public TextPosition SelectionStart => Anchor <= Active ? Anchor : Active;
    public TextPosition SelectionEnd => Anchor <= Active ? Active : Anchor;

    // Aliases for convenience/compatibility
    public TextPosition Start => SelectionStart;
    public TextPosition End => SelectionEnd;

    public SelectionDirection Direction => Anchor <= Active ? SelectionDirection.LTR : SelectionDirection.RTL;
    public bool IsEmpty => Anchor == Active;

    public bool Contains(TextPosition position)
    {
        return position >= SelectionStart && position <= SelectionEnd;
    }

    public Selection CollapseToStart() => new(SelectionStart, SelectionStart);
    public Selection CollapseToEnd() => new(SelectionEnd, SelectionEnd);

    public override string ToString() => $"[{SelectionStart.LineNumber},{SelectionStart.Column} -> {SelectionEnd.LineNumber},{SelectionEnd.Column}]";

    #region Instance methods

    /// <summary>
    /// Create a new selection with a different start position (anchor for LTR, updates direction).
    /// </summary>
    public Selection SetStartPosition(int startLineNumber, int startColumn)
    {
        if (Direction == SelectionDirection.LTR)
        {
            return new Selection(startLineNumber, startColumn, Active.LineNumber, Active.Column);
        }
        return new Selection(Anchor.LineNumber, Anchor.Column, startLineNumber, startColumn);
    }

    /// <summary>
    /// Create a new selection with a different end position (active for LTR, updates direction).
    /// </summary>
    public Selection SetEndPosition(int endLineNumber, int endColumn)
    {
        if (Direction == SelectionDirection.LTR)
        {
            return new Selection(Anchor.LineNumber, Anchor.Column, endLineNumber, endColumn);
        }
        return new Selection(endLineNumber, endColumn, Active.LineNumber, Active.Column);
    }

    /// <summary>
    /// Get the position at the active end of the selection.
    /// </summary>
    public TextPosition GetPosition() => Active;

    /// <summary>
    /// Get the position at the anchor end of the selection.
    /// </summary>
    public TextPosition GetSelectionStart() => Anchor;

    /// <summary>
    /// Test if this selection equals other selection.
    /// </summary>
    public bool EqualsSelection(Selection other) => SelectionsEqual(this, other);

    /// <summary>
    /// Get the direction of the selection.
    /// </summary>
    public SelectionDirection GetDirection() => Direction;

    #endregion

    #region Static factory methods

    /// <summary>
    /// Create a Selection from one or two positions.
    /// </summary>
    public static Selection FromPositions(TextPosition start, TextPosition? end = null)
    {
        TextPosition endPos = end ?? start;
        return new Selection(start, endPos);
    }

    /// <summary>
    /// Creates a Selection from a range, given a direction.
    /// </summary>
    public static Selection FromRange(Range range, SelectionDirection direction)
    {
        if (direction == SelectionDirection.LTR)
        {
            return new Selection(range.StartLineNumber, range.StartColumn, range.EndLineNumber, range.EndColumn);
        }
        else
        {
            return new Selection(range.EndLineNumber, range.EndColumn, range.StartLineNumber, range.StartColumn);
        }
    }

    /// <summary>
    /// Create with a direction.
    /// </summary>
    public static Selection CreateWithDirection(
        int startLineNumber, int startColumn,
        int endLineNumber, int endColumn,
        SelectionDirection direction)
    {
        if (direction == SelectionDirection.LTR)
        {
            return new Selection(startLineNumber, startColumn, endLineNumber, endColumn);
        }
        return new Selection(endLineNumber, endColumn, startLineNumber, startColumn);
    }

    /// <summary>
    /// Create a Selection from an ISelection-like data structure.
    /// </summary>
    public static Selection LiftSelection(int selectionStartLineNumber, int selectionStartColumn, int positionLineNumber, int positionColumn)
    {
        return new Selection(selectionStartLineNumber, selectionStartColumn, positionLineNumber, positionColumn);
    }

    #endregion

    #region Static equality methods

    /// <summary>
    /// Test if the two selections are equal.
    /// </summary>
    public static bool SelectionsEqual(Selection a, Selection b)
    {
        return a.Anchor.LineNumber == b.Anchor.LineNumber
            && a.Anchor.Column == b.Anchor.Column
            && a.Active.LineNumber == b.Active.LineNumber
            && a.Active.Column == b.Active.Column;
    }

    /// <summary>
    /// Test if two selection arrays are equal.
    /// </summary>
    public static bool SelectionsArrEqual(Selection[]? a, Selection[]? b)
    {
        if (a is null && b is null)
        {
            return true;
        }
        if (a is null || b is null)
        {
            return false;
        }
        if (a.Length != b.Length)
        {
            return false;
        }
        for (int i = 0; i < a.Length; i++)
        {
            if (!SelectionsEqual(a[i], b[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Test if two nullable selections are equal.
    /// </summary>
    public static bool EqualsSelection(Selection? a, Selection? b)
    {
        if (!a.HasValue && !b.HasValue)
        {
            return true;
        }
        if (!a.HasValue || !b.HasValue)
        {
            return false;
        }
        return SelectionsEqual(a.Value, b.Value);
    }

    #endregion
}
