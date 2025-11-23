/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// TypeScript reference: ts/src/vs/editor/contrib/find/browser/findController.ts (core sync logic)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;
using Selection = PieceTree.TextBuffer.Core.Selection;

namespace PieceTree.TextBuffer.DocUI
{
    /// <summary>
    /// Mirrors VS Code's FindStartFocusAction union; simplified for the DocUI harness.
    /// </summary>
    public enum FindFocusBehavior
    {
        NoFocusChange,
        FocusFindInput,
        FocusReplaceInput
    }

    /// <summary>
    /// Options provided when starting a find session.
    /// </summary>
    public sealed class FindStartOptions
    {
        public bool ForceRevealReplace { get; init; }
        public SelectionSeedMode SeedSearchStringFromSelection { get; init; } = SelectionSeedMode.None;
        public bool SeedSearchStringFromNonEmptySelection { get; init; }
        public bool SeedSearchStringFromGlobalClipboard { get; init; }
        public FindFocusBehavior ShouldFocus { get; init; } = FindFocusBehavior.NoFocusChange;
        public bool UpdateSearchScope { get; init; }
        public bool Loop { get; init; } = true;
    }

    /// <summary>
    /// Auto-find-in-selection preference used by command helpers.
    /// </summary>
    public enum AutoFindInSelectionMode
    {
        Never,
        Always,
        Multiline
    }

    /// <summary>
    /// Host-level options that normally come from VS Code editor options.
    /// </summary>
    public sealed class FindControllerHostOptions
    {
        public string WordSeparators { get; init; } = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";
        public AutoFindInSelectionMode AutoFindInSelection { get; init; } = AutoFindInSelectionMode.Never;
        public SeedSearchStringMode SeedSearchStringFromSelection { get; init; } = SeedSearchStringMode.Selection;
        public bool DefaultMatchCase { get; init; }
        public bool DefaultWholeWord { get; init; }
        public bool DefaultRegex { get; init; }
        public bool DefaultPreserveCase { get; init; }
        public bool Loop { get; init; } = true;
        public bool EnableGlobalFindClipboard { get; init; }
        public bool IsMacPlatform { get; init; }

        public bool UseGlobalFindClipboard => EnableGlobalFindClipboard && IsMacPlatform;
    }

    /// <summary>
    /// Matches VS Code's `find.seedSearchStringFromSelection` option.
    /// </summary>
    public enum SeedSearchStringMode
    {
        Never,
        Selection,
        Always
    }

    /// <summary>
    /// Minimal surface that the DocUI find controller expects from the editor host.
    /// </summary>
    public interface IEditorHost
    {
        TextModel Model { get; }
        FindControllerHostOptions Options { get; }
        Selection[] GetSelections();
        void SetSelections(IReadOnlyList<Selection> selections);
        void ApplyEdits(IEnumerable<TextEdit> edits);
        void SetValue(string value);
        string GetValue();
        void MoveCursor(TextPosition position);
    }

    public interface IFindControllerStorage
    {
        bool? ReadBool(string key);
        void WriteBool(string key, bool value);
    }

    public interface IFindControllerClipboard
    {
        bool IsEnabled { get; }
        string? ReadText();
        void WriteText(string text);
    }

    /// <summary>
    /// C# counterpart to VS Code's CommonFindController focusing on DocUI parity.
    /// </summary>
    public sealed class DocUIFindController : IDisposable
    {
        private const string StorageIsRegexKey = "editor.isRegex";
        private const string StorageMatchCaseKey = "editor.matchCase";
        private const string StorageWholeWordKey = "editor.wholeWord";
        private const string StoragePreserveCaseKey = "editor.preserveCase";

        private readonly IEditorHost _host;
        private readonly FindReplaceState _state;
        private readonly IFindControllerStorage _storage;
        private readonly IFindControllerClipboard _clipboard;
        private readonly EditorSelectionContext _selectionContext;
        private readonly Func<string?> _wordSeparatorsProvider;
        private FindFocusBehavior _focusBehavior = FindFocusBehavior.NoFocusChange;
        private bool _isDisposed;
        private FindModel? _model;

