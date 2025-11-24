/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Migrated from: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts

using System;
using PieceTree.TextBuffer;
using PieceTree.TextBuffer.Core;
using Xunit;

namespace PieceTree.TextBuffer.Tests.Helpers;

internal static class PieceTreeBufferAssertions
{
    public static void AssertPositions(PieceTreeFuzzHarness harness, params (int Offset, TextPosition Position)[] expectations)
    {
        foreach (var (offset, position) in expectations)
        {
            Assert.Equal(position, harness.GetPositionAt(offset));
        }
    }

    public static void AssertOffsets(PieceTreeFuzzHarness harness, params (TextPosition Position, int Offset)[] expectations)
    {
        foreach (var (position, expectedOffset) in expectations)
        {
            var actualOffset = harness.GetOffsetAt(position);
            Assert.Equal(expectedOffset, actualOffset);
        }
    }

    public static void AssertLineCount(PieceTreeFuzzHarness harness, int expectedLineCount)
    {
        Assert.Equal(expectedLineCount, harness.GetLineCount());
    }

    public static void AssertValueInRange(PieceTreeFuzzHarness harness, PieceTree.TextBuffer.Core.Range range, string expected)
    {
        var actual = harness.GetValueInRange(range);
        Assert.Equal(expected, actual);
    }

    public static void AssertLineContent(PieceTreeFuzzHarness harness, int lineNumber, string expectedContent)
    {
        var actual = harness.GetLineContent(lineNumber);
        Assert.Equal(expectedContent, actual);
    }

    public static void AssertState(PieceTreeFuzzHarness harness, string phase)
    {
        harness.AssertState(phase);
    }

    public static void AssertLineStarts(PieceTreeFuzzHarness harness, string phase)
    {
        var expectedStarts = LineStartBuilder.Build(harness.ExpectedText).LineStarts;
        for (var i = 0; i < expectedStarts.Count; i++)
        {
            var offset = expectedStarts[i];
            var position = harness.GetPositionAt(offset);
            Assert.Equal(i + 1, position.LineNumber);
            Assert.Equal(1, position.Column);
        }
    }

    public static void AssertSearchCachePrimed(PieceTreeFuzzHarness harness, string phase = "search-cache", params int[] offsets)
    {
        var model = harness.Buffer.InternalModel;
        var bufferLength = harness.Buffer.Length;
        var probes = offsets is { Length: > 0 }
            ? offsets
            : new[] { 0, bufferLength / 2, bufferLength };

        foreach (var probe in probes)
        {
            var clamped = Math.Clamp(probe, 0, Math.Max(0, bufferLength));
            var hit = model.NodeAt(clamped);
            if (hit.Node is null)
            {
                continue;
            }

            var cached = model.TryGetCachedNodeByOffset(clamped, out var cachedNode, out var cachedStartOffset);
            Assert.True(cached, $"Search cache did not contain an entry at offset {clamped} (phase: {phase})");
            Assert.Same(hit.Node, cachedNode);
            Assert.Equal(hit.NodeStartOffset, cachedStartOffset);
        }
    }
}
