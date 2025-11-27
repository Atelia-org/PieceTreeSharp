// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Class: PieceTreeSearchCache (cache field and helper methods)
// - Lines: 100-268
// Ported: 2025-11-19
// Updated: 2025-11-27 (PORT-PT-Search-Step12: release diagnostics, tuple reuse wiring)

using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Core;

/// <summary>
/// Immutable snapshot of the cache state. Exposed via <see cref="PieceTreeModel.Diagnostics"/> so
/// tests and telemetry hooks can assert hit/miss ratios even in release builds.
/// </summary>
public readonly record struct SearchCacheSnapshot(
    long HitCount,
    long MissCount,
    long ClearCount,
    int EntryCount,
    int EntriesRemaining,
    int LastInvalidatedOffset);

/// <summary>
/// Minimal port of VS Code's PieceTreeSearchCache. Stores up to <paramref name="limit"/> node hits so future
/// nodeAt/getLineContent shims can bypass repeated tree walks. Limit defaults to 1, mirroring TS usage.
/// The cache entry stores a tuple of (node, nodeStartOffset, nodeStartLineNumber) to enable short-circuit
/// lookups in NodeAt2 and GetLineRawContent when the cached node already covers the query.
/// </summary>
internal sealed class PieceTreeSearchCache
{
    private readonly int _limit;
    private readonly List<CacheEntry> _entries;
    private long _hitCount;
    private long _missCount;
    private long _clearCount;
    private int _lastInvalidatedOffset = -1;

    /// <summary>
    /// Raised whenever we drop entries (Clear/Invalidate). Allows telemetry hooks to observe churn.
    /// </summary>
    public event Action<SearchCacheSnapshot>? CacheInvalidated;

    public PieceTreeSearchCache(int limit = 1)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Cache limit must be positive.");
        }

        _limit = limit;
        _entries = new List<CacheEntry>(limit);
    }

    public SearchCacheSnapshot Snapshot => new(
        HitCount: _hitCount,
        MissCount: _missCount,
        ClearCount: _clearCount,
        EntryCount: _entries.Count,
        EntriesRemaining: Math.Max(0, _limit - _entries.Count),
        LastInvalidatedOffset: _lastInvalidatedOffset);

    public bool TryGetByOffset(int offset, out PieceTreeNode node, out int nodeStartOffset)
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            CacheEntry entry = _entries[i];
            if (entry.CoversOffset(offset))
            {
                node = entry.Node;
                nodeStartOffset = entry.NodeStartOffset;
                _hitCount++;
                return true;
            }
        }

        node = null!;
        nodeStartOffset = 0;
        _missCount++;
        return false;
    }

    public bool TryGetByLine(int lineNumber, out PieceTreeNode node, out int nodeStartOffset, out int nodeStartLineNumber)
    {
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            CacheEntry entry = _entries[i];
            if (entry.CoversLine(lineNumber))
            {
                node = entry.Node;
                nodeStartOffset = entry.NodeStartOffset;
                nodeStartLineNumber = entry.NodeStartLineNumber!.Value;
                _hitCount++;
                return true;
            }
        }

        node = null!;
        nodeStartOffset = 0;
        nodeStartLineNumber = 0;
        _missCount++;
        return false;
    }

    public void Remember(PieceTreeNode node, int nodeStartOffset, int? nodeStartLineNumber = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.IsSentinel)
        {
            return;
        }

        if (_entries.Count >= _limit)
        {
            _entries.RemoveAt(0);
        }

        _entries.Add(new CacheEntry(node, nodeStartOffset, nodeStartLineNumber));
    }

    public void Clear()
    {
        if (_entries.Count == 0)
        {
            return;
        }

        _entries.Clear();
        _clearCount++;
        _lastInvalidatedOffset = 0;
        OnCacheInvalidated();
    }

    public void InvalidateFromOffset(int offset)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        int threshold = Math.Max(0, offset);
        bool removed = false;
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].NodeStartOffset >= threshold)
            {
                _entries.RemoveAt(i);
                removed = true;
            }
        }

        if (removed)
        {
            _clearCount++;
            _lastInvalidatedOffset = threshold;
            OnCacheInvalidated();
        }
    }

    public void InvalidateRange(int startOffset, int length)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        int normalizedStart = Math.Max(0, startOffset);
        int normalizedLength = length < 0 ? 0 : length;
        int normalizedEnd = normalizedLength == int.MaxValue
            ? int.MaxValue
            : Math.Min(int.MaxValue, normalizedStart + normalizedLength);

        bool hadRemovals = false;
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].Intersects(normalizedStart, normalizedEnd))
            {
                _entries.RemoveAt(i);
                hadRemovals = true;
            }
        }
        if (hadRemovals)
        {
            _clearCount++;
            _lastInvalidatedOffset = normalizedStart;
            OnCacheInvalidated();
        }
    }

    public void Validate(Func<PieceTreeNode, int> computeOffset, int totalLength)
    {
        ArgumentNullException.ThrowIfNull(computeOffset);
        if (_entries.Count == 0)
        {
            return;
        }

        int maxLength = Math.Max(0, totalLength);
        for (int i = _entries.Count - 1; i >= 0; i--)
        {
            CacheEntry entry = _entries[i];
            if (entry.Node.IsSentinel || entry.Node.IsDetached)
            {
                _entries.RemoveAt(i);
                continue;
            }

            int actualOffset = computeOffset(entry.Node);
            if (actualOffset != entry.NodeStartOffset || actualOffset >= maxLength)
            {
                _entries.RemoveAt(i);
            }
        }
    }

    private void OnCacheInvalidated()
    {
        CacheInvalidated?.Invoke(Snapshot);
    }

    private readonly struct CacheEntry
    {
        public CacheEntry(PieceTreeNode node, int nodeStartOffset, int? nodeStartLineNumber)
        {
            Node = node;
            NodeStartOffset = nodeStartOffset;
            NodeStartLineNumber = nodeStartLineNumber;
        }

        public PieceTreeNode Node { get; }

        public int NodeStartOffset { get; }

        public int? NodeStartLineNumber { get; }

        public bool CoversOffset(int offset)
        {
            int start = NodeStartOffset;
            int end = NodeStartOffset + Node.Piece.Length;
            return start <= offset && offset <= end;
        }

        public bool CoversLine(int lineNumber)
        {
            if (!NodeStartLineNumber.HasValue)
            {
                return false;
            }

            int startLine = NodeStartLineNumber.Value;
            int endLine = startLine + Node.Piece.LineFeedCount;
            return startLine <= lineNumber && endLine >= lineNumber;
        }

        public bool Intersects(int rangeStart, int rangeEnd)
        {
            int entryStart = NodeStartOffset;
            int entryEnd = NodeStartOffset + Math.Max(0, Node.Piece.Length);
            if (rangeEnd <= rangeStart)
            {
                return entryStart <= rangeStart && rangeStart <= entryEnd;
            }

            return entryStart < rangeEnd && entryEnd > rangeStart;
        }
    }
}
