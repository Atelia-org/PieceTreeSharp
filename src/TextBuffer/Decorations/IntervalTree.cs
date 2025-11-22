// Source: vs/editor/common/model/intervalTree.ts
// - Class: IntervalTree (Lines: 268-1100)
// - Class: IntervalNode (Lines: 142-266)
// - Red-black tree implementation for decoration storage
// Ported: 2025-11-22

using System;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Decorations
{
    /// <summary>
    /// Augmented red-black tree that stores model decorations ordered by start offset
    /// and exposes overlap queries in O(log n + k).
    /// </summary>
    internal sealed class IntervalTree
    {
        private enum NodeColor
        {
            Red,
            Black,
        }

        private sealed class Node
        {
            public Node(ModelDecoration decoration)
            {
                Decoration = decoration;
                MaxEnd = decoration.Range.EndOffset;
            }

            public ModelDecoration Decoration { get; }
            public NodeColor Color { get; set; } = NodeColor.Red;
            public Node? Left { get; set; }
            public Node? Right { get; set; }
            public Node? Parent { get; set; }
            public int MaxEnd { get; set; }

            public void Recompute()
            {
                MaxEnd = Decoration.Range.EndOffset;
                if (Left is not null && Left.MaxEnd > MaxEnd)
                {
                    MaxEnd = Left.MaxEnd;
                }

                if (Right is not null && Right.MaxEnd > MaxEnd)
                {
                    MaxEnd = Right.MaxEnd;
                }
            }
        }

        private readonly Dictionary<string, Node> _nodesById = new(StringComparer.Ordinal);
        private Node? _root;

        public int Count => _nodesById.Count;

        public void Insert(ModelDecoration decoration)
        {
            ArgumentNullException.ThrowIfNull(decoration);

            var node = new Node(decoration);
            InsertNode(node);
            _nodesById[decoration.Id] = node;
        }

        public bool TryGet(string id, out ModelDecoration decoration)
        {
            if (_nodesById.TryGetValue(id, out var node))
            {
                decoration = node.Decoration;
                return true;
            }

            decoration = default!;
            return false;
        }

        public bool Remove(string id)
        {
            if (!_nodesById.TryGetValue(id, out var node))
            {
                return false;
            }

            DeleteNode(node);
            _nodesById.Remove(id);
            return true;
        }

        public void Reinsert(ModelDecoration decoration)
        {
            if (!_nodesById.TryGetValue(decoration.Id, out var existing))
            {
                Insert(decoration);
                return;
            }

            DeleteNode(existing);
            var replacement = new Node(decoration);
            InsertNode(replacement);
            _nodesById[decoration.Id] = replacement;
        }

        public IReadOnlyList<ModelDecoration> Search(TextRange range, int ownerFilter = DecorationOwnerIds.Any)
        {
            var result = new List<ModelDecoration>();
            CollectOverlaps(_root, range, ownerFilter, result);
            return result;
        }

        public IEnumerable<ModelDecoration> EnumerateFrom(int startOffset, int ownerFilter = DecorationOwnerIds.Any)
        {
            var node = FindFirstNodeStartingAtOrAfter(startOffset);
            while (node != null)
            {
                if (ownerFilter == DecorationOwnerIds.Any || node.Decoration.OwnerId == ownerFilter)
                {
                    yield return node.Decoration;
                }

                node = Successor(node);
            }
        }

        public IEnumerable<ModelDecoration> EnumerateAll()
        {
            var node = Minimum(_root);
            while (node != null)
            {
                yield return node.Decoration;
                node = Successor(node);
            }
        }

        private void CollectOverlaps(Node? node, TextRange range, int ownerFilter, List<ModelDecoration> target)
        {
            if (node == null || node.MaxEnd < range.StartOffset)
            {
                return;
            }

            CollectOverlaps(node.Left, range, ownerFilter, target);

            var currentRange = node.Decoration.Range;
            bool overlaps;
            if (currentRange.IsEmpty)
            {
                // TS Parity: Empty range uses [start, end) semantics (startOffset < endOffset, not <=)
                // Reference: ts/src/vs/editor/common/model/intervalTree.ts:240-242
                overlaps = currentRange.StartOffset >= range.StartOffset && currentRange.StartOffset < range.EndOffset;
            }
            else
            {
                overlaps = currentRange.StartOffset < range.EndOffset && currentRange.EndOffset > range.StartOffset;
            }

            if (overlaps)
            {
                if (ownerFilter == DecorationOwnerIds.Any || node.Decoration.OwnerId == ownerFilter)
                {
                    target.Add(node.Decoration);
                }
            }

            if (currentRange.StartOffset < range.EndOffset)
            {
                CollectOverlaps(node.Right, range, ownerFilter, target);
            }
        }

        private void InsertNode(Node node)
        {
            Node? parent = null;
            var current = _root;
            while (current != null)
            {
                parent = current;
                current = Compare(node, current) < 0 ? current.Left : current.Right;
            }

            node.Parent = parent;
            if (parent == null)
            {
                _root = node;
            }
            else if (Compare(node, parent) < 0)
            {
                parent.Left = node;
            }
            else
            {
                parent.Right = node;
            }

            FixInsert(node);
        }

        private void DeleteNode(Node node)
        {
            var y = node;
            var yOriginalColor = y.Color;
            Node? x;
            Node? xParent;

            if (node.Left == null)
            {
                x = node.Right;
                xParent = node.Parent;
                Transplant(node, node.Right);
            }
            else if (node.Right == null)
            {
                x = node.Left;
                xParent = node.Parent;
                Transplant(node, node.Left);
            }
            else
            {
                y = Minimum(node.Right)!;
                yOriginalColor = y.Color;
                x = y.Right;
                xParent = y.Parent;

                if (y.Parent == node)
                {
                    xParent = y;
                }
                else
                {
                    Transplant(y, y.Right);
                    y.Right = node.Right;
                    if (y.Right != null)
                    {
                        y.Right.Parent = y;
                    }
                }

                Transplant(node, y);
                y.Left = node.Left;
                if (y.Left != null)
                {
                    y.Left.Parent = y;
                }
                y.Color = node.Color;
                y.Recompute();
                UpdateMetadataUpwards(y);
            }

            UpdateMetadataUpwards(xParent);

            if (yOriginalColor == NodeColor.Black)
            {
                FixDelete(x, xParent);
            }
        }

        private void FixInsert(Node node)
        {
            while (node.Parent?.Color == NodeColor.Red)
            {
                var parent = node.Parent;
                var grandparent = parent.Parent;
                if (grandparent == null)
                {
                    break;
                }

                if (parent == grandparent.Left)
                {
                    var uncle = grandparent.Right;
                    if (uncle?.Color == NodeColor.Red)
                    {
                        parent.Color = NodeColor.Black;
                        uncle.Color = NodeColor.Black;
                        grandparent.Color = NodeColor.Red;
                        node = grandparent;
                    }
                    else
                    {
                        if (node == parent.Right)
                        {
                            node = parent;
                            RotateLeft(node);
                        }

                        parent.Color = NodeColor.Black;
                        grandparent.Color = NodeColor.Red;
                        RotateRight(grandparent);
                    }
                }
                else
                {
                    var uncle = grandparent.Left;
                    if (uncle?.Color == NodeColor.Red)
                    {
                        parent.Color = NodeColor.Black;
                        uncle.Color = NodeColor.Black;
                        grandparent.Color = NodeColor.Red;
                        node = grandparent;
                    }
                    else
                    {
                        if (node == parent.Left)
                        {
                            node = parent;
                            RotateRight(node);
                        }

                        parent.Color = NodeColor.Black;
                        grandparent.Color = NodeColor.Red;
                        RotateLeft(grandparent);
                    }
                }
            }

            if (_root != null)
            {
                _root.Color = NodeColor.Black;
            }
            UpdateMetadataUpwards(node);
        }

        private void FixDelete(Node? node, Node? parent)
        {
            while ((node != _root) && (node == null || node.Color == NodeColor.Black))
            {
                if (parent == null)
                {
                    break;
                }

                if (node == parent.Left)
                {
                    var sibling = parent.Right;
                    if (sibling?.Color == NodeColor.Red)
                    {
                        sibling.Color = NodeColor.Black;
                        parent.Color = NodeColor.Red;
                        RotateLeft(parent);
                        sibling = parent.Right;
                    }

                    if ((sibling?.Left == null || sibling.Left.Color == NodeColor.Black) &&
                        (sibling?.Right == null || sibling.Right.Color == NodeColor.Black))
                    {
                        if (sibling != null)
                        {
                            sibling.Color = NodeColor.Red;
                        }
                        node = parent;
                        parent = parent.Parent;
                    }
                    else
                    {
                        if (sibling?.Right == null || sibling.Right.Color == NodeColor.Black)
                        {
                            if (sibling?.Left != null)
                            {
                                sibling.Left.Color = NodeColor.Black;
                            }
                            if (sibling != null)
                            {
                                sibling.Color = NodeColor.Red;
                                RotateRight(sibling);
                            }
                            sibling = parent.Right;
                        }

                        if (sibling != null)
                        {
                            sibling.Color = parent.Color;
                            if (sibling.Right != null)
                            {
                                sibling.Right.Color = NodeColor.Black;
                            }
                        }

                        parent.Color = NodeColor.Black;
                        RotateLeft(parent);
                        node = _root;
                        break;
                    }
                }
                else
                {
                    var sibling = parent.Left;
                    if (sibling?.Color == NodeColor.Red)
                    {
                        sibling.Color = NodeColor.Black;
                        parent.Color = NodeColor.Red;
                        RotateRight(parent);
                        sibling = parent.Left;
                    }

                    if ((sibling?.Right == null || sibling.Right.Color == NodeColor.Black) &&
                        (sibling?.Left == null || sibling.Left.Color == NodeColor.Black))
                    {
                        if (sibling != null)
                        {
                            sibling.Color = NodeColor.Red;
                        }
                        node = parent;
                        parent = parent.Parent;
                    }
                    else
                    {
                        if (sibling?.Left == null || sibling.Left.Color == NodeColor.Black)
                        {
                            if (sibling?.Right != null)
                            {
                                sibling.Right.Color = NodeColor.Black;
                            }
                            if (sibling != null)
                            {
                                sibling.Color = NodeColor.Red;
                                RotateLeft(sibling);
                            }
                            sibling = parent.Left;
                        }

                        if (sibling != null)
                        {
                            sibling.Color = parent.Color;
                            if (sibling.Left != null)
                            {
                                sibling.Left.Color = NodeColor.Black;
                            }
                        }

                        parent.Color = NodeColor.Black;
                        RotateRight(parent);
                        node = _root;
                        break;
                    }
                }
            }

            if (node != null)
            {
                node.Color = NodeColor.Black;
            }
        }

        private void RotateLeft(Node node)
        {
            var pivot = node.Right;
            if (pivot == null)
            {
                return;
            }

            node.Right = pivot.Left;
            if (pivot.Left != null)
            {
                pivot.Left.Parent = node;
            }

            pivot.Parent = node.Parent;
            if (node.Parent == null)
            {
                _root = pivot;
            }
            else if (node == node.Parent.Left)
            {
                node.Parent.Left = pivot;
            }
            else
            {
                node.Parent.Right = pivot;
            }

            pivot.Left = node;
            node.Parent = pivot;

            node.Recompute();
            pivot.Recompute();
            UpdateMetadataUpwards(pivot.Parent);
        }

        private void RotateRight(Node node)
        {
            var pivot = node.Left;
            if (pivot == null)
            {
                return;
            }

            node.Left = pivot.Right;
            if (pivot.Right != null)
            {
                pivot.Right.Parent = node;
            }

            pivot.Parent = node.Parent;
            if (node.Parent == null)
            {
                _root = pivot;
            }
            else if (node == node.Parent.Right)
            {
                node.Parent.Right = pivot;
            }
            else
            {
                node.Parent.Left = pivot;
            }

            pivot.Right = node;
            node.Parent = pivot;

            node.Recompute();
            pivot.Recompute();
            UpdateMetadataUpwards(pivot.Parent);
        }

        private void Transplant(Node? u, Node? v)
        {
            if (u?.Parent == null)
            {
                _root = v;
            }
            else if (u == u.Parent.Left)
            {
                u.Parent.Left = v;
            }
            else
            {
                u.Parent.Right = v;
            }

            if (v != null)
            {
                v.Parent = u?.Parent;
            }

            UpdateMetadataUpwards(v?.Parent);
        }

        private int Compare(Node left, Node right)
        {
            var startComparison = left.Decoration.Range.StartOffset.CompareTo(right.Decoration.Range.StartOffset);
            if (startComparison != 0)
            {
                return startComparison;
            }

            var endComparison = left.Decoration.Range.EndOffset.CompareTo(right.Decoration.Range.EndOffset);
            if (endComparison != 0)
            {
                return endComparison;
            }

            return string.CompareOrdinal(left.Decoration.Id, right.Decoration.Id);
        }

        private Node? Minimum(Node? node)
        {
            var current = node;
            while (current?.Left != null)
            {
                current = current.Left;
            }

            return current;
        }

        private Node? Successor(Node node)
        {
            if (node.Right != null)
            {
                return Minimum(node.Right);
            }

            var current = node;
            var parent = current.Parent;
            while (parent != null && current == parent.Right)
            {
                current = parent;
                parent = parent.Parent;
            }

            return parent;
        }

        private Node? FindFirstNodeStartingAtOrAfter(int startOffset)
        {
            Node? current = _root;
            Node? candidate = null;
            while (current != null)
            {
                if (current.Decoration.Range.StartOffset >= startOffset)
                {
                    candidate = current;
                    current = current.Left;
                }
                else
                {
                    current = current.Right;
                }
            }

            return candidate;
        }

        private void UpdateMetadataUpwards(Node? node)
        {
            var current = node;
            while (current != null)
            {
                current.Recompute();
                current = current.Parent;
            }
        }
    }
}
