// Source: ts/src/vs/editor/common/core/edits/textEdit.ts
// - Class: TextReplacement (Lines: 220-320)
// - Class: TextEdit (Lines: 5-220)
// Ported: 2025-12-02 (Sprint05-M2-RangeMappingConversion)

using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Diff;

/// <summary>
/// Represents a single text replacement operation.
/// Corresponds to TextReplacement in TS rangeMapping.ts.
/// </summary>
public sealed class TextReplacement
{
    public Range Range { get; }
    public string Text { get; }

    public TextReplacement(Range range, string text)
    {
        Range = range;
        Text = text;
    }

    /// <summary>
    /// Returns true if this replacement is empty (empty range and no text).
    /// </summary>
    public bool IsEmpty => Range.IsEmpty && Text.Length == 0;

    /// <summary>
    /// Creates a deletion replacement.
    /// </summary>
    public static TextReplacement Delete(Range range) => new(range, string.Empty);

    /// <summary>
    /// Creates an insertion replacement at the given position.
    /// </summary>
    public static TextReplacement Insert(TextPosition position, string text)
        => new(Range.FromPositions(position, position), text);

    public bool Equals(TextReplacement other)
    {
        return Range.EqualsRange(other.Range) && Text == other.Text;
    }

    public override bool Equals(object? obj) => obj is TextReplacement other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Range, Text);

    public override string ToString()
    {
        var start = Range.GetStartPosition();
        var end = Range.GetEndPosition();
        return $"({start.LineNumber},{start.Column} -> {end.LineNumber},{end.Column}): \"{Text}\"";
    }
}

/// <summary>
/// Represents a sequence of non-overlapping, sorted text replacements.
/// Corresponds to TextEdit in TS textEdit.ts.
/// </summary>
/// <remarks>
/// This is different from the simple TextEdit struct in TextModel.cs.
/// This class handles multiple replacements and range calculations needed for diff operations.
/// </remarks>
public sealed class DiffTextEdit
{
    public IReadOnlyList<TextReplacement> Replacements { get; }

    public DiffTextEdit(IReadOnlyList<TextReplacement> replacements)
    {
        Replacements = replacements;
    }

    /// <summary>
    /// Creates a DiffTextEdit from a single replacement.
    /// </summary>
    public static DiffTextEdit Replace(Range originalRange, string newText)
        => new([new TextReplacement(originalRange, newText)]);

    /// <summary>
    /// Creates a DiffTextEdit that deletes a range.
    /// </summary>
    public static DiffTextEdit Delete(Range range)
        => new([TextReplacement.Delete(range)]);

    /// <summary>
    /// Creates a DiffTextEdit that inserts text at a position.
    /// </summary>
    public static DiffTextEdit Insert(TextPosition position, string newText)
        => new([TextReplacement.Insert(position, newText)]);

    /// <summary>
    /// Returns true if this edit is empty (no replacements).
    /// </summary>
    public bool IsEmpty => Replacements.Count == 0;

    /// <summary>
    /// Computes the new ranges that the replacements will occupy after the edit is applied.
    /// </summary>
    /// <remarks>
    /// TS Source: ts/src/vs/editor/common/core/edits/textEdit.ts
    /// TextEdit.getNewRanges() (Lines 160-175)
    /// </remarks>
    public IReadOnlyList<Range> GetNewRanges()
    {
        var newRanges = new List<Range>();
        int previousEditEndLineNumber = 0;
        int lineOffset = 0;
        int columnOffset = 0;

        foreach (var replacement in Replacements)
        {
            var textLength = TextLength.OfText(replacement.Text);
            var newRangeStart = new TextPosition(
                replacement.Range.StartLineNumber + lineOffset,
                replacement.Range.StartColumn + (replacement.Range.StartLineNumber == previousEditEndLineNumber ? columnOffset : 0)
            );
            var newRange = textLength.CreateRange(newRangeStart);
            newRanges.Add(newRange);

            lineOffset = newRange.EndLineNumber - replacement.Range.EndLineNumber;
            columnOffset = newRange.EndColumn - replacement.Range.EndColumn;
            previousEditEndLineNumber = replacement.Range.EndLineNumber;
        }

        return newRanges;
    }

    /// <summary>
    /// Applies the edit to a text provider and returns the result.
    /// </summary>
    public string Apply(Func<Range, string> getValueOfRange, TextPosition endPosition)
    {
        var result = new System.Text.StringBuilder();
        var lastEditEnd = new TextPosition(1, 1);

        foreach (var replacement in Replacements)
        {
            var editStart = replacement.Range.GetStartPosition();
            var editEnd = replacement.Range.GetEndPosition();

            var r = Range.FromPositions(lastEditEnd, editStart);
            if (!r.IsEmpty)
            {
                result.Append(getValueOfRange(r));
            }
            result.Append(replacement.Text);
            lastEditEnd = editEnd;
        }

        var finalRange = Range.FromPositions(lastEditEnd, endPosition);
        if (!finalRange.IsEmpty)
        {
            result.Append(getValueOfRange(finalRange));
        }

        return result.ToString();
    }

    public override string ToString()
    {
        return string.Join("\n", Replacements.Select(r => r.ToString()));
    }
}
