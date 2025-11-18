using System.Collections.Generic;
using System.Linq;

namespace PieceTree.TextBuffer.Core;

/// <summary>
/// Represents an immutable text chunk originating from either the original file or the change buffer.
/// </summary>
internal sealed class ChunkBuffer
{
    public ChunkBuffer(string buffer, IReadOnlyList<int> lineStarts)
    {
        Buffer = buffer;
        LineStarts = lineStarts.ToArray();
    }

    public string Buffer { get; }

    public IReadOnlyList<int> LineStarts { get; }
}
