using System;
using System.Text.RegularExpressions;
using Xunit;

namespace PieceTree.TextBuffer.Tests.DocUI
{
    public class RegexTest
    {
        [Fact]
        public void TestCaretRegex()
        {
            var text = string.Join("\n", new[] { "a", "b", "c", "" });
            var regex = new Regex("^", RegexOptions.Multiline);
            var matches = regex.Matches(text);

            Assert.Equal(4, matches.Count);
            Assert.Collection(matches,
                m => Assert.Equal(0, m.Index),
                m => Assert.Equal(2, m.Index),
                m => Assert.Equal(4, m.Index),
                m => Assert.Equal(6, m.Index));
        }
    }
}
