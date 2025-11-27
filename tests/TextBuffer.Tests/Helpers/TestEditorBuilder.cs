// WS5-PORT: Shared Test Harness - TestEditorBuilder
// Purpose: Quick TextModel construction with preset content and options
// Created: 2025-11-26

using System;
using System.Collections.Generic;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Cursor;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests.Helpers;

/// <summary>
/// Fluent builder for creating TextModel instances with preset configurations for testing.
/// Supports setting content, options, cursors, and selections.
/// </summary>
public sealed class TestEditorBuilder
{
    private string _content = string.Empty;
    private string _eol = "\n";
    private int _tabSize = 4;
    private int _indentSize = 4;
    private bool _indentSizeFollowsTabSize = true;
    private bool _insertSpaces = true;
    private bool _detectIndentation = false;
    private string _languageId = "plaintext";
    private List<Selection> _selections = new();
    private List<TextPosition> _cursors = new();
    private bool _stickyTabStops = false;
    private string _wordSeparators = CursorConfiguration.DefaultWordSeparators;

    /// <summary>
    /// Create a new TestEditorBuilder instance.
    /// </summary>
    public static TestEditorBuilder Create() => new();

    /// <summary>
    /// Set the initial text content.
    /// </summary>
    public TestEditorBuilder WithContent(string content)
    {
        _content = content ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Set content from multiple lines (joined with current EOL).
    /// </summary>
    public TestEditorBuilder WithLines(params string[] lines)
    {
        _content = string.Join(_eol, lines);
        return this;
    }

    /// <summary>
    /// Set the end-of-line sequence.
    /// </summary>
    public TestEditorBuilder WithEol(string eol)
    {
        _eol = eol == "\r\n" ? "\r\n" : "\n";
        return this;
    }

    /// <summary>
    /// Use CRLF line endings.
    /// </summary>
    public TestEditorBuilder WithCRLF() => WithEol("\r\n");

    /// <summary>
    /// Use LF line endings.
    /// </summary>
    public TestEditorBuilder WithLF() => WithEol("\n");

    /// <summary>
    /// Set the tab size.
    /// </summary>
    public TestEditorBuilder WithTabSize(int tabSize)
    {
        _tabSize = Math.Max(1, tabSize);
        if (_indentSizeFollowsTabSize)
        {
            _indentSize = _tabSize;
        }
        return this;
    }

    /// <summary>
    /// Set the indent size.
    /// </summary>
    public TestEditorBuilder WithIndentSize(int indentSize)
    {
        _indentSize = Math.Max(1, indentSize);
        _indentSizeFollowsTabSize = false;
        return this;
    }

    /// <summary>
    /// Set whether to insert spaces instead of tabs.
    /// </summary>
    public TestEditorBuilder WithInsertSpaces(bool insertSpaces)
    {
        _insertSpaces = insertSpaces;
        return this;
    }

    /// <summary>
    /// Set the language ID.
    /// </summary>
    public TestEditorBuilder WithLanguage(string languageId)
    {
        _languageId = languageId ?? "plaintext";
        return this;
    }

    /// <summary>
    /// Enable sticky tab stops.
    /// </summary>
    public TestEditorBuilder WithStickyTabStops(bool enabled = true)
    {
        _stickyTabStops = enabled;
        return this;
    }

    /// <summary>
    /// Enable or disable automatic indentation detection.
    /// Defaults to disabled for deterministic tests.
    /// </summary>
    public TestEditorBuilder WithDetectIndentation(bool enabled = true)
    {
        _detectIndentation = enabled;
        return this;
    }

    /// <summary>
    /// Set custom word separators.
    /// </summary>
    public TestEditorBuilder WithWordSeparators(string separators)
    {
        _wordSeparators = separators ?? CursorConfiguration.DefaultWordSeparators;
        return this;
    }

    /// <summary>
    /// Add a cursor at the specified position.
    /// </summary>
    public TestEditorBuilder WithCursor(int lineNumber, int column)
    {
        _cursors.Add(new TextPosition(lineNumber, column));
        return this;
    }

    /// <summary>
    /// Add a cursor at the specified position.
    /// </summary>
    public TestEditorBuilder WithCursor(TextPosition position)
    {
        _cursors.Add(position);
        return this;
    }

    /// <summary>
    /// Add a selection.
    /// </summary>
    public TestEditorBuilder WithSelection(int startLine, int startColumn, int endLine, int endColumn)
    {
        _selections.Add(new Selection(startLine, startColumn, endLine, endColumn));
        return this;
    }

    /// <summary>
    /// Add a selection.
    /// </summary>
    public TestEditorBuilder WithSelection(Selection selection)
    {
        _selections.Add(selection);
        return this;
    }

    /// <summary>
    /// Add a selection from a Range (uses end as active position).
    /// </summary>
    public TestEditorBuilder WithSelection(Range range)
    {
        _selections.Add(Selection.FromRange(range, SelectionDirection.LTR));
        return this;
    }

    /// <summary>
    /// Build the TextModel with the configured settings.
    /// </summary>
    public TextModel Build()
    {
        var creationOptions = new TextModelCreationOptions
        {
            TabSize = _tabSize,
            IndentSize = _indentSize,
            IndentSizeFollowsTabSize = _indentSizeFollowsTabSize,
            InsertSpaces = _insertSpaces,
            DetectIndentation = _detectIndentation,
            DefaultEol = _eol == "\r\n" ? DefaultEndOfLine.CRLF : DefaultEndOfLine.LF,
        };

        return new TextModel(_content, creationOptions, _languageId);
    }

    /// <summary>
    /// Build and return a TestEditorContext containing the model and cursor configuration.
    /// </summary>
    public TestEditorContext BuildContext()
    {
        var model = Build();
        var modelOptions = model.GetOptions();
        var editorOptions = new EditorCursorOptions
        {
            StickyTabStops = _stickyTabStops,
            WordSeparators = _wordSeparators,
        };
        var cursorConfig = new CursorConfiguration(modelOptions, editorOptions);

        return new TestEditorContext(model, cursorConfig, _selections, _cursors);
    }

    /// <summary>
    /// Parse content with pipe markers (|) to extract cursor positions.
    /// The pipes are removed from the content and their positions recorded.
    /// Example: "hello| world" -> content="hello world", cursor at (1, 6)
    /// </summary>
    public TestEditorBuilder WithMarkedContent(string markedContent)
    {
        var (content, positions) = CursorTestHelper.ParsePipePositions(markedContent);
        _content = content;
        _cursors.AddRange(positions);
        return this;
    }

    /// <summary>
    /// Parse content with selection markers [anchor] and |active|.
    /// Example: "[hello] world|" -> selection from (1,1) to end of line
    /// </summary>
    public TestEditorBuilder WithMarkedSelection(string markedContent)
    {
        var (content, selections) = CursorTestHelper.ParseSelectionMarkers(markedContent);
        _content = content;
        _selections.AddRange(selections);
        return this;
    }
}

/// <summary>
/// Container for a test editor setup with model and cursor configuration.
/// </summary>
public sealed class TestEditorContext
{
    public TextModel Model { get; }
    public CursorConfiguration CursorConfig { get; }
    public IReadOnlyList<Selection> InitialSelections { get; }
    public IReadOnlyList<TextPosition> InitialCursors { get; }

