/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Migrated from: ts/src/vs/editor/contrib/find/test/browser/findModel.test.ts
// B2-003: Port 43 TS tests to C# (39 tests, skipping 4 multi-cursor tests for Batch #3)

using System;
using System.Linq;
using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.DocUI;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests.DocUI
{
    /// <summary>
    /// FindModel tests migrated from TS findModel.test.ts.
    /// Covers search, navigation, replace, decorations, and edge cases.
    /// </summary>
    public class FindModelTests
    {
        // Standard test text used by most tests (matches TS findTest fixture)
        private static readonly string[] StandardTestText = new[]
        {
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

        [Fact]
        public void Test01_IncrementalFindFromBeginningOfFile()
        {
            TestEditorContext.RunTest(StandardTestText, ctx =>
            {
                ctx.SetPosition(1, 1);
                
                // Simulate typing 'H'
                ctx.State.Change(searchString: "H", moveCursor: true);
                ctx.AssertFindState(
                    cursor: new[] { 1, 12, 1, 13 },
                    highlighted: new[] { 1, 12, 1, 13 },
                    findDecorations: new[]
                    {
                        new[] { 1, 12, 1, 13 },
                        new[] { 2, 16, 2, 17 },
                        new[] { 6, 14, 6, 15 },
                        new[] { 6, 27, 6, 28 },
                        new[] { 7, 14, 7, 15 },
                        new[] { 8, 14, 8, 15 },
                        new[] { 9, 14, 9, 15 }
                    }
                );

                // Simulate typing 'He'
                ctx.State.Change(searchString: "He", moveCursor: true);
                ctx.AssertFindState(
                    cursor: new[] { 1, 12, 1, 14 },
                    highlighted: new[] { 1, 12, 1, 14 },
                    findDecorations: new[]
                    {
                        new[] { 1, 12, 1, 14 },
                        new[] { 6, 14, 6, 16 },
                        new[] { 6, 27, 6, 29 },
                        new[] { 7, 14, 7, 16 },
                        new[] { 8, 14, 8, 16 },
                        new[] { 9, 14, 9, 16 }
                    }
                );

                // Simulate typing 'Hello'
                ctx.State.Change(searchString: "Hello", moveCursor: true);
                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
                );

                // Toggle matchCase on
                ctx.State.Change(matchCase: true, moveCursor: true);
                ctx.AssertFindState(
                    cursor: new[] { 6, 27, 6, 32 },
                    highlighted: new[] { 6, 27, 6, 32 },
                    findDecorations: new[]
                    {
                        new[] { 6, 27, 6, 32 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                // Change to 'hello' (case-sensitive)
                ctx.State.Change(searchString: "hello", moveCursor: true);
                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
                );

                // Toggle wholeWord on
                ctx.State.Change(wholeWord: true, moveCursor: true);
                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 }
                    }
                );

                // Toggle matchCase off
                ctx.State.Change(matchCase: false, moveCursor: true);
                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                // Toggle wholeWord off
                ctx.State.Change(wholeWord: false, moveCursor: true);
                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
                );

                // Add search scope
                ctx.State.Change(searchScope: new[] { new Core.Range(new TextPosition(8, 1), new TextPosition(10, 1)) }, moveCursor: true);
                ctx.AssertFindState(
                    cursor: new[] { 8, 14, 8, 19 },
                    highlighted: new[] { 8, 14, 8, 19 },
                    findDecorations: new[]
                    {
                        new[] { 8, 14, 8, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
                );

                // Remove search scope
                ctx.State.Change(searchScope: null, searchScopeProvided: true, moveCursor: true);
                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
                );

                ctx.FindModel.Dispose();

                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
                );

                ctx.State.Change(searchString: "helloo", moveCursor: false);
                Assert.Equal(0, ctx.State.MatchesCount);
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
                );

                // Move cursor to position (6, 20)
                ctx.SetPosition(6, 20);

                ctx.AssertFindState(
                    cursor: new[] { 6, 20, 6, 20 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
                );

                ctx.State.Change(searchString: "Hello", moveCursor: true);
                ctx.AssertFindState(
                    cursor: new[] { 6, 27, 6, 32 },
                    highlighted: new[] { 6, 27, 6, 32 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 6, 27, 6, 32 },
                    highlighted: new[] { 6, 27, 6, 32 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 7, 14, 7, 19 },
                    highlighted: new[] { 7, 14, 7, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 8, 14, 8, 19 },
                    highlighted: new[] { 8, 14, 8, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                // Wrap around
                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
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
                    searchScope: new[] { new Core.Range(new TextPosition(7, 1), new TextPosition(9, 1)) },
                    moveCursor: false
                );
                
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 7, 14, 7, 19 },
                    highlighted: new[] { 7, 14, 7, 19 },
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 8, 14, 8, 19 },
                    highlighted: new[] { 8, 14, 8, 19 },
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                // Wrap back to first match in scope
                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 7, 14, 7, 19 },
                    highlighted: new[] { 7, 14, 7, 19 },
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );
            });
        }

        // Test07 & Test08: Multi-selection find - TODO(Batch #3)
        // Skipped: 'multi-selection find model next stays in scope (overlap)'
        // Skipped: 'multi-selection find model next stays in scope'

        [Fact]
        public void Test09_FindModelPrev()
        {
            TestEditorContext.RunTest(StandardTestText, ctx =>
            {
                ctx.State.Change(searchString: "hello", wholeWord: true, moveCursor: false);
                
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 8, 14, 8, 19 },
                    highlighted: new[] { 8, 14, 8, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 7, 14, 7, 19 },
                    highlighted: new[] { 7, 14, 7, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 6, 27, 6, 32 },
                    highlighted: new[] { 6, 27, 6, 32 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                // Wrap around
                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 8, 14, 8, 19 },
                    highlighted: new[] { 8, 14, 8, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
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
                    searchScope: new[] { new Core.Range(new TextPosition(7, 1), new TextPosition(9, 1)) },
                    moveCursor: false
                );
                
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 8, 14, 8, 19 },
                    highlighted: new[] { 8, 14, 8, 19 },
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 7, 14, 7, 19 },
                    highlighted: new[] { 7, 14, 7, 19 },
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                // Wrap back to last match in scope
                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 8, 14, 8, 19 },
                    highlighted: new[] { 8, 14, 8, 19 },
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
                );

                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                // Move cursor to (6, 20)
                ctx.SetPosition(6, 20);
                ctx.AssertFindState(
                    cursor: new[] { 6, 20, 6, 20 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                // Next from (6,20) should find (6,27)
                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 6, 27, 6, 32 },
                    highlighted: new[] { 6, 27, 6, 32 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 1, 1, 1, 1 },
                        new[] { 2, 1, 2, 1 },
                        new[] { 3, 1, 3, 1 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 1, 5, 1 },
                        new[] { 6, 1, 6, 1 },
                        new[] { 7, 1, 7, 1 },
                        new[] { 8, 1, 8, 1 },
                        new[] { 9, 1, 9, 1 },
                        new[] { 10, 1, 10, 1 },
                        new[] { 11, 1, 11, 1 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 2, 1, 2, 1 },
                    highlighted: new[] { 2, 1, 2, 1 },
                    findDecorations: new[]
                    {
                        new[] { 1, 1, 1, 1 },
                        new[] { 2, 1, 2, 1 },
                        new[] { 3, 1, 3, 1 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 1, 5, 1 },
                        new[] { 6, 1, 6, 1 },
                        new[] { 7, 1, 7, 1 },
                        new[] { 8, 1, 8, 1 },
                        new[] { 9, 1, 9, 1 },
                        new[] { 10, 1, 10, 1 },
                        new[] { 11, 1, 11, 1 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 3, 1, 3, 1 },
                    highlighted: new[] { 3, 1, 3, 1 },
                    findDecorations: new[]
                    {
                        new[] { 1, 1, 1, 1 },
                        new[] { 2, 1, 2, 1 },
                        new[] { 3, 1, 3, 1 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 1, 5, 1 },
                        new[] { 6, 1, 6, 1 },
                        new[] { 7, 1, 7, 1 },
                        new[] { 8, 1, 8, 1 },
                        new[] { 9, 1, 9, 1 },
                        new[] { 10, 1, 10, 1 },
                        new[] { 11, 1, 11, 1 },
                        new[] { 12, 1, 12, 1 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 1, 18, 1, 18 },
                        new[] { 2, 18, 2, 18 },
                        new[] { 3, 20, 3, 20 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 13, 5, 13 },
                        new[] { 6, 43, 6, 43 },
                        new[] { 7, 41, 7, 41 },
                        new[] { 8, 41, 8, 41 },
                        new[] { 9, 40, 9, 40 },
                        new[] { 10, 2, 10, 2 },
                        new[] { 11, 17, 11, 17 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 1, 18, 1, 18 },
                    highlighted: new[] { 1, 18, 1, 18 },
                    findDecorations: new[]
                    {
                        new[] { 1, 18, 1, 18 },
                        new[] { 2, 18, 2, 18 },
                        new[] { 3, 20, 3, 20 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 13, 5, 13 },
                        new[] { 6, 43, 6, 43 },
                        new[] { 7, 41, 7, 41 },
                        new[] { 8, 41, 8, 41 },
                        new[] { 9, 40, 9, 40 },
                        new[] { 10, 2, 10, 2 },
                        new[] { 11, 17, 11, 17 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 2, 18, 2, 18 },
                    highlighted: new[] { 2, 18, 2, 18 },
                    findDecorations: new[]
                    {
                        new[] { 1, 18, 1, 18 },
                        new[] { 2, 18, 2, 18 },
                        new[] { 3, 20, 3, 20 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 13, 5, 13 },
                        new[] { 6, 43, 6, 43 },
                        new[] { 7, 41, 7, 41 },
                        new[] { 8, 41, 8, 41 },
                        new[] { 9, 40, 9, 40 },
                        new[] { 10, 2, 10, 2 },
                        new[] { 11, 17, 11, 17 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 3, 20, 3, 20 },
                    highlighted: new[] { 3, 20, 3, 20 },
                    findDecorations: new[]
                    {
                        new[] { 1, 18, 1, 18 },
                        new[] { 2, 18, 2, 18 },
                        new[] { 3, 20, 3, 20 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 13, 5, 13 },
                        new[] { 6, 43, 6, 43 },
                        new[] { 7, 41, 7, 41 },
                        new[] { 8, 41, 8, 41 },
                        new[] { 9, 40, 9, 40 },
                        new[] { 10, 2, 10, 2 },
                        new[] { 11, 17, 11, 17 },
                        new[] { 12, 1, 12, 1 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 4, 1, 4, 1 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 4, 1, 4, 1 },
                    highlighted: new[] { 4, 1, 4, 1 },
                    findDecorations: new[]
                    {
                        new[] { 4, 1, 4, 1 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 12, 1, 12, 1 },
                    highlighted: new[] { 12, 1, 12, 1 },
                    findDecorations: new[]
                    {
                        new[] { 4, 1, 4, 1 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 4, 1, 4, 1 },
                    highlighted: new[] { 4, 1, 4, 1 },
                    findDecorations: new[]
                    {
                        new[] { 4, 1, 4, 1 },
                        new[] { 12, 1, 12, 1 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 1, 1, 1, 18 },
                        new[] { 2, 1, 2, 18 },
                        new[] { 3, 1, 3, 20 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 1, 5, 13 },
                        new[] { 6, 1, 6, 43 },
                        new[] { 7, 1, 7, 41 },
                        new[] { 8, 1, 8, 41 },
                        new[] { 9, 1, 9, 40 },
                        new[] { 10, 1, 10, 2 },
                        new[] { 11, 1, 11, 17 },
                        new[] { 12, 1, 12, 1 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 1, 1, 1, 18 },
                        new[] { 2, 1, 2, 18 },
                        new[] { 3, 1, 3, 20 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 1, 5, 13 },
                        new[] { 6, 1, 6, 43 },
                        new[] { 7, 1, 7, 41 },
                        new[] { 8, 1, 8, 41 },
                        new[] { 9, 1, 9, 40 },
                        new[] { 10, 1, 10, 2 },
                        new[] { 11, 1, 11, 17 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 18 },
                    highlighted: new[] { 1, 1, 1, 18 },
                    findDecorations: new[]
                    {
                        new[] { 1, 1, 1, 18 },
                        new[] { 2, 1, 2, 18 },
                        new[] { 3, 1, 3, 20 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 1, 5, 13 },
                        new[] { 6, 1, 6, 43 },
                        new[] { 7, 1, 7, 41 },
                        new[] { 8, 1, 8, 41 },
                        new[] { 9, 1, 9, 40 },
                        new[] { 10, 1, 10, 2 },
                        new[] { 11, 1, 11, 17 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 2, 1, 2, 18 },
                    highlighted: new[] { 2, 1, 2, 18 },
                    findDecorations: new[]
                    {
                        new[] { 1, 1, 1, 18 },
                        new[] { 2, 1, 2, 18 },
                        new[] { 3, 1, 3, 20 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 1, 5, 13 },
                        new[] { 6, 1, 6, 43 },
                        new[] { 7, 1, 7, 41 },
                        new[] { 8, 1, 8, 41 },
                        new[] { 9, 1, 9, 40 },
                        new[] { 10, 1, 10, 2 },
                        new[] { 11, 1, 11, 17 },
                        new[] { 12, 1, 12, 1 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 1, 1, 1, 18 },
                        new[] { 2, 1, 2, 18 },
                        new[] { 3, 1, 3, 20 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 1, 5, 13 },
                        new[] { 6, 1, 6, 43 },
                        new[] { 7, 1, 7, 41 },
                        new[] { 8, 1, 8, 41 },
                        new[] { 9, 1, 9, 40 },
                        new[] { 10, 1, 10, 2 },
                        new[] { 11, 1, 11, 17 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 12, 1, 12, 1 },
                    highlighted: new[] { 12, 1, 12, 1 },
                    findDecorations: new[]
                    {
                        new[] { 1, 1, 1, 18 },
                        new[] { 2, 1, 2, 18 },
                        new[] { 3, 1, 3, 20 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 1, 5, 13 },
                        new[] { 6, 1, 6, 43 },
                        new[] { 7, 1, 7, 41 },
                        new[] { 8, 1, 8, 41 },
                        new[] { 9, 1, 9, 40 },
                        new[] { 10, 1, 10, 2 },
                        new[] { 11, 1, 11, 17 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 11, 1, 11, 17 },
                    highlighted: new[] { 11, 1, 11, 17 },
                    findDecorations: new[]
                    {
                        new[] { 1, 1, 1, 18 },
                        new[] { 2, 1, 2, 18 },
                        new[] { 3, 1, 3, 20 },
                        new[] { 4, 1, 4, 1 },
                        new[] { 5, 1, 5, 13 },
                        new[] { 6, 1, 6, 43 },
                        new[] { 7, 1, 7, 41 },
                        new[] { 8, 1, 8, 41 },
                        new[] { 9, 1, 9, 40 },
                        new[] { 10, 1, 10, 2 },
                        new[] { 11, 1, 11, 17 },
                        new[] { 12, 1, 12, 1 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 4, 1, 4, 1 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 12, 1, 12, 1 },
                    highlighted: new[] { 12, 1, 12, 1 },
                    findDecorations: new[]
                    {
                        new[] { 4, 1, 4, 1 },
                        new[] { 12, 1, 12, 1 }
                    }
                );

                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 4, 1, 4, 1 },
                    highlighted: new[] { 4, 1, 4, 1 },
                    findDecorations: new[]
                    {
                        new[] { 4, 1, 4, 1 },
                        new[] { 12, 1, 12, 1 }
                    }
                );


                ctx.FindModel.FindPrevious();
                ctx.AssertFindState(
                    cursor: new[] { 12, 1, 12, 1 },
                    highlighted: new[] { 12, 1, 12, 1 },
                    findDecorations: new[]
                    {
                        new[] { 4, 1, 4, 1 },
                        new[] { 12, 1, 12, 1 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.SetPosition(6, 20);
                ctx.AssertFindState(
                    cursor: new[] { 6, 20, 6, 20 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );
                Assert.Equal("    cout << \"hello world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 6, 27, 6, 32 },
                    highlighted: new[] { 6, 27, 6, 32 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );
                Assert.Equal("    cout << \"hello world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 7, 14, 7, 19 },
                    highlighted: new[] { 7, 14, 7, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );
                Assert.Equal("    cout << \"hello world, hi!\" << endl;", ctx.Model.GetLineContent(6));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 8, 14, 8, 19 },
                    highlighted: new[] { 8, 14, 8, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );
                Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(7));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 }
                    }
                );
                Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(8));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 6, 16, 6, 16 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 11, 4, 11, 7 },
                        new[] { 11, 7, 11, 10 },
                        new[] { 11, 10, 11, 13 }
                    }
                );

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 11, 4, 11, 7 },
                    highlighted: new[] { 11, 4, 11, 7 },
                    findDecorations: new[]
                    {
                        new[] { 11, 4, 11, 7 },
                        new[] { 11, 7, 11, 10 },
                        new[] { 11, 10, 11, 13 }
                    }
                );
                Assert.Equal("// blablablaciao", ctx.Model.GetLineContent(11));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 11, 8, 11, 11 },
                    highlighted: new[] { 11, 8, 11, 11 },
                    findDecorations: new[]
                    {
                        new[] { 11, 8, 11, 11 },
                        new[] { 11, 11, 11, 14 }
                    }
                );
                Assert.Equal("// ciaoblablaciao", ctx.Model.GetLineContent(11));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 11, 12, 11, 15 },
                    highlighted: new[] { 11, 12, 11, 15 },
                    findDecorations: new[]
                    {
                        new[] { 11, 12, 11, 15 }
                    }
                );
                Assert.Equal("// ciaociaoblaciao", ctx.Model.GetLineContent(11));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 11, 16, 11, 16 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.SetPosition(6, 20);
                ctx.AssertFindState(
                    cursor: new[] { 6, 20, 6, 20 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );
                Assert.Equal("    cout << \"hello world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

                ctx.FindModel.ReplaceAll();
                ctx.AssertFindState(
                    cursor: new[] { 6, 17, 6, 17 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 1, 6, 3 },
                        new[] { 6, 3, 6, 5 },
                        new[] { 7, 1, 7, 3 },
                        new[] { 7, 3, 7, 5 },
                        new[] { 8, 1, 8, 3 },
                        new[] { 8, 3, 8, 5 },
                        new[] { 9, 1, 9, 3 },
                        new[] { 9, 3, 9, 5 }
                    }
                );

                ctx.FindModel.ReplaceAll();
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 1, 6, 3 },
                        new[] { 7, 1, 7, 3 },
                        new[] { 8, 1, 8, 3 },
                        new[] { 9, 1, 9, 3 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 11, 4, 11, 7 },
                        new[] { 11, 7, 11, 10 },
                        new[] { 11, 10, 11, 13 }
                    }
                );

                ctx.FindModel.ReplaceAll();
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 11, 4, 11, 7 },
                        new[] { 11, 7, 11, 10 },
                        new[] { 11, 10, 11, 13 }
                    }
                );

                ctx.FindModel.ReplaceAll();
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 2, 2, 2, 9 },
                        new[] { 3, 2, 3, 9 }
                    }
                );

                ctx.FindModel.ReplaceAll();
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                // Replace entire content using PushEditOperations
                var entireRange = new Core.Range(new TextPosition(1, 1), ctx.Model.GetPositionAt(ctx.Model.GetLength()));
                ctx.Model.PushEditOperations(new[] { new TextEdit(entireRange.Start, entireRange.End, "hello\nhi") });
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
                );
            });
        }

        [Fact]
        public void Test28_SelectAllMatchesHonorsSearchScopeAndOrdersByRangeStart()
        {
            TestEditorContext.RunTest(StandardTestText, ctx =>
            {
                ctx.State.Change(searchString: "hello", moveCursor: false);

                var scopedRanges = new[]
                {
                    new Core.Range(new TextPosition(6, 1), new TextPosition(7, ctx.Model.GetLineMaxColumn(7))),
                    new Core.Range(new TextPosition(9, 1), new TextPosition(10, ctx.Model.GetLineMaxColumn(10)))
                };

                ctx.State.Change(searchScope: scopedRanges, searchScopeProvided: true, moveCursor: false);

                var selections = ctx.FindModel.SelectAllMatches();

                var expected = new[]
                {
                    CreateRange(6, 14, 6, 19),
                    CreateRange(6, 27, 6, 32),
                    CreateRange(7, 14, 7, 19),
                    CreateRange(9, 14, 9, 19)
                };

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

                var selections = ctx.FindModel.SelectAllMatches();
                var ranges = ToRanges(selections);

                Assert.Equal(CreateRange(8, 14, 8, 19), ranges[0]);

                var expectedTail = new[]
                {
                    CreateRange(6, 14, 6, 19),
                    CreateRange(6, 27, 6, 32),
                    CreateRange(7, 14, 7, 19),
                    CreateRange(9, 14, 9, 19)
                };
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 2, 11, 2, 17 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 2, 11, 2, 17 },
                    highlighted: new[] { 2, 11, 2, 17 },
                    findDecorations: new[]
                    {
                        new[] { 2, 11, 2, 17 }
                    }
                );

                ctx.FindModel.FindNext();
                ctx.AssertFindState(
                    cursor: new[] { 2, 11, 2, 17 },
                    highlighted: new[] { 2, 11, 2, 17 },
                    findDecorations: new[]
                    {
                        new[] { 2, 11, 2, 17 }
                    }
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.Replace();

                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );
                Assert.Equal("    cout << \"hello world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 7, 14, 7, 19 },
                    highlighted: new[] { 7, 14, 7, 19 },
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );
                Assert.Equal("    cout << \"hi world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 8, 14, 8, 19 },
                    highlighted: new[] { 8, 14, 8, 19 },
                    findDecorations: new[]
                    {
                        new[] { 8, 14, 8, 19 }
                    }
                );
                Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(7));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 8, 16, 8, 16 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 8, 14, 8, 14 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.Replace();

                ctx.AssertFindState(
                    cursor: new[] { 8, 14, 8, 19 },
                    highlighted: new[] { 8, 14, 8, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                Assert.Equal("    cout << \"Hello world again\" << endl;", ctx.Model.GetLineContent(8));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 }
                    }
                );
                Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(8));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 7, 14, 7, 19 },
                    highlighted: new[] { 7, 14, 7, 19 },
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 }
                    }
                );
                Assert.Equal("    cout << \"hi world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 7, 16, 7, 16 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.ReplaceAll();

                Assert.Equal("    cout << \"hi world, Hello!\" << endl;", ctx.Model.GetLineContent(6));
                Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(7));
                Assert.Equal("    cout << \"hi world again\" << endl;", ctx.Model.GetLineContent(8));

                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.Replace();

                ctx.AssertFindState(
                    cursor: new[] { 6, 14, 6, 19 },
                    highlighted: new[] { 6, 14, 6, 19 },
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );
                Assert.Equal("    cout << \"hello world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 7, 14, 7, 19 },
                    highlighted: new[] { 7, 14, 7, 19 },
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );
                Assert.Equal("    cout << \"hilo world, Hello!\" << endl;", ctx.Model.GetLineContent(6));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 8, 14, 8, 19 },
                    highlighted: new[] { 8, 14, 8, 19 },
                    findDecorations: new[]
                    {
                        new[] { 8, 14, 8, 19 }
                    }
                );
                Assert.Equal("    cout << \"hilo world again\" << endl;", ctx.Model.GetLineContent(7));

                ctx.FindModel.Replace();
                ctx.AssertFindState(
                    cursor: new[] { 8, 18, 8, 18 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 20, 6, 25 },
                        new[] { 7, 20, 7, 25 },
                        new[] { 8, 20, 8, 25 },
                        new[] { 9, 19, 9, 24 }
                    }
                );

                ctx.FindModel.ReplaceAll();

                Assert.Equal("    cout << \"hello girl, Hello!\" << endl;", ctx.Model.GetLineContent(6));
                Assert.Equal("    cout << \"hello girl again\" << endl;", ctx.Model.GetLineContent(7));
                Assert.Equal("    cout << \"Hello girl again\" << endl;", ctx.Model.GetLineContent(8));
                Assert.Equal("    cout << \"hellogirl again\" << endl;", ctx.Model.GetLineContent(9));

                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 20, 7, 1 },
                        new[] { 8, 20, 9, 1 }
                    }
                );

                ctx.FindModel.ReplaceAll();

                Assert.Equal("    cout << \"hello girl, Hello!\" << endl;", ctx.Model.GetLineContent(6));
                Assert.Equal("    cout << \"Hello girl again\" << endl;", ctx.Model.GetLineContent(8));

                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
                );

                ctx.FindModel.ReplaceAll();

                Assert.Equal("    cout << \"goodbye world, Goodbye!\" << endl;", ctx.Model.GetLineContent(6));
                Assert.Equal("    cout << \"goodbye world again\" << endl;", ctx.Model.GetLineContent(7));
                Assert.Equal("    cout << \"Goodbye world again\" << endl;", ctx.Model.GetLineContent(8));
                Assert.Equal("    cout << \"goodbyeworld again\" << endl;", ctx.Model.GetLineContent(9));

                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 6, 27, 6, 32 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 8, 14, 8, 19 }
                    }
                );

                ctx.FindModel.ReplaceAll();
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
            var largeTextLines = new string[1101];
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
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 6, 14, 6, 19 },
                        new[] { 7, 14, 7, 19 },
                        new[] { 9, 14, 9, 19 }
                    }
                );

                ctx.FindModel.ReplaceAll();
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new int[0][]
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
                    searchScope: new[] { new Core.Range(new TextPosition(7, 1), new TextPosition(8, 1)) },
                    moveCursor: false
                );
                
                ctx.AssertFindState(
                    cursor: new[] { 1, 1, 1, 1 },
                    highlighted: null,
                    findDecorations: new[]
                    {
                        new[] { 7, 14, 7, 19 }
                    }
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
            var text = new[] { "alpha-beta alpha" };
            var options = new TestEditorContextOptions
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

        private static Range[] ToRanges(Selection[] selections)
        {
            var result = new Range[selections.Length];
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
}
