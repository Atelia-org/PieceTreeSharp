// Source: ts/src/vs/editor/common/model/textModel.ts
// - Class: TextModel (Lines: 120-2688)
// - Interfaces: ITextEdit, ITextChange and related types
// Ported: 2025-11-19

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.Services;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer;

public readonly struct TextEdit
{
    public readonly TextPosition Start;
    public readonly TextPosition End;
    public readonly string Text;

    public TextEdit(TextPosition start, TextPosition end, string text)
    {
        Start = start;
        End = end;
        Text = text;
    }
}

public readonly struct TextChange
{
    public readonly TextPosition Start;
    public readonly TextPosition End;
    public readonly string Text;

    public TextChange(TextPosition start, TextPosition end, string text)
    {
        Start = start;
        End = end;
        Text = text;
    }
}

public class TextModelContentChangedEventArgs : EventArgs
{
    public IReadOnlyList<TextChange> Changes { get; }
    public int VersionId { get; }
    public bool IsUndo { get; }
    public bool IsRedo { get; }
    public bool IsFlush { get; }

    public TextModelContentChangedEventArgs(IReadOnlyList<TextChange> changes, int versionId, bool isUndo, bool isRedo, bool isFlush)
    {
        Changes = changes;
        VersionId = versionId;
        IsUndo = isUndo;
        IsRedo = isRedo;
        IsFlush = isFlush;
    }
}

public delegate IReadOnlyList<TextPosition>? CursorStateComputer(IReadOnlyList<TextChange> inverseChanges);

public class TextModelLanguageConfigurationChangedEventArgs : EventArgs
{
    public TextModelLanguageConfigurationChangedEventArgs(string languageId)
    {
        LanguageId = string.IsNullOrWhiteSpace(languageId) ? "plaintext" : languageId;
    }

    public string LanguageId { get; }
}

public class TextModelAttachedChangedEventArgs : EventArgs
{
    public TextModelAttachedChangedEventArgs(bool isAttached)
    {
        IsAttached = isAttached;
    }

    public bool IsAttached { get; }
}

public class TextModel : ITextSearchAccess
{
    private const string DefaultUndoLabel = "Edit";
    private readonly PieceTreeBuffer _buffer;
    private readonly DecorationsTrees _decorationTrees = new();
    private readonly Dictionary<string, ModelDecoration> _decorationsById = new(StringComparer.Ordinal);
    private readonly Dictionary<int, HashSet<string>> _decorationIdsByOwner = new();
    private readonly IUndoRedoService _undoRedoService;
    private readonly ILanguageConfigurationService _languageConfigurationService;
    private readonly EditStack _editStack;
    private TextModelResolvedOptions _options;
    private TextModelCreationOptions _creationOptions;
    private string _languageId;
    private int _versionId = 1;
    private int _alternativeVersionId = 1;
    private string _eol;
    private bool _isUndoing;
    private bool _isRedoing;
    private int _attachedEditorCount;
    private IDisposable? _languageConfigurationSubscription;
    private int _nextDecorationOwnerId = DecorationOwnerIds.SearchHighlights + 1;
    private Cursor.CursorCollection? _cursorCollection;
    private Cursor.SnippetController? _snippetController;

    public event EventHandler<TextModelContentChangedEventArgs>? OnDidChangeContent;
    public event EventHandler<TextModelOptionsChangedEventArgs>? OnDidChangeOptions;
    public event EventHandler<TextModelLanguageChangedEventArgs>? OnDidChangeLanguage;
    public event EventHandler<TextModelDecorationsChangedEventArgs>? OnDidChangeDecorations;
    public event EventHandler<TextModelLanguageConfigurationChangedEventArgs>? OnDidChangeLanguageConfiguration;
    public event EventHandler<TextModelAttachedChangedEventArgs>? OnDidChangeAttached;

    public TextModel(string text, string defaultEol = "\n", string languageId = "plaintext")
        : this(text, TextModelCreationOptions.Default with { DefaultEol = StringToDefaultEol(defaultEol) }, languageId)
    {
    }

    public TextModel(
        string text,
        TextModelCreationOptions? creationOptions,
        string languageId = "plaintext",
        ILanguageConfigurationService? languageConfigurationService = null)
    {
        _languageConfigurationService = languageConfigurationService ?? LanguageConfigurationService.Instance;
        _undoRedoService = InProcUndoRedoService.Instance;
        _creationOptions = creationOptions ?? TextModelCreationOptions.Default;

        _buffer = new PieceTreeBuffer(text);
        _languageId = string.IsNullOrWhiteSpace(languageId) ? "plaintext" : languageId;

        var normalizedEol = NormalizeEol(_creationOptions.DefaultEol == DefaultEndOfLine.CRLF ? "\r\n" : "\n");
        if (_buffer.Length == 0)
        {
            _buffer.SetEol(normalizedEol);
            _eol = normalizedEol;
        }
        else
        {
            _eol = _buffer.GetEol();
        }

        var defaultEol = _eol == "\r\n" ? DefaultEndOfLine.CRLF : DefaultEndOfLine.LF;
        _options = TextModelResolvedOptions.Resolve(_creationOptions, defaultEol);
        _creationOptions = _options.CreationOptions;
        _editStack = new EditStack(this, _undoRedoService);

        if (_creationOptions.DetectIndentation && _buffer.Length > 0)
        {
            DetectIndentation(_creationOptions.InsertSpaces, _creationOptions.TabSize);
        }

        SubscribeToLanguageConfiguration(_languageId);
    }

    public int VersionId => _versionId;
    public int AlternativeVersionId => _alternativeVersionId;
    public string Eol => _eol;
    public bool CanUndo => _editStack.CanUndo;
    public bool CanRedo => _editStack.CanRedo;
    public string LanguageId => _languageId;

    public TextModelResolvedOptions GetOptions() => _options;

    public int AllocateDecorationOwnerId() => Interlocked.Increment(ref _nextDecorationOwnerId);

    public Cursor.CursorCollection CreateCursorCollection()
    {
        if (_cursorCollection is not null)
        {
            return _cursorCollection;
        }

        _cursorCollection = new Cursor.CursorCollection(this);
        return _cursorCollection;
    }

    public Cursor.SnippetController CreateSnippetController()
    {
        _snippetController?.Dispose();
        _snippetController = new Cursor.SnippetController(this);
        return _snippetController;
    }

    public string GetValue() => _buffer.GetText();

    public ITextSnapshot CreateSnapshot(bool preserveBom = false)
    {
        var bom = preserveBom ? _buffer.GetBom() : string.Empty;
        var snapshot = _buffer.InternalModel.CreateSnapshot(bom);
        return new TextModelSnapshot(snapshot);
    }

