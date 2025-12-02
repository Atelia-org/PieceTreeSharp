// Source: ts/src/vs/editor/contrib/snippet/browser/snippetController2.ts
// - Class: SnippetController2 (Lines: 30-500)
// Ported: 2025-11-22
// Extended: 2025-12-02 (P1: Final Tabstop $0, adjustWhitespace options)
// Extended: 2025-12-02 (P1.5: Placeholder grouping for synchronized editing)

using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Minimal snippet controller that can create snippet sessions and navigate placeholders.
/// </summary>
public sealed class SnippetController : IDisposable
{
    private readonly TextModel _model;
    private SnippetSession? _session;
    private bool _disposed;

    public SnippetController(TextModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    /// <summary>
    /// Returns true if the current session is at the final tabstop ($0).
    /// </summary>
    public bool IsAtFinalTabstop => _session?.IsAtFinalTabstop ?? false;

    /// <summary>
    /// Returns true if there is an active snippet session with placeholders.
    /// </summary>
    public bool HasActivePlaceholders => _session?.HasPlaceholders ?? false;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _session?.Dispose();
        _session = null;
    }

    public SnippetSession CreateSession()
    {
        _session?.Dispose();
        _session = new SnippetSession(_model);
        return _session;
    }

    public TextPosition? NextPlaceholder()
    {
        return _session?.NextPlaceholder();
    }

    public TextPosition? PrevPlaceholder()
    {
        return _session?.PrevPlaceholder();
    }

    /// <summary>
    /// Gets the current placeholder's range (start and end positions).
    /// </summary>
    public (TextPosition Start, TextPosition End)? GetCurrentPlaceholderRange()
    {
        return _session?.GetCurrentPlaceholderRange();
    }

    /// <summary>
    /// Gets all ranges for the current placeholder index (including mirrors).
    /// P1.5: Same-index placeholders are grouped together for synchronized editing.
    /// TS: Uses OneSnippet.computePossibleSelections() (snippetSession.ts L200-230).
    /// </summary>
    /// <returns>All ranges for the current placeholder index, or null if not at a valid placeholder.</returns>
    public IReadOnlyList<(TextPosition Start, TextPosition End)>? GetCurrentPlaceholderRanges()
    {
        return _session?.GetCurrentPlaceholderRanges();
    }

    /// <summary>
    /// Gets all selections (as TextRange) for the current placeholder.
    /// Convenience method that converts positions to ranges.
    /// P1.5: For synchronized editing of same-index placeholders.
    /// </summary>
    /// <returns>All selections for the current placeholder, or null if not at a valid placeholder.</returns>
    public IReadOnlyList<TextRange>? GetAllSelectionsForCurrentPlaceholder()
    {
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges = _session?.GetCurrentPlaceholderRanges();
        if (ranges == null || ranges.Count == 0)
        {
            return null;
        }

        List<TextRange> result = new(ranges.Count);
        foreach ((TextPosition Start, TextPosition End) range in ranges)
        {
            int startOffset = _model.GetOffsetAt(range.Start);
            int endOffset = _model.GetOffsetAt(range.End);
            result.Add(new TextRange(startOffset, endOffset));
        }
        return result;
    }

    /// <summary>
    /// Computes all possible selections for all placeholder groups.
    /// TS: OneSnippet.computePossibleSelections() (snippetSession.ts L200-230).
    /// </summary>
    /// <returns>A dictionary mapping placeholder index to all ranges for that index.</returns>
    public IReadOnlyDictionary<int, IReadOnlyList<(TextPosition Start, TextPosition End)>>? ComputePossibleSelections()
    {
        return _session?.ComputePossibleSelections();
    }

    public void InsertSnippetAt(TextPosition pos, string snippet)
    {
        InsertSnippetAt(pos, snippet, SnippetInsertOptions.Default);
    }

    public void InsertSnippetAt(TextPosition pos, string snippet, SnippetInsertOptions options)
    {
        SnippetSession session = CreateSession();
        session.InsertSnippet(pos, snippet, options);
    }
}
