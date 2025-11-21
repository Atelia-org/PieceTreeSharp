// Source: ts/src/vs/editor/test/common/diff/defaultLinesDiffComputer.test.ts
// - Tests: Word diff inner changes, ignore trim whitespace, move detection
// Ported: 2025-11-20

using System;
using System.Text;
using PieceTree.TextBuffer.Diff;
using Xunit;

namespace PieceTree.TextBuffer.Tests;

public class DiffTests
{
    [Fact]
    public void WordDiffProducesInnerChanges()
    {
        var original = "import { Baz, Bar } from \"foo\";";
        var modified = "import { Baz, Bar, Foo } from \"foo\";";

        var result = DiffComputer.Compute(original, modified);

        var change = Assert.Single(result.Changes);
        var inner = Assert.Single(change.InnerChanges);

        var insertionColumn = original.IndexOf(" } from", StringComparison.Ordinal);
        Assert.True(insertionColumn >= 0);
        Assert.Equal(insertionColumn + 1, inner.OriginalRange.StartColumn);
        Assert.Equal(inner.OriginalRange.StartColumn, inner.ModifiedRange.StartColumn);
        Assert.Equal(inner.OriginalRange.StartColumn, inner.OriginalRange.EndColumn);
        Assert.Equal(", Foo".Length, inner.ModifiedRange.EndColumn - inner.ModifiedRange.StartColumn);
    }

    [Fact]
    public void IgnoreTrimWhitespaceTreatsTrailingSpacesAsEqual()
    {
        var original = "alpha\nbeta\n";
        var modified = "alpha   \nbeta\t\n";

        var result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            IgnoreTrimWhitespace = true,
        });

        Assert.Empty(result.Changes);
        Assert.False(result.HitTimeout);
    }

    [Fact]
    public void MoveDetectionEmitsNestedMappings()
    {
        var original = string.Join('\n', new[]
        {
            "header();",
            "console.log(\"one\");",
            "console.log(\"two\");",
            "console.log(\"three\");",
            "footer();",
        });

        var modified = string.Join('\n', new[]
        {
            "header();",
            "footer();",
            "console.log(\"one\");",
            "console.log(\"TWO\");",
            "console.log(\"three\");",
        });

        var result = DiffComputer.Compute(original, modified, new DiffComputerOptions { ComputeMoves = true });

        var move = Assert.Single(result.Moves);
        Assert.Equal(2, move.Original.StartLineNumber);
        Assert.Equal(5, move.Original.EndLineNumberExclusive);
        Assert.Equal(3, move.Modified.StartLineNumber);
        Assert.Equal(6, move.Modified.EndLineNumberExclusive);

        var nestedMapping = Assert.Single(move.Changes);
        var inner = Assert.Single(nestedMapping.InnerChanges);
        Assert.Equal(3, inner.OriginalRange.StartLineNumber);
        Assert.Equal(4, inner.ModifiedRange.StartLineNumber);
        Assert.Equal(inner.OriginalRange.StartColumn, inner.ModifiedRange.StartColumn);
        Assert.True(inner.ModifiedRange.EndColumn - inner.ModifiedRange.StartColumn > 0);
    }

    [Fact]
    public void DiffRespectsTimeoutFlag()
    {
        const int lineCount = 12000;
        var original = BuildLargeDocument(lineCount, 'a');
        var modified = BuildLargeDocument(lineCount, 'b');

        var result = DiffComputer.Compute(original, modified, new DiffComputerOptions
        {
            MaxComputationTimeMs = 1,
            ComputeMoves = false,
        });

        Assert.True(result.HitTimeout);
    }

    private static string BuildLargeDocument(int lineCount, char payload)
    {
        var builder = new StringBuilder(lineCount * 64);
        for (var i = 0; i < lineCount; i++)
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
}
