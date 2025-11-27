// Source: ts/src/vs/editor/common/cursorCommon.ts
// - Class: CursorConfiguration (Lines: 30-180)
// Ported: 2025-11-26 (WS4-PORT-Core Stage 0)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Data used for column selection operations.
/// </summary>
public readonly struct ColumnSelectData
{
    public bool IsReal { get; init; }
    public int FromViewLineNumber { get; init; }
    public int FromViewVisualColumn { get; init; }
    public int ToViewLineNumber { get; init; }
    public int ToViewVisualColumn { get; init; }

    public ColumnSelectData(bool isReal, int fromViewLineNumber, int fromViewVisualColumn, int toViewLineNumber, int toViewVisualColumn)
    {
        IsReal = isReal;
        FromViewLineNumber = fromViewLineNumber;
        FromViewVisualColumn = fromViewVisualColumn;
        ToViewLineNumber = toViewLineNumber;
        ToViewVisualColumn = toViewVisualColumn;
    }
}

/// <summary>
/// Operation type for undo/redo purposes.
/// The goal is to introduce an undo stop when the controller switches between different operation types.
/// </summary>
public enum EditOperationType
{
    Other = 0,
    DeletingLeft = 2,
    DeletingRight = 3,
    TypingOther = 4,
    TypingFirstSpace = 5,
    TypingConsecutiveSpace = 6,
}

/// <summary>
/// Holds cursor configuration derived from editor and model options.
/// Mirrors VS Code's CursorConfiguration semantics where practical.
/// </summary>
public sealed class CursorConfiguration
{
    private readonly object _cursorMoveConfigurationBrand = new();
    private readonly LanguageConfigurationOptions _language;

    public bool ReadOnly { get; }
    public int TabSize { get; }
    public int IndentSize { get; }
    public bool InsertSpaces { get; }
    public bool StickyTabStops { get; }
    public int PageSize { get; }
    public int LineHeight { get; }
    public int TypicalHalfwidthCharacterWidth { get; }
    public bool UseTabStops { get; }
    public bool TrimWhitespaceOnDelete { get; }
    public string WordSeparators { get; }
    public bool EmptySelectionClipboard { get; }
    public bool CopyWithSyntaxHighlighting { get; }
    public bool MultiCursorMergeOverlapping { get; }
    public MultiCursorPasteMode MultiCursorPaste { get; }
    public int MultiCursorLimit { get; }
    public EditorAutoClosingStrategy AutoClosingBrackets { get; }
    public EditorAutoClosingStrategy AutoClosingComments { get; }
    public EditorAutoClosingStrategy AutoClosingQuotes { get; }
    public EditorAutoClosingEditStrategy AutoClosingDelete { get; }
    public EditorAutoClosingEditStrategy AutoClosingOvertype { get; }
    public EditorAutoSurroundStrategy AutoSurround { get; }
    public EditorAutoIndentStrategy AutoIndent { get; }
    public AutoClosingPairs AutoClosingPairs { get; }
    public IReadOnlyDictionary<string, string> SurroundingPairs { get; }
    public string? BlockCommentStartToken { get; }
    public AutoCloseBeforePredicates ShouldAutoCloseBefore { get; }
    public IReadOnlyList<string> WordSegmenterLocales { get; }
    public bool OvertypeOnPaste { get; }

    public const string DefaultWordSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";

