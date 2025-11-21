// Source: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts
// - Tests: delete CR in CRLF normalization tests (Lines: 1730+)
// Ported: 2025-11-19

using Xunit;
using PieceTree.TextBuffer;
using System.Collections.Generic;

namespace PieceTree.TextBuffer.Tests;

public class PieceTreeNormalizationTests
{
    private PieceTreeBuffer CreateTextBuffer(string[] chunks, bool normalizeEOL)
    {
        return PieceTreeBuffer.FromChunks(chunks, normalizeEOL);
    }

    [Fact]
    public void Delete_CR_In_CRLF_1()
    {
        // test('delete CR in CRLF 1', () => {
        //     const pieceTree = createTextBuffer([''], false);
        //     ds.add(pieceTree);
        //     const pieceTable = pieceTree.getPieceTree();
        //     pieceTable.insert(0, 'a\r\nb');
        //     pieceTable.delete(0, 2);
        //     assert.strictEqual(pieceTable.getLineCount(), 2);
        //     assertTreeInvariants(pieceTable);
        // });

        var buffer = CreateTextBuffer(new[] { "" }, false);
        buffer.ApplyEdit(0, 0, "a\r\nb");
        buffer.ApplyEdit(0, 2, null); // Delete "a\r"

        // Result should be "\nb"
        Assert.Equal("\nb", buffer.GetText());
        Assert.Equal("\n", buffer.GetLineContent(1));
        Assert.Equal("b", buffer.GetLineContent(2));
    }

    [Fact]
    public void Delete_CR_In_CRLF_2()
    {
        // test('delete CR in CRLF 2', () => {
        //     const pieceTree = createTextBuffer([''], false);
        //     ds.add(pieceTree);
        //     const pieceTable = pieceTree.getPieceTree();
        //     pieceTable.insert(0, 'a\r\nb');
        //     pieceTable.delete(2, 2);
        //     assert.strictEqual(pieceTable.getLineCount(), 2);
        //     assertTreeInvariants(pieceTable);
        // });

        var buffer = CreateTextBuffer(new[] { "" }, false);
        buffer.ApplyEdit(0, 0, "a\r\nb");
        buffer.ApplyEdit(2, 2, null); // Delete "\nb"

        // Result should be "a\r"
        Assert.Equal("a\r", buffer.GetText());
        Assert.Equal("a\r", buffer.GetLineContent(1));
        Assert.Equal("", buffer.GetLineContent(2));
    }

    [Fact]
    public void Line_Breaks_Replacement_Is_Not_Necessary_When_EOL_Is_Normalized()
    {
        // test('Line breaks replacement is not necessary when EOL is normalized', () => {
        //     const pieceTree = createTextBuffer(['abc']);
        //     ds.add(pieceTree);
        //     const pieceTable = pieceTree.getPieceTree();
        //     let str = 'abc';
        //     pieceTable.insert(3, 'def\nabc');
        //     str = str + 'def\nabc';
        //     testLineStarts(str, pieceTable);
        //     testLinesContent(str, pieceTable);
        //     assertTreeInvariants(pieceTable);
        // });

        var buffer = CreateTextBuffer(new[] { "abc" }, true); // normalizeEOL = true
        buffer.ApplyEdit(3, 0, "def\nabc");
        
        Assert.Equal("abcdef\nabc", buffer.GetText());
        Assert.Equal("abcdef\n", buffer.GetLineContent(1));
        Assert.Equal("abc", buffer.GetLineContent(2));
    }
}
