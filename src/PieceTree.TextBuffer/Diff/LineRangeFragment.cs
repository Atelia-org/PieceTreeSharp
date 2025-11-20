using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Diff;

internal sealed class LineRangeFragment
{
    private static readonly Dictionary<char, int> CharacterKeys = new();

    private static int GetKey(char ch)
    {
        if (!CharacterKeys.TryGetValue(ch, out var key))
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
        var histogram = new Dictionary<int, int>();
        var counter = 0;
        for (var i = range.StartLineNumber - 1; i < range.EndLineNumberExclusive - 1 && i < lines.Length; i++)
        {
            var line = lines[i];
            foreach (var ch in line)
            {
                counter++;
                var key = GetKey(ch);
                histogram[key] = histogram.TryGetValue(key, out var existing) ? existing + 1 : 1;
            }

            counter++;
            var newlineKey = GetKey('\n');
            histogram[newlineKey] = histogram.TryGetValue(newlineKey, out var newlineCount) ? newlineCount + 1 : 1;
        }

        _histogram = new int[Math.Max(histogram.Count, CharacterKeys.Count)];
        foreach (var kvp in histogram)
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
        var maxLength = Math.Max(_histogram.Length, other._histogram.Length);
        var sumDifferences = 0;
        for (var i = 0; i < maxLength; i++)
        {
            var a = i < _histogram.Length ? _histogram[i] : 0;
            var b = i < other._histogram.Length ? other._histogram[i] : 0;
            sumDifferences += Math.Abs(a - b);
        }

        return 1 - (double)sumDifferences / (_totalCount + other._totalCount);
    }
}
