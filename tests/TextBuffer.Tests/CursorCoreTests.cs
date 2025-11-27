// WS4-PORT-Core Stage 0 Tests
// Tests for CursorConfiguration, SingleCursorState, CursorState, and related types
// Created: 2025-11-26
using PieceTree.TextBuffer.Core;
using PieceTree.TextBuffer.Cursor;
using PieceTree.TextBuffer.Tests.Helpers;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests;

public class CursorCoreTests
{
    public static IEnumerable<object[]> CursorConfigurationPermutations => new[]
    {
        new object[] { 2, 2, true, EditorAutoIndentStrategy.Full },
        [4, 2, false, EditorAutoIndentStrategy.Keep],
        [8, 4, true, EditorAutoIndentStrategy.Brackets],
    };

    #region SelectionStartKind Tests

    [Fact]
    public void SelectionStartKind_HasCorrectValues()
    {
        Assert.Equal(0, (int)SelectionStartKind.Simple);
        Assert.Equal(1, (int)SelectionStartKind.Word);
        Assert.Equal(2, (int)SelectionStartKind.Line);
    }

    #endregion

    #region Selection Tests

    [Fact]
    public void Selection_SetStartPosition_UpdatesActiveForRtlSelections()
    {
        Selection selection = new(
            anchor: new TextPosition(2, 5),
            active: new TextPosition(1, 3));

        Assert.Equal(SelectionDirection.RTL, selection.Direction);

        Selection updated = selection.SetStartPosition(1, 1);

        Assert.Equal(new TextPosition(2, 5), updated.Anchor);
        Assert.Equal(new TextPosition(1, 1), updated.Active);
        Assert.Equal(SelectionDirection.RTL, updated.Direction);
    }

    [Fact]
    public void Selection_SetEndPosition_UpdatesAnchorForRtlSelections()
    {
        Selection selection = new(
            anchor: new TextPosition(3, 10),
            active: new TextPosition(1, 1));
        Assert.Equal(SelectionDirection.RTL, selection.Direction);

        Selection updated = selection.SetEndPosition(4, 2);

        Assert.Equal(new TextPosition(4, 2), updated.Anchor);
        Assert.Equal(new TextPosition(1, 1), updated.Active);
    }

    #endregion

    #region SingleCursorState Tests

    [Fact]
    public void SingleCursorState_Constructor_InitializesProperties()
    {
        Range selectionStart = new(1, 5, 1, 10);
        TextPosition position = new(1, 15);

        SingleCursorState state = new(
            selectionStart,
            SelectionStartKind.Word,
            0,
            position,
            3);

        Assert.Equal(selectionStart, state.SelectionStart);
        Assert.Equal(SelectionStartKind.Word, state.SelectionStartKind);
        Assert.Equal(0, state.SelectionStartLeftoverVisibleColumns);
        Assert.Equal(position, state.Position);
        Assert.Equal(3, state.LeftoverVisibleColumns);
    }

    [Fact]
    public void SingleCursorState_Selection_ComputedFromStartAndPosition()
    {
        // When position is after selectionStart
        Range selectionStart = new(1, 5, 1, 10);
        TextPosition position = new(1, 15);

        SingleCursorState state = new(selectionStart, SelectionStartKind.Word, 0, position, 0);

        CursorTestHelper.AssertSelection(state.Selection, 1, 5, 1, 15);
    }

    [Fact]
    public void SingleCursorState_Selection_ComputedWhenPositionBeforeStart()
    {
        // When position is before selectionStart
        Range selectionStart = new(1, 10, 1, 15);
        TextPosition position = new(1, 5);

        SingleCursorState state = new(selectionStart, SelectionStartKind.Word, 0, position, 0);

        CursorTestHelper.AssertSelection(state.Selection, 1, 5, 1, 15);
    }

    [Fact]
    public void SingleCursorState_HasSelection_TrueWhenNotEmpty()
    {
        Range selectionStart = new(1, 5, 1, 10);
        TextPosition position = new(1, 15);

        SingleCursorState state = new(selectionStart, SelectionStartKind.Word, 0, position, 0);

        Assert.True(state.HasSelection());
    }

    [Fact]
    public void SingleCursorState_HasSelection_FalseWhenCollapsed()
    {
        Range selectionStart = new(1, 5, 1, 5);
        TextPosition position = new(1, 5);

        SingleCursorState state = new(selectionStart, SelectionStartKind.Simple, 0, position, 0);

        Assert.False(state.HasSelection());
    }

