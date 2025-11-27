// Source: vs/editor/common/model/textModel.ts
// - getDecorationsInRange parameters (Lines: 899-902)
// - getLineDecorations parameters (Lines: 891-895)
// Ported: 2025-11-28 (CL8-Phase2: Decoration search filter options)

namespace PieceTree.TextBuffer.Decorations;

/// <summary>
/// Options for filtering decorations during search.
/// Mirrors TS getDecorationsInRange/getLineDecorations parameters.
/// </summary>
public readonly record struct DecorationSearchOptions
{
    /// <summary>
    /// Default options with no filtering applied.
    /// </summary>
    public static readonly DecorationSearchOptions Default = new();

    /// <summary>
    /// Parameterless constructor required for record struct with field initializers.
    /// </summary>
    public DecorationSearchOptions()
    {
    }

    /// <summary>
    /// Filter decorations by owner. 0 means no filter (all owners).
    /// </summary>
    public int OwnerFilter { get; init; } = DecorationOwnerIds.Any;

    /// <summary>
    /// If true, excludes validation decorations (squiggly-error, squiggly-warning, squiggly-info).
    /// </summary>
    public bool FilterOutValidation { get; init; } = false;

    /// <summary>
    /// If true, excludes decorations that affect font rendering.
    /// </summary>
    public bool FilterFontDecorations { get; init; } = false;

    /// <summary>
    /// If true, only returns decorations that render in the minimap.
    /// </summary>
    public bool OnlyMinimapDecorations { get; init; } = false;

    /// <summary>
    /// If true, only returns decorations that render in the glyph margin.
    /// </summary>
    public bool OnlyMarginDecorations { get; init; } = false;

    /// <summary>
    /// Tree scopes to search.
    /// </summary>
    public DecorationTreeScope Scope { get; init; } = DecorationTreeScope.All;

    /// <summary>
    /// Creates options that filter by owner only.
    /// </summary>
    public static DecorationSearchOptions ForOwner(int ownerId)
        => new() { OwnerFilter = ownerId };

    /// <summary>
    /// Creates options that filter by scope only.
    /// </summary>
    public static DecorationSearchOptions ForScope(DecorationTreeScope scope)
        => new() { Scope = scope };
}
