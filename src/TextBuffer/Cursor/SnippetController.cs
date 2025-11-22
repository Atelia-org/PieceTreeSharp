// Source: ts/src/vs/editor/contrib/snippet/browser/snippetController2.ts
// - Class: SnippetController2 (Lines: 30-500)
// Ported: 2025-11-22

using System;
using System.Collections.Generic;
using System.Linq;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Cursor
{
    /// <summary>
    /// Minimal snippet controller that can create snippet sessions and navigate placeholders.
    /// </summary>
    public sealed class SnippetController : IDisposable
    {
        private readonly TextModel _model;
        private SnippetSession? _session;
        private bool _disposed;

        public SnippetController(TextModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _session?.Dispose();
            _session = null;
        }

        public SnippetSession CreateSession()
        {
            _session?.Dispose();
            _session = new SnippetSession(_model);
            return _session;
        }

        public TextPosition? NextPlaceholder()
        {
            return _session?.NextPlaceholder();
        }

        public TextPosition? PrevPlaceholder()
        {
            return _session?.PrevPlaceholder();
        }

        public void InsertSnippetAt(TextPosition pos, string snippet)
        {
            var session = CreateSession();
            session.InsertSnippet(pos, snippet);
        }
    }
}
