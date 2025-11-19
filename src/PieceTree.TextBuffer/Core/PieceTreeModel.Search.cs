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
        if (index < 0) return 0;
        var piece = node.Piece;
        var lineStarts = _buffers[piece.BufferIndex].LineStarts;
        var expectedLineStartIndex = piece.Start.Line + index + 1;
        if (expectedLineStartIndex > piece.End.Line)
        {
            return lineStarts[piece.End.Line] + piece.End.Column - lineStarts[piece.Start.Line] - piece.Start.Column;
        }
        else
        {
            return lineStarts[expectedLineStartIndex] - lineStarts[piece.Start.Line] - piece.Start.Column;
        }
    }

    public string GetLineContent(int lineNumber)
    {
        if (_lastVisitedLine.LineNumber == lineNumber && _lastVisitedLine.Value != null)
        {
            return _lastVisitedLine.Value;
        }
        _lastVisitedLine.LineNumber = lineNumber;
        _lastVisitedLine.Value = GetLineRawContent(lineNumber);
        return _lastVisitedLine.Value;
    }

    public string GetLineRawContent(int lineNumber, int endOffset = 0)
    {
        var x = _root;
        string ret = "";
        
        while (!ReferenceEquals(x, _sentinel))
        {
             if (!ReferenceEquals(x.Left, _sentinel) && x.LineFeedsLeft >= lineNumber - 1)
             {
                 x = x.Left;
             }
             else if (x.LineFeedsLeft + x.Piece.LineFeedCount > lineNumber - 1)
             {
                 int prevAccumulatedValue = GetAccumulatedValue(x, lineNumber - x.LineFeedsLeft - 2);
                 int accumulatedValue = GetAccumulatedValue(x, lineNumber - x.LineFeedsLeft - 1);
                 var buffer = _buffers[x.Piece.BufferIndex].Buffer;
                 var startOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);
                 
                 return buffer.Substring(startOffset + prevAccumulatedValue, accumulatedValue - prevAccumulatedValue - endOffset);
             }
             else if (x.LineFeedsLeft + x.Piece.LineFeedCount == lineNumber - 1)
             {
                 int prevAccumulatedValue = GetAccumulatedValue(x, lineNumber - x.LineFeedsLeft - 2);
                 var buffer = _buffers[x.Piece.BufferIndex].Buffer;
                 var startOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);
                 ret = buffer.Substring(startOffset + prevAccumulatedValue, x.Piece.Length - prevAccumulatedValue);
                 break;
             }
             else
             {
                 lineNumber -= x.LineFeedsLeft + x.Piece.LineFeedCount;
                 x = x.Right;
             }
        }
        
        x = x.Next();
        while (!ReferenceEquals(x, _sentinel))
        {
            var buffer = _buffers[x.Piece.BufferIndex].Buffer;
            if (x.Piece.LineFeedCount > 0)
            {
                int accumulatedValue = GetAccumulatedValue(x, 0);
                var startOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);
                ret += buffer.Substring(startOffset, accumulatedValue - endOffset);
                return ret;
            }
            else
            {
                var startOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);
                ret += buffer.Substring(startOffset, x.Piece.Length);
            }
            x = x.Next();
        }
        
        return ret;
    }
}
