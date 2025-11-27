/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Migrated from: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts

using PieceTree.TextBuffer.Tests.Helpers;
using static PieceTree.TextBuffer.Tests.Helpers.PieceTreeDeterministicScripts;
using static PieceTree.TextBuffer.Tests.Helpers.PieceTreeScript;
using Range = PieceTree.TextBuffer.Core.Range;

namespace PieceTree.TextBuffer.Tests;

/// <summary>
/// Ports deterministic suites from ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts.
/// </summary>
public sealed class PieceTreeDeterministicTests
{
    #region Prefix sum for line feed (TS lines ~560-720)

    [Fact]
    public void PrefixSumBasicMatchesTsExpectations()
    {
        using PieceTreeFuzzHarness harness = new(nameof(PrefixSumBasicMatchesTsExpectations), initialText: "1\n2\n3\n4");
        PieceTreeBufferAssertions.AssertLineCount(harness, 4);
        PieceTreeBufferAssertions.AssertPositions(
            harness,
            (0, new TextPosition(1, 1)),
            (1, new TextPosition(1, 2)),
            (2, new TextPosition(2, 1)),
            (3, new TextPosition(2, 2)),
            (4, new TextPosition(3, 1)),
            (5, new TextPosition(3, 2)),
            (6, new TextPosition(4, 1)));
        PieceTreeBufferAssertions.AssertOffsets(
            harness,
            (new TextPosition(1, 1), 0),
            (new TextPosition(1, 2), 1),
            (new TextPosition(2, 1), 2),
            (new TextPosition(2, 2), 3),
            (new TextPosition(3, 1), 4),
            (new TextPosition(3, 2), 5),
            (new TextPosition(4, 1), 6));
        harness.AssertState("prefix-sum-basic");
    }

    [Fact]
    public void PrefixSumAppendMatchesTsExpectations()
    {
        using PieceTreeFuzzHarness harness = new(nameof(PrefixSumAppendMatchesTsExpectations), initialText: "a\nb\nc\nde");
        harness.Insert(8, "fh\ni\njk", "prefix-sum-append-insert");

        PieceTreeBufferAssertions.AssertLineCount(harness, 6);
        PieceTreeBufferAssertions.AssertPositions(harness, (9, new TextPosition(4, 4)));
        PieceTreeBufferAssertions.AssertOffsets(harness, (new TextPosition(1, 1), 0));
    }

    [Fact]
    public void PrefixSumInsertMatchesTsExpectations()
    {
        using PieceTreeFuzzHarness harness = new(nameof(PrefixSumInsertMatchesTsExpectations), initialText: "a\nb\nc\nde");
        harness.Insert(7, "fh\ni\njk", "prefix-sum-insert");

        PieceTreeBufferAssertions.AssertLineCount(harness, 6);
        PieceTreeBufferAssertions.AssertPositions(
            harness,
            (6, new TextPosition(4, 1)),
            (7, new TextPosition(4, 2)),
            (8, new TextPosition(4, 3)),
            (9, new TextPosition(4, 4)),
            (12, new TextPosition(6, 1)),
            (13, new TextPosition(6, 2)),
            (14, new TextPosition(6, 3)));
        PieceTreeBufferAssertions.AssertOffsets(
            harness,
            (new TextPosition(4, 1), 6),
            (new TextPosition(4, 2), 7),
            (new TextPosition(4, 3), 8),
            (new TextPosition(4, 4), 9),
            (new TextPosition(6, 1), 12),
            (new TextPosition(6, 2), 13),
            (new TextPosition(6, 3), 14));
    }

    [Fact]
    public void PrefixSumDeleteMatchesTsExpectations()
    {
        using PieceTreeFuzzHarness harness = new(nameof(PrefixSumDeleteMatchesTsExpectations), initialText: "a\nb\nc\ndefh\ni\njk");
        harness.Delete(7, 2, "prefix-sum-delete");

        Assert.Equal("a\nb\nc\ndh\ni\njk", harness.Buffer.GetText());
        PieceTreeBufferAssertions.AssertLineCount(harness, 6);
        AssertDhState(harness);
    }

