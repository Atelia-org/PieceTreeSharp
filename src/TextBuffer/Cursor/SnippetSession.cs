// Source: ts/src/vs/editor/contrib/snippet/browser/snippetSession.ts
// - Class: SnippetSession (Lines: 30-600)
// Ported: 2025-11-22

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// A minimal snippet session implementation that supports numbered placeholders like ${1:placeholder}.
/// It inserts the text and creates decorations for placeholders, providing navigation to next/prev placeholders.
/// </summary>
public sealed class SnippetSession : IDisposable
{
    private readonly TextModel _model;
    private readonly int _ownerId;
    // Each placeholder keeps a live ModelDecoration so later edits move the range automatically.
    private readonly List<(int Index, ModelDecoration Decoration)> _placeholders = [];
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
        Regex pattern = new(@"\$\{(\d+):([^}]+)\}");
        MatchCollection matches = pattern.Matches(snippet);

        // Build plainText while recording placeholder offsets so we can create decorations.
        StringBuilder builder = new(snippet.Length);
        List<(int Index, int Start, int Length)> placeholderPositions = [];
        int lastIndex = 0;
        foreach (Match match in matches.Cast<Match>())
        {
            int gIndex = match.Index;
            if (gIndex > lastIndex)
            {
                builder.Append(snippet.Substring(lastIndex, gIndex - lastIndex));
            }

            int index = int.Parse(match.Groups[1].Value);
            string text = match.Groups[2].Value;
            int startPos = builder.Length;
            builder.Append(text);
            placeholderPositions.Add((index, startPos, text.Length));
            lastIndex = gIndex + match.Length;
        }

        if (lastIndex < snippet.Length)
        {
            builder.Append(snippet.Substring(lastIndex));
        }

        string plainText = builder.ToString();

        int startOffset = _model.GetOffsetAt(start);
        _model.PushEditOperations([new TextEdit(start, start, plainText)], beforeCursorState: null);

        foreach ((int Index, int Start, int Length) entry in placeholderPositions)
        {
            int absoluteStart = startOffset + entry.Start;
            int absoluteEnd = absoluteStart + entry.Length;
            ModelDecorationOptions placeholderOptions = new()
            {
                Description = "snippet-placeholder",
                RenderKind = DecorationRenderKind.Generic,
                ShowIfCollapsed = true,
                InlineDescription = "snippet",
            };
            IReadOnlyList<ModelDecoration> created = _model.DeltaDecorations(
                _ownerId,
                oldDecorationIds: null,
                new[] { new ModelDeltaDecoration(new TextRange(absoluteStart, absoluteEnd), placeholderOptions) });
            if (created.Count == 1)
            {
                // Storing the decoration itself (instead of the original offsets) keeps navigation in sync with edits.
                _placeholders.Add((entry.Index, created[0]));
            }
        }

        _placeholders.Sort((a, b) => a.Index.CompareTo(b.Index));
        _current = -1;
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

        if (_current >= _placeholders.Count - 1)
        {
            _current = _placeholders.Count; // sentinel meaning "past the end" to prevent infinite loops
            return null;
        }

        _current++;
        return GetPlaceholderStart(_placeholders[_current]);
    }

    public TextPosition? PrevPlaceholder()
    {
        if (_placeholders.Count == 0)
        {
            return null;
        }

        if (_current == _placeholders.Count)
        {
            // if we just walked past the end, jump back to the last placeholder
            _current = _placeholders.Count - 1;
            return GetPlaceholderStart(_placeholders[_current]);
        }

        if (_current <= 0)
        {
            _current = -1;
            return null;
        }

        _current--;
        return GetPlaceholderStart(_placeholders[_current]);
    }

    private TextPosition GetPlaceholderStart((int Index, ModelDecoration Decoration) placeholder)
    {
        int startOffset = placeholder.Decoration.Range.StartOffset;
        // Re-evaluate positions on demand so placeholder edits cannot desync navigation state.
        return _model.GetPositionAt(startOffset);
    }
}
