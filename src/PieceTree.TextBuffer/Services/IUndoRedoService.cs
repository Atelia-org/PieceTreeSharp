using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Services;

internal interface IUndoRedoService
{
    void PushElement(TextModelUndoRedoElement element);
    void CloseOpenElement(TextModel model);
    TextModelUndoRedoElement? TryReopenLastElement(TextModel model);
    TextModelUndoRedoElement? PopUndo(TextModel model);
    TextModelUndoRedoElement? PopRedo(TextModel model);
    void PushRedoResult(TextModelUndoRedoElement element);
    bool CanUndo(TextModel model);
    bool CanRedo(TextModel model);
    void Clear(TextModel model);
}

internal sealed class TextModelUndoRedoElement
{
    public TextModelUndoRedoElement(TextModel model, EditStackElement element)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Element = element ?? throw new ArgumentNullException(nameof(element));
    }

    public TextModel Model { get; }
    internal EditStackElement Element { get; }
}

internal sealed class InProcUndoRedoService : IUndoRedoService
{
    public static InProcUndoRedoService Instance { get; } = new();

    private readonly Dictionary<TextModel, UndoRedoState> _states = new();

    private InProcUndoRedoService()
    {
    }

    public void PushElement(TextModelUndoRedoElement element)
    {
        var state = GetState(element.Model);
        state.OpenElement = element;
        state.Undo.Push(element);
        state.Redo.Clear();
    }

    public void CloseOpenElement(TextModel model)
    {
        var state = GetState(model);
        state.OpenElement = null;
    }

    public TextModelUndoRedoElement? TryReopenLastElement(TextModel model)
    {
        var state = GetState(model);
        if (state.Undo.Count == 0 || state.Redo.Count > 0)
        {
            return null;
        }

        var element = state.Undo.Peek();
        state.OpenElement = element;
        return element;
    }

    public TextModelUndoRedoElement? PopUndo(TextModel model)
    {
        var state = GetState(model);
        state.OpenElement = null;
        if (state.Undo.Count == 0)
        {
            return null;
        }

        var element = state.Undo.Pop();
        state.Redo.Push(element);
        return element;
    }

    public TextModelUndoRedoElement? PopRedo(TextModel model)
    {
        var state = GetState(model);
        state.OpenElement = null;
        if (state.Redo.Count == 0)
        {
            return null;
        }

        return state.Redo.Pop();
    }

    public void PushRedoResult(TextModelUndoRedoElement element)
    {
        var state = GetState(element.Model);
        state.OpenElement = null;
        state.Undo.Push(element);
    }

    public bool CanUndo(TextModel model) => GetState(model).Undo.Count > 0;

    public bool CanRedo(TextModel model) => GetState(model).Redo.Count > 0;

    public void Clear(TextModel model)
    {
        _states.Remove(model);
    }

    private UndoRedoState GetState(TextModel model)
    {
        if (!_states.TryGetValue(model, out var state))
        {
            state = new UndoRedoState();
            _states[model] = state;
        }

        return state;
    }

    private sealed class UndoRedoState
    {
        public Stack<TextModelUndoRedoElement> Undo { get; } = new();
        public Stack<TextModelUndoRedoElement> Redo { get; } = new();
        public TextModelUndoRedoElement? OpenElement { get; set; }
    }
}
