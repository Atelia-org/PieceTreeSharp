// Original C# implementation
// Purpose: Debug utilities for PieceTree model inspection in tests
// - Provides model dump functionality for test diagnostics
// Created: 2025-11-22

using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests.Helpers;

internal static class PieceTreeModelTestHelpers
{
    public static void DebugDumpModel(PieceTreeModel model)
    {
        if (model == null)
        {
            return;
        }

        Console.WriteLine("--- PieceTreeModel Dump ---");
        for (int i = 0; i < model.Buffers.Count; i++)
        {
            ChunkBuffer b = model.Buffers[i];
            string starts = string.Join(",", b.LineStarts);
            string content = b.Buffer.Replace("\n", "\\n").Replace("\r", "\\r");
            Console.WriteLine($"Buffer {i}: len={b.Length}, starts=[{starts}], content='{content}'");
        }

        foreach (PieceSegment piece in model.EnumeratePiecesInOrder())
        {
            ChunkBuffer chunk = model.Buffers[piece.BufferIndex];
            BufferCursor start = piece.Start;
            BufferCursor end = piece.End;
            string text = chunk.Slice(start, end).Replace("\n", "\\n").Replace("\r", "\\r");
            Console.WriteLine($"Piece BufIdx={piece.BufferIndex}; Start={start.Line}/{start.Column}; End={end.Line}/{end.Column}; Len={piece.Length}; LFcnt={piece.LineFeedCount}; Text='{text}'");
        }
        Console.WriteLine("--- End Dump ---");
    }
}
