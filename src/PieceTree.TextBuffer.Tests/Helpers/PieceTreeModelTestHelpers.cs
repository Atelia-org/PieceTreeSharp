using System;
using System.Linq;
using System.Text;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests.Helpers;

internal static class PieceTreeModelTestHelpers
{
    public static void DebugDumpModel(PieceTreeModel model)
    {
        if (model == null) return;

        Console.WriteLine("--- PieceTreeModel Dump ---");
        for (int i = 0; i < model.Buffers.Count; i++)
        {
            var b = model.Buffers[i];
            var starts = string.Join(",", b.LineStarts);
            var content = b.Buffer.Replace("\n", "\\n").Replace("\r", "\\r");
            Console.WriteLine($"Buffer {i}: len={b.Length}, starts=[{starts}], content='{content}'");
        }

        foreach (var piece in model.EnumeratePiecesInOrder())
        {
            var chunk = model.Buffers[piece.BufferIndex];
            var start = piece.Start;
            var end = piece.End;
            var text = chunk.Slice(start, end).Replace("\n", "\\n").Replace("\r", "\\r");
            Console.WriteLine($"Piece BufIdx={piece.BufferIndex}; Start={start.Line}/{start.Column}; End={end.Line}/{end.Column}; Len={piece.Length}; LFcnt={piece.LineFeedCount}; Text='{text}'");
        }
        Console.WriteLine("--- End Dump ---");
    }
}
