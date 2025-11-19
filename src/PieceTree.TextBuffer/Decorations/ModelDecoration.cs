using System;

namespace PieceTree.TextBuffer.Decorations
{
    public enum TrackedRangeStickiness
    {
        AlwaysGrowsWhenTypingAtEdges = 0,
        NeverGrowsWhenTypingAtEdges = 1,
        GrowsOnlyWhenTypingBefore = 2,
        GrowsOnlyWhenTypingAfter = 3,
    }

    public enum DecorationRenderKind
    {
        Selection = 0,
        Cursor = 1,
        SearchMatch = 2,
    }

    public sealed class ModelDecorationOptions
    {
        public string? ClassName { get; init; }
        public TrackedRangeStickiness Stickiness { get; init; } = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges;
        public bool IsWholeLine { get; init; }
        public bool CollapseOnReplaceEdit { get; init; }
        public bool ForceMoveMarkers { get; init; }
        public bool ShowIfCollapsed { get; init; } = true;
        public DecorationRenderKind RenderKind { get; init; } = DecorationRenderKind.Selection;

        public static ModelDecorationOptions Default { get; } = new();

        public static ModelDecorationOptions CreateCursorOptions() => new()
        {
            ForceMoveMarkers = true,
            RenderKind = DecorationRenderKind.Cursor,
            Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges,
            ShowIfCollapsed = true,
        };

        public static ModelDecorationOptions CreateSelectionOptions(TrackedRangeStickiness stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges)
            => new()
            {
                Stickiness = stickiness,
                RenderKind = DecorationRenderKind.Selection,
                ShowIfCollapsed = false,
            };

        public static ModelDecorationOptions CreateSearchMatchOptions() => new()
        {
            RenderKind = DecorationRenderKind.SearchMatch,
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ShowIfCollapsed = false,
        };
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
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public string Id { get; }
        public int OwnerId { get; }
        public TextRange Range { get; set; }
        public ModelDecorationOptions Options { get; }
        public int VersionId { get; internal set; }

        public bool IsCollapsed => Range.IsEmpty;
    }
}
