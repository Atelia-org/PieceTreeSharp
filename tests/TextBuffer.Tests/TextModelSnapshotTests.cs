// Original C# tests
// Purpose: Validate TextModelSnapshot chunk aggregation semantics
// Created: 2025-11-25 (ports ts/src/vs/editor/common/model/textModel.ts TextModelSnapshot cases)

using System.Collections.Generic;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using Xunit;

namespace PieceTree.TextBuffer.Tests;

public class TextModelSnapshotTests
{
    [Fact]
    public void TextModelCreateSnapshotReturnsWrapper()
    {
        var model = new TextModel("abc");
        var snapshot = model.CreateSnapshot();

        Assert.IsType<TextModelSnapshot>(snapshot);
        Assert.Equal("abc", snapshot.Read());
        Assert.Null(snapshot.Read());
    }

    [Fact]
    public void AggregatesChunksUntilThreshold()
    {
        const int chunk = 16 * 1024;
        var fake = new FakeSnapshot(new string?[]
        {
            new string('A', chunk),
            new string('B', chunk),
            new string('C', chunk),
            new string('D', chunk),
            new string('E', chunk),
            new string('F', chunk),
            null,
        });

        var snapshot = new TextModelSnapshot(fake);
        var first = snapshot.Read();
        Assert.Equal(64 * 1024, first!.Length);
        Assert.Equal(new string('A', chunk) + new string('B', chunk) + new string('C', chunk) + new string('D', chunk), first);

        var second = snapshot.Read();
        Assert.Equal(32 * 1024, second!.Length);
        Assert.Equal(new string('E', chunk) + new string('F', chunk), second);

        Assert.Null(snapshot.Read());
        Assert.Equal(7, fake.ReadCount);
    }

    [Fact]
    public void SkipsEmptyChunksAndDrainsSource()
    {
        var fake = new FakeSnapshot(new string?[] { string.Empty, "foo", string.Empty, "bar", null });
        var snapshot = new TextModelSnapshot(fake);

        Assert.Equal("foobar", snapshot.Read());
        Assert.Null(snapshot.Read());
        Assert.Equal(5, fake.ReadCount);
    }

    [Fact]
    public void RepeatedReadsAfterEosDoNotTouchSource()
    {
        var fake = new FakeSnapshot(new string?[] { null });
        var snapshot = new TextModelSnapshot(fake);

        Assert.Null(snapshot.Read());
        Assert.Equal(1, fake.ReadCount);

        Assert.Null(snapshot.Read());
        Assert.Equal(1, fake.ReadCount);
    }

    private sealed class FakeSnapshot : ITextSnapshot
    {
        private readonly Queue<string?> _chunks;

        public FakeSnapshot(IEnumerable<string?> chunks)
        {
            _chunks = new Queue<string?>(chunks);
        }

        public int ReadCount { get; private set; }

        public string? Read()
        {
            ReadCount++;
            return _chunks.Count == 0 ? null : _chunks.Dequeue();
        }
    }
}
