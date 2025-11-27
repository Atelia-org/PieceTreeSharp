// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Class: PieceTreeBase - Search/FindMatches operations (Lines: 1500-1800)
// Ported: 2025-11-19

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PieceTree.TextBuffer;

namespace PieceTree.TextBuffer.Core;

internal sealed partial class PieceTreeModel
{
    private readonly record struct LineIndexResult(int LineDelta, int Column);

    public List<FindMatch> FindMatchesLineByLine(Range searchRange, SearchData searchData, bool captureMatches, int limitResultCount)
    {
        var result = new List<FindMatch>();
        var searcher = new PieceTreeSearcher(searchData.WordSeparators, searchData.Regex);

        var startPosition = NodeAt2(searchRange.Start.LineNumber, searchRange.Start.Column);
        if (startPosition.Node == null) return result;

        var endPosition = NodeAt2(searchRange.End.LineNumber, searchRange.End.Column);
        if (endPosition.Node == null) return result;

        var start = PositionInBuffer(startPosition.Node, startPosition.Remainder);
        var end = PositionInBuffer(endPosition.Node, endPosition.Remainder);

        if (ReferenceEquals(startPosition.Node, endPosition.Node))
        {
            FindMatchesInNode(startPosition.Node, searcher, searchRange.Start.LineNumber, searchRange.Start.Column, start, end, searchData, captureMatches, limitResultCount, result);
            return result;
        }

        int startLineNumber = searchRange.Start.LineNumber;
        var currentNode = startPosition.Node;

        while (!ReferenceEquals(currentNode, endPosition.Node))
        {
            int lineBreakCnt = GetLineFeedCnt(currentNode.Piece.BufferIndex, start, currentNode.Piece.End);

            if (lineBreakCnt >= 1)
            {
                var lineStarts = _buffers[currentNode.Piece.BufferIndex].LineStarts;
                var startOffsetInBuffer = OffsetInBuffer(currentNode.Piece.BufferIndex, currentNode.Piece.Start);
                var nextLineStartOffset = lineStarts[start.Line + lineBreakCnt];
                var startColumn = startLineNumber == searchRange.Start.LineNumber ? searchRange.Start.Column : 1;
                
                var endCursor = PositionInBuffer(currentNode, nextLineStartOffset - startOffsetInBuffer);
                
                FindMatchesInNode(currentNode, searcher, startLineNumber, startColumn, start, endCursor, searchData, captureMatches, limitResultCount, result);

                if (result.Count >= limitResultCount) return result;

                startLineNumber += lineBreakCnt;
            }

            var startColumnForLine = startLineNumber == searchRange.Start.LineNumber ? searchRange.Start.Column - 1 : 0;
            
            // search for the remaining content
            if (startLineNumber == searchRange.End.LineNumber)
            {
                string text = GetLineContent(startLineNumber).Substring(startColumnForLine, searchRange.End.Column - 1 - startColumnForLine);
                FindMatchesInLine(searchData, searcher, text, searchRange.End.LineNumber, startColumnForLine, result, captureMatches, limitResultCount);
                return result;
            }

            string lineContent = GetLineContent(startLineNumber);
            if (startColumnForLine < lineContent.Length)
            {
                 FindMatchesInLine(searchData, searcher, lineContent.Substring(startColumnForLine), startLineNumber, startColumnForLine, result, captureMatches, limitResultCount);
            }

            if (result.Count >= limitResultCount) return result;

            startLineNumber++;
            startPosition = NodeAt2(startLineNumber, 1);
            currentNode = startPosition.Node;
            start = PositionInBuffer(startPosition.Node, startPosition.Remainder);
        }

        if (startLineNumber == searchRange.End.LineNumber)
        {
            var startColumn = startLineNumber == searchRange.Start.LineNumber ? searchRange.Start.Column - 1 : 0;
            string text = GetLineContent(startLineNumber).Substring(startColumn, searchRange.End.Column - 1 - startColumn);
            FindMatchesInLine(searchData, searcher, text, searchRange.End.LineNumber, startColumn, result, captureMatches, limitResultCount);
            return result;
        }

        var startCol = startLineNumber == searchRange.Start.LineNumber ? searchRange.Start.Column : 1;
        FindMatchesInNode(endPosition.Node, searcher, startLineNumber, startCol, start, end, searchData, captureMatches, limitResultCount, result);
        return result;
    }

