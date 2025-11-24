/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Migrated from: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts

namespace PieceTree.TextBuffer.Tests.Helpers;

using static PieceTree.TextBuffer.Tests.Helpers.PieceTreeScript;

internal static class PieceTreeDeterministicScripts
{
    public static PieceTreeScriptStep[] CrlfRandomBug01 { get; } = new[]
    {
        InsertStep(0, "\n\n\r\r", "crlf-random-bug1-insert-1"),
        InsertStep(1, "\r\n\r\n", "crlf-random-bug1-insert-2"),
        DeleteStep(5, 3, "crlf-random-bug1-delete-1"),
        DeleteStep(2, 3, "crlf-random-bug1-delete-2"),
    };

    public static PieceTreeScriptStep[] CrlfRandomBug02 { get; } = new[]
    {
        InsertStep(0, "\n\r\n\r", "crlf-random-bug2-insert-1"),
        InsertStep(2, "\n\r\r\r", "crlf-random-bug2-insert-2"),
        DeleteStep(4, 1, "crlf-random-bug2-delete-1"),
    };

    public static PieceTreeScriptStep[] CrlfRandomBug03 { get; } = new[]
    {
        InsertStep(0, "\n\n\n\r", "crlf-random-bug3-insert-1"),
        DeleteStep(2, 2, "crlf-random-bug3-delete-1"),
        DeleteStep(0, 2, "crlf-random-bug3-delete-2"),
        InsertStep(0, "\r\r\r\r", "crlf-random-bug3-insert-2"),
        InsertStep(2, "\r\n\r\r", "crlf-random-bug3-insert-3"),
        InsertStep(3, "\r\r\r\n", "crlf-random-bug3-insert-4"),
    };

    public static PieceTreeScriptStep[] CrlfRandomBug04 { get; } = new[]
    {
        InsertStep(0, "\n\n\n\n", "crlf-random-bug4-insert-1"),
        DeleteStep(3, 1, "crlf-random-bug4-delete-1"),
        InsertStep(1, "\r\r\r\r", "crlf-random-bug4-insert-2"),
        InsertStep(6, "\r\n\n\r", "crlf-random-bug4-insert-3"),
        DeleteStep(5, 3, "crlf-random-bug4-delete-2"),
    };

    public static PieceTreeScriptStep[] CrlfRandomBug05 { get; } = new[]
    {
        InsertStep(0, "\n\n\n\n", "crlf-random-bug5-insert-1"),
        DeleteStep(3, 1, "crlf-random-bug5-delete-1"),
        InsertStep(0, "\n\r\r\n", "crlf-random-bug5-insert-2"),
        InsertStep(4, "\n\r\r\n", "crlf-random-bug5-insert-3"),
        DeleteStep(4, 3, "crlf-random-bug5-delete-2"),
        InsertStep(5, "\r\r\n\r", "crlf-random-bug5-insert-4"),
        InsertStep(12, "\n\n\n\r", "crlf-random-bug5-insert-5"),
        InsertStep(5, "\r\r\r\n", "crlf-random-bug5-insert-6"),
        InsertStep(20, "\n\n\r\n", "crlf-random-bug5-insert-7"),
    };

    public static PieceTreeScriptStep[] CrlfRandomBug06 { get; } = new[]
    {
        InsertStep(0, "\n\r\r\n", "crlf-random-bug6-insert-1"),
        InsertStep(4, "\r\n\n\r", "crlf-random-bug6-insert-2"),
        InsertStep(3, "\r\n\n\n", "crlf-random-bug6-insert-3"),
        DeleteStep(4, 8, "crlf-random-bug6-delete-1"),
        InsertStep(4, "\r\n\n\r", "crlf-random-bug6-insert-4"),
        InsertStep(0, "\r\n\n\r", "crlf-random-bug6-insert-5"),
        DeleteStep(4, 0, "crlf-random-bug6-delete-2"),
        DeleteStep(8, 4, "crlf-random-bug6-delete-3"),
    };

    public static PieceTreeScriptStep[] CrlfRandomBug07 { get; } = new[]
    {
        InsertStep(0, "\r\r\n\n", "crlf-random-bug7-insert-1"),
        InsertStep(4, "\r\n\n\r", "crlf-random-bug7-insert-2"),
        InsertStep(7, "\n\r\r\r", "crlf-random-bug7-insert-3"),
        InsertStep(11, "\n\n\r\n", "crlf-random-bug7-insert-4"),
    };

    public static PieceTreeScriptStep[] CrlfRandomBug08 { get; } = new[]
    {
        InsertStep(0, "\r\n\n\r", "crlf-random-bug8-insert-1"),
        DeleteStep(1, 0, "crlf-random-bug8-delete-1"),
        InsertStep(3, "\n\n\n\r", "crlf-random-bug8-insert-2"),
        InsertStep(7, "\n\n\r\n", "crlf-random-bug8-insert-3"),
    };

