# DF-005 Result: MarkdownRenderer Prototype

## Summary
I have implemented the `MarkdownRenderer` prototype for the "DocUI" vision. This renderer converts a `TextModel` into a Markdown code block, visualizing cursors (`|`) and selections (`[...]`).

## Changes
1.  **New Class**: `src/PieceTree.TextBuffer/Rendering/MarkdownRenderer.cs`
    *   Implements `Render(TextModel model)`.
    *   Iterates line by line.
    *   Fetches decorations (Cursors/Selections) for each line.
    *   Inserts markers (`|`, `[`, `]`) into the text.
    *   Handles index shifting by sorting insertions descending.

2.  **Modified**: `src/PieceTree.TextBuffer/TextModel.cs`
    *   Exposed `GetOffsetAt(TextPosition)` and `GetPositionAt(int offset)` to allow mapping between Line/Column and Offsets.

3.  **Fixed**: `src/PieceTree.TextBuffer/Decorations/IntervalTree.cs`
    *   Updated `Search` method to correctly handle zero-length decorations (Cursors) at the start of a search range.

4.  **Tests**: `src/PieceTree.TextBuffer.Tests/MarkdownRendererTests.cs`
    *   Added tests for:
        *   Cursor rendering.
        *   Selection rendering.
        *   Multi-line cursors.
        *   Multi-line selections.

## Usage
```csharp
var model = new TextModel("Hello World");
// Add cursor/selection via model.AddDecoration(...)
var renderer = new MarkdownRenderer();
string markdown = renderer.Render(model);
```

## Verification
Ran `dotnet test src/PieceTree.TextBuffer.Tests/PieceTree.TextBuffer.Tests.csproj`. All 51 tests passed.
