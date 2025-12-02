// Source: ts/src/vs/editor/contrib/snippet/browser/snippetSession.ts
// - Class: OneSnippet (Lines: 30-250)
// - Class: SnippetSession (Lines: 300-600)
// - Static: SnippetSession.adjustWhitespace (Lines: 326-380)
// Source: ts/src/vs/editor/contrib/snippet/browser/snippetParser.ts
// - Class: Placeholder (Lines: 211-257) - isFinalTabstop semantics
// Source: ts/src/vs/editor/contrib/snippet/browser/snippetVariables.ts
// - Variable resolution pattern (Lines: 57-175)
// Ported: 2025-11-22
// Extended: 2025-12-02 (P1: Final Tabstop $0, adjustWhitespace)
// Extended: 2025-12-02 (P2: Variable Resolver - TM_FILENAME, SELECTION)

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

    /// <summary>
    /// Optional variable resolver for expanding variables like TM_FILENAME, SELECTION, etc.
    /// If null, variables will be expanded to empty strings.
    /// </summary>
    public ISnippetVariableResolver? VariableResolver { get; init; }

    public static SnippetInsertOptions Default { get; } = new();
}

/// <summary>
/// A minimal snippet session implementation that supports numbered placeholders like ${1:placeholder} and $0.
/// It inserts the text and creates decorations for placeholders, providing navigation to next/prev placeholders.
/// 
/// Placeholder index 0 ($0) is the "final tabstop" - it's always navigated to last.
/// 
/// P1.5 Placeholder Grouping: Same-index placeholders are grouped together for synchronized editing.
/// See TS: OneSnippet.computePossibleSelections() (snippetSession.ts L200-230)
/// </summary>
public sealed class SnippetSession : IDisposable
{
    private readonly TextModel _model;
    private readonly int _ownerId;
    // Each placeholder keeps a live ModelDecoration so later edits move the range automatically.
    // Index 0 ($0) is treated specially as the final tabstop.
    private readonly List<(int Index, bool IsFinalTabstop, ModelDecoration Decoration)> _placeholders = [];
    // P1.5: Placeholder grouping by index for synchronized editing
    // TS: OneSnippet._placeholderGroups in snippetSession.ts
    private readonly Dictionary<int, List<ModelDecoration>> _placeholderGroups = [];
    private int _current = -1;
    private bool _disposed;

    // Regex patterns for placeholder parsing
    // Matches: ${n:text} or ${n} (placeholder with or without default text)
    private static readonly Regex s_placeholderWithTextPattern = new(@"\$\{(\d+):([^}]*)\}", RegexOptions.Compiled);
    // Matches: ${n} (placeholder without default text)
    private static readonly Regex s_placeholderSimplePattern = new(@"\$\{(\d+)\}", RegexOptions.Compiled);
    // Matches: $n (simple tabstop without braces)
    private static readonly Regex s_tabstopPattern = new(@"\$(\d+)(?![{])", RegexOptions.Compiled);
    // Matches: ${VAR} (variable without default)
    private static readonly Regex s_variableSimplePattern = new(@"\$\{([A-Z_][A-Z_0-9]*)\}", RegexOptions.Compiled);
    // Matches: ${VAR:default} (variable with default)
    private static readonly Regex s_variableWithDefaultPattern = new(@"\$\{([A-Z_][A-Z_0-9]*):([^}]*)\}", RegexOptions.Compiled);

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

        // First, resolve variables (before whitespace adjustment)
        // Variables like ${TM_FILENAME} and ${VAR:default} are expanded here
        string resolvedSnippet = ResolveVariables(snippet, options.VariableResolver);

        // Apply whitespace adjustment if needed
        string adjustedSnippet = options.AdjustWhitespace
            ? AdjustWhitespace(_model, start, resolvedSnippet)
            : resolvedSnippet;

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

