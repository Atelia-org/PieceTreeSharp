/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// TypeScript source reference:
// File: ts/src/vs/editor/contrib/find/browser/findDecorations.ts
// Lines: 1-380 (FindDecorations class)

using System;
using System.Collections.Generic;
using System.Linq;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.DocUI
{
    /// <summary>
    /// Manages decorations (highlights) for find matches in the editor.
    /// Handles current match highlighting and all match decorations.
    /// </summary>
    public class FindDecorations : IDisposable
    {
        private const int FindDecorationsOwnerId = 1000; // Unique owner ID for find decorations
        
        private readonly TextModel _model;
        private readonly List<string> _decorationIds = new();
        private readonly List<string> _findScopeDecorationIds = new();
        private string? _highlightedDecorationId;
        private TextPosition _startPosition;
        private Range[]? _cachedFindScopes;
        
        // Decoration options (mimicking TS implementation)
        private static readonly ModelDecorationOptions CurrentFindMatchDecoration = new ModelDecorationOptions
        {
            Description = "current-find-match",
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ZIndex = 13,
            ClassName = "currentFindMatch",
            ShowIfCollapsed = true
        }.Normalize();

        private static readonly ModelDecorationOptions FindMatchDecoration = new ModelDecorationOptions
        {
            Description = "find-match",
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ZIndex = 10,
            ClassName = "findMatch",
            ShowIfCollapsed = true
        }.Normalize();

        private static readonly ModelDecorationOptions FindMatchNoOverviewDecoration = new ModelDecorationOptions
        {
            Description = "find-match-no-overview",
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ClassName = "findMatch",
            ShowIfCollapsed = true
        }.Normalize();

        private static readonly ModelDecorationOptions FindScopeDecoration = new ModelDecorationOptions
        {
            Description = "find-scope",
            ClassName = "findScope",
            IsWholeLine = true
        }.Normalize();

        public FindDecorations(TextModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _startPosition = new TextPosition(1, 1);
        }

        public int GetCount()
        {
            return _decorationIds.Count;
        }

        public Range[]? GetFindScopes()
        {
            if (_cachedFindScopes != null)
            {
                return _cachedFindScopes;
            }

            if (_findScopeDecorationIds.Count == 0)
            {
                return null;
            }

            var scopes = new List<Range>(_findScopeDecorationIds.Count);
            foreach (var id in _findScopeDecorationIds)
            {
                var decoration = _model.GetDecorationById(id);
                var range = decoration != null ? GetRangeFromDecoration(decoration) : null;
                if (range != null)
                {
                    scopes.Add(range.Value);
                }
            }

            _cachedFindScopes = scopes.Count == 0 ? null : scopes.ToArray();
            return _cachedFindScopes;
        }

        public TextPosition GetStartPosition()
        {
            return _startPosition;
        }

        public void SetStartPosition(TextPosition newStartPosition)
        {
            _startPosition = newStartPosition;
            SetCurrentMatch(null);
        }

        /// <summary>
        /// Sets the current find match decoration and returns the match position (1-based).
        /// </summary>
        public int SetCurrentMatch(Range? nextMatch)
        {
            string? newCurrentDecorationId = null;
            int matchPosition = 0;
            
            if (nextMatch != null)
            {
                // Find the decoration that matches this range
                for (int i = 0; i < _decorationIds.Count; i++)
                {
                    var decoration = _model.GetDecorationById(_decorationIds[i]);
                    if (decoration != null)
                    {
                        var range = GetRangeFromDecoration(decoration);
                        if (range != null && nextMatch.Equals(range))
                        {
                            newCurrentDecorationId = _decorationIds[i];
                            matchPosition = i + 1; // 1-based position
                            break;
                        }
                    }
                }
            }

            if (_highlightedDecorationId == newCurrentDecorationId)
            {
                return matchPosition;
            }

            var idsToReplace = new List<string>();
            var replacements = new List<ModelDeltaDecoration>();
            string? pendingHighlight = null;

            if (_highlightedDecorationId != null)
            {
                var previous = _model.GetDecorationById(_highlightedDecorationId);
                if (previous != null)
                {
                    idsToReplace.Add(_highlightedDecorationId);
                    replacements.Add(new ModelDeltaDecoration(previous.Range, FindMatchDecoration));
                }
                _highlightedDecorationId = null;
            }

            if (newCurrentDecorationId != null)
            {
                var current = _model.GetDecorationById(newCurrentDecorationId);
                if (current != null)
                {
                    idsToReplace.Add(newCurrentDecorationId);
                    replacements.Add(new ModelDeltaDecoration(current.Range, CurrentFindMatchDecoration));
                    pendingHighlight = newCurrentDecorationId;
                }
            }

            if (idsToReplace.Count > 0)
            {
                var result = _model.DeltaDecorations(FindDecorationsOwnerId, idsToReplace, replacements);
                for (int i = 0; i < idsToReplace.Count && i < result.Count; i++)
                {
                    var oldIndex = _decorationIds.IndexOf(idsToReplace[i]);
                    if (oldIndex >= 0)
                    {
                        _decorationIds[oldIndex] = result[i].Id;
                    }

                    if (pendingHighlight != null && idsToReplace[i] == pendingHighlight)
                    {
                        _highlightedDecorationId = result[i].Id;
                    }
                }
            }

            return matchPosition;
        }

        /// <summary>
        /// Sets all find match decorations.
        /// For large result sets (>1000), uses simplified decorations.
        /// </summary>
        public void Set(FindMatch[] findMatches, Range[]? findScopes)
        {
            var findMatchesOptions = FindMatchDecoration;
            
            // Optimize for large result sets (TS: >1000 matches)
            if (findMatches.Length > 1000)
            {
                findMatchesOptions = FindMatchNoOverviewDecoration;
            }

            // Create decoration deltas for find matches
            var newFindMatchesDecorations = new List<ModelDeltaDecoration>(findMatches.Length);
            foreach (var match in findMatches)
            {
                var startOffset = _model.GetOffsetAt(match.Range.Start);
                var endOffset = _model.GetOffsetAt(match.Range.End);
                var range = new TextRange(startOffset, endOffset);
                newFindMatchesDecorations.Add(new ModelDeltaDecoration(range, findMatchesOptions));
            }

            // Replace old decorations with new ones
            var newDecorations = _model.DeltaDecorations(
                FindDecorationsOwnerId,
                _decorationIds,
                newFindMatchesDecorations
            );
            
            _decorationIds.Clear();
            _decorationIds.AddRange(newDecorations.Select(d => d.Id));
            
            // Reset highlighted decoration ID since we replaced all decorations
            _highlightedDecorationId = null;

            // Update find scope decorations
            if (_findScopeDecorationIds.Count > 0)
            {
                _model.DeltaDecorations(FindDecorationsOwnerId, _findScopeDecorationIds, null);
                _findScopeDecorationIds.Clear();
            }
            _cachedFindScopes = null;
            
            if (findScopes != null && findScopes.Length > 0)
            {
                var scopeDecorations = new List<ModelDeltaDecoration>();
                foreach (var scope in findScopes)
                {
                    var startOffset = _model.GetOffsetAt(scope.Start);
                    var endOffset = _model.GetOffsetAt(scope.End);
                    var range = new TextRange(startOffset, endOffset);
                    scopeDecorations.Add(new ModelDeltaDecoration(range, FindScopeDecoration));
                }
                
                var scopeDecs = _model.DeltaDecorations(FindDecorationsOwnerId, null, scopeDecorations);
                _findScopeDecorationIds.AddRange(scopeDecs.Select(d => d.Id));
                _cachedFindScopes = CloneRanges(findScopes);
            }
            else
            {
                _cachedFindScopes = null;
            }
        }

        /// <summary>
        /// Clears all decorations.
        /// </summary>
        public void ClearDecorations()
        {
            if (_decorationIds.Count > 0)
            {
                _model.DeltaDecorations(FindDecorationsOwnerId, _decorationIds, null);
                _decorationIds.Clear();
            }
            
            if (_findScopeDecorationIds.Count > 0)
            {
                _model.DeltaDecorations(FindDecorationsOwnerId, _findScopeDecorationIds, null);
                _findScopeDecorationIds.Clear();
            }
            
            _highlightedDecorationId = null;
            _cachedFindScopes = null;
        }
        
        /// <summary>
        /// Resets decoration tracking without removing from model.
        /// </summary>
        public void Reset()
        {
            ClearDecorations();
            _startPosition = new TextPosition(1, 1);
        }

        /// <summary>
        /// Gets the current match range.
        /// </summary>
        public Range? GetCurrentMatchRange()
        {
            if (_highlightedDecorationId != null)
            {
                var decoration = _model.GetDecorationById(_highlightedDecorationId);
                if (decoration != null)
                {
                    return GetRangeFromDecoration(decoration);
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all match ranges.
        /// </summary>
        public Range[] GetAllMatchRanges()
        {
            var ranges = new List<Range>(_decorationIds.Count);
            foreach (var id in _decorationIds)
            {
                var decoration = _model.GetDecorationById(id);
                if (decoration != null)
                {
                    var range = GetRangeFromDecoration(decoration);
                    if (range != null)
                    {
                        ranges.Add(range.Value);
                    }
                }
            }
            return ranges.ToArray();
        }
        
        /// <summary>
        /// Gets the current match position (1-based index) for a given selection range.
        /// Returns 0 if no match is found at the selection.
        /// </summary>
        public int GetCurrentMatchesPosition(Range editorSelection)
        {
            var decorationsInRange = _model.GetDecorationsInRange(
                new TextRange(
                    _model.GetOffsetAt(editorSelection.Start),
                    _model.GetOffsetAt(editorSelection.End)
                ),
                FindDecorationsOwnerId
            );
            
            foreach (var decoration in decorationsInRange)
            {
                var index = _decorationIds.IndexOf(decoration.Id);
                if (index >= 0)
                {
                    return index + 1; // 1-based position
                }
            }
            
            return 0;
        }
        
        /// <summary>
        /// Finds the match before a given position.
        /// </summary>
        public Range? MatchBeforePosition(TextPosition position)
        {
            if (_decorationIds.Count == 0)
            {
                return null;
            }

            for (int i = _decorationIds.Count - 1; i >= 0; i--)
            {
                var decoration = _model.GetDecorationById(_decorationIds[i]);
                if (decoration != null)
                {
                    var r = GetRangeFromDecoration(decoration);
                    if (r == null)
                    {
                        continue;
                    }
                    
                    var range = r.Value;
                    if (range.End.LineNumber > position.LineNumber)
                    {
                        continue;
                    }
                    
                    if (range.End.LineNumber < position.LineNumber)
                    {
                        return range;
                    }
                    
                    if (range.End.Column > position.Column)
                    {
                        continue;
                    }
                    
                    return range;
                }
            }

            // Wrap around: return the last match
            if (_decorationIds.Count > 0)
            {
                var lastDecoration = _model.GetDecorationById(_decorationIds[^1]);
                if (lastDecoration != null)
                {
                    return GetRangeFromDecoration(lastDecoration);
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Finds the match after a given position.
        /// </summary>
        public Range? MatchAfterPosition(TextPosition position)
        {
            if (_decorationIds.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < _decorationIds.Count; i++)
            {
                var decoration = _model.GetDecorationById(_decorationIds[i]);
                if (decoration != null)
                {
                    var r = GetRangeFromDecoration(decoration);
                    if (r == null)
                    {
                        continue;
                    }
                    
                    var range = r.Value;
                    if (range.Start.LineNumber < position.LineNumber)
                    {
                        continue;
                    }
                    
                    if (range.Start.LineNumber > position.LineNumber)
                    {
                        return range;
                    }
                    
                    if (range.Start.Column < position.Column)
                    {
                        continue;
                    }
                    
                    return range;
                }
            }

            // Wrap around: return the first match
            if (_decorationIds.Count > 0)
            {
                var firstDecoration = _model.GetDecorationById(_decorationIds[0]);
                if (firstDecoration != null)
                {
                    return GetRangeFromDecoration(firstDecoration);
                }
            }
            
            return null;
        }
        
        private static Range[] CloneRanges(Range[] ranges)
        {
            var copy = new Range[ranges.Length];
            Array.Copy(ranges, copy, ranges.Length);
            return copy;
        }

        private Range? GetRangeFromDecoration(ModelDecoration decoration)
        {
            var startPos = _model.GetPositionAt(decoration.Range.StartOffset);
            var endPos = _model.GetPositionAt(decoration.Range.EndOffset);
            return new Range(startPos, endPos);
        }

        public void Dispose()
        {
            ClearDecorations();
        }
    }
}
