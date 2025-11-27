/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Migrated from: ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts
// B2-003: Port 43 TS tests to C# (39 tests, skipping 4 multi-cursor tests for Batch #3)

using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests.DocUI;

/// <summary>
/// FindModel tests migrated from TS findModel.test.ts.
/// Covers search, navigation, replace, decorations, and edge cases.
/// </summary>
public class FindModelTests
{
    // Standard test text used by most tests (matches TS findTest fixture)
    private static readonly string[] StandardTestText =
    [
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
    ];

    [Fact]
    public void Test01_IncrementalFindFromBeginningOfFile()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.SetPosition(1, 1);

            // Simulate typing 'H'
            ctx.State.Change(searchString: "H", moveCursor: true);
            ctx.AssertFindState(
                cursor: [1, 12, 1, 13],
                highlighted: [1, 12, 1, 13],
                findDecorations:
                [
                    [1, 12, 1, 13],
                    [2, 16, 2, 17],
                    [6, 14, 6, 15],
                    [6, 27, 6, 28],
                    [7, 14, 7, 15],
                    [8, 14, 8, 15],
                    [9, 14, 9, 15]
                ]
            );

            // Simulate typing 'He'
            ctx.State.Change(searchString: "He", moveCursor: true);
            ctx.AssertFindState(
                cursor: [1, 12, 1, 14],
                highlighted: [1, 12, 1, 14],
                findDecorations:
                [
                    [1, 12, 1, 14],
                    [6, 14, 6, 16],
                    [6, 27, 6, 29],
                    [7, 14, 7, 16],
                    [8, 14, 8, 16],
                    [9, 14, 9, 16]
                ]
            );

