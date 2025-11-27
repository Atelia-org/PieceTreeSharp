// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Functions: Text chunking and normalization utilities
// Ported: 2025-11-19

using System;
using System.Collections.Generic;
using System.Text;

namespace PieceTree.TextBuffer.Core;

internal static class ChunkUtilities
{
    public const int DefaultChunkSize = 65535;
    private static readonly int MinChunkLength = DefaultChunkSize - (DefaultChunkSize / 3);
    private static readonly int MaxChunkLength = MinChunkLength * 2;

    public static IEnumerable<string> SplitText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        int offset = 0;
        int length = text.Length;
        while (offset < length)
        {
            int sliceLength = Math.Min(DefaultChunkSize, length - offset);
            int end = offset + sliceLength;

            if (end < length)
            {
                char lastChar = text[end - 1];
                char nextChar = text[end];
                if (lastChar == '\r' && nextChar == '\n')
                {
                    end++;
                }
                else if (char.IsHighSurrogate(lastChar) && char.IsLowSurrogate(nextChar))
                {
                    end++;
                }
            }

            if (end > length)
            {
                end = length;
            }

            int size = end - offset;
            if (size <= 0)
            {
                break;
            }

            yield return text.Substring(offset, size);
            offset = end;
        }
    }

    public static List<ChunkBuffer> NormalizeChunks(IEnumerable<string> source, string eol)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(eol);

        List<ChunkBuffer> result = [];
        StringBuilder builder = new();

        foreach (string segment in source)
        {
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            builder.Append(segment);
            if (builder.Length >= MaxChunkLength)
            {
                Flush(builder, result, eol, force: false);
            }
        }

        Flush(builder, result, eol, force: true);

        if (result.Count == 0)
        {
            result.Add(ChunkBuffer.FromText(string.Empty));
        }

        return result;
    }

    private static void Flush(StringBuilder builder, List<ChunkBuffer> target, string eol, bool force)
    {
        if (builder.Length == 0)
        {
            return;
        }

        if (!force && builder.Length <= MinChunkLength)
        {
            return;
        }

        char lastChar = builder[^1];
        if (!force && (lastChar == '\r' || char.IsHighSurrogate(lastChar)))
        {
            // Keep the trailing char for the next chunk to avoid splitting CRLF or surrogate pairs.
            char carry = builder[^1];
            builder.Length -= 1;
            if (builder.Length == 0)
            {
                builder.Append(carry);
                return;
            }

            EmitChunk(builder, target, eol);
            builder.Append(carry);
            return;
        }

        EmitChunk(builder, target, eol);
    }

    private static void EmitChunk(StringBuilder builder, List<ChunkBuffer> target, string eol)
    {
        if (builder.Length == 0)
        {
            return;
        }

        string normalized = ReplaceLineEndings(builder, eol);
        target.Add(ChunkBuffer.FromText(normalized));
        builder.Clear();
    }

    private static string ReplaceLineEndings(StringBuilder builder, string eol)
    {
        if (builder.Length == 0)
        {
            return string.Empty;
        }

        string text = builder.ToString();
        StringBuilder result = new(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == '\r')
            {
                result.Append(eol);
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
            }
            else if (ch == '\n')
            {
                result.Append(eol);
            }
            else
            {
                result.Append(ch);
            }
        }

        return result.ToString();
    }
}
