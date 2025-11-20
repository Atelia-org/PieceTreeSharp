using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Cursor;

namespace PieceTree.TextBuffer.Tests
{
    public class CursorWordOperationsTests
    {
        [Fact]
        public void MoveWordRight_BasicWords()
        {
            var model = new TextModel("hello world-this_isCamelCase");
            // Move from start to first word boundary
            var pos = new TextPosition(1, 1);
            var next = WordOperations.MoveWordRight(model, pos, " -_");
            Assert.Equal(new TextPosition(1, 6), next); // after 'hello' -> space

            // Move to after 'world' (should skip '-')
            next = WordOperations.MoveWordRight(model, new TextPosition(1, 7), " -_");
            // 'world' is 5 letters -> start at 7 -> should go to 12
            Assert.Equal(new TextPosition(1, 12), next);
        }

        [Fact]
        public void MoveWordLeft_BasicWords()
        {
            var model = new TextModel("hello world-this_isCamelCase");
            var start = new TextPosition(1, 12); // after world
            var left = WordOperations.MoveWordLeft(model, start, " -_");
            Assert.Equal(new TextPosition(1, 7), left);

            left = WordOperations.MoveWordLeft(model, new TextPosition(1, 7), " -_");
            Assert.Equal(new TextPosition(1, 1), left);
        }

        [Fact]
        public void DeleteWordLeft_Basic()
        {
            var model = new TextModel("hello world");
            var sel = new Selection(new TextPosition(1, 1), new TextPosition(1, 12));
            var result = WordOperations.DeleteWordLeft(model, sel, " ");
            Assert.Equal(new Selection(new TextPosition(1, 7), new TextPosition(1, 12)), result);
        }
    }
}
