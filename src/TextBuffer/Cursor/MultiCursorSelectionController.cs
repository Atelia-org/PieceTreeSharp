// Source: ts/src/vs/editor/contrib/multicursor/browser/multicursor.ts
// - Class: MultiCursorSelectionController (Lines: 458-550)
// Ported: 2025-12-05 (Simplified version without FindController/Editor dependencies)
// 
// Key design differences from TS:
// - No FindController integration (we directly use TextModel.FindMatches)
// - No editor focus checks (out of scope for this library)
// - No isDisconnectedFromFindController logic (simplified)
// - Session state is managed internally based on selection changes

using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;
using Selection = PieceTree.TextBuffer.Core.Selection;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Configuration options for MultiCursorSelectionController.
/// </summary>
public sealed class MultiCursorSelectionOptions
{
    /// <summary>
    /// Word separator characters for whole-word matching.
    /// Default value matches VS Code's default wordSeparators.
    /// </summary>
    public string WordSeparators { get; init; } = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";

    /// <summary>
    /// Whether to match whole words only.
    /// When selection is empty (word expansion), this defaults to true.
    /// </summary>
    public bool WholeWord { get; init; }

    /// <summary>
    /// Whether the search is case-sensitive.
    /// When selection is empty (word expansion), this defaults to true.
    /// </summary>
    public bool MatchCase { get; init; }
}

/// <summary>
/// Controller for multi-cursor selection operations (Ctrl+D behavior).
/// Provides high-level API for:
/// - AddSelectionToNextFindMatch (Ctrl+D)
/// - MoveSelectionToNextFindMatch (Ctrl+K Ctrl+D)
/// - SelectAllMatches (Ctrl+Shift+L)
/// 
/// Based on TS MultiCursorSelectionController class (multicursor.ts L458-550).
/// 
/// Usage pattern:
/// 1. Create controller with TextModel
/// 2. Call AddSelectionToNextFindMatch with current selections
/// 3. Apply returned result (new selections + reveal range)
/// 4. Session is maintained until search text changes or is explicitly reset
/// </summary>
public sealed class MultiCursorSelectionController
{
    private readonly TextModel _model;
    private readonly MultiCursorSelectionOptions _options;
    private MultiCursorSession? _session;
    private string? _lastSearchText;

    /// <summary>
    /// Creates a new MultiCursorSelectionController.
    /// TS: MultiCursorSelectionController constructor (multicursor.ts L464-468)
    /// </summary>
    /// <param name="model">The text model to search.</param>
    /// <param name="options">Optional configuration. If null, uses defaults.</param>
    public MultiCursorSelectionController(TextModel model, MultiCursorSelectionOptions? options = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _options = options ?? new MultiCursorSelectionOptions();
    }

    /// <summary>
    /// Gets or creates a session for the given selections.
    /// TS: _beginSessionIfNeeded() (multicursor.ts L476-501)
    /// </summary>
    private bool EnsureSession(IReadOnlyList<Selection> currentSelections)
    {
        if (currentSelections == null || currentSelections.Count == 0)
        {
            return false;
        }

        // Get the primary selection (first in list)
        Selection primarySelection = currentSelections[0];

        // Determine search text
        string searchText = GetSearchText(primarySelection);
        if (string.IsNullOrEmpty(searchText))
        {
            return false;
        }

        // Check if session needs to be recreated
        // TS: Session is recreated when search text changes
        if (_session == null || _lastSearchText != searchText)
        {
            // Determine search options based on selection state
            // TS: When starting with empty selection, use wholeWord=true, matchCase=true
            bool wholeWord;
            bool matchCase;
            Selection? currentMatch = null;

            if (primarySelection.IsEmpty)
            {
                // Empty selection → expand to word
                wholeWord = true;
                matchCase = true;

                // Get word at position for initial match
                Range? wordRange = GetWordAtPosition(primarySelection.Start);
                if (wordRange != null)
                {
                    currentMatch = new Selection(
                        wordRange.Value.StartLineNumber,
                        wordRange.Value.StartColumn,
                        wordRange.Value.EndLineNumber,
                        wordRange.Value.EndColumn);
                }
            }
            else
            {
                // Non-empty selection → use configured options
                wholeWord = _options.WholeWord;
                matchCase = _options.MatchCase;
            }

            _session = new MultiCursorSession(
                _model,
                searchText,
                wholeWord,
                matchCase,
                wholeWord ? _options.WordSeparators : null,
                currentMatch);
            
            _lastSearchText = searchText;
        }

        return _session != null;
    }

