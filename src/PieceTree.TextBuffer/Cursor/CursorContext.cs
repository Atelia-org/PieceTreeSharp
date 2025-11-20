using System;
using System.Collections.Generic;
using System.Linq;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Cursor
{
    public sealed class CursorContext
    {
        private readonly TextModel _model;
        private readonly CursorCollection _collection;

        public CursorContext(TextModel model, CursorCollection collection)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        /// <summary>
        /// Compute the new set of active positions for every cursor after edits.
        /// For now returns the current active positions; cursor recovery across complex edits may be implemented later.
        /// </summary>
        public IReadOnlyList<TextPosition> ComputeAfterCursorState(IReadOnlyList<TextChange>? inverseChanges)
        {
            return _collection.GetCursorPositions();
        }
    }
}
