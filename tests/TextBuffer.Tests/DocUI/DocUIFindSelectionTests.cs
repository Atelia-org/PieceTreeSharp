/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Ported from ts/src/vs/editor/contrib/find/test/browser/find.test.ts

using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.DocUI;

namespace PieceTree.TextBuffer.Tests.DocUI;

public class DocUIFindSelectionTests
{
    private static readonly string[] SampleText =
    [
        "ABC DEF",
        "0123 456"
    ];

    [Fact]
    public void SearchStringAtPositionReturnsWordUnderCursor()
    {
        SelectionTestContext context = new(SampleText);

        Assert.Equal("ABC", FindUtilities.GetSelectionSearchString(context));

        context.SetPosition(1, 3);
        Assert.Equal("ABC", FindUtilities.GetSelectionSearchString(context));

        context.SetPosition(1, 5);
        Assert.Equal("DEF", FindUtilities.GetSelectionSearchString(context));
    }

    [Fact]
    public void SearchStringWithSingleLineSelectionReturnsSelectionText()
    {
        SelectionTestContext context = new(SampleText);

        context.SetSelection(1, 1, 1, 2);
        Assert.Equal("A", FindUtilities.GetSelectionSearchString(context));

        context.SetSelection(1, 2, 1, 4);
        Assert.Equal("BC", FindUtilities.GetSelectionSearchString(context));

        context.SetSelection(1, 2, 1, 7);
        Assert.Equal("BC DE", FindUtilities.GetSelectionSearchString(context));
    }

    [Fact]
    public void SearchStringWithMultilineSelectionReturnsNull()
    {
        SelectionTestContext context = new(SampleText);

        context.SetSelection(1, 1, 2, 1);
        Assert.Null(FindUtilities.GetSelectionSearchString(context));

        context.SetSelection(1, 1, 2, 4);
        Assert.Null(FindUtilities.GetSelectionSearchString(context));

        context.SetSelection(1, 7, 2, 4);
        Assert.Null(FindUtilities.GetSelectionSearchString(context));
    }

    [Fact]
    public void SearchStringRespectsCustomWordSeparatorsForHyphenatedWords()
    {
        SelectionTestContext context = new(["error-code delta"], wordSeparators: " \t");
        context.SetPosition(1, 6);

        Assert.Equal("error-code", FindUtilities.GetSelectionSearchString(context));
    }
}

internal sealed class SelectionTestContext : IEditorSelectionContext
{
    public TextModel Model { get; }
    public Selection Selection { get; private set; }
    public string? WordSeparators { get; }

    public SelectionTestContext(string[] lines, string? wordSeparators = null)
    {
        Model = new TextModel(string.Join("\n", lines));
        WordSeparators = wordSeparators;
        Selection = new Selection(new TextPosition(1, 1), new TextPosition(1, 1));
    }

    public void SetPosition(int lineNumber, int column)
    {
        Selection = new Selection(lineNumber, column, lineNumber, column);
    }

    public void SetSelection(int startLine, int startColumn, int endLine, int endColumn)
    {
        Selection = new Selection(startLine, startColumn, endLine, endColumn);
    }
}
