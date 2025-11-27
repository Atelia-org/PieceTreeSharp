// Source: ts/src/vs/editor/test/common/model/textModel.test.ts
// - Tests: TextModel creation, selection logic, line content, editing operations
// Ported: 2025-11-19

using System;
using System.Collections.Generic;
using System.Linq;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Decorations;
using PieceTree.TextBuffer.Services;
using Xunit;

namespace PieceTree.TextBuffer.Tests;

public class TextModelTests
{
    [Fact]
    public void TestSelectionLogic()
    {
        TextPosition p1 = new(1, 1);
        TextPosition p2 = new(1, 5);

        Selection sel1 = new(p1, p2); // LTR
        Assert.Equal(p1, sel1.Start);
        Assert.Equal(p2, sel1.End);
        Assert.Equal(SelectionDirection.LTR, sel1.Direction);
        Assert.False(sel1.IsEmpty);

        Selection sel2 = new(p2, p1); // RTL
        Assert.Equal(p1, sel2.Start);
        Assert.Equal(p2, sel2.End);
        Assert.Equal(SelectionDirection.RTL, sel2.Direction);
        Assert.False(sel2.IsEmpty);

        Selection sel3 = new(p1, p1);
        Assert.True(sel3.IsEmpty);
        Assert.Equal(sel3.Start, sel3.End);

        Assert.True(sel1.Contains(new TextPosition(1, 3)));
        Assert.True(sel1.Contains(p1));
        Assert.True(sel1.Contains(p2));
        Assert.False(sel1.Contains(new TextPosition(1, 6)));
    }

    [Fact]
    public void TestTextModel_Creation()
    {
        TextModel model = new("Hello\nWorld");
        Assert.Equal("Hello\nWorld", model.GetValue());
        Assert.Equal(2, model.GetLineCount());
        Assert.Equal("Hello", model.GetLineContent(1));
        Assert.Equal("World", model.GetLineContent(2));
        Assert.Equal(1, model.VersionId);
    }

    [Fact]
    public void TestTextModel_ApplyEdits()
    {
        TextModel model = new("Hello World");
        bool eventFired = false;
        model.OnDidChangeContent += (s, e) =>
        {
            eventFired = true;
            Assert.Equal(2, e.VersionId);
            Assert.Single(e.Changes);
        };

        // Replace "World" with "Universe"
        // "World" starts at 1, 7. Ends at 1, 12.
        TextEdit edit = new(new TextPosition(1, 7), new TextPosition(1, 12), "Universe");
        model.ApplyEdits([edit]);

        Assert.Equal("Hello Universe", model.GetValue());
        Assert.Equal(2, model.VersionId);
        Assert.True(eventFired);
    }

    [Fact]
    public void TestTextModel_MultipleEdits()
    {
        TextModel model = new("Hello World");

        // Insert "Big " before "World" -> "Hello Big World"
        // Replace "Hello" with "Hi" -> "Hi Big World"

        // Edits:
        // 1. Insert at 1, 7: "Big "
        // 2. Replace 1, 1 to 1, 6: "Hi"

        TextEdit edit1 = new(new TextPosition(1, 7), new TextPosition(1, 7), "Big ");
        TextEdit edit2 = new(new TextPosition(1, 1), new TextPosition(1, 6), "Hi");

        model.ApplyEdits([edit1, edit2]);

        Assert.Equal("Hi Big World", model.GetValue());
    }

    [Fact]
    public void TestTextModel_Decorations()
    {
        TextModel model = new("Hello World");
        // Decoration on "World" (offsets 6-11)
        TextRange range = new(6, 11);
        ModelDecoration decoration = model.AddDecoration(range, ModelDecorationOptions.CreateSelectionOptions());

        Assert.Equal(6, decoration.Range.StartOffset);
        Assert.Equal(11, decoration.Range.EndOffset);

        // Insert "Beautiful " before "World" at offset 6 (1, 7)
        TextEdit edit = new(new TextPosition(1, 7), new TextPosition(1, 7), "Beautiful ");
        model.ApplyEdits([edit]);

        Assert.Equal("Hello Beautiful World", model.GetValue());

        // Decoration should shift
        Assert.Equal(6, decoration.Range.StartOffset);
        Assert.Equal(21, decoration.Range.EndOffset);

        IReadOnlyList<ModelDecoration> found = model.GetDecorationsInRange(new TextRange(6, 21));
        Assert.Single(found);
        Assert.Equal(decoration, found[0]);
    }

