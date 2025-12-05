// Source: ts/src/vs/editor/contrib/multicursor/browser/multicursor.ts
// - Class: MultiCursorSession (Lines: 275-456)
// Ported: 2025-12-05 (Direct translation from TypeScript)
// Updated: 2025-12-05 (Fixed namespace + simplified to use TextModel.FindNextMatch directly)

using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;
using Selection = PieceTree.TextBuffer.Core.Selection;

namespace PieceTree.TextBuffer.Cursor;

/// <summary>
/// Manages a multi-cursor selection session for "Add Selection To Next Find Match" (Ctrl+D).
/// Tracks current selections and provides methods to add/move to next/previous matches.
/// Based on TS MultiCursorSession class (multicursor.ts L275-456).
/// </summary>
public sealed class MultiCursorSession
{
    private readonly TextModel _model;
    
    /// <summary>
    /// The text being searched for.
    /// </summary>
    public string SearchText { get; }
    
    /// <summary>
    /// Whether to match whole words only.
    /// </summary>
    public bool WholeWord { get; }
    
    /// <summary>
    /// Whether the search is case-sensitive.
    /// </summary>
    public bool MatchCase { get; }
    
    /// <summary>
    /// Word separators for whole-word matching.
    /// </summary>
    public string? WordSeparators { get; }
    
    /// <summary>
    /// The current match (used for first selection expansion).
    /// Set to null after being consumed.
    /// </summary>
    public Selection? CurrentMatch { get; set; }

