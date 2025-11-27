// Source: vs/editor/common/model.ts
// - Enum: TrackedRangeStickiness (Lines: 673-677)
// - Interface: IModelDecoration (Lines: 385-403)
// - Interface: IModelDecorationOptions (Lines: 147-310)
// - Interface: IModelDecorationOverviewRulerOptions (Lines: 127-133)
// - Interface: IModelDecorationMinimapOptions (Lines: 138-150)
// - Interface: IModelDecorationGlyphMarginOptions (Lines: 109-118)
// - Interface: InjectedTextOptions (Lines: 321-355)
// - Enum: OverviewRulerLane (Lines: 35-40)
// - Enum: GlyphMarginLane (Lines: 45-49)
// - Enum: MinimapPosition (Lines: 74-77)
// - Enum: TextDirection (Lines: 313-318)
// - Enum: InjectedTextCursorStops (Lines: 357-362)
// Ported: 2025-11-22

using System;
using System.Text;

namespace PieceTree.TextBuffer.Decorations
{
    public enum TrackedRangeStickiness
    {
        AlwaysGrowsWhenTypingAtEdges = 0,
        NeverGrowsWhenTypingAtEdges = 1,
        GrowsOnlyWhenTypingBefore = 2,
        GrowsOnlyWhenTypingAfter = 3,
    }

    [Flags]
    public enum OverviewRulerLane
    {
        Left = 1,
        Center = 2,
        Right = 4,
        Full = Left | Center | Right,
    }

    public enum MinimapPosition
    {
        Inline = 0,
        Gutter = 1,
    }

    public enum GlyphMarginLane
    {
        Left = 0,
        Center = 1,
        Right = 2,
    }

    public enum TextDirection
    {
        Ltr = 0,
        Rtl = 1,
    }

    [Flags]
    public enum InjectedTextCursorStops
    {
        None = 0,
        Before = 1,
        After = 2,
        Both = Before | After,
    }

    public enum DecorationRenderKind
    {
        /// <summary>
        /// No visual representation (used for tracked ranges).
        /// </summary>
        None = -1,
        Selection = 0,
        Cursor = 1,
        SearchMatch = 2,
        Generic = 3,
    }

    public sealed record class ModelDecorationOverviewRulerOptions
    {
        public string? Color { get; init; }
        public string? DarkColor { get; init; }
        public OverviewRulerLane Position { get; init; } = OverviewRulerLane.Center;

        public bool HasColor => !string.IsNullOrWhiteSpace(Color) || !string.IsNullOrWhiteSpace(DarkColor);
    }

    public sealed record class ModelDecorationMinimapOptions
    {
        public string? Color { get; init; }
        public string? DarkColor { get; init; }
        public MinimapPosition Position { get; init; } = MinimapPosition.Inline;
        public string? SectionHeaderStyle { get; init; }
        public string? SectionHeaderText { get; init; }
    }

    public sealed record class ModelDecorationGlyphMarginOptions
    {
        public GlyphMarginLane Position { get; init; } = GlyphMarginLane.Center;
        public bool PersistLane { get; init; }
    }

    public sealed record class ModelDecorationInjectedTextOptions
    {
        public string Content { get; init; } = string.Empty;
        public string? InlineClassName { get; init; }
        public bool InlineClassNameAffectsLetterSpacing { get; init; }
        public InjectedTextCursorStops CursorStops { get; init; } = InjectedTextCursorStops.Both;
        public object? AttachedData { get; init; }
    }

    public readonly record struct ModelDecorationBlockPadding(int Top, int Right, int Bottom, int Left);

    public sealed record class ModelDecorationOptions
    {
        private const int LineHeightCeiling = 300;
        private readonly bool _isNormalized;

