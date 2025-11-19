using System.Text.RegularExpressions;

namespace PieceTree.TextBuffer.Core;

public readonly record struct Range(TextPosition Start, TextPosition End)
{
    public Range(int startLine, int startColumn, int endLine, int endColumn)
        : this(new TextPosition(startLine, startColumn), new TextPosition(endLine, endColumn))
    {
    }
}

public class SearchData
{
    public Regex? Regex { get; }
    public string? SimpleSearch { get; }
    public WordCharacterClassifier? WordSeparators { get; }

    public SearchData(Regex? regex, WordCharacterClassifier? wordSeparators, string? simpleSearch)
    {
        Regex = regex;
        WordSeparators = wordSeparators;
        SimpleSearch = simpleSearch;
    }
}

public class FindMatch
{
    public Range Range { get; }
    public string[]? Matches { get; }

    public FindMatch(Range range, string[]? matches)
    {
        Range = range;
        Matches = matches;
    }
}

public class SearchParams
{
    public string SearchString { get; }
    public bool IsRegex { get; }
    public bool MatchCase { get; }
    public string? WordSeparators { get; }

    public SearchParams(string searchString, bool isRegex, bool matchCase, string? wordSeparators)
    {
        SearchString = searchString;
        IsRegex = isRegex;
        MatchCase = matchCase;
        WordSeparators = wordSeparators;
    }

    public SearchData? ParseSearchRequest()
    {
        if (string.IsNullOrEmpty(SearchString))
        {
            return null;
        }

        Regex? regex = null;
        try
        {
             RegexOptions options = RegexOptions.None;
             if (!MatchCase) options |= RegexOptions.IgnoreCase;
             if (SearchString.Contains('\n')) options |= RegexOptions.Multiline;

             string pattern = IsRegex ? SearchString : Regex.Escape(SearchString);
             regex = new Regex(pattern, options | RegexOptions.Compiled);
        }
        catch
        {
             return null;
        }

        string? simpleSearch = null;
        if (!IsRegex && !SearchString.Contains('\n'))
        {
             simpleSearch = SearchString;
             if (!MatchCase && simpleSearch.ToLower() != simpleSearch.ToUpper())
             {
                 simpleSearch = null;
             }
        }

        return new SearchData(regex, null, simpleSearch);
    }
}

public class WordCharacterClassifier
{
    // Stub
}