    public string GetValueInRange(Range range, EndOfLinePreference preference = EndOfLinePreference.TextDefined)
    {
        return preference switch
        {
            EndOfLinePreference.LF => GetValueInRangeInternal(range, normalizeLineEndings: true, out _),
            EndOfLinePreference.CRLF => NormalizeLfToCrLf(GetValueInRangeInternal(range, normalizeLineEndings: true, out _)),
            _ => GetValueInRangeInternal(range, normalizeLineEndings: false, out _),
        };
    }

    public int GetLength() => _buffer.Length;

    public int GetLineCount()
    {
        if (_buffer.Length == 0)
        {
            return 1;
        }

        return _buffer.GetPositionAt(_buffer.Length).LineNumber;
    }

    public string GetLineContent(int lineNumber)
    {
        var content = _buffer.GetLineContent(lineNumber);
        int len = content.Length;
        if (len > 0)
        {
            if (content[len - 1] == '\n')
            {
                len--;
                if (len > 0 && content[len - 1] == '\r')
                {
                    len--;
                }
            }
            else if (content[len - 1] == '\r')
            {
                len--;
            }
        }
        return content.Substring(0, len);
    }

    public int GetLineMaxColumn(int lineNumber) => GetLineContent(lineNumber).Length + 1;

    public int GetOffsetAt(TextPosition position) => _buffer.GetOffsetAt(position.LineNumber, position.Column);

    public TextPosition GetPositionAt(int offset) => _buffer.GetPositionAt(offset);

    #region Position/Range Validation (WS4-PORT-Core)

    /// <summary>
    /// Validate and clamp a position to be within the document bounds.
    /// </summary>
    public TextPosition ValidatePosition(TextPosition position)
    {
        var lineCount = GetLineCount();
        var line = position.LineNumber;

        if (line < 1)
        {
            return new TextPosition(1, 1);
        }

        if (line > lineCount)
        {
            var lastLineMaxCol = GetLineMaxColumn(lineCount);
            return new TextPosition(lineCount, lastLineMaxCol);
        }

        var minColumn = 1;
        var maxColumn = GetLineMaxColumn(line);
        var column = position.Column;

        if (column < minColumn)
        {
            return new TextPosition(line, minColumn);
        }

        if (column > maxColumn)
        {
            return new TextPosition(line, maxColumn);
        }

        return position;
    }

    /// <summary>
    /// Validate and clamp a range to be within the document bounds.
    /// </summary>
    public Range ValidateRange(Range range)
    {
        var start = ValidatePosition(range.GetStartPosition());
        var end = ValidatePosition(range.GetEndPosition());

        // Ensure start <= end
        if (start.CompareTo(end) > 0)
        {
            return new Range(end, start);
        }

        return new Range(start, end);
    }

    #endregion

    #region Tracked Ranges (WS4-PORT-Core)

    private int _nextTrackedRangeId = 1;
    private readonly Dictionary<string, ModelDecoration> _trackedRanges = new(StringComparer.Ordinal);
    private const int TrackedRangeOwnerId = -1; // Special owner ID for tracked ranges

    /// <summary>
    /// Allocate a new tracked range ID.
    /// </summary>
    public string AllocateTrackedRangeId()
    {
        return $"__tracked_range_{Interlocked.Increment(ref _nextTrackedRangeId)}__";
    }

    /// <summary>
    /// Set a tracked range. If id is null, allocates a new ID.
    /// If range is null, removes the tracked range.
    /// Returns the ID of the tracked range (or null if removed).
    /// </summary>
    /// <param name="id">The existing ID, or null to allocate new.</param>
    /// <param name="range">The new range, or null to remove.</param>
    /// <param name="stickiness">How the range should behave during edits.</param>
    /// <returns>The ID of the tracked range, or null if removed.</returns>
    internal string? _setTrackedRange(string? id, Range? range, Decorations.TrackedRangeStickiness stickiness)
    {
        if (range == null)
        {
            // Remove the tracked range
            if (id != null && _trackedRanges.TryGetValue(id, out var existing))
            {
                UnregisterDecoration(existing);
                _trackedRanges.Remove(id);
            }
            return null;
        }

        // Validate the range
        var validatedRange = ValidateRange(range.Value);
        var startOffset = GetOffsetAt(validatedRange.GetStartPosition());
        var endOffset = GetOffsetAt(validatedRange.GetEndPosition());
        var textRange = new Decorations.TextRange(startOffset, endOffset);

        var options = Decorations.ModelDecorationOptions.CreateHiddenOptions(stickiness);

        if (id != null && _trackedRanges.TryGetValue(id, out var existingDecor))
        {
            // Update existing tracked range - just update the range
            var previousRange = existingDecor.Range;
            existingDecor.Range = textRange;
            existingDecor.VersionId = _versionId;
            _decorationTrees.Reinsert(existingDecor);
            return id;
        }
        else
        {
            // Create new tracked range
            var newId = id ?? AllocateTrackedRangeId();
            var decoration = new Decorations.ModelDecoration(newId, TrackedRangeOwnerId, textRange, options);
            decoration.VersionId = _versionId;
            RegisterDecoration(decoration);
            _trackedRanges[newId] = decoration;
            return newId;
        }
    }

    /// <summary>
    /// Get the current range of a tracked range.
    /// Returns null if the ID is not found.
    /// </summary>
    /// <param name="id">The tracked range ID.</param>
    /// <returns>The current range, or null if not found.</returns>
    internal Range? _getTrackedRange(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        if (!_trackedRanges.TryGetValue(id, out var decoration))
        {
            return null;
        }

        var startPos = GetPositionAt(decoration.Range.StartOffset);
        var endPos = GetPositionAt(decoration.Range.EndOffset);
        return new Range(startPos, endPos);
    }

    #endregion

    public IReadOnlyList<FindMatch> FindMatches(string searchString, Range? searchRange, bool isRegex, bool matchCase, string? wordSeparators, bool captureMatches, int limitResultCount = TextModelSearch.DefaultLimit)
    {
        var searchParams = new SearchParams(searchString, isRegex, matchCase, wordSeparators);
        return FindMatches(searchParams, searchRange, captureMatches, limitResultCount);
    }

    public IReadOnlyList<FindMatch> FindMatches(
        string searchString,
        IReadOnlyList<Range>? searchRanges,
        bool findInSelection,
        bool isRegex,
        bool matchCase,
        string? wordSeparators,
        bool captureMatches,
        int limitResultCount = TextModelSearch.DefaultLimit)
    {
        var searchParams = new SearchParams(searchString, isRegex, matchCase, wordSeparators);
        return FindMatches(searchParams, searchRanges, findInSelection, captureMatches, limitResultCount);
    }