    public CursorConfiguration(TextModelResolvedOptions modelOptions, EditorCursorOptions? editorOptions = null)
    {
        ArgumentNullException.ThrowIfNull(modelOptions);

        EditorCursorOptions opts = editorOptions ?? EditorCursorOptions.Default;
        _language = opts.LanguageConfiguration ?? LanguageConfigurationOptions.Default;

        ReadOnly = opts.ReadOnly;
        TabSize = modelOptions.TabSize;
        IndentSize = modelOptions.IndentSize;
        InsertSpaces = modelOptions.InsertSpaces;
        StickyTabStops = opts.StickyTabStops;
        PageSize = Math.Max(1, opts.PageSize);
        LineHeight = Math.Max(1, opts.LineHeight);
        TypicalHalfwidthCharacterWidth = Math.Max(1, opts.TypicalHalfwidthCharacterWidth);
        UseTabStops = opts.UseTabStops;
        TrimWhitespaceOnDelete = opts.TrimWhitespaceOnDelete;
        WordSeparators = string.IsNullOrEmpty(opts.WordSeparators) ? DefaultWordSeparators : opts.WordSeparators!;
        EmptySelectionClipboard = opts.EmptySelectionClipboard;
        CopyWithSyntaxHighlighting = opts.CopyWithSyntaxHighlighting;
        MultiCursorMergeOverlapping = opts.MultiCursorMergeOverlapping;
        MultiCursorPaste = opts.MultiCursorPaste;
        MultiCursorLimit = Math.Max(1, opts.MultiCursorLimit);
        AutoClosingBrackets = opts.AutoClosingBrackets;
        AutoClosingComments = opts.AutoClosingComments;
        AutoClosingQuotes = opts.AutoClosingQuotes;
        AutoClosingDelete = opts.AutoClosingDelete;
        AutoClosingOvertype = opts.AutoClosingOvertype;
        AutoSurround = opts.AutoSurround;
        AutoIndent = opts.AutoIndent;
        WordSegmenterLocales = opts.WordSegmenterLocales is { Length: > 0 }
            ? Array.AsReadOnly((string[])opts.WordSegmenterLocales.Clone())
            : Array.Empty<string>();
        OvertypeOnPaste = opts.OvertypeOnPaste;

        AutoClosingPairs = new AutoClosingPairs(_language.AutoClosingPairs);
        SurroundingPairs = CreateSurroundingPairsDictionary(_language.SurroundingPairs);
        BlockCommentStartToken = _language.BlockCommentStartToken;
        ShouldAutoCloseBefore = BuildAutoClosePredicates(opts);
    }

    public int VisibleColumnFromColumn(ICursorSimpleModel model, TextPosition position)
    {
        ArgumentNullException.ThrowIfNull(model);
        string lineContent = model.GetLineContent(position.LineNumber);
        return CursorColumnsHelper.VisibleColumnFromColumn(lineContent, position.Column, TabSize);
    }

    public int ColumnFromVisibleColumn(ICursorSimpleModel model, int lineNumber, int visibleColumn)
    {
        ArgumentNullException.ThrowIfNull(model);

        string lineContent = model.GetLineContent(lineNumber);
        int result = CursorColumnsHelper.ColumnFromVisibleColumn(lineContent, visibleColumn, TabSize);

        int minColumn = model.GetLineMinColumn(lineNumber);
        if (result < minColumn)
        {
            return minColumn;
        }

        int maxColumn = model.GetLineMaxColumn(lineNumber);
        if (result > maxColumn)
        {
            return maxColumn;
        }

        return result;
    }

    /// <summary>
    /// Normalize indentation according to the configuration's tab/space preferences.
    /// </summary>
    public string NormalizeIndentation(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        int firstNonWhitespace = FirstNonWhitespaceIndex(text);
        if (firstNonWhitespace == -1)
        {
            return NormalizeLeadingWhitespace(text);
        }

        string prefix = NormalizeLeadingWhitespace(text[..firstNonWhitespace]);
        return prefix + text[firstNonWhitespace..];
    }

    private string NormalizeLeadingWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        int visible = 0;
        foreach (char ch in text)
        {
            visible = ch == '\t'
                ? CursorColumnsHelper.NextIndentTabStop(visible, IndentSize)
                : visible + 1;
        }

        if (!InsertSpaces)
        {
            int tabs = visible / IndentSize;
            int spaces = visible % IndentSize;
            return new string('\t', tabs) + new string(' ', spaces);
        }

