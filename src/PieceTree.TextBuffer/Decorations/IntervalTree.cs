using System;
using System.Collections.Generic;
using System.Linq;

namespace PieceTree.TextBuffer.Decorations
{
    /// <summary>
    /// Manages decorations in the text model.
    /// TODO: v2 - Implement full Red-Black Tree with delta propagation for O(log N) performance.
    /// Currently using a simplified List<ModelDecoration> implementation (O(N)) for v1.
    /// </summary>
    public class IntervalTree
    {
        private readonly List<ModelDecoration> _decorations = new List<ModelDecoration>();

        public void Insert(ModelDecoration decoration)
        {
            _decorations.Add(decoration);
        }

        public void Delete(ModelDecoration decoration)
        {
            _decorations.Remove(decoration);
        }

        public IEnumerable<ModelDecoration> Search(TextRange range)
        {
            // Return decorations that overlap with the range
            return _decorations.Where(d => 
            {
                if (d.Range.Length == 0)
                {
                    // Point decoration: must be within [Start, End)
                    // But wait, if range is [Start, End), does it include Start? Yes.
                    // Does it include End? No.
                    // So >= Start && < End.
                    return d.Range.StartOffset >= range.StartOffset && d.Range.StartOffset < range.EndOffset;
                }
                
                return d.Range.StartOffset < range.EndOffset && 
                       d.Range.EndOffset > range.StartOffset;
            });
        }

        public IEnumerable<ModelDecoration> GetAll()
        {
            return _decorations;
        }

        public void AcceptReplace(int offset, int length, int textLength)
        {
            // offset: start of the edit
            // length: length of text being replaced (deleted)
            // textLength: length of new text inserted

            foreach (var decoration in _decorations)
            {
                var start = decoration.Range.StartOffset;
                var end = decoration.Range.EndOffset;

                // 1. Handle Deletion
                // If the decoration is after the deleted range, shift it back
                if (length > 0)
                {
                    if (start >= offset + length)
                    {
                        start -= length;
                    }
                    else if (start >= offset)
                    {
                        // Start is inside the deleted range -> moves to offset
                        start = offset;
                    }

                    if (end >= offset + length)
                    {
                        end -= length;
                    }
                    else if (end >= offset)
                    {
                        // End is inside the deleted range -> moves to offset
                        end = offset;
                    }
                }

                // 2. Handle Insertion
                if (textLength > 0)
                {
                    // If insertion is strictly before start, shift start
                    if (offset < start)
                    {
                        start += textLength;
                    }
                    // If insertion is at start, check stickiness
                    else if (offset == start)
                    {
                        bool grow = decoration.Options.Stickiness == TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges ||
                                    decoration.Options.Stickiness == TrackedRangeStickiness.GrowsOnlyWhenTypingBefore;
                        if (!grow)
                        {
                            start += textLength;
                        }
                    }

                    // If insertion is strictly before end, shift end
                    if (offset < end)
                    {
                        end += textLength;
                    }
                    // If insertion is at end, check stickiness
                    else if (offset == end)
                    {
                        bool grow = decoration.Options.Stickiness == TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges ||
                                    decoration.Options.Stickiness == TrackedRangeStickiness.GrowsOnlyWhenTypingAfter;
                        if (grow)
                        {
                            end += textLength;
                        }
                    }
                }

                // Ensure valid range
                if (start > end) end = start;

                decoration.Range = new TextRange(start, end);
            }
        }
    }
}
