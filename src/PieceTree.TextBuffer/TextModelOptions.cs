using System;

namespace PieceTree.TextBuffer;

public enum EndOfLineSequence
{
    LF = 0,
    CRLF = 1,
}

public enum EndOfLinePreference
{
    TextDefined = 0,
    LF = 1,
    CRLF = 2,
}

public enum DefaultEndOfLine
{
    LF = 1,
    CRLF = 2,
}

public readonly struct TextModelUpdateOptions
{
    public int? TabSize { get; init; }
    public int? IndentSize { get; init; }
    public bool? InsertSpaces { get; init; }
    public bool? TrimAutoWhitespace { get; init; }
}

public sealed class TextModelOptionsChangedEventArgs : EventArgs
{
    public bool TabSizeChanged { get; }
    public bool IndentSizeChanged { get; }
    public bool InsertSpacesChanged { get; }
    public bool TrimAutoWhitespaceChanged { get; }

    public TextModelOptionsChangedEventArgs(bool tabSizeChanged, bool indentSizeChanged, bool insertSpacesChanged, bool trimAutoWhitespaceChanged)
    {
        TabSizeChanged = tabSizeChanged;
        IndentSizeChanged = indentSizeChanged;
        InsertSpacesChanged = insertSpacesChanged;
        TrimAutoWhitespaceChanged = trimAutoWhitespaceChanged;
    }
}

public sealed class TextModelLanguageChangedEventArgs : EventArgs
{
    public string OldLanguageId { get; }
    public string NewLanguageId { get; }

    public TextModelLanguageChangedEventArgs(string oldLanguageId, string newLanguageId)
    {
        OldLanguageId = oldLanguageId;
        NewLanguageId = newLanguageId;
    }
}

public sealed class TextModelResolvedOptions
{
    private readonly bool _indentSizeIsTabSize;

    public int TabSize { get; }
    public int IndentSize { get; }
    public bool InsertSpaces { get; }
    public DefaultEndOfLine DefaultEol { get; }
    public bool TrimAutoWhitespace { get; }

    public TextModelResolvedOptions(int tabSize, int indentSize, bool indentSizeIsTabSize, bool insertSpaces, DefaultEndOfLine defaultEol, bool trimAutoWhitespace)
    {
        TabSize = Math.Max(1, tabSize);
        if (indentSizeIsTabSize)
        {
            IndentSize = TabSize;
            _indentSizeIsTabSize = true;
        }
        else
        {
            IndentSize = Math.Max(1, indentSize);
            _indentSizeIsTabSize = false;
        }

        InsertSpaces = insertSpaces;
        DefaultEol = defaultEol;
        TrimAutoWhitespace = trimAutoWhitespace;
    }

    public static TextModelResolvedOptions CreateDefault(DefaultEndOfLine defaultEol)
    {
        return new TextModelResolvedOptions(tabSize: 4, indentSize: 4, indentSizeIsTabSize: true, insertSpaces: true, defaultEol, trimAutoWhitespace: false);
    }

    public TextModelResolvedOptions WithDefaultEol(DefaultEndOfLine defaultEol)
    {
        if (DefaultEol == defaultEol)
        {
            return this;
        }

        return new TextModelResolvedOptions(TabSize, IndentSize, _indentSizeIsTabSize, InsertSpaces, defaultEol, TrimAutoWhitespace);
    }

    public TextModelResolvedOptions WithUpdate(TextModelUpdateOptions update)
    {
        var newTabSize = update.TabSize ?? TabSize;
        var indentSizeFollowsTab = _indentSizeIsTabSize && !update.IndentSize.HasValue;
        var newIndentSize = update.IndentSize ?? (indentSizeFollowsTab ? newTabSize : IndentSize);
        var newInsertSpaces = update.InsertSpaces ?? InsertSpaces;
        var newTrimAutoWhitespace = update.TrimAutoWhitespace ?? TrimAutoWhitespace;

        return new TextModelResolvedOptions(newTabSize, newIndentSize, indentSizeFollowsTab, newInsertSpaces, DefaultEol, newTrimAutoWhitespace);
    }

    public TextModelOptionsChangedEventArgs Diff(TextModelResolvedOptions other)
    {
        return new TextModelOptionsChangedEventArgs(
            tabSizeChanged: TabSize != other.TabSize,
            indentSizeChanged: IndentSize != other.IndentSize,
            insertSpacesChanged: InsertSpaces != other.InsertSpaces,
            trimAutoWhitespaceChanged: TrimAutoWhitespace != other.TrimAutoWhitespace);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not TextModelResolvedOptions other)
        {
            return false;
        }

        return TabSize == other.TabSize
            && IndentSize == other.IndentSize
            && InsertSpaces == other.InsertSpaces
            && DefaultEol == other.DefaultEol
            && TrimAutoWhitespace == other.TrimAutoWhitespace;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TabSize, IndentSize, InsertSpaces, DefaultEol, TrimAutoWhitespace);
    }
}
