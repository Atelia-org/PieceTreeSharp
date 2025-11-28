// Source: ts/src/vs/editor/common/cursor/cursorWordOperations.ts
// - Class: WordOperations (Lines: 50-800)
// Ported: 2025-11-22
// Updated: 2025-11-28 (WS5-PORT: Full word operations for cursor movement)

using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Cursor;

public enum WordNavigationType
{
    WordStart = 0,      // Move to word start
    WordStartFast = 1,  // Move to word start, skipping single-char separators (Ctrl+Left)
    WordEnd = 2,        // Move to word end
    WordAccessibility = 3,  // Accessibility mode - skip separators
}

/// <summary>
/// Word type enum matching TS WordType.
/// </summary>
internal enum WordType
{
    None = 0,
    Regular = 1,
    Separator = 2
}

/// <summary>
/// Result of finding a word on a line.
/// </summary>
internal readonly struct FindWordResult
{
    public readonly int Start;
    public readonly int End;
    public readonly WordType WordType;
    public readonly WordCharacterClass NextCharClass;

    public FindWordResult(int start, int end, WordType wordType, WordCharacterClass nextCharClass)
    {
        Start = start;
        End = end;
        WordType = wordType;
        NextCharClass = nextCharClass;
    }

    public static FindWordResult Empty => new(-1, -1, WordType.None, WordCharacterClass.Regular);

    public bool IsValid => Start >= 0;
}

public static class WordOperations
{
    #region Core Word Finding Methods

    /// <summary>
    /// Create a word result.
    /// </summary>
    private static FindWordResult CreateWord(string lineContent, WordType wordType, WordCharacterClass nextCharClass, int start, int end)
    {
        return new FindWordResult(start, end, wordType, nextCharClass);
    }

    /// <summary>
    /// Find the end of a word starting from startIndex.
    /// </summary>
    private static int FindEndOfWord(string lineContent, CursorWordCharacterClassifier classifier, WordType wordType, int startIndex)
    {
        int len = lineContent.Length;
        for (int chIndex = startIndex; chIndex < len; chIndex++)
        {
            WordCharacterClass chClass = classifier.Get(lineContent[chIndex]);

            if (chClass == WordCharacterClass.Whitespace)
            {
                return chIndex;
            }
            if (wordType == WordType.Regular && chClass == WordCharacterClass.WordSeparator)
            {
                return chIndex;
            }
            if (wordType == WordType.Separator && chClass == WordCharacterClass.Regular)
            {
                return chIndex;
            }
        }
        return len;
    }

    /// <summary>
    /// Find the start of a word ending at startIndex.
    /// </summary>
    private static int FindStartOfWord(string lineContent, CursorWordCharacterClassifier classifier, WordType wordType, int startIndex)
    {
        for (int chIndex = startIndex; chIndex >= 0; chIndex--)
        {
            WordCharacterClass chClass = classifier.Get(lineContent[chIndex]);

            if (chClass == WordCharacterClass.Whitespace)
            {
                return chIndex + 1;
            }
            if (wordType == WordType.Regular && chClass == WordCharacterClass.WordSeparator)
            {
                return chIndex + 1;
            }
            if (wordType == WordType.Separator && chClass == WordCharacterClass.Regular)
            {
                return chIndex + 1;
            }
        }
        return 0;
    }

