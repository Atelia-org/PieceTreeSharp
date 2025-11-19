using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Core;

internal sealed partial class PieceTreeModel
{
    private const string SearchNotImplementedMessage = "PieceTreeModel search stub is not implemented (PT-004 placeholder).";

    public const int ChangeBufferId = 0;

    private readonly PieceTreeNode _sentinel = PieceTreeNode.Sentinel;
    private readonly PieceTreeSearchCache _searchCache = new();
    private struct LastVisitedLine { public int LineNumber; public string Value; }
    private LastVisitedLine _lastVisitedLine;
    private readonly List<ChunkBuffer> _buffers;
    private PieceTreeNode _root;
    private int _count;
    private bool _eolNormalized;
    private string _eol = "\n";

    public PieceTreeModel(List<ChunkBuffer> buffers, bool eolNormalized = false, string eol = "\n")
    {
        _buffers = buffers ?? throw new ArgumentNullException(nameof(buffers));
        _root = _sentinel;
        _eolNormalized = eolNormalized;
        _eol = eol;
    }

    public PieceTreeNode Root => _root;

    public int PieceCount => _count;

    internal IReadOnlyList<ChunkBuffer> Buffers => _buffers;

    public int TotalLength => ReferenceEquals(_root, _sentinel) ? 0 : _root.AggregatedLength;

    public int TotalLineFeeds => ReferenceEquals(_root, _sentinel) ? 0 : _root.AggregatedLineFeeds;

    public bool IsEmpty => ReferenceEquals(_root, _sentinel);

    internal PieceTreeSearchCache SearchCache => _searchCache;

    public string Eol => _eol;

    public void NormalizeEOL(string eol)
    {
        _eol = eol;
        var averageBufferSize = 65536;
        var sb = new System.Text.StringBuilder();
        var chunks = new List<string>();
        bool skipNextLF = false;

        foreach (var piece in EnumeratePiecesInOrder())
        {
            var buffer = _buffers[piece.BufferIndex];
            var text = buffer.Slice(piece.Start, piece.End);
            
            int start = 0;
            if (skipNextLF)
            {
                if (text.Length > 0 && text[0] == '\n')
                {
                    start = 1;
                }
                skipNextLF = false;
            }
            
            for (int i = start; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '\r')
                {
                    sb.Append(eol);
                    if (i + 1 < text.Length)
                    {
                        if (text[i + 1] == '\n')
                        {
                            i++;
                        }
                    }
                    else
                    {
                        skipNextLF = true;
                    }
                }
                else if (ch == '\n')
                {
                    sb.Append(eol);
                }
                else
                {
                    sb.Append(ch);
                }
            }
            
            if (sb.Length > averageBufferSize)
            {
                chunks.Add(sb.ToString());
                sb.Clear();
            }
        }
        
        if (sb.Length > 0)
        {
            chunks.Add(sb.ToString());
        }
        