    [Fact]
    public void SingleCursorState_Equals_ReturnsTrueForSameState()
    {
        SingleCursorState state1 = new(
            new Range(1, 5, 1, 10),
            SelectionStartKind.Word,
            2,
            new TextPosition(1, 15),
            3);

        SingleCursorState state2 = new(
            new Range(1, 5, 1, 10),
            SelectionStartKind.Word,
            2,
            new TextPosition(1, 15),
            3);

        Assert.True(state1.Equals(state2));
    }

    [Fact]
    public void SingleCursorState_Equals_ReturnsFalseForDifferentPosition()
    {
        SingleCursorState state1 = new(
            new Range(1, 5, 1, 10),
            SelectionStartKind.Word,
            0,
            new TextPosition(1, 15),
            0);

        SingleCursorState state2 = new(
            new Range(1, 5, 1, 10),
            SelectionStartKind.Word,
            0,
            new TextPosition(1, 20),
            0);

        Assert.False(state1.Equals(state2));
    }

    [Fact]
    public void SingleCursorState_Move_ExtendSelection()
    {
        SingleCursorState state = new(
            new Range(1, 5, 1, 10),
            SelectionStartKind.Word,
            0,
            new TextPosition(1, 15),
            0);

        SingleCursorState moved = state.Move(inSelectionMode: true, lineNumber: 2, column: 5, leftoverVisibleColumns: 0);

        // Selection start should be preserved
        Assert.Equal(1, moved.SelectionStart.StartLineNumber);
        Assert.Equal(5, moved.SelectionStart.StartColumn);
        Assert.Equal(SelectionStartKind.Word, moved.SelectionStartKind);

        // Position should be updated
        Assert.Equal(2, moved.Position.LineNumber);
        Assert.Equal(5, moved.Position.Column);
    }

    [Fact]
    public void SingleCursorState_Move_CollapseSelection()
    {
        SingleCursorState state = new(
            new Range(1, 5, 1, 10),
            SelectionStartKind.Word,
            0,
            new TextPosition(1, 15),
            0);

        SingleCursorState moved = state.Move(inSelectionMode: false, lineNumber: 2, column: 5, leftoverVisibleColumns: 3);

        // Selection should collapse to new position
        CursorTestHelper.AssertCursorStateSelection(moved, 2, 5, 2, 5);
        Assert.Equal(SelectionStartKind.Simple, moved.SelectionStartKind);

        // Position should be updated
        Assert.Equal(2, moved.Position.LineNumber);
        Assert.Equal(5, moved.Position.Column);
        Assert.Equal(3, moved.LeftoverVisibleColumns);
    }

    [Fact]
    public void SingleCursorState_Move_WithSelectionMode_PreservesStickyColumns()
    {
        Range selectionStart = new(2, 4, 2, 4);
        SingleCursorState state = new(
            selectionStart,
            SelectionStartKind.Line,
            selectionStartLeftoverVisibleColumns: 8,
            new TextPosition(2, 10),
            leftoverVisibleColumns: 3);

        SingleCursorState moved = state.Move(inSelectionMode: true, lineNumber: 3, column: 6, leftoverVisibleColumns: 12);

        CursorTestHelper.AssertCursorStateSelection(moved, 2, 4, 3, 6);
        Assert.Equal(8, moved.SelectionStartLeftoverVisibleColumns);
        Assert.Equal(12, moved.LeftoverVisibleColumns);
    }

    [Fact]
    public void SingleCursorState_Move_ResettingSelection_ClearsStickyColumns()
    {
        SingleCursorState state = new(
            new Range(3, 1, 3, 4),
            SelectionStartKind.Word,
            selectionStartLeftoverVisibleColumns: 5,
            new TextPosition(3, 4),
            leftoverVisibleColumns: 5);

        SingleCursorState moved = state.Move(inSelectionMode: false, lineNumber: 4, column: 2, leftoverVisibleColumns: 7);

        CursorTestHelper.AssertCursorStateSelection(moved, 4, 2, 4, 2);
        Assert.Equal(7, moved.SelectionStartLeftoverVisibleColumns);
        Assert.Equal(7, moved.LeftoverVisibleColumns);
        Assert.Equal(SelectionStartKind.Simple, moved.SelectionStartKind);
    }

    #endregion

    #region CursorState Tests