    /// <summary>
    /// Find the previous word on the line before the given column.
    /// </summary>
    internal static FindWordResult FindPreviousWordOnLine(string lineContent, CursorWordCharacterClassifier classifier, int column)
    {
        WordType wordType = WordType.None;
        int len = lineContent.Length;
        
        // Clamp starting index to valid range: column is 1-based, so column-2 is the 0-based index before column
        int startIndex = Math.Min(column - 2, len - 1);

        for (int chIndex = startIndex; chIndex >= 0; chIndex--)
        {
            WordCharacterClass chClass = classifier.Get(lineContent[chIndex]);

            if (chClass == WordCharacterClass.Regular)
            {
                if (wordType == WordType.Separator)
                {
                    return CreateWord(lineContent, wordType, chClass, chIndex + 1, FindEndOfWord(lineContent, classifier, wordType, chIndex + 1));
                }
                wordType = WordType.Regular;
            }
            else if (chClass == WordCharacterClass.WordSeparator)
            {
                if (wordType == WordType.Regular)
                {
                    return CreateWord(lineContent, wordType, chClass, chIndex + 1, FindEndOfWord(lineContent, classifier, wordType, chIndex + 1));
                }
                wordType = WordType.Separator;
            }
            else if (chClass == WordCharacterClass.Whitespace)
            {
                if (wordType != WordType.None)
                {
                    return CreateWord(lineContent, wordType, chClass, chIndex + 1, FindEndOfWord(lineContent, classifier, wordType, chIndex + 1));
                }
            }
        }

        if (wordType != WordType.None)
        {
            return CreateWord(lineContent, wordType, WordCharacterClass.Whitespace, 0, FindEndOfWord(lineContent, classifier, wordType, 0));
        }

        return FindWordResult.Empty;
    }

    /// <summary>
    /// Find the next word on the line at or after the given column.
    /// </summary>
    internal static FindWordResult FindNextWordOnLine(string lineContent, CursorWordCharacterClassifier classifier, int column)
    {
        WordType wordType = WordType.None;
        int len = lineContent.Length;

        if (len == 0)
        {
            return FindWordResult.Empty;
        }

        // Column is 1-based; column-1 is the 0-based index at the cursor location
        int startIndex = column - 1;
        if (startIndex < 0)
        {
            startIndex = 0;
        }
        if (startIndex >= len)
        {
            return FindWordResult.Empty;
        }

        for (int chIndex = startIndex; chIndex < len; chIndex++)
        {
            WordCharacterClass chClass = classifier.Get(lineContent[chIndex]);

            if (chClass == WordCharacterClass.Regular)
            {
                if (wordType == WordType.Separator)
                {
                    return CreateWord(lineContent, wordType, chClass, FindStartOfWord(lineContent, classifier, wordType, chIndex - 1), chIndex);
                }
                wordType = WordType.Regular;
            }
            else if (chClass == WordCharacterClass.WordSeparator)
            {
                if (wordType == WordType.Regular)
                {
                    return CreateWord(lineContent, wordType, chClass, FindStartOfWord(lineContent, classifier, wordType, chIndex - 1), chIndex);
                }
                wordType = WordType.Separator;
            }
            else if (chClass == WordCharacterClass.Whitespace)
            {
                if (wordType != WordType.None)
                {
                    return CreateWord(lineContent, wordType, chClass, FindStartOfWord(lineContent, classifier, wordType, chIndex - 1), chIndex);
                }
            }
        }

        if (wordType != WordType.None)
        {
            return CreateWord(lineContent, wordType, WordCharacterClass.Whitespace, FindStartOfWord(lineContent, classifier, wordType, len - 1), len);
        }

        return FindWordResult.Empty;
    }

    #endregion

    #region Word Left Movement

    /// <summary>
    /// Move left by a word according to the classifier.
    /// Core implementation matching TS moveWordLeft.
    /// </summary>
    public static TextPosition MoveWordLeft(TextModel model, TextPosition position, string? wordSeparators, WordNavigationType navigationType = WordNavigationType.WordStartFast, bool hasMulticursor = false)
    {
        CursorWordCharacterClassifier classifier = CursorWordCharacterClassifier.GetCached(wordSeparators);
        return MoveWordLeftCore(classifier, model, position, navigationType, hasMulticursor);
    }

    /// <summary>
    /// Move left by a word according to the classifier (for ICursorSimpleModel).
    /// </summary>
    public static TextPosition MoveWordLeft(CursorWordCharacterClassifier classifier, ICursorSimpleModel model, TextPosition position, WordNavigationType navigationType, bool hasMulticursor = false)
    {
        return MoveWordLeftCore(classifier, model, position, navigationType, hasMulticursor);
    }

