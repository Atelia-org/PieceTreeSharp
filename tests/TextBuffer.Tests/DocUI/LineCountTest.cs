using System;
using Xunit;
using PieceTree.TextBuffer;

namespace PieceTree.TextBuffer.Tests.DocUI
{
    public class LineCountTest
    {
        [Fact]
        public void TestLineCount()
        {
            // Test 1: "a\nb" should be 2 lines
            var text1 = "a\nb";
            var model1 = new TextModel(text1);
            Assert.Equal(2, model1.GetLineCount());
            
            // Test 2: "a\nb\n" should be 3 lines (with trailing empty line)
            var text2 = "a\nb\n";
            var model2 = new TextModel(text2);
            Assert.Equal(3, model2.GetLineCount());
            Assert.Equal(string.Empty, model2.GetLineContent(3));
            
            // Test 3: StandardTestText
            var lines = new[] {
                "// my cool header",
                "#include \"cool.h\"",
                "#include <iostream>",
                "",
                "int main() {",
                "    cout << \"hello world, Hello!\" << endl;",
                "    cout << \"hello world again\" << endl;",
                "    cout << \"Hello world again\" << endl;",
                "    cout << \"helloworld again\" << endl;",
                "}",
                "// blablablaciao",
                ""
            };
            var text3 = string.Join("\n", lines);
            var model3 = new TextModel(text3);
            Assert.Equal(lines.Length, model3.GetLineCount());
            Assert.Equal(string.Empty, model3.GetLineContent(lines.Length));
        }
    }
}
