using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer;

internal sealed class EditStack
{
    private readonly TextModel _model;
    private readonly List<EditStackElement> _undoStack = new();
    private readonly List<EditStackElement> _redoStack = new();
    private EditStackElement? _openElement;

    public EditStack(TextModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public EditStackElement GetOrCreateElement()
    {
        if (_openElement is null)
        {
            var element = new EditStackElement(_model.AlternativeVersionId, _model.Eol);
            _undoStack.Add(element);
            _openElement = element;
            _redoStack.Clear();
        }

        return _openElement;
    }

    public void PushStackElement()
    {
        CloseOpenElement();
    }

    public void PopStackElement()
    {
        if (_openElement is null && _undoStack.Count > 0)
        {
            _openElement = _undoStack[_undoStack.Count - 1];
        }
    }

    public void Clear()
    {
        _openElement = null;
        _undoStack.Clear();
        _redoStack.Clear();
    }

    public EditStackElement? PopUndo()
    {
        CloseOpenElement();
        if (_undoStack.Count == 0)
        {
            return null;
        }

        var element = _undoStack[_undoStack.Count - 1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        _redoStack.Add(element);
        return element;
    }

    public EditStackElement? PopRedoForApply()
    {
        CloseOpenElement();
        if (_redoStack.Count == 0)
        {
            return null;
        }

        var element = _redoStack[_redoStack.Count - 1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        return element;
    }

    public void PushRedoResult(EditStackElement element)
    {
        _undoStack.Add(element);
    }

    private void CloseOpenElement()
    {
        if (_openElement is null)
        {
            return;
        }

        if (_undoStack.Count > 0 && ReferenceEquals(_undoStack[_undoStack.Count - 1], _openElement) && !_openElement.HasEffect)
        {
            _undoStack.RemoveAt(_undoStack.Count - 1);
        }

        _openElement = null;
    }
}

internal sealed class EditStackElement
{
    private readonly List<RecordedEdit> _edits = new();

    public EditStackElement(int beforeVersionId, string beforeEol)
    {
        BeforeVersionId = beforeVersionId;
        AfterVersionId = beforeVersionId;
        BeforeEol = beforeEol;
        AfterEol = beforeEol;
    }

    public IReadOnlyList<RecordedEdit> Edits => _edits;
    public int BeforeVersionId { get; }
    public int AfterVersionId { get; private set; }
    public string BeforeEol { get; }
    public string AfterEol { get; private set; }

    public bool HasEffect => _edits.Count > 0 || !string.Equals(BeforeEol, AfterEol, StringComparison.Ordinal);

    public void AppendEdits(IEnumerable<RecordedEdit> edits, string currentEol, int alternativeVersionId)
    {
        _edits.AddRange(edits);
        AfterEol = currentEol;
        AfterVersionId = alternativeVersionId;
    }

    public void RecordEolChange(string eol, int alternativeVersionId)
    {
        AfterEol = eol;
        AfterVersionId = alternativeVersionId;
    }
}

internal sealed record class RecordedEdit(
    TextPosition OldStart,
    TextPosition OldEnd,
    TextPosition NewStart,
    TextPosition NewEnd,
    int OldStartOffset,
    int OldEndOffset,
    int NewStartOffset,
    int NewEndOffset,
    string OldText,
    string NewText);