    /// <summary>
    /// Creates a new MultiCursorSession.
    /// TS: MultiCursorSession constructor (multicursor.ts L338-346)
    /// </summary>
    /// <param name="model">The text model.</param>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="wholeWord">Whether to match whole words only.</param>
    /// <param name="matchCase">Whether the search is case-sensitive.</param>
    /// <param name="wordSeparators">Word separators for whole-word matching (null = no whole-word matching).</param>
    /// <param name="currentMatch">Optional initial match (for selection expansion).</param>
    public MultiCursorSession(
        TextModel model,
        string searchText,
        bool wholeWord,
        bool matchCase,
        string? wordSeparators = null,
        Selection? currentMatch = null)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        SearchText = searchText ?? throw new ArgumentNullException(nameof(searchText));
        WholeWord = wholeWord;
        MatchCase = matchCase;
        WordSeparators = wordSeparators;
        CurrentMatch = currentMatch;
    }

    /// <summary>
    /// Factory method to create a session from current editor state.
    /// TS: MultiCursorSession.create() (multicursor.ts L277-336)
    /// </summary>
    /// <param name="model">The text model.</param>
    /// <param name="currentSelection">The current selection.</param>
    /// <param name="wholeWord">Whether to match whole words.</param>
    /// <param name="matchCase">Whether the search is case-sensitive.</param>
    /// <param name="wordSeparators">Word separators for whole-word matching.</param>
    /// <returns>A new session or null if creation failed.</returns>
    public static MultiCursorSession? Create(
        TextModel model,
        Selection currentSelection,
        bool wholeWord = false,
        bool matchCase = false,
        string? wordSeparators = null)
    {
        // TS: If selection is empty, expand to current word
        string searchText;
        Selection? currentMatch = null;

        if (currentSelection.IsEmpty)
        {
            // Expand to word at cursor position
            // TODO: Integrate with WordCharacterClassifier for full word boundary support
            TextPosition pos = currentSelection.Start;
            Range? word = GetWordAtPosition(model, pos);
            
            if (word == null)
            {
                return null; // No word found
            }

            searchText = model.GetValueInRange(word.Value);
            currentMatch = new Selection(
                word.Value.StartLineNumber,
                word.Value.StartColumn,
                word.Value.EndLineNumber,
                word.Value.EndColumn);
        }
        else
        {
            // Use selected text
            searchText = model.GetValueInRange(new Range(
                currentSelection.Start.LineNumber,
                currentSelection.Start.Column,
                currentSelection.End.LineNumber,
                currentSelection.End.Column));
        }

        return new MultiCursorSession(model, searchText, wholeWord, matchCase, wordSeparators, currentMatch);
    }

    /// <summary>
    /// Adds a selection to the next find match.
    /// TS: addSelectionToNextFindMatch() (multicursor.ts L348-357)
    /// </summary>
    /// <param name="currentSelections">The current list of selections.</param>
    /// <returns>A result with updated selections, or null if no match found.</returns>
    public MultiCursorSessionResult? AddSelectionToNextFindMatch(IReadOnlyList<Selection> currentSelections)
    {
        Selection? nextMatch = GetNextMatch(currentSelections);
        if (nextMatch == null)
        {
            return null;
        }

        // Add nextMatch to existing selections
        List<Selection> newSelections = [.. currentSelections, nextMatch.Value];
        
        return new MultiCursorSessionResult(
            newSelections,
            nextMatch.Value.ToRange(),
            ScrollType.Smooth);
    }

    /// <summary>
    /// Moves the last selection to the next find match.
    /// TS: moveSelectionToNextFindMatch() (multicursor.ts L359-368)
    /// </summary>
    /// <param name="currentSelections">The current list of selections.</param>
    /// <returns>A result with updated selections, or null if no match found.</returns>
    public MultiCursorSessionResult? MoveSelectionToNextFindMatch(IReadOnlyList<Selection> currentSelections)
    {
        Selection? nextMatch = GetNextMatch(currentSelections);
        if (nextMatch == null)
        {
            return null;
        }

        // Replace last selection with nextMatch
        List<Selection> newSelections = [.. currentSelections.Take(currentSelections.Count - 1), nextMatch.Value];
        
        return new MultiCursorSessionResult(
            newSelections,
            nextMatch.Value.ToRange(),
            ScrollType.Smooth);
    }

    /// <summary>
    /// Gets the next match after the last selection.
    /// TS: _getNextMatch() (multicursor.ts L370-387)
    /// Uses TextModel.FindNextMatch directly for simpler integration.
    /// </summary>
    private Selection? GetNextMatch(IReadOnlyList<Selection> currentSelections)
    {
        // If we have a currentMatch (from initial word expansion), use it
        if (CurrentMatch != null)
        {
            Selection result = CurrentMatch.Value;
            CurrentMatch = null; // Consume it
            return result;
        }

        // Find next match after the last selection
        Selection lastSelection = currentSelections[^1];
        TextPosition searchStart = lastSelection.End;

        // Use TextModel.FindNextMatch directly (simplified from FindModel dependency)
        FindMatch? match = _model.FindNextMatch(
            SearchText,
            searchStart,
            isRegex: false,
            matchCase: MatchCase,
            wordSeparators: WholeWord ? WordSeparators : null,
            captureMatches: false);
        
        if (match == null)
        {
            // Wrap around: search from document start
            match = _model.FindNextMatch(
                SearchText,
                new TextPosition(1, 1),
                isRegex: false,
                matchCase: MatchCase,
                wordSeparators: WholeWord ? WordSeparators : null,
                captureMatches: false);
        }
        
        if (match == null)
        {
            return null;
        }

        return new Selection(
            match.Range.StartLineNumber,
            match.Range.StartColumn,
            match.Range.EndLineNumber,
            match.Range.EndColumn);
    }

    /// <summary>
    /// Adds a selection to the previous find match.
    /// TS: addSelectionToPreviousFindMatch() (multicursor.ts L389-398)
    /// </summary>
    public MultiCursorSessionResult? AddSelectionToPreviousFindMatch(IReadOnlyList<Selection> currentSelections)
    {
        Selection? previousMatch = GetPreviousMatch(currentSelections);
        if (previousMatch == null)
        {
            return null;
        }

        List<Selection> newSelections = [.. currentSelections, previousMatch.Value];
        
        return new MultiCursorSessionResult(
            newSelections,
            previousMatch.Value.ToRange(),
            ScrollType.Smooth);
    }

    /// <summary>
    /// Moves the last selection to the previous find match.
    /// TS: moveSelectionToPreviousFindMatch() (multicursor.ts L400-409)
    /// </summary>
    public MultiCursorSessionResult? MoveSelectionToPreviousFindMatch(IReadOnlyList<Selection> currentSelections)
    {
        Selection? previousMatch = GetPreviousMatch(currentSelections);
        if (previousMatch == null)
        {
            return null;
        }

        List<Selection> newSelections = [.. currentSelections.Take(currentSelections.Count - 1), previousMatch.Value];
        
        return new MultiCursorSessionResult(
            newSelections,
            previousMatch.Value.ToRange(),
            ScrollType.Smooth);
    }

    /// <summary>
    /// Gets the previous match before the last selection.
    /// TS: _getPreviousMatch() (multicursor.ts L411-428)
    /// Uses TextModel.FindPreviousMatch directly for simpler integration.
    /// </summary>
    private Selection? GetPreviousMatch(IReadOnlyList<Selection> currentSelections)
    {
        if (CurrentMatch != null)
        {
            Selection result = CurrentMatch.Value;
            CurrentMatch = null;
            return result;
        }

        Selection lastSelection = currentSelections[^1];
        TextPosition searchStart = lastSelection.Start;

        // Use TextModel.FindPreviousMatch directly (simplified from FindModel dependency)
        FindMatch? match = _model.FindPreviousMatch(
            SearchText,
            searchStart,
            isRegex: false,
            matchCase: MatchCase,
            wordSeparators: WholeWord ? WordSeparators : null,
            captureMatches: false);
        
        if (match == null)
        {
            // Wrap around: search from document end
            int lastLine = _model.GetLineCount();
            int lastCol = _model.GetLineMaxColumn(lastLine);
            match = _model.FindPreviousMatch(
                SearchText,
                new TextPosition(lastLine, lastCol),
                isRegex: false,
                matchCase: MatchCase,
                wordSeparators: WholeWord ? WordSeparators : null,
                captureMatches: false);
        }
        
        if (match == null)
        {
            return null;
        }

        return new Selection(
            match.Range.StartLineNumber,
            match.Range.StartColumn,
            match.Range.EndLineNumber,
            match.Range.EndColumn);
    }

    /// <summary>
    /// Selects all occurrences of the search text.
    /// TS: selectAll() (multicursor.ts L430-442)
    /// Uses TextModel.FindMatches directly for simpler integration.
    /// </summary>
    /// <returns>All matches found in the document.</returns>
    public IReadOnlyList<FindMatch> SelectAll()
    {
        return _model.FindMatches(
            SearchText,
            searchRange: null,
            isRegex: false,
            matchCase: MatchCase,
            wordSeparators: WholeWord ? WordSeparators : null,
            captureMatches: false);
    }

    /// <summary>
    /// Gets the word at a given position.
    /// Simplified version; full implementation would use WordCharacterClassifier.
    /// TODO: Integrate with CursorWordCharacterClassifier for better word boundary support.
    /// </summary>
    private static Range? GetWordAtPosition(TextModel model, TextPosition position)
    {
        string line = model.GetLineContent(position.LineNumber);
        if (string.IsNullOrEmpty(line) || position.Column > line.Length)
        {
            return null;
        }

        int col = position.Column - 1; // 0-based
        if (col >= line.Length)
        {
            return null;
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
}
