using System;

namespace PieceTree.TextBuffer.Decorations;

public readonly struct ModelDeltaDecoration
{
    public ModelDeltaDecoration(TextRange range, ModelDecorationOptions? options = null)
    {
        Range = range;
        Options = options ?? ModelDecorationOptions.Default;
    }

    public TextRange Range { get; }
    public ModelDecorationOptions Options { get; }
}
