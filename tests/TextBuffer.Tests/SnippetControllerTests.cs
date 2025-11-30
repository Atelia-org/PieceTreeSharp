// Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetController2.test.ts
// Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetSession.test.ts
// - Tests: Snippet insertion, placeholder navigation
// Ported: 2025-11-22
// Extended: 2025-11-30 with more TS parity tests

using PieceTree.TextBuffer.Cursor;

namespace PieceTree.TextBuffer.Tests;

public class SnippetControllerTests
{
    [Fact]
    public void SnippetInsert_CreatesPlaceholders_AndNavigates()
    {
        TextModel model = new("1234567890");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 2), "${1:foo}${2:bar}");

        TextPosition? next = controller.NextPlaceholder();
        Assert.NotNull(next);
        Assert.Equal(new TextPosition(1, 2), next);

        TextPosition? next2 = controller.NextPlaceholder();
        Assert.NotNull(next2);
        // First placeholder was 'foo' inserted at 2, second will follow after 3 chars
        Assert.Equal(new TextPosition(1, 5), next2);
    }

    [Fact]
    public void SnippetInsert_JustText_NoPlaceholders()
    {
        // TS: test('snippets, just text', ...)
        TextModel model = new("function foo() {}");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "foobar");

        // No placeholders, so NextPlaceholder should return null
        TextPosition? next = controller.NextPlaceholder();
        Assert.Null(next);

        Assert.Equal("foobarfunction foo() {}", model.GetValue());
    }

    [Fact]
    public void SnippetInsert_SinglePlaceholder_NavigatesToIt()
    {
        // TS: test('text edits & selection', ...)
        TextModel model = new("hello world");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:bar}");

        TextPosition? next = controller.NextPlaceholder();
        Assert.NotNull(next);
        Assert.Equal(new TextPosition(1, 1), next);

        // Second call should go past the end
        TextPosition? next2 = controller.NextPlaceholder();
        Assert.Null(next2);

        Assert.Equal("barhello world", model.GetValue());
    }

    [Fact]
    public void SnippetInsert_MultiplePlaceholders_NavigatesInOrder()
    {
        // TS: test('snippets, selections -> next/prev', ...)
        TextModel model = new("base");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:a}${2:b}${3:c}");

        // Navigate forward through all placeholders
        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);
        Assert.Equal(new TextPosition(1, 1), p1); // 'a' at position 1

        TextPosition? p2 = controller.NextPlaceholder();
        Assert.NotNull(p2);
        Assert.Equal(new TextPosition(1, 2), p2); // 'b' at position 2

        TextPosition? p3 = controller.NextPlaceholder();
        Assert.NotNull(p3);
        Assert.Equal(new TextPosition(1, 3), p3); // 'c' at position 3

        // Past the end
        TextPosition? p4 = controller.NextPlaceholder();
        Assert.Null(p4);

        Assert.Equal("abcbase", model.GetValue());
    }

    [Fact]
    public void SnippetInsert_PrevPlaceholder_NavigatesBackward()
    {
        TextModel model = new("x");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:aa}${2:bb}${3:cc}");

        // Go to the end
        controller.NextPlaceholder(); // -> aa
        controller.NextPlaceholder(); // -> bb
        controller.NextPlaceholder(); // -> cc
        controller.NextPlaceholder(); // -> past end

        // Now go back
        TextPosition? prev1 = controller.PrevPlaceholder();
        Assert.NotNull(prev1);
        Assert.Equal(new TextPosition(1, 5), prev1); // 'cc' at position 5

        TextPosition? prev2 = controller.PrevPlaceholder();
        Assert.NotNull(prev2);
        Assert.Equal(new TextPosition(1, 3), prev2); // 'bb' at position 3

        TextPosition? prev3 = controller.PrevPlaceholder();
        Assert.NotNull(prev3);
        Assert.Equal(new TextPosition(1, 1), prev3); // 'aa' at position 1

        // Past the beginning
        TextPosition? prev4 = controller.PrevPlaceholder();
        Assert.Null(prev4);
    }

    [Fact]
    public void SnippetSession_PlaceholdersTrackEdits()
    {
        // After editing the model, placeholder positions should update
        TextModel model = new("start");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 6), "${1:placeholder}");
        // Model is now "start placeholder"

        // First placeholder at column 6
        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);
        Assert.Equal(new TextPosition(1, 6), p1);

        // Insert text before the placeholder
        model.PushEditOperations([new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "PREFIX_")]);
        // Model is now "PREFIX_start placeholder"

        // Go back and check if placeholder position updated
        controller.PrevPlaceholder(); // reset
        TextPosition? p1Updated = controller.NextPlaceholder();
        Assert.NotNull(p1Updated);
        // Placeholder should have moved by 7 chars (length of "PREFIX_")
        Assert.Equal(new TextPosition(1, 13), p1Updated);
    }

    [Fact]
    public void SnippetInsert_EmptyPlaceholder()
    {
        TextModel model = new("test");
        SnippetController controller = new(model);
        // ${1:} is an empty placeholder
        controller.InsertSnippetAt(new TextPosition(1, 1), "before${1:}after");

        Assert.Equal("beforeaftertest", model.GetValue());

        TextPosition? p = controller.NextPlaceholder();
        Assert.NotNull(p);
        // Empty placeholder at position 7 (after "before")
        Assert.Equal(new TextPosition(1, 7), p);
    }

    [Fact]
    public void SnippetController_CreateSession_DisposesOldSession()
    {
        TextModel model = new("abc");
        SnippetController controller = new(model);

        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:first}");
        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);

        // Create a new session - old one should be disposed
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:second}");
        TextPosition? p2 = controller.NextPlaceholder();
        Assert.NotNull(p2);
        // Should be at the new session's placeholder
        Assert.Equal(new TextPosition(1, 1), p2);
    }

    [Fact]
    public void SnippetInsert_WithNewlines_MultiLine()
    {
        TextModel model = new("line1\nline2");
        SnippetController controller = new(model);
        // Snippet "${1:a}\n${2:b}" becomes "a\nb" (plain text with one newline)
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:a}\n${2:b}");

        // Result is "a\nb" + "line1\nline2" = "a\nbline1\nline2"
        // The snippet text "a\nb" is inserted at the beginning
        Assert.Equal("a\nbline1\nline2", model.GetValue());

        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);
        Assert.Equal(new TextPosition(1, 1), p1); // 'a' on line 1

        TextPosition? p2 = controller.NextPlaceholder();
        Assert.NotNull(p2);
        Assert.Equal(new TextPosition(2, 1), p2); // 'b' on line 2
    }

    [Fact]
    public void SnippetInsert_WithNewlines_ProperlyInsertsLines()
    {
        // More explicit test: snippet with trailing newline
        TextModel model = new("existing");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:new}\n");

        // Result: "new\nexisting"
        Assert.Equal("new\nexisting", model.GetValue());

        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);
        Assert.Equal(new TextPosition(1, 1), p1);
    }
}
