// Source Alignment: mirrors VS Code's intervalTree.test.ts coverage (insert/delete/change/search/normalize)
// Added: DEBUG counter assertions for lazy normalization stack fix parity (2025-11-27)

using System;
using System.Collections.Generic;
using System.Linq;
using PieceTree.TextBuffer.Decorations;
using Xunit;

namespace PieceTree.TextBuffer.Tests
{
    /// <summary>
    /// IntervalTree QA suite aligned with VS Code's intervalTree.test.ts scenarios.
    /// </summary>
    public class IntervalTreeTests
    {
        private static readonly ModelDecorationOptions HiddenOptions = ModelDecorationOptions.CreateHiddenOptions();

        #region Generated sequences (insert/delete/search parity)

        [Theory]
        [MemberData(nameof(GetGeneratedSequences), DisableDiscoveryEnumeration = true)]
        public void GeneratedSequencesStayConsistent(string name, IntervalOp[] operations, (int Start, int End)[] expectedFinalRanges)
        {
            var harness = new IntervalTreeHarness();
            foreach (var op in operations)
            {
                harness.Apply(op);
            }

            harness.AssertFinalOrder(expectedFinalRanges);
        }

        public static IEnumerable<object[]> GetGeneratedSequences()
        {
            yield return new object[]
            {
                "gen07",
                new[]
                {
                    Ops.Insert(24, 26),
                    Ops.Insert(11, 28),
                    Ops.Insert(27, 30),
                    Ops.Insert(80, 85),
                    Ops.Delete(1),
                    Ops.Search(0, 90)
                },
                new (int Start, int End)[]
                {
                    (24, 26),
                    (27, 30),
                    (80, 85)
                }
            };

            yield return new object[]
            {
                "gen10",
                new[]
                {
                    Ops.Insert(32, 40),
                    Ops.Insert(25, 29),
                    Ops.Insert(24, 32),
                    Ops.Search(20, 35)
                },
                new (int Start, int End)[]
                {
                    (24, 32),
                    (25, 29),
                    (32, 40)
                }
            };

            yield return new object[]
            {
                "gen18",
                new[]
                {
                    Ops.Insert(25, 25),
                    Ops.Insert(67, 79),
                    Ops.Delete(0),
                    Ops.Search(65, 75)
                },
                new (int Start, int End)[]
                {
                    (67, 79)
                }
            };
        }

        #endregion

        #region Change/Reinsert parity

        [Fact]
        public void ChangeOperation_ReinsertsNodeAndKeepsOrder()
        {
            var harness = new IntervalTreeHarness();
            harness.Apply(Ops.Insert(5, 10));
            harness.Apply(Ops.Insert(30, 40));
            harness.Apply(Ops.Insert(50, 60));
            harness.Apply(Ops.Change(1, 18, 55));
            harness.Apply(Ops.Search(0, 65));

            harness.AssertFinalOrder((5, 10), (18, 55), (50, 60));
        }

        #endregion

        #region AcceptReplace / lazy normalization scenarios

        [Fact]
        public void AcceptReplace_DeleteInsideRange_ReinsertsTouchedNodes()
        {
            var tree = new IntervalTree();
            var a = CreateDecoration("a", DecorationOwnerIds.Default, 10, 20);
            var b = CreateDecoration("b", DecorationOwnerIds.Default, 30, 40);
            tree.Insert(a);
            tree.Insert(b);

#if DEBUG
            IntervalTree.ResetDebugCounters();
            var removedBefore = IntervalTree.NodesRemovedCount;
#endif

            tree.AcceptReplace(offset: 12, length: 4, textLength: 2, forceMoveMarkers: false);

            var snapshot = tree.Search(new TextRange(0, 100))
                .Select(d => (d.Id, d.Range.StartOffset, d.Range.EndOffset))
                .OrderBy(tuple => tuple.Id)
                .ToArray();

            Assert.Equal(new[] { ("a", 10, 18), ("b", 28, 38) }, snapshot);
            Assert.Equal(2, tree.Count);

#if DEBUG
            var removedDelta = IntervalTree.NodesRemovedCount - removedBefore;
            Assert.Equal(1, removedDelta);
#endif
        }

        [Fact]
        public void LargeDeltaTriggersDeferredNormalization()
        {
            var tree = new IntervalTree();
            var decoration = CreateDecoration("huge", DecorationOwnerIds.Default, 100, 101);
            tree.Insert(decoration);

#if DEBUG
            IntervalTree.ResetDebugCounters();
            var normalizeBefore = IntervalTree.RequestNormalizeHits;
#endif

            tree.AcceptReplace(offset: 0, length: 0, textLength: 1_200_000_000, forceMoveMarkers: false);

#if DEBUG
            var normalizeAfter = IntervalTree.RequestNormalizeHits;
            Assert.True(normalizeAfter > normalizeBefore);
#endif

            var snapshot = tree.Search(new TextRange(0, int.MaxValue));
            Assert.Single(snapshot);
            Assert.Equal(1_200_000_100, snapshot[0].Range.StartOffset);
        }

