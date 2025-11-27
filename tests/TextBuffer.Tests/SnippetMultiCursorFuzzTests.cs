// Original C# implementation
// Purpose: Fuzz testing for snippet placeholders with multi-cursor scenarios
// - Validates snippet session state consistency across random edits
// Created: 2025-11-22

using System;
using System.Linq;
using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Cursor;
using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests;

public class SnippetMultiCursorFuzzTests
{
    [Fact]
    public void SnippetAndMultiCursor_Fuzz_NoCrashesAndInvariantsHold()
    {
        int seed = 12345;
        Random rng = new(seed);
        const int Iterations = 10; // reduced for faster, repeatable runs during CI

        for (int i = 0; i < Iterations; i++)
        {
            // Create simple base text
            string baseText = "base-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            TextModel model = new(baseText);
            int expectedLen = model.GetLength();

            // Create cursor collection and random cursors
            CursorCollection collection = model.CreateCursorCollection();
            int cursorCount = rng.Next(1, 4);
            TextPosition[] positions = new TextPosition[cursorCount];
            for (int c = 0; c < cursorCount; c++)
            {
                int offset = rng.Next(0, model.GetLength() + 1);
                positions[c] = model.GetPositionAt(offset);
                Cursor.Cursor cursor = collection.CreateCursor(positions[c]);
            }

            // Create a single snippet session and insert multiple snippets across cursors
            SnippetController controller = model.CreateSnippetController();
            SnippetSession session = controller.CreateSession();

            // Insert unique placeholder content per cursor so we can find and mutate later.
            for (int c = 0; c < cursorCount; c++)
            {
                TextPosition pos = positions[c];
                string token = $"PLH_{i}_{c}";
                string snippet = "${1:" + token + "}"; // ${1:PLH_i_c}
                session.InsertSnippet(pos, snippet);
                expectedLen += token.Length; // inserted plain token
            }

            // Get all snippet-placeholder decorations
            IReadOnlyList<ModelDecoration> decorations = model.GetDecorationsInRange(new TextRange(0, model.GetLength()));
            ModelDecoration[] placeholders = decorations.Where(d => d.Options.Description == "snippet-placeholder").ToArray();

            // Basic invariants: placeholders count should equal number of cursor placeholders inserted
            Assert.True(placeholders.Length >= cursorCount);

            // Navigate placeholders using the controller's NextPlaceholder and update each placeholder's content
            int navCount = 0;
            TextPosition? p;
            while ((p = controller.NextPlaceholder()) != null)
            {
                navCount++;
                TextPosition pos = p.Value;
                int startOff = model.GetOffsetAt(pos);

                // Find the corresponding decoration with the same start offset
                ModelDecoration? dec = placeholders.FirstOrDefault(d => d.Range.StartOffset == startOff);
                Assert.NotNull(dec);

                TextRange decRange = dec.Range;
                TextPosition decStartPos = model.GetPositionAt(decRange.StartOffset);
                TextPosition decEndPos = model.GetPositionAt(decRange.EndOffset);

                // Replace placeholder content with a new small string
                string newContent = "Z" + rng.Next(0, 99).ToString();
                model.PushEditOperations([new TextEdit(decStartPos, decEndPos, newContent)]);

                expectedLen += newContent.Length - decRange.Length;

                // After replacement, verify the model length matches expected update
                Assert.Equal(expectedLen, model.GetLength());
            }

            // There should have been at least one nav if there were placeholders
            Assert.True(placeholders.Length == 0 || navCount > 0);

            // Validate that all cursor positions are still valid offsets in the model
            IReadOnlyList<TextPosition> cursorPositions = collection.GetCursorPositions();
            foreach (TextPosition cp in cursorPositions)
            {
                int off = model.GetOffsetAt(cp);
                Assert.InRange(off, 0, model.GetLength());
            }

            // Clean up disposables
            collection.Dispose();
            controller.Dispose();
        }
    }
}
