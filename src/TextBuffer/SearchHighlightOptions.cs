// Source: ts/src/vs/editor/common/model/textModelSearch.ts
// - Interface: SearchParams and related types
// Ported: 2025-11-19

using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer;

public sealed class SearchHighlightOptions
{
    public string Query { get; init; } = string.Empty;
    public bool IsRegex { get; init; }
    public bool MatchCase { get; init; }
    public string? WordSeparators { get; init; }
    public bool CaptureMatches { get; init; }
    public int OwnerId { get; init; } = DecorationOwnerIds.SearchHighlights;
    public int Limit { get; init; } = TextModelSearch.DefaultLimit;
}
