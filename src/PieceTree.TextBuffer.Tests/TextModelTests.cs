using System;
using Xunit;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using System.Linq;
using PieceTree.TextBuffer.Decorations;

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
            Assert.Equal(1, e.Changes.Count);
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
        // Decoration on "World" (6, 11)
        var range = new TextRange(6, 11);
        var decoration = model.AddDecoration(range, ModelDecorationOptions.Default);
        
        Assert.Equal(6, decoration.Range.StartOffset);
        Assert.Equal(11, decoration.Range.EndOffset);
        
        // Insert "Beautiful " before "World" at offset 6 (1, 7)
        var edit = new TextEdit(new TextPosition(1, 7), new TextPosition(1, 7), "Beautiful ");
        model.ApplyEdits(new[] { edit });
        
        Assert.Equal("Hello Beautiful World", model.GetValue());
        
        // Decoration should shift
        Assert.Equal(16, decoration.Range.StartOffset);
        Assert.Equal(21, decoration.Range.EndOffset);
        
        var found = model.GetDecorationsInRange(new TextRange(16, 21));
        Assert.Single(found);
        Assert.Equal(decoration, found.First());
    }
}
