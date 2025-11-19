using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Core;

internal sealed partial class PieceTreeModel
{
    public void Insert(int offset, string value)
    {
        _lastVisitedLine = default;
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (_eolNormalized)
        {
            if (_eol == "\n")
            {
                if (value.Contains('\r'))
                {
                    _eolNormalized = false;
                }
            }
            else if (_eol == "\r\n")
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] == '\r')
                    {
                        if (i + 1 < value.Length && value[i + 1] == '\n')
                        {
                            i++;
                        }
                        else
                        {
                            _eolNormalized = false;
                            break;
                        }
                    }
                    else if (value[i] == '\n')
                    {
                        _eolNormalized = false;
                        break;
                    }
                }
            }
        }
        
        if (!ReferenceEquals(_root, _sentinel))
        {
            var hit = NodeAt(offset);
            var node = hit.Node;
            var remainder = hit.Remainder;
            var nodeStartOffset = hit.NodeStartOffset;
            var piece = node.Piece;
            var bufferIndex = piece.BufferIndex;
            var insertPosInBuffer = PositionInBuffer(node, remainder);

            // Optimization: append to the last change buffer node
            // TODO: Implement _lastChangeBufferPos tracking for this optimization
            
            if (nodeStartOffset == offset)
            {
                InsertContentToNodeLeft(value, node);
                _searchCache.InvalidateFromOffset(offset);
                ValidateCRLFWithPrevNode(node);
            }
            else if (nodeStartOffset + piece.Length > offset)
            {
                // Insert into middle
                var nodesToDel = new List<PieceTreeNode>();
                
                // Split node: [Start, InsertPos] ... [InsertPos, End]
                // We will reuse 'node' for the left part, and insert new node for right part.
                
                var newRightPiece = new PieceSegment(
                    piece.BufferIndex,
                    insertPosInBuffer,
                    piece.End,
                    GetLineFeedCnt(piece.BufferIndex, insertPosInBuffer, piece.End),
                    OffsetInBuffer(bufferIndex, piece.End) - OffsetInBuffer(bufferIndex, insertPosInBuffer)
                );

                if (EndWithCR(node.Piece) && StartWithLF(newRightPiece))
                {
                    ValidateCRLFWithPrevNode(node); // This might be wrong place or logic.
                    // Actually, we are splitting 'node'.
                    // 'node' becomes left part. 'newRightPiece' becomes right part.
                    // If original 'node' had \r\n split, it was already handled?
                    // No, we are splitting.
                    // If we split "A\r\nB" at \r|\n.
                    // Left: "A\r". Right: "\nB".
                    // Left LF: 1. Right LF: 1. Total 2.
                    // We need to fix it.
                    // ValidateCRLFWithPrevNode(newRightNode) would fix it.
                }

                DeleteNodeTail(node, insertPosInBuffer);

                var newPieces = CreateNewPieces(value);
                if (newRightPiece.Length > 0)
                {
                    RbInsertRight(node, newRightPiece);
                }

                var tmpNode = node;
                foreach (var p in newPieces)
                {
                    tmpNode = RbInsertRight(tmpNode, p);
                }
                
                DeleteNodes(nodesToDel);
                
                // Validate CRLF after insertions
                // We inserted newRightPiece (maybe) and newPieces.
                // We need to validate boundaries.
                
                // 1. node vs first inserted piece (or newRightPiece if no newPieces? No, newPieces always has at least 1).
                // Wait, newPieces comes from 'value'.
                // We insert newRightPiece FIRST (to the right of node).
                // Then we insert newPieces to the right of node (pushing newRightPiece further right? No).
                // RbInsertRight(node, newRightPiece) -> node.Right = newRightPiece.
                // Then RbInsertRight(tmpNode, p). tmpNode starts as node.
                // So we insert p1 right of node.
                // Then p2 right of p1.
                // So order: node -> p1 -> p2 -> ... -> newRightPiece?
                // No. RbInsertRight inserts as immediate right child or successor?
                // RbInsertRight(node, p) inserts p such that it follows node in in-order.
                
                // If we do:
                // RbInsertRight(node, newRightPiece);
                // tmpNode = node;
                // RbInsertRight(tmpNode, p); -> inserts p right of node.
                // So p comes between node and newRightPiece?
                // Yes.
                
                // So order: node -> newPieces -> newRightPiece.
                
                // We need to validate:
                // node vs newPieces[0]
                // newPieces[last] vs newRightPiece
                
                // But we iterate newPieces and insert them.
                // tmpNode tracks the last inserted node.
                
                // Let's look at the loop:
                // tmpNode = node;
                // foreach (var p in newPieces) { tmpNode = RbInsertRight(tmpNode, p); }
                // So tmpNode becomes the last inserted piece from value.
                
                // Wait, where is newRightPiece?
                // We inserted it first: RbInsertRight(node, newRightPiece).
                // So node -> newRightPiece.
                // Then we insert p1 right of node.
                // node -> p1 -> newRightPiece.
                // Then p2 right of p1.
                // node -> p1 -> p2 -> newRightPiece.
                
                // So correct order.
                
                // Validation:
                // 1. node vs newPieces[0] (which is node.Next())
                // 2. newPieces[last] vs newRightPiece (which is newPieces[last].Next())
                
                // We can just call ValidateCRLFWithPrevNode on the nodes we inserted + newRightPiece.
                
                // Actually, simpler:
                // After all insertions:
                // ValidateCRLFWithPrevNode(firstInsertedNode);
                // ...
                // ValidateCRLFWithPrevNode(newRightPieceNode);
                
                // But we don't have easy access to them unless we track them.
                // But we know the structure.
                
                // Let's just call ValidateCRLFWithPrevNode on the specific boundaries.
                
                // Boundary 1: node | next (first new piece)
                ValidateCRLFWithPrevNode(node.Next());
                
                // Boundary 2: last new piece | next (newRightPiece)
                // tmpNode is the last new piece.
                ValidateCRLFWithPrevNode(tmpNode.Next());
            }
            else
            {
                InsertContentToNodeRight(value, node);
                ValidateCRLFWithPrevNode(node.Next());
            }
        }
        else
        {
            // Insert new node
            var pieces = CreateNewPieces(value);
            var node = RbInsertLeft(_sentinel, pieces[0]);
            for (int k = 1; k < pieces.Count; k++)
            {
                node = RbInsertRight(node, pieces[k]);
            }
        }

        // RecomputeMetadataUpwards(_root); // Handled by RbInsert
    }

    public void Delete(int offset, int cnt)
    {
        _lastVisitedLine = default;
        if (cnt <= 0 || ReferenceEquals(_root, _sentinel))
        {
            return;
        }

        var startHit = NodeAt(offset);
        var endHit = NodeAt(offset + cnt);
        var startNode = startHit.Node;
        var endNode = endHit.Node;

        if (ReferenceEquals(startNode, endNode))
        {
            var startSplitPos = PositionInBuffer(startNode, startHit.Remainder);
            var endSplitPos = PositionInBuffer(startNode, endHit.Remainder);

            if (startHit.NodeStartOffset == offset)
            {
                if (cnt == startNode.Piece.Length)
                {
                    RbDelete(startNode);
                    return;
                }
                DeleteNodeHead(startNode, endSplitPos);
                _searchCache.InvalidateFromOffset(offset);
                return;
            }

            if (startHit.NodeStartOffset + startNode.Piece.Length == offset + cnt)
            {
                DeleteNodeTail(startNode, startSplitPos);
                return;
            }

            ShrinkNode(startNode, startSplitPos, endSplitPos);
            return;
        }

        var nodesToDel = new List<PieceTreeNode>();
        var startSplitPosInBuffer = PositionInBuffer(startNode, startHit.Remainder);
        DeleteNodeTail(startNode, startSplitPosInBuffer);
        _searchCache.InvalidateFromOffset(offset);
        if (startNode.Piece.Length == 0)
        {
            nodesToDel.Add(startNode);
        }

        var endSplitPosInBuffer = PositionInBuffer(endNode, endHit.Remainder);
        DeleteNodeHead(endNode, endSplitPosInBuffer);
        if (endNode.Piece.Length == 0)
        {
            nodesToDel.Add(endNode);
        }

        var secondNode = startNode.Next();
        while (!ReferenceEquals(secondNode, _sentinel) && !ReferenceEquals(secondNode, endNode))
        {
            nodesToDel.Add(secondNode);
            secondNode = secondNode.Next();
        }

        DeleteNodes(nodesToDel);
    }

    private void InsertContentToNodeLeft(string value, PieceTreeNode node)
    {
        // TODO: CRLF checks
        var newPieces = CreateNewPieces(value);
        var newNode = RbInsertLeft(node, newPieces[newPieces.Count - 1]);
        for (int k = newPieces.Count - 2; k >= 0; k--)
        {
            newNode = RbInsertLeft(newNode, newPieces[k]);
        }
    }

    private void InsertContentToNodeRight(string value, PieceTreeNode node)
    {
        // TODO: CRLF checks
        var newPieces = CreateNewPieces(value);
        var newNode = RbInsertRight(node, newPieces[0]);
        var tmpNode = newNode;
        for (int k = 1; k < newPieces.Count; k++)
        {
            tmpNode = RbInsertRight(tmpNode, newPieces[k]);
        }
    }

    private BufferCursor PositionInBuffer(PieceTreeNode node, int remainder)
    {
        var piece = node.Piece;
        var lineStarts = _buffers[piece.BufferIndex].LineStarts;
        var startOffset = lineStarts[piece.Start.Line] + piece.Start.Column;
        var targetOffset = startOffset + remainder;

        var lineIndex = FindLineIndex(lineStarts, targetOffset);
        var column = targetOffset - lineStarts[lineIndex];
        return new BufferCursor(lineIndex, column);
    }

    private int OffsetInBuffer(int bufferIndex, BufferCursor cursor)
    {
        var lineStarts = _buffers[bufferIndex].LineStarts;
        return lineStarts[cursor.Line] + cursor.Column;
    }

    private int GetLineFeedCnt(int bufferIndex, BufferCursor start, BufferCursor end)
    {
        return end.Line - start.Line;
    }

    private void ValidateCRLFWithPrevNode(PieceTreeNode nextNode)
    {
        if (ReferenceEquals(nextNode, _sentinel))
        {
            return;
        }

        var prevNode = nextNode.Prev();
        if (ReferenceEquals(prevNode, _sentinel))
        {
            return;
        }

        var prevPiece = prevNode.Piece;
        var piece = nextNode.Piece;

        if (EndWithCR(prevPiece) && StartWithLF(piece))
        {
            var newPrevPiece = prevPiece with { LineFeedCount = prevPiece.LineFeedCount - 1 };
            prevNode.Piece = newPrevPiece;
            RecomputeMetadataUpwards(prevNode);
        }
        else
        {
             // throw new Exception($"Validation failed. Prev: {prevPiece}, Curr: {piece}. EndWithCR: {EndWithCR(prevPiece)}, StartWithLF: {StartWithLF(piece)}");
        }
    }

    private bool EndWithCR(PieceSegment piece)
    {
        var buffer = _buffers[piece.BufferIndex];
        var endOffset = OffsetInBuffer(piece.BufferIndex, piece.End);
        if (endOffset == 0) return false;
        char ch = buffer.Buffer[endOffset - 1];
        return ch == '\r';
    }

    private bool StartWithLF(PieceSegment piece)
    {
        var buffer = _buffers[piece.BufferIndex];
        var startOffset = OffsetInBuffer(piece.BufferIndex, piece.Start);
        if (startOffset >= buffer.Buffer.Length) return false;
        return buffer.Buffer[startOffset] == '\n';
    }

    private void DeleteNodeTail(PieceTreeNode node, BufferCursor pos)
    {
        var piece = node.Piece;
        var originalLFCnt = piece.LineFeedCount;
        var originalEndOffset = OffsetInBuffer(piece.BufferIndex, piece.End);
        
        var newEnd = pos;
        var newEndOffset = OffsetInBuffer(piece.BufferIndex, newEnd);
        var newLineFeedCnt = GetLineFeedCnt(piece.BufferIndex, piece.Start, newEnd);
        
        var lf_delta = newLineFeedCnt - originalLFCnt;
        var size_delta = newEndOffset - originalEndOffset;
        var newLength = piece.Length + size_delta;
        
        node.Piece = new PieceSegment(piece.BufferIndex, piece.Start, newEnd, newLineFeedCnt, newLength);
        UpdateTreeMetadata(node, size_delta, lf_delta);
    }

    private void DeleteNodeHead(PieceTreeNode node, BufferCursor pos)
    {
        var piece = node.Piece;
        var originalLFCnt = piece.LineFeedCount;
        var originalStartOffset = OffsetInBuffer(piece.BufferIndex, piece.Start);
        
        var newStart = pos;
        var newLineFeedCnt = GetLineFeedCnt(piece.BufferIndex, newStart, piece.End);
        var newStartOffset = OffsetInBuffer(piece.BufferIndex, newStart);
        
        var lf_delta = newLineFeedCnt - originalLFCnt;
        var size_delta = originalStartOffset - newStartOffset;
        var newLength = piece.Length + size_delta;
        
        node.Piece = new PieceSegment(piece.BufferIndex, newStart, piece.End, newLineFeedCnt, newLength);
        UpdateTreeMetadata(node, size_delta, lf_delta);
    }

    private void ShrinkNode(PieceTreeNode node, BufferCursor start, BufferCursor end)
    {
        var piece = node.Piece;
        var originalStart = piece.Start;
        var originalEnd = piece.End;
        
        var oldLength = piece.Length;
        var oldLFCnt = piece.LineFeedCount;
        
        var newEnd = start;
        var newLineFeedCnt = GetLineFeedCnt(piece.BufferIndex, piece.Start, newEnd);
        var newLength = OffsetInBuffer(piece.BufferIndex, start) - OffsetInBuffer(piece.BufferIndex, originalStart);
        
        node.Piece = new PieceSegment(piece.BufferIndex, piece.Start, newEnd, newLineFeedCnt, newLength);
        UpdateTreeMetadata(node, newLength - oldLength, newLineFeedCnt - oldLFCnt);
        
        var newPiece = new PieceSegment(
            piece.BufferIndex,
            end,
            originalEnd,
            GetLineFeedCnt(piece.BufferIndex, end, originalEnd),
            OffsetInBuffer(piece.BufferIndex, originalEnd) - OffsetInBuffer(piece.BufferIndex, end)
        );
        
        RbInsertRight(node, newPiece);
    }

    private void DeleteNodes(List<PieceTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            RbDelete(node);
        }
    }

    private List<PieceSegment> CreateNewPieces(string text)
    {
        // TODO: Handle large text splitting (AverageBufferSize)
        var bufferIndex = _buffers.Count; // New buffer index? 
        // Wait, we should reuse buffer 0 (ChangeBuffer) if possible, or add new buffer.
        // For now, let's just add a new buffer for every insert (inefficient but correct for porting logic).
        // TS implementation tries to append to buffer 0.
        
        var chunkBuffer = ChunkBuffer.FromText(text);
        _buffers.Add(chunkBuffer);
        
        var piece = new PieceSegment(
            bufferIndex,
            BufferCursor.Zero,
            chunkBuffer.CreateEndCursor(),
            chunkBuffer.LineFeedCount,
            chunkBuffer.Length
        );
        
        return new List<PieceSegment> { piece };
    }

    private PieceTreeNode RbInsertRight(PieceTreeNode node, PieceSegment p)
    {
        var z = new PieceTreeNode(p, NodeColor.Red);
        z.Left = _sentinel;
        z.Right = _sentinel;
        z.Parent = _sentinel;
        z.SizeLeft = 0;
        z.LineFeedsLeft = 0;

        if (ReferenceEquals(_root, _sentinel))
        {
            _root = z;
            z.Color = NodeColor.Black;
        }
        else if (ReferenceEquals(node.Right, _sentinel))
        {
            node.Right = z;
            z.Parent = node;
        }
        else
        {
            var nextNode = Leftest(node.Right);
            nextNode.Left = z;
            z.Parent = nextNode;
        }

        InsertFixup(z);
        RecomputeMetadataUpwards(z);
        return z;
    }

    private PieceTreeNode RbInsertLeft(PieceTreeNode node, PieceSegment p)
    {
        var z = new PieceTreeNode(p, NodeColor.Red);
        z.Left = _sentinel;
        z.Right = _sentinel;
        z.Parent = _sentinel;
        z.SizeLeft = 0;
        z.LineFeedsLeft = 0;

        if (ReferenceEquals(_root, _sentinel))
        {
            _root = z;
            z.Color = NodeColor.Black;
        }
        else if (ReferenceEquals(node.Left, _sentinel))
        {
            node.Left = z;
            z.Parent = node;
        }
        else
        {
            var prevNode = Rightest(node.Left);
            prevNode.Right = z;
            z.Parent = prevNode;
        }

        InsertFixup(z);
        RecomputeMetadataUpwards(z);
        return z;
    }

    private void RbDelete(PieceTreeNode z)
    {
        PieceTreeNode x, y;

        if (ReferenceEquals(z.Left, _sentinel))
        {
            y = z;
            x = y.Right;
        }
        else if (ReferenceEquals(z.Right, _sentinel))
        {
            y = z;
            x = y.Left;
        }
        else
        {
            y = Leftest(z.Right);
            x = y.Right;
        }

        if (ReferenceEquals(y, _root))
        {
            _root = x;
            x.Color = NodeColor.Black;
            z.Detach();
            _sentinel.Parent = _sentinel; // Reset sentinel
            _root.Parent = _sentinel;
            return;
        }

        var yWasRed = (y.Color == NodeColor.Red);

        if (ReferenceEquals(y, y.Parent.Left))
        {
            y.Parent.Left = x;
        }
        else
        {
            y.Parent.Right = x;
        }

        if (ReferenceEquals(y, z))
        {
            x.Parent = y.Parent;
            RecomputeMetadataUpwards(x);
        }
        else
        {
            if (ReferenceEquals(y.Parent, z))
            {
                x.Parent = y;
            }
            else
            {
                x.Parent = y.Parent;
            }

            RecomputeMetadataUpwards(x);

            y.Left = z.Left;
            y.Right = z.Right;
            y.Parent = z.Parent;
            y.Color = z.Color;

            if (ReferenceEquals(z, _root))
            {
                _root = y;
            }
            else
            {
                if (ReferenceEquals(z, z.Parent.Left))
                {
                    z.Parent.Left = y;
                }
                else
                {
                    z.Parent.Right = y;
                }
            }

            if (!ReferenceEquals(y.Left, _sentinel))
            {
                y.Left.Parent = y;
            }
            if (!ReferenceEquals(y.Right, _sentinel))
            {
                y.Right.Parent = y;
            }

            y.SizeLeft = z.SizeLeft;
            y.LineFeedsLeft = z.LineFeedsLeft;
            RecomputeMetadataUpwards(y);
        }

        z.Detach();

        if (ReferenceEquals(x.Parent.Left, x))
        {
            var newSizeLeft = CalculateSize(x);
            var newLFLeft = CalculateLF(x);
            if (newSizeLeft != x.Parent.SizeLeft || newLFLeft != x.Parent.LineFeedsLeft)
            {
                var delta = newSizeLeft - x.Parent.SizeLeft;
                var lf_delta = newLFLeft - x.Parent.LineFeedsLeft;
                x.Parent.SizeLeft = newSizeLeft;
                x.Parent.LineFeedsLeft = newLFLeft;
                UpdateTreeMetadata(x.Parent, delta, lf_delta);
            }
        }

        RecomputeMetadataUpwards(x.Parent);

        if (yWasRed)
        {
            _sentinel.Parent = _sentinel;
            return;
        }

        DeleteFixup(x);
        _sentinel.Parent = _sentinel;
    }

    private void DeleteFixup(PieceTreeNode x)
    {
        while (!ReferenceEquals(x, _root) && x.Color == NodeColor.Black)
        {
            if (ReferenceEquals(x, x.Parent.Left))
            {
                var w = x.Parent.Right;
                if (w.Color == NodeColor.Red)
                {
                    w.Color = NodeColor.Black;
                    x.Parent.Color = NodeColor.Red;
                    RotateLeft(x.Parent);
                    w = x.Parent.Right;
                }

                if (w.Left.Color == NodeColor.Black && w.Right.Color == NodeColor.Black)
                {
                    w.Color = NodeColor.Red;
                    x = x.Parent;
                }
                else
                {
                    if (w.Right.Color == NodeColor.Black)
                    {
                        w.Left.Color = NodeColor.Black;
                        w.Color = NodeColor.Red;
                        RotateRight(w);
                        w = x.Parent.Right;
                    }

                    w.Color = x.Parent.Color;
                    x.Parent.Color = NodeColor.Black;
                    w.Right.Color = NodeColor.Black;
                    RotateLeft(x.Parent);
                    x = _root;
                }
            }
            else
            {
                var w = x.Parent.Left;
                if (w.Color == NodeColor.Red)
                {
                    w.Color = NodeColor.Black;
                    x.Parent.Color = NodeColor.Red;
                    RotateRight(x.Parent);
                    w = x.Parent.Left;
                }

                if (w.Left.Color == NodeColor.Black && w.Right.Color == NodeColor.Black)
                {
                    w.Color = NodeColor.Red;
                    x = x.Parent;
                }
                else
                {
                    if (w.Left.Color == NodeColor.Black)
                    {
                        w.Right.Color = NodeColor.Black;
                        w.Color = NodeColor.Red;
                        RotateLeft(w);
                        w = x.Parent.Left;
                    }

                    w.Color = x.Parent.Color;
                    x.Parent.Color = NodeColor.Black;
                    w.Left.Color = NodeColor.Black;
                    RotateRight(x.Parent);
                    x = _root;
                }
            }
        }
        x.Color = NodeColor.Black;
    }

    private void UpdateTreeMetadata(PieceTreeNode x, int delta, int lf_delta)
    {
        while (!ReferenceEquals(x, _root) && !ReferenceEquals(x, _sentinel))
        {
            if (ReferenceEquals(x.Parent.Left, x))
            {
                x.Parent.SizeLeft += delta;
                x.Parent.LineFeedsLeft += lf_delta;
            }
            x = x.Parent;
        }
    }

    private PieceTreeNode Leftest(PieceTreeNode node)
    {
        while (!ReferenceEquals(node.Left, _sentinel))
        {
            node = node.Left;
        }
        return node;
    }

    private PieceTreeNode Rightest(PieceTreeNode node)
    {
        while (!ReferenceEquals(node.Right, _sentinel))
        {
            node = node.Right;
        }
        return node;
    }

    private int CalculateSize(PieceTreeNode node)
    {
        if (ReferenceEquals(node, _sentinel)) return 0;
        return node.SizeLeft + node.Piece.Length + CalculateSize(node.Right);
    }

    private int CalculateLF(PieceTreeNode node)
    {
        if (ReferenceEquals(node, _sentinel)) return 0;
        return node.LineFeedsLeft + node.Piece.LineFeedCount + CalculateLF(node.Right);
    }

    private static int FindLineIndex(IReadOnlyList<int> lineStarts, int offset)
    {
        var low = 0;
        var high = lineStarts.Count - 1;
        while (low <= high)
        {
            var mid = low + ((high - low) / 2);
            var start = lineStarts[mid];
            if (mid == lineStarts.Count - 1)
            {
                return mid;
            }

            var next = lineStarts[mid + 1];
            if (offset < start)
            {
                high = mid - 1;
            }
            else if (offset >= next)
            {
                low = mid + 1;
            }
            else
            {
                return mid;
            }
        }
        return Math.Max(0, Math.Min(lineStarts.Count - 1, low));
    }
}
