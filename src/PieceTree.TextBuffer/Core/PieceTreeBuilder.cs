using System;
using System.Collections.Generic;
using System.Text;

namespace PieceTree.TextBuffer.Core;

internal class PieceTreeBuilder
{
    private readonly List<string> _chunks = new();
    private string _BOM = "";
    private bool _hasPreviousChar;
    private char _previousChar;
    private int _cr;
    private int _lf;
    private int _crlf;

    public void AcceptChunk(string chunk)
    {
        if (chunk.Length == 0)
        {
            return;
        }

        if (_chunks.Count == 0)
        {
            if (chunk.Length > 0 && chunk[0] == '\uFEFF')
            {
                _BOM = "\uFEFF";
                chunk = chunk.Substring(1);
            }
        }

        if (chunk.Length == 0)
        {
            return;
        }

        _chunks.Add(chunk);

        for (int i = 0; i < chunk.Length; i++)
        {
            char ch = chunk[i];
            if (ch == '\r')
            {
                if (i + 1 < chunk.Length)
                {
                    if (chunk[i + 1] == '\n')
                    {
                        _crlf++;
                        i++;
                    }
                    else
                    {
                        _cr++;
                    }
                }
                else
                {
                    _cr++;
                }
            }
            else if (ch == '\n')
            {
                _lf++;
            }
        }

        if (_hasPreviousChar && _previousChar == '\r' && chunk[0] == '\n')
        {
            _cr--;
            _crlf++;
        }

        _hasPreviousChar = true;
        _previousChar = chunk[chunk.Length - 1];
    }

    public PieceTreeBuildResult Finish(bool normalizeEOL)
    {
        var eol = "\n";
        if (_crlf > _lf && _crlf > _cr)
        {
            eol = "\r\n";
        }
        else if (_cr > _lf && _cr > _crlf)
        {
            eol = "\r";
        }

        var finalChunks = _chunks;
        bool eolNormalized = false;

        if (normalizeEOL && 
            ((eol == "\n" && (_cr > 0 || _crlf > 0)) ||
             (eol == "\r\n" && (_cr > 0 || _lf > 0)) ||
             (eol == "\r" && (_lf > 0 || _crlf > 0))))
        {
            eolNormalized = true;
            finalChunks = NormalizeChunks(_chunks, eol);
        }

        return BuildFromChunks(finalChunks, _BOM, eolNormalized, eol);
    }

    private static List<string> NormalizeChunks(List<string> chunks, string eol)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool skipNextLF = false;

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            if (chunk.Length == 0) continue;

            int start = 0;
            if (skipNextLF)
            {
                if (chunk[0] == '\n')
                {
                    start = 1;
                }
                skipNextLF = false;
            }

            for (int j = start; j < chunk.Length; j++)
            {
                char ch = chunk[j];
                if (ch == '\r')
                {
                    sb.Append(eol);
                    if (j + 1 < chunk.Length)
                    {
                        if (chunk[j + 1] == '\n')
                        {
                            j++;
                        }
                    }
                    else
                    {
                        // End of chunk, check next chunk
                        if (i + 1 < chunks.Count && chunks[i + 1].Length > 0 && chunks[i + 1][0] == '\n')
                        {
                            skipNextLF = true;
                        }
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

            if (sb.Length > 65536)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            result.Add(sb.ToString());
        }

        return result;
    }

    public static PieceTreeBuildResult BuildFromChunks(IEnumerable<string> chunks)
    {
        var builder = new PieceTreeBuilder();
        foreach (var chunk in chunks)
        {
            builder.AcceptChunk(chunk);
        }
        return builder.Finish(false);
    }

    private static PieceTreeBuildResult BuildFromChunks(IEnumerable<string> chunks, string bom, bool eolNormalized, string eol)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        var buffers = new List<ChunkBuffer> { ChunkBuffer.Empty };
        var model = new PieceTreeModel(buffers, eolNormalized, eol);
        
        foreach (var chunk in chunks)
        {
            var chunkBuffer = ChunkBuffer.FromText(chunk);
            buffers.Add(chunkBuffer);
            if (chunkBuffer.Length == 0)
            {
                continue;
            }

            var bufferIndex = buffers.Count - 1;
            var piece = new PieceSegment(
                bufferIndex,
                BufferCursor.Zero,
                chunkBuffer.CreateEndCursor(),
                chunkBuffer.LineFeedCount,
                chunkBuffer.Length
            );

            model.InsertPieceAtEnd(piece);
        }

        return new PieceTreeBuildResult(model, buffers);
    }
}

internal sealed record PieceTreeBuildResult(PieceTreeModel Model, List<ChunkBuffer> Buffers);
