// Source: ts/src/vs/editor/common/model.ts
// - Enums: EndOfLineSequence, EndOfLinePreference, DefaultEndOfLine
// Source: ts/src/vs/editor/common/core/misc/textModelDefaults.ts
// - Interface: ITextModelCreationOptions (EDITOR_MODEL_DEFAULTS)
// Ported: 2025-11-19

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

public readonly record struct BracketPairColorizationOptions(bool Enabled, bool IndependentColorPoolPerBracketType)
{
    public static BracketPairColorizationOptions Default { get; } = new(true, false);
}

public sealed record class TextModelCreationOptions
{
    public bool DetectIndentation { get; init; } = true;
    public DefaultEndOfLine DefaultEol { get; init; } = DefaultEndOfLine.LF;
    public int TabSize { get; init; } = 4;
    public int IndentSize { get; init; } = 4;
    public bool IndentSizeFollowsTabSize { get; init; } = true;
    public bool InsertSpaces { get; init; } = true;
    public bool TrimAutoWhitespace { get; init; } = true;
    public bool LargeFileOptimizations { get; init; } = true;
    public bool IsForSimpleWidget { get; init; } = false;
    public BracketPairColorizationOptions BracketPairColorizationOptions { get; init; } = BracketPairColorizationOptions.Default;

    /// <summary>
    /// When true, Cursor/CursorCollection use VS Code-compatible state management.
    /// Default: false for backward compatibility during transition.
    /// </summary>
    public bool EnableVsCursorParity { get; init; } = false;

    public static TextModelCreationOptions Default { get; } = new();

    public TextModelCreationOptions Normalize(DefaultEndOfLine fallbackEol)
    {
        int tabSize = Math.Max(1, TabSize);
        int indentSize = IndentSizeFollowsTabSize ? tabSize : Math.Max(1, IndentSize);
        DefaultEndOfLine defaultEol = DefaultEol == 0 ? fallbackEol : DefaultEol;
        return this with
        {
            TabSize = tabSize,
            IndentSize = indentSize,
            DefaultEol = defaultEol
        };
    }
}

public readonly struct TextModelUpdateOptions
{
    public int? TabSize { get; init; }
    public int? IndentSize { get; init; }
    public bool? InsertSpaces { get; init; }
    public bool? TrimAutoWhitespace { get; init; }
    public BracketPairColorizationOptions? BracketPairColorizationOptions { get; init; }
}

public sealed class TextModelOptionsChangedEventArgs : EventArgs
{
    public bool TabSizeChanged { get; }
    public bool IndentSizeChanged { get; }
    public bool InsertSpacesChanged { get; }
    public bool TrimAutoWhitespaceChanged { get; }
    public bool BracketPairColorizationOptionsChanged { get; }

