/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Test harness adapting TS withTestCodeEditor for C# FindModel tests
// Reference: ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts (L34-57)

using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.DocUI;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests.DocUI;

/// <summary>
/// Test harness that adapts TS withTestCodeEditor functionality for C# FindModel tests.
/// Provides a disposable context with TextModel, FindReplaceState, and FindModel.
/// </summary>
public class TestEditorContext : IDisposable
{
    public TextModel Model { get; }
    public FindReplaceState State { get; }
    public FindModel FindModel { get; }
    private Range[] _selections;

    private TestEditorContext(string[] lines, TestEditorContextOptions? options)
    {
        // Create TextModel (join lines with \n)
        // TS behavior: ['line1', 'line2', ''].join('\n') => 'line1\nline2\n'
        // C# string.Join: string.Join("\n", ['line1', 'line2', '']) => 'line1\nline2\n'
        // Both behave the same! The key is that if the last element is empty string,
        // join() will add the separator before it, creating a trailing newline.
        string text = string.Join("\n", lines);
        Model = new TextModel(text);

        // Create FindReplaceState
        State = new FindReplaceState();
        _selections = Array.Empty<Range>();

        // Create FindModel (binds to Model and State)
        string? configuredWordSeparators = options?.WordSeparators;
        Func<double?>? viewportHeightProvider = null;
        if (options?.ViewportHeightPx.HasValue == true)
        {
            double value = options.ViewportHeightPx.Value;
            viewportHeightProvider = () => value;
        }

        FindModel = new FindModel(Model, State, () => configuredWordSeparators, viewportHeightProvider);

        if (options?.InitialSelections is { Length: > 0 } initialSelections)
        {
            SetSelections(initialSelections);
        }
        else
        {
            SetSelections(new Range(new TextPosition(1, 1), new TextPosition(1, 1)));
        }
    }

    /// <summary>
    /// Runs a test with a disposable TestEditorContext.
    /// Mimics TS withTestCodeEditor pattern.
    /// </summary>
    public static void RunTest(string[] lines, Action<TestEditorContext> callback, TestEditorContextOptions? options = null)
    {
        using TestEditorContext ctx = new(lines, options);
        callback(ctx);
    }

    /// <summary>
    /// Sets the cursor position (simulates editor selection).
    /// </summary>
    public void SetPosition(int lineNumber, int column)
    {
        TextPosition position = new(lineNumber, column);
        SetSelections(new Range(position, position));
    }

    /// <summary>
    /// Sets the editor selections, mimicking VS Code's multi-cursor behavior
    /// (first selection becomes primary).
    /// </summary>
    public void SetSelections(params Range[] selections)
    {
        Range[] sanitized;
        if (selections == null || selections.Length == 0)
        {
            sanitized = [new Range(new TextPosition(1, 1), new TextPosition(1, 1))];
        }
        else
        {
            sanitized = CloneRanges(selections);
        }

        _selections = sanitized;
        FindModel.SetSelections(_selections, primaryIndex: 0);
    }

    /// <summary>
    /// Returns a clone of the current selection array for use in tests (e.g., search scopes).
    /// </summary>
    public Range[] GetSelections()
    {
        return CloneRanges(_selections);
    }

    /// <summary>
    /// Gets the current selection as a range (based on current match or cursor position).
    /// </summary>
    public Range GetSelection()
    {
        return FindModel.GetSelection();
    }

    /// <summary>
    /// Gets the find state (current match and all matches) from decorations.
    /// Mimics TS _getFindState function.
    /// </summary>
    public FindDecorationsState GetFindState()
    {
        List<Range> currentFindMatches = [];
        List<Range> allFindMatches = [];

        // Query all decorations from the model (entire text range)
        // Use [0, textLength] to include decorations at text end (e.g., line 12 at offset 254)
        TextRange textRange = new(0, Model.GetLength() + 1);
        IReadOnlyList<ModelDecoration> allDecorations = Model.GetDecorationsInRange(textRange);

        foreach (ModelDecoration dec in allDecorations)
        {
            // Convert TextRange to Range
            Range range = TextRangeToRange(dec.Range);

            if (dec.Options.ClassName == "currentFindMatch")
            {
                currentFindMatches.Add(range);
                allFindMatches.Add(range);
            }
            else if (dec.Options.ClassName == "findMatch")
            {
                allFindMatches.Add(range);
            }
        }

        // Sort by range start position (simple comparison)
        currentFindMatches.Sort((a, b) =>
        {
            int lineCmp = a.Start.LineNumber.CompareTo(b.Start.LineNumber);
            if (lineCmp != 0)
            {
                return lineCmp;
            }

            return a.Start.Column.CompareTo(b.Start.Column);
        });
        allFindMatches.Sort((a, b) =>
        {
            int lineCmp = a.Start.LineNumber.CompareTo(b.Start.LineNumber);
            if (lineCmp != 0)
            {
                return lineCmp;
            }

            return a.Start.Column.CompareTo(b.Start.Column);
        });

        return new FindDecorationsState
        {
            Highlighted = currentFindMatches.ToArray(),
            FindDecorations = allFindMatches.ToArray()
        };
    }

