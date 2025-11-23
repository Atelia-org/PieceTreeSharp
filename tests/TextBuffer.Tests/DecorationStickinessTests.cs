// Source: ts/src/vs/editor/test/common/model/modelDecorations.test.ts
// - Focus: tracked range stickiness matrix for insertions at edges
// Ported: 2025-11-23

using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Tests
{
    public class DecorationStickinessTests
    {
        [Theory]
        [InlineData(TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges, true, true)]
        [InlineData(TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges, false, false)]
        [InlineData(TrackedRangeStickiness.GrowsOnlyWhenTypingBefore, true, false)]
        [InlineData(TrackedRangeStickiness.GrowsOnlyWhenTypingAfter, false, true)]
        public void InsertionsAtEdgesMatchStickinessMatrix(TrackedRangeStickiness stickiness, bool growsAtStart, bool growsAtEnd)
        {
            var startResult = SimulateEdgeInsertion(stickiness, insertAtStart: true);
            var originalLength = startResult.OriginalEnd - startResult.OriginalStart;
            var expectedStart = growsAtStart ? startResult.OriginalStart : startResult.OriginalStart + startResult.InsertLength;
            var expectedLength = growsAtStart ? originalLength + startResult.InsertLength : originalLength;

            Assert.Equal(expectedStart, startResult.UpdatedRange.StartOffset);
            Assert.Equal(expectedLength, startResult.UpdatedRange.Length);

            var endResult = SimulateEdgeInsertion(stickiness, insertAtStart: false);
            originalLength = endResult.OriginalEnd - endResult.OriginalStart;
            var expectedEnd = growsAtEnd ? endResult.OriginalEnd + endResult.InsertLength : endResult.OriginalEnd;
            expectedLength = growsAtEnd ? originalLength + endResult.InsertLength : originalLength;

            Assert.Equal(expectedEnd, endResult.UpdatedRange.EndOffset);
            Assert.Equal(expectedLength, endResult.UpdatedRange.Length);
            Assert.Equal(endResult.OriginalStart, endResult.UpdatedRange.StartOffset);
        }

        private static StickinessResult SimulateEdgeInsertion(TrackedRangeStickiness stickiness, bool insertAtStart)
        {
            var model = new TextModel("abcdefghijklmnop");
            var decoration = model.AddDecoration(new TextRange(4, 9), new ModelDecorationOptions
            {
                Stickiness = stickiness,
            });

            var originalStart = decoration.Range.StartOffset;
            var originalEnd = decoration.Range.EndOffset;
            const string insertText = "ZZ";
            var insertPosition = model.GetPositionAt(insertAtStart ? originalStart : originalEnd);
            model.ApplyEdits(new[] { new TextEdit(insertPosition, insertPosition, insertText) });

            return new StickinessResult(originalStart, originalEnd, insertText.Length, decoration.Range);
        }

        private readonly record struct StickinessResult(int OriginalStart, int OriginalEnd, int InsertLength, TextRange UpdatedRange);
    }
}
