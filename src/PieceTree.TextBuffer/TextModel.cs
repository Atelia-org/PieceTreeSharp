using System;
using System.Collections.Generic;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer;

public readonly struct TextEdit
{
    public readonly TextPosition Start;
    public readonly TextPosition End;
    public readonly string Text;

    public TextEdit(TextPosition start, TextPosition end, string text)
    {
        Start = start;
        End = end;
        Text = text;
    }
}

public readonly struct TextChange
{
    public readonly TextPosition Start;
    public readonly TextPosition End;
    public readonly string Text;

    public TextChange(TextPosition start, TextPosition end, string text)
    {
        Start = start;
        End = end;
        Text = text;
    }
}

public class TextModelContentChangedEventArgs : EventArgs
{
    public IReadOnlyList<TextChange> Changes { get; }
    public int VersionId { get; }
    public bool IsUndo { get; }
    public bool IsRedo { get; }
    public bool IsFlush { get; }

    public TextModelContentChangedEventArgs(IReadOnlyList<TextChange> changes, int versionId, bool isUndo, bool isRedo, bool isFlush)
    {
        Changes = changes;
        VersionId = versionId;
        IsUndo = isUndo;
        IsRedo = isRedo;
        IsFlush = isFlush;
    }
}

public class TextModel
{
    private readonly PieceTreeBuffer _buffer;
    private readonly IntervalTree _decorations = new();
    private readonly EditStack _editStack;
    private TextModelResolvedOptions _options;
    private string _languageId;
    private int _versionId = 1;
    private int _alternativeVersionId = 1;
    private string _eol;
    private bool _isUndoing;
    private bool _isRedoing;

    public event EventHandler<TextModelContentChangedEventArgs>? OnDidChangeContent;
    public event EventHandler<TextModelOptionsChangedEventArgs>? OnDidChangeOptions;
    public event EventHandler<TextModelLanguageChangedEventArgs>? OnDidChangeLanguage;

    public TextModel(string text, string defaultEol = "\n", string languageId = "plaintext")
    {
        _buffer = new PieceTreeBuffer(text);
        _languageId = string.IsNullOrWhiteSpace(languageId) ? "plaintext" : languageId;

        var normalizedEol = NormalizeEol(defaultEol);
        if (_buffer.Length == 0)
        {
            _buffer.SetEol(normalizedEol);
            _eol = normalizedEol;
        }
        else
        {
            _eol = _buffer.GetEol();
        }

        _options = TextModelResolvedOptions.CreateDefault(_eol == "\r\n" ? DefaultEndOfLine.CRLF : DefaultEndOfLine.LF);
        _editStack = new EditStack(this);
    }

    public int VersionId => _versionId;
    public int AlternativeVersionId => _alternativeVersionId;
    public string Eol => _eol;
    public bool CanUndo => _editStack.CanUndo;
    public bool CanRedo => _editStack.CanRedo;
    public string LanguageId => _languageId;

    public TextModelResolvedOptions GetOptions() => _options;

    public string GetValue() => _buffer.GetText();

    public int GetLength() => _buffer.Length;

    public int GetLineCount()
    {
        if (_buffer.Length == 0)
        {
            return 1;
        }

        return _buffer.GetPositionAt(_buffer.Length).LineNumber;
    }

    public string GetLineContent(int lineNumber)
    {
        var content = _buffer.GetLineContent(lineNumber);
        int len = content.Length;
        if (len > 0)
        {
            if (content[len - 1] == '\n')
            {
                len--;
                if (len > 0 && content[len - 1] == '\r')
                {
                    len--;
                }
            }
            else if (content[len - 1] == '\r')
            {
                len--;
            }
        }
        return content.Substring(0, len);
    }

    public int GetLineMaxColumn(int lineNumber) => GetLineContent(lineNumber).Length + 1;

    public int GetOffsetAt(TextPosition position) => _buffer.GetOffsetAt(position.LineNumber, position.Column);

