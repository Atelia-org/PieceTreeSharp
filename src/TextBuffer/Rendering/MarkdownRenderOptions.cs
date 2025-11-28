using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.DocUI;

namespace PieceTree.TextBuffer.Rendering;

public sealed class MarkdownRenderOptions
{
    public int OwnerIdFilter { get; init; } = DecorationOwnerIds.Any;
    public IReadOnlyList<int>? OwnerIdFilters { get; init; }
    public Func<int, bool>? OwnerFilterPredicate { get; init; }
    public int? StartLineNumber { get; init; }
    public int? EndLineNumber { get; init; }
    public int? LineCount { get; init; }
    public bool IncludeGlyphAnnotations { get; init; } = true;
    public bool IncludeMarginAnnotations { get; init; } = true;
    public bool IncludeOverviewAnnotations { get; init; } = true;
    public bool IncludeMinimapAnnotations { get; init; } = true;
    public bool IncludeInjectedText { get; init; } = true;
    
    /// <summary>
    /// Optional FindDecorations instance to use for search match rendering.
    /// When provided, the renderer will use cached match data instead of re-querying the model.
    /// </summary>
    public FindDecorations? FindDecorations { get; init; }
    
    /// <summary>
    /// When true and FindDecorations is provided, skip querying model for find-related decorations.
    /// </summary>
    public bool UseDirectFindDecorations { get; init; } = true;
}