    private void FindMatchesInNode(PieceTreeNode node, PieceTreeSearcher searcher, int startLineNumber, int startColumn, BufferCursor startCursor, BufferCursor endCursor, SearchData searchData, bool captureMatches, int limitResultCount, List<FindMatch> result)
    {
        var buffer = _buffers[node.Piece.BufferIndex];
        var startOffsetInBuffer = OffsetInBuffer(node.Piece.BufferIndex, node.Piece.Start);
        var start = OffsetInBuffer(node.Piece.BufferIndex, startCursor);
        var end = OffsetInBuffer(node.Piece.BufferIndex, endCursor);

        string searchText;
        
        // Assuming no word separators for now as per stub
        searchText = buffer.Buffer;
        searcher.Reset(start);

        Match? m;
        do
        {
            m = searcher.Next(searchText);
            if (m != null && m.Success)
            {
                if (m.Index >= end) return;

                var ret = PositionInBuffer(node, m.Index - startOffsetInBuffer);
                var lineFeedCnt = GetLineFeedCnt(node.Piece.BufferIndex, startCursor, ret);
                var retStartColumn = ret.Line == startCursor.Line ? ret.Column - startCursor.Column + startColumn : ret.Column + 1;
                var retEndColumn = retStartColumn + m.Length;
                
                var range = new Range(startLineNumber + lineFeedCnt, retStartColumn, startLineNumber + lineFeedCnt, retEndColumn);
                string[]? matches = null;
                if (captureMatches)
                {
                    matches = new string[m.Groups.Count];
                    for(int i=0; i<m.Groups.Count; i++) matches[i] = m.Groups[i].Value;
                }
                result.Add(new FindMatch(range, matches));

                if (m.Index + m.Length >= end) return;
                if (result.Count >= limitResultCount) return;
            }
        } while (m != null && m.Success);
    }

    private void FindMatchesInLine(SearchData searchData, PieceTreeSearcher searcher, string text, int lineNumber, int deltaOffset, List<FindMatch> result, bool captureMatches, int limitResultCount)
    {
        if (!captureMatches && searchData.SimpleSearch != null)
        {
            string searchString = searchData.SimpleSearch;
            int searchStringLen = searchString.Length;
            int lastMatchIndex = -searchStringLen;
            
            while ((lastMatchIndex = text.IndexOf(searchString, lastMatchIndex + searchStringLen, StringComparison.Ordinal)) != -1)
            {
                if (searchData.WordSeparators != null && !searchData.WordSeparators.IsValidMatch(text, lastMatchIndex, searchStringLen))
                {
                    continue;
                }

                result.Add(new FindMatch(new Range(lineNumber, lastMatchIndex + 1 + deltaOffset, lineNumber, lastMatchIndex + 1 + searchStringLen + deltaOffset), null));
                if (result.Count >= limitResultCount) return;
            }
            return;
        }

        searcher.Reset(0);
        Match? m;
        do
        {
            m = searcher.Next(text);
            if (m != null && m.Success)
            {
                string[]? matches = null;
                if (captureMatches)
                {
                    matches = new string[m.Groups.Count];
                    for(int i=0; i<m.Groups.Count; i++) matches[i] = m.Groups[i].Value;
                }
                result.Add(new FindMatch(new Range(lineNumber, m.Index + 1 + deltaOffset, lineNumber, m.Index + 1 + m.Length + deltaOffset), matches));
                if (result.Count >= limitResultCount) return;
            }
        } while (m != null && m.Success);
    }

