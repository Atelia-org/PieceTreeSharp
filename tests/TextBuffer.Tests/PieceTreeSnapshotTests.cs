// Source: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// - Tests: Snapshot immutability and content reading
// Ported: 2025-11-19 (updated 2025-11-25 to use TextModel snapshots)

using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Tests.Helpers;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeSnapshotTests
{
    [Fact]
    public void SnapshotReadsContent()
    {
        TextModel model = new("Hello World");
        ITextSnapshot snapshot = model.CreateSnapshot();

        Assert.Equal("Hello World", SnapshotReader.ReadAll(snapshot));
    }

    [Fact]
    public void SnapshotIsImmutable()
    {
        TextModel model = new("Hello");
        ITextSnapshot snapshot = model.CreateSnapshot();
        string snapshotContent = SnapshotReader.ReadAll(snapshot);
        Assert.Equal("Hello", snapshotContent);

        model.ApplyEdits(
        [
            new TextEdit(new TextPosition(1, 6), new TextPosition(1, 6), " World")
        ]);

        Assert.Equal("Hello", snapshotContent);
        Assert.Equal("Hello World", model.GetValue());
    }
}