            // TS: snippetSession.ts OneSnippet._decor
            // - inactive: NeverGrowsWhenTypingAtEdges (so placeholders shift, not expand)
            // - active: AlwaysGrowsWhenTypingAtEdges (so typing expands the placeholder)
            // We use NeverGrows here because these are "inactive" initially
            ModelDecorationOptions placeholderOptions = new()
            {
                Description = isFinalTabstop ? "snippet-final-tabstop" : "snippet-placeholder",
                RenderKind = DecorationRenderKind.Generic,
                ShowIfCollapsed = true,
                InlineDescription = "snippet",
                Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
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

        // P1.5: Build placeholder groups by index
        // TS: groupBy(_snippet.placeholders, Placeholder.compareByIndex) in snippetSession.ts L48
        GroupPlaceholdersByIndex();

        _current = -1;
    }

    /// <summary>
    /// Groups placeholders by their index for synchronized editing.
    /// TS: OneSnippet constructor uses groupBy() to create _placeholderGroups (snippetSession.ts L48).
    /// </summary>
    private void GroupPlaceholdersByIndex()
    {
        _placeholderGroups.Clear();
        foreach ((int Index, bool IsFinalTabstop, ModelDecoration Decoration) entry in _placeholders)
        {
            if (!_placeholderGroups.TryGetValue(entry.Index, out List<ModelDecoration>? group))
            {
                group = [];
                _placeholderGroups[entry.Index] = group;
            }
            group.Add(entry.Decoration);
        }
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

    /// <summary>
    /// Resolves snippet variables like ${TM_FILENAME}, ${SELECTION}, ${VAR:default}.
    /// Based on TS VariableResolver pattern (snippetVariables.ts).
    /// 
    /// Variable resolution order:
    /// 1. Try the provided resolver (if any)
    /// 2. Use default value from ${VAR:default} syntax
    /// 3. Fall back to empty string for unknown variables
    /// </summary>
    /// <param name="snippet">The snippet template with variables.</param>
    /// <param name="resolver">Optional variable resolver.</param>
    /// <returns>The snippet with variables resolved to their values.</returns>
    private static string ResolveVariables(string snippet, ISnippetVariableResolver? resolver)
    {
        if (string.IsNullOrEmpty(snippet))
        {
            return snippet;
        }

        // First, resolve ${VAR:default} patterns (variables with default values)
        string result = s_variableWithDefaultPattern.Replace(snippet, match =>
        {
            string varName = match.Groups[1].Value;
            string defaultValue = match.Groups[2].Value;
            
            // Try to resolve the variable
            string? resolved = resolver?.Resolve(varName);
            
            // If resolved is non-null and non-empty, use it; otherwise use default
            // TS behavior: if resolver returns undefined or empty string, use default
            return !string.IsNullOrEmpty(resolved) ? resolved : defaultValue;
        });

        // Then, resolve ${VAR} patterns (variables without default values)
        result = s_variableSimplePattern.Replace(result, match =>
        {
            string varName = match.Groups[1].Value;
            
            // Try to resolve the variable
            string? resolved = resolver?.Resolve(varName);
            
            // If resolved is non-null, use it; otherwise empty string
            // Unknown variables silently expand to empty string
            return resolved ?? string.Empty;
        });

        return result;
    }

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

        // Move to next placeholder, skipping same-index duplicates (mirrors)
        // TS: _placeholderGroupsIdx navigates between groups, not individual placeholders
        int currentIndex = _current >= 0 ? _placeholders[_current].Index : -1;
        do
        {
            _current++;
        } while (_current < _placeholders.Count && _placeholders[_current].Index == currentIndex);

        if (_current >= _placeholders.Count)
        {
            _current = _placeholders.Count; // sentinel
            return null;
        }

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
            // if we just walked past the end, jump back to the last placeholder group
            _current = _placeholders.Count - 1;
            // Find the first in this group
            int groupIndex = _placeholders[_current].Index;
            while (_current > 0 && _placeholders[_current - 1].Index == groupIndex)
            {
                _current--;
            }
            return GetPlaceholderStart(_placeholders[_current]);
        }

        if (_current <= 0)
        {
            _current = -1;
            return null;
        }

        // Move to previous placeholder group
        // TS: _placeholderGroupsIdx navigates between groups
        int currentIndex = _placeholders[_current].Index;
        