    /// <summary>
    /// Gets the search text from a selection.
    /// If selection is empty, expands to word at cursor position.
    /// TS: Part of MultiCursorSession.create() (multicursor.ts L303-328)
    /// </summary>
    private string GetSearchText(Selection selection)
    {
        if (selection.IsEmpty)
        {
            // Expand to word at cursor position
            Range? wordRange = GetWordAtPosition(selection.Start);
            if (wordRange == null)
            {
                return string.Empty;
            }

            return _model.GetValueInRange(wordRange.Value);
        }
        else
        {
            // Use selected text, normalize line endings
            Range range = new(selection.Start, selection.End);
            string text = _model.GetValueInRange(range);
            return text.Replace("\r\n", "\n");
        }
    }

    /// <summary>
    /// Gets the word at a given position using simple word character detection.
    /// For full TS parity, this would integrate with WordCharacterClassifier.
    /// TS: editor.getConfiguredWordAtPosition() (multicursor.ts L316)
    /// </summary>
    private Range? GetWordAtPosition(TextPosition position)
    {
        string line = _model.GetLineContent(position.LineNumber);
        if (string.IsNullOrEmpty(line))
        {
            return null;
        }

        int col = position.Column - 1; // 0-based
        if (col >= line.Length)
        {
            // At or past end of line
            if (line.Length == 0)
            {
                return null;
            }
            col = line.Length - 1;
        }

        // Simple word detection: alphanumeric + underscore
        if (!IsWordChar(line[col]))
        {
            return null;
        }

        // Find word boundaries
        int start = col;
        while (start > 0 && IsWordChar(line[start - 1]))
        {
            start--;
        }

        int end = col;
        while (end < line.Length - 1 && IsWordChar(line[end + 1]))
        {
            end++;
        }

        return new Range(
            position.LineNumber,
            start + 1, // 1-based column
            position.LineNumber,
            end + 2);  // 1-based, exclusive end
    }

    private static bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    /// <summary>
    /// Adds the next match to the current selections.
    /// TS: addSelectionToNextFindMatch() (multicursor.ts L536-547)
    /// 
    /// Behavior:
    /// - First call: If selection is empty, expands to word and adds that as first match
    /// - Subsequent calls: Finds next occurrence and adds it to selections
    /// - Returns null if no more matches found
    /// </summary>
    /// <param name="currentSelections">Current selections in the editor.</param>
    /// <returns>Result with new selections, or null if no match found.</returns>
    public MultiCursorSessionResult? AddSelectionToNextFindMatch(
        IReadOnlyList<Selection> currentSelections)
    {
        if (!EnsureSession(currentSelections))
        {
            return null;
        }

        return _session!.AddSelectionToNextFindMatch(currentSelections);
    }

    /// <summary>
    /// Moves the last selection to the next match (skip current, go to next).
    /// TS: moveSelectionToNextFindMatch() (multicursor.ts L549-552)
    /// 
    /// Behavior:
    /// - Removes the last selection
    /// - Adds the next matching occurrence instead
    /// - Useful for "skip this occurrence" workflow
    /// </summary>
    /// <param name="currentSelections">Current selections in the editor.</param>
    /// <returns>Result with new selections, or null if no match found.</returns>
    public MultiCursorSessionResult? MoveSelectionToNextFindMatch(
        IReadOnlyList<Selection> currentSelections)
    {
        if (!EnsureSession(currentSelections))
        {
            return null;
        }

        return _session!.MoveSelectionToNextFindMatch(currentSelections);
    }

