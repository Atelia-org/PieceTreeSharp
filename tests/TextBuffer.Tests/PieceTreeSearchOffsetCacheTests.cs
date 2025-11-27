/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Migrated from: ts/src/vs/editor/test/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.test.ts (search offset cache suite)

using PieceTree.TextBuffer.Tests.Helpers;
using Xunit;
using static PieceTree.TextBuffer.Tests.Helpers.PieceTreeDeterministicScripts;
using static PieceTree.TextBuffer.Tests.Helpers.PieceTreeScript;

namespace PieceTree.TextBuffer.Tests;

/// <summary>
/// Deterministic coverage for the TS "search offset cache" suite (lines 1810-1884).
/// </summary>
public sealed class PieceTreeSearchOffsetCacheTests
{
    [Fact]
    public void RenderWhitespaceScriptPreservesSearchCache()
    {
        using PieceTreeFuzzHarness harness = CreateHarness(nameof(RenderWhitespaceScriptPreservesSearchCache), SearchOffsetRenderWhitespaceSeed);
        RunScript(harness, SearchOffsetRenderWhitespace);

        PieceTreeBufferAssertions.AssertState(harness, "search-offset-render-final");
        PieceTreeBufferAssertions.AssertLineStarts(harness, "search-offset-render-final");
        PieceTreeBufferAssertions.AssertSearchCachePrimed(harness, "search-offset-render-final");
    }

    [Fact]
    public void NormalizedInsert_AppendsWithoutTrailingLf_MaintainsCache()
    {
        using PieceTreeFuzzHarness harness = CreateHarness(nameof(NormalizedInsert_AppendsWithoutTrailingLf_MaintainsCache), SearchOffsetNormalizedSeedWithoutTrailingLf);
        RunScript(harness, SearchOffsetNormalizedEolCase1);

        PieceTreeBufferAssertions.AssertState(harness, "search-offset-norm-case1");
        PieceTreeBufferAssertions.AssertLineStarts(harness, "search-offset-norm-case1");
        PieceTreeBufferAssertions.AssertSearchCachePrimed(harness, "search-offset-norm-case1");
    }

    [Fact]
    public void NormalizedInsert_AppendsAfterTrailingLf_MaintainsCache()
    {
        using PieceTreeFuzzHarness harness = CreateHarness(nameof(NormalizedInsert_AppendsAfterTrailingLf_MaintainsCache), SearchOffsetNormalizedSeedWithTrailingLf);
        RunScript(harness, SearchOffsetNormalizedEolCase2);

        PieceTreeBufferAssertions.AssertState(harness, "search-offset-norm-case2");
        PieceTreeBufferAssertions.AssertLineStarts(harness, "search-offset-norm-case2");
        PieceTreeBufferAssertions.AssertSearchCachePrimed(harness, "search-offset-norm-case2");
    }

    [Fact]
    public void NormalizedInsert_WithinPrefix_MaintainsCache()
    {
        using PieceTreeFuzzHarness harness = CreateHarness(nameof(NormalizedInsert_WithinPrefix_MaintainsCache), SearchOffsetNormalizedSeedWithTrailingLf);
        RunScript(harness, SearchOffsetNormalizedEolCase3);

        PieceTreeBufferAssertions.AssertState(harness, "search-offset-norm-case3");
        PieceTreeBufferAssertions.AssertLineStarts(harness, "search-offset-norm-case3");
        PieceTreeBufferAssertions.AssertSearchCachePrimed(harness, "search-offset-norm-case3");
    }

    [Fact]
    public void NormalizedInsert_BeforeTrailingLf_MaintainsCache()
    {
        using PieceTreeFuzzHarness harness = CreateHarness(nameof(NormalizedInsert_BeforeTrailingLf_MaintainsCache), SearchOffsetNormalizedSeedWithTrailingLf);
        RunScript(harness, SearchOffsetNormalizedEolCase4);

        PieceTreeBufferAssertions.AssertState(harness, "search-offset-norm-case4");
        PieceTreeBufferAssertions.AssertLineStarts(harness, "search-offset-norm-case4");
        PieceTreeBufferAssertions.AssertSearchCachePrimed(harness, "search-offset-norm-case4");
    }

    private static PieceTreeFuzzHarness CreateHarness(string testName, string initialText)
    {
        return new PieceTreeFuzzHarness(testName, initialText: initialText);
    }
}
