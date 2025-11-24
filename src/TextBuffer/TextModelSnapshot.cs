// Source: ts/src/vs/editor/common/model/textModel.ts
// - Class: TextModelSnapshot (Lines: 72-115)
// Ported: 2025-11-25

using System;
using System.Collections.Generic;
using System.Text;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer;

internal sealed class TextModelSnapshot : ITextSnapshot
{
    private const int ChunkSizeThreshold = 64 * 1024;
    private readonly ITextSnapshot _source;
    private bool _endOfStream;

    public TextModelSnapshot(ITextSnapshot source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public string? Read()
    {
        if (_endOfStream)
        {
            return null;
        }

        var chunks = new List<string>();
        var aggregateLength = 0;

        while (true)
        {
            var chunk = _source.Read();
            if (chunk == null)
            {
                _endOfStream = true;
                return aggregateLength == 0 ? null : ConcatChunks(chunks, aggregateLength);
            }

            if (chunk.Length == 0)
            {
                continue;
            }

            chunks.Add(chunk);
            aggregateLength += chunk.Length;

            if (aggregateLength >= ChunkSizeThreshold)
            {
                return ConcatChunks(chunks, aggregateLength);
            }
        }
    }

    private static string ConcatChunks(List<string> chunks, int totalLength)
    {
        if (chunks.Count == 0)
        {
            return string.Empty;
        }

        if (chunks.Count == 1)
        {
            return chunks[0];
        }

        var builder = new StringBuilder(totalLength);
        foreach (var chunk in chunks)
        {
            builder.Append(chunk);
        }

        return builder.ToString();
    }
}