        #endregion

        #region Overlap queries + owner filters

        [Theory]
        [MemberData(nameof(GetCormenQueries), DisableDiscoveryEnumeration = true)]
        public void IntervalSearchMatchesCormenReference(int queryStart, int queryEnd, (int Start, int End)[] expected)
        {
            var tree = BuildCormenTree();
            var actual = tree.Search(new TextRange(queryStart, queryEnd))
                .Select(d => (Start: d.Range.StartOffset, End: d.Range.EndOffset))
                .OrderBy(tuple => tuple.Start)
                .ThenBy(tuple => tuple.End)
                .ToArray();

            Assert.Equal(expected, actual);
        }

        public static IEnumerable<object[]> GetCormenQueries()
        {
            yield return new object[] { 1, 2, new (int Start, int End)[] { (0, 3) } };
            yield return new object[] { 4, 8, new (int Start, int End)[] { (5, 8), (6, 10), (8, 9) } };
            yield return new object[] { 10, 15, new (int Start, int End)[] { (6, 10), (15, 23) } };
            yield return new object[] { 21, 25, new (int Start, int End)[] { (15, 23), (16, 21), (25, 30) } };
            yield return new object[] { 24, 24, new (int Start, int End)[] { (25, 30) } };
        }

        [Fact]
        public void OwnerFiltersReturnExpectedIntervals()
        {
            var tree = new IntervalTree();
            var global = CreateDecoration("global", DecorationOwnerIds.Default, 0, 5);
            var ownerA = CreateDecoration("owner-a", 101, 5, 10);
            var ownerB = CreateDecoration("owner-b", 202, 10, 15);
            var ownerA2 = CreateDecoration("owner-a2", 101, 20, 25);

            tree.Insert(global);
            tree.Insert(ownerA);
            tree.Insert(ownerB);
            tree.Insert(ownerA2);

            var ownerAResults = tree.Search(new TextRange(0, 30), ownerFilter: 101)
                .Select(d => d.Id)
                .ToArray();
            Assert.Equal(new[] { "global", "owner-a", "owner-a2" }, ownerAResults);

            var ownerBResults = tree.Search(new TextRange(0, 30), ownerFilter: 202)
                .Select(d => d.Id)
                .ToArray();
            Assert.Equal(new[] { "global", "owner-b" }, ownerBResults);
        }

        #endregion

        #region Lazy normalization regression

        [Fact]
        public void LazyNormalizationStackRegression_AllowsRepeatedSearches()
        {
            var tree = new IntervalTree();
            for (int i = 0; i < 50; i++)
            {
                var start = i * 20;
                tree.Insert(CreateDecoration($"range-{i}", DecorationOwnerIds.Default, start, start + 10));
            }

            for (int iteration = 0; iteration < 10; iteration++)
            {
                var offset = (iteration * 15) % 200;
                tree.AcceptReplace(offset, length: 5, textLength: 3, forceMoveMarkers: false);
                var matches = tree.Search(new TextRange(0, 1_500));
                Assert.Equal(50, matches.Count);
            }

            // Re-run search multiple times to ensure sentinel visitation flags are reset (IntervalTree-StackFix parity)
            for (int iteration = 0; iteration < 5; iteration++)
            {
                var matches = tree.Search(new TextRange(0, 1_500));
                Assert.Equal(50, matches.Count);
            }
        }

        #endregion

        #region Helpers

        private static IntervalTree BuildCormenTree()
        {
            var tree = new IntervalTree();
            var data = new (int Start, int End)[]
            {
                (16, 21),
                (8, 9),
                (25, 30),
                (5, 8),
                (15, 23),
                (17, 19),
                (26, 26),
                (0, 3),
                (6, 10),
                (19, 20)
            };

            for (int i = 0; i < data.Length; i++)
            {
                var (start, end) = data[i];
                tree.Insert(CreateDecoration($"cormen-{i}", DecorationOwnerIds.Default, start, end));
            }

            return tree;
        }

        private static ModelDecoration CreateDecoration(string id, int ownerId, int start, int end)
            => new(id, ownerId, new TextRange(start, end), HiddenOptions);

        private sealed class IntervalTreeHarness
        {
            private readonly IntervalTree _tree = new();
            private readonly List<ModelDecoration?> _nodes = new();
            private readonly List<TestInterval?> _records = new();
            private readonly List<TestInterval> _ordered = new();
            private int _lastNodeId = -1;

            public void Apply(IntervalOp op)
            {
                switch (op.Type)
                {
                    case IntervalOpType.Insert:
                        Insert(op);
                        break;
                    case IntervalOpType.Delete:
                        Delete(op.NodeIndex);
                        break;
                    case IntervalOpType.Change:
                        Change(op);
                        break;
                    case IntervalOpType.Search:
                        AssertSearch(op.Start, op.End, op.OwnerFilter);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(op));
                }
            }