    /// <summary>
    /// Core implementation for move word left.
    /// </summary>
    private static TextPosition MoveWordLeftCore(CursorWordCharacterClassifier classifier, object model, TextPosition position, WordNavigationType navigationType, bool hasMulticursor)
    {
        int lineNumber = position.LineNumber;
        int column = position.Column;

        Func<int, string> getLineContent = model switch
        {
            TextModel tm => ln => tm.GetLineContent(ln),
            ICursorSimpleModel cm => ln => cm.GetLineContent(ln),
            _ => throw new ArgumentException("Model must be TextModel or ICursorSimpleModel")
        };

        Func<int, int> getLineMaxColumn = model switch
        {
            TextModel tm => ln => tm.GetLineMaxColumn(ln),
            ICursorSimpleModel cm => ln => cm.GetLineMaxColumn(ln),
            _ => throw new ArgumentException("Model must be TextModel or ICursorSimpleModel")
        };

        int getLineCount() => model switch
        {
            TextModel tm => tm.GetLineCount(),
            ICursorSimpleModel cm => cm.GetLineCount(),
            _ => throw new ArgumentException("Model must be TextModel or ICursorSimpleModel")
        };

        // Normalize position to valid range
        int totalLines = getLineCount();
        if (lineNumber > totalLines)
        {
            lineNumber = totalLines;
            column = getLineMaxColumn(lineNumber);
        }
        else if (lineNumber < 1)
        {
            return new TextPosition(1, 1);
        }

        int maxCol = getLineMaxColumn(lineNumber);
        if (column > maxCol)
        {
            column = maxCol;
        }
        else if (column < 1)
        {
            column = 1;
        }

        if (column == 1)
        {
            if (lineNumber > 1)
            {
                lineNumber--;
                column = getLineMaxColumn(lineNumber);
            }
        }

        string lineContent = getLineContent(lineNumber);
        FindWordResult prevWordOnLine = FindPreviousWordOnLine(lineContent, classifier, column);

        if (navigationType == WordNavigationType.WordStart)
        {
            return new TextPosition(lineNumber, prevWordOnLine.IsValid ? prevWordOnLine.Start + 1 : 1);
        }

        if (navigationType == WordNavigationType.WordStartFast)
        {
            if (!hasMulticursor // avoid having multiple cursors stop at different locations
                && prevWordOnLine.IsValid
                && prevWordOnLine.WordType == WordType.Separator
                && prevWordOnLine.End - prevWordOnLine.Start == 1
                && prevWordOnLine.NextCharClass == WordCharacterClass.Regular)
            {
                // Skip over a word made up of one single separator and followed by a regular character
                prevWordOnLine = FindPreviousWordOnLine(lineContent, classifier, prevWordOnLine.Start + 1);
            }

            return new TextPosition(lineNumber, prevWordOnLine.IsValid ? prevWordOnLine.Start + 1 : 1);
        }

        if (navigationType == WordNavigationType.WordAccessibility)
        {
            while (prevWordOnLine.IsValid && prevWordOnLine.WordType == WordType.Separator)
            {
                // Skip over words made up of only separators
                prevWordOnLine = FindPreviousWordOnLine(lineContent, classifier, prevWordOnLine.Start + 1);
            }

            return new TextPosition(lineNumber, prevWordOnLine.IsValid ? prevWordOnLine.Start + 1 : 1);
        }

        // WordEnd navigation - stop at word ends
        if (prevWordOnLine.IsValid && column <= prevWordOnLine.End + 1)
        {
            prevWordOnLine = FindPreviousWordOnLine(lineContent, classifier, prevWordOnLine.Start + 1);
        }

        return new TextPosition(lineNumber, prevWordOnLine.IsValid ? prevWordOnLine.End + 1 : 1);
    }

