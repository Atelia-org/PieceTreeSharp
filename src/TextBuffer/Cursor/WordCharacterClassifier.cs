// Source: ts/src/vs/editor/common/core/wordCharacterClassifier.ts
// - Class: WordCharacterClassifier (Lines: 20-150)
// Ported: 2025-11-22
// Updated: 2025-11-28 (WS5-PORT: Full word classification support)
// Note: Uses WordCharacterClass enum from Core namespace to avoid duplication

using System.Collections.Concurrent;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Cursor;

// Note: WordCharacterClass enum is defined in Core/SearchTypes.cs
// This classifier provides additional convenience methods for cursor operations.

/// <summary>
/// Classifier that mirrors TS WordCharacterClassifier semantics for cursor word operations.
/// Uses a provided separators string to classify characters as Regular, Whitespace, or WordSeparator.
/// Extends Core.WordCharacterClassifier with convenience methods for cursor navigation.
/// </summary>
public sealed class CursorWordCharacterClassifier
{
    private readonly HashSet<char> _wordSeparatorSet;
    private readonly string _wordSeparators;

    // Cache for classifier instances (matches TS LRU cache pattern)
    private static readonly ConcurrentDictionary<string, CursorWordCharacterClassifier> _cache = new();

    public CursorWordCharacterClassifier(string? wordSeparators)
    {
        _wordSeparators = wordSeparators ?? CursorConfiguration.DefaultWordSeparators;
        _wordSeparatorSet = new HashSet<char>(_wordSeparators);
    }

    /// <summary>
    /// Get a cached classifier for the given word separators.
    /// </summary>
    public static CursorWordCharacterClassifier GetCached(string? wordSeparators)
    {
        string key = wordSeparators ?? CursorConfiguration.DefaultWordSeparators;
        return _cache.GetOrAdd(key, k => new CursorWordCharacterClassifier(k));
    }

    /// <summary>
    /// Get the character class for a given character.
    /// </summary>
    public WordCharacterClass Get(char ch)
    {
        if (ch == ' ' || ch == '\t')
        {
            return WordCharacterClass.Whitespace;
        }

        if (_wordSeparatorSet.Contains(ch))
        {
            return WordCharacterClass.WordSeparator;
        }

        return WordCharacterClass.Regular;
    }

    /// <summary>
    /// Get the character class for a given char code.
    /// </summary>
    public WordCharacterClass Get(int charCode)
    {
        return Get((char)charCode);
    }

    /// <summary>
    /// Check if a character is a word character (Regular class).
    /// </summary>
    public bool IsWordChar(char ch)
    {
        return Get(ch) == WordCharacterClass.Regular;
    }

    /// <summary>
    /// Check if a character is a separator (Whitespace or WordSeparator).
    /// </summary>
    public bool IsSeparator(char ch)
    {
        WordCharacterClass cls = Get(ch);
        return cls == WordCharacterClass.Whitespace || cls == WordCharacterClass.WordSeparator;
    }

    /// <summary>
    /// Check if a character is whitespace.
    /// </summary>
    public bool IsWhitespace(char ch)
    {
        return Get(ch) == WordCharacterClass.Whitespace;
    }

    /// <summary>
    /// Check if a character is a word separator (not whitespace).
    /// </summary>
    public bool IsWordSeparator(char ch)
    {
        return Get(ch) == WordCharacterClass.WordSeparator;
    }

    /// <summary>
    /// The word separators string.
    /// </summary>
    public string WordSeparators => _wordSeparators;
}
