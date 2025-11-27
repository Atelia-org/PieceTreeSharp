// Source: ts/src/vs/editor/common/textModelEvents.ts
// - Interface: IModelDecorationsChangedEvent and related types
// Ported: 2025-11-19

using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer;

public sealed record class LineHeightChange(int OwnerId, string DecorationId, int LineNumber, int? LineHeight);

public sealed record class LineFontChange(int OwnerId, string DecorationId, int LineNumber);

public sealed class TextModelDecorationsChangedEventArgs : EventArgs
{
    public TextModelDecorationsChangedEventArgs(
        IReadOnlyList<DecorationChange> changes,
        int modelVersionId,
        bool affectsMinimap,
        bool affectsOverviewRuler,
        bool affectsGlyphMargin,
        bool affectsLineNumber,
        IReadOnlyList<int> affectedInjectedTextLines,
        IReadOnlyList<LineHeightChange> affectedLineHeights,
        IReadOnlyList<LineFontChange> affectedFontLines)
    {
        Changes = changes;
        ModelVersionId = modelVersionId;
        AffectsMinimap = affectsMinimap;
        AffectsOverviewRuler = affectsOverviewRuler;
        AffectsGlyphMargin = affectsGlyphMargin;
        AffectsLineNumber = affectsLineNumber;
        AffectedInjectedTextLines = affectedInjectedTextLines;
        AffectedLineHeights = affectedLineHeights;
        AffectedFontLines = affectedFontLines;
    }

    public IReadOnlyList<DecorationChange> Changes { get; }
    public int ModelVersionId { get; }
    public bool AffectsMinimap { get; }
    public bool AffectsOverviewRuler { get; }
    public bool AffectsGlyphMargin { get; }
    public bool AffectsLineNumber { get; }
    public IReadOnlyList<int> AffectedInjectedTextLines { get; }
    public IReadOnlyList<LineHeightChange> AffectedLineHeights { get; }
    public IReadOnlyList<LineFontChange> AffectedFontLines { get; }
}
