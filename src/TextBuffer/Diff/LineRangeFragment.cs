// Source: ts/src/vs/editor/common/diff/defaultLinesDiffComputer/utils.ts
// - Class: LineRangeFragment (Lines: 30-74)
// Ported: 2025-11-19

using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Diff;

internal sealed class LineRangeFragment
{
    private static readonly Dictionary<char, int> CharacterKeys = [];

    private static int GetKey(char ch)
    {
        if (!CharacterKeys.TryGetValue(ch, out int key))
        {
            key = CharacterKeys.Count;
            CharacterKeys[ch] = key;
        }

        return key;
    }

    private readonly int[] _histogram;
    private readonly int _totalCount;

    public LineRangeFragment(LineRange range, string[] lines, DetailedLineRangeMapping source)
    {
        Range = range;
        Lines = lines;
        Source = source;

        _histogram = new int[Math.Max(1, CharacterKeys.Count)];
        Dictionary<int, int> histogram = [];
        int counter = 0;
        for (int i = range.StartLineNumber - 1; i < range.EndLineNumberExclusive - 1 && i < lines.Length; i++)
        {
            string line = lines[i];
            foreach (char ch in line)
            {
                counter++;
                int key = GetKey(ch);
                histogram[key] = histogram.TryGetValue(key, out int existing) ? existing + 1 : 1;
            }

            counter++;
            int newlineKey = GetKey('\n');
            histogram[newlineKey] = histogram.TryGetValue(newlineKey, out int newlineCount) ? newlineCount + 1 : 1;
        }

        _histogram = new int[Math.Max(histogram.Count, CharacterKeys.Count)];
        foreach (KeyValuePair<int, int> kvp in histogram)
        {
            if (kvp.Key >= _histogram.Length)
            {
                Array.Resize(ref _histogram, kvp.Key + 1);
            }

            _histogram[kvp.Key] = kvp.Value;
        }

        _totalCount = Math.Max(1, counter);
    }

    public LineRange Range { get; }
    public string[] Lines { get; }
    public DetailedLineRangeMapping Source { get; }

    public double ComputeSimilarity(LineRangeFragment other)
    {
        int maxLength = Math.Max(_histogram.Length, other._histogram.Length);
        int sumDifferences = 0;
        for (int i = 0; i < maxLength; i++)
        {
            int a = i < _histogram.Length ? _histogram[i] : 0;
            int b = i < other._histogram.Length ? other._histogram[i] : 0;
            sumDifferences += Math.Abs(a - b);
        }

        return 1 - ((double)sumDifferences / (_totalCount + other._totalCount));
    }
}
