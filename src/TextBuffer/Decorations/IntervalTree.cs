// Source: vs/editor/common/model/intervalTree.ts
// - Class: IntervalTree (Lines: 268-1100)
// - Class: IntervalNode (Lines: 142-266)
// - Red-black tree implementation for decoration storage with lazy delta normalization
// Ported: 2025-11-22, Refactored: 2025-11-26 (WS3-PORT-Tree)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PieceTree.TextBuffer.Decorations
{
    /// <summary>
    /// NodeFlags bitmask encoding RB color, visited state, validation, stickiness, etc.
    /// Mirrors TS Constants enum in intervalTree.ts
    /// </summary>
    [Flags]
    internal enum NodeFlags : uint
    {
        // Color: bit 0
        ColorMask = 0b00000001,
        ColorBlack = 0,
        ColorRed = 1,

        // IsVisited: bit 1
        IsVisitedMask = 0b00000010,
        IsVisited = 0b00000010,

        // IsForValidation: bit 2
        IsForValidationMask = 0b00000100,
        IsForValidation = 0b00000100,

        // Stickiness: bits 3-4
        StickinessMask = 0b00011000,
        StickinessShift = 3,

        // CollapseOnReplaceEdit: bit 5
        CollapseOnReplaceEditMask = 0b00100000,
        CollapseOnReplaceEdit = 0b00100000,

        // IsMargin: bit 6
        IsMarginMask = 0b01000000,
        IsMargin = 0b01000000,

        // AffectsFont: bit 7
        AffectsFontMask = 0b10000000,
        AffectsFont = 0b10000000,
    }

    /// <summary>
    /// Interval tree node with TS-style fields for lazy delta normalization.
    /// </summary>
    internal sealed class IntervalNode
    {
        /// <summary>
        /// Contains binary encoded information for color, visited, isForValidation, stickiness, etc.
        /// </summary>
        public uint Metadata;

        public IntervalNode Parent;
        public IntervalNode Left;
        public IntervalNode Right;

        /// <summary>Local start offset (relative, before delta application)</summary>
        public int Start;
        /// <summary>Local end offset (relative, before delta application)</summary>
        public int End;
        /// <summary>Delta for right subtree</summary>
        public int Delta;
        /// <summary>Max end in subtree (for interval search pruning)</summary>
        public int MaxEnd;

        public string Id;
        public int OwnerId;
        public ModelDecorationOptions? Options;

        /// <summary>Cached version when absolute offsets were computed</summary>
        public int CachedVersionId;
        /// <summary>Cached absolute start offset</summary>
        public int CachedAbsoluteStart;
        /// <summary>Cached absolute end offset</summary>
        public int CachedAbsoluteEnd;

        /// <summary>Back-reference to the ModelDecoration</summary>
        public ModelDecoration? Decoration;

        public IntervalNode(string id, int start, int end)
        {
            Metadata = 0;
            Parent = this;
            Left = this;
            Right = this;
            SetColor(true); // Red

            Start = start;
            End = end;
            Delta = 0;
            MaxEnd = end;

            Id = id;
            OwnerId = 0;
            Options = null;
            SetIsForValidation(false);
            SetIsInGlyphMargin(false);
            SetStickiness(TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges);
            SetCollapseOnReplaceEdit(false);
            SetAffectsFont(false);

            CachedVersionId = 0;
            CachedAbsoluteStart = start;
            CachedAbsoluteEnd = end;

            SetIsVisited(false);

            Decoration = null;
        }

        public void Reset(int versionId, int start, int end)
        {
            Start = start;
            End = end;
            MaxEnd = end;
            CachedVersionId = versionId;
            CachedAbsoluteStart = start;
            CachedAbsoluteEnd = end;
        }

        public void SetOptions(ModelDecorationOptions options)
        {
            Options = options;
            var className = options.ClassName;
            SetIsForValidation(
                className == "squiggly-error" ||
                className == "squiggly-warning" ||
                className == "squiggly-info"
            );
            SetIsInGlyphMargin(options.GlyphMarginClassName != null);
            SetStickiness(options.Stickiness);
            SetCollapseOnReplaceEdit(options.CollapseOnReplaceEdit);
            SetAffectsFont(options.AffectsFont);
        }

        public void SetCachedOffsets(int absoluteStart, int absoluteEnd, int cachedVersionId)
        {
            CachedVersionId = cachedVersionId;
            CachedAbsoluteStart = absoluteStart;
            CachedAbsoluteEnd = absoluteEnd;
        }

        public void Detach()
        {
            Parent = null!;
            Left = null!;
            Right = null!;
        }

        #region Metadata accessors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsRed() => (Metadata & (uint)NodeFlags.ColorMask) == (uint)NodeFlags.ColorRed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsBlack() => (Metadata & (uint)NodeFlags.ColorMask) == (uint)NodeFlags.ColorBlack;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetColor(bool red)
        {
            if (red)
                Metadata = (Metadata & ~(uint)NodeFlags.ColorMask) | (uint)NodeFlags.ColorRed;
            else
                Metadata = (Metadata & ~(uint)NodeFlags.ColorMask) | (uint)NodeFlags.ColorBlack;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsVisited() => (Metadata & (uint)NodeFlags.IsVisitedMask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIsVisited(bool value)
        {
            if (value)
                Metadata |= (uint)NodeFlags.IsVisited;
            else
                Metadata &= ~(uint)NodeFlags.IsVisitedMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsForValidation() => (Metadata & (uint)NodeFlags.IsForValidationMask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIsForValidation(bool value)
        {
            if (value)
                Metadata |= (uint)NodeFlags.IsForValidation;
            else
                Metadata &= ~(uint)NodeFlags.IsForValidationMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInGlyphMargin() => (Metadata & (uint)NodeFlags.IsMarginMask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetIsInGlyphMargin(bool value)
        {
            if (value)
                Metadata |= (uint)NodeFlags.IsMargin;
            else
                Metadata &= ~(uint)NodeFlags.IsMarginMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AffectsFont() => (Metadata & (uint)NodeFlags.AffectsFontMask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetAffectsFont(bool value)
        {
            if (value)
                Metadata |= (uint)NodeFlags.AffectsFont;
            else
                Metadata &= ~(uint)NodeFlags.AffectsFontMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TrackedRangeStickiness GetStickiness()
        {
            return (TrackedRangeStickiness)((Metadata & (uint)NodeFlags.StickinessMask) >> (int)NodeFlags.StickinessShift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStickiness(TrackedRangeStickiness stickiness)
        {
            Metadata = (Metadata & ~(uint)NodeFlags.StickinessMask) | ((uint)stickiness << (int)NodeFlags.StickinessShift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetCollapseOnReplaceEdit() => (Metadata & (uint)NodeFlags.CollapseOnReplaceEditMask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCollapseOnReplaceEdit(bool value)
        {
            if (value)
                Metadata |= (uint)NodeFlags.CollapseOnReplaceEdit;
            else
                Metadata &= ~(uint)NodeFlags.CollapseOnReplaceEditMask;
        }
        #endregion
    }

    /// <summary>
    /// Augmented red-black tree that stores model decorations ordered by start offset
    /// and exposes overlap queries in O(log n + k).
    /// Uses lazy delta normalization to achieve O(log n) edits instead of O(n).
    /// </summary>
    internal sealed class IntervalTree
    {
        /// <summary>
        /// Safety bounds for delta values to prevent integer overflow.
        /// Based on V8's SMI (Small Integer) limits: -(1 &lt;&lt; 30) to (1 &lt;&lt; 30)
        /// </summary>
        private const int MinSafeDelta = -(1 << 30);
        private const int MaxSafeDelta = 1 << 30;

        /// <summary>
        /// Sentinel node representing null leaves. All null pointers point to SENTINEL.
        /// This eliminates null checks in tree operations.
        /// </summary>
        internal static readonly IntervalNode Sentinel;

        static IntervalTree()
        {
            Sentinel = new IntervalNode(null!, 0, 0);
            Sentinel.Parent = Sentinel;
            Sentinel.Left = Sentinel;
            Sentinel.Right = Sentinel;
            Sentinel.SetColor(false); // Black
        }

#if DEBUG
        // DEBUG counters
        private static int _nodesRemovedCount;
        private static int _requestNormalizeHits;

        public static int NodesRemovedCount => _nodesRemovedCount;
        public static int RequestNormalizeHits => _requestNormalizeHits;

        public static void ResetDebugCounters()
        {
            _nodesRemovedCount = 0;
            _requestNormalizeHits = 0;
        }
#endif

        private readonly Dictionary<string, IntervalNode> _nodesById = new(StringComparer.Ordinal);
        private IntervalNode _root;
        private bool _normalizePending;

        public IntervalTree()
        {
            _root = Sentinel;
            _normalizePending = false;
        }

        internal IntervalNode Root => _root;
        internal bool NormalizePending => _normalizePending;

        public int Count => _nodesById.Count;

        /// <summary>
        /// Request normalization of delta values. Called when delta exceeds safe bounds.
        /// </summary>
        public void RequestNormalize()
        {
            _normalizePending = true;
#if DEBUG
            _requestNormalizeHits++;
#endif
        }

        #region Public API

        public void Insert(ModelDecoration decoration)
        {
            ArgumentNullException.ThrowIfNull(decoration);

            var range = decoration.Range;
            var node = new IntervalNode(decoration.Id, range.StartOffset, range.EndOffset)
            {
                OwnerId = decoration.OwnerId,
                Decoration = decoration
            };
            node.SetOptions(decoration.Options);

            RbTreeInsert(node);
            _nodesById[decoration.Id] = node;
            NormalizeDeltaIfNeeded();
        }

        public bool TryGet(string id, out ModelDecoration? decoration)
        {
            if (_nodesById.TryGetValue(id, out var node))
            {
                decoration = node.Decoration;
                return true;
            }

            decoration = null;
            return false;
        }

        public bool Remove(string id)
        {
            if (!_nodesById.TryGetValue(id, out var node))
            {
                return false;
            }

            RbTreeDelete(node);
            _nodesById.Remove(id);
#if DEBUG
            _nodesRemovedCount++;
#endif
            NormalizeDeltaIfNeeded();
            return true;
        }

        public void Reinsert(ModelDecoration decoration)
        {
            if (_nodesById.TryGetValue(decoration.Id, out var existing))
            {
                RbTreeDelete(existing);
#if DEBUG
                _nodesRemovedCount++;
#endif
            }

            var range = decoration.Range;
            var node = new IntervalNode(decoration.Id, range.StartOffset, range.EndOffset)
            {
                OwnerId = decoration.OwnerId,
                Decoration = decoration
            };
            node.SetOptions(decoration.Options);

            RbTreeInsert(node);
            _nodesById[decoration.Id] = node;
            NormalizeDeltaIfNeeded();
        }

        public IReadOnlyList<ModelDecoration> Search(TextRange range, int ownerFilter = DecorationOwnerIds.Any)
        {
            if (_root == Sentinel)
            {
                return Array.Empty<ModelDecoration>();
            }

            var result = new List<ModelDecoration>();
            var intervalStart = range.StartOffset;
            var intervalEnd = range.EndOffset;
            // Handle empty query range: expand to include at least one position
            if (intervalEnd <= intervalStart)
            {
                intervalEnd = intervalStart == int.MaxValue ? int.MaxValue : intervalStart + 1;
            }
            IntervalSearch(_root, intervalStart, intervalEnd, ownerFilter, result, 0);
            return result;
        }

        public IEnumerable<ModelDecoration> EnumerateFrom(int startOffset, int ownerFilter = DecorationOwnerIds.Any)
        {
            // Collect to list first to avoid issues with tree modification during enumeration
            NormalizeDeltaIfNeeded();
            var result = new List<ModelDecoration>();
            var node = FindFirstNodeStartingAtOrAfter(startOffset);
            while (node != Sentinel)
            {
                if (node.Decoration != null && DecorationOwnerIds.MatchesFilter(ownerFilter, node.OwnerId))
                {
                    result.Add(node.Decoration);
                }

                node = Successor(node);
            }
            return result;
        }

        public IEnumerable<ModelDecoration> EnumerateAll()
        {
            // Collect to list first to avoid issues with tree modification during enumeration
            NormalizeDeltaIfNeeded();
            var result = new List<ModelDecoration>();
            var node = Minimum(_root);
            while (node != Sentinel && node != null)
            {
                if (node.Decoration != null)
                {
                    result.Add(node.Decoration);
                }

                node = Successor(node);
            }
            return result;
        }

        /// <summary>
        /// Resolve absolute offsets for a node by walking up the tree and summing deltas.
        /// </summary>
        public void ResolveNode(IntervalNode node, int cachedVersionId)
        {
            var initialNode = node;
            int delta = 0;
            while (node != _root)
            {
                if (node == node.Parent.Right)
                {
                    delta += node.Parent.Delta;
                }
                node = node.Parent;
            }

            var nodeStart = initialNode.Start + delta;
            var nodeEnd = initialNode.End + delta;
            initialNode.SetCachedOffsets(nodeStart, nodeEnd, cachedVersionId);

            // Update the ModelDecoration.Range cache
            if (initialNode.Decoration != null)
            {
                initialNode.Decoration.Range = new TextRange(nodeStart, nodeEnd);
            }
        }

        /// <summary>
        /// Accept a replace edit. Uses the TS four-phase algorithm for lazy updates.
        /// </summary>
        public void AcceptReplace(int offset, int length, int textLength, bool forceMoveMarkers)
        {
            // (1) collect all nodes that are intersecting this edit as nodes of interest
            var nodesOfInterest = SearchForEditing(offset, offset + length);

            // (2) remove all nodes that are intersecting this edit
            for (int i = 0; i < nodesOfInterest.Count; i++)
            {
                var node = nodesOfInterest[i];
                RbTreeDelete(node);
#if DEBUG
                _nodesRemovedCount++;
#endif
            }
            NormalizeDeltaIfNeeded();

            // (3) edit all tree nodes except the nodes of interest (lazy delta update)
            NoOverlapReplace(offset, offset + length, textLength);
            NormalizeDeltaIfNeeded();

            // (4) edit the nodes of interest and insert them back in the tree
            for (int i = 0; i < nodesOfInterest.Count; i++)
            {
                var node = nodesOfInterest[i];
                node.Start = node.CachedAbsoluteStart;
                node.End = node.CachedAbsoluteEnd;
                NodeAcceptEdit(node, offset, offset + length, textLength, forceMoveMarkers);
                node.MaxEnd = node.End;
                RbTreeInsert(node);

                // Update ModelDecoration.Range
                if (node.Decoration != null)
                {
                    node.Decoration.Range = new TextRange(node.Start, node.End);
                }
            }
            NormalizeDeltaIfNeeded();
        }

        #endregion

        #region Delta Normalization

        private void NormalizeDeltaIfNeeded()
        {
            if (!_normalizePending)
            {
                return;
            }
            _normalizePending = false;
            NormalizeDelta();
        }

        /// <summary>
        /// In-order traversal to apply accumulated deltas to start/end and reset delta to 0.
        /// Uses iterative approach to avoid stack allocations.
        /// </summary>
        private void NormalizeDelta()
        {
            var node = _root;
            int delta = 0;

            while (node != Sentinel)
            {
                if (node.Left != Sentinel && !node.Left.IsVisited())
                {
                    // go left
                    node = node.Left;
                    continue;
                }

                if (node.Right != Sentinel && !node.Right.IsVisited())
                {
                    // go right
                    delta += node.Delta;
                    node = node.Right;
                    continue;
                }

                // handle current node
                node.Start = delta + node.Start;
                node.End = delta + node.End;
                node.Delta = 0;
                RecomputeMaxEnd(node);

                node.SetIsVisited(true);

                // going up from this node
                node.Left.SetIsVisited(false);
                node.Right.SetIsVisited(false);
                if (node == node.Parent.Right)
                {
                    delta -= node.Parent.Delta;
                }
                node = node.Parent;
            }

            _root.SetIsVisited(false);
        }

        #endregion

        #region Editing

        private enum MarkerMoveSemantics
        {
            MarkerDefined = 0,
            ForceMove = 1,
            ForceStay = 2
        }

        private static bool AdjustMarkerBeforeColumn(int markerOffset, bool markerStickToPreviousCharacter, int checkOffset, MarkerMoveSemantics moveSemantics)
        {
            if (markerOffset < checkOffset)
            {
                return true;
            }
            if (markerOffset > checkOffset)
            {
                return false;
            }
            if (moveSemantics == MarkerMoveSemantics.ForceMove)
            {
                return false;
            }
            if (moveSemantics == MarkerMoveSemantics.ForceStay)
            {
                return true;
            }
            return markerStickToPreviousCharacter;
        }

        /// <summary>
        /// Apply edit to a single node. Mirrors TS nodeAcceptEdit.
        /// </summary>
        private static void NodeAcceptEdit(IntervalNode node, int start, int end, int textLength, bool forceMoveMarkers)
        {
            var nodeStickiness = node.GetStickiness();
            var startStickToPreviousCharacter = (
                nodeStickiness == TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges ||
                nodeStickiness == TrackedRangeStickiness.GrowsOnlyWhenTypingBefore
            );
            var endStickToPreviousCharacter = (
                nodeStickiness == TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges ||
                nodeStickiness == TrackedRangeStickiness.GrowsOnlyWhenTypingBefore
            );

            var deletingCnt = end - start;
            var insertingCnt = textLength;
            var commonLength = Math.Min(deletingCnt, insertingCnt);

            var nodeStart = node.Start;
            var startDone = false;

            var nodeEnd = node.End;
            var endDone = false;

            if (start <= nodeStart && nodeEnd <= end && node.GetCollapseOnReplaceEdit())
            {
                // This edit encompasses the entire decoration range
                // and the decoration has asked to become collapsed
                node.Start = start;
                startDone = true;
                node.End = start;
                endDone = true;
            }

            {
                var moveSemantics = forceMoveMarkers ? MarkerMoveSemantics.ForceMove : (deletingCnt > 0 ? MarkerMoveSemantics.ForceStay : MarkerMoveSemantics.MarkerDefined);
                if (!startDone && AdjustMarkerBeforeColumn(nodeStart, startStickToPreviousCharacter, start, moveSemantics))
                {
                    startDone = true;
                }
                if (!endDone && AdjustMarkerBeforeColumn(nodeEnd, endStickToPreviousCharacter, start, moveSemantics))
                {
                    endDone = true;
                }
            }

            if (commonLength > 0 && !forceMoveMarkers)
            {
                var moveSemantics = deletingCnt > insertingCnt ? MarkerMoveSemantics.ForceStay : MarkerMoveSemantics.MarkerDefined;
                if (!startDone && AdjustMarkerBeforeColumn(nodeStart, startStickToPreviousCharacter, start + commonLength, moveSemantics))
                {
                    startDone = true;
                }
                if (!endDone && AdjustMarkerBeforeColumn(nodeEnd, endStickToPreviousCharacter, start + commonLength, moveSemantics))
                {
                    endDone = true;
                }
            }

            {
                var moveSemantics = forceMoveMarkers ? MarkerMoveSemantics.ForceMove : MarkerMoveSemantics.MarkerDefined;
                if (!startDone && AdjustMarkerBeforeColumn(nodeStart, startStickToPreviousCharacter, end, moveSemantics))
                {
                    node.Start = start + insertingCnt;
                    startDone = true;
                }
                if (!endDone && AdjustMarkerBeforeColumn(nodeEnd, endStickToPreviousCharacter, end, moveSemantics))
                {
                    node.End = start + insertingCnt;
                    endDone = true;
                }
            }

            // Finish
            var deltaColumn = insertingCnt - deletingCnt;
            if (!startDone)
            {
                node.Start = Math.Max(0, nodeStart + deltaColumn);
            }
            if (!endDone)
            {
                node.End = Math.Max(0, nodeEnd + deltaColumn);
            }

            if (node.Start > node.End)
            {
                node.End = node.Start;
            }
        }

        /// <summary>
        /// Search for nodes that intersect with the edit range.
        /// </summary>
        private List<IntervalNode> SearchForEditing(int start, int end)
        {
            var node = _root;
            int delta = 0;
            int nodeMaxEnd;
            int nodeStart;
            int nodeEnd;
            var result = new List<IntervalNode>();

            while (node != Sentinel)
            {
                if (node.IsVisited())
                {
                    // going up from this node
                    node.Left.SetIsVisited(false);
                    node.Right.SetIsVisited(false);
                    if (node == node.Parent.Right)
                    {
                        delta -= node.Parent.Delta;
                    }
                    node = node.Parent;
                    continue;
                }

                if (!node.Left.IsVisited())
                {
                    // first time seeing this node
                    nodeMaxEnd = delta + node.MaxEnd;
                    if (nodeMaxEnd < start)
                    {
                        // cover case b) from above
                        // there is no need to search this node or its children
                        node.SetIsVisited(true);
                        continue;
                    }

                    if (node.Left != Sentinel)
                    {
                        // go left
                        node = node.Left;
                        continue;
                    }
                }

                // handle current node
                nodeStart = delta + node.Start;
                if (nodeStart > end)
                {
                    // cover case a) from above
                    // there is no need to search this node or its right subtree
                    node.SetIsVisited(true);
                    continue;
                }

                nodeEnd = delta + node.End;
                if (nodeEnd >= start)
                {
                    node.SetCachedOffsets(nodeStart, nodeEnd, 0);
                    result.Add(node);
                }
                node.SetIsVisited(true);

                if (node.Right != Sentinel && !node.Right.IsVisited())
                {
                    // go right
                    delta += node.Delta;
                    node = node.Right;
                    continue;
                }
            }

            _root.SetIsVisited(false);
            return result;
        }

        /// <summary>
        /// Apply edit delta to nodes not in the edit range (lazy update).
        /// </summary>
        private void NoOverlapReplace(int start, int end, int textLength)
        {
            var node = _root;
            int delta = 0;
            int nodeMaxEnd;
            int nodeStart;
            var editDelta = textLength - (end - start);

            while (node != Sentinel)
            {
                if (node.IsVisited())
                {
                    // going up from this node
                    node.Left.SetIsVisited(false);
                    node.Right.SetIsVisited(false);
                    if (node == node.Parent.Right)
                    {
                        delta -= node.Parent.Delta;
                    }
                    RecomputeMaxEnd(node);
                    node = node.Parent;
                    continue;
                }

                if (!node.Left.IsVisited())
                {
                    // first time seeing this node
                    nodeMaxEnd = delta + node.MaxEnd;
                    if (nodeMaxEnd < start)
                    {
                        // cover case b) from above
                        // there is no need to search this node or its children
                        node.SetIsVisited(true);
                        continue;
                    }

                    if (node.Left != Sentinel)
                    {
                        // go left
                        node = node.Left;
                        continue;
                    }
                }

                // handle current node
                nodeStart = delta + node.Start;
                if (nodeStart > end)
                {
                    // This node is after the edit - apply delta lazily
                    node.Start += editDelta;
                    node.End += editDelta;
                    node.Delta += editDelta;
                    if (node.Delta < MinSafeDelta || node.Delta > MaxSafeDelta)
                    {
                        RequestNormalize();
                    }
                    // cover case a) from above
                    // there is no need to search this node or its right subtree
                    node.SetIsVisited(true);
                    continue;
                }

                node.SetIsVisited(true);

                if (node.Right != Sentinel && !node.Right.IsVisited())
                {
                    // go right
                    delta += node.Delta;
                    node = node.Right;
                    continue;
                }
            }

            _root.SetIsVisited(false);
        }

        #endregion

        #region Interval Search

        /// <summary>
        /// Iterative interval search using IsVisited flags.
        /// Mirrors TS intervalSearch function for stack-safety with deep trees.
        /// </summary>
        private void IntervalSearch(IntervalNode startNode, int intervalStart, int intervalEnd, int ownerFilter, List<ModelDecoration> result, int initialDelta)
        {
            // https://en.wikipedia.org/wiki/Interval_tree#Augmented_tree
            // Now, it is known that two intervals A and B overlap only when both
            // A.low <= B.high and A.high >= B.low. When searching the trees for
            // nodes overlapping with a given interval, you can immediately skip:
            //  a) all nodes to the right of nodes whose low value is past the end of the given interval.
            //  b) all nodes that have their maximum 'high' value below the start of the given interval.

            var node = startNode;
            int delta = initialDelta;
            int nodeMaxEnd;
            int nodeStart;
            int nodeEnd;

            while (node != Sentinel)
            {
                if (node.IsVisited())
                {
                    // going up from this node
                    node.Left.SetIsVisited(false);
                    node.Right.SetIsVisited(false);
                    if (node == node.Parent.Right)
                    {
                        delta -= node.Parent.Delta;
                    }
                    node = node.Parent;
                    continue;
                }

                if (!node.Left.IsVisited())
                {
                    // first time seeing this node
                    nodeMaxEnd = delta + node.MaxEnd;
                    if (nodeMaxEnd < intervalStart)
                    {
                        // cover case b) from above
                        // there is no need to search this node or its children
                        node.SetIsVisited(true);
                        continue;
                    }

                    if (node.Left != Sentinel)
                    {
                        // go left
                        node = node.Left;
                        continue;
                    }
                }

                // handle current node
                nodeStart = delta + node.Start;
                if (nodeStart > intervalEnd)
                {
                    // cover case a) from above
                    // there is no need to search this node or its right subtree
                    node.SetIsVisited(true);
                    continue;
                }

                nodeEnd = delta + node.End;

                if (nodeEnd >= intervalStart)
                {
                    // Cache absolute offsets even if filters exclude the node, matching TS semantics.
                    node.SetCachedOffsets(nodeStart, nodeEnd, 0);
                    if (node.Decoration != null && DecorationOwnerIds.MatchesFilter(ownerFilter, node.OwnerId))
                    {
                        node.Decoration.Range = new TextRange(nodeStart, nodeEnd);
                        result.Add(node.Decoration);
                    }
                }

                node.SetIsVisited(true);

                if (node.Right != Sentinel && !node.Right.IsVisited())
                {
                    // go right
                    delta += node.Delta;
                    node = node.Right;
                    continue;
                }
            }

            _root.SetIsVisited(false);
        }

        #endregion

        #region Red-Black Tree Operations

        private void RbTreeInsert(IntervalNode newNode)
        {
            if (_root == Sentinel)
            {
                newNode.Parent = Sentinel;
                newNode.Left = Sentinel;
                newNode.Right = Sentinel;
                newNode.SetColor(false); // Black
                _root = newNode;
                return;
            }

            TreeInsert(newNode);
            RecomputeMaxEndWalkToRoot(newNode.Parent);

            // Repair tree
            var x = newNode;
            while (x != _root && x.Parent.IsRed())
            {
                if (x.Parent == x.Parent.Parent.Left)
                {
                    var y = x.Parent.Parent.Right;

                    if (y.IsRed())
                    {
                        x.Parent.SetColor(false);
                        y.SetColor(false);
                        x.Parent.Parent.SetColor(true);
                        x = x.Parent.Parent;
                    }
                    else
                    {
                        if (x == x.Parent.Right)
                        {
                            x = x.Parent;
                            LeftRotate(x);
                        }
                        x.Parent.SetColor(false);
                        x.Parent.Parent.SetColor(true);
                        RightRotate(x.Parent.Parent);
                    }
                }
                else
                {
                    var y = x.Parent.Parent.Left;

                    if (y.IsRed())
                    {
                        x.Parent.SetColor(false);
                        y.SetColor(false);
                        x.Parent.Parent.SetColor(true);
                        x = x.Parent.Parent;
                    }
                    else
                    {
                        if (x == x.Parent.Left)
                        {
                            x = x.Parent;
                            RightRotate(x);
                        }
                        x.Parent.SetColor(false);
                        x.Parent.Parent.SetColor(true);
                        LeftRotate(x.Parent.Parent);
                    }
                }
            }

            _root.SetColor(false); // Black
        }

        private void TreeInsert(IntervalNode z)
        {
            int delta = 0;
            var x = _root;
            var zAbsoluteStart = z.Start;
            var zAbsoluteEnd = z.End;

            while (true)
            {
                var cmp = IntervalCompare(zAbsoluteStart, zAbsoluteEnd, x.Start + delta, x.End + delta);
                if (cmp < 0)
                {
                    // this node should be inserted to the left
                    // => it is not affected by the node's delta
                    if (x.Left == Sentinel)
                    {
                        z.Start -= delta;
                        z.End -= delta;
                        z.MaxEnd -= delta;
                        x.Left = z;
                        break;
                    }
                    else
                    {
                        x = x.Left;
                    }
                }
                else
                {
                    // this node should be inserted to the right
                    // => it is affected by the node's delta
                    if (x.Right == Sentinel)
                    {
                        z.Start -= (delta + x.Delta);
                        z.End -= (delta + x.Delta);
                        z.MaxEnd -= (delta + x.Delta);
                        x.Right = z;
                        break;
                    }
                    else
                    {
                        delta += x.Delta;
                        x = x.Right;
                    }
                }
            }

            z.Parent = x;
            z.Left = Sentinel;
            z.Right = Sentinel;
            z.SetColor(true); // Red
        }

        private void RbTreeDelete(IntervalNode z)
        {
            IntervalNode x;
            IntervalNode y;

            if (z.Left == Sentinel)
            {
                x = z.Right;
                y = z;

                // x's delta is no longer influenced by z's delta
                x.Delta += z.Delta;
                if (x.Delta < MinSafeDelta || x.Delta > MaxSafeDelta)
                {
                    RequestNormalize();
                }
                x.Start += z.Delta;
                x.End += z.Delta;
            }
            else if (z.Right == Sentinel)
            {
                x = z.Left;
                y = z;
            }
            else
            {
                y = Leftest(z.Right);
                x = y.Right;

                // y's delta is no longer influenced by z's delta,
                // but we don't want to walk the entire right-hand-side subtree of x.
                // we therefore maintain z's delta in y, and adjust only x
                x.Start += y.Delta;
                x.End += y.Delta;
                x.Delta += y.Delta;
                if (x.Delta < MinSafeDelta || x.Delta > MaxSafeDelta)
                {
                    RequestNormalize();
                }

                y.Start += z.Delta;
                y.End += z.Delta;
                y.Delta = z.Delta;
                if (y.Delta < MinSafeDelta || y.Delta > MaxSafeDelta)
                {
                    RequestNormalize();
                }
            }

            if (y == _root)
            {
                _root = x;
                x.SetColor(false); // Black

                z.Detach();
                ResetSentinel();
                RecomputeMaxEnd(x);
                _root.Parent = Sentinel;
                return;
            }

            var yWasRed = y.IsRed();

            if (y == y.Parent.Left)
            {
                y.Parent.Left = x;
            }
            else
            {
                y.Parent.Right = x;
            }

            if (y == z)
            {
                x.Parent = y.Parent;
            }
            else
            {
                if (y.Parent == z)
                {
                    x.Parent = y;
                }
                else
                {
                    x.Parent = y.Parent;
                }

                y.Left = z.Left;
                y.Right = z.Right;
                y.Parent = z.Parent;
                y.SetColor(z.IsRed());

                if (z == _root)
                {
                    _root = y;
                }
                else
                {
                    if (z == z.Parent.Left)
                    {
                        z.Parent.Left = y;
                    }
                    else
                    {
                        z.Parent.Right = y;
                    }
                }

                if (y.Left != Sentinel)
                {
                    y.Left.Parent = y;
                }
                if (y.Right != Sentinel)
                {
                    y.Right.Parent = y;
                }
            }

            z.Detach();

            if (yWasRed)
            {
                RecomputeMaxEndWalkToRoot(x.Parent);
                if (y != z)
                {
                    RecomputeMaxEndWalkToRoot(y);
                    RecomputeMaxEndWalkToRoot(y.Parent);
                }
                ResetSentinel();
                return;
            }

            RecomputeMaxEndWalkToRoot(x);
            RecomputeMaxEndWalkToRoot(x.Parent);
            if (y != z)
            {
                RecomputeMaxEndWalkToRoot(y);
                RecomputeMaxEndWalkToRoot(y.Parent);
            }

            // RB-DELETE-FIXUP
            while (x != _root && x.IsBlack())
            {
                if (x == x.Parent.Left)
                {
                    var w = x.Parent.Right;

                    if (w.IsRed())
                    {
                        w.SetColor(false);
                        x.Parent.SetColor(true);
                        LeftRotate(x.Parent);
                        w = x.Parent.Right;
                    }

                    if (w.Left.IsBlack() && w.Right.IsBlack())
                    {
                        w.SetColor(true);
                        x = x.Parent;
                    }
                    else
                    {
                        if (w.Right.IsBlack())
                        {
                            w.Left.SetColor(false);
                            w.SetColor(true);
                            RightRotate(w);
                            w = x.Parent.Right;
                        }

                        w.SetColor(x.Parent.IsRed());
                        x.Parent.SetColor(false);
                        w.Right.SetColor(false);
                        LeftRotate(x.Parent);
                        x = _root;
                    }
                }
                else
                {
                    var w = x.Parent.Left;

                    if (w.IsRed())
                    {
                        w.SetColor(false);
                        x.Parent.SetColor(true);
                        RightRotate(x.Parent);
                        w = x.Parent.Left;
                    }

                    if (w.Left.IsBlack() && w.Right.IsBlack())
                    {
                        w.SetColor(true);
                        x = x.Parent;
                    }
                    else
                    {
                        if (w.Left.IsBlack())
                        {
                            w.Right.SetColor(false);
                            w.SetColor(true);
                            LeftRotate(w);
                            w = x.Parent.Left;
                        }

                        w.SetColor(x.Parent.IsRed());
                        x.Parent.SetColor(false);
                        w.Left.SetColor(false);
                        RightRotate(x.Parent);
                        x = _root;
                    }
                }
            }

            x.SetColor(false); // Black
            ResetSentinel();
        }

        private static IntervalNode Leftest(IntervalNode node)
        {
            while (node.Left != Sentinel)
            {
                node = node.Left;
            }
            return node;
        }

        private void ResetSentinel()
        {
            Sentinel.Parent = Sentinel;
            Sentinel.Left = Sentinel;
            Sentinel.Right = Sentinel;
            Sentinel.Delta = 0;
            Sentinel.Start = 0;
            Sentinel.End = 0;
            Sentinel.SetIsVisited(false);
        }

        #endregion

        #region Rotations

        private void LeftRotate(IntervalNode x)
        {
            var y = x.Right;

            y.Delta += x.Delta;
            if (y.Delta < MinSafeDelta || y.Delta > MaxSafeDelta)
            {
                RequestNormalize();
            }
            y.Start += x.Delta;
            y.End += x.Delta;

            x.Right = y.Left;
            if (y.Left != Sentinel)
            {
                y.Left.Parent = x;
            }
            y.Parent = x.Parent;
            if (x.Parent == Sentinel)
            {
                _root = y;
            }
            else if (x == x.Parent.Left)
            {
                x.Parent.Left = y;
            }
            else
            {
                x.Parent.Right = y;
            }

            y.Left = x;
            x.Parent = y;

            RecomputeMaxEnd(x);
            RecomputeMaxEnd(y);
        }

        private void RightRotate(IntervalNode y)
        {
            var x = y.Left;

            y.Delta -= x.Delta;
            if (y.Delta < MinSafeDelta || y.Delta > MaxSafeDelta)
            {
                RequestNormalize();
            }
            y.Start -= x.Delta;
            y.End -= x.Delta;

            y.Left = x.Right;
            if (x.Right != Sentinel)
            {
                x.Right.Parent = y;
            }
            x.Parent = y.Parent;
            if (y.Parent == Sentinel)
            {
                _root = x;
            }
            else if (y == y.Parent.Right)
            {
                y.Parent.Right = x;
            }
            else
            {
                y.Parent.Left = x;
            }

            x.Right = y;
            y.Parent = x;

            RecomputeMaxEnd(y);
            RecomputeMaxEnd(x);
        }

        #endregion

        #region MaxEnd Computation

        private static int ComputeMaxEnd(IntervalNode node)
        {
            int maxEnd = node.End;
            if (node.Left != Sentinel)
            {
                var leftMaxEnd = node.Left.MaxEnd;
                if (leftMaxEnd > maxEnd)
                {
                    maxEnd = leftMaxEnd;
                }
            }
            if (node.Right != Sentinel)
            {
                var rightMaxEnd = node.Right.MaxEnd + node.Delta;
                if (rightMaxEnd > maxEnd)
                {
                    maxEnd = rightMaxEnd;
                }
            }
            return maxEnd;
        }

        private static void RecomputeMaxEnd(IntervalNode node)
        {
            node.MaxEnd = ComputeMaxEnd(node);
        }

        private void RecomputeMaxEndWalkToRoot(IntervalNode node)
        {
            while (node != Sentinel)
            {
                var maxEnd = ComputeMaxEnd(node);
                if (node.MaxEnd == maxEnd)
                {
                    // no need to go further
                    return;
                }
                node.MaxEnd = maxEnd;
                node = node.Parent;
            }
        }

        #endregion

        #region Utils

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IntervalCompare(int aStart, int aEnd, int bStart, int bEnd)
        {
            if (aStart == bStart)
            {
                return aEnd - bEnd;
            }
            return aStart - bStart;
        }

        private IntervalNode Minimum(IntervalNode node)
        {
            if (node == Sentinel)
            {
                return Sentinel;
            }
            var current = node;
            while (current.Left != Sentinel)
            {
                current = current.Left;
            }
            return current;
        }

        private IntervalNode Successor(IntervalNode node)
        {
            if (node == Sentinel)
            {
                return Sentinel;
            }
            if (node.Right != Sentinel)
            {
                return Minimum(node.Right);
            }

            var current = node;
            var parent = current.Parent;
            while (parent != Sentinel && current == parent.Right)
            {
                current = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        /// <summary>
        /// Delta-aware BST search so we can locate the starting node without eagerly normalizing the tree.
        /// </summary>
        private IntervalNode FindFirstNodeStartingAtOrAfter(int startOffset)
        {
            var current = _root;
            var candidate = Sentinel;
            int delta = 0;

            while (current != Sentinel)
            {
                var absoluteStart = current.Start + delta;
                if (absoluteStart >= startOffset)
                {
                    candidate = current;
                    current = current.Left;
                    continue;
                }

                if (current.Right == Sentinel)
                {
                    break;
                }

                delta += current.Delta;
                current = current.Right;
            }

            return candidate;
        }

        #endregion
    }
}
