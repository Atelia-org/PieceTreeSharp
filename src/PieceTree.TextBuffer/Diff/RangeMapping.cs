// Source: ts/src/vs/editor/common/diff/rangeMapping.ts
// - Class: RangeMapping (Lines: 243-320)
// - Class: LineRangeMapping (Lines: 19-195)
// - Class: DetailedLineRangeMapping (Lines: 196-240)
// - Function: lineRangeMappingFromRangeMappings (Lines: 322-395)
// Ported: 2025-11-19

using System;
using System.Collections.Generic;
using System.Linq;
using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Diff;

public sealed class RangeMapping
{
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

    public override string ToString() => $"{{{OriginalRange}->{ModifiedRange}}}";

    private static Range PlusRange(Range left, Range right)
    {
        var start = left.Start <= right.Start ? left.Start : right.Start;
        var end = left.End >= right.End ? left.End : right.End;
        return new Range(start, end);
    }
}

public class LineRangeMapping
{
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
        var orig = Original.ToInclusiveRange();
        var mod = Modified.ToInclusiveRange();
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
        var start = NormalizePosition(new TextPosition(range.StartLineNumber, 1), lines);
        var end = NormalizePosition(new TextPosition(range.EndLineNumberExclusive - 1, int.MaxValue), lines);
        return Range.FromPositions(start, end);
    }

    protected static TextPosition NormalizePosition(TextPosition position, IReadOnlyList<string> lines)
    {
        var lineNumber = Math.Clamp(position.LineNumber, 1, Math.Max(1, lines.Count));
        var line = lines[Math.Max(1, Math.Min(lineNumber, lines.Count)) - 1];
        var column = Math.Clamp(position.Column, 1, line.Length + 1);
        return new TextPosition(lineNumber, column);
    }
}

public sealed class DetailedLineRangeMapping : LineRangeMapping
{
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
        var mapping = ToRangeMapping();
        return new DetailedLineRangeMapping(Original, Modified, mapping == null ? Array.Empty<RangeMapping>() : new[] { mapping });
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
        var mappings = alignments.Select(a => GetLineRangeMapping(a, originalLines, modifiedLines)).ToList();
        var groups = GroupAdjacentBy(mappings, (a1, a2) => a1.Original.IntersectsOrTouches(a2.Original) || a1.Modified.IntersectsOrTouches(a2.Modified));

        var changes = new List<DetailedLineRangeMapping>();
        foreach (var group in groups)
        {
            var first = group[0];
            var last = group[^1];
            var inner = group.SelectMany(g => g.InnerChanges).ToArray();
            changes.Add(new DetailedLineRangeMapping(first.Original.Join(last.Original), first.Modified.Join(last.Modified), inner));
        }

        return changes;
    }

    public static DetailedLineRangeMapping GetLineRangeMapping(RangeMapping mapping, string[] originalLines, string[] modifiedLines)
    {
        var lineStartDelta = 0;
        var lineEndDelta = 0;
        var originalRange = mapping.OriginalRange;
        var modifiedRange = mapping.ModifiedRange;

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

        var originalLineRange = new LineRange(originalRange.StartLineNumber + lineStartDelta, originalRange.EndLineNumber + 1 + lineEndDelta);
        var modifiedLineRange = new LineRange(modifiedRange.StartLineNumber + lineStartDelta, modifiedRange.EndLineNumber + 1 + lineEndDelta);

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

        var current = new List<T> { items[0] };
        for (var i = 1; i < items.Count; i++)
        {
            var item = items[i];
            if (predicate(current[^1], item))
            {
                current.Add(item);
            }
            else
            {
                yield return current;
                current = new List<T> { item };
            }
        }

        yield return current;
    }
}
