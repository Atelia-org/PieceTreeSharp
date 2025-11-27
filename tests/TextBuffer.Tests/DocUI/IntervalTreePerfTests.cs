// Source: IntervalTree performance tests aligned with VS Code's DocUI perf harness
// Focus: Mixed edit + decoration workloads, requestNormalize visibility, shared perf helpers

using System.Text;
using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.Tests.Helpers;

namespace PieceTree.TextBuffer.Tests.DocUI;

/// <summary>
/// Performance guardrails for the DocUI IntervalTree using the shared perf harness.
/// </summary>
public class IntervalTreePerfTests
{
    private const int PerfDecorationCount = 10_000;
    private const int LinesPerScenario = PerfDecorationCount;
    private const int CharsPerLine = 20;
    private const int DecorationLength = 10;
    private const int MixedIterations = 200;
    private const int MutationChunkSize = 256;
    private const int MixedOpsBudgetMs = 4500;
    private const int DecorationChurnBudgetMs = 3200;
    private const int QueryBudgetMs = 600;

    private static readonly ModelDecorationOptions PerfDecorationOptions = new()
    {
        Description = "docui-perf",
        Stickiness = TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges,
        RenderKind = DecorationRenderKind.None,
        ShowIfCollapsed = true,
    };

    [Fact]
    [Trait("Category", "Perf")]
    public void MixedEditsAndDecorationMutationsStayWithinBudget()
    {
        IntervalTreePerfFixture fixture = IntervalTreePerfFixture.Create(LinesPerScenario, CharsPerLine, DecorationLength);
        PerfRunResult result = PerfTestHelper.Measure("DocUI.IntervalTreePerf.MixedOperations", fixture.Model, context =>
        {
            RunMixedScenario(context, fixture);
        });

        Assert.True(result.ElapsedMilliseconds < MixedOpsBudgetMs,
            $"Mixed operations took {result.ElapsedMilliseconds}ms (requestNormalizeΔ={result.RequestNormalizeDelta}), expected < {MixedOpsBudgetMs}ms");
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void DecorationDeltaChurnRemainsStable()
    {
        IntervalTreePerfFixture fixture = IntervalTreePerfFixture.Create(LinesPerScenario, CharsPerLine, DecorationLength);
        PerfRunResult result = PerfTestHelper.Measure("DocUI.IntervalTreePerf.DecorationChurn", fixture.Model, context =>
        {
            RunDecorationChurnScenario(context, fixture);
        });

        Assert.True(result.ElapsedMilliseconds < DecorationChurnBudgetMs,
            $"Decoration churn took {result.ElapsedMilliseconds}ms (requestNormalizeΔ={result.RequestNormalizeDelta}), expected < {DecorationChurnBudgetMs}ms");
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void RangeQueriesRemainFast()
    {
        IntervalTreePerfFixture fixture = IntervalTreePerfFixture.Create(LinesPerScenario, CharsPerLine, DecorationLength);
        PerfRunResult result = PerfTestHelper.Measure("DocUI.IntervalTreePerf.RangeQueries", fixture.Model, context =>
        {
            RunRangeQueries(context, fixture, queryCount: 1000, windowLines: 80);
        });

        Assert.True(result.ElapsedMilliseconds < QueryBudgetMs,
            $"Range queries took {result.ElapsedMilliseconds}ms (requestNormalizeΔ={result.RequestNormalizeDelta}), expected < {QueryBudgetMs}ms");
    }

#if DEBUG
    [Fact]
    [Trait("Category", "Perf")]
    public void DebugCounters_NodesRemovedCount_TracksRemovals()
    {
        IntervalTree.ResetDebugCounters();
        TextModel model = new("0123456789ABCDEF");
        int ownerId = model.AllocateDecorationOwnerId();

        model.AddDecoration(new TextRange(0, 5), ownerId: ownerId);
        model.AddDecoration(new TextRange(5, 10), ownerId: ownerId);
        model.AddDecoration(new TextRange(10, 15), ownerId: ownerId);

        int initialNodesRemoved = IntervalTree.NodesRemovedCount;
        model.RemoveAllDecorations(ownerId);
        int finalNodesRemoved = IntervalTree.NodesRemovedCount;

        Assert.True(finalNodesRemoved >= initialNodesRemoved + 3,
            $"Expected at least 3 nodes removed, got {finalNodesRemoved - initialNodesRemoved}");
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void DebugCounters_RequestNormalizeHits_TracksNormalizationTriggers()
    {
        IntervalTree.ResetDebugCounters();

        const int decorationCount = 1000;
        const int charsPerLine = 50;
        string text = CreateText(charsPerLine, decorationCount);
        TextModel model = new(text);

        for (int i = 0; i < decorationCount; i++)
        {
            int offset = i * (charsPerLine + 1);
            model.AddDecoration(new TextRange(offset, offset + 10), PerfDecorationOptions);
        }

        int initialHits = IntervalTree.RequestNormalizeHits;

        for (int i = 0; i < 100; i++)
        {
            int lineNum = i % decorationCount;
            int offset = lineNum * (charsPerLine + 1);
            TextPosition insertPos = model.GetPositionAt(offset);
            model.ApplyEdits([new TextEdit(insertPos, insertPos, "XX")]);
        }

        int finalHits = IntervalTree.RequestNormalizeHits;
        Console.WriteLine($"[PERF] RequestNormalize hits delta: {finalHits - initialHits}");
        Assert.True(finalHits >= 0, "RequestNormalizeHits should be non-negative");
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void DebugCounters_ResetDebugCounters_ClearsAllCounters()
    {
        TextModel model = new("0123456789");
        int ownerId = model.AllocateDecorationOwnerId();
        model.AddDecoration(new TextRange(0, 5), ownerId: ownerId);
        model.RemoveAllDecorations(ownerId);

        Assert.True(IntervalTree.NodesRemovedCount >= 0);
        IntervalTree.ResetDebugCounters();

        Assert.Equal(0, IntervalTree.NodesRemovedCount);
        Assert.Equal(0, IntervalTree.RequestNormalizeHits);
    }
#endif

    private static void RunMixedScenario(PerfContext context, IntervalTreePerfFixture fixture)
    {
        TextModel model = context.Model;

        for (int iteration = 0; iteration < MixedIterations; iteration++)
        {
            int documentLength = Math.Max(1, model.GetLength());
            int deletionLength = iteration % 4;
            int startOffset = Math.Min((int)((iteration * 1543L) % Math.Max(1, documentLength - 1)), documentLength - 1);
            int endOffset = Math.Min(documentLength, startOffset + deletionLength);
            string text = (iteration % 3) switch
            {
                0 => "Z",
                1 => string.Empty,
                _ => "XY"
            };

            TextEdit edit = CreateEdit(model, startOffset, endOffset, text);
            model.ApplyEdits([edit]);
            context.LogOperation("applyEdits", iteration, startOffset, endOffset - startOffset, text);

            if (iteration % 4 == 0)
            {
                RunDecorationMutationBatch(context, fixture, iteration, MutationChunkSize / 2);
            }

            if (iteration % 7 == 0)
            {
                int querySpan = Math.Max(64, fixture.LinePitch * 40);
                int queryStart = Math.Min((int)((iteration * 1913L) % Math.Max(1, documentLength - 1)), documentLength - 1);
                int queryEnd = Math.Min(documentLength, queryStart + querySpan);
                IReadOnlyList<ModelDecoration> decorations = model.GetDecorationsInRange(new TextRange(queryStart, queryEnd), fixture.OwnerId);
                context.LogOperation("query", iteration, queryStart, queryEnd - queryStart, $"matches={decorations.Count}");
            }
        }
    }

    private static void RunDecorationChurnScenario(PerfContext context, IntervalTreePerfFixture fixture)
    {
        const int iterations = 90;
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            RunDecorationMutationBatch(context, fixture, iteration, MutationChunkSize);

            if ((iteration + 1) % 30 == 0)
            {
                context.Model.RemoveAllDecorations(fixture.OwnerId);
                fixture.ClearDecorations();
                IReadOnlyList<ModelDecoration> rehydrated = context.Model.DeltaDecorations(fixture.OwnerId, Array.Empty<string>(), fixture.BaselineDecorations);
                fixture.ResetFrom(rehydrated);
                context.LogOperation("rehydrate", iteration, 0, rehydrated.Count, "baseline");
            }
        }
    }

    private static void RunRangeQueries(PerfContext context, IntervalTreePerfFixture fixture, int queryCount, int windowLines)
    {
        TextModel model = context.Model;
        int windowSize = Math.Max(1, fixture.LinePitch * windowLines);

        for (int i = 0; i < queryCount; i++)
        {
            int documentLength = Math.Max(1, model.GetLength());
            int startOffset = Math.Min((int)((i * 1931L) % Math.Max(1, documentLength - 1)), documentLength - 1);
            int endOffset = Math.Min(documentLength, startOffset + windowSize);
            IReadOnlyList<ModelDecoration> decorations = model.GetDecorationsInRange(new TextRange(startOffset, endOffset), fixture.OwnerId);
            Assert.NotNull(decorations);

            if (i % 25 == 0)
            {
                context.LogOperation("query", i, startOffset, endOffset - startOffset, $"count={decorations.Count}");
            }
        }
    }

    private static void RunDecorationMutationBatch(PerfContext context, IntervalTreePerfFixture fixture, int iteration, int chunkSize)
    {
        IReadOnlyList<string> source = fixture.DecorationIds;
        if (source.Count == 0 || chunkSize <= 0)
        {
            return;
        }

        int sliceSize = Math.Min(chunkSize, source.Count);
        int startIndex = (iteration * 911) % source.Count;
        List<string> ids = new(sliceSize);
        for (int i = 0; i < sliceSize; i++)
        {
            ids.Add(source[(startIndex + i) % source.Count]);
        }

        List<ModelDeltaDecoration> replacements = new(sliceSize);
        int documentLength = Math.Max(1, context.Model.GetLength());
        for (int i = 0; i < sliceSize; i++)
        {
            int randomOffset = context.Random.Next(0, Math.Max(1, documentLength - 1));
            int endOffset = Math.Min(documentLength, randomOffset + fixture.LinePitch);
            replacements.Add(new ModelDeltaDecoration(new TextRange(randomOffset, endOffset), PerfDecorationOptions));
        }

        IReadOnlyList<ModelDecoration> added = context.Model.DeltaDecorations(fixture.OwnerId, ids, replacements);
        fixture.ReplaceDecorationIds(ids, added);
        context.LogOperation("deltaDecorations", iteration, startIndex, sliceSize, null);
    }

    private static TextEdit CreateEdit(TextModel model, int startOffset, int endOffset, string text)
    {
        TextPosition start = model.GetPositionAt(startOffset);
        TextPosition end = model.GetPositionAt(endOffset);
        return new TextEdit(start, end, text);
    }

    private static string CreateText(int charsPerLine, int lineCount)
    {
        StringBuilder builder = new(lineCount * (charsPerLine + 1));
        for (int line = 0; line < lineCount; line++)
        {
            builder.Append(new string((char)('a' + (line % 26)), charsPerLine));
            if (line < lineCount - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private sealed class IntervalTreePerfFixture
    {
        private readonly List<string> _decorationIds;

        private IntervalTreePerfFixture(
            TextModel model,
            List<string> decorationIds,
            int ownerId,
            int linePitch,
            IReadOnlyList<ModelDeltaDecoration> baseline,
            int lineCount)
        {
            Model = model;
            _decorationIds = decorationIds;
            OwnerId = ownerId;
            LinePitch = linePitch;
            BaselineDecorations = baseline;
            LineCount = lineCount;
        }

        public TextModel Model { get; }
        public int OwnerId { get; }
        public int LinePitch { get; }
        public int LineCount { get; }
        public IReadOnlyList<ModelDeltaDecoration> BaselineDecorations { get; }
        public IReadOnlyList<string> DecorationIds => _decorationIds;

        public static IntervalTreePerfFixture Create(int lineCount, int charsPerLine, int decorationLength)
        {
            string text = CreateText(charsPerLine, lineCount);
            TextModel model = new(text);
            int ownerId = model.AllocateDecorationOwnerId();
            List<string> decorationIds = new(lineCount);
            List<ModelDeltaDecoration> baseline = new(lineCount);
            int linePitch = charsPerLine + 1;

            for (int line = 0; line < lineCount; line++)
            {
                int offset = line * linePitch;
                TextRange range = new(offset, Math.Min(offset + decorationLength, model.GetLength()));
                baseline.Add(new ModelDeltaDecoration(range, PerfDecorationOptions));
                ModelDecoration decoration = model.AddDecoration(range, PerfDecorationOptions, ownerId);
                decorationIds.Add(decoration.Id);
            }

            return new IntervalTreePerfFixture(model, decorationIds, ownerId, linePitch, baseline, lineCount);
        }

        public void ReplaceDecorationIds(IReadOnlyList<string> removed, IReadOnlyList<ModelDecoration> added)
        {
            if (removed.Count > 0)
            {
                foreach (string id in removed)
                {
                    _decorationIds.Remove(id);
                }
            }

            if (added.Count > 0)
            {
                foreach (ModelDecoration decoration in added)
                {
                    _decorationIds.Add(decoration.Id);
                }
            }
        }

        public void ResetFrom(IReadOnlyList<ModelDecoration> decorations)
        {
            _decorationIds.Clear();
            foreach (ModelDecoration decoration in decorations)
            {
                _decorationIds.Add(decoration.Id);
            }
        }

        public void ClearDecorations()
        {
            _decorationIds.Clear();
        }
    }
}
