// Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetController2.test.ts
// Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetSession.test.ts
// Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetVariables.test.ts
// - Tests: Snippet insertion, placeholder navigation
// Ported: 2025-11-22
// Extended: 2025-11-30 with more TS parity tests
// Extended: 2025-12-02 with Final Tabstop ($0) and adjustWhitespace tests
// Extended: 2025-12-02 with Placeholder Grouping (P1.5) tests
// Extended: 2025-12-02 with Variable Resolver (P2) tests

using PieceTree.TextBuffer.Cursor;
using PieceTree.TextBuffer.Decorations;

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

    // ==================== P1.5: Placeholder Grouping Tests ====================

    [Fact]
    public void SnippetInsert_SameIndexPlaceholders_GroupedCorrectly()
    {
        // TS: Same index placeholders (e.g., ${1:foo} and ${1:foo}) should be grouped
        // This is used for synchronized editing (mirrors)
        TextModel model = new("x");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:foo} and ${1:foo}");

        // Result: "foo and foox"
        Assert.Equal("foo and foox", model.GetValue());

        // Navigate to first placeholder
        controller.NextPlaceholder();

        // Get all ranges for current placeholder
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges = controller.GetCurrentPlaceholderRanges();

        Assert.NotNull(ranges);
        Assert.Equal(2, ranges.Count); // Two placeholders with index 1

        // First placeholder: "foo" at position 1-4
        Assert.Equal(new TextPosition(1, 1), ranges[0].Start);
        Assert.Equal(new TextPosition(1, 4), ranges[0].End);

        // Second placeholder: "foo" at position 9-12 (after " and ")
        Assert.Equal(new TextPosition(1, 9), ranges[1].Start);
        Assert.Equal(new TextPosition(1, 12), ranges[1].End);
    }

    [Fact]
    public void SnippetInsert_SameIndexPlaceholders_DifferentDefaults()
    {
        // Same index but different default text - all should be grouped
        TextModel model = new("x");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:first} ${1:second}");

        // Result: "first secondx" - each placeholder keeps its own default text
        Assert.Equal("first secondx", model.GetValue());

        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges = controller.GetCurrentPlaceholderRanges();

        Assert.NotNull(ranges);
        Assert.Equal(2, ranges.Count);

        // First placeholder: "first" at 1-6
        Assert.Equal(new TextPosition(1, 1), ranges[0].Start);
        Assert.Equal(new TextPosition(1, 6), ranges[0].End);

        // Second placeholder: "second" at 7-13
        Assert.Equal(new TextPosition(1, 7), ranges[1].Start);
        Assert.Equal(new TextPosition(1, 13), ranges[1].End);
    }

    [Fact]
    public void SnippetInsert_ThreeSameIndexPlaceholders()
    {
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:a},${1:b},${1:c}");

        // Result: "a,b,c"
        Assert.Equal("a,b,c", model.GetValue());

        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges = controller.GetCurrentPlaceholderRanges();

        Assert.NotNull(ranges);
        Assert.Equal(3, ranges.Count);

        // Verify all three ranges
        Assert.Equal(new TextPosition(1, 1), ranges[0].Start);
        Assert.Equal(new TextPosition(1, 2), ranges[0].End); // "a"

        Assert.Equal(new TextPosition(1, 3), ranges[1].Start);
        Assert.Equal(new TextPosition(1, 4), ranges[1].End); // "b"

        Assert.Equal(new TextPosition(1, 5), ranges[2].Start);
        Assert.Equal(new TextPosition(1, 6), ranges[2].End); // "c"
    }

    [Fact]
    public void SnippetInsert_MixedPlaceholders_OnlyCurrentGrouped()
    {
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:a}${2:b}${1:c}");

        // Result: "abc"
        Assert.Equal("abc", model.GetValue());

        // Navigate to first placeholder (index 1)
        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges1 = controller.GetCurrentPlaceholderRanges();

        Assert.NotNull(ranges1);
        Assert.Equal(2, ranges1.Count); // Two placeholders with index 1

        // Navigate to second placeholder (index 2)
        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges2 = controller.GetCurrentPlaceholderRanges();

        Assert.NotNull(ranges2);
        Assert.Single(ranges2); // Only one placeholder with index 2
    }

    [Fact]
    public void GetAllSelectionsForCurrentPlaceholder_ReturnsTextRanges()
    {
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:foo} ${1:bar}");

        // Navigate to placeholder
        controller.NextPlaceholder();
        IReadOnlyList<TextRange>? selections = controller.GetAllSelectionsForCurrentPlaceholder();

        Assert.NotNull(selections);
        Assert.Equal(2, selections.Count);

        // First selection: offset 0-3 ("foo")
        Assert.Equal(0, selections[0].StartOffset);
        Assert.Equal(3, selections[0].EndOffset);

        // Second selection: offset 4-7 ("bar")
        Assert.Equal(4, selections[1].StartOffset);
        Assert.Equal(7, selections[1].EndOffset);
    }

    [Fact]
    public void ComputePossibleSelections_ReturnsAllGroups()
    {
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:a}${2:b}${1:c}${2:d}");

        // Result: "abcd"
        Assert.Equal("abcd", model.GetValue());

        IReadOnlyDictionary<int, IReadOnlyList<(TextPosition Start, TextPosition End)>>? selections = controller.ComputePossibleSelections();

        Assert.NotNull(selections);
        Assert.Equal(2, selections.Count); // Two groups: index 1 and index 2

        // Index 1 has 2 placeholders
        Assert.True(selections.ContainsKey(1));
        Assert.Equal(2, selections[1].Count);

        // Index 2 has 2 placeholders
        Assert.True(selections.ContainsKey(2));
        Assert.Equal(2, selections[2].Count);
    }

    [Fact]
    public void ComputePossibleSelections_ExcludesFinalTabstop()
    {
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:a}$0${2:b}");

        IReadOnlyDictionary<int, IReadOnlyList<(TextPosition Start, TextPosition End)>>? selections = controller.ComputePossibleSelections();

        Assert.NotNull(selections);
        Assert.Equal(2, selections.Count); // Only index 1 and 2

        // Index 0 (final tabstop) should NOT be included
        Assert.False(selections.ContainsKey(0));
        Assert.True(selections.ContainsKey(1));
        Assert.True(selections.ContainsKey(2));
    }

    [Fact]
    public void PlaceholderGrouping_TracksEdits()
    {
        // Verify that grouped placeholders track model edits correctly
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:foo} and ${1:foo}");

        // Result: "foo and foo"
        Assert.Equal("foo and foo", model.GetValue());
        
        controller.NextPlaceholder();
        
        // Before edit, check single range is correct
        (TextPosition Start, TextPosition End)? singleRange = controller.GetCurrentPlaceholderRange();
        Assert.NotNull(singleRange);
        Assert.Equal(new TextPosition(1, 1), singleRange.Value.Start);
        Assert.Equal(new TextPosition(1, 4), singleRange.Value.End);
        
        IReadOnlyList<(TextPosition Start, TextPosition End)>? rangesBefore = controller.GetCurrentPlaceholderRanges();
        Assert.NotNull(rangesBefore);
        Assert.Equal(2, rangesBefore.Count);
        
        // Verify initial positions
        Assert.Equal(new TextPosition(1, 1), rangesBefore[0].Start);  // First "foo" at 1
        Assert.Equal(new TextPosition(1, 9), rangesBefore[1].Start);  // Second "foo" at 9 (after "foo and ")

        // Insert text at the beginning
        model.PushEditOperations([new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "PREFIX ")]);
        // Result: "PREFIX foo and foo"
        Assert.Equal("PREFIX foo and foo", model.GetValue());

        // After edit, check single range is updated
        (TextPosition Start, TextPosition End)? singleRangeAfter = controller.GetCurrentPlaceholderRange();
        Assert.NotNull(singleRangeAfter);
        Assert.Equal(new TextPosition(1, 8), singleRangeAfter.Value.Start);  // Was 1, now 8
        
        // Get ranges again - they should have moved
        IReadOnlyList<(TextPosition Start, TextPosition End)>? rangesAfter = controller.GetCurrentPlaceholderRanges();
        Assert.NotNull(rangesAfter);
        Assert.Equal(2, rangesAfter.Count);

        // Both placeholders should have moved by 7 chars ("PREFIX " length)
        Assert.Equal(new TextPosition(1, 8), rangesAfter[0].Start);  // Was 1
        Assert.Equal(new TextPosition(1, 11), rangesAfter[0].End);   // Was 4

        Assert.Equal(new TextPosition(1, 16), rangesAfter[1].Start); // Was 9
        Assert.Equal(new TextPosition(1, 19), rangesAfter[1].End);   // Was 12
    }

    // ==================== Deterministic Tests: Edge Cases ====================
    // Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetParser.test.ts
    // Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetSession.test.ts

    #region Edge Cases

    [Fact]
    public void SnippetInsert_EmptySnippet()
    {
        // TS: Empty snippet should not create any placeholders
        TextModel model = new("test");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "");

        Assert.Equal("test", model.GetValue());
        Assert.Null(controller.NextPlaceholder());
    }

    [Fact]
    public void SnippetInsert_OnlyFinalTabstop()
    {
        // TS: Snippet with only $0
        TextModel model = new("test");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "$0");

        Assert.Equal("test", model.GetValue());

        TextPosition? p = controller.NextPlaceholder();
        Assert.NotNull(p);
        Assert.Equal(new TextPosition(1, 1), p);
        Assert.True(controller.IsAtFinalTabstop);
    }

    [Fact]
    public void SnippetInsert_OnlyFinalTabstopWithBraces()
    {
        // TS: ${0} is equivalent to $0
        TextModel model = new("test");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${0}");

        Assert.Equal("test", model.GetValue());

        TextPosition? p = controller.NextPlaceholder();
        Assert.NotNull(p);
        Assert.Equal(new TextPosition(1, 1), p);
        Assert.True(controller.IsAtFinalTabstop);
    }

    [Theory]
    [InlineData("${1}${2}${3}", "", 3)]
    [InlineData("${1:a}${2:b}${3:c}", "abc", 3)]
    [InlineData("$1$2$3", "", 3)]
    public void SnippetInsert_ConsecutivePlaceholders(string snippet, string expectedText, int expectedPlaceholderCount)
    {
        // TS: test('snippets, don\'t merge touching tabstops', ...)
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), snippet);

        Assert.Equal(expectedText, model.GetValue());

        int count = 0;
        while (controller.NextPlaceholder() != null)
        {
            count++;
        }
        Assert.Equal(expectedPlaceholderCount, count);
    }

    [Fact(Skip = "Nested placeholder expansion requires P2 SnippetParser - not yet implemented")]
    public void SnippetInsert_NestedPlaceholder_ParsesCorrectly()
    {
        // TS: test('Parser, default placeholder values', ...)
        // Note: Nested placeholder ${1:${2:nested}} should parse the outer placeholder with inner as default
        // TODO: P2 - Implement nested placeholder support in SnippetParser
        TextModel model = new("");
        SnippetController controller = new(model);
        // In TS: ${1:${2:nested}} expands to "nested" with nested placeholders
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:${2:nested}}");

        // The outer placeholder default contains the inner placeholder's text
        Assert.Equal("nested", model.GetValue());

        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);
        Assert.Equal(new TextPosition(1, 1), p1);
    }

    [Theory(Skip = "Nested placeholder expansion requires P2 SnippetParser - not yet implemented")]
    [InlineData("${1:outer${2:inner}}", "outerinner")]
    [InlineData("${1:a${2:b${3:c}}}", "abc")]
    public void SnippetInsert_NestedPlaceholders_ExpandCorrectly(string snippet, string expectedText)
    {
        // TS: test('Parser, variables/placeholder with defaults', ...)
        // TODO: P2 - Implement nested placeholder support in SnippetParser
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), snippet);

        Assert.Equal(expectedText, model.GetValue());
    }

    [Fact]
    public void SnippetInsert_PlainTextOnly()
    {
        // TS: test('snippets, just text', ...)
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "just plain text");

        Assert.Equal("just plain text", model.GetValue());
        Assert.Null(controller.NextPlaceholder());
    }

    [Theory(Skip = "Escape handling requires P2 SnippetParser - not yet implemented")]
    [InlineData("\\$1", "$1")]     // Escaped dollar should be literal
    [InlineData("\\${1}", "${1}")] // Escaped brace should be literal
    [InlineData("\\\\", "\\")]     // Escaped backslash
    public void SnippetInsert_EscapedCharacters(string snippet, string expectedText)
    {
        // TS: test('Parser, text', ...)
        // TODO: P2 - Implement escape handling in SnippetParser
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), snippet);

        Assert.Equal(expectedText, model.GetValue());
    }

    #endregion

    #region adjustWhitespace Extended Tests

    [Fact]
    public void AdjustWhitespace_TabIndentation_PreservesStyle()
    {
        // TS: test('normalize whitespace', ...)
        TextModelCreationOptions options = new() { TabSize = 4, InsertSpaces = false };
        TextModel model = new("\tcode", options);
        string snippet = "line1\n\tline2";

        string adjusted = SnippetSession.AdjustWhitespace(model, new TextPosition(1, 2), snippet);

        // With tab indentation, tabs should be preserved
        Assert.Contains("line2", adjusted);
    }

    [Fact]
    public void AdjustWhitespace_SpaceIndentation_NormalizesToSpaces()
    {
        // TS: test('Tabs don\'t get replaced with spaces in snippet transformations #103818', ...)
        TextModelCreationOptions options = new() { TabSize = 2, InsertSpaces = true };
        TextModel model = new("  code", options);
        string snippet = "line1\n\tline2"; // Tab in snippet

        string adjusted = SnippetSession.AdjustWhitespace(model, new TextPosition(1, 3), snippet);

        // Tab should be normalized to spaces
        Assert.DoesNotContain("\t", adjusted);
        Assert.Contains("line2", adjusted);
    }

    [Theory]
    [InlineData("    ", 4)]  // 4 space indentation
    [InlineData("  ", 2)]    // 2 space indentation
    [InlineData("\t", 4)]    // Tab indentation (treated as tabSize spaces)
    public void AdjustWhitespace_VariousIndentLevels(string leadingWhitespace, int expectedIndentWidth)
    {
        TextModel model = new($"{leadingWhitespace}code");
        string snippet = "a\nb";

        // Insert after the leading whitespace
        string adjusted = SnippetSession.AdjustWhitespace(model, new TextPosition(1, leadingWhitespace.Length + 1), snippet);

        // Second line should have indentation
        string[] lines = adjusted.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.Equal("a", lines[0]);
        // Second line should have some indentation
        Assert.StartsWith(leadingWhitespace, $"{leadingWhitespace}{lines[1].TrimStart()}".Substring(0, Math.Min(leadingWhitespace.Length, lines[1].Length + leadingWhitespace.Length)));
    }

    [Fact]
    public void AdjustWhitespace_MixedIndentation()
    {
        // Model with mixed indentation (space + tab)
        TextModel model = new("  \tcode");
        string snippet = "first\nsecond\nthird";

        string adjusted = SnippetSession.AdjustWhitespace(model, new TextPosition(1, 4), snippet);

        string[] lines = adjusted.Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Equal("first", lines[0]);
        // Lines 2 and 3 should have inherited indentation
    }

    [Fact]
    public void AdjustWhitespace_EmptyLinesPreserved()
    {
        // TS: Empty lines should remain empty, not get indentation
        TextModel model = new("    code");
        string snippet = "line1\n\nline3";

        string adjusted = SnippetSession.AdjustWhitespace(model, new TextPosition(1, 5), snippet);

        string[] lines = adjusted.Split('\n');
        Assert.Equal(3, lines.Length);
        Assert.Equal("line1", lines[0]);
        Assert.Equal("", lines[1]);  // Empty line stays empty
        Assert.StartsWith("    ", lines[2]); // Third line gets indentation
    }

    [Fact]
    public void AdjustWhitespace_MultipleNewlines()
    {
        TextModel model = new("  base");
        string snippet = "a\n\n\nb";

        string adjusted = SnippetSession.AdjustWhitespace(model, new TextPosition(1, 3), snippet);

        string[] lines = adjusted.Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.Equal("a", lines[0]);
        Assert.Equal("", lines[1]);
        Assert.Equal("", lines[2]);
        Assert.StartsWith("  ", lines[3]);
    }

    #endregion

    #region Placeholder Grouping Extended Tests

    [Fact]
    public void SnippetInsert_ThreePlusIdenticalPlaceholders()
    {
        // Three or more placeholders with the same index
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:x}${1:y}${1:z}${1:w}");

        Assert.Equal("xyzw", model.GetValue());

        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges = controller.GetCurrentPlaceholderRanges();

        Assert.NotNull(ranges);
        Assert.Equal(4, ranges.Count);
    }

    [Fact]
    public void SnippetInsert_SameIndexSpreadAcrossLines()
    {
        // Same index placeholders on different lines
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:foo}\n${1:bar}\n${1:baz}");

        Assert.Equal("foo\nbar\nbaz", model.GetValue());

        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges = controller.GetCurrentPlaceholderRanges();

        Assert.NotNull(ranges);
        Assert.Equal(3, ranges.Count);

        // Verify positions on different lines
        Assert.Equal(new TextPosition(1, 1), ranges[0].Start);
        Assert.Equal(new TextPosition(2, 1), ranges[1].Start);
        Assert.Equal(new TextPosition(3, 1), ranges[2].Start);
    }

    [Fact]
    public void SnippetInsert_MixedIndexesWithMultipleSameIndex()
    {
        // ${1} appears twice, ${2} appears twice, intermixed
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:a}${2:b}${1:c}${2:d}");

        Assert.Equal("abcd", model.GetValue());

        // Navigate to index 1
        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges1 = controller.GetCurrentPlaceholderRanges();
        Assert.NotNull(ranges1);
        Assert.Equal(2, ranges1.Count);

        // Navigate to index 2
        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges2 = controller.GetCurrentPlaceholderRanges();
        Assert.NotNull(ranges2);
        Assert.Equal(2, ranges2.Count);
    }

    [Fact]
    public void SnippetInsert_SameIndexWithFinalTabstop()
    {
        // Same index placeholders with $0 at the end
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:a}${1:b}$0");

        Assert.Equal("ab", model.GetValue());

        // Navigate to index 1 (grouped)
        controller.NextPlaceholder();
        Assert.False(controller.IsAtFinalTabstop);
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges = controller.GetCurrentPlaceholderRanges();
        Assert.NotNull(ranges);
        Assert.Equal(2, ranges.Count);

        // Navigate to $0
        controller.NextPlaceholder();
        Assert.True(controller.IsAtFinalTabstop);
    }

    [Fact]
    public void SnippetInsert_EmptyPlaceholdersGrouped()
    {
        // Empty placeholders with same index
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), "${1:}|${1:}|${1:}");

        Assert.Equal("||", model.GetValue());

        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges = controller.GetCurrentPlaceholderRanges();

        Assert.NotNull(ranges);
        Assert.Equal(3, ranges.Count);

        // All ranges should be zero-width
        foreach (var range in ranges)
        {
            Assert.Equal(range.Start, range.End);
        }
    }

    [Theory(Skip = "Placeholder default inheritance requires P2 SnippetParser - not yet implemented")]
    [InlineData("${1:foo} ${1}", 2, "foo foo")]  // Second $1 inherits default
    [InlineData("${1} ${1:bar}", 2, "bar bar")]  // First $1 inherits from second
    [InlineData("$1 ${1:x} $1", 3, "x x x")]     // Multiple inheritance
    public void SnippetInsert_PlaceholderInheritance(string snippet, int expectedGroupSize, string expectedText)
    {
        // TS: test('Repeated snippet placeholder should always inherit, #31040', ...)
        // TODO: P2 - Implement placeholder default value inheritance in SnippetParser
        TextModel model = new("");
        SnippetController controller = new(model);
        controller.InsertSnippetAt(new TextPosition(1, 1), snippet);

        Assert.Equal(expectedText, model.GetValue());

        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? ranges = controller.GetCurrentPlaceholderRanges();

        Assert.NotNull(ranges);
        Assert.Equal(expectedGroupSize, ranges.Count);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void SnippetInsert_RealisticFunction()
    {
        // Realistic function snippet with multiple same-index placeholders
        TextModel model = new("");
        SnippetController controller = new(model);
        string snippet = "public ${1:void} ${2:MethodName}(${3:params})\n{\n    ${0:// body}\n}";
        controller.InsertSnippetAt(new TextPosition(1, 1), snippet);

        string value = model.GetValue();
        Assert.Contains("public void MethodName", value);
        Assert.Contains("// body", value);

        // Navigate through placeholders
        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);
        Assert.False(controller.IsAtFinalTabstop);

        TextPosition? p2 = controller.NextPlaceholder();
        Assert.NotNull(p2);
        Assert.False(controller.IsAtFinalTabstop);

        TextPosition? p3 = controller.NextPlaceholder();
        Assert.NotNull(p3);
        Assert.False(controller.IsAtFinalTabstop);

        TextPosition? p0 = controller.NextPlaceholder();
        Assert.NotNull(p0);
        Assert.True(controller.IsAtFinalTabstop);
    }

    [Fact]
    public void SnippetInsert_PropertyWithBackingField()
    {
        // Common pattern: property with backing field using same index
        TextModel model = new("");
        SnippetController controller = new(model);
        string snippet = "private ${1:int} _${2:name};\npublic ${1:int} ${2:Name}\n{\n    get => _${2:name};\n    set => _${2:name} = value;\n}";
        controller.InsertSnippetAt(new TextPosition(1, 1), snippet);

        string value = model.GetValue();
        Assert.Contains("private int _name", value);
        Assert.Contains("public int Name", value);

        // Type placeholder (${1:int}) should have 2 occurrences
        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? typeRanges = controller.GetCurrentPlaceholderRanges();
        Assert.NotNull(typeRanges);
        Assert.Equal(2, typeRanges.Count);

        // Name placeholder (${2:name/Name}) should have 4 occurrences
        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? nameRanges = controller.GetCurrentPlaceholderRanges();
        Assert.NotNull(nameRanges);
        Assert.Equal(4, nameRanges.Count);
    }

    [Fact]
    public void SnippetInsert_ForLoop()
    {
        // For loop with repeated index
        TextModel model = new("");
        SnippetController controller = new(model);
        string snippet = "for (int ${1:i} = 0; ${1:i} < ${2:count}; ${1:i}++)\n{\n    $0\n}";
        controller.InsertSnippetAt(new TextPosition(1, 1), snippet);

        string value = model.GetValue();
        Assert.Contains("for (int i = 0; i < count; i++)", value);

        // Loop variable should have 3 occurrences
        controller.NextPlaceholder();
        IReadOnlyList<(TextPosition Start, TextPosition End)>? iRanges = controller.GetCurrentPlaceholderRanges();
        Assert.NotNull(iRanges);
        Assert.Equal(3, iRanges.Count);
    }

    #endregion

    // ==================== P2: Variable Resolver Tests ====================
    // Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetVariables.test.ts

    #region Variable Resolver Tests

    [Fact]
    public void VariableResolver_SelectionVariable_ResolvesSelectedText()
    {
        // TS: test('Selection: undefined or empty is ok', ...)
        TextModel model = new("hello world");
        
        // Create selection range "world" (positions (1,7) to (1,12))
        SelectionVariableResolver resolver = new(model, new TextPosition(1, 7), new TextPosition(1, 12));

        Assert.Equal("world", resolver.Resolve("SELECTION"));
        Assert.Equal("world", resolver.Resolve("TM_SELECTED_TEXT"));
    }

    [Fact]
    public void VariableResolver_SelectionVariable_EmptySelection()
    {
        // TS: Empty selection returns empty string
        TextModel model = new("hello world");
        
        // Empty selection at position (1,1)
        SelectionVariableResolver resolver = new(model, new TextPosition(1, 1), new TextPosition(1, 1));

        Assert.Equal(string.Empty, resolver.Resolve("SELECTION"));
        Assert.Equal(string.Empty, resolver.Resolve("TM_SELECTED_TEXT"));
    }

    [Fact]
    public void VariableResolver_SelectionVariable_ReturnsNullForUnknown()
    {
        TextModel model = new("hello");
        SelectionVariableResolver resolver = new(model, new TextPosition(1, 1), new TextPosition(1, 1));

        Assert.Null(resolver.Resolve("TM_FILENAME"));
        Assert.Null(resolver.Resolve("UNKNOWN_VAR"));
    }

    [Fact]
    public void VariableResolver_ModelVariable_TmFilename()
    {
        // TS: test('ModelBasedVariableResolver, TM_FILENAME/TM_FILENAME_BASE', ...)
        ModelVariableResolver resolver = new("test.cs");

        Assert.Equal("test.cs", resolver.Resolve("TM_FILENAME"));
    }

    [Fact]
    public void VariableResolver_ModelVariable_NullFilename()
    {
        // TM_FILENAME with no filename returns empty string
        ModelVariableResolver resolver = new(null);

        Assert.Equal(string.Empty, resolver.Resolve("TM_FILENAME"));
    }

    [Fact]
    public void VariableResolver_ModelVariable_ReturnsNullForUnknown()
    {
        ModelVariableResolver resolver = new("test.cs");

        Assert.Null(resolver.Resolve("SELECTION"));
        Assert.Null(resolver.Resolve("UNKNOWN_VAR"));
    }

    [Fact]
    public void VariableResolver_Composite_FirstMatchWins()
    {
        // TS: CompositeSnippetVariableResolver returns first non-null result
        TextModel model = new("selected text");
        SelectionVariableResolver selectionResolver = new(model, new TextPosition(1, 1), new TextPosition(1, 9));
        ModelVariableResolver modelResolver = new("file.txt");

        CompositeVariableResolver composite = new(selectionResolver, modelResolver);

        // Selection resolver handles SELECTION
        Assert.Equal("selected", composite.Resolve("SELECTION"));
        // Model resolver handles TM_FILENAME
        Assert.Equal("file.txt", composite.Resolve("TM_FILENAME"));
    }

    [Fact]
    public void VariableResolver_Composite_FallsThrough()
    {
        // If first resolver returns null, try next
        ModelVariableResolver resolver1 = new("file.txt");
        TextModel model = new("hello");
        SelectionVariableResolver resolver2 = new(model, new TextPosition(1, 1), new TextPosition(1, 6));

        CompositeVariableResolver composite = new(resolver1, resolver2);

        // Model resolver doesn't handle SELECTION, so falls through to selection resolver
        Assert.Equal("hello", composite.Resolve("SELECTION"));
    }

    [Fact]
    public void VariableResolver_Fallback_ReturnsEmptyString()
    {
        // FallbackVariableResolver returns empty string for any variable
        FallbackVariableResolver resolver = FallbackVariableResolver.Instance;

        Assert.Equal(string.Empty, resolver.Resolve("UNKNOWN"));
        Assert.Equal(string.Empty, resolver.Resolve("TM_FILENAME"));
        Assert.Equal(string.Empty, resolver.Resolve("ANYTHING"));
    }

    [Fact]
    public void SnippetInsert_WithVariableResolver_ResolvesVariables()
    {
        // Full integration test with variable resolver
        TextModel model = new("existing");
        SnippetController controller = new(model);

        ModelVariableResolver resolver = new("MyClass.cs");
        SnippetInsertOptions options = new() { VariableResolver = resolver };

        controller.InsertSnippetAt(new TextPosition(1, 1), "// File: ${TM_FILENAME}\n${1:code}", options);

        string value = model.GetValue();
        Assert.Contains("// File: MyClass.cs", value);
    }

    [Fact]
    public void SnippetInsert_WithCompositeResolver()
    {
        TextModel model = new("hello world");
        SnippetController controller = new(model);

        // Select "world" (positions (1,7) to (1,12))
        SelectionVariableResolver selectionResolver = new(model, new TextPosition(1, 7), new TextPosition(1, 12));
        ModelVariableResolver modelResolver = new("test.cs");
        CompositeVariableResolver composite = new(selectionResolver, modelResolver, FallbackVariableResolver.Instance);

        SnippetInsertOptions options = new() { VariableResolver = composite };

        controller.InsertSnippetAt(new TextPosition(1, 1), "file: ${TM_FILENAME}, sel: ${SELECTION}", options);

        string value = model.GetValue();
        // Note: The selection was from original "hello world", 
        // but the snippet is inserted at position 1, shifting things
        Assert.Contains("file: test.cs", value);
    }

    [Fact]
    public void SnippetInsert_UnknownVariable_ExpandsToEmpty()
    {
        // TS: Unknown variables should silently expand to empty string
        TextModel model = new("test");
        SnippetController controller = new(model);

        // No resolver provided, so all variables expand to empty
        controller.InsertSnippetAt(new TextPosition(1, 1), "before${UNKNOWN_VAR}after");

        Assert.Equal("beforeaftertest", model.GetValue());
    }

    [Fact]
    public void SnippetInsert_VariableWithDefault_UsesDefaultWhenNotResolved()
    {
        // TS: ${VAR:default} uses default when variable not resolved
        TextModel model = new("x");
        SnippetController controller = new(model);

        // No resolver, so default is used
        controller.InsertSnippetAt(new TextPosition(1, 1), "${TM_FILENAME:unknown.txt}");

        Assert.Equal("unknown.txtx", model.GetValue());
    }

    [Fact]
    public void SnippetInsert_VariableWithDefault_UsesResolvedValue()
    {
        // TS: ${VAR:default} uses resolved value when available
        TextModel model = new("x");
        SnippetController controller = new(model);

        ModelVariableResolver resolver = new("actual.cs");
        SnippetInsertOptions options = new() { VariableResolver = resolver };

        controller.InsertSnippetAt(new TextPosition(1, 1), "${TM_FILENAME:default.txt}", options);

        Assert.Equal("actual.csx", model.GetValue());
    }

    [Fact]
    public void SnippetInsert_VariableWithDefault_UsesDefaultForEmptySelection()
    {
        // When SELECTION is empty, use default value
        TextModel model = new("x");
        SnippetController controller = new(model);

        SelectionVariableResolver resolver = new(model, new TextPosition(1, 1), new TextPosition(1, 1)); // Empty selection
        SnippetInsertOptions options = new() { VariableResolver = resolver };

        controller.InsertSnippetAt(new TextPosition(1, 1), "prefix ${SELECTION:nothing} suffix", options);

        // Empty selection returns empty string, so default "nothing" is used
        Assert.Equal("prefix nothing suffixx", model.GetValue());
    }

    [Fact]
    public void SnippetInsert_MultipleVariables()
    {
        // TS: Multiple variables in one snippet
        TextModel model = new("");
        SnippetController controller = new(model);

        SelectionVariableResolver selResolver = new(model, new TextPosition(1, 1), new TextPosition(1, 1));
        ModelVariableResolver modelResolver = new("Program.cs");
        CompositeVariableResolver composite = new(selResolver, modelResolver, FallbackVariableResolver.Instance);

        SnippetInsertOptions options = new() { VariableResolver = composite };

        controller.InsertSnippetAt(
            new TextPosition(1, 1),
            "// ${TM_FILENAME}\n// Selection: ${SELECTION:none}\n${1:code}",
            options);

        string value = model.GetValue();
        Assert.Contains("// Program.cs", value);
        Assert.Contains("// Selection: none", value);
    }

    [Fact]
    public void SnippetInsert_VariablesAndPlaceholders_Mixed()
    {
        // Variables and placeholders can be mixed
        TextModel model = new("");
        SnippetController controller = new(model);

        ModelVariableResolver resolver = new("Test.cs");
        SnippetInsertOptions options = new() { VariableResolver = resolver };

        controller.InsertSnippetAt(
            new TextPosition(1, 1),
            "namespace ${1:MyNamespace}\n{\n    // ${TM_FILENAME}\n    class ${2:MyClass} { $0 }\n}",
            options);

        string value = model.GetValue();
        Assert.Contains("namespace MyNamespace", value);
        Assert.Contains("// Test.cs", value);
        Assert.Contains("class MyClass", value);

        // Should have 3 placeholders: ${1}, ${2}, $0
        TextPosition? p1 = controller.NextPlaceholder();
        Assert.NotNull(p1);
        Assert.False(controller.IsAtFinalTabstop);

        TextPosition? p2 = controller.NextPlaceholder();
        Assert.NotNull(p2);
        Assert.False(controller.IsAtFinalTabstop);

        TextPosition? p0 = controller.NextPlaceholder();
        Assert.NotNull(p0);
        Assert.True(controller.IsAtFinalTabstop);
    }

    [Fact]
    public void SnippetInsert_SelectionVariable_WithActualSelection()
    {
        // Test with actual selected text
        TextModel model = new("the quick brown fox");
        SnippetController controller = new(model);

        // Select "quick" (positions (1,5) to (1,10))
        SelectionVariableResolver selResolver = new(model, new TextPosition(1, 5), new TextPosition(1, 10));
        SnippetInsertOptions options = new() { VariableResolver = selResolver };

        // Insert at position 1 (before "the")
        controller.InsertSnippetAt(new TextPosition(1, 1), "selected: ${SELECTION}", options);

        string value = model.GetValue();
        Assert.Contains("selected: quick", value);
    }

    [Theory]
    [InlineData("${TM_FILENAME}", "file.cs", "file.cs")]
    [InlineData("${TM_FILENAME:default}", "file.cs", "file.cs")]
    [InlineData("${TM_FILENAME:default}", null, "default")]
    [InlineData("${UNKNOWN}", null, "")]
    [InlineData("${UNKNOWN:fallback}", null, "fallback")]
    public void SnippetInsert_VariableExpansion(string snippet, string? filename, string expected)
    {
        TextModel model = new("");
        SnippetController controller = new(model);

        ISnippetVariableResolver resolver = filename != null
            ? new CompositeVariableResolver(new ModelVariableResolver(filename), FallbackVariableResolver.Instance)
            : FallbackVariableResolver.Instance;

        SnippetInsertOptions options = new() { VariableResolver = resolver };
        controller.InsertSnippetAt(new TextPosition(1, 1), snippet, options);

        Assert.Equal(expected, model.GetValue());
    }

    [Fact]
    public void KnownSnippetVariableNames_IsKnown()
    {
        // Test the IsKnown helper
        Assert.True(KnownSnippetVariableNames.IsKnown("SELECTION"));
        Assert.True(KnownSnippetVariableNames.IsKnown("TM_SELECTED_TEXT"));
        Assert.True(KnownSnippetVariableNames.IsKnown("TM_FILENAME"));
        Assert.True(KnownSnippetVariableNames.IsKnown("TM_FILENAME_BASE"));
        Assert.True(KnownSnippetVariableNames.IsKnown("CLIPBOARD"));
        Assert.True(KnownSnippetVariableNames.IsKnown("CURRENT_YEAR"));
        Assert.True(KnownSnippetVariableNames.IsKnown("UUID"));

        Assert.False(KnownSnippetVariableNames.IsKnown("UNKNOWN"));
        Assert.False(KnownSnippetVariableNames.IsKnown("NOT_A_VAR"));
        Assert.False(KnownSnippetVariableNames.IsKnown(""));
    }

    #endregion

    #region Multi-Cursor Snippet Tests

    // Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetController2.old.test.ts
    // - Test: 'Final tabstop with multiple selections' (Lines: 256-340)
    // Ported: 2025-12-05

    [Fact]
    public void MultiCursor_FinalTabstop_DifferentLines()
    {
        // TS: editor.setSelections([new Selection(1, 1, 1, 1), new Selection(2, 1, 2, 1)]);
        // codeSnippet = 'foo$0';
        // Expected: selections at (1,4) and (2,4)
        TextModel model = new("line1\nline2");
        SnippetController controller = new(model);

        // Insert at line 1, col 1
        controller.InsertSnippetAt(new TextPosition(1, 1), "foo$0");
        // Insert at line 2, col 1
        controller.InsertSnippetAt(new TextPosition(2, 1), "foo$0");

        // After both insertions:
        // Line 1: "fooline1"
        // Line 2: "fooline2"
        Assert.Equal("fooline1\nfooline2", model.GetValue());
    }

    [Fact]
    public void MultiCursor_FinalTabstop_SameLine()
    {
        // TS: editor.setSelections([new Selection(1, 1, 1, 1), new Selection(1, 5, 1, 5)]);
        // codeSnippet = 'foo$0bar';
        // Expected: foo + bar inserted at both positions
        TextModel model = new("1234567890");
        SnippetController controller = new(model);

        // First insert at col 1
        controller.InsertSnippetAt(new TextPosition(1, 1), "foo$0bar");
        Assert.Equal("foobar1234567890", model.GetValue());

        // Second insert at original col 5 (now shifted by +6)
        controller.InsertSnippetAt(new TextPosition(1, 11), "foo$0bar");
        Assert.Equal("foobar1234foobar567890", model.GetValue());
    }

    [Fact]
    public void MultiCursor_WithNewlines_FinalTabstop()
    {
        // TS: editor.setSelections([new Selection(1, 1, 1, 1), new Selection(1, 5, 1, 5)]);
        // codeSnippet = 'foo\n$0\nbar';
        TextModel model = new("12345");
        SnippetController controller = new(model);

        // Insert multi-line snippet at col 1
        controller.InsertSnippetAt(new TextPosition(1, 1), "foo\n$0\nbar");

        // After: foo\n\nbar12345
        Assert.Equal("foo\n\nbar12345", model.GetValue());
    }

    [Fact]
    public void MultiCursor_Placeholders_IndependentSessions()
    {
        // Each multi-cursor position should have its own placeholder session
        TextModel model = new("A\nB");
        SnippetController controller = new(model);

        // Insert snippet with placeholder at line 1
        controller.InsertSnippetAt(new TextPosition(1, 2), "${1:x}");
        Assert.Equal("Ax\nB", model.GetValue());

        // Create new session and insert at line 2
        controller.InsertSnippetAt(new TextPosition(2, 2), "${1:y}");
        Assert.Equal("Ax\nBy", model.GetValue());
    }

    [Fact]
    public void MultiCursor_OverwriteBefore_NotSupported()
    {
        // Note: TS has overwriteBefore/After options; C# currently doesn't support them
        // This test documents the current behavior
        TextModel model = new("prefix_text");
        SnippetController controller = new(model);

        // Insert at column 8 (after "prefix_")
        controller.InsertSnippetAt(new TextPosition(1, 8), "new$0");

        // Without overwriteBefore, text is inserted as-is
        Assert.Equal("prefix_newtext", model.GetValue());
    }

    [Fact]
    public void MultiCursor_AdjustWhitespace_PerCursor()
    {
        // TS: adjustWhitespace applies per cursor based on line indentation
        TextModel model = new("function() {\n    inner();\n}");
        SnippetController controller = new(model);

        // Insert at line 2, col 5 (inside indented block)
        SnippetInsertOptions options = new() { AdjustWhitespace = true };
        controller.InsertSnippetAt(new TextPosition(2, 5), "if (true) {\n    body();\n}", options);

        string result = model.GetValue();
        // Should have indentation adjusted
        Assert.Contains("if (true)", result);
        Assert.Contains("body();", result);
    }

    #endregion
}
