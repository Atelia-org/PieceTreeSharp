// Source: ts/src/vs/editor/common/cursor/cursorCollection.ts
// - Class: CursorCollection (Lines: 15-250)
// Ported: 2025-11-22

using System;
using System.Collections.Generic;
using System.Linq;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Cursor;

public sealed class CursorCollection : IDisposable
{
    private readonly TextModel _model;
    private readonly List<Cursor> _cursors = [];
    private bool _disposed;

    public CursorCollection(TextModel model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public IReadOnlyList<Cursor> Cursors => _cursors.AsReadOnly();

    public Cursor CreateCursor(TextPosition? start = null)
    {
        Cursor cursor = new(_model);
        if (start.HasValue)
        {
            cursor.MoveTo(start.Value);
        }
        _cursors.Add(cursor);
        return cursor;
    }

    public void RemoveCursor(Cursor cursor)
    {
        if (_cursors.Remove(cursor))
        {
            cursor.Dispose();
        }
    }

    public IReadOnlyList<TextPosition> GetCursorPositions()
    {
        List<TextPosition> positions = new(_cursors.Count);
        foreach (Cursor c in _cursors)
        {
            positions.Add(c.Selection.Active);
        }
        return positions;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (Cursor c in _cursors.ToArray())
        {
            c.Dispose();
        }
        _cursors.Clear();
    }
}