        return new string(' ', visible);
    }

    private static int FirstNonWhitespaceIndex(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch != ' ' && ch != '\t')
            {
                return i;
            }
        }
        return -1;
    }

    private AutoCloseBeforePredicates BuildAutoClosePredicates(EditorCursorOptions options)
    {
        return new AutoCloseBeforePredicates(
            BuildPredicate(options.AutoClosingQuotes, _language.AutoCloseBeforeQuotes),
            BuildPredicate(options.AutoClosingBrackets, _language.AutoCloseBeforeBrackets),
            BuildPredicate(options.AutoClosingComments, _language.AutoCloseBeforeComments));
    }

    private static Func<char, bool> BuildPredicate(EditorAutoClosingStrategy strategy, string? charSet)
    {
        return strategy switch
        {
            EditorAutoClosingStrategy.Always => static _ => true,
            EditorAutoClosingStrategy.Never => static _ => false,
            EditorAutoClosingStrategy.BeforeWhitespace => static ch => IsWhitespaceOrTab(ch),
            EditorAutoClosingStrategy.LanguageDefined => BuildLanguageDefinedPredicate(charSet),
            _ => static _ => true,
        };
    }

    private static Func<char, bool> BuildLanguageDefinedPredicate(string? charSet)
    {
        if (string.IsNullOrEmpty(charSet))
        {
            return static _ => true;
        }

        HashSet<char> lookup = new(charSet);
        return ch => lookup.Contains(ch);
    }

    private static bool IsWhitespaceOrTab(char ch) => ch == ' ' || ch == '\t';

    private static IReadOnlyDictionary<string, string> CreateSurroundingPairsDictionary(IEnumerable<SurroundingPairDefinition> pairs)
    {
        Dictionary<string, string> dict = new(StringComparer.Ordinal);
        if (pairs != null)
        {
            foreach (SurroundingPairDefinition pair in pairs)
            {
                if (!string.IsNullOrEmpty(pair.Open) && !string.IsNullOrEmpty(pair.Close))
                {
                    dict[pair.Open] = pair.Close;
                }
            }
        }
        return new ReadOnlyDictionary<string, string>(dict);
    }
}

/// <summary>
/// Editor-level cursor options (distinct from model options).
/// </summary>
public sealed class EditorCursorOptions
{
    public bool ReadOnly { get; init; }
    public bool StickyTabStops { get; init; }
    public int PageSize { get; init; } = 10;
    public int LineHeight { get; init; } = 18;
    public int TypicalHalfwidthCharacterWidth { get; init; } = 8;
    public bool UseTabStops { get; init; } = true;
    public bool TrimWhitespaceOnDelete { get; init; } = true;
    public string? WordSeparators { get; init; }
    public bool EmptySelectionClipboard { get; init; } = true;
    public bool CopyWithSyntaxHighlighting { get; init; } = true;
    public bool MultiCursorMergeOverlapping { get; init; } = true;
    public MultiCursorPasteMode MultiCursorPaste { get; init; } = MultiCursorPasteMode.Spread;
    public int MultiCursorLimit { get; init; } = 10000;
    public EditorAutoClosingStrategy AutoClosingBrackets { get; init; } = EditorAutoClosingStrategy.LanguageDefined;
    public EditorAutoClosingStrategy AutoClosingComments { get; init; } = EditorAutoClosingStrategy.LanguageDefined;
    public EditorAutoClosingStrategy AutoClosingQuotes { get; init; } = EditorAutoClosingStrategy.LanguageDefined;
    public EditorAutoClosingEditStrategy AutoClosingDelete { get; init; } = EditorAutoClosingEditStrategy.Auto;
    public EditorAutoClosingEditStrategy AutoClosingOvertype { get; init; } = EditorAutoClosingEditStrategy.Auto;
    public EditorAutoSurroundStrategy AutoSurround { get; init; } = EditorAutoSurroundStrategy.LanguageDefined;
    public EditorAutoIndentStrategy AutoIndent { get; init; } = EditorAutoIndentStrategy.Full;
    public string[]? WordSegmenterLocales { get; init; }
    public bool OvertypeOnPaste { get; init; }
    public LanguageConfigurationOptions LanguageConfiguration { get; init; } = LanguageConfigurationOptions.Default;

    public static EditorCursorOptions Default { get; } = new();
}

public enum MultiCursorPasteMode
{
    Spread,
    Full,
}

public enum EditorAutoClosingStrategy
{
    Always,
    LanguageDefined,
    BeforeWhitespace,
    Never,
}

public enum EditorAutoSurroundStrategy
{
    LanguageDefined,
    Quotes,
    Brackets,
    Never,
}

public enum EditorAutoClosingEditStrategy
{
    Always,
    Auto,
    Never,
}

public enum EditorAutoIndentStrategy
{
    None = 0,
    Keep = 1,
    Brackets = 2,
    Advanced = 3,
    Full = 4,
}