    public TextModelOptionsChangedEventArgs(bool tabSizeChanged, bool indentSizeChanged, bool insertSpacesChanged, bool trimAutoWhitespaceChanged, bool bracketPairColorizationOptionsChanged)
    {
        TabSizeChanged = tabSizeChanged;
        IndentSizeChanged = indentSizeChanged;
        InsertSpacesChanged = insertSpacesChanged;
        TrimAutoWhitespaceChanged = trimAutoWhitespaceChanged;
        BracketPairColorizationOptionsChanged = bracketPairColorizationOptionsChanged;
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

    public TextModelCreationOptions CreationOptions { get; }
    public int TabSize { get; }
    public int IndentSize { get; }
    public bool InsertSpaces { get; }
    public DefaultEndOfLine DefaultEol { get; }
    public bool TrimAutoWhitespace { get; }
    public bool DetectIndentation => CreationOptions.DetectIndentation;
    public bool LargeFileOptimizations => CreationOptions.LargeFileOptimizations;
    public bool IsForSimpleWidget => CreationOptions.IsForSimpleWidget;
    public BracketPairColorizationOptions BracketPairColorizationOptions => CreationOptions.BracketPairColorizationOptions;

    /// <summary>
    /// When true, Cursor/CursorCollection use VS Code-compatible state management.
    /// Default: false for backward compatibility during transition.
    /// </summary>
    public bool EnableVsCursorParity => CreationOptions.EnableVsCursorParity;

    private TextModelResolvedOptions(TextModelCreationOptions creationOptions, int tabSize, int indentSize, bool indentSizeIsTabSize, bool insertSpaces, DefaultEndOfLine defaultEol, bool trimAutoWhitespace)
    {
        CreationOptions = creationOptions;
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

    public static TextModelResolvedOptions Resolve(TextModelCreationOptions? creationOptions, DefaultEndOfLine fallbackEol)
    {
        TextModelCreationOptions normalized = (creationOptions ?? TextModelCreationOptions.Default).Normalize(fallbackEol);
        bool indentSizeIsTab = normalized.IndentSizeFollowsTabSize;
        TextModelCreationOptions sanitized = normalized with
        {
            TabSize = normalized.TabSize,
            IndentSize = normalized.IndentSize,
            IndentSizeFollowsTabSize = indentSizeIsTab
        };

        return new TextModelResolvedOptions(sanitized, sanitized.TabSize, sanitized.IndentSize, indentSizeIsTab, sanitized.InsertSpaces, sanitized.DefaultEol, sanitized.TrimAutoWhitespace);
    }

    public TextModelResolvedOptions WithDefaultEol(DefaultEndOfLine defaultEol)
    {
        if (DefaultEol == defaultEol)
        {
            return this;
        }

        TextModelCreationOptions updated = CreationOptions with { DefaultEol = defaultEol };
        return new TextModelResolvedOptions(updated, TabSize, IndentSize, _indentSizeIsTabSize, InsertSpaces, defaultEol, TrimAutoWhitespace);
    }

    public TextModelResolvedOptions WithUpdate(TextModelUpdateOptions update)
    {
        int newTabSize = update.TabSize ?? TabSize;
        bool indentSizeFollowsTab = _indentSizeIsTabSize && !update.IndentSize.HasValue;
        int newIndentSize = update.IndentSize ?? (indentSizeFollowsTab ? newTabSize : IndentSize);
        bool newInsertSpaces = update.InsertSpaces ?? InsertSpaces;
        bool newTrimAutoWhitespace = update.TrimAutoWhitespace ?? TrimAutoWhitespace;
        BracketPairColorizationOptions newBracketOptions = update.BracketPairColorizationOptions ?? BracketPairColorizationOptions;

        TextModelCreationOptions updatedCreation = CreationOptions with
        {
            TabSize = newTabSize,
            IndentSize = newIndentSize,
            IndentSizeFollowsTabSize = indentSizeFollowsTab,
            InsertSpaces = newInsertSpaces,
            TrimAutoWhitespace = newTrimAutoWhitespace,
            BracketPairColorizationOptions = newBracketOptions
        };

        return new TextModelResolvedOptions(updatedCreation, newTabSize, newIndentSize, indentSizeFollowsTab, newInsertSpaces, DefaultEol, newTrimAutoWhitespace);
    }

    public TextModelOptionsChangedEventArgs Diff(TextModelResolvedOptions other)
    {
        return new TextModelOptionsChangedEventArgs(
            tabSizeChanged: TabSize != other.TabSize,
            indentSizeChanged: IndentSize != other.IndentSize,
            insertSpacesChanged: InsertSpaces != other.InsertSpaces,
            trimAutoWhitespaceChanged: TrimAutoWhitespace != other.TrimAutoWhitespace,
            bracketPairColorizationOptionsChanged: !BracketPairColorizationOptions.Equals(other.BracketPairColorizationOptions));
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
            && TrimAutoWhitespace == other.TrimAutoWhitespace
            && BracketPairColorizationOptions.Equals(other.BracketPairColorizationOptions);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(TabSize, IndentSize, InsertSpaces, DefaultEol, TrimAutoWhitespace, BracketPairColorizationOptions);
    }
}