    [Fact]
    public void CursorState_Constructor_InitializesBothStates()
    {
        SingleCursorState modelState = new(
            new Range(1, 1, 1, 1),
            SelectionStartKind.Simple,
            0,
            new TextPosition(1, 1),
            0);

        SingleCursorState viewState = new(
            new Range(1, 1, 1, 1),
            SelectionStartKind.Simple,
            0,
            new TextPosition(1, 1),
            0);

        CursorState cursorState = new(modelState, viewState);

        Assert.Equal(modelState, cursorState.ModelState);
        Assert.Equal(viewState, cursorState.ViewState);
    }

    [Fact]
    public void CursorState_FromModelSelection_CreatesCorrectState()
    {
        Selection selection = new(1, 5, 2, 10);

        // FromModelSelection returns PartialModelCursorState
        PartialModelCursorState partialState = CursorState.FromModelSelection(selection);

        // Model state should be set correctly - active position is (2,10)
        Assert.Equal(2, partialState.ModelState.Position.LineNumber);
        Assert.Equal(10, partialState.ModelState.Position.Column);

        // Selection start should match start of selection
        Assert.Equal(1, partialState.ModelState.SelectionStart.StartLineNumber);
        Assert.Equal(5, partialState.ModelState.SelectionStart.StartColumn);
    }

    [Fact]
    public void CursorState_Equals_ReturnsTrueForSameState()
    {
        SingleCursorState modelState = new(
            new Range(1, 1, 1, 1),
            SelectionStartKind.Simple,
            0,
            new TextPosition(1, 1),
            0);

        SingleCursorState viewState = new(
            new Range(1, 1, 1, 1),
            SelectionStartKind.Simple,
            0,
            new TextPosition(1, 1),
            0);

        CursorState state1 = new(modelState, viewState);
        CursorState state2 = new(modelState, viewState);

        Assert.True(state1.Equals(state2));
    }

    #endregion

    #region PartialCursorState Tests

    [Fact]
    public void PartialModelCursorState_CanBeCreated()
    {
        SingleCursorState singleState = new(
            new Range(1, 5, 2, 10),
            SelectionStartKind.Simple,
            0,
            new TextPosition(2, 10),
            0);
        PartialModelCursorState state = new(singleState);

        Assert.Equal(1, state.ModelState.SelectionStart.StartLineNumber);
        Assert.Equal(5, state.ModelState.SelectionStart.StartColumn);
        Assert.Equal(2, state.ModelState.SelectionStart.EndLineNumber);
        Assert.Equal(10, state.ModelState.SelectionStart.EndColumn);
    }

    [Fact]
    public void PartialViewCursorState_CanBeCreated()
    {
        SingleCursorState singleState = new(
            new Range(1, 5, 2, 10),
            SelectionStartKind.Simple,
            0,
            new TextPosition(2, 10),
            0);
        PartialViewCursorState state = new(singleState);

        Assert.Equal(1, state.ViewState.SelectionStart.StartLineNumber);
        Assert.Equal(5, state.ViewState.SelectionStart.StartColumn);
        Assert.Equal(2, state.ViewState.SelectionStart.EndLineNumber);
        Assert.Equal(10, state.ViewState.SelectionStart.EndColumn);
    }

    #endregion

    #region CursorConfiguration Tests

    [Theory]
    [MemberData(nameof(CursorConfigurationPermutations))]
    public void CursorConfiguration_ResolvesBuilderPermutations(int tabSize, int indentSize, bool insertSpaces, EditorAutoIndentStrategy autoIndent)
    {
        TestEditorContext context = TestEditorBuilder.Create()
            .WithLines("function foo() {", "\treturn 0;", "}")
            .WithTabSize(tabSize)
            .WithIndentSize(indentSize)
            .WithInsertSpaces(insertSpaces)
            .BuildContext();

        CursorConfiguration config = new(context.Model.GetOptions(), new EditorCursorOptions
        {
            AutoIndent = autoIndent,
            StickyTabStops = true,
        });

        Assert.Equal(tabSize, config.TabSize);
        Assert.Equal(indentSize, config.IndentSize);
        Assert.Equal(insertSpaces, config.InsertSpaces);
        Assert.Equal(autoIndent, config.AutoIndent);
        Assert.True(config.StickyTabStops);
    }

