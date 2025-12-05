// Source: ts/src/vs/editor/contrib/snippet/test/browser/snippetParser.test.ts
// - Test: 'Transform -> FormatString#resolve' (Lines: 654-687)
// - Test: 'Snippet optional transforms are not applied correctly' #37702 (Lines: 703-716)
// - Test: 'Variable transformation doesn\'t work if undefined variables' #51769 (Lines: 723-729)
// Ported: 2025-12-05 (Direct translation from TypeScript)

using PieceTree.TextBuffer.Snippet;
using System.Text.RegularExpressions;
using Xunit;

namespace PieceTree.TextBuffer.Tests;

/// <summary>
/// Tests for Snippet Transform and FormatString functionality.
/// Direct translation from TS snippetParser.test.ts.
/// </summary>
public class SnippetTransformTests
{
    #region FormatString#resolve - Shorthand Functions

    // TS: test('Transform -> FormatString#resolve', function () {
    // Lines 654-687

    [Fact]
    public void FormatString_Resolve_Upcase()
    {
        // assert.strictEqual(new FormatString(1, 'upcase').resolve('foo'), 'FOO');
        Assert.Equal("FOO", new FormatString(1, "upcase").Resolve("foo"));
    }

    [Fact]
    public void FormatString_Resolve_Downcase()
    {
        // assert.strictEqual(new FormatString(1, 'downcase').resolve('FOO'), 'foo');
        Assert.Equal("foo", new FormatString(1, "downcase").Resolve("FOO"));
    }

    [Fact]
    public void FormatString_Resolve_Capitalize()
    {
        // assert.strictEqual(new FormatString(1, 'capitalize').resolve('bar'), 'Bar');
        Assert.Equal("Bar", new FormatString(1, "capitalize").Resolve("bar"));
    }

    [Fact]
    public void FormatString_Resolve_Capitalize_MultipleWords()
    {
        // assert.strictEqual(new FormatString(1, 'capitalize').resolve('bar no repeat'), 'Bar no repeat');
        Assert.Equal("Bar no repeat", new FormatString(1, "capitalize").Resolve("bar no repeat"));
    }

    [Fact]
    public void FormatString_Resolve_Capitalize_SingleChar()
    {
        // Edge case: single character should be uppercased
        Assert.Equal("A", new FormatString(1, "capitalize").Resolve("a"));
        Assert.Equal("Z", new FormatString(1, "capitalize").Resolve("z"));
    }

    [Theory]
    [InlineData("bar-foo", "BarFoo")]
    [InlineData("bar-42-foo", "Bar42Foo")]
    [InlineData("snake_AndPascalCase", "SnakeAndPascalCase")]
    [InlineData("kebab-AndPascalCase", "KebabAndPascalCase")]
    [InlineData("_justPascalCase", "JustPascalCase")]
    public void FormatString_Resolve_PascalCase(string input, string expected)
    {
        // TS Lines 661-665
        Assert.Equal(expected, new FormatString(1, "pascalcase").Resolve(input));
    }

    [Theory]
    [InlineData("bar-foo", "barFoo")]
    [InlineData("bar-42-foo", "bar42Foo")]
    [InlineData("snake_AndCamelCase", "snakeAndCamelCase")]
    [InlineData("kebab-AndCamelCase", "kebabAndCamelCase")]
    [InlineData("_JustCamelCase", "justCamelCase")]
    public void FormatString_Resolve_CamelCase(string input, string expected)
    {
        // TS Lines 666-670
        Assert.Equal(expected, new FormatString(1, "camelcase").Resolve(input));
    }

    [Fact]
    public void FormatString_Resolve_UnknownShorthand_PassThrough()
    {
        // assert.strictEqual(new FormatString(1, 'notKnown').resolve('input'), 'input');
        Assert.Equal("input", new FormatString(1, "notKnown").Resolve("input"));
    }

    #endregion

    #region FormatString#resolve - Conditional (if/else)

    [Theory]
    [InlineData(null, "")]     // undefined -> ''
    [InlineData("", "")]       // '' -> ''
    [InlineData("bar", "foo")] // 'bar' -> 'foo' (ifValue)
    public void FormatString_Resolve_IfValue(string? input, string expected)
    {
        // TS Lines 674-676: if condition
        // new FormatString(1, undefined, 'foo', undefined).resolve(...)
        Assert.Equal(expected, new FormatString(1, null, "foo", null).Resolve(input));
    }

    [Theory]
    [InlineData(null, "foo")]  // undefined -> 'foo' (elseValue)
    [InlineData("", "foo")]    // '' -> 'foo' (elseValue)
    [InlineData("bar", "bar")] // 'bar' -> 'bar' (has value, ignore elseValue)
    public void FormatString_Resolve_ElseValue(string? input, string expected)
    {
        // TS Lines 679-681: else condition
        // new FormatString(1, undefined, undefined, 'foo').resolve(...)
        Assert.Equal(expected, new FormatString(1, null, null, "foo").Resolve(input));
    }

