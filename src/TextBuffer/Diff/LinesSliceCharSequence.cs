// Source: ts/src/vs/editor/common/diff/defaultLinesDiffComputer/linesSliceCharSequence.ts
// - Class: LinesSliceCharSequence (Lines: 14-246)
// Ported: 2025-11-19

using System.Text;
using PieceTree.TextBuffer.Diff.Algorithms;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Diff;

internal sealed class LinesSliceCharSequence : ISequence
{
    private readonly List<int> _elements = [];
    private readonly List<int> _firstElementOffsetByLineIdx = [];
    private readonly List<int> _lineStartOffsets = [];
    private readonly List<int> _trimmedWhitespaceByLineIdx = [];

    private readonly string[] _lines;
    private readonly Range _range;
    private readonly bool _considerWhitespaceChanges;

    public LinesSliceCharSequence(string[] lines, Range range, bool considerWhitespaceChanges)
    {
        _lines = lines;
        _range = range;
        _considerWhitespaceChanges = considerWhitespaceChanges;
        BuildElements();
    }

    public string Text => GetText(new OffsetRange(0, Length));

    public string GetText(OffsetRange range)
    {
        StringBuilder builder = new(range.Length);
        int start = Math.Clamp(range.Start, 0, _elements.Count);
        int end = Math.Clamp(range.EndExclusive, start, _elements.Count);
        for (int i = start; i < end; i++)
        {
            builder.Append((char)_elements[i]);
        }

        return builder.ToString();
    }

    public int GetElement(int offset) => _elements[offset];

    public int Length => _elements.Count;

    public int GetBoundaryScore(int length)
    {
        CharBoundaryCategory prevCategory = GetCategory(length > 0 ? _elements[length - 1] : -1);
        CharBoundaryCategory nextCategory = GetCategory(length < _elements.Count ? _elements[length] : -1);

        if (prevCategory == CharBoundaryCategory.LineBreakCr && nextCategory == CharBoundaryCategory.LineBreakLf)
        {
            return 0;
        }

        if (prevCategory == CharBoundaryCategory.LineBreakLf)
        {
            return 150;
        }

        int score = 0;
        if (prevCategory != nextCategory)
        {
            score += 10;
            if (prevCategory == CharBoundaryCategory.WordLower && nextCategory == CharBoundaryCategory.WordUpper)
            {
                score += 1;
            }
        }

        score += GetCategoryBoundaryScore(prevCategory);
        score += GetCategoryBoundaryScore(nextCategory);
        return score;
    }

    public bool IsStronglyEqual(int offset1, int offset2)
    {
        return _elements[offset1] == _elements[offset2];
    }

    public TextPosition TranslateOffset(int offset, bool preferLeft = false)
    {
        int index = FindLastIndex(_firstElementOffsetByLineIdx, v => v <= offset);
        if (index < 0)
        {
            index = 0;
        }

        int lineOffset = offset - _firstElementOffsetByLineIdx[index];
        int lineNumber = _range.Start.LineNumber + index;
        int column = 1 + _lineStartOffsets[index] + lineOffset + ((lineOffset == 0 && preferLeft) ? 0 : _trimmedWhitespaceByLineIdx[index]);
        return new TextPosition(lineNumber, column);
    }

    public Range TranslateRange(OffsetRange range)
    {
        TextPosition start = TranslateOffset(range.Start, false);
        TextPosition end = TranslateOffset(range.EndExclusive, true);
        if (end < start)
        {
            return Range.FromPositions(end, end);
        }

        return Range.FromPositions(start, end);
    }

    public OffsetRange? FindWordContaining(int offset)
    {
        if (offset < 0 || offset >= _elements.Count)
        {
            return null;
        }

        if (!IsWordChar(_elements[offset]))
        {
            return null;
        }

        int start = offset;
        while (start > 0 && IsWordChar(_elements[start - 1]))
        {
            start--;
        }

        int end = offset;
        while (end < _elements.Count && IsWordChar(_elements[end]))
        {
            end++;
        }

        return new OffsetRange(start, end);
    }

    public OffsetRange? FindSubWordContaining(int offset)
    {
        if (offset < 0 || offset >= _elements.Count)
        {
            return null;
        }

        if (!IsWordChar(_elements[offset]))
        {
            return null;
        }

        int start = offset;
        while (start > 0 && IsWordChar(_elements[start - 1]) && !IsUpperCase(_elements[start]))
        {
            start--;
        }

        int end = offset;
        while (end < _elements.Count && IsWordChar(_elements[end]) && !IsUpperCase(_elements[end]))
        {
            end++;
        }

        return new OffsetRange(start, end);
    }

