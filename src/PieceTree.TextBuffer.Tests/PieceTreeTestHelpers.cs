// Source: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// - Helper: Text reconstruction from PieceTree model
// Ported: 2025-11-22

using System.Text;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests;

internal static class PieceTreeTestHelpers
{
    public static string ReconstructText(PieceTreeBuildResult result)
    {
        var builder = new StringBuilder();
        foreach (var piece in result.Model.EnumeratePiecesInOrder())
        {
            var buffer = result.Buffers[piece.BufferIndex];
            builder.Append(buffer.Slice(piece.Start, piece.End));
        }

        return builder.ToString();
    }
}
