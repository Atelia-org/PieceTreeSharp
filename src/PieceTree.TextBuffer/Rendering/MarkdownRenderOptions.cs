using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Rendering;

public sealed class MarkdownRenderOptions
{
    public MarkdownSearchOptions? Search { get; init; }
    public int OwnerIdFilter { get; init; } = DecorationOwnerIds.Any;
}

public sealed class MarkdownSearchOptions
{
    public string Query { get; init; } = string.Empty;
    public bool IsRegex { get; init; }
    public bool MatchCase { get; init; }
    public string? WordSeparators { get; init; }
    public bool CaptureMatches { get; init; }
    public int Limit { get; init; } = TextModelSearch.DefaultLimit;
}
