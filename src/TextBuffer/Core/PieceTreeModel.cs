// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Class: PieceTreeBase (Lines: 268-1882)
// Ported: 2025-11-19

using System.Text;

namespace PieceTree.TextBuffer.Core;

internal sealed partial class PieceTreeModel
{
    private const string SearchNotImplementedMessage = "PieceTreeModel search stub is not implemented (PT-004 placeholder).";

    public const int ChangeBufferId = 0;
    private const int AverageBufferSize = ChunkUtilities.DefaultChunkSize;

    private readonly PieceTreeNode _sentinel = PieceTreeNode.CreateSentinel();
    private readonly PieceTreeSearchCache _searchCache = new();
    private struct LastVisitedLine { public int LineNumber; public string Value; }
    private LastVisitedLine _lastVisitedLine;
    private BufferCursor _lastChangeBufferPos;
    private int _lastChangeBufferOffset;
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
        _lastChangeBufferPos = BufferCursor.Zero;
        _lastChangeBufferOffset = _buffers.Count > ChangeBufferId ? _buffers[ChangeBufferId].Length : 0;
        Diagnostics = new DiagnosticsView(this);
    }

    public sealed class DiagnosticsView
    {
        private readonly PieceTreeModel _owner;

        internal DiagnosticsView(PieceTreeModel owner)
        {
            _owner = owner;
        }

        public SearchCacheSnapshot SearchCache => _owner._searchCache.Snapshot;
    }

    public PieceTreeNode Root => _root;

    public int PieceCount => _count;

    internal IReadOnlyList<ChunkBuffer> Buffers => _buffers;

    internal PieceTreeNode Sentinel => _sentinel;

    public int TotalLength => ReferenceEquals(_root, _sentinel) ? 0 : _root.AggregatedLength;

    public int TotalLineFeeds => ReferenceEquals(_root, _sentinel) ? 0 : _root.AggregatedLineFeeds;

    public bool IsEmpty => ReferenceEquals(_root, _sentinel);

    internal PieceTreeSearchCache SearchCache => _searchCache;

    public DiagnosticsView Diagnostics { get; }

    public string Eol => _eol;

    public void NormalizeEOL(string eol)
    {
        _eol = eol;
        int averageBufferSize = 65536;
        StringBuilder sb = new();
        List<string> chunks = [];
        bool skipNextLF = false;

        foreach (PieceSegment piece in EnumeratePiecesInOrder())
        {
            ChunkBuffer buffer = _buffers[piece.BufferIndex];
            string text = buffer.Slice(piece.Start, piece.End);

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
            _searchCache.Clear();
            _buffers.Clear();
            _buffers.Add(ChunkBuffer.Empty);

            foreach (string chunk in chunks)
            {
                ChunkBuffer chunkBuffer = ChunkBuffer.FromText(chunk);
                _buffers.Add(chunkBuffer);
                int bufferIndex = _buffers.Count - 1;
                PieceSegment piece = new(
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
        // Reset change buffer write pointer when normalizing EOLs
        _lastChangeBufferPos = BufferCursor.Zero;
        _lastChangeBufferOffset = 0;
    }

    private IEnumerable<PieceTreeNode> EnumerateNodesPostOrder()
    {
        if (ReferenceEquals(_root, _sentinel))
        {
            yield break;
        }

        Stack<PieceTreeNode> stack = new();
        PieceTreeNode visited = _sentinel;
        PieceTreeNode current = _root;
        while (!ReferenceEquals(current, _sentinel) || stack.Count > 0)
        {
            if (!ReferenceEquals(current, _sentinel))
            {
                stack.Push(current);
                current = current.Left;
            }
            else
            {
                PieceTreeNode peek = stack.Peek();
                if (!ReferenceEquals(peek.Right, _sentinel) && !ReferenceEquals(peek.Right, visited))
                {
                    current = peek.Right;
                }
                else
                {
                    stack.Pop();
                    yield return peek;
                    visited = peek;
                }
            }
        }
    }

    private void ComputeBufferMetadata()
    {
        PieceTreeDebug.Log($"DEBUG ComputeBufferMetadata START: TotalLength={TotalLength}, TotalLineFeeds={TotalLineFeeds}");

        foreach (PieceTreeNode node in EnumerateNodesPostOrder())
        {
            PieceSegment normalizedPiece = NormalizePiece(node.Piece);
            if (!node.Piece.Equals(normalizedPiece))
            {
                node.Piece = normalizedPiece;
            }

            node.RecomputeAggregates(_sentinel);
        }

        _searchCache.Validate(GetOffsetOfNode, TotalLength);
        PieceTreeDebug.Log($"DEBUG ComputeBufferMetadata END: TotalLength={TotalLength}, TotalLineFeeds={TotalLineFeeds}");
    }

    private PieceSegment NormalizePiece(PieceSegment piece)
    {
        if (piece.Length == 0 && piece.LineFeedCount == 0)
        {
            return piece;
        }

        PieceSegment normalized = CreateSegment(piece.BufferIndex, piece.Start, piece.End);
        if (normalized.Length != piece.Length || normalized.LineFeedCount != piece.LineFeedCount)
        {
            PieceTreeDebug.Log($"NormalizePiece: BufIdx={piece.BufferIndex}, oldLen={piece.Length}, newLen={normalized.Length}, oldLF={piece.LineFeedCount}, newLF={normalized.LineFeedCount}");
            return normalized;
        }

        return piece;
    }

    public PieceTreeNode InsertPieceAtEnd(PieceSegment piece)
    {
        int insertionOffset = TotalLength;
        _searchCache.InvalidateFromOffset(insertionOffset);

        PieceTreeNode node = new(piece, _sentinel);
        node.ResetLinks();

        PieceTreeNode parent = _sentinel;
        PieceTreeNode current = _root;
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
        PieceTreeNode x = _root;
        if (_searchCache.TryGetByOffset(offset, out PieceTreeNode? cachedNode, out int cachedStartOffset))
        {
            return new NodeHit(cachedNode, offset - cachedStartOffset, cachedStartOffset, 0);
        }

        int nodeStartOffset = 0;

        while (!ReferenceEquals(x, _sentinel))
        {
            if (x.SizeLeft > offset)
            {
                x = x.Left;
            }
            else if (x.SizeLeft + x.Piece.Length >= offset)
            {
                nodeStartOffset += x.SizeLeft;
                NodeHit hit = new(x, offset - x.SizeLeft, nodeStartOffset, 0);
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
        foreach (PieceTreeNode node in EnumerateNodesInOrder())
        {
            yield return node.Piece;
        }
    }

    internal string GetNearestChunk(int offset)
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        offset = Math.Clamp(offset, 0, TotalLength);
        NodeHit hit = NodeAt(offset);
        if (hit == default || ReferenceEquals(hit.Node, _sentinel))
        {
            return string.Empty;
        }

        if (hit.Remainder == hit.Node.Piece.Length)
        {
            PieceTreeNode? next = hit.Node.Next();
            if (ReferenceEquals(next, _sentinel) || next is null)
            {
                return string.Empty;
            }

            ChunkBuffer buffer = _buffers[next.Piece.BufferIndex];
            return buffer.Slice(next.Piece.Start, next.Piece.End);
        }

        BufferCursor sliceStart = hit.Remainder == 0
            ? hit.Node.Piece.Start
            : PositionInBuffer(hit.Node, hit.Remainder);

        if (sliceStart.Equals(hit.Node.Piece.End))
        {
            return string.Empty;
        }

        ChunkBuffer currentBuffer = _buffers[hit.Node.Piece.BufferIndex];
        return currentBuffer.Slice(sliceStart, hit.Node.Piece.End);
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

        Stack<PieceTreeNode> stack = new();
        PieceTreeNode current = _root;
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

        int offset = node.SizeLeft;
        PieceTreeNode current = node;
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

        int lineFeeds = node.LineFeedsLeft;
        PieceTreeNode current = node;
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
                PieceTreeNode uncle = node.Parent.Parent.Right;
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
                PieceTreeNode uncle = node.Parent.Parent.Left;
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
        PieceTreeNode pivot = node.Right;
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
        PieceTreeNode pivot = node.Left;
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

    internal void AssertPieceIntegrity()
    {
        ValidatePieceMetadata();
        ValidateTreeInvariants();
        _searchCache.Validate(GetOffsetOfNode, TotalLength);
    }

    private void ValidatePieceMetadata()
    {
        int aggregatedLength = 0;
        int aggregatedLineFeeds = 0;

        foreach (PieceSegment piece in EnumeratePiecesInOrder())
        {
            ChunkBuffer buffer = _buffers[piece.BufferIndex];
            string textSlice = buffer.Slice(piece.Start, piece.End);
            if (textSlice.Length != piece.Length)
            {
                throw new InvalidOperationException($"Piece metadata mismatch: expected length {textSlice.Length} but recorded {piece.Length} for buffer {piece.BufferIndex}.");
            }

            int computedLineFeeds = GetLineFeedCnt(piece.BufferIndex, piece.Start, piece.End);
            if (computedLineFeeds != piece.LineFeedCount)
            {
                throw new InvalidOperationException($"Piece line feed mismatch: expected {computedLineFeeds} but recorded {piece.LineFeedCount} for buffer {piece.BufferIndex}.");
            }

            aggregatedLength += piece.Length;
            aggregatedLineFeeds += piece.LineFeedCount;
        }

        if (aggregatedLength != TotalLength)
        {
            throw new InvalidOperationException($"TotalLength mismatch: aggregated pieces = {aggregatedLength}, tree metadata = {TotalLength}.");
        }

        if (aggregatedLineFeeds != TotalLineFeeds)
        {
            throw new InvalidOperationException($"TotalLineFeeds mismatch: aggregated pieces = {aggregatedLineFeeds}, tree metadata = {TotalLineFeeds}.");
        }
    }

    private void ValidateTreeInvariants()
    {
        if (_sentinel.Color != NodeColor.Black
            || !ReferenceEquals(_sentinel.Left, _sentinel)
            || !ReferenceEquals(_sentinel.Right, _sentinel)
            || !ReferenceEquals(_sentinel.Parent, _sentinel))
        {
            throw new InvalidOperationException("Sentinel invariants violated: sentinel must remain black and self-referential.");
        }

        if (_sentinel.SizeLeft != 0 || _sentinel.LineFeedsLeft != 0 || _sentinel.AggregatedLength != 0 || _sentinel.AggregatedLineFeeds != 0)
        {
            throw new InvalidOperationException($"Sentinel metadata must remain zero (sizeLeft={_sentinel.SizeLeft}, lfLeft={_sentinel.LineFeedsLeft}, aggLen={_sentinel.AggregatedLength}, aggLf={_sentinel.AggregatedLineFeeds}).");
        }

        if (ReferenceEquals(_root, _sentinel))
        {
            return;
        }

        if (!ReferenceEquals(_root.Parent, _sentinel))
        {
            throw new InvalidOperationException("Root parent must point to sentinel.");
        }

        if (_root.Color != NodeColor.Black)
        {
            throw new InvalidOperationException("Root must be black (TS assertTreeInvariants parity).");
        }

        _ = AssertNodeInvariants(_root);
    }

    private int AssertNodeInvariants(PieceTreeNode node)
    {
        if (ReferenceEquals(node, _sentinel))
        {
            return 1;
        }

        if (!ReferenceEquals(node.Left, _sentinel) && !ReferenceEquals(node.Left.Parent, node))
        {
            throw new InvalidOperationException($"Left parent mismatch at {DescribeNode(node)}.");
        }

        if (!ReferenceEquals(node.Right, _sentinel) && !ReferenceEquals(node.Right.Parent, node))
        {
            throw new InvalidOperationException($"Right parent mismatch at {DescribeNode(node)}.");
        }

        if (node.Color == NodeColor.Red)
        {
            if (node.Left.Color != NodeColor.Black || node.Right.Color != NodeColor.Black)
            {
                throw new InvalidOperationException($"Red node must have black children ({DescribeNode(node)}).");
            }
        }

        int leftBlackHeight = AssertNodeInvariants(node.Left);
        int rightBlackHeight = AssertNodeInvariants(node.Right);
        if (leftBlackHeight != rightBlackHeight)
        {
            throw new InvalidOperationException($"Black height mismatch at {DescribeNode(node)} (left={leftBlackHeight}, right={rightBlackHeight}).");
        }

        int leftAggregatedLength = ReferenceEquals(node.Left, _sentinel) ? 0 : node.Left.AggregatedLength;
        if (node.SizeLeft != leftAggregatedLength)
        {
            throw new InvalidOperationException($"SizeLeft mismatch at {DescribeNode(node)} expected={leftAggregatedLength} actual={node.SizeLeft}.");
        }

        int leftAggregatedLineFeeds = ReferenceEquals(node.Left, _sentinel) ? 0 : node.Left.AggregatedLineFeeds;
        if (node.LineFeedsLeft != leftAggregatedLineFeeds)
        {
            throw new InvalidOperationException($"LineFeedsLeft mismatch at {DescribeNode(node)} expected={leftAggregatedLineFeeds} actual={node.LineFeedsLeft}.");
        }

        int rightAggregatedLength = ReferenceEquals(node.Right, _sentinel) ? 0 : node.Right.AggregatedLength;
        int expectedAggregatedLength = leftAggregatedLength + node.Piece.Length + rightAggregatedLength;
        if (node.AggregatedLength != expectedAggregatedLength)
        {
            throw new InvalidOperationException($"AggregatedLength mismatch at {DescribeNode(node)} expected={expectedAggregatedLength} actual={node.AggregatedLength}.");
        }

        int rightAggregatedLineFeeds = ReferenceEquals(node.Right, _sentinel) ? 0 : node.Right.AggregatedLineFeeds;
        int expectedAggregatedLineFeeds = leftAggregatedLineFeeds + node.Piece.LineFeedCount + rightAggregatedLineFeeds;
        if (node.AggregatedLineFeeds != expectedAggregatedLineFeeds)
        {
            throw new InvalidOperationException($"AggregatedLineFeeds mismatch at {DescribeNode(node)} expected={expectedAggregatedLineFeeds} actual={node.AggregatedLineFeeds}.");
        }

        return leftBlackHeight + (node.Color == NodeColor.Black ? 1 : 0);
    }

    private static string DescribeNode(PieceTreeNode node)
    {
        PieceSegment piece = node.Piece;
        return $"buf={piece.BufferIndex} start=({piece.Start.Line},{piece.Start.Column}) end=({piece.End.Line},{piece.End.Column}) len={piece.Length}";
    }
}

internal sealed record PieceTreeSearchPlan(string QueryText);

internal readonly record struct NodeHit(PieceTreeNode Node, int Remainder, int NodeStartOffset, int NodeStartLineNumber);

internal sealed record PieceTreeSearchResult;
