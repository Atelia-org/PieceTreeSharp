// Source: ts/src/vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts
// - Class: LineStarts (Lines: 27-31)
// - Functions: createLineStarts, createLineStartsFast (Lines: 61-98)
// Ported: 2025-11-19

namespace PieceTree.TextBuffer.Core;

/// <summary>
/// Immutable line-start metadata used by <see cref="ChunkBuffer"/> to mimic TS createLineStarts fast/slow helpers.
/// </summary>
internal readonly struct LineStartTable
{
    private static readonly int[] DefaultStarts = [0];
    private readonly int[]? _lineStarts;

    public LineStartTable(int[] lineStarts, int carriageReturnCount, int lineFeedCount, int carriageReturnLineFeedCount, bool isBasicAscii)
    {
        ArgumentNullException.ThrowIfNull(lineStarts);
        if (lineStarts.Length == 0 || lineStarts[0] != 0)
        {
            throw new ArgumentException("Line starts must begin with 0.", nameof(lineStarts));
        }

        _lineStarts = lineStarts;
        CarriageReturnCount = carriageReturnCount;
        LineFeedCount = lineFeedCount;
        CarriageReturnLineFeedCount = carriageReturnLineFeedCount;
        IsBasicAscii = isBasicAscii;
    }

    public static LineStartTable Empty { get; } = new LineStartTable(DefaultStarts, 0, 0, 0, true);

    public bool IsEmpty => StartsArray.Length == 1;

    public int EntryCount => StartsArray.Length;

    public int LineBreakCount => EntryCount - 1;

    public int CarriageReturnCount { get; }

    public int LineFeedCount { get; }

    public int CarriageReturnLineFeedCount { get; }

    public bool IsBasicAscii { get; }

    public IReadOnlyList<int> LineStarts => StartsArray;

    internal int this[int index] => StartsArray[index];

    internal ReadOnlySpan<int> AsSpan() => StartsArray;

    internal int[] RawArray => StartsArray;

    private int[] StartsArray => _lineStarts ?? DefaultStarts;
}

/// <summary>
/// Single-pass builder that records CR/LF/CRLF counts and ASCII hints like TS createLineStarts helpers do.
/// Optional parameters exist so tests can force the slower validation-heavy path when needed.
/// </summary>
internal static class LineStartBuilder
{
    public static LineStartTable Build(string? text, bool forceSlowPath = false, bool trackAscii = true)
    {
        text ??= string.Empty;
        if (text.Length == 0)
        {
            return LineStartTable.Empty;
        }

        return forceSlowPath
            ? BuildWithFallback(text, trackAscii)
            : BuildWithSpan(text, trackAscii);
    }

    private static LineStartTable BuildWithSpan(string text, bool trackAscii)
    {
        return BuildCore(text.AsSpan(), trackAscii);
    }

    private static LineStartTable BuildWithFallback(string text, bool trackAscii)
    {
        // Slow path mirrors TS createLineStarts by forcing a temporary char array allocation.
        char[] buffer = text.ToCharArray();
        return BuildCore(buffer.AsSpan(), trackAscii);
    }

    private static LineStartTable BuildCore(ReadOnlySpan<char> text, bool trackAscii)
    {
        List<int> starts = new(Math.Max(4, text.Length / 64)) { 0 };
        int cr = 0;
        int lf = 0;
        int crlf = 0;
        bool isBasicAscii = true;

        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            char? peekNext = index + 1 < text.Length ? text[index + 1] : null;
            ProcessChar(current, peekNext, ref index, starts, ref cr, ref lf, ref crlf, trackAscii, ref isBasicAscii);
            index++;
        }

        bool basicAscii = trackAscii ? isBasicAscii : true;
        return new LineStartTable(starts.ToArray(), cr, lf, crlf, basicAscii);
    }

    private static void ProcessChar(
        char current,
        char? peekNext,
        ref int index,
        List<int> starts,
        ref int carriageReturnCount,
        ref int lineFeedCount,
        ref int carriageReturnLineFeedCount,
        bool trackAscii,
        ref bool isBasicAscii)
    {
        if (current == '\r')
        {
            if (peekNext == '\n')
            {
                carriageReturnLineFeedCount++;
                starts.Add(index + 2);
                index++;
            }
            else
            {
                carriageReturnCount++;
                starts.Add(index + 1);
            }

            return;
        }

        if (current == '\n')
        {
            lineFeedCount++;
            starts.Add(index + 1);
            return;
        }

        if (!trackAscii || !isBasicAscii)
        {
            return;
        }

        if (current != '\t' && (current < 32 || current > 126))
        {
            isBasicAscii = false;
        }
    }
}
