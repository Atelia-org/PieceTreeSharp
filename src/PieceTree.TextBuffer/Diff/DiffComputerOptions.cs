// Source: ts/src/vs/editor/common/diff/defaultLinesDiffComputer/defaultLinesDiffComputer.ts
// - Interface: ILinesDiffComputerOptions (Lines: 15-30)
// Source: ts/src/vs/editor/common/diff/linesDiffComputer.ts
// - Interface: LinesDiffComputerOptions
// Ported: 2025-11-21

namespace PieceTree.TextBuffer.Diff;

public sealed class DiffComputerOptions
{
    /// <summary>
    /// Mirrors VS Code's "Ignore trim whitespace" toggle. When true, the diff treats leading/trailing
    /// whitespace changes within equal lines as unchanged unless characters differ.
    /// </summary>
    public bool IgnoreTrimWhitespace { get; init; }

    /// <summary>
    /// Maximum amount of time in milliseconds the diff computation should take before returning a partial result.
    /// Use 0 for no timeout (matches TS InfiniteTimeout).
    /// </summary>
    public int MaxComputationTimeMs { get; init; }

    /// <summary>
    /// When true, the diff computation attempts to detect moved blocks.
    /// </summary>
    public bool ComputeMoves { get; init; } = true;

    /// <summary>
    /// When true (default), character-level diffs are extended to cover entire words when possible.
    /// Disable to inspect raw diff hunks without TS prettify heuristics.
    /// </summary>
    public bool ExtendToWordBoundaries { get; init; } = true;

    /// <summary>
    /// When true, inner changes will be extended to cover camelCase/PascalCase subwords similar to VS Code.
    /// </summary>
    public bool ExtendToSubwords { get; init; }
}