    /// <summary>
    /// Finds the node containing the specified line and column position.
    /// Uses the search cache to short-circuit tree traversal when
    /// the cached node already covers the query (TS parity: nodeAt2).
    /// </summary>
    private NodeHit NodeAt2(int lineNumber, int column)
    {
        // Use GetOffsetAt + NodeAt for correct line/column to offset translation
        // This ensures the cache is properly populated and line tracking is consistent.
        // The cache optimization happens inside NodeAt for offset-based lookups.
        int offset = GetOffsetAt(lineNumber, column);
        return NodeAt(offset);
    }

    public int GetOffsetAt(int lineNumber, int column)
    {
        int leftLen = 0;
        var x = _root;

        while (!ReferenceEquals(x, _sentinel))
        {
            if (!ReferenceEquals(x.Left, _sentinel) && x.LineFeedsLeft + 1 >= lineNumber)
            {
                x = x.Left;
            }
            else if (x.LineFeedsLeft + x.Piece.LineFeedCount + 1 >= lineNumber)
            {
                leftLen += x.SizeLeft;
                int accumulatedValInCurrentIndex = GetAccumulatedValue(x, lineNumber - x.LineFeedsLeft - 2);
                return leftLen + accumulatedValInCurrentIndex + column - 1;
            }
            else
            {
                lineNumber -= x.LineFeedsLeft + x.Piece.LineFeedCount;
                leftLen += x.SizeLeft + x.Piece.Length;
                x = x.Right;
            }
        }
        return leftLen;
    }

    private LineIndexResult GetIndexOf(PieceTreeNode node, int accumulatedValue)
    {
        if (ReferenceEquals(node, _sentinel))
        {
            return new LineIndexResult(0, 0);
        }

        var piece = node.Piece;
        var position = PositionInBuffer(node, accumulatedValue);
        var lineDelta = position.Line - piece.Start.Line;

        var pieceStartOffset = OffsetInBuffer(piece.BufferIndex, piece.Start);
        var pieceEndOffset = OffsetInBuffer(piece.BufferIndex, piece.End);
        if (pieceEndOffset - pieceStartOffset == accumulatedValue)
        {
            var realLineCount = GetLineFeedCnt(piece.BufferIndex, piece.Start, position);
            if (realLineCount != lineDelta)
            {
                return new LineIndexResult(realLineCount, 0);
            }
        }

        return new LineIndexResult(lineDelta, position.Column);
    }

    private int GetCharCode(NodeHit nodePos)
    {
        var node = nodePos.Node;
        if (node is null || ReferenceEquals(node, _sentinel))
        {
            return 0;
        }

        if (nodePos.Remainder == node.Piece.Length)
        {
            var next = node.Next();
            if (ReferenceEquals(next, _sentinel) || next is null)
            {
                return 0;
            }

            var nextBuffer = _buffers[next.Piece.BufferIndex];
            var nextOffset = OffsetInBuffer(next.Piece.BufferIndex, next.Piece.Start);
            return nextOffset < nextBuffer.Length ? nextBuffer.Buffer[nextOffset] : 0;
        }

        var buffer = _buffers[node.Piece.BufferIndex];
        var startOffset = OffsetInBuffer(node.Piece.BufferIndex, node.Piece.Start);
        var targetOffset = startOffset + nodePos.Remainder;
        if ((uint)targetOffset >= (uint)buffer.Length)
        {
            return 0;
        }

        return buffer.Buffer[targetOffset];
    }