    [Fact]
    public void CursorConfiguration_Constructor_InitializesAllProperties()
    {
        TextModelResolvedOptions modelOptions = TextModelResolvedOptions.Resolve(
            new TextModelCreationOptions { TabSize = 4, IndentSize = 4, InsertSpaces = true },
            DefaultEndOfLine.LF);
        EditorCursorOptions editorOptions = new()
        {
            ReadOnly = false,
            StickyTabStops = true,
            PageSize = 30,
            LineHeight = 20,
            TypicalHalfwidthCharacterWidth = 9,
            WordSeparators = "`~!@#$%^&*()-=+",
            MultiCursorMergeOverlapping = true,
            MultiCursorPaste = MultiCursorPasteMode.Full,
            UseTabStops = false,
            TrimWhitespaceOnDelete = false,
            EmptySelectionClipboard = false,
            CopyWithSyntaxHighlighting = false,
            AutoClosingBrackets = EditorAutoClosingStrategy.Never,
            AutoClosingQuotes = EditorAutoClosingStrategy.BeforeWhitespace,
            WordSegmenterLocales = ["en-US"],
            OvertypeOnPaste = true,
        };

        CursorConfiguration config = new(modelOptions, editorOptions);

        Assert.False(config.ReadOnly);
        Assert.Equal(4, config.TabSize);
        Assert.Equal(4, config.IndentSize);
        Assert.True(config.InsertSpaces);
        Assert.True(config.StickyTabStops);
        Assert.Equal(30, config.PageSize);
        Assert.Equal(20, config.LineHeight);
        Assert.Equal("`~!@#$%^&*()-=+", config.WordSeparators);
        Assert.True(config.MultiCursorMergeOverlapping);
        Assert.Equal(MultiCursorPasteMode.Full, config.MultiCursorPaste);
        Assert.False(config.UseTabStops);
        Assert.False(config.TrimWhitespaceOnDelete);
        Assert.False(config.EmptySelectionClipboard);
        Assert.False(config.CopyWithSyntaxHighlighting);
        Assert.Equal(EditorAutoClosingStrategy.Never, config.AutoClosingBrackets);
        Assert.Equal(EditorAutoClosingStrategy.BeforeWhitespace, config.AutoClosingQuotes);
        Assert.Equal(9, config.TypicalHalfwidthCharacterWidth);
        Assert.False(config.ShouldAutoCloseBefore.Bracket('\t'));
        Assert.False(config.ShouldAutoCloseBefore.Comment('x'));
        Assert.Single(config.WordSegmenterLocales);
        Assert.Equal("en-US", config.WordSegmenterLocales[0]);
        Assert.True(config.OvertypeOnPaste);
    }

    [Fact]
    public void CursorConfiguration_FromTextModelResolvedOptions_Defaults()
    {
        TextModelResolvedOptions modelOptions = TextModelResolvedOptions.Resolve(
            new TextModelCreationOptions { TabSize = 2, IndentSize = 2, InsertSpaces = false },
            DefaultEndOfLine.LF);
        EditorCursorOptions editorOptions = EditorCursorOptions.Default;

        CursorConfiguration config = new(modelOptions, editorOptions);

        Assert.Equal(2, config.TabSize);
        Assert.Equal(2, config.IndentSize);
        Assert.False(config.InsertSpaces);
    }

    #endregion

    #region VisibleColumn Helper Tests

    [Fact]
    public void CursorColumnsHelper_VisibleColumnFromColumn_BasicCase()
    {
        // Simple case: no tabs, column 5 should be visible column 5
        CursorTestHelper.AssertVisibleColumn("Hello World", column: 6, tabSize: 4, expectedVisibleColumn: 5);
    }

    [Fact]
    public void CursorColumnsHelper_VisibleColumnFromColumn_WithTabs()
    {
        // Tab at position 1, tabSize=4: column 5 = after tab (4) + 1 = visible 5
        CursorTestHelper.AssertVisibleColumn("\tHello", column: 2, tabSize: 4, expectedVisibleColumn: 4);
    }

    [Fact]
    public void CursorColumnsHelper_ColumnFromVisibleColumn_BasicCase()
    {
        // Simple case: visible column 5 should be column 6 (1-based)
        CursorTestHelper.AssertColumnFromVisible("Hello World", visibleColumn: 5, tabSize: 4, expectedColumn: 6);
    }

    [Fact]
    public void CursorColumnsHelper_ColumnFromVisibleColumn_WithTabs()
    {
        // Tab at position 1, tabSize=4: visible column 4 should be column 2
        CursorTestHelper.AssertColumnFromVisible("\tHello", visibleColumn: 4, tabSize: 4, expectedColumn: 2);
    }

