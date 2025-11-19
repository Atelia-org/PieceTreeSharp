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

    public Selection CollapseToStart() => new Selection(SelectionStart, SelectionStart);
    public Selection CollapseToEnd() => new Selection(SelectionEnd, SelectionEnd);
    
    public override string ToString() => $"[{SelectionStart.LineNumber},{SelectionStart.Column} -> {SelectionEnd.LineNumber},{SelectionEnd.Column}]";
}
