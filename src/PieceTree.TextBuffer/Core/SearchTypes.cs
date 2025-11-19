using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PieceTree.TextBuffer.Core;

public readonly record struct Range(TextPosition Start, TextPosition End)
{
    public Range(int startLine, int startColumn, int endLine, int endColumn)
        : this(new TextPosition(startLine, startColumn), new TextPosition(endLine, endColumn))
    {
    }

    public int StartLineNumber => Start.LineNumber;
    public int EndLineNumber => End.LineNumber;
    public int StartColumn => Start.Column;
    public int EndColumn => End.Column;
}

public enum WordCharacterClass
{
    Regular = 0,
    Whitespace = 1,
    WordSeparator = 2,
}

public interface ITextSearchAccess
{
    int LineCount { get; }
    string EndOfLine { get; }
    string GetLineContent(int lineNumber);
    int GetLineMaxColumn(int lineNumber);
    int GetOffsetAt(TextPosition position);
    TextPosition GetPositionAt(int offset);
    string GetValueInRange(Range range, bool normalizeLineEndings, out LineFeedCounter? lineFeedCounter);
}

public sealed class SearchData
{
    public SearchData(Regex regex, WordCharacterClassifier? wordSeparators, string? simpleSearch, bool isMultiline, bool isCaseSensitive)
    {
        Regex = regex;
        WordSeparators = wordSeparators;
        SimpleSearch = simpleSearch;
        IsMultiline = isMultiline;
        IsCaseSensitive = isCaseSensitive;
    }

    public Regex Regex { get; }
    public string? SimpleSearch { get; }
    public WordCharacterClassifier? WordSeparators { get; }
    public bool IsMultiline { get; }
    public bool IsCaseSensitive { get; }
}

public sealed class FindMatch
{
    public FindMatch(Range range, string[]? matches)
    {
        Range = range;
        Matches = matches;
    }

    public Range Range { get; }
    public string[]? Matches { get; }
}

public sealed class SearchParams
{
    public SearchParams(string searchString, bool isRegex, bool matchCase, string? wordSeparators)
    {
        SearchString = searchString ?? string.Empty;
        IsRegex = isRegex;
        MatchCase = matchCase;
        WordSeparators = wordSeparators;
    }

    public string SearchString { get; }
    public bool IsRegex { get; }
    public bool MatchCase { get; }
    public string? WordSeparators { get; }

    public SearchData? ParseSearchRequest()
    {
        if (string.IsNullOrEmpty(SearchString))
        {
            return null;
        }

        var isMultiline = IsMultilinePattern(SearchString, IsRegex);
        string pattern;
        if (IsRegex)
        {
            pattern = ExpandUnicodeEscapes(SearchString);
        }
        else
        {
            pattern = Regex.Escape(SearchString);
        }

        var options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
        if (!MatchCase)
        {
            options |= RegexOptions.IgnoreCase;
        }
        if (isMultiline)
        {
            options |= RegexOptions.Multiline;
        }

        Regex regex;
        try
        {
            regex = new Regex(pattern, options);
        }
        catch (ArgumentException)
        {
            return null;
        }

        string? simpleSearch = null;
        if (!IsRegex && !isMultiline)
        {
            var hasCaseVariance = HasCaseVariance(SearchString);
            if (MatchCase || !hasCaseVariance)
            {
                simpleSearch = SearchString;
            }
        }

        var classifier = string.IsNullOrEmpty(WordSeparators) ? null : new WordCharacterClassifier(WordSeparators!);
        return new SearchData(regex, classifier, simpleSearch, isMultiline, MatchCase);
    }

    private static bool HasCaseVariance(string value)
    {
        var lower = value.ToLowerInvariant();
        var upper = value.ToUpperInvariant();
        return !string.Equals(lower, upper, StringComparison.Ordinal);
    }

    private static bool IsMultilinePattern(string searchString, bool isRegex)
    {
        if (string.IsNullOrEmpty(searchString))
        {
            return false;
        }

        if (!isRegex)
        {
            return searchString.IndexOf('\n') >= 0 || searchString.IndexOf('\r') >= 0;
        }

        for (int i = 0; i < searchString.Length; i++)
        {
            var ch = searchString[i];
            if (ch == '\n')
            {
                return true;
            }

            if (ch == '\\' && i + 1 < searchString.Length)
            {
                i++;
                var next = searchString[i];
                if (next == 'n' || next == 'r' || next == 'W')
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string ExpandUnicodeEscapes(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return pattern;
        }

        var sb = new StringBuilder(pattern.Length);
        for (int i = 0; i < pattern.Length; i++)
        {
            var ch = pattern[i];
            if (ch == '\\' && i + 2 < pattern.Length && pattern[i + 1] == 'u' && pattern[i + 2] == '{')
            {
                var end = pattern.IndexOf('}', i + 3);
                if (end > i)
                {
                    var span = pattern.AsSpan(i + 3, end - (i + 3));
                    if (int.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint))
                    {
                        sb.Append(char.ConvertFromUtf32(codePoint));
                        i = end;
                        continue;
                    }
                }
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }
}

public sealed class LineFeedCounter
{
    private readonly int[] _lfOffsets;

    public LineFeedCounter(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            _lfOffsets = Array.Empty<int>();
            return;
        }

        var list = new List<int>();
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                list.Add(i);
            }
        }

        _lfOffsets = list.Count == 0 ? Array.Empty<int>() : list.ToArray();
    }

