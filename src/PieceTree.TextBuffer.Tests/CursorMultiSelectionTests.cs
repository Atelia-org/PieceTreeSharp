// Source: ts/src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts
// - Tests: Multi-cursor editing and rendering
// Ported: 2025-11-22

using System;
using System.Linq;
using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Rendering;
using PieceTree.TextBuffer.Cursor;

namespace PieceTree.TextBuffer.Tests
{
    public class CursorMultiSelectionTests
    {
        [Fact]
        public void MultiCursor_RendersMultipleCursorsAndSelections()
        {
            var model = new TextModel("Hello World\nThis is a test");
            var renderer = new MarkdownRenderer();
            var collection = new CursorCollection(model);

            var c1 = collection.CreateCursor(new TextPosition(1, 1));
            var c2 = collection.CreateCursor(new TextPosition(1, 7));

            var output = renderer.Render(model);
            Assert.Contains("|", output);
            // Should have two cursor markers
            Assert.Equal(2, output.Count(ch => ch == '|'));
            c1.Dispose();
            c2.Dispose();
            collection.Dispose();
        }

        [Fact]
        public void MultiCursor_EditAtMultipleCursors()
        {
            var model = new TextModel("Hello World\nThis is a test");
            var renderer = new MarkdownRenderer();
            var collection = new CursorCollection(model);

            var c1 = collection.CreateCursor(new TextPosition(1, 1));
            var c2 = collection.CreateCursor(new TextPosition(1, 7));

            // Insert "X" at both cursors using batch edits
            var before = collection.GetCursorPositions();
            var edits = new[]
            {
                new TextEdit(c1.Selection.Active, c1.Selection.Active, "X"),
                new TextEdit(c2.Selection.Active, c2.Selection.Active, "Y"),
            };

            var ctx = new CursorContext(model, collection);
            model.PushEditOperations(edits, beforeCursorState: before, cursorStateComputer: ctx.ComputeAfterCursorState);

            // After insertion the line should start with 'X' and 'Hello ' should have a Y inserted
            var line = model.GetLineContent(1);
            Assert.StartsWith("X", line);
            Assert.Contains("Y", line);

            c1.Dispose();
            c2.Dispose();
            collection.Dispose();
        }
    }
}
