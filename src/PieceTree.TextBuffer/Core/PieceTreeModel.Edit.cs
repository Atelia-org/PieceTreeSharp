using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Core;

internal sealed partial class PieceTreeModel
{
    public void Insert(int offset, string value)
    {
        PieceTreeDebug.Log($"DEBUG Insert: offset={offset}, value='{value?.Replace("\n","\\n")?.Replace("\r","\\r")}'");
        _lastVisitedLine = default;
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        _searchCache.InvalidateFromOffset(offset);

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
        
        var prevChangeBufferPos = _lastChangeBufferPos;
        if (!ReferenceEquals(_root, _sentinel))
        {
            var hit = NodeAt(offset);
            var node = hit.Node;
            var remainder = hit.Remainder;
            var nodeStartOffset = hit.NodeStartOffset;
            var piece = node.Piece;
            var bufferIndex = piece.BufferIndex;
            var insertPosInBuffer = PositionInBuffer(node, remainder);
            PieceTreeDebug.Log($"DEBUG Insert position: nodeBufIdx={node.Piece.BufferIndex}, nodeStart={node.Piece.Start}, nodeEnd={node.Piece.End}, remainder={remainder}, insertPosInBuffer={insertPosInBuffer}");

            // Optimization: append to the last change buffer node
            // TODO: Implement _lastChangeBufferPos tracking for this optimization
            
            if (nodeStartOffset == offset)
            {
                var prevNode = node.Prev();
                var tryAppend = false;
                BufferCursor? appendStart = null;
                if (!ReferenceEquals(prevNode, _sentinel) && prevNode.Piece.BufferIndex == ChangeBufferId)
                {
                    var prevEndCursor = prevNode.Piece.End;
                    var prevEndPos = PositionInBuffer(prevNode, prevNode.Piece.Length);
                    if (prevEndPos.Line == _lastChangeBufferPos.Line && prevEndPos.Column == _lastChangeBufferPos.Column)
                    {
                        tryAppend = true;
                        appendStart = prevEndCursor;
                    }
                }

                var appendedLeft = InsertContentToNodeLeft(value, node, tryAppend, appendStart);
                ValidateCRLFWithPrevNode(node);
                if (!appendedLeft)
                {
                    _lastChangeBufferPos = BufferCursor.Zero;
                }
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

                var tryAppend = piece.BufferIndex == ChangeBufferId && insertPosInBuffer.Line == _lastChangeBufferPos.Line && insertPosInBuffer.Column == _lastChangeBufferPos.Column;
                var newPieces = CreateNewPieces(value, tryAppend, insertPosInBuffer);
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
                // If we did not append into the change buffer during this middle insert, reset lastChangeBufferPos.
                if (prevChangeBufferPos.Line == _lastChangeBufferPos.Line && prevChangeBufferPos.Column == _lastChangeBufferPos.Column)
                {
                    _lastChangeBufferPos = BufferCursor.Zero;
                }
            }
            else
            {
                var isAppendToChange = piece.BufferIndex == ChangeBufferId && insertPosInBuffer.Line == _lastChangeBufferPos.Line && insertPosInBuffer.Column == _lastChangeBufferPos.Column;
                InsertContentToNodeRight(value, node, isAppendToChange, insertPosInBuffer);
                ValidateCRLFWithPrevNode(node.Next());
            }
        }
        else
        {
            // Insert new node
            // Consider append into the change buffer when inserting into an empty tree
            var changeBufferEnd = _buffers[ChangeBufferId].CreateEndCursor();
            var tryAppendNew = _buffers[ChangeBufferId].Length + value.Length <= ChunkUtilities.DefaultChunkSize;
            var pieces = CreateNewPieces(value, tryAppendNew, changeBufferEnd);
            var node = RbInsertLeft(_sentinel, pieces[0]);
            for (int k = 1; k < pieces.Count; k++)
            {
                node = RbInsertRight(node, pieces[k]);
            }
            if (prevChangeBufferPos.Line == _lastChangeBufferPos.Line && prevChangeBufferPos.Column == _lastChangeBufferPos.Column)
            {
                _lastChangeBufferPos = BufferCursor.Zero;
            }
        }

        // Recompute aggregates and revalidate cache only for the affected region.
        ComputeBufferMetadata();
        _searchCache.InvalidateFromOffset(offset);
        PieceTreeDebug.Log($"DEBUG Insert: TotalLength={TotalLength}, TotalLineFeeds={TotalLineFeeds}");
        PieceTreeDebug.Log("DEBUG Insert: Pieces after insert:");
        foreach (var p in EnumeratePiecesInOrder())
        {
            var buf = _buffers[p.BufferIndex];
            var text = buf.Slice(p.Start, p.End).Replace("\n", "\\n").Replace("\r","\\r");
            PieceTreeDebug.Log($"Piece BufIdx={p.BufferIndex}; Start={p.Start.Line}/{p.Start.Column}; End={p.End.Line}/{p.End.Column}; Len={p.Length}; LFcnt={p.LineFeedCount}; Text='{text}'");
        }
    }