    /// <summary>
    /// CursorWordLeft - move cursor to previous word boundary (Ctrl+Left behavior).
    /// </summary>
    public static TextPosition CursorWordLeft(TextModel model, TextPosition position, string? wordSeparators, bool hasMulticursor = false)
    {
        return MoveWordLeft(model, position, wordSeparators, WordNavigationType.WordStartFast, hasMulticursor);
    }

    /// <summary>
    /// CursorWordStartLeft - move cursor to start of previous word (VS-style).
    /// </summary>
    public static TextPosition CursorWordStartLeft(TextModel model, TextPosition position, string? wordSeparators)
    {
        return MoveWordLeft(model, position, wordSeparators, WordNavigationType.WordStart, false);
    }

    /// <summary>
    /// CursorWordEndLeft - move cursor to end of previous word.
    /// </summary>
    public static TextPosition CursorWordEndLeft(TextModel model, TextPosition position, string? wordSeparators)
    {
        return MoveWordLeft(model, position, wordSeparators, WordNavigationType.WordEnd, false);
    }

    #endregion

    #region Word Right Movement

    /// <summary>
    /// Move right by a word according to the classifier.
    /// Core implementation matching TS moveWordRight.
    /// </summary>
    public static TextPosition MoveWordRight(TextModel model, TextPosition position, string? wordSeparators, WordNavigationType navigationType = WordNavigationType.WordEnd)
    {
        CursorWordCharacterClassifier classifier = CursorWordCharacterClassifier.GetCached(wordSeparators);
        return MoveWordRightCore(classifier, model, position, navigationType);
    }

    /// <summary>
    /// Move right by a word according to the classifier (for ICursorSimpleModel).
    /// </summary>
    public static TextPosition MoveWordRight(CursorWordCharacterClassifier classifier, ICursorSimpleModel model, TextPosition position, WordNavigationType navigationType)
    {
        return MoveWordRightCore(classifier, model, position, navigationType);
    }

    /// <summary>
    /// Core implementation for move word right.
    /// </summary>
    private static TextPosition MoveWordRightCore(CursorWordCharacterClassifier classifier, object model, TextPosition position, WordNavigationType navigationType)
    {
        int lineNumber = position.LineNumber;
        int column = position.Column;

        Func<int, string> getLineContent = model switch
        {
            TextModel tm => ln => tm.GetLineContent(ln),
            ICursorSimpleModel cm => ln => cm.GetLineContent(ln),
            _ => throw new ArgumentException("Model must be TextModel or ICursorSimpleModel")
        };

        Func<int, int> getLineMaxColumn = model switch
        {
            TextModel tm => ln => tm.GetLineMaxColumn(ln),
            ICursorSimpleModel cm => ln => cm.GetLineMaxColumn(ln),
            _ => throw new ArgumentException("Model must be TextModel or ICursorSimpleModel")
        };

        Func<int> getLineCount = model switch
        {
            TextModel tm => () => tm.GetLineCount(),
            ICursorSimpleModel cm => () => cm.GetLineCount(),
            _ => throw new ArgumentException("Model must be TextModel or ICursorSimpleModel")
        };

        // Normalize position to valid range
        int totalLines = getLineCount();
        if (lineNumber > totalLines)
        {
            lineNumber = totalLines;
            column = getLineMaxColumn(lineNumber);
        }
        else if (lineNumber < 1)
        {
            lineNumber = 1;
            column = 1;
        }

        int maxCol = getLineMaxColumn(lineNumber);
        if (column > maxCol)
        {
            column = maxCol;
        }
        else if (column < 1)
        {
            column = 1;
        }

        bool movedDown = false;
        if (column == getLineMaxColumn(lineNumber))
        {
            if (lineNumber < getLineCount())
            {
                movedDown = true;
                lineNumber++;
                column = 1;
            }
        }

        string lineContent = getLineContent(lineNumber);
        FindWordResult nextWordOnLine = FindNextWordOnLine(lineContent, classifier, column);

        if (navigationType == WordNavigationType.WordEnd)
        {
            if (nextWordOnLine.IsValid && nextWordOnLine.WordType == WordType.Separator)
            {
                if (nextWordOnLine.End - nextWordOnLine.Start == 1 && nextWordOnLine.NextCharClass == WordCharacterClass.Regular)
                {
                    // Skip over a word made up of one single separator and followed by a regular character
                    nextWordOnLine = FindNextWordOnLine(lineContent, classifier, nextWordOnLine.End + 1);
                }
            }
            if (nextWordOnLine.IsValid)
            {
                column = nextWordOnLine.End + 1;
            }
            else
            {
                column = getLineMaxColumn(lineNumber);
            }
        }
        else if (navigationType == WordNavigationType.WordAccessibility)
        {
            if (movedDown)
            {
                column = 0;
            }

            while (nextWordOnLine.IsValid
                && (nextWordOnLine.WordType == WordType.Separator
                    || nextWordOnLine.Start + 1 <= column))
            {
                nextWordOnLine = FindNextWordOnLine(lineContent, classifier, nextWordOnLine.End + 1);
            }

            if (nextWordOnLine.IsValid)
            {
                column = nextWordOnLine.Start + 1;
            }
            else
            {
                column = getLineMaxColumn(lineNumber);
            }
        }
        else // WordStart or WordStartFast
        {
            if (nextWordOnLine.IsValid && !movedDown && column >= nextWordOnLine.Start + 1)
            {
                nextWordOnLine = FindNextWordOnLine(lineContent, classifier, nextWordOnLine.End + 1);
            }
            if (nextWordOnLine.IsValid)
            {
                column = nextWordOnLine.Start + 1;
            }
            else
            {
                column = getLineMaxColumn(lineNumber);
            }
        }

        return new TextPosition(lineNumber, column);
    }

