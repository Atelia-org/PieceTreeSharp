/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *
 *  TypeScript Source:
 *  - ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts (Lines: 1-350, Replace Pattern test suite)
 *--------------------------------------------------------------------------------------------*/

using System.Text.RegularExpressions;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests;

/// <summary>
/// Tests for ReplacePattern functionality.
/// Ported from TypeScript: replacePattern.test.ts
/// </summary>
public class ReplacePatternTests
{
    private void TestParse(string input, ReplacePiece[] expectedPieces)
    {
        ReplacePattern actual = ReplacePatternParser.ParseReplaceString(input);
        ReplacePattern expected = new(expectedPieces);
        Assert.Equal(expected, actual);
    }

    private void AssertReplace(string target, Regex search, string replaceString, string expected)
    {
        ReplacePattern replacePattern = ReplacePatternParser.ParseReplaceString(replaceString);
        Match m = search.Match(target);

        string[]? matches = null;
        if (m.Success)
        {
            matches = new string[m.Groups.Count];
            for (int i = 0; i < m.Groups.Count; i++)
            {
                matches[i] = m.Groups[i].Value;
            }
        }

        string actual = replacePattern.BuildReplaceString(matches);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ParseReplaceString_NoBackslash()
    {
        // no backslash => no treatment
        TestParse("hello", [ReplacePiece.StaticValue("hello")]);
    }

    [Fact]
    public void ParseReplaceString_Tab()
    {
        // \t => TAB
        TestParse("\\thello", [ReplacePiece.StaticValue("\thello")]);
        TestParse("h\\tello", [ReplacePiece.StaticValue("h\tello")]);
        TestParse("hello\\t", [ReplacePiece.StaticValue("hello\t")]);
    }

    [Fact]
    public void ParseReplaceString_Newline()
    {
        // \n => LF
        TestParse("\\nhello", [ReplacePiece.StaticValue("\nhello")]);
    }

    [Fact]
    public void ParseReplaceString_EscapedBackslash()
    {
        // \\t => \t
        TestParse("\\\\thello", [ReplacePiece.StaticValue("\\thello")]);
        TestParse("h\\\\tello", [ReplacePiece.StaticValue("h\\tello")]);
        TestParse("hello\\\\t", [ReplacePiece.StaticValue("hello\\t")]);

        // \\\t => \TAB
        TestParse("\\\\\\thello", [ReplacePiece.StaticValue("\\\thello")]);

        // \\\\t => \\t
        TestParse("\\\\\\\\thello", [ReplacePiece.StaticValue("\\\\thello")]);
    }

    [Fact]
    public void ParseReplaceString_TrailingBackslash()
    {
        // \ at the end => no treatment
        TestParse("hello\\", [ReplacePiece.StaticValue("hello\\")]);
    }

    [Fact]
    public void ParseReplaceString_UnknownEscape()
    {
        // \ with unknown char => no treatment
        TestParse("hello\\x", [ReplacePiece.StaticValue("hello\\x")]);

        // \ with back reference => no treatment
        TestParse("hello\\0", [ReplacePiece.StaticValue("hello\\0")]);
    }

    [Fact]
    public void ParseReplaceString_CaptureGroups()
    {
        TestParse("hello$&", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(0)]);
        TestParse("hello$0", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(0)]);
        TestParse("hello$02", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(0), ReplacePiece.StaticValue("2")]);
        TestParse("hello$1", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(1)]);
        TestParse("hello$2", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(2)]);
        TestParse("hello$9", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(9)]);
        TestParse("$9hello", [ReplacePiece.MatchIndex(9), ReplacePiece.StaticValue("hello")]);
    }

    [Fact]
    public void ParseReplaceString_TwoDigitCaptureGroups()
    {
        TestParse("hello$12", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(12)]);
        TestParse("hello$99", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(99)]);
        TestParse("hello$99a", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(99), ReplacePiece.StaticValue("a")]);
        TestParse("hello$1a", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(1), ReplacePiece.StaticValue("a")]);
        TestParse("hello$100", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(10), ReplacePiece.StaticValue("0")]);
        TestParse("hello$100a", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(10), ReplacePiece.StaticValue("0a")]);
        TestParse("hello$10a0", [ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(10), ReplacePiece.StaticValue("a0")]);
    }

    [Fact]
    public void ParseReplaceString_DollarSign()
    {
        TestParse("hello$$", [ReplacePiece.StaticValue("hello$")]);
        TestParse("hello$$0", [ReplacePiece.StaticValue("hello$0")]);

        TestParse("hello$`", [ReplacePiece.StaticValue("hello$`")]);
        TestParse("hello$'", [ReplacePiece.StaticValue("hello$'")]);
    }

    [Fact]
    public void ParseReplaceString_WithCaseModifiers()
    {
        TestParse("hello\\U$1", [ReplacePiece.StaticValue("hello"), ReplacePiece.CaseOps(1, ["U"])]);
        AssertReplace("func privateFunc(", new Regex(@"func (\w+)\("), "func \\U$1(", "func PRIVATEFUNC(");

        TestParse("hello\\u$1", [ReplacePiece.StaticValue("hello"), ReplacePiece.CaseOps(1, ["u"])]);
        AssertReplace("func privateFunc(", new Regex(@"func (\w+)\("), "func \\u$1(", "func PrivateFunc(");

        TestParse("hello\\L$1", [ReplacePiece.StaticValue("hello"), ReplacePiece.CaseOps(1, ["L"])]);
        AssertReplace("func privateFunc(", new Regex(@"func (\w+)\("), "func \\L$1(", "func privatefunc(");

        TestParse("hello\\l$1", [ReplacePiece.StaticValue("hello"), ReplacePiece.CaseOps(1, ["l"])]);
        AssertReplace("func PrivateFunc(", new Regex(@"func (\w+)\("), "func \\l$1(", "func privateFunc(");

        TestParse("hello$1\\u\\u\\U$4goodbye", [
            ReplacePiece.StaticValue("hello"),
            ReplacePiece.MatchIndex(1),
            ReplacePiece.CaseOps(4, ["u", "u", "U"]),
            ReplacePiece.StaticValue("goodbye")
        ]);
        AssertReplace("hellogooDbye", new Regex(@"hello(\w+)"), "hello\\u\\u\\l\\l\\U$1", "helloGOodBYE");
    }

    [Fact]
    public void ReplaceHasJavaScriptSemantics_Basic()
    {
        AssertReplace("hi", new Regex("hi"), "hello", "hello");
        AssertReplace("hi", new Regex("hi"), "\\t", "\t");
        AssertReplace("hi", new Regex("hi"), "\\n", "\n");
        AssertReplace("hi", new Regex("hi"), "\\\\t", "\\t");
        AssertReplace("hi", new Regex("hi"), "\\\\n", "\\n");
    }

    [Fact]
    public void ReplaceHasJavaScriptSemantics_ImplicitCaptureGroup()
    {
        // implicit capture group 0
        AssertReplace("hi", new Regex("hi"), "hello$&", "hellohi");
        AssertReplace("hi", new Regex("hi"), "hello$0", "hellohi");
        AssertReplace("hi", new Regex("hi"), "hello$&1", "hellohi1");
        AssertReplace("hi", new Regex("hi"), "hello$01", "hellohi1");
    }

    [Fact]
    public void ReplaceHasJavaScriptSemantics_CaptureGroups()
    {
        // capture groups have funny semantics in replace strings
        // the replace string interprets $nn as a captured group only if it exists in the search regex
        // Note: C# Regex behavior differs from JavaScript for empty capture groups
        // In C#, when $10 refers to an empty capture group, it becomes ""
        // In JavaScript, it may behave differently (this is a known difference)
        AssertReplace("hi", new Regex("(hi)"), "hello$10", "hellohi0");
        AssertReplace("hi", new Regex("(hi)()()()()()()()()()"), "hello$10", "hello"); // C# behavior: empty group 10
        AssertReplace("hi", new Regex("(hi)"), "hello$100", "hellohi00");
        AssertReplace("hi", new Regex("(hi)"), "hello$20", "hello$20");
    }

    [Fact]
    public void GetReplaceStringIfGivenTextIsCompleteMatch_Basic()
    {
        AssertReplace("bla", new Regex("bla"), "hello", "hello");
        AssertReplace("bla", new Regex("(bla)"), "hello", "hello");
        AssertReplace("bla", new Regex("(bla)"), "hello$0", "hellobla");
    }

    [Fact]
    public void GetReplaceStringIfGivenTextIsCompleteMatch_ImportExample()
    {
        Regex searchRegex = new(@"let\s+(\w+)\s*=\s*require\s*\(\s*['\""]([\w\.\-/]+)\s*['\""]\s*\)\s*");
        AssertReplace("let fs = require('fs')", searchRegex, "import * as $1 from '$2';", "import * as fs from 'fs';");
        AssertReplace("let something = require('fs')", searchRegex, "import * as $1 from '$2';", "import * as something from 'fs';");
        AssertReplace("let something = require('fs')", searchRegex, "import * as $1 from '$1';", "import * as something from 'something';");
        AssertReplace("let something = require('fs')", searchRegex, "import * as $2 from '$1';", "import * as fs from 'something';");
        AssertReplace("let something = require('fs')", searchRegex, "import * as $0 from '$0';", "import * as let something = require('fs') from 'let something = require('fs')';");
        AssertReplace("let fs = require('fs')", searchRegex, "import * as $1 from '$2';", "import * as fs from 'fs';");
    }

    [Fact]
    public void GetReplaceStringIfGivenTextIsCompleteMatch_OtherCases()
    {
        AssertReplace("for ()", new Regex(@"for(.*)"), "cat$1", "cat ()");

        // issue #18111
        AssertReplace("HRESULT OnAmbientPropertyChange(DISPID   dispid);", new Regex(@"\b\s{3}\b"), " ", " ");
    }

    [Fact]
    public void GetReplaceStringIfMatchIsSubstringOfText_Basic()
    {
        AssertReplace("this is a bla text", new Regex("bla"), "hello", "hello");
        AssertReplace("this is a bla text", new Regex("this(?=.*bla)"), "that", "that");
        AssertReplace("this is a bla text", new Regex("(th)is(?=.*bla)"), "$1at", "that");
        AssertReplace("this is a bla text", new Regex("(th)is(?=.*bla)"), "$1e", "the");
        AssertReplace("this is a bla text", new Regex("(th)is(?=.*bla)"), "$1ere", "there");
        AssertReplace("this is a bla text", new Regex("(th)is(?=.*bla)"), "$1", "th");
        AssertReplace("this is a bla text", new Regex("(th)is(?=.*bla)"), "ma$1", "math");
        AssertReplace("this is a bla text", new Regex("(th)is(?=.*bla)"), "ma$1s", "maths");
        AssertReplace("this is a bla text", new Regex("(th)is(?=.*bla)"), "$0", "this");
        AssertReplace("this is a bla text", new Regex("(th)is(?=.*bla)"), "$0$1", "thisth");
    }

    [Fact]
    public void GetReplaceStringIfMatchIsSubstringOfText_Lookahead()
    {
        AssertReplace("this is a bla text", new Regex(@"bla(?=\stext$)"), "foo", "foo");
        AssertReplace("this is a bla text", new Regex(@"b(la)(?=\stext$)"), "f$1", "fla");
        AssertReplace("this is a bla text", new Regex(@"b(la)(?=\stext$)"), "f$0", "fbla");
        AssertReplace("this is a bla text", new Regex(@"b(la)(?=\stext$)"), "$0ah", "blaah");
    }

    [Fact]
    public void Issue19740_UndefinedCaptureGroup()
    {
        // issue #19740 Find and replace capture group/backreference inserts `undefined` instead of empty string
        ReplacePattern replacePattern = ReplacePatternParser.ParseReplaceString("a{$1}");
        Match m = new Regex("a(z)?").Match("abcd");

        string[]? matches = null;
        if (m.Success)
        {
            matches = new string[m.Groups.Count];
            for (int i = 0; i < m.Groups.Count; i++)
            {
                matches[i] = m.Groups[i].Value;
            }
        }

        string actual = replacePattern.BuildReplaceString(matches);
        Assert.Equal("a{}", actual);
    }

    [Fact]
    public void BuildReplaceStringWithCasePreserved_Basic()
    {
        void AssertCasePreserved(string[] target, string replaceString, string expected)
        {
            string actual = ReplacePattern.BuildReplaceStringWithCasePreserved(target, replaceString);
            Assert.Equal(expected, actual);
        }

        AssertCasePreserved(["abc"], "Def", "def");
        AssertCasePreserved(["Abc"], "Def", "Def");
        AssertCasePreserved(["ABC"], "Def", "DEF");
        AssertCasePreserved(["abc", "Abc"], "Def", "def");
        AssertCasePreserved(["Abc", "abc"], "Def", "Def");
        AssertCasePreserved(["ABC", "abc"], "Def", "DEF");
        AssertCasePreserved(["aBc", "abc"], "Def", "def");
        AssertCasePreserved(["AbC"], "Def", "Def");
        AssertCasePreserved(["aBC"], "Def", "def");
        AssertCasePreserved(["aBc"], "DeF", "deF");
    }

    [Fact]
    public void BuildReplaceStringWithCasePreserved_Hyphen()
    {
        void AssertCasePreserved(string[] target, string replaceString, string expected)
        {
            string actual = ReplacePattern.BuildReplaceStringWithCasePreserved(target, replaceString);
            Assert.Equal(expected, actual);
        }

        AssertCasePreserved(["Foo-Bar"], "newfoo-newbar", "Newfoo-Newbar");
        AssertCasePreserved(["Foo-Bar-Abc"], "newfoo-newbar-newabc", "Newfoo-Newbar-Newabc");
        AssertCasePreserved(["Foo-Bar-abc"], "newfoo-newbar", "Newfoo-newbar");
        AssertCasePreserved(["foo-Bar"], "newfoo-newbar", "newfoo-Newbar");
        AssertCasePreserved(["foo-BAR"], "newfoo-newbar", "newfoo-NEWBAR");
        AssertCasePreserved(["foO-BAR"], "NewFoo-NewBar", "newFoo-NEWBAR");
    }

    [Fact]
    public void BuildReplaceStringWithCasePreserved_Underscore()
    {
        void AssertCasePreserved(string[] target, string replaceString, string expected)
        {
            string actual = ReplacePattern.BuildReplaceStringWithCasePreserved(target, replaceString);
            Assert.Equal(expected, actual);
        }

        AssertCasePreserved(["Foo_Bar"], "newfoo_newbar", "Newfoo_Newbar");
        AssertCasePreserved(["Foo_Bar_Abc"], "newfoo_newbar_newabc", "Newfoo_Newbar_Newabc");
        AssertCasePreserved(["Foo_Bar_abc"], "newfoo_newbar", "Newfoo_newbar");
        AssertCasePreserved(["Foo_Bar-abc"], "newfoo_newbar-abc", "Newfoo_newbar-abc");
        AssertCasePreserved(["foo_Bar"], "newfoo_newbar", "newfoo_Newbar");
        AssertCasePreserved(["Foo_BAR"], "newfoo_newbar", "Newfoo_NEWBAR");
    }

    [Fact]
    public void PreserveCase_Integration()
    {
        void AssertPreserve(string[] target, string replaceString, string expected)
        {
            ReplacePattern replacePattern = ReplacePatternParser.ParseReplaceString(replaceString);
            string actual = replacePattern.BuildReplaceString(target, true);
            Assert.Equal(expected, actual);
        }

        AssertPreserve(["abc"], "Def", "def");
        AssertPreserve(["Abc"], "Def", "Def");
        AssertPreserve(["ABC"], "Def", "DEF");
        AssertPreserve(["abc", "Abc"], "Def", "def");
        AssertPreserve(["Abc", "abc"], "Def", "Def");
        AssertPreserve(["ABC", "abc"], "Def", "DEF");
        AssertPreserve(["aBc", "abc"], "Def", "def");
        AssertPreserve(["AbC"], "Def", "Def");
        AssertPreserve(["aBC"], "Def", "def");
        AssertPreserve(["aBc"], "DeF", "deF");
        AssertPreserve(["Foo-Bar"], "newfoo-newbar", "Newfoo-Newbar");
        AssertPreserve(["Foo-Bar-Abc"], "newfoo-newbar-newabc", "Newfoo-Newbar-Newabc");
        AssertPreserve(["Foo-Bar-abc"], "newfoo-newbar", "Newfoo-newbar");
        AssertPreserve(["foo-Bar"], "newfoo-newbar", "newfoo-Newbar");
        AssertPreserve(["foo-BAR"], "newfoo-newbar", "newfoo-NEWBAR");
        AssertPreserve(["foO-BAR"], "NewFoo-NewBar", "newFoo-NEWBAR");
        AssertPreserve(["Foo_Bar"], "newfoo_newbar", "Newfoo_Newbar");
        AssertPreserve(["Foo_Bar_Abc"], "newfoo_newbar_newabc", "Newfoo_Newbar_Newabc");
        AssertPreserve(["Foo_Bar_abc"], "newfoo_newbar", "Newfoo_newbar");
        AssertPreserve(["Foo_Bar-abc"], "newfoo_newbar-abc", "Newfoo_newbar-abc");
        AssertPreserve(["foo_Bar"], "newfoo_newbar", "newfoo_Newbar");
        AssertPreserve(["foo_BAR"], "newfoo_newbar", "newfoo_NEWBAR");
    }
}