    public int CountLinesIn(OffsetRange range)
    {
        TextPosition start = TranslateOffset(range.Start);
        TextPosition end = TranslateOffset(range.EndExclusive);
        return Math.Max(0, end.LineNumber - start.LineNumber);
    }

    public OffsetRange ExtendToFullLines(OffsetRange range)
    {
        int start = FindLastIndex(_firstElementOffsetByLineIdx, x => x <= range.Start);
        start = Math.Max(0, start);
        int end = FindFirstIndex(_firstElementOffsetByLineIdx, x => range.EndExclusive <= x);
        if (end < 0)
        {
            end = _elements.Count;
        }

        return new OffsetRange(start >= 0 ? _firstElementOffsetByLineIdx[start] : 0, end);
    }

    private void BuildElements()
    {
        _firstElementOffsetByLineIdx.Add(0);
        for (int lineNumber = _range.StartLineNumber; lineNumber <= _range.EndLineNumber; lineNumber++)
        {
            string line = _lines[lineNumber - 1];
            int lineStartOffset = 0;
            if (lineNumber == _range.StartLineNumber && _range.StartColumn > 1)
            {
                lineStartOffset = _range.StartColumn - 1;
                line = line.Substring(lineStartOffset);
            }

            _lineStartOffsets.Add(lineStartOffset);

            int trimmedWhitespace = 0;
            string processedLine = line;
            if (!_considerWhitespaceChanges)
            {
                string trimmedStart = processedLine.TrimStart();
                trimmedWhitespace = processedLine.Length - trimmedStart.Length;
                processedLine = trimmedStart.TrimEnd();
            }

            _trimmedWhitespaceByLineIdx.Add(trimmedWhitespace);

            int maxLength = lineNumber == _range.EndLineNumber
                ? Math.Min(_range.EndColumn - 1 - lineStartOffset - trimmedWhitespace, processedLine.Length)
                : processedLine.Length;

            for (int i = 0; i < maxLength; i++)
            {
                _elements.Add(processedLine[i]);
            }

            if (lineNumber < _range.EndLineNumber)
            {
                _elements.Add('\n');
                _firstElementOffsetByLineIdx.Add(_elements.Count);
            }
        }
    }

    private static int FindLastIndex(IReadOnlyList<int> list, Func<int, bool> predicate)
    {
        int low = 0;
        int high = list.Count - 1;
        int result = -1;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (predicate(list[mid]))
            {
                result = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return result;
    }

    private static int FindFirstIndex(IReadOnlyList<int> list, Func<int, bool> predicate)
    {
        int low = 0;
        int high = list.Count - 1;
        int result = -1;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (predicate(list[mid]))
            {
                result = list[mid];
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        return result;
    }

    private static bool IsWordChar(int charCode)
    {
        return (charCode >= 'a' && charCode <= 'z')
            || (charCode >= 'A' && charCode <= 'Z')
            || (charCode >= '0' && charCode <= '9');
    }

    private static bool IsUpperCase(int charCode)
    {
        return charCode >= 'A' && charCode <= 'Z';
    }

    private static CharBoundaryCategory GetCategory(int charCode)
    {
        if (charCode == '\n')
        {
            return CharBoundaryCategory.LineBreakLf;
        }

        if (charCode == '\r')
        {
            return CharBoundaryCategory.LineBreakCr;
        }

        if (charCode == ' ' || charCode == '\t')
        {
            return CharBoundaryCategory.Space;
        }

        if (charCode >= 'a' && charCode <= 'z')
        {
            return CharBoundaryCategory.WordLower;
        }

        if (charCode >= 'A' && charCode <= 'Z')
        {
            return CharBoundaryCategory.WordUpper;
        }

        if (charCode >= '0' && charCode <= '9')
        {
            return CharBoundaryCategory.WordNumber;
        }

        if (charCode == -1)
        {
            return CharBoundaryCategory.End;
        }

        if (charCode == ',' || charCode == ';')
        {
            return CharBoundaryCategory.Separator;
        }

        return CharBoundaryCategory.Other;
    }

    private static int GetCategoryBoundaryScore(CharBoundaryCategory category)
    {
        return category switch
        {
            CharBoundaryCategory.WordLower => 0,
            CharBoundaryCategory.WordUpper => 0,
            CharBoundaryCategory.WordNumber => 0,
            CharBoundaryCategory.End => 10,
            CharBoundaryCategory.Other => 2,
            CharBoundaryCategory.Separator => 30,
            CharBoundaryCategory.Space => 3,
            CharBoundaryCategory.LineBreakCr => 10,
            CharBoundaryCategory.LineBreakLf => 10,
            _ => 0,
        };
    }

    private enum CharBoundaryCategory
    {
        WordLower,
        WordUpper,
        WordNumber,
        End,
        Other,
        Separator,
        Space,
        LineBreakCr,
        LineBreakLf,
    }
}