        // Skip all items with same index (going backward)
        while (_current > 0 && _placeholders[_current - 1].Index == currentIndex)
        {
            _current--;
        }
        
        // Now _current points to the first item of current group, move to previous group
        if (_current <= 0)
        {
            _current = -1;
            return null;
        }
        
        _current--;
        // Now find the first item in this new group
        int newIndex = _placeholders[_current].Index;
        while (_current > 0 && _placeholders[_current - 1].Index == newIndex)
        {
            _current--;
        }
        
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

    /// <summary>
    /// Gets all ranges for the current placeholder index (including mirrors).
    /// P1.5: Same-index placeholders are grouped together for synchronized editing.
    /// TS: OneSnippet.computePossibleSelections() (snippetSession.ts L200-230).
    /// </summary>
    /// <returns>All ranges for the current placeholder index, or null if not at a valid placeholder.</returns>
    public IReadOnlyList<(TextPosition Start, TextPosition End)>? GetCurrentPlaceholderRanges()
    {
        if (_current < 0 || _current >= _placeholders.Count)
        {
            return null;
        }

        int currentIndex = _placeholders[_current].Index;
        return GetPlaceholderRangesByIndex(currentIndex);
    }

    /// <summary>
    /// Gets all ranges for placeholders with the specified index.
    /// TS: Part of computePossibleSelections() logic (snippetSession.ts L200-230).
    /// </summary>
    /// <param name="index">The placeholder index to query.</param>
    /// <returns>All ranges for the specified index, or null if index not found or decorations lost.</returns>
    public IReadOnlyList<(TextPosition Start, TextPosition End)>? GetPlaceholderRangesByIndex(int index)
    {
        if (!_placeholderGroups.TryGetValue(index, out List<ModelDecoration>? group) || group.Count == 0)
        {
            return null;
        }

        List<(TextPosition Start, TextPosition End)> result = new(group.Count);
        foreach (ModelDecoration decoration in group)
        {
            // Get the current range from the model (decorations track edits automatically)
            TextRange range = decoration.Range;
            result.Add((_model.GetPositionAt(range.StartOffset), _model.GetPositionAt(range.EndOffset)));
        }

        return result;
    }

    /// <summary>
    /// Computes all possible selections for all placeholder groups.
    /// TS: OneSnippet.computePossibleSelections() (snippetSession.ts L200-230).
    /// </summary>
    /// <returns>A dictionary mapping placeholder index to all ranges for that index.</returns>
    public IReadOnlyDictionary<int, IReadOnlyList<(TextPosition Start, TextPosition End)>> ComputePossibleSelections()
    {
        Dictionary<int, IReadOnlyList<(TextPosition Start, TextPosition End)>> result = [];

        foreach ((int index, List<ModelDecoration> group) in _placeholderGroups)
        {
            // Skip final tabstop in selection computation (TS: if (placeholder.isFinalTabstop) break;)
            if (index == 0)
            {
                continue;
            }

            List<(TextPosition Start, TextPosition End)> ranges = new(group.Count);
            bool hasValidRanges = true;

            foreach (ModelDecoration decoration in group)
            {
                TextRange range = decoration.Range;
                // Check if decoration still has a valid range
                // TS: if (!range) { result.delete(placeholder.index); break; }
                if (range.StartOffset < 0 || range.EndOffset < 0)
                {
                    hasValidRanges = false;
                    break;
                }
                ranges.Add((_model.GetPositionAt(range.StartOffset), _model.GetPositionAt(range.EndOffset)));
            }

            if (hasValidRanges && ranges.Count > 0)
            {
                result[index] = ranges;
            }
        }

        return result;
    }

    private TextPosition GetPlaceholderStart((int Index, bool IsFinalTabstop, ModelDecoration Decoration) placeholder)
    {
        int startOffset = placeholder.Decoration.Range.StartOffset;
        // Re-evaluate positions on demand so placeholder edits cannot desync navigation state.
        return _model.GetPositionAt(startOffset);
    }
}