public sealed class AutoCloseBeforePredicates
{
    public AutoCloseBeforePredicates(Func<char, bool> quote, Func<char, bool> bracket, Func<char, bool> comment)
    {
        Quote = quote ?? (_ => true);
        Bracket = bracket ?? (_ => true);
        Comment = comment ?? (_ => true);
    }

    public Func<char, bool> Quote { get; }
    public Func<char, bool> Bracket { get; }
    public Func<char, bool> Comment { get; }
}

public sealed record class AutoClosingPairDefinition(string Open, string Close, IReadOnlyList<string> NotIn)
{
    public AutoClosingPairDefinition(string open, string close)
        : this(open, close, Array.Empty<string>())
    {
    }
}

public sealed record class SurroundingPairDefinition(string Open, string Close);

public sealed record class LanguageConfigurationOptions
{
    private static readonly AutoClosingPairDefinition[] s_defaultAutoClosingPairs =
    [
        new("(", ")"),
        new("[", "]"),
        new("{", "}"),
        new("'", "'"),
        new("\"", "\""),
        new("`", "`"),
    ];

    private static readonly SurroundingPairDefinition[] s_defaultSurroundingPairs =
    [
        new("(", ")"),
        new("[", "]"),
        new("{", "}"),
        new("'", "'"),
        new("\"", "\""),
        new("`", "`"),
    ];

    private const string DefaultQuoteAutoCloseBefore = " \t\r\n)]}'\";:>,./?";
    private const string DefaultBracketAutoCloseBefore = " \t\r\n)]}>.,;:";
    private const string DefaultCommentAutoCloseBefore = " \t\r\n";

    public static LanguageConfigurationOptions Default { get; } = new();

    public IReadOnlyList<AutoClosingPairDefinition> AutoClosingPairs { get; init; } = s_defaultAutoClosingPairs;
    public IReadOnlyList<SurroundingPairDefinition> SurroundingPairs { get; init; } = s_defaultSurroundingPairs;
    public string AutoCloseBeforeQuotes { get; init; } = DefaultQuoteAutoCloseBefore;
    public string AutoCloseBeforeBrackets { get; init; } = DefaultBracketAutoCloseBefore;
    public string AutoCloseBeforeComments { get; init; } = DefaultCommentAutoCloseBefore;
    public string? BlockCommentStartToken { get; init; } = "/*";
    public IReadOnlyList<string> ElectricCharacters { get; init; } = Array.Empty<string>();
}

public sealed class AutoClosingPairs
{
    public AutoClosingPairs(IEnumerable<AutoClosingPairDefinition> pairs)
    {
        Pairs = pairs?.ToArray() ?? Array.Empty<AutoClosingPairDefinition>();
    }

    public IReadOnlyList<AutoClosingPairDefinition> Pairs { get; }
}

/// <summary>
/// Represents a simple model (either the model or the view model).
/// This interface matches TS ICursorSimpleModel.
/// </summary>
public interface ICursorSimpleModel
{
    /// <summary>
    /// Get the number of lines in the model.
    /// </summary>
    int GetLineCount();

    /// <summary>
    /// Get the content of a specific line (without EOL).
    /// </summary>
    string GetLineContent(int lineNumber);

    /// <summary>
    /// Get the minimum column for a line (typically 1).
    /// </summary>
    int GetLineMinColumn(int lineNumber);

    /// <summary>
    /// Get the maximum column for a line (line length + 1).
    /// </summary>
    int GetLineMaxColumn(int lineNumber);

    /// <summary>
    /// Get the first non-whitespace column for a line.
    /// </summary>
    int GetLineFirstNonWhitespaceColumn(int lineNumber);

    /// <summary>
    /// Get the last non-whitespace column for a line.
    /// </summary>
    int GetLineLastNonWhitespaceColumn(int lineNumber);

    /// <summary>
    /// Normalize a position according to position affinity.
    /// </summary>
    TextPosition NormalizePosition(TextPosition position, PositionAffinity affinity);

    /// <summary>
    /// Get the column at which indentation stops for a given line.
    /// </summary>
    int GetLineIndentColumn(int lineNumber);
}

/// <summary>
/// Position affinity for normalization.
/// </summary>
public enum PositionAffinity
{
    /// <summary>
    /// No preference.
    /// </summary>
    None = 0,