    [Fact]
    public void TextModel_RaisesDecorationEvents()
    {
        TextModel model = new("Hello World");
        ModelDecoration decoration = model.AddDecoration(new TextRange(6, 11), ModelDecorationOptions.CreateSelectionOptions());
        TextModelDecorationsChangedEventArgs? observed = null;
        model.OnDidChangeDecorations += (_, args) => observed = args;

        model.ApplyEdits(
        [
            new TextEdit(new TextPosition(1, 6), new TextPosition(1, 6), "Beautiful ")
        ]);

        Assert.NotNull(observed);
        Assert.Contains(observed!.Changes, c => c.Id == decoration.Id && c.Kind == DecorationDeltaKind.Updated);
    }

    [Fact]
    public void UndoRedo_Roundtrip()
    {
        TextModel model = new("Hello");
        model.PushEditOperations(
        [
            new TextEdit(new TextPosition(1, 6), new TextPosition(1, 6), " World")
        ]);

        Assert.Equal("Hello World", model.GetValue());
        Assert.True(model.CanUndo);
        Assert.True(model.Undo());
        Assert.Equal("Hello", model.GetValue());
        Assert.True(model.CanRedo);
        Assert.True(model.Redo());
        Assert.Equal("Hello World", model.GetValue());
    }

    [Fact]
    public void StackElementBoundariesAreRespected()
    {
        TextModel model = new("abc123");
        model.PushEditOperations(
        [
            new TextEdit(new TextPosition(1, 4), new TextPosition(1, 7), "XYZ")
        ]);

        model.PushStackElement();

        model.PushEditOperations(
        [
            new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "HELLO ")
        ]);

        Assert.Equal("HELLO abcXYZ", model.GetValue());

        Assert.True(model.Undo());
        Assert.Equal("abcXYZ", model.GetValue());

