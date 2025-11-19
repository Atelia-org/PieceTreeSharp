using Xunit;
using PieceTree.TextBuffer.Core;
using System.Collections.Generic;
using System.Linq;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeSearchTests
{
    private PieceTreeModel CreateModel(string text)
    {
        var buildResult = PieceTreeBuilder.BuildFromChunks(new[] { text });
        return buildResult.Model;
    }

    [Fact]
    public void TestBasicStringFind()
    {
        var model = CreateModel("foo bar foo");
        var searchParams = new SearchParams("foo", false, false, null);
        var searchData = searchParams.ParseSearchRequest();
        
        var matches = model.FindMatchesLineByLine(new Range(1, 1, 1, 12), searchData!, false, 1000);
        
        Assert.Equal(2, matches.Count);
        Assert.Equal(1, matches[0].Range.Start.LineNumber);
        Assert.Equal(1, matches[0].Range.Start.Column);
        Assert.Equal(1, matches[0].Range.End.LineNumber);
        Assert.Equal(4, matches[0].Range.End.Column);
        
        Assert.Equal(1, matches[1].Range.Start.LineNumber);
        Assert.Equal(9, matches[1].Range.Start.Column);
        Assert.Equal(1, matches[1].Range.End.LineNumber);
        Assert.Equal(12, matches[1].Range.End.Column);
    }

    [Fact]
    public void TestRegexFind()
    {
        var model = CreateModel("foo bar foo");
        var searchParams = new SearchParams("f[o]+", true, false, null);
        var searchData = searchParams.ParseSearchRequest();
        
        var matches = model.FindMatchesLineByLine(new Range(1, 1, 1, 12), searchData!, false, 1000);
        
        Assert.Equal(2, matches.Count);
        Assert.Equal(1, matches[0].Range.Start.Column);
        Assert.Equal(9, matches[1].Range.Start.Column);
    }

    [Fact]
    public void TestMultilineFind()
    {
        var model = CreateModel("foo\nbar\nfoo");
        var searchParams = new SearchParams("bar", false, false, null);
        var searchData = searchParams.ParseSearchRequest();
        
        var matches = model.FindMatchesLineByLine(new Range(1, 1, 3, 4), searchData!, false, 1000);
        
        Assert.Single(matches);
        Assert.Equal(2, matches[0].Range.Start.LineNumber);
        Assert.Equal(1, matches[0].Range.Start.Column);
    }
}
