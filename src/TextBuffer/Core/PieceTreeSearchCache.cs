// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Class: PieceTreeSearchCache (cache field and helper methods)
// - Lines: 100-268
// Ported: 2025-11-19
// Updated: 2025-11-26 (WS1-PORT-SearchCore: added DEBUG counters and extended cache entry tuple)

using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Core;

#if DEBUG
/// <summary>
/// Diagnostic counters for the PieceTreeSearchCache. Only available in DEBUG builds.
/// Tracks CacheHit, CacheMiss, and ClearedAfterEdit events for testing and profiling.
/// </summary>
public class SearchCacheDiagnostics
{
    private long _cacheHit;
    private long _cacheMiss;
    private long _clearedAfterEdit;

    public long CacheHit => _cacheHit;
    public long CacheMiss => _cacheMiss;
    public long ClearedAfterEdit => _clearedAfterEdit;

    public void RecordHit() => System.Threading.Interlocked.Increment(ref _cacheHit);
    public void RecordMiss() => System.Threading.Interlocked.Increment(ref _cacheMiss);
    public void RecordClear() => System.Threading.Interlocked.Increment(ref _clearedAfterEdit);

    public void Reset()
    {
        _cacheHit = 0;
        _cacheMiss = 0;
        _clearedAfterEdit = 0;
    }

    public SearchCacheSnapshot ToSnapshot() => new SearchCacheSnapshot(_cacheHit, _cacheMiss, _clearedAfterEdit);
}

/// <summary>
/// Immutable snapshot of search cache diagnostics for testing assertions.
/// </summary>
public readonly record struct SearchCacheSnapshot(long CacheHit, long CacheMiss, long ClearedAfterEdit);
#endif

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

#if DEBUG
    private readonly SearchCacheDiagnostics _diagnostics = new();
    
    /// <summary>
    /// Gets the diagnostic counters for this cache. Only available in DEBUG builds.
    /// </summary>
    public SearchCacheDiagnostics Diagnostics => _diagnostics;
#endif

    public PieceTreeSearchCache(int limit = 1)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Cache limit must be positive.");
        }

        _limit = limit;
        _entries = new List<CacheEntry>(limit);
    }

    public bool TryGetByOffset(int offset, out PieceTreeNode node, out int nodeStartOffset)
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.CoversOffset(offset))
            {
                node = entry.Node;
                nodeStartOffset = entry.NodeStartOffset;
#if DEBUG
                _diagnostics.RecordHit();
#endif
                return true;
            }
        }

        node = null!;
        nodeStartOffset = 0;
#if DEBUG
        _diagnostics.RecordMiss();
#endif
        return false;
    }

    public bool TryGetByLine(int lineNumber, out PieceTreeNode node, out int nodeStartOffset, out int nodeStartLineNumber)
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.CoversLine(lineNumber))
            {
                node = entry.Node;
                nodeStartOffset = entry.NodeStartOffset;
                nodeStartLineNumber = entry.NodeStartLineNumber!.Value;
#if DEBUG
                _diagnostics.RecordHit();
#endif
                return true;
            }
        }

        node = null!;
        nodeStartOffset = 0;
        nodeStartLineNumber = 0;
#if DEBUG
        _diagnostics.RecordMiss();
#endif
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
#if DEBUG
        if (_entries.Count > 0)
        {
            _diagnostics.RecordClear();
        }
#endif
        _entries.Clear();
    }

    public void InvalidateFromOffset(int offset)
    {
        InvalidateRange(offset, int.MaxValue);
    }

    public void InvalidateRange(int startOffset, int length)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        var normalizedStart = Math.Max(0, startOffset);
        var normalizedLength = length < 0 ? 0 : length;
        var normalizedEnd = normalizedLength == int.MaxValue
            ? int.MaxValue
            : Math.Min(int.MaxValue, normalizedStart + normalizedLength);

        var hadRemovals = false;
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            if (_entries[i].Intersects(normalizedStart, normalizedEnd))
            {
                _entries.RemoveAt(i);
                hadRemovals = true;
            }
        }
#if DEBUG
        if (hadRemovals)
        {
            _diagnostics.RecordClear();
        }
#endif
    }

    public void Validate(Func<PieceTreeNode, int> computeOffset, int totalLength)
    {
        ArgumentNullException.ThrowIfNull(computeOffset);
        if (_entries.Count == 0)
        {
            return;
        }

        var maxLength = Math.Max(0, totalLength);
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            var entry = _entries[i];
            if (entry.Node.IsSentinel || entry.Node.IsDetached)
            {
                _entries.RemoveAt(i);
                continue;
            }

            var actualOffset = computeOffset(entry.Node);
            if (actualOffset != entry.NodeStartOffset || actualOffset >= maxLength)
            {
                _entries.RemoveAt(i);
            }
        }
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
            var start = NodeStartOffset;
            var end = NodeStartOffset + Node.Piece.Length;
            return start <= offset && offset <= end;
        }

        public bool CoversLine(int lineNumber)
        {
            if (!NodeStartLineNumber.HasValue)
            {
                return false;
            }

            var startLine = NodeStartLineNumber.Value;
            var endLine = startLine + Node.Piece.LineFeedCount;
            return startLine < lineNumber && endLine >= lineNumber;
        }

        public bool Intersects(int rangeStart, int rangeEnd)
        {
            var entryStart = NodeStartOffset;
            var entryEnd = NodeStartOffset + Math.Max(0, Node.Piece.Length);
            if (rangeEnd <= rangeStart)
            {
                return entryStart <= rangeStart && rangeStart <= entryEnd;
            }

            return entryStart < rangeEnd && entryEnd > rangeStart;
        }
    }
}
