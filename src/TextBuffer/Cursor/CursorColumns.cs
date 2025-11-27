// Source: ts/src/vs/editor/common/cursor/cursorColumnSelection.ts
// - Methods: visibleColumnFromColumn, columnFromVisibleColumn (Lines: 10-50)
// Ported: 2025-11-22

using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Helper conversions between visible columns and buffer positions. This is a simplified version
/// that handles tabs using the model's TabSize in options and injected text lengths.
/// </summary>
public static class CursorColumns
{
    public static int GetVisibleColumnFromPosition(TextModel model, TextPosition pos, int tabSize)
    {
        string line = model.GetLineContent(pos.LineNumber);
        int visible = 0;
        for (int i = 0; i < Math.Min(line.Length, pos.Column - 1); i++)
        {
            if (line[i] == '\t')
            {
                int add = tabSize - (visible % tabSize);
                visible += add;
            }
            else
            {
                visible++;
            }
        }

        // account for injected text on the line before pos
        IReadOnlyList<ModelDecoration> injected = model.GetInjectedTextInLine(pos.LineNumber, DecorationOwnerIds.Any);
        foreach (ModelDecoration dec in injected)
        {
            TextPosition decPos = model.GetPositionAt(dec.Range.StartOffset);
            if (decPos.Column <= pos.Column)
            {
                string content = dec.Options.Before?.Content ?? string.Empty;
                string after = dec.Options.After?.Content ?? string.Empty;
                visible += content.Length + after.Length;
            }
        }

        return visible + 1; // visible column is 1-based
    }

    public static TextPosition GetPositionFromVisibleColumn(TextModel model, int lineNumber, int visibleColumn, int tabSize)
    {
        string line = model.GetLineContent(lineNumber);
        int visible = 0;
        int index = 0;
        while (index < line.Length && visible + 1 < visibleColumn)
        {
            if (line[index] == '\t')
            {
                int add = tabSize - (visible % tabSize);
                visible += add;
                index++;
            }
            else
            {
                visible++;
                index++;
            }
        }

        // account for injected text in the same line
        IReadOnlyList<ModelDecoration> injected = model.GetInjectedTextInLine(lineNumber, DecorationOwnerIds.Any);
        foreach (ModelDecoration dec in injected)
        {
            TextPosition decPos = model.GetPositionAt(dec.Range.StartOffset);
            if (decPos.Column <= index + 1)
            {
                string content = dec.Options.Before?.Content ?? string.Empty;
                string after = dec.Options.After?.Content ?? string.Empty;
                visible += content.Length + after.Length;
            }
        }

        return new TextPosition(lineNumber, Math.Min(index + 1, line.Length + 1));
    }
}
