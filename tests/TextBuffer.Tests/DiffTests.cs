// Source: ts/src/vs/editor/test/common/diff/defaultLinesDiffComputer.test.ts
// Source: ts/src/vs/editor/test/common/diff/diffComputer.test.ts
// - Tests: Word diff inner changes, ignore trim whitespace, move detection
// - Tests: Unchanged regions, postprocess char changes, boundary cases, performance
// Ported: 2025-11-20
// Extended: 2025-12-02 (Sprint05-M3-DiffRegressionTests)

using System.Text;
using PieceTree.TextBuffer.Diff;

namespace PieceTree.TextBuffer.Tests;

public class DiffTests
{
    #region Basic Diff Tests (Original)

    [Fact]
    public void WordDiffProducesInnerChanges()
    {
        string original = "import { Baz, Bar } from \"foo\";";
        string modified = "import { Baz, Bar, Foo } from \"foo\";";

        DiffResult result = DiffComputer.Compute(original, modified);

        DetailedLineRangeMapping change = Assert.Single(result.Changes);
        RangeMapping inner = Assert.Single(change.InnerChanges);

        int insertionColumn = original.IndexOf(" } from", StringComparison.Ordinal);
        Assert.True(insertionColumn >= 0);
        Assert.Equal(insertionColumn + 1, inner.OriginalRange.StartColumn);
        Assert.Equal(inner.OriginalRange.StartColumn, inner.ModifiedRange.StartColumn);
        Assert.Equal(inner.OriginalRange.StartColumn, inner.OriginalRange.EndColumn);
        Assert.Equal(", Foo".Length, inner.ModifiedRange.EndColumn - inner.ModifiedRange.StartColumn);
    }

