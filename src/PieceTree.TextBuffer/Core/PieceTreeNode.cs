namespace PieceTree.TextBuffer.Core;

internal enum NodeColor
{
    Red,
    Black
}

internal sealed class PieceTreeNode
{
    internal PieceTreeNode(PieceSegment piece, NodeColor color)
    {
        Piece = piece;
        Color = color;
        Left = Sentinel;
        Right = Sentinel;
        Parent = Sentinel;
        SizeLeft = 0;
        LineFeedsLeft = 0;
        AggregatedLength = piece.Length;
        AggregatedLineFeeds = piece.LineFeedCount;
    }

    public PieceTreeNode(PieceSegment piece)
        : this(piece, NodeColor.Red)
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

    public static PieceTreeNode Sentinel { get; } = CreateSentinel();

    internal bool IsSentinel => ReferenceEquals(this, Sentinel);

    internal PieceTreeNode Next()
    {
        if (!ReferenceEquals(Right, Sentinel))
        {
            var node = Right;
            while (!ReferenceEquals(node.Left, Sentinel))
            {
                node = node.Left;
            }
            return node;
        }

        var current = this;
        while (!ReferenceEquals(current.Parent, Sentinel))
        {
            if (ReferenceEquals(current.Parent.Left, current))
            {
                break;
            }
            current = current.Parent;
        }

        if (ReferenceEquals(current.Parent, Sentinel))
        {
            return Sentinel;
        }
        return current.Parent;
    }

    internal PieceTreeNode Prev()
    {
        if (!ReferenceEquals(Left, Sentinel))
        {
            var node = Left;
            while (!ReferenceEquals(node.Right, Sentinel))
            {
                node = node.Right;
            }
            return node;
        }

        var current = this;
        while (!ReferenceEquals(current.Parent, Sentinel))
        {
            if (ReferenceEquals(current.Parent.Right, current))
            {
                break;
            }
            current = current.Parent;
        }

        if (ReferenceEquals(current.Parent, Sentinel))
        {
            return Sentinel;
        }
        return current.Parent;
    }

    internal void Detach()
    {
        Parent = Sentinel;
        Left = Sentinel;
        Right = Sentinel;
    }

    private static PieceTreeNode CreateSentinel()
    {
        var sentinel = new PieceTreeNode(PieceSegment.Empty, NodeColor.Black)
        {
            AggregatedLength = 0,
            AggregatedLineFeeds = 0,
            SizeLeft = 0,
            LineFeedsLeft = 0
        };

        sentinel.Parent = sentinel;
        sentinel.Left = sentinel;
        sentinel.Right = sentinel;
        return sentinel;
    }

    internal void ResetLinks()
    {
        Parent = Sentinel;
        Left = Sentinel;
        Right = Sentinel;
        SizeLeft = 0;
        LineFeedsLeft = 0;
        AggregatedLength = Piece.Length;
        AggregatedLineFeeds = Piece.LineFeedCount;
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
