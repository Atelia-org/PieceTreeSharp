/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Ported from vs/editor/contrib/find/browser/findController.ts#getSelectionSearchString

using System;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.DocUI
{
    /// <summary>
    /// Controls how <see cref="FindUtilities.GetSelectionSearchString"/> seeds text from the editor selection.
    /// Mirrors the TS union type ('none' | 'single' | 'multiple').
    /// </summary>
    public enum SelectionSeedMode
    {
        None,
        Single,
        Multiple
    }

    /// <summary>
    /// Minimal context required to derive selection-based search strings (TextModel + current selection + word separators).
    /// </summary>
    public interface IEditorSelectionContext
    {
        TextModel Model { get; }
        Selection Selection { get; }
        string? WordSeparators { get; }
    }

    /// <summary>
    /// Represents the word that surrounds a caret position.
    /// </summary>
    public readonly record struct WordAtPosition(string Word, int StartColumn, int EndColumn);

    /// <summary>
    /// Utility helpers shared by DocUI Find/Replace components.
    /// </summary>
    public static class FindUtilities
    {
        private const int SearchStringMaxLength = 524_288;
        private const string DefaultWordSeparators = "`~!@#$%^&*()-=+[{]}\\|;:'\",.<>/?";

        /// <summary>
        /// Derives the search string based on the current selection and word-under-cursor semantics.
        /// TS parity of getSelectionSearchString() used by CommonFindController.
        /// </summary>
        public static string? GetSelectionSearchString(
            IEditorSelectionContext context,
            SelectionSeedMode seedMode = SelectionSeedMode.Single,
            bool seedFromNonEmptySelection = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var selection = context.Selection;
            if (!ShouldUseSelection(selection, seedMode))
            {
                return null;
            }

            if (selection.IsEmpty)
            {
                if (seedFromNonEmptySelection)
                {
                    return null;
                }

                var word = GetWordAtPosition(context);
                return word?.Word;
            }

            var range = new Range(selection.SelectionStart, selection.SelectionEnd);
            var model = context.Model;
            var startOffset = model.GetOffsetAt(range.Start);
            var endOffset = model.GetOffsetAt(range.End);

            if (endOffset - startOffset >= SearchStringMaxLength)
            {
                return null;
            }

            return model.GetValueInRange(range);
        }

        /// <summary>
        /// Attempts to locate the word that contains or neighbors the caret position.
        /// Returns null when the caret is on whitespace or separators.
        /// </summary>
        public static WordAtPosition? GetWordAtPosition(IEditorSelectionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var position = context.Selection.SelectionStart;
            var lineText = context.Model.GetLineContent(position.LineNumber) ?? string.Empty;
            if (lineText.Length == 0)
            {
                return null;
            }

            var separators = context.WordSeparators ?? DefaultWordSeparators;
            var classifier = WordCharacterClassifierCache.Get(separators);

            var caretIndex = Math.Clamp(position.Column - 1, 0, lineText.Length);
            var probeIndex = caretIndex;
            if (probeIndex == lineText.Length && probeIndex > 0)
            {
                if (!UnicodeUtility.TryGetPreviousCodePoint(lineText, probeIndex, out _, out var prevLength))
                {
                    return null;
                }

                probeIndex -= prevLength;
            }

            if (!TryGetWordBounds(lineText, probeIndex, classifier, out var wordStart, out var wordEnd))
            {
                return null;
            }

            var word = lineText.Substring(wordStart, wordEnd - wordStart);
            return new WordAtPosition(word, wordStart + 1, wordEnd + 1);
        }

        private static bool ShouldUseSelection(Selection selection, SelectionSeedMode seedMode)
        {
            return seedMode switch
            {
                SelectionSeedMode.None => false,
                SelectionSeedMode.Single => selection.SelectionStart.LineNumber == selection.SelectionEnd.LineNumber,
                SelectionSeedMode.Multiple => true,
                _ => false
            };
        }

        private static bool TryGetWordBounds(string text, int initialIndex, WordCharacterClassifier classifier, out int wordStart, out int wordEnd)
        {
            wordStart = 0;
            wordEnd = 0;
            if (text.Length == 0 || initialIndex < 0 || initialIndex >= text.Length)
            {
                return false;
            }

            if (!UnicodeUtility.TryGetCodePointAt(text, initialIndex, out var codePoint, out var codeUnitLength) || !IsWordCharacter(codePoint, classifier))
            {
                if (!UnicodeUtility.TryGetPreviousCodePoint(text, initialIndex, out codePoint, out codeUnitLength) || !IsWordCharacter(codePoint, classifier))
                {
                    return false;
                }

                initialIndex -= codeUnitLength;
            }

            wordStart = initialIndex;
            wordEnd = initialIndex + codeUnitLength;

            while (wordStart > 0 && UnicodeUtility.TryGetPreviousCodePoint(text, wordStart, out var prevCodePoint, out var prevLength) && IsWordCharacter(prevCodePoint, classifier))
            {
                wordStart -= prevLength;
            }

            while (wordEnd < text.Length && UnicodeUtility.TryGetCodePointAt(text, wordEnd, out var nextCodePoint, out var nextLength) && IsWordCharacter(nextCodePoint, classifier))
            {
                wordEnd += nextLength;
            }

            return true;
        }

        private static bool IsWordCharacter(int codePoint, WordCharacterClassifier classifier)
        {
            return classifier.GetClass(codePoint) == WordCharacterClass.Regular;
        }
    }
}
