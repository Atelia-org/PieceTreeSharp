using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using Xunit;
using Range = PieceTree.TextBuffer.Core.Range;

namespace TextBuffer.Tests;

/// <summary>
/// Data record for TextModelData test results.
/// </summary>
public record TextBufferData(
    string[] Lines,
    string Eol,
    bool IsBasicAscii,
    bool ContainsRtl
);

/// <summary>
/// Tests for TextModelData functionality using PieceTreeBuffer as the underlying implementation.
/// </summary>
public class TextModelDataTests
{
    /// <summary>
    /// Helper method to create TextBufferData from a string using PieceTreeBuffer.
    /// </summary>
    private static TextBufferData GetTextModelData(string text)
    {
        var buffer = new PieceTreeBuffer(text);
        return new TextBufferData(
            Lines: buffer.GetLinesContent(),
            Eol: buffer.GetEol(),
            IsBasicAscii: !buffer.MightContainNonBasicAscii(),
            ContainsRtl: buffer.MightContainRtl()
        );
    }

    [Fact]
    public void OneLineText()
    {
        var data = GetTextModelData("Hello world!");

        Assert.Single(data.Lines);
        Assert.Equal("Hello world!", data.Lines[0]);
        Assert.Equal("\n", data.Eol);
        Assert.True(data.IsBasicAscii);
        Assert.False(data.ContainsRtl);
    }

    [Fact]
    public void MultilineText()
    {
        var data = GetTextModelData("Hello,\r\ndear friend\nHow\rare\r\nyou?");

        Assert.Equal(5, data.Lines.Length);
        Assert.Equal("Hello,", data.Lines[0]);
        Assert.Equal("dear friend", data.Lines[1]);
        Assert.Equal("How", data.Lines[2]);
        Assert.Equal("are", data.Lines[3]);
        Assert.Equal("you?", data.Lines[4]);
        Assert.Equal("\r\n", data.Eol);
        Assert.True(data.IsBasicAscii);
        Assert.False(data.ContainsRtl);
    }

    [Fact]
    public void NonBasicASCII()
    {
        var data = GetTextModelData("Hello,\nZürich");

        Assert.Equal(2, data.Lines.Length);
        Assert.Equal("Hello,", data.Lines[0]);
        Assert.Equal("Zürich", data.Lines[1]);
        Assert.Equal("\n", data.Eol);
        Assert.False(data.IsBasicAscii);
        Assert.False(data.ContainsRtl);
    }

    [Fact]
    public void ContainsRTL_Hebrew()
    {
        var data = GetTextModelData("Hello,\nזוהי עובדה מבוססת שדעתו");

        Assert.Equal(2, data.Lines.Length);
        Assert.Equal("Hello,", data.Lines[0]);
        Assert.Equal("זוהי עובדה מבוססת שדעתו", data.Lines[1]);
        Assert.Equal("\n", data.Eol);
        Assert.False(data.IsBasicAscii);
        Assert.True(data.ContainsRtl);
    }

    [Fact]
    public void ContainsRTL_Arabic()
    {
        var data = GetTextModelData("Hello,\nهناك حقيقة مثبتة منذ زمن طويل");

        Assert.Equal(2, data.Lines.Length);
        Assert.Equal("Hello,", data.Lines[0]);
        Assert.Equal("هناك حقيقة مثبتة منذ زمن طويل", data.Lines[1]);
        Assert.Equal("\n", data.Eol);
        Assert.False(data.IsBasicAscii);
        Assert.True(data.ContainsRtl);
    }

    #region GetValueLengthInRange Tests

    [Fact]
    public void GetValueLengthInRange_SingleLine()
    {
        var model = new TextModel("Hello World");
        var range = new Range(new TextPosition(1, 1), new TextPosition(1, 6)); // "Hello"

        int length = model.GetValueLengthInRange(range);

        Assert.Equal(5, length);
    }

    [Fact]
    public void GetValueLengthInRange_MultiLine_TextDefined()
    {
        // Using LF as EOL (default)
        var model = new TextModel("Hello\nWorld");
        var range = new Range(new TextPosition(1, 1), new TextPosition(2, 6)); // "Hello\nWorld"

        int length = model.GetValueLengthInRange(range, EndOfLinePreference.TextDefined);

        Assert.Equal(11, length); // 5 (Hello) + 1 (\n) + 5 (World)
    }

    [Fact]
    public void GetValueLengthInRange_MultiLine_LF()
    {
        // Document has CRLF, but we request LF
        var model = new TextModel("Hello\r\nWorld");
        var range = new Range(new TextPosition(1, 1), new TextPosition(2, 6)); // Full content

        int length = model.GetValueLengthInRange(range, EndOfLinePreference.LF);

        // Document offset is 12 (5 + 2 + 5), but LF preference subtracts 1 per line break
        Assert.Equal(11, length); // 5 (Hello) + 1 (\n) + 5 (World)
    }

    [Fact]
    public void GetValueLengthInRange_MultiLine_CRLF()
    {
        // Document has LF, but we request CRLF
        var model = new TextModel("Hello\nWorld");
        var range = new Range(new TextPosition(1, 1), new TextPosition(2, 6)); // Full content

        int length = model.GetValueLengthInRange(range, EndOfLinePreference.CRLF);

        // Document offset is 11 (5 + 1 + 5), but CRLF preference adds 1 per line break
        Assert.Equal(12, length); // 5 (Hello) + 2 (\r\n) + 5 (World)
    }

    [Fact]
    public void GetValueLengthInRange_EmptyRange()
    {
        var model = new TextModel("Hello World");
        var range = new Range(new TextPosition(1, 3), new TextPosition(1, 3)); // Empty range

        int length = model.GetValueLengthInRange(range);

        Assert.Equal(0, length);
    }

    #endregion
}
