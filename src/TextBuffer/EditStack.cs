// Source: ts/src/vs/editor/common/model/editStack.ts
// - Class: EditStack (Lines: 384-452)
// - Class: EditStackElement (integrated into C# implementation)
// Ported: 2025-11-19

using PieceTree.TextBuffer.Services;

namespace PieceTree.TextBuffer;

internal sealed class EditStack
{
    private readonly TextModel _model;
    private readonly IUndoRedoService _undoRedoService;
    private TextModelUndoRedoElement? _openElement;

    public EditStack(TextModel model, IUndoRedoService undoRedoService)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _undoRedoService = undoRedoService ?? InProcUndoRedoService.Instance;
    }

    public bool CanUndo => _undoRedoService.CanUndo(_model);
    public bool CanRedo => _undoRedoService.CanRedo(_model);

    public EditStackElement GetOrCreateElement(string? label, IReadOnlyList<TextPosition>? beforeCursorState)
    {
        if (_openElement is null)
        {
            EditStackElement element = new(_model.AlternativeVersionId, _model.Eol, label, beforeCursorState);
            _openElement = new TextModelUndoRedoElement(_model, element);
            _undoRedoService.PushElement(_openElement);
        }
        else
        {
            _openElement.Element.UpdateLabel(label);
            _openElement.Element.CaptureBeforeCursorState(beforeCursorState);
        }

        return _openElement.Element;
    }

    public void PushStackElement()
    {
        if (_openElement is null)
        {
            return;
        }

        _undoRedoService.CloseOpenElement(_model);
        _openElement = null;
    }

    public void PopStackElement()
    {
        if (_openElement != null)
        {
            return;
        }

        _openElement = _undoRedoService.TryReopenLastElement(_model);
    }

    public void Clear()
    {
        _openElement = null;
        _undoRedoService.Clear(_model);
    }

    public TextModelUndoRedoElement? PopUndo()
    {
        PushStackElement();
        return _undoRedoService.PopUndo(_model);
    }

    public TextModelUndoRedoElement? PopRedo()
    {
        PushStackElement();
        return _undoRedoService.PopRedo(_model);
    }

    public void PushRedoResult(TextModelUndoRedoElement element)
    {
        _undoRedoService.PushRedoResult(element);
    }

    public void CloseOpenElement()
    {
        PushStackElement();
    }
}

internal sealed class EditStackElement
{
    private readonly List<RecordedEdit> _edits = [];

    public EditStackElement(int beforeVersionId, string beforeEol, string? label, IReadOnlyList<TextPosition>? beforeCursorState)
    {
        BeforeVersionId = beforeVersionId;
        AfterVersionId = beforeVersionId;
        BeforeEol = beforeEol;
        AfterEol = beforeEol;
        Label = label;
        BeforeCursorState = beforeCursorState;
    }

    public IReadOnlyList<RecordedEdit> Edits => _edits;
    public int BeforeVersionId { get; }
    public int AfterVersionId { get; private set; }
    public string BeforeEol { get; }
    public string AfterEol { get; private set; }
    public string? Label { get; private set; }
    public IReadOnlyList<TextPosition>? BeforeCursorState { get; private set; }
    public IReadOnlyList<TextPosition>? AfterCursorState { get; private set; }

    public bool HasEffect => _edits.Count > 0 || !string.Equals(BeforeEol, AfterEol, StringComparison.Ordinal);

    public void UpdateLabel(string? label)
    {
        if (!string.IsNullOrEmpty(label) && string.IsNullOrEmpty(Label))
        {
            Label = label;
        }
    }

    public void CaptureBeforeCursorState(IReadOnlyList<TextPosition>? cursorState)
    {
        if (cursorState != null && BeforeCursorState == null)
        {
            BeforeCursorState = cursorState;
        }
    }

    public void AppendEdits(IEnumerable<RecordedEdit> edits, string currentEol, int alternativeVersionId, IReadOnlyList<TextPosition>? afterCursorState)
    {
        _edits.AddRange(edits);
        AfterEol = currentEol;
        AfterVersionId = alternativeVersionId;
        if (afterCursorState != null)
        {
            AfterCursorState = afterCursorState;
        }
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
