// Source: ts/src/vs/editor/common/diff/defaultLinesDiffComputer/lineSequence.ts
// - Class: LineSequence (Lines: 10-45)
// Ported: 2025-11-19

using System;
using System.Linq;
using PieceTree.TextBuffer.Diff.Algorithms;

namespace PieceTree.TextBuffer.Diff;

internal sealed class LineSequence : ISequence
{
    private readonly int[] _trimmedHash;
    private readonly string[] _lines;

    public LineSequence(int[] trimmedHash, string[] lines)
    {
        _trimmedHash = trimmedHash;
        _lines = lines;
    }

    public int GetElement(int offset) => _trimmedHash[offset];

    public int Length => _trimmedHash.Length;

    public int GetBoundaryScore(int length)
    {
        int indentationBefore = length == 0 ? 0 : GetIndentation(_lines[length - 1]);
        int indentationAfter = length == _lines.Length ? 0 : GetIndentation(_lines[Math.Min(length, _lines.Length - 1)]);
        return 1000 - (indentationBefore + indentationAfter);
    }

    public string GetText(OffsetRange range)
    {
        int start = Math.Clamp(range.Start, 0, _lines.Length);
        int end = Math.Clamp(range.EndExclusive, start, _lines.Length);
        return string.Join('\n', _lines.Skip(start).Take(end - start));
    }

    public bool IsStronglyEqual(int offset1, int offset2)
    {
        return _lines[offset1] == _lines[offset2];
    }

    private static int GetIndentation(string line)
    {
        int i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
        {
            i++;
        }

        return i;
    }
}