    public TextPosition GetPositionAt(int offset)
    {
        if (IsEmpty)
        {
            return TextPosition.Origin;
        }

        offset = Math.Clamp(offset, 0, TotalLength);

        var node = _root;
        var lineCount = 0;
        var originalOffset = offset;

        while (!ReferenceEquals(node, _sentinel))
        {
            if (node.SizeLeft != 0 && node.SizeLeft >= offset)
            {
                node = node.Left;
                continue;
            }

            if (node.SizeLeft + node.Piece.Length >= offset)
            {
                var relative = offset - node.SizeLeft;
                var index = GetIndexOf(node, relative);
                lineCount += node.LineFeedsLeft + index.LineDelta;

                if (index.LineDelta == 0)
                {
                    var lineStartOffset = GetOffsetAt(lineCount + 1, 1);
                    var column = originalOffset - lineStartOffset;
                    return new TextPosition(lineCount + 1, column + 1);
                }

                return new TextPosition(lineCount + 1, index.Column + 1);
            }

            offset -= node.SizeLeft + node.Piece.Length;
            lineCount += node.LineFeedsLeft + node.Piece.LineFeedCount;
            node = node.Right;
        }

        return new TextPosition(lineCount + 1, 1);
    }

    public int GetLineLength(int lineNumber)
    {
        if (lineNumber < 1)
        {
            return 0;
        }

        var lineCount = GetLineCount();
        if (lineCount == 0)
        {
            return 0;
        }

        if (lineNumber >= lineCount)
        {
            var startOffset = GetOffsetAt(lineCount, 1);
            return TotalLength - startOffset;
        }

        var start = GetOffsetAt(lineNumber, 1);
        var end = GetOffsetAt(lineNumber + 1, 1);
        var lineBreakLength = GetLineBreakLengthBefore(end);
        return Math.Max(0, end - start - lineBreakLength);
    }

    /// <summary>
    /// Determines the actual line break length (accounting for CRLF pairs) that ends the
    /// line preceding <paramref name="nextLineOffset"/>. This mirrors the TS behavior where
    /// getLineLength subtracts the precise terminator width rather than the configured EOL.
    /// </summary>
    private int GetLineBreakLengthBefore(int nextLineOffset)
    {
        if (nextLineOffset <= 0 || TotalLength == 0)
        {
            return 0;
        }

        var lastCharOffset = nextLineOffset - 1;
        var lastChar = GetCharCode(lastCharOffset);
        if (lastChar == '\n')
        {
            var prevChar = lastCharOffset - 1 >= 0
                ? GetCharCode(lastCharOffset - 1)
                : 0;
            return prevChar == '\r' ? 2 : 1;
        }

        if (lastChar == '\r' || lastChar == '\u2028' || lastChar == '\u2029' || lastChar == '\u0085')
        {
            return 1;
        }

        return 0;
    }

    public int GetLineCount() => TotalLineFeeds + 1;

    public int GetCharCode(int offset)
    {
        if (offset < 0 || TotalLength == 0)
        {
            return 0;
        }

        var clamped = Math.Min(offset, TotalLength - 1);
        var nodePos = NodeAt(clamped);
        return GetCharCode(nodePos);
    }

    public int GetLineCharCode(int lineNumber, int columnIndex)
    {
        if (lineNumber < 1 || columnIndex < 0)
        {
            return 0;
        }

        var nodePos = NodeAt2(lineNumber, columnIndex + 1);
        return GetCharCode(nodePos);
    }

    /// <summary>
    /// Computes the accumulated byte offset from the beginning of a piece to the end of the
    /// specified line index within that piece.
    /// 
    /// When EOL is normalized to \n only, this method uses the buffer's LineStarts array for O(1)
    /// computation (TS parity: getAccumulatedValue). Otherwise, it falls back to character scanning
    /// to properly handle CRLF pairs as single line breaks.
    /// </summary>
    /// <param name="node">The piece tree node.</param>
    /// <param name="index">The 0-based line index within the piece (-1 returns 0).</param>
    /// <returns>The byte offset from piece start to the end of the specified line.</returns>
    private int GetAccumulatedValue(PieceTreeNode node, int index)
    {
        if (index < 0)
        {
            return 0;
        }

        var piece = node.Piece;
        var lineStarts = _buffers[piece.BufferIndex].LineStarts;
        var expectedLineStartIndex = piece.Start.Line + index + 1;
        var startOffset = lineStarts[piece.Start.Line] + piece.Start.Column;

        if (expectedLineStartIndex > piece.End.Line)
        {
            var endOffset = lineStarts[piece.End.Line] + piece.End.Column;
            return endOffset - startOffset;
        }

        return lineStarts[expectedLineStartIndex] - startOffset;
    }