    /// <summary>
    /// Prefer left side.
    /// </summary>
    Left = 1,

    /// <summary>
    /// Prefer right side.
    /// </summary>
    Right = 2,

    /// <summary>
    /// Prefer left side for RTL text.
    /// </summary>
    LeftOfInjectedText = 3,

    /// <summary>
    /// Prefer right side for RTL text.
    /// </summary>
    RightOfInjectedText = 4,
}

/// <summary>
/// Helper class for visible column calculations.
/// Matches TS CursorColumns class.
/// </summary>
public static class CursorColumnsHelper
{
    /// <summary>
    /// Compute the next visible column after a character.
    /// </summary>
    private static int NextVisibleColumn(char ch, int visibleColumn, int tabSize)
    {
        if (ch == '\t')
        {
            return NextRenderTabStop(visibleColumn, tabSize);
        }
        // Simplified: treat all other chars as width 1
        // Full parity would check IsFullWidthCharacter / IsEmojiImprecise
        return visibleColumn + 1;
    }

    /// <summary>
    /// Returns a visible column from a column.
    /// </summary>
    /// <param name="lineContent">The content of the line.</param>
    /// <param name="column">The 1-based column.</param>
    /// <param name="tabSize">The tab size.</param>
    /// <returns>The 0-based visible column.</returns>
    public static int VisibleColumnFromColumn(string lineContent, int column, int tabSize)
    {
        if (string.IsNullOrEmpty(lineContent))
        {
            return 0;
        }

        int textLen = Math.Min(column - 1, lineContent.Length);
        int result = 0;

        for (int i = 0; i < textLen; i++)
        {
            result = NextVisibleColumn(lineContent[i], result, tabSize);
        }

        return result;
    }

    /// <summary>
    /// Returns a column from a visible column.
    /// </summary>
    /// <param name="lineContent">The content of the line.</param>
    /// <param name="visibleColumn">The 0-based visible column.</param>
    /// <param name="tabSize">The tab size.</param>
    /// <returns>The 1-based column.</returns>
    public static int ColumnFromVisibleColumn(string lineContent, int visibleColumn, int tabSize)
    {
        if (visibleColumn <= 0)
        {
            return 1;
        }

        if (string.IsNullOrEmpty(lineContent))
        {
            return 1;
        }

        int beforeVisibleColumn = 0;
        int beforeColumn = 1;
        int index = 0;

        while (index < lineContent.Length)
        {
            int afterVisibleColumn = NextVisibleColumn(lineContent[index], beforeVisibleColumn, tabSize);
            int afterColumn = index + 2; // 1-based

            if (afterVisibleColumn >= visibleColumn)
            {
                int beforeDelta = visibleColumn - beforeVisibleColumn;
                int afterDelta = afterVisibleColumn - visibleColumn;
                if (afterDelta < beforeDelta)
                {
                    return afterColumn;
                }
                else
                {
                    return beforeColumn;
                }
            }

            beforeVisibleColumn = afterVisibleColumn;
            beforeColumn = afterColumn;
            index++;
        }

        // Walked the entire string
        return lineContent.Length + 1;
    }

    /// <summary>
    /// Compute the next tab stop for rendering.
    /// Works with 0-based visible columns.
    /// </summary>
    public static int NextRenderTabStop(int visibleColumn, int tabSize)
    {
        return visibleColumn + tabSize - (visibleColumn % tabSize);
    }

    /// <summary>
    /// Compute the previous tab stop for rendering.
    /// Works with 0-based visible columns.
    /// </summary>
    public static int PrevRenderTabStop(int visibleColumn, int tabSize)
    {
        return Math.Max(0, visibleColumn - 1 - ((visibleColumn - 1) % tabSize));
    }

    /// <summary>
    /// Compute the next indent tab stop.
    /// </summary>
    public static int NextIndentTabStop(int visibleColumn, int indentSize)
    {
        return NextRenderTabStop(visibleColumn, indentSize);
    }

    /// <summary>
    /// Compute the previous indent tab stop.
    /// </summary>
    public static int PrevIndentTabStop(int visibleColumn, int indentSize)
    {
        return PrevRenderTabStop(visibleColumn, indentSize);
    }
}
