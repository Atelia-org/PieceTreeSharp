// Source: ts/src/vs/editor/common/cursor/oneCursor.ts
// - Class: Cursor (Lines: 15-200)
// Ported: 2025-11-22

using System;
using System.Collections.Generic;
using System.Linq;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer;

namespace PieceTree.TextBuffer.Cursor;

public class Cursor : IDisposable
{
    private readonly TextModel _model;
    private Selection _selection;
    private int _stickyColumn = -1;
    private bool _isColumnSelecting = false;
    private int _columnSelectAnchorVisible = -1;
    private TextPosition _columnSelectAnchorPosition = new TextPosition(1,1);
    private readonly int _ownerId;
    private string[] _decorationIds = Array.Empty<string>();
    private bool _disposed;

    public Cursor(TextModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _selection = new Selection(new TextPosition(1, 1), new TextPosition(1, 1));
        _ownerId = _model.AllocateDecorationOwnerId();
        _model.OnDidChangeOptions += HandleOptionsChanged;
        UpdateDecorations();
    }

    public Selection Selection => _selection;

    public void MoveTo(TextPosition position)
    {
        var validated = ValidatePosition(position);
        _selection = new Selection(validated, validated);
        _stickyColumn = -1;
        UpdateDecorations();
    }

    public void StartColumnSelection()
    {
        var tabSize = _model.GetOptions().TabSize;
        _isColumnSelecting = true;
        _columnSelectAnchorVisible = CursorColumns.GetVisibleColumnFromPosition(_model, _selection.Active, tabSize);
        _columnSelectAnchorPosition = _selection.Active;
    }

    public void ColumnSelectTo(TextPosition active)
    {
        if (!_isColumnSelecting)
        {
            // fall back to normal selection
            SelectTo(active);
            return;
        }

        var tabSize = _model.GetOptions().TabSize;
        var anchorReal = CursorColumns.GetPositionFromVisibleColumn(_model, _columnSelectAnchorPosition.LineNumber, _columnSelectAnchorVisible, tabSize);
        var activeVisible = CursorColumns.GetVisibleColumnFromPosition(_model, active, tabSize);
        var activeReal = CursorColumns.GetPositionFromVisibleColumn(_model, active.LineNumber, activeVisible, tabSize);
        _selection = new Selection(anchorReal, activeReal);
        UpdateDecorations();
    }

    public void EndColumnSelection()
    {
        _isColumnSelecting = false;
        _columnSelectAnchorVisible = -1;
    }

    public void SelectTo(TextPosition position)
    {
        var validated = ValidatePosition(position);
        _selection = new Selection(_selection.Anchor, validated);
        _stickyColumn = -1;
        UpdateDecorations();
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

    public void MoveWordLeft(string? wordSeparators = null)
    {
        var current = _selection.Active;
        var target = WordOperations.MoveWordLeft(_model, current, wordSeparators);
        MoveTo(target);
    }

    public void MoveWordRight(string? wordSeparators = null)
    {
        var current = _selection.Active;
        var target = WordOperations.MoveWordRight(_model, current, wordSeparators);
        MoveTo(target);
    }

    public void SelectWordLeft(string? wordSeparators = null)
    {
        _selection = WordOperations.SelectWordLeft(_model, _selection, wordSeparators);
        UpdateDecorations();
    }

    public void SelectWordRight(string? wordSeparators = null)
    {
        _selection = WordOperations.SelectWordRight(_model, _selection, wordSeparators);
        UpdateDecorations();
    }

    public void DeleteWordLeft(string? wordSeparators = null)
    {
        var sel = WordOperations.DeleteWordLeft(_model, _selection, wordSeparators);
        var start = sel.Start;
        var end = sel.End;
        var edit = new TextEdit(start, end, string.Empty);
        _model.PushEditOperations(new[] { edit }, null);
        // Move cursor to start
        MoveTo(start);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _model.OnDidChangeOptions -= HandleOptionsChanged;
        _model.RemoveAllDecorations(_ownerId);
    }

    private TextPosition ValidatePosition(TextPosition position)
    {
        var lineCount = _model.GetLineCount();
        var line = Math.Clamp(position.LineNumber, 1, lineCount);
        
        var lineLen = _model.GetLineContent(line).Length;
        var col = Math.Clamp(position.Column, 1, lineLen + 1);
        
        return new TextPosition(line, col);
    }

    private void UpdateDecorations()
    {
        if (_disposed)
        {
            return;
        }

        var specs = BuildDecorations();
        var created = _model.DeltaDecorations(_ownerId, _decorationIds, specs);
        _decorationIds = created.Select(d => d.Id).ToArray();
    }

    private IReadOnlyList<ModelDeltaDecoration> BuildDecorations()
    {
        var result = new List<ModelDeltaDecoration>();

        var activeOffset = _model.GetOffsetAt(_selection.Active);
        result.Add(new ModelDeltaDecoration(new TextRange(activeOffset, activeOffset), ModelDecorationOptions.CreateCursorOptions()));

        if (!_selection.IsEmpty)
        {
            var startOffset = _model.GetOffsetAt(_selection.Start);
            var endOffset = _model.GetOffsetAt(_selection.End);
            result.Add(new ModelDeltaDecoration(new TextRange(startOffset, endOffset), ModelDecorationOptions.CreateSelectionOptions()));
        }

        return result;
    }

    private void HandleOptionsChanged(object? sender, TextModelOptionsChangedEventArgs e)
    {
        _stickyColumn = -1;
        // TODO(SnippetController): integrate snippet tabstop stickiness so column resets fold into snippet navigation once ported.
    }
}