    public int CountBefore(int offset)
    {
        if (_lfOffsets.Length == 0 || offset <= 0)
        {
            return 0;
        }

        int low = 0;
        int high = _lfOffsets.Length - 1;
        int result = -1;
        while (low <= high)
        {
            int mid = low + ((high - low) >> 1);
            if (_lfOffsets[mid] < offset)
            {
                result = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return result + 1;
    }
}

public sealed class WordCharacterClassifier
{
    private readonly Dictionary<int, WordCharacterClass> _classes = new();

    public WordCharacterClassifier(string separators)
    {
        if (!string.IsNullOrEmpty(separators))
        {
            foreach (var rune in separators.EnumerateRunes())
            {
                _classes[rune.Value] = WordCharacterClass.WordSeparator;
            }
        }

        SetClass(' ', WordCharacterClass.Whitespace);
        SetClass('\t', WordCharacterClass.Whitespace);
    }

    private void SetClass(int codePoint, WordCharacterClass @class)
    {
        _classes[codePoint] = @class;
    }

    public WordCharacterClass GetClass(int codePoint)
    {
        if (_classes.TryGetValue(codePoint, out var @class))
        {
            return @class;
        }

        if (UnicodeUtility.IsWhitespace(codePoint))
        {
            return WordCharacterClass.Whitespace;
        }

        return WordCharacterClass.Regular;
    }

    public bool IsValidMatch(string text, int matchStartIndex, int matchLength)
    {
        if (matchLength == 0)
        {
            return true;
        }

        if (!IsLeftBoundary(text, matchStartIndex))
        {
            return false;
        }

        return IsRightBoundary(text, matchStartIndex, matchLength);
    }

    private bool IsLeftBoundary(string text, int matchStartIndex)
    {
        if (matchStartIndex <= 0)
        {
            return true;
        }

        if (!UnicodeUtility.TryGetPreviousCodePoint(text, matchStartIndex, out var before, out _))
        {
            return true;
        }

        if (IsSeparatorOrLineBreak(before))
        {
            return true;
        }

        if (!UnicodeUtility.TryGetCodePointAt(text, matchStartIndex, out var current, out _))
        {
            return true;
        }

        return IsSeparatorOrLineBreak(current);
    }

    private bool IsRightBoundary(string text, int matchStartIndex, int matchLength)
    {
        var endIndex = matchStartIndex + matchLength;
        if (endIndex >= text.Length)
        {
            return true;
        }

        if (!UnicodeUtility.TryGetCodePointAt(text, endIndex, out var after, out _))
        {
            return true;
        }

        if (IsSeparatorOrLineBreak(after))
        {
            return true;
        }

        if (!UnicodeUtility.TryGetPreviousCodePoint(text, endIndex, out var lastInMatch, out _))
        {
            return true;
        }

        return IsSeparatorOrLineBreak(lastInMatch);
    }

    private bool IsSeparatorOrLineBreak(int codePoint)
    {
        return GetClass(codePoint) != WordCharacterClass.Regular || UnicodeUtility.IsLineBreak(codePoint);
    }
}

internal static class UnicodeUtility
{
    public static bool TryGetCodePointAt(string text, int index, out int codePoint, out int codeUnitLength)
    {
        codePoint = 0;
        codeUnitLength = 0;
        if ((uint)index >= (uint)text.Length)
        {
            return false;
        }

        var ch = text[index];
        if (char.IsHighSurrogate(ch) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            codePoint = char.ConvertToUtf32(ch, text[index + 1]);
            codeUnitLength = 2;
            return true;
        }

        codePoint = ch;
        codeUnitLength = 1;
        return true;
    }

    public static bool TryGetPreviousCodePoint(string text, int index, out int codePoint, out int codeUnitLength)
    {
        codePoint = 0;
        codeUnitLength = 0;
        if (index <= 0 || index > text.Length)
        {
            return false;
        }

        index--;
        var ch = text[index];
        if (char.IsLowSurrogate(ch) && index - 1 >= 0 && char.IsHighSurrogate(text[index - 1]))
        {
            codePoint = char.ConvertToUtf32(text[index - 1], ch);
            codeUnitLength = 2;
            return true;
        }

        codePoint = ch;
        codeUnitLength = 1;
        return true;
    }

    public static int AdvanceByCodePoint(string text, int index)
    {
        if ((uint)index >= (uint)text.Length)
        {
            return text.Length;
        }

        var ch = text[index];
        if (char.IsHighSurrogate(ch) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            return index + 2;
        }

        return index + 1;
    }

    public static bool IsWhitespace(int codePoint)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(codePoint), 0);
        return category == UnicodeCategory.SpaceSeparator
            || category == UnicodeCategory.LineSeparator
            || category == UnicodeCategory.ParagraphSeparator;
    }

    public static bool IsLineBreak(int codePoint)
    {
        return codePoint == '\n' || codePoint == '\r';
    }
}
