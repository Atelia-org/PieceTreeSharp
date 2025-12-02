// Source: ts/src/vs/editor/contrib/snippet/browser/snippetSession.ts
// - Class: OneSnippet (Lines: 30-250)
// - Class: SnippetSession (Lines: 300-600)
// - Static: SnippetSession.adjustWhitespace (Lines: 326-380)
// Source: ts/src/vs/editor/contrib/snippet/browser/snippetParser.ts
// - Class: Placeholder (Lines: 211-257) - isFinalTabstop semantics
// Ported: 2025-11-22
// Extended: 2025-12-02 (P1: Final Tabstop $0, adjustWhitespace)

using System.Text;
using System.Text.RegularExpressions;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Options for snippet insertion.
/// </summary>
public sealed class SnippetInsertOptions
{
    /// <summary>
    /// Whether to adjust whitespace/indentation for multi-line snippets.
    /// Default is true.
    /// </summary>
    public bool AdjustWhitespace { get; init; } = true;

    /// <summary>
    /// Number of characters to overwrite before the insertion point.
    /// </summary>
    public int OverwriteBefore { get; init; }

    /// <summary>
    /// Number of characters to overwrite after the insertion point.
    /// </summary>
    public int OverwriteAfter { get; init; }

    public static SnippetInsertOptions Default { get; } = new();
}

/// <summary>
/// A minimal snippet session implementation that supports numbered placeholders like ${1:placeholder} and $0.
/// It inserts the text and creates decorations for placeholders, providing navigation to next/prev placeholders.
/// 
/// Placeholder index 0 ($0) is the "final tabstop" - it's always navigated to last.
/// </summary>
public sealed class SnippetSession : IDisposable
{
    private readonly TextModel _model;
    private readonly int _ownerId;
    // Each placeholder keeps a live ModelDecoration so later edits move the range automatically.
    // Index 0 ($0) is treated specially as the final tabstop.
    private readonly List<(int Index, bool IsFinalTabstop, ModelDecoration Decoration)> _placeholders = [];
    private int _current = -1;
    private bool _disposed;

    // Regex patterns for placeholder parsing
    // Matches: ${n:text} or ${n} (placeholder with or without default text)
    private static readonly Regex s_placeholderWithTextPattern = new(@"\$\{(\d+):([^}]*)\}", RegexOptions.Compiled);
    // Matches: ${n} (placeholder without default text)
    private static readonly Regex s_placeholderSimplePattern = new(@"\$\{(\d+)\}", RegexOptions.Compiled);
    // Matches: $n (simple tabstop without braces)
    private static readonly Regex s_tabstopPattern = new(@"\$(\d+)(?![{])", RegexOptions.Compiled);

    public SnippetSession(TextModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _ownerId = _model.AllocateDecorationOwnerId();
    }

    public bool HasPlaceholders => _placeholders.Count > 0;

