// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBufferBuilder.ts
// - Class: PieceTreeTextBufferBuilder (Lines: 67-188)
// Ported: 2025-11-19

namespace PieceTree.TextBuffer.Core;

internal sealed class PieceTreeBuilder
{
    private readonly List<ChunkBuffer> _chunks = [];
    private string _bom = string.Empty;
    private bool _hasPreviousChar;
    private char _previousChar;
    private int _cr;
    private int _lf;
    private int _crlf;
    private bool _containsRtl;
    private bool _containsUnusualLineTerminators;
    private bool _isBasicAscii = true;

    public void AcceptChunk(string? chunk)
    {
        if (string.IsNullOrEmpty(chunk))
        {
            return;
        }

        if (_chunks.Count == 0 && chunk.Length > 0 && chunk[0] == '\uFEFF')
        {
            _bom = "\uFEFF";
            chunk = chunk.Substring(1);
            if (chunk.Length == 0)
            {
                return;
            }
        }

        char lastChar = chunk[^1];
        if (lastChar == '\r' || char.IsHighSurrogate(lastChar))
        {
            AcceptChunkInternal(chunk.Substring(0, chunk.Length - 1));
            _hasPreviousChar = true;
            _previousChar = lastChar;
        }
        else
        {
            AcceptChunkInternal(chunk);
            _hasPreviousChar = false;
            _previousChar = lastChar;
        }
    }

    public PieceTreeTextBufferFactory Finish(PieceTreeBuilderOptions? options = null)
    {
        FinalizeChunks();
        PieceTreeBuilderOptions resolved = options ?? PieceTreeBuilderOptions.Default;
        return new PieceTreeTextBufferFactory(
            new List<ChunkBuffer>(_chunks),
            _bom,
            _cr,
            _lf,
            _crlf,
            _containsRtl,
            _containsUnusualLineTerminators,
            _isBasicAscii,
            resolved);
    }

    public static PieceTreeBuildResult BuildFromChunks(
        IEnumerable<string> chunks,
        bool normalizeEol = false,
        DefaultEndOfLine defaultEol = DefaultEndOfLine.LF)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        PieceTreeBuilder builder = new();
        foreach (string chunk in chunks)
        {
            builder.AcceptChunk(chunk);
        }

        PieceTreeBuilderOptions options = PieceTreeBuilderOptions.Default with
        {
            NormalizeEol = normalizeEol,
            DefaultEndOfLine = defaultEol
        };

        return builder.Finish(options).Create(defaultEol);
    }

    private void AcceptChunkInternal(string chunk)
    {
        if (_hasPreviousChar)
        {
            chunk = string.Concat(_previousChar, chunk);
            _hasPreviousChar = false;
        }

        if (string.IsNullOrEmpty(chunk))
        {
            return;
        }

        AddChunk(chunk);
    }

    private void AddChunk(string chunk)
    {
        foreach (string segment in ChunkUtilities.SplitText(chunk))
        {
            if (segment.Length == 0)
            {
                continue;
            }

            LineStartTable lineStarts = LineStartBuilder.Build(segment);
            _cr += lineStarts.CarriageReturnCount;
            _lf += lineStarts.LineFeedCount;
            _crlf += lineStarts.CarriageReturnLineFeedCount;

            bool isChunkBasicAscii = lineStarts.IsBasicAscii;
            if (_isBasicAscii && !isChunkBasicAscii)
            {
                _isBasicAscii = false;
            }

            if (!isChunkBasicAscii)
            {
                if (!_containsRtl && TextMetadataScanner.ContainsRightToLeftCharacters(segment))
                {
                    _containsRtl = true;
                }

                if (!_containsUnusualLineTerminators && TextMetadataScanner.ContainsUnusualLineTerminators(segment))
                {
                    _containsUnusualLineTerminators = true;
                }
            }

            ChunkBuffer buffer = ChunkBuffer.FromPrecomputed(segment, lineStarts);
            _chunks.Add(buffer);
        }
    }

    private void FinalizeChunks()
    {
        if (_hasPreviousChar)
        {
            _hasPreviousChar = false;
            if (_chunks.Count == 0)
            {
                AddChunk(_previousChar.ToString());
            }
            else
            {
                int lastIndex = _chunks.Count - 1;
                ChunkBuffer lastChunk = _chunks[lastIndex];
                string merged = string.Concat(lastChunk.Buffer, _previousChar);
                LineStartTable lineStarts = LineStartBuilder.Build(merged);

                _cr += lineStarts.CarriageReturnCount - lastChunk.CarriageReturnCount;
                _lf += lineStarts.LineFeedCount - lastChunk.LineFeedOnlyCount;
                _crlf += lineStarts.CarriageReturnLineFeedCount - lastChunk.CarriageReturnLineFeedCount;

                bool isChunkBasicAscii = lineStarts.IsBasicAscii;
                if (_isBasicAscii && !isChunkBasicAscii)
                {
                    _isBasicAscii = false;
                }

                if (!isChunkBasicAscii)
                {
                    if (!_containsRtl && TextMetadataScanner.ContainsRightToLeftCharacters(merged))
                    {
                        _containsRtl = true;
                    }

                    if (!_containsUnusualLineTerminators && TextMetadataScanner.ContainsUnusualLineTerminators(merged))
                    {
                        _containsUnusualLineTerminators = true;
                    }
                }

                _chunks[lastIndex] = ChunkBuffer.FromPrecomputed(merged, lineStarts);
            }
        }

        if (_chunks.Count == 0)
        {
            _chunks.Add(ChunkBuffer.FromText(string.Empty));
        }
    }
}

internal sealed record PieceTreeBuilderOptions
{
    public bool NormalizeEol { get; init; } = true;
    public bool RepairInvalidLines { get; init; } = false;
    public bool TrimAutoWhitespace { get; init; } = false;
    public DefaultEndOfLine DefaultEndOfLine { get; init; } = DefaultEndOfLine.LF;

    public static PieceTreeBuilderOptions Default { get; } = new();
}

internal sealed record PieceTreeBuildResult(
    PieceTreeModel Model,
    List<ChunkBuffer> Buffers,
    string Bom,
    bool MightContainRtl,
    bool MightContainUnusualLineTerminators,
    bool MightContainNonBasicAscii,
    PieceTreeBuilderOptions Options);
