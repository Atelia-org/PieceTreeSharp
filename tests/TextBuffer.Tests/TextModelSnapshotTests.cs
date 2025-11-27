// Original C# tests
// Purpose: Validate TextModelSnapshot chunk aggregation semantics
// Created: 2025-11-25 (ports ts/src/vs/editor/common/model/textModel.ts TextModelSnapshot cases)

using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests;

public class TextModelSnapshotTests
{
    [Fact]
    public void TextModelCreateSnapshotReturnsWrapper()
    {
        TextModel model = new("abc");
        ITextSnapshot snapshot = model.CreateSnapshot();

        Assert.IsType<TextModelSnapshot>(snapshot);
        Assert.Equal("abc", snapshot.Read());
        Assert.Null(snapshot.Read());
    }

    [Fact]
    public void AggregatesChunksUntilThreshold()
    {
        const int chunk = 16 * 1024;
        FakeSnapshot fake = new(new string?[]
        {
            new('A', chunk),
            new('B', chunk),
            new('C', chunk),
            new('D', chunk),
            new('E', chunk),
            new('F', chunk),
            null,
        });

        TextModelSnapshot snapshot = new(fake);
        string? first = snapshot.Read();
        Assert.Equal(64 * 1024, first!.Length);
        Assert.Equal(new string('A', chunk) + new string('B', chunk) + new string('C', chunk) + new string('D', chunk), first);

        string? second = snapshot.Read();
        Assert.Equal(32 * 1024, second!.Length);
        Assert.Equal(new string('E', chunk) + new string('F', chunk), second);

        Assert.Null(snapshot.Read());
        Assert.Equal(7, fake.ReadCount);
    }

    [Fact]
    public void SkipsEmptyChunksAndDrainsSource()
    {
        FakeSnapshot fake = new(new string?[] { string.Empty, "foo", string.Empty, "bar", null });
        TextModelSnapshot snapshot = new(fake);

        Assert.Equal("foobar", snapshot.Read());
        Assert.Null(snapshot.Read());
        Assert.Equal(5, fake.ReadCount);
    }

    [Fact]
    public void RepeatedReadsAfterEosDoNotTouchSource()
    {
        FakeSnapshot fake = new(new string?[] { null });
        TextModelSnapshot snapshot = new(fake);

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