    /// <summary>
    /// Adds the previous match to the current selections.
    /// TS: addSelectionToPreviousFindMatch() (multicursor.ts L554-557)
    /// </summary>
    /// <param name="currentSelections">Current selections in the editor.</param>
    /// <returns>Result with new selections, or null if no match found.</returns>
    public MultiCursorSessionResult? AddSelectionToPreviousFindMatch(
        IReadOnlyList<Selection> currentSelections)
    {
        if (!EnsureSession(currentSelections))
        {
            return null;
        }

        return _session!.AddSelectionToPreviousFindMatch(currentSelections);
    }

    /// <summary>
    /// Moves the last selection to the previous match.
    /// TS: moveSelectionToPreviousFindMatch() (multicursor.ts L559-562)
    /// </summary>
    /// <param name="currentSelections">Current selections in the editor.</param>
    /// <returns>Result with new selections, or null if no match found.</returns>
    public MultiCursorSessionResult? MoveSelectionToPreviousFindMatch(
        IReadOnlyList<Selection> currentSelections)
    {
        if (!EnsureSession(currentSelections))
        {
            return null;
        }

        return _session!.MoveSelectionToPreviousFindMatch(currentSelections);
    }

    /// <summary>
    /// Selects all occurrences of the current search text.
    /// TS: selectAll() (multicursor.ts L564-598)
    /// 
    /// Behavior:
    /// - Finds all matches for the search text in the document
    /// - Returns selections for all matches
    /// - If possible, keeps the primary cursor at the original selection position
    /// </summary>
    /// <param name="currentSelections">Current selections in the editor.</param>
    /// <returns>Result with all matching selections, or null if no matches found.</returns>
    public MultiCursorSessionResult? SelectAllMatches(
        IReadOnlyList<Selection> currentSelections)
    {
        if (!EnsureSession(currentSelections))
        {
            return null;
        }

        IReadOnlyList<FindMatch> matches = _session!.SelectAll();
        if (matches.Count == 0)
        {
            return null;
        }

        // Convert FindMatch results to Selections
        List<Selection> selections = new(matches.Count);
        
        // TS: Try to keep primary cursor at the intersection with current selection
        // (multicursor.ts L583-592)
        Selection? primarySelection = currentSelections.Count > 0 ? currentSelections[0] : null;
        int primaryIndex = 0;

        for (int i = 0; i < matches.Count; i++)
        {
            FindMatch match = matches[i];
            Selection sel = new(
                match.Range.StartLineNumber,
                match.Range.StartColumn,
                match.Range.EndLineNumber,
                match.Range.EndColumn);
            
            selections.Add(sel);

            // Check if this match intersects with the primary selection
            if (primarySelection != null)
            {
                Range? intersection = match.Range.IntersectRanges(primarySelection.Value.ToRange());
                if (intersection != null)
                {
                    primaryIndex = i;
                }
            }
        }

        // Move the primary selection to the front if it's not already there
        if (primaryIndex > 0)
        {
            Selection primary = selections[primaryIndex];
            selections.RemoveAt(primaryIndex);
            selections.Insert(0, primary);
        }

        // Use the first selection as reveal range
        Range revealRange = selections[0].ToRange();

        return new MultiCursorSessionResult(
            selections,
            revealRange,
            ScrollType.Smooth);
    }

    /// <summary>
    /// Resets the current session.
    /// Call this when the user cancels the multi-cursor operation
    /// or when the document changes significantly.
    /// TS: _endSession() (multicursor.ts L503-514)
    /// </summary>
    public void ResetSession()
    {
        _session = null;
        _lastSearchText = null;
    }

    /// <summary>
    /// Gets the current search text, if a session is active.
    /// Useful for debugging or displaying the current search term.
    /// </summary>
    public string? CurrentSearchText => _session?.SearchText;

    /// <summary>
    /// Gets whether a session is currently active.
    /// </summary>
    public bool HasActiveSession => _session != null;
}
