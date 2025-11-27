/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// TypeScript source reference:
// File: ts/src/vs/editor/contrib/find/browser/findState.ts
// Lines: 1-340 (FindReplaceState class and related types)

using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.DocUI;

/// <summary>
/// Event arguments for find/replace state changes.
/// </summary>
public class FindReplaceStateChangedEventArgs : EventArgs
{
    public bool MoveCursor { get; set; }
    public bool UpdateHistory { get; set; }
    public bool SearchString { get; set; }
    public bool ReplaceString { get; set; }
    public bool IsRevealed { get; set; }
    public bool IsReplaceRevealed { get; set; }
    public bool IsRegex { get; set; }
    public bool WholeWord { get; set; }
    public bool MatchCase { get; set; }
    public bool PreserveCase { get; set; }
    public bool SearchScope { get; set; }
    public bool MatchesPosition { get; set; }
    public bool MatchesCount { get; set; }
    public bool CurrentMatch { get; set; }
    public bool Loop { get; set; }
    public bool IsSearching { get; set; }
}

/// <summary>
/// Manages the state of find/replace operations in the editor.
/// Tracks search parameters, match counts, and current match position.
/// </summary>
public class FindReplaceState : IDisposable
{
    private string _searchString;
    private string _replaceString;
    private bool _isRevealed;
    private bool _isReplaceRevealed;
    private bool _isRegex;
    private bool _wholeWord;
    private bool _matchCase;
    private bool _preserveCase;
    private Range[]? _searchScope;
    private int _matchesPosition;
    private int _matchesCount;
    private Range? _currentMatch;
    private bool _loop;
    private bool _isSearching;

    // Event for state changes
    public event EventHandler<FindReplaceStateChangedEventArgs>? OnFindReplaceStateChange;

    // Properties
    public string SearchString
    {
        get => _searchString;
        set => _searchString = value;
    }

    public string ReplaceString
    {
        get => _replaceString;
        set => _replaceString = value;
    }

    public bool IsRevealed
    {
        get => _isRevealed;
        set => _isRevealed = value;
    }

    public bool IsReplaceRevealed
    {
        get => _isReplaceRevealed;
        set => _isReplaceRevealed = value;
    }

    public bool IsRegex
    {
        get => _isRegex;
        set => _isRegex = value;
    }

    public bool WholeWord
    {
        get => _wholeWord;
        set => _wholeWord = value;
    }

    public bool MatchCase
    {
        get => _matchCase;
        set => _matchCase = value;
    }

    public bool PreserveCase
    {
        get => _preserveCase;
        set => _preserveCase = value;
    }

    public Range[]? SearchScope
    {
        get => _searchScope;
        set => _searchScope = value;
    }

    public int MatchesPosition
    {
        get => _matchesPosition;
        private set => _matchesPosition = value;
    }

    public int MatchesCount
    {
        get => _matchesCount;
        private set => _matchesCount = value;
    }

    public Range? CurrentMatch
    {
        get => _currentMatch;
        private set => _currentMatch = value;
    }

    public bool Loop
    {
        get => _loop;
        set => _loop = value;
    }

    public bool IsSearching
    {
        get => _isSearching;
        set => _isSearching = value;
    }

    // Constructor
    public FindReplaceState()
    {
        _searchString = string.Empty;
        _replaceString = string.Empty;
        _isRevealed = false;
        _isReplaceRevealed = false;
        _isRegex = false;
        _wholeWord = false;
        _matchCase = false;
        _preserveCase = false;
        _searchScope = null;
        _matchesPosition = 0;
        _matchesCount = 0;
        _currentMatch = null;
        _loop = true;
        _isSearching = false;
    }

    /// <summary>
    /// Updates match information (position, count, current match).
    /// </summary>
    public void ChangeMatchInfo(int matchesPosition, int matchesCount, Range? currentMatch = null, bool clearCurrentMatch = false)
    {
        FindReplaceStateChangedEventArgs changeEvent = new();
        bool somethingChanged = false;

        // Normalize match position
        if (matchesCount == 0)
        {
            matchesPosition = 0;
        }
        if (matchesPosition > matchesCount)
        {
            matchesPosition = matchesCount;
        }

        if (_matchesPosition != matchesPosition)
        {
            _matchesPosition = matchesPosition;
            changeEvent.MatchesPosition = true;
            somethingChanged = true;
        }

        if (_matchesCount != matchesCount)
        {
            _matchesCount = matchesCount;
            changeEvent.MatchesCount = true;
            somethingChanged = true;
        }

        if (clearCurrentMatch)
        {
            if (_currentMatch != null)
            {
                _currentMatch = null;
                changeEvent.CurrentMatch = true;
                somethingChanged = true;
            }
        }
        else if (currentMatch != null)
        {
            if (_currentMatch != currentMatch)
            {
                _currentMatch = currentMatch;
                changeEvent.CurrentMatch = true;
                somethingChanged = true;
            }
        }

        if (somethingChanged)
        {
            OnFindReplaceStateChange?.Invoke(this, changeEvent);
        }
    }

