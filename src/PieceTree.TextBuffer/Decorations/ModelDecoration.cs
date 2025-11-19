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

    public class ModelDecorationOptions
    {
        public string ClassName { get; set; }
        public TrackedRangeStickiness Stickiness { get; set; }
        public bool IsWholeLine { get; set; }
        
        public static readonly ModelDecorationOptions Default = new ModelDecorationOptions 
        { 
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges 
        };
    }

    public struct TextRange
    {
        public int StartOffset { get; }
        public int EndOffset { get; }
        public int Length => EndOffset - StartOffset;

        public TextRange(int startOffset, int endOffset)
        {
            StartOffset = startOffset;
            EndOffset = endOffset;
        }

        public override string ToString() => $"[{StartOffset}, {EndOffset})";
    }

    public class ModelDecoration
    {
        public string Id { get; }
        public TextRange Range { get; set; }
        public ModelDecorationOptions Options { get; }

        public ModelDecoration(string id, TextRange range, ModelDecorationOptions options)
        {
            Id = id;
            Range = range;
            Options = options;
        }
    }
}
