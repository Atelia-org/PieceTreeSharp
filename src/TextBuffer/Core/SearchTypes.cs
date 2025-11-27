// Source: ts/src/vs/editor/common/model/textModelSearch.ts
// - Classes: SearchParams, SearchData, FindMatch
// - Lines: 1-200
// Source: ts/src/vs/editor/common/core/wordCharacterClassifier.ts
// - Class: WordCharacterClassifier
// - Lines: 1-100
// Ported: 2025-11-19

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PieceTree.TextBuffer;

namespace PieceTree.TextBuffer.Core;

public readonly partial record struct Range
{
    public TextPosition Start { get; init; }
    public TextPosition End { get; init; }

    public Range(TextPosition start, TextPosition end)
    {
        if (start.CompareTo(end) <= 0)
        {
            Start = start;
            End = end;
        }
        else
        {
            Start = end;
            End = start;
        }
    }

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

/// <summary>
/// Mirrors VS Code's <c>SearchParams</c> while forcing .NET's regex engine into
/// ECMAScript + culture-invariant mode. This approximates the JavaScript
/// implementation (unicode/global flags plus surrogate-safe '.' rewrites), but
/// case-insensitive matches still follow .NET's invariant ASCII folding instead
/// of V8/ICU's full Unicode tables.
/// </summary>
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

    private const string UnicodeWildcardPattern = @"(?:[\uD800-\uDBFF][\uDC00-\uDFFF]|[^\u000A\u000D\u2028\u2029])";

    public SearchData? ParseSearchRequest()
    {
        if (string.IsNullOrEmpty(SearchString))
        {
            return null;
        }

        bool isMultiline = IsMultilinePattern(SearchString, IsRegex);
        string pattern;
        if (IsRegex)
        {
            string expanded = ExpandUnicodeEscapes(SearchString);
            pattern = ApplyUnicodeWildcardCompatibility(expanded);
        }
        else
        {
            pattern = Regex.Escape(SearchString);
        }

        RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ECMAScript;
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
            bool hasCaseVariance = HasCaseVariance(SearchString);
            if (MatchCase || !hasCaseVariance)
            {
                simpleSearch = SearchString;
            }
        }

        // TS Parity: use cached WordCharacterClassifier (getMapForWordSeparators) – we implement a 10-entry LRU
        WordCharacterClassifier? classifier = string.IsNullOrEmpty(WordSeparators) ? null : WordCharacterClassifierCache.Get(WordSeparators!);
        return new SearchData(regex, classifier, simpleSearch, isMultiline, MatchCase);
    }

    private static string ApplyUnicodeWildcardCompatibility(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.IndexOf('.') < 0)
        {
            return pattern;
        }

        StringBuilder builder = new(pattern.Length);
        bool inCharClass = false;
        bool escaping = false;
        for (int i = 0; i < pattern.Length; i++)
        {
            char ch = pattern[i];
            if (escaping)
            {
                builder.Append('\\').Append(ch);
                escaping = false;
                continue;
            }

            if (ch == '\\')
            {
                escaping = true;
                continue;
            }

            if (ch == '[' && !inCharClass)
            {
                inCharClass = true;
                builder.Append(ch);
                continue;
            }

            if (ch == ']' && inCharClass)
            {
                inCharClass = false;
                builder.Append(ch);
                continue;
            }

            if (!inCharClass && ch == '.')
            {
                builder.Append(UnicodeWildcardPattern);
                continue;
            }

            builder.Append(ch);
        }

        if (escaping)
        {
            builder.Append('\\');
        }

        return builder.ToString();
    }

    private static bool HasCaseVariance(string value)
    {
        string lower = value.ToLowerInvariant();
        string upper = value.ToUpperInvariant();
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
            // TS parity: plain-text searches only treat LF as multiline; lone CR keeps line-by-line path.
            return searchString.IndexOf('\n') >= 0;
        }
        return SearchPatternUtilities.IsMultilineRegexSource(searchString);
    }

    private static string ExpandUnicodeEscapes(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return pattern;
        }

        StringBuilder sb = new(pattern.Length);
        for (int i = 0; i < pattern.Length; i++)
        {
            char ch = pattern[i];
            if (ch == '\\' && i + 2 < pattern.Length && pattern[i + 1] == 'u' && pattern[i + 2] == '{')
            {
                int end = pattern.IndexOf('}', i + 3);
                if (end > i)
                {
                    ReadOnlySpan<char> span = pattern.AsSpan(i + 3, end - (i + 3));
                    if (int.TryParse(span, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int codePoint))
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

internal static class SearchPatternUtilities
{
    internal static bool IsMultilineRegexSource(string? source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return false;
        }

        for (int i = 0; i < source.Length; i++)
        {
            char ch = source[i];
            if (ch == '\n')
            {
                return true;
            }

            if (ch == '\\' && i + 1 < source.Length)
            {
                i++;
                char next = source[i];
                if (next == 'n' || next == 'r' || next == 'W')
                {
                    return true;
                }
            }
        }

        return false;
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

        List<int> list = [];
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

/// <summary>
/// Port of VS Code's <c>WordCharacterClassifier</c>. The TypeScript version can
/// hydrate an <c>Intl.Segmenter</c> for locale-aware boundaries; this .NET port
/// only honors the configurable separator list until the Intl/ICU backlog
/// lands.
/// </summary>
public sealed class WordCharacterClassifier
{
    private readonly Dictionary<int, WordCharacterClass> _classes = [];

    public WordCharacterClassifier(string separators)
    {
        if (!string.IsNullOrEmpty(separators))
        {
            foreach (Rune rune in separators.EnumerateRunes())
            {
                _classes[rune.Value] = WordCharacterClass.WordSeparator;
            }
        }

        SetClass(' ', WordCharacterClass.Whitespace);
        SetClass('\t', WordCharacterClass.Whitespace);
        SetClass('\r', WordCharacterClass.Whitespace);
        SetClass('\n', WordCharacterClass.Whitespace);
    }

    private void SetClass(int codePoint, WordCharacterClass @class)
    {
        _classes[codePoint] = @class;
    }

    public WordCharacterClass GetClass(int codePoint)
    {
        return _classes.TryGetValue(codePoint, out WordCharacterClass @class) ? @class : WordCharacterClass.Regular;
    }

    public bool IsValidMatch(string text, int matchStartIndex, int matchLength)
    {
        ArgumentNullException.ThrowIfNull(text);
        int textLength = text.Length;
        if (textLength == 0)
        {
            return true;
        }

        return LeftIsWordBoundary(text, matchStartIndex, matchLength)
            && RightIsWordBoundary(text, textLength, matchStartIndex, matchLength);
    }

    private bool LeftIsWordBoundary(string text, int matchStartIndex, int matchLength)
    {
        if (matchStartIndex <= 0)
        {
            return true;
        }

        if (!UnicodeUtility.TryGetPreviousCodePoint(text, matchStartIndex, out int before, out _))
        {
            return true;
        }

        if (UnicodeUtility.IsLineBreak(before) || IsWordSeparator(before))
        {
            return true;
        }

        if (matchLength > 0)
        {
            if (!UnicodeUtility.TryGetCodePointAt(text, matchStartIndex, out int firstInMatch, out _))
            {
                return true;
            }

            if (IsWordSeparator(firstInMatch))
            {
                return true;
            }
        }

        return false;
    }

    private bool RightIsWordBoundary(string text, int textLength, int matchStartIndex, int matchLength)
    {
        int endIndex = matchStartIndex + matchLength;
        if (endIndex >= textLength)
        {
            return true;
        }

        if (!UnicodeUtility.TryGetCodePointAt(text, endIndex, out int after, out _))
        {
            return true;
        }

        if (UnicodeUtility.IsLineBreak(after) || IsWordSeparator(after))
        {
            return true;
        }

        if (matchLength > 0)
        {
            if (!UnicodeUtility.TryGetPreviousCodePoint(text, endIndex, out int lastInMatch, out _))
            {
                return true;
            }

            if (IsWordSeparator(lastInMatch))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsWordSeparator(int codePoint)
    {
        return GetClass(codePoint) != WordCharacterClass.Regular;
    }
}

/// <summary>
/// 10-entry LRU mirroring VS Code's <c>getMapForWordSeparators</c>. Locale hints
/// are not part of the cache key yet; wiring them through is tracked as part of
/// the Intl.Segmenter/word-separator backlog.
/// </summary>
internal static class WordCharacterClassifierCache
{
    private const int MaxEntries = 10;
    private static readonly Dictionary<string, (WordCharacterClassifier classifier, long stamp)> _cache = new(StringComparer.Ordinal);
    private static long _counter;
    private static readonly object _sync = new();

    public static WordCharacterClassifier Get(string separators)
    {
        if (string.IsNullOrEmpty(separators))
        {
            // Empty separators => classifier with no word separators; cache key can be empty string
            separators = string.Empty;
        }
        lock (_sync)
        {
            _counter++;
            if (_cache.TryGetValue(separators, out (WordCharacterClassifier classifier, long stamp) entry))
            {
                _cache[separators] = (entry.classifier, _counter);
                return entry.classifier;
            }
            WordCharacterClassifier classifier = new(separators);
            if (_cache.Count >= MaxEntries)
            {
                string? oldestKey = null;
                long oldestStamp = long.MaxValue;
                foreach (KeyValuePair<string, (WordCharacterClassifier classifier, long stamp)> kv in _cache)
                {
                    if (kv.Value.stamp < oldestStamp)
                    {
                        oldestStamp = kv.Value.stamp;
                        oldestKey = kv.Key;
                    }
                }
                if (oldestKey != null)
                {
                    _cache.Remove(oldestKey);
                }
            }
            _cache[separators] = (classifier, _counter);
            return classifier;
        }
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

        char ch = text[index];
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
        char ch = text[index];
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

        char ch = text[index];
        if (char.IsHighSurrogate(ch) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            return index + 2;
        }

        return index + 1;
    }

    public static bool IsWhitespace(int codePoint)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(codePoint), 0);
        return category == UnicodeCategory.SpaceSeparator
            || category == UnicodeCategory.LineSeparator
            || category == UnicodeCategory.ParagraphSeparator;
    }

    public static bool IsLineBreak(int codePoint)
    {
        return codePoint == '\n' || codePoint == '\r';
    }
}