    public TextPosition GetPositionAt(int offset) => _buffer.GetPositionAt(offset);

    public ModelDecoration AddDecoration(TextRange range, ModelDecorationOptions options)
    {
        var decoration = new ModelDecoration(Guid.NewGuid().ToString(), range, options);
        _decorations.Insert(decoration);
        return decoration;
    }

    public IEnumerable<ModelDecoration> GetDecorationsInRange(TextRange range) => _decorations.Search(range);

    public void PushStackElement() => _editStack.PushStackElement();

    public void PopStackElement() => _editStack.PopStackElement();

    public IReadOnlyList<TextChange> PushEditOperations(TextEdit[] edits)
    {
        if (edits is null)
        {
            throw new ArgumentNullException(nameof(edits));
        }

        return ApplyEditsInternal(edits, recordInUndoStack: true, isUndo: false, isRedo: false);
    }

    public void ApplyEdits(TextEdit[] edits)
    {
        PushEditOperations(edits ?? Array.Empty<TextEdit>());
    }

    public bool Undo()
    {
        var element = _editStack.PopUndo();
        if (element is null)
        {
            return false;
        }

        ApplyRecordedEdits(element, isUndo: true);
        return true;
    }

    public bool Redo()
    {
        var element = _editStack.PopRedoForApply();
        if (element is null)
        {
            return false;
        }

        ApplyRecordedEdits(element, isUndo: false);
        _editStack.PushRedoResult(element);
        return true;
    }

    public void PushEol(EndOfLineSequence sequence)
    {
        var target = SequenceToString(sequence);
        if (string.Equals(_eol, target, StringComparison.Ordinal))
        {
            return;
        }

        var element = _editStack.GetOrCreateElement();
        SetEolInternal(target, recordStackDelta: false, isUndo: false, isRedo: false);
        element.RecordEolChange(target, _alternativeVersionId);
    }

    public void SetEol(EndOfLineSequence sequence)
    {
        SetEolInternal(SequenceToString(sequence), recordStackDelta: false, isUndo: false, isRedo: false);
    }

    public void UpdateOptions(TextModelUpdateOptions update)
    {
        var updated = _options.WithUpdate(update);
        if (_options.Equals(updated))
        {
            return;
        }

        var diff = _options.Diff(updated);
        _options = updated;
        OnDidChangeOptions?.Invoke(this, diff);
    }

    public void DetectIndentation(bool defaultInsertSpaces, int defaultTabSize)
    {
        var maxLines = Math.Min(GetLineCount(), 200);
        var spaceSamples = new List<int>();
        var spaceIndentedLines = 0;
        var tabIndentedLines = 0;

        for (int line = 1; line <= maxLines; line++)
        {
            var content = GetLineContent(line);
            if (content.Length == 0)
            {
                continue;
            }

            int index = 0;
            while (index < content.Length && content[index] == ' ')
            {
                index++;
            }

            if (index > 0)
            {
                spaceIndentedLines++;
                spaceSamples.Add(index);
                continue;
            }

            if (index < content.Length && content[index] == '\t')
            {
                tabIndentedLines++;
            }
        }

        var useTabs = tabIndentedLines > spaceIndentedLines && tabIndentedLines > 0;
        var insertSpaces = useTabs ? false : (spaceIndentedLines > 0 || defaultInsertSpaces);
        var indentSize = defaultTabSize;

        if (spaceSamples.Count > 0)
        {
            indentSize = spaceSamples[0];
            for (int i = 1; i < spaceSamples.Count; i++)
            {
                indentSize = GreatestCommonDivisor(indentSize, spaceSamples[i]);
            }

            if (indentSize <= 0)
            {
                indentSize = defaultTabSize;
            }
        }

        UpdateOptions(new TextModelUpdateOptions
        {
            InsertSpaces = insertSpaces,
            TabSize = indentSize,
            IndentSize = indentSize,
        });
    }

