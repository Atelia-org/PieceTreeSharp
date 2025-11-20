using System;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Cursor
{
    public sealed record class CursorState
    {
        public int OwnerId { get; init; }
        public Selection Selection { get; init; }
        public int StickyColumn { get; init; }
        public string[] DecorationIds { get; init; } = Array.Empty<string>();

        public CursorState(int ownerId, Selection selection, int stickyColumn = -1, string[]? decorationIds = null)
        {
            OwnerId = ownerId;
            Selection = selection;
            StickyColumn = stickyColumn;
            DecorationIds = decorationIds ?? Array.Empty<string>();
        }
    }
}
