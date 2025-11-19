namespace PieceTree.TextBuffer.Decorations;

public enum DecorationDeltaKind
{
    Added,
    Removed,
    Updated,
}

public readonly struct DecorationChange
{
    public DecorationChange(string id, int ownerId, TextRange range, DecorationDeltaKind kind)
    {
        Id = id;
        OwnerId = ownerId;
        Range = range;
        Kind = kind;
    }

    public DecorationChange(ModelDecoration decoration, DecorationDeltaKind kind)
        : this(decoration.Id, decoration.OwnerId, decoration.Range, kind)
    {
    }

    public string Id { get; }
    public int OwnerId { get; }
    public TextRange Range { get; }
    public DecorationDeltaKind Kind { get; }
}
