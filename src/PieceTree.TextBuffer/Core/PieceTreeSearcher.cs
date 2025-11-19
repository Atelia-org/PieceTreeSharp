using System.Text.RegularExpressions;

namespace PieceTree.TextBuffer.Core;

public class PieceTreeSearcher
{
    private readonly Regex _searchRegex;
    private readonly WordCharacterClassifier? _wordSeparators;
    private int _lastIndex;
    private int _prevMatchStartIndex = -1;
    private int _prevMatchLength = 0;

    public PieceTreeSearcher(WordCharacterClassifier? wordSeparators, Regex searchRegex)
    {
        _wordSeparators = wordSeparators;
        _searchRegex = searchRegex;
    }

    public void Reset(int lastIndex)
    {
        _lastIndex = lastIndex;
        _prevMatchStartIndex = -1;
        _prevMatchLength = 0;
    }

    public Match? Next(string text)
    {
        int textLength = text.Length;
        Match m;

        do
        {
            if (_prevMatchStartIndex + _prevMatchLength == textLength)
            {
                return null;
            }

            if (_lastIndex > textLength) return null;

            m = _searchRegex.Match(text, _lastIndex);
            if (!m.Success)
            {
                return null;
            }

            int matchStartIndex = m.Index;
            int matchLength = m.Length;

            if (matchStartIndex == _prevMatchStartIndex && matchLength == _prevMatchLength)
            {
                if (matchLength == 0)
                {
                    _lastIndex++;
                    if (_lastIndex > textLength) return null;
                    continue;
                }
                return null;
            }

            _prevMatchStartIndex = matchStartIndex;
            _prevMatchLength = matchLength;
            
            if (_wordSeparators != null && !_wordSeparators.IsValidMatch(text, matchStartIndex, matchLength))
            {
                _lastIndex = matchStartIndex + 1;
                continue;
            }

            _lastIndex = matchStartIndex + matchLength;
            return m;

        } while (true);
    }
}
