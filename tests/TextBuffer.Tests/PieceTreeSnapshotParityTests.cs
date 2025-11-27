// Source: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// - Tests: bug #45564 (piece immutability) + immutable snapshot 1/2/3
// Ported: 2025-11-25

using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Tests.Helpers;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeSnapshotParityTests
{
    [Fact]
    public void Bug45564_PieceTreePiecesRemainImmutable()
    {
        // Mirrors TS "bug #45564, piece tree pieces should be immutable" script.
        TextModel model = new("\n");
        model.ApplyEdits([Edit(2, 1, 2, 1, "!")]);

        ITextSnapshot snapshotA = model.CreateSnapshot();
        ITextSnapshot snapshotB = model.CreateSnapshot();
        Assert.Equal(model.GetValue(), Drain(snapshotA));

        model.ApplyEdits([Edit(2, 1, 2, 2, string.Empty)]);
        model.ApplyEdits([Edit(2, 1, 2, 1, "!")]);

        Assert.Equal(model.GetValue(), Drain(snapshotB));
    }

    [Fact]
    public void ImmutableSnapshot_RangeDeletionRoundTrip()
    {
        // Mirrors TS "immutable snapshot 1" – delete range then reinsert original payload.
        TextModel model = new("abc\ndef");
        ITextSnapshot snapshot = model.CreateSnapshot();

        model.ApplyEdits([Edit(2, 1, 2, 4, string.Empty)]);
        model.ApplyEdits([Edit(1, 1, 2, 1, "abc\ndef")]);

        string expected = "abc\ndef";
        Assert.Equal(expected, model.GetValue());
        Assert.Equal(expected, Drain(snapshot));
    }

    [Fact]
    public void ImmutableSnapshot_InsertAfterDeletion()
    {
        // Mirrors TS "immutable snapshot 2" – insert + delete returns to original state.
        TextModel model = new("abc\ndef");
        ITextSnapshot snapshot = model.CreateSnapshot();

        model.ApplyEdits([Edit(2, 1, 2, 1, "!")]);
        model.ApplyEdits([Edit(2, 1, 2, 2, string.Empty)]);

        string expected = "abc\ndef";
        Assert.Equal(expected, model.GetValue());
        Assert.Equal(expected, Drain(snapshot));
    }

    [Fact]
    public void ImmutableSnapshot_DetectsSubsequentMutations()
    {
        // Mirrors TS "immutable snapshot 3" – snapshot freezes the first mutation.
        TextModel model = new("abc\ndef");
        model.ApplyEdits([Edit(2, 4, 2, 4, "!")]);
        ITextSnapshot snapshot = model.CreateSnapshot();

        model.ApplyEdits([Edit(2, 5, 2, 5, "!")]);

        Assert.NotEqual(model.GetValue(), Drain(snapshot));
    }

    private static TextEdit Edit(int startLine, int startColumn, int endLine, int endColumn, string text)
    {
        TextPosition start = new(startLine, startColumn);
        TextPosition end = new(endLine, endColumn);
        return new TextEdit(start, end, text);
    }

    private static string Drain(ITextSnapshot snapshot) => SnapshotReader.ReadAll(snapshot);
}
