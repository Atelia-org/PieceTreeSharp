// Source: ts/src/vs/editor/common/diff/rangeMapping.ts
// Tests for LineRangeMapping.Inverse(), LineRangeMapping.Clip(), DiffMove.Flip()
// Tests for RangeMapping.FromEdit(), FromEditJoin(), ToTextEdit()
// Tests for DetailedLineRangeMapping.ToTextEdit()
// Created: 2025-12-02
// Updated: 2025-12-02 (Sprint05-M2-RangeMappingConversion)

using PieceTree.TextBuffer.Diff;

namespace PieceTree.TextBuffer.Tests;

public class RangeMappingTests
{
    #region LineRangeMapping.Inverse Tests

    [Fact]
    public void Inverse_EmptyMappings_ReturnsEntireRange()
    {
        var mapping = Array.Empty<LineRangeMapping>();
        int originalLineCount = 10;
        int modifiedLineCount = 10;

        var result = LineRangeMapping.Inverse(mapping, originalLineCount, modifiedLineCount);

        Assert.Single(result);
        Assert.Equal(1, result[0].Original.StartLineNumber);
        Assert.Equal(11, result[0].Original.EndLineNumberExclusive);
        Assert.Equal(1, result[0].Modified.StartLineNumber);
        Assert.Equal(11, result[0].Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void Inverse_SingleMappingInMiddle_ReturnsTwoUnchangedRegions()
    {
        // Original: lines 1-10, Mapping covers lines 3-5 -> 3-6
        var mapping = new[]
        {
            new LineRangeMapping(new LineRange(3, 5), new LineRange(3, 6))
        };

        var result = LineRangeMapping.Inverse(mapping, 10, 11);

        Assert.Equal(2, result.Count);
        // First unchanged region: lines 1-3
        Assert.Equal(1, result[0].Original.StartLineNumber);
        Assert.Equal(3, result[0].Original.EndLineNumberExclusive);
        Assert.Equal(1, result[0].Modified.StartLineNumber);
        Assert.Equal(3, result[0].Modified.EndLineNumberExclusive);
        // Second unchanged region: lines 5-11 (original), 6-12 (modified)
        Assert.Equal(5, result[1].Original.StartLineNumber);
        Assert.Equal(11, result[1].Original.EndLineNumberExclusive);
        Assert.Equal(6, result[1].Modified.StartLineNumber);
        Assert.Equal(12, result[1].Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void Inverse_MappingAtStart_ReturnsOnlyTrailingRegion()
    {
        // Mapping covers lines 1-3 -> 1-4
        var mapping = new[]
        {
            new LineRangeMapping(new LineRange(1, 3), new LineRange(1, 4))
        };

        var result = LineRangeMapping.Inverse(mapping, 10, 11);

        Assert.Single(result);
        Assert.Equal(3, result[0].Original.StartLineNumber);
        Assert.Equal(11, result[0].Original.EndLineNumberExclusive);
        Assert.Equal(4, result[0].Modified.StartLineNumber);
        Assert.Equal(12, result[0].Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void Inverse_MultipleMappings_ReturnsGapsBetween()
    {
        var mapping = new[]
        {
            new LineRangeMapping(new LineRange(2, 4), new LineRange(2, 4)),
            new LineRangeMapping(new LineRange(6, 8), new LineRange(6, 9)),
        };

        var result = LineRangeMapping.Inverse(mapping, 10, 11);

        Assert.Equal(3, result.Count);
        // Before first change: 1-2
        Assert.Equal(1, result[0].Original.StartLineNumber);
        Assert.Equal(2, result[0].Original.EndLineNumberExclusive);
        // Gap between changes: 4-6
        Assert.Equal(4, result[1].Original.StartLineNumber);
        Assert.Equal(6, result[1].Original.EndLineNumberExclusive);
        // After second change: 8-11
        Assert.Equal(8, result[2].Original.StartLineNumber);
        Assert.Equal(11, result[2].Original.EndLineNumberExclusive);
    }

    #endregion

    #region LineRangeMapping.Clip Tests

    [Fact]
    public void Clip_EmptyMappings_ReturnsEmpty()
    {
        var mapping = Array.Empty<LineRangeMapping>();
        var originalRange = new LineRange(1, 10);
        var modifiedRange = new LineRange(1, 10);

        var result = LineRangeMapping.Clip(mapping, originalRange, modifiedRange);

        Assert.Empty(result);
    }

    [Fact]
    public void Clip_MappingWithinRange_ReturnsClippedMapping()
    {
        var mapping = new[]
        {
            new LineRangeMapping(new LineRange(5, 10), new LineRange(5, 12))
        };
        var originalRange = new LineRange(3, 15);
        var modifiedRange = new LineRange(3, 20);

        var result = LineRangeMapping.Clip(mapping, originalRange, modifiedRange);

        Assert.Single(result);
        Assert.Equal(5, result[0].Original.StartLineNumber);
        Assert.Equal(10, result[0].Original.EndLineNumberExclusive);
        Assert.Equal(5, result[0].Modified.StartLineNumber);
        Assert.Equal(12, result[0].Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void Clip_MappingPartiallyOutside_ReturnsIntersection()
    {
        var mapping = new[]
        {
            new LineRangeMapping(new LineRange(5, 15), new LineRange(5, 20))
        };
        var originalRange = new LineRange(1, 10);
        var modifiedRange = new LineRange(1, 15);

        var result = LineRangeMapping.Clip(mapping, originalRange, modifiedRange);

        Assert.Single(result);
        Assert.Equal(5, result[0].Original.StartLineNumber);
        Assert.Equal(10, result[0].Original.EndLineNumberExclusive);
        Assert.Equal(5, result[0].Modified.StartLineNumber);
        Assert.Equal(15, result[0].Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void Clip_MappingCompletelyOutside_ReturnsEmpty()
    {
        var mapping = new[]
        {
            new LineRangeMapping(new LineRange(20, 30), new LineRange(20, 35))
        };
        var originalRange = new LineRange(1, 10);
        var modifiedRange = new LineRange(1, 15);

        var result = LineRangeMapping.Clip(mapping, originalRange, modifiedRange);

        Assert.Empty(result);
    }

    [Fact]
    public void Clip_MultipleMappings_FiltersAndClips()
    {
        var mapping = new[]
        {
            new LineRangeMapping(new LineRange(2, 5), new LineRange(2, 6)),    // Partially in
            new LineRangeMapping(new LineRange(8, 12), new LineRange(9, 15)),  // Partially in
            new LineRangeMapping(new LineRange(20, 25), new LineRange(25, 30)) // Out of range
        };
        var originalRange = new LineRange(3, 10);
        var modifiedRange = new LineRange(3, 12);

        var result = LineRangeMapping.Clip(mapping, originalRange, modifiedRange);

        Assert.Equal(2, result.Count);
        // First mapping clipped: 3-5 -> 3-6
        Assert.Equal(3, result[0].Original.StartLineNumber);
        Assert.Equal(5, result[0].Original.EndLineNumberExclusive);
        Assert.Equal(3, result[0].Modified.StartLineNumber);
        Assert.Equal(6, result[0].Modified.EndLineNumberExclusive);
        // Second mapping clipped: 8-10 -> 9-12
        Assert.Equal(8, result[1].Original.StartLineNumber);
        Assert.Equal(10, result[1].Original.EndLineNumberExclusive);
        Assert.Equal(9, result[1].Modified.StartLineNumber);
        Assert.Equal(12, result[1].Modified.EndLineNumberExclusive);
    }

    #endregion

    #region DiffMove.Flip Tests

    [Fact]
    public void DiffMove_Flip_SwapsOriginalAndModified()
    {
        var lineRangeMapping = new LineRangeMapping(new LineRange(1, 5), new LineRange(10, 15));
        var changes = Array.Empty<DetailedLineRangeMapping>();
        var move = new DiffMove(lineRangeMapping, changes);

        var flipped = move.Flip();

        Assert.Equal(10, flipped.Original.StartLineNumber);
        Assert.Equal(15, flipped.Original.EndLineNumberExclusive);
        Assert.Equal(1, flipped.Modified.StartLineNumber);
        Assert.Equal(5, flipped.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void DiffMove_Flip_FlipsInnerChanges()
    {
        var lineRangeMapping = new LineRangeMapping(new LineRange(2, 5), new LineRange(3, 6));
        var innerChange = new DetailedLineRangeMapping(
            new LineRange(3, 4),
            new LineRange(4, 5),
            new[]
            {
                new RangeMapping(
                    new PieceTree.TextBuffer.Core.Range(new TextPosition(3, 5), new TextPosition(3, 10)),
                    new PieceTree.TextBuffer.Core.Range(new TextPosition(4, 5), new TextPosition(4, 12)))
            });
        var move = new DiffMove(lineRangeMapping, new[] { innerChange });

        var flipped = move.Flip();

        Assert.Single(flipped.Changes);
        var flippedChange = flipped.Changes[0];
        // Inner change line ranges are flipped
        Assert.Equal(4, flippedChange.Original.StartLineNumber);
        Assert.Equal(5, flippedChange.Original.EndLineNumberExclusive);
        Assert.Equal(3, flippedChange.Modified.StartLineNumber);
        Assert.Equal(4, flippedChange.Modified.EndLineNumberExclusive);
        // Inner RangeMapping is also flipped
        Assert.Single(flippedChange.InnerChanges);
        var flippedInner = flippedChange.InnerChanges[0];
        Assert.Equal(4, flippedInner.OriginalRange.StartLineNumber);
        Assert.Equal(3, flippedInner.ModifiedRange.StartLineNumber);
    }

    [Fact]
    public void DiffMove_Flip_TwiceReturnsOriginal()
    {
        var lineRangeMapping = new LineRangeMapping(new LineRange(5, 10), new LineRange(15, 20));
        var move = new DiffMove(lineRangeMapping, Array.Empty<DetailedLineRangeMapping>());

        var doubleFlipped = move.Flip().Flip();

        Assert.Equal(move.Original.StartLineNumber, doubleFlipped.Original.StartLineNumber);
        Assert.Equal(move.Original.EndLineNumberExclusive, doubleFlipped.Original.EndLineNumberExclusive);
        Assert.Equal(move.Modified.StartLineNumber, doubleFlipped.Modified.StartLineNumber);
        Assert.Equal(move.Modified.EndLineNumberExclusive, doubleFlipped.Modified.EndLineNumberExclusive);
    }

    #endregion

    #region LineRangeMapping.Flip Tests (instance method)

    [Fact]
    public void LineRangeMapping_Flip_SwapsOriginalAndModified()
    {
        var mapping = new LineRangeMapping(new LineRange(1, 5), new LineRange(10, 20));

        var flipped = mapping.Flip();

        Assert.Equal(10, flipped.Original.StartLineNumber);
        Assert.Equal(20, flipped.Original.EndLineNumberExclusive);
        Assert.Equal(1, flipped.Modified.StartLineNumber);
        Assert.Equal(5, flipped.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void RangeMapping_Flip_SwapsOriginalAndModified()
    {
        var mapping = new RangeMapping(
            new PieceTree.TextBuffer.Core.Range(new TextPosition(1, 1), new TextPosition(5, 10)),
            new PieceTree.TextBuffer.Core.Range(new TextPosition(10, 1), new TextPosition(20, 15)));

        var flipped = mapping.Flip();

        Assert.Equal(10, flipped.OriginalRange.StartLineNumber);
        Assert.Equal(1, flipped.ModifiedRange.StartLineNumber);
    }

    #endregion

    #region TextLength Tests

    [Fact]
    public void TextLength_OfText_EmptyString()
    {
        var length = TextLength.OfText("");
        Assert.Equal(0, length.LineCount);
        Assert.Equal(0, length.ColumnCount);
        Assert.True(length.IsZero);
    }

    [Fact]
    public void TextLength_OfText_SingleLineNoNewline()
    {
        var length = TextLength.OfText("hello");
        Assert.Equal(0, length.LineCount);
        Assert.Equal(5, length.ColumnCount);
    }

    [Fact]
    public void TextLength_OfText_SingleLineWithNewline()
    {
        var length = TextLength.OfText("hello\n");
        Assert.Equal(1, length.LineCount);
        Assert.Equal(0, length.ColumnCount);
    }

    [Fact]
    public void TextLength_OfText_MultipleLines()
    {
        var length = TextLength.OfText("hello\nworld\nfoo");
        Assert.Equal(2, length.LineCount);
        Assert.Equal(3, length.ColumnCount); // "foo" = 3 chars
    }

    [Fact]
    public void TextLength_CreateRange_SameLine()
    {
        var length = new TextLength(0, 5);
        var startPos = new TextPosition(3, 10);

        var range = length.CreateRange(startPos);

        Assert.Equal(3, range.StartLineNumber);
        Assert.Equal(10, range.StartColumn);
        Assert.Equal(3, range.EndLineNumber);
        Assert.Equal(15, range.EndColumn);
    }

    [Fact]
    public void TextLength_CreateRange_MultiLine()
    {
        var length = new TextLength(2, 5);
        var startPos = new TextPosition(3, 10);

        var range = length.CreateRange(startPos);

        Assert.Equal(3, range.StartLineNumber);
        Assert.Equal(10, range.StartColumn);
        Assert.Equal(5, range.EndLineNumber);
        Assert.Equal(6, range.EndColumn); // ColumnCount + 1
    }

    [Fact]
    public void TextLength_AddToPosition_SameLine()
    {
        var length = new TextLength(0, 5);
        var pos = new TextPosition(3, 10);

        var result = length.AddToPosition(pos);

        Assert.Equal(3, result.LineNumber);
        Assert.Equal(15, result.Column);
    }

    [Fact]
    public void TextLength_AddToPosition_MultiLine()
    {
        var length = new TextLength(2, 5);
        var pos = new TextPosition(3, 10);

        var result = length.AddToPosition(pos);

        Assert.Equal(5, result.LineNumber);
        Assert.Equal(6, result.Column);
    }

    #endregion

    #region DiffTextEdit Tests

    [Fact]
    public void DiffTextEdit_GetNewRanges_SingleInsertion()
    {
        var edit = DiffTextEdit.Insert(new TextPosition(1, 1), "hello");

        var newRanges = edit.GetNewRanges();

        Assert.Single(newRanges);
        Assert.Equal(1, newRanges[0].StartLineNumber);
        Assert.Equal(1, newRanges[0].StartColumn);
        Assert.Equal(1, newRanges[0].EndLineNumber);
        Assert.Equal(6, newRanges[0].EndColumn);
    }

    [Fact]
    public void DiffTextEdit_GetNewRanges_SingleDeletion()
    {
        var range = new PieceTree.TextBuffer.Core.Range(1, 1, 1, 10);
        var edit = DiffTextEdit.Delete(range);

        var newRanges = edit.GetNewRanges();

        Assert.Single(newRanges);
        // Deletion produces empty range at start position
        Assert.Equal(1, newRanges[0].StartLineNumber);
        Assert.Equal(1, newRanges[0].StartColumn);
        Assert.Equal(1, newRanges[0].EndLineNumber);
        Assert.Equal(1, newRanges[0].EndColumn);
    }

    [Fact]
    public void DiffTextEdit_GetNewRanges_MultiLineInsertion()
    {
        var edit = DiffTextEdit.Insert(new TextPosition(1, 1), "line1\nline2\nline3");

        var newRanges = edit.GetNewRanges();

        Assert.Single(newRanges);
        Assert.Equal(1, newRanges[0].StartLineNumber);
        Assert.Equal(1, newRanges[0].StartColumn);
        Assert.Equal(3, newRanges[0].EndLineNumber);
        Assert.Equal(6, newRanges[0].EndColumn); // "line3" = 5 chars + 1
    }

    [Fact]
    public void DiffTextEdit_GetNewRanges_MultipleReplacements()
    {
        var replacements = new[]
        {
            new TextReplacement(new PieceTree.TextBuffer.Core.Range(1, 1, 1, 4), "XX"),     // "abc" -> "XX"
            new TextReplacement(new PieceTree.TextBuffer.Core.Range(1, 10, 1, 15), "YYYY"), // "defgh" -> "YYYY"
        };
        var edit = new DiffTextEdit(replacements);

        var newRanges = edit.GetNewRanges();

        Assert.Equal(2, newRanges.Count);
        // First replacement: (1,1) -> (1,3)
        Assert.Equal(1, newRanges[0].StartLineNumber);
        Assert.Equal(1, newRanges[0].StartColumn);
        Assert.Equal(1, newRanges[0].EndLineNumber);
        Assert.Equal(3, newRanges[0].EndColumn);
        // Second replacement: offset shifts due to first edit
        // Original: (1,10) -> (1,15), but first edit removed 1 char (3-2=1)
        // New start column: 10 - 1 = 9
        Assert.Equal(1, newRanges[1].StartLineNumber);
        Assert.Equal(9, newRanges[1].StartColumn);
        Assert.Equal(1, newRanges[1].EndLineNumber);
        Assert.Equal(13, newRanges[1].EndColumn);
    }

    #endregion

    #region RangeMapping.FromEdit Tests

    [Fact]
    public void RangeMapping_FromEdit_SingleReplacement()
    {
        var edit = DiffTextEdit.Replace(
            new PieceTree.TextBuffer.Core.Range(1, 5, 1, 10),
            "HELLO"
        );

        var mappings = RangeMapping.FromEdit(edit);

        Assert.Single(mappings);
        // Original range: (1,5) -> (1,10)
        Assert.Equal(1, mappings[0].OriginalRange.StartLineNumber);
        Assert.Equal(5, mappings[0].OriginalRange.StartColumn);
        Assert.Equal(1, mappings[0].OriginalRange.EndLineNumber);
        Assert.Equal(10, mappings[0].OriginalRange.EndColumn);
        // Modified range: (1,5) -> (1,10) - same length "HELLO"
        Assert.Equal(1, mappings[0].ModifiedRange.StartLineNumber);
        Assert.Equal(5, mappings[0].ModifiedRange.StartColumn);
        Assert.Equal(1, mappings[0].ModifiedRange.EndLineNumber);
        Assert.Equal(10, mappings[0].ModifiedRange.EndColumn);
    }

    [Fact]
    public void RangeMapping_FromEdit_InsertionExpandsRange()
    {
        var edit = DiffTextEdit.Replace(
            new PieceTree.TextBuffer.Core.Range(1, 5, 1, 5), // Empty range = insertion
            "inserted"
        );

        var mappings = RangeMapping.FromEdit(edit);

        Assert.Single(mappings);
        // Original range is empty at position
        Assert.True(mappings[0].OriginalRange.IsEmpty);
        // Modified range spans the inserted text
        Assert.Equal(1, mappings[0].ModifiedRange.StartLineNumber);
        Assert.Equal(5, mappings[0].ModifiedRange.StartColumn);
        Assert.Equal(1, mappings[0].ModifiedRange.EndLineNumber);
        Assert.Equal(13, mappings[0].ModifiedRange.EndColumn);
    }

    [Fact]
    public void RangeMapping_FromEdit_MultipleReplacements()
    {
        var replacements = new[]
        {
            new TextReplacement(new PieceTree.TextBuffer.Core.Range(1, 1, 1, 3), "A"),
            new TextReplacement(new PieceTree.TextBuffer.Core.Range(2, 1, 2, 5), "BBBB"),
        };
        var edit = new DiffTextEdit(replacements);

        var mappings = RangeMapping.FromEdit(edit);

        Assert.Equal(2, mappings.Count);
        // First mapping
        Assert.Equal(1, mappings[0].OriginalRange.StartLineNumber);
        Assert.Equal(1, mappings[0].ModifiedRange.StartLineNumber);
        // Second mapping - line number is adjusted
        Assert.Equal(2, mappings[1].OriginalRange.StartLineNumber);
        Assert.Equal(2, mappings[1].ModifiedRange.StartLineNumber);
    }

    #endregion

    #region RangeMapping.FromEditJoin Tests

    [Fact]
    public void RangeMapping_FromEditJoin_SingleReplacement()
    {
        var edit = DiffTextEdit.Replace(
            new PieceTree.TextBuffer.Core.Range(1, 5, 1, 10),
            "HELLO"
        );

        var mapping = RangeMapping.FromEditJoin(edit);

        Assert.Equal(1, mapping.OriginalRange.StartLineNumber);
        Assert.Equal(5, mapping.OriginalRange.StartColumn);
        Assert.Equal(1, mapping.OriginalRange.EndLineNumber);
        Assert.Equal(10, mapping.OriginalRange.EndColumn);
    }

    [Fact]
    public void RangeMapping_FromEditJoin_MultipleReplacementsJoined()
    {
        var replacements = new[]
        {
            new TextReplacement(new PieceTree.TextBuffer.Core.Range(1, 1, 1, 5), "AA"),
            new TextReplacement(new PieceTree.TextBuffer.Core.Range(1, 10, 1, 15), "BB"),
        };
        var edit = new DiffTextEdit(replacements);

        var mapping = RangeMapping.FromEditJoin(edit);

        // Joined original: (1,1) to (1,15)
        Assert.Equal(1, mapping.OriginalRange.StartLineNumber);
        Assert.Equal(1, mapping.OriginalRange.StartColumn);
        Assert.Equal(1, mapping.OriginalRange.EndLineNumber);
        Assert.Equal(15, mapping.OriginalRange.EndColumn);
        // Joined modified: (1,1) to (1,9) due to offset calculations
        Assert.Equal(1, mapping.ModifiedRange.StartLineNumber);
        Assert.Equal(1, mapping.ModifiedRange.StartColumn);
    }

    [Fact]
    public void RangeMapping_FromEditJoin_ThrowsOnEmptyEdit()
    {
        var edit = new DiffTextEdit(Array.Empty<TextReplacement>());

        Assert.Throws<InvalidOperationException>(() => RangeMapping.FromEditJoin(edit));
    }

    #endregion

    #region RangeMapping.ToTextEdit Tests

    [Fact]
    public void RangeMapping_ToTextEdit_CreatesReplacement()
    {
        var mapping = new RangeMapping(
            new PieceTree.TextBuffer.Core.Range(1, 1, 1, 5),
            new PieceTree.TextBuffer.Core.Range(1, 1, 1, 10)
        );

        // Mock getValueOfRange - returns "NEW_TEXT" for the modified range
        string GetValueOfRange(PieceTree.TextBuffer.Core.Range range) => "NEW_TEXT";

        var replacement = mapping.ToTextEdit(GetValueOfRange);

        Assert.Equal(1, replacement.Range.StartLineNumber);
        Assert.Equal(1, replacement.Range.StartColumn);
        Assert.Equal(1, replacement.Range.EndLineNumber);
        Assert.Equal(5, replacement.Range.EndColumn);
        Assert.Equal("NEW_TEXT", replacement.Text);
    }

    #endregion

    #region DetailedLineRangeMapping.ToTextEdit Tests

    [Fact]
    public void DetailedLineRangeMapping_ToTextEdit_EmptyMappings()
    {
        var mappings = Array.Empty<DetailedLineRangeMapping>();

        string GetValueOfRange(PieceTree.TextBuffer.Core.Range range) => "";

        var edit = DetailedLineRangeMapping.ToTextEdit(mappings, GetValueOfRange);

        Assert.True(edit.IsEmpty);
    }

    [Fact]
    public void DetailedLineRangeMapping_ToTextEdit_SingleMappingWithInnerChanges()
    {
        var innerChanges = new[]
        {
            new RangeMapping(
                new PieceTree.TextBuffer.Core.Range(1, 1, 1, 5),
                new PieceTree.TextBuffer.Core.Range(1, 1, 1, 8)
            ),
            new RangeMapping(
                new PieceTree.TextBuffer.Core.Range(1, 10, 1, 12),
                new PieceTree.TextBuffer.Core.Range(1, 13, 1, 15)
            ),
        };
        var mapping = new DetailedLineRangeMapping(
            new LineRange(1, 2),
            new LineRange(1, 2),
            innerChanges
        );

        // Track which ranges were requested
        var requestedRanges = new List<PieceTree.TextBuffer.Core.Range>();
        string GetValueOfRange(PieceTree.TextBuffer.Core.Range range)
        {
            requestedRanges.Add(range);
            return $"text_{range.StartColumn}";
        }

        var edit = DetailedLineRangeMapping.ToTextEdit(new[] { mapping }, GetValueOfRange);

        Assert.Equal(2, edit.Replacements.Count);
        // First inner change becomes first replacement
        Assert.Equal(1, edit.Replacements[0].Range.StartColumn);
        Assert.Equal(5, edit.Replacements[0].Range.EndColumn);
        Assert.Equal("text_1", edit.Replacements[0].Text);
        // Second inner change becomes second replacement
        Assert.Equal(10, edit.Replacements[1].Range.StartColumn);
        Assert.Equal("text_13", edit.Replacements[1].Text);
    }

    [Fact]
    public void DetailedLineRangeMapping_ToTextEdit_MultipleMappings()
    {
        var mapping1 = new DetailedLineRangeMapping(
            new LineRange(1, 2),
            new LineRange(1, 3),
            new[]
            {
                new RangeMapping(
                    new PieceTree.TextBuffer.Core.Range(1, 1, 1, 5),
                    new PieceTree.TextBuffer.Core.Range(1, 1, 2, 3)
                ),
            }
        );
        var mapping2 = new DetailedLineRangeMapping(
            new LineRange(5, 6),
            new LineRange(6, 7),
            new[]
            {
                new RangeMapping(
                    new PieceTree.TextBuffer.Core.Range(5, 1, 5, 10),
                    new PieceTree.TextBuffer.Core.Range(6, 1, 6, 5)
                ),
            }
        );

        string GetValueOfRange(PieceTree.TextBuffer.Core.Range range) => "replaced";

        var edit = DetailedLineRangeMapping.ToTextEdit(new[] { mapping1, mapping2 }, GetValueOfRange);

        Assert.Equal(2, edit.Replacements.Count);
        // First replacement from mapping1
        Assert.Equal(1, edit.Replacements[0].Range.StartLineNumber);
        // Second replacement from mapping2
        Assert.Equal(5, edit.Replacements[1].Range.StartLineNumber);
    }

    #endregion
}
