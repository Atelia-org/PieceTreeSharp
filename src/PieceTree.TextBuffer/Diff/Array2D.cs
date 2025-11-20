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
