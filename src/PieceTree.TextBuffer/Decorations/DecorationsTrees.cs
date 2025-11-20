using System;
using System.Collections.Generic;
using System.Linq;

namespace PieceTree.TextBuffer.Decorations;

[Flags]
internal enum DecorationTreeScope
{
    Regular = 1,
    Overview = 2,
    InjectedText = 4,
    All = Regular | Overview | InjectedText,
}

internal sealed class DecorationsTrees
{
    private static readonly IReadOnlyList<ModelDecoration> Empty = Array.Empty<ModelDecoration>();
    private readonly IntervalTree _regular = new();
    private readonly IntervalTree _overview = new();
    private readonly IntervalTree _injected = new();

    public int Count => _regular.Count + _overview.Count + _injected.Count;

    public void Insert(ModelDecoration decoration)
    {
        SelectTree(decoration.Options).Insert(decoration);
    }

    public void Remove(ModelDecoration decoration)
    {
        SelectTree(decoration.Options).Remove(decoration.Id);
    }

    public void Reinsert(ModelDecoration decoration)
    {
        SelectTree(decoration.Options).Reinsert(decoration);
    }

    public IReadOnlyList<ModelDecoration> Search(TextRange range, int ownerFilter = DecorationOwnerIds.Any, DecorationTreeScope scope = DecorationTreeScope.All)
    {
        var regular = scope.HasFlag(DecorationTreeScope.Regular)
            ? _regular.Search(range, ownerFilter)
            : Empty;
        var overview = scope.HasFlag(DecorationTreeScope.Overview)
            ? _overview.Search(range, ownerFilter)
            : Empty;
        var injected = scope.HasFlag(DecorationTreeScope.InjectedText)
            ? _injected.Search(range, ownerFilter)
            : Empty;

        return MergeOrdered(regular, overview, injected);
    }

    public IEnumerable<ModelDecoration> EnumerateAll()
    {
        foreach (var decoration in _regular.EnumerateAll())
        {
            yield return decoration;
        }

        foreach (var decoration in _overview.EnumerateAll())
        {
            yield return decoration;
        }

        foreach (var decoration in _injected.EnumerateAll())
        {
            yield return decoration;
        }
    }

    public IEnumerable<ModelDecoration> EnumerateFrom(int offset)
    {
        foreach (var decoration in _regular.EnumerateFrom(offset))
        {
            yield return decoration;
        }

        foreach (var decoration in _overview.EnumerateFrom(offset))
        {
            yield return decoration;
        }

        foreach (var decoration in _injected.EnumerateFrom(offset))
        {
            yield return decoration;
        }
    }

    private IntervalTree SelectTree(ModelDecorationOptions options)
    {
        if (options.HasInjectedText)
        {
            return _injected;
        }

        if (options.AffectsOverviewRuler)
        {
            return _overview;
        }

        return _regular;
    }

    private static IReadOnlyList<ModelDecoration> MergeOrdered(params IReadOnlyList<ModelDecoration>[] sources)
    {
        var nonEmpty = sources.Where(static list => list.Count > 0).ToArray();
        if (nonEmpty.Length == 0)
        {
            return Empty;
        }

        if (nonEmpty.Length == 1)
        {
            return nonEmpty[0];
        }

        var indices = new int[nonEmpty.Length];
        var comparer = DecorationComparer.Instance;
        var total = nonEmpty.Sum(static list => list.Count);
        var merged = new List<ModelDecoration>(total);

        while (true)
        {
            var nextIndex = -1;
            ModelDecoration? candidate = null;
            for (int i = 0; i < nonEmpty.Length; i++)
            {
                var idx = indices[i];
                if (idx >= nonEmpty[i].Count)
                {
                    continue;
                }

                var current = nonEmpty[i][idx];
                if (candidate is null || comparer.Compare(current, candidate) < 0)
                {
                    candidate = current;
                    nextIndex = i;
                }
            }

            if (nextIndex == -1)
            {
                break;
            }

            merged.Add(candidate!);
            indices[nextIndex]++;
        }

        return merged;
    }

    private sealed class DecorationComparer : IComparer<ModelDecoration>
    {
        public static DecorationComparer Instance { get; } = new();

        public int Compare(ModelDecoration? x, ModelDecoration? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var cmp = x.Range.StartOffset.CompareTo(y.Range.StartOffset);
            if (cmp != 0)
            {
                return cmp;
            }

            cmp = x.Range.EndOffset.CompareTo(y.Range.EndOffset);
            if (cmp != 0)
            {
                return cmp;
            }

            return string.CompareOrdinal(x.Id, y.Id);
        }
    }
}