    [Theory]
    [InlineData(null, "foo")]  // undefined -> 'foo' (elseValue)
    [InlineData("", "foo")]    // '' -> 'foo' (elseValue)
    [InlineData("baz", "bar")] // 'baz' -> 'bar' (ifValue)
    public void FormatString_Resolve_IfElseValue(string? input, string expected)
    {
        // TS Lines 684-686: if-else condition
        // new FormatString(1, undefined, 'bar', 'foo').resolve(...)
        Assert.Equal(expected, new FormatString(1, null, "bar", "foo").Resolve(input));
    }

    #endregion

    #region Transform#resolve

    [Fact]
    public void Transform_Resolve_MultipleCaptures_Issue37702()
    {
        // TS: test('Snippet optional transforms are not applied correctly when reusing the same variable, #37702', ...)
        // Lines 703-716
        // transform.appendChild(new FormatString(1, 'upcase'));
        // transform.appendChild(new FormatString(2, 'upcase'));
        // transform.regexp = /^(.)|-(.)/g;
        // assert.strictEqual(transform.resolve('my-file-name'), 'MyFileName');

        var transform = new Transform();
        transform.AppendChild(new FormatString(1, "upcase"));
        transform.AppendChild(new FormatString(2, "upcase"));
        transform.Regexp = new Regex(@"^(.)|-(.)", RegexOptions.None);

        Assert.Equal("MyFileName", transform.Resolve("my-file-name"));
    }

    [Fact]
    public void Transform_Clone_Resolve_Issue37702()
    {
        // const clone = transform.clone();
        // assert.strictEqual(clone.resolve('my-file-name'), 'MyFileName');

        var transform = new Transform();
        transform.AppendChild(new FormatString(1, "upcase"));
        transform.AppendChild(new FormatString(2, "upcase"));
        transform.Regexp = new Regex(@"^(.)|-(.)", RegexOptions.None);

        var clone = (Transform)transform.Clone();
        Assert.Equal("MyFileName", clone.Resolve("my-file-name"));
    }

    [Fact]
    public void Transform_ToTextmateString_Issue51769()
    {
        // TS: test('Variable transformation doesn\'t work if undefined variables are used in the same snippet #51769', ...)
        // Lines 723-729
        // const transform = new Transform();
        // transform.appendChild(new Text('bar'));
        // transform.regexp = new RegExp('foo', 'gi');
        // assert.strictEqual(transform.toTextmateString(), '/foo/bar/ig');

        var transform = new Transform();
        transform.AppendChild(new Text("bar"));
        transform.Regexp = new Regex("foo", RegexOptions.IgnoreCase);

        Assert.Equal("/foo/bar/ig", transform.ToTextmateString());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void FormatString_Resolve_EmptyString_ReturnsEmpty()
    {
        Assert.Equal("", new FormatString(1, "upcase").Resolve(""));
        Assert.Equal("", new FormatString(1, "upcase").Resolve(null));
    }

    [Fact]
    public void Transform_Resolve_NoMatch_ElseBranch()
    {
        // TS: when the regex didn't match and when the transform has
        // else branches, then run those
        var transform = new Transform();
        transform.AppendChild(new FormatString(1, null, null, "default"));
        transform.Regexp = new Regex("xyz"); // Won't match "abc"

        Assert.Equal("default", transform.Resolve("abc"));
    }

    [Fact]
    public void Transform_Resolve_NoMatch_NoElse_ReturnsOriginal()
    {
        var transform = new Transform();
        transform.AppendChild(new FormatString(1, "upcase"));
        transform.Regexp = new Regex("xyz"); // Won't match "abc"

        Assert.Equal("abc", transform.Resolve("abc"));
    }

    [Fact]
    public void Text_Escape_SpecialCharacters()
    {
        // TS: Text.escape() escapes $, }, |, \
        Assert.Equal(@"\$", Text.Escape("$"));
        Assert.Equal(@"\}", Text.Escape("}"));
        Assert.Equal(@"\|", Text.Escape("|"));
        Assert.Equal(@"\\", Text.Escape(@"\"));
        Assert.Equal(@"hello \$world\}", Text.Escape("hello $world}"));
    }

    [Fact]
    public void Marker_AppendChild_MergesAdjacentText()
    {
        // TS: Marker.appendChild() merges adjacent Text nodes
        var transform = new Transform();
        transform.AppendChild(new Text("hello"));
        transform.AppendChild(new Text(" world"));

        Assert.Single(transform.Children);
        Assert.Equal("hello world", ((Text)transform.Children[0]).Value);
    }

    #endregion
}
