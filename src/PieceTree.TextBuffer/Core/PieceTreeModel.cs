using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Core;

internal sealed class PieceTreeModel
{
    private const string SearchNotImplementedMessage = "PieceTreeModel search stub is not implemented (PT-004 placeholder).";

    public const int ChangeBufferId = 0;

    private readonly PieceTreeNode _sentinel = PieceTreeNode.Sentinel;
    private readonly PieceTreeSearchCache _searchCache = new();
    private PieceTreeNode _root;
    private int _count;

    public PieceTreeModel()
    {
        _root = _sentinel;
    }

    public PieceTreeNode Root => _root;

    public int PieceCount => _count;

    public int TotalLength => ReferenceEquals(_root, _sentinel) ? 0 : _root.AggregatedLength;

    public int TotalLineFeeds => ReferenceEquals(_root, _sentinel) ? 0 : _root.AggregatedLineFeeds;

    public bool IsEmpty => ReferenceEquals(_root, _sentinel);

    internal PieceTreeSearchCache SearchCache => _searchCache;

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

internal sealed record PieceTreeSearchResult;