        public string Description { get; init; } = "model-decoration";
        public TrackedRangeStickiness Stickiness { get; init; } = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges;
        public int ZIndex { get; init; }
        public bool IsWholeLine { get; init; }
        public bool ShouldFillLineOnLineBreak { get; init; }
        public bool ShowIfCollapsed { get; init; } = false;
        public bool CollapseOnReplaceEdit { get; init; }
        public bool HideInCommentTokens { get; init; }
        public bool HideInStringTokens { get; init; }
        public bool? BlockIsAfterEnd { get; init; }
        public bool? BlockDoesNotCollapse { get; init; }
        public ModelDecorationBlockPadding? BlockPadding { get; init; }
        public string? BlockClassName { get; init; }
        public string? ClassName { get; init; }
        public string? InlineClassName { get; init; }
        public bool InlineClassNameAffectsLetterSpacing { get; init; }
        public string? BeforeContentClassName { get; init; }
        public string? AfterContentClassName { get; init; }
        public string? GlyphMarginClassName { get; init; }
        public string? LinesDecorationsClassName { get; init; }
        public string? LineNumberClassName { get; init; }
        public string? FirstLineDecorationClassName { get; init; }
        public string? MarginClassName { get; init; }
        public string? HoverMessage { get; init; }
        public string? GlyphMarginHoverMessage { get; init; }
        public string? LineNumberHoverMessage { get; init; }
        public string? LinesDecorationsTooltip { get; init; }
        public int? LineHeight { get; init; }
        public string? FontSize { get; init; }
        public string? FontFamily { get; init; }
        public string? FontWeight { get; init; }
        public string? FontStyle { get; init; }
        public string? TextDecoration { get; init; }
        public string? MarginHoverMessage { get; init; }
        public string? InlineDescription { get; init; }
        public ModelDecorationOverviewRulerOptions? OverviewRuler { get; init; }
        public ModelDecorationMinimapOptions? Minimap { get; init; }
        public ModelDecorationGlyphMarginOptions? GlyphMargin { get; init; }
        public ModelDecorationInjectedTextOptions? Before { get; init; }
        public ModelDecorationInjectedTextOptions? After { get; init; }
        public TextDirection? TextDirection { get; init; }
        public DecorationRenderKind RenderKind { get; init; } = DecorationRenderKind.Selection;

        public static ModelDecorationOptions Default { get; } = new ModelDecorationOptions().Normalize();

        public static ModelDecorationOptions CreateCursorOptions() => new ModelDecorationOptions
        {
            Description = "cursor",
            RenderKind = DecorationRenderKind.Cursor,
            ShowIfCollapsed = true,
        }.Normalize();

        public static ModelDecorationOptions CreateSelectionOptions(TrackedRangeStickiness stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges)
            => new ModelDecorationOptions
            {
                Description = "selection",
                RenderKind = DecorationRenderKind.Selection,
                Stickiness = stickiness,
                ShowIfCollapsed = false,
            }.Normalize();

        public static ModelDecorationOptions CreateSearchMatchOptions() => new ModelDecorationOptions
        {
            Description = "search-match",
            RenderKind = DecorationRenderKind.SearchMatch,
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ShowIfCollapsed = false,
        }.Normalize();

        /// <summary>
        /// Create options for a hidden decoration (used for tracked ranges).
        /// These decorations have no visual representation.
        /// </summary>
        public static ModelDecorationOptions CreateHiddenOptions(TrackedRangeStickiness stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges)
            => new ModelDecorationOptions
            {
                Description = "tracked-range",
                RenderKind = DecorationRenderKind.None,
                Stickiness = stickiness,
                ShowIfCollapsed = false,
            }.Normalize();

        internal bool HasInjectedText => Before is not null || After is not null;
        internal bool AffectsFont => !string.IsNullOrEmpty(FontSize) || !string.IsNullOrEmpty(FontFamily) || !string.IsNullOrEmpty(FontWeight) || !string.IsNullOrEmpty(FontStyle);
        internal bool AffectsOverviewRuler => OverviewRuler?.HasColor == true;
        internal bool AffectsMinimap => Minimap is not null;
        internal bool AffectsGlyphMargin => !string.IsNullOrWhiteSpace(GlyphMarginClassName);
        internal bool AffectsLineNumber => !string.IsNullOrWhiteSpace(LineNumberClassName);

        internal ModelDecorationOptions Normalize()
        {
            if (_isNormalized)
            {
                return this;
            }

            return new ModelDecorationOptions(this);
        }