        Assert.True(model.Undo());
        Assert.Equal("abc123", model.GetValue());
    }

    [Fact]
    public void UpdateOptionsRaisesChangeEvent()
    {
        TextModel model = new("line");
        TextModelOptionsChangedEventArgs? captured = null;
        model.OnDidChangeOptions += (_, args) => captured = args;

        model.UpdateOptions(new TextModelUpdateOptions
        {
            TabSize = 2,
            InsertSpaces = false,
            TrimAutoWhitespace = false,
        });

        Assert.NotNull(captured);
        Assert.True(captured!.TabSizeChanged);
        Assert.True(captured.InsertSpacesChanged);
        Assert.True(captured.TrimAutoWhitespaceChanged);
        Assert.False(model.GetOptions().TrimAutoWhitespace);
        Assert.Equal(2, model.GetOptions().TabSize);
    }

    [Fact]
    public void DetectIndentationPrefersSpaces()
    {
        string text = "def\n  foo()\n    bar()\n";
        TextModel model = new(text);
        model.DetectIndentation(defaultInsertSpaces: false, defaultTabSize: 4);

        TextModelResolvedOptions options = model.GetOptions();
        Assert.True(options.InsertSpaces);
        Assert.Equal(2, options.TabSize);
        Assert.Equal(2, options.IndentSize);
    }

    [Fact]
    public void PushEolIsUndoable()
    {
        TextModel model = new("A\nB\n");
        model.PushEol(EndOfLineSequence.CRLF);
        Assert.Contains("\r\n", model.GetValue());

        bool undoEventSeen = false;
        model.OnDidChangeContent += (_, args) =>
        {
            if (args.IsUndo)
            {
                undoEventSeen = true;
            }
        };

        Assert.True(model.Undo());
        Assert.Equal("A\nB\n", model.GetValue());
        Assert.True(undoEventSeen);
    }

    [Fact]
    public void SetLanguageRaisesEvent()
    {
        TextModel model = new("text");
        string? newLanguage = null;
        model.OnDidChangeLanguage += (_, args) => newLanguage = args.NewLanguageId;

        model.SetLanguage("csharp");
        Assert.Equal("csharp", newLanguage);

        newLanguage = null;
        model.SetLanguage("csharp");
        Assert.Null(newLanguage);
    }

    [Fact]
    public void CreationOptionsAreApplied()
    {
        TextModelCreationOptions creation = new()
        {
            DetectIndentation = false,
            TabSize = 2,
            IndentSize = 2,
            InsertSpaces = false,
            TrimAutoWhitespace = true,
            DefaultEol = DefaultEndOfLine.CRLF,
            LargeFileOptimizations = false,
            BracketPairColorizationOptions = new BracketPairColorizationOptions(false, true),
        };

        TextModel model = new("line1\r\nline2", creation, "plaintext");
        TextModelResolvedOptions options = model.GetOptions();

        Assert.Equal(2, options.TabSize);
        Assert.Equal(2, options.IndentSize);
        Assert.False(options.InsertSpaces);
        Assert.True(options.TrimAutoWhitespace);
        Assert.Equal(DefaultEndOfLine.CRLF, options.DefaultEol);
        Assert.False(options.LargeFileOptimizations);
        Assert.False(options.BracketPairColorizationOptions.Enabled);
        Assert.True(options.BracketPairColorizationOptions.IndependentColorPoolPerBracketType);
    }

    [Fact]
    public void DetectIndentationRunsDuringConstruction()
    {
        TextModelCreationOptions creation = new()
        {
            DetectIndentation = true,
            TabSize = 4,
            IndentSize = 4,
            InsertSpaces = true,
        };

        TextModel model = new("\tline\n\tindent", creation, "plaintext");
        TextModelResolvedOptions options = model.GetOptions();

        Assert.False(options.InsertSpaces);
        Assert.Equal(4, options.TabSize);
    }

    [Fact]
    public void CursorStateComputerIsInvoked()
    {
        TextModel model = new("abc");
        bool invoked = false;

        CursorStateComputer computer = inverseChanges =>
        {
            invoked = inverseChanges.Count > 0;
            return null;
        };

        model.PushEditOperations(
            [new TextEdit(new TextPosition(1, 4), new TextPosition(1, 4), "!")],
            beforeCursorState: null,
            cursorStateComputer: computer,
            undoLabel: "exclaim");

        Assert.True(invoked);
    }

    [Fact]
    public void LanguageConfigurationEventsFire()
    {
        TestLanguageConfigurationService service = new();
        TextModel model = new("text", TextModelCreationOptions.Default, "langA", service);
        string? observed = null;
        model.OnDidChangeLanguageConfiguration += (_, args) => observed = args.LanguageId;

        service.Raise("langA");
        Assert.Equal("langA", observed);

        observed = null;
        model.SetLanguage("langB");

        service.Raise("langA");
        Assert.Null(observed);

        service.Raise("langB");
        Assert.Equal("langB", observed);
    }

    [Fact]
    public void AttachedEventsFireOnTransitions()
    {
        TextModel model = new("text");
        List<bool> events = [];
        model.OnDidChangeAttached += (_, args) => events.Add(args.IsAttached);

        model.AttachEditor();
        model.AttachEditor();
        model.DetachEditor();
        model.DetachEditor();

        Assert.Equal(new[] { true, false }, events);
    }

    private sealed class TestLanguageConfigurationService : ILanguageConfigurationService
    {
        private readonly List<(string LanguageId, EventHandler<LanguageConfigurationChangedEventArgs> Handler)> _handlers = [];

        public event EventHandler<LanguageConfigurationChangedEventArgs>? OnDidChange;

        public IDisposable Subscribe(string languageId, EventHandler<LanguageConfigurationChangedEventArgs> callback)
        {
            _handlers.Add((languageId, callback));
            return new DelegateDisposable(() => _handlers.RemoveAll(tuple => tuple.LanguageId == languageId && tuple.Handler == callback));
        }

        public void Raise(string languageId)
        {
            LanguageConfigurationChangedEventArgs args = new(languageId);
            OnDidChange?.Invoke(this, args);
            foreach ((string LanguageId, EventHandler<LanguageConfigurationChangedEventArgs> Handler) in _handlers.ToArray())
            {
                if (string.Equals(LanguageId, languageId, StringComparison.Ordinal))
                {
                    Handler(this, args);
                }
            }
        }

        private sealed class DelegateDisposable : IDisposable
        {
            private readonly Action _dispose;
            private bool _isDisposed;

            public DelegateDisposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                _dispose();
            }
        }
    }
}
