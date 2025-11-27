// Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetController2.test.ts
// Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetSession.test.ts
// - Tests: Snippet insertion, placeholder navigation
// Ported: 2025-11-22

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
}
