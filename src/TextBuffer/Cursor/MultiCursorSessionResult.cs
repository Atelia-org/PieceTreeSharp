// Source: ts/src/vs/editor/contrib/multicursor/browser/multicursor.ts
// - Class: MultiCursorSessionResult (Lines: 267-273)
// Ported: 2025-12-05 (Direct translation from TypeScript)

using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;
using Selection = PieceTree.TextBuffer.Core.Selection;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Result of a multi-cursor session operation (add, move, select all).
/// Contains the new selections and information about what range to reveal.
/// Based on TS MultiCursorSessionResult class (multicursor.ts L267-273).
/// </summary>
public sealed record MultiCursorSessionResult(
    IReadOnlyList<Selection> Selections,
    Range RevealRange,
    ScrollType RevealScrollType
);

/// <summary>
/// Scroll behavior when revealing a range.
/// Based on TS ScrollType enum.
/// </summary>
public enum ScrollType
{
    /// <summary>
    /// Smooth animated scroll.
    /// </summary>
    Smooth,

    /// <summary>
    /// Immediate jump to position.
    /// </summary>
    Immediate
}