    public static PieceTreeScriptStep[] CrlfRandomBug09 { get; } = new[]
    {
        InsertStep(0, "\n\n\n\n", "crlf-random-bug9-insert-1"),
        InsertStep(3, "\n\r\n\r", "crlf-random-bug9-insert-2"),
        InsertStep(2, "\n\r\n\n", "crlf-random-bug9-insert-3"),
        InsertStep(0, "\n\n\r\r", "crlf-random-bug9-insert-4"),
        InsertStep(3, "\r\r\r\r", "crlf-random-bug9-insert-5"),
        InsertStep(3, "\n\n\r\r", "crlf-random-bug9-insert-6"),
    };

    public static PieceTreeScriptStep[] CrlfRandomBug10 { get; } = new[]
    {
        InsertStep(0, "qneW", "crlf-random-bug10-insert-1"),
        InsertStep(0, "YhIl", "crlf-random-bug10-insert-2"),
        InsertStep(0, "qdsm", "crlf-random-bug10-insert-3"),
        DeleteStep(7, 0, "crlf-random-bug10-delete-1"),
        InsertStep(12, "iiPv", "crlf-random-bug10-insert-4"),
        InsertStep(9, "V\rSA", "crlf-random-bug10-insert-5"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomBug01 { get; } = new[]
    {
        InsertStep(1, "\r\n\r\n", "cls-random-bug1-insert-1"),
        DeleteStep(5, 3, "cls-random-bug1-delete-1"),
        DeleteStep(2, 3, "cls-random-bug1-delete-2"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomBug02 { get; } = new[]
    {
        InsertStep(2, "\n\r\r\r", "cls-random-bug2-insert-1"),
        DeleteStep(4, 1, "cls-random-bug2-delete-1"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomBug03 { get; } = new[]
    {
        DeleteStep(2, 2, "cls-random-bug3-delete-1"),
        DeleteStep(0, 2, "cls-random-bug3-delete-2"),
        InsertStep(0, "\r\r\r\r", "cls-random-bug3-insert-1"),
        InsertStep(2, "\r\n\r\r", "cls-random-bug3-insert-2"),
        InsertStep(3, "\r\r\r\n", "cls-random-bug3-insert-3"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomBug04 { get; } = new[]
    {
        DeleteStep(3, 1, "cls-random-bug4-delete-1"),
        InsertStep(1, "\r\r\r\r", "cls-random-bug4-insert-1"),
        InsertStep(6, "\r\n\n\r", "cls-random-bug4-insert-2"),
        DeleteStep(5, 3, "cls-random-bug4-delete-2"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomBug05 { get; } = new[]
    {
        DeleteStep(3, 1, "cls-random-bug5-delete-1"),
        InsertStep(0, "\n\r\r\n", "cls-random-bug5-insert-1"),
        InsertStep(4, "\n\r\r\n", "cls-random-bug5-insert-2"),
        DeleteStep(4, 3, "cls-random-bug5-delete-2"),
        InsertStep(5, "\r\r\n\r", "cls-random-bug5-insert-3"),
        InsertStep(12, "\n\n\n\r", "cls-random-bug5-insert-4"),
        InsertStep(5, "\r\r\r\n", "cls-random-bug5-insert-5"),
        InsertStep(20, "\n\n\r\n", "cls-random-bug5-insert-6"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomBug06 { get; } = new[]
    {
        InsertStep(4, "\r\n\n\r", "cls-random-bug6-insert-1"),
        InsertStep(3, "\r\n\n\n", "cls-random-bug6-insert-2"),
        DeleteStep(4, 8, "cls-random-bug6-delete-1"),
        InsertStep(4, "\r\n\n\r", "cls-random-bug6-insert-3"),
        InsertStep(0, "\r\n\n\r", "cls-random-bug6-insert-4"),
        DeleteStep(4, 0, "cls-random-bug6-delete-2"),
        DeleteStep(8, 4, "cls-random-bug6-delete-3"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomBug07 { get; } = new[]
    {
        DeleteStep(1, 0, "cls-random-bug7-delete-1"),
        InsertStep(3, "\n\n\n\r", "cls-random-bug7-insert-1"),
        InsertStep(7, "\n\n\r\n", "cls-random-bug7-insert-2"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomBug08 { get; } = new[]
    {
        InsertStep(4, "\r\n\n\r", "cls-random-bug8-insert-1"),
        InsertStep(7, "\n\r\r\r", "cls-random-bug8-insert-2"),
        InsertStep(11, "\n\n\r\n", "cls-random-bug8-insert-3"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomBug09 { get; } = new[]
    {
        InsertStep(0, "YhIl", "cls-random-bug9-insert-1"),
        InsertStep(0, "qdsm", "cls-random-bug9-insert-2"),
        DeleteStep(7, 0, "cls-random-bug9-delete-1"),
        InsertStep(12, "iiPv", "cls-random-bug9-insert-3"),
        InsertStep(9, "V\rSA", "cls-random-bug9-insert-4"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomBug10 { get; } = new[]
    {
        InsertStep(3, "\n\r\n\r", "cls-random-bug10-insert-1"),
        InsertStep(2, "\n\r\n\n", "cls-random-bug10-insert-2"),
        InsertStep(0, "\n\n\r\r", "cls-random-bug10-insert-3"),
        InsertStep(3, "\r\r\r\r", "cls-random-bug10-insert-4"),
        InsertStep(3, "\n\n\r\r", "cls-random-bug10-insert-5"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomChunkBug01 { get; } = new[]
    {
        DeleteStep(0, 2, "cls-random-chunk-bug1-delete-1"),
        InsertStep(1, "\r\r\n\n", "cls-random-chunk-bug1-insert-1"),
        InsertStep(7, "\r\r\r\r", "cls-random-chunk-bug1-insert-2"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomChunkBug02 { get; } = new[]
    {
        InsertStep(16, "\r\n\r\r", "cls-random-chunk-bug2-insert-1"),
        InsertStep(13, "\n\n\r\r", "cls-random-chunk-bug2-insert-2"),
        InsertStep(19, "\n\n\r\n", "cls-random-chunk-bug2-insert-3"),
        DeleteStep(5, 0, "cls-random-chunk-bug2-delete-1"),
        DeleteStep(11, 2, "cls-random-chunk-bug2-delete-2"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomChunkBug03 { get; } = new[]
    {
        InsertStep(4, "\n\n\r\n\r\r\n\n\r", "cls-random-chunk-bug3-insert-1"),
        DeleteStep(4, 4, "cls-random-chunk-bug3-delete-1"),
        InsertStep(11, "\r\n\r\n\n\r\r\n\n", "cls-random-chunk-bug3-insert-2"),
        DeleteStep(1, 2, "cls-random-chunk-bug3-delete-2"),
    };

    public static PieceTreeScriptStep[] CentralizedLineStartsRandomChunkBug04 { get; } = new[]
    {
        InsertStep(4, "\n\n\r\n", "cls-random-chunk-bug4-insert-1"),
        InsertStep(3, "\r\n\n\n", "cls-random-chunk-bug4-insert-2"),
    };

    #region Search offset cache (TS lines 1810-1884)

    public const string SearchOffsetRenderWhitespaceSeed = "class Name{\n\t\n\t\t\tget() {\n\n\t\t\t}\n\t\t}";
    public const string SearchOffsetNormalizedSeedWithoutTrailingLf = "abc";
    public const string SearchOffsetNormalizedSeedWithTrailingLf = "abc\n";

    public static PieceTreeScriptStep[] SearchOffsetRenderWhitespace { get; } = new[]
    {
        InsertStep(12, "s", "search-offset-render-step-01"),
        InsertStep(13, "e", "search-offset-render-step-02"),
        InsertStep(14, "t", "search-offset-render-step-03"),
        InsertStep(15, "()", "search-offset-render-step-04"),
        DeleteStep(16, 1, "search-offset-render-step-05"),
        InsertStep(17, "()", "search-offset-render-step-06"),
        DeleteStep(18, 1, "search-offset-render-step-07"),
        InsertStep(18, "}", "search-offset-render-step-08"),
        InsertStep(12, "\n", "search-offset-render-step-09"),
        DeleteStep(12, 1, "search-offset-render-step-10"),
        DeleteStep(18, 1, "search-offset-render-step-11"),
        InsertStep(18, "}", "search-offset-render-step-12"),
        DeleteStep(17, 2, "search-offset-render-step-13"),
        DeleteStep(16, 1, "search-offset-render-step-14"),
        InsertStep(16, ")", "search-offset-render-step-15"),
        DeleteStep(15, 2, "search-offset-render-step-16"),
    };

    public static PieceTreeScriptStep[] SearchOffsetNormalizedEolCase1 { get; } = new[]
    {
        InsertStep(3, "def\nabc", "search-offset-norm-case1-insert-1"),
    };

    public static PieceTreeScriptStep[] SearchOffsetNormalizedEolCase2 { get; } = new[]
    {
        InsertStep(4, "def\nabc", "search-offset-norm-case2-insert-1"),
    };

    public static PieceTreeScriptStep[] SearchOffsetNormalizedEolCase3 { get; } = new[]
    {
        InsertStep(2, "def\nabc", "search-offset-norm-case3-insert-1"),
    };

    public static PieceTreeScriptStep[] SearchOffsetNormalizedEolCase4 { get; } = new[]
    {
        InsertStep(3, "def\nabc", "search-offset-norm-case4-insert-1"),
    };

    #endregion
}