    public IReadOnlyList<FindMatch> FindMatches(SearchParams searchParams, Range? searchRange = null, bool captureMatches = false, int limitResultCount = TextModelSearch.DefaultLimit)
    {
        ArgumentNullException.ThrowIfNull(searchParams);
        var searchData = searchParams.ParseSearchRequest();
        if (searchData == null)
        {
            return Array.Empty<FindMatch>();
        }

        var range = searchRange ?? GetDocumentRange();
        var rangeSet = SearchRangeSet.FromRange(this, range);
        return TextModelSearch.FindMatches(this, searchData, rangeSet, captureMatches, limitResultCount);
    }

    public IReadOnlyList<FindMatch> FindMatches(
        SearchParams searchParams,
        IReadOnlyList<Range>? searchRanges,
        bool findInSelection,
        bool captureMatches = false,
        int limitResultCount = TextModelSearch.DefaultLimit)
    {
        ArgumentNullException.ThrowIfNull(searchParams);
        var searchData = searchParams.ParseSearchRequest();
        if (searchData == null)
        {
            return Array.Empty<FindMatch>();
        }

        var rangeSet = SearchRangeSet.FromRanges(this, searchRanges, findInSelection);
        return TextModelSearch.FindMatches(this, searchData, rangeSet, captureMatches, limitResultCount);
    }

    public FindMatch? FindNextMatch(string searchString, TextPosition searchStart, bool isRegex, bool matchCase, string? wordSeparators, bool captureMatches = false)
    {
        var searchParams = new SearchParams(searchString, isRegex, matchCase, wordSeparators);
        return FindNextMatch(searchParams, searchStart, captureMatches);
    }

    public FindMatch? FindNextMatch(
        string searchString,
        TextPosition searchStart,
        IReadOnlyList<Range>? searchRanges,
        bool findInSelection,
        bool isRegex,
        bool matchCase,
        string? wordSeparators,
        bool captureMatches = false)
    {
        var searchParams = new SearchParams(searchString, isRegex, matchCase, wordSeparators);
        return FindNextMatch(searchParams, searchStart, searchRanges, findInSelection, captureMatches);
    }

    public FindMatch? FindNextMatch(SearchParams searchParams, TextPosition searchStart, bool captureMatches = false)
    {
        ArgumentNullException.ThrowIfNull(searchParams);
        var searchData = searchParams.ParseSearchRequest();
        if (searchData == null)
        {
            return null;
        }

        return TextModelSearch.FindNextMatch(this, searchData, searchStart, captureMatches);
    }

    public FindMatch? FindNextMatch(
        SearchParams searchParams,
        TextPosition searchStart,
        IReadOnlyList<Range>? searchRanges,
        bool findInSelection,
        bool captureMatches = false)
    {
        ArgumentNullException.ThrowIfNull(searchParams);
        var searchData = searchParams.ParseSearchRequest();
        if (searchData == null)
        {
            return null;
        }

        var rangeSet = SearchRangeSet.FromRanges(this, searchRanges, findInSelection);
        return TextModelSearch.FindNextMatch(this, searchData, searchStart, captureMatches, rangeSet);
    }

    public FindMatch? FindPreviousMatch(string searchString, TextPosition searchStart, bool isRegex, bool matchCase, string? wordSeparators, bool captureMatches = false)
    {
        var searchParams = new SearchParams(searchString, isRegex, matchCase, wordSeparators);
        return FindPreviousMatch(searchParams, searchStart, captureMatches);
    }

    public FindMatch? FindPreviousMatch(
        string searchString,
        TextPosition searchStart,
        IReadOnlyList<Range>? searchRanges,
        bool findInSelection,
        bool isRegex,
        bool matchCase,
        string? wordSeparators,
        bool captureMatches = false)
    {
        var searchParams = new SearchParams(searchString, isRegex, matchCase, wordSeparators);
        return FindPreviousMatch(searchParams, searchStart, searchRanges, findInSelection, captureMatches);
    }

    public FindMatch? FindPreviousMatch(SearchParams searchParams, TextPosition searchStart, bool captureMatches = false)
    {
        ArgumentNullException.ThrowIfNull(searchParams);
        var searchData = searchParams.ParseSearchRequest();
        if (searchData == null)
        {
            return null;
        }

        return TextModelSearch.FindPreviousMatch(this, searchData, searchStart, captureMatches);
    }

    public FindMatch? FindPreviousMatch(
        SearchParams searchParams,
        TextPosition searchStart,
        IReadOnlyList<Range>? searchRanges,
        bool findInSelection,
        bool captureMatches = false)
    {
        ArgumentNullException.ThrowIfNull(searchParams);
        var searchData = searchParams.ParseSearchRequest();
        if (searchData == null)
        {
            return null;
        }

        var rangeSet = SearchRangeSet.FromRanges(this, searchRanges, findInSelection);
        return TextModelSearch.FindPreviousMatch(this, searchData, searchStart, captureMatches, rangeSet);
    }

    public ModelDecoration AddDecoration(TextRange range, ModelDecorationOptions? options = null, int ownerId = DecorationOwnerIds.Default)
    {
        var decoration = CreateDecoration(range, options ?? ModelDecorationOptions.Default, ownerId);
        RegisterDecoration(decoration);
        RaiseDecorationsChanged(new[] { new DecorationChange(decoration, DecorationDeltaKind.Added) });
        return decoration;
    }

    public IReadOnlyList<ModelDecoration> GetDecorationsInRange(TextRange range, int ownerIdFilter = DecorationOwnerIds.Any)
        => _decorationTrees.Search(range, ownerIdFilter);

    public IReadOnlyList<ModelDecoration> GetAllDecorations(int ownerIdFilter = DecorationOwnerIds.Any)
    {
        if (_decorationTrees.Count == 0)
        {
            return Array.Empty<ModelDecoration>();
        }

        var decorations = new List<ModelDecoration>();
        foreach (var decoration in _decorationTrees.EnumerateAll())
        {
            if (!DecorationOwnerIds.MatchesFilter(ownerIdFilter, decoration.OwnerId))
            {
                continue;
            }

            decorations.Add(decoration);
        }

        return decorations.Count == 0 ? Array.Empty<ModelDecoration>() : decorations;
    }

