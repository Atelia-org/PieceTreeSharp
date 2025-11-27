// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts
// - Interface: ITextSnapshot, snapshot implementation
// - Lines: 50-150
// Ported: 2025-11-19

namespace PieceTree.TextBuffer.Core;

internal sealed class PieceTreeSnapshot : ITextSnapshot
{
    private readonly IReadOnlyList<PieceSegment> _pieces;
    private readonly IReadOnlyList<ChunkBuffer> _buffers;
    private readonly string _bom;
    private int _index;

    public PieceTreeSnapshot(PieceTreeModel model, string bom)
    {
        _pieces = new List<PieceSegment>(model.EnumeratePiecesInOrder());
        _buffers = new List<ChunkBuffer>(model.Buffers);
        _bom = bom ?? string.Empty;
        _index = 0;
    }

    public string? Read()
    {
        if (_pieces.Count == 0)
        {
            if (_index == 0)
            {
                _index++;
                return _bom;
            }

            return null;
        }

        if (_index >= _pieces.Count)
        {
            return null;
        }

        bool isFirstPiece = _index == 0;
        PieceSegment piece = _pieces[_index++];
        ChunkBuffer buffer = _buffers[piece.BufferIndex];
        string slice = buffer.Slice(piece.Start, piece.End);
        return isFirstPiece ? _bom + slice : slice;
    }
}
