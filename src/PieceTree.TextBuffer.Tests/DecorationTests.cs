using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Tests
{
    public class DecorationTests
    {
        [Fact]
        public void DeltaDecorationsTrackOwnerScopes()
        {
            var model = new TextModel("alpha beta gamma");
            var owner = model.AllocateDecorationOwnerId();

            var added = model.DeltaDecorations(owner, null, new[]
            {
                new ModelDeltaDecoration(new TextRange(0, 5), ModelDecorationOptions.CreateSelectionOptions()),
                new ModelDeltaDecoration(new TextRange(6, 10), ModelDecorationOptions.CreateSelectionOptions()),
            });

            Assert.Equal(2, added.Count);
            Assert.Equal(2, model.GetDecorationsInRange(new TextRange(0, model.GetLength()), owner).Count);

            model.RemoveAllDecorations(owner);
            Assert.Empty(model.GetDecorationsInRange(new TextRange(0, model.GetLength()), owner));
        }

        [Fact]
        public void CollapseOnReplaceEditShrinksRange()
        {
            var model = new TextModel("function test() { call(); }");
            var options = new ModelDecorationOptions { CollapseOnReplaceEdit = true };
            var decoration = model.AddDecoration(new TextRange(13, 19), options);

            var startPosition = model.GetPositionAt(decoration.Range.StartOffset);
            var endPosition = model.GetPositionAt(decoration.Range.EndOffset);
            var expectedOffset = decoration.Range.StartOffset;

            model.ApplyEdits(new[]
            {
                new TextEdit(startPosition, endPosition, "noop();")
            });

            Assert.True(decoration.Range.IsEmpty);
            Assert.Equal(expectedOffset, decoration.Range.StartOffset);
        }

        [Fact]
        public void StickinessHonorsInsertions()
        {
            var model = new TextModel("abcdefghij");
            var always = model.AddDecoration(new TextRange(2, 4), new ModelDecorationOptions { Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges });
            var never = model.AddDecoration(new TextRange(5, 7), new ModelDecorationOptions { Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges });

            var originalAlwaysEnd = always.Range.EndOffset;
            var originalNeverStart = never.Range.StartOffset;

            // Insert at the leading edge of both decorations
            var insertAtAlways = model.GetPositionAt(always.Range.StartOffset);
            model.ApplyEdits(new[] { new TextEdit(insertAtAlways, insertAtAlways, "XX") });

            var insertAtNever = model.GetPositionAt(never.Range.StartOffset);
            model.ApplyEdits(new[] { new TextEdit(insertAtNever, insertAtNever, "YY") });

            Assert.Equal(2, always.Range.StartOffset);
            Assert.True(always.Range.EndOffset > originalAlwaysEnd);

            Assert.True(never.Range.StartOffset > originalNeverStart);
        }
    }
}
