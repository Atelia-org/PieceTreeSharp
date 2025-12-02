// Source: ts/src/vs/editor/contrib/snippet/browser/snippetController2.ts
// - Class: SnippetController2 (Lines: 30-500)
// Ported: 2025-11-22
// Extended: 2025-12-02 (P1: Final Tabstop $0, adjustWhitespace options)

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
