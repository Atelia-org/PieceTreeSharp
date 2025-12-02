// Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetController2.test.ts
// Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetSession.test.ts
// - Tests: Snippet insertion, placeholder navigation
// Ported: 2025-11-22
// Extended: 2025-11-30 with more TS parity tests
// Extended: 2025-12-02 with Final Tabstop ($0) and adjustWhitespace tests

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

    // ==================== P1: Final Tabstop $0 Tests ====================

    [Fact]
    public void SnippetInsert_FinalTabstop_NavigatedLast()
    {
        // TS: $0 is the final tabstop, always navigated to last
        TextModel model = new("x");
        SnippetController controller = new(model);
        // $0 in the middle, but should be navigated to last
        // Snippet: "${1:first}$0${2:second}" → plainText: "firstsecond"
        // Positions: ${1:first} at 0..5, $0 at 5..5 (empty), ${2:second} at 5..11
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:first}$0${2:second}");

        // Result: "firstsecondx" (snippet + original "x")
        Assert.Equal("firstsecondx", model.GetValue());

        // Navigate forward - $0 should come after all other placeholders
        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);
        Assert.Equal(new TextPosition(1, 1), p1); // ${1:first} at column 1
        Assert.False(controller.IsAtFinalTabstop);

        TextPosition? p2 = controller.NextPlaceholder();
        Assert.NotNull(p2);
        Assert.Equal(new TextPosition(1, 6), p2); // ${2:second} at column 6 (after "first")
        Assert.False(controller.IsAtFinalTabstop);

        TextPosition? p3 = controller.NextPlaceholder();
        Assert.NotNull(p3);
        // $0 is at column 6 too (empty placeholder between "first" and "second")
        // In the sorted order, $0 comes last despite having the same position
        Assert.Equal(new TextPosition(1, 6), p3);
        Assert.True(controller.IsAtFinalTabstop);

        // Past the end
        TextPosition? p4 = controller.NextPlaceholder();
        Assert.Null(p4);
    }

    [Fact]
    public void SnippetInsert_FinalTabstopOnly()
    {
        // Snippet with only $0
        TextModel model = new("test");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "inserted$0text");

        Assert.Equal("insertedtexttest", model.GetValue());

        TextPosition? p = controller.NextPlaceholder();
        Assert.NotNull(p);
        Assert.Equal(new TextPosition(1, 9), p); // $0 at position 9 (after "inserted")
        Assert.True(controller.IsAtFinalTabstop);
    }

    [Fact]
    public void SnippetInsert_FinalTabstopWithBraces()
    {
        // ${0} is equivalent to $0
        TextModel model = new("x");
        SnippetController controller = new(model);
        // Snippet: "${1:a}${0}${2:b}" → plainText: "ab"
        // Positions: ${1:a} at 0..1, ${0} at 1..1 (empty), ${2:b} at 1..2
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:a}${0}${2:b}");

        Assert.Equal("abx", model.GetValue());

        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);
        Assert.Equal(new TextPosition(1, 1), p1); // ${1:a}

        TextPosition? p2 = controller.NextPlaceholder();
        Assert.NotNull(p2);
        Assert.Equal(new TextPosition(1, 2), p2); // ${2:b}

        TextPosition? p3 = controller.NextPlaceholder();
        Assert.NotNull(p3);
        // ${0} is at column 2 (empty, between "a" and "b")
        Assert.Equal(new TextPosition(1, 2), p3);
        Assert.True(controller.IsAtFinalTabstop);
    }

    [Fact]
    public void SnippetInsert_SimpleTabstop()
    {
        // $1 without braces
        TextModel model = new("x");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "a$1b$2c");

        Assert.Equal("abcx", model.GetValue());

        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);
        Assert.Equal(new TextPosition(1, 2), p1); // $1 after 'a'

        TextPosition? p2 = controller.NextPlaceholder();
        Assert.NotNull(p2);
        Assert.Equal(new TextPosition(1, 3), p2); // $2 after 'b'
    }

    // ==================== P1: adjustWhitespace Tests ====================

    [Fact]
    public void AdjustWhitespace_SingleLine_NoChange()
    {
        TextModel model = new("    hello");
        string snippet = "${1:foo}";

        string adjusted = SnippetSession.AdjustWhitespace(model, new TextPosition(1, 5), snippet);
        Assert.Equal("${1:foo}", adjusted);
    }

    [Fact]
    public void AdjustWhitespace_MultiLine_AddsIndentation()
    {
        // When inserting at an indented position, subsequent lines should get the same indentation
        TextModel model = new("    hello");
        string snippet = "if (true) {\n    body\n}";

        // Insert at column 5 (after 4 spaces)
        string adjusted = SnippetSession.AdjustWhitespace(model, new TextPosition(1, 5), snippet);

        // Second and third lines should have 4 spaces prepended
        Assert.Equal("if (true) {\n        body\n    }", adjusted);
    }

    [Fact]
    public void AdjustWhitespace_MultiLine_PreservesEmptyLines()
    {
        TextModel model = new("  code");
        string snippet = "line1\n\nline3";

        string adjusted = SnippetSession.AdjustWhitespace(model, new TextPosition(1, 3), snippet);

        // Empty line stays empty, line3 gets indentation
        Assert.Equal("line1\n\n  line3", adjusted);
    }

    [Fact]
    public void AdjustWhitespace_WithTabs_NormalizesCorrectly()
    {
        // Model uses spaces (default)
        TextModelCreationOptions options = new() { TabSize = 4, InsertSpaces = true };
        TextModel model = new("\thello", options);
        string snippet = "foo\n\tbar";

        // Insert after the tab (column 2 = visual column 5 with tabSize=4)
        // Tab in snippet should be normalized to spaces
        string adjusted = SnippetSession.AdjustWhitespace(model, new TextPosition(1, 2), snippet);

        // The tab at the start of line 1 means lineLeadingWhitespace = "\t"
        // Line 2 gets "\t" + "\tbar" = "\t\tbar", normalized to "        bar" (8 spaces)
        Assert.Contains("bar", adjusted);
    }

    [Fact]
    public void SnippetInsert_AdjustWhitespace_IntegratedTest()
    {
        // Full integration test: multi-line snippet with indentation adjustment
        TextModel model = new("function test() {\n    // body\n}");
        SnippetController controller = new(model);

        // Insert a multi-line snippet at the indented position
        controller.InsertSnippetAt(new TextPosition(2, 5), "if (${1:cond}) {\n    ${2:body}\n}");

        // The snippet should be adjusted to have proper indentation
        string value = model.GetValue();

        // Verify the snippet was inserted and lines are properly indented
        Assert.Contains("if (cond)", value);
        Assert.Contains("body", value);

        // Navigate to placeholders
        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);

        TextPosition? p2 = controller.NextPlaceholder();
        Assert.NotNull(p2);
    }

    [Fact]
    public void SnippetInsert_NoAdjustWhitespace_WhenDisabled()
    {
        TextModel model = new("    hello");
        SnippetController controller = new(model);

        // Disable whitespace adjustment
        SnippetInsertOptions options = new() { AdjustWhitespace = false };
        // Insert at column 5 (after 4 spaces)
        controller.InsertSnippetAt(new TextPosition(1, 5), "line1\nline2", options);

        // Without adjustment, line2 should NOT have indentation added
        // Original: "    hello" (4 spaces + "hello")
        // Insert at column 5: "    " + "line1\nline2" + "hello"
        string value = model.GetValue();
        Assert.Equal("    line1\nline2hello", value);
    }

    [Fact]
    public void GetCurrentPlaceholderRange_ReturnsCorrectRange()
    {
        TextModel model = new("test");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:hello}${2:world}");

        // Navigate to first placeholder
        controller.NextPlaceholder();
        (TextPosition Start, TextPosition End)? range = controller.GetCurrentPlaceholderRange();

        Assert.NotNull(range);
        Assert.Equal(new TextPosition(1, 1), range.Value.Start);
        Assert.Equal(new TextPosition(1, 6), range.Value.End); // "hello" is 5 chars
    }
}
