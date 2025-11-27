using System;
using System.Text.RegularExpressions;
using Xunit;

namespace PieceTree.TextBuffer.Tests.DocUI;

public class EmptyStringRegexTest
{
    [Fact]
    public void TestCaretOnEmptyString()
    {
        string text = "";
        Regex regex = new("^", RegexOptions.Multiline);
        MatchCollection matches = regex.Matches(text);

        Assert.Single(matches);
        Assert.Equal(0, matches[0].Index);
        Assert.Equal(0, matches[0].Length);
    }
}