    [Fact]
    public void PrefixSumAddDeleteSequenceMatchesTsExpectations()
    {
        using PieceTreeFuzzHarness harness = new(nameof(PrefixSumAddDeleteSequenceMatchesTsExpectations), initialText: "a\nb\nc\nde");
        harness.Insert(8, "fh\ni\njk", "prefix-sum-add-delete-insert");
        harness.Delete(7, 2, "prefix-sum-add-delete-delete");

        Assert.Equal("a\nb\nc\ndh\ni\njk", harness.Buffer.GetText());
        PieceTreeBufferAssertions.AssertLineCount(harness, 6);
        AssertDhState(harness);
    }

    [Fact]
    public void PrefixSumInsertRandomBugOneMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = new(nameof(PrefixSumInsertRandomBugOneMatchesTsScript), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, " ZX \n Z\nZ\n YZ\nY\nZXX ", "prefix-sum-insert-bug1-step1"),
            InsertStep(14, "X ZZ\nYZZYZXXY Y XY\n ", "prefix-sum-insert-bug1-step2"));
    }

    [Fact]
    public void PrefixSumInsertRandomBugTwoMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = new(nameof(PrefixSumInsertRandomBugTwoMatchesTsScript), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "ZYZ\nYY XY\nX \nZ Y \nZ ", "prefix-sum-insert-bug2-step1"),
            InsertStep(3, "XXY \n\nY Y YYY  ZYXY ", "prefix-sum-insert-bug2-step2"));
    }

    [Fact]
    public void PrefixSumDeleteRandomBugOneMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = new(nameof(PrefixSumDeleteRandomBugOneMatchesTsScript), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "ba\na\nca\nba\ncbab\ncaa ", "prefix-sum-delete-bug1-1"),
            InsertStep(13, "cca\naabb\ncac\nccc\nab ", "prefix-sum-delete-bug1-2"),
            DeleteStep(5, 8, "prefix-sum-delete-bug1-3"),
            DeleteStep(30, 2, "prefix-sum-delete-bug1-4"),
            InsertStep(24, "cbbacccbac\nbaaab\n\nc ", "prefix-sum-delete-bug1-5"),
            DeleteStep(29, 3, "prefix-sum-delete-bug1-6"),
            DeleteStep(23, 9, "prefix-sum-delete-bug1-7"),
            DeleteStep(21, 5, "prefix-sum-delete-bug1-8"),
            DeleteStep(30, 3, "prefix-sum-delete-bug1-9"),
            InsertStep(3, "cb\nac\nc\n\nacc\nbb\nb\nc ", "prefix-sum-delete-bug1-10"),
            DeleteStep(19, 5, "prefix-sum-delete-bug1-11"),
            InsertStep(18, "\nbb\n\nacbc\ncbb\nc\nbb\n ", "prefix-sum-delete-bug1-12"),
            InsertStep(65, "cbccbac\nbc\n\nccabba\n ", "prefix-sum-delete-bug1-13"),
            InsertStep(77, "a\ncacb\n\nac\n\n\n\n\nabab ", "prefix-sum-delete-bug1-14"),
            DeleteStep(30, 9, "prefix-sum-delete-bug1-15"),
            InsertStep(45, "b\n\nc\nba\n\nbbbba\n\naa\n ", "prefix-sum-delete-bug1-16"),
            InsertStep(82, "ab\nbb\ncabacab\ncbc\na ", "prefix-sum-delete-bug1-17"),
            DeleteStep(123, 9, "prefix-sum-delete-bug1-18"),
            DeleteStep(71, 2, "prefix-sum-delete-bug1-19"),
            InsertStep(33, "acaa\nacb\n\naa\n\nc\n\n\n\n ", "prefix-sum-delete-bug1-20"));
    }

    [Fact]
    public void PrefixSumDeleteRandomBugRbTreeOneMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = new(nameof(PrefixSumDeleteRandomBugRbTreeOneMatchesTsScript), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "YXXZ\n\nYY\n", "prefix-sum-delete-rbtree1-1"),
            DeleteStep(0, 5, "prefix-sum-delete-rbtree1-2"),
            InsertStep(0, "ZXYY\nX\nZ\n", "prefix-sum-delete-rbtree1-3"),
            InsertStep(10, "\nXY\nYXYXY", "prefix-sum-delete-rbtree1-4"));
    }

    [Fact]
    public void PrefixSumDeleteRandomBugRbTreeTwoMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = new(nameof(PrefixSumDeleteRandomBugRbTreeTwoMatchesTsScript), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "YXXZ\n\nYY\n", "prefix-sum-delete-rbtree2-1"),
            InsertStep(0, "ZXYY\nX\nZ\n", "prefix-sum-delete-rbtree2-2"),
            InsertStep(10, "\nXY\nYXYXY", "prefix-sum-delete-rbtree2-3"),
            InsertStep(8, "YZXY\nZ\nYX", "prefix-sum-delete-rbtree2-4"),
            InsertStep(12, "XX\nXXYXYZ", "prefix-sum-delete-rbtree2-5"),
            DeleteStep(0, 4, "prefix-sum-delete-rbtree2-6"));
    }

    [Fact]
    public void PrefixSumDeleteRandomBugRbTreeThreeMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = new(nameof(PrefixSumDeleteRandomBugRbTreeThreeMatchesTsScript), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "YXXZ\n\nYY\n", "prefix-sum-delete-rbtree3-1"),
            DeleteStep(7, 2, "prefix-sum-delete-rbtree3-2"),
            DeleteStep(6, 1, "prefix-sum-delete-rbtree3-3"),
            DeleteStep(0, 5, "prefix-sum-delete-rbtree3-4"),
            InsertStep(0, "ZXYY\nX\nZ\n", "prefix-sum-delete-rbtree3-5"),
            InsertStep(10, "\nXY\nYXYXY", "prefix-sum-delete-rbtree3-6"),
            InsertStep(8, "YZXY\nZ\nYX", "prefix-sum-delete-rbtree3-7"),
            InsertStep(12, "XX\nXXYXYZ", "prefix-sum-delete-rbtree3-8"),
            DeleteStep(0, 4, "prefix-sum-delete-rbtree3-9"),
            DeleteStep(30, 3, "prefix-sum-delete-rbtree3-10"));
    }

    #endregion

    #region Offset 2 position (TS lines ~720-760)

    [Fact]
    public void OffsetToPositionRandomBugOneMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = new(nameof(OffsetToPositionRandomBugOneMatchesTsScript), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "huuyYzUfKOENwGgZLqn ", "offset2pos-bug1-1"),
            DeleteStep(18, 2, "offset2pos-bug1-2"),
            DeleteStep(3, 1, "offset2pos-bug1-3"),
            DeleteStep(12, 4, "offset2pos-bug1-4"),
            InsertStep(3, "hMbnVEdTSdhLlPevXKF ", "offset2pos-bug1-5"),
            DeleteStep(22, 8, "offset2pos-bug1-6"),
            InsertStep(4, "S umSnYrqOmOAV\nEbZJ ", "offset2pos-bug1-7"));
    }

    #endregion

    #region Get text in range (TS lines ~760-940)

    [Fact]
    public void GetTextInRangeReturnsExpectedSegments()
    {
        using PieceTreeFuzzHarness harness = new(nameof(GetTextInRangeReturnsExpectedSegments), initialText: "a\nb\nc\nde");
        harness.Insert(8, "fh\ni\njk", "range-basic-insert");
        harness.Delete(7, 2, "range-basic-delete");

        PieceTreeBufferAssertions.AssertValueInRange(harness, new Range(new TextPosition(1, 1), new TextPosition(1, 3)), "a\n");
        PieceTreeBufferAssertions.AssertValueInRange(harness, new Range(new TextPosition(2, 1), new TextPosition(2, 3)), "b\n");
        PieceTreeBufferAssertions.AssertValueInRange(harness, new Range(new TextPosition(3, 1), new TextPosition(3, 3)), "c\n");
        PieceTreeBufferAssertions.AssertValueInRange(harness, new Range(new TextPosition(4, 1), new TextPosition(4, 4)), "dh\n");
        PieceTreeBufferAssertions.AssertValueInRange(harness, new Range(new TextPosition(5, 1), new TextPosition(5, 3)), "i\n");
        PieceTreeBufferAssertions.AssertValueInRange(harness, new Range(new TextPosition(6, 1), new TextPosition(6, 3)), "jk");
    }

    [Fact]
    public void GetTextInRangeRandomValueSequence()
    {
        using PieceTreeFuzzHarness harness = new(nameof(GetTextInRangeRandomValueSequence), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "ZXXY", "range-random-1"),
            InsertStep(1, "XZZY", "range-random-2"),
            InsertStep(5, "\nX\n\n", "range-random-3"),
            InsertStep(3, "\nXX\n", "range-random-4"),
            InsertStep(12, "YYYX", "range-random-5"));
    }

    [Fact]
    public void GetTextInRangeHandlesEmptyRange()
    {
        using PieceTreeFuzzHarness harness = new(nameof(GetTextInRangeHandlesEmptyRange), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "XZ\nZ", "range-empty-1"),
            DeleteStep(0, 3, "range-empty-2"),
            DeleteStep(0, 1, "range-empty-3"),
            InsertStep(0, "ZYX\n", "range-empty-4"),
            DeleteStep(0, 4, "range-empty-5"));

        string value = harness.GetValueInRange(new Range(TextPosition.Origin, TextPosition.Origin));
        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void GetTextInRangeRandomBugOneMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = new(nameof(GetTextInRangeRandomBugOneMatchesTsScript), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "huuyYzUfKOENwGgZLqn ", "range-bug1-1"),
            DeleteStep(18, 2, "range-bug1-2"),
            DeleteStep(3, 1, "range-bug1-3"),
            DeleteStep(12, 4, "range-bug1-4"),
            InsertStep(3, "hMbnVEdTSdhLlPevXKF ", "range-bug1-5"),
            DeleteStep(22, 8, "range-bug1-6"),
            InsertStep(4, "S umSnYrqOmOAV\nEbZJ ", "range-bug1-7"));
    }

    [Fact]
    public void GetTextInRangeRandomBugTwoMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = new(nameof(GetTextInRangeRandomBugTwoMatchesTsScript), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "xfouRDZwdAHjVXJAMV\n ", "range-bug2-1"),
            InsertStep(16, "dBGndxpFZBEAIKykYYx ", "range-bug2-2"),
            DeleteStep(7, 6, "range-bug2-3"),
            DeleteStep(9, 7, "range-bug2-4"),
            DeleteStep(17, 6, "range-bug2-5"),
            DeleteStep(0, 4, "range-bug2-6"),
            InsertStep(9, "qvEFXCNvVkWgvykahYt ", "range-bug2-7"),
            DeleteStep(4, 6, "range-bug2-8"),
            InsertStep(11, "OcSChUYT\nzPEBOpsGmR ", "range-bug2-9"),
            InsertStep(15, "KJCozaXTvkE\nxnqAeTz ", "range-bug2-10"));
    }

    [Fact]
    public void GetLineRawContentSingleLineMatchesTsExpectations()
    {
        using PieceTreeFuzzHarness harness = new(nameof(GetLineRawContentSingleLineMatchesTsExpectations), initialText: "1");
        Assert.Equal("1", harness.Buffer.InternalModel.GetLineRawContent(1));

        harness.Insert(1, "2", "line-content-single-insert");
        Assert.Equal("12", harness.Buffer.InternalModel.GetLineRawContent(1));
    }

    [Fact]
    public void GetLineRawContentMultipleLinesMatchesTsExpectations()
    {
        using PieceTreeFuzzHarness harness = new(nameof(GetLineRawContentMultipleLinesMatchesTsExpectations), initialText: "1\n2\n3\n4");
        Assert.Equal("1\n", harness.Buffer.InternalModel.GetLineRawContent(1));
        Assert.Equal("2\n", harness.Buffer.InternalModel.GetLineRawContent(2));
        Assert.Equal("3\n", harness.Buffer.InternalModel.GetLineRawContent(3));
        Assert.Equal("4", harness.Buffer.InternalModel.GetLineRawContent(4));
    }

    [Fact]
    public void GetLineRawContentAfterMutationsMatchesTsExpectations()
    {
        using PieceTreeFuzzHarness harness = new(nameof(GetLineRawContentAfterMutationsMatchesTsExpectations), initialText: "a\nb\nc\nde");
        harness.Insert(8, "fh\ni\njk", "line-content-after-insert");
        harness.Delete(7, 2, "line-content-after-delete");

        Assert.Equal("a\n", harness.Buffer.InternalModel.GetLineRawContent(1));
        Assert.Equal("b\n", harness.Buffer.InternalModel.GetLineRawContent(2));
        Assert.Equal("c\n", harness.Buffer.InternalModel.GetLineRawContent(3));
        Assert.Equal("dh\n", harness.Buffer.InternalModel.GetLineRawContent(4));
        Assert.Equal("i\n", harness.Buffer.InternalModel.GetLineRawContent(5));
        Assert.Equal("jk", harness.Buffer.InternalModel.GetLineRawContent(6));
    }

    [Fact]
    public void GetTextInRangeRandomOneMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = new(nameof(GetTextInRangeRandomOneMatchesTsScript), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "J eNnDzQpnlWyjmUu\ny ", "range-random-one-1"),
            InsertStep(0, "QPEeRAQmRwlJqtZSWhQ ", "range-random-one-2"),
            DeleteStep(5, 1, "range-random-one-3"));
    }

    [Fact]
    public void GetTextInRangeRandomTwoMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = new(nameof(GetTextInRangeRandomTwoMatchesTsScript), initialText: string.Empty);
        RunScript(
            harness,
            InsertStep(0, "DZoQ tglPCRHMltejRI ", "range-random-two-1"),
            InsertStep(10, "JRXiyYqJ qqdcmbfkKX ", "range-random-two-2"),
            DeleteStep(16, 3, "range-random-two-3"),
            DeleteStep(25, 1, "range-random-two-4"),
            InsertStep(18, "vH\nNlvfqQJPm\nSFkhMc ", "range-random-two-5"));
    }

    #endregion

    #region CRLF normalization (TS lines 1054-1292)

    // These deterministic delete tests intentionally overlap with PieceTreeNormalizationTests
    // so the CRLF suite remains contiguous with the original TS ordering.
    [Fact]
    public void CrlfDeleteCrInCrlfOneMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfDeleteCrInCrlfOneMatchesTsScript));
        harness.Insert(0, "a\r\nb", "crlf-delete-1-insert");
        harness.Delete(0, 2, "crlf-delete-1-delete");
        PieceTreeBufferAssertions.AssertLineCount(harness, 2);
        harness.AssertState("crlf-delete-1-final");
    }

    [Fact]
    public void CrlfDeleteCrInCrlfTwoMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfDeleteCrInCrlfTwoMatchesTsScript));
        harness.Insert(0, "a\r\nb", "crlf-delete-2-insert");
        harness.Delete(2, 2, "crlf-delete-2-delete");
        PieceTreeBufferAssertions.AssertLineCount(harness, 2);
        harness.AssertState("crlf-delete-2-final");
    }

    [Fact]
    public void CrlfRandomBug01MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfRandomBug01MatchesTsScript));
        RunScript(harness, CrlfRandomBug01);
        harness.AssertState("crlf-random-bug-01-final");
    }

    [Fact]
    public void CrlfRandomBug02MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfRandomBug02MatchesTsScript));
        RunScript(harness, CrlfRandomBug02);
        harness.AssertState("crlf-random-bug-02-final");
    }

    [Fact]
    public void CrlfRandomBug03MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfRandomBug03MatchesTsScript));
        RunScript(harness, CrlfRandomBug03);
        harness.AssertState("crlf-random-bug-03-final");
    }

    [Fact]
    public void CrlfRandomBug04MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfRandomBug04MatchesTsScript));
        RunScript(harness, CrlfRandomBug04);
        harness.AssertState("crlf-random-bug-04-final");
    }

    [Fact]
    public void CrlfRandomBug05MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfRandomBug05MatchesTsScript));
        RunScript(harness, CrlfRandomBug05);
        harness.AssertState("crlf-random-bug-05-final");
    }

    [Fact]
    public void CrlfRandomBug06MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfRandomBug06MatchesTsScript));
        RunScript(harness, CrlfRandomBug06);
        harness.AssertState("crlf-random-bug-06-final");
    }

    [Fact]
    public void CrlfRandomBug07MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfRandomBug07MatchesTsScript));
        RunScript(harness, CrlfRandomBug07);
        harness.AssertState("crlf-random-bug-07-final");
    }

    [Fact]
    public void CrlfRandomBug08MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfRandomBug08MatchesTsScript));
        RunScript(harness, CrlfRandomBug08);
        harness.AssertState("crlf-random-bug-08-final");
    }

    [Fact]
    public void CrlfRandomBug09MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfRandomBug09MatchesTsScript));
        RunScript(harness, CrlfRandomBug09);
        harness.AssertState("crlf-random-bug-09-final");
    }

    [Fact]
    public void CrlfRandomBug10MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateCrlfHarness(nameof(CrlfRandomBug10MatchesTsScript));
        RunScript(harness, CrlfRandomBug10);
        harness.AssertState("crlf-random-bug-10-final");
    }

    #endregion

    #region Centralized lineStarts with CRLF (TS lines 1294-1589)

    [Fact]
    public void CentralizedLineStartsDeleteCrlfOneMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsDeleteCrlfOneMatchesTsScript), normalizeChunks: false, "a\r\nb");
        harness.Delete(2, 2, "cls-delete-crlf-1");
        PieceTreeBufferAssertions.AssertLineCount(harness, 2);
        harness.AssertState("cls-delete-crlf-1-final");
    }

    [Fact]
    public void CentralizedLineStartsDeleteCrlfTwoMatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsDeleteCrlfTwoMatchesTsScript), normalizeChunks: true, "a\r\nb");
        harness.Delete(0, 2, "cls-delete-crlf-2");
        PieceTreeBufferAssertions.AssertLineCount(harness, 2);
        harness.AssertState("cls-delete-crlf-2-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomBug01MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomBug01MatchesTsScript), normalizeChunks: false, "\n\n\r\r");
        RunScript(harness, CentralizedLineStartsRandomBug01);
        harness.AssertState("cls-random-bug-01-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomBug02MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomBug02MatchesTsScript), normalizeChunks: false, "\n\r\n\r");
        RunScript(harness, CentralizedLineStartsRandomBug02);
        harness.AssertState("cls-random-bug-02-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomBug03MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomBug03MatchesTsScript), normalizeChunks: false, "\n\n\n\r");
        RunScript(harness, CentralizedLineStartsRandomBug03);
        harness.AssertState("cls-random-bug-03-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomBug04MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomBug04MatchesTsScript), normalizeChunks: false, "\n\n\n\n");
        RunScript(harness, CentralizedLineStartsRandomBug04);
        harness.AssertState("cls-random-bug-04-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomBug05MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomBug05MatchesTsScript), normalizeChunks: false, "\n\n\n\n");
        RunScript(harness, CentralizedLineStartsRandomBug05);
        harness.AssertState("cls-random-bug-05-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomBug06MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomBug06MatchesTsScript), normalizeChunks: false, "\n\r\r\n");
        RunScript(harness, CentralizedLineStartsRandomBug06);
        harness.AssertState("cls-random-bug-06-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomBug07MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomBug07MatchesTsScript), normalizeChunks: false, "\r\n\n\r");
        RunScript(harness, CentralizedLineStartsRandomBug07);
        harness.AssertState("cls-random-bug-07-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomBug08MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomBug08MatchesTsScript), normalizeChunks: false, "\r\r\n\n");
        RunScript(harness, CentralizedLineStartsRandomBug08);
        harness.AssertState("cls-random-bug-08-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomBug09MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomBug09MatchesTsScript), normalizeChunks: false, "qneW");
        RunScript(harness, CentralizedLineStartsRandomBug09);
        harness.AssertState("cls-random-bug-09-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomBug10MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomBug10MatchesTsScript), normalizeChunks: false, "\n\n\n\n");
        RunScript(harness, CentralizedLineStartsRandomBug10);
        harness.AssertState("cls-random-bug-10-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomChunkBug01MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomChunkBug01MatchesTsScript), normalizeChunks: false, "\n\r\r\n\n\n\r\n\r");
        string expected = RunScriptWithMirror(harness, CentralizedLineStartsRandomChunkBug01);
        AssertFinalText(harness, expected, "cls-random-chunk-bug-01-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomChunkBug02MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomChunkBug02MatchesTsScript), normalizeChunks: false, "\n\r\n\n\n\r\n\r\n\r\r\n\n\n\r\r\n\r\n");
        string expected = RunScriptWithMirror(harness, CentralizedLineStartsRandomChunkBug02);
        AssertFinalText(harness, expected, "cls-random-chunk-bug-02-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomChunkBug03MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomChunkBug03MatchesTsScript), normalizeChunks: false, "\r\n\n\n\n\n\n\r\n");
        string expected = RunScriptWithMirror(harness, CentralizedLineStartsRandomChunkBug03);
        AssertFinalText(harness, expected, "cls-random-chunk-bug-03-final");
    }

    [Fact]
    public void CentralizedLineStartsRandomChunkBug04MatchesTsScript()
    {
        using PieceTreeFuzzHarness harness = CreateHarnessFromChunks(nameof(CentralizedLineStartsRandomChunkBug04MatchesTsScript), normalizeChunks: false, "\n\r\n\r");
        string expected = RunScriptWithMirror(harness, CentralizedLineStartsRandomChunkBug04);
        AssertFinalText(harness, expected, "cls-random-chunk-bug-04-final");
    }

    #endregion

    private static void AssertDhState(PieceTreeFuzzHarness harness)
    {
        PieceTreeBufferAssertions.AssertPositions(
            harness,
            (6, new TextPosition(4, 1)),
            (7, new TextPosition(4, 2)),
            (8, new TextPosition(4, 3)),
            (9, new TextPosition(5, 1)),
            (11, new TextPosition(6, 1)),
            (12, new TextPosition(6, 2)),
            (13, new TextPosition(6, 3)));
        PieceTreeBufferAssertions.AssertOffsets(
            harness,
            (new TextPosition(4, 1), 6),
            (new TextPosition(4, 2), 7),
            (new TextPosition(4, 3), 8),
            (new TextPosition(5, 1), 9),
            (new TextPosition(6, 1), 11),
            (new TextPosition(6, 2), 12),
            (new TextPosition(6, 3), 13));
    }

    private static PieceTreeFuzzHarness CreateCrlfHarness(string testName)
    {
        return new PieceTreeFuzzHarness(testName, new[] { string.Empty }, normalizeChunks: false);
    }

    private static PieceTreeFuzzHarness CreateHarnessFromChunks(string testName, bool normalizeChunks, params string[] chunks)
    {
        return new PieceTreeFuzzHarness(testName, chunks, normalizeChunks);
    }

    private static void AssertFinalText(PieceTreeFuzzHarness harness, string expected, string phase)
    {
        Assert.Equal(expected, harness.Buffer.GetText());
        harness.AssertState(phase);
    }
}
