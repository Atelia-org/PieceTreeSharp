// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Functions: createLineStarts, createLineStartsFast (Lines: 27-98)
// Ported: 2025-11-19

using System;

namespace PieceTree.TextBuffer.Core;

/// <summary>
/// Represents an immutable text chunk originating from either the original file or the change buffer.
/// Responsible for computing line starts so <see cref="BufferCursor"/> math matches the TS PieceTree.
/// </summary>
internal sealed class ChunkBuffer
{
    private readonly string _buffer;
    private readonly LineStartTable _lineStarts;

    private ChunkBuffer(string buffer, LineStartTable lineStarts)
    {
        _buffer = buffer;
        _lineStarts = lineStarts;
    }

    public static ChunkBuffer Empty { get; } = new ChunkBuffer(string.Empty, LineStartTable.Empty);

    public string Buffer => _buffer;

    public int Length => _buffer.Length;

    public int LineFeedCount => _lineStarts.LineBreakCount;

    public int CarriageReturnCount => _lineStarts.CarriageReturnCount;

    public int LineFeedOnlyCount => _lineStarts.LineFeedCount;

    public int CarriageReturnLineFeedCount => _lineStarts.CarriageReturnLineFeedCount;

    public bool IsBasicAscii => _lineStarts.IsBasicAscii;

    public IReadOnlyList<int> LineStarts => _lineStarts.LineStarts;

    public static ChunkBuffer FromText(string? text, bool forceSlowPath = false, bool trackAscii = true)
    {
        text ??= string.Empty;
        LineStartTable lineStarts = LineStartBuilder.Build(text, forceSlowPath, trackAscii);
        return new ChunkBuffer(text, lineStarts);
    }

    internal static ChunkBuffer FromPrecomputed(string text, LineStartTable lineStarts) => new(text, lineStarts);

    internal BufferCursor CreateEndCursor()
    {
        ReadOnlySpan<int> starts = _lineStarts.AsSpan();
        int lastLine = starts.Length - 1;
        int column = _buffer.Length - starts[lastLine];
        return new BufferCursor(lastLine, column);
    }

    internal int GetOffset(BufferCursor cursor)
    {
        ReadOnlySpan<int> starts = _lineStarts.AsSpan();
        if ((uint)cursor.Line >= (uint)starts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(cursor), "Cursor line outside chunk.");
        }

        int lineStart = starts[cursor.Line];
        int nextLineStart = cursor.Line + 1 < starts.Length ? starts[cursor.Line + 1] : _buffer.Length;
        int absolute = lineStart + cursor.Column;
        if (cursor.Column < 0 || absolute > nextLineStart)
        {
            throw new ArgumentOutOfRangeException(nameof(cursor), "Cursor column outside chunk.");
        }

        return absolute;
    }

    internal string Slice(BufferCursor start, BufferCursor end)
    {
        int startOffset = GetOffset(start);
        int endOffset = GetOffset(end);
        if (endOffset < startOffset)
        {
            throw new ArgumentException("End cursor cannot precede start cursor.", nameof(end));
        }

        if (endOffset == startOffset)
        {
            return string.Empty;
        }

        return _buffer.Substring(startOffset, endOffset - startOffset);
    }

    internal ChunkBuffer Append(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return this;
        }

        LineStartTable appended = LineStartBuilder.Build(text);
        int[] oldStarts = _lineStarts.RawArray;
        int[] appendedStarts = appended.RawArray;
        int offset = _buffer.Length;
        int[] mergedStarts = new int[oldStarts.Length + Math.Max(0, appendedStarts.Length - 1)];
        Array.Copy(oldStarts, mergedStarts, oldStarts.Length);
        for (int i = 1; i < appendedStarts.Length; i++)
        {
            mergedStarts[oldStarts.Length + i - 1] = appendedStarts[i] + offset;
        }

        int crCount = _lineStarts.CarriageReturnCount + appended.CarriageReturnCount;
        int lfCount = _lineStarts.LineFeedCount + appended.LineFeedCount;
        int crlfCount = _lineStarts.CarriageReturnLineFeedCount + appended.CarriageReturnLineFeedCount;

        bool isAscii = _lineStarts.IsBasicAscii && appended.IsBasicAscii;
        LineStartTable mergedTable = new(mergedStarts, crCount, lfCount, crlfCount, isAscii);

        return new ChunkBuffer(string.Concat(_buffer, text), mergedTable);
    }

}
