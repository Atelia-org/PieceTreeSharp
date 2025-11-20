using System;
using System.Collections.Generic;
using System.Text;
using PieceTree.TextBuffer;

namespace PieceTree.TextBuffer.Core;

internal sealed class PieceTreeTextBufferFactory
{
    private const int PreviewLengthLimit = 1000;

    private readonly IReadOnlyList<ChunkBuffer> _chunks;
    private readonly string _bom;
    private readonly int _cr;
    private readonly int _lf;
    private readonly int _crlf;
    private readonly bool _containsRtl;
    private readonly bool _containsUnusualLineTerminators;
    private readonly bool _isBasicAscii;
    private readonly PieceTreeBuilderOptions _options;

    public PieceTreeTextBufferFactory(
        IReadOnlyList<ChunkBuffer> chunks,
        string bom,
        int cr,
        int lf,
        int crlf,
        bool containsRtl,
        bool containsUnusualLineTerminators,
        bool isBasicAscii,
        PieceTreeBuilderOptions options)
    {
        _chunks = chunks ?? throw new ArgumentNullException(nameof(chunks));
        _bom = bom ?? string.Empty;
        _cr = cr;
        _lf = lf;
        _crlf = crlf;
        _containsRtl = containsRtl;
        _containsUnusualLineTerminators = containsUnusualLineTerminators;
        _isBasicAscii = isBasicAscii;
        _options = options ?? PieceTreeBuilderOptions.Default;
    }

    public PieceTreeBuildResult Create(DefaultEndOfLine defaultEol)
    {
        var eol = DetermineEol(defaultEol);
        var (buffers, normalized) = MaterializeBuffers(eol);

        var model = new PieceTreeModel(buffers, normalized, eol);
        for (int i = 1; i < buffers.Count; i++)
        {
            var chunk = buffers[i];
            if (chunk.Length == 0)
            {
                continue;
            }

            var piece = new PieceSegment(
                i,
                BufferCursor.Zero,
                chunk.CreateEndCursor(),
                chunk.LineFeedCount,
                chunk.Length);
            model.InsertPieceAtEnd(piece);
        }

        return new PieceTreeBuildResult(
            model,
            buffers,
            _bom,
            _containsRtl,
            _containsUnusualLineTerminators,
            !_isBasicAscii,
            _options);
    }

    public string GetFirstLineText(int lengthLimit)
    {
        if (lengthLimit <= 0 || _chunks.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Math.Min(lengthLimit, PreviewLengthLimit));
        var remaining = lengthLimit;
        foreach (var chunk in _chunks)
        {
            if (chunk.Buffer.Length == 0)
            {
                continue;
            }

            var take = Math.Min(remaining, chunk.Buffer.Length);
            builder.Append(chunk.Buffer.AsSpan(0, take));
            if (builder.Length >= lengthLimit)
            {
                break;
            }

            remaining = lengthLimit - builder.Length;
            if (remaining <= 0)
            {
                break;
            }
        }

        var candidate = builder.Length > lengthLimit
            ? builder.ToString(0, lengthLimit)
            : builder.ToString();

        var newlineIndex = IndexOfLineBreak(candidate);
        if (newlineIndex >= 0)
        {
            return candidate.Substring(0, Math.Min(newlineIndex, lengthLimit));
        }

        return candidate;
    }

    public string GetLastLineText(int lengthLimit)
    {
        if (lengthLimit <= 0 || _chunks.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Math.Min(lengthLimit, PreviewLengthLimit));
        var remaining = lengthLimit;
        for (int i = _chunks.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var chunk = _chunks[i];
            if (chunk.Buffer.Length == 0)
            {
                continue;
            }

            var take = Math.Min(remaining, chunk.Buffer.Length);
            var sliceStart = chunk.Buffer.Length - take;
            builder.Insert(0, chunk.Buffer.AsSpan(sliceStart, take));
            remaining = lengthLimit - builder.Length;
        }

        var candidate = builder.Length > lengthLimit
            ? builder.ToString(builder.Length - lengthLimit, lengthLimit)
            : builder.ToString();

        return ExtractLastLine(candidate);
    }

    private (List<ChunkBuffer> Buffers, bool Normalized) MaterializeBuffers(string eol)
    {
        List<ChunkBuffer> workingChunks;
        var normalized = false;

        if (_options.NormalizeEol && RequiresNormalization(eol))
        {
            workingChunks = ChunkUtilities.NormalizeChunks(GetChunkStrings(), eol);
            normalized = true;
        }
        else
        {
            workingChunks = new List<ChunkBuffer>(_chunks.Count);
            foreach (var chunk in _chunks)
            {
                workingChunks.Add(chunk);
            }
        }

        var buffers = new List<ChunkBuffer>(workingChunks.Count + 1)
        {
            ChunkBuffer.Empty
        };
        buffers.AddRange(workingChunks);
        return (buffers, normalized);
    }

    private IEnumerable<string> GetChunkStrings()
    {
        foreach (var chunk in _chunks)
        {
            yield return chunk.Buffer;
        }
    }

    private string DetermineEol(DefaultEndOfLine defaultEol)
    {
        var totalEols = _cr + _lf + _crlf;
        var totalCr = _cr + _crlf;
        if (totalEols == 0)
        {
            return defaultEol == DefaultEndOfLine.CRLF ? "\r\n" : "\n";
        }

        if (totalCr > totalEols / 2)
        {
            return "\r\n";
        }

        return "\n";
    }

    private bool RequiresNormalization(string eol)
    {
        if (eol == "\r\n")
        {
            return _cr > 0 || _lf > 0;
        }

        return _cr > 0 || _crlf > 0;
    }

    private static int IndexOfLineBreak(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '\r' || ch == '\n')
            {
                return i;
            }
        }

        return -1;
    }

    private static string ExtractLastLine(string text)
    {
        for (int i = text.Length - 1; i >= 0; i--)
        {
            var ch = text[i];
            if (ch == '\n')
            {
                var start = (i - 1 >= 0 && text[i - 1] == '\r') ? i - 1 : i;
                return text.Substring(start + 1);
            }

            if (ch == '\r')
            {
                return text.Substring(i + 1);
            }
        }

        return text;
    }
}
