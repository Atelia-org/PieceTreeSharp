// WS2-PORT: Range/Selection Helper APIs Unit Tests
// Covers P0 APIs from INV-RangeSelection-GapReport.md

using PieceTree.TextBuffer.Core;

// Resolve ambiguity with System.Range
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests;

public class RangeSelectionHelperTests
{
    #region Range construction normalization

    [Theory]
    [InlineData(1, 1, 1, 1, 1, 1, 1, 1)] // empty
    [InlineData(1, 2, 1, 1, 1, 1, 1, 2)] // swap same line
    [InlineData(2, 1, 1, 2, 1, 2, 2, 1)] // swap start/end lines
    [InlineData(1, 1, 1, 2, 1, 1, 1, 2)] // no swap same line
    [InlineData(1, 1, 2, 1, 1, 1, 2, 1)] // no swap different lines
    public void RangeCtor_NormalizesEndpoints(
        int inputStartLine, int inputStartColumn,
        int inputEndLine, int inputEndColumn,
        int expectedStartLine, int expectedStartColumn,
        int expectedEndLine, int expectedEndColumn)
    {
        Range range = new(inputStartLine, inputStartColumn, inputEndLine, inputEndColumn);

        Assert.Equal(expectedStartLine, range.StartLineNumber);
        Assert.Equal(expectedStartColumn, range.StartColumn);
        Assert.Equal(expectedEndLine, range.EndLineNumber);
        Assert.Equal(expectedEndColumn, range.EndColumn);
    }

    #endregion

    #region Range.ContainsPosition / StrictContainsPosition

    [Theory]
    [InlineData(1, 1, 3, 5, 2, 3, true)]  // Middle of range
    [InlineData(1, 1, 3, 5, 1, 1, true)]  // At start (inclusive)
    [InlineData(1, 1, 3, 5, 3, 5, true)]  // At end (inclusive)
    [InlineData(1, 1, 3, 5, 0, 5, false)] // Before range (line)
    [InlineData(1, 1, 3, 5, 4, 1, false)] // After range (line)
    [InlineData(1, 5, 1, 10, 1, 4, false)] // Same line, before start column
    [InlineData(1, 5, 1, 10, 1, 11, false)] // Same line, after end column
    public void ContainsPosition_ReturnsExpected(
        int startLine, int startCol, int endLine, int endCol,
        int posLine, int posCol, bool expected)
    {
        Range range = new(startLine, startCol, endLine, endCol);
        TextPosition position = new(posLine, posCol);
        Assert.Equal(expected, Range.ContainsPosition(range, position));
        Assert.Equal(expected, range.ContainsPosition(position));
    }

    [Theory]
    [InlineData(1, 1, 3, 5, 2, 3, true)]  // Middle of range
    [InlineData(1, 1, 3, 5, 1, 1, false)] // At start (exclusive)
    [InlineData(1, 1, 3, 5, 3, 5, false)] // At end (exclusive)
    [InlineData(1, 1, 3, 5, 1, 2, true)]  // Just after start
    [InlineData(1, 1, 3, 5, 3, 4, true)]  // Just before end
    public void StrictContainsPosition_ReturnsExpected(
        int startLine, int startCol, int endLine, int endCol,
        int posLine, int posCol, bool expected)
    {
        Range range = new(startLine, startCol, endLine, endCol);
        TextPosition position = new(posLine, posCol);
        Assert.Equal(expected, Range.StrictContainsPosition(range, position));
    }

    #endregion

    #region Range.ContainsRange / StrictContainsRange