    [Fact]
    public void CursorColumnsHelper_RoundTrip_PreservesColumn()
    {
        string lineContent = "Hello\tWorld";
        int tabSize = 4;
        int originalColumn = 8;

        CursorTestHelper.AssertColumnRoundTrip(lineContent, originalColumn, tabSize);
    }

    [Fact]
    public void CursorColumnsHelper_RoundTrip_WithSurrogatePair()
    {
        const string lineContent = "    Third Line🐶";
        CursorTestHelper.AssertColumnRoundTrip(lineContent, column: 17, tabSize: 4);
    }

    [Theory]
    [InlineData(0, 4, 4)]
    [InlineData(3, 4, 4)]
    [InlineData(4, 4, 8)]
    [InlineData(7, 2, 8)]
    public void CursorColumnsHelper_NextIndentTabStop_ComputesIndentGuides(int visibleColumn, int indentSize, int expected)
    {
        Assert.Equal(expected, CursorColumnsHelper.NextIndentTabStop(visibleColumn, indentSize));
    }

    #endregion

    #region EditOperationType Tests

    [Fact]
    public void EditOperationType_HasCorrectValues()
    {
        Assert.Equal(0, (int)EditOperationType.Other);
        Assert.Equal(2, (int)EditOperationType.DeletingLeft);
        Assert.Equal(3, (int)EditOperationType.DeletingRight);
        Assert.Equal(4, (int)EditOperationType.TypingOther);
        Assert.Equal(5, (int)EditOperationType.TypingFirstSpace);
        Assert.Equal(6, (int)EditOperationType.TypingConsecutiveSpace);
    }

    #endregion

    #region ColumnSelectData Tests

    [Fact]
    public void ColumnSelectData_CanBeCreated()
    {
        ColumnSelectData data = new(
            isReal: true,
            fromViewLineNumber: 1,
            fromViewVisualColumn: 5,
            toViewLineNumber: 3,
            toViewVisualColumn: 10);

        Assert.True(data.IsReal);
        Assert.Equal(1, data.FromViewLineNumber);
        Assert.Equal(5, data.FromViewVisualColumn);
        Assert.Equal(3, data.ToViewLineNumber);
        Assert.Equal(10, data.ToViewVisualColumn);
    }

    #endregion

    #region MultiCursorPaste Tests

    [Fact]
    public void MultiCursorPaste_DefaultValue_IsSpread()
    {
        CursorConfiguration config = new(
            TextModelResolvedOptions.Resolve(new TextModelCreationOptions(), DefaultEndOfLine.LF),
            EditorCursorOptions.Default);

        Assert.Equal(MultiCursorPasteMode.Spread, config.MultiCursorPaste);
    }

    [Fact]
    public void CursorConfiguration_ShouldAutoCloseBefore_UsesLanguageDefinitions()
    {
        TextModelResolvedOptions modelOptions = TextModelResolvedOptions.Resolve(new TextModelCreationOptions(), DefaultEndOfLine.LF);
        EditorCursorOptions editorOptions = new()
        {
            AutoClosingQuotes = EditorAutoClosingStrategy.LanguageDefined,
            AutoClosingBrackets = EditorAutoClosingStrategy.BeforeWhitespace,
            AutoClosingComments = EditorAutoClosingStrategy.Never,
            LanguageConfiguration = LanguageConfigurationOptions.Default with { AutoCloseBeforeQuotes = "abc" },
        };

        CursorConfiguration config = new(modelOptions, editorOptions);

        Assert.True(config.ShouldAutoCloseBefore.Quote('a'));
        Assert.False(config.ShouldAutoCloseBefore.Quote('z'));
        Assert.True(config.ShouldAutoCloseBefore.Bracket(' '));
        Assert.False(config.ShouldAutoCloseBefore.Comment(' '));
    }

