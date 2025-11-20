using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Cursor
{
    /// <summary>
    /// A minimal snippet session implementation that supports numbered placeholders like ${1:placeholder}.
    /// It inserts the text and creates decorations for placeholders, providing navigation to next/prev placeholders.
    /// </summary>
    public sealed class SnippetSession : IDisposable
    {
        private readonly TextModel _model;
        private readonly int _ownerId;
        private readonly List<(int Index, TextPosition Start, TextPosition End)> _placeholders = new();
        private int _current = -1;
        private bool _disposed;

        public SnippetSession(TextModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _ownerId = _model.AllocateDecorationOwnerId();
        }

        public bool HasPlaceholders => _placeholders.Count > 0;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _model.RemoveAllDecorations(_ownerId);
        }

        /// <summary>
        /// Inserts a snippet at the given location in the model and identifies placeholders.
        /// </summary>
        public void InsertSnippet(TextPosition start, string snippet)
        {
            // naive parser for ${n:text} forms
            var pattern = new Regex(@"\$\{(\d+):([^}]+)\}");
            var matches = pattern.Matches(snippet);

            // Build plainText while recording placeholder offsets so we can create decorations.
            var builder = new System.Text.StringBuilder(snippet.Length);
            var placeholderPositions = new List<(int Index, int Start, int Length)>();
            int lastIndex = 0;
            foreach (Match match in matches.Cast<Match>())
            {
                var gIndex = match.Index;
                if (gIndex > lastIndex)
                {
                    builder.Append(snippet.Substring(lastIndex, gIndex - lastIndex));
                }

                var index = int.Parse(match.Groups[1].Value);
                var text = match.Groups[2].Value;
                var startPos = builder.Length;
                builder.Append(text);
                placeholderPositions.Add((index, startPos, text.Length));
                lastIndex = gIndex + match.Length;
            }

            if (lastIndex < snippet.Length)
            {
                builder.Append(snippet.Substring(lastIndex));
            }

            var plainText = builder.ToString();

            var startOffset = _model.GetOffsetAt(start);
            _model.PushEditOperations(new[] { new TextEdit(start, start, plainText) }, beforeCursorState: null);

            foreach (var entry in placeholderPositions)
            {
                var absoluteStart = startOffset + entry.Start;
                var absoluteEnd = absoluteStart + entry.Length;
                var startPos = _model.GetPositionAt(absoluteStart);
                var endPos = _model.GetPositionAt(absoluteEnd);
                _placeholders.Add((entry.Index, startPos, endPos));
                var placeholderOptions = new ModelDecorationOptions
                {
                    Description = "snippet-placeholder",
                    RenderKind = DecorationRenderKind.Generic,
                    ShowIfCollapsed = true,
                    InlineDescription = "snippet",
                };
                _model.DeltaDecorations(_ownerId, null, new[] { new ModelDeltaDecoration(new TextRange(absoluteStart, absoluteEnd), placeholderOptions) });
            }

            _placeholders.Sort((a, b) => a.Index.CompareTo(b.Index));
            _current = _placeholders.Count > 0 ? -1 : -1;
            if (_current >= 0)
            {
                // move cursors to first placeholder - callers will coordinate multi-cursor behavior
            }
        }

        public TextPosition? NextPlaceholder()
        {
            if (_placeholders.Count == 0)
            {
                return null;
            }

            _current = Math.Min(_current + 1, _placeholders.Count - 1);
            return _placeholders[_current].Start;
        }

        public TextPosition? PrevPlaceholder()
        {
            if (_placeholders.Count == 0)
            {
                return null;
            }

            _current = Math.Max(-1, _current - 1);
            if (_current == -1)
            {
                return null;
            }

            return _placeholders[_current].Start;
        }
    }
}