    public string GetLineContent(int lineNumber)
    {
        if (_lastVisitedLine.LineNumber == lineNumber && _lastVisitedLine.Value != null)
        {
            return _lastVisitedLine.Value;
        }

        _lastVisitedLine.LineNumber = lineNumber;
        var lastLineNumber = TotalLineFeeds + 1;
        if (lineNumber == lastLineNumber)
        {
            _lastVisitedLine.Value = GetLineRawContent(lineNumber);
        }
        else if (_eolNormalized)
        {
            _lastVisitedLine.Value = GetLineRawContent(lineNumber, _eol.Length);
        }
        else
        {
            var rawContent = GetLineRawContent(lineNumber);
            _lastVisitedLine.Value = TrimTrailingLineFeed(rawContent);
        }

        return _lastVisitedLine.Value;
    }

    public string GetLineRawContent(int lineNumber, int endOffset = 0)
    {
        PieceTreeNode x = _root;
        int relativeLineNumber = lineNumber;

        if (_searchCache.TryGetByLine(lineNumber, out var cachedNode, out _, out var cachedStartLine))
        {
            x = cachedNode;
            relativeLineNumber = lineNumber - (cachedStartLine - 1);
            return GetContentFromNode(x, relativeLineNumber, endOffset);
        }

        while (!ReferenceEquals(x, _sentinel))
        {
             if (!ReferenceEquals(x.Left, _sentinel) && x.LineFeedsLeft >= relativeLineNumber - 1)
             {
                 x = x.Left;
             }
             else if (x.LineFeedsLeft + x.Piece.LineFeedCount > relativeLineNumber - 1)
             {
                 relativeLineNumber -= x.LineFeedsLeft;
                 _searchCache.Remember(x, GetOffsetOfNode(x), lineNumber - (relativeLineNumber - 1));
                 return GetContentFromNode(x, relativeLineNumber, endOffset);
             }
             else if (x.LineFeedsLeft + x.Piece.LineFeedCount == relativeLineNumber - 1)
             {
                 relativeLineNumber -= x.LineFeedsLeft;
                 _searchCache.Remember(x, GetOffsetOfNode(x), lineNumber - (relativeLineNumber - 1));
                 return GetContentFromNode(x, relativeLineNumber, endOffset);
             }
             else
             {
                 relativeLineNumber -= x.LineFeedsLeft + x.Piece.LineFeedCount;
                 x = x.Right;
             }
        }
        
        return "";
    }

    private string GetContentFromNode(PieceTreeNode x, int relativeLineNumber, int endOffset)
    {
#if DEBUG
        PieceTreeDebug.Log($"DEBUG GetContentFromNode: NodeBufIdx={x.Piece.BufferIndex}, NodeStart={x.Piece.Start}, NodeEnd={x.Piece.End}, NodeLen={x.Piece.Length}, NodeLF={x.Piece.LineFeedCount}, relativeLineNumber={relativeLineNumber}, endOffset={endOffset}");
        var lineStartsArr = string.Join(",", _buffers[x.Piece.BufferIndex].LineStarts);
        PieceTreeDebug.Log($"DEBUG GetContentFromNode: buffer[{x.Piece.BufferIndex}].LineStarts=[{lineStartsArr}], bufferLen={_buffers[x.Piece.BufferIndex].Length}");
#endif
        int prevAccumulatedValue = GetAccumulatedValue(x, relativeLineNumber - 2);
        int accumulatedValue = GetAccumulatedValue(x, relativeLineNumber - 1);
        var buffer = _buffers[x.Piece.BufferIndex].Buffer;
        var startOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);

