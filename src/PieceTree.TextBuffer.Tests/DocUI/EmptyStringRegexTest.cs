using System;
using System.Text.RegularExpressions;
using Xunit;

namespace PieceTree.TextBuffer.Tests.DocUI
{
    public class EmptyStringRegexTest
    {
        [Fact]
        public void TestCaretOnEmptyString()
        {
            var text = "";
            var regex = new Regex("^", RegexOptions.Multiline);
            var matches = regex.Matches(text);

            Assert.Single(matches);
            Assert.Equal(0, matches[0].Index);
            Assert.Equal(0, matches[0].Length);
        }
    }
}
