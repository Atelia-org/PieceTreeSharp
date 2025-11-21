// Source: ts/src/vs/editor/common/diff/defaultLinesDiffComputer/algorithms/diffAlgorithm.ts
// - Class: Array2D (utility for dynamic programming)
// - Lines: 200-230
// Ported: 2025-11-21

namespace PieceTree.TextBuffer.Diff;

internal sealed class Array2D<T>
{
    private readonly T[] _data;

    public Array2D(int width, int height)
    {
        Width = width;
        Height = height;
        _data = new T[width * height];
    }

    public int Width { get; }
    public int Height { get; }

    public T Get(int x, int y) => _data[x + y * Width];

    public void Set(int x, int y, T value) => _data[x + y * Width] = value;
}