    /// <summary>
    /// CursorWordRight - move cursor to next word boundary (Ctrl+Right behavior).
    /// </summary>
    public static TextPosition CursorWordRight(TextModel model, TextPosition position, string? wordSeparators)
    {
        return MoveWordRight(model, position, wordSeparators, WordNavigationType.WordEnd);
    }

    /// <summary>
    /// CursorWordStartRight - move cursor to start of next word (VS-style).
    /// </summary>
    public static TextPosition CursorWordStartRight(TextModel model, TextPosition position, string? wordSeparators)
    {
        return MoveWordRight(model, position, wordSeparators, WordNavigationType.WordStart);
    }

    /// <summary>
    /// CursorWordEndRight - move cursor to end of next word.
    /// </summary>
    public static TextPosition CursorWordEndRight(TextModel model, TextPosition position, string? wordSeparators)
    {
        return MoveWordRight(model, position, wordSeparators, WordNavigationType.WordEnd);
    }

    #endregion

    #region Selection Operations

    public static Selection SelectWordRight(TextModel model, Selection current, string? wordSeparators)
    {
        TextPosition newActive = MoveWordRight(model, current.Active, wordSeparators, WordNavigationType.WordEnd);
        return new Selection(current.Anchor, newActive);
    }

    public static Selection SelectWordLeft(TextModel model, Selection current, string? wordSeparators)
    {
        TextPosition newActive = MoveWordLeft(model, current.Active, wordSeparators, WordNavigationType.WordStartFast, false);
        return new Selection(current.Anchor, newActive);
    }

    #endregion

    #region Delete Word Operations

