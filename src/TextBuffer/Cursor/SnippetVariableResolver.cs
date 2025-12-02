// Source: ts/src/vs/editor/contrib/snippet/browser/snippetVariables.ts
// - Interface: VariableResolver (snippetParser.ts L20-30)
// - Class: CompositeSnippetVariableResolver (Lines: 57-70)
// - Class: SelectionBasedVariableResolver (Lines: 72-135)
// - Class: ModelBasedVariableResolver (Lines: 137-175)
// Ported: 2025-12-02 (P2: Variable Resolver framework)

using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Interface for resolving snippet variables like TM_FILENAME, SELECTION, etc.
/// Based on TS VariableResolver interface in snippetParser.ts.
/// </summary>
public interface ISnippetVariableResolver
{
    /// <summary>
    /// Resolves a variable by name.
    /// </summary>
    /// <param name="variableName">The variable name (e.g., "TM_FILENAME", "SELECTION").</param>
    /// <returns>The resolved value, or null if this resolver cannot handle the variable.</returns>
    string? Resolve(string variableName);
}

/// <summary>
/// Combines multiple variable resolvers, returning the first non-null result.
/// Based on TS CompositeSnippetVariableResolver (snippetVariables.ts L57-70).
/// </summary>
public sealed class CompositeVariableResolver : ISnippetVariableResolver
{
    private readonly ISnippetVariableResolver[] _delegates;

    public CompositeVariableResolver(params ISnippetVariableResolver[] delegates)
    {
        _delegates = delegates ?? throw new ArgumentNullException(nameof(delegates));
    }

    public CompositeVariableResolver(IEnumerable<ISnippetVariableResolver> delegates)
    {
        ArgumentNullException.ThrowIfNull(delegates);
        _delegates = delegates.ToArray();
    }

    /// <inheritdoc />
    public string? Resolve(string variableName)
    {
        foreach (ISnippetVariableResolver resolver in _delegates)
        {
            string? value = resolver.Resolve(variableName);
            if (value != null)
            {
                return value;
            }
        }
        return null;
    }
}

/// <summary>
/// Resolves selection-based variables: SELECTION, TM_SELECTED_TEXT.
/// Based on TS SelectionBasedVariableResolver (snippetVariables.ts L72-135).
/// 
/// Simplified implementation: only handles SELECTION and TM_SELECTED_TEXT.
/// Other variables like TM_CURRENT_LINE, TM_CURRENT_WORD, TM_LINE_INDEX, 
/// TM_LINE_NUMBER, CURSOR_INDEX, CURSOR_NUMBER are not implemented (P3).
/// </summary>
public sealed class SelectionVariableResolver : ISnippetVariableResolver
{
    private readonly TextModel _model;
    private readonly Core.Range _selection;

    /// <summary>
    /// Creates a selection-based variable resolver.
    /// </summary>
    /// <param name="model">The text model.</param>
    /// <param name="selection">The current selection range.</param>
    public SelectionVariableResolver(TextModel model, Core.Range selection)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _selection = selection;
    }

    /// <summary>
    /// Creates a selection-based variable resolver from positions.
    /// </summary>
    /// <param name="model">The text model.</param>
    /// <param name="selectionStart">The selection start position.</param>
    /// <param name="selectionEnd">The selection end position.</param>
    public SelectionVariableResolver(TextModel model, TextPosition selectionStart, TextPosition selectionEnd)
        : this(model, new Core.Range(selectionStart, selectionEnd))
    {
    }

    /// <summary>
    /// Creates a selection-based variable resolver from a TextRange (offset-based).
    /// </summary>
    /// <param name="model">The text model.</param>
    /// <param name="textRange">The selection as a TextRange (offset-based).</param>
    public SelectionVariableResolver(TextModel model, TextRange textRange)
        : this(model, new Core.Range(model.GetPositionAt(textRange.StartOffset), model.GetPositionAt(textRange.EndOffset)))
    {
    }

    /// <inheritdoc />
    public string? Resolve(string variableName)
    {
        // TS: SelectionBasedVariableResolver.resolve() (snippetVariables.ts L80-135)
        return variableName switch
        {
            "SELECTION" or "TM_SELECTED_TEXT" => ResolveSelection(),
            _ => null
        };
    }

    private string? ResolveSelection()
    {
        // TS: if (name === 'SELECTION' || name === 'TM_SELECTED_TEXT') { ... }
        // Return the selected text, or empty string if no selection
        if (_selection.Start == _selection.End)
        {
            return string.Empty;
        }

        return _model.GetValueInRange(_selection);
    }
}

/// <summary>
/// Resolves model-based variables: TM_FILENAME.
/// Based on TS ModelBasedVariableResolver (snippetVariables.ts L137-175).
/// 
/// Simplified implementation: only handles TM_FILENAME.
/// Other variables like TM_FILENAME_BASE, TM_DIRECTORY, TM_FILEPATH, 
/// RELATIVE_FILEPATH are not implemented (P3).
/// </summary>
public sealed class ModelVariableResolver : ISnippetVariableResolver
{
    private readonly string? _filename;

