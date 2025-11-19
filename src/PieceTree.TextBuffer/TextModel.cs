using System;
using System.Collections.Generic;
using PieceTree.TextBuffer.Core;

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
    private int _versionId = 1;
    private int _alternativeVersionId = 1;
    private string _eol = "\n";

    public event EventHandler<TextModelContentChangedEventArgs>? OnDidChangeContent;

    public TextModel(string text, string defaultEol = "\n")
    {
        _buffer = new PieceTreeBuffer(text);
        _eol = defaultEol;
    }

    public int VersionId => _versionId;
    public int AlternativeVersionId => _alternativeVersionId;
    public string Eol => _eol;

    public string GetValue() => _buffer.GetText();
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
    public int GetLength() => _buffer.Length;
    
    public int GetLineCount()
    {
        if (_buffer.Length == 0) return 1;
        return _buffer.GetPositionAt(_buffer.Length).LineNumber;
    }

    public void ApplyEdits(TextEdit[] edits)
    {
        if (edits == null || edits.Length == 0) return;

        // Sort edits descending to apply them without invalidating offsets of earlier edits
        var sortedEdits = new List<TextEdit>(edits);
        sortedEdits.Sort((a, b) => 
        {
            int cmp = b.Start.CompareTo(a.Start);
            if (cmp != 0) return cmp;
            return b.End.CompareTo(a.End);
        });

        var changes = new List<TextChange>();

        foreach (var edit in sortedEdits)
        {
            int startOffset = _buffer.GetOffsetAt(edit.Start.LineNumber, edit.Start.Column);
            int endOffset = _buffer.GetOffsetAt(edit.End.LineNumber, edit.End.Column);
            int length = endOffset - startOffset;

            _buffer.ApplyEdit(startOffset, length, edit.Text);
            changes.Add(new TextChange(edit.Start, edit.End, edit.Text));
        }

        _versionId++;
        _alternativeVersionId++;
        
        OnDidChangeContent?.Invoke(this, new TextModelContentChangedEventArgs(changes, _versionId, false, false, false));
    }
}