    [Fact]
    public void CursorConfiguration_NormalizeIndentation_FollowsInsertSpacesSetting()
    {
        TextModelResolvedOptions spacesOptions = TextModelResolvedOptions.Resolve(
            new TextModelCreationOptions { InsertSpaces = true, TabSize = 4, IndentSize = 4 },
            DefaultEndOfLine.LF);
        TextModelResolvedOptions tabsOptions = TextModelResolvedOptions.Resolve(
            new TextModelCreationOptions { InsertSpaces = false, TabSize = 4, IndentSize = 4 },
            DefaultEndOfLine.LF);

        CursorConfiguration spacesConfig = new(spacesOptions);
        CursorConfiguration tabsConfig = new(tabsOptions);

        Assert.Equal("        foo", spacesConfig.NormalizeIndentation("\t\tfoo"));
        Assert.Equal("\t\tfoo", tabsConfig.NormalizeIndentation("\t\tfoo"));
        Assert.Equal(string.Empty, spacesConfig.NormalizeIndentation(string.Empty));
    }

    #endregion

    #region CL7 Stage 1 - Cursor State Wiring Tests

    [Fact]
    public void Cursor_SetState_ValidatesModelState_OutOfBounds()
    {
        TextModel model = new("Hello\nWorld", new TextModelCreationOptions { EnableVsCursorParity = true });
        CursorContext context = CursorContext.FromModel(model);

        using Cursor.Cursor cursor = new(context);

        // Set state with out-of-bounds position (line 5 doesn't exist)
        SingleCursorState invalidState = new(
            new Range(1, 1, 1, 1),
            SelectionStartKind.Simple,
            0,
            new TextPosition(5, 100), // Invalid: only 2 lines
            0);

        cursor.SetState(context, invalidState, null);

        // Should be clamped to valid position
        CursorState result = cursor.AsCursorState();
        Assert.Equal(2, result.ModelState.Position.LineNumber); // Clamped to line 2
        Assert.True(result.ModelState.Position.Column <= 6);    // Clamped to max column
    }

    [Fact]
    public void Cursor_SetState_ValidatesModelState_ColumnTooLarge()
    {
        TextModel model = new("Hello", new TextModelCreationOptions { EnableVsCursorParity = true });
        CursorContext context = CursorContext.FromModel(model);

        using Cursor.Cursor cursor = new(context);

        // Set state with out-of-bounds column
        SingleCursorState invalidState = new(
            new Range(1, 1, 1, 1),
            SelectionStartKind.Simple,
            0,
            new TextPosition(1, 100), // Invalid: "Hello" is only 5 chars
            0);

        cursor.SetState(context, invalidState, null);

        CursorState result = cursor.AsCursorState();
        Assert.Equal(1, result.ModelState.Position.LineNumber);
        Assert.Equal(6, result.ModelState.Position.Column); // Clamped to column 6 (after "Hello")
    }

    [Fact]
    public void Cursor_TrackedRange_SurvivesEdit()
    {
        TextModel model = new("Hello World", new TextModelCreationOptions { EnableVsCursorParity = true });
        CursorContext context = CursorContext.FromModel(model);

        using Cursor.Cursor cursor = new(context);

        // Set selection on "World"
        cursor.SetState(context,
            new SingleCursorState(
                new Range(1, 7, 1, 12),
                SelectionStartKind.Simple,
                0,
                new TextPosition(1, 12),
                0),
            null);

        cursor.StartTrackingSelection(context);

        // Insert text before selection
        model.PushEditOperations([new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "XXX")], null);

        // Read back from markers
        Selection recovered = cursor.ReadSelectionFromMarkers(context);