    /// <summary>
    /// Returns true if currently at the final tabstop ($0).
    /// </summary>
    public bool IsAtFinalTabstop => _current >= 0 && _current < _placeholders.Count && _placeholders[_current].IsFinalTabstop;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _model.RemoveAllDecorations(_ownerId);
    }

    /// <summary>
    /// Inserts a snippet at the given location in the model and identifies placeholders.
    /// </summary>
    public void InsertSnippet(TextPosition start, string snippet) =>
        InsertSnippet(start, snippet, SnippetInsertOptions.Default);

    /// <summary>
    /// Inserts a snippet at the given location in the model and identifies placeholders.
    /// </summary>
    /// <param name="start">The position to insert the snippet.</param>
    /// <param name="snippet">The snippet template string.</param>
    /// <param name="options">Insertion options.</param>
    public void InsertSnippet(TextPosition start, string snippet, SnippetInsertOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Apply whitespace adjustment if needed
        string adjustedSnippet = options.AdjustWhitespace
            ? AdjustWhitespace(_model, start, snippet)
            : snippet;

        // Parse placeholders and build plain text
        ParseResult result = ParseSnippet(adjustedSnippet);
        string plainText = result.PlainText;

        int startOffset = _model.GetOffsetAt(start);
        _model.PushEditOperations([new TextEdit(start, start, plainText)], beforeCursorState: null);

        // Create decorations for each placeholder
        foreach ((int Index, int Start, int Length) entry in result.Placeholders)
        {
            int absoluteStart = startOffset + entry.Start;
            int absoluteEnd = absoluteStart + entry.Length;
            bool isFinalTabstop = entry.Index == 0;

            ModelDecorationOptions placeholderOptions = new()
            {
                Description = isFinalTabstop ? "snippet-final-tabstop" : "snippet-placeholder",
                RenderKind = DecorationRenderKind.Generic,
                ShowIfCollapsed = true,
                InlineDescription = "snippet",
            };

            IReadOnlyList<ModelDecoration> created = _model.DeltaDecorations(
                _ownerId,
                oldDecorationIds: null,
                [new ModelDeltaDecoration(new TextRange(absoluteStart, absoluteEnd), placeholderOptions)]);

            if (created.Count == 1)
            {
                _placeholders.Add((entry.Index, isFinalTabstop, created[0]));
            }
        }

        // Sort placeholders: non-final tabstops by index ascending, final tabstop ($0) last
        // This matches TS Placeholder.compareByIndex semantics
        _placeholders.Sort((a, b) =>
        {
            if (a.Index == b.Index)
            {
                return 0;
            }

            if (a.IsFinalTabstop)
            {
                return 1; // $0 always comes last
            }

            if (b.IsFinalTabstop)
            {
                return -1; // $0 always comes last
            }

            return a.Index.CompareTo(b.Index);
        });

        _current = -1;
    }

    /// <summary>
    /// Adjusts whitespace/indentation for multi-line snippets.
    /// Based on TS SnippetSession.adjustWhitespace (Lines 326-380).
    /// 
    /// For multi-line snippets:
    /// - The first line is NOT extra-indented (it uses insertion position's column)
    /// - Lines 2..N get the leading whitespace of the insertion line prepended
    /// - All indentation is normalized according to model settings
    /// </summary>
    public static string AdjustWhitespace(TextModel model, TextPosition position, string snippet)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(snippet);

        // Get the leading whitespace from the line up to the insertion column
        string lineContent = model.GetLineContent(position.LineNumber);
        string lineLeadingWhitespace = GetLeadingWhitespace(lineContent, 0, position.Column - 1);

        // Split snippet into lines
        string[] lines = snippet.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        if (lines.Length <= 1)
        {
            // Single-line snippet, no adjustment needed
            return snippet;
        }

        // Get model's EOL and options for normalization
        string eol = model.Eol;
        TextModelResolvedOptions options = model.GetOptions();

        // Adjust each line
        StringBuilder result = new(snippet.Length + lineLeadingWhitespace.Length * (lines.Length - 1));

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                result.Append(eol);

                // Lines after the first get the leading whitespace prepended
                // and then normalized
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    string adjustedLine = NormalizeIndentation(lineLeadingWhitespace + lines[i], options);
                    result.Append(adjustedLine);
                }
                // Empty lines stay empty
            }
            else
            {
                // First line: just normalize its indentation (no extra whitespace)
                result.Append(NormalizeIndentation(lines[0], options));
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Normalizes indentation in a string according to model options.
    /// Based on CursorConfiguration.NormalizeIndentation.
    /// </summary>
    private static string NormalizeIndentation(string text, TextModelResolvedOptions options)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        int firstNonWhitespace = FirstNonWhitespaceIndex(text);
        if (firstNonWhitespace == -1)
        {
            return NormalizeLeadingWhitespace(text, options);
        }

        string prefix = NormalizeLeadingWhitespace(text[..firstNonWhitespace], options);
        return prefix + text[firstNonWhitespace..];
    }

    private static string NormalizeLeadingWhitespace(string text, TextModelResolvedOptions options)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        int visible = 0;
        foreach (char ch in text)
        {
            visible = ch == '\t'
                ? CursorColumnsHelper.NextIndentTabStop(visible, options.IndentSize)
                : visible + 1;
        }

        if (!options.InsertSpaces)
        {
            int tabs = visible / options.IndentSize;
            int spaces = visible % options.IndentSize;
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

    /// <summary>
    /// Gets the leading whitespace from a string up to a maximum position.
    /// Based on TS getLeadingWhitespace from strings.ts.
    /// </summary>
    private static string GetLeadingWhitespace(string str, int start, int end)
    {
        int length = Math.Min(end, str.Length);
        int i = start;

        while (i < length)
        {
            char ch = str[i];
            if (ch != ' ' && ch != '\t')
            {
                break;
            }

            i++;
        }

        return str[start..i];
    }

    /// <summary>
    /// Parses a snippet template and extracts placeholders.
    /// Supports: ${n:text}, ${n}, $n, and $0 (final tabstop)
    /// </summary>
    private static ParseResult ParseSnippet(string snippet)
    {
        StringBuilder builder = new(snippet.Length);
        List<(int Index, int Start, int Length)> placeholders = [];

        // First pass: collect all placeholder positions with their original indices
        List<(int SnippetStart, int SnippetEnd, int Index, string DefaultText)> matches = [];

        // Match ${n:text} - placeholder with default text
        foreach (Match m in s_placeholderWithTextPattern.Matches(snippet))
        {
            matches.Add((m.Index, m.Index + m.Length, int.Parse(m.Groups[1].Value), m.Groups[2].Value));
        }

        // Match ${n} - placeholder without default text
        foreach (Match m in s_placeholderSimplePattern.Matches(snippet))
        {
            // Avoid duplicates (${n} might be matched if we already have ${n:text} at same position)
            if (!matches.Any(x => x.SnippetStart == m.Index))
            {
                matches.Add((m.Index, m.Index + m.Length, int.Parse(m.Groups[1].Value), string.Empty));
            }
        }

        // Match $n - simple tabstop
        foreach (Match m in s_tabstopPattern.Matches(snippet))
        {
            // Avoid overlap with ${...} patterns
            if (!matches.Any(x => m.Index >= x.SnippetStart && m.Index < x.SnippetEnd))
            {
                matches.Add((m.Index, m.Index + m.Length, int.Parse(m.Groups[1].Value), string.Empty));
            }
        }

        // Sort by position in snippet
        matches.Sort((a, b) => a.SnippetStart.CompareTo(b.SnippetStart));

        // Build plain text and record placeholder positions
        int lastIndex = 0;
        foreach ((int SnippetStart, int SnippetEnd, int Index, string DefaultText) match in matches)
        {
            // Append text before this placeholder
            if (match.SnippetStart > lastIndex)
            {
                builder.Append(snippet.AsSpan(lastIndex, match.SnippetStart - lastIndex));
            }

            // Record placeholder position in plain text
            int plainTextStart = builder.Length;
            builder.Append(match.DefaultText);
            placeholders.Add((match.Index, plainTextStart, match.DefaultText.Length));

            lastIndex = match.SnippetEnd;
        }

        // Append remaining text
        if (lastIndex < snippet.Length)
        {
            builder.Append(snippet.AsSpan(lastIndex));
        }

        return new ParseResult(builder.ToString(), placeholders);
    }

    private readonly record struct ParseResult(string PlainText, List<(int Index, int Start, int Length)> Placeholders);

    public TextPosition? NextPlaceholder()
    {
        if (_placeholders.Count == 0)
        {
            return null;
        }

        if (_current >= _placeholders.Count - 1)
        {
            _current = _placeholders.Count; // sentinel meaning "past the end" to prevent infinite loops
            return null;
        }

        _current++;
        return GetPlaceholderStart(_placeholders[_current]);
    }

    public TextPosition? PrevPlaceholder()
    {
        if (_placeholders.Count == 0)
        {
            return null;
        }

        if (_current == _placeholders.Count)
        {
            // if we just walked past the end, jump back to the last placeholder
            _current = _placeholders.Count - 1;
            return GetPlaceholderStart(_placeholders[_current]);
        }

        if (_current <= 0)
        {
            _current = -1;
            return null;
        }

        _current--;
        return GetPlaceholderStart(_placeholders[_current]);
    }

    /// <summary>
    /// Gets the current placeholder's range (start and end positions).
    /// </summary>
    public (TextPosition Start, TextPosition End)? GetCurrentPlaceholderRange()
    {
        if (_current < 0 || _current >= _placeholders.Count)
        {
            return null;
        }

        var placeholder = _placeholders[_current];
        int startOffset = placeholder.Decoration.Range.StartOffset;
        int endOffset = placeholder.Decoration.Range.EndOffset;
        return (_model.GetPositionAt(startOffset), _model.GetPositionAt(endOffset));
    }

    private TextPosition GetPlaceholderStart((int Index, bool IsFinalTabstop, ModelDecoration Decoration) placeholder)
    {
        int startOffset = placeholder.Decoration.Range.StartOffset;
        // Re-evaluate positions on demand so placeholder edits cannot desync navigation state.
        return _model.GetPositionAt(startOffset);
    }
}
