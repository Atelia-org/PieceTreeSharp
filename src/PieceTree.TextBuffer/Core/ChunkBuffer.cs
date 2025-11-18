using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Core;

/// <summary>
/// Represents an immutable text chunk originating from either the original file or the change buffer.
/// Responsible for computing line starts so <see cref="BufferCursor"/> math matches the TS PieceTree.
/// </summary>
internal sealed class ChunkBuffer
{
    private readonly string _buffer;
    private readonly int[] _lineStarts;

    private ChunkBuffer(string buffer, int[] lineStarts)
    {
        _buffer = buffer;
        _lineStarts = lineStarts;
    }

    public static ChunkBuffer Empty { get; } = new ChunkBuffer(string.Empty, new[] { 0 });

    public string Buffer => _buffer;

    public int Length => _buffer.Length;

    public int LineFeedCount => Math.Max(0, _lineStarts.Length - 1);

    public IReadOnlyList<int> LineStarts => _lineStarts;

    public static ChunkBuffer FromText(string? text)
    {
        text ??= string.Empty;
        var lineStarts = ComputeLineStarts(text);
        return new ChunkBuffer(text, lineStarts);
    }

    internal BufferCursor CreateEndCursor()
    {
        var lastLine = _lineStarts.Length - 1;
        var column = _buffer.Length - _lineStarts[lastLine];
        return new BufferCursor(lastLine, column);
    }

    internal int GetOffset(BufferCursor cursor)
    {
        if ((uint)cursor.Line >= (uint)_lineStarts.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(cursor), "Cursor line outside chunk.");
        }

        var lineStart = _lineStarts[cursor.Line];
        var nextLineStart = cursor.Line + 1 < _lineStarts.Length ? _lineStarts[cursor.Line + 1] : _buffer.Length;
        var absolute = lineStart + cursor.Column;
        if (cursor.Column < 0 || absolute > nextLineStart)
        {
            throw new ArgumentOutOfRangeException(nameof(cursor), "Cursor column outside chunk.");
        }

        return absolute;
    }

    internal string Slice(BufferCursor start, BufferCursor end)
    {
        var startOffset = GetOffset(start);
        var endOffset = GetOffset(end);
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

    private static int[] ComputeLineStarts(string text)
    {
        var starts = new List<int> { 0 };
        var index = 0;
        while (index < text.Length)
        {
            var ch = text[index];
            if (ch == '\r')
            {
                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    starts.Add(index + 2);
                    index += 2;
                    continue;
                }

                starts.Add(index + 1);
            }
            else if (ch == '\n')
            {
                starts.Add(index + 1);
            }

            index++;
        }

        return starts.ToArray();
    }
}