    /// <summary>
    /// Creates a model-based variable resolver.
    /// </summary>
    /// <param name="filename">The filename (without path) to use for TM_FILENAME.</param>
    public ModelVariableResolver(string? filename)
    {
        _filename = filename;
    }

    /// <inheritdoc />
    public string? Resolve(string variableName)
    {
        // TS: ModelBasedVariableResolver.resolve() (snippetVariables.ts L148-175)
        return variableName switch
        {
            "TM_FILENAME" => _filename ?? string.Empty,
            _ => null
        };
    }
}

/// <summary>
/// A fallback resolver that returns empty string for any unknown variable.
/// This ensures unknown variables don't cause errors - they just expand to nothing.
/// Based on TS behavior where unknown variables fallback to empty string.
/// </summary>
public sealed class FallbackVariableResolver : ISnippetVariableResolver
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static FallbackVariableResolver Instance { get; } = new();

    private FallbackVariableResolver() { }

    /// <inheritdoc />
    public string? Resolve(string variableName)
    {
        // Unknown variables resolve to empty string
        return string.Empty;
    }
}

/// <summary>
/// Known snippet variable names.
/// Based on TS KnownSnippetVariableNames (snippetVariables.ts L22-56).
/// </summary>
public static class KnownSnippetVariableNames
{
    // Selection-based variables
    public const string Selection = "SELECTION";
    public const string TmSelectedText = "TM_SELECTED_TEXT";
    public const string TmCurrentLine = "TM_CURRENT_LINE";
    public const string TmCurrentWord = "TM_CURRENT_WORD";
    public const string TmLineIndex = "TM_LINE_INDEX";
    public const string TmLineNumber = "TM_LINE_NUMBER";
    public const string CursorIndex = "CURSOR_INDEX";
    public const string CursorNumber = "CURSOR_NUMBER";

    // Model-based variables
    public const string TmFilename = "TM_FILENAME";
    public const string TmFilenameBase = "TM_FILENAME_BASE";
    public const string TmDirectory = "TM_DIRECTORY";
    public const string TmDirectoryBase = "TM_DIRECTORY_BASE";
    public const string TmFilepath = "TM_FILEPATH";
    public const string RelativeFilepath = "RELATIVE_FILEPATH";

    // Clipboard variable (not implemented)
    public const string Clipboard = "CLIPBOARD";

    // Comment variables (not implemented)
    public const string LineComment = "LINE_COMMENT";
    public const string BlockCommentStart = "BLOCK_COMMENT_START";
    public const string BlockCommentEnd = "BLOCK_COMMENT_END";

    // Time variables (not implemented)
    public const string CurrentYear = "CURRENT_YEAR";
    public const string CurrentYearShort = "CURRENT_YEAR_SHORT";
    public const string CurrentMonth = "CURRENT_MONTH";
    public const string CurrentDate = "CURRENT_DATE";
    public const string CurrentHour = "CURRENT_HOUR";
    public const string CurrentMinute = "CURRENT_MINUTE";
    public const string CurrentSecond = "CURRENT_SECOND";
    public const string CurrentDayName = "CURRENT_DAY_NAME";
    public const string CurrentDayNameShort = "CURRENT_DAY_NAME_SHORT";
    public const string CurrentMonthName = "CURRENT_MONTH_NAME";
    public const string CurrentMonthNameShort = "CURRENT_MONTH_NAME_SHORT";
    public const string CurrentSecondsUnix = "CURRENT_SECONDS_UNIX";
    public const string CurrentTimezoneOffset = "CURRENT_TIMEZONE_OFFSET";

    // Workspace variables (not implemented)
    public const string WorkspaceName = "WORKSPACE_NAME";
    public const string WorkspaceFolder = "WORKSPACE_FOLDER";

    // Random variables (not implemented)
    public const string Random = "RANDOM";
    public const string RandomHex = "RANDOM_HEX";
    public const string Uuid = "UUID";

    /// <summary>
    /// Checks if a variable name is a known snippet variable.
    /// </summary>
    public static bool IsKnown(string name) => name switch
    {
        Selection or TmSelectedText or TmCurrentLine or TmCurrentWord or
        TmLineIndex or TmLineNumber or CursorIndex or CursorNumber or
        TmFilename or TmFilenameBase or TmDirectory or TmDirectoryBase or
        TmFilepath or RelativeFilepath or Clipboard or
        LineComment or BlockCommentStart or BlockCommentEnd or
        CurrentYear or CurrentYearShort or CurrentMonth or CurrentDate or
        CurrentHour or CurrentMinute or CurrentSecond or CurrentDayName or
        CurrentDayNameShort or CurrentMonthName or CurrentMonthNameShort or
        CurrentSecondsUnix or CurrentTimezoneOffset or
        WorkspaceName or WorkspaceFolder or
        Random or RandomHex or Uuid => true,
        _ => false
    };
}