    [Theory]
    [InlineData(1, 1, 5, 10, 2, 2, 4, 8, true)]  // Inner range
    [InlineData(1, 1, 5, 10, 1, 1, 5, 10, true)] // Equal ranges
    [InlineData(1, 1, 5, 10, 1, 1, 6, 1, false)] // Extends past end
    [InlineData(1, 1, 5, 10, 0, 1, 5, 10, false)] // Starts before
    [InlineData(2, 5, 2, 10, 2, 6, 2, 9, true)]  // Same line inner
    [InlineData(2, 2, 5, 10, 1, 3, 2, 2, false)] // Begins before start
    [InlineData(2, 2, 5, 10, 2, 1, 2, 2, false)] // Start column before range
    [InlineData(2, 2, 5, 10, 2, 2, 5, 11, false)] // Ends past end column
    [InlineData(2, 2, 5, 10, 2, 2, 6, 1, false)] // Ends past end line
    [InlineData(2, 2, 5, 10, 5, 9, 6, 1, false)] // Start inside end beyond
    [InlineData(2, 2, 5, 10, 5, 10, 6, 1, false)] // Touches end then extends
    [InlineData(2, 2, 5, 10, 2, 3, 5, 9, true)]  // Strict inner range
    [InlineData(2, 2, 5, 10, 3, 100, 4, 100, true)] // Deep interior
    public void ContainsRange_ReturnsExpected(
        int r1StartLine, int r1StartCol, int r1EndLine, int r1EndCol,
        int r2StartLine, int r2StartCol, int r2EndLine, int r2EndCol,
        bool expected)
    {
        Range range = new(r1StartLine, r1StartCol, r1EndLine, r1EndCol);
        Range other = new(r2StartLine, r2StartCol, r2EndLine, r2EndCol);
        Assert.Equal(expected, Range.ContainsRange(range, other));
        Assert.Equal(expected, range.ContainsRange(other));
    }

    [Theory]
    [InlineData(1, 1, 5, 10, 2, 2, 4, 8, true)]  // Inner range - strict
    [InlineData(1, 1, 5, 10, 1, 1, 5, 10, false)] // Equal ranges - not strict
    [InlineData(1, 1, 5, 10, 1, 2, 5, 9, true)]  // Starts after, ends before
    [InlineData(1, 1, 5, 10, 1, 1, 5, 9, false)] // Same start
    [InlineData(1, 1, 5, 10, 1, 2, 5, 10, false)] // Same end
    public void StrictContainsRange_ReturnsExpected(
        int r1StartLine, int r1StartCol, int r1EndLine, int r1EndCol,
        int r2StartLine, int r2StartCol, int r2EndLine, int r2EndCol,
        bool expected)
    {
        Range range = new(r1StartLine, r1StartCol, r1EndLine, r1EndCol);
        Range other = new(r2StartLine, r2StartCol, r2EndLine, r2EndCol);
        Assert.Equal(expected, Range.StrictContainsRange(range, other));
    }

    #endregion

    #region Range.IntersectRanges / AreIntersecting / AreIntersectingOrTouching

