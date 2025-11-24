// Source: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// - Tests: bug #45564 (piece immutability) + immutable snapshot 1/2/3
// Ported: 2025-11-25

using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Tests.Helpers;
using Xunit;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeSnapshotParityTests
{
    [Fact]
    public void Bug45564_PieceTreePiecesRemainImmutable()
    {
        // Mirrors TS "bug #45564, piece tree pieces should be immutable" script.
        var model = new TextModel("\n");
        model.ApplyEdits(new[] { Edit(2, 1, 2, 1, "!") });

        var snapshotA = model.CreateSnapshot();
        var snapshotB = model.CreateSnapshot();
        Assert.Equal(model.GetValue(), Drain(snapshotA));

        model.ApplyEdits(new[] { Edit(2, 1, 2, 2, string.Empty) });
        model.ApplyEdits(new[] { Edit(2, 1, 2, 1, "!") });

        Assert.Equal(model.GetValue(), Drain(snapshotB));
    }

    [Fact]
    public void ImmutableSnapshot_RangeDeletionRoundTrip()
    {
        // Mirrors TS "immutable snapshot 1" – delete range then reinsert original payload.
        var model = new TextModel("abc\ndef");
        var snapshot = model.CreateSnapshot();

        model.ApplyEdits(new[] { Edit(2, 1, 2, 4, string.Empty) });
        model.ApplyEdits(new[] { Edit(1, 1, 2, 1, "abc\ndef") });

        var expected = "abc\ndef";
        Assert.Equal(expected, model.GetValue());
        Assert.Equal(expected, Drain(snapshot));
    }

    [Fact]
    public void ImmutableSnapshot_InsertAfterDeletion()
    {
        // Mirrors TS "immutable snapshot 2" – insert + delete returns to original state.
        var model = new TextModel("abc\ndef");
        var snapshot = model.CreateSnapshot();

        model.ApplyEdits(new[] { Edit(2, 1, 2, 1, "!") });
        model.ApplyEdits(new[] { Edit(2, 1, 2, 2, string.Empty) });

        var expected = "abc\ndef";
        Assert.Equal(expected, model.GetValue());
        Assert.Equal(expected, Drain(snapshot));
    }

    [Fact]
    public void ImmutableSnapshot_DetectsSubsequentMutations()
    {
        // Mirrors TS "immutable snapshot 3" – snapshot freezes the first mutation.
        var model = new TextModel("abc\ndef");
        model.ApplyEdits(new[] { Edit(2, 4, 2, 4, "!") });
        var snapshot = model.CreateSnapshot();

        model.ApplyEdits(new[] { Edit(2, 5, 2, 5, "!") });

        Assert.NotEqual(model.GetValue(), Drain(snapshot));
    }

    private static TextEdit Edit(int startLine, int startColumn, int endLine, int endColumn, string text)
    {
        var start = new TextPosition(startLine, startColumn);
        var end = new TextPosition(endLine, endColumn);
        return new TextEdit(start, end, text);
    }

    private static string Drain(ITextSnapshot snapshot) => SnapshotReader.ReadAll(snapshot);
}