    public void Delete(int offset, int cnt)
    {
        _lastVisitedLine = default;
        if (cnt <= 0 || ReferenceEquals(_root, _sentinel))
        {
            return;
        }

        _searchCache.InvalidateFromOffset(offset);

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
                    var next = startNode.Next();
                    RbDelete(startNode);
                    ValidateCRLFWithPrevNode(next);
                    return;
                }
                DeleteNodeHead(startNode, endSplitPos);
                ValidateCRLFWithPrevNode(startNode);
                ValidateCRLFWithNextNode(startNode);
                return;
            }

            if (startHit.NodeStartOffset + startNode.Piece.Length == offset + cnt)
            {
                DeleteNodeTail(startNode, startSplitPos);
                ValidateCRLFWithNextNode(startNode);
                return;
            }

            ShrinkNode(startNode, startSplitPos, endSplitPos);
            return;
        }

        var nodesToDel = new List<PieceTreeNode>();
        var startSplitPosInBuffer = PositionInBuffer(startNode, startHit.Remainder);
        DeleteNodeTail(startNode, startSplitPosInBuffer);
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

        var prev = startNode.Piece.Length == 0 ? startNode.Prev() : startNode;
        DeleteNodes(nodesToDel);
        ValidateCRLFWithNextNode(prev);
        // Recompute aggregates and revalidate cache only for the affected region.
        ComputeBufferMetadata();
        _searchCache.InvalidateFromOffset(offset);
    }

    private bool InsertContentToNodeLeft(string value, PieceTreeNode node, bool tryAppendToChangeBuffer = false, BufferCursor? appendStart = null)
    {
        // TODO: CRLF checks
        var prevPos = _lastChangeBufferPos;
        var newPieces = CreateNewPieces(value, tryAppendToChangeBuffer, appendStart);
        var newNode = RbInsertLeft(node, newPieces[newPieces.Count - 1]);
        for (int k = newPieces.Count - 2; k >= 0; k--)
        {
            newNode = RbInsertLeft(newNode, newPieces[k]);
        }

        ValidateCRLFWithPrevNode(newNode);
        return _lastChangeBufferPos.Line != prevPos.Line || _lastChangeBufferPos.Column != prevPos.Column;
    }

    private bool InsertContentToNodeRight(string value, PieceTreeNode node, bool tryAppendToChangeBuffer = false, BufferCursor? appendStart = null)
    {
        // TODO: CRLF checks
        var prevPos = _lastChangeBufferPos;
        var newPieces = CreateNewPieces(value, tryAppendToChangeBuffer, appendStart);
        var newNode = RbInsertRight(node, newPieces[0]);
        var tmpNode = newNode;
        for (int k = 1; k < newPieces.Count; k++)
        {
            tmpNode = RbInsertRight(tmpNode, newPieces[k]);
        }
        ValidateCRLFWithPrevNode(newNode);
        // Also validate the boundary between the last new piece and the next node (if any)
        ValidateCRLFWithPrevNode(tmpNode.Next());
        return _lastChangeBufferPos.Line != prevPos.Line || _lastChangeBufferPos.Column != prevPos.Column;
    }

    private BufferCursor PositionInBuffer(PieceTreeNode node, int remainder)
    {
        var piece = node.Piece;
        var buffer = _buffers[piece.BufferIndex];
        var lineStarts = buffer.LineStarts;
        var startOffset = Math.Clamp(lineStarts[piece.Start.Line] + piece.Start.Column, 0, buffer.Length);
        var targetOffset = Math.Clamp(startOffset + remainder, 0, buffer.Length);

        var lineIndex = FindLineIndex(lineStarts, targetOffset);
        var column = Math.Clamp(targetOffset - lineStarts[lineIndex], 0, (lineIndex == lineStarts.Count - 1 ? buffer.Length - lineStarts[lineIndex] : lineStarts[lineIndex + 1] - lineStarts[lineIndex]));
        return new BufferCursor(lineIndex, column);
    }

    private int OffsetInBuffer(int bufferIndex, BufferCursor cursor)
    {
        var buffer = _buffers[bufferIndex];
        var lineStarts = buffer.LineStarts;
        if (lineStarts.Count == 0) return 0;
        var line = Math.Clamp(cursor.Line, 0, lineStarts.Count - 1);
        var nextLineStart = line == lineStarts.Count - 1 ? buffer.Length : lineStarts[line + 1];
        var column = Math.Clamp(cursor.Column, 0, nextLineStart - lineStarts[line]);
        return lineStarts[line] + column;
    }

    private int GetLineFeedCnt(int bufferIndex, BufferCursor start, BufferCursor end)
    {
        // Compute the number of line feeds within the slice [start, end) of the given buffer.
        // We need to account for CRLF pairs as a single line feed and lone CR or LF characters as a single feed.
        // Using line starts alone (end.Line - start.Line) is not sufficient when dealing with slices that split CRLF
        // across piece boundaries (e.g. a piece containing just the CR char originating from a CRLF pair).
        var buffer = _buffers[bufferIndex];
        var startOffset = buffer.GetOffset(start);
        var endOffset = buffer.GetOffset(end);
        if (endOffset <= startOffset) return 0;
        var slice = buffer.Buffer.AsSpan(startOffset, endOffset - startOffset);
        // Intentionally no debug prints here; keep this function quiet in normal runs
        var count = 0;
        for (int i = 0; i < slice.Length; i++)
        {
            var ch = slice[i];
            if (ch == '\r')
            {
                if (i + 1 < slice.Length && slice[i + 1] == '\n')
                {
                    count++;
                    i++; // skip the LF after the CR
                }
                else
                {
                    count++;
                }
            }
            else if (ch == '\n')
            {
                count++;
            }
        }
        return count;
    }

    private void ValidateCRLFWithPrevNode(PieceTreeNode nextNode)
    {
        if (!ShouldCheckCRLF() || ReferenceEquals(nextNode, _sentinel))
        {
            return;
        }

        var prevNode = nextNode.Prev();
        if (ReferenceEquals(prevNode, _sentinel))
        {
            return;
        }

        var endWithCr = EndWithCR(prevNode.Piece);
        var startWithLf = StartWithLF(nextNode.Piece);
#if DEBUG
        PieceTreeDebug.Log($"ValidateCRLF PrevNode BufIdx={prevNode.Piece.BufferIndex}; endWithCR={endWithCr}; NextNode BufIdx={nextNode.Piece.BufferIndex}; startWithLF={startWithLf}");
#endif
        if (endWithCr && startWithLf)
        {
            FixCRLF(prevNode, nextNode);
        }
    }

    private void ValidateCRLFWithNextNode(PieceTreeNode node)
    {
        if (!ShouldCheckCRLF() || ReferenceEquals(node, _sentinel))
        {
            return;
        }

        var nextNode = node.Next();
        if (ReferenceEquals(nextNode, _sentinel))
        {
            return;
        }

        var endWithCr = EndWithCR(node.Piece);
        var startWithLf = StartWithLF(nextNode.Piece);
#if DEBUG
        PieceTreeDebug.Log($"ValidateCRLF NextNode BufIdx={node.Piece.BufferIndex}; endWithCR={endWithCr}; NextNode BufIdx={nextNode.Piece.BufferIndex}; startWithLF={startWithLf}");
#endif
        if (endWithCr && startWithLf)
        {
            FixCRLF(node, nextNode);
        }
    }

    private void FixCRLF(PieceTreeNode prevNode, PieceTreeNode nextNode)
    {
        if (ReferenceEquals(prevNode, _sentinel) || ReferenceEquals(nextNode, _sentinel))
        {
            return;
        }
        // Guard: only fix CRLF when previous ends with CR and next begins with LF
        if (!(EndWithCR(prevNode.Piece) && StartWithLF(nextNode.Piece)))
        {
            return;
        }
        var mutationOffset = GetOffsetOfNode(prevNode);
        
        _searchCache.InvalidateFromOffset(mutationOffset);

        var nodesToDelete = new List<PieceTreeNode>(2);
        RemoveTrailingCarriageReturn(prevNode, nodesToDelete);
        RemoveLeadingLineFeed(nextNode, nodesToDelete);
        

        var pieces = CreateNewPieces("\r\n");
        var insertionAnchor = prevNode;
        foreach (var piece in pieces)
        {
            insertionAnchor = RbInsertRight(insertionAnchor, piece);
        }

        foreach (var candidate in nodesToDelete)
        {
            if (!ReferenceEquals(candidate, _sentinel) && candidate.Piece.Length == 0)
            {
                RbDelete(candidate);
            }
        }
        // Recompute aggregates and revalidate search caches because we performed a CR/LF normalization.
        ComputeBufferMetadata();
    }

    private void RemoveTrailingCarriageReturn(PieceTreeNode node, List<PieceTreeNode> nodesToDelete)
    {
        if (ReferenceEquals(node, _sentinel) || node.Piece.Length == 0)
        {
            return;
        }

        var piece = node.Piece;
        var buffer = _buffers[piece.BufferIndex];
        var endOffset = buffer.GetOffset(piece.End);
        if (endOffset == 0)
        {
            return;
        }
        // Ensure the last character is indeed a carriage return
        if (buffer.Buffer[endOffset - 1] != '\r')
        {
            return;
        }
        var newEnd = CursorFromOffset(piece.BufferIndex, endOffset - 1);
        var newLength = piece.Length - 1;
        var newLineFeeds = GetLineFeedCnt(piece.BufferIndex, piece.Start, newEnd);
        node.Piece = new PieceSegment(piece.BufferIndex, piece.Start, newEnd, newLineFeeds, newLength);
        RecomputeMetadataUpwards(node);

        if (newLength == 0)
        {
            nodesToDelete.Add(node);
        }
    }

    private void RemoveLeadingLineFeed(PieceTreeNode node, List<PieceTreeNode> nodesToDelete)
    {
        if (ReferenceEquals(node, _sentinel) || node.Piece.Length == 0)
        {
            return;
        }

        var piece = node.Piece;
        var buffer = _buffers[piece.BufferIndex];
        var startOffset = buffer.GetOffset(piece.Start);
        if (startOffset >= buffer.Length)
        {
            return;
        }

        // Ensure the first character is indeed a line feed
        if (buffer.Buffer[startOffset] != '\n')
        {
            return;
        }

        var newStart = CursorFromOffset(piece.BufferIndex, startOffset + 1);
        var newLength = piece.Length - 1;
        var newLineFeeds = GetLineFeedCnt(piece.BufferIndex, newStart, piece.End);
        node.Piece = new PieceSegment(piece.BufferIndex, newStart, piece.End, newLineFeeds, newLength);
        RecomputeMetadataUpwards(node);

        if (newLength == 0)
        {
            nodesToDelete.Add(node);
        }
    }

    private bool ShouldCheckCRLF() => !(_eolNormalized && _eol == "\n");

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
        // Diagnostics removed: do not print slice info during normal test runs
        
        var lf_delta = newLineFeedCnt - originalLFCnt;
        var size_delta = newEndOffset - originalEndOffset;
        var newLength = piece.Length + size_delta;
        
        node.Piece = new PieceSegment(piece.BufferIndex, piece.Start, newEnd, newLineFeedCnt, newLength);
        UpdateTreeMetadata(node, size_delta, lf_delta);
        RecomputeMetadataUpwards(node);
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
        RecomputeMetadataUpwards(node);
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
        RecomputeMetadataUpwards(node);
        
        var newPiece = new PieceSegment(
            piece.BufferIndex,
            end,
            originalEnd,
            GetLineFeedCnt(piece.BufferIndex, end, originalEnd),
            OffsetInBuffer(piece.BufferIndex, originalEnd) - OffsetInBuffer(piece.BufferIndex, end)
        );
        
        var newNode = RbInsertRight(node, newPiece);
        ValidateCRLFWithPrevNode(newNode);
    }

    private void DeleteNodes(List<PieceTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            RbDelete(node);
        }
    }

    private List<PieceSegment> CreateNewPieces(string text, bool tryAppendToChangeBuffer = false, BufferCursor? appendStart = null)
    {
        var pieces = new List<PieceSegment>();
        if (string.IsNullOrEmpty(text))
        {
            return pieces;
        }

        // Use TS-like split helper to maintain good chunk sizes and CR/surrogate safety.
        var first = true;
        foreach (var slice in ChunkUtilities.SplitText(text))
        {
            // Only append to the change buffer when explicitly asked and when the tree is empty
            // or the append start was at the change buffer end and no other nodes reference the change buffer.
            if (tryAppendToChangeBuffer && first && appendStart.HasValue && _buffers.Count > 0)
            {
                var changeBuf = _buffers[ChangeBufferId];
                // Only append small slices to change buffer to reduce GC/alloc/fragmentation.
                if (changeBuf.Length + slice.Length <= ChunkUtilities.DefaultChunkSize)
                {
                    var combined = changeBuf.Buffer + slice;
                    var newChangeBuffer = ChunkBuffer.FromText(combined);
                    // Update existing pieces that reference the change buffer to map to the new buffer's cursor positions.
                    var oldChangeBuffer = _buffers[ChangeBufferId];
                    _buffers[ChangeBufferId] = newChangeBuffer;
                    MigrateChangeBufferPieces(oldChangeBuffer, newChangeBuffer);
                    var oldStartOffset = oldChangeBuffer.GetOffset(appendStart.Value);
                    var startCursor = CursorFromOffset(ChangeBufferId, oldStartOffset);
                    var endCursor = newChangeBuffer.CreateEndCursor();
                    var lf = GetLineFeedCnt(ChangeBufferId, startCursor, endCursor);
                    var len = OffsetInBuffer(ChangeBufferId, endCursor) - OffsetInBuffer(ChangeBufferId, startCursor);
#if DEBUG
                    PieceTreeDebug.Log($"DEBUG CreateNewPieces Append to ChangeBuffer: startCursor={startCursor}, endCursor={endCursor}, lf={lf}, len={len}, changeBufLen={newChangeBuffer.Length}");
#endif
                    pieces.Add(new PieceSegment(ChangeBufferId, startCursor, endCursor, lf, len));
                    // Update last change buffer position
                    _lastChangeBufferPos = endCursor;
                    first = false;
                    // continue with the rest (no further try to append to change buffer in this call)
                    tryAppendToChangeBuffer = false;
                    continue;
                }
            }

            // Default: create a new chunk for this slice
            var piece = CreatePieceFromNewBuffer(slice);
#if DEBUG
            PieceTreeDebug.Log($"DEBUG CreateNewPieces Created new piece from new buffer: bufIdx={piece.BufferIndex}, start={piece.Start}, end={piece.End}, lf={piece.LineFeedCount}, len={piece.Length}");
#endif
            pieces.Add(piece);
            first = false;
        }

        return pieces;
    }

    private void MigrateChangeBufferPieces(ChunkBuffer oldBuffer, ChunkBuffer newBuffer)
    {
        // For every node using ChangeBufferId, re-map its Start/End cursors using the absolute offsets derived from the old buffer
        var nodes = EnumerateNodesInOrder();
        foreach (var node in nodes)
        {
            if (node is null || ReferenceEquals(node, _sentinel)) continue;
            if (node.Piece.BufferIndex != ChangeBufferId) continue;

            var oldStartOffset = oldBuffer.GetOffset(node.Piece.Start);
            var oldEndOffset = oldBuffer.GetOffset(node.Piece.End);
            var newStart = CursorFromOffset(ChangeBufferId, oldStartOffset);
            var newEnd = CursorFromOffset(ChangeBufferId, oldEndOffset);
            var newLF = GetLineFeedCnt(ChangeBufferId, newStart, newEnd);
            var newLen = OffsetInBuffer(ChangeBufferId, newEnd) - OffsetInBuffer(ChangeBufferId, newStart);
            node.Piece = new PieceSegment(ChangeBufferId, newStart, newEnd, newLF, newLen);
            RecomputeMetadataUpwards(node);
        }
    }

    private PieceSegment CreatePieceFromNewBuffer(string text)
    {
        // TODO (AA4-006): reuse buffer 0/change buffer when possible instead of allocating a new chunk for every insert.
        var chunkBuffer = ChunkBuffer.FromText(text);
        _buffers.Add(chunkBuffer);
        var bufferIndex = _buffers.Count - 1;
        // Compute the accurate line feed count for this new chunk using the same scanner that we use for slices
        var endCursor = chunkBuffer.CreateEndCursor();
        var lf = GetLineFeedCnt(bufferIndex, BufferCursor.Zero, endCursor);
        var len = chunkBuffer.Length;
        return new PieceSegment(
            bufferIndex,
            BufferCursor.Zero,
            endCursor,
            lf,
            len
        );
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
        _count++;
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
        _count++;
        RecomputeMetadataUpwards(z);
        return z;
    }

    private void RbDelete(PieceTreeNode z)
    {
        var deletionOffset = GetOffsetOfNode(z);
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
            // Ensure search cache invalidation for root deletes
            _searchCache.InvalidateFromOffset(deletionOffset);
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
        _count = Math.Max(0, _count - 1);
        _searchCache.InvalidateFromOffset(deletionOffset);

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

    private BufferCursor CursorFromOffset(int bufferIndex, int absoluteOffset)
    {
        var buffer = _buffers[bufferIndex];
        absoluteOffset = Math.Clamp(absoluteOffset, 0, buffer.Buffer.Length);
        var lineStarts = buffer.LineStarts;
        var lineIndex = FindLineIndex(lineStarts, absoluteOffset);
        var column = absoluteOffset - lineStarts[lineIndex];
        return new BufferCursor(lineIndex, column);
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