        public DocUIFindController(
            IEditorHost host,
            IFindControllerStorage? storage = null,
            IFindControllerClipboard? clipboard = null,
            Func<string?>? wordSeparatorsProvider = null)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _state = new FindReplaceState();
            _storage = storage ?? new NullFindControllerStorage();
            _clipboard = clipboard ?? new NullFindControllerClipboard();
            _wordSeparatorsProvider = wordSeparatorsProvider ?? (() => _host.Options.WordSeparators);
            _selectionContext = new EditorSelectionContext(_host.Model, _wordSeparatorsProvider);
            _state.OnFindReplaceStateChange += OnStateChanged;
            LoadPersistedOptions();
        }

        public FindReplaceState State => _state;
        public bool IsFindWidgetVisible { get; private set; }
        public FindFocusBehavior FocusTarget => _focusBehavior;
        public bool IsFindInputFocused => _focusBehavior == FindFocusBehavior.FocusFindInput;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _state.OnFindReplaceStateChange -= OnStateChanged;
            DisposeFindModel();
            _state.Dispose();
        }

        #region Command helpers

        public void StartFindAction()
        {
            var seedPreference = _host.Options.SeedSearchStringFromSelection;
            var shouldSeed = seedPreference != SeedSearchStringMode.Never;
            var seedMode = shouldSeed ? SelectionSeedMode.Single : SelectionSeedMode.None;
            var requireNonEmpty = seedPreference == SeedSearchStringMode.Selection;
            Start(new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = seedMode,
                SeedSearchStringFromNonEmptySelection = requireNonEmpty,
                SeedSearchStringFromGlobalClipboard = _host.Options.UseGlobalFindClipboard,
                ShouldFocus = FindFocusBehavior.FocusFindInput,
                UpdateSearchScope = ShouldAutoFindInSelection(),
                Loop = _host.Options.Loop
            });
        }

        public void StartFindReplaceAction()
        {
            var selection = GetPrimarySelection();
            var singleLineSelection = !selection.IsEmpty && selection.SelectionStart.LineNumber == selection.SelectionEnd.LineNumber;
            var hostSeedPreference = _host.Options.SeedSearchStringFromSelection;
            var allowSelectionSeed = hostSeedPreference != SeedSearchStringMode.Never;
            var seedFromSelection = allowSelectionSeed && singleLineSelection && !IsFindInputFocused;
            var shouldFocus = (IsFindInputFocused || seedFromSelection)
                ? FindFocusBehavior.FocusReplaceInput
                : FindFocusBehavior.FocusFindInput;

            Start(new FindStartOptions
            {
                ForceRevealReplace = true,
                SeedSearchStringFromSelection = seedFromSelection ? SelectionSeedMode.Single : SelectionSeedMode.None,
                SeedSearchStringFromNonEmptySelection = hostSeedPreference == SeedSearchStringMode.Selection,
                SeedSearchStringFromGlobalClipboard = allowSelectionSeed && _host.Options.UseGlobalFindClipboard,
                ShouldFocus = shouldFocus,
                UpdateSearchScope = false,
                Loop = _host.Options.Loop
            });
        }

        public void StartFindWithSelectionAction()
        {
            Start(new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = SelectionSeedMode.Multiple,
                SeedSearchStringFromNonEmptySelection = false,
                SeedSearchStringFromGlobalClipboard = false,
                ShouldFocus = FindFocusBehavior.NoFocusChange,
                UpdateSearchScope = false,
                Loop = _host.Options.Loop
            });
        }

        public bool NextMatchFindAction()
        {
            if (MoveToNextMatch())
            {
                return true;
            }

            var shouldSeed = string.IsNullOrEmpty(_state.SearchString)
                && _host.Options.SeedSearchStringFromSelection != SeedSearchStringMode.Never;

            Start(new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = shouldSeed ? SelectionSeedMode.Single : SelectionSeedMode.None,
                SeedSearchStringFromNonEmptySelection = _host.Options.SeedSearchStringFromSelection == SeedSearchStringMode.Selection,
                SeedSearchStringFromGlobalClipboard = _host.Options.UseGlobalFindClipboard,
                ShouldFocus = FindFocusBehavior.NoFocusChange,
                UpdateSearchScope = false,
                Loop = _host.Options.Loop
            });

            return MoveToNextMatch();
        }

        public bool PreviousMatchFindAction()
        {
            if (MoveToPrevMatch())
            {
                return true;
            }

            var shouldSeed = string.IsNullOrEmpty(_state.SearchString)
                && _host.Options.SeedSearchStringFromSelection != SeedSearchStringMode.Never;

            Start(new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = shouldSeed ? SelectionSeedMode.Single : SelectionSeedMode.None,
                SeedSearchStringFromNonEmptySelection = _host.Options.SeedSearchStringFromSelection == SeedSearchStringMode.Selection,
                SeedSearchStringFromGlobalClipboard = _host.Options.UseGlobalFindClipboard,
                ShouldFocus = FindFocusBehavior.NoFocusChange,
                UpdateSearchScope = false,
                Loop = _host.Options.Loop
            });

            return MoveToPrevMatch();
        }

        public bool NextSelectionMatchFindAction()
        {
            _ = SeedSearchStringFromSelection(SelectionSeedMode.Single, seedFromNonEmptySelection: false);
            if (MoveToNextMatch())
            {
                return true;
            }

            Start(new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = SelectionSeedMode.None,
                SeedSearchStringFromNonEmptySelection = false,
                SeedSearchStringFromGlobalClipboard = false,
                ShouldFocus = FindFocusBehavior.NoFocusChange,
                UpdateSearchScope = false,
                Loop = _host.Options.Loop
            });

            return MoveToNextMatch();
        }

        public bool PreviousSelectionMatchFindAction()
        {
            _ = SeedSearchStringFromSelection(SelectionSeedMode.Single, seedFromNonEmptySelection: false);
            if (MoveToPrevMatch())
            {
                return true;
            }

            Start(new FindStartOptions
            {
                ForceRevealReplace = false,
                SeedSearchStringFromSelection = SelectionSeedMode.None,
                SeedSearchStringFromNonEmptySelection = false,
                SeedSearchStringFromGlobalClipboard = false,
                ShouldFocus = FindFocusBehavior.NoFocusChange,
                UpdateSearchScope = false,
                Loop = _host.Options.Loop
            });

            return MoveToPrevMatch();
        }

        public bool SelectAllMatchesAction()
        {
            EnsureModelSelectionUpToDate();
            var selections = EnsureModel().SelectAllMatches();
            if (selections.Length == 0)
            {
                return false;
            }

            _host.SetSelections(selections);
            return true;
        }

        #endregion

        #region Public operations

        public void Start(FindStartOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            EnsureModelSelectionUpToDate();

            string? searchString = null;
            var seededSearch = TrySeedSearchString(options.SeedSearchStringFromSelection, options.SeedSearchStringFromNonEmptySelection);
            if (seededSearch.HasValue)
            {
                searchString = ResolveSeededSearchString(seededSearch.Value);
            }
            else if (options.SeedSearchStringFromGlobalClipboard && _clipboard.IsEnabled)
            {
                var clipboardText = _clipboard.ReadText();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    searchString = clipboardText;
                }
            }

            var shouldAutoScope = ShouldAutoFindInSelection();
            var shouldUpdateScope = options.UpdateSearchScope || shouldAutoScope;
            Range[]? searchScope = null;
            if (shouldUpdateScope)
            {
                searchScope = BuildSearchScope(ignoreEmpty: true);
            }

            // C# host exposes AutoFindInSelection, so compute scope inside Start to cover indirect callers (e.g., Next/Prev fallbacks).
            var shouldApplyScope = searchScope != null;
            var wasWidgetVisible = _state.IsRevealed;
            var shouldRevealReplace = options.ForceRevealReplace || (wasWidgetVisible && _state.IsReplaceRevealed);

            _state.Change(
                searchString: searchString ?? _state.SearchString,
                isRevealed: true,
                isReplaceRevealed: shouldRevealReplace,
                searchScope: shouldApplyScope ? searchScope : null,
                searchScopeProvided: shouldApplyScope,
                loop: options.Loop,
                moveCursor: false,
                updateHistory: true);

            if (options.ShouldFocus != FindFocusBehavior.NoFocusChange)
            {
                _focusBehavior = options.ShouldFocus;
            }

            IsFindWidgetVisible = true;
            EnsureModel();
        }

        public bool MoveToNextMatch()
        {
            EnsureModelSelectionUpToDate();
            EnsureModel().FindNext();
            return _state.CurrentMatch != null;
        }

        public bool MoveToPrevMatch()
        {
            EnsureModelSelectionUpToDate();
            EnsureModel().FindPrevious();
            return _state.CurrentMatch != null;
        }

        public bool Replace()
        {
            EnsureModelSelectionUpToDate();
            EnsureModel().Replace();
            return _state.CurrentMatch != null;
        }

        public int ReplaceAll()
        {
            EnsureModelSelectionUpToDate();
            return EnsureModel().ReplaceAll();
        }

        public void CloseFindWidget()
        {
            _state.Change(isRevealed: false, searchScope: null, searchScopeProvided: true, moveCursor: false);
            IsFindWidgetVisible = false;
            _focusBehavior = FindFocusBehavior.NoFocusChange;
            DisposeFindModel();
        }

        public void ToggleRegex()
        {
            _state.Change(isRegex: !_state.IsRegex, moveCursor: false);
        }

        public void ToggleMatchCase()
        {
            _state.Change(matchCase: !_state.MatchCase, moveCursor: false);
        }

        public void ToggleWholeWord()
        {
            _state.Change(wholeWord: !_state.WholeWord, moveCursor: false);
        }

        public void TogglePreserveCase()
        {
            _state.Change(preserveCase: !_state.PreserveCase, moveCursor: false);
        }

        public void SetSearchString(string? value, bool seededFromSelection = false)
        {
            SetSearchStringInternal(value ?? string.Empty, seededFromSelection);
        }

        public void SetReplaceString(string? value)
        {
            _state.Change(replaceString: value ?? string.Empty, moveCursor: false);
        }

        public string? GetGlobalFindClipboardText()
        {
            return _clipboard.IsEnabled ? _clipboard.ReadText() : null;
        }

        #endregion

        #region Helpers

        private FindModel EnsureModel()
        {
            if (_model == null)
            {
                _model = new FindModel(_host.Model, _state, _wordSeparatorsProvider);
                var selection = GetPrimarySelection();
                _model.SetSelection(new Range(selection.SelectionStart, selection.SelectionEnd));
            }

            return _model;
        }

        private void DisposeFindModel()
        {
            if (_model == null)
            {
                return;
            }

            _model.Dispose();
            _model = null;
            _state.ChangeMatchInfo(0, 0, clearCurrentMatch: true);
        }

        private void LoadPersistedOptions()
        {
            var matchCase = _storage.ReadBool(StorageMatchCaseKey) ?? _host.Options.DefaultMatchCase;
            var wholeWord = _storage.ReadBool(StorageWholeWordKey) ?? _host.Options.DefaultWholeWord;
            var isRegex = _storage.ReadBool(StorageIsRegexKey) ?? _host.Options.DefaultRegex;
            var preserveCase = _storage.ReadBool(StoragePreserveCaseKey) ?? _host.Options.DefaultPreserveCase;

            _state.Change(
                matchCase: matchCase,
                wholeWord: wholeWord,
                isRegex: isRegex,
                preserveCase: preserveCase,
                moveCursor: false,
                updateHistory: false);
        }

        private void OnStateChanged(object? sender, FindReplaceStateChangedEventArgs e)
        {
            if (e.IsRegex)
            {
                _storage.WriteBool(StorageIsRegexKey, _state.IsRegex);
            }

            if (e.MatchCase)
            {
                _storage.WriteBool(StorageMatchCaseKey, _state.MatchCase);
            }

            if (e.WholeWord)
            {
                _storage.WriteBool(StorageWholeWordKey, _state.WholeWord);
            }

            if (e.PreserveCase)
            {
                _storage.WriteBool(StoragePreserveCaseKey, _state.PreserveCase);
            }

            if (e.SearchString && _clipboard.IsEnabled && _host.Options.UseGlobalFindClipboard && !string.IsNullOrEmpty(_state.SearchString))
            {
                _clipboard.WriteText(_state.SearchString);
            }

            if (e.CurrentMatch && _state.CurrentMatch.HasValue)
            {
                ApplySelectionToHost(_state.CurrentMatch.Value);
            }

            if (e.IsRevealed && !_state.IsRevealed)
            {
                IsFindWidgetVisible = false;
                _focusBehavior = FindFocusBehavior.NoFocusChange;
                DisposeFindModel();
            }
        }

        private void EnsureModelSelectionUpToDate()
        {
            var selection = GetPrimarySelection();
            _selectionContext.Update(selection);
            _model?.SetSelection(new Range(selection.SelectionStart, selection.SelectionEnd));
        }

        private Selection GetPrimarySelection()
        {
            var selections = _host.GetSelections();
            return selections.Length > 0 ? selections[0] : new Selection(TextPosition.Origin, TextPosition.Origin);
        }

        private void ApplySelectionToHost(Range range)
        {
            var selection = new Selection(range.Start, range.End);
            _host.SetSelections(new[] { selection });
        }

        private bool ShouldAutoFindInSelection()
        {
            var autoMode = _host.Options.AutoFindInSelection;
            if (autoMode == AutoFindInSelectionMode.Never)
            {
                return false;
            }

            var primary = GetPrimarySelection();
            if (autoMode == AutoFindInSelectionMode.Always)
            {
                return !primary.IsEmpty;
            }

            if (autoMode == AutoFindInSelectionMode.Multiline)
            {
                return !primary.IsEmpty && primary.SelectionStart.LineNumber != primary.SelectionEnd.LineNumber;
            }

            return false;
        }

        private Range[]? BuildSearchScope(bool ignoreEmpty)
        {
            var selections = _host.GetSelections();
            var ranges = new List<Range>();
            foreach (var selection in selections)
            {
                if (selection.IsEmpty && ignoreEmpty)
                {
                    continue;
                }
                ranges.Add(new Range(selection.SelectionStart, selection.SelectionEnd));
            }

            return ranges.Count > 0 ? ranges.ToArray() : null;
        }

        private SeededSearchString? TrySeedSearchString(SelectionSeedMode mode, bool seedFromNonEmptySelection)
        {
            if (mode == SelectionSeedMode.None)
            {
                return null;
            }

            var selection = GetPrimarySelection();
            _selectionContext.Update(selection);

            var seed = FindUtilities.GetSelectionSearchString(_selectionContext, mode, seedFromNonEmptySelection);
            if (string.IsNullOrEmpty(seed))
            {
                return null;
            }

            return new SeededSearchString(seed, ShouldNormalizeSeed(mode));
        }

        private bool SeedSearchStringFromSelection(SelectionSeedMode mode, bool seedFromNonEmptySelection)
        {
            var seed = TrySeedSearchString(mode, seedFromNonEmptySelection);
            if (!seed.HasValue)
            {
                return false;
            }

            SetSearchStringInternal(seed.Value.Text, seed.Value.ShouldNormalize);
            return true;
        }

        private string NormalizeSeededSearchString(string searchString)
        {
            return _state.IsRegex ? Regex.Escape(searchString) : searchString;
        }

        private string ResolveSeededSearchString(SeededSearchString seed)
        {
            return seed.ShouldNormalize
                ? NormalizeSeededSearchString(seed.Text)
                : seed.Text;
        }

        private static bool ShouldNormalizeSeed(SelectionSeedMode mode)
        {
            return mode == SelectionSeedMode.Single;
        }

        private void SetSearchStringInternal(string searchString, bool seededFromSelection)
        {
            var normalized = seededFromSelection
                ? NormalizeSeededSearchString(searchString)
                : searchString;

            _state.Change(searchString: normalized, moveCursor: false);
        }

        #endregion

        #region Nested helpers

        private readonly struct SeededSearchString
        {
            public SeededSearchString(string text, bool shouldNormalize)
            {
                Text = text;
                ShouldNormalize = shouldNormalize;
            }

            public string Text { get; }
            public bool ShouldNormalize { get; }
        }

        private sealed class EditorSelectionContext : IEditorSelectionContext
        {
            private Selection _selection;

            private readonly Func<string?> _wordSeparatorsProvider;

            public EditorSelectionContext(TextModel model, Func<string?> wordSeparatorsProvider)
            {
                Model = model;
                _wordSeparatorsProvider = wordSeparatorsProvider ?? (() => null);
                _selection = new Selection(TextPosition.Origin, TextPosition.Origin);
            }

            public TextModel Model { get; }
            public Selection Selection => _selection;
            public string? WordSeparators => _wordSeparatorsProvider();

            public void Update(Selection selection)
            {
                _selection = selection;
            }
        }

        private sealed class NullFindControllerStorage : IFindControllerStorage
        {
            public bool? ReadBool(string key) => null;
            public void WriteBool(string key, bool value) { }
        }

        private sealed class NullFindControllerClipboard : IFindControllerClipboard
        {
            public bool IsEnabled => false;
            public string? ReadText() => null;
            public void WriteText(string text) { }
        }

        #endregion
    }
}