/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Migrated core scenarios from ts/src/vs/editor/contrib/find/test/browser/findController.test.ts

using System;
using System.Collections.Generic;
using System.Linq;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.DocUI;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests.DocUI
{
    public sealed class DocUIFindControllerTests
    {
        [Fact(DisplayName = "issue #1857: F3 reuses typed search text")]
        public void Issue1857_FindNextUsesTypedValue()
        {
            var host = new TestEditorHost(new[] { "ABC", "ABC", "XYZ", "ABC" });
            using var controller = CreateController(host);
            var state = controller.State;

            controller.StartFindAction();
            state.Change(searchString: "A", moveCursor: true);
            state.Change(searchString: "AB", moveCursor: true);
            state.Change(searchString: "ABC", moveCursor: true);
            AssertSelection(host, 1, 1, 1, 4);

            controller.CloseFindWidget();
            host.SetPrimarySelection(new Selection(1, 4, 1, 4));

            host.Model.ApplyEdits(new[]
            {
                new TextEdit(new TextPosition(1, 1), new TextPosition(1, 4), string.Empty),
                new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "XYZ"),
            });
            host.SetPrimarySelection(new Selection(1, 4, 1, 4));

            Assert.True(controller.NextMatchFindAction());
            AssertSelection(host, 2, 1, 2, 4);
            Assert.Equal("ABC", state.SearchString);
            Assert.Equal(FindFocusBehavior.NoFocusChange, controller.FocusTarget);
        }

        [Fact(DisplayName = "issue #3090: F3 loops when multiple matches share a line")]
        public void Issue3090_FindNextLoopsWithinSingleLine()
        {
            var options = new FindControllerHostOptions
            {
                SeedSearchStringFromSelection = SeedSearchStringMode.Always
            };
            var host = new TestEditorHost(new[] { "import nls = require('vs/nls');" }, options);
            using var controller = CreateController(host);

            host.SetPrimarySelection(new Selection(1, 9, 1, 9));

            Assert.True(controller.NextMatchFindAction());
            AssertSelection(host, 1, 26, 1, 29);

            Assert.True(controller.NextMatchFindAction());
            AssertSelection(host, 1, 8, 1, 11);
        }

        [Fact(DisplayName = "issue #3090: Shift+F3 loops within single line")]
        public void Issue3090_PreviousMatchLoopsWithinSingleLine()
        {
            var options = new FindControllerHostOptions
            {
                SeedSearchStringFromSelection = SeedSearchStringMode.Always
            };
            var host = new TestEditorHost(new[] { "import nls = require('vs/nls');" }, options);
            using var controller = CreateController(host);

            host.SetPrimarySelection(new Selection(1, 9, 1, 9));

            Assert.True(controller.NextMatchFindAction());
            Assert.True(controller.NextMatchFindAction());

            Assert.True(controller.PreviousMatchFindAction());
            AssertSelection(host, 1, 26, 1, 29);

            Assert.True(controller.PreviousMatchFindAction());
            AssertSelection(host, 1, 8, 1, 11);
        }

        [Fact(DisplayName = "issue #6149: auto-escape seeded regex selection")]
        public void Issue6149_RegexSelectionAutoEscapes()
        {
            var host = new TestEditorHost(new[]
            {
                "var x = (3 * 5)",
                "var y = (3 * 5)",
                "var z = (3  * 5)"
            });
            using var controller = CreateController(host);

            host.SetPrimarySelection(new Selection(1, 9, 1, 16));
            controller.ToggleRegex();
            controller.StartFindAction();

            Assert.True(controller.MoveToNextMatch());
            AssertSelection(host, 2, 9, 2, 16);

            Assert.True(controller.MoveToNextMatch());
            AssertSelection(host, 1, 9, 1, 16);
            Assert.Equal("\\(3\\ \\*\\ 5\\)", controller.State.SearchString);
        }

        [Fact(DisplayName = "issue #41027: keep search text when find input active")]
        public void Issue41027_FindInputStaysInControl()
        {
            var host = new TestEditorHost(new[] { "test" });
            using var controller = CreateController(host);
            const string pattern = "tes.";

            controller.ToggleRegex();
            controller.SetSearchString(pattern);
            controller.Start(new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = SelectionSeedMode.None,
                SeedSearchStringFromNonEmptySelection = false,
                SeedSearchStringFromGlobalClipboard = false,
                ShouldFocus = FindFocusBehavior.FocusFindInput,
                UpdateSearchScope = false,
                Loop = true
            });

            Assert.True(controller.MoveToNextMatch());
            controller.StartFindReplaceAction();

            Assert.Equal(pattern, controller.State.SearchString);
        }

        [Fact(DisplayName = "issue #41027: replace UI hides when widget reopens via Ctrl+F")]
        public void Issue41027_ReplacePanelResetsWhenWidgetReopens()
        {
            var host = new TestEditorHost(new[] { "foo" });
            using var controller = CreateController(host);

            controller.StartFindReplaceAction();
            Assert.True(controller.State.IsReplaceRevealed);

            controller.CloseFindWidget();
            controller.StartFindAction();

            Assert.False(controller.State.IsReplaceRevealed);
        }

        [Fact(DisplayName = "Ctrl+F reseeds from the latest selection even if a search exists")]
        public void CtrlFReseedsFromSelectionEvenWhenSearchStringExists()
        {
            var options = new FindControllerHostOptions
            {
                SeedSearchStringFromSelection = SeedSearchStringMode.Selection
            };
            var host = new TestEditorHost(new[] { "foo bar" }, options);
            using var controller = CreateController(host);

            host.SetPrimarySelection(new Selection(1, 1, 1, 4));
            controller.StartFindAction();
            Assert.Equal("foo", controller.State.SearchString);

            host.SetPrimarySelection(new Selection(1, 5, 1, 8));
            controller.StartFindAction();

            Assert.Equal("bar", controller.State.SearchString);
        }

        [Fact(DisplayName = "StartFindReplace honors seedSearchStringFromSelection = never")]
        public void StartFindReplaceHonorsNeverSeedPreference()
        {
            var options = new FindControllerHostOptions
            {
                SeedSearchStringFromSelection = SeedSearchStringMode.Never,
                EnableGlobalFindClipboard = true,
                IsMacPlatform = true
            };
            var host = new TestEditorHost(new[] { "foo", "bar" }, options);
            var clipboard = new TestFindControllerClipboard(isEnabled: true, initial: "clip");

            using var controller = CreateController(host, clipboard: clipboard);
            controller.SetSearchString("foo");
            host.SetPrimarySelection(new Selection(2, 1, 2, 4));

            controller.StartFindReplaceAction();

            Assert.Equal("foo", controller.State.SearchString);
        }

        [Fact(DisplayName = "issue #9043: search scope clears when widget closes")]
        public void Issue9043_SearchScopeClearsWhenHidden()
        {
            var host = new TestEditorHost(new[]
            {
                "var x = (3 * 5)",
                "var y = (3 * 5)",
                "var z = (3 * 5)"
            });
            using var controller = CreateController(host);

            controller.Start(new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = SelectionSeedMode.None,
                SeedSearchStringFromNonEmptySelection = false,
                SeedSearchStringFromGlobalClipboard = false,
                ShouldFocus = FindFocusBehavior.NoFocusChange,
                UpdateSearchScope = false,
                Loop = true
            });
            Assert.Null(controller.State.SearchScope);

            var scopedRange = new Range(new TextPosition(1, 1), new TextPosition(1, 5));
            controller.State.Change(searchScope: new[] { scopedRange }, searchScopeProvided: true, moveCursor: false);
            AssertScopes(controller, scopedRange);

            controller.CloseFindWidget();
            Assert.Null(controller.State.SearchScope);
        }

        [Fact(DisplayName = "Find model disposes when widget hides and clears match info")]
        public void FindModelDisposesWhenWidgetHides()
        {
            var host = new TestEditorHost(new[]
            {
                "foo foo",
                "foo"
            });
            using var controller = CreateController(host);

            controller.SetSearchString("foo");
            controller.StartFindAction();

            Assert.True(controller.MoveToNextMatch());
            Assert.Equal(3, controller.State.MatchesCount);
            Assert.NotNull(controller.State.CurrentMatch);

            controller.CloseFindWidget();
            Assert.Equal(0, controller.State.MatchesCount);
            Assert.Null(controller.State.CurrentMatch);

            host.SetValue("bar");
            Assert.Equal(0, controller.State.MatchesCount);

            controller.SetSearchString("bar");
            controller.StartFindAction();

            Assert.True(controller.MoveToNextMatch());
            Assert.Equal(1, controller.State.MatchesCount);
        }

        [Fact(DisplayName = "issue #27083: update scope when widget shows")]
        public void Issue27083_SearchScopeUpdatesWhenVisible()
        {
            var options = new FindControllerHostOptions
            {
                AutoFindInSelection = AutoFindInSelectionMode.Always,
                SeedSearchStringFromSelection = SeedSearchStringMode.Never
            };
            var host = new TestEditorHost(new[]
            {
                "var x = (3 * 5)",
                "var y = (3 * 5)",
                "var z = (3 * 5)"
            }, options);
            using var controller = CreateController(host);
            var startOptions = new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = SelectionSeedMode.None,
                SeedSearchStringFromNonEmptySelection = false,
                SeedSearchStringFromGlobalClipboard = false,
                ShouldFocus = FindFocusBehavior.NoFocusChange,
                UpdateSearchScope = true,
                Loop = true
            };

            host.SetPrimarySelection(new Selection(1, 1, 2, 1));
            controller.Start(startOptions);
            AssertScopes(controller, new Range(new TextPosition(1, 1), new TextPosition(2, 1)));

            controller.CloseFindWidget();

            host.SetSelections(new Selection(1, 1, 2, 1), new Selection(2, 1, 2, 5));
            controller.Start(startOptions);
            AssertScopes(controller,
                new Range(new TextPosition(1, 1), new TextPosition(2, 1)),
                new Range(new TextPosition(2, 1), new TextPosition(2, 5)));
        }

        [Fact(DisplayName = "issue #58604: do not update scope for empty selection")]
        public void Issue58604_ScopeStaysNullForEmptySelection()
        {
            var options = new FindControllerHostOptions
            {
                AutoFindInSelection = AutoFindInSelectionMode.Always,
                SeedSearchStringFromSelection = SeedSearchStringMode.Never
            };
            var host = new TestEditorHost(new[]
            {
                "var x = (3 * 5)",
                "var y = (3 * 5)",
                "var z = (3 * 5)"
            }, options);
            using var controller = CreateController(host);

            host.SetPrimarySelection(new Selection(1, 2, 1, 2));
            controller.Start(new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = SelectionSeedMode.None,
                SeedSearchStringFromNonEmptySelection = false,
                SeedSearchStringFromGlobalClipboard = false,
                ShouldFocus = FindFocusBehavior.NoFocusChange,
                UpdateSearchScope = true,
                Loop = true
            });

            Assert.Null(controller.State.SearchScope);
        }

        [Fact(DisplayName = "issue #58604: update scope when selection not empty")]
        public void Issue58604_ScopeUpdatesForNonEmptySelection()
        {
            var options = new FindControllerHostOptions
            {
                AutoFindInSelection = AutoFindInSelectionMode.Always,
                SeedSearchStringFromSelection = SeedSearchStringMode.Never
            };
            var host = new TestEditorHost(new[]
            {
                "var x = (3 * 5)",
                "var y = (3 * 5)",
                "var z = (3 * 5)"
            }, options);
            using var controller = CreateController(host);

            host.SetPrimarySelection(new Selection(1, 2, 1, 3));
            controller.Start(new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = SelectionSeedMode.None,
                SeedSearchStringFromNonEmptySelection = false,
                SeedSearchStringFromGlobalClipboard = false,
                ShouldFocus = FindFocusBehavior.NoFocusChange,
                UpdateSearchScope = true,
                Loop = true
            });

            AssertScopes(controller, new Range(new TextPosition(1, 2), new TextPosition(1, 3)));
        }

        [Fact(DisplayName = "auto find-in-selection applies during fallback start")]
        public void AutoFindInSelectionAppliesDuringFallbackStart()
        {
            var options = new FindControllerHostOptions
            {
                AutoFindInSelection = AutoFindInSelectionMode.Multiline
            };
            var host = new TestEditorHost(new[]
            {
                "alpha beta",
                "gamma delta"
            }, options);
            using var controller = CreateController(host);

            host.SetPrimarySelection(new Selection(1, 1, 2, 6));
            Assert.Null(controller.State.SearchScope);

            Assert.False(controller.NextMatchFindAction());

            AssertScopes(controller, new Range(new TextPosition(1, 1), new TextPosition(2, 6)));
        }

        [Fact(DisplayName = "scope persists when caret collapses during auto find in selection")]
        public void SearchScopePersistsWhenSelectionCollapses()
        {
            var options = new FindControllerHostOptions
            {
                AutoFindInSelection = AutoFindInSelectionMode.Always,
                SeedSearchStringFromSelection = SeedSearchStringMode.Never
            };
            var host = new TestEditorHost(new[]
            {
                "var scoped = true",
                "var other = false"
            }, options);
            using var controller = CreateController(host);
            var startOptions = new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = SelectionSeedMode.None,
                SeedSearchStringFromNonEmptySelection = false,
                SeedSearchStringFromGlobalClipboard = false,
                ShouldFocus = FindFocusBehavior.NoFocusChange,
                UpdateSearchScope = true,
                Loop = true
            };

            host.SetPrimarySelection(new Selection(1, 5, 1, 11));
            controller.Start(startOptions);
            var scopedRange = new Range(new TextPosition(1, 5), new TextPosition(1, 11));
            AssertScopes(controller, scopedRange);

            host.SetPrimarySelection(new Selection(1, 11, 1, 11));
            controller.Start(startOptions);

            AssertScopes(controller, scopedRange);
        }

        [Fact(DisplayName = "issue #38232: next selection match honours regex")]
        public void Issue38232_NextSelectionMatchRegex()
        {
            var host = new TestEditorHost(new[]
            {
                "([funny]",
                string.Empty,
                "([funny]"
            });
            using var controller = CreateController(host);

            controller.State.Change(isRegex: true, moveCursor: false);
            host.SetPrimarySelection(new Selection(1, 1, 1, 9));

            Assert.True(controller.NextSelectionMatchFindAction());
            AssertSelection(host, 3, 1, 3, 9);
        }

        [Fact(DisplayName = "issue #38232: previous selection match honours regex")]
        public void Issue38232_PreviousSelectionMatchRegex()
        {
            var host = new TestEditorHost(new[]
            {
                "([funny]",
                string.Empty,
                "([funny]"
            });
            using var controller = CreateController(host);

            controller.State.Change(isRegex: true, moveCursor: false);
            host.SetPrimarySelection(new Selection(3, 1, 3, 9));

            Assert.True(controller.PreviousSelectionMatchFindAction());
            AssertSelection(host, 1, 1, 1, 9);
        }

        [Fact(DisplayName = "issue #38232: next selection match with widget open")]
        public void Issue38232_NextSelectionMatchRegexWithWidgetOpen()
        {
            var host = new TestEditorHost(new[]
            {
                "([funny]",
                string.Empty,
                "([funny]"
            });
            using var controller = CreateController(host);

            controller.StartFindAction();
            controller.State.Change(isRegex: true, moveCursor: false);
            host.SetPrimarySelection(new Selection(1, 1, 1, 9));

            Assert.True(controller.NextSelectionMatchFindAction());
            AssertSelection(host, 3, 1, 3, 9);
        }

        [Fact(DisplayName = "Ctrl/Cmd+F3 on whitespace shows find widget even without seed")]
        public void NextSelectionMatchOnWhitespaceRevealsWidget()
        {
            var host = new TestEditorHost(new[]
            {
                "   foo",
                "bar"
            });
            using var controller = CreateController(host);

            host.SetPrimarySelection(new Selection(1, 1, 1, 1));
            Assert.False(controller.IsFindWidgetVisible);

            Assert.False(controller.NextSelectionMatchFindAction());
            Assert.True(controller.IsFindWidgetVisible);
            Assert.Equal(string.Empty, controller.State.SearchString);
        }

        [Fact(DisplayName = "issue #47400: StartFindWithSelection captures multi-line selections")]
        public void Issue47400_StartFindWithSelectionSeedsMultilineSelection()
        {
            var host = new TestEditorHost(new[]
            {
                "foo",
                "bar"
            });
            using var controller = CreateController(host);

            controller.SetSearchString("seed");
            host.SetPrimarySelection(new Selection(1, 1, 2, 4));

            controller.StartFindWithSelectionAction();

            Assert.Equal("foo\nbar", controller.State.SearchString);
        }

        [Fact(DisplayName = "StartFindWithSelection keeps literal parentheses when regex enabled")]
        public void StartFindWithSelectionDoesNotEscapeRegexCharacters()
        {
            var host = new TestEditorHost(new[]
            {
                "foo(bar)",
                "baz"
            });
            using var controller = CreateController(host);

            controller.ToggleRegex();
            controller.SetSearchString("seed");
            host.SetPrimarySelection(new Selection(1, 1, 1, 9));

            controller.StartFindWithSelectionAction();

            Assert.Equal("foo(bar)", controller.State.SearchString);
        }

        [Fact(DisplayName = "issue #109756: StartFindWithSelection seeds word at caret for empty selection")]
        public void Issue109756_StartFindWithSelectionSeedsWordUnderCaret()
        {
            var host = new TestEditorHost(new[] { "foo bar baz" });
            using var controller = CreateController(host);

            controller.SetSearchString("seed");
            host.SetPrimarySelection(new Selection(1, 5, 1, 5));

            controller.StartFindWithSelectionAction();

            Assert.Equal("bar", controller.State.SearchString);
        }

        [Fact(DisplayName = "SelectAllMatchesAction applies selections with primary first")]
        public void SelectAllMatchesActionAppliesSelections()
        {
            var host = new TestEditorHost(new[]
            {
                "foo foo",
                "foo"
            });
            using var controller = CreateController(host);

            host.SetPrimarySelection(new Selection(2, 1, 2, 4));
            controller.SetSearchString("foo");

            Assert.True(controller.SelectAllMatchesAction());

            var selections = host.GetSelections();
            Assert.Equal(3, selections.Length);

            Assert.Equal(2, selections[0].SelectionStart.LineNumber);
            Assert.Equal(1, selections[0].SelectionStart.Column);
            Assert.Equal(2, selections[0].SelectionEnd.LineNumber);
            Assert.Equal(4, selections[0].SelectionEnd.Column);

            Assert.Equal(1, selections[1].SelectionStart.LineNumber);
            Assert.Equal(1, selections[1].SelectionStart.Column);
            Assert.Equal(1, selections[1].SelectionEnd.LineNumber);
            Assert.Equal(4, selections[1].SelectionEnd.Column);

            Assert.Equal(1, selections[2].SelectionStart.LineNumber);
            Assert.Equal(5, selections[2].SelectionStart.Column);
            Assert.Equal(1, selections[2].SelectionEnd.LineNumber);
            Assert.Equal(8, selections[2].SelectionEnd.Column);
        }

        [Fact(DisplayName = "SelectAllMatchesAction returns false when no matches exist")]
        public void SelectAllMatchesActionReturnsFalseWhenNoMatches()
        {
            var host = new TestEditorHost(new[] { "abc" });
            using var controller = CreateController(host);

            controller.SetSearchString("zzz");

            Assert.False(controller.SelectAllMatchesAction());
        }

        [Fact(DisplayName = "default preserveCase option hydrates state")]
        public void DefaultPreserveCaseOptionHydratesState()
        {
            var options = new FindControllerHostOptions
            {
                DefaultPreserveCase = true
            };
            var host = new TestEditorHost(new[] { "text" }, options);

            using var controller = CreateController(host);
            Assert.True(controller.State.PreserveCase);
        }

        [Fact(DisplayName = "preserveCase toggle persists via storage")]
        public void PreserveCaseTogglePersistsAcrossSessions()
        {
            var storage = new TestFindControllerStorage();

            var hostA = new TestEditorHost(new[] { "alpha" });
            using (var controller = CreateController(hostA, storage))
            {
                Assert.False(controller.State.PreserveCase);
                controller.TogglePreserveCase();
                Assert.True(controller.State.PreserveCase);
            }

            var hostB = new TestEditorHost(new[] { "beta" });
            using var controllerB = CreateController(hostB, storage);
            Assert.True(controllerB.State.PreserveCase);
            controllerB.TogglePreserveCase();
            Assert.False(controllerB.State.PreserveCase);
        }

        [Fact(DisplayName = "empty global clipboard does not clear search")]
        public void EmptyGlobalClipboardDoesNotClearSearchString()
        {
            var options = new FindControllerHostOptions
            {
                EnableGlobalFindClipboard = true,
                IsMacPlatform = true
            };
            var host = new TestEditorHost(new[] { "foo" }, options);
            var clipboard = new TestFindControllerClipboard(isEnabled: true, initial: string.Empty);

            using var controller = CreateController(host, clipboard: clipboard);
            controller.State.Change(searchString: "foo", moveCursor: false);

            controller.StartFindAction();

            Assert.Equal("foo", controller.State.SearchString);
        }

        private static DocUIFindController CreateController(TestEditorHost host, TestFindControllerStorage? storage = null, TestFindControllerClipboard? clipboard = null)
        {
            return new DocUIFindController(host, storage ?? new TestFindControllerStorage(), clipboard ?? new TestFindControllerClipboard());
        }

        private static void AssertSelection(TestEditorHost host, int startLine, int startColumn, int endLine, int endColumn)
        {
            var selection = host.GetSelections().Single();
            Assert.Equal(startLine, selection.SelectionStart.LineNumber);
            Assert.Equal(startColumn, selection.SelectionStart.Column);
            Assert.Equal(endLine, selection.SelectionEnd.LineNumber);
            Assert.Equal(endColumn, selection.SelectionEnd.Column);
        }

        private static void AssertScopes(DocUIFindController controller, params Range[] expected)
        {
            var scope = controller.State.SearchScope;
            if (expected == null || expected.Length == 0)
            {
                Assert.Null(scope);
                return;
            }

            Assert.NotNull(scope);
            Assert.Equal(expected.Length, scope!.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], scope[i]);
            }
        }

        private sealed class TestEditorHost : IEditorHost
        {
            private readonly TextModel _model;
            private Selection[] _selections;

            public TestEditorHost(string[] lines, FindControllerHostOptions? options = null)
            {
                if (lines == null)
                {
                    throw new ArgumentNullException(nameof(lines));
                }

                var text = string.Join("\n", lines);
                _model = new TextModel(text);
                Options = options ?? new FindControllerHostOptions();
                _selections = new[] { new Selection(TextPosition.Origin, TextPosition.Origin) };
            }

            public TextModel Model => _model;
            public FindControllerHostOptions Options { get; }

            public Selection[] GetSelections()
            {
                return _selections.Length == 0 ? Array.Empty<Selection>() : _selections.ToArray();
            }

            void IEditorHost.SetSelections(IReadOnlyList<Selection> selections)
            {
                SetSelectionsCore(selections);
            }

            public void SetSelections(params Selection[] selections)
            {
                SetSelectionsCore(selections);
            }

            public void SetPrimarySelection(Selection selection)
            {
                SetSelections(selection);
            }

            public void ApplyEdits(IEnumerable<TextEdit> edits)
            {
                var array = edits?.ToArray() ?? Array.Empty<TextEdit>();
                if (array.Length == 0)
                {
                    return;
                }

                _model.ApplyEdits(array);
            }

            public void SetValue(string value)
            {
                var end = _model.GetPositionAt(_model.GetLength());
                _model.ApplyEdits(new[] { new TextEdit(TextPosition.Origin, end, value ?? string.Empty) });
            }

            public string GetValue()
            {
                return _model.GetValue();
            }

            public void MoveCursor(TextPosition position)
            {
                SetSelections(new Selection(position, position));
            }

            private void SetSelectionsCore(IReadOnlyList<Selection>? selections)
            {
                if (selections == null || selections.Count == 0)
                {
                    _selections = new[] { new Selection(TextPosition.Origin, TextPosition.Origin) };
                    return;
                }

                _selections = new Selection[selections.Count];
                for (int i = 0; i < selections.Count; i++)
                {
                    _selections[i] = selections[i];
                }
            }
        }

        private sealed class TestFindControllerStorage : IFindControllerStorage
        {
            private readonly Dictionary<string, bool> _values;

            public TestFindControllerStorage(IDictionary<string, bool>? seed = null)
            {
                _values = seed != null
                    ? new Dictionary<string, bool>(seed, StringComparer.Ordinal)
                    : new Dictionary<string, bool>(StringComparer.Ordinal);
            }

            public bool? ReadBool(string key)
            {
                return _values.TryGetValue(key, out var value) ? value : null;
            }

            public void WriteBool(string key, bool value)
            {
                _values[key] = value;
            }
        }

        private sealed class TestFindControllerClipboard : IFindControllerClipboard
        {
            public TestFindControllerClipboard(bool isEnabled = false, string? initial = null)
            {
                IsEnabled = isEnabled;
                Text = initial;
            }

            public bool IsEnabled { get; set; }
            public string? Text { get; private set; }

            public string? ReadText() => Text;

            public void WriteText(string text)
            {
                Text = text;
            }
        }
    }
}
