// Source: ts/src/vs/editor/common/diff/rangeMapping.ts
// - Class: RangeMapping (Lines: 243-320)
// - Class: LineRangeMapping (Lines: 19-195)
// - Class: DetailedLineRangeMapping (Lines: 196-240)
// - Function: lineRangeMappingFromRangeMappings (Lines: 322-395)
// - Static: LineRangeMapping.inverse() (Lines: 19-45)
// - Static: LineRangeMapping.clip() (Lines: 47-57)
// - Static: RangeMapping.fromEdit() (Lines: 243-250)
// - Static: RangeMapping.fromEditJoin() (Lines: 252-257)
// - Instance: RangeMapping.toTextEdit() (Lines: 313-320)
// - Static: DetailedLineRangeMapping.toTextEdit() (Lines: 196-205)
// Ported: 2025-11-19
// Updated: 2025-12-02 (Added Inverse, Clip, FromEdit, FromEditJoin, ToTextEdit)

using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Diff;

public sealed class RangeMapping
{
    /// <summary>
    /// Creates RangeMappings from a DiffTextEdit.
    /// Each replacement in the edit produces a mapping from original range to the new range after edit.
    /// </summary>
    /// <remarks>
    /// TS Source: ts/src/vs/editor/common/diff/rangeMapping.ts
    /// RangeMapping.fromEdit() (Lines 243-250)
    /// </remarks>
    public static IReadOnlyList<RangeMapping> FromEdit(DiffTextEdit edit)
    {
        var newRanges = edit.GetNewRanges();
        var result = new List<RangeMapping>(edit.Replacements.Count);
        for (int i = 0; i < edit.Replacements.Count; i++)
        {
            result.Add(new RangeMapping(edit.Replacements[i].Range, newRanges[i]));
        }
        return result;
    }

    /// <summary>
    /// Creates a single joined RangeMapping from a DiffTextEdit.
    /// </summary>
    /// <remarks>
    /// TS Source: ts/src/vs/editor/common/diff/rangeMapping.ts
    /// RangeMapping.fromEditJoin() (Lines 252-257)
    /// </remarks>
    public static RangeMapping FromEditJoin(DiffTextEdit edit)
    {
        var mappings = FromEdit(edit);
        return Join(mappings);
    }

    /// <summary>
    /// Joins multiple RangeMappings into one.
    /// </summary>
    /// <remarks>
    /// TS Source: ts/src/vs/editor/common/diff/rangeMapping.ts
    /// RangeMapping.join() (Lines 259-268)
    /// </remarks>
    public static RangeMapping Join(IReadOnlyList<RangeMapping> rangeMappings)
    {
        if (rangeMappings.Count == 0)
        {
            throw new InvalidOperationException("Cannot join an empty list of range mappings");
        }
        var result = rangeMappings[0];
        for (int i = 1; i < rangeMappings.Count; i++)
        {
            result = result.Join(rangeMappings[i]);
        }
        return result;
    }

    /// <summary>
    /// Asserts that the range mappings are sorted.
    /// </summary>
    public static void AssertSorted(IReadOnlyList<RangeMapping> rangeMappings)
    {
        for (int i = 1; i < rangeMappings.Count; i++)
        {
            var previous = rangeMappings[i - 1];
            var current = rangeMappings[i];
            if (!(previous.OriginalRange.GetEndPosition() <= current.OriginalRange.GetStartPosition()
                && previous.ModifiedRange.GetEndPosition() <= current.ModifiedRange.GetStartPosition()))
            {
                throw new InvalidOperationException("Range mappings must be sorted");
            }
        }
    }

    public RangeMapping(Range originalRange, Range modifiedRange)
    {
        OriginalRange = originalRange;
        ModifiedRange = modifiedRange;
    }

    public Range OriginalRange { get; }
    public Range ModifiedRange { get; }

    public RangeMapping Flip() => new(ModifiedRange, OriginalRange);

    public RangeMapping Join(RangeMapping other)
    {
        return new RangeMapping(PlusRange(OriginalRange, other.OriginalRange), PlusRange(ModifiedRange, other.ModifiedRange));
    }