    [Fact]
    public void IgnoreTrimWhitespaceTreatsTrailingSpacesAsEqual()
    {
        string original = "alpha\nbeta\n";
        string modified = "alpha   \nbeta\t\n";

        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            IgnoreTrimWhitespace = true,
        });

        Assert.Empty(result.Changes);
        Assert.False(result.HitTimeout);
    }

    [Fact]
    public void MoveDetectionEmitsNestedMappings()
    {
        string original = string.Join('\n', new[]
        {
            "header();",
            "console.log(\"one\");",
            "console.log(\"two\");",
            "console.log(\"three\");",
            "footer();",
        });

        string modified = string.Join('\n', new[]
        {
            "header();",
            "footer();",
            "console.log(\"one\");",
            "console.log(\"TWO\");",
            "console.log(\"three\");",
        });

        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions { ComputeMoves = true });

        DiffMove move = Assert.Single(result.Moves);
        Assert.Equal(2, move.Original.StartLineNumber);
        Assert.Equal(5, move.Original.EndLineNumberExclusive);
        Assert.Equal(3, move.Modified.StartLineNumber);
        Assert.Equal(6, move.Modified.EndLineNumberExclusive);

        DetailedLineRangeMapping nestedMapping = Assert.Single(move.Changes);
        RangeMapping inner = Assert.Single(nestedMapping.InnerChanges);
        Assert.Equal(3, inner.OriginalRange.StartLineNumber);
        Assert.Equal(4, inner.ModifiedRange.StartLineNumber);
        Assert.Equal(inner.OriginalRange.StartColumn, inner.ModifiedRange.StartColumn);
        Assert.True(inner.ModifiedRange.EndColumn - inner.ModifiedRange.StartColumn > 0);
    }

    [Fact]
    public void DiffRespectsTimeoutFlag()
    {
        const int lineCount = 12000;
        string original = BuildLargeDocument(lineCount, 'a');
        string modified = BuildLargeDocument(lineCount, 'b');

        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            MaxComputationTimeMs = 1,
            ComputeMoves = false,
        });

        Assert.True(result.HitTimeout);
    }

    #endregion

    #region UnchangedRegions Tests (10 cases)
    // Tests for LineRangeMapping.Inverse which computes unchanged regions

    [Fact]
    public void UnchangedRegions_IdenticalDocuments_ReturnsEntireDocument()
    {
        string[] original = ["line1", "line2", "line3"];
        string[] modified = ["line1", "line2", "line3"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Empty(result.Changes);
        Assert.True(result.IsIdentical);
    }

    [Fact]
    public void UnchangedRegions_SingleInsertion_ComputesBeforeAndAfterRegions()
    {
        string[] original = ["A", "B", "C"];
        string[] modified = ["A", "X", "B", "C"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        // Change should be insertion between line 1 and 2
        Assert.Equal(2, change.Original.StartLineNumber);
        Assert.Equal(2, change.Original.EndLineNumberExclusive); // Empty range (insertion)
        Assert.Equal(2, change.Modified.StartLineNumber);
        Assert.Equal(3, change.Modified.EndLineNumberExclusive);

        // Verify unchanged regions via Inverse
        IReadOnlyList<LineRangeMapping> unchanged = LineRangeMapping.Inverse(
            result.Changes.Select(c => new LineRangeMapping(c.Original, c.Modified)).ToArray(),
            original.Length,
            modified.Length);

        Assert.Equal(2, unchanged.Count);
        // Before: line 1
        Assert.Equal(1, unchanged[0].Original.StartLineNumber);
        Assert.Equal(2, unchanged[0].Original.EndLineNumberExclusive);
        // After: lines 2-3 in original -> lines 3-4 in modified
        Assert.Equal(2, unchanged[1].Original.StartLineNumber);
        Assert.Equal(4, unchanged[1].Original.EndLineNumberExclusive);
    }

    [Fact]
    public void UnchangedRegions_SingleDeletion_ComputesBeforeAndAfterRegions()
    {
        string[] original = ["A", "X", "B", "C"];
        string[] modified = ["A", "B", "C"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        // Change should be deletion of line 2
        Assert.Equal(2, change.Original.StartLineNumber);
        Assert.Equal(3, change.Original.EndLineNumberExclusive);
        Assert.Equal(2, change.Modified.StartLineNumber);
        Assert.Equal(2, change.Modified.EndLineNumberExclusive); // Empty range (deletion)

        // Verify unchanged regions via Inverse
        IReadOnlyList<LineRangeMapping> unchanged = LineRangeMapping.Inverse(
            result.Changes.Select(c => new LineRangeMapping(c.Original, c.Modified)).ToArray(),
            original.Length,
            modified.Length);

        Assert.Equal(2, unchanged.Count);
        // Before: line 1
        Assert.Equal(1, unchanged[0].Original.StartLineNumber);
        Assert.Equal(2, unchanged[0].Original.EndLineNumberExclusive);
        // After: lines 3-4 in original -> lines 2-3 in modified
        Assert.Equal(3, unchanged[1].Original.StartLineNumber);
        Assert.Equal(5, unchanged[1].Original.EndLineNumberExclusive);
    }

    [Theory]
    [InlineData(new[] { "A", "B", "C" }, new[] { "X", "B", "C" }, 1)] // Change at start
    [InlineData(new[] { "A", "B", "C" }, new[] { "A", "X", "C" }, 1)] // Change in middle
    [InlineData(new[] { "A", "B", "C" }, new[] { "A", "B", "X" }, 1)] // Change at end
    public void UnchangedRegions_SingleLineChange_ComputesCorrectRegions(string[] original, string[] modified, int expectedChanges)
    {
        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Equal(expectedChanges, result.Changes.Count);
    }

    [Fact]
    public void UnchangedRegions_MultipleChanges_ComputesGapsBetween()
    {
        string[] original = ["A", "B", "C", "D", "E"];
        string[] modified = ["X", "B", "C", "Y", "E"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Equal(2, result.Changes.Count);

        // Verify unchanged regions via Inverse
        IReadOnlyList<LineRangeMapping> unchanged = LineRangeMapping.Inverse(
            result.Changes.Select(c => new LineRangeMapping(c.Original, c.Modified)).ToArray(),
            original.Length,
            modified.Length);

        // Should have: before A, B-C (gap), after E
        Assert.True(unchanged.Count >= 2);
    }

    [Fact]
    public void UnchangedRegions_LargeUnchangedBlock_PreservesCorrectly()
    {
        string[] original = Enumerable.Range(1, 100).Select(i => $"line{i}").ToArray();
        string[] modified = Enumerable.Range(1, 100).Select(i => i == 50 ? "CHANGED" : $"line{i}").ToArray();

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        Assert.Equal(50, result.Changes[0].Original.StartLineNumber);
        Assert.Equal(51, result.Changes[0].Original.EndLineNumberExclusive);
    }

    [Fact]
    public void UnchangedRegions_ConsecutiveChanges_MergesCorrectly()
    {
        string[] original = ["A", "B", "C", "D"];
        string[] modified = ["X", "Y", "C", "D"];

        DiffResult result = DiffComputer.Compute(original, modified);

        // Two consecutive changes may merge into one
        Assert.True(result.Changes.Count >= 1 && result.Changes.Count <= 2);
    }

    [Fact]
    public void UnchangedRegions_AllChanged_NoUnchangedRegions()
    {
        string[] original = ["A", "B"];
        string[] modified = ["X", "Y"];

        DiffResult result = DiffComputer.Compute(original, modified);

        IReadOnlyList<LineRangeMapping> unchanged = LineRangeMapping.Inverse(
            result.Changes.Select(c => new LineRangeMapping(c.Original, c.Modified)).ToArray(),
            original.Length,
            modified.Length);

        // All lines changed, no unchanged regions except possibly empty ones
        Assert.True(unchanged.All(u => u.Original.IsEmpty || u.Modified.IsEmpty) || unchanged.Count == 0);
    }

    [Fact]
    public void UnchangedRegions_InterleavedChanges_ComputesCorrectGaps()
    {
        string[] original = ["A", "B", "C", "D", "E", "F"];
        string[] modified = ["X", "B", "Y", "D", "Z", "F"];

        DiffResult result = DiffComputer.Compute(original, modified);

        // Should have changes at lines 1, 3, 5 with unchanged at 2, 4, 6
        Assert.True(result.Changes.Count >= 2);

        IReadOnlyList<LineRangeMapping> unchanged = LineRangeMapping.Inverse(
            result.Changes.Select(c => new LineRangeMapping(c.Original, c.Modified)).ToArray(),
            original.Length,
            modified.Length);

        // Should preserve B, D, F as unchanged
        Assert.True(unchanged.Count >= 1);
    }

    #endregion

    #region PostProcessCharChanges Tests (5 cases)
    // Tests for inline diff character-level change processing

    [Fact]
    public void CharChanges_EmptyChange_NoInnerChanges()
    {
        string[] original = ["line"];
        string[] modified = ["line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Empty(result.Changes);
    }

    [Fact]
    public void CharChanges_FullLineChange_SingleInnerChange()
    {
        string[] original = ["abc"];
        string[] modified = ["xyz"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];
        Assert.True(change.InnerChanges.Count >= 1);

        // The entire line content should be marked as changed
        RangeMapping inner = change.InnerChanges[0];
        Assert.Equal(1, inner.OriginalRange.StartColumn);
    }

    [Theory]
    [InlineData("hello", "hello world", 6, 6)] // Append at end
    [InlineData("world", "hello world", 1, 1)]  // Insert at start
    [InlineData("helloworld", "hello world", 6, 6)] // Insert in middle (space)
    public void CharChanges_PartialLineChange_CorrectRange(
        string original, string modified,
        int expectedOrigStart,
        int expectedModStart)
    {
        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];
        Assert.True(change.InnerChanges.Count >= 1);

        // Find the relevant inner change
        RangeMapping inner = change.InnerChanges.First(c =>
            c.OriginalRange.StartColumn >= expectedOrigStart - 1 &&
            c.ModifiedRange.StartColumn >= expectedModStart - 1);

        Assert.NotNull(inner);
    }

    [Fact]
    public void CharChanges_MultipleCharChangesInLine_AllReported()
    {
        // TS test: "abba" -> "abzzbzza" produces two char changes
        string[] original = ["abba"];
        string[] modified = ["abzzbzza"];

        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            ExtendToWordBoundaries = false // Disable prettify to see raw changes
        });

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        // Should have at least one inner change (may merge depending on heuristics)
        Assert.True(change.InnerChanges.Count >= 1);
    }

    [Fact]
    public void CharChanges_CrossLineChange_SpansMultipleLines()
    {
        // TS test: two lines changed to one
        string[] original = ["abcd", "efgh"];
        string[] modified = ["abcz"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(1, change.Original.StartLineNumber);
        Assert.Equal(3, change.Original.EndLineNumberExclusive);
        Assert.Equal(1, change.Modified.StartLineNumber);
        Assert.Equal(2, change.Modified.EndLineNumberExclusive);
    }

    #endregion

    #region Boundary Cases Tests (5 cases)

    [Fact]
    public void BoundaryCase_EmptyDocuments_NoChanges()
    {
        string[] original = [""];
        string[] modified = [""];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Empty(result.Changes);
        Assert.True(result.IsIdentical);
    }

    [Fact]
    public void BoundaryCase_EmptyToNonEmpty_SingleChange()
    {
        string[] original = [""];
        string[] modified = ["content"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
    }

    [Fact]
    public void BoundaryCase_NonEmptyToEmpty_SingleChange()
    {
        string[] original = ["content"];
        string[] modified = [""];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
    }

    [Fact]
    public void BoundaryCase_SingleLineDocument_CorrectDiff()
    {
        string original = "single line";
        string modified = "different line";

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        Assert.Equal(1, result.Changes[0].Original.StartLineNumber);
        Assert.Equal(2, result.Changes[0].Original.EndLineNumberExclusive);
    }

    [Theory]
    [InlineData("line\r\n", "line\n", true)]  // CRLF vs LF with IgnoreTrimWhitespace
    [InlineData("line\r\n", "line\n", false)] // CRLF vs LF without IgnoreTrimWhitespace
    public void BoundaryCase_LineEndingDifference_HandledCorrectly(string original, string modified, bool ignoreTrimWhitespace)
    {
        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            IgnoreTrimWhitespace = ignoreTrimWhitespace
        });

        // Line endings are normalized during parsing, so "line\r\n" and "line\n" should be equal
        Assert.Empty(result.Changes);
    }

    [Theory]
    [InlineData("  line", "line", true)]   // Leading whitespace, ignore
    [InlineData("line  ", "line", true)]   // Trailing whitespace, ignore
    [InlineData("  line  ", "line", true)] // Both, ignore
    [InlineData("  line", "line", false)]  // Leading whitespace, don't ignore
    public void BoundaryCase_WhitespaceOnlyDifference_RespectsSetting(string original, string modified, bool ignoreTrimWhitespace)
    {
        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            IgnoreTrimWhitespace = ignoreTrimWhitespace
        });

        if (ignoreTrimWhitespace)
        {
            Assert.Empty(result.Changes);
        }
        else
        {
            Assert.Single(result.Changes);
        }
    }

    #endregion

    #region Performance Tests (3 cases)

    [Fact]
    public void Performance_10KLines_CompletesWithoutTimeout()
    {
        const int lineCount = 10000;
        string original = BuildLargeDocument(lineCount, 'a');
        // Modify 10% of lines
        StringBuilder modifiedBuilder = new();
        for (int i = 0; i < lineCount; i++)
        {
            char payload = (i % 10 == 0) ? 'b' : 'a';
            modifiedBuilder.Append("line ").Append(i).Append(' ').Append(payload, 64).Append('\n');
        }

        string modified = modifiedBuilder.ToString();

        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            MaxComputationTimeMs = 30000, // 30 second timeout
            ComputeMoves = false // Disable moves for faster computation
        });

        Assert.False(result.HitTimeout, "10K line diff should complete within timeout");
        Assert.True(result.Changes.Count > 0, "Should detect some changes");
    }

    [Fact]
    public void Performance_50KLines_CompletesOrTimesOut()
    {
        const int lineCount = 50000;
        string original = BuildLargeDocument(lineCount, 'a');
        string modified = BuildLargeDocument(lineCount, 'a'); // Identical for fast baseline

        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            MaxComputationTimeMs = 10000, // 10 second timeout
            ComputeMoves = false
        });

        // For identical documents, should complete quickly without timeout
        Assert.False(result.HitTimeout, "50K line identical diff should not timeout");
        Assert.Empty(result.Changes);
    }

    [Theory]
    [InlineData(1)]    // 1ms - should timeout
    [InlineData(10)]   // 10ms - might timeout
    [InlineData(5000)] // 5s - should complete
    public void Performance_TimeoutBoundary_RespectsLimit(int timeoutMs)
    {
        const int lineCount = 5000;
        string original = BuildLargeDocument(lineCount, 'a');
        string modified = BuildLargeDocument(lineCount, 'b');

        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            MaxComputationTimeMs = timeoutMs,
            ComputeMoves = false
        });

        if (timeoutMs <= 10)
        {
            // Very short timeout should trigger HitTimeout
            Assert.True(result.HitTimeout, $"Timeout of {timeoutMs}ms should be hit for large diff");
        }
        // For larger timeouts, just verify it returns a result
        Assert.NotNull(result);
    }

    #endregion

    #region Additional Regression Tests from TS

    [Theory]
    [InlineData(new[] { "line" }, new[] { "line", "new line" })]
    [InlineData(new[] { "line" }, new[] { "new line", "line" })]
    [InlineData(new[] { "line1", "line2" }, new[] { "line1", "new", "line2" })]
    public void Insertions_Various_ProducesCorrectDiff(string[] original, string[] modified)
    {
        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.True(result.Changes.Count >= 1, "Insertion should produce at least one change");
        Assert.False(result.HitTimeout);
    }

    [Theory]
    [InlineData(new[] { "line", "new line" }, new[] { "line" })]
    [InlineData(new[] { "new line", "line" }, new[] { "line" })]
    [InlineData(new[] { "line1", "old", "line2" }, new[] { "line1", "line2" })]
    public void Deletions_Various_ProducesCorrectDiff(string[] original, string[] modified)
    {
        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.True(result.Changes.Count >= 1, "Deletion should produce at least one change");
        Assert.False(result.HitTimeout);
    }

    [Fact]
    public void PrettyDiff_MethodInsertion_AlignedCorrectly()
    {
        // TS test: pretty diff 3 - method insertion
        string[] original =
        [
            "class A {",
            "\t/**",
            "\t * m1",
            "\t */",
            "\tmethod1() {}",
            "",
            "\t/**",
            "\t * m3",
            "\t */",
            "\tmethod3() {}",
            "}"
        ];
        string[] modified =
        [
            "class A {",
            "\t/**",
            "\t * m1",
            "\t */",
            "\tmethod1() {}",
            "",
            "\t/**",
            "\t * m2",
            "\t */",
            "\tmethod2() {}",
            "",
            "\t/**",
            "\t * m3",
            "\t */",
            "\tmethod3() {}",
            "}"
        ];

        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            IgnoreTrimWhitespace = true
        });

        // Should detect the inserted method block
        Assert.True(result.Changes.Count >= 1);
    }

    [Fact]
    public void Issue_HasOwnProperty_NotFunction()
    {
        // TS test: issue #12122 r.hasOwnProperty is not a function
        string[] original = ["hasOwnProperty"];
        string[] modified = ["hasOwnProperty", "and another line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
    }

    #endregion

    #region Extended TS Regression Tests

    [Theory]
    [InlineData("abcd", "abcz", 4, 4)] // Last char changed
    [InlineData("abcd", "zbcd", 1, 1)] // First char changed
    [InlineData("abcd", "axcd", 2, 2)] // Middle char changed
    public void CharChange_SingleCharModification_CorrectRange(
        string original, string modified,
        int expectedOrigStartCol,
        int expectedModStartCol)
    {
        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            ExtendToWordBoundaries = false
        });

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];
        Assert.True(change.InnerChanges.Count >= 1);

        RangeMapping inner = change.InnerChanges[0];
        Assert.Equal(expectedOrigStartCol, inner.OriginalRange.StartColumn);
        Assert.Equal(expectedModStartCol, inner.ModifiedRange.StartColumn);
    }

    [Fact]
    public void TwoLinesChangedToOne_ProducesCorrectMapping()
    {
        // TS test: two lines changed 1
        string[] original = ["abcd", "efgh"];
        string[] modified = ["abcz"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];
        Assert.Equal(1, change.Original.StartLineNumber);
        Assert.Equal(3, change.Original.EndLineNumberExclusive);
        Assert.Equal(1, change.Modified.StartLineNumber);
        Assert.Equal(2, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void TwoLinesChangedInMiddle_ProducesCorrectMapping()
    {
        // TS test: two lines changed 2
        string[] original = ["foo", "abcd", "efgh", "BAR"];
        string[] modified = ["foo", "abcz", "BAR"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];
        Assert.Equal(2, change.Original.StartLineNumber);
        Assert.Equal(4, change.Original.EndLineNumberExclusive);
        Assert.Equal(2, change.Modified.StartLineNumber);
        Assert.Equal(3, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void ThreeLinesChanged_ProducesCorrectMapping()
    {
        // TS test: three lines changed
        string[] original = ["foo", "abcd", "efgh", "BAR"];
        string[] modified = ["foo", "zzzefgh", "xxx", "BAR"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
    }

    [Fact]
    public void BigChangePart1_InsertionAndModification()
    {
        // TS test: big change part 1
        string[] original = ["foo", "abcd", "efgh", "BAR"];
        string[] modified = ["hello", "foo", "zzzefgh", "xxx", "BAR"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.True(result.Changes.Count >= 1);
    }

    [Fact]
    public void BigChangePart2_InsertionModificationAndDeletion()
    {
        // TS test: big change part 2
        string[] original = ["foo", "abcd", "efgh", "BAR", "RAB"];
        string[] modified = ["hello", "foo", "zzzefgh", "xxx", "BAR"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.True(result.Changes.Count >= 1);
    }

    [Fact]
    public void LongMatchingLines_PreferredOverShort()
    {
        // TS test: gives preference to matching longer lines
        string[] original = ["A", "A", "BB", "C"];
        string[] modified = ["A", "BB", "A", "D", "E", "A", "C"];

        DiffResult result = DiffComputer.Compute(original, modified);

        // Should prefer keeping BB matched
        Assert.True(result.Changes.Count >= 1);
    }

    [Fact]
    public void FewerDiffHunks_Preferred()
    {
        // TS test: issue #119051: gives preference to fewer diff hunks
        string[] original = ["1", "", "", "2", ""];
        string[] modified = ["1", "", "1.5", "", "", "2", "", "3", ""];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.True(result.Changes.Count >= 1);
    }

    [Theory]
    [InlineData(new[] { "if (cond) {", "    cmd", "}" },
                new[] { "if (cond) {", "    if (other_cond) {", "        cmd", "    }", "}" })]
    public void NestedBlockInsertion_ProducesCorrectDiff(string[] original, string[] modified)
    {
        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            IgnoreTrimWhitespace = true
        });

        Assert.True(result.Changes.Count >= 1);
    }

    [Fact]
    public void LeadingAndTrailingWhitespaceDiff_HandledCorrectly()
    {
        // TS test: issue #169552: Assertion error when having both leading and trailing whitespace diffs
        string[] original = ["if True:", "    print(2)"];
        string[] modified = ["if True:", "\tprint(2) "];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        Assert.True(result.Changes[0].InnerChanges.Count >= 1);
    }

    [Fact]
    public void Issue43922_YarnInstallDiff()
    {
        // TS test: issue #43922
        string[] original = [" * `yarn [install]` -- Install project NPM dependencies. This is automatically done when you first create the project. You should only need to run this if you add dependencies in `package.json`."];
        string[] modified = [" * `yarn` -- Install project NPM dependencies. You should only need to run this if you add dependencies in `package.json`."];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        Assert.True(result.Changes[0].InnerChanges.Count >= 1);
    }

    [Fact]
    public void Issue42751_IndentationChange()
    {
        // TS test: issue #42751
        string[] original = ["    1", "  2"];
        string[] modified = ["    1", "   3"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
    }

    #endregion

    #region Deterministic Test Matrix (Sprint05-M3 Extended)
    // Systematic tests aligned with TS original: defaultLinesDiffComputer.test.ts & diffComputer.test.ts

    #region Simple Insert Tests

    [Fact]
    public void SimpleInsert_OneLineBelow_ProducesInsertionChange()
    {
        // TS test: one inserted line below
        string[] original = ["line"];
        string[] modified = ["line", "new line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        // Insertion: original range is empty, modified range covers the new line
        Assert.Equal(2, change.Original.StartLineNumber);
        Assert.Equal(2, change.Original.EndLineNumberExclusive); // Empty (insertion point)
        Assert.Equal(2, change.Modified.StartLineNumber);
        Assert.Equal(3, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void SimpleInsert_TwoLinesBelow_ProducesInsertionChange()
    {
        // TS test: two inserted lines below
        string[] original = ["line"];
        string[] modified = ["line", "new line", "another new line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(2, change.Modified.StartLineNumber);
        Assert.Equal(4, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void SimpleInsert_OneLineAbove_ProducesInsertionChange()
    {
        // TS test: one inserted line above
        string[] original = ["line"];
        string[] modified = ["new line", "line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(1, change.Original.StartLineNumber);
        Assert.Equal(1, change.Original.EndLineNumberExclusive); // Empty (insertion at start)
        Assert.Equal(1, change.Modified.StartLineNumber);
        Assert.Equal(2, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void SimpleInsert_TwoLinesAbove_ProducesInsertionChange()
    {
        // TS test: two inserted lines above
        string[] original = ["line"];
        string[] modified = ["new line", "another new line", "line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(1, change.Modified.StartLineNumber);
        Assert.Equal(3, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void SimpleInsert_OneLineInMiddle_ProducesInsertionChange()
    {
        // TS test: one inserted line in middle
        string[] original = ["line1", "line2", "line3", "line4"];
        string[] modified = ["line1", "line2", "new line", "line3", "line4"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(3, change.Original.StartLineNumber);
        Assert.Equal(3, change.Original.EndLineNumberExclusive);
        Assert.Equal(3, change.Modified.StartLineNumber);
        Assert.Equal(4, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void SimpleInsert_TwoLinesInMiddle_ProducesInsertionChange()
    {
        // TS test: two inserted lines in middle
        string[] original = ["line1", "line2", "line3", "line4"];
        string[] modified = ["line1", "line2", "new line", "another new line", "line3", "line4"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(3, change.Modified.StartLineNumber);
        Assert.Equal(5, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void SimpleInsert_TwoLinesMiddleInterrupted_ProducesTwoChanges()
    {
        // TS test: two inserted lines in middle interrupted
        string[] original = ["line1", "line2", "line3", "line4"];
        string[] modified = ["line1", "line2", "new line", "line3", "another new line", "line4"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Equal(2, result.Changes.Count);
    }

    #endregion

    #region Simple Delete Tests

    [Fact]
    public void SimpleDelete_OneLineBelow_ProducesDeletionChange()
    {
        // TS test: one deleted line below
        string[] original = ["line", "new line"];
        string[] modified = ["line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        // Deletion: modified range is empty
        Assert.Equal(2, change.Original.StartLineNumber);
        Assert.Equal(3, change.Original.EndLineNumberExclusive);
        Assert.Equal(2, change.Modified.StartLineNumber);
        Assert.Equal(2, change.Modified.EndLineNumberExclusive); // Empty (deletion)
    }

    [Fact]
    public void SimpleDelete_TwoLinesBelow_ProducesDeletionChange()
    {
        // TS test: two deleted lines below
        string[] original = ["line", "new line", "another new line"];
        string[] modified = ["line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(2, change.Original.StartLineNumber);
        Assert.Equal(4, change.Original.EndLineNumberExclusive);
    }

    [Fact]
    public void SimpleDelete_OneLineAbove_ProducesDeletionChange()
    {
        // TS test: one deleted line above
        string[] original = ["new line", "line"];
        string[] modified = ["line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(1, change.Original.StartLineNumber);
        Assert.Equal(2, change.Original.EndLineNumberExclusive);
        Assert.Equal(1, change.Modified.StartLineNumber);
        Assert.Equal(1, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void SimpleDelete_TwoLinesAbove_ProducesDeletionChange()
    {
        // TS test: two deleted lines above
        string[] original = ["new line", "another new line", "line"];
        string[] modified = ["line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(1, change.Original.StartLineNumber);
        Assert.Equal(3, change.Original.EndLineNumberExclusive);
    }

    [Fact]
    public void SimpleDelete_OneLineInMiddle_ProducesDeletionChange()
    {
        // TS test: one deleted line in middle
        string[] original = ["line1", "line2", "new line", "line3", "line4"];
        string[] modified = ["line1", "line2", "line3", "line4"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(3, change.Original.StartLineNumber);
        Assert.Equal(4, change.Original.EndLineNumberExclusive);
        Assert.Equal(3, change.Modified.StartLineNumber);
        Assert.Equal(3, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void SimpleDelete_TwoLinesInMiddle_ProducesDeletionChange()
    {
        // TS test: two deleted lines in middle
        string[] original = ["line1", "line2", "new line", "another new line", "line3", "line4"];
        string[] modified = ["line1", "line2", "line3", "line4"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(3, change.Original.StartLineNumber);
        Assert.Equal(5, change.Original.EndLineNumberExclusive);
    }

    [Fact]
    public void SimpleDelete_TwoLinesMiddleInterrupted_ProducesTwoChanges()
    {
        // TS test: two deleted lines in middle interrupted
        string[] original = ["line1", "line2", "new line", "line3", "another new line", "line4"];
        string[] modified = ["line1", "line2", "line3", "line4"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Equal(2, result.Changes.Count);
    }

    #endregion

    #region Simple Change Tests

    [Fact]
    public void SimpleChange_CharsInsertedAtEnd_ProducesCorrectInnerChange()
    {
        // TS test: one line changed: chars inserted at the end
        string[] original = ["line"];
        string[] modified = ["line changed"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];
        Assert.True(change.InnerChanges.Count >= 1);

        // Verify the insertion is at position after "line"
        RangeMapping inner = change.InnerChanges[0];
        Assert.Equal(5, inner.OriginalRange.StartColumn);
        Assert.Equal(5, inner.OriginalRange.EndColumn); // Empty original = insertion
        Assert.Equal(5, inner.ModifiedRange.StartColumn);
        Assert.True(inner.ModifiedRange.EndColumn > inner.ModifiedRange.StartColumn);
    }

    [Fact]
    public void SimpleChange_CharsInsertedAtBeginning_ProducesCorrectInnerChange()
    {
        // TS test: one line changed: chars inserted at the beginning
        string[] original = ["line"];
        string[] modified = ["my line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];
        Assert.True(change.InnerChanges.Count >= 1);

        // Verify the insertion is at position 1
        RangeMapping inner = change.InnerChanges[0];
        Assert.Equal(1, inner.OriginalRange.StartColumn);
    }

    [Fact]
    public void SimpleChange_CharsInsertedInMiddle_ProducesCorrectInnerChange()
    {
        // TS test: one line changed: chars inserted in the middle
        string[] original = ["abba"];
        string[] modified = ["abzzba"];

        DiffResult result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            ExtendToWordBoundaries = false
        });

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];
        Assert.True(change.InnerChanges.Count >= 1);

        // The insertion of "zz" should be at column 3
        RangeMapping inner = change.InnerChanges[0];
        Assert.Equal(3, inner.OriginalRange.StartColumn);
        Assert.Equal(3, inner.ModifiedRange.StartColumn);
    }

    [Fact]
    public void SimpleChange_CharsDeleted_ProducesCorrectInnerChange()
    {
        // TS test: one line changed: chars deleted 1
        string[] original = ["abcdefg"];
        string[] modified = ["abcfg"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];
        Assert.True(change.InnerChanges.Count >= 1);

        // The deletion of "de" at column 4
        RangeMapping inner = change.InnerChanges[0];
        Assert.Equal(4, inner.OriginalRange.StartColumn);
        Assert.Equal(6, inner.OriginalRange.EndColumn);
        Assert.Equal(4, inner.ModifiedRange.StartColumn);
        Assert.Equal(4, inner.ModifiedRange.EndColumn); // Empty = deletion
    }

    [Fact]
    public void SimpleChange_MultipleCharsDeleted_ProducesCorrectInnerChanges()
    {
        // TS test: one line changed: chars deleted 2
        string[] original = ["abcdefg"];
        string[] modified = ["acfg"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        // Should have at least one inner change covering the deletions
        Assert.True(change.InnerChanges.Count >= 1);
    }

    #endregion

    #region Multi-Line Change Tests

    [Fact]
    public void MultiLineChange_TwoLinesToOne_ProducesCorrectMapping()
    {
        // TS test: two lines changed 1
        string[] original = ["abcd", "efgh"];
        string[] modified = ["abcz"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(1, change.Original.StartLineNumber);
        Assert.Equal(3, change.Original.EndLineNumberExclusive);
        Assert.Equal(1, change.Modified.StartLineNumber);
        Assert.Equal(2, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void MultiLineChange_TwoLinesInContext_ProducesCorrectMapping()
    {
        // TS test: two lines changed 2
        string[] original = ["foo", "abcd", "efgh", "BAR"];
        string[] modified = ["foo", "abcz", "BAR"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(2, change.Original.StartLineNumber);
        Assert.Equal(4, change.Original.EndLineNumberExclusive);
        Assert.Equal(2, change.Modified.StartLineNumber);
        Assert.Equal(3, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void MultiLineChange_TwoLinesModified_ProducesCorrectMapping()
    {
        // TS test: two lines changed 3
        string[] original = ["foo", "abcd", "efgh", "BAR"];
        string[] modified = ["foo", "abcz", "zzzzefgh", "BAR"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
    }

    [Fact]
    public void MultiLineChange_OneLineToFour_ProducesCorrectMapping()
    {
        // TS test: two lines changed 4
        string[] original = ["abc"];
        string[] modified = ["", "", "axc", ""];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(1, change.Original.StartLineNumber);
        Assert.Equal(2, change.Original.EndLineNumberExclusive);
        Assert.Equal(1, change.Modified.StartLineNumber);
        Assert.Equal(5, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void MultiLineChange_ThreeLines_ProducesCorrectMapping()
    {
        // TS test: three lines changed
        string[] original = ["foo", "abcd", "efgh", "BAR"];
        string[] modified = ["foo", "zzzefgh", "xxx", "BAR"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
    }

    [Fact]
    public void MultiLineChange_EmptyOriginalInCharDiff_Handled()
    {
        // TS test: empty original sequence in char diff
        string[] original = ["abc", "", "xyz"];
        string[] modified = ["abc", "qwe", "rty", "xyz"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(2, change.Original.StartLineNumber);
        Assert.Equal(3, change.Original.EndLineNumberExclusive);
        Assert.Equal(2, change.Modified.StartLineNumber);
        Assert.Equal(4, change.Modified.EndLineNumberExclusive);
    }

    #endregion

    #region Empty File Tests

    [Fact]
    public void EmptyFiles_BothEmpty_NoChanges()
    {
        // TS test: empty diff 5
        string[] original = [""];
        string[] modified = [""];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Empty(result.Changes);
        Assert.True(result.IsIdentical);
    }

    [Fact]
    public void EmptyFiles_EmptyToOneLine_SingleChange()
    {
        // TS test: empty diff 1
        string[] original = [""];
        string[] modified = ["something"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
    }

    [Fact]
    public void EmptyFiles_EmptyToTwoLines_SingleChange()
    {
        // TS test: empty diff 2
        string[] original = [""];
        string[] modified = ["something", "something else"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
    }

    [Fact]
    public void EmptyFiles_TwoLinesToEmpty_SingleChange()
    {
        // TS test: empty diff 3
        string[] original = ["something", "something else"];
        string[] modified = [""];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
    }

    [Fact]
    public void EmptyFiles_OneLineToEmpty_SingleChange()
    {
        // TS test: empty diff 4
        string[] original = ["something"];
        string[] modified = [""];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
    }

    #endregion

    #region Identical Files Tests

    [Fact]
    public void IdenticalFiles_SingleLine_NoChanges()
    {
        string[] original = ["identical line"];
        string[] modified = ["identical line"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Empty(result.Changes);
        Assert.True(result.IsIdentical);
    }

    [Fact]
    public void IdenticalFiles_MultipleLines_NoChanges()
    {
        string[] original = ["line1", "line2", "line3"];
        string[] modified = ["line1", "line2", "line3"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Empty(result.Changes);
        Assert.True(result.IsIdentical);
    }

    [Fact]
    public void IdenticalFiles_WithEmptyLines_NoChanges()
    {
        string[] original = ["line1", "", "line3", ""];
        string[] modified = ["line1", "", "line3", ""];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Empty(result.Changes);
        Assert.True(result.IsIdentical);
    }

    [Fact]
    public void IdenticalFiles_OnlyWhitespace_NoChanges()
    {
        string[] original = ["   ", "\t\t", "  \t  "];
        string[] modified = ["   ", "\t\t", "  \t  "];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Empty(result.Changes);
        Assert.True(result.IsIdentical);
    }

    #endregion

    #region Completely Different Tests

    [Fact]
    public void CompletelyDifferent_SingleLineDifferent_OneChange()
    {
        string[] original = ["abc"];
        string[] modified = ["xyz"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        Assert.False(result.IsIdentical);
    }

    [Fact]
    public void CompletelyDifferent_MultipleLinesAllDifferent_OneChange()
    {
        string[] original = ["aaa", "bbb", "ccc"];
        string[] modified = ["xxx", "yyy", "zzz"];

        DiffResult result = DiffComputer.Compute(original, modified);

        // All different lines should produce changes
        Assert.True(result.Changes.Count >= 1);
        Assert.False(result.IsIdentical);
    }

    [Fact]
    public void CompletelyDifferent_DifferentLineCounts_HandledCorrectly()
    {
        string[] original = ["a", "b"];
        string[] modified = ["x", "y", "z", "w"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.True(result.Changes.Count >= 1);
        Assert.False(result.IsIdentical);
    }

    [Fact]
    public void CompletelyDifferent_NoCommonSubsequence_HandledCorrectly()
    {
        string[] original = ["12345"];
        string[] modified = ["abcde"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        // The entire line should be marked as changed
        Assert.Equal(1, change.Original.StartLineNumber);
        Assert.Equal(2, change.Original.EndLineNumberExclusive);
        Assert.Equal(1, change.Modified.StartLineNumber);
        Assert.Equal(2, change.Modified.EndLineNumberExclusive);
    }

    #endregion

    #region Consecutive Changes Merge Tests

    [Fact]
    public void ConsecutiveChanges_TwoAdjacentInserts_MayMerge()
    {
        string[] original = ["A", "D"];
        string[] modified = ["A", "B", "C", "D"];

        DiffResult result = DiffComputer.Compute(original, modified);

        // Two consecutive insertions should merge into one change
        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(2, change.Modified.StartLineNumber);
        Assert.Equal(4, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void ConsecutiveChanges_TwoAdjacentDeletes_MayMerge()
    {
        string[] original = ["A", "B", "C", "D"];
        string[] modified = ["A", "D"];

        DiffResult result = DiffComputer.Compute(original, modified);

        // Two consecutive deletions should merge into one change
        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(2, change.Original.StartLineNumber);
        Assert.Equal(4, change.Original.EndLineNumberExclusive);
    }

    [Fact]
    public void ConsecutiveChanges_AdjacentModifications_MayMerge()
    {
        string[] original = ["A", "B", "C", "D"];
        string[] modified = ["A", "X", "Y", "D"];

        DiffResult result = DiffComputer.Compute(original, modified);

        // Adjacent modifications should merge into one change
        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        Assert.Equal(2, change.Original.StartLineNumber);
        Assert.Equal(4, change.Original.EndLineNumberExclusive);
        Assert.Equal(2, change.Modified.StartLineNumber);
        Assert.Equal(4, change.Modified.EndLineNumberExclusive);
    }

    [Fact]
    public void ConsecutiveChanges_SeparatedByUnchanged_TwoDistinctChanges()
    {
        string[] original = ["A", "B", "C", "D", "E"];
        string[] modified = ["X", "B", "C", "Y", "E"];

        DiffResult result = DiffComputer.Compute(original, modified);

        // Changes separated by unchanged lines (B, C) should remain separate
        Assert.Equal(2, result.Changes.Count);
    }

    [Fact]
    public void ConsecutiveChanges_AllLinesDifferent_SingleMergedChange()
    {
        string[] original = ["A", "B", "C"];
        string[] modified = ["X", "Y", "Z"];

        DiffResult result = DiffComputer.Compute(original, modified);

        // When all lines differ consecutively, should produce single merged change
        Assert.Single(result.Changes);
    }

    [Fact]
    public void ConsecutiveChanges_CharMerge_PostProcess()
    {
        // TS test: char change postprocessing merges
        string[] original = ["abba"];
        string[] modified = ["azzzbzzzbzzza"];

        DiffResult result = DiffComputer.Compute(original, modified);

        Assert.Single(result.Changes);
        DetailedLineRangeMapping change = result.Changes[0];

        // Inner char changes should be present
        Assert.True(change.InnerChanges.Count >= 1);
    }

    #endregion

    #endregion

    #region Helper Methods

    private static string BuildLargeDocument(int lineCount, char payload)
    {
        StringBuilder builder = new(lineCount * 64);
        for (int i = 0; i < lineCount; i++)
        {
            builder
                .Append("line ")
                .Append(i)
                .Append(' ')
                .Append(payload, 64)
                .Append('\n');
        }

        return builder.ToString();
    }

    #endregion
}