    public IReadOnlyList<ModelDecoration> GetLineDecorations(int lineNumber, int ownerIdFilter = DecorationOwnerIds.Any)
    {
        if (lineNumber < 1 || lineNumber > GetLineCount())
        {
            return Array.Empty<ModelDecoration>();
        }

        var start = GetOffsetAt(new TextPosition(lineNumber, 1));
        var end = lineNumber == GetLineCount()
            ? _buffer.Length
            : GetOffsetAt(new TextPosition(lineNumber + 1, 1));

        var range = new TextRange(start, end);
        var decorations = _decorationTrees.Search(range, ownerIdFilter);
        if (decorations.Count == 0)
        {
            return Array.Empty<ModelDecoration>();
        }

        var filtered = new List<ModelDecoration>(decorations.Count);
        foreach (var decoration in decorations)
        {
            if (decoration.IsCollapsed && !decoration.Options.ShowIfCollapsed)
            {
                continue;
            }

            filtered.Add(decoration);
        }

        return filtered.Count == 0 ? Array.Empty<ModelDecoration>() : filtered;
    }

    public ModelDecoration? GetDecorationById(string decorationId)
    {
        return _decorationsById.GetValueOrDefault(decorationId);
    }

    public IReadOnlyList<string> GetDecorationIdsByOwner(int ownerId)
    {
        if (ownerId == DecorationOwnerIds.Any)
        {
            return _decorationsById.Count == 0
                ? Array.Empty<string>()
                : _decorationsById.Keys.ToArray();
        }

        if (!_decorationIdsByOwner.TryGetValue(ownerId, out var ids) || ids.Count == 0)
        {
            return Array.Empty<string>();
        }

        return ids.ToArray();
    }

    public IReadOnlyList<ModelDecoration> DeltaDecorations(int ownerId, IReadOnlyList<string>? oldDecorationIds, IReadOnlyList<ModelDeltaDecoration>? newDecorations)
    {
        var changes = new List<DecorationChange>();

        if (oldDecorationIds != null)
        {
            foreach (var id in oldDecorationIds)
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (_decorationsById.TryGetValue(id, out var existing) && existing.OwnerId == ownerId)
                {
                    UnregisterDecoration(existing);
                    changes.Add(new DecorationChange(existing, DecorationDeltaKind.Removed));
                }
            }
        }

        var added = new List<ModelDecoration>();
        if (newDecorations != null)
        {
            foreach (var descriptor in newDecorations)
            {
                var decoration = CreateDecoration(descriptor.Range, descriptor.Options, ownerId);
                RegisterDecoration(decoration);
                added.Add(decoration);
                changes.Add(new DecorationChange(decoration, DecorationDeltaKind.Added));
            }
        }

        if (changes.Count > 0)
        {
            RaiseDecorationsChanged(changes);
        }

