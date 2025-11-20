using System;

namespace PieceTree.TextBuffer.Decorations;

public readonly struct ModelDeltaDecoration
{
    public ModelDeltaDecoration(TextRange range, ModelDecorationOptions? options = null)
    {
        Range = range;
        Options = (options ?? ModelDecorationOptions.Default).Normalize();
    }

    public TextRange Range { get; }
    public ModelDecorationOptions Options { get; }
}
