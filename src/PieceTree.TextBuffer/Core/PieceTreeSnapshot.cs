// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts
// - Interface: ITextSnapshot, snapshot implementation
// - Lines: 50-150
// Ported: 2025-11-19

using System.Collections.Generic;
using System.Text;

namespace PieceTree.TextBuffer.Core;

internal sealed class PieceTreeSnapshot : ITextSnapshot
{
    private readonly IReadOnlyList<PieceSegment> _pieces;
    private readonly IReadOnlyList<ChunkBuffer> _buffers;
    private readonly string _bom;

    public PieceTreeSnapshot(PieceTreeModel model, string bom)
    {
        _pieces = new List<PieceSegment>(model.EnumeratePiecesInOrder());
        _buffers = new List<ChunkBuffer>(model.Buffers);
        _bom = bom;
    }

    public string? Read()
    {
        if (_pieces.Count == 0)
        {
            return _bom;
        }

        var sb = new StringBuilder(_bom);
        foreach (var piece in _pieces)
        {
            var buffer = _buffers[piece.BufferIndex];
            sb.Append(buffer.Slice(piece.Start, piece.End));
        }
        return sb.ToString();
    }
}
