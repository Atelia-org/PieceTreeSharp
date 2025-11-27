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
        List<FindMatch> result = [];
        PieceTreeSearcher searcher = new(searchData.WordSeparators, searchData.Regex);

        NodeHit startPosition = NodeAt2(searchRange.Start.LineNumber, searchRange.Start.Column);
        PieceTreeNode? startNode = startPosition.Node;
        if (startNode is null)
        {
            return result;
        }

        NodeHit endPosition = NodeAt2(searchRange.End.LineNumber, searchRange.End.Column);
        PieceTreeNode? endNodeCandidate = endPosition.Node;
        if (endNodeCandidate is null)
        {
            return result;
        }

        BufferCursor start = PositionInBuffer(startNode, startPosition.Remainder);
        BufferCursor end = PositionInBuffer(endNodeCandidate, endPosition.Remainder);

        if (ReferenceEquals(startNode, endNodeCandidate))
        {
            FindMatchesInNode(startNode, searcher, searchRange.Start.LineNumber, searchRange.Start.Column, start, end, searchData, captureMatches, limitResultCount, result);
            return result;
        }

        int startLineNumber = searchRange.Start.LineNumber;
        PieceTreeNode? currentNode = startNode;
        PieceTreeNode endNode = endNodeCandidate!;

        while (!ReferenceEquals(currentNode, endNode))
        {
            if (currentNode is null)
            {
                return result;
            }

            PieceTreeNode node = currentNode;
            int lineBreakCnt = GetLineFeedCnt(node.Piece.BufferIndex, start, node.Piece.End);
            if (PieceTreeDebug.IsEnabled)
            {
                int bufferIndex = node.Piece.BufferIndex;
                PieceTreeDebug.Log($"FindMatches loop: nodeBufIdx={bufferIndex}, startLine={startLineNumber}, lineBreakCnt={lineBreakCnt}");
            }

            if (lineBreakCnt >= 1)
            {
                IReadOnlyList<int> lineStarts = _buffers[node.Piece.BufferIndex].LineStarts;
                int startOffsetInBuffer = OffsetInBuffer(node.Piece.BufferIndex, node.Piece.Start);
                int nextLineStartOffset = lineStarts[start.Line + lineBreakCnt];
                int startColumn = startLineNumber == searchRange.Start.LineNumber ? searchRange.Start.Column : 1;

                BufferCursor endCursor = PositionInBuffer(node, nextLineStartOffset - startOffsetInBuffer);

                FindMatchesInNode(node, searcher, startLineNumber, startColumn, start, endCursor, searchData, captureMatches, limitResultCount, result);

                if (result.Count >= limitResultCount)
                {
                    return result;
                }

                startLineNumber += lineBreakCnt;
            }

            int startColumnForLine = startLineNumber == searchRange.Start.LineNumber ? searchRange.Start.Column - 1 : 0;

            // search for the remaining content
            if (startLineNumber == searchRange.End.LineNumber)
            {
                string text = GetLineContent(startLineNumber, startPosition).Substring(startColumnForLine, searchRange.End.Column - 1 - startColumnForLine);
                FindMatchesInLine(searchData, searcher, text, searchRange.End.LineNumber, startColumnForLine, result, captureMatches, limitResultCount);
                return result;
            }

            string lineContent = GetLineContent(startLineNumber, startPosition);
            if (startColumnForLine < lineContent.Length)
            {
                FindMatchesInLine(searchData, searcher, lineContent.Substring(startColumnForLine), startLineNumber, startColumnForLine, result, captureMatches, limitResultCount);
                if (PieceTreeDebug.IsEnabled)
                {
                    PieceTreeDebug.Log($"FindMatches processed line={startLineNumber} via per-line search");
                }
            }

            if (result.Count >= limitResultCount)
            {
                return result;
            }

            startLineNumber++;
            startPosition = NodeAt2(startLineNumber, 1);
            PieceTreeNode? nextNode = startPosition.Node;
            if (nextNode is null)
            {
                return result;
            }

            currentNode = nextNode;
            start = PositionInBuffer(nextNode, startPosition.Remainder);
        }

        if (PieceTreeDebug.IsEnabled)
        {
            PieceTreeDebug.Log(
                $"FindMatchesLineByLine finalSegment: startLine={startLineNumber}, endLine={searchRange.End.LineNumber}, endColumn={searchRange.End.Column}, " +
                $"startNodeLine={startPosition.NodeStartLineNumber}, remainder={startPosition.Remainder}, nodeBufIdx={startPosition.Node?.Piece.BufferIndex}");
        }

        if (startLineNumber == searchRange.End.LineNumber)
        {
            int startColumn = startLineNumber == searchRange.Start.LineNumber ? searchRange.Start.Column - 1 : 0;
            string text = GetLineContent(startLineNumber, startPosition).Substring(startColumn, searchRange.End.Column - 1 - startColumn);
            FindMatchesInLine(searchData, searcher, text, searchRange.End.LineNumber, startColumn, result, captureMatches, limitResultCount);
            return result;
        }

        int startCol = startLineNumber == searchRange.Start.LineNumber ? searchRange.Start.Column : 1;
        FindMatchesInNode(endNode, searcher, startLineNumber, startCol, start, end, searchData, captureMatches, limitResultCount, result);
        return result;
    }

    private void FindMatchesInNode(PieceTreeNode node, PieceTreeSearcher searcher, int startLineNumber, int startColumn, BufferCursor startCursor, BufferCursor endCursor, SearchData searchData, bool captureMatches, int limitResultCount, List<FindMatch> result)
    {
        ChunkBuffer buffer = _buffers[node.Piece.BufferIndex];
        int startOffsetInBuffer = OffsetInBuffer(node.Piece.BufferIndex, node.Piece.Start);
        int start = OffsetInBuffer(node.Piece.BufferIndex, startCursor);
        int end = OffsetInBuffer(node.Piece.BufferIndex, endCursor);

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
                if (m.Index >= end)
                {
                    return;
                }

                BufferCursor ret = PositionInBuffer(node, m.Index - startOffsetInBuffer);
                int lineFeedCnt = GetLineFeedCnt(node.Piece.BufferIndex, startCursor, ret);
                int retStartColumn = ret.Line == startCursor.Line ? ret.Column - startCursor.Column + startColumn : ret.Column + 1;
                int retEndColumn = retStartColumn + m.Length;

                Range range = new(startLineNumber + lineFeedCnt, retStartColumn, startLineNumber + lineFeedCnt, retEndColumn);
                string[]? matches = null;
                if (captureMatches)
                {
                    matches = new string[m.Groups.Count];
                    for (int i = 0; i < m.Groups.Count; i++)
                    {
                        matches[i] = m.Groups[i].Value;
                    }
                }
                    var findMatch = new FindMatch(range, matches);
                    result.Add(findMatch);
                    if (PieceTreeDebug.IsEnabled)
                    {
                        PieceTreeDebug.Log($"FindMatchesInNode hit: range={findMatch.Range}");
                    }

                if (m.Index + m.Length >= end)
                {
                    return;
                }

                if (result.Count >= limitResultCount)
                {
                    return;
                }
            }
        } while (m != null && m.Success);
    }

    private void FindMatchesInLine(SearchData searchData, PieceTreeSearcher searcher, string text, int lineNumber, int deltaOffset, List<FindMatch> result, bool captureMatches, int limitResultCount)
    {
        if (!captureMatches && searchData.SimpleSearch != null)
        {
            var searchString = searchData.SimpleSearch;
            var searchStringLen = searchString.Length;
            var lastMatchIndex = -searchStringLen;

            while ((lastMatchIndex = text.IndexOf(searchString, lastMatchIndex + searchStringLen, StringComparison.Ordinal)) != -1)
            {
                if (searchData.WordSeparators != null && !searchData.WordSeparators.IsValidMatch(text, lastMatchIndex, searchStringLen))
                {
                    continue;
                }

                var literalMatch = new FindMatch(new Range(lineNumber, lastMatchIndex + 1 + deltaOffset, lineNumber, lastMatchIndex + 1 + searchStringLen + deltaOffset), null);
                result.Add(literalMatch);
                if (PieceTreeDebug.IsEnabled)
                {
                    PieceTreeDebug.Log($"FindMatchesInLine literal hit: line={lineNumber}, start={literalMatch.Range.Start.Column}");
                }

                if (result.Count >= limitResultCount)
                {
                    return;
                }
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
                    for (var i = 0; i < m.Groups.Count; i++)
                    {
                        matches[i] = m.Groups[i].Value;
                    }
                }

                var regexMatch = new FindMatch(new Range(lineNumber, m.Index + 1 + deltaOffset, lineNumber, m.Index + 1 + m.Length + deltaOffset), matches);
                result.Add(regexMatch);
                if (PieceTreeDebug.IsEnabled)
                {
                    PieceTreeDebug.Log($"FindMatchesInLine regex hit: line={lineNumber}, start={regexMatch.Range.Start.Column}");
                }

                if (result.Count >= limitResultCount)
                {
                    return;
                }
            }
        }
        while (m != null && m.Success);
    }

    /// <summary>
    /// Finds the node containing the specified line and column position.
    /// Mirrors VS Code's nodeAt2 implementation by descending the tree once,
    /// capturing the (node, nodeStartOffset, nodeStartLineNumber) tuple, and
    /// falling back to a forward walk when the requested column overflows the node.
    /// </summary>
    private NodeHit NodeAt2(int lineNumber, int column)
        => NodeAt2Internal(lineNumber, column, allowCache: true);

    private NodeHit NodeAt2Internal(int lineNumber, int column, bool allowCache)
    {
        if (IsEmpty)
        {
            return default;
        }

        int targetLine = Math.Clamp(lineNumber, 1, GetLineCount());
        int targetColumn = Math.Max(1, column);

        if (allowCache && _searchCache.TryGetByLine(targetLine, out PieceTreeNode? cachedNode, out int cachedStartOffset, out int cachedStartLine))
        {
            var cachedHit = ResolveLineHit(cachedNode, cachedStartOffset, cachedStartLine, targetLine, targetColumn);
            if (!EnsureCachedLineHitMatchesRequest(cachedHit, targetLine, targetColumn))
            {
                return NodeAt2Internal(targetLine, targetColumn, allowCache: false);
            }

            return cachedHit;
        }

        PieceTreeNode x = _root;
        int offsetBase = 0;
        int remainingLine = targetLine;
        int remainingColumn = targetColumn;

        while (!ReferenceEquals(x, _sentinel))
        {
            if (!ReferenceEquals(x.Left, _sentinel) && x.LineFeedsLeft >= remainingLine - 1)
            {
                x = x.Left;
                continue;
            }

            int leftPlusPiece = x.LineFeedsLeft + x.Piece.LineFeedCount;
            if (leftPlusPiece > remainingLine - 1)
            {
                int relativeLineNumber = remainingLine - x.LineFeedsLeft;
                int nodeStartOffset = offsetBase + x.SizeLeft;
                int nodeStartLineNumber = targetLine - (relativeLineNumber - 1);
                int prev = GetAccumulatedValue(x, relativeLineNumber - 2);
                int accumulated = GetAccumulatedValue(x, relativeLineNumber - 1);
                int remainder = Math.Min(prev + remainingColumn - 1, accumulated);
                _searchCache.Remember(x, nodeStartOffset, nodeStartLineNumber);
                return new NodeHit(x, remainder, nodeStartOffset, nodeStartLineNumber);
            }

            if (leftPlusPiece == remainingLine - 1)
            {
                int relativeLineNumber = remainingLine - x.LineFeedsLeft;
                int nodeStartOffset = offsetBase + x.SizeLeft;
                int nodeStartLineNumber = targetLine - (relativeLineNumber - 1);
                int prev = GetAccumulatedValue(x, relativeLineNumber - 2);
                int available = Math.Max(0, x.Piece.Length - prev);
                if (remainingColumn - 1 <= available)
                {
                    int remainder = prev + remainingColumn - 1;
                    _searchCache.Remember(x, nodeStartOffset, nodeStartLineNumber);
                    return new NodeHit(x, remainder, nodeStartOffset, nodeStartLineNumber);
                }

                remainingColumn -= available;
                return SearchForwardFromNode(x, nodeStartOffset, nodeStartLineNumber, remainingColumn);
            }

            remainingLine -= leftPlusPiece;
            offsetBase += x.SizeLeft + x.Piece.Length;
            x = x.Right;
        }

        return default;
    }

    private NodeHit ResolveLineHit(PieceTreeNode node, int nodeStartOffset, int nodeStartLineNumber, int lineNumber, int column)
    {
        if (node is null || ReferenceEquals(node, _sentinel))
        {
            return default;
        }
        
        if (PieceTreeDebug.IsEnabled)
        {
            PieceTreeDebug.Log($"ResolveLineHit: requestedLine={lineNumber}, column={column}, nodeBufIdx={node.Piece.BufferIndex}, nodeStartLine={nodeStartLineNumber}");
        }

        int relativeLineNumber = ComputeRelativeLineNumber(nodeStartLineNumber, lineNumber);
        int prev = GetAccumulatedValue(node, relativeLineNumber - 2);
        int accumulated = GetAccumulatedValue(node, relativeLineNumber - 1);
        if (PieceTreeDebug.IsEnabled)
        {
            PieceTreeDebug.Log($"ResolveLineHit: relativeLine={relativeLineNumber}, prev={prev}, accumulated={accumulated}");
            int absoluteOffset = nodeStartOffset + Math.Min(node.Piece.Length, Math.Max(0, prev + column - 1));
            var position = GetPositionAt(Math.Clamp(absoluteOffset, 0, TotalLength));
            PieceTreeDebug.Log($"ResolveLineHit absolute position => {position.LineNumber}:{position.Column}");
        }

        if (relativeLineNumber - 1 < node.Piece.LineFeedCount)
        {
            int remainder = Math.Min(prev + column - 1, accumulated);
            return new NodeHit(node, remainder, nodeStartOffset, nodeStartLineNumber);
        }

        int available = Math.Max(0, node.Piece.Length - prev);
        if (column - 1 <= available)
        {
            int remainder = prev + column - 1;
            return new NodeHit(node, remainder, nodeStartOffset, nodeStartLineNumber);
        }

        int remainingColumn = column - available;
        return SearchForwardFromNode(node, nodeStartOffset, nodeStartLineNumber, remainingColumn);
    }

    private NodeHit SearchForwardFromNode(PieceTreeNode startNode, int nodeStartOffset, int nodeStartLineNumber, int remainingColumn)
    {
        int offset = nodeStartOffset + startNode.Piece.Length;
        int currentLineStart = nodeStartLineNumber + startNode.Piece.LineFeedCount;
        PieceTreeNode current = startNode.Next();
        int targetColumn = Math.Max(1, remainingColumn);

        while (!ReferenceEquals(current, _sentinel))
        {
            if (targetColumn <= 1)
            {
                _searchCache.Remember(current, offset, currentLineStart);
                return new NodeHit(current, 0, offset, currentLineStart);
            }

            if (current.Piece.LineFeedCount > 0)
            {
                int accumulated = GetAccumulatedValue(current, 0);
                int remainder = Math.Min(targetColumn - 1, accumulated);
                _searchCache.Remember(current, offset, currentLineStart);
                return new NodeHit(current, remainder, offset, currentLineStart);
            }

            if (current.Piece.Length >= targetColumn - 1)
            {
                int remainder = targetColumn - 1;
                _searchCache.Remember(current, offset, currentLineStart);
                return new NodeHit(current, remainder, offset, currentLineStart);
            }

            targetColumn -= current.Piece.Length;
            offset += current.Piece.Length;
            currentLineStart += current.Piece.LineFeedCount;
            current = current.Next();
        }

        return default;
    }

    private bool EnsureCachedLineHitMatchesRequest(NodeHit hit, int lineNumber, int column)
    {
        if (hit.Node is null || ReferenceEquals(hit.Node, _sentinel))
        {
            return false;
        }

        int absoluteOffset = hit.NodeStartOffset + Math.Clamp(hit.Remainder, 0, Math.Max(0, hit.Node.Piece.Length));
        TextPosition position = GetPositionAt(Math.Clamp(absoluteOffset, 0, Math.Max(0, TotalLength)));
        return position.LineNumber == lineNumber && position.Column == column;
    }

    private static int ComputeRelativeLineNumber(int nodeStartLineNumber, int absoluteLineNumber)
    {
        if (nodeStartLineNumber <= 0)
        {
            return 1;
        }

        return Math.Max(1, absoluteLineNumber - nodeStartLineNumber + 1);
    }

    private static int ComputeRelativeLineNumber(NodeHit hit, int absoluteLineNumber)
        => ComputeRelativeLineNumber(hit.NodeStartLineNumber, absoluteLineNumber);

    public int GetOffsetAt(int lineNumber, int column)
    {
        int leftLen = 0;
        PieceTreeNode x = _root;

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

        PieceSegment piece = node.Piece;
        BufferCursor position = PositionInBuffer(node, accumulatedValue);
        int lineDelta = position.Line - piece.Start.Line;

        int pieceStartOffset = OffsetInBuffer(piece.BufferIndex, piece.Start);
        int pieceEndOffset = OffsetInBuffer(piece.BufferIndex, piece.End);
        if (pieceEndOffset - pieceStartOffset == accumulatedValue)
        {
            int realLineCount = GetLineFeedCnt(piece.BufferIndex, piece.Start, position);
            if (realLineCount != lineDelta)
            {
                return new LineIndexResult(realLineCount, 0);
            }
        }

        return new LineIndexResult(lineDelta, position.Column);
    }

    private int GetCharCode(NodeHit nodePos)
    {
        PieceTreeNode? node = nodePos.Node;
        if (node is null || ReferenceEquals(node, _sentinel))
        {
            return 0;
        }

        if (nodePos.Remainder == node.Piece.Length)
        {
            PieceTreeNode? next = node.Next();
            if (ReferenceEquals(next, _sentinel) || next is null)
            {
                return 0;
            }

            ChunkBuffer nextBuffer = _buffers[next.Piece.BufferIndex];
            int nextOffset = OffsetInBuffer(next.Piece.BufferIndex, next.Piece.Start);
            return nextOffset < nextBuffer.Length ? nextBuffer.Buffer[nextOffset] : 0;
        }

        ChunkBuffer buffer = _buffers[node.Piece.BufferIndex];
        int startOffset = OffsetInBuffer(node.Piece.BufferIndex, node.Piece.Start);
        int targetOffset = startOffset + nodePos.Remainder;
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

        PieceTreeNode node = _root;
        int lineCount = 0;
        int originalOffset = offset;

        while (!ReferenceEquals(node, _sentinel))
        {
            if (node.SizeLeft != 0 && node.SizeLeft >= offset)
            {
                node = node.Left;
                continue;
            }

            if (node.SizeLeft + node.Piece.Length >= offset)
            {
                int relative = offset - node.SizeLeft;
                LineIndexResult index = GetIndexOf(node, relative);
                lineCount += node.LineFeedsLeft + index.LineDelta;

                if (index.LineDelta == 0)
                {
                    int lineStartOffset = GetOffsetAt(lineCount + 1, 1);
                    int column = originalOffset - lineStartOffset;
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

        int lineCount = GetLineCount();
        if (lineCount == 0)
        {
            return 0;
        }

        if (lineNumber >= lineCount)
        {
            int startOffset = GetOffsetAt(lineCount, 1);
            return TotalLength - startOffset;
        }

        int start = GetOffsetAt(lineNumber, 1);
        int end = GetOffsetAt(lineNumber + 1, 1);
        int lineBreakLength = GetLineBreakLengthBefore(end);
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

        int lastCharOffset = nextLineOffset - 1;
        int lastChar = GetCharCode(lastCharOffset);
        if (lastChar == '\n')
        {
            int prevChar = lastCharOffset - 1 >= 0
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

        int clamped = Math.Min(offset, TotalLength - 1);
        NodeHit nodePos = NodeAt(clamped);
        return GetCharCode(nodePos);
    }

    public int GetLineCharCode(int lineNumber, int columnIndex)
    {
        if (lineNumber < 1 || columnIndex < 0)
        {
            return 0;
        }

        NodeHit nodePos = NodeAt2(lineNumber, columnIndex + 1);
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

        if (!ShouldCheckCRLF())
        {
            PieceSegment piece = node.Piece;
            IReadOnlyList<int> lineStarts = _buffers[piece.BufferIndex].LineStarts;
            int expectedLineStartIndex = piece.Start.Line + index + 1;
            int startOffset = lineStarts[piece.Start.Line] + piece.Start.Column;

            if (expectedLineStartIndex > piece.End.Line)
            {
                int endOffset = lineStarts[piece.End.Line] + piece.End.Column;
                return endOffset - startOffset;
            }

            return lineStarts[expectedLineStartIndex] - startOffset;
        }

        return GetAccumulatedValueByScan(node, index);
    }

    private int GetAccumulatedValueByScan(PieceTreeNode node, int index)
    {
        PieceSegment piece = node.Piece;
        string buffer = _buffers[piece.BufferIndex].Buffer;
        int startOffset = OffsetInBuffer(piece.BufferIndex, piece.Start);
        int endOffset = OffsetInBuffer(piece.BufferIndex, piece.End);
        if (endOffset <= startOffset)
        {
            return 0;
        }

        int length = endOffset - startOffset;
        ReadOnlySpan<char> span = buffer.AsSpan(startOffset, length);
        if (index >= piece.LineFeedCount)
        {
            return length;
        }

        int remainingBreaks = index + 1;
        int position = 0;
        while (position < span.Length)
        {
            char ch = span[position];
            position++;
            if (ch == '\r')
            {
                if (position < span.Length && span[position] == '\n')
                {
                    position++;
                }
                remainingBreaks--;
            }
            else if (ch == '\n')
            {
                remainingBreaks--;
            }

            if (remainingBreaks == 0)
            {
                return position;
            }
        }

        return span.Length;
    }

    public string GetLineContent(int lineNumber)
    {
        if (_lastVisitedLine.LineNumber == lineNumber && _lastVisitedLine.Value != null)
        {
            return _lastVisitedLine.Value;
        }

        string value = ResolveLineContent(lineNumber, null);
        _lastVisitedLine.LineNumber = lineNumber;
        _lastVisitedLine.Value = value;
        return value;
    }

    private string GetLineContent(int lineNumber, NodeHit? hint)
    {
        if (!hint.HasValue || hint.Value.NodeStartLineNumber <= 0)
        {
            return GetLineContent(lineNumber);
        }

        string value = ResolveLineContent(lineNumber, hint);
        if (_lastVisitedLine.LineNumber == lineNumber)
        {
            _lastVisitedLine.Value = value;
        }

        return value;
    }

    public string GetLineRawContent(int lineNumber, int endOffset = 0)
    {
        return GetLineRawContentInternal(lineNumber, endOffset, null);
    }

    private string GetLineRawContentInternal(int lineNumber, int endOffset, NodeHit? hint)
    {
        if (lineNumber < 1 || lineNumber > GetLineCount())
        {
            return string.Empty;
        }

        NodeHit effectiveHint;
        if (hint.HasValue && hint.Value.NodeStartLineNumber > 0)
        {
            effectiveHint = hint.Value;
        }
        else if (_searchCache.TryGetByLine(lineNumber, out PieceTreeNode? cachedNode, out int nodeStartOffset, out int nodeStartLine))
        {
            effectiveHint = new NodeHit(cachedNode, 0, nodeStartOffset, nodeStartLine);
        }
        else
        {
            effectiveHint = NodeAt2(lineNumber, 1);
        }

        if (effectiveHint.Node is null || ReferenceEquals(effectiveHint.Node, _sentinel))
        {
            return string.Empty;
        }

        int relativeLineNumber = ComputeRelativeLineNumber(effectiveHint, lineNumber);
        return GetContentFromNode(effectiveHint.Node, relativeLineNumber, endOffset);
    }

    private string ResolveLineContent(int lineNumber, NodeHit? hint)
    {
        int lastLineNumber = TotalLineFeeds + 1;
        if (lineNumber == lastLineNumber)
        {
            return GetLineRawContentInternal(lineNumber, 0, hint);
        }

        if (_eolNormalized)
        {
            return GetLineRawContentInternal(lineNumber, _eol.Length, hint);
        }

        string rawContent = GetLineRawContentInternal(lineNumber, 0, hint);
        return TrimTrailingLineFeed(rawContent);
    }

    private string GetContentFromNode(PieceTreeNode x, int relativeLineNumber, int endOffset)
    {
#if DEBUG
        PieceTreeDebug.Log($"DEBUG GetContentFromNode: NodeBufIdx={x.Piece.BufferIndex}, NodeStart={x.Piece.Start}, NodeEnd={x.Piece.End}, NodeLen={x.Piece.Length}, NodeLF={x.Piece.LineFeedCount}, relativeLineNumber={relativeLineNumber}, endOffset={endOffset}");
        string lineStartsArr = string.Join(",", _buffers[x.Piece.BufferIndex].LineStarts);
        PieceTreeDebug.Log($"DEBUG GetContentFromNode: buffer[{x.Piece.BufferIndex}].LineStarts=[{lineStartsArr}], bufferLen={_buffers[x.Piece.BufferIndex].Length}");
#endif
        int prevAccumulatedValue = GetAccumulatedValue(x, relativeLineNumber - 2);
        int accumulatedValue = GetAccumulatedValue(x, relativeLineNumber - 1);
        string buffer = _buffers[x.Piece.BufferIndex].Buffer;
        int startOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.Start);

        string ret;
        if (relativeLineNumber - 1 < x.Piece.LineFeedCount)
        {
            int sliceStart = startOffset + prevAccumulatedValue;
            int pieceEndOffset = OffsetInBuffer(x.Piece.BufferIndex, x.Piece.End);
            int sliceEnd = FindLineBreakBoundary(buffer, sliceStart, pieceEndOffset);
            int length = Math.Max(0, sliceEnd - sliceStart - endOffset);
            ret = length == 0 ? string.Empty : buffer.Substring(sliceStart, length);
        }
        else
        {
            ret = buffer.Substring(startOffset + prevAccumulatedValue, x.Piece.Length - prevAccumulatedValue);

            PieceTreeNode next = x.Next();
            while (!ReferenceEquals(next, _sentinel))
            {
                string buf = _buffers[next.Piece.BufferIndex].Buffer;
                if (next.Piece.LineFeedCount > 0)
                {
                    int acc = GetAccumulatedValue(next, 0);
                    int st = OffsetInBuffer(next.Piece.BufferIndex, next.Piece.Start);
                    if (acc - endOffset < 0)
                    {
                        PieceTreeDebug.Log($"DEBUG GetContentFromNode cross-node negative len: nextBuf={next.Piece.BufferIndex}, start={st}, acc={acc}, endOffset={endOffset}, pieceStart={next.Piece.Start}, pieceEnd={next.Piece.End}, pieceLen={next.Piece.Length}, pieceLF={next.Piece.LineFeedCount}, bufLen={buf.Length}");
                    }
                    ret += buf.Substring(st, acc - endOffset);
                    break; // Found end of line
                }
                else
                {
                    int st = OffsetInBuffer(next.Piece.BufferIndex, next.Piece.Start);
                    ret += buf.Substring(st, next.Piece.Length);
                }
                next = next.Next();
            }
        }

        // Backward traversal if we started at the beginning of the node
        if (relativeLineNumber == 1)
        {
            PieceTreeNode p = x.Prev();
            while (!ReferenceEquals(p, _sentinel))
            {
                string pBuf = _buffers[p.Piece.BufferIndex].Buffer;
                int pStart = OffsetInBuffer(p.Piece.BufferIndex, p.Piece.Start);

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
        int index = startOffset;
        int limit = Math.Min(sliceEndExclusive, buffer.Length);
        while (index < limit)
        {
            char ch = buffer[index];
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

        char lastChar = text[^1];
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
