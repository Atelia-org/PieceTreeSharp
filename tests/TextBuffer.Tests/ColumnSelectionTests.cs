// Source: ts/src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts
// - Tests: Column selection and visible column calculations
// Ported: 2025-11-22

using PieceTree.TextBuffer.Cursor;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Tests;

public class ColumnSelectionTests
{
    [Fact]
    public void VisibleColumn_RoundTrip_WithTabs()
    {
        TextModel model = new("a\tb");
        int tabSize = 4;
        TextPosition pos = new(1, 1);
        int visible = CursorColumns.GetVisibleColumnFromPosition(model, pos, tabSize);
        TextPosition pos2 = CursorColumns.GetPositionFromVisibleColumn(model, 1, visible, tabSize);
        Assert.Equal(pos, pos2);

        pos = new TextPosition(1, 3);
        visible = CursorColumns.GetVisibleColumnFromPosition(model, pos, tabSize);
        pos2 = CursorColumns.GetPositionFromVisibleColumn(model, 1, visible, tabSize);
        Assert.Equal(pos, pos2);
    }

    [Fact]
    public void VisibleColumn_AcountsForInjectedText_BeforeAndAfter()
    {
        TextModel model = new("Hello World");
        int owner = model.AllocateDecorationOwnerId();
        int startOff = model.GetOffsetAt(new TextPosition(1, 6));
        ModelDecorationOptions options = new()
        {
            Before = new ModelDecorationInjectedTextOptions { Content = "BEF" },
            After = new ModelDecorationInjectedTextOptions { Content = "AFT" },
            ShowIfCollapsed = true,
        };
        model.DeltaDecorations(owner, null, new[] { new ModelDeltaDecoration(new TextRange(startOff, startOff), options) });

        TextPosition pos = new(1, 6); // before 'W'
        int visible = CursorColumns.GetVisibleColumnFromPosition(model, pos, tabSize: 4);
        // visible column should include BEF before content, so larger than 6
        Assert.True(visible > 6);
    }

    [Fact]
    public void Cursor_ColumnSelection_Basic()
    {
        TextModel model = new("a\tc\nshort");
        Cursor.Cursor cursor = new(model);
        cursor.MoveTo(new TextPosition(1, 1));
        cursor.StartColumnSelection();
        cursor.ColumnSelectTo(new TextPosition(2, 2));
        Assert.False(cursor.Selection.IsEmpty);
        // Validate anchor on line 1 and active on line 2
        Assert.Equal(1, cursor.Selection.Start.LineNumber);
        Assert.Equal(2, cursor.Selection.End.LineNumber);
    }
}
