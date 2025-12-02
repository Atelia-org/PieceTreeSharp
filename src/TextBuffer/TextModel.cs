// Source: ts/src/vs/editor/common/model/textModel.ts
// - Class: TextModel (Lines: 120-2688)
// - Interfaces: ITextEdit, ITextChange and related types
// Ported: 2025-11-19

using System.Text;
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
    private readonly Dictionary<int, HashSet<string>> _decorationIdsByOwner = [];
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
    private int _nextDecorationOwnerId = DecorationOwnerIds.FirstAllocatableOwnerId;
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

        string normalizedEol = NormalizeEol(_creationOptions.DefaultEol == DefaultEndOfLine.CRLF ? "\r\n" : "\n");
        if (_buffer.Length == 0)
        {
            _buffer.SetEol(normalizedEol);
            _eol = normalizedEol;
        }
        else
        {
            _eol = _buffer.GetEol();
        }

        DefaultEndOfLine defaultEol = _eol == "\r\n" ? DefaultEndOfLine.CRLF : DefaultEndOfLine.LF;
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

    public Cursor.CursorCollection CreateCursorCollection(Cursor.EditorCursorOptions? editorOptions = null)
    {
        if (_cursorCollection is not null)
        {
            return _cursorCollection;
        }

        Cursor.CursorContext context = Cursor.CursorContext.FromModel(this, editorOptions);
        _cursorCollection = new Cursor.CursorCollection(context);
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
        string bom = preserveBom ? _buffer.GetBom() : string.Empty;
        ITextSnapshot snapshot = _buffer.InternalModel.CreateSnapshot(bom);
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
        string content = _buffer.GetLineContent(lineNumber);
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
        int lineCount = GetLineCount();
        int line = position.LineNumber;

        if (line < 1)
        {
            return new TextPosition(1, 1);
        }

        if (line > lineCount)
        {
            int lastLineMaxCol = GetLineMaxColumn(lineCount);
            return new TextPosition(lineCount, lastLineMaxCol);
        }

        int minColumn = 1;
        int maxColumn = GetLineMaxColumn(line);
        int column = position.Column;

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
        TextPosition start = ValidatePosition(range.GetStartPosition());
        TextPosition end = ValidatePosition(range.GetEndPosition());

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
            if (id != null && _trackedRanges.TryGetValue(id, out ModelDecoration? existing))
            {
                UnregisterDecoration(existing);
                _trackedRanges.Remove(id);
            }
            return null;
        }

        // Validate the range
        Range validatedRange = ValidateRange(range.Value);
        int startOffset = GetOffsetAt(validatedRange.GetStartPosition());
        int endOffset = GetOffsetAt(validatedRange.GetEndPosition());
        TextRange textRange = new(startOffset, endOffset);

        ModelDecorationOptions options = Decorations.ModelDecorationOptions.CreateHiddenOptions(stickiness);

        if (id != null && _trackedRanges.TryGetValue(id, out ModelDecoration? existingDecor))
        {
            // Update existing tracked range - just update the range
            TextRange previousRange = existingDecor.Range;
            existingDecor.Range = textRange;
            existingDecor.VersionId = _versionId;
            _decorationTrees.Reinsert(existingDecor);
            return id;
        }
        else
        {
            // Create new tracked range
            string newId = id ?? AllocateTrackedRangeId();
            ModelDecoration decoration = new(newId, TrackedRangeOwnerId, textRange, options);
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

        if (!_trackedRanges.TryGetValue(id, out ModelDecoration? decoration))
        {
            return null;
        }

        TextPosition startPos = GetPositionAt(decoration.Range.StartOffset);
        TextPosition endPos = GetPositionAt(decoration.Range.EndOffset);
        return new Range(startPos, endPos);
    }

    #endregion

    public IReadOnlyList<FindMatch> FindMatches(string searchString, Range? searchRange, bool isRegex, bool matchCase, string? wordSeparators, bool captureMatches, int limitResultCount = TextModelSearch.DefaultLimit)
    {
        SearchParams searchParams = new(searchString, isRegex, matchCase, wordSeparators);
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
        SearchParams searchParams = new(searchString, isRegex, matchCase, wordSeparators);
        return FindMatches(searchParams, searchRanges, findInSelection, captureMatches, limitResultCount);
    }

    public IReadOnlyList<FindMatch> FindMatches(SearchParams searchParams, Range? searchRange = null, bool captureMatches = false, int limitResultCount = TextModelSearch.DefaultLimit)
    {
        ArgumentNullException.ThrowIfNull(searchParams);
        SearchData? searchData = searchParams.ParseSearchRequest();
        if (searchData == null)
        {
            return Array.Empty<FindMatch>();
        }

        Range range = searchRange ?? GetDocumentRange();
        SearchRangeSet rangeSet = SearchRangeSet.FromRange(this, range);
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
        SearchData? searchData = searchParams.ParseSearchRequest();
        if (searchData == null)
        {
            return Array.Empty<FindMatch>();
        }

        SearchRangeSet rangeSet = SearchRangeSet.FromRanges(this, searchRanges, findInSelection);
        return TextModelSearch.FindMatches(this, searchData, rangeSet, captureMatches, limitResultCount);
    }

    public FindMatch? FindNextMatch(string searchString, TextPosition searchStart, bool isRegex, bool matchCase, string? wordSeparators, bool captureMatches = false)
    {
        SearchParams searchParams = new(searchString, isRegex, matchCase, wordSeparators);
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
        SearchParams searchParams = new(searchString, isRegex, matchCase, wordSeparators);
        return FindNextMatch(searchParams, searchStart, searchRanges, findInSelection, captureMatches);
    }

    public FindMatch? FindNextMatch(SearchParams searchParams, TextPosition searchStart, bool captureMatches = false)
    {
        ArgumentNullException.ThrowIfNull(searchParams);
        SearchData? searchData = searchParams.ParseSearchRequest();
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
        SearchData? searchData = searchParams.ParseSearchRequest();
        if (searchData == null)
        {
            return null;
        }

        SearchRangeSet rangeSet = SearchRangeSet.FromRanges(this, searchRanges, findInSelection);
        return TextModelSearch.FindNextMatch(this, searchData, searchStart, captureMatches, rangeSet);
    }

    public FindMatch? FindPreviousMatch(string searchString, TextPosition searchStart, bool isRegex, bool matchCase, string? wordSeparators, bool captureMatches = false)
    {
        SearchParams searchParams = new(searchString, isRegex, matchCase, wordSeparators);
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
        SearchParams searchParams = new(searchString, isRegex, matchCase, wordSeparators);
        return FindPreviousMatch(searchParams, searchStart, searchRanges, findInSelection, captureMatches);
    }

    public FindMatch? FindPreviousMatch(SearchParams searchParams, TextPosition searchStart, bool captureMatches = false)
    {
        ArgumentNullException.ThrowIfNull(searchParams);
        SearchData? searchData = searchParams.ParseSearchRequest();
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
        SearchData? searchData = searchParams.ParseSearchRequest();
        if (searchData == null)
        {
            return null;
        }

        SearchRangeSet rangeSet = SearchRangeSet.FromRanges(this, searchRanges, findInSelection);
        return TextModelSearch.FindPreviousMatch(this, searchData, searchStart, captureMatches, rangeSet);
    }

    public ModelDecoration AddDecoration(TextRange range, ModelDecorationOptions? options = null, int ownerId = DecorationOwnerIds.Any)
    {
        ModelDecoration decoration = CreateDecoration(range, options ?? ModelDecorationOptions.Default, ownerId);
        RegisterDecoration(decoration);
        RaiseDecorationsChanged(new[] { new DecorationChange(decoration, DecorationDeltaKind.Added) });
        return decoration;
    }

    public IReadOnlyList<ModelDecoration> GetDecorationsInRange(TextRange range, int ownerIdFilter = DecorationOwnerIds.Any)
        => GetDecorationsInRange(range, ownerIdFilter, filterOutValidation: false, filterFontDecorations: false, onlyMinimapDecorations: false, onlyMarginDecorations: false);

    /// <summary>
    /// Get decorations in a range with explicit TS-style filters.
    /// </summary>
    public IReadOnlyList<ModelDecoration> GetDecorationsInRange(
        TextRange range,
        int ownerIdFilter,
        bool filterOutValidation,
        bool filterFontDecorations,
        bool onlyMinimapDecorations,
        bool onlyMarginDecorations)
    {
        DecorationSearchOptions options = new()
        {
            OwnerFilter = ownerIdFilter,
            FilterOutValidation = filterOutValidation,
            FilterFontDecorations = filterFontDecorations,
            OnlyMinimapDecorations = onlyMinimapDecorations,
            OnlyMarginDecorations = onlyMarginDecorations,
        };
        return GetDecorationsInRange(range, options);
    }

    /// <summary>
    /// Get decorations in a range with filtering options.
    /// Mirrors TS getDecorationsInRange with filter parameters.
    /// </summary>
    public IReadOnlyList<ModelDecoration> GetDecorationsInRange(TextRange range, DecorationSearchOptions options)
        => _decorationTrees.Search(range, options);

    public IReadOnlyList<ModelDecoration> GetAllDecorations(int ownerIdFilter = DecorationOwnerIds.Any)
        => GetAllDecorations(ownerIdFilter, filterOutValidation: false, filterFontDecorations: false);

    public IReadOnlyList<ModelDecoration> GetAllDecorations(int ownerIdFilter, bool filterOutValidation, bool filterFontDecorations)
    {
        if (_decorationTrees.Count == 0)
        {
            return Array.Empty<ModelDecoration>();
        }

        DecorationSearchOptions options = new()
        {
            OwnerFilter = ownerIdFilter,
            FilterOutValidation = filterOutValidation,
            FilterFontDecorations = filterFontDecorations,
        };

        return GetAllDecorations(options);
    }

    /// <summary>
    /// Get all decorations with filtering options.
    /// </summary>
    public IReadOnlyList<ModelDecoration> GetAllDecorations(DecorationSearchOptions options)
    {
        if (_decorationTrees.Count == 0)
        {
            return Array.Empty<ModelDecoration>();
        }

        // Use full document range
        TextRange range = new(0, _buffer.Length);
        return _decorationTrees.Search(range, options);
    }

    public IReadOnlyList<ModelDecoration> GetLineDecorations(int lineNumber, int ownerIdFilter = DecorationOwnerIds.Any)
        => GetLineDecorations(lineNumber, ownerIdFilter, filterOutValidation: false, filterFontDecorations: false);

    public IReadOnlyList<ModelDecoration> GetLineDecorations(int lineNumber, int ownerIdFilter, bool filterOutValidation, bool filterFontDecorations)
    {
        DecorationSearchOptions options = new()
        {
            OwnerFilter = ownerIdFilter,
            FilterOutValidation = filterOutValidation,
            FilterFontDecorations = filterFontDecorations,
        };
        return GetLineDecorations(lineNumber, options);
    }

    /// <summary>
    /// Get decorations for a specific line with filtering options.
    /// Mirrors TS getLineDecorations with filter parameters.
    /// </summary>
    public IReadOnlyList<ModelDecoration> GetLineDecorations(int lineNumber, DecorationSearchOptions options)
    {
        if (lineNumber < 1 || lineNumber > GetLineCount())
        {
            return Array.Empty<ModelDecoration>();
        }

        int start = GetOffsetAt(new TextPosition(lineNumber, 1));
        int end = lineNumber == GetLineCount()
            ? _buffer.Length
            : GetOffsetAt(new TextPosition(lineNumber + 1, 1));

        TextRange range = new(start, end);
        IReadOnlyList<ModelDecoration> decorations = _decorationTrees.Search(range, options);
        if (decorations.Count == 0)
        {
            return Array.Empty<ModelDecoration>();
        }

        // Apply ShowIfCollapsed filter (not part of DecorationSearchOptions)
        List<ModelDecoration> filtered = new(decorations.Count);
        foreach (ModelDecoration decoration in decorations)
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

        if (!_decorationIdsByOwner.TryGetValue(ownerId, out HashSet<string>? ids) || ids.Count == 0)
        {
            return Array.Empty<string>();
        }

        return ids.ToArray();
    }

    public IReadOnlyList<ModelDecoration> DeltaDecorations(int ownerId, IReadOnlyList<string>? oldDecorationIds, IReadOnlyList<ModelDeltaDecoration>? newDecorations)
    {
        List<DecorationChange> changes = [];

        if (oldDecorationIds != null)
        {
            foreach (string id in oldDecorationIds)
            {
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (_decorationsById.TryGetValue(id, out ModelDecoration? existing) && existing.OwnerId == ownerId)
                {
                    UnregisterDecoration(existing);
                    changes.Add(new DecorationChange(existing, DecorationDeltaKind.Removed));
                }
            }
        }

        List<ModelDecoration> added = [];
        if (newDecorations != null)
        {
            foreach (ModelDeltaDecoration descriptor in newDecorations)
            {
                ModelDecoration decoration = CreateDecoration(descriptor.Range, descriptor.Options, ownerId);
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
        if (!_decorationIdsByOwner.TryGetValue(ownerId, out HashSet<string>? ids) || ids.Count == 0)
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

        SearchParams searchParams = new(options.Query, options.IsRegex, options.MatchCase, options.WordSeparators);
        IReadOnlyList<FindMatch> matches = FindMatches(searchParams, searchRange: null, captureMatches: options.CaptureMatches, limitResultCount: options.Limit);
        List<ModelDeltaDecoration> projections = new(matches.Count);
        foreach (FindMatch match in matches)
        {
            int startOffset = GetOffsetAt(match.Range.Start);
            int endOffset = GetOffsetAt(match.Range.End);
            projections.Add(new ModelDeltaDecoration(new TextRange(startOffset, endOffset), ModelDecorationOptions.CreateSearchMatchOptions()));
        }

        string[] previous = _decorationIdsByOwner.TryGetValue(options.OwnerId, out HashSet<string>? ids)
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

        int lineStart = GetOffsetAt(new TextPosition(lineNumber, 1));
        int lineEnd = lineNumber == GetLineCount()
            ? _buffer.Length
            : GetOffsetAt(new TextPosition(lineNumber + 1, 1));

        TextRange range = new(lineStart, lineEnd);
        DecorationSearchOptions options = new()
        {
            OwnerFilter = ownerIdFilter,
            Scope = DecorationTreeScope.InjectedText,
        };
        IReadOnlyList<ModelDecoration> decorations = _decorationTrees.Search(range, options);
        if (decorations.Count == 0)
        {
            return Array.Empty<ModelDecoration>();
        }

        List<ModelDecoration> filtered = new(decorations.Count);
        foreach (ModelDecoration decoration in decorations)
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
        IReadOnlyList<ModelDecoration> decorations = _decorationTrees.Search(range, ownerIdFilter);
        if (decorations.Count == 0)
        {
            return Array.Empty<ModelDecoration>();
        }

        List<ModelDecoration> result = [];
        foreach (ModelDecoration decoration in decorations)
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

        TextRange range = new(0, _buffer.Length);
        DecorationSearchOptions options = new()
        {
            OwnerFilter = ownerIdFilter,
            OnlyMarginDecorations = true,
        };

        IReadOnlyList<ModelDecoration> decorations = _decorationTrees.Search(range, options);
        return decorations.Count == 0 ? Array.Empty<ModelDecoration>() : decorations;
    }

    public void PushStackElement() => _editStack.PushStackElement();

    public void PopStackElement() => _editStack.PopStackElement();

    public void AttachEditor()
    {
        int previous = _attachedEditorCount;
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
        TextModelUndoRedoElement? element = _editStack.PopUndo();
        if (element is null)
        {
            return false;
        }

        ApplyRecordedEdits(element.Element, isUndo: true);
        return true;
    }

    public bool Redo()
    {
        TextModelUndoRedoElement? element = _editStack.PopRedo();
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
        string target = SequenceToString(sequence);
        if (string.Equals(_eol, target, StringComparison.Ordinal))
        {
            return;
        }

        EditStackElement element = _editStack.GetOrCreateElement(null, null);
        SetEolInternal(target, recordStackDelta: false, isUndo: false, isRedo: false);
        element.RecordEolChange(target, _alternativeVersionId);
    }

    public void SetEol(EndOfLineSequence sequence)
    {
        SetEolInternal(SequenceToString(sequence), recordStackDelta: false, isUndo: false, isRedo: false);
    }

    public void UpdateOptions(TextModelUpdateOptions update)
    {
        TextModelResolvedOptions updated = _options.WithUpdate(update);
        if (_options.Equals(updated))
        {
            return;
        }

        TextModelOptionsChangedEventArgs diff = _options.Diff(updated);
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
        int maxCommon = Math.Min(aLength, bLength);
        while (i < maxCommon && a[i] == b[i])
        {
            i++;
        }

        int aSpacesCount = 0;
        int aTabsCount = 0;
        for (int j = i; j < aLength; j++)
        {
            char ch = a[j];
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
            char ch = b[j];
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

    private static readonly int[] AllowedTabSizeGuesses = [2, 4, 6, 8, 3, 5, 7];
    private const int MaxAllowedTabSizeGuess = 8;

    public void DetectIndentation(bool defaultInsertSpaces, int defaultTabSize)
    {
        int linesCount = Math.Min(GetLineCount(), 10000);
        int linesIndentedWithTabsCount = 0;
        int linesIndentedWithSpacesCount = 0;
        string previousLineText = string.Empty;
        int previousLineIndentation = 0;
        int[] spacesDiffCount = new int[MaxAllowedTabSizeGuess + 1];
        SpacesDiffResult diffResult = new();

        for (int line = 1; line <= linesCount; line++)
        {
            string currentLineText = GetLineContent(line);
            int currentLineLength = currentLineText.Length;

            bool currentLineHasContent = false;
            int currentLineIndentation = 0;
            int currentLineSpacesCount = 0;
            int currentLineTabsCount = 0;

            for (int j = 0; j < currentLineLength; j++)
            {
                char ch = currentLineText[j];
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

            int currentSpacesDiff = diffResult.SpacesDiff;
            if (currentSpacesDiff <= MaxAllowedTabSizeGuess)
            {
                spacesDiffCount[currentSpacesDiff]++;
            }

            previousLineText = currentLineText;
            previousLineIndentation = currentLineIndentation;
        }

        bool insertSpaces = defaultInsertSpaces;
        if (linesIndentedWithTabsCount != linesIndentedWithSpacesCount)
        {
            insertSpaces = linesIndentedWithTabsCount < linesIndentedWithSpacesCount;
        }

        int tabSize = defaultTabSize;

        if (insertSpaces)
        {
            double tabSizeScore = insertSpaces ? 0 : 0.1 * linesCount;

            foreach (int possibleTabSize in AllowedTabSizeGuesses)
            {
                int possibleScore = spacesDiffCount[possibleTabSize];
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

        string previous = _languageId;
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

        List<PendingEdit> pending = PreparePendingEdits(edits);
        if (pending.Count == 0)
        {
            return Array.Empty<TextChange>();
        }

        Range documentRangeBeforeEdit = GetDocumentRange();
        bool isFlushEdit = pending.Count == 1
            && pending[0].Edit.Start.Equals(documentRangeBeforeEdit.Start)
            && pending[0].Edit.End.Equals(documentRangeBeforeEdit.End);

        List<DecorationChange> decorationChanges = ApplyPendingEdits(pending, forceMoveMarkers);

        foreach (PendingEdit edit in pending)
        {
            edit.NewStartPosition = _buffer.GetPositionAt(edit.NewStartOffset);
            edit.NewEndPosition = _buffer.GetPositionAt(edit.NewEndOffset);
        }

        IncreaseVersionId();

        List<TextChange> changes = new(pending.Count);
        foreach (PendingEdit edit in pending)
        {
            changes.Add(new TextChange(edit.Edit.Start, edit.Edit.End, edit.NewText));
        }

        List<RecordedEdit> recordedEdits = new(pending.Count);
        foreach (PendingEdit edit in pending)
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

            EditStackElement element = _editStack.GetOrCreateElement(undoLabel ?? DefaultUndoLabel, beforeCursorState);
            element.AppendEdits(recordedEdits, _eol, _alternativeVersionId, afterCursorState);
        }

        return changes;
    }

    private List<PendingEdit> PreparePendingEdits(TextEdit[] edits)
    {
        List<PendingEdit> pending = new(edits.Length);
        foreach (TextEdit edit in edits)
        {
            int startOffset = _buffer.GetOffsetAt(edit.Start.LineNumber, edit.Start.Column);
            int endOffset = _buffer.GetOffsetAt(edit.End.LineNumber, edit.End.Column);
            if (endOffset < startOffset)
            {
                (startOffset, endOffset) = (endOffset, startOffset);
            }

            string newText = edit.Text ?? string.Empty;
            if (startOffset == endOffset && newText.Length == 0)
            {
                continue;
            }

            string oldText = _buffer.GetText(startOffset, endOffset - startOffset);
            pending.Add(new PendingEdit(edit, startOffset, endOffset, oldText));
        }

        pending.Sort((a, b) => a.OldStartOffset.CompareTo(b.OldStartOffset));

        int delta = 0;
        foreach (PendingEdit edit in pending)
        {
            edit.NewStartOffset = edit.OldStartOffset + delta;
            edit.NewEndOffset = edit.NewStartOffset + edit.NewText.Length;
            delta += edit.NewText.Length - (edit.OldEndOffset - edit.OldStartOffset);
        }

        return pending;
    }

    private List<DecorationChange> ApplyPendingEdits(List<PendingEdit> pending, bool forceMoveMarkers)
    {
        List<PendingEdit> applyOrder = new(pending);
        applyOrder.Sort((a, b) =>
        {
            int cmp = b.OldStartOffset.CompareTo(a.OldStartOffset);
            if (cmp != 0)
            {
                return cmp;
            }

            return b.OldEndOffset.CompareTo(a.OldEndOffset);
        });

        List<DecorationChange> decorationChanges = [];
        foreach (PendingEdit edit in applyOrder)
        {
            int removedLength = edit.OldEndOffset - edit.OldStartOffset;
            IReadOnlyList<DecorationChange> deltas = _decorationTrees.AcceptReplace(edit.OldStartOffset, removedLength, edit.NewText.Length, forceMoveMarkers);
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
                TextEdit[] replay = new TextEdit[element.Edits.Count];
                for (int i = 0; i < element.Edits.Count; i++)
                {
                    RecordedEdit recorded = element.Edits[i];
                    replay[i] = isUndo
                        ? new TextEdit(recorded.NewStart, recorded.NewEnd, recorded.OldText)
                        : new TextEdit(recorded.OldStart, recorded.OldEnd, recorded.NewText);
                }

                ApplyEditsInternal(replay, recordInUndoStack: false, isUndo: isUndo, isRedo: !isUndo);
            }

            string targetEol = isUndo ? element.BeforeEol : element.AfterEol;
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

        TextPosition oldEnd = GetDocumentEndPosition();
        _buffer.SetEol(newEol);
        _eol = newEol;
        IncreaseVersionId();

        TextChange change = new(new TextPosition(1, 1), oldEnd, _buffer.GetText());
        OnDidChangeContent?.Invoke(this, new TextModelContentChangedEventArgs(new[] { change }, _versionId, isUndo, isRedo, false));

        if (recordStackDelta)
        {
            EditStackElement element = _editStack.GetOrCreateElement(null, null);
            element.RecordEolChange(newEol, _alternativeVersionId);
        }

        _options = _options.WithDefaultEol(newEol == "\r\n" ? DefaultEndOfLine.CRLF : DefaultEndOfLine.LF);
        _creationOptions = _options.CreationOptions;
    }

    private TextPosition GetDocumentEndPosition()
    {
        int lineCount = GetLineCount();
        string lastLine = GetLineContent(lineCount);
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
        int lineCount = GetLineCount();
        TextPosition end = new(lineCount, GetLineMaxColumn(lineCount));
        return new Range(new TextPosition(1, 1), end);
    }

    private string GetValueInRangeInternal(Range range, bool normalizeLineEndings, out LineFeedCounter? lineFeedCounter)
    {
        int startOffset = GetOffsetAt(range.Start);
        int endOffset = GetOffsetAt(range.End);
        if (endOffset < startOffset)
        {
            (startOffset, endOffset) = (endOffset, startOffset);
        }

        int length = Math.Max(0, endOffset - startOffset);
        string value = _buffer.GetText(startOffset, length);
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

        string normalized = NormalizeToLf(value);
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

        StringBuilder builder = new(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
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
            int temp = right;
            right = left % right;
            left = temp;
        }

        return left == 0 ? 1 : left;
    }

    private ModelDecoration CreateDecoration(TextRange range, ModelDecorationOptions options, int ownerId)
    {
        ModelDecoration decoration = new(Guid.NewGuid().ToString(), ownerId, range, options);
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
        if (!_decorationIdsByOwner.TryGetValue(decoration.OwnerId, out HashSet<string>? ids))
        {
            ids = new HashSet<string>(StringComparer.Ordinal);
            _decorationIdsByOwner[decoration.OwnerId] = ids;
        }

        ids.Add(decoration.Id);
    }

    private void UntrackDecoration(ModelDecoration decoration)
    {
        if (_decorationIdsByOwner.TryGetValue(decoration.OwnerId, out HashSet<string>? ids))
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

        TextModelDecorationsChangedEventArgs args = BuildDecorationsChangedEventArgs(changes);
        OnDidChangeDecorations?.Invoke(this, args);
    }

    private TextModelDecorationsChangedEventArgs BuildDecorationsChangedEventArgs(IReadOnlyList<DecorationChange> changes)
    {
        bool affectsMinimap = false;
        bool affectsOverviewRuler = false;
        bool affectsGlyphMargin = false;
        bool affectsLineNumber = false;
        SortedSet<int> injectedLines = [];
        HashSet<LineHeightChange> lineHeightChanges = [];
        HashSet<LineFontChange> fontLineChanges = [];

        foreach (DecorationChange change in changes)
        {
            ModelDecorationOptions options = change.Options;
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

        int startOffset = range.StartOffset;
        int endOffset = range.EndOffset;
        int startLine = GetPositionAt(startOffset).LineNumber;
        int endLine = GetPositionAt(endOffset).LineNumber;
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