            public void AssertFinalOrder(params (int Start, int End)[] expected)
            {
                var normalizedExpected = expected
                    .OrderBy(tuple => tuple.Start)
                    .ThenBy(tuple => tuple.End)
                    .ToArray();

                var actual = _tree.EnumerateAll()
                    .Select(d => (Start: d.Range.StartOffset, End: d.Range.EndOffset))
                    .OrderBy(tuple => tuple.Start)
                    .ThenBy(tuple => tuple.End)
                    .ToArray();

                Assert.Equal(normalizedExpected, actual);
            }

            private void Insert(IntervalOp op)
            {
                var nodeId = ++_lastNodeId;
                var decoration = CreateDecoration($"node-{nodeId}", op.OwnerFilter, op.Start, op.End);
                _tree.Insert(decoration);

                var interval = new TestInterval(decoration.Id, op.OwnerFilter, op.Start, op.End);
                if (nodeId == _nodes.Count)
                {
                    _nodes.Add(decoration);
                    _records.Add(interval);
                }
                else
                {
                    _nodes[nodeId] = decoration;
                    _records[nodeId] = interval;
                }

                _ordered.Add(interval);
                SortOrdered();
                AssertTreeMatchesOracle();
            }

            private void Delete(int nodeId)
            {
                var decoration = GetDecoration(nodeId);
                Assert.True(_tree.Remove(decoration.Id), $"Failed to remove decoration {decoration.Id}");

                var record = GetInterval(nodeId);
                _ordered.Remove(record);
                _nodes[nodeId] = null;
                _records[nodeId] = null;

                SortOrdered();
                AssertTreeMatchesOracle();
            }

            private void Change(IntervalOp op)
            {
                var decoration = GetDecoration(op.NodeIndex);
                decoration.Range = new TextRange(op.Start, op.End);
                _tree.Reinsert(decoration);

                var record = GetInterval(op.NodeIndex);
                record.Start = op.Start;
                record.End = op.End;

                SortOrdered();
                AssertTreeMatchesOracle();
            }

            private void AssertSearch(int start, int end, int ownerFilter)
            {
                var actual = _tree.Search(new TextRange(start, end), ownerFilter)
                    .Select(d => (Start: d.Range.StartOffset, End: d.Range.EndOffset))
                    .OrderBy(tuple => tuple.Start)
                    .ThenBy(tuple => tuple.End)
                    .ToArray();

                var expected = _ordered
                    .Where(interval => DecorationOwnerIds.MatchesFilter(ownerFilter, interval.OwnerId) && interval.Start <= end && interval.End >= start)
                    .Select(interval => (Start: interval.Start, End: interval.End))
                    .OrderBy(tuple => tuple.Start)
                    .ThenBy(tuple => tuple.End)
                    .ToArray();

                Assert.Equal(expected, actual);
            }

            private void SortOrdered()
            {
                _ordered.Sort((a, b) =>
                {
                    var cmp = a.Start.CompareTo(b.Start);
                    return cmp != 0 ? cmp : a.End.CompareTo(b.End);
                });
            }

            private void AssertTreeMatchesOracle()
            {
                var actual = _tree.EnumerateAll()
                    .Select(d => (d.Id, d.Range.StartOffset, d.Range.EndOffset))
                    .ToArray();

                var expected = _ordered
                    .Select(interval => (interval.DecorationId, interval.Start, interval.End))
                    .ToArray();

                Assert.Equal(expected, actual);
            }

            private ModelDecoration GetDecoration(int nodeId)
            {
                if (nodeId < 0 || nodeId >= _nodes.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(nodeId));
                }

                return _nodes[nodeId] ?? throw new InvalidOperationException($"Node {nodeId} has been deleted");
            }

            private TestInterval GetInterval(int nodeId)
            {
                if (nodeId < 0 || nodeId >= _records.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(nodeId));
                }

                return _records[nodeId] ?? throw new InvalidOperationException($"Interval {nodeId} has been deleted");
            }
        }

        private sealed class TestInterval
        {
            public TestInterval(string decorationId, int ownerId, int start, int end)
            {
                DecorationId = decorationId;
                OwnerId = ownerId;
                Start = start;
                End = end;
            }

            public string DecorationId { get; }
            public int OwnerId { get; }
            public int Start { get; set; }
            public int End { get; set; }
        }

        private static class Ops
        {
            public static IntervalOp Insert(int start, int end, int ownerId = DecorationOwnerIds.Default)
                => new(IntervalOpType.Insert, -1, start, end, ownerId);

            public static IntervalOp Delete(int nodeId)
                => new(IntervalOpType.Delete, nodeId, 0, 0, DecorationOwnerIds.Default);

            public static IntervalOp Change(int nodeId, int start, int end)
                => new(IntervalOpType.Change, nodeId, start, end, DecorationOwnerIds.Default);

            public static IntervalOp Search(int start, int end, int ownerFilter = DecorationOwnerIds.Any)
                => new(IntervalOpType.Search, -1, start, end, ownerFilter);
        }

        public enum IntervalOpType
        {
            Insert,
            Delete,
            Change,
            Search,
        }

        public readonly record struct IntervalOp(IntervalOpType Type, int NodeIndex, int Start, int End, int OwnerFilter);

        #endregion
    }
}
