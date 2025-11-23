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
        private const double DefaultViewportHeightPx = 600d;
        
        private readonly TextModel _model;
        private readonly int _ownerId;
        private readonly Func<double?> _viewportHeightProvider;
        private readonly List<string> _decorationIds = new();
        private readonly List<string> _overviewRulerApproximationDecorationIds = new();
        private readonly List<string> _findScopeDecorationIds = new();
        private string? _highlightedDecorationId;
        private string? _rangeHighlightDecorationId;
        private TextPosition _startPosition;
        
        // Decoration options (mimicking TS implementation)
        private static readonly ModelDecorationOptions CurrentFindMatchDecoration = new ModelDecorationOptions
        {
            Description = "current-find-match",
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ZIndex = 13,
            ClassName = "currentFindMatch",
            InlineClassName = "currentFindMatchInline",
            ShowIfCollapsed = true,
            OverviewRuler = new ModelDecorationOverviewRulerOptions
            {
                Color = "overviewRuler.findMatchForeground",
                Position = OverviewRulerLane.Center,
            },
            Minimap = new ModelDecorationMinimapOptions
            {
                Color = "minimap.findMatch",
                Position = MinimapPosition.Inline,
            },
        }.Normalize();

        private static readonly ModelDecorationOptions FindMatchDecoration = new ModelDecorationOptions
        {
            Description = "find-match",
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ZIndex = 10,
            ClassName = "findMatch",
            InlineClassName = "findMatchInline",
            ShowIfCollapsed = true,
            OverviewRuler = new ModelDecorationOverviewRulerOptions
            {
                Color = "overviewRuler.findMatchForeground",
                Position = OverviewRulerLane.Center,
            },
            Minimap = new ModelDecorationMinimapOptions
            {
                Color = "minimap.findMatch",
                Position = MinimapPosition.Inline,
            },
        }.Normalize();

        private static readonly ModelDecorationOptions FindMatchNoOverviewDecoration = new ModelDecorationOptions
        {
            Description = "find-match-no-overview",
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ClassName = "findMatch",
            InlineClassName = "findMatchInline",
            ShowIfCollapsed = true
        }.Normalize();

        private static readonly ModelDecorationOptions FindMatchOnlyOverviewDecoration = new ModelDecorationOptions
        {
            Description = "find-match-only-overview",
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            OverviewRuler = new ModelDecorationOverviewRulerOptions
            {
                Color = "overviewRuler.findMatchForeground",
                Position = OverviewRulerLane.Center,
            },
        }.Normalize();

        private static readonly ModelDecorationOptions RangeHighlightDecoration = new ModelDecorationOptions
        {
            Description = "find-range-highlight",
            Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
            ClassName = "rangeHighlight",
            IsWholeLine = true,
        }.Normalize();

        private static readonly ModelDecorationOptions FindScopeDecoration = new ModelDecorationOptions
        {
            Description = "find-scope",
            ClassName = "findScope",
            IsWholeLine = true
        }.Normalize();

        public FindDecorations(TextModel model, Func<double?>? viewportHeightProvider = null)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _ownerId = _model.AllocateDecorationOwnerId();
            _viewportHeightProvider = viewportHeightProvider ?? (() => null);
            _startPosition = new TextPosition(1, 1);
        }

        public int GetCount()
        {
            return _decorationIds.Count;
        }

        public Range[]? GetFindScopes()
        {
            if (_findScopeDecorationIds.Count == 0)
            {
                return null;
            }

            var scopes = new List<Range>(_findScopeDecorationIds.Count);
            foreach (var id in _findScopeDecorationIds)
            {
                var decoration = _model.GetDecorationById(id);
                if (decoration == null)
                {
                    continue;
                }

                var range = GetRangeFromDecoration(decoration);
                if (range.HasValue)
                {
                    scopes.Add(range.Value);
                }
            }

            return scopes.Count == 0 ? null : scopes.ToArray();
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
            Range? highlightRange = null;
            
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
                            highlightRange = NormalizeHighlightRange(range.Value);
                            break;
                        }
                    }
                }
            }

            if (_highlightedDecorationId != null && _highlightedDecorationId == newCurrentDecorationId)
            {
                ReplaceRangeHighlight(highlightRange);
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
                var result = _model.DeltaDecorations(_ownerId, idsToReplace, replacements);
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

            ReplaceRangeHighlight(highlightRange);
            return matchPosition;
        }

        /// <summary>
        /// Sets all find match decorations.
        /// For large result sets (>1000), uses simplified decorations.
        /// </summary>
        public void Set(FindMatch[] findMatches, Range[]? findScopes)
        {
            var findMatchesOptions = FindMatchDecoration;
            var overviewApproxDecorations = Array.Empty<ModelDeltaDecoration>();
            
            // Optimize for large result sets (TS: >1000 matches)
            if (findMatches.Length > 1000)
            {
                findMatchesOptions = FindMatchNoOverviewDecoration;
                overviewApproxDecorations = BuildOverviewDecorations(findMatches);
            }

            // Create decoration deltas for find matches
            var newFindMatchesDecorations = new List<ModelDeltaDecoration>(findMatches.Length);
            foreach (var match in findMatches)
            {
                newFindMatchesDecorations.Add(new ModelDeltaDecoration(ToTextRange(match.Range), findMatchesOptions));
            }

            // Replace old decorations with new ones
            var newDecorations = _model.DeltaDecorations(
                _ownerId,
                _decorationIds,
                newFindMatchesDecorations
            );
            
            _decorationIds.Clear();
            _decorationIds.AddRange(newDecorations.Select(d => d.Id));
            
            // Reset highlighted decoration ID since we replaced all decorations
            _highlightedDecorationId = null;
            ClearRangeHighlight();

            // Overview ruler approximations
            if (_overviewRulerApproximationDecorationIds.Count > 0 || overviewApproxDecorations.Length > 0)
            {
                var overview = _model.DeltaDecorations(_ownerId, _overviewRulerApproximationDecorationIds, overviewApproxDecorations);
                _overviewRulerApproximationDecorationIds.Clear();
                if (overview.Count > 0)
                {
                    _overviewRulerApproximationDecorationIds.AddRange(overview.Select(d => d.Id));
                }
            }

            // Update find scope decorations
            if (_findScopeDecorationIds.Count > 0)
            {
                _model.DeltaDecorations(_ownerId, _findScopeDecorationIds, null);
                _findScopeDecorationIds.Clear();
            }
            
            if (findScopes != null && findScopes.Length > 0)
            {
                var scopeDecorations = new List<ModelDeltaDecoration>(findScopes.Length);
                foreach (var scope in findScopes)
                {
                    scopeDecorations.Add(new ModelDeltaDecoration(ToTextRange(scope), FindScopeDecoration));
                }
                
                var scopeDecs = _model.DeltaDecorations(_ownerId, null, scopeDecorations);
                _findScopeDecorationIds.AddRange(scopeDecs.Select(d => d.Id));
            }
        }

        /// <summary>
        /// Clears all decorations.
        /// </summary>
        public void ClearDecorations()
        {
            if (_decorationIds.Count > 0)
            {
                _model.DeltaDecorations(_ownerId, _decorationIds, null);
                _decorationIds.Clear();
            }
            
            if (_overviewRulerApproximationDecorationIds.Count > 0)
            {
                _model.DeltaDecorations(_ownerId, _overviewRulerApproximationDecorationIds, null);
                _overviewRulerApproximationDecorationIds.Clear();
            }

            if (_findScopeDecorationIds.Count > 0)
            {
                _model.DeltaDecorations(_ownerId, _findScopeDecorationIds, null);
                _findScopeDecorationIds.Clear();
            }
            
            _highlightedDecorationId = null;
            ClearRangeHighlight();
        }
        
        /// <summary>
        /// Resets decoration tracking without removing from model.
        /// </summary>
        public void Reset()
        {
            ClearDecorations();
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
                _ownerId
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
        
        private TextRange ToTextRange(Range range)
        {
            var startOffset = _model.GetOffsetAt(range.Start);
            var endOffset = _model.GetOffsetAt(range.End);
            return new TextRange(startOffset, endOffset);
        }

        private Range NormalizeHighlightRange(Range range)
        {
            if (range.StartLineNumber == range.EndLineNumber)
            {
                return range;
            }

            if (range.EndColumn != 1)
            {
                return range;
            }

            var previousLine = Math.Max(range.EndLineNumber - 1, range.StartLineNumber);
            var previousLineMaxColumn = _model.GetLineMaxColumn(previousLine);
            return new Range(range.StartLineNumber, range.StartColumn, previousLine, previousLineMaxColumn);
        }

        private void ReplaceRangeHighlight(Range? highlightRange)
        {
            ClearRangeHighlight();
            if (!highlightRange.HasValue)
            {
                return;
            }

            var newId = _model.DeltaDecorations(_ownerId, null, new[]
            {
                new ModelDeltaDecoration(ToTextRange(highlightRange.Value), RangeHighlightDecoration),
            });

            if (newId.Count > 0)
            {
                _rangeHighlightDecorationId = newId[0].Id;
            }
        }

        private void ClearRangeHighlight()
        {
            if (_rangeHighlightDecorationId == null)
            {
                return;
            }

            _model.DeltaDecorations(_ownerId, new[] { _rangeHighlightDecorationId }, null);
            _rangeHighlightDecorationId = null;
        }

        private ModelDeltaDecoration[] BuildOverviewDecorations(FindMatch[] findMatches)
        {
            if (findMatches.Length == 0)
            {
                return Array.Empty<ModelDeltaDecoration>();
            }

            var mergeLinesDelta = CalculateMergeLinesDelta();
            var decorations = new List<ModelDeltaDecoration>();
            var prevStart = findMatches[0].Range.StartLineNumber;
            var prevEnd = findMatches[0].Range.EndLineNumber;

            for (int i = 1; i < findMatches.Length; i++)
            {
                var current = findMatches[i].Range;
                if (prevEnd + mergeLinesDelta >= current.StartLineNumber)
                {
                    if (current.EndLineNumber > prevEnd)
                    {
                        prevEnd = current.EndLineNumber;
                    }
                    continue;
                }

                decorations.Add(CreateOverviewDecoration(prevStart, prevEnd));
                prevStart = current.StartLineNumber;
                prevEnd = current.EndLineNumber;
            }

            decorations.Add(CreateOverviewDecoration(prevStart, prevEnd));
            return decorations.ToArray();
        }

        private int CalculateMergeLinesDelta()
        {
            var viewportHeight = _viewportHeightProvider() ?? DefaultViewportHeightPx;
            if (viewportHeight <= 0)
            {
                viewportHeight = DefaultViewportHeightPx;
            }
            var lineCount = Math.Max(1, _model.GetLineCount());
            var approxPixelsPerLine = viewportHeight / lineCount;
            if (approxPixelsPerLine <= 0)
            {
                return 2;
            }

            var delta = (int)Math.Ceiling(3d / approxPixelsPerLine);
            return Math.Max(2, delta);
        }

        private ModelDeltaDecoration CreateOverviewDecoration(int startLineNumber, int endLineNumber)
        {
            var start = new TextPosition(startLineNumber, 1);
            var end = new TextPosition(Math.Max(startLineNumber, endLineNumber), 1);
            return new ModelDeltaDecoration(new TextRange(_model.GetOffsetAt(start), _model.GetOffsetAt(end)), FindMatchOnlyOverviewDecoration);
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
