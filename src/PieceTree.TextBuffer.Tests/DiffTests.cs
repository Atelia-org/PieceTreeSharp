using System;
using System.Collections.Generic;
using Xunit;
using PieceTree.TextBuffer.Diff;

namespace PieceTree.TextBuffer.Tests
{
    public class DiffTests
    {
        [Fact]
        public void TestInsert()
        {
            var original = "abc";
            var modified = "abxc";
            var changes = DiffComputer.ComputeDiff(original, modified);
            
            Assert.Single(changes);
            Assert.Equal(2, changes[0].OriginalStart);
            Assert.Equal(0, changes[0].OriginalLength);
            Assert.Equal(2, changes[0].ModifiedStart);
            Assert.Equal(1, changes[0].ModifiedLength);
        }

        [Fact]
        public void TestDelete()
        {
            var original = "abc";
            var modified = "ac";
            var changes = DiffComputer.ComputeDiff(original, modified);
            
            Assert.Single(changes);
            Assert.Equal(1, changes[0].OriginalStart);
            Assert.Equal(1, changes[0].OriginalLength);
            Assert.Equal(1, changes[0].ModifiedStart);
            Assert.Equal(0, changes[0].ModifiedLength);
        }

        [Fact]
        public void TestReplace()
        {
            var original = "abc";
            var modified = "axc";
            var changes = DiffComputer.ComputeDiff(original, modified);
            
            Assert.Single(changes);
            Assert.Equal(1, changes[0].OriginalStart);
            Assert.Equal(1, changes[0].OriginalLength);
            Assert.Equal(1, changes[0].ModifiedStart);
            Assert.Equal(1, changes[0].ModifiedLength);
        }

        [Fact]
        public void TestEmpty()
        {
            var original = "";
            var modified = "";
            var changes = DiffComputer.ComputeDiff(original, modified);
            
            Assert.Empty(changes);
        }

        [Fact]
        public void TestIdentical()
        {
            var original = "abc";
            var modified = "abc";
            var changes = DiffComputer.ComputeDiff(original, modified);
            
            Assert.Empty(changes);
        }
        
        [Fact]
        public void TestComplex()
        {
            var original = "abcdef";
            var modified = "dacfe";
            var changes = DiffComputer.ComputeDiff(original, modified);
            VerifyChanges(original, modified, changes);
        }

        private void VerifyChanges(string original, string modified, DiffChange[] changes)
        {
            int originalIndex = 0;
            int modifiedIndex = 0;
            
            foreach (var change in changes)
            {
                // Check unchanged part before this change
                int unchangedLen = change.OriginalStart - originalIndex;
                Assert.Equal(unchangedLen, change.ModifiedStart - modifiedIndex); // "Unchanged length mismatch"
                
                if (unchangedLen > 0)
                {
                    var s1 = original.Substring(originalIndex, unchangedLen);
                    var s2 = modified.Substring(modifiedIndex, unchangedLen);
                    Assert.Equal(s1, s2); // "Unchanged content mismatch"
                }
                
                originalIndex = change.OriginalEnd;
                modifiedIndex = change.ModifiedEnd;
            }
            
            // Check remaining tail
            int remainingOriginal = original.Length - originalIndex;
            int remainingModified = modified.Length - modifiedIndex;
            Assert.Equal(remainingOriginal, remainingModified); // "Tail length mismatch"
            
            if (remainingOriginal > 0)
            {
                var s1 = original.Substring(originalIndex, remainingOriginal);
                var s2 = modified.Substring(modifiedIndex, remainingModified);
                Assert.Equal(s1, s2); // "Tail content mismatch"
            }
        }
    }
}