    /// <summary>
    /// Find last non-whitespace character before startIndex.
    /// </summary>
    private static int LastNonWhitespaceIndex(string str, int startIndex)
    {
        for (int i = startIndex; i >= 0; i--)
        {
            char ch = str[i];
            if (ch != ' ' && ch != '\t')
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Find first non-whitespace character at or after startIndex.
    /// </summary>
    private static int FirstNonWhitespaceIndex(string str, int startIndex)
    {
        for (int i = startIndex; i < str.Length; i++)
        {
            char ch = str[i];
            if (ch != ' ' && ch != '\t')
            {
                return i;
            }
        }
        return str.Length;
    }

    /// <summary>
    /// Delete word left whitespace (helper for deleteWordLeft).
    /// </summary>
    private static Range? DeleteWordLeftWhitespace(TextModel model, TextPosition position)
    {
        string lineContent = model.GetLineContent(position.LineNumber);
        int startIndex = position.Column - 2;
        if (startIndex < 0)
        {
            return null;
        }

        int lastNonWhitespace = LastNonWhitespaceIndex(lineContent, startIndex);
        if (lastNonWhitespace + 1 < startIndex)
        {
            return new Range(position.LineNumber, lastNonWhitespace + 2, position.LineNumber, position.Column);
        }
        return null;
    }

    /// <summary>
    /// Delete word right whitespace (helper for deleteWordRight).
    /// </summary>
    private static Range? DeleteWordRightWhitespace(TextModel model, TextPosition position)
    {
        string lineContent = model.GetLineContent(position.LineNumber);
        int startIndex = position.Column - 1;
        int firstNonWhitespace = FirstNonWhitespaceIndex(lineContent, startIndex);
        if (startIndex + 1 < firstNonWhitespace)
        {
            return new Range(position.LineNumber, position.Column, position.LineNumber, firstNonWhitespace + 1);
        }
        return null;
    }

    /// <summary>
    /// DeleteWordLeft - delete the word to the left of the cursor.
    /// Returns the range to delete.
    /// </summary>
    public static Range DeleteWordLeft(TextModel model, TextPosition position, string? wordSeparators, WordNavigationType navigationType = WordNavigationType.WordStart, bool whitespaceHeuristics = true)
    {
        if (position.LineNumber == 1 && position.Column == 1)
        {
            // Ignore deleting at beginning of file
            return new Range(1, 1, 1, 1);
        }

        if (whitespaceHeuristics)
        {
            Range? r = DeleteWordLeftWhitespace(model, position);
            if (r.HasValue)
            {
                return r.Value;
            }
        }

        CursorWordCharacterClassifier classifier = CursorWordCharacterClassifier.GetCached(wordSeparators);
        int lineNumber = position.LineNumber;
        int column = position.Column;

        string lineContent = model.GetLineContent(lineNumber);
        FindWordResult prevWordOnLine = FindPreviousWordOnLine(lineContent, classifier, column);

        if (navigationType == WordNavigationType.WordStart)
        {
            if (prevWordOnLine.IsValid)
            {
                column = prevWordOnLine.Start + 1;
            }
            else
            {
                if (column > 1)
                {
                    column = 1;
                }
                else
                {
                    lineNumber--;
                    column = model.GetLineMaxColumn(lineNumber);
                }
            }
        }
        else
        {
            if (prevWordOnLine.IsValid && column <= prevWordOnLine.End + 1)
            {
                prevWordOnLine = FindPreviousWordOnLine(lineContent, classifier, prevWordOnLine.Start + 1);
            }
            if (prevWordOnLine.IsValid)
            {
                column = prevWordOnLine.End + 1;
            }
            else
            {
                if (column > 1)
                {
                    column = 1;
                }
                else
                {
                    lineNumber--;
                    column = model.GetLineMaxColumn(lineNumber);
                }
            }
        }

        return new Range(lineNumber, column, position.LineNumber, position.Column);
    }

    /// <summary>
    /// DeleteWordRight - delete the word to the right of the cursor.
    /// Returns the range to delete.
    /// </summary>
    public static Range DeleteWordRight(TextModel model, TextPosition position, string? wordSeparators, WordNavigationType navigationType = WordNavigationType.WordEnd, bool whitespaceHeuristics = true)
    {
        int lineCount = model.GetLineCount();
        int maxColumn = model.GetLineMaxColumn(position.LineNumber);
        if (position.LineNumber == lineCount && position.Column == maxColumn)
        {
            // Ignore deleting at end of file
            return new Range(position.LineNumber, position.Column, position.LineNumber, position.Column);
        }

        if (whitespaceHeuristics)
        {
            Range? r = DeleteWordRightWhitespace(model, position);
            if (r.HasValue)
            {
                return r.Value;
            }
        }

        CursorWordCharacterClassifier classifier = CursorWordCharacterClassifier.GetCached(wordSeparators);
        int lineNumber = position.LineNumber;
        int column = position.Column;

        string lineContent = model.GetLineContent(lineNumber);
        FindWordResult nextWordOnLine = FindNextWordOnLine(lineContent, classifier, column);

        if (navigationType == WordNavigationType.WordEnd)
        {
            if (nextWordOnLine.IsValid)
            {
                column = nextWordOnLine.End + 1;
            }
            else
            {
                if (column < maxColumn || lineNumber == lineCount)
                {
                    column = maxColumn;
                }
                else
                {
                    lineNumber++;
                    nextWordOnLine = FindNextWordOnLine(model.GetLineContent(lineNumber), classifier, 1);
                    if (nextWordOnLine.IsValid)
                    {
                        column = nextWordOnLine.Start + 1;
                    }
                    else
                    {
                        column = model.GetLineMaxColumn(lineNumber);
                    }
                }
            }
        }
        else
        {
            if (nextWordOnLine.IsValid && column >= nextWordOnLine.Start + 1)
            {
                nextWordOnLine = FindNextWordOnLine(lineContent, classifier, nextWordOnLine.End + 1);
            }
            if (nextWordOnLine.IsValid)
            {
                column = nextWordOnLine.Start + 1;
            }
            else
            {
                if (column < maxColumn || lineNumber == lineCount)
                {
                    column = maxColumn;
                }
                else
                {
                    lineNumber++;
                    nextWordOnLine = FindNextWordOnLine(model.GetLineContent(lineNumber), classifier, 1);
                    if (nextWordOnLine.IsValid)
                    {
                        column = nextWordOnLine.Start + 1;
                    }
                    else
                    {
                        column = model.GetLineMaxColumn(lineNumber);
                    }
                }
            }
        }

        return new Range(position.LineNumber, position.Column, lineNumber, column);
    }

    /// <summary>
    /// Check if character at index is whitespace (space or tab).
    /// </summary>
    private static bool CharAtIsWhitespace(string str, int index)
    {
        if (index < 0 || index >= str.Length)
        {
            return false;
        }
        char ch = str[index];
        return ch == ' ' || ch == '\t';
    }

    /// <summary>
    /// Delete whitespace around position (for DeleteInsideWord).
    /// </summary>
    private static Range? DeleteInsideWordWhitespace(TextModel model, TextPosition position)
    {
        string lineContent = model.GetLineContent(position.LineNumber);
        int lineContentLength = lineContent.Length;

        if (lineContentLength == 0)
        {
            return null;
        }

        int leftIndex = Math.Max(position.Column - 2, 0);
        if (!CharAtIsWhitespace(lineContent, leftIndex))
        {
            return null;
        }

        int rightIndex = Math.Min(position.Column - 1, lineContentLength - 1);
        if (!CharAtIsWhitespace(lineContent, rightIndex))
        {
            return null;
        }

        // Walk over whitespace to the left
        while (leftIndex > 0 && CharAtIsWhitespace(lineContent, leftIndex - 1))
        {
            leftIndex--;
        }

        // Walk over whitespace to the right
        while (rightIndex + 1 < lineContentLength && CharAtIsWhitespace(lineContent, rightIndex + 1))
        {
            rightIndex++;
        }

        return new Range(position.LineNumber, leftIndex + 1, position.LineNumber, rightIndex + 2);
    }

    /// <summary>
    /// Determine the delete range for DeleteInsideWord.
    /// </summary>
    private static Range DeleteInsideWordDetermineDeleteRange(TextModel model, TextPosition position, string? wordSeparators)
    {
        string lineContent = model.GetLineContent(position.LineNumber);
        int lineLength = lineContent.Length;

        if (lineLength == 0)
        {
            // Empty line
            if (position.LineNumber > 1)
            {
                return new Range(position.LineNumber - 1, model.GetLineMaxColumn(position.LineNumber - 1), position.LineNumber, 1);
            }
            else
            {
                if (position.LineNumber < model.GetLineCount())
                {
                    return new Range(position.LineNumber, 1, position.LineNumber + 1, 1);
                }
                else
                {
                    // Empty model
                    return new Range(position.LineNumber, 1, position.LineNumber, 1);
                }
            }
        }

        CursorWordCharacterClassifier classifier = CursorWordCharacterClassifier.GetCached(wordSeparators);

        bool TouchesWord(FindWordResult word) =>
            word.IsValid && word.Start + 1 <= position.Column && position.Column <= word.End + 1;

        Range CreateRangeWithPosition(int startColumn, int endColumn)
        {
            startColumn = Math.Min(startColumn, position.Column);
            endColumn = Math.Max(endColumn, position.Column);
            return new Range(position.LineNumber, startColumn, position.LineNumber, endColumn);
        }

        Range DeleteWordAndAdjacentWhitespace(FindWordResult word)
        {
            int startColumn = word.Start + 1;
            int endColumn = word.End + 1;
            bool expandedToTheRight = false;
            while (endColumn - 1 < lineLength && CharAtIsWhitespace(lineContent, endColumn - 1))
            {
                expandedToTheRight = true;
                endColumn++;
            }
            if (!expandedToTheRight)
            {
                while (startColumn > 1 && CharAtIsWhitespace(lineContent, startColumn - 2))
                {
                    startColumn--;
                }
            }
            return CreateRangeWithPosition(startColumn, endColumn);
        }

        FindWordResult prevWordOnLine = FindPreviousWordOnLine(lineContent, classifier, position.Column);
        if (TouchesWord(prevWordOnLine))
        {
            return DeleteWordAndAdjacentWhitespace(prevWordOnLine);
        }

        FindWordResult nextWordOnLine = FindNextWordOnLine(lineContent, classifier, position.Column);
        if (TouchesWord(nextWordOnLine))
        {
            return DeleteWordAndAdjacentWhitespace(nextWordOnLine);
        }

        if (prevWordOnLine.IsValid && nextWordOnLine.IsValid)
        {
            return CreateRangeWithPosition(prevWordOnLine.End + 1, nextWordOnLine.Start + 1);
        }
        if (prevWordOnLine.IsValid)
        {
            return CreateRangeWithPosition(prevWordOnLine.Start + 1, prevWordOnLine.End + 1);
        }
        if (nextWordOnLine.IsValid)
        {
            return CreateRangeWithPosition(nextWordOnLine.Start + 1, nextWordOnLine.End + 1);
        }

        return CreateRangeWithPosition(1, lineLength + 1);
    }

    /// <summary>
    /// DeleteInsideWord - delete the word the cursor is inside.
    /// Returns the range to delete.
    /// </summary>
    public static Range DeleteInsideWord(TextModel model, TextPosition position, string? wordSeparators)
    {
        Range? r = DeleteInsideWordWhitespace(model, position);
        if (r.HasValue)
        {
            return r.Value;
        }

        return DeleteInsideWordDetermineDeleteRange(model, position, wordSeparators);
    }

    /// <summary>
    /// Legacy DeleteWordLeft for backward compatibility.
    /// Returns a Selection representing the delete range.
    /// </summary>
    public static Selection DeleteWordLeft(TextModel model, Selection current, string? wordSeparators)
    {
        Range range = DeleteWordLeft(model, current.Active, wordSeparators);
        return new Selection(new TextPosition(range.StartLineNumber, range.StartColumn), new TextPosition(range.EndLineNumber, range.EndColumn));
    }

    #endregion
}
