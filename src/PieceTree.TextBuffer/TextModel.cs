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
    private readonly IntervalTree _decorations = new();
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

    public string GetValue() => _buffer.GetText();

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
        _decorations.Insert(decoration);
        TrackDecoration(decoration);
        RaiseDecorationsChanged(new[] { new DecorationChange(decoration, DecorationDeltaKind.Added) });
        return decoration;
    }

    public IReadOnlyList<ModelDecoration> GetDecorationsInRange(TextRange range, int ownerIdFilter = DecorationOwnerIds.Any)
        => _decorations.Search(range, ownerIdFilter);

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

                if (_decorations.TryGet(id, out var existing) && existing.OwnerId == ownerId)
                {
                    _decorations.Remove(id);
                    UntrackDecoration(existing);
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
                _decorations.Insert(decoration);
                TrackDecoration(decoration);
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
        string? undoLabel = null)
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
            cursorStateComputer);
    }

    public void ApplyEdits(TextEdit[] edits)
    {
        PushEditOperations(edits ?? Array.Empty<TextEdit>());
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

    public void DetectIndentation(bool defaultInsertSpaces, int defaultTabSize)
    {
        var maxLines = Math.Min(GetLineCount(), 200);
        var spaceSamples = new List<int>();
        var spaceIndentedLines = 0;
        var tabIndentedLines = 0;

        for (int line = 1; line <= maxLines; line++)
        {
            var content = GetLineContent(line);
            if (content.Length == 0)
            {
                continue;
            }

            int index = 0;
            while (index < content.Length && content[index] == ' ')
            {
                index++;
            }

            if (index > 0)
            {
                spaceIndentedLines++;
                spaceSamples.Add(index);
                continue;
            }

            if (index < content.Length && content[index] == '\t')
            {
                tabIndentedLines++;
            }
        }

        var useTabs = tabIndentedLines > spaceIndentedLines && tabIndentedLines > 0;
        var insertSpaces = useTabs ? false : (spaceIndentedLines > 0 || defaultInsertSpaces);
        var indentSize = defaultTabSize;

        if (spaceSamples.Count > 0)
        {
            indentSize = spaceSamples[0];
            for (int i = 1; i < spaceSamples.Count; i++)
            {
                indentSize = GreatestCommonDivisor(indentSize, spaceSamples[i]);
            }

            if (indentSize <= 0)
            {
                indentSize = defaultTabSize;
            }
        }

        UpdateOptions(new TextModelUpdateOptions
        {
            InsertSpaces = insertSpaces,
            TabSize = indentSize,
            IndentSize = indentSize,
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
        CursorStateComputer? cursorStateComputer = null)
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

        var decorationChanges = ApplyPendingEdits(pending);

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

        OnDidChangeContent?.Invoke(this, new TextModelContentChangedEventArgs(changes, _versionId, isUndo, isRedo, false));

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

    private List<DecorationChange> ApplyPendingEdits(List<PendingEdit> pending)
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
            var deltas = AdjustDecorationsForEdit(edit.OldStartOffset, removedLength, edit.NewText.Length);
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

        OnDidChangeDecorations?.Invoke(this, new TextModelDecorationsChangedEventArgs(changes, _versionId));
    }

    private IReadOnlyList<DecorationChange> AdjustDecorationsForEdit(int offset, int removedLength, int insertedLength)
    {
        if (_decorations.Count == 0 || (removedLength == 0 && insertedLength == 0))
        {
            return Array.Empty<DecorationChange>();
        }

        var processed = new HashSet<string>(StringComparer.Ordinal);
        var changes = new List<DecorationChange>();
        var searchEnd = removedLength > 0 ? offset + removedLength : offset + insertedLength;
        var overlaps = _decorations.Search(new TextRange(Math.Max(0, offset - 1), Math.Max(offset, searchEnd + 1)));
        foreach (var decoration in overlaps)
        {
            if (!processed.Add(decoration.Id))
            {
                continue;
            }

            if (UpdateDecorationRange(decoration, offset, removedLength, insertedLength))
            {
                changes.Add(new DecorationChange(decoration, DecorationDeltaKind.Updated));
            }
        }

        foreach (var decoration in _decorations.EnumerateFrom(offset))
        {
            if (!processed.Add(decoration.Id))
            {
                continue;
            }

            if (UpdateDecorationRange(decoration, offset, removedLength, insertedLength))
            {
                changes.Add(new DecorationChange(decoration, DecorationDeltaKind.Updated));
            }
        }

        return changes;
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

        private bool UpdateDecorationRange(ModelDecoration decoration, int offset, int removedLength, int insertedLength)
        {
            var range = decoration.Range;
            var start = range.StartOffset;
            var end = range.EndOffset;
            var originalStart = start;
            var originalEnd = end;
            var deleteEnd = offset + removedLength;
            var collapsedByReplace = false;

            if (removedLength > 0)
            {
                if (decoration.Options.CollapseOnReplaceEdit && start >= offset && end <= deleteEnd)
                {
                    start = offset;
                    end = offset;
                    collapsedByReplace = true;
                }
            else
            {
                if (start >= deleteEnd)
                {
                    start -= removedLength;
                }
                else if (start >= offset)
                {
                    start = offset;
                }

                if (end >= deleteEnd)
                {
                    end -= removedLength;
                }
                else if (end >= offset)
                {
                    end = offset;
                }
            }
        }

            if (insertedLength > 0)
        {
            var growStart = !decoration.Options.ForceMoveMarkers &&
                (decoration.Options.Stickiness == TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges ||
                 decoration.Options.Stickiness == TrackedRangeStickiness.GrowsOnlyWhenTypingBefore);

            var growEnd = !decoration.Options.ForceMoveMarkers &&
                (decoration.Options.Stickiness == TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges ||
                 decoration.Options.Stickiness == TrackedRangeStickiness.GrowsOnlyWhenTypingAfter);

            if (offset < start)
            {
                start += insertedLength;
            }
                else if (offset == start && !growStart && !collapsedByReplace)
            {
                start += insertedLength;
            }

            if (offset < end)
            {
                end += insertedLength;
            }
                else if (offset == end && growEnd && !collapsedByReplace)
            {
                end += insertedLength;
            }
        }

        if (start < 0)
        {
            start = 0;
        }

        if (end < start)
        {
            end = start;
        }

        if (start == originalStart && end == originalEnd)
        {
            return false;
        }

        decoration.Range = new TextRange(start, end);
        decoration.VersionId = _versionId;
        _decorations.Reinsert(decoration);
        return true;
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
