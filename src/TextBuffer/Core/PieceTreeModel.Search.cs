// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Class: PieceTreeBase - Search/FindMatches operations (Lines: 1500-1800)
// Ported: 2025-11-19

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PieceTree.TextBuffer.Core;

internal sealed partial class PieceTreeModel
{
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

    private NodeHit NodeAt2(int lineNumber, int column)
    {
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

    private int GetAccumulatedValue(PieceTreeNode node, int index)
    {
        if (index < 0)
        {
            return 0;
        }

        var piece = node.Piece;
        var buffer = _buffers[piece.BufferIndex].Buffer;
        var startOffset = OffsetInBuffer(piece.BufferIndex, piece.Start);
        var endOffset = OffsetInBuffer(piece.BufferIndex, piece.End);
        if (endOffset <= startOffset)
        {
            return 0;
        }

        var span = buffer.AsSpan(startOffset, endOffset - startOffset);
        var consumed = 0;
        var lineBreaksSeen = 0;

        while (consumed < span.Length)
        {
            var ch = span[consumed];
            consumed++;

            if (ch == '\r')
            {
                if (consumed < span.Length && span[consumed] == '\n')
                {
                    consumed++;
                }

                lineBreaksSeen++;
            }
            else if (ch == '\n')
            {
                lineBreaksSeen++;
            }

            if (lineBreaksSeen >= index + 1)
            {
                break;
            }
        }

        return consumed;
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