    /// <summary>
    /// Changes one or more state properties and fires change event.
    /// </summary>
    public void Change(
        string? searchString = null,
        string? replaceString = null,
        bool? isRevealed = null,
        bool? isReplaceRevealed = null,
        bool? isRegex = null,
        bool? wholeWord = null,
        bool? matchCase = null,
        bool? preserveCase = null,
        Range[]? searchScope = null,
        bool searchScopeProvided = false,
        bool? loop = null,
        bool? isSearching = null,
        bool moveCursor = false,
        bool updateHistory = true)
    {
        FindReplaceStateChangedEventArgs changeEvent = new()
        {
            MoveCursor = moveCursor,
            UpdateHistory = updateHistory
        };
        bool somethingChanged = false;

        if (searchString != null && _searchString != searchString)
        {
            _searchString = searchString;
            changeEvent.SearchString = true;
            somethingChanged = true;
        }

        if (replaceString != null && _replaceString != replaceString)
        {
            _replaceString = replaceString;
            changeEvent.ReplaceString = true;
            somethingChanged = true;
        }

        if (isRevealed.HasValue && _isRevealed != isRevealed.Value)
        {
            _isRevealed = isRevealed.Value;
            changeEvent.IsRevealed = true;
            somethingChanged = true;
        }

        if (isReplaceRevealed.HasValue && _isReplaceRevealed != isReplaceRevealed.Value)
        {
            _isReplaceRevealed = isReplaceRevealed.Value;
            changeEvent.IsReplaceRevealed = true;
            somethingChanged = true;
        }

        if (isRegex.HasValue && _isRegex != isRegex.Value)
        {
            _isRegex = isRegex.Value;
            changeEvent.IsRegex = true;
            somethingChanged = true;
        }

        if (wholeWord.HasValue && _wholeWord != wholeWord.Value)
        {
            _wholeWord = wholeWord.Value;
            changeEvent.WholeWord = true;
            somethingChanged = true;
        }

        if (matchCase.HasValue && _matchCase != matchCase.Value)
        {
            _matchCase = matchCase.Value;
            changeEvent.MatchCase = true;
            somethingChanged = true;
        }

        if (preserveCase.HasValue && _preserveCase != preserveCase.Value)
        {
            _preserveCase = preserveCase.Value;
            changeEvent.PreserveCase = true;
            somethingChanged = true;
        }

        bool scopeArgumentProvided = searchScopeProvided || searchScope != null;
        if (scopeArgumentProvided)
        {
            Range[]? nextScope = searchScope;
            bool scopeChanged = !AreScopesEqual(_searchScope, nextScope);
            if (scopeChanged)
            {
                _searchScope = searchScope;
                changeEvent.SearchScope = true;
                somethingChanged = true;
            }
        }

        if (loop.HasValue && _loop != loop.Value)
        {
            _loop = loop.Value;
            changeEvent.Loop = true;
            somethingChanged = true;
        }

        if (isSearching.HasValue && _isSearching != isSearching.Value)
        {
            _isSearching = isSearching.Value;
            changeEvent.IsSearching = true;
            somethingChanged = true;
        }

        if (somethingChanged)
        {
            OnFindReplaceStateChange?.Invoke(this, changeEvent);
        }
    }

    private static bool AreScopesEqual(Range[]? left, Range[]? right)
    {
        if (left == null && right == null)
        {
            return true;
        }

        if (left == null || right == null)
        {
            return false;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        for (int i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates a SearchParams object from the current state.
    /// Integrates WholeWord support by passing WordSeparators when needed.
    /// </summary>
    public SearchParams CreateSearchParams(string? wordSeparators = null)
    {
        // Use default word separators if WholeWord is enabled and none provided
        // Default word separators from VS Code: `~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?
        const string DefaultWordSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";
        string? effectiveWordSeparators = _wholeWord
            ? (wordSeparators ?? DefaultWordSeparators)
            : null;

        return new SearchParams(
            searchString: _searchString,
            isRegex: _isRegex,
            matchCase: _matchCase,
            wordSeparators: effectiveWordSeparators
        );
    }

    /// <summary>
    /// Checks if the cursor can navigate backwards.
    /// </summary>
    public bool CanNavigateBack()
    {
        return CanNavigateInLoop() || (_matchesPosition != 1);
    }

    /// <summary>
    /// Checks if the cursor can navigate forwards.
    /// </summary>
    public bool CanNavigateForward()
    {
        return CanNavigateInLoop() || (_matchesPosition < _matchesCount);
    }

    private bool CanNavigateInLoop()
    {
        // If looping is enabled or matches exceed limit, allow navigation
        return _loop || (_matchesCount >= 19999); // MATCHES_LIMIT
    }

    public void Dispose()
    {
        // Clear event handlers
        OnFindReplaceStateChange = null;
    }
}
