using System;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Cursor;

public class Cursor
{
    private readonly TextModel _model;
    private Selection _selection;
    private int _stickyColumn = -1;

    public Cursor(TextModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _selection = new Selection(new TextPosition(1, 1), new TextPosition(1, 1));
    }

    public Selection Selection => _selection;

    public void MoveTo(TextPosition position)
    {
        var validated = ValidatePosition(position);
        _selection = new Selection(validated, validated);
        _stickyColumn = -1;
    }

    public void SelectTo(TextPosition position)
    {
        var validated = ValidatePosition(position);
        _selection = new Selection(_selection.Anchor, validated);
        _stickyColumn = -1;
    }

    public void MoveLeft()
    {
        var current = _selection.Active;
        if (current.Column > 1)
        {
            MoveTo(new TextPosition(current.LineNumber, current.Column - 1));
        }
        else if (current.LineNumber > 1)
        {
            var prevLine = current.LineNumber - 1;
            var len = _model.GetLineContent(prevLine).Length;
            MoveTo(new TextPosition(prevLine, len + 1));
        }
    }

    public void MoveRight()
    {
        var current = _selection.Active;
        var lineLen = _model.GetLineContent(current.LineNumber).Length;
        
        if (current.Column <= lineLen)
        {
            MoveTo(new TextPosition(current.LineNumber, current.Column + 1));
        }
        else if (current.LineNumber < _model.GetLineCount())
        {
            MoveTo(new TextPosition(current.LineNumber + 1, 1));
        }
    }

    public void MoveUp()
    {
        var current = _selection.Active;
        if (current.LineNumber > 1)
        {
            int sticky = _stickyColumn;
            if (sticky == -1) sticky = current.Column;

            var newLine = current.LineNumber - 1;
            var len = _model.GetLineContent(newLine).Length;
            var newCol = Math.Min(sticky, len + 1);
            
            MoveTo(new TextPosition(newLine, newCol));
            _stickyColumn = sticky;
        }
    }

    public void MoveDown()
    {
        var current = _selection.Active;
        if (current.LineNumber < _model.GetLineCount())
        {
            int sticky = _stickyColumn;
            if (sticky == -1) sticky = current.Column;

            var newLine = current.LineNumber + 1;
            var len = _model.GetLineContent(newLine).Length;
            var newCol = Math.Min(sticky, len + 1);
            
            MoveTo(new TextPosition(newLine, newCol));
            _stickyColumn = sticky;
        }
    }

    private TextPosition ValidatePosition(TextPosition position)
    {
        var lineCount = _model.GetLineCount();
        var line = Math.Clamp(position.LineNumber, 1, lineCount);
        
        var lineLen = _model.GetLineContent(line).Length;
        var col = Math.Clamp(position.Column, 1, lineLen + 1);
        
        return new TextPosition(line, col);
    }
}
