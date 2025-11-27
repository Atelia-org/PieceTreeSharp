// Source: ts/src/vs/editor/common/core/wordCharacterClassifier.ts
// - Class: WordCharacterClassifier (Lines: 20-150)
// Ported: 2025-11-22

using System;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Minimal classifier that mirrors TS WordCharacterClassifier semantics for our cursor operations.
/// It uses a provided separators string to decide whether a character is a separator.
/// </summary>
public sealed class WordCharacterClassifier
{
    private readonly string? _wordSeparators;

    public WordCharacterClassifier(string? wordSeparators)
    {
        _wordSeparators = wordSeparators;
    }

    public bool IsWordChar(char ch)
    {
        if (char.IsWhiteSpace(ch))
        {
            return false;
        }

        if (_wordSeparators is null)
        {
            return !char.IsPunctuation(ch);
        }

        return !_wordSeparators.Contains(ch);
    }

    public bool IsSeparator(char ch) => !IsWordChar(ch);
}
