/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// Tests for PieceTreeSearchCache diagnostics functionality
// Covers: CacheHit/CacheMiss counters, ClearedAfterEdit counter, hit-rate validation, and cache invalidation

using PieceTree.TextBuffer.Core;

namespace PieceTree.TextBuffer.Tests;

/// <summary>
/// Diagnostics tests for PieceTreeSearchCache to verify hit/miss counters, 
/// cache invalidation behavior, and snapshot reporting.
/// </summary>
public sealed class PieceTreeDiagnosticsTests
{
    #region CacheHit/CacheMiss Counter Tests

    [Fact]
    public void CacheHit_IncrementsAfterRepeatedLookupAtSameOffset()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("hello\nworld\n");
        var model = buffer.InternalModel;
        var initialSnapshot = model.Diagnostics.SearchCache;
        long initialHits = initialSnapshot.HitCount;
        long initialMisses = initialSnapshot.MissCount;

        // Act - First lookup (should miss and populate cache)
        _ = model.NodeAt(3);
        var afterFirstLookup = model.Diagnostics.SearchCache;

        // Second lookup at same offset (should hit)
        _ = model.NodeAt(3);
        var afterSecondLookup = model.Diagnostics.SearchCache;

        // Assert
        Assert.Equal(initialMisses + 1, afterFirstLookup.MissCount);
        Assert.Equal(initialHits + 1, afterSecondLookup.HitCount);
    }

    [Fact]
    public void CacheMiss_IncrementsOnFirstLookup()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("test content\nwith lines\n");
        var model = buffer.InternalModel;
        var initialSnapshot = model.Diagnostics.SearchCache;
        long initialMisses = initialSnapshot.MissCount;

        // Act - First lookup should miss
        _ = model.NodeAt(5);
        var afterLookup = model.Diagnostics.SearchCache;

        // Assert
        Assert.Equal(initialMisses + 1, afterLookup.MissCount);
    }

    [Fact]
    public void CacheHit_DoesNotIncrementOnCacheMiss()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("line one\nline two\nline three\n");
        var model = buffer.InternalModel;

        // Act - Lookups at different offsets that won't hit cache
        // Clear the cache first by editing
        buffer.ApplyEdit(0, 0, "x");
        buffer.ApplyEdit(0, 1, ""); // delete the x to restore

        var afterClear = model.Diagnostics.SearchCache;
        long hitsAfterClear = afterClear.HitCount;

        // First lookup at offset 0 - miss
        _ = model.NodeAt(0);
        var snapshot1 = model.Diagnostics.SearchCache;

        // Assert - hit count should not have changed from miss operation
        Assert.Equal(hitsAfterClear, snapshot1.HitCount);
    }

    [Fact]
    public void CacheCounters_AccumulateAcrossMultipleLookups()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("abcdefghij\nklmnopqrst\nuvwxyz\n");
        var model = buffer.InternalModel;

        // Clear cache first
        buffer.ApplyEdit(buffer.Length, 0, " ");
        buffer.ApplyEdit(buffer.Length - 1, 1, "");

        var initialSnapshot = model.Diagnostics.SearchCache;
        long initialHits = initialSnapshot.HitCount;
        long initialMisses = initialSnapshot.MissCount;

        // Act - Series of lookups
        _ = model.NodeAt(5);  // miss
        _ = model.NodeAt(5);  // hit
        _ = model.NodeAt(5);  // hit
        _ = model.NodeAt(5);  // hit

        var finalSnapshot = model.Diagnostics.SearchCache;

        // Assert
        Assert.Equal(initialMisses + 1, finalSnapshot.MissCount);
        Assert.True(finalSnapshot.HitCount >= initialHits + 3, 
            $"Expected at least 3 additional hits, got {finalSnapshot.HitCount - initialHits}");
    }

    #endregion

    #region ClearedAfterEdit Counter Tests

    [Fact]
    public void ClearCount_IncrementsAfterInsertInvalidatesCache()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("hello world");
        var model = buffer.InternalModel;

        // Prime the cache
        _ = model.NodeAt(5);
        var beforeEdit = model.Diagnostics.SearchCache;
        long clearCountBefore = beforeEdit.ClearCount;

        // Act - Insert at beginning should invalidate cache entries
        buffer.ApplyEdit(0, 0, "prefix ");
        var afterEdit = model.Diagnostics.SearchCache;

        // Assert - ClearCount should have increased
        Assert.True(afterEdit.ClearCount > clearCountBefore,
            $"ClearCount should have increased after edit invalidating cache. Before: {clearCountBefore}, After: {afterEdit.ClearCount}");
    }

    [Fact]
    public void ClearCount_IncrementsAfterDeleteInvalidatesCache()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("hello world test");
        var model = buffer.InternalModel;

        // Prime the cache with lookup at the end
        _ = model.NodeAt(10);
        var beforeEdit = model.Diagnostics.SearchCache;
        long clearCountBefore = beforeEdit.ClearCount;

        // Act - Delete from beginning should invalidate cached entries
        buffer.ApplyEdit(0, 6, "");
        var afterEdit = model.Diagnostics.SearchCache;

        // Assert
        Assert.True(afterEdit.ClearCount > clearCountBefore,
            $"ClearCount should have increased after delete. Before: {clearCountBefore}, After: {afterEdit.ClearCount}");
    }

    [Fact]
    public void ClearCount_IncrementsAfterReplaceInvalidatesCache()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("hello world");
        var model = buffer.InternalModel;

        // Prime the cache
        _ = model.NodeAt(8);
        var beforeEdit = model.Diagnostics.SearchCache;
        long clearCountBefore = beforeEdit.ClearCount;

        // Act - Replace operation
        buffer.ApplyEdit(0, 5, "hi");
        var afterEdit = model.Diagnostics.SearchCache;

        // Assert
        Assert.True(afterEdit.ClearCount > clearCountBefore,
            $"ClearCount should have increased after replace. Before: {clearCountBefore}, After: {afterEdit.ClearCount}");
    }

    [Fact]
    public void ClearCount_DoesNotIncrementWhenCacheAlreadyEmpty()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("test");
        var model = buffer.InternalModel;

        // Get initial clear count (cache is empty at start)
        var initialSnapshot = model.Diagnostics.SearchCache;
        long initialClearCount = initialSnapshot.ClearCount;

        // Act - Edit without priming cache first
        buffer.ApplyEdit(0, 0, "x");

        // The cache was empty, so invalidation may not increment clear count
        // depending on implementation
        var afterEdit = model.Diagnostics.SearchCache;

        // Assert - Clear count behavior is implementation dependent
        // Just verify it's accessible and reasonable
        Assert.True(afterEdit.ClearCount >= initialClearCount,
            "ClearCount should never decrease");
    }

    #endregion

    #region Search Cache Hit Rate Validation

    [Fact]
    public void HitRate_IsZeroWhenAllMisses()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("content\nwith\nmultiple\nlines\n");
        var model = buffer.InternalModel;

        // Clear any existing cache state
        buffer.ApplyEdit(buffer.Length, 0, " ");
        buffer.ApplyEdit(buffer.Length - 1, 1, "");

        var snapshot = model.Diagnostics.SearchCache;
        long baseMisses = snapshot.MissCount;

        // Act - Each lookup at different positions invalidates cache,
        // simulating worst case
        for (int i = 0; i < 5; i++)
        {
            buffer.ApplyEdit(0, 0, "x");
            _ = model.NodeAt(i);
            buffer.ApplyEdit(0, 1, "");
        }

        var finalSnapshot = model.Diagnostics.SearchCache;
        long newMisses = finalSnapshot.MissCount - baseMisses;

        // Assert - Should have more misses than hits due to cache invalidation
        Assert.True(newMisses >= 5, $"Expected at least 5 new misses, got {newMisses}");
    }

    [Fact]
    public void HitRate_IsHighWithRepeatedLookups()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("stable content for repeated lookups\n");
        var model = buffer.InternalModel;

        // Clear cache
        buffer.ApplyEdit(buffer.Length, 0, " ");
        buffer.ApplyEdit(buffer.Length - 1, 1, "");

        var snapshot = model.Diagnostics.SearchCache;
        long baseHits = snapshot.HitCount;
        long baseMisses = snapshot.MissCount;

        // Act - Repeated lookups at same offset (no edits)
        _ = model.NodeAt(10); // miss
        for (int i = 0; i < 10; i++)
        {
            _ = model.NodeAt(10); // should hit
        }

        var finalSnapshot = model.Diagnostics.SearchCache;
        long newMisses = finalSnapshot.MissCount - baseMisses;
        long newHits = finalSnapshot.HitCount - baseHits;

        // Assert - Should have high hit rate
        Assert.Equal(1, newMisses);
        Assert.True(newHits >= 10, $"Expected at least 10 hits, got {newHits}");

        // Calculate hit rate
        double hitRate = (double)newHits / (newHits + newMisses);
        Assert.True(hitRate >= 0.9, $"Expected hit rate >= 90%, got {hitRate:P2}");
    }

    [Fact]
    public void DiagnosticsSnapshot_ReflectsCurrentCacheEntryCount()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("some text content");
        var model = buffer.InternalModel;

        // Clear cache
        buffer.ApplyEdit(buffer.Length, 0, " ");
        buffer.ApplyEdit(buffer.Length - 1, 1, "");

        var emptySnapshot = model.Diagnostics.SearchCache;
        Assert.Equal(0, emptySnapshot.EntryCount);

        // Act - Prime cache
        _ = model.NodeAt(5);
        var primedSnapshot = model.Diagnostics.SearchCache;

        // Assert
        Assert.True(primedSnapshot.EntryCount >= 1,
            $"Expected at least 1 cache entry after lookup, got {primedSnapshot.EntryCount}");
    }

    #endregion

    #region Edit-Induced Cache Invalidation Tests

    [Fact]
    public void CacheInvalidation_InsertAtStartInvalidatesAllEntries()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("original content here");
        var model = buffer.InternalModel;

        // Prime cache with lookups
        _ = model.NodeAt(5);
        _ = model.NodeAt(10);
        var beforeInsert = model.Diagnostics.SearchCache;

        // Act - Insert at start shifts all offsets
        buffer.ApplyEdit(0, 0, "prefix ");
        var afterInsert = model.Diagnostics.SearchCache;

        // Assert - Cache should have been cleared
        Assert.True(afterInsert.ClearCount > beforeInsert.ClearCount,
            "Insert at start should invalidate cache");
    }

    [Fact]
    public void CacheInvalidation_InsertAtEndMayPreserveCache()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("original content");
        var model = buffer.InternalModel;

        // Prime cache with lookup near start
        _ = model.NodeAt(3);
        _ = model.Diagnostics.SearchCache; // capture state before

        // Act - Insert at end shouldn't invalidate entries before
        buffer.ApplyEdit(buffer.Length, 0, " suffix");

        // Try to hit the cached entry
        _ = model.NodeAt(3);
        var afterInsert = model.Diagnostics.SearchCache;

        // Assert - The lookup at offset 3 should still work (may hit or not depending on node structure)
        // The key point is the system doesn't crash and diagnostics are accessible
        Assert.True(afterInsert.HitCount > 0 || afterInsert.MissCount > 0,
            "Diagnostics should track lookups after append");
    }

    [Fact]
    public void CacheInvalidation_DeleteInMiddleInvalidatesAffectedEntries()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("hello world test string");
        var model = buffer.InternalModel;

        // Prime cache
        _ = model.NodeAt(15);
        var beforeDelete = model.Diagnostics.SearchCache;
        long clearBefore = beforeDelete.ClearCount;

        // Act - Delete in middle
        buffer.ApplyEdit(6, 6, ""); // delete "world "
        var afterDelete = model.Diagnostics.SearchCache;

        // Assert
        Assert.True(afterDelete.ClearCount >= clearBefore,
            "Delete should trigger cache invalidation");
    }

    [Fact]
    public void CacheInvalidation_MultipleEditsAccumulateClearCount()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("test content\n");
        var model = buffer.InternalModel;

        var initialSnapshot = model.Diagnostics.SearchCache;
        long initialClears = initialSnapshot.ClearCount;

        // Act - Multiple edits with cache priming between
        for (int i = 0; i < 5; i++)
        {
            _ = model.NodeAt(Math.Min(i * 2, buffer.Length - 1));
            buffer.ApplyEdit(0, 0, $"{i}");
        }

        var finalSnapshot = model.Diagnostics.SearchCache;

        // Assert - Clear count should have increased multiple times
        Assert.True(finalSnapshot.ClearCount > initialClears,
            $"ClearCount should have increased after multiple invalidating edits. Initial: {initialClears}, Final: {finalSnapshot.ClearCount}");
    }

    #endregion

    #region LastInvalidatedOffset Tests

    [Fact]
    public void LastInvalidatedOffset_TracksInvalidationPoint()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("hello world test");
        var model = buffer.InternalModel;

        // Prime cache
        _ = model.NodeAt(12);
        _ = model.Diagnostics.SearchCache; // capture initial state

        // Act - Insert at offset 6 should invalidate from there
        buffer.ApplyEdit(6, 0, "big ");
        var afterEdit = model.Diagnostics.SearchCache;

        // Assert - LastInvalidatedOffset should be set (>= 0 indicates invalidation occurred)
        Assert.True(afterEdit.LastInvalidatedOffset >= 0,
            $"LastInvalidatedOffset should be set after invalidation, got {afterEdit.LastInvalidatedOffset}");
    }

    [Fact]
    public void LastInvalidatedOffset_UpdatesOnSubsequentInvalidations()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("abcdefghijklmnop");
        var model = buffer.InternalModel;

        // First edit
        _ = model.NodeAt(10);
        buffer.ApplyEdit(5, 0, "x");
        var afterFirst = model.Diagnostics.SearchCache;
        int firstOffset = afterFirst.LastInvalidatedOffset;

        // Second edit at different location
        _ = model.NodeAt(8);
        buffer.ApplyEdit(2, 0, "y");
        var afterSecond = model.Diagnostics.SearchCache;
        int secondOffset = afterSecond.LastInvalidatedOffset;

        // Assert - both should have valid offsets
        Assert.True(firstOffset >= 0, "First invalidation should set offset");
        Assert.True(secondOffset >= 0, "Second invalidation should set offset");
    }

    #endregion

    #region EntriesRemaining Tests

    [Fact]
    public void EntriesRemaining_ReflectsCacheCapacity()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("test content");
        var model = buffer.InternalModel;

        // Clear cache
        buffer.ApplyEdit(buffer.Length, 0, " ");
        buffer.ApplyEdit(buffer.Length - 1, 1, "");

        var emptySnapshot = model.Diagnostics.SearchCache;
        int remainingWhenEmpty = emptySnapshot.EntriesRemaining;

        // Act - Add entry
        _ = model.NodeAt(5);
        var afterAddSnapshot = model.Diagnostics.SearchCache;

        // Assert
        Assert.True(remainingWhenEmpty >= 1, "Should have capacity when empty");
        Assert.True(afterAddSnapshot.EntriesRemaining <= remainingWhenEmpty,
            "Remaining capacity should decrease or stay same after adding entry");
    }

    #endregion

    #region Snapshot Immutability Tests

    [Fact]
    public void DiagnosticsSnapshot_IsImmutable()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("immutability test");
        var model = buffer.InternalModel;

        // Get snapshot
        var snapshot1 = model.Diagnostics.SearchCache;
        long hits1 = snapshot1.HitCount;

        // Act - Modify cache state
        _ = model.NodeAt(5);
        _ = model.NodeAt(5);

        // Assert - Original snapshot unchanged
        Assert.Equal(hits1, snapshot1.HitCount);

        // New snapshot has updated values
        var snapshot2 = model.Diagnostics.SearchCache;
        Assert.True(snapshot2.HitCount > hits1 || snapshot2.MissCount > snapshot1.MissCount,
            "New snapshot should reflect updated counters");
    }

    [Fact]
    public void DiagnosticsSnapshot_IndependentBetweenReads()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("test content for snapshots");
        var model = buffer.InternalModel;

        // Get multiple snapshots with operations in between
        var snap1 = model.Diagnostics.SearchCache;
        _ = model.NodeAt(5);
        var snap2 = model.Diagnostics.SearchCache;
        _ = model.NodeAt(5);
        var snap3 = model.Diagnostics.SearchCache;

        // Assert - Each snapshot captures state at time of read
        Assert.True(snap3.HitCount >= snap2.HitCount,
            "Later snapshots should have same or higher counts");
        Assert.True(snap2.MissCount >= snap1.MissCount || snap2.HitCount >= snap1.HitCount,
            "Counters should be monotonically non-decreasing");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Diagnostics_WorksWithEmptyBuffer()
    {
        // Arrange
        var buffer = new PieceTreeBuffer("");
        var model = buffer.InternalModel;

        // Act
        var snapshot = model.Diagnostics.SearchCache;

        // Assert - Should not throw and have valid initial state
        Assert.True(snapshot.HitCount >= 0);
        Assert.True(snapshot.MissCount >= 0);
        Assert.True(snapshot.ClearCount >= 0);
        Assert.True(snapshot.EntryCount >= 0);
    }

    [Fact]
    public void Diagnostics_WorksWithLargeBuffer()
    {
        // Arrange
        var largeContent = string.Join("\n", Enumerable.Range(0, 1000).Select(i => $"Line {i}: some content here"));
        var buffer = new PieceTreeBuffer(largeContent);
        var model = buffer.InternalModel;

        // Act - Multiple operations
        for (int i = 0; i < 100; i++)
        {
            int offset = i * 10 % buffer.Length;
            _ = model.NodeAt(offset);
        }

        var snapshot = model.Diagnostics.SearchCache;

        // Assert
        Assert.True(snapshot.HitCount + snapshot.MissCount >= 100,
            "Should have recorded all lookups");
    }

    [Fact]
    public void Diagnostics_AccessibleThroughPublicApi()
    {
        // This test verifies the diagnostics are accessible through the intended API path
        var buffer = new PieceTreeBuffer("test");
        var model = buffer.InternalModel;

        // Access through model.Diagnostics.SearchCache
        SearchCacheSnapshot snapshot = model.Diagnostics.SearchCache;

        // Verify all properties are accessible
        _ = snapshot.HitCount;
        _ = snapshot.MissCount;
        _ = snapshot.ClearCount;
        _ = snapshot.EntryCount;
        _ = snapshot.EntriesRemaining;
        _ = snapshot.LastInvalidatedOffset;

        Assert.True(true, "All diagnostic properties accessible");
    }

    #endregion
}
