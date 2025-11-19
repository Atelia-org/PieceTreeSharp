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
        _wordSeparators = wordSeparators;
        _searchRegex = searchRegex;
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