        string ret;
        if (relativeLineNumber - 1 < x.Piece.LineFeedCount)
        {
            var sliceStart = startOffset + prevAccumulatedValue;
            var pieceEndOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.End);
            var sliceEnd = FindLineBreakBoundary(buffer, sliceStart, pieceEndOffset);
            var length = Math.Max(0, sliceEnd - sliceStart - endOffset);
            ret = length == 0 ? string.Empty : buffer.Substring(sliceStart, length);
        }
        else
        {
             ret = buffer.Substring(startOffset + prevAccumulatedValue, x.Piece.Length - prevAccumulatedValue);
             
             var next = x.Next();
             while (!ReferenceEquals(next, _sentinel))
             {
                var buf = _buffers[next.Piece.BufferIndex].Buffer;
                if (next.Piece.LineFeedCount > 0)
                {
                    int acc = GetAccumulatedValue(next, 0);
                    var st = OffsetInBuffer(next.Piece.BufferIndex, next.Piece.Start);
                    if (acc - endOffset < 0)
                    {
                        PieceTreeDebug.Log($"DEBUG GetContentFromNode cross-node negative len: nextBuf={next.Piece.BufferIndex}, start={st}, acc={acc}, endOffset={endOffset}, pieceStart={next.Piece.Start}, pieceEnd={next.Piece.End}, pieceLen={next.Piece.Length}, pieceLF={next.Piece.LineFeedCount}, bufLen={buf.Length}");
                    }
                    ret += buf.Substring(st, acc - endOffset);
                    break; // Found end of line
                }
                else
                {
                    var st = OffsetInBuffer(next.Piece.BufferIndex, next.Piece.Start);
                    ret += buf.Substring(st, next.Piece.Length);
                }
                next = next.Next();
             }
        }

        // Backward traversal if we started at the beginning of the node
        if (relativeLineNumber == 1)
        {
            var p = x.Prev();
            while (!ReferenceEquals(p, _sentinel))
            {
                var pBuf = _buffers[p.Piece.BufferIndex].Buffer;
                var pStart = OffsetInBuffer(p.Piece.BufferIndex, p.Piece.Start);
                
                if (p.Piece.LineFeedCount == 0)
                {
                    ret = pBuf.Substring(pStart, p.Piece.Length) + ret;
                }
                else
                {
                    int lastLFOffset = GetAccumulatedValue(p, p.Piece.LineFeedCount - 1);
                    int startInP = lastLFOffset;
                    int len = p.Piece.Length - startInP;
                    ret = pBuf.Substring(pStart + startInP, len) + ret;
                    break;
                }
                p = p.Prev();
            }
        }

        return ret;
    }

    private static int FindLineBreakBoundary(string buffer, int startOffset, int sliceEndExclusive)
    {
        var index = startOffset;
        var limit = Math.Min(sliceEndExclusive, buffer.Length);
        while (index < limit)
        {
            var ch = buffer[index];
            index++;
            if (ch == '\r')
            {
                if (index < limit && buffer[index] == '\n')
                {
                    index++;
                }
                break;
            }

            if (ch == '\n')
            {
                break;
            }
        }

        return Math.Min(index, limit);
    }

    private static string TrimTrailingLineFeed(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var lastChar = text[^1];
        if (lastChar == '\n')
        {
            if (text.Length >= 2 && text[^2] == '\r')
            {
                return text.Substring(0, text.Length - 2);
            }

            return text.Substring(0, text.Length - 1);
        }

        if (lastChar == '\r')
        {
            return text.Substring(0, text.Length - 1);
        }

        return text;
    }
}