    public void SetLanguage(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId) || string.Equals(languageId, _languageId, StringComparison.Ordinal))
        {
            return;
        }

        var previous = _languageId;
        _languageId = languageId;
        OnDidChangeLanguage?.Invoke(this, new TextModelLanguageChangedEventArgs(previous, languageId));
    }

    private IReadOnlyList<TextChange> ApplyEditsInternal(TextEdit[] edits, bool recordInUndoStack, bool isUndo, bool isRedo)
    {
        if (edits.Length == 0)
        {
            return Array.Empty<TextChange>();
        }

        var pending = PreparePendingEdits(edits);
        if (pending.Count == 0)
        {
            return Array.Empty<TextChange>();
        }

        ApplyPendingEdits(pending);

        foreach (var edit in pending)
        {
            edit.NewStartPosition = _buffer.GetPositionAt(edit.NewStartOffset);
            edit.NewEndPosition = _buffer.GetPositionAt(edit.NewEndOffset);
        }

        IncreaseVersionId();

        var changes = new List<TextChange>(pending.Count);
        foreach (var edit in pending)
        {
            changes.Add(new TextChange(edit.Edit.Start, edit.Edit.End, edit.NewText));
        }

        var recordedEdits = new List<RecordedEdit>(pending.Count);
        foreach (var edit in pending)
        {
            recordedEdits.Add(new RecordedEdit(
                edit.Edit.Start,
                edit.Edit.End,
                edit.NewStartPosition,
                edit.NewEndPosition,
                edit.OldStartOffset,
                edit.OldEndOffset,
                edit.NewStartOffset,
                edit.NewEndOffset,
                edit.OldText,
                edit.NewText));
        }

        OnDidChangeContent?.Invoke(this, new TextModelContentChangedEventArgs(changes, _versionId, isUndo, isRedo, false));

        if (recordInUndoStack && !_isUndoing && !_isRedoing)
        {
            var element = _editStack.GetOrCreateElement();
            element.AppendEdits(recordedEdits, _eol, _alternativeVersionId);
        }

        return changes;
    }

    private List<PendingEdit> PreparePendingEdits(TextEdit[] edits)
    {
        var pending = new List<PendingEdit>(edits.Length);
        foreach (var edit in edits)
        {
            var startOffset = _buffer.GetOffsetAt(edit.Start.LineNumber, edit.Start.Column);
            var endOffset = _buffer.GetOffsetAt(edit.End.LineNumber, edit.End.Column);
            if (endOffset < startOffset)
            {
                (startOffset, endOffset) = (endOffset, startOffset);
            }

            var newText = edit.Text ?? string.Empty;
            if (startOffset == endOffset && newText.Length == 0)
            {
                continue;
            }

            var oldText = _buffer.GetText(startOffset, endOffset - startOffset);
            pending.Add(new PendingEdit(edit, startOffset, endOffset, oldText));
        }

        pending.Sort((a, b) => a.OldStartOffset.CompareTo(b.OldStartOffset));

        int delta = 0;
        foreach (var edit in pending)
        {
            edit.NewStartOffset = edit.OldStartOffset + delta;
            edit.NewEndOffset = edit.NewStartOffset + edit.NewText.Length;
            delta += edit.NewText.Length - (edit.OldEndOffset - edit.OldStartOffset);
        }

        return pending;
    }

    private void ApplyPendingEdits(List<PendingEdit> pending)
    {
        var applyOrder = new List<PendingEdit>(pending);
        applyOrder.Sort((a, b) =>
        {
            var cmp = b.OldStartOffset.CompareTo(a.OldStartOffset);
            if (cmp != 0)
            {
                return cmp;
            }

            return b.OldEndOffset.CompareTo(a.OldEndOffset);
        });

        foreach (var edit in applyOrder)
        {
            var removedLength = edit.OldEndOffset - edit.OldStartOffset;
            _decorations.AcceptReplace(edit.OldStartOffset, removedLength, edit.NewText.Length);
            _buffer.ApplyEdit(edit.OldStartOffset, removedLength, edit.NewText);
        }
    }

    private void ApplyRecordedEdits(EditStackElement element, bool isUndo)
    {
        _isUndoing = isUndo;
        _isRedoing = !isUndo;
        try
        {
            if (element.Edits.Count > 0)
            {
                var replay = new TextEdit[element.Edits.Count];
                for (int i = 0; i < element.Edits.Count; i++)
                {
                    var recorded = element.Edits[i];
                    replay[i] = isUndo
                        ? new TextEdit(recorded.NewStart, recorded.NewEnd, recorded.OldText)
                        : new TextEdit(recorded.OldStart, recorded.OldEnd, recorded.NewText);
                }

                ApplyEditsInternal(replay, recordInUndoStack: false, isUndo: isUndo, isRedo: !isUndo);
            }

            var targetEol = isUndo ? element.BeforeEol : element.AfterEol;
            if (!string.Equals(_eol, targetEol, StringComparison.Ordinal))
            {
                SetEolInternal(targetEol, recordStackDelta: false, isUndo: isUndo, isRedo: !isUndo);
            }

            OverwriteAlternativeVersionId(isUndo ? element.BeforeVersionId : element.AfterVersionId);
        }
        finally
        {
            _isUndoing = false;
            _isRedoing = false;
        }
    }

    private void SetEolInternal(string newEol, bool recordStackDelta, bool isUndo, bool isRedo)
    {
        if (string.Equals(_eol, newEol, StringComparison.Ordinal))
        {
            return;
        }

        var oldEnd = GetDocumentEndPosition();
        _buffer.SetEol(newEol);
        _eol = newEol;
        IncreaseVersionId();

        var change = new TextChange(new TextPosition(1, 1), oldEnd, _buffer.GetText());
        OnDidChangeContent?.Invoke(this, new TextModelContentChangedEventArgs(new[] { change }, _versionId, isUndo, isRedo, false));

        if (recordStackDelta)
        {
            var element = _editStack.GetOrCreateElement();
            element.RecordEolChange(newEol, _alternativeVersionId);
        }

        _options = _options.WithDefaultEol(newEol == "\r\n" ? DefaultEndOfLine.CRLF : DefaultEndOfLine.LF);
    }

    private TextPosition GetDocumentEndPosition()
    {
        var lineCount = GetLineCount();
        var lastLine = GetLineContent(lineCount);
        return new TextPosition(lineCount, lastLine.Length + 1);
    }

    private void IncreaseVersionId()
    {
        _versionId++;
        _alternativeVersionId++;
    }

    private void OverwriteAlternativeVersionId(int newValue)
    {
        _alternativeVersionId = newValue;
    }

    private static string SequenceToString(EndOfLineSequence sequence) => sequence == EndOfLineSequence.CRLF ? "\r\n" : "\n";

    private static string NormalizeEol(string value) => string.Equals(value, "\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    private static int GreatestCommonDivisor(int left, int right)
    {
        left = Math.Abs(left);
        right = Math.Abs(right);
        while (right != 0)
        {
            var temp = right;
            right = left % right;
            left = temp;
        }

        return left == 0 ? 1 : left;
    }

    private sealed class PendingEdit
    {
        public PendingEdit(TextEdit edit, int oldStartOffset, int oldEndOffset, string oldText)
        {
            Edit = edit;
            OldStartOffset = oldStartOffset;
            OldEndOffset = oldEndOffset;
            OldText = oldText;
            NewText = edit.Text ?? string.Empty;
        }

        public TextEdit Edit { get; }
        public int OldStartOffset { get; }
        public int OldEndOffset { get; }
        public string OldText { get; }
        public string NewText { get; }
        public int NewStartOffset { get; set; }
        public int NewEndOffset { get; set; }
        public TextPosition NewStartPosition { get; set; }
        public TextPosition NewEndPosition { get; set; }
    }
}