    [Fact]
    public void IntersectRanges_Overlapping_ReturnsIntersection()
    {
        Range a = new(1, 1, 3, 10);
        Range b = new(2, 5, 5, 1);
        Range? result = Range.IntersectRanges(a, b);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Value.StartLineNumber);
        Assert.Equal(5, result.Value.StartColumn);
        Assert.Equal(3, result.Value.EndLineNumber);
        Assert.Equal(10, result.Value.EndColumn);
    }

    [Fact]
    public void IntersectRanges_NoOverlap_ReturnsNull()
    {
        Range a = new(1, 1, 2, 5);
        Range b = new(3, 1, 4, 5);
        Range? result = Range.IntersectRanges(a, b);
        Assert.Null(result);
    }

    [Fact]
    public void IntersectRanges_Touching_ReturnsEmptyRange()
    {
        Range a = new(1, 1, 2, 5);
        Range b = new(2, 5, 3, 1);
        Range? result = Range.IntersectRanges(a, b);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Value.StartLineNumber);
        Assert.Equal(5, result.Value.StartColumn);
        Assert.Equal(2, result.Value.EndLineNumber);
        Assert.Equal(5, result.Value.EndColumn);
        Assert.True(result.Value.IsEmpty);
    }

    [Theory]
    [InlineData(1, 1, 2, 5, 2, 5, 3, 1, true)]  // Touching at point
    [InlineData(1, 1, 2, 5, 2, 6, 3, 1, false)] // Not touching
    [InlineData(1, 1, 3, 1, 2, 1, 4, 1, true)]  // Overlapping
    public void AreIntersectingOrTouching_ReturnsExpected(
        int a1, int a2, int a3, int a4,
        int b1, int b2, int b3, int b4, bool expected)
    {
        Range a = new(a1, a2, a3, a4);
        Range b = new(b1, b2, b3, b4);
        Assert.Equal(expected, Range.AreIntersectingOrTouching(a, b));
    }

    [Theory]
    [InlineData(1, 1, 2, 5, 2, 5, 3, 1, false)] // Touching only - not intersecting
    [InlineData(1, 1, 2, 6, 2, 5, 3, 1, true)]  // Actually overlapping
    [InlineData(1, 1, 3, 1, 2, 1, 4, 1, true)]  // Overlapping
    [InlineData(2, 2, 3, 2, 4, 2, 5, 2, false)] // Disjoint (TS parity)
    [InlineData(4, 2, 5, 2, 2, 2, 3, 2, false)] // Disjoint reverse order
    [InlineData(4, 2, 5, 2, 5, 2, 6, 2, false)] // Touch end but not overlap
    [InlineData(5, 2, 6, 2, 4, 2, 5, 2, false)] // Touch start but not overlap
    [InlineData(2, 2, 2, 7, 2, 4, 2, 6, true)]   // Nested same line
    [InlineData(2, 2, 2, 7, 2, 4, 2, 9, true)]   // Other extends past end but overlaps
    [InlineData(2, 4, 2, 9, 2, 2, 2, 7, true)]   // Reverse overlap
    public void AreIntersecting_ReturnsExpected(
        int a1, int a2, int a3, int a4,
        int b1, int b2, int b3, int b4, bool expected)
    {
        Range a = new(a1, a2, a3, a4);
        Range b = new(b1, b2, b3, b4);
        Assert.Equal(expected, Range.AreIntersecting(a, b));
    }

    #endregion

    #region Range.PlusRange / Normalize

    [Fact]
    public void PlusRange_ReturnsUnion()
    {
        Range a = new(2, 5, 4, 10);
        Range b = new(1, 1, 3, 8);
        Range result = Range.PlusRange(a, b);

        Assert.Equal(1, result.StartLineNumber);
        Assert.Equal(1, result.StartColumn);
        Assert.Equal(4, result.EndLineNumber);
        Assert.Equal(10, result.EndColumn);
    }

    [Fact]
    public void PlusRange_SameLine_TakesMinMaxColumns()
    {
        Range a = new(1, 5, 1, 10);
        Range b = new(1, 3, 1, 15);
        Range result = Range.PlusRange(a, b);

        Assert.Equal(1, result.StartLineNumber);
        Assert.Equal(3, result.StartColumn);
        Assert.Equal(1, result.EndLineNumber);
        Assert.Equal(15, result.EndColumn);
    }

    [Fact]
    public void Normalize_ReversedRange_SwapsEndpoints()
    {
        Range result = Range.Normalize(5, 10, 1, 1);

        Assert.Equal(1, result.StartLineNumber);
        Assert.Equal(1, result.StartColumn);
        Assert.Equal(5, result.EndLineNumber);
        Assert.Equal(10, result.EndColumn);
    }

    [Fact]
    public void Normalize_SameLine_ReversedColumns_SwapsColumns()
    {
        Range result = Range.Normalize(1, 10, 1, 5);

        Assert.Equal(1, result.StartLineNumber);
        Assert.Equal(5, result.StartColumn);
        Assert.Equal(1, result.EndLineNumber);
        Assert.Equal(10, result.EndColumn);
    }

    #endregion

    #region Range instance methods: SetStartPosition, SetEndPosition, CollapseToStart/End, Delta, IsSingleLine

    [Fact]
    public void SetEndPosition_CreatesNewRange()
    {
        Range range = new(1, 1, 2, 5);
        Range result = range.SetEndPosition(3, 10);

        Assert.Equal(1, result.StartLineNumber);
        Assert.Equal(1, result.StartColumn);
        Assert.Equal(3, result.EndLineNumber);
        Assert.Equal(10, result.EndColumn);
    }

    [Fact]
    public void SetEndPosition_DoesNotMutateOriginalRange()
    {
        Range range = new(1, 1, 2, 5);
        Range clone = range;

        _ = range.SetEndPosition(4, 7);

        Assert.Equal(clone, range);
    }

    [Fact]
    public void SetStartPosition_CreatesNewRange()
    {
        Range range = new(1, 1, 3, 10);
        Range result = range.SetStartPosition(2, 5);

        Assert.Equal(2, result.StartLineNumber);
        Assert.Equal(5, result.StartColumn);
        Assert.Equal(3, result.EndLineNumber);
        Assert.Equal(10, result.EndColumn);
    }

    [Fact]
    public void SetStartPosition_DoesNotMutateOriginalRange()
    {
        Range range = new(1, 1, 3, 10);
        Range clone = range;

        _ = range.SetStartPosition(2, 5);

        Assert.Equal(clone, range);
    }

    [Fact]
    public void CollapseToStart_ReturnsEmptyRangeAtStart()
    {
        Range range = new(2, 5, 4, 10);
        Range result = range.CollapseToStart();

        Assert.Equal(2, result.StartLineNumber);
        Assert.Equal(5, result.StartColumn);
        Assert.Equal(2, result.EndLineNumber);
        Assert.Equal(5, result.EndColumn);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void CollapseToEnd_ReturnsEmptyRangeAtEnd()
    {
        Range range = new(2, 5, 4, 10);
        Range result = range.CollapseToEnd();

        Assert.Equal(4, result.StartLineNumber);
        Assert.Equal(10, result.StartColumn);
        Assert.Equal(4, result.EndLineNumber);
        Assert.Equal(10, result.EndColumn);
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void Delta_SingleArg_MovesLines()
    {
        Range range = new(2, 5, 4, 10);
        Range result = range.Delta(3);

        Assert.Equal(5, result.StartLineNumber);
        Assert.Equal(5, result.StartColumn);
        Assert.Equal(7, result.EndLineNumber);
        Assert.Equal(10, result.EndColumn);
    }

    [Fact]
    public void Delta_FourArgs_MovesAllCoordinates()
    {
        Range range = new(2, 5, 4, 10);
        Range result = range.Delta(1, 2, 3, 4);

        Assert.Equal(3, result.StartLineNumber);
        Assert.Equal(7, result.StartColumn);
        Assert.Equal(7, result.EndLineNumber);
        Assert.Equal(14, result.EndColumn);
    }

    [Theory]
    [InlineData(1, 1, 1, 10, true)]   // Same line
    [InlineData(1, 1, 2, 1, false)]   // Different lines
    public void IsSingleLine_ReturnsExpected(int sl, int sc, int el, int ec, bool expected)
    {
        Range range = new(sl, sc, el, ec);
        Assert.Equal(expected, range.IsSingleLine);
    }

    #endregion

    #region Selection static methods

    [Fact]
    public void Selection_FromPositions_SinglePosition_CreatesCollapsedSelection()
    {
        TextPosition pos = new(3, 5);
        Selection selection = Selection.FromPositions(pos);

        Assert.Equal(3, selection.Anchor.LineNumber);
        Assert.Equal(5, selection.Anchor.Column);
        Assert.Equal(3, selection.Active.LineNumber);
        Assert.Equal(5, selection.Active.Column);
        Assert.True(selection.IsEmpty);
    }

    [Fact]
    public void Selection_FromPositions_TwoPositions_CreatesSelection()
    {
        TextPosition start = new(1, 1);
        TextPosition end = new(3, 5);
        Selection selection = Selection.FromPositions(start, end);

        Assert.Equal(1, selection.Anchor.LineNumber);
        Assert.Equal(1, selection.Anchor.Column);
        Assert.Equal(3, selection.Active.LineNumber);
        Assert.Equal(5, selection.Active.Column);
    }

    [Fact]
    public void Selection_FromPositions_ReversedInputs_NormalizesStartEnd()
    {
        TextPosition start = new(5, 10);
        TextPosition end = new(3, 1);
        Selection selection = Selection.FromPositions(start, end);

        Assert.Equal(end, selection.SelectionStart);
        Assert.Equal(start, selection.SelectionEnd);
        Assert.Equal(SelectionDirection.RTL, selection.Direction);
    }

    [Fact]
    public void Selection_FromRange_LTR_AnchorIsStart()
    {
        Range range = new(1, 1, 3, 5);
        Selection selection = Selection.FromRange(range, SelectionDirection.LTR);

        Assert.Equal(SelectionDirection.LTR, selection.Direction);
        Assert.Equal(1, selection.Anchor.LineNumber);
        Assert.Equal(1, selection.Anchor.Column);
        Assert.Equal(3, selection.Active.LineNumber);
        Assert.Equal(5, selection.Active.Column);
    }

    [Fact]
    public void Selection_FromRange_RTL_AnchorIsEnd()
    {
        Range range = new(1, 1, 3, 5);
        Selection selection = Selection.FromRange(range, SelectionDirection.RTL);

        Assert.Equal(SelectionDirection.RTL, selection.Direction);
        Assert.Equal(3, selection.Anchor.LineNumber);
        Assert.Equal(5, selection.Anchor.Column);
        Assert.Equal(1, selection.Active.LineNumber);
        Assert.Equal(1, selection.Active.Column);
    }

    [Fact]
    public void Selection_CreateWithDirection_LTR()
    {
        Selection selection = Selection.CreateWithDirection(1, 1, 3, 5, SelectionDirection.LTR);

        Assert.Equal(SelectionDirection.LTR, selection.Direction);
        Assert.Equal(new TextPosition(1, 1), selection.Anchor);
        Assert.Equal(new TextPosition(3, 5), selection.Active);
    }

    [Fact]
    public void Selection_CreateWithDirection_RTL()
    {
        Selection selection = Selection.CreateWithDirection(1, 1, 3, 5, SelectionDirection.RTL);

        Assert.Equal(SelectionDirection.RTL, selection.Direction);
        Assert.Equal(new TextPosition(3, 5), selection.Anchor);
        Assert.Equal(new TextPosition(1, 1), selection.Active);
    }

    [Fact]
    public void Selection_SetStartPosition_LTR_UpdatesAnchor()
    {
        Selection selection = new(1, 1, 3, 5); // LTR
        Selection result = selection.SetStartPosition(2, 3);

        Assert.Equal(2, result.Anchor.LineNumber);
        Assert.Equal(3, result.Anchor.Column);
        Assert.Equal(3, result.Active.LineNumber);
        Assert.Equal(5, result.Active.Column);
    }

    [Fact]
    public void Selection_SetEndPosition_LTR_UpdatesActive()
    {
        Selection selection = new(1, 1, 3, 5); // LTR
        Selection result = selection.SetEndPosition(4, 8);

        Assert.Equal(1, result.Anchor.LineNumber);
        Assert.Equal(1, result.Anchor.Column);
        Assert.Equal(4, result.Active.LineNumber);
        Assert.Equal(8, result.Active.Column);
    }

    [Fact]
    public void Selection_SetStartPosition_RTL_UpdatesActiveOnly()
    {
        Selection selection = new(new TextPosition(5, 10), new TextPosition(3, 1)); // RTL
        Selection result = selection.SetStartPosition(2, 4);

        Assert.Equal(new TextPosition(5, 10), result.Anchor);
        Assert.Equal(new TextPosition(2, 4), result.Active);
        Assert.Equal(SelectionDirection.RTL, result.Direction);
        Assert.Equal(new TextPosition(3, 1), selection.Active); // original untouched (return semantics)
    }

    [Fact]
    public void Selection_SetEndPosition_RTL_UpdatesAnchorOnly()
    {
        Selection selection = new(new TextPosition(5, 10), new TextPosition(3, 1)); // RTL
        Selection result = selection.SetEndPosition(6, 2);

        Assert.Equal(new TextPosition(6, 2), result.Anchor);
        Assert.Equal(new TextPosition(3, 1), result.Active);
        Assert.Equal(SelectionDirection.RTL, result.Direction);
        Assert.Equal(new TextPosition(5, 10), selection.Anchor);
    }

    #endregion

    #region Selection equality

    [Fact]
    public void Selection_SelectionsEqual_SameSelection_ReturnsTrue()
    {
        Selection a = new(1, 1, 3, 5);
        Selection b = new(1, 1, 3, 5);
        Assert.True(Selection.SelectionsEqual(a, b));
    }

    [Fact]
    public void Selection_SelectionsEqual_DifferentSelection_ReturnsFalse()
    {
        Selection a = new(1, 1, 3, 5);
        Selection b = new(1, 1, 3, 6);
        Assert.False(Selection.SelectionsEqual(a, b));
    }

    [Fact]
    public void Selection_SelectionsArrEqual_SameArrays_ReturnsTrue()
    {
        Selection[] a = [new(1, 1, 2, 2), new(3, 3, 4, 4)];
        Selection[] b = [new(1, 1, 2, 2), new(3, 3, 4, 4)];
        Assert.True(Selection.SelectionsArrEqual(a, b));
    }

    [Fact]
    public void Selection_SelectionsArrEqual_DifferentLengths_ReturnsFalse()
    {
        Selection[] a = [new(1, 1, 2, 2)];
        Selection[] b = [new(1, 1, 2, 2), new(3, 3, 4, 4)];
        Assert.False(Selection.SelectionsArrEqual(a, b));
    }

    [Fact]
    public void Selection_SelectionsArrEqual_BothNull_ReturnsTrue()
    {
        Assert.True(Selection.SelectionsArrEqual(null, null));
    }

    [Fact]
    public void Selection_SelectionsArrEqual_OneNull_ReturnsFalse()
    {
        Selection[] a = [new(1, 1, 2, 2)];
        Assert.False(Selection.SelectionsArrEqual(a, null));
        Assert.False(Selection.SelectionsArrEqual(null, a));
    }

    [Fact]
    public void Selection_EqualsSelection_Nullable_BothNull_ReturnsTrue()
    {
        Assert.True(Selection.EqualsSelection(null, null));
    }

    [Fact]
    public void Selection_EqualsSelection_Nullable_OneNull_ReturnsFalse()
    {
        Selection a = new(1, 1, 2, 2);
        Assert.False(Selection.EqualsSelection(a, null));
        Assert.False(Selection.EqualsSelection(null, a));
    }

    #endregion

    #region TextPosition extensions

    [Fact]
    public void TextPosition_With_ChangesLineAndColumn()
    {
        TextPosition pos = new(3, 5);
        TextPosition result = pos.With(newLineNumber: 7, newColumn: 10);

        Assert.Equal(7, result.LineNumber);
        Assert.Equal(10, result.Column);
    }

    [Fact]
    public void TextPosition_With_NullParams_KeepsOriginal()
    {
        TextPosition pos = new(3, 5);
        TextPosition result = pos.With();

        Assert.Equal(3, result.LineNumber);
        Assert.Equal(5, result.Column);
        Assert.Equal(pos, result);
    }

    [Fact]
    public void TextPosition_With_PartialChange_KeepsUnchanged()
    {
        TextPosition pos = new(3, 5);

        TextPosition result1 = pos.With(newLineNumber: 7);
        Assert.Equal(7, result1.LineNumber);
        Assert.Equal(5, result1.Column);

        TextPosition result2 = pos.With(newColumn: 10);
        Assert.Equal(3, result2.LineNumber);
        Assert.Equal(10, result2.Column);
    }

    [Fact]
    public void TextPosition_Delta_AppliesDeltas()
    {
        TextPosition pos = new(3, 5);
        TextPosition result = pos.Delta(2, 3);

        Assert.Equal(5, result.LineNumber);
        Assert.Equal(8, result.Column);
    }

    [Fact]
    public void TextPosition_Delta_ClampsToMinimumOne()
    {
        TextPosition pos = new(3, 5);
        TextPosition result = pos.Delta(-10, -10);

        Assert.Equal(1, result.LineNumber);
        Assert.Equal(1, result.Column);
    }

    [Fact]
    public void TextPosition_IsBefore_ReturnsCorrectly()
    {
        TextPosition a = new(1, 5);
        TextPosition b = new(2, 3);
        TextPosition c = new(1, 10);

        Assert.True(a.IsBefore(b));
        Assert.True(a.IsBefore(c));
        Assert.False(b.IsBefore(a));
        Assert.False(a.IsBefore(a)); // Equal positions
    }

    [Fact]
    public void TextPosition_IsBeforeOrEqual_ReturnsCorrectly()
    {
        TextPosition a = new(1, 5);
        TextPosition b = new(2, 3);
        TextPosition c = new(1, 5);

        Assert.True(a.IsBeforeOrEqual(b));
        Assert.True(a.IsBeforeOrEqual(c)); // Equal
        Assert.False(b.IsBeforeOrEqual(a));
    }

    [Fact]
    public void TextPosition_Compare_Static_ReturnsCorrectOrder()
    {
        TextPosition a = new(1, 5);
        TextPosition b = new(2, 3);
        TextPosition c = new(1, 10);

        Assert.True(TextPosition.Compare(a, b) < 0);
        Assert.True(TextPosition.Compare(b, a) > 0);
        Assert.True(TextPosition.Compare(a, c) < 0);
        Assert.Equal(0, TextPosition.Compare(a, a));
    }

    [Fact]
    public void TextPosition_Equals_Static_Nullable_BothNull_ReturnsTrue()
    {
        Assert.True(TextPosition.Equals(null, null));
    }

    [Fact]
    public void TextPosition_Equals_Static_Nullable_OneNull_ReturnsFalse()
    {
        TextPosition a = new(1, 1);
        Assert.False(TextPosition.Equals(a, null));
        Assert.False(TextPosition.Equals(null, a));
    }

    [Fact]
    public void TextPosition_Equals_Static_Nullable_SameValue_ReturnsTrue()
    {
        TextPosition a = new(3, 5);
        TextPosition b = new(3, 5);
        Assert.True(TextPosition.Equals(a, b));
    }

    #endregion

    #region Range sorting / comparison

    [Fact]
    public void Range_CompareRangesUsingStarts_SortsCorrectly()
    {
        Range a = new(1, 1, 2, 5);
        Range b = new(1, 5, 3, 1);
        Range c = new(2, 1, 4, 1);

        Assert.True(Range.CompareRangesUsingStarts(a, b) < 0); // Same line, a starts before
        Assert.True(Range.CompareRangesUsingStarts(a, c) < 0); // a starts on earlier line
        Assert.True(Range.CompareRangesUsingStarts(c, a) > 0);
    }

    [Fact]
    public void Range_CompareRangesUsingStarts_NullHandling()
    {
        Range a = new(1, 1, 2, 5);

        Assert.True(Range.CompareRangesUsingStarts(a, null) > 0); // Non-null > null
        Assert.True(Range.CompareRangesUsingStarts(null, a) < 0); // null < non-null
        Assert.Equal(0, Range.CompareRangesUsingStarts(null, null));
    }

    [Fact]
    public void Range_CompareRangesUsingEnds_SortsCorrectly()
    {
        Range a = new(1, 1, 2, 5);
        Range b = new(1, 1, 2, 10);
        Range c = new(1, 1, 3, 1);

        Assert.True(Range.CompareRangesUsingEnds(a, b) < 0); // Same end line, a ends before
        Assert.True(Range.CompareRangesUsingEnds(a, c) < 0); // a ends on earlier line
        Assert.True(Range.CompareRangesUsingEnds(c, a) > 0);
    }

    [Theory]
    [InlineData(1, 1, 1, 3, 1, 2, 1, 4, -1)]
    [InlineData(1, 1, 1, 3, 1, 1, 1, 4, -1)]
    [InlineData(1, 2, 1, 3, 1, 1, 1, 4, -1)]
    [InlineData(1, 1, 1, 4, 1, 2, 1, 4, -1)]
    [InlineData(1, 1, 1, 4, 1, 1, 1, 4, 0)]
    [InlineData(1, 2, 1, 4, 1, 1, 1, 4, 1)]
    [InlineData(1, 1, 1, 5, 1, 2, 1, 4, 1)]
    [InlineData(1, 1, 2, 4, 1, 1, 1, 4, 1)]
    [InlineData(1, 2, 5, 1, 1, 1, 1, 4, 1)]
    public void Range_CompareRangesUsingEnds_MatchesTypeScriptCases(
        int a1, int a2, int a3, int a4,
        int b1, int b2, int b3, int b4,
        int expectedSign)
    {
        Range a = new(a1, a2, a3, a4);
        Range b = new(b1, b2, b3, b4);
        int comparison = Math.Sign(Range.CompareRangesUsingEnds(a, b));
        Assert.Equal(expectedSign, comparison);
    }

    [Fact]
    public void Range_EqualsRange_NullSafe()
    {
        Range a = new(1, 1, 2, 5);
        Range b = new(1, 1, 2, 5);

        Assert.True(Range.EqualsRange(a, b));
        Assert.True(Range.EqualsRange(null, null));
        Assert.False(Range.EqualsRange(a, null));
        Assert.False(Range.EqualsRange(null, a));
    }

    [Fact]
    public void Range_SpansMultipleLines_ReturnsCorrectly()
    {
        Range single = new(1, 1, 1, 10);
        Range multi = new(1, 1, 2, 5);

        Assert.False(Range.SpansMultipleLines(single));
        Assert.True(Range.SpansMultipleLines(multi));
    }

    #endregion
}
