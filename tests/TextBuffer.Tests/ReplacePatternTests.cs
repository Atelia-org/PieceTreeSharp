/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *
 *  TypeScript Source:
 *  - ts/src/vs/editor/contrib/find/test/browser/replacePattern.test.ts (Lines: 1-350, Replace Pattern test suite)
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Text.RegularExpressions;
using Xunit;
using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests
{
    /// <summary>
    /// Tests for ReplacePattern functionality.
    /// Ported from TypeScript: replacePattern.test.ts
    /// </summary>
    public class ReplacePatternTests
    {
        private void TestParse(string input, ReplacePiece[] expectedPieces)
        {
            var actual = ReplacePatternParser.ParseReplaceString(input);
            var expected = new ReplacePattern(expectedPieces);
            Assert.Equal(expected, actual);
        }

        private void AssertReplace(string target, Regex search, string replaceString, string expected)
        {
            var replacePattern = ReplacePatternParser.ParseReplaceString(replaceString);
            var m = search.Match(target);
            
            string[]? matches = null;
            if (m.Success)
            {
                matches = new string[m.Groups.Count];
                for (int i = 0; i < m.Groups.Count; i++)
                {
                    matches[i] = m.Groups[i].Value;
                }
            }
            
            var actual = replacePattern.BuildReplaceString(matches);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ParseReplaceString_NoBackslash()
        {
            // no backslash => no treatment
            TestParse("hello", new[] { ReplacePiece.StaticValue("hello") });
        }

        [Fact]
        public void ParseReplaceString_Tab()
        {
            // \t => TAB
            TestParse("\\thello", new[] { ReplacePiece.StaticValue("\thello") });
            TestParse("h\\tello", new[] { ReplacePiece.StaticValue("h\tello") });
            TestParse("hello\\t", new[] { ReplacePiece.StaticValue("hello\t") });
        }

        [Fact]
        public void ParseReplaceString_Newline()
        {
            // \n => LF
            TestParse("\\nhello", new[] { ReplacePiece.StaticValue("\nhello") });
        }

        [Fact]
        public void ParseReplaceString_EscapedBackslash()
        {
            // \\t => \t
            TestParse("\\\\thello", new[] { ReplacePiece.StaticValue("\\thello") });
            TestParse("h\\\\tello", new[] { ReplacePiece.StaticValue("h\\tello") });
            TestParse("hello\\\\t", new[] { ReplacePiece.StaticValue("hello\\t") });

            // \\\t => \TAB
            TestParse("\\\\\\thello", new[] { ReplacePiece.StaticValue("\\\thello") });

            // \\\\t => \\t
            TestParse("\\\\\\\\thello", new[] { ReplacePiece.StaticValue("\\\\thello") });
        }

        [Fact]
        public void ParseReplaceString_TrailingBackslash()
        {
            // \ at the end => no treatment
            TestParse("hello\\", new[] { ReplacePiece.StaticValue("hello\\") });
        }

        [Fact]
        public void ParseReplaceString_UnknownEscape()
        {
            // \ with unknown char => no treatment
            TestParse("hello\\x", new[] { ReplacePiece.StaticValue("hello\\x") });

            // \ with back reference => no treatment
            TestParse("hello\\0", new[] { ReplacePiece.StaticValue("hello\\0") });
        }

        [Fact]
        public void ParseReplaceString_CaptureGroups()
        {
            TestParse("hello$&", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(0) });
            TestParse("hello$0", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(0) });
            TestParse("hello$02", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(0), ReplacePiece.StaticValue("2") });
            TestParse("hello$1", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(1) });
            TestParse("hello$2", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(2) });
            TestParse("hello$9", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(9) });
            TestParse("$9hello", new[] { ReplacePiece.MatchIndex(9), ReplacePiece.StaticValue("hello") });
        }

        [Fact]
        public void ParseReplaceString_TwoDigitCaptureGroups()
        {
            TestParse("hello$12", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(12) });
            TestParse("hello$99", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(99) });
            TestParse("hello$99a", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(99), ReplacePiece.StaticValue("a") });
            TestParse("hello$1a", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(1), ReplacePiece.StaticValue("a") });
            TestParse("hello$100", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(10), ReplacePiece.StaticValue("0") });
            TestParse("hello$100a", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(10), ReplacePiece.StaticValue("0a") });
            TestParse("hello$10a0", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.MatchIndex(10), ReplacePiece.StaticValue("a0") });
        }

        [Fact]
        public void ParseReplaceString_DollarSign()
        {
            TestParse("hello$$", new[] { ReplacePiece.StaticValue("hello$") });
            TestParse("hello$$0", new[] { ReplacePiece.StaticValue("hello$0") });

            TestParse("hello$`", new[] { ReplacePiece.StaticValue("hello$`") });
            TestParse("hello$'", new[] { ReplacePiece.StaticValue("hello$'") });
        }

        [Fact]
        public void ParseReplaceString_WithCaseModifiers()
        {
            TestParse("hello\\U$1", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.CaseOps(1, new[] { "U" }) });
            AssertReplace("func privateFunc(", new Regex(@"func (\w+)\("), "func \\U$1(", "func PRIVATEFUNC(");

            TestParse("hello\\u$1", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.CaseOps(1, new[] { "u" }) });
            AssertReplace("func privateFunc(", new Regex(@"func (\w+)\("), "func \\u$1(", "func PrivateFunc(");

            TestParse("hello\\L$1", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.CaseOps(1, new[] { "L" }) });
            AssertReplace("func privateFunc(", new Regex(@"func (\w+)\("), "func \\L$1(", "func privatefunc(");

            TestParse("hello\\l$1", new[] { ReplacePiece.StaticValue("hello"), ReplacePiece.CaseOps(1, new[] { "l" }) });
            AssertReplace("func PrivateFunc(", new Regex(@"func (\w+)\("), "func \\l$1(", "func privateFunc(");

            TestParse("hello$1\\u\\u\\U$4goodbye", new[] { 
                ReplacePiece.StaticValue("hello"), 
                ReplacePiece.MatchIndex(1), 
                ReplacePiece.CaseOps(4, new[] { "u", "u", "U" }), 
                ReplacePiece.StaticValue("goodbye") 
            });
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
            var searchRegex = new Regex(@"let\s+(\w+)\s*=\s*require\s*\(\s*['\""]([\w\.\-/]+)\s*['\""]\s*\)\s*");
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
            var replacePattern = ReplacePatternParser.ParseReplaceString("a{$1}");
            var m = new Regex("a(z)?").Match("abcd");
            
            string[]? matches = null;
            if (m.Success)
            {
                matches = new string[m.Groups.Count];
                for (int i = 0; i < m.Groups.Count; i++)
                {
                    matches[i] = m.Groups[i].Value;
                }
            }
            
            var actual = replacePattern.BuildReplaceString(matches);
            Assert.Equal("a{}", actual);
        }

        [Fact]
        public void BuildReplaceStringWithCasePreserved_Basic()
        {
            void AssertCasePreserved(string[] target, string replaceString, string expected)
            {
                var actual = ReplacePattern.BuildReplaceStringWithCasePreserved(target, replaceString);
                Assert.Equal(expected, actual);
            }

            AssertCasePreserved(new[] { "abc" }, "Def", "def");
            AssertCasePreserved(new[] { "Abc" }, "Def", "Def");
            AssertCasePreserved(new[] { "ABC" }, "Def", "DEF");
            AssertCasePreserved(new[] { "abc", "Abc" }, "Def", "def");
            AssertCasePreserved(new[] { "Abc", "abc" }, "Def", "Def");
            AssertCasePreserved(new[] { "ABC", "abc" }, "Def", "DEF");
            AssertCasePreserved(new[] { "aBc", "abc" }, "Def", "def");
            AssertCasePreserved(new[] { "AbC" }, "Def", "Def");
            AssertCasePreserved(new[] { "aBC" }, "Def", "def");
            AssertCasePreserved(new[] { "aBc" }, "DeF", "deF");
        }

        [Fact]
        public void BuildReplaceStringWithCasePreserved_Hyphen()
        {
            void AssertCasePreserved(string[] target, string replaceString, string expected)
            {
                var actual = ReplacePattern.BuildReplaceStringWithCasePreserved(target, replaceString);
                Assert.Equal(expected, actual);
            }

            AssertCasePreserved(new[] { "Foo-Bar" }, "newfoo-newbar", "Newfoo-Newbar");
            AssertCasePreserved(new[] { "Foo-Bar-Abc" }, "newfoo-newbar-newabc", "Newfoo-Newbar-Newabc");
            AssertCasePreserved(new[] { "Foo-Bar-abc" }, "newfoo-newbar", "Newfoo-newbar");
            AssertCasePreserved(new[] { "foo-Bar" }, "newfoo-newbar", "newfoo-Newbar");
            AssertCasePreserved(new[] { "foo-BAR" }, "newfoo-newbar", "newfoo-NEWBAR");
            AssertCasePreserved(new[] { "foO-BAR" }, "NewFoo-NewBar", "newFoo-NEWBAR");
        }

        [Fact]
        public void BuildReplaceStringWithCasePreserved_Underscore()
        {
            void AssertCasePreserved(string[] target, string replaceString, string expected)
            {
                var actual = ReplacePattern.BuildReplaceStringWithCasePreserved(target, replaceString);
                Assert.Equal(expected, actual);
            }

            AssertCasePreserved(new[] { "Foo_Bar" }, "newfoo_newbar", "Newfoo_Newbar");
            AssertCasePreserved(new[] { "Foo_Bar_Abc" }, "newfoo_newbar_newabc", "Newfoo_Newbar_Newabc");
            AssertCasePreserved(new[] { "Foo_Bar_abc" }, "newfoo_newbar", "Newfoo_newbar");
            AssertCasePreserved(new[] { "Foo_Bar-abc" }, "newfoo_newbar-abc", "Newfoo_newbar-abc");
            AssertCasePreserved(new[] { "foo_Bar" }, "newfoo_newbar", "newfoo_Newbar");
            AssertCasePreserved(new[] { "Foo_BAR" }, "newfoo_newbar", "Newfoo_NEWBAR");
        }

        [Fact]
        public void PreserveCase_Integration()
        {
            void AssertPreserve(string[] target, string replaceString, string expected)
            {
                var replacePattern = ReplacePatternParser.ParseReplaceString(replaceString);
                var actual = replacePattern.BuildReplaceString(target, true);
                Assert.Equal(expected, actual);
            }

            AssertPreserve(new[] { "abc" }, "Def", "def");
            AssertPreserve(new[] { "Abc" }, "Def", "Def");
            AssertPreserve(new[] { "ABC" }, "Def", "DEF");
            AssertPreserve(new[] { "abc", "Abc" }, "Def", "def");
            AssertPreserve(new[] { "Abc", "abc" }, "Def", "Def");
            AssertPreserve(new[] { "ABC", "abc" }, "Def", "DEF");
            AssertPreserve(new[] { "aBc", "abc" }, "Def", "def");
            AssertPreserve(new[] { "AbC" }, "Def", "Def");
            AssertPreserve(new[] { "aBC" }, "Def", "def");
            AssertPreserve(new[] { "aBc" }, "DeF", "deF");
            AssertPreserve(new[] { "Foo-Bar" }, "newfoo-newbar", "Newfoo-Newbar");
            AssertPreserve(new[] { "Foo-Bar-Abc" }, "newfoo-newbar-newabc", "Newfoo-Newbar-Newabc");
            AssertPreserve(new[] { "Foo-Bar-abc" }, "newfoo-newbar", "Newfoo-newbar");
            AssertPreserve(new[] { "foo-Bar" }, "newfoo-newbar", "newfoo-Newbar");
            AssertPreserve(new[] { "foo-BAR" }, "newfoo-newbar", "newfoo-NEWBAR");
            AssertPreserve(new[] { "foO-BAR" }, "NewFoo-NewBar", "newFoo-NEWBAR");
            AssertPreserve(new[] { "Foo_Bar" }, "newfoo_newbar", "Newfoo_Newbar");
            AssertPreserve(new[] { "Foo_Bar_Abc" }, "newfoo_newbar_newabc", "Newfoo_Newbar_Newabc");
            AssertPreserve(new[] { "Foo_Bar_abc" }, "newfoo_newbar", "Newfoo_newbar");
            AssertPreserve(new[] { "Foo_Bar-abc" }, "newfoo_newbar-abc", "Newfoo_newbar-abc");
            AssertPreserve(new[] { "foo_Bar" }, "newfoo_newbar", "newfoo_Newbar");
            AssertPreserve(new[] { "foo_BAR" }, "newfoo_newbar", "newfoo_NEWBAR");
        }
    }
}
