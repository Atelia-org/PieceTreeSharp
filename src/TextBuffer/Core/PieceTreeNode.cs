// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/rbTreeBase.ts
// - Class: TreeNode (Lines: 8-425)
// Ported: 2025-11-19

using System;

namespace PieceTree.TextBuffer.Core;

internal enum NodeColor
{
    Red,
    Black
}

internal sealed class PieceTreeNode
{
    private readonly PieceTreeNode _sentinel;

    private PieceTreeNode()
    {
        Piece = PieceSegment.Empty;
        Color = NodeColor.Black;
        _sentinel = this;
        Parent = this;
        Left = this;
        Right = this;
        SizeLeft = 0;
        LineFeedsLeft = 0;
        AggregatedLength = 0;
        AggregatedLineFeeds = 0;
        IsDetached = false;
    }

    internal PieceTreeNode(PieceSegment piece, NodeColor color, PieceTreeNode sentinel)
    {
        ArgumentNullException.ThrowIfNull(sentinel);

        Piece = piece;
        Color = color;
        _sentinel = sentinel;
        Left = sentinel;
        Right = sentinel;
        Parent = sentinel;
        SizeLeft = 0;
        LineFeedsLeft = 0;
        AggregatedLength = piece.Length;
        AggregatedLineFeeds = piece.LineFeedCount;
        IsDetached = false;
    }

    internal PieceTreeNode(PieceSegment piece, PieceTreeNode sentinel)
        : this(piece, NodeColor.Red, sentinel)
    {
    }

    public PieceSegment Piece { get; set; }

    public PieceTreeNode Parent { get; set; }

    public PieceTreeNode Left { get; set; }

    public PieceTreeNode Right { get; set; }

    public NodeColor Color { get; set; }

    public int SizeLeft { get; set; }

    public int LineFeedsLeft { get; set; }

    public int AggregatedLength { get; private set; }

    public int AggregatedLineFeeds { get; private set; }

    internal bool IsDetached { get; private set; }

    internal bool IsSentinel => ReferenceEquals(this, _sentinel);

    internal PieceTreeNode Next()
    {
        var sentinel = _sentinel;

        if (!ReferenceEquals(Right, sentinel))
        {
            var node = Right;
            while (!ReferenceEquals(node.Left, sentinel))
            {
                node = node.Left;
            }
            return node;
        }

        var current = this;
        while (!ReferenceEquals(current.Parent, sentinel))
        {
            if (ReferenceEquals(current.Parent.Left, current))
            {
                break;
            }
            current = current.Parent;
        }

        if (ReferenceEquals(current.Parent, sentinel))
        {
            return sentinel;
        }
        return current.Parent;
    }

    internal PieceTreeNode Prev()
    {
        var sentinel = _sentinel;

        if (!ReferenceEquals(Left, sentinel))
        {
            var node = Left;
            while (!ReferenceEquals(node.Right, sentinel))
            {
                node = node.Right;
            }
            return node;
        }

        var current = this;
        while (!ReferenceEquals(current.Parent, sentinel))
        {
            if (ReferenceEquals(current.Parent.Right, current))
            {
                break;
            }
            current = current.Parent;
        }

        if (ReferenceEquals(current.Parent, sentinel))
        {
            return sentinel;
        }
        return current.Parent;
    }

    internal void Detach()
    {
        Parent = _sentinel;
        Left = _sentinel;
        Right = _sentinel;
        IsDetached = true;
    }

    internal static PieceTreeNode CreateSentinel() => new();

    internal void ResetLinks()
    {
        Parent = _sentinel;
        Left = _sentinel;
        Right = _sentinel;
        SizeLeft = 0;
        LineFeedsLeft = 0;
        AggregatedLength = Piece.Length;
        AggregatedLineFeeds = Piece.LineFeedCount;
        IsDetached = false;
    }

    internal void RecomputeAggregates(PieceTreeNode sentinel)
    {
        if (ReferenceEquals(this, sentinel))
        {
            return;
        }

        var leftLength = ReferenceEquals(Left, sentinel) ? 0 : Left.AggregatedLength;
        var leftLf = ReferenceEquals(Left, sentinel) ? 0 : Left.AggregatedLineFeeds;
        var rightLength = ReferenceEquals(Right, sentinel) ? 0 : Right.AggregatedLength;
        var rightLf = ReferenceEquals(Right, sentinel) ? 0 : Right.AggregatedLineFeeds;

        SizeLeft = leftLength;
        LineFeedsLeft = leftLf;
        AggregatedLength = leftLength + Piece.Length + rightLength;
        AggregatedLineFeeds = leftLf + Piece.LineFeedCount + rightLf;
    }
}
