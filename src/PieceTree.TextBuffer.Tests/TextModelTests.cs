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
        var p1 = new TextPosition(1, 1);
        var p2 = new TextPosition(1, 5);
        
        var sel1 = new Selection(p1, p2); // LTR
        Assert.Equal(p1, sel1.Start);
        Assert.Equal(p2, sel1.End);
        Assert.Equal(SelectionDirection.LTR, sel1.Direction);
        Assert.False(sel1.IsEmpty);
        
        var sel2 = new Selection(p2, p1); // RTL
        Assert.Equal(p1, sel2.Start);
        Assert.Equal(p2, sel2.End);
        Assert.Equal(SelectionDirection.RTL, sel2.Direction);
        Assert.False(sel2.IsEmpty);
        
        var sel3 = new Selection(p1, p1);
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
        var model = new TextModel("Hello\nWorld");
        Assert.Equal("Hello\nWorld", model.GetValue());
        Assert.Equal(2, model.GetLineCount());
        Assert.Equal("Hello", model.GetLineContent(1));
        Assert.Equal("World", model.GetLineContent(2));
        Assert.Equal(1, model.VersionId);
    }

    [Fact]
    public void TestTextModel_ApplyEdits()
    {
        var model = new TextModel("Hello World");
        bool eventFired = false;
        model.OnDidChangeContent += (s, e) => 
        {
            eventFired = true;
            Assert.Equal(2, e.VersionId);
            Assert.Single(e.Changes);
        };

        // Replace "World" with "Universe"
        // "World" starts at 1, 7. Ends at 1, 12.
        var edit = new TextEdit(new TextPosition(1, 7), new TextPosition(1, 12), "Universe");
        model.ApplyEdits(new[] { edit });

        Assert.Equal("Hello Universe", model.GetValue());
        Assert.Equal(2, model.VersionId);
        Assert.True(eventFired);
    }

    [Fact]
    public void TestTextModel_MultipleEdits()
    {
        var model = new TextModel("Hello World");
        
        // Insert "Big " before "World" -> "Hello Big World"
        // Replace "Hello" with "Hi" -> "Hi Big World"
        
        // Edits:
        // 1. Insert at 1, 7: "Big "
        // 2. Replace 1, 1 to 1, 6: "Hi"
        
        var edit1 = new TextEdit(new TextPosition(1, 7), new TextPosition(1, 7), "Big ");
        var edit2 = new TextEdit(new TextPosition(1, 1), new TextPosition(1, 6), "Hi");
        
        model.ApplyEdits(new[] { edit1, edit2 });
        
        Assert.Equal("Hi Big World", model.GetValue());
    }

    [Fact]
    public void TestTextModel_Decorations()
    {
        var model = new TextModel("Hello World");
        // Decoration on "World" (offsets 6-11)
        var range = new TextRange(6, 11);
        var decoration = model.AddDecoration(range, ModelDecorationOptions.CreateSelectionOptions());
        
        Assert.Equal(6, decoration.Range.StartOffset);
        Assert.Equal(11, decoration.Range.EndOffset);
        
        // Insert "Beautiful " before "World" at offset 6 (1, 7)
        var edit = new TextEdit(new TextPosition(1, 7), new TextPosition(1, 7), "Beautiful ");
        model.ApplyEdits(new[] { edit });
        
        Assert.Equal("Hello Beautiful World", model.GetValue());
        
        // Decoration should shift
        Assert.Equal(6, decoration.Range.StartOffset);
        Assert.Equal(21, decoration.Range.EndOffset);
        
        var found = model.GetDecorationsInRange(new TextRange(6, 21));
        Assert.Single(found);
        Assert.Equal(decoration, found[0]);
    }

    [Fact]
    public void TextModel_RaisesDecorationEvents()
    {
        var model = new TextModel("Hello World");
        var decoration = model.AddDecoration(new TextRange(6, 11), ModelDecorationOptions.CreateSelectionOptions());
        TextModelDecorationsChangedEventArgs? observed = null;
        model.OnDidChangeDecorations += (_, args) => observed = args;

        model.ApplyEdits(new[]
        {
            new TextEdit(new TextPosition(1, 6), new TextPosition(1, 6), "Beautiful ")
        });

        Assert.NotNull(observed);
        Assert.Contains(observed!.Changes, c => c.Id == decoration.Id && c.Kind == DecorationDeltaKind.Updated);
    }

    [Fact]
    public void UndoRedo_Roundtrip()
    {
        var model = new TextModel("Hello");
        model.PushEditOperations(new[]
        {
            new TextEdit(new TextPosition(1, 6), new TextPosition(1, 6), " World")
        });

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
        var model = new TextModel("abc123");
        model.PushEditOperations(new[]
        {
            new TextEdit(new TextPosition(1, 4), new TextPosition(1, 7), "XYZ")
        });

        model.PushStackElement();

        model.PushEditOperations(new[]
        {
            new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "HELLO ")
        });

        Assert.Equal("HELLO abcXYZ", model.GetValue());

        Assert.True(model.Undo());
        Assert.Equal("abcXYZ", model.GetValue());

        Assert.True(model.Undo());
        Assert.Equal("abc123", model.GetValue());
    }

    [Fact]
    public void UpdateOptionsRaisesChangeEvent()
    {
        var model = new TextModel("line");
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
        var text = "def\n  foo()\n    bar()\n";
        var model = new TextModel(text);
        model.DetectIndentation(defaultInsertSpaces: false, defaultTabSize: 4);

        var options = model.GetOptions();
        Assert.True(options.InsertSpaces);
        Assert.Equal(2, options.TabSize);
        Assert.Equal(2, options.IndentSize);
    }

    [Fact]
    public void PushEolIsUndoable()
    {
        var model = new TextModel("A\nB\n");
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
        var model = new TextModel("text");
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
        var creation = new TextModelCreationOptions
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

        var model = new TextModel("line1\r\nline2", creation, "plaintext");
        var options = model.GetOptions();

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
        var creation = new TextModelCreationOptions
        {
            DetectIndentation = true,
            TabSize = 4,
            IndentSize = 4,
            InsertSpaces = true,
        };

        var model = new TextModel("\tline\n\tindent", creation, "plaintext");
        var options = model.GetOptions();

        Assert.False(options.InsertSpaces);
        Assert.Equal(4, options.TabSize);
    }

    [Fact]
    public void CursorStateComputerIsInvoked()
    {
        var model = new TextModel("abc");
        bool invoked = false;

        CursorStateComputer computer = inverseChanges =>
        {
            invoked = inverseChanges.Count > 0;
            return null;
        };

        model.PushEditOperations(
            new[] { new TextEdit(new TextPosition(1, 4), new TextPosition(1, 4), "!") },
            beforeCursorState: null,
            cursorStateComputer: computer,
            undoLabel: "exclaim");

        Assert.True(invoked);
    }

    [Fact]
    public void LanguageConfigurationEventsFire()
    {
        var service = new TestLanguageConfigurationService();
        var model = new TextModel("text", TextModelCreationOptions.Default, "langA", service);
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
        var model = new TextModel("text");
        var events = new System.Collections.Generic.List<bool>();
        model.OnDidChangeAttached += (_, args) => events.Add(args.IsAttached);

        model.AttachEditor();
        model.AttachEditor();
        model.DetachEditor();
        model.DetachEditor();

        Assert.Equal(new[] { true, false }, events);
    }

    private sealed class TestLanguageConfigurationService : ILanguageConfigurationService
    {
        private readonly List<(string LanguageId, EventHandler<LanguageConfigurationChangedEventArgs> Handler)> _handlers = new();

        public event EventHandler<LanguageConfigurationChangedEventArgs>? OnDidChange;

        public IDisposable Subscribe(string languageId, EventHandler<LanguageConfigurationChangedEventArgs> callback)
        {
            _handlers.Add((languageId, callback));
            return new DelegateDisposable(() => _handlers.RemoveAll(tuple => tuple.LanguageId == languageId && tuple.Handler == callback));
        }

        public void Raise(string languageId)
        {
            var args = new LanguageConfigurationChangedEventArgs(languageId);
            OnDidChange?.Invoke(this, args);
            foreach (var entry in _handlers.ToArray())
            {
                if (string.Equals(entry.LanguageId, languageId, StringComparison.Ordinal))
                {
                    entry.Handler(this, args);
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