        return added;
    }

    public void RemoveAllDecorations(int ownerId)
    {
        if (!_decorationIdsByOwner.TryGetValue(ownerId, out var ids) || ids.Count == 0)
        {
            return;
        }

        DeltaDecorations(ownerId, ids.ToArray(), Array.Empty<ModelDeltaDecoration>());
    }

    public IReadOnlyList<ModelDecoration> HighlightSearchMatches(SearchHighlightOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrEmpty(options.Query))
        {
            RemoveAllDecorations(options.OwnerId);
            return Array.Empty<ModelDecoration>();
        }

        var searchParams = new SearchParams(options.Query, options.IsRegex, options.MatchCase, options.WordSeparators);
        var matches = FindMatches(searchParams, searchRange: null, captureMatches: options.CaptureMatches, limitResultCount: options.Limit);
        var projections = new List<ModelDeltaDecoration>(matches.Count);
        foreach (var match in matches)
        {
            var startOffset = GetOffsetAt(match.Range.Start);
            var endOffset = GetOffsetAt(match.Range.End);
            projections.Add(new ModelDeltaDecoration(new TextRange(startOffset, endOffset), ModelDecorationOptions.CreateSearchMatchOptions()));
        }

        var previous = _decorationIdsByOwner.TryGetValue(options.OwnerId, out var ids)
            ? ids.ToArray()
            : Array.Empty<string>();

        // TODO(FindController): When incremental find wiring lands, wrap highlight refreshes inside dedicated pushStackElement boundaries.
        return DeltaDecorations(options.OwnerId, previous, projections);
    }

    public IReadOnlyList<ModelDecoration> GetInjectedTextInLine(int lineNumber, int ownerIdFilter = DecorationOwnerIds.Any)
    {
        if (lineNumber < 1 || lineNumber > GetLineCount())
        {
            return Array.Empty<ModelDecoration>();
        }

        var lineStart = GetOffsetAt(new TextPosition(lineNumber, 1));
        var lineEnd = lineNumber == GetLineCount()
            ? _buffer.Length
            : GetOffsetAt(new TextPosition(lineNumber + 1, 1));

        var range = new TextRange(lineStart, lineEnd);
        var decorations = _decorationTrees.Search(range, ownerIdFilter, DecorationTreeScope.InjectedText);
        if (decorations.Count == 0)
        {
            return Array.Empty<ModelDecoration>();
        }

        var filtered = new List<ModelDecoration>(decorations.Count);
        foreach (var decoration in decorations)
        {
            if (decoration.IsCollapsed && !decoration.Options.ShowIfCollapsed)
            {
                continue;
            }

            filtered.Add(decoration);
        }

        return filtered;
    }

    public IReadOnlyList<ModelDecoration> GetFontDecorationsInRange(TextRange range, int ownerIdFilter = DecorationOwnerIds.Any)
    {
        var decorations = _decorationTrees.Search(range, ownerIdFilter);
        if (decorations.Count == 0)
        {
            return Array.Empty<ModelDecoration>();
        }

        var result = new List<ModelDecoration>();
        foreach (var decoration in decorations)
        {
            if (decoration.Options.AffectsFont || decoration.Options.LineHeight.HasValue)
            {
                result.Add(decoration);
            }
        }

        return result;
    }

    public IReadOnlyList<ModelDecoration> GetAllMarginDecorations(int ownerIdFilter = DecorationOwnerIds.Any)
    {
        if (_decorationTrees.Count == 0)
        {
            return Array.Empty<ModelDecoration>();
        }

        var result = new List<ModelDecoration>();
        foreach (var decoration in _decorationTrees.EnumerateAll())
        {
            if (!DecorationOwnerIds.MatchesFilter(ownerIdFilter, decoration.OwnerId))
            {
                continue;
            }

            var options = decoration.Options;
            if (options.AffectsGlyphMargin ||
                !string.IsNullOrWhiteSpace(options.MarginClassName) ||
                !string.IsNullOrWhiteSpace(options.LinesDecorationsClassName) ||
                !string.IsNullOrWhiteSpace(options.LineNumberClassName))
            {
                result.Add(decoration);
            }
        }

        return result;
    }

    public void PushStackElement() => _editStack.PushStackElement();

    public void PopStackElement() => _editStack.PopStackElement();

    public void AttachEditor()
    {
        var previous = _attachedEditorCount;
        _attachedEditorCount++;
        if (previous == 0 && _attachedEditorCount == 1)
        {
            OnDidChangeAttached?.Invoke(this, new TextModelAttachedChangedEventArgs(true));
        }
    }

    public void DetachEditor()
    {
        if (_attachedEditorCount == 0)
        {
            return;
        }

        _attachedEditorCount--;
        if (_attachedEditorCount == 0)
        {
            OnDidChangeAttached?.Invoke(this, new TextModelAttachedChangedEventArgs(false));
        }
    }

    public IReadOnlyList<TextChange> PushEditOperations(
        TextEdit[] edits,
        IReadOnlyList<TextPosition>? beforeCursorState = null,
        CursorStateComputer? cursorStateComputer = null,
        string? undoLabel = null,
        bool forceMoveMarkers = false)
    {
        if (edits is null)
        {
            throw new ArgumentNullException(nameof(edits));
        }

        return ApplyEditsInternal(
            edits,
            recordInUndoStack: true,
            isUndo: false,
            isRedo: false,
            undoLabel,
            beforeCursorState,
            cursorStateComputer,
            forceMoveMarkers);
    }

    public void ApplyEdits(TextEdit[] edits, bool forceMoveMarkers = false)
    {
        PushEditOperations(edits ?? Array.Empty<TextEdit>(), forceMoveMarkers: forceMoveMarkers);
    }

    public bool Undo()
    {
        var element = _editStack.PopUndo();
        if (element is null)
        {
            return false;
        }

        ApplyRecordedEdits(element.Element, isUndo: true);
        return true;
    }

    public bool Redo()
    {
        var element = _editStack.PopRedo();
        if (element is null)
        {
            return false;
        }

        ApplyRecordedEdits(element.Element, isUndo: false);
        _editStack.PushRedoResult(element);
        return true;
    }

    public void PushEol(EndOfLineSequence sequence)
    {
        var target = SequenceToString(sequence);
        if (string.Equals(_eol, target, StringComparison.Ordinal))
        {
            return;
        }

        var element = _editStack.GetOrCreateElement(null, null);
        SetEolInternal(target, recordStackDelta: false, isUndo: false, isRedo: false);
        element.RecordEolChange(target, _alternativeVersionId);
    }

    public void SetEol(EndOfLineSequence sequence)
    {
        SetEolInternal(SequenceToString(sequence), recordStackDelta: false, isUndo: false, isRedo: false);
    }

    public void UpdateOptions(TextModelUpdateOptions update)
    {
        var updated = _options.WithUpdate(update);
        if (_options.Equals(updated))
        {
            return;
        }

        var diff = _options.Diff(updated);
        _options = updated;
        _creationOptions = updated.CreationOptions;
        OnDidChangeOptions?.Invoke(this, diff);
    }

    private sealed class SpacesDiffResult
    {
        public int SpacesDiff;
        public bool LooksLikeAlignment;
    }

    private static void ComputeSpacesDiff(string a, int aLength, string b, int bLength, SpacesDiffResult result)
    {
        result.SpacesDiff = 0;
        result.LooksLikeAlignment = false;

        int i = 0;
        var maxCommon = Math.Min(aLength, bLength);
        while (i < maxCommon && a[i] == b[i])
        {
            i++;
        }

        int aSpacesCount = 0;
        int aTabsCount = 0;
        for (int j = i; j < aLength; j++)
        {
            var ch = a[j];
            if (ch == ' ')
            {
                aSpacesCount++;
            }
            else
            {
                aTabsCount++;
            }
        }

        int bSpacesCount = 0;
        int bTabsCount = 0;
        for (int j = i; j < bLength; j++)
        {
            var ch = b[j];
            if (ch == ' ')
            {
                bSpacesCount++;
            }
            else
            {
                bTabsCount++;
            }
        }

        if (aSpacesCount > 0 && aTabsCount > 0)
        {
            return;
        }

        if (bSpacesCount > 0 && bTabsCount > 0)
        {
            return;
        }

        int tabsDiff = Math.Abs(aTabsCount - bTabsCount);
        int spacesDiff = Math.Abs(aSpacesCount - bSpacesCount);

        if (tabsDiff == 0)
        {
            result.SpacesDiff = spacesDiff;

            if (spacesDiff > 0 && bSpacesCount - 1 >= 0 && bSpacesCount - 1 < a.Length && bSpacesCount < b.Length)
            {
                if (b[bSpacesCount] != ' ' && a[bSpacesCount - 1] == ' ')
                {
                    if (a.Length > 0 && a[^1] == ',')
                    {
                        result.LooksLikeAlignment = true;
                    }
                }
            }

            return;
        }

        if (tabsDiff != 0 && spacesDiff % tabsDiff == 0)
        {
            result.SpacesDiff = spacesDiff / tabsDiff;
        }
    }

    private static readonly int[] AllowedTabSizeGuesses = { 2, 4, 6, 8, 3, 5, 7 };
    private const int MaxAllowedTabSizeGuess = 8;

    public void DetectIndentation(bool defaultInsertSpaces, int defaultTabSize)
    {
        var linesCount = Math.Min(GetLineCount(), 10000);
        var linesIndentedWithTabsCount = 0;
        var linesIndentedWithSpacesCount = 0;
        var previousLineText = string.Empty;
        var previousLineIndentation = 0;
        var spacesDiffCount = new int[MaxAllowedTabSizeGuess + 1];
        var diffResult = new SpacesDiffResult();

        for (int line = 1; line <= linesCount; line++)
        {
            var currentLineText = GetLineContent(line);
            var currentLineLength = currentLineText.Length;

            var currentLineHasContent = false;
            var currentLineIndentation = 0;
            var currentLineSpacesCount = 0;
            var currentLineTabsCount = 0;

            for (int j = 0; j < currentLineLength; j++)
            {
                var ch = currentLineText[j];
                if (ch == '\t')
                {
                    currentLineTabsCount++;
                }
                else if (ch == ' ')
                {
                    currentLineSpacesCount++;
                }
                else
                {
                    currentLineHasContent = true;
                    currentLineIndentation = j;
                    break;
                }
            }

            if (!currentLineHasContent)
            {
                continue;
            }

            if (currentLineTabsCount > 0)
            {
                linesIndentedWithTabsCount++;
            }
            else if (currentLineSpacesCount > 1)
            {
                linesIndentedWithSpacesCount++;
            }

            ComputeSpacesDiff(previousLineText, previousLineIndentation, currentLineText, currentLineIndentation, diffResult);

            if (diffResult.LooksLikeAlignment)
            {
                if (!(defaultInsertSpaces && defaultTabSize == diffResult.SpacesDiff))
                {
                    continue;
                }
            }

            var currentSpacesDiff = diffResult.SpacesDiff;
            if (currentSpacesDiff <= MaxAllowedTabSizeGuess)
            {
                spacesDiffCount[currentSpacesDiff]++;
            }

            previousLineText = currentLineText;
            previousLineIndentation = currentLineIndentation;
        }

        var insertSpaces = defaultInsertSpaces;
        if (linesIndentedWithTabsCount != linesIndentedWithSpacesCount)
        {
            insertSpaces = linesIndentedWithTabsCount < linesIndentedWithSpacesCount;
        }

        var tabSize = defaultTabSize;

        if (insertSpaces)
        {
            double tabSizeScore = insertSpaces ? 0 : 0.1 * linesCount;

            foreach (var possibleTabSize in AllowedTabSizeGuesses)
            {
                var possibleScore = spacesDiffCount[possibleTabSize];
                if (possibleScore > tabSizeScore)
                {
                    tabSizeScore = possibleScore;
                    tabSize = possibleTabSize;
                }
            }

            if (tabSize == 4 && spacesDiffCount[4] > 0 && spacesDiffCount[2] > 0 && spacesDiffCount[2] >= spacesDiffCount[4] / 2)
            {
                tabSize = 2;
            }
        }

        UpdateOptions(new TextModelUpdateOptions
        {
            InsertSpaces = insertSpaces,
            TabSize = tabSize,
            IndentSize = tabSize,
        });
    }

    public void SetLanguage(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId) || string.Equals(languageId, _languageId, StringComparison.Ordinal))
        {
            return;
        }

        var previous = _languageId;
        _languageId = languageId;
        SubscribeToLanguageConfiguration(_languageId);
        OnDidChangeLanguage?.Invoke(this, new TextModelLanguageChangedEventArgs(previous, languageId));
    }

    private IReadOnlyList<TextChange> ApplyEditsInternal(
        TextEdit[] edits,
        bool recordInUndoStack,
        bool isUndo,
        bool isRedo,
        string? undoLabel = null,
        IReadOnlyList<TextPosition>? beforeCursorState = null,
        CursorStateComputer? cursorStateComputer = null,
        bool forceMoveMarkers = false)
    {
        if (edits.Length == 0)
        {
            return Array.Empty<TextChange>();
        }

        var pending = PreparePendingEdits(edits);
        if (pending.Count == 0)
        {
            return Array.Empty<TextChange>();
        }

        var documentRangeBeforeEdit = GetDocumentRange();
        var isFlushEdit = pending.Count == 1
            && pending[0].Edit.Start.Equals(documentRangeBeforeEdit.Start)
            && pending[0].Edit.End.Equals(documentRangeBeforeEdit.End);

        var decorationChanges = ApplyPendingEdits(pending, forceMoveMarkers);

        foreach (var edit in pending)
        {
            edit.NewStartPosition = _buffer.GetPositionAt(edit.NewStartOffset);
            edit.NewEndPosition = _buffer.GetPositionAt(edit.NewEndOffset);
        }

        IncreaseVersionId();

        var changes = new List<TextChange>(pending.Count);
        foreach (var edit in pending)
        {
            changes.Add(new TextChange(edit.Edit.Start, edit.Edit.End, edit.NewText));
        }

        var recordedEdits = new List<RecordedEdit>(pending.Count);
        foreach (var edit in pending)
        {
            recordedEdits.Add(new RecordedEdit(
                edit.Edit.Start,
                edit.Edit.End,
                edit.NewStartPosition,
                edit.NewEndPosition,
                edit.OldStartOffset,
                edit.OldEndOffset,
                edit.NewStartOffset,
                edit.NewEndOffset,
                edit.OldText,
                edit.NewText));
        }

        OnDidChangeContent?.Invoke(this, new TextModelContentChangedEventArgs(changes, _versionId, isUndo, isRedo, isFlushEdit));

        if (decorationChanges.Count > 0)
        {
            RaiseDecorationsChanged(decorationChanges);
        }

        if (recordInUndoStack && !_isUndoing && !_isRedoing)
        {
            IReadOnlyList<TextPosition>? afterCursorState = null;
            if (cursorStateComputer != null)
            {
                try
                {
                    afterCursorState = cursorStateComputer(changes);
                }
                catch
                {
                    afterCursorState = null;
                }
            }

            var element = _editStack.GetOrCreateElement(undoLabel ?? DefaultUndoLabel, beforeCursorState);
            element.AppendEdits(recordedEdits, _eol, _alternativeVersionId, afterCursorState);
        }

        return changes;
    }

    private List<PendingEdit> PreparePendingEdits(TextEdit[] edits)
    {
        var pending = new List<PendingEdit>(edits.Length);
        foreach (var edit in edits)
        {
            var startOffset = _buffer.GetOffsetAt(edit.Start.LineNumber, edit.Start.Column);
            var endOffset = _buffer.GetOffsetAt(edit.End.LineNumber, edit.End.Column);
            if (endOffset < startOffset)
            {
                (startOffset, endOffset) = (endOffset, startOffset);
            }

            var newText = edit.Text ?? string.Empty;
            if (startOffset == endOffset && newText.Length == 0)
            {
                continue;
            }

            var oldText = _buffer.GetText(startOffset, endOffset - startOffset);
            pending.Add(new PendingEdit(edit, startOffset, endOffset, oldText));
        }

        pending.Sort((a, b) => a.OldStartOffset.CompareTo(b.OldStartOffset));

        int delta = 0;
        foreach (var edit in pending)
        {
            edit.NewStartOffset = edit.OldStartOffset + delta;
            edit.NewEndOffset = edit.NewStartOffset + edit.NewText.Length;
            delta += edit.NewText.Length - (edit.OldEndOffset - edit.OldStartOffset);
        }

        return pending;
    }

    private List<DecorationChange> ApplyPendingEdits(List<PendingEdit> pending, bool forceMoveMarkers)
    {
        var applyOrder = new List<PendingEdit>(pending);
        applyOrder.Sort((a, b) =>
        {
            var cmp = b.OldStartOffset.CompareTo(a.OldStartOffset);
            if (cmp != 0)
            {
                return cmp;
            }

            return b.OldEndOffset.CompareTo(a.OldEndOffset);
        });

        var decorationChanges = new List<DecorationChange>();
        foreach (var edit in applyOrder)
        {
            var removedLength = edit.OldEndOffset - edit.OldStartOffset;
            var deltas = AdjustDecorationsForEdit(edit.OldStartOffset, removedLength, edit.NewText.Length, forceMoveMarkers);
            if (deltas.Count > 0)
            {
                decorationChanges.AddRange(deltas);
            }

            _buffer.ApplyEdit(edit.OldStartOffset, removedLength, edit.NewText);
        }

        return decorationChanges;
    }

    private void ApplyRecordedEdits(EditStackElement element, bool isUndo)
    {
        _isUndoing = isUndo;
        _isRedoing = !isUndo;
        try
        {
            if (element.Edits.Count > 0)
            {
                var replay = new TextEdit[element.Edits.Count];
                for (int i = 0; i < element.Edits.Count; i++)
                {
                    var recorded = element.Edits[i];
                    replay[i] = isUndo
                        ? new TextEdit(recorded.NewStart, recorded.NewEnd, recorded.OldText)
                        : new TextEdit(recorded.OldStart, recorded.OldEnd, recorded.NewText);
                }

                ApplyEditsInternal(replay, recordInUndoStack: false, isUndo: isUndo, isRedo: !isUndo);
            }

            var targetEol = isUndo ? element.BeforeEol : element.AfterEol;
            if (!string.Equals(_eol, targetEol, StringComparison.Ordinal))
            {
                SetEolInternal(targetEol, recordStackDelta: false, isUndo: isUndo, isRedo: !isUndo);
            }

            OverwriteAlternativeVersionId(isUndo ? element.BeforeVersionId : element.AfterVersionId);
        }
        finally
        {
            _isUndoing = false;
            _isRedoing = false;
        }
    }

    private void SetEolInternal(string newEol, bool recordStackDelta, bool isUndo, bool isRedo)
    {
        if (string.Equals(_eol, newEol, StringComparison.Ordinal))
        {
            return;
        }

        var oldEnd = GetDocumentEndPosition();
        _buffer.SetEol(newEol);
        _eol = newEol;
        IncreaseVersionId();

        var change = new TextChange(new TextPosition(1, 1), oldEnd, _buffer.GetText());
        OnDidChangeContent?.Invoke(this, new TextModelContentChangedEventArgs(new[] { change }, _versionId, isUndo, isRedo, false));

        if (recordStackDelta)
        {
            var element = _editStack.GetOrCreateElement(null, null);
            element.RecordEolChange(newEol, _alternativeVersionId);
        }

        _options = _options.WithDefaultEol(newEol == "\r\n" ? DefaultEndOfLine.CRLF : DefaultEndOfLine.LF);
        _creationOptions = _options.CreationOptions;
    }

    private TextPosition GetDocumentEndPosition()
    {
        var lineCount = GetLineCount();
        var lastLine = GetLineContent(lineCount);
        return new TextPosition(lineCount, lastLine.Length + 1);
    }

    private void IncreaseVersionId()
    {
        _versionId++;
        _alternativeVersionId++;
    }

    private void OverwriteAlternativeVersionId(int newValue)
    {
        _alternativeVersionId = newValue;
    }

    private Range GetDocumentRange()
    {
        var lineCount = GetLineCount();
        var end = new TextPosition(lineCount, GetLineMaxColumn(lineCount));
        return new Range(new TextPosition(1, 1), end);
    }

    private string GetValueInRangeInternal(Range range, bool normalizeLineEndings, out LineFeedCounter? lineFeedCounter)
    {
        var startOffset = GetOffsetAt(range.Start);
        var endOffset = GetOffsetAt(range.End);
        if (endOffset < startOffset)
        {
            (startOffset, endOffset) = (endOffset, startOffset);
        }

        var length = Math.Max(0, endOffset - startOffset);
        var value = _buffer.GetText(startOffset, length);
        if (!normalizeLineEndings)
        {
            lineFeedCounter = null;
            return value;
        }

        if (_eol == "\n")
        {
            lineFeedCounter = null;
            return value;
        }

        var normalized = NormalizeToLf(value);
        lineFeedCounter = new LineFeedCounter(normalized);
        return normalized;
    }

    private static string NormalizeLfToCrLf(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text.Replace("\n", "\r\n");
    }

    private static string NormalizeToLf(string text)
    {
        if (string.IsNullOrEmpty(text) || text.IndexOf('\r') < 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
                builder.Append('\n');
            }
            else
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    int ITextSearchAccess.LineCount => GetLineCount();

    string ITextSearchAccess.EndOfLine => _eol;

    string ITextSearchAccess.GetLineContent(int lineNumber) => GetLineContent(lineNumber);

    int ITextSearchAccess.GetLineMaxColumn(int lineNumber) => GetLineMaxColumn(lineNumber);

    int ITextSearchAccess.GetOffsetAt(TextPosition position) => GetOffsetAt(position);

    TextPosition ITextSearchAccess.GetPositionAt(int offset) => GetPositionAt(offset);

    string ITextSearchAccess.GetValueInRange(Range range, bool normalizeLineEndings, out LineFeedCounter? lineFeedCounter)
        => GetValueInRangeInternal(range, normalizeLineEndings, out lineFeedCounter);

    private static string SequenceToString(EndOfLineSequence sequence) => sequence == EndOfLineSequence.CRLF ? "\r\n" : "\n";

    private static string NormalizeEol(string value) => string.Equals(value, "\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    private static DefaultEndOfLine StringToDefaultEol(string value)
    {
        return string.Equals(NormalizeEol(value), "\r\n", StringComparison.Ordinal)
            ? DefaultEndOfLine.CRLF
            : DefaultEndOfLine.LF;
    }

    private static int GreatestCommonDivisor(int left, int right)
    {
        left = Math.Abs(left);
        right = Math.Abs(right);
        while (right != 0)
        {
            var temp = right;
            right = left % right;
            left = temp;
        }

        return left == 0 ? 1 : left;
    }

    private ModelDecoration CreateDecoration(TextRange range, ModelDecorationOptions options, int ownerId)
    {
        var decoration = new ModelDecoration(Guid.NewGuid().ToString(), ownerId, range, options);
        decoration.VersionId = _versionId;
        return decoration;
    }

    private void RegisterDecoration(ModelDecoration decoration)
    {
        _decorationsById[decoration.Id] = decoration;
        _decorationTrees.Insert(decoration);
        TrackDecoration(decoration);
    }

    private void UnregisterDecoration(ModelDecoration decoration)
    {
        _decorationTrees.Remove(decoration);
        _decorationsById.Remove(decoration.Id);
        UntrackDecoration(decoration);
    }

    private void TrackDecoration(ModelDecoration decoration)
    {
        if (!_decorationIdsByOwner.TryGetValue(decoration.OwnerId, out var ids))
        {
            ids = new HashSet<string>(StringComparer.Ordinal);
            _decorationIdsByOwner[decoration.OwnerId] = ids;
        }

        ids.Add(decoration.Id);
    }

    private void UntrackDecoration(ModelDecoration decoration)
    {
        if (_decorationIdsByOwner.TryGetValue(decoration.OwnerId, out var ids))
        {
            ids.Remove(decoration.Id);
            if (ids.Count == 0)
            {
                _decorationIdsByOwner.Remove(decoration.OwnerId);
            }
        }
    }

    private void RaiseDecorationsChanged(IReadOnlyList<DecorationChange> changes)
    {
        if (changes.Count == 0)
        {
            return;
        }

        var args = BuildDecorationsChangedEventArgs(changes);
        OnDidChangeDecorations?.Invoke(this, args);
    }

    private TextModelDecorationsChangedEventArgs BuildDecorationsChangedEventArgs(IReadOnlyList<DecorationChange> changes)
    {
        var affectsMinimap = false;
        var affectsOverviewRuler = false;
        var affectsGlyphMargin = false;
        var affectsLineNumber = false;
        var injectedLines = new SortedSet<int>();
        var lineHeightChanges = new HashSet<LineHeightChange>();
        var fontLineChanges = new HashSet<LineFontChange>();

        foreach (var change in changes)
        {
            var options = change.Options;
            affectsMinimap |= options.AffectsMinimap;
            affectsOverviewRuler |= options.AffectsOverviewRuler;
            affectsGlyphMargin |= options.AffectsGlyphMargin;
            affectsLineNumber |= options.AffectsLineNumber || !string.IsNullOrWhiteSpace(options.LineNumberClassName);

            RecordDecorationRange(change.OwnerId, change.Id, change.Range, options, injectedLines, lineHeightChanges, fontLineChanges);
            if (change.OldRange.HasValue)
            {
                RecordDecorationRange(change.OwnerId, change.Id, change.OldRange.Value, options, injectedLines, lineHeightChanges, fontLineChanges);
            }
        }

        return new TextModelDecorationsChangedEventArgs(
            changes,
            _versionId,
            affectsMinimap,
            affectsOverviewRuler,
            affectsGlyphMargin,
            affectsLineNumber,
            injectedLines.ToArray(),
            lineHeightChanges.ToArray(),
            fontLineChanges.ToArray());
    }

    private void RecordDecorationRange(
        int ownerId,
        string decorationId,
        TextRange range,
        ModelDecorationOptions options,
        SortedSet<int> injectedLines,
        HashSet<LineHeightChange> lineHeightChanges,
        HashSet<LineFontChange> fontLineChanges)
    {
        if (!options.HasInjectedText && !options.LineHeight.HasValue && !options.AffectsFont)
        {
            return;
        }

        var startOffset = range.StartOffset;
        var endOffset = range.EndOffset;
        var startLine = GetPositionAt(startOffset).LineNumber;
        var endLine = GetPositionAt(endOffset).LineNumber;
        if (endLine < startLine)
        {
            endLine = startLine;
        }

        if (options.HasInjectedText)
        {
            for (int line = startLine; line <= endLine; line++)
            {
                injectedLines.Add(line);
            }
        }

        if (options.LineHeight.HasValue)
        {
            for (int line = startLine; line <= endLine; line++)
            {
                lineHeightChanges.Add(new LineHeightChange(ownerId, decorationId, line, options.LineHeight));
            }
        }

        if (options.AffectsFont)
        {
            for (int line = startLine; line <= endLine; line++)
            {
                fontLineChanges.Add(new LineFontChange(ownerId, decorationId, line));
            }
        }
    }

    private IReadOnlyList<DecorationChange> AdjustDecorationsForEdit(int offset, int removedLength, int insertedLength, bool forceMoveMarkers)
    {
        if (_decorationTrees.Count == 0 || (removedLength == 0 && insertedLength == 0))
        {
            return Array.Empty<DecorationChange>();
        }

        var processed = new HashSet<string>(StringComparer.Ordinal);
        var changes = new List<DecorationChange>();
        var searchEnd = removedLength > 0 ? offset + removedLength : offset + insertedLength;
        var overlaps = _decorationTrees.Search(new TextRange(Math.Max(0, offset - 1), Math.Max(offset, searchEnd + 1)));
        ProcessDecorations(overlaps);
        ProcessDecorations(_decorationTrees.EnumerateFrom(offset));

        return changes;

        void ProcessDecorations(IEnumerable<ModelDecoration> decorations)
        {
            foreach (var decoration in decorations)
            {
                if (!processed.Add(decoration.Id))
                {
                    continue;
                }

                var previousRange = decoration.Range;
                if (DecorationRangeUpdater.ApplyEdit(decoration, offset, removedLength, insertedLength, forceMoveMarkers))
                {
                    decoration.VersionId = _versionId;
                    _decorationTrees.Reinsert(decoration);
                    changes.Add(new DecorationChange(decoration, DecorationDeltaKind.Updated, previousRange));
                }
            }
        }
    }

    private void SubscribeToLanguageConfiguration(string languageId)
    {
        _languageConfigurationSubscription?.Dispose();
        _languageConfigurationSubscription = _languageConfigurationService.Subscribe(languageId, HandleLanguageConfigurationChanged);
    }

    private void HandleLanguageConfigurationChanged(object? sender, LanguageConfigurationChangedEventArgs args)
    {
        OnDidChangeLanguageConfiguration?.Invoke(this, new TextModelLanguageConfigurationChangedEventArgs(args.LanguageId));
    }

    

    private sealed class PendingEdit
    {
        public PendingEdit(TextEdit edit, int oldStartOffset, int oldEndOffset, string oldText)
        {
            Edit = edit;
            OldStartOffset = oldStartOffset;
            OldEndOffset = oldEndOffset;
            OldText = oldText;
            NewText = edit.Text ?? string.Empty;
        }

        public TextEdit Edit { get; }
        public int OldStartOffset { get; }
        public int OldEndOffset { get; }
        public string OldText { get; }
        public string NewText { get; }
        public int NewStartOffset { get; set; }
        public int NewEndOffset { get; set; }
        public TextPosition NewStartPosition { get; set; }
        public TextPosition NewEndPosition { get; set; }
    }
}