    /// <summary>
    /// Creates a TextReplacement that transforms the original to the modified text.
    /// </summary>
    /// <remarks>
    /// TS Source: ts/src/vs/editor/common/diff/rangeMapping.ts
    /// RangeMapping.toTextEdit() (Lines 313-320)
    /// </remarks>
    public TextReplacement ToTextEdit(Func<Range, string> getValueOfRange)
    {
        var newText = getValueOfRange(ModifiedRange);
        return new TextReplacement(OriginalRange, newText);
    }

    public override string ToString() => $"{{{OriginalRange}->{ModifiedRange}}}";

    private static Range PlusRange(Range left, Range right)
    {
        TextPosition start = left.Start <= right.Start ? left.Start : right.Start;
        TextPosition end = left.End >= right.End ? left.End : right.End;
        return new Range(start, end);
    }
}

public class LineRangeMapping
{
    /// <summary>
    /// Returns the inverse of the given mappings, representing the unchanged regions.
    /// </summary>
    /// <remarks>
    /// TS Source: ts/src/vs/editor/common/diff/rangeMapping.ts
    /// LineRangeMapping.inverse() (Lines 19-45)
    /// </remarks>
    public static IReadOnlyList<LineRangeMapping> Inverse(
        IReadOnlyList<LineRangeMapping> mapping,
        int originalLineCount,
        int modifiedLineCount)
    {
        var result = new List<LineRangeMapping>();
        int lastOriginalEndLineNumber = 1;
        int lastModifiedEndLineNumber = 1;

        foreach (var m in mapping)
        {
            var r = new LineRangeMapping(
                new LineRange(lastOriginalEndLineNumber, m.Original.StartLineNumber),
                new LineRange(lastModifiedEndLineNumber, m.Modified.StartLineNumber)
            );
            if (!r.Modified.IsEmpty)
            {
                result.Add(r);
            }
            lastOriginalEndLineNumber = m.Original.EndLineNumberExclusive;
            lastModifiedEndLineNumber = m.Modified.EndLineNumberExclusive;
        }

        var final = new LineRangeMapping(
            new LineRange(lastOriginalEndLineNumber, originalLineCount + 1),
            new LineRange(lastModifiedEndLineNumber, modifiedLineCount + 1)
        );
        if (!final.Modified.IsEmpty)
        {
            result.Add(final);
        }

        return result;
    }

    /// <summary>
    /// Clips the mappings to the specified original and modified ranges.
    /// </summary>
    /// <remarks>
    /// TS Source: ts/src/vs/editor/common/diff/rangeMapping.ts
    /// LineRangeMapping.clip() (Lines 47-57)
    /// </remarks>
    public static IReadOnlyList<LineRangeMapping> Clip(
        IReadOnlyList<LineRangeMapping> mapping,
        LineRange originalRange,
        LineRange modifiedRange)
    {
        var result = new List<LineRangeMapping>();
        foreach (var m in mapping)
        {
            var original = m.Original.Intersect(originalRange);
            var modified = m.Modified.Intersect(modifiedRange);
            if (original is LineRange orig && !orig.IsEmpty && modified is LineRange mod && !mod.IsEmpty)
            {
                result.Add(new LineRangeMapping(orig, mod));
            }
        }
        return result;
    }

    public LineRangeMapping(LineRange original, LineRange modified)
    {
        Original = original;
        Modified = modified;
    }

    public LineRange Original { get; }
    public LineRange Modified { get; }

    public virtual LineRangeMapping Flip() => new(Modified, Original);

    public LineRangeMapping Join(LineRangeMapping other)
    {
        return new LineRangeMapping(Original.Join(other.Original), Modified.Join(other.Modified));
    }

    public virtual RangeMapping? ToRangeMapping()
    {
        Range? orig = Original.ToInclusiveRange();
        Range? mod = Modified.ToInclusiveRange();
        if (orig.HasValue && mod.HasValue)
        {
            return new RangeMapping(orig.Value, mod.Value);
        }

        if (Original.StartLineNumber == 1 || Modified.StartLineNumber == 1)
        {
            if (!(Original.StartLineNumber == 1 && Modified.StartLineNumber == 1))
            {
                throw new InvalidOperationException("Invalid diff mapping");
            }

            return new RangeMapping(
                new Range(new TextPosition(Original.StartLineNumber, 1), new TextPosition(Original.EndLineNumberExclusive, 1)),
                new Range(new TextPosition(Modified.StartLineNumber, 1), new TextPosition(Modified.EndLineNumberExclusive, 1)));
        }

        return new RangeMapping(
            new Range(new TextPosition(Original.StartLineNumber - 1, int.MaxValue), new TextPosition(Original.EndLineNumberExclusive - 1, int.MaxValue)),
            new Range(new TextPosition(Modified.StartLineNumber - 1, int.MaxValue), new TextPosition(Modified.EndLineNumberExclusive - 1, int.MaxValue)));
    }

