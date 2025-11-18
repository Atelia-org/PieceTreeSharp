using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Core;

internal static class PieceTreeBuilder
{
    public static PieceTreeBuildResult BuildFromChunks(IEnumerable<string> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        var model = new PieceTreeModel();
        var buffers = new List<ChunkBuffer> { ChunkBuffer.Empty };
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
