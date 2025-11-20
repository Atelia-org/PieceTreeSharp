namespace PieceTree.TextBuffer.Decorations;

public enum DecorationDeltaKind
{
    Added,
    Removed,
    Updated,
}

public readonly struct DecorationChange
{
    public DecorationChange(string id, int ownerId, TextRange range, DecorationDeltaKind kind, ModelDecorationOptions options, TextRange? oldRange = null)
    {
        Id = id;
        OwnerId = ownerId;
        Range = range;
        Kind = kind;
        Options = options;
        OldRange = oldRange;
    }

    public DecorationChange(ModelDecoration decoration, DecorationDeltaKind kind, TextRange? oldRange = null)
        : this(decoration.Id, decoration.OwnerId, decoration.Range, kind, decoration.Options, oldRange)
    {
    }

    public string Id { get; }
    public int OwnerId { get; }
    public TextRange Range { get; }
    public DecorationDeltaKind Kind { get; }
    public ModelDecorationOptions Options { get; }
    public TextRange? OldRange { get; }
}