    public RangeMapping ToRangeMapping(string[] originalLines, string[] modifiedLines)
    {
        return ToRangeMapping2(originalLines, modifiedLines);
    }

    public RangeMapping ToRangeMapping2(string[] original, string[] modified)
    {
        if (IsValidLineNumber(Original.EndLineNumberExclusive, original) && IsValidLineNumber(Modified.EndLineNumberExclusive, modified))
        {
            return new RangeMapping(
                new Range(new TextPosition(Original.StartLineNumber, 1), new TextPosition(Original.EndLineNumberExclusive, 1)),
                new Range(new TextPosition(Modified.StartLineNumber, 1), new TextPosition(Modified.EndLineNumberExclusive, 1)));
        }

        if (!Original.IsEmpty && !Modified.IsEmpty)
        {
            return new RangeMapping(
                Range.FromPositions(new TextPosition(Original.StartLineNumber, 1), NormalizePosition(new TextPosition(Original.EndLineNumberExclusive - 1, int.MaxValue), original)),
                Range.FromPositions(new TextPosition(Modified.StartLineNumber, 1), NormalizePosition(new TextPosition(Modified.EndLineNumberExclusive - 1, int.MaxValue), modified))
            );
        }

        if (Original.StartLineNumber > 1 && Modified.StartLineNumber > 1)
        {
            return new RangeMapping(
                Range.FromPositions(NormalizePosition(new TextPosition(Original.StartLineNumber - 1, int.MaxValue), original), NormalizePosition(new TextPosition(Original.EndLineNumberExclusive - 1, int.MaxValue), original)),
                Range.FromPositions(NormalizePosition(new TextPosition(Modified.StartLineNumber - 1, int.MaxValue), modified), NormalizePosition(new TextPosition(Modified.EndLineNumberExclusive - 1, int.MaxValue), modified))
            );
        }

        throw new InvalidOperationException("Unexpected diff mapping");
    }

    private static bool IsValidLineNumber(int lineNumber, IReadOnlyList<string> lines)
    {
        return lineNumber >= 1 && lineNumber <= lines.Count;
    }

    private static Range NormalizeRange(LineRange range, string[] lines)
    {
        TextPosition start = NormalizePosition(new TextPosition(range.StartLineNumber, 1), lines);
        TextPosition end = NormalizePosition(new TextPosition(range.EndLineNumberExclusive - 1, int.MaxValue), lines);
        return Range.FromPositions(start, end);
    }

    protected static TextPosition NormalizePosition(TextPosition position, IReadOnlyList<string> lines)
    {
        int lineNumber = Math.Clamp(position.LineNumber, 1, Math.Max(1, lines.Count));
        string line = lines[Math.Max(1, Math.Min(lineNumber, lines.Count)) - 1];
        int column = Math.Clamp(position.Column, 1, line.Length + 1);
        return new TextPosition(lineNumber, column);
    }
}

public sealed class DetailedLineRangeMapping : LineRangeMapping
{
    /// <summary>
    /// Converts a sequence of DetailedLineRangeMappings to a DiffTextEdit.
    /// </summary>
    /// <remarks>
    /// TS Source: ts/src/vs/editor/common/diff/rangeMapping.ts
    /// DetailedLineRangeMapping.toTextEdit() (Lines 196-205)
    /// </remarks>
    public static DiffTextEdit ToTextEdit(IReadOnlyList<DetailedLineRangeMapping> mapping, Func<Range, string> getValueOfRange)
    {
        var replacements = new List<TextReplacement>();
        foreach (var m in mapping)
        {
            foreach (var r in m.InnerChanges)
            {
                var replacement = r.ToTextEdit(getValueOfRange);
                replacements.Add(replacement);
            }
        }
        return new DiffTextEdit(replacements);
    }

