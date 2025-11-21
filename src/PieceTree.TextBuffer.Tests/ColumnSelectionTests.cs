// Source: ts/src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts
// - Tests: Column selection and visible column calculations
// Ported: 2025-11-22

using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Cursor;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Tests
{
    public class ColumnSelectionTests
    {
        [Fact]
        public void VisibleColumn_RoundTrip_WithTabs()
        {
            var model = new TextModel("a\tb");
            var tabSize = 4;
            var pos = new TextPosition(1, 1);
            var visible = CursorColumns.GetVisibleColumnFromPosition(model, pos, tabSize);
            var pos2 = CursorColumns.GetPositionFromVisibleColumn(model, 1, visible, tabSize);
            Assert.Equal(pos, pos2);

            pos = new TextPosition(1, 3);
            visible = CursorColumns.GetVisibleColumnFromPosition(model, pos, tabSize);
            pos2 = CursorColumns.GetPositionFromVisibleColumn(model, 1, visible, tabSize);
            Assert.Equal(pos, pos2);
        }

        [Fact]
        public void VisibleColumn_AcountsForInjectedText_BeforeAndAfter()
        {
            var model = new TextModel("Hello World");
            var owner = model.AllocateDecorationOwnerId();
            var startOff = model.GetOffsetAt(new TextPosition(1, 6));
            var options = new ModelDecorationOptions
            {
                Before = new ModelDecorationInjectedTextOptions { Content = "BEF" },
                After = new ModelDecorationInjectedTextOptions { Content = "AFT" },
                ShowIfCollapsed = true,
            };
            model.DeltaDecorations(owner, null, new[] { new ModelDeltaDecoration(new TextRange(startOff, startOff), options) });

            var pos = new TextPosition(1, 6); // before 'W'
            var visible = CursorColumns.GetVisibleColumnFromPosition(model, pos, tabSize: 4);
            // visible column should include BEF before content, so larger than 6
            Assert.True(visible > 6);
        }

        [Fact]
        public void Cursor_ColumnSelection_Basic()
        {
            var model = new TextModel("a\tc\nshort");
            var cursor = new PieceTree.TextBuffer.Cursor.Cursor(model);
            cursor.MoveTo(new TextPosition(1, 1));
            cursor.StartColumnSelection();
            cursor.ColumnSelectTo(new TextPosition(2, 2));
            Assert.False(cursor.Selection.IsEmpty);
            // Validate anchor on line 1 and active on line 2
            Assert.Equal(1, cursor.Selection.Start.LineNumber);
            Assert.Equal(2, cursor.Selection.End.LineNumber);
        }
    }
}