    /// <summary>
    /// Converts TextRange (offset-based) to Range (position-based).
    /// </summary>
    private Range TextRangeToRange(Decorations.TextRange textRange)
    {
        TextPosition start = Model.GetPositionAt(textRange.StartOffset);
        TextPosition end = Model.GetPositionAt(textRange.EndOffset);
        return new Range(start, end);
    }

    /// <summary>
    /// Asserts the find state matches expected values.
    /// Mimics TS assertFindState function.
    /// </summary>
    /// <param name="cursor">Expected cursor position [lineNumber, column, lineNumber, column]</param>
    /// <param name="highlighted">Expected highlighted range (null if none) [lineNumber, column, lineNumber, column]</param>
    /// <param name="findDecorations">Expected find match decorations (array of ranges)</param>
    public void AssertFindState(
        int[] cursor,
        int[]? highlighted,
        int[][] findDecorations)
    {
        // Assert cursor position (use current match if available)
        Range expectedCursor = new(
            new TextPosition(cursor[0], cursor[1]),
            new TextPosition(cursor[2], cursor[3])
        );
        Range actualCursor = GetSelection();

        if (!expectedCursor.Equals(actualCursor))
        {
            throw new Exception(
                $"Cursor mismatch: expected [{cursor[0]},{cursor[1]},{cursor[2]},{cursor[3]}], " +
                $"actual [{actualCursor.Start.LineNumber},{actualCursor.Start.Column},{actualCursor.End.LineNumber},{actualCursor.End.Column}]"
            );
        }

        // Get actual state
        FindDecorationsState state = GetFindState();

        // Assert highlighted match
        Range[] expectedHighlighted = highlighted != null
            ? [ new Range(
                new TextPosition(highlighted[0], highlighted[1]),
                new TextPosition(highlighted[2], highlighted[3])
              )]
            : Array.Empty<Range>();

        if (state.Highlighted.Length != expectedHighlighted.Length)
        {
            throw new Exception(
                $"Highlighted count mismatch: expected {expectedHighlighted.Length}, actual {state.Highlighted.Length}"
            );
        }

        for (int i = 0; i < expectedHighlighted.Length; i++)
        {
            if (!expectedHighlighted[i].Equals(state.Highlighted[i]))
            {
                throw new Exception(
                    $"Highlighted[{i}] mismatch: expected {RangeToString(expectedHighlighted[i])}, " +
                    $"actual {RangeToString(state.Highlighted[i])}"
                );
            }
        }

        // Assert all find decorations
        Range[] expectedDecorations = findDecorations
            .Select(r => new Range(
                new TextPosition(r[0], r[1]),
                new TextPosition(r[2], r[3])
            ))
            .ToArray();

        if (state.FindDecorations.Length != expectedDecorations.Length)
        {
            string actualRanges = string.Join(", ", state.FindDecorations.Select(RangeToString));
            string expectedRanges = string.Join(", ", expectedDecorations.Select(RangeToString));
            throw new Exception(
                $"FindDecorations count mismatch: expected {expectedDecorations.Length}, actual {state.FindDecorations.Length}\n" +
                $"Expected: {expectedRanges}\n" +
                $"Actual: {actualRanges}"
            );
        }

        for (int i = 0; i < expectedDecorations.Length; i++)
        {
            if (!expectedDecorations[i].Equals(state.FindDecorations[i]))
            {
                throw new Exception(
                    $"FindDecorations[{i}] mismatch: expected {RangeToString(expectedDecorations[i])}, " +
                    $"actual {RangeToString(state.FindDecorations[i])}"
                );
            }
        }
    }

    private static string RangeToString(Range r)
    {
        return $"[{r.Start.LineNumber},{r.Start.Column},{r.End.LineNumber},{r.End.Column}]";
    }

    private static Range[] CloneRanges(Range[] ranges)
    {
        if (ranges == null || ranges.Length == 0)
        {
            return Array.Empty<Range>();
        }

        Range[] clone = new Range[ranges.Length];
        for (int i = 0; i < ranges.Length; i++)
        {
            Range source = ranges[i];
            clone[i] = new Range(source.Start, source.End);
        }

        return clone;
    }

    public void Dispose()
    {
        FindModel?.Dispose();
        State?.Dispose();
    }
}

/// <summary>
/// Represents the find decorations state (highlighted + all matches).
/// </summary>
public class FindDecorationsState
{
    public Range[] Highlighted { get; set; } = Array.Empty<Range>();
    public Range[] FindDecorations { get; set; } = Array.Empty<Range>();
}

public sealed class TestEditorContextOptions
{
    public string? WordSeparators { get; init; }
    public double? ViewportHeightPx { get; init; }
    public Range[]? InitialSelections { get; init; }
}
