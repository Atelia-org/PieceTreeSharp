using Xunit;
using PieceTree.TextBuffer.Decorations;
using System.Linq;

namespace PieceTree.TextBuffer.Tests
{
    public class DecorationTests
    {
        [Fact]
        public void TestAddDecoration()
        {
            var tree = new IntervalTree();
            var decoration = new ModelDecoration("1", new TextRange(10, 20), ModelDecorationOptions.Default);
            tree.Insert(decoration);

            var results = tree.Search(new TextRange(0, 30)).ToList();
            Assert.Single(results);
            Assert.Equal("1", results[0].Id);
        }

        [Fact]
        public void TestInsertTextBeforeDecoration()
        {
            var tree = new IntervalTree();
            // Range [10, 20)
            var decoration = new ModelDecoration("1", new TextRange(10, 20), ModelDecorationOptions.Default);
            tree.Insert(decoration);

            // Insert 5 chars at 0
            tree.AcceptReplace(0, 0, 5);

            // Should shift to [15, 25)
            Assert.Equal(15, decoration.Range.StartOffset);
            Assert.Equal(25, decoration.Range.EndOffset);
        }

        [Fact]
        public void TestInsertTextInsideDecoration()
        {
            var tree = new IntervalTree();
            // Range [10, 20)
            var decoration = new ModelDecoration("1", new TextRange(10, 20), ModelDecorationOptions.Default);
            tree.Insert(decoration);

            // Insert 5 chars at 15
            tree.AcceptReplace(15, 0, 5);

            // Should expand to [10, 25)
            Assert.Equal(10, decoration.Range.StartOffset);
            Assert.Equal(25, decoration.Range.EndOffset);
        }

        [Fact]
        public void TestDeleteTextBeforeDecoration()
        {
            var tree = new IntervalTree();
            // Range [10, 20)
            var decoration = new ModelDecoration("1", new TextRange(10, 20), ModelDecorationOptions.Default);
            tree.Insert(decoration);

            // Delete 5 chars at 0
            tree.AcceptReplace(0, 5, 0);

            // Should shift to [5, 15)
            Assert.Equal(5, decoration.Range.StartOffset);
            Assert.Equal(15, decoration.Range.EndOffset);
        }

        [Fact]
        public void TestDeleteTextOverlappingStart()
        {
            var tree = new IntervalTree();
            // Range [10, 20)
            var decoration = new ModelDecoration("1", new TextRange(10, 20), ModelDecorationOptions.Default);
            tree.Insert(decoration);

            // Delete from 5 to 15 (length 10)
            tree.AcceptReplace(5, 10, 0);

            // Start was 10. Deleted range [5, 15).
            // 10 is inside deleted range, so it moves to 5.
            // End was 20. 20 is after deleted range, so it shifts by -10 -> 10.
            // Result: [5, 10)
            Assert.Equal(5, decoration.Range.StartOffset);
            Assert.Equal(10, decoration.Range.EndOffset);
        }

        [Fact]
        public void TestStickiness_AlwaysGrows()
        {
            var tree = new IntervalTree();
            var options = new ModelDecorationOptions { Stickiness = TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges };
            var decoration = new ModelDecoration("1", new TextRange(10, 20), options);
            tree.Insert(decoration);

            // Insert at start (10)
            tree.AcceptReplace(10, 0, 5);
            // Should grow: [10, 25)
            Assert.Equal(10, decoration.Range.StartOffset);
            Assert.Equal(25, decoration.Range.EndOffset);

            // Insert at end (25)
            tree.AcceptReplace(25, 0, 5);
            // Should grow: [10, 30)
            Assert.Equal(10, decoration.Range.StartOffset);
            Assert.Equal(30, decoration.Range.EndOffset);
        }

        [Fact]
        public void TestStickiness_NeverGrows()
        {
            var tree = new IntervalTree();
            var options = new ModelDecorationOptions { Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges };
            var decoration = new ModelDecoration("1", new TextRange(10, 20), options);
            tree.Insert(decoration);

            // Insert at start (10)
            tree.AcceptReplace(10, 0, 5);
            // Should shift: [15, 25)
            Assert.Equal(15, decoration.Range.StartOffset);
            Assert.Equal(25, decoration.Range.EndOffset);

            // Insert at end (25)
            tree.AcceptReplace(25, 0, 5);
            // Should not grow: [15, 25)
            Assert.Equal(15, decoration.Range.StartOffset);
            Assert.Equal(25, decoration.Range.EndOffset);
        }
    }
}