    public DetailedLineRangeMapping(LineRange original, LineRange modified, IReadOnlyList<RangeMapping>? innerChanges)
        : base(original, modified)
    {
        InnerChanges = innerChanges ?? Array.Empty<RangeMapping>();
    }

    public IReadOnlyList<RangeMapping> InnerChanges { get; }

    public override LineRangeMapping Flip()
    {
        return new DetailedLineRangeMapping(Modified, Original, InnerChanges.Select(c => c.Flip()).ToArray());
    }

    public DetailedLineRangeMapping WithInnerChangesFromLineRanges()
    {
        RangeMapping? mapping = ToRangeMapping();
        return new DetailedLineRangeMapping(Original, Modified, mapping == null ? Array.Empty<RangeMapping>() : [mapping]);
    }
}

internal static class LineRangeMappingBuilder
{
    public static IReadOnlyList<DetailedLineRangeMapping> FromRangeMappings(
        IReadOnlyList<RangeMapping> alignments,
        string[] originalLines,
        string[] modifiedLines,
        bool dontAssertStartLine = false)
    {
        List<DetailedLineRangeMapping> mappings = alignments.Select(a => GetLineRangeMapping(a, originalLines, modifiedLines)).ToList();
        IEnumerable<List<DetailedLineRangeMapping>> groups = GroupAdjacentBy(mappings, (a1, a2) => a1.Original.IntersectsOrTouches(a2.Original) || a1.Modified.IntersectsOrTouches(a2.Modified));

        List<DetailedLineRangeMapping> changes = [];
        foreach (List<DetailedLineRangeMapping> group in groups)
        {
            DetailedLineRangeMapping first = group[0];
            DetailedLineRangeMapping last = group[^1];
            RangeMapping[] inner = group.SelectMany(g => g.InnerChanges).ToArray();
            changes.Add(new DetailedLineRangeMapping(first.Original.Join(last.Original), first.Modified.Join(last.Modified), inner));
        }

        return changes;
    }

    public static DetailedLineRangeMapping GetLineRangeMapping(RangeMapping mapping, string[] originalLines, string[] modifiedLines)
    {
        int lineStartDelta = 0;
        int lineEndDelta = 0;
        Range originalRange = mapping.OriginalRange;
        Range modifiedRange = mapping.ModifiedRange;

        if (modifiedRange.EndColumn == 1 && originalRange.EndColumn == 1 && originalRange.StartLineNumber + lineStartDelta <= originalRange.EndLineNumber && modifiedRange.StartLineNumber + lineStartDelta <= modifiedRange.EndLineNumber)
        {
            lineEndDelta = -1;
        }

        if (modifiedRange.StartColumn - 1 >= GetLineLength(modifiedLines, modifiedRange.StartLineNumber)
            && originalRange.StartColumn - 1 >= GetLineLength(originalLines, originalRange.StartLineNumber)
            && originalRange.StartLineNumber <= originalRange.EndLineNumber + lineEndDelta
            && modifiedRange.StartLineNumber <= modifiedRange.EndLineNumber + lineEndDelta)
        {
            lineStartDelta = 1;
        }

        LineRange originalLineRange = new(originalRange.StartLineNumber + lineStartDelta, originalRange.EndLineNumber + 1 + lineEndDelta);
        LineRange modifiedLineRange = new(modifiedRange.StartLineNumber + lineStartDelta, modifiedRange.EndLineNumber + 1 + lineEndDelta);

        return new DetailedLineRangeMapping(originalLineRange, modifiedLineRange, new[] { mapping });
    }

    private static int GetLineLength(string[] lines, int lineNumber)
    {
        if (lines.Length == 0)
        {
            return 0;
        }

        lineNumber = Math.Clamp(lineNumber, 1, lines.Length);
        return lines[lineNumber - 1].Length;
    }

    private static IEnumerable<List<T>> GroupAdjacentBy<T>(IReadOnlyList<T> items, Func<T, T, bool> predicate)
    {
        if (items.Count == 0)
        {
            yield break;
        }

        List<T> current = [items[0]];
        for (int i = 1; i < items.Count; i++)
        {
            T? item = items[i];
            if (predicate(current[^1], item))
            {
                current.Add(item);
            }
            else
            {
                yield return current;
                current = [item];
            }
        }

        yield return current;
    }
}