        if (chunks.Count > 0)
        {
            _root = _sentinel;
            _count = 0;
            _searchCache.InvalidateFromOffset(0);
            _buffers.Clear();
            _buffers.Add(ChunkBuffer.Empty);
            
            foreach (var chunk in chunks)
            {
                var chunkBuffer = ChunkBuffer.FromText(chunk);
                _buffers.Add(chunkBuffer);
                var bufferIndex = _buffers.Count - 1;
                var piece = new PieceSegment(
                    bufferIndex,
                    BufferCursor.Zero,
                    chunkBuffer.CreateEndCursor(),
                    chunkBuffer.LineFeedCount,
                    chunkBuffer.Length
                );
                InsertPieceAtEnd(piece);
            }
            
            _eolNormalized = true;
        }
    }

    public PieceTreeNode InsertPieceAtEnd(PieceSegment piece)
    {
        var insertionOffset = TotalLength;
        _searchCache.InvalidateFromOffset(insertionOffset);

        var node = new PieceTreeNode(piece);
        node.ResetLinks();

        var parent = _sentinel;
        var current = _root;
        while (!ReferenceEquals(current, _sentinel))
        {
            parent = current;
            current = current.Right;
        }

        node.Parent = parent;
        if (ReferenceEquals(parent, _sentinel))
        {
            _root = node;
        }
        else
        {
            parent.Right = node;
        }

        _count++;
        InsertFixup(node);
        RecomputeMetadataUpwards(node);
        return node;
    }

    internal NodeHit NodeAt(int offset)
    {
        var x = _root;
        if (_searchCache.TryGetByOffset(offset, out var cachedNode, out var cachedStartOffset))
        {
            return new NodeHit(cachedNode, offset - cachedStartOffset, cachedStartOffset);
        }

        var nodeStartOffset = 0;

        while (!ReferenceEquals(x, _sentinel))
        {
            if (x.SizeLeft > offset)
            {
                x = x.Left;
            }
            else if (x.SizeLeft + x.Piece.Length >= offset)
            {
                nodeStartOffset += x.SizeLeft;
                var hit = new NodeHit(x, offset - x.SizeLeft, nodeStartOffset);
                _searchCache.Remember(x, nodeStartOffset);
                return hit;
            }
            else
            {
                offset -= x.SizeLeft + x.Piece.Length;
                nodeStartOffset += x.SizeLeft + x.Piece.Length;
                x = x.Right;
            }
        }

        return default;
    }

    internal bool TryGetCachedNodeByOffset(int offset, out PieceTreeNode node, out int nodeStartOffset)
    {
        return _searchCache.TryGetByOffset(offset, out node, out nodeStartOffset);
    }

    internal bool TryGetCachedNodeByLine(int lineNumber, out PieceTreeNode node, out int nodeStartOffset, out int nodeStartLineNumber)
    {
        return _searchCache.TryGetByLine(lineNumber, out node, out nodeStartOffset, out nodeStartLineNumber);
    }

    internal void RememberNodePosition(PieceTreeNode node, int nodeStartOffset, int? nodeStartLineNumber = null)
    {
        _searchCache.Remember(node, nodeStartOffset, nodeStartLineNumber);
    }

    internal void InvalidateCacheFromOffset(int offset) => _searchCache.InvalidateFromOffset(offset);

    public IEnumerable<PieceSegment> EnumeratePiecesInOrder()
    {
        foreach (var node in EnumerateNodesInOrder())
        {
            yield return node.Piece;
        }
    }

    public ITextSnapshot CreateSnapshot(string bom)
    {
        return new PieceTreeSnapshot(this, bom);
    }

    internal IEnumerable<PieceTreeNode> EnumerateNodesInOrder()
    {
        if (ReferenceEquals(_root, _sentinel))
        {
            yield break;
        }

        var stack = new Stack<PieceTreeNode>();
        var current = _root;
        while (!ReferenceEquals(current, _sentinel) || stack.Count > 0)
        {
            while (!ReferenceEquals(current, _sentinel))
            {
                stack.Push(current);
                current = current.Left;
            }

            current = stack.Pop();
            yield return current;
            current = current.Right;
        }
    }

    internal int GetOffsetOfNode(PieceTreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (ReferenceEquals(node, _sentinel))
        {
            throw new ArgumentException("Sentinel node does not have an offset.", nameof(node));
        }

        var offset = node.SizeLeft;
        var current = node;
        while (!ReferenceEquals(current.Parent, _sentinel))
        {
            if (ReferenceEquals(current, current.Parent.Right))
            {
                offset += current.Parent.SizeLeft + current.Parent.Piece.Length;
            }

            current = current.Parent;
        }

        return offset;
    }

    internal int GetLineFeedsBeforeNode(PieceTreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (ReferenceEquals(node, _sentinel))
        {
            throw new ArgumentException("Sentinel node does not have line feed metadata.", nameof(node));
        }

        var lineFeeds = node.LineFeedsLeft;
        var current = node;
        while (!ReferenceEquals(current.Parent, _sentinel))
        {
            if (ReferenceEquals(current, current.Parent.Right))
            {
                lineFeeds += current.Parent.LineFeedsLeft + current.Parent.Piece.LineFeedCount;
            }

            current = current.Parent;
        }

        return lineFeeds;
    }

    public PieceTreeSearchResult ExecuteSearch(PieceTreeSearchPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        throw new NotSupportedException(SearchNotImplementedMessage);
    }

    private void InsertFixup(PieceTreeNode node)
    {
        while (node.Parent.Color == NodeColor.Red)
        {
            if (ReferenceEquals(node.Parent, node.Parent.Parent.Left))
            {
                var uncle = node.Parent.Parent.Right;
                if (uncle.Color == NodeColor.Red)
                {
                    node.Parent.Color = NodeColor.Black;
                    uncle.Color = NodeColor.Black;
                    node.Parent.Parent.Color = NodeColor.Red;
                    node = node.Parent.Parent;
                }
                else
                {
                    if (ReferenceEquals(node, node.Parent.Right))
                    {
                        node = node.Parent;
                        RotateLeft(node);
                    }

                    node.Parent.Color = NodeColor.Black;
                    node.Parent.Parent.Color = NodeColor.Red;
                    RotateRight(node.Parent.Parent);
                }
            }
            else
            {
                var uncle = node.Parent.Parent.Left;
                if (uncle.Color == NodeColor.Red)
                {
                    node.Parent.Color = NodeColor.Black;
                    uncle.Color = NodeColor.Black;
                    node.Parent.Parent.Color = NodeColor.Red;
                    node = node.Parent.Parent;
                }
                else
                {
                    if (ReferenceEquals(node, node.Parent.Left))
                    {
                        node = node.Parent;
                        RotateRight(node);
                    }

                    node.Parent.Color = NodeColor.Black;
                    node.Parent.Parent.Color = NodeColor.Red;
                    RotateLeft(node.Parent.Parent);
                }
            }
        }

        _root.Color = NodeColor.Black;
    }

    private void RotateLeft(PieceTreeNode node)
    {
        var pivot = node.Right;
        node.Right = pivot.Left;
        if (!ReferenceEquals(pivot.Left, _sentinel))
        {
            pivot.Left.Parent = node;
        }

        pivot.Parent = node.Parent;
        if (ReferenceEquals(node.Parent, _sentinel))
        {
            _root = pivot;
        }
        else if (ReferenceEquals(node, node.Parent.Left))
        {
            node.Parent.Left = pivot;
        }
        else
        {
            node.Parent.Right = pivot;
        }

        pivot.Left = node;
        node.Parent = pivot;

        node.RecomputeAggregates(_sentinel);
        pivot.RecomputeAggregates(_sentinel);
        RecomputeMetadataUpwards(pivot.Parent);
    }

    private void RotateRight(PieceTreeNode node)
    {
        var pivot = node.Left;
        node.Left = pivot.Right;
        if (!ReferenceEquals(pivot.Right, _sentinel))
        {
            pivot.Right.Parent = node;
        }

        pivot.Parent = node.Parent;
        if (ReferenceEquals(node.Parent, _sentinel))
        {
            _root = pivot;
        }
        else if (ReferenceEquals(node, node.Parent.Right))
        {
            node.Parent.Right = pivot;
        }
        else
        {
            node.Parent.Left = pivot;
        }

        pivot.Right = node;
        node.Parent = pivot;

        node.RecomputeAggregates(_sentinel);
        pivot.RecomputeAggregates(_sentinel);
        RecomputeMetadataUpwards(pivot.Parent);
    }

    private void RecomputeMetadataUpwards(PieceTreeNode node)
    {
        while (!ReferenceEquals(node, _sentinel))
        {
            node.RecomputeAggregates(_sentinel);
            node = node.Parent;
        }
    }
}

internal sealed record PieceTreeSearchPlan(string QueryText);

internal readonly record struct NodeHit(PieceTreeNode Node, int Remainder, int NodeStartOffset);

internal sealed record PieceTreeSearchResult;
