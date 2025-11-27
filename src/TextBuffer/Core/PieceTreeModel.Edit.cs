// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Class: PieceTreeBase - Insert/Delete operations (Lines: 800-1500)
// Ported: 2025-11-19

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

        var mutationStart = Math.Max(0, offset - 1);
        var mutationLength = Math.Max(1, value.Length + 2);
        _searchCache.InvalidateRange(mutationStart, mutationLength);

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
            int remainder;
            int nodeStartOffset;
            if (node is null)
            {
                node = Rightest(_root);
                nodeStartOffset = GetOffsetOfNode(node);
                remainder = node.Piece.Length;
            }
            else
            {
                nodeStartOffset = hit.NodeStartOffset;
                remainder = hit.Remainder;
            }

            var piece = node.Piece;
            var bufferIndex = piece.BufferIndex;
            var insertPosInBuffer = PositionInBuffer(node, remainder);
            if (PieceTreeDebug.IsEnabled)
            {
                var nodeText = _buffers[piece.BufferIndex].Slice(piece.Start, piece.End)
                    .Replace("\n", "\\n").Replace("\r", "\\r");
                PieceTreeDebug.Log($"DEBUG Insert position: nodeBufIdx={node.Piece.BufferIndex}, nodeStart={node.Piece.Start}, nodeEnd={node.Piece.End}, remainder={remainder}, insertPosInBuffer={insertPosInBuffer}, nodeStartOffset={nodeStartOffset}, nodeText='{nodeText}'");
            }

            if (TryAppendToChangeBufferNode(node, value, nodeStartOffset, offset))
            {
                ComputeBufferMetadata();
                return;
            }

            if (nodeStartOffset == offset)
            {
                var prevNode = node.Prev();
                if (!ReferenceEquals(prevNode, _sentinel))
                {
                    var prevStartOffset = GetOffsetOfNode(prevNode);
                    if (TryAppendToChangeBufferNode(prevNode, value, prevStartOffset, offset))
                    {
                        ComputeBufferMetadata();
                        return;
                    }
                }

                InsertContentToNodeLeft(value, node);
            }
            else if (nodeStartOffset + piece.Length > offset)
            {
                var newRightPiece = CreateSegment(
                    piece.BufferIndex,
                    insertPosInBuffer,
                    piece.End,
                    GetLineFeedCnt(piece.BufferIndex, insertPosInBuffer, piece.End)
                );
                if (PieceTreeDebug.IsEnabled)
                {
                    var newRightText = _buffers[newRightPiece.BufferIndex]
                        .Slice(newRightPiece.Start, newRightPiece.End)
                        .Replace("\n", "\\n").Replace("\r", "\\r");
                    PieceTreeDebug.Log($"DEBUG Split right piece len={newRightPiece.Length}, text='{newRightText}'");
                }

                if (EndWithCR(node.Piece) && StartWithLF(newRightPiece))
                {
                    ValidateCRLFWithPrevNode(node);
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

                ValidateCRLFWithPrevNode(node.Next());
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
            InsertIntoEmptyTree(value);
        }

        ComputeBufferMetadata();
        PieceTreeDebug.Log($"DEBUG Insert: TotalLength={TotalLength}, TotalLineFeeds={TotalLineFeeds}");
        PieceTreeDebug.Log("DEBUG Insert: Pieces after insert:");
        foreach (var p in EnumeratePiecesInOrder())
        {
            var buf = _buffers[p.BufferIndex];
            var text = buf.Slice(p.Start, p.End).Replace("\n", "\\n").Replace("\r","\\r");
            PieceTreeDebug.Log($"Piece BufIdx={p.BufferIndex}; Start={p.Start.Line}/{p.Start.Column}; End={p.End.Line}/{p.End.Column}; Len={p.Length}; LFcnt={p.LineFeedCount}; Text='{text}'");
        }
    }

    private void InsertIntoEmptyTree(string value)
    {
        var pieces = CreateNewPieces(value);
        if (pieces.Count == 0)
        {
            return;
        }

        var node = RbInsertLeft(_sentinel, pieces[0]);
        for (int k = 1; k < pieces.Count; k++)
        {
            node = RbInsertRight(node, pieces[k]);
        }
    }

    private bool TryAppendToChangeBufferNode(PieceTreeNode node, string value, int nodeStartOffset, int insertOffset)
    {
        if (ReferenceEquals(node, _sentinel) || string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (value.Length > AverageBufferSize)
        {
            return false;
        }

        if (node.Piece.Length >= AverageBufferSize)
        {
            return false;
        }

        if (node.Piece.Length + value.Length > AverageBufferSize)
        {
            return false;
        }

        if (node.Piece.BufferIndex != ChangeBufferId)
        {
            return false;
        }

        if (node.Piece.End.Line != _lastChangeBufferPos.Line || node.Piece.End.Column != _lastChangeBufferPos.Column)
        {
            return false;
        }

        if (nodeStartOffset + node.Piece.Length != insertOffset)
        {
            return false;
        }

        AppendToChangeBufferNode(node, value);
        return true;
    }

    private void AppendToChangeBufferNode(PieceTreeNode node, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        // Step 1: Check if next node starts with LF and current value ends with CR
        // If so, we need to "steal" the LF from the next node and append it to our value
        var adjusted = AdjustCarriageReturnFromNext(value, node);
        if (string.IsNullOrEmpty(adjusted))
        {
            return;
        }

        // Step 2: Detect hitCRLF - if the change buffer currently ends with \r and 
        // the incoming text starts with \n, we need to adjust the line starts
        // This mirrors TS appendToNode hitCRLF logic (lines 1460-1471)
        var hitCRLF = ShouldCheckCRLF() && StartWithLF(adjusted) && EndWithCR(node);
        
        var changeBuffer = _buffers[ChangeBufferId];
        var oldPiece = node.Piece;
        var startOffset = changeBuffer.GetOffset(oldPiece.Start);
        var oldEndOffset = changeBuffer.GetOffset(oldPiece.End);
        
        // Append to buffer first (as in TS)
        var newBuffer = changeBuffer.Append(adjusted);
        var oldLineStarts = changeBuffer.LineStarts;
        _buffers[ChangeBufferId] = newBuffer;

        if (hitCRLF)
        {
            newBuffer = ChunkBuffer.FromText(newBuffer.Buffer);
            _buffers[ChangeBufferId] = newBuffer;

            if (oldLineStarts.Count >= 2)
            {
                var prevStartOffset = oldLineStarts[oldLineStarts.Count - 2];
                _lastChangeBufferPos = new BufferCursor(Math.Max(0, _lastChangeBufferPos.Line - 1), Math.Max(0, startOffset - prevStartOffset));
            }

            var bridgeOffset = Math.Max(0, GetOffsetOfNode(node) + oldPiece.Length - 1);
            _searchCache.InvalidateRange(bridgeOffset, 2);
        }
        
        var appendedLength = adjusted.Length;
        var newEndOffset = oldEndOffset + appendedLength;
        var newStartCursor = CursorFromOffset(ChangeBufferId, startOffset);
        var newEndCursor = CursorFromOffset(ChangeBufferId, newEndOffset);
        var newLineFeeds = GetLineFeedCnt(ChangeBufferId, newStartCursor, newEndCursor);
        var updatedPiece = CreateSegment(ChangeBufferId, newStartCursor, newEndCursor, newLineFeeds);
        node.Piece = updatedPiece;
        var sizeDelta = updatedPiece.Length - oldPiece.Length;
        var lfDelta = updatedPiece.LineFeedCount - oldPiece.LineFeedCount;
        UpdateTreeMetadata(node, sizeDelta, lfDelta);
        RecomputeMetadataUpwards(node);
        _lastChangeBufferOffset = newBuffer.Length;
        _lastChangeBufferPos = newEndCursor;
    }

    private string AdjustCarriageReturnFromNext(string value, PieceTreeNode node)
    {
        if (!ShouldCheckCRLF() || string.IsNullOrEmpty(value) || value[^1] != '\r')
        {
            return value;
        }

        var nextNode = node.Next();
        if (ReferenceEquals(nextNode, _sentinel) || !StartWithLF(nextNode.Piece))
        {
            return value;
        }

        var nextOffset = Math.Max(0, GetOffsetOfNode(nextNode) - 1);
        _searchCache.InvalidateRange(nextOffset, 2);
        var nodesToDelete = new List<PieceTreeNode>(1);
        RemoveLeadingLineFeed(nextNode, nodesToDelete);
        DeleteNodes(nodesToDelete);
        return value + "\n";
    }

    public void Delete(int offset, int cnt)
    {
        _lastVisitedLine = default;
        if (cnt <= 0 || ReferenceEquals(_root, _sentinel))
        {
            return;
        }

        var mutationStart = Math.Max(0, offset - 1);
        var mutationLength = Math.Max(1, cnt + 2);
        _searchCache.InvalidateRange(mutationStart, mutationLength);

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
    }

    private void InsertContentToNodeLeft(string value, PieceTreeNode node)
    {
        var nodesToDelete = new List<PieceTreeNode>();
        if (ShouldCheckCRLF() && EndWithCR(value) && StartWithLF(node.Piece))
        {
            var piece = node.Piece;
            var newStart = new BufferCursor(piece.Start.Line + 1, 0);
            var newPiece = CreateSegment(piece.BufferIndex, newStart, piece.End);
            node.Piece = newPiece;
            UpdateTreeMetadata(node, newPiece.Length - piece.Length, newPiece.LineFeedCount - piece.LineFeedCount);
            RecomputeMetadataUpwards(node);
            value += "\n";
            if (node.Piece.Length == 0)
            {
                nodesToDelete.Add(node);
            }
        }

        var newPieces = CreateNewPieces(value);
        var newNode = RbInsertLeft(node, newPieces[newPieces.Count - 1]);
        for (int k = newPieces.Count - 2; k >= 0; k--)
        {
            newNode = RbInsertLeft(newNode, newPieces[k]);
        }

        ValidateCRLFWithPrevNode(newNode);
        DeleteNodes(nodesToDelete);
    }

    private void InsertContentToNodeRight(string value, PieceTreeNode node)
    {
        value = AdjustCarriageReturnFromNext(value, node);
        var newPieces = CreateNewPieces(value);
        var newNode = RbInsertRight(node, newPieces[0]);
        var tmpNode = newNode;
        for (int k = 1; k < newPieces.Count; k++)
        {
            tmpNode = RbInsertRight(tmpNode, newPieces[k]);
        }
        ValidateCRLFWithPrevNode(newNode);
        ValidateCRLFWithPrevNode(tmpNode.Next());
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
        PieceTreeDebug.Log($"FixCRLF: prevOffset={GetOffsetOfNode(prevNode)}, prevText='{_buffers[prevNode.Piece.BufferIndex].Slice(prevNode.Piece.Start, prevNode.Piece.End).Replace("\n", "\\n").Replace("\r", "\\r")}', nextText='{_buffers[nextNode.Piece.BufferIndex].Slice(nextNode.Piece.Start, nextNode.Piece.End).Replace("\n", "\\n").Replace("\r", "\\r")}'");
        var mutationOffset = GetOffsetOfNode(prevNode);
        
        var mutationStart = Math.Max(0, mutationOffset - 1);
        _searchCache.InvalidateRange(mutationStart, 4);

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
        var newPiece = CreateSegment(piece.BufferIndex, piece.Start, newEnd);
        node.Piece = newPiece;
        RecomputeMetadataUpwards(node);

        if (newPiece.Length == 0)
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
        var newPiece = CreateSegment(piece.BufferIndex, newStart, piece.End);
        node.Piece = newPiece;
        RecomputeMetadataUpwards(node);

        if (newPiece.Length == 0)
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

    /// <summary>
    /// Checks if the node's piece ends with a carriage return character.
    /// Used for hitCRLF detection in appendToNode (TS parity).
    /// </summary>
    private bool EndWithCR(PieceTreeNode node)
    {
        if (ReferenceEquals(node, _sentinel) || node.Piece.Length == 0)
        {
            return false;
        }
        return EndWithCR(node.Piece);
    }

    private static bool EndWithCR(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }
        return text[^1] == '\r';
    }

    private bool StartWithLF(PieceSegment piece)
    {
        var buffer = _buffers[piece.BufferIndex];
        var startOffset = OffsetInBuffer(piece.BufferIndex, piece.Start);
        if (startOffset >= buffer.Buffer.Length) return false;
        return buffer.Buffer[startOffset] == '\n';
    }

    private static bool StartWithLF(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return text[0] == '\n';
    }

    private void DeleteNodeTail(PieceTreeNode node, BufferCursor pos)
    {
        var piece = node.Piece;
        var newEnd = pos;
        var newPiece = CreateSegment(piece.BufferIndex, piece.Start, newEnd);
        var size_delta = newPiece.Length - piece.Length;
        var lf_delta = newPiece.LineFeedCount - piece.LineFeedCount;
        node.Piece = newPiece;
        UpdateTreeMetadata(node, size_delta, lf_delta);
        RecomputeMetadataUpwards(node);
    }

    private void DeleteNodeHead(PieceTreeNode node, BufferCursor pos)
    {
        var piece = node.Piece;
        var newStart = pos;
        var newPiece = CreateSegment(piece.BufferIndex, newStart, piece.End);
        var size_delta = newPiece.Length - piece.Length;
        var lf_delta = newPiece.LineFeedCount - piece.LineFeedCount;
        node.Piece = newPiece;
        UpdateTreeMetadata(node, size_delta, lf_delta);
        RecomputeMetadataUpwards(node);
    }

    private void ShrinkNode(PieceTreeNode node, BufferCursor start, BufferCursor end)
    {
        var piece = node.Piece;
        var originalEnd = piece.End;
        var oldLength = piece.Length;
        var oldLFCnt = piece.LineFeedCount;

        var newEnd = start;
        var leftPiece = CreateSegment(piece.BufferIndex, piece.Start, newEnd);
        node.Piece = leftPiece;
        UpdateTreeMetadata(node, leftPiece.Length - oldLength, leftPiece.LineFeedCount - oldLFCnt);
        RecomputeMetadataUpwards(node);
        
        var rightPiece = CreateSegment(piece.BufferIndex, end, originalEnd);
        if (rightPiece.Length > 0)
        {
            var newNode = RbInsertRight(node, rightPiece);
            ValidateCRLFWithPrevNode(newNode);
        }
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
        var pieces = new List<PieceSegment>();
        if (string.IsNullOrEmpty(text))
        {
            return pieces;
        }

        if (text.Length > AverageBufferSize)
        {
            foreach (var slice in ChunkUtilities.SplitText(text))
            {
                if (string.IsNullOrEmpty(slice))
                {
                    continue;
                }

                var segment = CreatePieceFromNewBuffer(slice);
                pieces.Add(segment);
            }
            return pieces;
        }

        var changeBuffer = _buffers[ChangeBufferId];
        var startOffset = changeBuffer.Length;
        
        // Step 3: CRLF bridge while creating new pieces
        // Mirror TS createNewPieces (lines 1208-1223): if buffer[0] ends with \r and text starts with \n,
        // we need to use a sentinel/placeholder character to keep LineStarts monotonic.
        var start = _lastChangeBufferPos;
        
        // Check: buffer[0].lineStarts last entry == startOffset && startOffset != 0 && text starts with LF && buffer ends with CR
        var lineStarts = changeBuffer.LineStarts;
        if (lineStarts.Count > 0 && 
            lineStarts[lineStarts.Count - 1] == startOffset &&
            startOffset != 0 &&
            StartWithLF(text) &&
            EndWithCR(changeBuffer.Buffer))
        {
            // Advance _lastChangeBufferPos.column by 1 (for the placeholder)
            _lastChangeBufferPos = new BufferCursor(_lastChangeBufferPos.Line, _lastChangeBufferPos.Column + 1);
            start = _lastChangeBufferPos;
            
            // Append placeholder '_' + text to buffer
            var textWithPlaceholder = "_" + text;
            var newBuffer = changeBuffer.Append(textWithPlaceholder);
            _buffers[ChangeBufferId] = newBuffer;
            
            // Increment startOffset to skip the placeholder
            startOffset += 1;
            
            // Invalidate cache at the bridge position
            var bridgeOffset = Math.Max(0, startOffset - 2);
            _searchCache.InvalidateRange(bridgeOffset, 3);
            
            var endCursor = newBuffer.CreateEndCursor();
            var lf = GetLineFeedCnt(ChangeBufferId, start, endCursor);
            var piece = CreateSegment(ChangeBufferId, start, endCursor, lf);
            pieces.Add(piece);
            _lastChangeBufferPos = endCursor;
            _lastChangeBufferOffset = newBuffer.Length;
        }
        else
        {
            // Normal path - no CRLF bridging needed
            var newBuffer = changeBuffer.Append(text);
            _buffers[ChangeBufferId] = newBuffer;
            var endCursor = newBuffer.CreateEndCursor();
            var lf = GetLineFeedCnt(ChangeBufferId, start, endCursor);
            var piece = CreateSegment(ChangeBufferId, start, endCursor, lf);
            pieces.Add(piece);
            _lastChangeBufferPos = endCursor;
            _lastChangeBufferOffset = newBuffer.Length;
        }
        
        return pieces;
    }

    private PieceSegment CreatePieceFromNewBuffer(string text)
    {
        // TODO (AA4-006): reuse buffer 0/change buffer when possible instead of allocating a new chunk for every insert.
        var chunkBuffer = ChunkBuffer.FromText(text);
        _buffers.Add(chunkBuffer);
        var bufferIndex = _buffers.Count - 1;
        var endCursor = chunkBuffer.CreateEndCursor();
        return CreateSegment(bufferIndex, BufferCursor.Zero, endCursor);
    }

    private PieceSegment CreateSegment(int bufferIndex, BufferCursor start, BufferCursor end)
    {
        var buffer = _buffers[bufferIndex];
        var startOffset = OffsetInBuffer(bufferIndex, start);
        var endOffset = OffsetInBuffer(bufferIndex, end);
        var length = Math.Max(0, endOffset - startOffset);
        var lineFeeds = length == 0 ? 0 : GetLineFeedCnt(bufferIndex, start, end);
        var textLength = buffer.Slice(start, end).Length;
        if (textLength != length)
        {
            PieceTreeDebug.Log($"CreateSegment mismatch: buffer={bufferIndex}, start={start.Line}/{start.Column}, end={end.Line}/{end.Column}, computedLen={length}, actualLen={textLength}");
            length = textLength;
        }
        return new PieceSegment(bufferIndex, start, end, lineFeeds, length);
    }

    private PieceSegment CreateSegment(int bufferIndex, BufferCursor start, BufferCursor end, int lineFeeds)
    {
        var buffer = _buffers[bufferIndex];
        var startOffset = OffsetInBuffer(bufferIndex, start);
        var endOffset = OffsetInBuffer(bufferIndex, end);
        var length = Math.Max(0, endOffset - startOffset);
        var textLength = buffer.Slice(start, end).Length;
        if (textLength != length)
        {
            PieceTreeDebug.Log($"CreateSegment mismatch: buffer={bufferIndex}, start={start.Line}/{start.Column}, end={end.Line}/{end.Column}, computedLen={length}, actualLen={textLength}");
            length = textLength;
        }
        return new PieceSegment(bufferIndex, start, end, lineFeeds, length);
    }

    private PieceTreeNode RbInsertRight(PieceTreeNode node, PieceSegment p)
    {
        var z = new PieceTreeNode(p, NodeColor.Red, _sentinel);
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
        var z = new PieceTreeNode(p, NodeColor.Red, _sentinel);
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