        private ModelDecorationOptions(ModelDecorationOptions source)
        {
            _isNormalized = true;
            Description = string.IsNullOrWhiteSpace(source.Description) ? "model-decoration" : source.Description;
            Stickiness = source.Stickiness;
            ZIndex = source.ZIndex;
            IsWholeLine = source.IsWholeLine;
            ShouldFillLineOnLineBreak = source.ShouldFillLineOnLineBreak;
            ShowIfCollapsed = source.ShowIfCollapsed;
            CollapseOnReplaceEdit = source.CollapseOnReplaceEdit;
            HideInCommentTokens = source.HideInCommentTokens;
            HideInStringTokens = source.HideInStringTokens;
            BlockIsAfterEnd = source.BlockIsAfterEnd;
            BlockDoesNotCollapse = source.BlockDoesNotCollapse;
            BlockPadding = NormalizePadding(source.BlockPadding);
            BlockClassName = CleanClassName(source.BlockClassName);
            ClassName = CleanClassName(source.ClassName);
            InlineClassName = CleanClassName(source.InlineClassName);
            InlineClassNameAffectsLetterSpacing = source.InlineClassNameAffectsLetterSpacing;
            BeforeContentClassName = CleanClassName(source.BeforeContentClassName);
            AfterContentClassName = CleanClassName(source.AfterContentClassName);
            GlyphMarginClassName = CleanClassName(source.GlyphMarginClassName);
            LinesDecorationsClassName = CleanClassName(source.LinesDecorationsClassName);
            LineNumberClassName = CleanClassName(source.LineNumberClassName);
            FirstLineDecorationClassName = CleanClassName(source.FirstLineDecorationClassName);
            MarginClassName = CleanClassName(source.MarginClassName);
            HoverMessage = source.HoverMessage;
            GlyphMarginHoverMessage = source.GlyphMarginHoverMessage;
            LineNumberHoverMessage = source.LineNumberHoverMessage;
            LinesDecorationsTooltip = source.LinesDecorationsTooltip;
            LineHeight = NormalizeLineHeight(source.LineHeight);
            FontSize = source.FontSize;
            FontFamily = source.FontFamily;
            FontWeight = source.FontWeight;
            FontStyle = source.FontStyle;
            TextDecoration = source.TextDecoration;
            MarginHoverMessage = source.MarginHoverMessage;
            InlineDescription = source.InlineDescription;
            OverviewRuler = source.OverviewRuler is null ? null : new ModelDecorationOverviewRulerOptions
            {
                Color = source.OverviewRuler.Color,
                DarkColor = source.OverviewRuler.DarkColor,
                Position = source.OverviewRuler.Position,
            };
            Minimap = source.Minimap is null ? null : new ModelDecorationMinimapOptions
            {
                Color = source.Minimap.Color,
                DarkColor = source.Minimap.DarkColor,
                Position = source.Minimap.Position,
                SectionHeaderStyle = source.Minimap.SectionHeaderStyle,
                SectionHeaderText = source.Minimap.SectionHeaderText,
            };
            GlyphMargin = source.GlyphMargin is null ? null : new ModelDecorationGlyphMarginOptions
            {
                Position = source.GlyphMargin.Position,
                PersistLane = source.GlyphMargin.PersistLane,
            };
            Before = NormalizeInjectedText(source.Before);
            After = NormalizeInjectedText(source.After);
            TextDirection = source.TextDirection;
            RenderKind = source.RenderKind;
        }

        private static int? NormalizeLineHeight(int? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            var clamped = Math.Clamp(value.Value, 1, LineHeightCeiling);
            return clamped;
        }

        private static ModelDecorationBlockPadding? NormalizePadding(ModelDecorationBlockPadding? padding)
        {
            if (!padding.HasValue)
            {
                return null;
            }

            var value = padding.Value;
            return new ModelDecorationBlockPadding(
                Math.Max(0, value.Top),
                Math.Max(0, value.Right),
                Math.Max(0, value.Bottom),
                Math.Max(0, value.Left));
        }

        private static ModelDecorationInjectedTextOptions? NormalizeInjectedText(ModelDecorationInjectedTextOptions? value)
        {
            if (value is null)
            {
                return null;
            }

            return new ModelDecorationInjectedTextOptions
            {
                Content = value.Content ?? string.Empty,
                InlineClassName = CleanClassName(value.InlineClassName),
                InlineClassNameAffectsLetterSpacing = value.InlineClassNameAffectsLetterSpacing,
                CursorStops = value.CursorStops,
                AttachedData = value.AttachedData,
            };
        }

        private static string? CleanClassName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
                {
                    builder.Append(ch);
                }
                else
                {
                    builder.Append(' ');
                }
            }

            var cleaned = builder.ToString().Trim();
            return cleaned.Length == 0 ? null : cleaned;
        }
    }

    public readonly struct TextRange
    {
        public int StartOffset { get; }
        public int EndOffset { get; }
        public int Length => Math.Max(0, EndOffset - StartOffset);
        public bool IsEmpty => StartOffset == EndOffset;

        public TextRange(int startOffset, int endOffset)
        {
            if (endOffset < startOffset)
            {
                (startOffset, endOffset) = (endOffset, startOffset);
            }

            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public TextRange With(int? start = null, int? end = null) => new(start ?? StartOffset, end ?? EndOffset);

        public override string ToString() => $"[{StartOffset}, {EndOffset})";
    }

    public sealed class ModelDecoration
    {
        public ModelDecoration(string id, int ownerId, TextRange range, ModelDecorationOptions options)
        {
            Id = id;
            OwnerId = ownerId;
            Range = range;
            Options = options?.Normalize() ?? throw new ArgumentNullException(nameof(options));
        }

        public string Id { get; }
        public int OwnerId { get; }
        public TextRange Range { get; set; }
        public ModelDecorationOptions Options { get; }
        public int VersionId { get; internal set; }

        public bool IsCollapsed => Range.IsEmpty;
    }
}
