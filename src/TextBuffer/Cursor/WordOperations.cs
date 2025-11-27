// Source: ts/src/vs/editor/common/cursor/cursorWordOperations.ts
// - Class: WordOperations (Lines: 50-800)
// Ported: 2025-11-22

using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Cursor;

public enum WordNavigationType
{
    Word, // Move by word
    WordPart, // Move by word-part (camelCase separated)
}

public static class WordOperations
{
    /// <summary>Move right by a word according to the classifier.</summary>
    public static TextPosition MoveWordRight(TextModel model, TextPosition from, string? wordSeparators)
    {
        WordCharacterClassifier classifier = new(wordSeparators);
        string content = model.GetLineContent(from.LineNumber);
        int index = Math.Clamp(from.Column - 1, 0, content.Length);

        if (index >= content.Length)
        {
            // Move to next line start, if any
            if (from.LineNumber < model.GetLineCount())
            {
                return new TextPosition(from.LineNumber + 1, 1);
            }
            return new TextPosition(from.LineNumber, model.GetLineMaxColumn(from.LineNumber));
        }

        // If currently on separator, skip separators
        while (index < content.Length && classifier.IsSeparator(content[index]))
        {
            index++;
        }

        // Now skip word characters
        if (index < content.Length && classifier.IsWordChar(content[index]))
        {
            while (index < content.Length && classifier.IsWordChar(content[index]))
            {
                index++;
            }
        }

        // If we reached the end of line, move to next line start
        if (index >= content.Length)
        {
            if (from.LineNumber < model.GetLineCount())
            {
                return new TextPosition(from.LineNumber + 1, 1);
            }
            return new TextPosition(from.LineNumber, content.Length + 1);
        }

        return new TextPosition(from.LineNumber, index + 1);
    }

    public static TextPosition MoveWordLeft(TextModel model, TextPosition from, string? wordSeparators)
    {
        WordCharacterClassifier classifier = new(wordSeparators);
        string content = model.GetLineContent(from.LineNumber);
        int index = Math.Clamp(from.Column - 1, 0, content.Length);

        if (index == 0)
        {
            if (from.LineNumber > 1)
            {
                string prevLine = model.GetLineContent(from.LineNumber - 1);
                return new TextPosition(from.LineNumber - 1, prevLine.Length + 1);
            }
            return new TextPosition(1, 1);
        }

        // If just at the end of a word char, move left past separators first
        // If at non-word char (separator), skip separators left
        if (index > 0)
        {
            // index is column-1
            int i = index - 1;
            // skip separators left
            while (i >= 0 && classifier.IsSeparator(content[i]))
            {
                i--;
            }
            // skip word chars left
            while (i >= 0 && classifier.IsWordChar(content[i]))
            {
                i--;
            }
            return new TextPosition(from.LineNumber, Math.Max(1, i + 2));
        }

        return new TextPosition(from.LineNumber, 1);
    }

    public static Selection SelectWordRight(TextModel model, Selection current, string? wordSeparators)
    {
        TextPosition newActive = MoveWordRight(model, current.Active, wordSeparators);
        return new Selection(current.Anchor, newActive);
    }

    public static Selection SelectWordLeft(TextModel model, Selection current, string? wordSeparators)
    {
        TextPosition newActive = MoveWordLeft(model, current.Active, wordSeparators);
        return new Selection(current.Anchor, newActive);
    }

    public static Selection DeleteWordLeft(TextModel model, Selection current, string? wordSeparators)
    {
        // delete from newPosition to current.Active
        TextPosition newActive = MoveWordLeft(model, current.Active, wordSeparators);
        return new Selection(newActive, current.Active);
    }
}
