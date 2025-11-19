using System;
using System.Collections.Generic;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer;

public sealed class TextModelDecorationsChangedEventArgs : EventArgs
{
    public TextModelDecorationsChangedEventArgs(IReadOnlyList<DecorationChange> changes, int modelVersionId)
    {
        Changes = changes;
        ModelVersionId = modelVersionId;
    }

    public IReadOnlyList<DecorationChange> Changes { get; }
    public int ModelVersionId { get; }
}