            // Simulate typing 'Hello'
            ctx.State.Change(searchString: "Hello", moveCursor: true);
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );

            // Toggle matchCase on
            ctx.State.Change(matchCase: true, moveCursor: true);
            ctx.AssertFindState(
                cursor: [6, 27, 6, 32],
                highlighted: [6, 27, 6, 32],
                findDecorations:
                [
                    [6, 27, 6, 32],
                    [8, 14, 8, 19]
                ]
            );

            // Change to 'hello' (case-sensitive)
            ctx.State.Change(searchString: "hello", moveCursor: true);
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [9, 14, 9, 19]
                ]
            );

            // Toggle wholeWord on
            ctx.State.Change(wholeWord: true, moveCursor: true);
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19]
                ]
            );

            // Toggle matchCase off
            ctx.State.Change(matchCase: false, moveCursor: true);
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            // Toggle wholeWord off
            ctx.State.Change(wholeWord: false, moveCursor: true);
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );

            // Add search scope
            ctx.State.Change(searchScope: [new Core.Range(new TextPosition(8, 1), new TextPosition(10, 1))], moveCursor: true);
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );

            // Remove search scope
            ctx.State.Change(searchScope: null, searchScopeProvided: true, moveCursor: true);
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );
        });
    }

    [Fact]
    public void Test02_FindModelRemovesItsDecorations()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", moveCursor: false);

            Assert.Equal(5, ctx.State.MatchesCount);
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );

            ctx.FindModel.Dispose();

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
        });
    }

    [Fact]
    public void Test03_FindModelUpdatesStateMatchesCount()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", moveCursor: false);

            Assert.Equal(5, ctx.State.MatchesCount);
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );

            ctx.State.Change(searchString: "helloo", moveCursor: false);
            Assert.Equal(0, ctx.State.MatchesCount);
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
        });
    }

    [Fact]
    public void Test04_FindModelReactsToPositionChange()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );

            // Move cursor to position (6, 20)
            ctx.SetPosition(6, 20);

            ctx.AssertFindState(
                cursor: [6, 20, 6, 20],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );

            ctx.State.Change(searchString: "Hello", moveCursor: true);
            ctx.AssertFindState(
                cursor: [6, 27, 6, 32],
                highlighted: [6, 27, 6, 32],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );
        });
    }

    [Fact]
    public void Test05_FindModelNext()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", wholeWord: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [6, 27, 6, 32],
                highlighted: [6, 27, 6, 32],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            // Wrap around
            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
        });
    }

    [Fact]
    public void Test06_FindModelNextStaysInScope()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(
                searchString: "hello",
                wholeWord: true,
                searchScope: [new Core.Range(new TextPosition(7, 1), new TextPosition(9, 1))],
                moveCursor: false
            );

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            // Wrap back to first match in scope
            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
        });
    }

    [Fact]
    public void Test07_MultiSelectionFindModelNextStaysInScopeOverlap()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            Range[] scopeSelections =
            [
                new Range(new TextPosition(7, 1), new TextPosition(8, 2)),
                new Range(new TextPosition(8, 1), new TextPosition(9, 1))
            ];

            ctx.SetSelections(scopeSelections);
            Range[] searchScope = ctx.GetSelections();
            ctx.SetPosition(1, 1);

            ctx.State.Change(
                searchString: "hello",
                wholeWord: true,
                searchScope: searchScope,
                searchScopeProvided: true,
                moveCursor: false
            );

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
        });
    }

    [Fact]
    public void Test08_MultiSelectionFindModelNextStaysInScope()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            Range[] scopeSelections =
            [
                new Range(new TextPosition(6, 1), new TextPosition(7, 38)),
                new Range(new TextPosition(9, 3), new TextPosition(9, 38))
            ];

            ctx.SetSelections(scopeSelections);
            Range[] searchScope = ctx.GetSelections();
            ctx.SetPosition(1, 1);

            ctx.State.Change(
                searchString: "hello",
                matchCase: true,
                searchScope: searchScope,
                searchScopeProvided: true,
                moveCursor: false
            );

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [9, 14, 9, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [9, 14, 9, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [9, 14, 9, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [9, 14, 9, 19],
                highlighted: [9, 14, 9, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [9, 14, 9, 19]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [9, 14, 9, 19]
                ]
            );
        });
    }

    [Fact]
    public void Test09_FindModelPrev()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", wholeWord: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [6, 27, 6, 32],
                highlighted: [6, 27, 6, 32],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            // Wrap around
            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
        });
    }

    [Fact]
    public void Test10_FindModelPrevStaysInScope()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(
                searchString: "hello",
                wholeWord: true,
                searchScope: [new Core.Range(new TextPosition(7, 1), new TextPosition(9, 1))],
                moveCursor: false
            );

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            // Wrap back to last match in scope
            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
        });
    }

    [Fact]
    public void Test11_FindModelNextPrevWithNoMatches()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "helloo", wholeWord: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );

            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
        });
    }

    [Fact]
    public void Test12_FindModelNextPrevRespectsCursorPosition()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", wholeWord: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            // Move cursor to (6, 20)
            ctx.SetPosition(6, 20);
            ctx.AssertFindState(
                cursor: [6, 20, 6, 20],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            // Next from (6,20) should find (6,27)
            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [6, 27, 6, 32],
                highlighted: [6, 27, 6, 32],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
        });
    }

    [Fact]
    public void Test13_Find_Caret()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "^", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [1, 1, 1, 1],
                    [2, 1, 2, 1],
                    [3, 1, 3, 1],
                    [4, 1, 4, 1],
                    [5, 1, 5, 1],
                    [6, 1, 6, 1],
                    [7, 1, 7, 1],
                    [8, 1, 8, 1],
                    [9, 1, 9, 1],
                    [10, 1, 10, 1],
                    [11, 1, 11, 1],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [2, 1, 2, 1],
                highlighted: [2, 1, 2, 1],
                findDecorations:
                [
                    [1, 1, 1, 1],
                    [2, 1, 2, 1],
                    [3, 1, 3, 1],
                    [4, 1, 4, 1],
                    [5, 1, 5, 1],
                    [6, 1, 6, 1],
                    [7, 1, 7, 1],
                    [8, 1, 8, 1],
                    [9, 1, 9, 1],
                    [10, 1, 10, 1],
                    [11, 1, 11, 1],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [3, 1, 3, 1],
                highlighted: [3, 1, 3, 1],
                findDecorations:
                [
                    [1, 1, 1, 1],
                    [2, 1, 2, 1],
                    [3, 1, 3, 1],
                    [4, 1, 4, 1],
                    [5, 1, 5, 1],
                    [6, 1, 6, 1],
                    [7, 1, 7, 1],
                    [8, 1, 8, 1],
                    [9, 1, 9, 1],
                    [10, 1, 10, 1],
                    [11, 1, 11, 1],
                    [12, 1, 12, 1]
                ]
            );
        });
    }

    [Fact]
    public void Test14_Find_Dollar()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "$", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [1, 18, 1, 18],
                    [2, 18, 2, 18],
                    [3, 20, 3, 20],
                    [4, 1, 4, 1],
                    [5, 13, 5, 13],
                    [6, 43, 6, 43],
                    [7, 41, 7, 41],
                    [8, 41, 8, 41],
                    [9, 40, 9, 40],
                    [10, 2, 10, 2],
                    [11, 17, 11, 17],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [1, 18, 1, 18],
                highlighted: [1, 18, 1, 18],
                findDecorations:
                [
                    [1, 18, 1, 18],
                    [2, 18, 2, 18],
                    [3, 20, 3, 20],
                    [4, 1, 4, 1],
                    [5, 13, 5, 13],
                    [6, 43, 6, 43],
                    [7, 41, 7, 41],
                    [8, 41, 8, 41],
                    [9, 40, 9, 40],
                    [10, 2, 10, 2],
                    [11, 17, 11, 17],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [2, 18, 2, 18],
                highlighted: [2, 18, 2, 18],
                findDecorations:
                [
                    [1, 18, 1, 18],
                    [2, 18, 2, 18],
                    [3, 20, 3, 20],
                    [4, 1, 4, 1],
                    [5, 13, 5, 13],
                    [6, 43, 6, 43],
                    [7, 41, 7, 41],
                    [8, 41, 8, 41],
                    [9, 40, 9, 40],
                    [10, 2, 10, 2],
                    [11, 17, 11, 17],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [3, 20, 3, 20],
                highlighted: [3, 20, 3, 20],
                findDecorations:
                [
                    [1, 18, 1, 18],
                    [2, 18, 2, 18],
                    [3, 20, 3, 20],
                    [4, 1, 4, 1],
                    [5, 13, 5, 13],
                    [6, 43, 6, 43],
                    [7, 41, 7, 41],
                    [8, 41, 8, 41],
                    [9, 40, 9, 40],
                    [10, 2, 10, 2],
                    [11, 17, 11, 17],
                    [12, 1, 12, 1]
                ]
            );
        });
    }

    [Fact]
    public void Test15_FindNext_CaretDollar()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "^$", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [4, 1, 4, 1],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [4, 1, 4, 1],
                highlighted: [4, 1, 4, 1],
                findDecorations:
                [
                    [4, 1, 4, 1],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [12, 1, 12, 1],
                highlighted: [12, 1, 12, 1],
                findDecorations:
                [
                    [4, 1, 4, 1],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [4, 1, 4, 1],
                highlighted: [4, 1, 4, 1],
                findDecorations:
                [
                    [4, 1, 4, 1],
                    [12, 1, 12, 1]
                ]
            );
        });
    }

    [Fact]
    public void Test16_Find_DotStar()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: ".*", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [1, 1, 1, 18],
                    [2, 1, 2, 18],
                    [3, 1, 3, 20],
                    [4, 1, 4, 1],
                    [5, 1, 5, 13],
                    [6, 1, 6, 43],
                    [7, 1, 7, 41],
                    [8, 1, 8, 41],
                    [9, 1, 9, 40],
                    [10, 1, 10, 2],
                    [11, 1, 11, 17],
                    [12, 1, 12, 1]
                ]
            );
        });
    }

    [Fact]
    public void Test17_FindNext_CaretDotStarDollar()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "^.*$", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [1, 1, 1, 18],
                    [2, 1, 2, 18],
                    [3, 1, 3, 20],
                    [4, 1, 4, 1],
                    [5, 1, 5, 13],
                    [6, 1, 6, 43],
                    [7, 1, 7, 41],
                    [8, 1, 8, 41],
                    [9, 1, 9, 40],
                    [10, 1, 10, 2],
                    [11, 1, 11, 17],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [1, 1, 1, 18],
                highlighted: [1, 1, 1, 18],
                findDecorations:
                [
                    [1, 1, 1, 18],
                    [2, 1, 2, 18],
                    [3, 1, 3, 20],
                    [4, 1, 4, 1],
                    [5, 1, 5, 13],
                    [6, 1, 6, 43],
                    [7, 1, 7, 41],
                    [8, 1, 8, 41],
                    [9, 1, 9, 40],
                    [10, 1, 10, 2],
                    [11, 1, 11, 17],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [2, 1, 2, 18],
                highlighted: [2, 1, 2, 18],
                findDecorations:
                [
                    [1, 1, 1, 18],
                    [2, 1, 2, 18],
                    [3, 1, 3, 20],
                    [4, 1, 4, 1],
                    [5, 1, 5, 13],
                    [6, 1, 6, 43],
                    [7, 1, 7, 41],
                    [8, 1, 8, 41],
                    [9, 1, 9, 40],
                    [10, 1, 10, 2],
                    [11, 1, 11, 17],
                    [12, 1, 12, 1]
                ]
            );
        });
    }

    [Fact]
    public void Test18_FindPrev_CaretDotStarDollar()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "^.*$", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [1, 1, 1, 18],
                    [2, 1, 2, 18],
                    [3, 1, 3, 20],
                    [4, 1, 4, 1],
                    [5, 1, 5, 13],
                    [6, 1, 6, 43],
                    [7, 1, 7, 41],
                    [8, 1, 8, 41],
                    [9, 1, 9, 40],
                    [10, 1, 10, 2],
                    [11, 1, 11, 17],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [12, 1, 12, 1],
                highlighted: [12, 1, 12, 1],
                findDecorations:
                [
                    [1, 1, 1, 18],
                    [2, 1, 2, 18],
                    [3, 1, 3, 20],
                    [4, 1, 4, 1],
                    [5, 1, 5, 13],
                    [6, 1, 6, 43],
                    [7, 1, 7, 41],
                    [8, 1, 8, 41],
                    [9, 1, 9, 40],
                    [10, 1, 10, 2],
                    [11, 1, 11, 17],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [11, 1, 11, 17],
                highlighted: [11, 1, 11, 17],
                findDecorations:
                [
                    [1, 1, 1, 18],
                    [2, 1, 2, 18],
                    [3, 1, 3, 20],
                    [4, 1, 4, 1],
                    [5, 1, 5, 13],
                    [6, 1, 6, 43],
                    [7, 1, 7, 41],
                    [8, 1, 8, 41],
                    [9, 1, 9, 40],
                    [10, 1, 10, 2],
                    [11, 1, 11, 17],
                    [12, 1, 12, 1]
                ]
            );
        });
    }

    [Fact]
    public void Test19_FindPrev_CaretDollar()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "^$", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [4, 1, 4, 1],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [12, 1, 12, 1],
                highlighted: [12, 1, 12, 1],
                findDecorations:
                [
                    [4, 1, 4, 1],
                    [12, 1, 12, 1]
                ]
            );

            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [4, 1, 4, 1],
                highlighted: [4, 1, 4, 1],
                findDecorations:
                [
                    [4, 1, 4, 1],
                    [12, 1, 12, 1]
                ]
            );


            ctx.FindModel.FindPrevious();
            ctx.AssertFindState(
                cursor: [12, 1, 12, 1],
                highlighted: [12, 1, 12, 1],
                findDecorations:
                [
                    [4, 1, 4, 1],
                    [12, 1, 12, 1]
                ]
            );
        });
    }

    [Fact]
    public void Test20_ReplaceHello()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", replaceString: "hi", wholeWord: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.SetPosition(6, 20);
            ctx.AssertFindState(
                cursor: [6, 20, 6, 20],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
            Assert.Equal("    cout << \"hello world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [6, 27, 6, 32],
                highlighted: [6, 27, 6, 32],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
            Assert.Equal("    cout << \"hello world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
            Assert.Equal("    cout << \"hello world, hi!\" << endl;", ctx.Model.GetLineContent(6));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [8, 14, 8, 19]
                ]
            );
            Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(7));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19]
                ]
            );
            Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(8));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [6, 16, 6, 16],
                highlighted: null,
                findDecorations: []
            );
            Assert.Equal("    cout << \"hi world, hi!\" << endl;", ctx.Model.GetLineContent(6));
        });
    }

    [Fact]
    public void Test21_ReplaceBla()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "bla", replaceString: "ciao", moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [11, 4, 11, 7],
                    [11, 7, 11, 10],
                    [11, 10, 11, 13]
                ]
            );

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [11, 4, 11, 7],
                highlighted: [11, 4, 11, 7],
                findDecorations:
                [
                    [11, 4, 11, 7],
                    [11, 7, 11, 10],
                    [11, 10, 11, 13]
                ]
            );
            Assert.Equal("// blablablaciao", ctx.Model.GetLineContent(11));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [11, 8, 11, 11],
                highlighted: [11, 8, 11, 11],
                findDecorations:
                [
                    [11, 8, 11, 11],
                    [11, 11, 11, 14]
                ]
            );
            Assert.Equal("// ciaoblablaciao", ctx.Model.GetLineContent(11));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [11, 12, 11, 15],
                highlighted: [11, 12, 11, 15],
                findDecorations:
                [
                    [11, 12, 11, 15]
                ]
            );
            Assert.Equal("// ciaociaoblaciao", ctx.Model.GetLineContent(11));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [11, 16, 11, 16],
                highlighted: null,
                findDecorations: []
            );
            Assert.Equal("// ciaociaociaociao", ctx.Model.GetLineContent(11));
        });
    }

    [Fact]
    public void Test22_ReplaceAllHello()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", replaceString: "hi", wholeWord: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.SetPosition(6, 20);
            ctx.AssertFindState(
                cursor: [6, 20, 6, 20],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
            Assert.Equal("    cout << \"hello world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

            ctx.FindModel.ReplaceAll();
            ctx.AssertFindState(
                cursor: [6, 17, 6, 17],
                highlighted: null,
                findDecorations: []
            );
            Assert.Equal("    cout << \"hi world, hi!\" << endl;", ctx.Model.GetLineContent(6));
            Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(7));
            Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(8));
        });
    }

    [Fact]
    public void Test23_ReplaceAllTwoSpacesWithOneSpace()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "  ", replaceString: " ", moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 1, 6, 3],
                    [6, 3, 6, 5],
                    [7, 1, 7, 3],
                    [7, 3, 7, 5],
                    [8, 1, 8, 3],
                    [8, 3, 8, 5],
                    [9, 1, 9, 3],
                    [9, 3, 9, 5]
                ]
            );

            ctx.FindModel.ReplaceAll();
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 1, 6, 3],
                    [7, 1, 7, 3],
                    [8, 1, 8, 3],
                    [9, 1, 9, 3]
                ]
            );
            Assert.Equal("  cout << \"hello world, Hello!\" << endl;", ctx.Model.GetLineContent(6));
            Assert.Equal("  cout << \"hello world again\" << endl;", ctx.Model.GetLineContent(7));
            Assert.Equal("  cout << \"Hello world again\" << endl;", ctx.Model.GetLineContent(8));
            Assert.Equal("  cout << \"helloworld again\" << endl;", ctx.Model.GetLineContent(9));
        });
    }

    [Fact]
    public void Test24_ReplaceAllBla()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "bla", replaceString: "ciao", moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [11, 4, 11, 7],
                    [11, 7, 11, 10],
                    [11, 10, 11, 13]
                ]
            );

            ctx.FindModel.ReplaceAll();
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
            Assert.Equal("// ciaociaociaociao", ctx.Model.GetLineContent(11));
        });
    }

    [Fact]
    public void Test25_ReplaceAllBlaWithBackslashTBackslashN()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "bla", replaceString: "<\\n\\t>", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [11, 4, 11, 7],
                    [11, 7, 11, 10],
                    [11, 10, 11, 13]
                ]
            );

            ctx.FindModel.ReplaceAll();
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
            Assert.Equal("// <", ctx.Model.GetLineContent(11));
            Assert.Equal("\t><", ctx.Model.GetLineContent(12));
            Assert.Equal("\t><", ctx.Model.GetLineContent(13));
            Assert.Equal("\t>ciao", ctx.Model.GetLineContent(14));
        });
    }

    [Fact]
    public void Test26_Issue3516_ReplaceAllMovesPageCursorFocusScrollToLastReplacement()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "include", replaceString: "bar", moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [2, 2, 2, 9],
                    [3, 2, 3, 9]
                ]
            );

            ctx.FindModel.ReplaceAll();
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );

            Assert.Equal("#bar \"cool.h\"", ctx.Model.GetLineContent(2));
            Assert.Equal("#bar <iostream>", ctx.Model.GetLineContent(3));
        });
    }

    [Fact]
    public void Test27_ListensToModelContentChanges()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", replaceString: "hi", wholeWord: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            // Replace entire content using PushEditOperations
            Range entireRange = new(new TextPosition(1, 1), ctx.Model.GetPositionAt(ctx.Model.GetLength()));
            ctx.Model.PushEditOperations([new TextEdit(entireRange.Start, entireRange.End, "hello\nhi")]);
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
        });
    }

    [Fact]
    public void Test28_SelectAllMatchesHonorsSearchScopeAndOrdersByRangeStart()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", moveCursor: false);

            Range[] scopedRanges =
            [
                new Core.Range(new TextPosition(6, 1), new TextPosition(7, ctx.Model.GetLineMaxColumn(7))),
                new Core.Range(new TextPosition(9, 1), new TextPosition(10, ctx.Model.GetLineMaxColumn(10)))
            ];

            ctx.State.Change(searchScope: scopedRanges, searchScopeProvided: true, moveCursor: false);

            Selection[] selections = ctx.FindModel.SelectAllMatches();

            Range[] expected =
            [
                CreateRange(6, 14, 6, 19),
                CreateRange(6, 27, 6, 32),
                CreateRange(7, 14, 7, 19),
                CreateRange(9, 14, 9, 19)
            ];

            Assert.Equal(expected, ToRanges(selections));
        });
    }

    [Fact]
    public void Test29_SelectAllMatchesMaintainsPrimaryCursorWhenSelectionIsMatch()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", moveCursor: false);

            ctx.FindModel.FindNext();
            ctx.FindModel.FindNext();
            ctx.FindModel.FindNext();
            ctx.FindModel.FindNext();

            Selection[] selections = ctx.FindModel.SelectAllMatches();
            Range[] ranges = ToRanges(selections);

            Assert.Equal(CreateRange(8, 14, 8, 19), ranges[0]);

            Range[] expectedTail =
            [
                CreateRange(6, 14, 6, 19),
                CreateRange(6, 27, 6, 32),
                CreateRange(7, 14, 7, 19),
                CreateRange(9, 14, 9, 19)
            ];
            Assert.Equal(expectedTail, ranges.Skip(1).ToArray());
        });
    }

    [Fact]
    public void Test30_Issue1914_NPEWhenThereIsOnlyOneFindMatch()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "cool.h", moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [2, 11, 2, 17]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [2, 11, 2, 17],
                highlighted: [2, 11, 2, 17],
                findDecorations:
                [
                    [2, 11, 2, 17]
                ]
            );

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [2, 11, 2, 17],
                highlighted: [2, 11, 2, 17],
                findDecorations:
                [
                    [2, 11, 2, 17]
                ]
            );
        });
    }

    [Fact]
    public void Test31_ReplaceWhenSearchStringHasLookAheadRegex()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello(?=\\sworld)", replaceString: "hi", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.Replace();

            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
            Assert.Equal("    cout << \"hello world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
            Assert.Equal("    cout << \"hi world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [8, 14, 8, 19]
                ]
            );
            Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(7));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [8, 16, 8, 16],
                highlighted: null,
                findDecorations: []
            );
            Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(8));
        });
    }

    [Fact]
    public void Test32_ReplaceWhenSearchStringHasLookAheadRegexAndCursorIsAtLastMatch()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello(?=\\sworld)", replaceString: "hi", isRegex: true, moveCursor: false);

            ctx.SetPosition(8, 14);

            ctx.AssertFindState(
                cursor: [8, 14, 8, 14],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.Replace();

            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            Assert.Equal("    cout << \"Hello world again\" << endl;", ctx.Model.GetLineContent(8));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19]
                ]
            );
            Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(8));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [7, 14, 7, 19]
                ]
            );
            Assert.Equal("    cout << \"hi world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [7, 16, 7, 16],
                highlighted: null,
                findDecorations: []
            );
            Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(7));
        });
    }

    [Fact]
    public void Test33_ReplaceAllWhenSearchStringHasLookAheadRegex()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello(?=\\sworld)", replaceString: "hi", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.ReplaceAll();

            Assert.Equal("    cout << \"hi world, Hello!\" << endl;", ctx.Model.GetLineContent(6));
            Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(7));
            Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(8));

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
        });
    }

    [Fact]
    public void Test34_ReplaceWhenSearchStringHasLookAheadRegexAndReplaceStringHasCaptureGroups()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hel(lo)(?=\\sworld)", replaceString: "hi$1", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.Replace();

            ctx.AssertFindState(
                cursor: [6, 14, 6, 19],
                highlighted: [6, 14, 6, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
            Assert.Equal("    cout << \"hello world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [7, 14, 7, 19],
                highlighted: [7, 14, 7, 19],
                findDecorations:
                [
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );
            Assert.Equal("    cout << \"hilo world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [8, 14, 8, 19]
                ]
            );
            Assert.Equal("    cout << \"hilo world again\" << endl;", ctx.Model.GetLineContent(7));

            ctx.FindModel.Replace();
            ctx.AssertFindState(
                cursor: [8, 18, 8, 18],
                highlighted: null,
                findDecorations: []
            );
            Assert.Equal("    cout << \"hilo world again\" << endl;", ctx.Model.GetLineContent(8));
        });
    }

    [Fact]
    public void Test35_ReplaceAllWhenSearchStringHasLookAheadRegexAndReplaceStringHasCaptureGroups()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "wo(rl)d(?=.*;$)", replaceString: "gi$1", isRegex: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 20, 6, 25],
                    [7, 20, 7, 25],
                    [8, 20, 8, 25],
                    [9, 19, 9, 24]
                ]
            );

            ctx.FindModel.ReplaceAll();

            Assert.Equal("    cout << \"hello girl, Hello!\" << endl;", ctx.Model.GetLineContent(6));
            Assert.Equal("    cout << \"hello girl again\" << endl;", ctx.Model.GetLineContent(7));
            Assert.Equal("    cout << \"Hello girl again\" << endl;", ctx.Model.GetLineContent(8));
            Assert.Equal("    cout << \"hellogirl again\" << endl;", ctx.Model.GetLineContent(9));

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
        });
    }

    [Fact]
    public void Test36_ReplaceAllWhenSearchStringIsMultilineAndHasLookAheadRegexAndReplaceStringHasCaptureGroups()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "wo(rl)d(.*;\\n)(?=.*hello)", replaceString: "gi$1$2", isRegex: true, matchCase: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 20, 7, 1],
                    [8, 20, 9, 1]
                ]
            );

            ctx.FindModel.ReplaceAll();

            Assert.Equal("    cout << \"hello girl, Hello!\" << endl;", ctx.Model.GetLineContent(6));
            Assert.Equal("    cout << \"Hello girl again\" << endl;", ctx.Model.GetLineContent(8));

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
        });
    }

    [Fact]
    public void Test37_ReplaceAllPreservingCase()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", replaceString: "goodbye", isRegex: false, matchCase: false, preserveCase: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );

            ctx.FindModel.ReplaceAll();

            Assert.Equal("    cout << \"goodbye world, Goodbye!\" << endl;", ctx.Model.GetLineContent(6));
            Assert.Equal("    cout << \"goodbye world again\" << endl;", ctx.Model.GetLineContent(7));
            Assert.Equal("    cout << \"Goodbye world again\" << endl;", ctx.Model.GetLineContent(8));
            Assert.Equal("    cout << \"goodbyeworld again\" << endl;", ctx.Model.GetLineContent(9));

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
        });
    }

    [Fact]
    public void Test38_Issue18711_ReplaceAllWithEmptyString()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", replaceString: "", wholeWord: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19]
                ]
            );

            ctx.FindModel.ReplaceAll();
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
            Assert.Equal("    cout << \" world, !\" << endl;", ctx.Model.GetLineContent(6));
            Assert.Equal("    cout << \" world again\" << endl;", ctx.Model.GetLineContent(7));
            Assert.Equal("    cout << \" world again\" << endl;", ctx.Model.GetLineContent(8));
        });
    }

    [Fact]
    public void Test39_Issue32522_ReplaceAllWithCaretOnMoreThan1000Matches()
    {
        // Generate large text with >1000 lines
        string[] largeTextLines = new string[1101];
        for (int i = 0; i < 1100; i++)
        {
            largeTextLines[i] = $"line{i}";
        }
        largeTextLines[1100] = string.Empty;

        TestEditorContext.RunTest(largeTextLines, ctx =>
        {
            ctx.State.Change(searchString: "^", replaceString: "a ", isRegex: true, moveCursor: false);
            ctx.FindModel.ReplaceAll();

            for (int i = 0; i < 1100; i++)
            {
                Assert.Equal($"a line{i}", ctx.Model.GetLineContent(i + 1));
            }
            // Last line should also have replacement
            Assert.Equal("a ", ctx.Model.GetLineContent(1101));
        });
    }

    [Fact]
    public void Test40_Issue19740_FindAndReplaceCaptureGroupBackreferenceInsertsUndefinedInsteadOfEmptyString()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello(z)?", replaceString: "hi$1", isRegex: true, matchCase: true, moveCursor: false);

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [7, 14, 7, 19],
                    [9, 14, 9, 19]
                ]
            );

            ctx.FindModel.ReplaceAll();
            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations: []
            );
            Assert.Equal("    cout << \"hi world, Hello!\" << endl;", ctx.Model.GetLineContent(6));
            Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(7));
            Assert.Equal("    cout << \"hiworld again\" << endl;", ctx.Model.GetLineContent(9));
        });
    }

    [Fact]
    public void Test41_Issue27083_SearchScopeWorksEvenIfItIsASingleLine()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(
                searchString: "hello",
                wholeWord: true,
                searchScope: [new Core.Range(new TextPosition(7, 1), new TextPosition(8, 1))],
                moveCursor: false
            );

            ctx.AssertFindState(
                cursor: [1, 1, 1, 1],
                highlighted: null,
                findDecorations:
                [
                    [7, 14, 7, 19]
                ]
            );
        });
    }

    [Fact]
    public void Test42_Issue3516_ControlBehaviorOfNextOperationsNotLoopingBackToBeginning()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", loop: false, moveCursor: false);

            Assert.Equal(5, ctx.State.MatchesCount);

            // Test next operations
            Assert.Equal(0, ctx.State.MatchesPosition);
            // Note: C# doesn't have CanNavigateForward/Back methods, so we skip those assertions

            ctx.FindModel.FindNext();
            Assert.Equal(1, ctx.State.MatchesPosition);

            ctx.FindModel.FindNext();
            Assert.Equal(2, ctx.State.MatchesPosition);

            ctx.FindModel.FindNext();
            Assert.Equal(3, ctx.State.MatchesPosition);

            ctx.FindModel.FindNext();
            Assert.Equal(4, ctx.State.MatchesPosition);

            ctx.FindModel.FindNext();
            Assert.Equal(5, ctx.State.MatchesPosition);

            // Should not loop - stay at position 5
            ctx.FindModel.FindNext();
            Assert.Equal(5, ctx.State.MatchesPosition);

            ctx.FindModel.FindNext();
            Assert.Equal(5, ctx.State.MatchesPosition);

            // Test previous operations
            ctx.FindModel.FindPrevious();
            Assert.Equal(4, ctx.State.MatchesPosition);

            ctx.FindModel.FindPrevious();
            Assert.Equal(3, ctx.State.MatchesPosition);

            ctx.FindModel.FindPrevious();
            Assert.Equal(2, ctx.State.MatchesPosition);

            ctx.FindModel.FindPrevious();
            Assert.Equal(1, ctx.State.MatchesPosition);

            // Should not loop - stay at position 1
            ctx.FindModel.FindPrevious();
            Assert.Equal(1, ctx.State.MatchesPosition);

            ctx.FindModel.FindPrevious();
            Assert.Equal(1, ctx.State.MatchesPosition);
        });
    }

    [Fact]
    public void Test43_Issue3516_ControlBehaviorOfNextOperationsLoopingBackToBeginning()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", moveCursor: false);

            Assert.Equal(5, ctx.State.MatchesCount);

            // Test next operations
            Assert.Equal(0, ctx.State.MatchesPosition);

            ctx.FindModel.FindNext();
            Assert.Equal(1, ctx.State.MatchesPosition);

            ctx.FindModel.FindNext();
            Assert.Equal(2, ctx.State.MatchesPosition);

            ctx.FindModel.FindNext();
            Assert.Equal(3, ctx.State.MatchesPosition);

            ctx.FindModel.FindNext();
            Assert.Equal(4, ctx.State.MatchesPosition);

            ctx.FindModel.FindNext();
            Assert.Equal(5, ctx.State.MatchesPosition);

            // Loop back to 1
            ctx.FindModel.FindNext();
            Assert.Equal(1, ctx.State.MatchesPosition);

            ctx.FindModel.FindNext();
            Assert.Equal(2, ctx.State.MatchesPosition);

            // Test previous operations
            ctx.FindModel.FindPrevious();
            Assert.Equal(1, ctx.State.MatchesPosition);

            // Loop back to 5
            ctx.FindModel.FindPrevious();
            Assert.Equal(5, ctx.State.MatchesPosition);

            ctx.FindModel.FindPrevious();
            Assert.Equal(4, ctx.State.MatchesPosition);

            ctx.FindModel.FindPrevious();
            Assert.Equal(3, ctx.State.MatchesPosition);

            ctx.FindModel.FindPrevious();
            Assert.Equal(2, ctx.State.MatchesPosition);

            ctx.FindModel.FindPrevious();
            Assert.Equal(1, ctx.State.MatchesPosition);
        });
    }

    [Fact]
    public void Test44_WholeWordRespectsCustomWordSeparators()
    {
        string[] text = ["alpha-beta alpha"];
        TestEditorContextOptions options = new()
        {
            WordSeparators = " \t"
        };

        TestEditorContext.RunTest(text, ctx =>
        {
            ctx.State.Change(searchString: "alpha", wholeWord: true, moveCursor: false);
            Assert.Equal(1, ctx.State.MatchesCount);

            ctx.State.Change(wholeWord: false, moveCursor: false);
            Assert.Equal(2, ctx.State.MatchesCount);
        }, options);
    }

    [Fact]
    public void Test45_SearchScopeTracksEditsAfterTyping()
    {
        string[] text = ["alpha beta gamma"];

        TestEditorContext.RunTest(text, ctx =>
        {
            ctx.State.Change(searchString: "beta", moveCursor: false);

            Range[] scope =
            [
                new Core.Range(new TextPosition(1, 7), new TextPosition(1, 11))
            ];
            ctx.State.Change(searchScope: scope, searchScopeProvided: true, moveCursor: false);

            Assert.Equal(1, ctx.State.MatchesCount);

            ctx.Model.PushEditOperations(
            [
                new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "delta ")
            ]);

            Assert.Equal(1, ctx.State.MatchesCount);

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [1, 13, 1, 17],
                highlighted: [1, 13, 1, 17],
                findDecorations:
                [
                    [1, 13, 1, 17]
                ]
            );
        });
    }

    // Regression guard for TS issue #27083 – multi-line scopes normalize to full lines.
    [Fact]
    public void Test46_MultilineScopeIsNormalizedToFullLines()
    {
        string[] text =
        [
            "alpha and omega",
            "second line"
        ];

        TestEditorContext.RunTest(text, ctx =>
        {
            ctx.State.Change(searchString: "alpha", moveCursor: false);

            Range[] scope =
            [
                new Core.Range(new TextPosition(1, 4), new TextPosition(2, 1))
            ];
            ctx.State.Change(searchScope: scope, searchScopeProvided: true, moveCursor: true);

            Assert.Equal(1, ctx.State.MatchesCount);

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [1, 1, 1, 6],
                highlighted: [1, 1, 1, 6],
                findDecorations:
                [
                    [1, 1, 1, 6]
                ]
            );
        });
    }

    [Fact]
    public void Test47_RegexReplaceWithinScopeUsesLiveRangesAfterEdit()
    {
        string[] text =
        [
            "scope header",
            "match capture",
            string.Empty
        ];

        TestEditorContext.RunTest(text, ctx =>
        {
            ctx.State.Change(searchString: "(match)", replaceString: "$1!", isRegex: true, moveCursor: false);

            Range[] scope =
            [
                new Core.Range(new TextPosition(2, 1), new TextPosition(2, 14))
            ];
            ctx.State.Change(searchScope: scope, searchScopeProvided: true, moveCursor: true);

            ctx.FindModel.FindNext();

            ctx.Model.PushEditOperations(
            [
                new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "intro\n")
            ]);

            ctx.FindModel.FindNext();
            ctx.FindModel.Replace();

            Assert.Equal("match! capture", ctx.Model.GetLineContent(3));
        });
    }

    [Fact]
    public void Test48_FlushEditKeepsFindNextProgress()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", moveCursor: false);
            ctx.SetPosition(8, 1);

            ctx.FindModel.FindNext();
            ctx.AssertFindState(
                cursor: [8, 14, 8, 19],
                highlighted: [8, 14, 8, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );
            Assert.Equal(4, ctx.State.MatchesPosition);

            TextPosition documentEnd = ctx.Model.GetPositionAt(ctx.Model.GetLength());
            string entireDocument = ctx.Model.GetValue();
            ctx.Model.PushEditOperations(
            [
                new TextEdit(new TextPosition(1, 1), documentEnd, entireDocument)
            ]);

            ctx.FindModel.FindNext();

            ctx.AssertFindState(
                cursor: [9, 14, 9, 19],
                highlighted: [9, 14, 9, 19],
                findDecorations:
                [
                    [6, 14, 6, 19],
                    [6, 27, 6, 32],
                    [7, 14, 7, 19],
                    [8, 14, 8, 19],
                    [9, 14, 9, 19]
                ]
            );
            Assert.Equal(5, ctx.State.MatchesPosition);
        });
    }

    [Fact]
    public void Test49_SelectAllMatchesRespectsPrimarySelectionOrder()
    {
        TestEditorContext.RunTest(StandardTestText, ctx =>
        {
            ctx.State.Change(searchString: "hello", moveCursor: false);

            Range primary = CreateRange(7, 14, 7, 19);
            Range secondary = CreateRange(6, 27, 6, 32);
            ctx.SetSelections(primary, secondary);

            Selection[] selections = ctx.FindModel.SelectAllMatches();
            Range[] ranges = ToRanges(selections);

            Assert.Equal(primary, ranges[0]);

            Range[] expectedTail =
            [
                CreateRange(6, 14, 6, 19),
                CreateRange(6, 27, 6, 32),
                CreateRange(8, 14, 8, 19),
                CreateRange(9, 14, 9, 19)
            ];

            Assert.Equal(expectedTail, ranges.Skip(1).ToArray());
        });
    }

    private static Range[] ToRanges(Selection[] selections)
    {
        Range[] result = new Range[selections.Length];
        for (int i = 0; i < selections.Length; i++)
        {
            result[i] = new Range(selections[i].Start, selections[i].End);
        }
        return result;
    }

    private static Range CreateRange(int startLine, int startColumn, int endLine, int endColumn)
    {
        return new Range(new TextPosition(startLine, startColumn), new TextPosition(endLine, endColumn));
    }
}
