/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// TypeScript source reference:
// File: ts/src/vs/editor/contrib/find/browser/findModel.ts
// Lines: 1-600 (FindModelBoundToEditorModel class)

using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;
using Selection = PieceTree.TextBuffer.Core.Selection;

namespace PieceTree.TextBuffer.DocUI;

/// <summary>
/// FindModel bound to a TextModel.
/// Manages find/replace operations and coordinates with FindReplaceState and FindDecorations.
/// </summary>
public class FindModel : IDisposable
{
    private const int MatchesLimit = 19999;
    private const int LargeReplaceMatchThreshold = 1000;

    private readonly TextModel _model;
    private readonly FindReplaceState _state;
    private readonly FindDecorations _decorations;
    private readonly Func<string?> _wordSeparatorsProvider;
    private readonly EventHandler<TextModelContentChangedEventArgs> _modelContentChangedHandler;
    private bool _isDisposed;
    private Range _currentSelection;
    private bool _needsResearch;
    private TextPosition _globalStartPosition;
    private TextPosition? _startPositionBeforeSelectionScope;
    // TS Parity: _ignoreModelContentChanged flag prevents double research after Replace/ReplaceAll
    private bool _ignoreModelContentChanged;
    private bool _hasPendingSearchScopeOverride;
    private Range[]? _pendingSearchScopeOverride;

    public FindModel(
        TextModel model,
        FindReplaceState state,
        Func<string?>? wordSeparatorsProvider = null,
        Func<double?>? viewportHeightProvider = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _decorations = new FindDecorations(model, viewportHeightProvider);
        _wordSeparatorsProvider = wordSeparatorsProvider ?? (() => null);
        _currentSelection = new Range(new TextPosition(1, 1), new TextPosition(1, 1));
        _modelContentChangedHandler = OnModelContentChanged;
        _globalStartPosition = _decorations.GetStartPosition();
        _startPositionBeforeSelectionScope = null;

        _state.OnFindReplaceStateChange += OnStateChanged;
        _model.OnDidChangeContent += _modelContentChangedHandler;

        Research(moveCursor: false);
    }

    public void SetSelection(Range selection)
    {
        SetSelections(new[] { selection }, primaryIndex: 0);
    }

    public void SetSelections(IReadOnlyList<Range> selections, int? primaryIndex = null)
    {
        if (_isDisposed)
        {
            return;
        }

        Range[] sanitized;
        if (selections == null || selections.Count == 0)
        {
            sanitized = [CloneRange(_currentSelection)];
            primaryIndex = 0;
        }
        else
        {
            sanitized = new Range[selections.Count];
            for (int i = 0; i < selections.Count; i++)
            {
                sanitized[i] = CloneRange(selections[i]);
            }
        }

        int normalizedPrimary = primaryIndex ?? 0;
        if (normalizedPrimary < 0)
        {
            normalizedPrimary = 0;
        }
        if (normalizedPrimary >= sanitized.Length)
        {
            normalizedPrimary = sanitized.Length - 1;
        }

        _currentSelection = sanitized[normalizedPrimary];
        UpdateStartPosition(_currentSelection.Start, _state.SearchScope == null);
        _state.ChangeMatchInfo(_state.MatchesPosition, _state.MatchesCount, clearCurrentMatch: true);
    }

    private void UpdateStartPosition(TextPosition position, bool updateGlobal)
    {
        _decorations.SetStartPosition(position);
        if (updateGlobal)
        {
            _globalStartPosition = position;
        }
    }

    private void HandleSearchScopeTransition()
    {
        if (_state.SearchScope != null)
        {
            _startPositionBeforeSelectionScope ??= _globalStartPosition;
            return;
        }

        if (_startPositionBeforeSelectionScope.HasValue)
        {
            UpdateStartPosition(_startPositionBeforeSelectionScope.Value, updateGlobal: true);
            _startPositionBeforeSelectionScope = null;
        }
        else
        {
            UpdateStartPosition(_currentSelection.Start, updateGlobal: true);
        }
    }

