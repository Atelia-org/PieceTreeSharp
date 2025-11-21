// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Functions: Searcher class and search implementation
// - Lines: 1500-1700
// Ported: 2025-11-19

using System;
using System.Text.RegularExpressions;

namespace PieceTree.TextBuffer.Core;

public class PieceTreeSearcher
{
    private readonly Regex _searchRegex;
    private readonly WordCharacterClassifier? _wordSeparators;
    private int _lastIndex;
    private int _prevMatchStartIndex = -1;
    private int _prevMatchLength = -1;

    public PieceTreeSearcher(WordCharacterClassifier? wordSeparators, Regex searchRegex)
    {
        ArgumentNullException.ThrowIfNull(searchRegex);
        _wordSeparators = wordSeparators;
        _searchRegex = EnsureEcmaRegex(searchRegex);
    }

    private static Regex EnsureEcmaRegex(Regex regex)
    {
        if ((regex.Options & RegexOptions.ECMAScript) != 0)
        {
            return regex;
        }

        var options = regex.Options | RegexOptions.ECMAScript;
        return new Regex(regex.ToString(), options, regex.MatchTimeout);
    }

    public void Reset(int lastIndex)
    {
        _lastIndex = Math.Max(0, lastIndex);
        _prevMatchStartIndex = -1;
        _prevMatchLength = -1;
    }

    public Match? Next(string text)
    {
        var textLength = text.Length;

        while (true)
        {
            if (_lastIndex > textLength)
            {
                return null;
            }

            var match = _searchRegex.Match(text, _lastIndex);
            if (!match.Success)
            {
                return null;
            }

            var matchStartIndex = match.Index;
            var matchLength = match.Length;

            if (matchStartIndex == _prevMatchStartIndex && matchLength == _prevMatchLength)
            {
                if (matchLength == 0)
                {
                    AdvanceForZeroLength(text, matchStartIndex);
                    continue;
                }

                return null;
            }

            _prevMatchStartIndex = matchStartIndex;
            _prevMatchLength = matchLength;

            AdvanceLastIndex(text, matchStartIndex, matchLength);

            if (_wordSeparators != null && !_wordSeparators.IsValidMatch(text, matchStartIndex, matchLength))
            {
                continue;
            }

            return match;
        }
    }

    private void AdvanceLastIndex(string text, int matchStartIndex, int matchLength)
    {
        if (matchLength == 0)
        {
            AdvanceForZeroLength(text, matchStartIndex);
            return;
        }

        _lastIndex = matchStartIndex + matchLength;
    }

    private void AdvanceForZeroLength(string text, int matchStartIndex)
    {
        var nextIndex = UnicodeUtility.AdvanceByCodePoint(text, matchStartIndex);
        if (nextIndex <= matchStartIndex)
        {
            nextIndex = matchStartIndex + 1;
        }

        _lastIndex = nextIndex;
    }
}
