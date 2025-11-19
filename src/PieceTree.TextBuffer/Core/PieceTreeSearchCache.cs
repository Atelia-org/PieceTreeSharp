using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Core;

/// <summary>
/// Minimal port of VS Code's PieceTreeSearchCache. Stores up to <paramref name="limit"/> node hits so future
/// nodeAt/getLineContent shims can bypass repeated tree walks. Limit defaults to 1, mirroring TS usage.
/// </summary>
internal sealed class PieceTreeSearchCache
{
    private readonly int _limit;
    private readonly List<CacheEntry> _entries;

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
                return true;
            }
        }

        node = null!;
        nodeStartOffset = 0;
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
                return true;
            }
        }

        node = null!;
        nodeStartOffset = 0;
        nodeStartLineNumber = 0;
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

    public void InvalidateFromOffset(int offset)
    {
        if (_entries.Count == 0)
        {
            return;
        }

        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            if (!_entries[i].IsValid(offset))
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

        public bool IsValid(int earliestMutationOffset)
        {
            if (Node.IsDetached)
            {
                return false;
            }

            return NodeStartOffset < earliestMutationOffset;
        }
    }
}