    public Range GetSelection()
    {
        return GetSelectionRange();
    }

    private void OnStateChanged(object? sender, FindReplaceStateChangedEventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }

        if (e.SearchString || e.IsRegex || e.WholeWord || e.MatchCase || e.SearchScope)
        {
            if (e.SearchScope)
            {
                HandleSearchScopeTransition();
                SetPendingSearchScopeOverride(_state.SearchScope);
            }
            Research(e.MoveCursor);
        }
    }

    private void OnModelContentChanged(object? sender, TextModelContentChangedEventArgs e)
    {
        if (_isDisposed)
        {
            return;
        }

        if (_ignoreModelContentChanged)
        {
            // Skip heavy recomputation – Replace / ReplaceAll already invoked Research.
            _ignoreModelContentChanged = false;
            return;
        }

        Range selection = GetSelectionRange();
        UpdateStartPosition(selection.Start, _state.SearchScope == null);

        if (e.IsFlush)
        {
            _decorations.Reset();
            _state.ChangeMatchInfo(0, 0, clearCurrentMatch: true);
            _needsResearch = true;
            return;
        }
        Research(moveCursor: false);
    }

    private void Research(bool moveCursor)
    {
        _needsResearch = false;
        Range[]? findScopes = ResolveFindScopes();
        IReadOnlyList<FindMatch> findMatches = FindMatches(findScopes, captureMatches: false, limitResultCount: MatchesLimit);

        _decorations.Set(findMatches.ToArray(), findScopes);

        int matchesPosition = ComputeMatchesPosition(GetSelectionRange(), findMatches);
        _state.ChangeMatchInfo(matchesPosition, _decorations.GetCount(), clearCurrentMatch: true);

        if (moveCursor && findMatches.Count > 0)
        {
            MoveToNextMatchFrom(_decorations.GetStartPosition());
        }
    }

    private void EnsureResearched()
    {
        if (_needsResearch)
        {
            Research(moveCursor: false);
        }
    }

    private int ClampToModel(int offset)
    {
        int length = _model.GetLength();
        if (offset < 0)
        {
            return 0;
        }

        if (offset > length)
        {
            return length;
        }

        return offset;
    }

    private Range GetSelectionRange()
    {
        return _state.CurrentMatch ?? _currentSelection;
    }

    private Range[]? ResolveFindScopes()
    {
        if (TryConsumePendingSearchScopeOverride(out Range[]? pendingOverride))
        {
            return NormalizeScopes(pendingOverride);
        }

        Range[]? hydratedScopes = CloneRanges(_decorations.GetFindScopes());
        if (hydratedScopes != null)
        {
            return NormalizeScopes(hydratedScopes);
        }

        Range[]? stateScopes = CloneRanges(_state.SearchScope);
        return NormalizeScopes(stateScopes);
    }

    private Range[]? GetActiveFindScopesForReplace()
    {
        Range[]? hydratedScopes = CloneRanges(_decorations.GetFindScopes());
        if (hydratedScopes != null)
        {
            return NormalizeScopes(hydratedScopes);
        }

        Range[]? stateScopes = CloneRanges(_state.SearchScope);
        return NormalizeScopes(stateScopes);
    }

    private void SetPendingSearchScopeOverride(Range[]? scopes)
    {
        _pendingSearchScopeOverride = CloneRanges(scopes);
        _hasPendingSearchScopeOverride = true;
    }

    private bool TryConsumePendingSearchScopeOverride(out Range[]? overrideValue)
    {
        if (!_hasPendingSearchScopeOverride)
        {
            overrideValue = null;
            return false;
        }

        _hasPendingSearchScopeOverride = false;
        overrideValue = _pendingSearchScopeOverride;
        _pendingSearchScopeOverride = null;
        return true;
    }

    private static Range[]? CloneRanges(Range[]? scopes)
    {
        if (scopes == null || scopes.Length == 0)
        {
            return null;
        }

        Range[] clone = new Range[scopes.Length];
        for (int i = 0; i < scopes.Length; i++)
        {
            clone[i] = CloneRange(scopes[i]);
        }
        return clone;
    }

    private static Range CloneRange(Range source)
    {
        return new Range(source.Start, source.End);
    }

    private Range[]? NormalizeScopes(Range[]? scopes)
    {
        if (scopes == null || scopes.Length == 0)
        {
            return null;
        }

        List<Range> normalized = new(scopes.Length);
        foreach (Range scope in scopes)
        {
            int startLine = scope.StartLineNumber;
            int endLine = scope.EndLineNumber;
            if (endLine < startLine)
            {
                continue;
            }

            if (startLine == endLine)
            {
                normalized.Add(scope);
                continue;
            }

            TextPosition normalizedStart = new(startLine, 1);
            int normalizedEndLine = scope.EndColumn == 1
                ? Math.Max(startLine, endLine - 1)
                : endLine;
            int normalizedEndColumn = _model.GetLineMaxColumn(normalizedEndLine);
            TextPosition normalizedEnd = new(normalizedEndLine, normalizedEndColumn);
            normalized.Add(new Range(normalizedStart, normalizedEnd));
        }

        return normalized.Count == 0 ? null : normalized.ToArray();
    }

    private IReadOnlyList<FindMatch> FindMatches(Range[]? findScopes, bool captureMatches, int limitResultCount)
    {
        if (string.IsNullOrEmpty(_state.SearchString))
        {
            return Array.Empty<FindMatch>();
        }

        SearchParams searchParams = CreateSearchParams();

        if (findScopes != null && findScopes.Length > 0)
        {
            return _model.FindMatches(
                searchParams,
                searchRanges: findScopes,
                findInSelection: true,
                captureMatches: captureMatches,
                limitResultCount: limitResultCount
            );
        }

        return _model.FindMatches(
            searchParams,
            searchRange: null,
            captureMatches: captureMatches,
            limitResultCount: limitResultCount
        );
    }

    public void FindNext()
    {
        EnsureResearched();
        MoveToNextMatchFrom(GetSelectionRange().End);
    }

    public void FindPrevious()
    {
        EnsureResearched();
        MoveToPrevMatchFrom(GetSelectionRange().Start);
    }

    private void MoveToNextMatchFrom(TextPosition position)
    {
        if (_decorations.GetCount() == 0)
        {
            return;
        }

        if (!_state.CanNavigateForward())
        {
            Range? previous = _decorations.MatchBeforePosition(position);
            if (previous != null)
            {
                SetCurrentFindMatch(previous.Value);
            }
            return;
        }

        if (_decorations.GetCount() < MatchesLimit)
        {
            Range? nextMatch = _decorations.MatchAfterPosition(position);
            if (nextMatch != null && nextMatch.Value.IsEmpty && PositionsEqual(nextMatch.Value.Start, position))
            {
                position = GetNextSearchPosition(position);
                nextMatch = _decorations.MatchAfterPosition(position);
            }

            if (nextMatch != null)
            {
                SetCurrentFindMatch(nextMatch.Value);
            }
            return;
        }

        FindMatch? next = GetNextMatchFromModel(position, forceMove: true);
        if (next != null)
        {
            SetCurrentFindMatch(next.Range);
        }
    }

    private void MoveToPrevMatchFrom(TextPosition position)
    {
        if (_decorations.GetCount() == 0)
        {
            return;
        }

        if (!_state.CanNavigateBack())
        {
            Range? next = _decorations.MatchAfterPosition(position);
            if (next != null)
            {
                SetCurrentFindMatch(next.Value);
            }
            return;
        }

        if (_decorations.GetCount() < MatchesLimit)
        {
            Range? prevMatch = _decorations.MatchBeforePosition(position);
            if (prevMatch != null && prevMatch.Value.IsEmpty && PositionsEqual(prevMatch.Value.End, position))
            {
                position = GetPrevSearchPosition(position);
                prevMatch = _decorations.MatchBeforePosition(position);
            }

            if (prevMatch != null)
            {
                SetCurrentFindMatch(prevMatch.Value);
            }
            return;
        }

        FindMatch? previous = GetPreviousMatchFromModel(position, forceMove: true);
        if (previous != null)
        {
            SetCurrentFindMatch(previous.Range);
        }
    }

    private TextPosition GetNextSearchPosition(TextPosition after)
    {
        bool isUsingLineStops = _state.IsRegex && (
            _state.SearchString.Contains('^') || _state.SearchString.Contains('$')
        );

        int lineNumber = after.LineNumber;
        int column = after.Column;

        if (isUsingLineStops || column == _model.GetLineMaxColumn(lineNumber))
        {
            lineNumber = lineNumber == _model.GetLineCount() ? 1 : lineNumber + 1;
            column = 1;
        }
        else
        {
            column++;
        }

        return new TextPosition(lineNumber, column);
    }

    private TextPosition GetPrevSearchPosition(TextPosition before)
    {
        bool isUsingLineStops = _state.IsRegex && (
            _state.SearchString.Contains('^') || _state.SearchString.Contains('$')
        );

        int lineNumber = before.LineNumber;
        int column = before.Column;

        if (isUsingLineStops || column == 1)
        {
            lineNumber = lineNumber == 1 ? _model.GetLineCount() : lineNumber - 1;
            column = _model.GetLineMaxColumn(lineNumber);
        }
        else
        {
            column--;
        }

        return new TextPosition(lineNumber, column);
    }

    private void SetCurrentFindMatch(Range match)
    {
        _currentSelection = match;
        // TS Parity: SetCurrentMatch returns matchPosition which must be synchronized to state
        // Reference: ts/src/vs/editor/contrib/find/browser/findModel.ts:_updateDecorations
        int matchPosition = _decorations.SetCurrentMatch(match);
        _state.ChangeMatchInfo(matchPosition, _decorations.GetCount(), match);
    }

    public void Replace()
    {
        EnsureResearched();
        if (_decorations.GetCount() == 0)
        {
            return;
        }

        Range selection = GetSelectionRange();
        Range? currentMatch = _decorations.GetCurrentMatchRange();

        if (currentMatch == null || !selection.Equals(currentMatch.Value))
        {
            MoveToNextMatchFrom(selection.Start);
            currentMatch = _decorations.GetCurrentMatchRange();
            if (currentMatch == null)
            {
                return;
            }

            if (!selection.Equals(currentMatch.Value))
            {
                _decorations.SetStartPosition(selection.Start);
                SetCurrentFindMatch(currentMatch.Value);
                return;
            }
        }

        Range matchRange = currentMatch.Value;
        int replaceStartOffset = _model.GetOffsetAt(matchRange.Start);
        string[]? matches = GetMatchesForReplace(matchRange);
        ReplacePattern replacePattern = GetReplacePattern();
        string replaceString = replacePattern.BuildReplaceString(matches, _state.PreserveCase);

        TextEdit edit = new(matchRange.Start, matchRange.End, replaceString);
        _ignoreModelContentChanged = true;
        _model.PushEditOperations([edit]);

        int caretOffset = replaceStartOffset + replaceString.Length;
        TextPosition caret = _model.GetPositionAt(caretOffset);
        SetSelection(new Range(caret, caret));
        Research(moveCursor: true);
    }

    public int ReplaceAll()
    {
        EnsureResearched();
        if (_decorations.GetCount() == 0)
        {
            return 0;
        }

        ReplacePattern replacePattern = GetReplacePattern();
        SelectionSnapshot selectionSnapshot = CaptureSelectionSnapshot();
        Range[]? findScopes = ResolveFindScopes();

        if (findScopes == null && _state.MatchesCount >= MatchesLimit)
        {
            int largeResult = _largeReplaceAll(replacePattern, selectionSnapshot);
            if (largeResult > 0)
            {
                Research(moveCursor: false);
            }
            return largeResult;
        }

        bool captureMatches = replacePattern.HasReplacementPatterns || _state.PreserveCase;
        IReadOnlyList<FindMatch> findMatches = FindMatches(findScopes, captureMatches, int.MaxValue);
        if (findMatches.Count == 0)
        {
            return 0;
        }

        int replaced;
        if (findScopes == null && findMatches.Count > LargeReplaceMatchThreshold)
        {
            replaced = _largeReplaceAll(replacePattern, selectionSnapshot);
        }
        else
        {
            replaced = _regularReplaceAll(findMatches, replacePattern, selectionSnapshot);
        }

        if (replaced > 0)
        {
            Research(moveCursor: false);
        }

        return replaced;
    }

    private int _regularReplaceAll(IReadOnlyList<FindMatch> matches, ReplacePattern replacePattern, SelectionSnapshot selectionSnapshot)
    {
        int selectionStartOffset = selectionSnapshot.StartOffset;
        int selectionEndOffset = selectionSnapshot.EndOffset;
        bool collapsed = selectionSnapshot.IsCollapsed;
        int newStartOffset = selectionStartOffset;
        int newEndOffset = selectionEndOffset;

        List<TextEdit> edits = new(matches.Count);
        foreach (FindMatch match in matches)
        {
            string replaceString = replacePattern.BuildReplaceString(match.Matches, _state.PreserveCase);
            edits.Add(new TextEdit(match.Range.Start, match.Range.End, replaceString));

            int matchStartOffset = _model.GetOffsetAt(match.Range.Start);
            int matchEndOffset = _model.GetOffsetAt(match.Range.End);
            int diff = replaceString.Length - (matchEndOffset - matchStartOffset);

            if (matchEndOffset <= selectionStartOffset)
            {
                newStartOffset += diff;
            }
            else if (selectionStartOffset >= matchStartOffset && selectionStartOffset <= matchEndOffset)
            {
                int relative = selectionStartOffset - matchStartOffset;
                newStartOffset = matchStartOffset + Math.Min(replaceString.Length, relative);
            }

            if (!collapsed)
            {
                if (matchEndOffset <= selectionEndOffset)
                {
                    newEndOffset += diff;
                }
                else if (selectionEndOffset >= matchStartOffset && selectionEndOffset <= matchEndOffset)
                {
                    int relativeEnd = selectionEndOffset - matchStartOffset;
                    newEndOffset = matchStartOffset + Math.Min(replaceString.Length, relativeEnd);
                }
            }
        }

        TextEdit[] orderedEdits = edits
            .OrderBy(edit => _model.GetOffsetAt(edit.Start))
            .ToArray();

        _ignoreModelContentChanged = true;
        _model.PushEditOperations(orderedEdits);

        if (collapsed)
        {
            newEndOffset = newStartOffset;
        }

        ApplySelectionOffsets(newStartOffset, newEndOffset);
        return matches.Count;
    }

    private FindMatch? GetNextMatchFromModel(TextPosition position, bool forceMove, bool captureMatches = false)
    {
        SearchParams searchParams = CreateSearchParams();
        Range[]? normalizedScopes = ResolveFindScopes();
        ReadOnlyCollection<Range>? readOnlyScopes = normalizedScopes != null && normalizedScopes.Length > 0
            ? Array.AsReadOnly(normalizedScopes)
            : null;
        Range searchRange = GetSearchRange(normalizedScopes);
        return GetNextMatchFromModel(searchParams, readOnlyScopes, searchRange, position, captureMatches, forceMove, false);
    }

    private FindMatch? GetNextMatchFromModel(SearchParams searchParams, IReadOnlyList<Range>? findScopes, Range searchRange, TextPosition position, bool captureMatches, bool forceMove, bool isRecursed)
    {
        position = ClampToSearchRangeForNext(position, searchRange);
        FindMatch? nextMatch = FindNextMatch(searchParams, position, findScopes, captureMatches);

        if (forceMove && nextMatch != null && nextMatch.Range.IsEmpty && PositionsEqual(nextMatch.Range.Start, position))
        {
            TextPosition adjusted = GetNextSearchPosition(position);
            return GetNextMatchFromModel(searchParams, findScopes, searchRange, adjusted, captureMatches, false, true);
        }

        if (nextMatch == null)
        {
            return null;
        }

        if (!isRecursed && !RangeContains(searchRange, nextMatch.Range))
        {
            return GetNextMatchFromModel(searchParams, findScopes, searchRange, nextMatch.Range.End, captureMatches, forceMove, true);
        }

        return nextMatch;
    }

    private FindMatch? GetPreviousMatchFromModel(TextPosition position, bool forceMove, bool captureMatches = false)
    {
        SearchParams searchParams = CreateSearchParams();
        Range[]? normalizedScopes = ResolveFindScopes();
        ReadOnlyCollection<Range>? readOnlyScopes = normalizedScopes != null && normalizedScopes.Length > 0
            ? Array.AsReadOnly(normalizedScopes)
            : null;
        Range searchRange = GetSearchRange(normalizedScopes);
        return GetPreviousMatchFromModel(searchParams, readOnlyScopes, searchRange, position, captureMatches, forceMove, false);
    }

    private FindMatch? GetPreviousMatchFromModel(SearchParams searchParams, IReadOnlyList<Range>? findScopes, Range searchRange, TextPosition position, bool captureMatches, bool forceMove, bool isRecursed)
    {
        position = ClampToSearchRangeForPrev(position, searchRange);
        FindMatch? prevMatch = FindPreviousMatch(searchParams, position, findScopes, captureMatches);

        if (forceMove && prevMatch != null && prevMatch.Range.IsEmpty && PositionsEqual(prevMatch.Range.End, position))
        {
            TextPosition adjusted = GetPrevSearchPosition(position);
            return GetPreviousMatchFromModel(searchParams, findScopes, searchRange, adjusted, captureMatches, false, true);
        }

        if (prevMatch == null)
        {
            return null;
        }

        if (!isRecursed && !RangeContains(searchRange, prevMatch.Range))
        {
            return GetPreviousMatchFromModel(searchParams, findScopes, searchRange, prevMatch.Range.Start, captureMatches, forceMove, true);
        }

        return prevMatch;
    }

    private FindMatch? FindNextMatch(SearchParams searchParams, TextPosition position, IReadOnlyList<Range>? findScopes, bool captureMatches)
    {
        if (findScopes != null)
        {
            return _model.FindNextMatch(searchParams, position, findScopes, findInSelection: true, captureMatches: captureMatches);
        }

        return _model.FindNextMatch(searchParams, position, captureMatches);
    }

    private FindMatch? FindPreviousMatch(SearchParams searchParams, TextPosition position, IReadOnlyList<Range>? findScopes, bool captureMatches)
    {
        if (findScopes != null)
        {
            return _model.FindPreviousMatch(searchParams, position, findScopes, findInSelection: true, captureMatches: captureMatches);
        }

        return _model.FindPreviousMatch(searchParams, position, captureMatches);
    }

    private Range GetSearchRange(Range[]? findScopes)
    {
        if (findScopes != null && findScopes.Length > 0)
        {
            return findScopes[0];
        }

        return GetFullModelRange();
    }

    private static TextPosition ClampToSearchRangeForNext(TextPosition position, Range searchRange)
    {
        if (ComparePositions(position, searchRange.End) > 0)
        {
            return searchRange.Start;
        }

        if (ComparePositions(position, searchRange.Start) < 0)
        {
            return searchRange.Start;
        }

        return position;
    }

    private static TextPosition ClampToSearchRangeForPrev(TextPosition position, Range searchRange)
    {
        if (ComparePositions(position, searchRange.End) > 0)
        {
            return searchRange.End;
        }

        if (ComparePositions(position, searchRange.Start) < 0)
        {
            return searchRange.End;
        }

        return position;
    }

    private static bool RangeContains(Range container, Range candidate)
    {
        return ComparePositions(candidate.Start, container.Start) >= 0
            && ComparePositions(candidate.End, container.End) <= 0;
    }

    private int _largeReplaceAll(ReplacePattern replacePattern, SelectionSnapshot selectionSnapshot)
    {
        SearchParams searchParams = CreateSearchParams();
        SearchData? searchData = searchParams.ParseSearchRequest();
        if (searchData == null)
        {
            return 0;
        }

        Regex regex = EnsureMultilineRegex(searchData.Regex);
        Range fullRange = GetFullModelRange();
        string buffer = _model.GetValueInRange(fullRange, EndOfLinePreference.LF);
        bool preserveCase = _state.PreserveCase;
        bool hasDynamicReplacement = replacePattern.HasReplacementPatterns || preserveCase;
        string? staticReplacement = null;
        if (!hasDynamicReplacement)
        {
            staticReplacement = replacePattern.BuildReplaceString(null, preserveCase);
        }

        int replacements = 0;
        string updated = regex.Replace(buffer, match =>
        {
            replacements++;
            if (hasDynamicReplacement)
            {
                return replacePattern.BuildReplaceString(ExtractMatches(match), preserveCase);
            }

            return staticReplacement!;
        });

        if (replacements == 0)
        {
            return 0;
        }

        _ignoreModelContentChanged = true;
        _model.PushEditOperations([new TextEdit(fullRange.Start, fullRange.End, updated)]);

        int endOffset = selectionSnapshot.IsCollapsed ? selectionSnapshot.StartOffset : selectionSnapshot.EndOffset;
        ApplySelectionOffsets(selectionSnapshot.StartOffset, endOffset);
        return replacements;
    }

    private SelectionSnapshot CaptureSelectionSnapshot()
    {
        Range selection = GetSelectionRange();
        int startOffset = _model.GetOffsetAt(selection.Start);
        int endOffset = _model.GetOffsetAt(selection.End);
        return new SelectionSnapshot(startOffset, endOffset);
    }

    private void ApplySelectionOffsets(int startOffset, int endOffset)
    {
        int orderedStart = Math.Min(startOffset, endOffset);
        int orderedEnd = Math.Max(startOffset, endOffset);
        int clampedStart = ClampToModel(orderedStart);
        int clampedEnd = ClampToModel(orderedEnd);
        SetSelection(new Range(_model.GetPositionAt(clampedStart), _model.GetPositionAt(clampedEnd)));
    }

    private Range GetFullModelRange()
    {
        int lastLine = _model.GetLineCount();
        return new Range(new TextPosition(1, 1), new TextPosition(lastLine, _model.GetLineMaxColumn(lastLine)));
    }

    private SearchParams CreateSearchParams()
    {
        return _state.CreateSearchParams(_wordSeparatorsProvider());
    }

    private static Regex EnsureMultilineRegex(Regex regex)
    {
        return regex.Options.HasFlag(RegexOptions.Multiline)
            ? regex
            : new Regex(regex.ToString(), regex.Options | RegexOptions.Multiline);
    }

    private static string[] ExtractMatches(Match match)
    {
        GroupCollection groups = match.Groups;
        string[] values = new string[groups.Count];
        for (int i = 0; i < groups.Count; i++)
        {
            values[i] = groups[i].Value;
        }
        return values;
    }

    private readonly record struct SelectionSnapshot(int StartOffset, int EndOffset)
    {
        public bool IsCollapsed => StartOffset == EndOffset;
    }

    public Selection[] SelectAllMatches()
    {
        EnsureResearched();

        if (_decorations.GetCount() == 0)
        {
            return Array.Empty<Selection>();
        }

        Range[]? findScopes = ResolveFindScopes();
        IReadOnlyList<FindMatch> matches = FindMatches(findScopes, captureMatches: false, int.MaxValue);
        if (matches.Count == 0)
        {
            return Array.Empty<Selection>();
        }

        SelectionInfo[] selections = new SelectionInfo[matches.Count];
        for (int i = 0; i < matches.Count; i++)
        {
            Range matchRange = matches[i].Range;
            selections[i] = new SelectionInfo(matchRange, new Selection(matchRange.Start, matchRange.End));
        }

        Array.Sort(selections, (left, right) => CompareRanges(left.Range, right.Range));

        Range primaryRange = GetSelectionRange();
        int primaryIndex = -1;
        for (int i = 0; i < selections.Length; i++)
        {
            if (selections[i].Range.Equals(primaryRange))
            {
                primaryIndex = i;
                break;
            }
        }

        Selection[] result = new Selection[selections.Length];
        if (primaryIndex >= 0)
        {
            result[0] = selections[primaryIndex].Selection;
            int writeIndex = 1;
            for (int i = 0; i < selections.Length; i++)
            {
                if (i == primaryIndex)
                {
                    continue;
                }
                result[writeIndex++] = selections[i].Selection;
            }
        }
        else
        {
            for (int i = 0; i < selections.Length; i++)
            {
                result[i] = selections[i].Selection;
            }
        }

        return result;
    }

    private ReplacePattern GetReplacePattern()
    {
        if (_state.IsRegex)
        {
            return ReplacePatternParser.ParseReplaceString(_state.ReplaceString);
        }

        return ReplacePattern.FromStaticValue(_state.ReplaceString);
    }

    private string[]? GetMatchesForReplace(Range range)
    {
        if (!_state.IsRegex)
        {
            return null;
        }

        SearchParams searchParams = CreateSearchParams();
        Range[]? activeScopes = GetActiveFindScopesForReplace();
        FindMatch? match;
        if (activeScopes != null && activeScopes.Length > 0)
        {
            match = _model.FindNextMatch(searchParams, range.Start, activeScopes, findInSelection: true, captureMatches: true);
        }
        else
        {
            match = _model.FindNextMatch(searchParams, range.Start, captureMatches: true);
        }

        if (match != null && match.Range.Equals(range))
        {
            return match.Matches;
        }

        return null;
    }

    private int ComputeMatchesPosition(Range selection, IReadOnlyList<FindMatch> matches)
    {
        if (matches.Count == 0)
        {
            return 0;
        }

        int position = _decorations.GetCurrentMatchesPosition(selection);
        if (position != 0)
        {
            return position;
        }

        int matchAfterIndex = -1;
        for (int i = 0; i < matches.Count; i++)
        {
            Range matchRange = matches[i].Range;
            if (ComparePositions(matchRange.Start, selection.Start) >= 0)
            {
                matchAfterIndex = i;
                break;
            }
        }

        if (matchAfterIndex == -1)
        {
            matchAfterIndex = matches.Count;
        }

        if (matchAfterIndex > 0)
        {
            return matchAfterIndex;
        }

        return 0;
    }

    private static int CompareRanges(Range left, Range right)
    {
        int startComparison = ComparePositions(left.Start, right.Start);
        if (startComparison != 0)
        {
            return startComparison;
        }

        return ComparePositions(left.End, right.End);
    }

    private static int ComparePositions(TextPosition left, TextPosition right)
    {
        if (left.LineNumber != right.LineNumber)
        {
            return left.LineNumber.CompareTo(right.LineNumber);
        }

        return left.Column.CompareTo(right.Column);
    }

    private static bool PositionsEqual(TextPosition left, TextPosition right)
    {
        return left.LineNumber == right.LineNumber && left.Column == right.Column;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _state.OnFindReplaceStateChange -= OnStateChanged;
        _model.OnDidChangeContent -= _modelContentChangedHandler;
        _decorations.Dispose();
    }

    private readonly record struct SelectionInfo(Range Range, Selection Selection);
}
