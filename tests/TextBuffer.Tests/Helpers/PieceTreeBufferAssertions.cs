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
        foreach ((int offset, TextPosition position) in expectations)
        {
            Assert.Equal(position, harness.GetPositionAt(offset));
        }
    }

    public static void AssertOffsets(PieceTreeFuzzHarness harness, params (TextPosition Position, int Offset)[] expectations)
    {
        foreach ((TextPosition position, int expectedOffset) in expectations)
        {
            int actualOffset = harness.GetOffsetAt(position);
            Assert.Equal(expectedOffset, actualOffset);
        }
    }

    public static void AssertLineCount(PieceTreeFuzzHarness harness, int expectedLineCount)
    {
        Assert.Equal(expectedLineCount, harness.GetLineCount());
    }

    public static void AssertValueInRange(PieceTreeFuzzHarness harness, PieceTree.TextBuffer.Core.Range range, string expected)
    {
        string actual = harness.GetValueInRange(range);
        Assert.Equal(expected, actual);
    }

    public static void AssertLineContent(PieceTreeFuzzHarness harness, int lineNumber, string expectedContent)
    {
        string actual = harness.GetLineContent(lineNumber);
        Assert.Equal(expectedContent, actual);
    }

    public static void AssertState(PieceTreeFuzzHarness harness, string phase)
    {
        harness.AssertState(phase);
    }

    public static void AssertLineStarts(PieceTreeFuzzHarness harness, string phase)
    {
        IReadOnlyList<int> expectedStarts = LineStartBuilder.Build(harness.ExpectedText).LineStarts;
        for (int i = 0; i < expectedStarts.Count; i++)
        {
            int offset = expectedStarts[i];
            TextPosition position = harness.GetPositionAt(offset);
            Assert.Equal(i + 1, position.LineNumber);
            Assert.Equal(1, position.Column);
        }
    }

    public static void AssertSearchCachePrimed(PieceTreeFuzzHarness harness, string phase = "search-cache", params int[] offsets)
    {
        PieceTreeModel model = harness.Buffer.InternalModel;
        int bufferLength = harness.Buffer.Length;
        int[] probes = offsets is { Length: > 0 }
            ? offsets
            : [0, bufferLength / 2, bufferLength];

        foreach (int probe in probes)
        {
            int clamped = Math.Clamp(probe, 0, Math.Max(0, bufferLength));
            NodeHit hit = model.NodeAt(clamped);
            if (hit.Node is null)
            {
                continue;
            }

            bool cached = model.TryGetCachedNodeByOffset(clamped, out PieceTreeNode? cachedNode, out int cachedStartOffset);
            Assert.True(cached, $"Search cache did not contain an entry at offset {clamped} (phase: {phase})");
            Assert.Same(hit.Node, cachedNode);
            Assert.Equal(hit.NodeStartOffset, cachedStartOffset);
        }
    }
}
