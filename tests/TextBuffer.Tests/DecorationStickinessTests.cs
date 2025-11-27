// Source: ts/src/vs/editor/test/common/model/modelDecorations.test.ts
// - Focus: tracked range stickiness matrix for insertions at edges
// Ported: 2025-11-23

using PieceTree.TextBuffer.Decorations;

namespace PieceTree.TextBuffer.Tests;

public class DecorationStickinessTests
{
    [Theory]
    [InlineData(TrackedRangeStickiness.AlwaysGrowsWhenTypingAtEdges, true, true)]
    [InlineData(TrackedRangeStickiness.NeverGrowsWhenTypingAtEdges, false, false)]
    [InlineData(TrackedRangeStickiness.GrowsOnlyWhenTypingBefore, true, false)]
    [InlineData(TrackedRangeStickiness.GrowsOnlyWhenTypingAfter, false, true)]
    public void InsertionsAtEdgesMatchStickinessMatrix(TrackedRangeStickiness stickiness, bool growsAtStart, bool growsAtEnd)
    {
        StickinessResult startResult = SimulateEdgeInsertion(stickiness, insertAtStart: true);
        int originalLength = startResult.OriginalEnd - startResult.OriginalStart;
        int expectedStart = growsAtStart ? startResult.OriginalStart : startResult.OriginalStart + startResult.InsertLength;
        int expectedLength = growsAtStart ? originalLength + startResult.InsertLength : originalLength;

        Assert.Equal(expectedStart, startResult.UpdatedRange.StartOffset);
        Assert.Equal(expectedLength, startResult.UpdatedRange.Length);

        StickinessResult endResult = SimulateEdgeInsertion(stickiness, insertAtStart: false);
        originalLength = endResult.OriginalEnd - endResult.OriginalStart;
        int expectedEnd = growsAtEnd ? endResult.OriginalEnd + endResult.InsertLength : endResult.OriginalEnd;
        expectedLength = growsAtEnd ? originalLength + endResult.InsertLength : originalLength;

        Assert.Equal(expectedEnd, endResult.UpdatedRange.EndOffset);
        Assert.Equal(expectedLength, endResult.UpdatedRange.Length);
        Assert.Equal(endResult.OriginalStart, endResult.UpdatedRange.StartOffset);
    }

    private static StickinessResult SimulateEdgeInsertion(TrackedRangeStickiness stickiness, bool insertAtStart)
    {
        TextModel model = new("abcdefghijklmnop");
        ModelDecoration decoration = model.AddDecoration(new TextRange(4, 9), new ModelDecorationOptions
        {
            Stickiness = stickiness,
        });

        int originalStart = decoration.Range.StartOffset;
        int originalEnd = decoration.Range.EndOffset;
        const string insertText = "ZZ";
        TextPosition insertPosition = model.GetPositionAt(insertAtStart ? originalStart : originalEnd);
        model.ApplyEdits([new TextEdit(insertPosition, insertPosition, insertText)]);

        return new StickinessResult(originalStart, originalEnd, insertText.Length, decoration.Range);
    }

    private readonly record struct StickinessResult(int OriginalStart, int OriginalEnd, int InsertLength, TextRange UpdatedRange);
}
