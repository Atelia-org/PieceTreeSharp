using System;
using System.Collections.Generic;
using PieceTree.TextBuffer.Decorations;

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
}