    public TestEditorContext(
        TextModel model,
        CursorConfiguration cursorConfig,
        IReadOnlyList<Selection> selections,
        IReadOnlyList<TextPosition> cursors)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        CursorConfig = cursorConfig ?? throw new ArgumentNullException(nameof(cursorConfig));
        InitialSelections = selections ?? Array.Empty<Selection>();
        InitialCursors = cursors ?? Array.Empty<TextPosition>();
    }

    /// <summary>
    /// Get the primary cursor position (first cursor or start of first selection).
    /// </summary>
    public TextPosition PrimaryCursor
    {
        get
        {
            if (InitialCursors.Count > 0)
            {
                return InitialCursors[0];
            }
            if (InitialSelections.Count > 0)
            {
                return InitialSelections[0].GetPosition();
            }
            return new TextPosition(1, 1);
        }
    }

    /// <summary>
    /// Get the primary selection (first selection or collapsed at first cursor).
    /// </summary>
    public Selection PrimarySelection
    {
        get
        {
            if (InitialSelections.Count > 0)
            {
                return InitialSelections[0];
            }
            if (InitialCursors.Count > 0)
            {
                var pos = InitialCursors[0];
                return Selection.FromPositions(pos, pos);
            }
            return new Selection(1, 1, 1, 1);
        }
    }

    /// <summary>
    /// Create a SingleCursorState from the primary selection.
    /// </summary>
    public SingleCursorState CreateSingleCursorState()
    {
        var sel = PrimarySelection;
        return new SingleCursorState(
            Range.FromPositions(sel.GetSelectionStart(), sel.GetSelectionStart()),
            SelectionStartKind.Simple,
            0,
            sel.GetPosition(),
            0);
    }

    /// <summary>
    /// Get the line content at the specified line number.
    /// </summary>
    public string GetLineContent(int lineNumber) => Model.GetLineContent(lineNumber);

    /// <summary>
    /// Get the full model content.
    /// </summary>
    public string GetValue() => Model.GetValue();

    /// <summary>
    /// Apply edits to the model.
    /// </summary>
    public void ApplyEdits(params TextEdit[] edits) => Model.ApplyEdits(edits);
}