        // Selection should have shifted by 3
        Assert.Equal(1, recovered.Anchor.LineNumber);
        Assert.Equal(10, recovered.Anchor.Column); // Was 7, shifted by 3
        Assert.Equal(15, recovered.Active.Column); // Was 12, shifted by 3
    }

    [Fact]
    public void Cursor_TrackedRange_CollapsedSelection_StaysCollapsed()
    {
        TextModel model = new("Hello World", new TextModelCreationOptions { EnableVsCursorParity = true });
        CursorContext context = CursorContext.FromModel(model);

        using Cursor.Cursor cursor = new(context);

        // Set collapsed cursor at position 6
        cursor.SetState(context,
            new SingleCursorState(
                new Range(1, 6, 1, 6),
                SelectionStartKind.Simple,
                0,
                new TextPosition(1, 6),
                0),
            null);

        cursor.StartTrackingSelection(context);

        // Insert text before cursor
        model.PushEditOperations([new TextEdit(new TextPosition(1, 1), new TextPosition(1, 1), "XX")], null);

        Selection recovered = cursor.ReadSelectionFromMarkers(context);

        // Should still be collapsed, just shifted
        Assert.True(recovered.IsEmpty);
        Assert.Equal(8, recovered.Active.Column); // Was 6, shifted by 2
    }

    [Fact]
    public void Cursor_AsCursorState_ReturnsValidState()
    {
        TextModel model = new("Hello", new TextModelCreationOptions { EnableVsCursorParity = true });
        CursorContext context = CursorContext.FromModel(model);

        using Cursor.Cursor cursor = new(context);
        cursor.MoveTo(new TextPosition(1, 3));

        CursorState state = cursor.AsCursorState();

        Assert.NotNull(state.ModelState);
        Assert.NotNull(state.ViewState);
        Assert.Equal(1, state.ModelState.Position.LineNumber);
        Assert.Equal(3, state.ModelState.Position.Column);
    }

    [Fact]
    public void Cursor_EnsureValidState_ClampsAfterEdit()
    {
        TextModel model = new("Hello World", new TextModelCreationOptions { EnableVsCursorParity = true });
        CursorContext context = CursorContext.FromModel(model);

        using Cursor.Cursor cursor = new(context);

        // Move cursor to end
        cursor.MoveTo(new TextPosition(1, 12));
        Assert.Equal(12, cursor.Selection.Active.Column);

        // Delete part of the content
        model.PushEditOperations([new TextEdit(new TextPosition(1, 6), new TextPosition(1, 12), "")], null);

        // Now content is "Hello", cursor position may be invalid
        cursor.EnsureValidState(context);

        CursorState state = cursor.AsCursorState();
        Assert.True(state.ModelState.Position.Column <= 6); // Clamped to valid range
    }

    [Fact]
    public void Cursor_LegacyMode_StillWorks()
    {
        // Test that cursor works when EnableVsCursorParity = false (default)
        TextModel model = new("Hello World");
        CursorContext context = CursorContext.FromModel(model);

        using Cursor.Cursor cursor = new(context);
        cursor.MoveTo(new TextPosition(1, 6));

        Assert.Equal(1, cursor.Selection.Active.LineNumber);
        Assert.Equal(6, cursor.Selection.Active.Column);

        cursor.SelectTo(new TextPosition(1, 12));
        Assert.False(cursor.Selection.IsEmpty);
    }

    [Fact]
    public void Cursor_DualMode_FlagOnPath()
    {
        TextModel model = new("Line 1\nLine 2", new TextModelCreationOptions { EnableVsCursorParity = true });
        CursorContext context = CursorContext.FromModel(model);

        using Cursor.Cursor cursor = new(context);

        // Use SetState API (Stage 1 path)
        SingleCursorState newState = new(
            new Range(1, 3, 1, 6),
            SelectionStartKind.Word,
            0,
            new TextPosition(1, 6),
            5);

        cursor.SetState(context, newState, null);

        CursorState result = cursor.AsCursorState();
        Assert.Equal(SelectionStartKind.Word, result.ModelState.SelectionStartKind);
        Assert.Equal(5, result.ModelState.LeftoverVisibleColumns);
        CursorTestHelper.AssertSelection(result.ModelState.Selection, 1, 3, 1, 6);
    }

    [Fact]
    public void Cursor_ReadSelectionFromMarkers_FallbackWhenNotTracking()
    {
        TextModel model = new("Hello", new TextModelCreationOptions { EnableVsCursorParity = true });
        CursorContext context = CursorContext.FromModel(model);

        using Cursor.Cursor cursor = new(context);
        cursor.MoveTo(new TextPosition(1, 3));

        // Stop tracking
        cursor.StopTrackingSelection(context);

        // ReadSelectionFromMarkers should return current selection as fallback
        Selection result = cursor.ReadSelectionFromMarkers(context);

        Assert.Equal(1, result.Active.LineNumber);
        Assert.Equal(3, result.Active.Column);
    }

    #endregion

    #region CL7 TODOs

    [Fact(Skip = "TODO(#delta-2025-11-26-aa4-cl7-cursor-core): Port SelectHighlightsAction parity from ts/src/vs/editor/contrib/multicursor/test/browser/multicursor.test.ts once MultiCursorSelectionController is wired up.")]
    public void SelectHighlightsAction_ParityPending()
    {
    }

    [Fact(Skip = "TODO(#delta-2025-11-26-aa4-cl7-snippet): Add multi-cursor snippet integration tests after SnippetController mirrors TS cursor.test.ts flows.")]
    public void MultiCursorSnippetIntegration_ParityPending()
    {
    }

    #endregion
}
